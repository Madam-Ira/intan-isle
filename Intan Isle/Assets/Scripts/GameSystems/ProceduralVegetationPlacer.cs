using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dense procedural vegetation — no sparse terrain.
/// Every visible ground must have layered vegetation:
///   Canopy → Understory → Groundcover → Vines → Epiphytes
///
/// Placement:
///   - Uses Physics.Raycast from above to find ground
///   - Zone-aware density (POLLUTION/TOXIC = zero vegetation)
///   - Streams: place 50 trees at start, cull > 2km behind player
///   - Refreshes every 30s as player moves
///
/// Assign prefab arrays in Inspector for each vegetation layer.
/// </summary>
public class ProceduralVegetationPlacer : MonoBehaviour
{
    // ── Prefab lists (assign in Inspector) ───────────────────────

    [Header("Canopy Trees (tallest layer)")]
    [SerializeField] private GameObject[] canopyPrefabs;

    [Header("Understory (mid layer)")]
    [SerializeField] private GameObject[] understoryPrefabs;

    [Header("Groundcover + Grasses")]
    [SerializeField] private GameObject[] groundcoverPrefabs;

    [Header("Kampung Heritage Trees")]
    [SerializeField] private GameObject[] kampungPrefabs;

    // ── Placement settings ────────────────────────────────────────

    [Header("Density")]
    [SerializeField] private float placementRadius  = 200f;   // around player
    [SerializeField] private int   canopyCount      = 50;     // on startup
    [SerializeField] private int   understoryCount  = 80;
    [SerializeField] private int   groundcoverCount = 120;
    [SerializeField] private float cullDistance     = 2000f;  // behind player
    [SerializeField] private float refreshInterval  = 30f;

    [Header("Slope Rules")]
    [SerializeField] private float slopeDenseAngle  = 15f;    // > this → highland trees

    [Header("Raycast")]
    [SerializeField] private float raycastHeight    = 2000f;
    [SerializeField] private LayerMask groundMask   = ~0;

    // ── Runtime ───────────────────────────────────────────────────
    private Transform             _player;
    private List<GameObject>      _placed    = new List<GameObject>();
    private GameObject            _vegRoot;
    private ZoneType              _currentZone = ZoneType.FOREST;

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Requires Cesium physics meshes to be loaded before raycasting.
        // Disabled at runtime until Cesium terrain is ready.
        // Enable manually after Cesium tiles have fully streamed in.
        enabled = false;
        Debug.Log("[VegetationPlacer] Disabled — enable this component manually once Cesium terrain has loaded.");
    }

    public void BeginPlacement()
    {
        enabled  = true;
        _player  = FindPlayerRig();
        _vegRoot = new GameObject("ProceduralVegetation");
        _vegRoot.transform.SetParent(transform);

        StartCoroutine(InitialPlacement());
        StartCoroutine(StreamingLoop());
    }

    // ── Initial 50 trees around player start ─────────────────────

    private IEnumerator InitialPlacement()
    {
        yield return new WaitForSeconds(1f); // Wait for Cesium terrain to settle

        PlaceBatch(canopyCount,      canopyPrefabs,      placementRadius);
        PlaceBatch(understoryCount,  understoryPrefabs,  placementRadius * 0.8f);
        PlaceBatch(groundcoverCount, groundcoverPrefabs, placementRadius * 0.6f);

        Debug.Log("[ProceduralVegetation] Initial placement: "
            + canopyCount + " canopy + " + understoryCount + " understory + "
            + groundcoverCount + " groundcover placed.");
    }

    // ── Streaming: refresh as player moves ───────────────────────

    private IEnumerator StreamingLoop()
    {
        Vector3 lastRefreshPos = Vector3.one * float.MaxValue;

        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            if (_player == null) continue;

            float moved = Vector3.Distance(_player.position, lastRefreshPos);
            if (moved < placementRadius * 0.5f) continue; // not moved enough

            // Cull vegetation > cullDistance behind player
            CullDistant();

            // Update zone from global shader property
            float zt = Shader.GetGlobalFloat("_IntanIsle_ZoneType");
            _currentZone = (ZoneType)(int)zt;

            // No vegetation in dead zones
            if (_currentZone.IsToxic()
                && _currentZone != ZoneType.TRANSBOUNDARY_HAZE
                && _currentZone != ZoneType.DEFORESTATION)
            {
                lastRefreshPos = _player.position;
                continue;
            }

            // Place new batch ahead of player
            PlaceBatch(canopyCount / 2,      canopyPrefabs,      placementRadius);
            PlaceBatch(understoryCount / 2,  understoryPrefabs,  placementRadius * 0.8f);
            PlaceBatch(groundcoverCount / 2, groundcoverPrefabs, placementRadius * 0.6f);

            lastRefreshPos = _player.position;
        }
    }

    // ── Core placement logic ──────────────────────────────────────

    private void PlaceBatch(int count, GameObject[] prefabs, float radius)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        if (_player == null) return;

        float zt = Shader.GetGlobalFloat("_IntanIsle_ZoneType");
        _currentZone = (ZoneType)(int)zt;

        // Determine density multiplier from zone
        float densityMult = GetDensityMultiplier(_currentZone);
        int   actualCount = Mathf.RoundToInt(count * densityMult);

        for (int i = 0; i < actualCount; i++)
        {
            Vector2 rand2D = Random.insideUnitCircle * radius;
            Vector3 origin = _player.position + new Vector3(rand2D.x, raycastHeight, rand2D.y);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundMask))
                continue;

            // Slope check — steep slopes → highland trees only
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            GameObject[] pool = GetPrefabPool(slopeAngle, _currentZone, prefabs);
            if (pool == null || pool.Length == 0) continue;

            var prefab = pool[Random.Range(0, pool.Length)];
            if (prefab == null) continue;

            // Random scale variation (Ghibli: no two trees identical)
            float scale = Random.Range(0.75f, 1.35f);
            float yRot  = Random.Range(0f, 360f);

            var go = Instantiate(prefab, hit.point, Quaternion.Euler(0, yRot, 0), _vegRoot.transform);
            go.transform.localScale = Vector3.one * scale;
            _placed.Add(go);
        }
    }

    private float GetDensityMultiplier(ZoneType z) => z switch
    {
        ZoneType.ANCIENT_FOREST    => 1.5f,  // maximum density
        ZoneType.PROTECTED_FOREST  => 1.3f,
        ZoneType.SACRED_FOREST     => 1.2f,
        ZoneType.FOREST            => 1.0f,
        ZoneType.MANGROVE          => 0.9f,
        ZoneType.KAMPUNG_HERITAGE  => 0.7f,
        ZoneType.FOOD_SECURITY     => 0.6f,
        ZoneType.WATERWAY          => 0.5f,
        ZoneType.DEFORESTATION     => 0.1f,  // sparse dead
        ZoneType.TRANSBOUNDARY_HAZE=> 0.15f,
        ZoneType.CORAL_DEGRADATION => 0.2f,
        ZoneType.POLLUTION         => 0.0f,  // zero
        ZoneType.TOXIC             => 0.0f,
        ZoneType.RIVER_POLLUTION   => 0.05f,
        _                          => 1.0f,
    };

    private GameObject[] GetPrefabPool(float slope, ZoneType zone, GameObject[] defaultPool)
    {
        // Highland slope: use canopy (tall highland trees)
        if (slope > slopeDenseAngle) return canopyPrefabs ?? defaultPool;

        // Kampung zones: use kampung-specific if available
        if (zone == ZoneType.KAMPUNG_HERITAGE && kampungPrefabs != null && kampungPrefabs.Length > 0)
            return kampungPrefabs;

        return defaultPool;
    }

    private void CullDistant()
    {
        if (_player == null) return;
        for (int i = _placed.Count - 1; i >= 0; i--)
        {
            if (_placed[i] == null) { _placed.RemoveAt(i); continue; }
            if (Vector3.Distance(_placed[i].transform.position, _player.position) > cullDistance)
            {
                Destroy(_placed[i]);
                _placed.RemoveAt(i);
            }
        }
    }

    private Transform FindPlayerRig()
    {
        var go = GameObject.Find("PlayerRig");
        return go != null ? go.transform : transform;
    }

    // ── Public status ─────────────────────────────────────────────
    public int PlacedCount => _placed.Count;
}
