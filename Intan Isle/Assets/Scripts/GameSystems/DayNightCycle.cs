using System;
using UnityEngine;

public enum GamePhase
{
    Dawn,
    Morning,
    Midday,
    Afternoon,
    Dusk,
    Night
}

/// <summary>
/// Day/night cycle with two modes:
///
///   syncToRealTime = true  (default)
///     Phase is driven by the actual local clock via TimeZoneService.
///     Phase transitions fire OnPhaseChanged when the real hour crosses
///     a phase boundary. The local time is checked every second.
///     Phase hour boundaries are configurable below (tropical defaults).
///
///   syncToRealTime = false
///     Original timer-based behaviour — each phase lasts phaseDuration
///     seconds and advances automatically.
///
/// TriggerPhaseEffects fires on every phase entry regardless of mode:
///   Dawn  → AWFWaterSystem.CollectWater()
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    [Header("Real-Time Sync")]
    [Tooltip("Drive phase from the actual local clock (via TimeZoneService). Recommended.")]
    [SerializeField] private bool syncToRealTime = true;

    [Header("Phase Hour Boundaries (real-time mode, tropical defaults)")]
    [Tooltip("Hour when Dawn begins (e.g. 5.5 = 05:30)")]
    [SerializeField] private float dawnHour      = 5.5f;
    [Tooltip("Hour when Morning begins")]
    [SerializeField] private float morningHour   = 7.0f;
    [Tooltip("Hour when Midday begins")]
    [SerializeField] private float middayHour    = 11.0f;
    [Tooltip("Hour when Afternoon begins")]
    [SerializeField] private float afternoonHour = 14.0f;
    [Tooltip("Hour when Dusk begins")]
    [SerializeField] private float duskHour      = 17.5f;
    [Tooltip("Hour when Night begins (Dawn resumes below dawnHour)")]
    [SerializeField] private float nightHour     = 19.5f;

    [Header("Timer Mode (syncToRealTime = false)")]
    [Tooltip("Duration of each phase in seconds when not using real-time sync")]
    public float phaseDuration = 300f;

    // ── State ──────────────────────────────────────────────────────
    private GamePhase _currentPhase = GamePhase.Dawn;
    private float     _phaseTimer   = 0f;
    private float     _realCheckTimer = 0f;

    public event Action<GamePhase> OnPhaseChanged;
    public GamePhase CurrentPhase => _currentPhase;

    /// <summary>Fractional progress through the current phase (0-1).</summary>
    public float PhaseProgress
    {
        get
        {
            if (syncToRealTime && TimeZoneService.Instance != null)
                return RealTimePhaseProgress(TimeZoneService.Instance.LocalTime);
            return Mathf.Clamp01(_phaseTimer / phaseDuration);
        }
    }

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        if (syncToRealTime)
            SyncToRealTime(forceEvent: true);
        else
            TriggerPhaseEffects(_currentPhase);
    }

    void Update()
    {
        if (syncToRealTime)
        {
            _realCheckTimer += Time.deltaTime;
            if (_realCheckTimer >= 1f)   // check once per second
            {
                _realCheckTimer = 0f;
                SyncToRealTime(forceEvent: false);
            }
        }
        else
        {
            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= phaseDuration)
            {
                _phaseTimer -= phaseDuration;
                AdvancePhase();
            }
        }
    }

    // ── Real-time sync ─────────────────────────────────────────────

    private void SyncToRealTime(bool forceEvent)
    {
        DateTime local;

        if (TimeZoneService.Instance != null)
            local = TimeZoneService.Instance.LocalTime;
        else
            local = DateTime.UtcNow.AddHours(8f);  // UTC+8 fallback

        var newPhase = TimeZoneService.PhaseFromLocalTime(local,
            dawnHour, morningHour, middayHour, afternoonHour, duskHour, nightHour);

        if (newPhase != _currentPhase || forceEvent)
        {
            _currentPhase = newPhase;
            OnPhaseChanged?.Invoke(_currentPhase);
            TriggerPhaseEffects(_currentPhase);

            Debug.Log($"[DayNightCycle] Phase → {_currentPhase}  (local {local:HH:mm}  {TimeZoneService.Instance?.TimeZoneAbbr ?? "UTC+8"})");
        }
    }

    // ── Timer-based advance ───────────────────────────────────────

    private void AdvancePhase()
    {
        _currentPhase = (GamePhase)(((int)_currentPhase + 1) % 6);
        OnPhaseChanged?.Invoke(_currentPhase);
        TriggerPhaseEffects(_currentPhase);
    }

    // ── Phase effects ─────────────────────────────────────────────

    private void TriggerPhaseEffects(GamePhase phase)
    {
        if (phase == GamePhase.Dawn && AWFWaterSystem.Instance != null)
            AWFWaterSystem.Instance.CollectWater();
    }

    // ── Phase progress for smooth lighting ───────────────────────

    private float RealTimePhaseProgress(DateTime local)
    {
        float h = local.Hour + local.Minute / 60f + local.Second / 3600f;

        float start, end;
        switch (_currentPhase)
        {
            case GamePhase.Dawn:      start = dawnHour;      end = morningHour;   break;
            case GamePhase.Morning:   start = morningHour;   end = middayHour;    break;
            case GamePhase.Midday:    start = middayHour;    end = afternoonHour; break;
            case GamePhase.Afternoon: start = afternoonHour; end = duskHour;      break;
            case GamePhase.Dusk:      start = duskHour;      end = nightHour;     break;
            case GamePhase.Night:
                // Night wraps midnight: treat as 19.5-29.5 range
                if (h >= nightHour)  return Mathf.Clamp01((h - nightHour) / (24f - nightHour + dawnHour));
                return Mathf.Clamp01((h + (24f - nightHour)) / (24f - nightHour + dawnHour));
            default: return 0f;
        }
        return Mathf.Clamp01((h - start) / (end - start));
    }

    // ── Public helpers ────────────────────────────────────────────

    /// <summary>Force an immediate phase change (e.g. for cutscenes or testing).</summary>
    public void ForcePhase(GamePhase phase)
    {
        _currentPhase = phase;
        _phaseTimer   = 0f;
        OnPhaseChanged?.Invoke(_currentPhase);
        TriggerPhaseEffects(_currentPhase);
    }
}
