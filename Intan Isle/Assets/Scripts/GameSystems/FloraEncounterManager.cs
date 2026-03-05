using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GPS-based flora discovery and encounter system.
///
/// Every refreshInterval seconds, reads the player's GPS position and
/// checks AsiaFloraData for entries whose spawn range overlaps the player.
///
/// When a matching entry is found and the player is in the Veiled World
/// (VeiledWorldManager.InVeiledWorld), the encounter is activated:
///   - Shader globals set for TeamLab glow mode
///   - Barakah discovery bonus awarded (once per ID per session)
///   - Passive proximity Barakah tick begins
///   - OnFloraEncountered event fires (for HUD / SoulLedger)
///   - Interaction mode checked (LIVING_WATER requires nearby water,
///     TIDAL_ZONE requires low tide, CAVE_INTERIOR requires CaveManager,
///     CANOPY_HOVER requires flight)
///
/// Shader globals:
///   _IntanIsle_FloraActive    0/1 — any flora encounter active
///   _IntanIsle_FloraGlowMode  0–5 — maps to FloraInteractionMode
///   _IntanIsle_FloraGlowColor r,g,b via SetGlobalVector
///
/// Discovery state is session-only (reset on play).
/// Persist discovered IDs via PlayerPrefs using SaveDiscoveries() /
/// LoadDiscoveries() when save system is ready.
/// </summary>
public class FloraEncounterManager : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("How often to re-scan nearby flora (seconds)")]
    [SerializeField] private float refreshInterval     = 5f;
    [Tooltip("Extra radius added to each entry's spawnRadiusKm for early detection")]
    [SerializeField] private float detectionBufferKm   = 50f;
    [Tooltip("Encounter activates only when within this km of entry centre")]
    [SerializeField] private float activationRadiusKm  = 200f;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter barakahMeter;

    // ── Singleton ──────────────────────────────────────────────────
    public static FloraEncounterManager Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public static event Action<FloraEntry> OnFloraEncountered;     // first time in range
    public static event Action<FloraEntry> OnFloraDiscovered;      // first time ever (new ID)
    public static event Action<FloraEntry> OnFloraLeft;            // left range

    // ── Shader globals ────────────────────────────────────────────
    private static readonly int SH_FloraActive    = Shader.PropertyToID("_IntanIsle_FloraActive");
    private static readonly int SH_FloraGlowMode  = Shader.PropertyToID("_IntanIsle_FloraGlowMode");
    private static readonly int SH_FloraGlowColor = Shader.PropertyToID("_IntanIsle_FloraGlowColor");

    // ── Runtime ───────────────────────────────────────────────────
    private CesiumForUnity.CesiumGlobeAnchor _playerAnchor;
    private Transform                        _playerRig;
    private double _lat, _lon;
    private float  _barakahTimer;

    // Currently active encounters (id → entry)
    private readonly Dictionary<int, FloraEntry> _active    = new();
    // IDs discovered this session
    private readonly HashSet<int>                _discovered = new();

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (barakahMeter == null) barakahMeter = FindObjectOfType<BarakahMeter>();
        Shader.SetGlobalFloat(SH_FloraActive, 0f);
    }

    IEnumerator Start()
    {
        yield return null;
        var rig = GameObject.Find("PlayerRig");
        if (rig != null)
        {
            _playerRig    = rig.transform;
            _playerAnchor = rig.GetComponent<CesiumForUnity.CesiumGlobeAnchor>();
        }
        StartCoroutine(RefreshLoop());
    }

    void Update()
    {
        if (_active.Count == 0 || barakahMeter == null) return;
        if (!VeiledWorldManager.InVeiledWorld) return;

        _barakahTimer += Time.deltaTime;
        if (_barakahTimer < 0.5f) return;
        _barakahTimer = 0f;

        // Passive proximity Barakah from all active encounters
        float total = 0f;
        foreach (var entry in _active.Values)
            total += entry.conservation.ProximityBarakahRate();

        if (total > 0.001f)
            barakahMeter.AddBarakah(total * 0.5f, BarakahSource.HumaneCare);
    }

    // ── Refresh loop ──────────────────────────────────────────────

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            ReadGPS();
            ScanNearby();
        }
    }

    private void ReadGPS()
    {
        if (_playerAnchor != null)
        {
            _lon = _playerAnchor.longitudeLatitudeHeight.x;
            _lat = _playerAnchor.longitudeLatitudeHeight.y;
        }
        else if (_playerRig != null)
        {
            const double sgLat = 1.3521, sgLon = 103.8198, mPerLat = 111000.0;
            double mPerLon = System.Math.Cos(sgLat * System.Math.PI / 180.0) * mPerLat;
            _lat = sgLat + _playerRig.position.z / mPerLat;
            _lon = sgLon + _playerRig.position.x / mPerLon;
        }
    }

    // ── Scan ──────────────────────────────────────────────────────

    private void ScanNearby()
    {
        var nearby = AsiaFloraData.GetNearby(_lat, _lon, activationRadiusKm + detectionBufferKm);
        var nowActive = new HashSet<int>();

        foreach (var entry in nearby)
        {
            double dist = DistKm(_lat, _lon, entry.latitude, entry.longitude);
            if (dist > activationRadiusKm + entry.spawnRadiusKm) continue;

            // Check interaction mode prerequisites
            if (!MeetsInteractionRequirements(entry)) continue;

            nowActive.Add(entry.id);

            if (!_active.ContainsKey(entry.id))
                ActivateEncounter(entry);
        }

        // Deactivate entries no longer in range
        var toRemove = new List<int>();
        foreach (var id in _active.Keys)
            if (!nowActive.Contains(id))
                toRemove.Add(id);

        foreach (var id in toRemove)
        {
            OnFloraLeft?.Invoke(_active[id]);
            _active.Remove(id);
            Debug.Log($"[FloraEncounter] Left range: ID {id}");
        }

        UpdateShaderGlobals();
    }

    private bool MeetsInteractionRequirements(FloraEntry entry)
    {
        return entry.interactionMode switch
        {
            FloraInteractionMode.FULL_IMMERSION =>
                VeiledWorldManager.InVeiledWorld,

            FloraInteractionMode.LIVING_WATER =>
                VeiledWorldManager.InVeiledWorld &&
                WaterBodyManager_HasNearbyWater(),

            FloraInteractionMode.TIDAL_ZONE =>
                VeiledWorldManager.InVeiledWorld &&
                TideService.Instance != null &&
                TideService.Instance.Current.state == TideState.LOW,

            FloraInteractionMode.CAVE_INTERIOR =>
                CaveManager.Instance != null && CaveManager.Instance.InsideCave,

            FloraInteractionMode.CANOPY_HOVER =>
                VeiledWorldManager.InVeiledWorld &&
                _playerRig != null && _playerRig.position.y > 10f,

            FloraInteractionMode.GROUND_LEVEL =>
                true,   // always accessible

            FloraInteractionMode.QUIET_FOREST =>
                true,   // medicinal/contemplative — accessible regardless of Veiled World state

            _ => true,
        };
    }

    private void ActivateEncounter(FloraEntry entry)
    {
        _active[entry.id] = entry;
        OnFloraEncountered?.Invoke(entry);

        bool firstTime = !_discovered.Contains(entry.id);
        if (firstTime)
        {
            _discovered.Add(entry.id);
            OnFloraDiscovered?.Invoke(entry);

            // One-time discovery Barakah
            float bonus = entry.discoveryBarakahBonus > 0f
                ? entry.discoveryBarakahBonus
                : entry.conservation.DiscoveryBarakah();

            if (barakahMeter != null && bonus > 0f)
                barakahMeter.AddBarakah(bonus, BarakahSource.HumaneCare);

            Debug.Log($"[FloraEncounter] NEW DISCOVERY: {entry.name} ({entry.scientific}) — " +
                      $"+{bonus:F1} Barakah  [{entry.conservation.DisplayName()}]");
        }
        else
        {
            Debug.Log($"[FloraEncounter] Re-encounter: {entry.name}");
        }
    }

    private void UpdateShaderGlobals()
    {
        if (_active.Count == 0)
        {
            Shader.SetGlobalFloat(SH_FloraActive, 0f);
            return;
        }

        Shader.SetGlobalFloat(SH_FloraActive, 1f);

        // Use the rarest active entry for glow mode + colour
        FloraEntry rarest = null;
        foreach (var e in _active.Values)
        {
            if (rarest == null || (int)e.conservation < (int)rarest.conservation)
                rarest = e;
        }

        if (rarest != null)
        {
            Shader.SetGlobalFloat(SH_FloraGlowMode, (float)rarest.interactionMode);
            Color c = rarest.conservation.BadgeColor();
            Shader.SetGlobalVector(SH_FloraGlowColor, new Vector4(c.r, c.g, c.b, c.a));
        }
    }

    // ── Public API ────────────────────────────────────────────────

    public IReadOnlyDictionary<int, FloraEntry> ActiveEncounters => _active;
    public IReadOnlyCollection<int> DiscoveredIds => _discovered;
    public int DiscoveryCount => _discovered.Count;
    public int TotalCount => AsiaFloraData.All.Count;

    /// <summary>Returns a compact HUD line for the rarest active encounter.</summary>
    public string HUDEncounterLine()
    {
        FloraEntry rarest = null;
        foreach (var e in _active.Values)
            if (rarest == null || (int)e.conservation < (int)rarest.conservation)
                rarest = e;

        if (rarest == null) return string.Empty;

        return $"{rarest.name}  <size=75%><i>{rarest.scientific}</i></size>\n" +
               $"<size=75%>{rarest.conservation.DisplayName()}</size>";
    }

    /// <summary>
    /// Persist discovered IDs to PlayerPrefs.
    /// Call from game save system when implemented.
    /// </summary>
    public void SaveDiscoveries()
    {
        PlayerPrefs.SetString("FloraDiscovered", string.Join(",", _discovered));
        PlayerPrefs.Save();
    }

    /// <summary>Load persisted IDs on game start.</summary>
    public void LoadDiscoveries()
    {
        string raw = PlayerPrefs.GetString("FloraDiscovered", "");
        if (string.IsNullOrEmpty(raw)) return;
        foreach (var s in raw.Split(','))
            if (int.TryParse(s, out int id)) _discovered.Add(id);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private bool WaterBodyManager_HasNearbyWater()
    {
        var wbm = FindObjectOfType<WaterBodyManager>();
        return wbm != null && wbm.ActiveSurfaceCount > 0;
    }

    private static double DistKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = lat1 - lat2, dLon = lon1 - lon2;
        return System.Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
    }
}
