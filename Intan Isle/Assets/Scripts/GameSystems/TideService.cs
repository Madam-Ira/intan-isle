using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// ── Tide state ─────────────────────────────────────────────────────────────

public enum TideState { RISING, HIGH, FALLING, LOW }
public enum TideExtremeType { HIGH, LOW }

// ── Tide snapshot ──────────────────────────────────────────────────────────

public struct TideSnapshot
{
    public float          heightM;           // current water height in metres (chart datum)
    public float          normalisedHeight;  // 0 = lowest today, 1 = highest today
    public TideState      state;
    public TideExtremeType nextExtremeType;
    public DateTime       nextExtremeTime;
    public float          nextExtremeHeightM;
    public float          rangeM;            // today's tidal range (high - low)
    public bool           isLiveData;        // true = WorldTides API, false = harmonic estimate
}

// ── WorldTides JSON shims ─────────────────────────────────────────────────

[Serializable] class WT_Root    { public int status; public WT_Height[] heights; public WT_Extreme[] extremes; }
[Serializable] class WT_Height  { public long dt; public float height; }
[Serializable] class WT_Extreme { public long dt; public float height; public string type; }

/// <summary>
/// Real-world tide service using the WorldTides API (worldtides.info).
///
/// Setup:
///   1. Register at worldtides.info — free tier: 50 requests/day
///   2. Paste your API key into the "Api Key" field in the Inspector
///
/// Without an API key, a 5-constituent harmonic model provides a plausible
/// SE-Asian mixed-semidiurnal tide pattern (M2 + S2 + K1 + O1 + N2).
///
/// Data is fetched once per fetch interval (default 6 h) for the player's
/// GPS location and cached. CurrentHeightM interpolates in real-time from
/// the cached series.
///
/// Events:
///   OnTideUpdated(TideSnapshot)  — fired every updateInterval seconds
///   OnTideStateChanged(TideSnapshot) — fired when state (RISING/HIGH/…) changes
///
/// Integrations:
///   TideEnvironmentController — reads snapshot for visuals + Barakah
///   CaveManager               — gates SEA_CAVE / TIDAL access by height
///   WaterBodyManager          — adjusts coastal surface heights
///   BunianHUD                 — tide readout panel
/// </summary>
public class TideService : MonoBehaviour
{
    [Header("API")]
    [Tooltip("WorldTides API key — worldtides.info (free tier: 50 req/day)")]
    [SerializeField] private string apiKey = "";
    [Tooltip("How often to re-fetch tide data from the server (hours)")]
    [SerializeField] private float fetchIntervalHours = 6f;
    [Tooltip("How often to re-evaluate CurrentHeightM from cached data (seconds)")]
    [SerializeField] private float updateInterval = 60f;

    [Header("Fallback GPS")]
    [SerializeField] private float fallbackLat =  1.3521f;
    [SerializeField] private float fallbackLon = 103.8198f;

    [Header("Display")]
    [Tooltip("Label shown in HUD when using harmonic estimate instead of live data")]
    [SerializeField] private string harmonicLabel = "~est.";

    // ── Singleton ──────────────────────────────────────────────────
    public static TideService Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public static event Action<TideSnapshot> OnTideUpdated;
    public static event Action<TideSnapshot> OnTideStateChanged;

    // ── Public state ──────────────────────────────────────────────
    public TideSnapshot Current     { get; private set; }
    public bool         HasLiveData { get; private set; }

    // ── Cached series ─────────────────────────────────────────────
    private WT_Height[]  _heights;    // hourly heights from API
    private WT_Extreme[] _extremes;   // high/low extremes from API
    private float        _todayMin = float.MaxValue;
    private float        _todayMax = float.MinValue;

    // ── Runtime ───────────────────────────────────────────────────
    private CesiumForUnity.CesiumGlobeAnchor _playerAnchor;
    private double    _lat, _lon;
    private TideState _lastState = TideState.RISING;
    private float     _updateTimer;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        var rig = GameObject.Find("PlayerRig");
        if (rig != null) _playerAnchor = rig.GetComponent<CesiumForUnity.CesiumGlobeAnchor>();

        ReadGPS();
        if (!string.IsNullOrWhiteSpace(apiKey))
            yield return FetchTideData();

        StartCoroutine(FetchLoop());
        StartCoroutine(UpdateLoop());
    }

    // ── Loops ─────────────────────────────────────────────────────

    private IEnumerator FetchLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(fetchIntervalHours * 3600f);
            ReadGPS();
            if (!string.IsNullOrWhiteSpace(apiKey))
                yield return FetchTideData();
        }
    }

    private IEnumerator UpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            EvaluateCurrent();
        }
    }

    private void ReadGPS()
    {
        if (_playerAnchor != null)
        {
            _lon = _playerAnchor.longitudeLatitudeHeight.x;
            _lat = _playerAnchor.longitudeLatitudeHeight.y;
        }
        else { _lat = fallbackLat; _lon = fallbackLon; }
    }

    // ── WorldTides API fetch ──────────────────────────────────────

    private IEnumerator FetchTideData()
    {
        long start  = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        // Fetch 30 hours so we always have the next extreme
        string url  = $"https://www.worldtides.info/api/v3?heights&extremes" +
                      $"&lat={_lat:F5}&lon={_lon:F5}&key={apiKey}" +
                      $"&start={start}&length=108000&step=3600";

        using var req = UnityWebRequest.Get(url);
        req.timeout = 15;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[TideService] API fetch failed: {req.error} — using harmonic model.");
            HasLiveData = false;
            EvaluateCurrent();
            yield break;
        }

        WT_Root root;
        try   { root = JsonUtility.FromJson<WT_Root>(req.downloadHandler.text); }
        catch { Debug.LogWarning("[TideService] JSON parse error — using harmonic model."); yield break; }

        if (root?.status != 200 || root.heights == null || root.heights.Length == 0)
        {
            Debug.LogWarning($"[TideService] API status {root?.status} — using harmonic model.");
            HasLiveData = false;
            EvaluateCurrent();
            yield break;
        }

        _heights  = root.heights;
        _extremes = root.extremes;
        HasLiveData = true;

        // Compute today's range
        _todayMin = float.MaxValue;
        _todayMax = float.MinValue;
        foreach (var h in _heights)
        {
            if (h.height < _todayMin) _todayMin = h.height;
            if (h.height > _todayMax) _todayMax = h.height;
        }

        EvaluateCurrent();
        Debug.Log($"[TideService] API data: {_heights.Length} heights, {_extremes?.Length ?? 0} extremes.  Range {_todayMin:F2}–{_todayMax:F2} m");
    }

    // ── Evaluate current snapshot ─────────────────────────────────

    private void EvaluateCurrent()
    {
        float height = HasLiveData ? InterpolateHeight() : HarmonicHeight();
        float min, max;

        if (HasLiveData && _todayMin < float.MaxValue)
        {
            min = _todayMin; max = _todayMax;
        }
        else
        {
            // Harmonic range for SE Asia: approx 0.2 – 2.6 m
            min = 0.20f; max = 2.60f;
        }

        float range      = Mathf.Max(max - min, 0.01f);
        float normalised = Mathf.Clamp01((height - min) / range);
        var   state      = ClassifyState(height, normalised);

        // Next extreme
        TideExtremeType nextType    = TideExtremeType.HIGH;
        DateTime        nextTime    = DateTime.UtcNow.AddHours(6.2);  // fallback M2/2
        float           nextHeightM = max;

        if (HasLiveData && _extremes != null && _extremes.Length > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var ex in _extremes)
            {
                if (ex.dt > now)
                {
                    nextTime    = DateTimeOffset.FromUnixTimeSeconds(ex.dt).LocalDateTime;
                    nextHeightM = ex.height;
                    nextType    = ex.type.StartsWith("H", StringComparison.OrdinalIgnoreCase)
                                  ? TideExtremeType.HIGH : TideExtremeType.LOW;
                    break;
                }
            }
        }
        else
        {
            // Harmonic: next extreme is next M2 peak or trough from current trend
            nextType    = state == TideState.RISING || state == TideState.LOW
                          ? TideExtremeType.HIGH : TideExtremeType.LOW;
            nextTime    = DateTime.UtcNow.AddHours(state == TideState.RISING ? 3.1 : 3.1);
            nextHeightM = nextType == TideExtremeType.HIGH ? max : min;
        }

        var snap = new TideSnapshot
        {
            heightM           = height,
            normalisedHeight  = normalised,
            state             = state,
            nextExtremeType   = nextType,
            nextExtremeTime   = nextTime,
            nextExtremeHeightM= nextHeightM,
            rangeM            = range,
            isLiveData        = HasLiveData,
        };

        bool stateChanged = snap.state != _lastState;
        _lastState = snap.state;
        Current    = snap;

        OnTideUpdated?.Invoke(snap);
        if (stateChanged) OnTideStateChanged?.Invoke(snap);

        Debug.Log($"[TideService] {(HasLiveData ? "API" : "harmonic")} height={height:F2}m  {state}  norm={normalised:F2}");
    }

    // ── API interpolation ─────────────────────────────────────────

    private float InterpolateHeight()
    {
        if (_heights == null || _heights.Length == 0) return HarmonicHeight();

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Find surrounding data points
        for (int i = 0; i < _heights.Length - 1; i++)
        {
            if (now >= _heights[i].dt && now <= _heights[i + 1].dt)
            {
                float t = (float)(now - _heights[i].dt) / (_heights[i + 1].dt - _heights[i].dt);
                return Mathf.Lerp(_heights[i].height, _heights[i + 1].height, t);
            }
        }

        // Outside cached window — return last known value
        return _heights[_heights.Length - 1].height;
    }

    // ── Harmonic tide model ───────────────────────────────────────
    //
    // 5-constituent model for SE Asian mixed-semidiurnal tides.
    // Constituent amplitudes typical for Singapore / Strait of Malacca:
    //   M2  (principal lunar semidiurnal)  T=12.4206 h  A=0.55 m
    //   S2  (principal solar semidiurnal)  T=12.0000 h  A=0.15 m
    //   K1  (luni-solar diurnal)           T=23.9345 h  A=0.35 m
    //   O1  (lunar diurnal)                T=25.8194 h  A=0.25 m
    //   N2  (larger lunar elliptic)        T=12.6584 h  A=0.12 m
    //
    // MSL offset of +1.35 m gives a chart-datum range of 0.0 – 2.77 m.
    // Phase angles set to produce a realistic daily cycle from J2000.0.

    private static readonly float[] _A = { 0.55f, 0.15f, 0.35f, 0.25f, 0.12f };
    private static readonly float[] _T = { 12.4206f, 12.0f, 23.9345f, 25.8194f, 12.6584f };
    private static readonly float[] _P = { 0.00f, 0.45f, 1.20f, 0.80f, 0.30f }; // radians, approximate

    private static float HarmonicHeight()
    {
        // Hours since J2000.0 epoch (2000-01-01 12:00 UTC)
        double j2000    = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc).Ticks;
        double nowTicks = DateTime.UtcNow.ToUniversalTime().Ticks;
        double hoursFromEpoch = (nowTicks - j2000) / (double)TimeSpan.TicksPerHour;

        float h = 1.35f;  // mean sea level offset above chart datum
        for (int i = 0; i < _A.Length; i++)
            h += _A[i] * Mathf.Cos((float)(2.0 * Math.PI * hoursFromEpoch / _T[i]) - _P[i]);

        return Mathf.Max(0f, h);
    }

    // ── State classification ──────────────────────────────────────

    private TideState ClassifyState(float height, float norm)
    {
        // Use the previous snapshot to determine direction
        float prev = Current.heightM;
        float diff = height - prev;

        // High / Low threshold: within 5% of range from extreme
        if (norm >= 0.92f) return TideState.HIGH;
        if (norm <= 0.08f) return TideState.LOW;
        return diff >= 0f ? TideState.RISING : TideState.FALLING;
    }

    // ── Public helpers ────────────────────────────────────────────

    /// <summary>True when tide height is below threshold — used to gate sea cave / tidal cave access.</summary>
    public bool IsBelowThreshold(float thresholdM) => Current.heightM < thresholdM;

    /// <summary>Formatted tide readout string for HUD display.</summary>
    public string HUDString()
    {
        string stateIcon = Current.state switch
        {
            TideState.RISING  => "↑",
            TideState.HIGH    => "▲",
            TideState.FALLING => "↓",
            TideState.LOW     => "▼",
            _                 => "—",
        };
        string typeLabel  = Current.nextExtremeType == TideExtremeType.HIGH ? "HW" : "LW";
        string nextTime   = Current.nextExtremeTime.ToString("HH:mm");
        string src        = HasLiveData ? "" : $" <size=75%>{harmonicLabel}</size>";

        return $"{stateIcon} {Current.heightM:F2} m{src}\n" +
               $"<size=75%>{Current.state}  {typeLabel} {nextTime} {Current.nextExtremeHeightM:F2}m</size>";
    }
}
