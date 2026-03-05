using System;
using UnityEngine;

/// <summary>
/// Bridges IntanIsleZoneData to URP shaders via Shader.SetGlobal*.
/// Attach to: PlayerRig or a persistent GameSystems object.
///
/// Global shader properties set every frame:
///   _IntanIsle_ZoneType       float  0–13 (ZoneType enum)
///   _IntanIsle_ZoneBlend      float  0=fully inside zone, 1=at edge
///   _IntanIsle_PrevZoneType   float  previous zone (for smooth transition)
///   _IntanIsle_DayPhase       float  0–5 (EmotionalDayNightCycle phase)
///   _IntanIsle_TimeNorm       float  0–1 within current phase
///   _IntanIsle_IsVeiledWorld  float  0 or 1
/// </summary>
public class ZoneShaderLinker : MonoBehaviour
{
    // ── Singapore origin for world-space offset computation ───────
    private const double SG_Lat = 1.3521;
    private const double SG_Lon = 103.8198;
    private const double M_PER_LAT = 111000.0;  // metres per degree latitude

    [Header("Data")]
    [SerializeField] private IntanIsleZoneData zoneData;

    [Header("Zone Transition")]
    [Tooltip("Time in seconds to blend between two zone types")]
    [SerializeField] private float blendSpeed = 2.0f;

    [Header("GPS Source (optional — falls back to Transform)")]
    [SerializeField] private Transform playerRig;

    // ── Runtime state ─────────────────────────────────────────────
    private float   _currentZoneType = 0f;
    private float   _prevZoneType    = 0f;
    private float   _blendFactor     = 0f;
    private float   _targetBlend     = 0f;
    private double  _playerLat       = SG_Lat;
    private double  _playerLon       = SG_Lon;

    // ── Property IDs ─────────────────────────────────────────────
    private static readonly int ID_ZoneType      = Shader.PropertyToID("_IntanIsle_ZoneType");
    private static readonly int ID_ZoneBlend     = Shader.PropertyToID("_IntanIsle_ZoneBlend");
    private static readonly int ID_PrevZoneType  = Shader.PropertyToID("_IntanIsle_PrevZoneType");
    private static readonly int ID_DayPhase      = Shader.PropertyToID("_IntanIsle_DayPhase");
    private static readonly int ID_TimeNorm      = Shader.PropertyToID("_IntanIsle_TimeNorm");
    private static readonly int ID_IsVeiled      = Shader.PropertyToID("_IntanIsle_IsVeiledWorld");

    void Awake()
    {
        if (zoneData == null) zoneData = IntanIsleZoneData.Get();
        if (playerRig == null) playerRig = transform;
        ComputeWorldPositions();
    }

    void Update()
    {
        EstimatePlayerLatLon();
        UpdateZone();
        PushGlobals();
    }

    // ── Compute world-space XZ for every zone (once at startup) ──
    private void ComputeWorldPositions()
    {
        if (zoneData == null) return;
        double mPerLon = M_PER_LAT * Math.Cos(SG_Lat * Math.PI / 180.0);

        foreach (var z in zoneData.zones)
        {
            double dx = (z.longitude - SG_Lon) * mPerLon;
            double dz = (z.latitude  - SG_Lat) * M_PER_LAT;
            z.worldXZ     = new Vector2((float)dx, (float)dz);
            z.worldPosReady = true;
        }
    }

    // ── Estimate player lat/lon from world position ───────────────
    private void EstimatePlayerLatLon()
    {
        // Prefer CesiumGlobeAnchor if available on playerRig
#if CESIUM_FOR_UNITY
        var anchor = playerRig.GetComponent<CesiumForUnity.CesiumGlobeAnchor>();
        if (anchor != null)
        {
            _playerLon = anchor.longitudeLatitudeHeight.x;
            _playerLat = anchor.longitudeLatitudeHeight.y;
            return;
        }
#endif
        // Fall back: reverse-compute from world XZ
        double mPerLon = M_PER_LAT * Math.Cos(SG_Lat * Math.PI / 180.0);
        _playerLat = SG_Lat + playerRig.position.z / M_PER_LAT;
        _playerLon = SG_Lon + playerRig.position.x / mPerLon;
    }

    // ── Find dominant zone and blend toward it ────────────────────
    private void UpdateZone()
    {
        if (zoneData == null) return;

        var zone        = zoneData.GetDominantZone(_playerLat, _playerLon);
        float newType   = zone != null ? (float)zone.zoneType : 0f;
        _targetBlend    = zoneData.GetBlendFactor(_playerLat, _playerLon, zone);

        if (!Mathf.Approximately(newType, _currentZoneType))
        {
            _prevZoneType    = _currentZoneType;
            _currentZoneType = newType;
        }

        _blendFactor = Mathf.MoveTowards(_blendFactor, _targetBlend, Time.deltaTime * blendSpeed);
    }

    // ── Push all globals to shader system ────────────────────────
    private void PushGlobals()
    {
        Shader.SetGlobalFloat(ID_ZoneType,     _currentZoneType);
        Shader.SetGlobalFloat(ID_ZoneBlend,    _blendFactor);
        Shader.SetGlobalFloat(ID_PrevZoneType, _prevZoneType);

        // DayPhase + TimeNorm filled by EmotionalDayNightCycle (or defaults)
        // IsVeiledWorld filled by VeiledWorldManager (or defaults)
    }

    // ── Public: called by VeiledWorldManager ──────────────────────
    public void SetVeiledWorld(bool veiled)
        => Shader.SetGlobalFloat(ID_IsVeiled, veiled ? 1f : 0f);

    // ── Public: called by EmotionalDayNightCycle ──────────────────
    public void SetDayPhase(float phase, float timeNorm)
    {
        Shader.SetGlobalFloat(ID_DayPhase,  phase);
        Shader.SetGlobalFloat(ID_TimeNorm, timeNorm);
    }

    // ── Gizmos ────────────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (zoneData == null) return;
        foreach (var z in zoneData.zones)
        {
            if (!z.worldPosReady) continue;
            Vector3 centre = new Vector3(z.worldXZ.x, 0, z.worldXZ.y);
            Gizmos.color   = z.zoneType.IsHealthy() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(centre, z.radiusKm * 1000f);
            UnityEditor.Handles.Label(centre + Vector3.up * 500f, z.zoneName);
        }
    }
#endif
}
