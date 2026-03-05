using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// ── Weather condition enum — full OWM range ────────────────────────────────

public enum WeatherCondition
{
    // ── Clear / Cloud ──────────────────────────────────────────────
    CLEAR            = 0,   // 800   — full sunshine
    MOSTLY_CLEAR     = 1,   // 801   — few clouds (11-25 %)
    PARTLY_CLOUDY    = 2,   // 802   — scattered (25-50 %)
    BROKEN_CLOUDS    = 3,   // 803   — broken (51-84 %)
    OVERCAST         = 4,   // 804   — full overcast (85-100 %)

    // ── Drizzle ────────────────────────────────────────────────────
    LIGHT_DRIZZLE    = 10,  // 300-301
    DRIZZLE          = 11,  // 302-311
    HEAVY_DRIZZLE    = 12,  // 312-321

    // ── Rain ───────────────────────────────────────────────────────
    LIGHT_RAIN       = 20,  // 500
    RAIN             = 21,  // 501
    HEAVY_RAIN       = 22,  // 502-503
    EXTREME_RAIN     = 23,  // 504
    FREEZING_RAIN    = 24,  // 511
    LIGHT_SHOWER     = 25,  // 520
    SHOWER           = 26,  // 521-522
    RAGGED_SHOWER    = 27,  // 531

    // ── Thunderstorm ───────────────────────────────────────────────
    THUNDER_LIGHT    = 30,  // 200-201   — thunderstorm + light rain
    THUNDERSTORM     = 31,  // 202, 210-211
    HEAVY_THUNDER    = 32,  // 212
    THUNDER_DRIZZLE  = 33,  // 230-232   — thunderstorm + drizzle
    THUNDER_HAIL     = 34,  // 221       — thunderstorm + hail

    // ── Snow ───────────────────────────────────────────────────────
    LIGHT_SNOW       = 40,  // 600, 620
    SNOW             = 41,  // 601, 621
    HEAVY_SNOW       = 42,  // 602, 622
    SLEET            = 43,  // 611-613
    RAIN_SNOW        = 44,  // 615-616
    BLIZZARD         = 45,  // heavy snow + wind ≥ 10 m/s

    // ── Atmosphere ─────────────────────────────────────────────────
    MIST             = 50,  // 701   — visibility 1-2 km
    SMOKE            = 51,  // 711
    HAZE             = 52,  // 721   — transboundary peat / plantation haze
    SAND_WHIRLS      = 53,  // 731
    FOG              = 54,  // 741   — visibility 0.5-1 km
    DENSE_FOG        = 55,  //       — visibility < 200 m (inferred from vis field)
    SAND             = 56,  // 751
    DUST             = 57,  // 761
    VOLCANIC_ASH     = 58,  // 762
    SQUALL           = 59,  // 771
    TORNADO          = 60,  // 781

    UNKNOWN          = 99,
}

// ── Weather snapshot ────────────────────────────────────────────────────────

[Serializable]
public class WeatherSnapshot
{
    public WeatherCondition condition;
    public float tempCelsius;
    public float humidity;        // %
    public float windSpeedMs;     // m/s
    public float windGustMs;      // m/s
    public float windDeg;         // degrees from N
    public float visibilityM;     // metres
    public float cloudCoverage;   // 0-100 %
    public float rainMmPerHour;   // mm/h (if present)
    public float snowMmPerHour;   // mm/h (if present)
    public string description;
    public bool isDay;

    public override string ToString() =>
        $"{condition} | {tempCelsius:F1}°C | RH {humidity}% | " +
        $"wind {windSpeedMs:F1}m/s {windDeg:F0}° | vis {visibilityM:F0}m | {description}";
}

// ── OWM JSON shims ──────────────────────────────────────────────────────────

[Serializable] class OWM_Root    { public OWM_Weather[] weather; public OWM_Main main; public OWM_Wind wind; public OWM_Clouds clouds; public float visibility; public OWM_Rain rain; public OWM_Snow snow; public OWM_Sys sys; public int timezone; }
[Serializable] class OWM_Weather { public int id; public string main; public string description; public string icon; }
[Serializable] class OWM_Main    { public float temp; public float humidity; }
[Serializable] class OWM_Wind    { public float speed; public float deg; public float gust; }
[Serializable] class OWM_Clouds  { public float all; }
[Serializable] class OWM_Rain    { public float _1h; public float _3h; }
[Serializable] class OWM_Snow    { public float _1h; public float _3h; }
[Serializable] class OWM_Sys     { public long sunrise; public long sunset; }

/// <summary>
/// Fetches real-world weather from OpenWeatherMap for the player's GPS location.
///
/// Setup:
///   1. Get a free API key from openweathermap.org
///   2. Paste it into the "Api Key" field on this component in the Inspector
///
/// Exposes 37 weather conditions with full visual + Barakah treatment.
/// Events: OnWeatherChanged(snap)  — condition changed
///         OnWeatherRefreshed(snap) — every successful fetch (for wind updates)
/// Public: ForceCondition(cond) — test without API
/// </summary>
public class WeatherService : MonoBehaviour
{
    [Header("API")]
    [Tooltip("OpenWeatherMap API key — openweathermap.org (free tier)")]
    [SerializeField] private string apiKey = "";
    [Tooltip("Fetch interval in minutes")]
    [SerializeField] private float refreshMinutes = 10f;

    [Header("Fallback GPS")]
    [SerializeField] private float fallbackLat =  1.3521f;
    [SerializeField] private float fallbackLon = 103.8198f;

    // ── Singleton ──────────────────────────────────────────────────
    public static WeatherService Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public static event Action<WeatherSnapshot> OnWeatherChanged;
    public static event Action<WeatherSnapshot> OnWeatherRefreshed;

    // ── State ──────────────────────────────────────────────────────
    public WeatherSnapshot Current { get; private set; } = new WeatherSnapshot
    {
        condition = WeatherCondition.CLEAR, tempCelsius = 28f,
        humidity = 75f, isDay = true, visibilityM = 10000f,
        description = "clear sky",
    };
    public bool  HasData                  { get; private set; }
    /// <summary>OWM timezone offset in decimal hours (0 if not yet fetched).</summary>
    public float OWMTimezoneOffsetHours   { get; private set; }

    private CesiumForUnity.CesiumGlobeAnchor _playerAnchor;
    private double _lat, _lon;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    IEnumerator Start()
    {
        yield return null;
        var rig = GameObject.Find("PlayerRig");
        if (rig != null) _playerAnchor = rig.GetComponent<CesiumForUnity.CesiumGlobeAnchor>();
        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            ReadGPS();
            if (!string.IsNullOrWhiteSpace(apiKey))
                yield return FetchWeather();
            else
                Debug.LogWarning("[WeatherService] No API key — using fallback clear weather.");
            yield return new WaitForSeconds(refreshMinutes * 60f);
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

    // ── API ────────────────────────────────────────────────────────

    private IEnumerator FetchWeather()
    {
        string url = $"https://api.openweathermap.org/data/2.5/weather" +
                     $"?lat={_lat:F6}&lon={_lon:F6}&appid={apiKey}&units=metric";

        using var req = UnityWebRequest.Get(url);
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[WeatherService] Fetch failed: {req.error}");
            yield break;
        }

        var snap = Parse(req.downloadHandler.text);
        if (snap == null) yield break;

        bool changed = snap.condition != Current.condition;
        Current = snap;
        HasData = true;

        OnWeatherRefreshed?.Invoke(snap);
        if (changed) OnWeatherChanged?.Invoke(snap);

        Debug.Log($"[WeatherService] {snap}");
    }

    // ── JSON parse ─────────────────────────────────────────────────

    private WeatherSnapshot Parse(string json)
    {
        OWM_Root r;
        try   { r = JsonUtility.FromJson<OWM_Root>(json); }
        catch { Debug.LogWarning("[WeatherService] JSON parse error"); return null; }
        if (r?.weather == null || r.weather.Length == 0) return null;

        long now   = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool isDay = r.sys != null && now >= r.sys.sunrise && now <= r.sys.sunset;

        float vis  = r.visibility > 0 ? r.visibility : 10000f;
        float rain = r.rain != null ? (r.rain._1h > 0 ? r.rain._1h : r.rain._3h / 3f) : 0f;
        float snow = r.snow != null ? (r.snow._1h > 0 ? r.snow._1h : r.snow._3h / 3f) : 0f;

        // Expose OWM's timezone offset (seconds → hours) for TimeZoneService
        OWMTimezoneOffsetHours = r.timezone / 3600f;

        var cond = MapCondition(r.weather[0].id, vis, r.wind.speed);

        return new WeatherSnapshot
        {
            condition      = cond,
            tempCelsius    = r.main.temp,
            humidity       = r.main.humidity,
            windSpeedMs    = r.wind.speed,
            windGustMs     = r.wind.gust,
            windDeg        = r.wind.deg,
            visibilityM    = vis,
            cloudCoverage  = r.clouds.all,
            rainMmPerHour  = rain,
            snowMmPerHour  = snow,
            description    = r.weather[0].description,
            isDay          = isDay,
        };
    }

    private static WeatherCondition MapCondition(int id, float visM, float windMs)
    {
        return id switch
        {
            // Thunderstorm
            200 or 201             => WeatherCondition.THUNDER_LIGHT,
            202                    => WeatherCondition.THUNDERSTORM,
            210 or 211             => WeatherCondition.THUNDERSTORM,
            212                    => WeatherCondition.HEAVY_THUNDER,
            221                    => WeatherCondition.THUNDER_HAIL,
            230 or 231 or 232      => WeatherCondition.THUNDER_DRIZZLE,
            >= 200 and <= 232      => WeatherCondition.THUNDERSTORM,

            // Drizzle
            300 or 301             => WeatherCondition.LIGHT_DRIZZLE,
            302                    => WeatherCondition.HEAVY_DRIZZLE,
            >= 310 and <= 311      => WeatherCondition.DRIZZLE,
            312                    => WeatherCondition.HEAVY_DRIZZLE,
            >= 313 and <= 321      => WeatherCondition.HEAVY_DRIZZLE,
            >= 300 and <= 321      => WeatherCondition.DRIZZLE,

            // Rain
            500                    => WeatherCondition.LIGHT_RAIN,
            501                    => WeatherCondition.RAIN,
            502 or 503             => WeatherCondition.HEAVY_RAIN,
            504                    => WeatherCondition.EXTREME_RAIN,
            511                    => WeatherCondition.FREEZING_RAIN,
            520                    => WeatherCondition.LIGHT_SHOWER,
            521 or 522             => WeatherCondition.SHOWER,
            531                    => WeatherCondition.RAGGED_SHOWER,
            >= 500 and <= 531      => WeatherCondition.RAIN,

            // Snow — blizzard if wind high
            600 or 620             => WeatherCondition.LIGHT_SNOW,
            601 or 621             => windMs >= 10f ? WeatherCondition.BLIZZARD : WeatherCondition.SNOW,
            602 or 622             => windMs >= 10f ? WeatherCondition.BLIZZARD : WeatherCondition.HEAVY_SNOW,
            611 or 612 or 613      => WeatherCondition.SLEET,
            615 or 616             => WeatherCondition.RAIN_SNOW,
            >= 600 and <= 622      => WeatherCondition.SNOW,

            // Atmosphere
            701                    => WeatherCondition.MIST,
            711                    => WeatherCondition.SMOKE,
            721                    => WeatherCondition.HAZE,
            731                    => WeatherCondition.SAND_WHIRLS,
            741                    => visM < 200f ? WeatherCondition.DENSE_FOG : WeatherCondition.FOG,
            751                    => WeatherCondition.SAND,
            761                    => WeatherCondition.DUST,
            762                    => WeatherCondition.VOLCANIC_ASH,
            771                    => WeatherCondition.SQUALL,
            781                    => WeatherCondition.TORNADO,

            // Cloud / Clear
            800                    => WeatherCondition.CLEAR,
            801                    => WeatherCondition.MOSTLY_CLEAR,
            802                    => WeatherCondition.PARTLY_CLOUDY,
            803                    => WeatherCondition.BROKEN_CLOUDS,
            >= 804                 => WeatherCondition.OVERCAST,

            _                      => WeatherCondition.UNKNOWN,
        };
    }

    // ── Public helpers ─────────────────────────────────────────────

    public bool IsRaining    => Current.condition is
        WeatherCondition.LIGHT_DRIZZLE or WeatherCondition.DRIZZLE or WeatherCondition.HEAVY_DRIZZLE or
        WeatherCondition.LIGHT_RAIN or WeatherCondition.RAIN or WeatherCondition.HEAVY_RAIN or
        WeatherCondition.EXTREME_RAIN or WeatherCondition.FREEZING_RAIN or
        WeatherCondition.LIGHT_SHOWER or WeatherCondition.SHOWER or WeatherCondition.RAGGED_SHOWER or
        WeatherCondition.THUNDER_LIGHT or WeatherCondition.THUNDERSTORM or WeatherCondition.HEAVY_THUNDER or
        WeatherCondition.THUNDER_DRIZZLE or WeatherCondition.THUNDER_HAIL or WeatherCondition.SQUALL;

    public bool IsSnowing    => Current.condition is
        WeatherCondition.LIGHT_SNOW or WeatherCondition.SNOW or WeatherCondition.HEAVY_SNOW or
        WeatherCondition.BLIZZARD or WeatherCondition.SLEET or WeatherCondition.RAIN_SNOW;

    public bool IsHazardous  => Current.condition is
        WeatherCondition.HAZE or WeatherCondition.SMOKE or WeatherCondition.DUST or
        WeatherCondition.SAND or WeatherCondition.SAND_WHIRLS or WeatherCondition.VOLCANIC_ASH or
        WeatherCondition.SQUALL or WeatherCondition.TORNADO;

    public bool IsReducedVis => Current.condition is
        WeatherCondition.MIST or WeatherCondition.FOG or WeatherCondition.DENSE_FOG or
        WeatherCondition.HAZE or WeatherCondition.SMOKE or WeatherCondition.DUST or
        WeatherCondition.SAND or WeatherCondition.VOLCANIC_ASH;

    /// <summary>Force a condition for testing — bypasses API.</summary>
    public void ForceCondition(WeatherCondition cond)
    {
        var snap = new WeatherSnapshot
        {
            condition   = cond,
            tempCelsius = Current.tempCelsius,
            humidity    = Current.humidity,
            windSpeedMs = Current.windSpeedMs,
            windGustMs  = Current.windGustMs,
            windDeg     = Current.windDeg,
            visibilityM = Current.visibilityM,
            isDay       = Current.isDay,
            description = cond.ToString().ToLower().Replace('_', ' '),
        };
        Current = snap;
        OnWeatherChanged?.Invoke(snap);
        OnWeatherRefreshed?.Invoke(snap);
    }
}
