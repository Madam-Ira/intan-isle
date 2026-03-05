using System;
using System.Collections;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// GPS-based cave system manager.
///
/// Every refreshInterval seconds, checks player GPS against AsiaCaveData.
/// When the player is within a cave's entranceProximityKm AND at ground
/// altitude (< altitudeThresholdM), EnterCave() is fired.
///
/// Hooks:
///   OnCaveEntered(CaveEntry)  — subscribe for narrative / audio events
///   OnCaveExited(CaveEntry)   — subscribe to clean up
///
/// Wires into:
///   CaveEnvironmentController — lighting, fog, particles
///   BarakahMeter              — Barakah gain/drain while inside
///   BunianHUD (via shader global _IntanIsle_InCave 0/1)
/// </summary>
public class CaveManager : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Player is considered 'at a cave entrance' within this km")]
    [SerializeField] private float entranceProximityKm  = 0.5f;
    [Tooltip("Max altitude (metres, Unity Y) to count as ground level")]
    [SerializeField] private float altitudeThresholdM   = 20f;
    [Tooltip("GPS check interval in seconds")]
    [SerializeField] private float refreshInterval      = 2f;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter              barakahMeter;
    [SerializeField] private CaveEnvironmentController envController;

    // ── Events ────────────────────────────────────────────────────
    public static event Action<CaveEntry> OnCaveEntered;
    public static event Action<CaveEntry> OnCaveExited;

    // ── Shader global ─────────────────────────────────────────────
    private static readonly int ID_InCave     = Shader.PropertyToID("_IntanIsle_InCave");
    private static readonly int ID_CaveType   = Shader.PropertyToID("_IntanIsle_CaveType");

    // ── Singleton ─────────────────────────────────────────────────
    public static CaveManager Instance { get; private set; }

    // ── Runtime ───────────────────────────────────────────────────
    private CesiumGlobeAnchor _playerAnchor;
    private Transform         _playerRig;
    private CaveEntry         _currentCave;
    private double            _playerLat;
    private double            _playerLon;
    private float             _barakahTimer;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (barakahMeter  == null) barakahMeter  = FindObjectOfType<BarakahMeter>();
        if (envController == null) envController = FindObjectOfType<CaveEnvironmentController>();

        Shader.SetGlobalFloat(ID_InCave,   0f);
        Shader.SetGlobalFloat(ID_CaveType, 0f);
    }

    IEnumerator Start()
    {
        yield return null;  // wait one frame for Cesium init
        var rigGO = GameObject.Find("PlayerRig");
        if (rigGO != null)
        {
            _playerRig    = rigGO.transform;
            _playerAnchor = rigGO.GetComponent<CesiumGlobeAnchor>();
        }
        StartCoroutine(RefreshLoop());
    }

    void Update()
    {
        if (_currentCave == null || barakahMeter == null) return;

        _barakahTimer += Time.deltaTime;
        if (_barakahTimer < 0.5f) return;
        _barakahTimer = 0f;

        // Apply Barakah rate while inside a cave (always — cave energy is always present)
        float rate = _currentCave.status.BarakahRate();
        if (Mathf.Abs(rate) > 0.001f)
            barakahMeter.AddBarakah(rate * 0.5f,
                rate > 0 ? BarakahSource.HumaneCare : BarakahSource.Restraint);
    }

    // ── Refresh loop ──────────────────────────────────────────────

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            ReadPlayerGPS();
            CheckCaveProximity();
        }
    }

    private void ReadPlayerGPS()
    {
        if (_playerAnchor != null)
        {
            _playerLon = _playerAnchor.longitudeLatitudeHeight.x;
            _playerLat = _playerAnchor.longitudeLatitudeHeight.y;
        }
        else if (_playerRig != null)
        {
            // Fallback: crude world-space offset from Singapore
            const double sgLat = 1.3521, sgLon = 103.8198, mPerLat = 111000.0;
            double mPerLon = mPerLat * Math.Cos(sgLat * Math.PI / 180.0);
            _playerLat = sgLat + _playerRig.position.z / mPerLat;
            _playerLon = sgLon + _playerRig.position.x / mPerLon;
        }
    }

    private void CheckCaveProximity()
    {
        // Only detect cave entry when at ground altitude
        float altitude = _playerRig != null ? _playerRig.position.y : 0f;
        if (altitude > altitudeThresholdM)
        {
            if (_currentCave != null) ExitCurrentCave();
            return;
        }

        var nearest = AsiaCaveData.GetNearest(_playerLat, _playerLon, entranceProximityKm, out double distKm);

        if (nearest != null)
        {
            if (_currentCave == null || _currentCave.name != nearest.name)
                EnterNewCave(nearest, distKm);
        }
        else if (_currentCave != null)
        {
            ExitCurrentCave();
        }
    }

    // ── Cave enter / exit ─────────────────────────────────────────

    private void EnterNewCave(CaveEntry cave, double distKm)
    {
        if (_currentCave != null) ExitCurrentCave();

        _currentCave = cave;

        Shader.SetGlobalFloat(ID_InCave,   1f);
        Shader.SetGlobalFloat(ID_CaveType, (float)cave.caveType);

        if (envController != null) envController.EnterCave(cave);

        OnCaveEntered?.Invoke(cave);

        Debug.Log($"[CaveManager] Entered: {cave.name} ({cave.caveType} / {cave.status}) — {distKm:F2} km away");
    }

    private void ExitCurrentCave()
    {
        var exiting = _currentCave;
        _currentCave = null;

        Shader.SetGlobalFloat(ID_InCave,   0f);
        Shader.SetGlobalFloat(ID_CaveType, 0f);

        if (envController != null) envController.ExitCave();

        OnCaveExited?.Invoke(exiting);

        Debug.Log($"[CaveManager] Exited: {exiting?.name}");
    }

    // ── Public status ─────────────────────────────────────────────
    public bool      InsideCave   => _currentCave != null;
    public CaveEntry CurrentCave  => _currentCave;

    // ── Editor gizmos ─────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw cave entrance spheres in scene view (world-space approx from Singapore)
        const double sgLat = 1.3521, sgLon = 103.8198, mPerLat = 111000.0;
        double mPerLon = mPerLat * Math.Cos(sgLat * Math.PI / 180.0);

        foreach (var cave in AsiaCaveData.All)
        {
            float wx = (float)((cave.longitude - sgLon) * mPerLon);
            float wz = (float)((cave.latitude  - sgLat) * mPerLat);
            var   pos = new Vector3(wx, 0f, wz);

            Gizmos.color = cave.status == CaveStatus.SACRED   ? new Color(1f, 0.8f, 0.2f, 0.6f) :
                           cave.status == CaveStatus.PRISTINE ? new Color(0f, 1f, 0.6f, 0.5f)   :
                           cave.status == CaveStatus.DEGRADED ? new Color(1f, 0.2f, 0f, 0.5f)   :
                                                                new Color(0.5f, 0.5f, 1f, 0.4f);

            Gizmos.DrawWireSphere(pos, cave.radiusM);
            UnityEditor.Handles.Label(pos + Vector3.up * (cave.radiusM + 5f),
                $"{cave.name}\n({cave.caveType.DisplayName()})");
        }
    }
#endif
}
