using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Controls the cave interior atmosphere:
///   - Ambient light colour fades to cave-specific dark tone
///   - Fog density increases
///   - Accent point light spawned at entrance
///   - Drip particle system (procedural)
///   - Bioluminescence particles for KARST / SEA_CAVE
///   - Incense-glow particles for SACRED_TEMPLE
///   - Crystal sparkle particles for CRYSTAL / ICE_CAVE
///   - Optional URP post-processing volume weight
///
/// Called by CaveManager when entering or exiting a cave.
/// All changes are fully reversed on exit via coroutines.
/// </summary>
public class CaveEnvironmentController : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 1.5f;

    [Header("URP Post-Processing (optional)")]
    [Tooltip("A cave-specific Volume override — weight is lerped to 1 on enter, 0 on exit")]
    [SerializeField] private Volume caveVolume;

    // ── Saved exterior state ──────────────────────────────────────
    private Color  _savedAmbientSky;
    private Color  _savedAmbientEquator;
    private Color  _savedAmbientGround;
    private float  _savedFogDensity;
    private Color  _savedFogColor;
    private bool   _savedFog;
    private float  _savedFarClip;

    // ── Runtime ───────────────────────────────────────────────────
    private Light       _accentLight;
    private GameObject  _dripParticles;
    private GameObject  _ambientParticles;
    private bool        _insideCave;
    private Coroutine   _transitionCo;
    private CaveEntry   _currentCave;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        SaveExteriorState();
    }

    // ── Public API ────────────────────────────────────────────────

    public void EnterCave(CaveEntry cave)
    {
        if (_insideCave && _currentCave?.name == cave.name) return;
        _currentCave = cave;

        if (_transitionCo != null) StopCoroutine(_transitionCo);
        _transitionCo = StartCoroutine(TransitionIn(cave));
    }

    public void ExitCave()
    {
        if (!_insideCave) return;
        if (_transitionCo != null) StopCoroutine(_transitionCo);
        _transitionCo = StartCoroutine(TransitionOut());
    }

    public bool InsideCave => _insideCave;
    public CaveEntry CurrentCave => _currentCave;

    // ── Transition in ─────────────────────────────────────────────

    private IEnumerator TransitionIn(CaveEntry cave)
    {
        _insideCave = true;

        Color targetAmbient = cave.caveType.AmbientColor();
        Color targetFog     = cave.caveType.AmbientColor() * 1.5f;
        float targetFog_D   = cave.caveType.FogDensity();

        // Enable fog
        RenderSettings.fog = true;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * transitionSpeed);

            RenderSettings.ambientSkyColor     = Color.Lerp(_savedAmbientSky,     targetAmbient, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(_savedAmbientEquator, targetAmbient * 0.7f, t);
            RenderSettings.ambientGroundColor  = Color.Lerp(_savedAmbientGround,  targetAmbient * 0.4f, t);
            RenderSettings.fogColor            = Color.Lerp(_savedFogColor,       targetFog, t);
            RenderSettings.fogDensity          = Mathf.Lerp(_savedFogDensity,     targetFog_D, t);

            if (caveVolume != null)
                caveVolume.weight = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        SpawnCaveAccentLight(cave);
        SpawnCaveParticles(cave);
    }

    // ── Transition out ────────────────────────────────────────────

    private IEnumerator TransitionOut()
    {
        DestroyParticles();

        float t = 0f;
        Color startAmbient = RenderSettings.ambientSkyColor;
        Color startFog     = RenderSettings.fogColor;
        float startFog_D   = RenderSettings.fogDensity;

        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * transitionSpeed);

            RenderSettings.ambientSkyColor     = Color.Lerp(startAmbient,  _savedAmbientSky,     t);
            RenderSettings.ambientEquatorColor = Color.Lerp(startAmbient,  _savedAmbientEquator, t);
            RenderSettings.ambientGroundColor  = Color.Lerp(startAmbient,  _savedAmbientGround,  t);
            RenderSettings.fogColor            = Color.Lerp(startFog,      _savedFogColor,       t);
            RenderSettings.fogDensity          = Mathf.Lerp(startFog_D,    _savedFogDensity,     t);
            RenderSettings.fog                 = _savedFog || t < 1f;

            if (caveVolume != null)
                caveVolume.weight = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        RenderSettings.fog = _savedFog;
        if (_accentLight != null) { Destroy(_accentLight.gameObject); _accentLight = null; }
        _insideCave  = false;
        _currentCave = null;
    }

    // ── Accent point light ────────────────────────────────────────

    private void SpawnCaveAccentLight(CaveEntry cave)
    {
        if (_accentLight != null) Destroy(_accentLight.gameObject);

        var go = new GameObject("CaveAccentLight");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 2f;

        _accentLight          = go.AddComponent<Light>();
        _accentLight.type     = LightType.Point;
        _accentLight.color    = cave.caveType.AccentLight();
        _accentLight.intensity= 1.5f;
        _accentLight.range    = Mathf.Clamp(cave.radiusM * 0.8f, 10f, 80f);
    }

    // ── Cave particle systems ─────────────────────────────────────

    private void SpawnCaveParticles(CaveEntry cave)
    {
        DestroyParticles();

        // Dripping water — all wet cave types
        bool hasDrips = cave.caveType == CaveType.KARST
                     || cave.caveType == CaveType.SEA_CAVE
                     || cave.caveType == CaveType.TIDAL
                     || cave.caveType == CaveType.CRYSTAL
                     || cave.caveType == CaveType.ICE_CAVE;

        if (hasDrips)
            _dripParticles = MakeDripSystem(cave.caveType.AccentLight() * 0.8f);

        // Type-specific ambient particles
        _ambientParticles = cave.caveType switch
        {
            CaveType.KARST         => MakeGlowSystem(new Color(0.0f, 1.0f, 0.6f, 0.5f), 0.05f),  // bioluminescent cyan
            CaveType.SEA_CAVE      => MakeGlowSystem(new Color(0.1f, 0.8f, 1.0f, 0.4f), 0.04f),  // sea-blue sparkle
            CaveType.TIDAL         => MakeGlowSystem(new Color(0.0f, 0.9f, 0.8f, 0.4f), 0.04f),
            CaveType.ICE_CAVE      => MakeGlowSystem(new Color(0.7f, 0.9f, 1.0f, 0.5f), 0.03f),  // ice crystal
            CaveType.CRYSTAL       => MakeGlowSystem(new Color(0.8f, 0.4f, 1.0f, 0.6f), 0.05f),  // mineral sparkle
            CaveType.SACRED_TEMPLE => MakeGlowSystem(new Color(1.0f, 0.8f, 0.3f, 0.3f), 0.02f),  // incense-gold motes
            CaveType.BURIAL        => MakeGlowSystem(new Color(0.5f, 0.1f, 0.7f, 0.2f), 0.015f), // ancestral purple
            CaveType.VOLCANIC_VENT => MakeGlowSystem(new Color(1.0f, 0.3f, 0.0f, 0.4f), 0.06f),  // ember sparks
            _                      => null,
        };
    }

    private GameObject MakeDripSystem(Color color)
    {
        var go  = new GameObject("CaveDrips");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 5f;

        var ps  = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime    = 1.5f;
        main.startSpeed       = 3f;
        main.startSize        = 0.04f;
        main.startColor       = color;
        main.gravityModifier  = 1.2f;
        main.maxParticles     = 80;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(8f, 0.1f, 8f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    private GameObject MakeGlowSystem(Color color, float size)
    {
        var go  = new GameObject("CaveGlow");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 1.5f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime  = 4f;
        main.startSpeed     = 0.3f;
        main.startSize      = size;
        main.startColor     = color;
        main.gravityModifier= 0f;
        main.simulationSpace= ParticleSystemSimulationSpace.World;
        main.maxParticles   = 150;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = Mathf.Clamp(5f, 2f, 12f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.2f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        fade.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    private void DestroyParticles()
    {
        if (_dripParticles    != null) { Destroy(_dripParticles);    _dripParticles    = null; }
        if (_ambientParticles != null) { Destroy(_ambientParticles); _ambientParticles = null; }
    }

    // ── Save / restore ────────────────────────────────────────────

    private void SaveExteriorState()
    {
        _savedAmbientSky     = RenderSettings.ambientSkyColor;
        _savedAmbientEquator = RenderSettings.ambientEquatorColor;
        _savedAmbientGround  = RenderSettings.ambientGroundColor;
        _savedFogDensity     = RenderSettings.fogDensity;
        _savedFogColor       = RenderSettings.fogColor;
        _savedFog            = RenderSettings.fog;
    }

    void OnDisable() { if (_insideCave) ExitCave(); }
}
