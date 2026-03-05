using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Applies full visual + Barakah treatment for all 37 WeatherCondition values.
///
/// Visual systems:
///   Rain family       — parameterised rain streaks (rate, speed, size, stretch)
///   Snow family       — drifting flakes (light / heavy / blizzard horizontal drift)
///   Sleet / mixed     — combined rain streaks + ice chips
///   Dust / Sand       — horizontal brownish particles driven by wind
///   Fog / Mist family — large slow drifting volumetric blobs
///   Haze / Smoke      — tinted volumetric + RenderSettings tint
///   Volcanic ash      — heavy grey drifting + extreme fog
///   Squall            — extreme rain + strong wind velocity
///   Tornado           — spiral particle vortex
///   Thunder hail      — rain streaks + ice chip particles
///
/// Ambient light colour, fog density, fog colour, URP Volume weights, and
/// Barakah rates are defined for every condition.
///
/// Cave-aware: all FX suppressed while CaveManager.InsideCave is true.
/// </summary>
public class WeatherEnvironmentController : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 0.6f;

    [Header("URP Post-Processing (optional)")]
    [SerializeField] private Volume rainVolume;
    [SerializeField] private Volume hazeVolume;
    [SerializeField] private Volume snowVolume;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter barakahMeter;

    // ── Saved exterior state ──────────────────────────────────────
    private Color _savedAmbientSky;
    private float _savedFogDensity;
    private Color _savedFogColor;
    private bool  _savedFog;

    // ── Particle objects ──────────────────────────────────────────
    private GameObject _precip;     // rain / snow / sleet / hail
    private GameObject _precip2;    // secondary (e.g. ice chips on sleet)
    private GameObject _atmos;      // fog / mist / haze / smoke / dust / ash
    private GameObject _vortex;     // tornado spiral

    // ── Lights ───────────────────────────────────────────────────
    private Light     _thunderLight;
    private Coroutine _thunderCo;

    // ── Transition ────────────────────────────────────────────────
    private Coroutine         _transitionCo;
    private WeatherCondition  _active = WeatherCondition.UNKNOWN;
    private float             _barakahTimer;

    // ── Shader globals ────────────────────────────────────────────
    private static readonly int SH_WeatherType   = Shader.PropertyToID("_IntanIsle_WeatherType");
    private static readonly int SH_RainIntensity = Shader.PropertyToID("_IntanIsle_RainIntensity");
    private static readonly int SH_SnowIntensity = Shader.PropertyToID("_IntanIsle_SnowIntensity");
    private static readonly int SH_HazeIntensity = Shader.PropertyToID("_IntanIsle_HazeIntensity");

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (barakahMeter == null) barakahMeter = FindObjectOfType<BarakahMeter>();
        SaveState();
        Shader.SetGlobalFloat(SH_WeatherType,   0f);
        Shader.SetGlobalFloat(SH_RainIntensity, 0f);
        Shader.SetGlobalFloat(SH_SnowIntensity, 0f);
        Shader.SetGlobalFloat(SH_HazeIntensity, 0f);
    }

    void OnEnable()
    {
        WeatherService.OnWeatherChanged   += OnWeatherChanged;
        WeatherService.OnWeatherRefreshed += OnWeatherRefreshed;
    }

    void OnDisable()
    {
        WeatherService.OnWeatherChanged   -= OnWeatherChanged;
        WeatherService.OnWeatherRefreshed -= OnWeatherRefreshed;
        DestroyAll();
    }

    void Update()
    {
        if (barakahMeter == null) return;
        _barakahTimer += Time.deltaTime;
        if (_barakahTimer < 0.5f) return;
        _barakahTimer = 0f;
        ApplyBarakah();
    }

    // ── Event handlers ────────────────────────────────────────────

    private void OnWeatherChanged(WeatherSnapshot snap)
    {
        if (CaveManager.Instance != null && CaveManager.Instance.InsideCave) return;
        _active = snap.condition;
        if (_transitionCo != null) StopCoroutine(_transitionCo);
        _transitionCo = StartCoroutine(Transition(snap));
    }

    private void OnWeatherRefreshed(WeatherSnapshot snap)
    {
        ApplyWindToParticles(snap.windSpeedMs, snap.windDeg);
    }

    // ── Transition coroutine ──────────────────────────────────────

    private IEnumerator Transition(WeatherSnapshot snap)
    {
        DestroyAll();
        StopThunder();

        Color targetAmbient = GetAmbientColor(snap.condition);
        float targetFogD    = GetFogDensity(snap.condition);
        Color targetFogCol  = GetFogColor(snap.condition);

        bool needFog = targetFogD > 0.0005f;
        if (needFog) RenderSettings.fog = true;

        float startFogD   = RenderSettings.fogDensity;
        Color startAmbient= RenderSettings.ambientSkyColor;
        Color startFogCol = RenderSettings.fogColor;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * transitionSpeed);

            RenderSettings.ambientSkyColor     = Color.Lerp(startAmbient,  targetAmbient, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(startAmbient,  targetAmbient * 0.75f, t);
            RenderSettings.ambientGroundColor  = Color.Lerp(startAmbient,  targetAmbient * 0.45f, t);
            RenderSettings.fogDensity          = Mathf.Lerp(startFogD,     targetFogD,   t);
            RenderSettings.fogColor            = Color.Lerp(startFogCol,   targetFogCol, t);
            RenderSettings.fog                 = needFog || t < 1f || _savedFog;

            SetVolumeWeight(rainVolume, Mathf.Lerp(rainVolume != null ? rainVolume.weight : 0f, IsRainCondition(snap.condition) ? 1f : 0f, t));
            SetVolumeWeight(hazeVolume, Mathf.Lerp(hazeVolume != null ? hazeVolume.weight : 0f, IsHazeCondition(snap.condition) ? 1f : 0f, t));
            SetVolumeWeight(snowVolume, Mathf.Lerp(snowVolume != null ? snowVolume.weight : 0f, IsSnowCondition(snap.condition) ? 1f : 0f, t));

            yield return null;
        }

        if (!needFog && !_savedFog) RenderSettings.fog = false;

        // Spawn FX
        SpawnParticles(snap);
        StartThunderIfNeeded(snap.condition);

        // Shader globals
        Shader.SetGlobalFloat(SH_WeatherType,   (float)snap.condition);
        Shader.SetGlobalFloat(SH_RainIntensity, GetRainIntensity(snap.condition));
        Shader.SetGlobalFloat(SH_SnowIntensity, GetSnowIntensity(snap.condition));
        Shader.SetGlobalFloat(SH_HazeIntensity, GetHazeIntensity(snap.condition));

        ApplyWindToParticles(snap.windSpeedMs, snap.windDeg);
    }

    // ── Particle spawning — full 37-condition coverage ────────────

    private void SpawnParticles(WeatherSnapshot snap)
    {
        var c = snap.condition;

        switch (c)
        {
            // ── Clear / Cloud — no precipitation, ambient only ──────
            case WeatherCondition.CLEAR:
            case WeatherCondition.MOSTLY_CLEAR:
            case WeatherCondition.PARTLY_CLOUDY:
            case WeatherCondition.BROKEN_CLOUDS:
            case WeatherCondition.OVERCAST:
                break;  // purely ambient light change, no particles

            // ── Drizzle ─────────────────────────────────────────────
            case WeatherCondition.LIGHT_DRIZZLE:
                _precip = MakeRain(rate: 60,   size: 0.030f, speed: 2.0f, alpha: 0.30f, tint: new Color(0.80f, 0.85f, 1.0f));
                break;
            case WeatherCondition.DRIZZLE:
                _precip = MakeRain(rate: 120,  size: 0.035f, speed: 2.5f, alpha: 0.38f, tint: new Color(0.75f, 0.82f, 1.0f));
                break;
            case WeatherCondition.HEAVY_DRIZZLE:
                _precip = MakeRain(rate: 220,  size: 0.040f, speed: 3.0f, alpha: 0.45f, tint: new Color(0.70f, 0.80f, 1.0f));
                break;

            // ── Rain ────────────────────────────────────────────────
            case WeatherCondition.LIGHT_RAIN:
                _precip = MakeRain(rate: 280,  size: 0.045f, speed: 5.5f, alpha: 0.50f, tint: new Color(0.65f, 0.78f, 1.0f));
                break;
            case WeatherCondition.RAIN:
                _precip = MakeRain(rate: 420,  size: 0.050f, speed: 7.0f, alpha: 0.55f, tint: new Color(0.60f, 0.75f, 1.0f));
                break;
            case WeatherCondition.HEAVY_RAIN:
                _precip = MakeRain(rate: 750,  size: 0.055f, speed: 9.5f, alpha: 0.62f, tint: new Color(0.55f, 0.70f, 1.0f));
                break;
            case WeatherCondition.EXTREME_RAIN:
                _precip = MakeRain(rate: 1400, size: 0.065f, speed: 13f,  alpha: 0.72f, tint: new Color(0.45f, 0.62f, 0.95f));
                break;
            case WeatherCondition.FREEZING_RAIN:
                _precip  = MakeRain(rate: 350,  size: 0.048f, speed: 7.0f, alpha: 0.55f, tint: new Color(0.75f, 0.88f, 1.0f));
                _precip2 = MakeIceChips(rate: 80, size: 0.025f);
                break;
            case WeatherCondition.LIGHT_SHOWER:
                _precip = MakeRain(rate: 300,  size: 0.048f, speed: 6.0f, alpha: 0.50f, tint: new Color(0.62f, 0.76f, 1.0f));
                break;
            case WeatherCondition.SHOWER:
                _precip = MakeRain(rate: 600,  size: 0.055f, speed: 9.0f, alpha: 0.60f, tint: new Color(0.55f, 0.72f, 1.0f));
                break;
            case WeatherCondition.RAGGED_SHOWER:
                _precip = MakeRain(rate: 800,  size: 0.060f, speed: 11f,  alpha: 0.65f, tint: new Color(0.50f, 0.68f, 0.95f));
                break;

            // ── Thunderstorm ────────────────────────────────────────
            case WeatherCondition.THUNDER_DRIZZLE:
                _precip = MakeRain(rate: 100,  size: 0.038f, speed: 4.0f, alpha: 0.45f, tint: new Color(0.60f, 0.72f, 0.90f));
                break;
            case WeatherCondition.THUNDER_LIGHT:
                _precip = MakeRain(rate: 400,  size: 0.052f, speed: 8.0f, alpha: 0.60f, tint: new Color(0.50f, 0.65f, 0.90f));
                break;
            case WeatherCondition.THUNDERSTORM:
                _precip = MakeRain(rate: 850,  size: 0.060f, speed: 12f,  alpha: 0.68f, tint: new Color(0.42f, 0.60f, 0.88f));
                break;
            case WeatherCondition.HEAVY_THUNDER:
                _precip = MakeRain(rate: 1200, size: 0.068f, speed: 15f,  alpha: 0.75f, tint: new Color(0.38f, 0.55f, 0.85f));
                break;
            case WeatherCondition.THUNDER_HAIL:
                _precip  = MakeRain(rate: 900,  size: 0.062f, speed: 13f,  alpha: 0.70f, tint: new Color(0.40f, 0.58f, 0.88f));
                _precip2 = MakeIceChips(rate: 200, size: 0.04f);
                break;

            // ── Snow ────────────────────────────────────────────────
            case WeatherCondition.LIGHT_SNOW:
                _precip = MakeSnow(rate: 80,  size: 0.030f, speed: 0.6f, drift: 0.15f);
                break;
            case WeatherCondition.SNOW:
                _precip = MakeSnow(rate: 200, size: 0.042f, speed: 0.9f, drift: 0.25f);
                break;
            case WeatherCondition.HEAVY_SNOW:
                _precip = MakeSnow(rate: 500, size: 0.055f, speed: 1.2f, drift: 0.35f);
                break;
            case WeatherCondition.BLIZZARD:
                _precip = MakeSnow(rate: 900, size: 0.045f, speed: 2.5f, drift: 1.20f);  // high horizontal drift
                break;
            case WeatherCondition.SLEET:
                _precip  = MakeRain(rate: 300,  size: 0.042f, speed: 6.0f, alpha: 0.50f, tint: new Color(0.80f, 0.88f, 1.0f));
                _precip2 = MakeSnow(rate: 120,  size: 0.025f, speed: 1.8f, drift: 0.30f);
                break;
            case WeatherCondition.RAIN_SNOW:
                _precip  = MakeRain(rate: 200,  size: 0.040f, speed: 5.0f, alpha: 0.45f, tint: new Color(0.78f, 0.86f, 1.0f));
                _precip2 = MakeSnow(rate: 150,  size: 0.032f, speed: 1.0f, drift: 0.20f);
                break;

            // ── Atmosphere ──────────────────────────────────────────
            case WeatherCondition.MIST:
                _atmos = MakeFog(color: new Color(0.88f, 0.90f, 0.92f, 0.10f), size: 10f, count: 200, speed: 0.2f);
                break;
            case WeatherCondition.FOG:
                _atmos = MakeFog(color: new Color(0.80f, 0.82f, 0.85f, 0.15f), size: 14f, count: 350, speed: 0.15f);
                break;
            case WeatherCondition.DENSE_FOG:
                _atmos = MakeFog(color: new Color(0.72f, 0.74f, 0.78f, 0.22f), size: 18f, count: 500, speed: 0.10f);
                break;
            case WeatherCondition.HAZE:
                _atmos = MakeFog(color: new Color(0.88f, 0.76f, 0.46f, 0.18f), size: 16f, count: 400, speed: 0.20f);
                break;
            case WeatherCondition.SMOKE:
                _atmos = MakeFog(color: new Color(0.52f, 0.48f, 0.40f, 0.22f), size: 14f, count: 400, speed: 0.18f);
                break;
            case WeatherCondition.SAND_WHIRLS:
                _atmos  = MakeDust(color: new Color(0.88f, 0.74f, 0.42f, 0.30f), rate: 120, horizontal: 0.8f);
                _vortex = MakeTornado(scale: 0.4f);   // small whirl
                break;
            case WeatherCondition.SAND:
                _atmos = MakeDust(color: new Color(0.90f, 0.78f, 0.50f, 0.28f), rate: 300, horizontal: 1.4f);
                break;
            case WeatherCondition.DUST:
                _atmos = MakeDust(color: new Color(0.82f, 0.70f, 0.48f, 0.25f), rate: 200, horizontal: 1.0f);
                break;
            case WeatherCondition.VOLCANIC_ASH:
                _atmos  = MakeFog(color: new Color(0.40f, 0.38f, 0.36f, 0.28f), size: 18f, count: 600, speed: 0.12f);
                _precip = MakeDust(color: new Color(0.35f, 0.32f, 0.30f, 0.35f), rate: 160, horizontal: 0.2f);
                break;
            case WeatherCondition.SQUALL:
                _precip = MakeRain(rate: 1100, size: 0.065f, speed: 16f, alpha: 0.72f, tint: new Color(0.45f, 0.62f, 0.90f));
                break;
            case WeatherCondition.TORNADO:
                _atmos  = MakeDust(color: new Color(0.55f, 0.50f, 0.44f, 0.30f), rate: 250, horizontal: 1.8f);
                _vortex = MakeTornado(scale: 1.0f);
                break;
        }
    }

    // ── Rain system ───────────────────────────────────────────────

    private GameObject MakeRain(float rate, float size, float speed, float alpha, Color tint)
    {
        var go  = new GameObject("WeatherRain");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 22f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.4f, 2.0f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(speed * 0.85f, speed * 1.15f);
        main.startSize       = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startColor      = new Color(tint.r, tint.g, tint.b, alpha);
        main.gravityModifier = 1.5f;
        main.maxParticles    = 4000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = rate;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale     = new Vector3(70f, 0.1f, 70f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material      = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode    = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.06f;
        renderer.lengthScale   = 2.5f;

        return go;
    }

    // ── Snow system ───────────────────────────────────────────────

    private GameObject MakeSnow(float rate, float size, float speed, float drift)
    {
        var go  = new GameObject("WeatherSnow");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 18f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed * 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor      = new Color(0.92f, 0.95f, 1.0f, 0.75f);
        main.gravityModifier = 0.12f;
        main.maxParticles    = 2500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = rate;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale     = new Vector3(55f, 0.1f, 55f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-drift, drift);
        vel.z = new ParticleSystem.MinMaxCurve(-drift, drift);
        vel.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    // ── Ice chips (sleet / freezing rain / hail) ──────────────────

    private GameObject MakeIceChips(float rate, float size)
    {
        var go  = new GameObject("WeatherIce");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 20f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = 1.0f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSize       = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
        main.startColor      = new Color(0.85f, 0.92f, 1.0f, 0.85f);
        main.gravityModifier = 2.0f;
        main.maxParticles    = 600;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = rate;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale     = new Vector3(50f, 0.1f, 50f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    // ── Volumetric fog / mist / haze / smoke / ash ────────────────

    private GameObject MakeFog(Color color, float size, int count, float speed)
    {
        var go  = new GameObject("WeatherFog");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 2f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(10f, 18f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed * 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.4f);
        main.startColor      = color;
        main.gravityModifier = 0f;
        main.maxParticles    = count;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = count / 15f;  // fill to steady state in ~15 s

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale     = new Vector3(90f, 6f, 90f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 0.15f), new GradientAlphaKey(color.a, 0.85f), new GradientAlphaKey(0f, 1f) }
        );
        fade.color = g;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    // ── Dust / Sand — horizontal-biased ──────────────────────────

    private GameObject MakeDust(Color color, float rate, float horizontal)
    {
        var go  = new GameObject("WeatherDust");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 3f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startColor      = color;
        main.gravityModifier = 0f;
        main.maxParticles    = 2000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = rate;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale     = new Vector3(80f, 8f, 80f);

        // Strong horizontal drift to give wind-blown feel
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-horizontal, horizontal);
        vel.z = new ParticleSystem.MinMaxCurve(-horizontal * 0.5f, horizontal * 0.5f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]  { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[]  { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        fade.color = g;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    // ── Tornado / Sand whirl vortex ───────────────────────────────

    private GameObject MakeTornado(float scale)
    {
        var go  = new GameObject("WeatherVortex");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(2f * scale, 5f * scale);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f * scale, 0.2f * scale);
        main.startColor      = new Color(0.60f, 0.55f, 0.45f, 0.40f);
        main.gravityModifier = -0.3f;    // spiral upward
        main.maxParticles    = 1200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.rateOverTime = 180f * scale;

        // Cone shape — narrow at bottom, wide at top
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle     = 12f * scale;
        sh.radius    = 1.5f * scale;

        // Orbital velocity for spiral
        var orb = ps.velocityOverLifetime;
        orb.enabled     = true;
        orb.orbitalY    = new ParticleSystem.MinMaxCurve(3f * scale);
        orb.radial      = new ParticleSystem.MinMaxCurve(-0.5f * scale);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    // ── Thunder ───────────────────────────────────────────────────

    private void StartThunderIfNeeded(WeatherCondition c)
    {
        bool hasThunder = c is WeatherCondition.THUNDER_LIGHT or WeatherCondition.THUNDERSTORM
                            or WeatherCondition.HEAVY_THUNDER or WeatherCondition.THUNDER_HAIL
                            or WeatherCondition.THUNDER_DRIZZLE;
        if (!hasThunder) return;

        // Intensity multiplier per severity
        float mult = c switch
        {
            WeatherCondition.THUNDER_LIGHT   => 0.5f,
            WeatherCondition.THUNDER_DRIZZLE => 0.4f,
            WeatherCondition.THUNDERSTORM    => 1.0f,
            WeatherCondition.HEAVY_THUNDER   => 1.6f,
            WeatherCondition.THUNDER_HAIL    => 1.3f,
            _                                => 1.0f,
        };

        if (_thunderLight == null)
        {
            var tgo          = new GameObject("ThunderLight");
            tgo.transform.SetParent(transform);
            tgo.transform.localPosition = Vector3.up * 120f;
            _thunderLight               = tgo.AddComponent<Light>();
            _thunderLight.type          = LightType.Directional;
            _thunderLight.color         = new Color(0.78f, 0.84f, 1.0f);
            _thunderLight.intensity     = 0f;
        }
        _thunderCo = StartCoroutine(ThunderLoop(mult));
    }

    private void StopThunder()
    {
        if (_thunderCo  != null) { StopCoroutine(_thunderCo);  _thunderCo = null; }
        if (_thunderLight != null) { Destroy(_thunderLight.gameObject); _thunderLight = null; }
    }

    private IEnumerator ThunderLoop(float intensityMult)
    {
        while (true)
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(6f, 28f));

            int flashes = UnityEngine.Random.value > 0.5f ? 2 : 3;
            for (int i = 0; i < flashes; i++)
            {
                _thunderLight.intensity = UnityEngine.Random.Range(3f, 9f) * intensityMult;
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.10f));
                _thunderLight.intensity = 0f;
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.04f, 0.12f));
            }
        }
    }

    // ── Wind ──────────────────────────────────────────────────────

    private void ApplyWindToParticles(float speedMs, float deg)
    {
        float rad = (deg + 180f) * Mathf.Deg2Rad;
        float vx  = Mathf.Sin(rad) * speedMs * 0.25f;
        float vz  = Mathf.Cos(rad) * speedMs * 0.25f;

        AddWindToSystem(_precip,  vx, vz, 1.0f);
        AddWindToSystem(_precip2, vx, vz, 0.8f);
        AddWindToSystem(_atmos,   vx, vz, 0.3f);
        AddWindToSystem(_vortex,  vx, vz, 0.5f);
    }

    private static void AddWindToSystem(GameObject go, float vx, float vz, float scale)
    {
        if (go == null) return;
        var ps  = go.GetComponent<ParticleSystem>();
        if (ps  == null) return;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        // Preserve existing y; only override x/z
        float cy = vel.enabled ? 0f : 0f;
        vel.x = new ParticleSystem.MinMaxCurve(vx * scale - 0.3f, vx * scale + 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(vz * scale - 0.3f, vz * scale + 0.3f);
    }

    // ── RenderSettings targets ────────────────────────────────────

    private static Color GetAmbientColor(WeatherCondition c) => c switch
    {
        WeatherCondition.CLEAR           => new Color(0.56f, 0.68f, 0.85f),
        WeatherCondition.MOSTLY_CLEAR    => new Color(0.52f, 0.63f, 0.80f),
        WeatherCondition.PARTLY_CLOUDY   => new Color(0.47f, 0.55f, 0.70f),
        WeatherCondition.BROKEN_CLOUDS   => new Color(0.40f, 0.46f, 0.60f),
        WeatherCondition.OVERCAST        => new Color(0.34f, 0.38f, 0.46f),

        WeatherCondition.LIGHT_DRIZZLE   => new Color(0.42f, 0.48f, 0.58f),
        WeatherCondition.DRIZZLE         => new Color(0.38f, 0.44f, 0.55f),
        WeatherCondition.HEAVY_DRIZZLE   => new Color(0.34f, 0.40f, 0.52f),

        WeatherCondition.LIGHT_RAIN      => new Color(0.35f, 0.40f, 0.52f),
        WeatherCondition.RAIN            => new Color(0.30f, 0.36f, 0.48f),
        WeatherCondition.HEAVY_RAIN      => new Color(0.25f, 0.30f, 0.42f),
        WeatherCondition.EXTREME_RAIN    => new Color(0.20f, 0.25f, 0.36f),
        WeatherCondition.FREEZING_RAIN   => new Color(0.32f, 0.38f, 0.52f),
        WeatherCondition.LIGHT_SHOWER    => new Color(0.35f, 0.40f, 0.52f),
        WeatherCondition.SHOWER          => new Color(0.28f, 0.34f, 0.46f),
        WeatherCondition.RAGGED_SHOWER   => new Color(0.24f, 0.30f, 0.44f),

        WeatherCondition.THUNDER_LIGHT   => new Color(0.28f, 0.32f, 0.45f),
        WeatherCondition.THUNDERSTORM    => new Color(0.20f, 0.23f, 0.35f),
        WeatherCondition.HEAVY_THUNDER   => new Color(0.15f, 0.18f, 0.30f),
        WeatherCondition.THUNDER_DRIZZLE => new Color(0.26f, 0.30f, 0.42f),
        WeatherCondition.THUNDER_HAIL    => new Color(0.18f, 0.22f, 0.34f),

        WeatherCondition.LIGHT_SNOW      => new Color(0.72f, 0.76f, 0.84f),
        WeatherCondition.SNOW            => new Color(0.65f, 0.70f, 0.80f),
        WeatherCondition.HEAVY_SNOW      => new Color(0.58f, 0.63f, 0.74f),
        WeatherCondition.BLIZZARD        => new Color(0.50f, 0.55f, 0.68f),
        WeatherCondition.SLEET           => new Color(0.48f, 0.54f, 0.65f),
        WeatherCondition.RAIN_SNOW       => new Color(0.42f, 0.48f, 0.60f),

        WeatherCondition.MIST            => new Color(0.62f, 0.64f, 0.66f),
        WeatherCondition.FOG             => new Color(0.55f, 0.57f, 0.60f),
        WeatherCondition.DENSE_FOG       => new Color(0.44f, 0.46f, 0.50f),
        WeatherCondition.HAZE            => new Color(0.74f, 0.64f, 0.38f),
        WeatherCondition.SMOKE           => new Color(0.50f, 0.46f, 0.38f),
        WeatherCondition.SAND_WHIRLS     => new Color(0.82f, 0.70f, 0.44f),
        WeatherCondition.SAND            => new Color(0.85f, 0.73f, 0.46f),
        WeatherCondition.DUST            => new Color(0.78f, 0.67f, 0.44f),
        WeatherCondition.VOLCANIC_ASH    => new Color(0.36f, 0.34f, 0.32f),
        WeatherCondition.SQUALL          => new Color(0.22f, 0.26f, 0.38f),
        WeatherCondition.TORNADO         => new Color(0.28f, 0.28f, 0.32f),

        _                                => new Color(0.56f, 0.68f, 0.85f),
    };

    private static float GetFogDensity(WeatherCondition c) => c switch
    {
        WeatherCondition.DENSE_FOG       => 0.040f,
        WeatherCondition.FOG             => 0.025f,
        WeatherCondition.VOLCANIC_ASH    => 0.035f,
        WeatherCondition.SMOKE           => 0.025f,
        WeatherCondition.HAZE            => 0.020f,
        WeatherCondition.MIST            => 0.012f,
        WeatherCondition.SAND            => 0.018f,
        WeatherCondition.DUST            => 0.015f,
        WeatherCondition.SAND_WHIRLS     => 0.012f,
        WeatherCondition.BLIZZARD        => 0.018f,
        WeatherCondition.HEAVY_SNOW      => 0.012f,
        WeatherCondition.SNOW            => 0.006f,
        WeatherCondition.SLEET           => 0.008f,
        WeatherCondition.HEAVY_THUNDER   => 0.014f,
        WeatherCondition.THUNDERSTORM    => 0.010f,
        WeatherCondition.EXTREME_RAIN    => 0.012f,
        WeatherCondition.HEAVY_RAIN      => 0.008f,
        WeatherCondition.TORNADO         => 0.022f,
        WeatherCondition.SQUALL          => 0.016f,
        WeatherCondition.OVERCAST        => 0.001f,
        _                                => 0f,
    };

    private static Color GetFogColor(WeatherCondition c) => c switch
    {
        WeatherCondition.HAZE            => new Color(0.88f, 0.74f, 0.42f),
        WeatherCondition.SMOKE           => new Color(0.55f, 0.50f, 0.40f),
        WeatherCondition.SAND            => new Color(0.90f, 0.78f, 0.50f),
        WeatherCondition.SAND_WHIRLS     => new Color(0.88f, 0.76f, 0.48f),
        WeatherCondition.DUST            => new Color(0.84f, 0.72f, 0.48f),
        WeatherCondition.VOLCANIC_ASH    => new Color(0.38f, 0.36f, 0.34f),
        WeatherCondition.TORNADO         => new Color(0.50f, 0.46f, 0.40f),
        WeatherCondition.LIGHT_SNOW
            or WeatherCondition.SNOW
            or WeatherCondition.HEAVY_SNOW
            or WeatherCondition.BLIZZARD  => new Color(0.88f, 0.90f, 0.95f),
        WeatherCondition.SLEET
            or WeatherCondition.RAIN_SNOW => new Color(0.78f, 0.82f, 0.90f),
        _                                 => new Color(0.68f, 0.72f, 0.80f),
    };

    // ── Intensity scalars for shader globals ──────────────────────

    private static float GetRainIntensity(WeatherCondition c) => c switch
    {
        WeatherCondition.LIGHT_DRIZZLE   => 0.10f,
        WeatherCondition.DRIZZLE         => 0.18f,
        WeatherCondition.HEAVY_DRIZZLE   => 0.26f,
        WeatherCondition.LIGHT_RAIN      => 0.35f,
        WeatherCondition.RAIN            => 0.50f,
        WeatherCondition.HEAVY_RAIN      => 0.68f,
        WeatherCondition.EXTREME_RAIN    => 0.88f,
        WeatherCondition.FREEZING_RAIN   => 0.55f,
        WeatherCondition.LIGHT_SHOWER    => 0.38f,
        WeatherCondition.SHOWER          => 0.60f,
        WeatherCondition.RAGGED_SHOWER   => 0.72f,
        WeatherCondition.THUNDER_DRIZZLE => 0.22f,
        WeatherCondition.THUNDER_LIGHT   => 0.55f,
        WeatherCondition.THUNDERSTORM    => 0.80f,
        WeatherCondition.HEAVY_THUNDER   => 0.95f,
        WeatherCondition.THUNDER_HAIL    => 0.85f,
        WeatherCondition.SQUALL          => 1.00f,
        WeatherCondition.SLEET           => 0.40f,
        WeatherCondition.RAIN_SNOW       => 0.30f,
        _                                => 0f,
    };

    private static float GetSnowIntensity(WeatherCondition c) => c switch
    {
        WeatherCondition.LIGHT_SNOW      => 0.20f,
        WeatherCondition.SNOW            => 0.50f,
        WeatherCondition.HEAVY_SNOW      => 0.75f,
        WeatherCondition.BLIZZARD        => 1.00f,
        WeatherCondition.SLEET           => 0.35f,
        WeatherCondition.RAIN_SNOW       => 0.30f,
        _                                => 0f,
    };

    private static float GetHazeIntensity(WeatherCondition c) => c switch
    {
        WeatherCondition.MIST            => 0.15f,
        WeatherCondition.FOG             => 0.40f,
        WeatherCondition.DENSE_FOG       => 0.80f,
        WeatherCondition.HAZE            => 0.65f,
        WeatherCondition.SMOKE           => 0.72f,
        WeatherCondition.SAND_WHIRLS     => 0.45f,
        WeatherCondition.SAND            => 0.60f,
        WeatherCondition.DUST            => 0.55f,
        WeatherCondition.VOLCANIC_ASH    => 1.00f,
        WeatherCondition.TORNADO         => 0.70f,
        _                                => 0f,
    };

    // ── Barakah rates ─────────────────────────────────────────────

    private void ApplyBarakah()
    {
        if (CaveManager.Instance != null && CaveManager.Instance.InsideCave) return;

        float rate = BarakahRate(_active);
        if (Mathf.Abs(rate) < 0.001f) return;

        barakahMeter.AddBarakah(rate * 0.5f,
            rate > 0 ? BarakahSource.HumaneCare : BarakahSource.Restraint);
    }

    private static float BarakahRate(WeatherCondition c) => c switch
    {
        // Gentle rain — life-giving
        WeatherCondition.LIGHT_DRIZZLE   =>  0.4f,
        WeatherCondition.DRIZZLE         =>  0.5f,
        WeatherCondition.HEAVY_DRIZZLE   =>  0.6f,
        WeatherCondition.LIGHT_RAIN      =>  0.8f,
        WeatherCondition.RAIN            =>  1.0f,
        WeatherCondition.LIGHT_SHOWER    =>  0.7f,
        WeatherCondition.SHOWER          =>  0.6f,
        WeatherCondition.RAIN_SNOW       =>  0.3f,
        WeatherCondition.LIGHT_SNOW      =>  0.4f,
        WeatherCondition.SNOW            =>  0.5f,
        // Intense / destructive precipitation — mild drain
        WeatherCondition.HEAVY_RAIN      => -0.3f,
        WeatherCondition.EXTREME_RAIN    => -0.6f,
        WeatherCondition.RAGGED_SHOWER   => -0.3f,
        WeatherCondition.HEAVY_SNOW      => -0.2f,
        WeatherCondition.BLIZZARD        => -0.5f,
        WeatherCondition.SLEET           => -0.3f,
        WeatherCondition.FREEZING_RAIN   => -0.4f,
        // Thunderstorms
        WeatherCondition.THUNDER_LIGHT   => -0.2f,
        WeatherCondition.THUNDERSTORM    => -0.5f,
        WeatherCondition.HEAVY_THUNDER   => -0.8f,
        WeatherCondition.THUNDER_HAIL    => -0.7f,
        WeatherCondition.THUNDER_DRIZZLE => -0.1f,
        WeatherCondition.SQUALL          => -0.6f,
        WeatherCondition.TORNADO         => -1.5f,
        // Ecological harm — significant drain
        WeatherCondition.HAZE            => -2.0f,  // peat/plantation fire haze
        WeatherCondition.SMOKE           => -1.5f,
        WeatherCondition.VOLCANIC_ASH    => -1.0f,
        WeatherCondition.DUST            => -0.6f,
        WeatherCondition.SAND            => -0.5f,
        WeatherCondition.SAND_WHIRLS     => -0.3f,
        // Neutral
        _                                =>  0f,
    };

    // ── Helpers ───────────────────────────────────────────────────

    private static bool IsRainCondition(WeatherCondition c) =>
        GetRainIntensity(c) > 0.05f;

    private static bool IsHazeCondition(WeatherCondition c) =>
        GetHazeIntensity(c) > 0.05f;

    private static bool IsSnowCondition(WeatherCondition c) =>
        GetSnowIntensity(c) > 0.05f;

    private static void SetVolumeWeight(Volume v, float w)
    {
        if (v != null) v.weight = w;
    }

    // ── Save / restore ────────────────────────────────────────────

    private void SaveState()
    {
        _savedAmbientSky = RenderSettings.ambientSkyColor;
        _savedFogDensity = RenderSettings.fogDensity;
        _savedFogColor   = RenderSettings.fogColor;
        _savedFog        = RenderSettings.fog;
    }

    private void DestroyAll()
    {
        if (_precip  != null) { Destroy(_precip);  _precip  = null; }
        if (_precip2 != null) { Destroy(_precip2); _precip2 = null; }
        if (_atmos   != null) { Destroy(_atmos);   _atmos   = null; }
        if (_vortex  != null) { Destroy(_vortex);  _vortex  = null; }
        StopThunder();
    }

    // ── Public status ─────────────────────────────────────────────
    public WeatherCondition ActiveCondition => _active;
}
