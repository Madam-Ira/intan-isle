using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// GPS-based water body system — spawns Suimono water surfaces for rivers,
/// lakes, reservoirs, hot springs, and crater lakes near the player.
///
/// Large seas and oceans are data-only (Barakah effects only).
/// Renderable water types (RIVER, LAKE, RESERVOIR, HOT_SPRING, CRATER_LAKE,
/// WETLAND, ESTUARY) get a Suimono surface spawned when the player comes
/// within activationRadiusKm, and despawned beyond despawnRadiusKm.
///
/// Also applies per-second Barakah effects when the player is within
/// barakahProximityKm of any water body.
///
/// Requires:
///   - SUIMONO_Surface prefab assigned in Inspector
///   - Optional: fx_mist_hotspring prefab for hot springs
///   - BarakahMeter auto-found
/// </summary>
public class WaterBodyManager : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Suimono SUIMONO_Surface prefab")]
    [SerializeField] private GameObject waterSurfacePrefab;
    [Tooltip("Suimono fx_mist_hotspring prefab (optional)")]
    [SerializeField] private GameObject hotSpringMistPrefab;

    [Header("Proximity")]
    [Tooltip("Spawn Suimono surface when player is within this many km of a renderable water body")]
    [SerializeField] private float activationRadiusKm  = 2f;
    [Tooltip("Despawn when player moves beyond this distance")]
    [SerializeField] private float despawnRadiusKm     = 4f;
    [Tooltip("Apply Barakah effects within this km")]
    [SerializeField] private float barakahProximityKm  = 1f;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter barakahMeter;

    [Header("GPS Refresh")]
    [Tooltip("How often to re-check player GPS and update active water bodies (seconds)")]
    [SerializeField] private float refreshInterval = 3f;

    // ── Internal ──────────────────────────────────────────────────
    private CesiumGeoreference _geo;
    private CesiumGlobeAnchor  _playerAnchor;
    private BarakahMeter       _barakah;

    // Active spawned water bodies: water body name → spawned GO
    private Dictionary<string, GameObject> _active = new Dictionary<string, GameObject>();
    // Same map for hot spring mist FX
    private Dictionary<string, GameObject> _mist   = new Dictionary<string, GameObject>();

    private double _playerLat;
    private double _playerLon;
    private float  _barakahTimer;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (barakahMeter == null) barakahMeter = FindObjectOfType<BarakahMeter>();
        _barakah = barakahMeter;
    }

    IEnumerator Start()
    {
        // Wait a frame for Cesium to initialise
        yield return null;

        _geo = FindObjectOfType<CesiumGeoreference>();

        var playerRig = GameObject.Find("PlayerRig");
        if (playerRig != null)
            _playerAnchor = playerRig.GetComponent<CesiumGlobeAnchor>();

        StartCoroutine(RefreshLoop());
    }

    void Update()
    {
        if (_barakah == null || !VeiledWorldManager.InVeiledWorld) return;

        _barakahTimer += Time.deltaTime;
        if (_barakahTimer < 0.5f) return;
        _barakahTimer = 0f;

        ApplyWaterBarakah();
    }

    // ── Refresh loop ──────────────────────────────────────────────

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            ReadPlayerGPS();
            ManageWaterSurfaces();
        }
    }

    private void ReadPlayerGPS()
    {
        if (_playerAnchor != null)
        {
            _playerLon = _playerAnchor.longitudeLatitudeHeight.x;
            _playerLat = _playerAnchor.longitudeLatitudeHeight.y;
        }
        else
        {
            // Fallback: Singapore origin
            _playerLat = 1.3521;
            _playerLon = 103.8198;
        }
    }

    // ── Spawn / despawn water surfaces ───────────────────────────

    private void ManageWaterSurfaces()
    {
        // Find renderable water bodies within activation+despawn range
        var nearby = AsiaWaterData.GetNearby(_playerLat, _playerLon, despawnRadiusKm + 50f);

        var toRemove = new List<string>();

        // Despawn bodies now out of range
        foreach (var kv in _active)
        {
            var entry = AsiaWaterData.All.Find(w => w.name == kv.Key);
            if (entry == null) { toRemove.Add(kv.Key); continue; }

            double dist = DistKm(_playerLat, _playerLon, entry.latitude, entry.longitude);
            if (dist > despawnRadiusKm + entry.radiusKm)
            {
                Destroy(kv.Value);
                if (_mist.TryGetValue(kv.Key, out var m)) Destroy(m);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var k in toRemove) { _active.Remove(k); _mist.Remove(k); }

        // Spawn bodies now in range
        if (waterSurfacePrefab == null) return;

        foreach (var entry in nearby)
        {
            if (!entry.waterType.IsRenderable()) continue;
            if (_active.ContainsKey(entry.name)) continue;

            double dist = DistKm(_playerLat, _playerLon, entry.latitude, entry.longitude);
            if (dist > activationRadiusKm + entry.radiusKm) continue;

            SpawnWaterSurface(entry);
        }
    }

    private void SpawnWaterSurface(WaterBodyEntry entry)
    {
        // Place at GPS coordinates via CesiumGlobeAnchor
        var go = Instantiate(waterSurfacePrefab);
        go.name = "Water_" + entry.name;

        // Scale to water body footprint — cap at 4 km diameter
        float diamM = Mathf.Min(entry.radiusKm * 2000f, 4000f);
        go.transform.localScale = new Vector3(diamM, 1f, diamM);

        // Attach CesiumGlobeAnchor so it stays at correct globe position
        var anchor = go.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = new double3(
            entry.longitude,
            entry.latitude,
            0.5); // 0.5 m above terrain datum

        // Tint water colour by health if a Renderer is available
        ApplyWaterTint(go, entry.health.SurfaceTint());

        _active[entry.name] = go;

        // Hot spring mist FX
        if (entry.waterType == WaterType.HOT_SPRING && hotSpringMistPrefab != null)
        {
            var mist = Instantiate(hotSpringMistPrefab);
            mist.name = "Mist_" + entry.name;
            var ma = mist.AddComponent<CesiumGlobeAnchor>();
            ma.longitudeLatitudeHeight = new double3(entry.longitude, entry.latitude, 1.0);
            _mist[entry.name] = mist;
        }

        Debug.Log($"[WaterBodyManager] Spawned: {entry.name} ({entry.waterType} / {entry.health})");
    }

    private void ApplyWaterTint(GameObject go, Color tint)
    {
        foreach (var rend in go.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in rend.materials)
            {
                // Suimono uses _WaterColor; fallback to _Color and _BaseColor
                if (mat.HasProperty("_WaterColor"))      mat.SetColor("_WaterColor", tint);
                else if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", tint);
                else if (mat.HasProperty("_Color"))      mat.SetColor("_Color", tint);
            }
        }
    }

    // ── Barakah effects ───────────────────────────────────────────

    private void ApplyWaterBarakah()
    {
        float totalRate = 0f;

        foreach (var entry in AsiaWaterData.All)
        {
            double dist = DistKm(_playerLat, _playerLon, entry.latitude, entry.longitude);
            if (dist > barakahProximityKm) continue;

            totalRate += entry.health.BarakahRate();
        }

        if (Mathf.Abs(totalRate) > 0.001f)
            _barakah.AddBarakah(totalRate * 0.5f, BarakahSource.HumaneCare);
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static double DistKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = lat1 - lat2;
        double dLon = lon1 - lon2;
        return System.Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
    }

    // ── Public status ─────────────────────────────────────────────
    public int ActiveSurfaceCount => _active.Count;
}
