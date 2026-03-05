using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// ── Weather condition enum ─────────────────────────────────────────────────

public enum WeatherCondition
{
    CLEAR,          // 800
    MOSTLY_CLEAR,   // 801
    PARTLY_CLOUDY,  // 802
    OVERCAST,       // 803-804
    MIST,           // 701
    SMOKE,          // 711
    HAZE,           // 721  (transboundary peat/plantation haze)
    DUST,           // 731, 761
    FOG,            // 741
    DRIZZLE,        // 3xx
    RAIN,           // 500-501
    HEAVY_RAIN,     // 502-504
    SHOWER,         // 520-531
    THUNDERSTORM,   // 2xx
    SNOW,           // 6xx
    VOLCANIC_ASH,   // 762
    SQUALL,         // 771
    UNKNOWN,
}

// ── Parsed weather snapshot ────────────────────────────────────────────────

[Serializable]
public class WeatherSnapshot
{
    public WeatherCondition condition = WeatherCondition.UNKNOWN;
    public float  tempCelsius;
    public float  humidity;        // 0-100 %
    public float  windSpeedMs;     // m/s
    public float  windDeg;         // degrees
    public float  visibilityM;     // metres (default 10 000)
    public float  cloudCoverage;   // 0-100 %
    public string description;
    public bool   isDay;           // true = daytime icon

    public override string ToString() =>
        $"{condition} | {tempCelsius:F1}°C | {humidity}% RH | wind {windSpeedMs:F1}m/s | vis {visibilityM:F0}m";
}

// ── OpenWeatherMap JSON shims (JsonUtility) ────────────────────────────────

[Serializable] class OWM_Root       { public OWM_Weather[] weather; public OWM_Main main; public OWM_Wind wind; public OWM_Clouds clouds; public float visibility; public int dt; public OWM_Sys sys; }
[Serializable] class OWM_Weather    { public int id; public string main; public string description; public string icon; }
[Serializable] class OWM_Main       { public float temp; public float humidity; public float pressure; }
[Serializable] class OWM_Wind       { public float speed; public float deg; }
[Serializable] class OWM_Clouds     { public float all; }
[Serializable] class OWM_Sys        { public long sunrise; public long sunset; }

/// <summary>
/// Fetches real-world weather from OpenWeatherMap for the player's GPS location.
///
/// Setup:
///   1. Get a free API key from openweathermap.org
///   2. Paste it into the "Api Key" field in the Inspector
///   3. Attach this MonoBehaviour to the GameSystems object
///
/// Refresh rate: every refreshMinutes (default 10) minutes.
/// On API failure: retains last known condition; falls back to CLEAR on first call.
///
/// Events:
///   OnWeatherChanged(WeatherSnapshot)  — fired when condition changes
///   OnWeatherRefreshed(WeatherSnapshot) — fired on every successful fetch
/// </summary>
public class WeatherService : MonoBehaviour
{
    [Header("API")]
    [Tooltip("OpenWeatherMap API key (free tier works — openweathermap.org)")]
    [SerializeField] private string apiKey = "";
    [Tooltip("How often to fetch weather data (minutes)")]
    [SerializeField] private float refreshMinutes = 10f;

    [Header("Fallback GPS (used if no PlayerRig/CesiumGlobeAnchor)")]
    [SerializeField] private float fallbackLat =  1.3521f;  // Singapore
    [SerializeField] private float fallbackLon = 103.8198f;

    // ── Singleton ──────────────────────────────────────────────────
    public static WeatherService Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public static event Action<WeatherSnapshot> OnWeatherChanged;
    public static event Action<WeatherSnapshot> OnWeatherRefreshed;

    // ── Public state ──────────────────────────────────────────────
    public WeatherSnapshot Current { get; private set; } = new WeatherSnapshot
    {
        condition   = WeatherCondition.CLEAR,
        tempCelsius = 28f,
        humidity    = 75f,
        isDay       = true,
        visibilityM = 10000f,
    };

    public bool HasData { get; private set; }

    // ── Internal ──────────────────────────────────────────────────
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
        yield return null;  // wait for Cesium init
        var rig = GameObject.Find("PlayerRig");
        if (rig != null)
            _playerAnchor = rig.GetComponent<CesiumForUnity.CesiumGlobeAnchor>();

        StartCoroutine(RefreshLoop());
    }

    // ── Refresh loop ──────────────────────────────────────────────

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            ReadGPS();
            if (!string.IsNullOrEmpty(apiKey))
                yield return FetchWeather();
            else
                Debug.LogWarning("[WeatherService] No API key set — using fallback weather.");

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
        else
        {
            _lat = fallbackLat;
            _lon = fallbackLon;
        }
    }

    // ── API fetch ─────────────────────────────────────────────────

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

        var snapshot = ParseResponse(req.downloadHandler.text);
        if (snapshot == null) yield break;

        bool condChanged = snapshot.condition != Current.condition;
        Current  = snapshot;
        HasData  = true;

        OnWeatherRefreshed?.Invoke(snapshot);
        if (condChanged) OnWeatherChanged?.Invoke(snapshot);

        Debug.Log($"[WeatherService] {snapshot}");
    }

    // ── JSON parsing ──────────────────────────────────────────────

    private WeatherSnapshot ParseResponse(string json)
    {
        OWM_Root root;
        try   { root = JsonUtility.FromJson<OWM_Root>(json); }
        catch { Debug.LogWarning("[WeatherService] JSON parse error"); return null; }
        if (root?.weather == null || root.weather.Length == 0) return null;

        long now     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        bool isDay   = now >= root.sys.sunrise && now <= root.sys.sunset;

        return new WeatherSnapshot
        {
            condition   = MapCondition(root.weather[0].id),
            tempCelsius = root.main.temp,
            humidity    = root.main.humidity,
            windSpeedMs = root.wind.speed,
            windDeg     = root.wind.deg,
            visibilityM = root.visibility > 0 ? root.visibility : 10000f,
            cloudCoverage = root.clouds.all,
            description = root.weather[0].description,
            isDay       = isDay,
        };
    }

    private static WeatherCondition MapCondition(int id)
    {
        return id switch
        {
            >= 200 and <= 232 => WeatherCondition.THUNDERSTORM,
            >= 300 and <= 321 => WeatherCondition.DRIZZLE,
            500               => WeatherCondition.RAIN,
            501               => WeatherCondition.RAIN,
            >= 502 and <= 504 => WeatherCondition.HEAVY_RAIN,
            >= 520 and <= 531 => WeatherCondition.SHOWER,
            >= 600 and <= 622 => WeatherCondition.SNOW,
            701               => WeatherCondition.MIST,
            711               => WeatherCondition.SMOKE,
            721               => WeatherCondition.HAZE,
            731               => WeatherCondition.DUST,
            741               => WeatherCondition.FOG,
            751               => WeatherCondition.DUST,
            761               => WeatherCondition.DUST,
            762               => WeatherCondition.VOLCANIC_ASH,
            771               => WeatherCondition.SQUALL,
            781               => WeatherCondition.SQUALL,
            800               => WeatherCondition.CLEAR,
            801               => WeatherCondition.MOSTLY_CLEAR,
            802               => WeatherCondition.PARTLY_CLOUDY,
            >= 803            => WeatherCondition.OVERCAST,
            _                 => WeatherCondition.UNKNOWN,
        };
    }

    // ── Public helpers ────────────────────────────────────────────

    public bool IsRaining    => Current.condition is WeatherCondition.DRIZZLE
                                                   or WeatherCondition.RAIN
                                                   or WeatherCondition.HEAVY_RAIN
                                                   or WeatherCondition.SHOWER
                                                   or WeatherCondition.THUNDERSTORM;

    public bool IsHazardous  => Current.condition is WeatherCondition.HAZE
                                                   or WeatherCondition.SMOKE
                                                   or WeatherCondition.DUST
                                                   or WeatherCondition.VOLCANIC_ASH
                                                   or WeatherCondition.SQUALL;

    public bool IsReducedVis => Current.condition is WeatherCondition.FOG
                                                   or WeatherCondition.MIST
                                                   or WeatherCondition.HAZE
                                                   or WeatherCondition.SMOKE
                                                   or WeatherCondition.DUST
                                                   or WeatherCondition.VOLCANIC_ASH;

    /// <summary>Manually force a specific condition (useful for testing in editor).</summary>
    public void ForceCondition(WeatherCondition cond)
    {
        Current = new WeatherSnapshot
        {
            condition   = cond,
            tempCelsius = Current.tempCelsius,
            humidity    = Current.humidity,
            windSpeedMs = Current.windSpeedMs,
            visibilityM = Current.visibilityM,
            isDay       = Current.isDay,
            description = cond.ToString().ToLower().Replace('_', ' '),
        };
        OnWeatherChanged?.Invoke(Current);
        OnWeatherRefreshed?.Invoke(Current);
    }
}
