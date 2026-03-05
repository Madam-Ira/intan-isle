using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Emotional Day/Night Cycle — not a clock. Emotional rhythm.
///
/// 6 phases, each a distinct ecological mood:
///   0 Dawn    — slow, meditative, saffron gold, Blessing Meter refreshes
///   1 Morning — gentle brightening, emerald light, active
///   2 Midday  — full saturation, white-gold, productive
///   3 Dusk    — amber-violet sarong gradient, reflective
///   4 Night   — moonlit blue-silver, bioluminescence activates
///   5 Veiled Night — deep midnight, max bioluminescence, Bunian dimension visible
///
/// Controls: Directional Light, sky colour, fog, global post-processing.
/// Notifies: ZoneShaderLinker (for shader globals)
/// Notifies: BarakahMeter (Blessing refresh at Dawn)
/// </summary>
public class EmotionalDayNightCycle : MonoBehaviour
{
    public static EmotionalDayNightCycle Instance { get; private set; }

    // ── Phase timing (seconds per phase) ─────────────────────────
    [Header("Phase Durations (seconds)")]
    [SerializeField] private float[] phaseDurations = { 180f, 240f, 300f, 200f, 300f, 240f };

    [Header("Scene Links")]
    [SerializeField] private Light         directionalLight;
    [SerializeField] private Volume        postProcessVolume;
    [SerializeField] private ZoneShaderLinker shaderLinker;
    [SerializeField] private BarakahMeter  barakahMeter;

    // ── Per-phase sky colours (locked palette) ────────────────────

    private static readonly Color[] SkyColors = {
        new Color(0.957f, 0.784f, 0.259f, 1f), // Dawn:   saffron gold   #F4C842
        new Color(0.290f, 0.549f, 0.247f, 1f), // Morning:emerald        #4A8C3F
        new Color(0.910f, 0.937f, 0.980f, 1f), // Midday: bright white-blue
        new Color(0.545f, 0.290f, 0.420f, 1f), // Dusk:   sarong amber-violet #8B4A6B
        new Color(0.063f, 0.133f, 0.275f, 1f), // Night:  moonlit blue-silver
        new Color(0.039f, 0.122f, 0.102f, 1f), // Veiled Night: midnight teal #0A1F1A
    };

    private static readonly Color[] SunColors = {
        new Color(1.0f,  0.82f, 0.55f, 1f),  // Dawn:   warm saffron
        new Color(1.0f,  0.95f, 0.85f, 1f),  // Morning:warm white
        new Color(1.0f,  0.98f, 0.95f, 1f),  // Midday: pure white
        new Color(1.0f,  0.65f, 0.35f, 1f),  // Dusk:   amber
        new Color(0.55f, 0.65f, 0.90f, 1f),  // Night:  cool blue-silver
        new Color(0.25f, 0.55f, 0.45f, 1f),  // Veiled: deep teal
    };

    private static readonly float[] SunIntensities = {
        0.6f, 0.9f, 1.1f, 0.7f, 0.15f, 0.05f
    };

    private static readonly Color[] FogColors = {
        new Color(0.98f, 0.91f, 0.72f, 1f),  // Dawn fog: golden haze
        new Color(0.72f, 0.86f, 0.78f, 1f),  // Morning: light green mist
        new Color(0.90f, 0.94f, 1.00f, 1f),  // Midday: clear
        new Color(0.70f, 0.50f, 0.55f, 1f),  // Dusk: amber-violet
        new Color(0.15f, 0.20f, 0.38f, 1f),  // Night: dark blue
        new Color(0.04f, 0.15f, 0.12f, 1f),  // Veiled: midnight teal
    };

    private static readonly float[] FogDensities = {
        0.0008f, 0.0003f, 0.0002f, 0.0006f, 0.0010f, 0.0020f
    };

    // ── Runtime ───────────────────────────────────────────────────
    public int   CurrentPhase    { get; private set; }
    public float PhaseProgress   { get; private set; } // 0–1
    public bool  BioLumActive    { get; private set; }

    private float _phaseTimer    = 0f;
    private ColorAdjustments _colorAdj;
    private Vignette         _vignette;
    private Bloom            _bloom;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentPhase = 0;
        _phaseTimer  = 0f;

        // Get post-process components
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out _colorAdj);
            postProcessVolume.profile.TryGet(out _vignette);
            postProcessVolume.profile.TryGet(out _bloom);
        }

        if (directionalLight == null)
        {
            var lightGO = GameObject.Find("Directional Light");
            if (lightGO != null) directionalLight = lightGO.GetComponent<Light>();
        }

        if (shaderLinker == null)
            shaderLinker = FindObjectOfType<ZoneShaderLinker>();
        if (barakahMeter == null)
            barakahMeter = FindObjectOfType<BarakahMeter>();
    }

    void Update()
    {
        AdvancePhase();
        ApplyPhaseVisuals();
        PushShaderGlobals();
    }

    // ── Phase advancement ─────────────────────────────────────────

    private void AdvancePhase()
    {
        int   phases = phaseDurations.Length;
        float dur    = phaseDurations[CurrentPhase];

        _phaseTimer  += Time.deltaTime;
        PhaseProgress = Mathf.Clamp01(_phaseTimer / dur);

        if (_phaseTimer >= dur)
        {
            _phaseTimer = 0f;
            int next    = (CurrentPhase + 1) % phases;

            // Dawn event: Blessing refresh
            if (next == 0 && barakahMeter != null)
            {
                barakahMeter.AddBarakah(10f, BarakahSource.Consistency);
                Debug.Log("[DayNight] Dawn — Blessing Meter refreshed.");
            }

            CurrentPhase = next;
            Debug.Log("[DayNight] Phase → " + PhaseName(CurrentPhase));
        }
    }

    // ── Visual interpolation across phases ───────────────────────

    private void ApplyPhaseVisuals()
    {
        int   curr = CurrentPhase;
        int   next = (curr + 1) % SkyColors.Length;
        float t    = PhaseProgress;

        // Directional light
        if (directionalLight != null)
        {
            directionalLight.color     = Color.Lerp(SunColors[curr], SunColors[next], t);
            directionalLight.intensity = Mathf.Lerp(SunIntensities[curr], SunIntensities[next], t);

            // Sun angle: full day arc (negative elevation at night)
            float angle = Mathf.Lerp(curr * 60f, (curr + 1) * 60f, t); // 0–360 over 6 phases
            directionalLight.transform.rotation = Quaternion.Euler(angle - 90f, 30f, 0f);
        }

        // Fog
        RenderSettings.fogColor   = Color.Lerp(FogColors[curr],   FogColors[next],   t);
        RenderSettings.fogDensity = Mathf.Lerp(FogDensities[curr],FogDensities[next], t);
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.Exponential;

        // Sky ambient
        RenderSettings.ambientSkyColor = Color.Lerp(SkyColors[curr], SkyColors[next], t);

        // Bioluminescence: active in Night + Veiled Night
        BioLumActive = curr >= 4;
        Shader.SetGlobalFloat("_IntanIsle_BioLum", BioLumActive ? 1f : 0f);
        Shader.SetGlobalFloat("_IntanIsle_BioLumIntensity",
            BioLumActive ? Mathf.Lerp(0f, 1f, (curr == 4 ? PhaseProgress : 1f)) : 0f);

        // Post-process bloom intensifies at night
        if (_bloom != null)
        {
            _bloom.threshold.overrideState = true;
            _bloom.threshold.value         = 0.8f;
            _bloom.intensity.overrideState = true;
            _bloom.intensity.value         = BioLumActive ? Mathf.Lerp(1.2f, 2.0f, PhaseProgress) : 0.5f;
        }
    }

    // ── Push to ZoneShaderLinker ──────────────────────────────────

    private void PushShaderGlobals()
    {
        float globalPhase = CurrentPhase + PhaseProgress;
        Shader.SetGlobalFloat("_IntanIsle_DayPhase", globalPhase);
        Shader.SetGlobalFloat("_IntanIsle_TimeNorm",  PhaseProgress);
        if (shaderLinker != null) shaderLinker.SetDayPhase(CurrentPhase, PhaseProgress);
    }

    // ── Helper ────────────────────────────────────────────────────

    private string PhaseName(int p) => p switch
    {
        0 => "Dawn",
        1 => "Morning",
        2 => "Midday",
        3 => "Dusk",
        4 => "Night",
        5 => "Veiled Night",
        _ => "Unknown",
    };

    public string CurrentPhaseName => PhaseName(CurrentPhase);
    public bool   IsRunning        => enabled;
}
