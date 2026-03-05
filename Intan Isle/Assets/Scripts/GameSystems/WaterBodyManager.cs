using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// GPS-based water body system — spawns Suimono FX for all water types near the player.
///
/// Water surface types   → SUIMONO_Surface prefab
/// Waterfalls / Geysers  → fx_waterfall + fx_mist_waterfall prefabs
/// Hot springs           → SUIMONO_Surface + fx_mist_hotspring prefab
/// Underground rivers    → fx_rippleNormals prefab (subtle, no open surface)
/// Ocean / sea shoreline → shorelineObject prefab added to surface
///
/// Large oceans/seas are data-only (Barakah effects, no 3D surface).
/// All renderable bodies get a CesiumGlobeAnchor so they stay globe-accurate.
/// Barakah effects apply within barakahProximityKm in Veiled World.
/// </summary>
public class WaterBodyManager : MonoBehaviour
{
    [Header("Water Surface Prefabs")]
    [Tooltip("Suimono SUIMONO_Surface prefab — rivers, lakes, reservoirs, glaciers, reefs")]
    [SerializeField] private GameObject waterSurfacePrefab;
    [Tooltip("Suimono shorelineObject prefab — added to large water surfaces")]
    [SerializeField] private GameObject shorelinePrefab;

    [Header("FX Prefabs")]
    [Tooltip("Suimono fx_waterfall prefab")]
    [SerializeField] private GameObject waterfallPrefab;
    [Tooltip("Suimono fx_mist_waterfall prefab")]
    [SerializeField] private GameObject waterfallMistPrefab;
    [Tooltip("Suimono fx_mist_hotspring prefab")]
    [SerializeField] private GameObject hotSpringMistPrefab;
    [Tooltip("Suimono fx_rippleNormals prefab — used for underground rivers")]
    [SerializeField] private GameObject ripplePrefab;

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
        // Find any water bodies within activation+despawn range
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
        foreach (var entry in nearby)
        {
            bool renderable = entry.waterType.IsRenderable()
                           || entry.waterType.IsWaterfall()
                           || entry.waterType.IsUnderground();
            if (!renderable) continue;
            if (_active.ContainsKey(entry.name)) continue;

            double dist = DistKm(_playerLat, _playerLon, entry.latitude, entry.longitude);
            if (dist > activationRadiusKm + entry.radiusKm) continue;

            SpawnWaterBody(entry);
        }
    }

    private void SpawnWaterBody(WaterBodyEntry entry)
    {
        if (entry.waterType.IsWaterfall())
        {
            SpawnWaterfall(entry);
            return;
        }
        if (entry.waterType.IsUnderground())
        {
            SpawnUnderground(entry);
            return;
        }
        SpawnWaterSurface(entry);
    }

    // ── Water surface (rivers, lakes, glaciers, reefs, etc.) ──────

    private void SpawnWaterSurface(WaterBodyEntry entry)
    {
        if (waterSurfacePrefab == null) return;

        var go = Instantiate(waterSurfacePrefab);
        go.name = "Water_" + entry.name;

        // Scale to footprint — cap at 4 km diameter for performance
        float diamM = Mathf.Min(entry.radiusKm * 2000f, 4000f);
        go.transform.localScale = new Vector3(diamM, 1f, diamM);

        var anchor = go.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = new double3(entry.longitude, entry.latitude, 0.5);

        ApplyWaterTint(go, entry.health.SurfaceTint());
        _active[entry.name] = go;

        // FX overlays
        switch (entry.waterType)
        {
            case WaterType.HOT_SPRING:
                SpawnFX(hotSpringMistPrefab, "Mist_", entry, 1.0, ref _mist);
                break;

            case WaterType.LAKE:
            case WaterType.RESERVOIR:
            case WaterType.FJORD:
            case WaterType.CORAL_REEF:
                // Shoreline ring for larger standing water
                if (entry.radiusKm >= 1f)
                    SpawnFX(shorelinePrefab, "Shore_", entry, 0.2, ref _mist);
                break;
        }

        Debug.Log($"[WaterBodyManager] Spawned surface: {entry.name} ({entry.waterType} / {entry.health})");
    }

    // ── Waterfalls / Geysers ──────────────────────────────────────

    private void SpawnWaterfall(WaterBodyEntry entry)
    {
        if (waterfallPrefab == null) return;

        var go = Instantiate(waterfallPrefab);
        go.name = "Waterfall_" + entry.name;
        go.transform.localScale = Vector3.one * Mathf.Clamp(entry.radiusKm * 200f, 1f, 50f);

        var anchor = go.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = new double3(entry.longitude, entry.latitude, 2.0);
        _active[entry.name] = go;

        // Mist at base
        SpawnFX(waterfallMistPrefab, "WFMist_", entry, 0.5, ref _mist);

        Debug.Log($"[WaterBodyManager] Spawned waterfall: {entry.name}");
    }

    // ── Underground rivers ────────────────────────────────────────

    private void SpawnUnderground(WaterBodyEntry entry)
    {
        if (ripplePrefab == null) return;

        var go = Instantiate(ripplePrefab);
        go.name = "Underground_" + entry.name;
        go.transform.localScale = Vector3.one * 3f;

        var anchor = go.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = new double3(entry.longitude, entry.latitude, 0.1);
        _active[entry.name] = go;

        Debug.Log($"[WaterBodyManager] Spawned underground marker: {entry.name}");
    }

    // ── FX helper ────────────────────────────────────────────────

    private void SpawnFX(GameObject prefab, string prefix, WaterBodyEntry entry,
                         double heightOffset, ref Dictionary<string, GameObject> dict)
    {
        if (prefab == null) return;
        var fx = Instantiate(prefab);
        fx.name = prefix + entry.name;
        var a = fx.AddComponent<CesiumGlobeAnchor>();
        a.longitudeLatitudeHeight = new double3(entry.longitude, entry.latitude, heightOffset);
        dict[entry.name] = fx;
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
