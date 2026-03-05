using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Reacts to WeatherService.OnWeatherChanged to apply:
///   - Rain / drizzle / heavy-rain / thunderstorm particle systems
///   - Fog density + tint (fog, mist, haze, smoke, dust)
///   - Ambient light colour shift (overcast → blue-grey, haze → orange-brown)
///   - Wind-driven particle speed
///   - Thunder flash (brief point light spike on thunderstorm)
///   - Optional URP post-process Volume weight
///   - Barakah drain/gain every 0.5 s based on condition
///
/// Respects cave state — weather FX are suppressed while inside a cave.
/// Does NOT override CaveEnvironmentController; cave atmosphere always wins.
/// </summary>
public class WeatherEnvironmentController : MonoBehaviour
{
    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 0.6f;

    [Header("URP Post-Processing (optional)")]
    [Tooltip("Weight is lerped to 1 during rain/storm, 0 otherwise")]
    [SerializeField] private Volume rainVolume;
    [Tooltip("Weight is lerped to 1 during haze/fog/smoke, 0 otherwise")]
    [SerializeField] private Volume hazeVolume;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter barakahMeter;

    // ── Saved exterior state ──────────────────────────────────────
    private Color _savedAmbientSky;
    private float _savedFogDensity;
    private Color _savedFogColor;
    private bool  _savedFog;

    // ── Runtime ───────────────────────────────────────────────────
    private WeatherCondition  _currentCondition = WeatherCondition.UNKNOWN;
    private GameObject        _rainParticles;
    private GameObject        _fogParticles;
    private Light             _thunderLight;
    private Coroutine         _transitionCo;
    private Coroutine         _thunderCo;
    private float             _barakahTimer;

    // Shader globals
    private static readonly int ID_WeatherType     = Shader.PropertyToID("_IntanIsle_WeatherType");
    private static readonly int ID_RainIntensity   = Shader.PropertyToID("_IntanIsle_RainIntensity");
    private static readonly int ID_HazeIntensity   = Shader.PropertyToID("_IntanIsle_HazeIntensity");

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (barakahMeter == null) barakahMeter = FindObjectOfType<BarakahMeter>();
        SaveState();

        Shader.SetGlobalFloat(ID_WeatherType,   0f);
        Shader.SetGlobalFloat(ID_RainIntensity, 0f);
        Shader.SetGlobalFloat(ID_HazeIntensity, 0f);
    }

    void OnEnable()
    {
        WeatherService.OnWeatherChanged   += HandleWeatherChanged;
        WeatherService.OnWeatherRefreshed += HandleWeatherRefreshed;
    }

    void OnDisable()
    {
        WeatherService.OnWeatherChanged   -= HandleWeatherChanged;
        WeatherService.OnWeatherRefreshed -= HandleWeatherRefreshed;
    }

    void Update()
    {
        if (barakahMeter == null) return;

        _barakahTimer += Time.deltaTime;
        if (_barakahTimer < 0.5f) return;
        _barakahTimer = 0f;

        ApplyWeatherBarakah();
    }

    // ── Event handlers ────────────────────────────────────────────

    private void HandleWeatherChanged(WeatherSnapshot snap)
    {
        if (CaveManager.Instance != null && CaveManager.Instance.InsideCave) return;

        _currentCondition = snap.condition;

        if (_transitionCo != null) StopCoroutine(_transitionCo);
        _transitionCo = StartCoroutine(TransitionToWeather(snap));
    }

    private void HandleWeatherRefreshed(WeatherSnapshot snap)
    {
        // Update wind on particles even if condition didn't change
        ApplyWindToParticles(snap.windSpeedMs, snap.windDeg);
    }

    // ── Main transition ───────────────────────────────────────────

    private IEnumerator TransitionToWeather(WeatherSnapshot snap)
    {
        // --- clean up previous FX
        DestroyParticles();
        if (_thunderCo != null) { StopCoroutine(_thunderCo); _thunderCo = null; }

        // --- compute targets
        Color  targetAmbient    = AmbientColor(snap);
        float  targetFogDensity = FogDensity(snap);
        Color  targetFogColor   = FogColor(snap);
        bool   needFog          = targetFogDensity > 0.001f;
        float  targetRainVol    = snap.condition.IsRaining()   ? 1f : 0f;
        float  targetHazeVol    = snap.condition.IsAtmospheric() ? 1f : 0f;

        if (needFog) RenderSettings.fog = true;

        float t = 0f;
        Color startAmbient    = RenderSettings.ambientSkyColor;
        float startFogDensity = RenderSettings.fogDensity;
        Color startFogColor   = RenderSettings.fogColor;

        while (t < 1f)
        {
            t = Mathf.MoveTowards(t, 1f, Time.deltaTime * transitionSpeed);

            RenderSettings.ambientSkyColor = Color.Lerp(startAmbient,    targetAmbient,    t);
            RenderSettings.fogDensity      = Mathf.Lerp(startFogDensity, targetFogDensity, t);
            RenderSettings.fogColor        = Color.Lerp(startFogColor,   targetFogColor,   t);
            RenderSettings.fog             = needFog || t < 1f || _savedFog;

            if (rainVolume != null) rainVolume.weight = Mathf.Lerp(rainVolume.weight, targetRainVol, t);
            if (hazeVolume != null) hazeVolume.weight = Mathf.Lerp(hazeVolume.weight, targetHazeVol, t);

            yield return null;
        }

        if (!needFog && !_savedFog) RenderSettings.fog = false;

        // --- spawn new FX
        SpawnWeatherParticles(snap);

        // --- shader globals
        Shader.SetGlobalFloat(ID_WeatherType,   (float)snap.condition);
        Shader.SetGlobalFloat(ID_RainIntensity, RainIntensity(snap.condition));
        Shader.SetGlobalFloat(ID_HazeIntensity, HazeIntensity(snap.condition));

        // --- thunder
        if (snap.condition == WeatherCondition.THUNDERSTORM)
            _thunderCo = StartCoroutine(ThunderLoop());
    }

    // ── Particle FX ───────────────────────────────────────────────

    private void SpawnWeatherParticles(WeatherSnapshot snap)
    {
        switch (snap.condition)
        {
            case WeatherCondition.DRIZZLE:
                _rainParticles = MakeRainSystem(80f,  0.04f, 2f,  new Color(0.7f, 0.8f, 1f, 0.35f));
                break;
            case WeatherCondition.RAIN:
                _rainParticles = MakeRainSystem(300f, 0.05f, 6f,  new Color(0.6f, 0.75f, 1f, 0.5f));
                break;
            case WeatherCondition.HEAVY_RAIN:
            case WeatherCondition.SHOWER:
                _rainParticles = MakeRainSystem(700f, 0.06f, 9f,  new Color(0.5f, 0.7f, 1f,  0.6f));
                break;
            case WeatherCondition.THUNDERSTORM:
                _rainParticles = MakeRainSystem(900f, 0.07f, 12f, new Color(0.4f, 0.6f, 0.9f, 0.65f));
                break;
            case WeatherCondition.SNOW:
                _rainParticles = MakeSnowSystem();
                break;
            case WeatherCondition.FOG:
            case WeatherCondition.MIST:
                _fogParticles = MakeFogSystem(new Color(0.85f, 0.88f, 0.9f, 0.12f));
                break;
            case WeatherCondition.HAZE:
                _fogParticles = MakeFogSystem(new Color(0.9f, 0.78f, 0.5f, 0.18f));
                break;
            case WeatherCondition.SMOKE:
                _fogParticles = MakeFogSystem(new Color(0.6f, 0.55f, 0.45f, 0.25f));
                break;
            case WeatherCondition.DUST:
                _fogParticles = MakeFogSystem(new Color(0.85f, 0.72f, 0.45f, 0.22f));
                break;
            case WeatherCondition.VOLCANIC_ASH:
                _fogParticles = MakeFogSystem(new Color(0.45f, 0.42f, 0.4f, 0.3f));
                break;
        }

        ApplyWindToParticles(snap.windSpeedMs, snap.windDeg);
    }

    private GameObject MakeRainSystem(float rate, float size, float speed, Color color)
    {
        var go   = new GameObject("WeatherRain");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 20f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime      = 1.8f;
        main.startSpeed         = speed;
        main.startSize          = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startColor         = color;
        main.gravityModifier    = 1.5f;
        main.maxParticles       = 3000;
        main.simulationSpace    = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(60f, 0.1f, 60f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale   = 2f;

        return go;
    }

    private GameObject MakeSnowSystem()
    {
        var go   = new GameObject("WeatherSnow");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 15f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = 5f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor      = new Color(0.95f, 0.97f, 1f, 0.7f);
        main.gravityModifier = 0.15f;
        main.maxParticles    = 2000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 150f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(50f, 0.1f, 50f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material   = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        return go;
    }

    private GameObject MakeFogSystem(Color color)
    {
        var go   = new GameObject("WeatherFog");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 1f;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime   = 12f;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(4f, 12f);
        main.startColor      = color;
        main.gravityModifier = 0f;
        main.maxParticles    = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(80f, 4f, 80f);

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

    private void DestroyParticles()
    {
        if (_rainParticles != null) { Destroy(_rainParticles); _rainParticles = null; }
        if (_fogParticles  != null) { Destroy(_fogParticles);  _fogParticles  = null; }
    }

    // ── Wind ─────────────────────────────────────────────────────

    private void ApplyWindToParticles(float speedMs, float deg)
    {
        // Convert met wind direction (where wind comes FROM, degrees from N)
        // to Unity velocity vector (XZ plane)
        float rad    = (deg + 180f) * Mathf.Deg2Rad;  // flip: "from" → "to"
        float vx     = Mathf.Sin(rad) * speedMs * 0.3f;
        float vz     = Mathf.Cos(rad) * speedMs * 0.3f;

        SetWindOnSystem(_rainParticles, vx, vz);
        SetWindOnSystem(_fogParticles,  vx * 0.3f, vz * 0.3f);
    }

    private static void SetWindOnSystem(GameObject go, float vx, float vz)
    {
        if (go == null) return;
        var ps  = go.GetComponent<ParticleSystem>();
        if (ps  == null) return;
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(vx - 0.5f, vx + 0.5f);
        vel.z = new ParticleSystem.MinMaxCurve(vz - 0.5f, vz + 0.5f);
    }

    // ── Thunder flash ────────────────────────────────────────────

    private IEnumerator ThunderLoop()
    {
        if (_thunderLight == null)
        {
            var go           = new GameObject("ThunderLight");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.up * 100f;
            _thunderLight              = go.AddComponent<Light>();
            _thunderLight.type         = LightType.Directional;
            _thunderLight.color        = new Color(0.8f, 0.85f, 1f);
            _thunderLight.intensity    = 0f;
        }

        while (true)
        {
            // Wait random interval between strikes
            yield return new WaitForSeconds(UnityEngine.Random.Range(8f, 30f));

            // Double flash
            for (int i = 0; i < 2; i++)
            {
                _thunderLight.intensity = UnityEngine.Random.Range(3f, 8f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.04f, 0.12f));
                _thunderLight.intensity = 0f;
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    // ── RenderSettings targets ────────────────────────────────────

    private static Color AmbientColor(WeatherSnapshot snap) => snap.condition switch
    {
        WeatherCondition.CLEAR        => new Color(0.55f, 0.65f, 0.8f),
        WeatherCondition.MOSTLY_CLEAR => new Color(0.5f,  0.6f,  0.75f),
        WeatherCondition.PARTLY_CLOUDY=> new Color(0.45f, 0.52f, 0.65f),
        WeatherCondition.OVERCAST     => new Color(0.35f, 0.38f, 0.45f),
        WeatherCondition.RAIN         => new Color(0.3f,  0.35f, 0.45f),
        WeatherCondition.HEAVY_RAIN   => new Color(0.22f, 0.26f, 0.38f),
        WeatherCondition.THUNDERSTORM => new Color(0.18f, 0.2f,  0.32f),
        WeatherCondition.DRIZZLE      => new Color(0.38f, 0.42f, 0.52f),
        WeatherCondition.SHOWER       => new Color(0.3f,  0.34f, 0.44f),
        WeatherCondition.FOG          => new Color(0.55f, 0.56f, 0.58f),
        WeatherCondition.MIST         => new Color(0.58f, 0.6f,  0.62f),
        WeatherCondition.HAZE         => new Color(0.72f, 0.62f, 0.38f),
        WeatherCondition.SMOKE        => new Color(0.5f,  0.45f, 0.35f),
        WeatherCondition.DUST         => new Color(0.75f, 0.65f, 0.4f),
        WeatherCondition.VOLCANIC_ASH => new Color(0.38f, 0.35f, 0.32f),
        WeatherCondition.SNOW         => new Color(0.75f, 0.78f, 0.85f),
        WeatherCondition.SQUALL       => new Color(0.28f, 0.3f,  0.4f),
        _                             => new Color(0.55f, 0.65f, 0.8f),
    };

    private static float FogDensity(WeatherSnapshot snap) => snap.condition switch
    {
        WeatherCondition.FOG          => 0.025f,
        WeatherCondition.MIST         => 0.012f,
        WeatherCondition.HAZE         => 0.018f,
        WeatherCondition.SMOKE        => 0.022f,
        WeatherCondition.DUST         => 0.020f,
        WeatherCondition.VOLCANIC_ASH => 0.030f,
        WeatherCondition.HEAVY_RAIN   => 0.010f,
        WeatherCondition.THUNDERSTORM => 0.012f,
        WeatherCondition.OVERCAST     => 0.002f,
        WeatherCondition.SNOW         => 0.008f,
        WeatherCondition.SQUALL       => 0.015f,
        _                             => 0f,
    };

    private static Color FogColor(WeatherSnapshot snap) => snap.condition switch
    {
        WeatherCondition.HAZE         => new Color(0.85f, 0.72f, 0.45f),
        WeatherCondition.SMOKE        => new Color(0.6f,  0.55f, 0.44f),
        WeatherCondition.DUST         => new Color(0.85f, 0.72f, 0.48f),
        WeatherCondition.VOLCANIC_ASH => new Color(0.42f, 0.4f,  0.38f),
        WeatherCondition.SNOW         => new Color(0.88f, 0.90f, 0.95f),
        _                             => new Color(0.72f, 0.75f, 0.82f),
    };

    private static float RainIntensity(WeatherCondition c) => c switch
    {
        WeatherCondition.DRIZZLE      => 0.2f,
        WeatherCondition.RAIN         => 0.5f,
        WeatherCondition.SHOWER       => 0.65f,
        WeatherCondition.HEAVY_RAIN   => 0.85f,
        WeatherCondition.THUNDERSTORM => 1.0f,
        _                             => 0f,
    };

    private static float HazeIntensity(WeatherCondition c) => c switch
    {
        WeatherCondition.MIST         => 0.2f,
        WeatherCondition.FOG          => 0.5f,
        WeatherCondition.HAZE         => 0.7f,
        WeatherCondition.SMOKE        => 0.8f,
        WeatherCondition.DUST         => 0.75f,
        WeatherCondition.VOLCANIC_ASH => 1.0f,
        _                             => 0f,
    };

    // ── Barakah effects ───────────────────────────────────────────

    private void ApplyWeatherBarakah()
    {
        if (barakahMeter == null) return;
        if (CaveManager.Instance != null && CaveManager.Instance.InsideCave) return;

        float rate = BarakahRate(_currentCondition);
        if (Mathf.Abs(rate) > 0.001f)
        {
            var source = rate > 0 ? BarakahSource.HumaneCare : BarakahSource.Restraint;
            barakahMeter.AddBarakah(rate * 0.5f, source);
        }
    }

    private static float BarakahRate(WeatherCondition c) => c switch
    {
        // Life-giving rain — gentle positive
        WeatherCondition.DRIZZLE      =>  0.5f,
        WeatherCondition.RAIN         =>  0.8f,
        WeatherCondition.SHOWER       =>  0.6f,
        WeatherCondition.SNOW         =>  0.4f,
        // Violent weather — slight drain
        WeatherCondition.HEAVY_RAIN   => -0.3f,
        WeatherCondition.THUNDERSTORM => -0.5f,
        WeatherCondition.SQUALL       => -0.4f,
        // Ecological harm — significant drain
        WeatherCondition.HAZE         => -2.0f,  // transboundary haze (peat fires)
        WeatherCondition.SMOKE        => -1.5f,
        WeatherCondition.VOLCANIC_ASH => -1.0f,
        WeatherCondition.DUST         => -0.8f,
        // Neutral / clear
        _                             =>  0f,
    };

    // ── Save / restore ────────────────────────────────────────────

    private void SaveState()
    {
        _savedAmbientSky  = RenderSettings.ambientSkyColor;
        _savedFogDensity  = RenderSettings.fogDensity;
        _savedFogColor    = RenderSettings.fogColor;
        _savedFog         = RenderSettings.fog;
    }

    // ── Public status ─────────────────────────────────────────────
    public WeatherCondition CurrentCondition => _currentCondition;

    void OnDisable()
    {
        DestroyParticles();
        if (_thunderLight != null) Destroy(_thunderLight.gameObject);
    }
}

// ── Extension helpers ──────────────────────────────────────────────────────

public static class WeatherConditionExtensions
{
    public static bool IsRaining(this WeatherCondition c) =>
        c is WeatherCondition.DRIZZLE or WeatherCondition.RAIN or WeatherCondition.HEAVY_RAIN
          or WeatherCondition.SHOWER  or WeatherCondition.THUNDERSTORM;

    public static bool IsAtmospheric(this WeatherCondition c) =>
        c is WeatherCondition.FOG or WeatherCondition.MIST or WeatherCondition.HAZE
          or WeatherCondition.SMOKE or WeatherCondition.DUST or WeatherCondition.VOLCANIC_ASH;
}
