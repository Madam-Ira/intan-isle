using System.Collections;
using UnityEngine;

/// <summary>
/// Applies real-world tide data from TideService to the game environment:
///
///   Water surface offset
///     Active Suimono water surfaces shift vertically with the tide.
///     WaterBodyManager.SetTideOffset(metres) nudges all spawned coastal
///     surfaces up / down relative to their base globe-anchor height.
///
///   Tidal cave gating
///     CaveManager respects tidalAccessThresholdM on SEA_CAVE / TIDAL
///     entries — CaveManager will not enter them unless the tide is below
///     that threshold.
///
///   Shader globals
///     _IntanIsle_TideHeight    0-1 normalised height (0 = low, 1 = high)
///     _IntanIsle_TideState     0=rising 1=high 2=falling 3=low
///
///   Barakah effects (0.5 s tick)
///     HIGH tide  → +0.6/s  (ocean fullness, life abundance)
///     RISING     → +0.3/s  (momentum, vitality)
///     LOW tide   → -0.4/s  (exposed mud, ecology stress)
///     FALLING    → neutral
///     Haze + low tide (double ecological stress) → extra -0.5/s
///
///   Ambient light tint
///     Tide HEIGHT subtly shifts ambient light — a high sea reflects more
///     light onto nearby land; low tide exposes wet sand (warm tone).
///
///   All changes are smooth, using coroutine lerps.
/// </summary>
public class TideEnvironmentController : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Maximum vertical shift applied to coastal water surfaces (metres)")]
    [SerializeField] private float maxSurfaceShiftM = 2.0f;
    [Tooltip("Speed of ambient colour lerp on tide change")]
    [SerializeField] private float ambientLerpSpeed = 0.15f;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter    barakahMeter;
    [SerializeField] private WaterBodyManager waterBodyManager;

    // ── Shader globals ────────────────────────────────────────────
    private static readonly int SH_TideHeight = Shader.PropertyToID("_IntanIsle_TideHeight");
    private static readonly int SH_TideState  = Shader.PropertyToID("_IntanIsle_TideState");

    // ── Runtime ───────────────────────────────────────────────────
    private TideState _currentState = TideState.RISING;
    private float     _barakahTimer;
    private float     _targetNorm;
    private float     _currentNorm;
    private Coroutine _ambientCo;

    // Ambient tint targets
    private Color _savedAmbientSky;
    private bool  _stateSaved;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (barakahMeter    == null) barakahMeter    = FindObjectOfType<BarakahMeter>();
        if (waterBodyManager == null) waterBodyManager = FindObjectOfType<WaterBodyManager>();
    }

    void OnEnable()
    {
        TideService.OnTideUpdated      += HandleTideUpdated;
        TideService.OnTideStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        TideService.OnTideUpdated      -= HandleTideUpdated;
        TideService.OnTideStateChanged -= HandleStateChanged;
    }

    void Update()
    {
        // Smooth tide height for shader + water surface
        _currentNorm = Mathf.MoveTowards(_currentNorm, _targetNorm, Time.deltaTime * 0.02f);
        Shader.SetGlobalFloat(SH_TideHeight, _currentNorm);

        float offsetM = (_currentNorm - 0.5f) * 2f * maxSurfaceShiftM;
        waterBodyManager?.SetTideOffset(offsetM);

        // Barakah tick
        _barakahTimer += Time.deltaTime;
        if (_barakahTimer >= 0.5f)
        {
            _barakahTimer = 0f;
            ApplyBarakah();
        }
    }

    // ── Event handlers ────────────────────────────────────────────

    private void HandleTideUpdated(TideSnapshot snap)
    {
        _targetNorm = snap.normalisedHeight;
        Shader.SetGlobalFloat(SH_TideState, (float)snap.state);
    }

    private void HandleStateChanged(TideSnapshot snap)
    {
        _currentState = snap.state;

        if (!_stateSaved)
        {
            _savedAmbientSky = RenderSettings.ambientSkyColor;
            _stateSaved = true;
        }

        Color targetTint = TideAmbientTint(snap.state, snap.normalisedHeight);
        if (_ambientCo != null) StopCoroutine(_ambientCo);
        _ambientCo = StartCoroutine(LerpAmbient(targetTint));

        Debug.Log($"[TideEnvironment] Tide state → {snap.state}  height={snap.heightM:F2}m  norm={snap.normalisedHeight:F2}");
    }

    // ── Ambient colour nudge ──────────────────────────────────────
    // Subtle only — weather and cave take priority. We only shift the
    // sky ambient slightly toward a tidal tint.

    private static Color TideAmbientTint(TideState state, float norm)
    {
        Color base_ = RenderSettings.ambientSkyColor;
        return state switch
        {
            TideState.HIGH    => Color.Lerp(base_, new Color(0.45f, 0.58f, 0.75f), 0.12f),  // ocean-blue tint
            TideState.RISING  => Color.Lerp(base_, new Color(0.48f, 0.60f, 0.72f), 0.06f),
            TideState.LOW     => Color.Lerp(base_, new Color(0.62f, 0.58f, 0.45f), 0.10f),  // warm mud-flat tint
            TideState.FALLING => base_,
            _                 => base_,
        };
    }

    private IEnumerator LerpAmbient(Color target)
    {
        Color start = RenderSettings.ambientSkyColor;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * ambientLerpSpeed);
            RenderSettings.ambientSkyColor = Color.Lerp(start, target, t);
            yield return null;
        }
    }

    // ── Barakah ───────────────────────────────────────────────────

    private void ApplyBarakah()
    {
        if (barakahMeter == null) return;
        if (CaveManager.Instance != null && CaveManager.Instance.InsideCave) return;

        float rate = _currentState switch
        {
            TideState.HIGH    =>  0.6f,
            TideState.RISING  =>  0.3f,
            TideState.FALLING =>  0.0f,
            TideState.LOW     => -0.4f,
            _                 =>  0.0f,
        };

        // Compounding ecological stress: haze + low tide
        if (_currentState == TideState.LOW &&
            WeatherService.Instance != null && WeatherService.Instance.IsHazardous)
            rate -= 0.5f;

        if (Mathf.Abs(rate) > 0.001f)
            barakahMeter.AddBarakah(rate * 0.5f,
                rate > 0 ? BarakahSource.HumaneCare : BarakahSource.Restraint);
    }
}
