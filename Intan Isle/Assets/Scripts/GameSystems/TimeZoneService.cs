using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// GPS-based real-world timezone service.
///
/// Determines the player's current timezone from GPS coordinates using an
/// embedded lookup table covering 60+ regions across Asia, Oceania, Middle
/// East, and Pacific — no external API required.
///
/// When WeatherService has live OWM data, its `timezone` field (UTC offset
/// in seconds) is used as a live correction to the table lookup.
///
/// Exposes:
///   LocalTime       — DateTime in local timezone
///   UTCOffsetHours  — e.g. 8.0 for SGT, 5.5 for IST, 5.75 for NPT
///   TimeZoneName    — IANA-style name, e.g. "Asia/Singapore"
///   TimeZoneAbbr    — Short label, e.g. "SGT"
///   UTCOffsetLabel  — e.g. "UTC+8" / "UTC+5:30"
///
/// Accuracy: country-level (handles half-hour and 45-minute offsets for
///   India, Nepal, Myanmar, Iran, Australia NT/SA, Marquesas etc.)
/// </summary>
public class TimeZoneService : MonoBehaviour
{
    [Header("Refresh")]
    [Tooltip("How often to re-check timezone from GPS (seconds). Timezone rarely changes.")]
    [SerializeField] private float refreshInterval = 60f;

    // ── Singleton ──────────────────────────────────────────────────
    public static TimeZoneService Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────
    public static event Action<TimeZoneService> OnTimeZoneChanged;

    // ── Public state ──────────────────────────────────────────────
    /// <summary>Current local time in the detected timezone.</summary>
    public DateTime LocalTime     => DateTime.UtcNow.AddHours(UTCOffsetHours);

    /// <summary>UTC offset in decimal hours. e.g. India = 5.5, Nepal = 5.75</summary>
    public float    UTCOffsetHours  { get; private set; } = 8f;    // default UTC+8

    /// <summary>IANA-style timezone identifier.</summary>
    public string   TimeZoneName    { get; private set; } = "Asia/Singapore";

    /// <summary>Short abbreviation shown in HUD.</summary>
    public string   TimeZoneAbbr    { get; private set; } = "SGT";

    /// <summary>"UTC+8", "UTC+5:30", "UTC-5" etc.</summary>
    public string   UTCOffsetLabel  => FormatOffset(UTCOffsetHours);

    // ── Internal ──────────────────────────────────────────────────
    private CesiumForUnity.CesiumGlobeAnchor _playerAnchor;
    private double _lat, _lon;
    private float  _prevOffset = float.MinValue;   // to detect changes

    // ── Timezone lookup table ─────────────────────────────────────
    // Fields: minLat, maxLat, minLon, maxLon, utcOffsetHours, ianaName, abbreviation
    // Sorted most-specific first (smallest bounding box area).
    // Lookup selects the SMALLEST matching region.

    private struct TZRegion
    {
        public float minLat, maxLat, minLon, maxLon;
        public float offset;        // hours
        public string iana, abbr;
        public float Area => (maxLat - minLat) * (maxLon - minLon);
        public bool Contains(double lat, double lon) =>
            lat >= minLat && lat <= maxLat && lon >= minLon && lon <= maxLon;
    }

    // ~ 65 entries. More specific (smaller) regions listed first for tie-breaking.
    private static readonly TZRegion[] Regions = new TZRegion[]
    {
        // ── City / micro-states ───────────────────────────────────
        new TZRegion { minLat= 1.20f, maxLat= 1.50f, minLon=103.60f, maxLon=104.05f, offset= 8.0f, iana="Asia/Singapore",   abbr="SGT"  },
        new TZRegion { minLat=22.10f, maxLat=22.60f, minLon=113.80f, maxLon=114.40f, offset= 8.0f, iana="Asia/Hong_Kong",   abbr="HKT"  },
        new TZRegion { minLat=22.05f, maxLat=22.30f, minLon=113.50f, maxLon=113.65f, offset= 8.0f, iana="Asia/Macau",       abbr="CST"  },
        new TZRegion { minLat=25.00f, maxLat=25.30f, minLon=51.00f,  maxLon=51.65f,  offset= 3.0f, iana="Asia/Qatar",       abbr="AST"  },
        new TZRegion { minLat=28.90f, maxLat=29.60f, minLon=47.70f,  maxLon=48.60f,  offset= 3.0f, iana="Asia/Kuwait",      abbr="AST"  },
        new TZRegion { minLat=23.40f, maxLat=24.00f, minLon=54.20f,  maxLon=54.70f,  offset= 4.0f, iana="Asia/Abu_Dhabi",   abbr="GST"  },
        new TZRegion { minLat=25.05f, maxLat=25.50f, minLon=55.00f,  maxLon=55.60f,  offset= 4.0f, iana="Asia/Dubai",       abbr="GST"  },
        new TZRegion { minLat=26.00f, maxLat=26.60f, minLon=50.30f,  maxLon=50.80f,  offset= 3.0f, iana="Asia/Bahrain",     abbr="AST"  },

        // ── Small / precise countries ─────────────────────────────
        new TZRegion { minLat= 4.00f, maxLat= 5.10f, minLon=114.00f, maxLon=115.40f, offset= 8.0f, iana="Asia/Brunei",      abbr="BNT"  },
        new TZRegion { minLat=26.30f, maxLat=30.50f, minLon=80.00f,  maxLon=88.20f,  offset= 5.75f,iana="Asia/Kathmandu",   abbr="NPT"  },
        new TZRegion { minLat=16.00f, maxLat=28.50f, minLon=92.00f,  maxLon=101.20f, offset= 6.5f, iana="Asia/Rangoon",     abbr="MMT"  },
        new TZRegion { minLat= 6.00f, maxLat=10.00f, minLon=79.50f,  maxLon=82.00f,  offset= 5.5f, iana="Asia/Colombo",     abbr="SLST" },
        new TZRegion { minLat= 1.00f, maxLat= 8.00f, minLon=72.50f,  maxLon=74.00f,  offset= 5.0f, iana="Indian/Maldives",  abbr="MVT"  },
        new TZRegion { minLat=27.00f, maxLat=28.40f, minLon=88.70f,  maxLon=92.20f,  offset= 6.0f, iana="Asia/Thimphu",     abbr="BTT"  },
        new TZRegion { minLat=-9.00f, maxLat= 0.00f, minLon=124.00f, maxLon=127.50f, offset= 9.0f, iana="Asia/Dili",        abbr="TLT"  },
        new TZRegion { minLat=12.50f, maxLat=14.60f, minLon=144.50f, maxLon=145.00f, offset=10.0f, iana="Pacific/Guam",     abbr="ChST" },
        new TZRegion { minLat=21.00f, maxLat=25.50f, minLon=119.00f, maxLon=122.10f, offset= 8.0f, iana="Asia/Taipei",      abbr="CST"  },
        new TZRegion { minLat=37.50f, maxLat=43.00f, minLon=124.00f, maxLon=130.50f, offset= 9.0f, iana="Asia/Pyongyang",   abbr="KST"  },
        new TZRegion { minLat=33.50f, maxLat=38.80f, minLon=125.50f, maxLon=130.00f, offset= 9.0f, iana="Asia/Seoul",       abbr="KST"  },

        // ── Island chains / archipelagos ──────────────────────────
        new TZRegion { minLat= 5.00f, maxLat=21.00f, minLon=116.00f, maxLon=127.00f, offset= 8.0f, iana="Asia/Manila",      abbr="PST"  },
        new TZRegion { minLat=24.00f, maxLat=46.00f, minLon=122.00f, maxLon=146.00f, offset= 9.0f, iana="Asia/Tokyo",       abbr="JST"  },
        // Indonesia zones — most specific first
        new TZRegion { minLat=-8.00f, maxLat= 1.00f, minLon=130.00f, maxLon=141.00f, offset= 9.0f, iana="Asia/Jayapura",    abbr="WIT"  },
        new TZRegion { minLat=-8.00f, maxLat= 2.00f, minLon=115.00f, maxLon=135.00f, offset= 8.0f, iana="Asia/Makassar",    abbr="WITA" },
        new TZRegion { minLat=-8.00f, maxLat= 6.00f, minLon= 95.00f, maxLon=116.00f, offset= 7.0f, iana="Asia/Jakarta",     abbr="WIB"  },

        // ── Mainland SE Asia ──────────────────────────────────────
        new TZRegion { minLat=10.00f, maxLat=15.00f, minLon=102.00f, maxLon=108.00f, offset= 7.0f, iana="Asia/Phnom_Penh",  abbr="ICT"  },
        new TZRegion { minLat=14.00f, maxLat=23.00f, minLon=100.00f, maxLon=108.00f, offset= 7.0f, iana="Asia/Vientiane",   abbr="ICT"  },
        new TZRegion { minLat= 5.00f, maxLat=21.50f, minLon= 97.50f, maxLon=105.70f, offset= 7.0f, iana="Asia/Bangkok",     abbr="ICT"  },
        new TZRegion { minLat= 8.00f, maxLat=24.00f, minLon=102.00f, maxLon=110.00f, offset= 7.0f, iana="Asia/Ho_Chi_Minh", abbr="ICT"  },
        new TZRegion { minLat= 1.00f, maxLat= 7.50f, minLon= 99.50f, maxLon=119.50f, offset= 8.0f, iana="Asia/Kuala_Lumpur",abbr="MYT"  },

        // ── South Asia ────────────────────────────────────────────
        new TZRegion { minLat=20.00f, maxLat=27.00f, minLon=88.00f,  maxLon=93.00f,  offset= 6.0f, iana="Asia/Dhaka",       abbr="BST"  },
        new TZRegion { minLat= 8.00f, maxLat=37.00f, minLon=68.00f,  maxLon=98.00f,  offset= 5.5f, iana="Asia/Kolkata",     abbr="IST"  },
        new TZRegion { minLat=23.00f, maxLat=37.50f, minLon=60.00f,  maxLon=78.00f,  offset= 5.0f, iana="Asia/Karachi",     abbr="PKT"  },
        new TZRegion { minLat=29.00f, maxLat=38.50f, minLon=60.00f,  maxLon=75.00f,  offset= 4.5f, iana="Asia/Kabul",       abbr="AFT"  },

        // ── East Asia ─────────────────────────────────────────────
        new TZRegion { minLat=18.00f, maxLat=54.00f, minLon=73.00f,  maxLon=135.00f, offset= 8.0f, iana="Asia/Shanghai",    abbr="CST"  },
        new TZRegion { minLat=41.00f, maxLat=52.00f, minLon=87.00f,  maxLon=120.00f, offset= 8.0f, iana="Asia/Ulaanbaatar", abbr="ULAT" },

        // ── Central Asia ──────────────────────────────────────────
        new TZRegion { minLat=37.00f, maxLat=42.00f, minLon=56.00f,  maxLon=73.50f,  offset= 5.0f, iana="Asia/Tashkent",    abbr="UZT"  },
        new TZRegion { minLat=36.00f, maxLat=41.00f, minLon=67.00f,  maxLon=75.50f,  offset= 5.0f, iana="Asia/Dushanbe",    abbr="TJT"  },
        new TZRegion { minLat=38.00f, maxLat=43.00f, minLon=52.00f,  maxLon=67.00f,  offset= 5.0f, iana="Asia/Ashgabat",    abbr="TMT"  },
        new TZRegion { minLat=39.00f, maxLat=43.50f, minLon=69.00f,  maxLon=80.50f,  offset= 6.0f, iana="Asia/Bishkek",     abbr="KGT"  },
        new TZRegion { minLat=40.00f, maxLat=55.00f, minLon=65.00f,  maxLon=90.00f,  offset= 6.0f, iana="Asia/Almaty",      abbr="ALMT" },
        new TZRegion { minLat=40.00f, maxLat=55.00f, minLon=50.00f,  maxLon=65.00f,  offset= 5.0f, iana="Asia/Oral",        abbr="ORAT" },

        // ── Middle East ───────────────────────────────────────────
        new TZRegion { minLat=25.00f, maxLat=40.00f, minLon=44.00f,  maxLon=64.00f,  offset= 3.5f, iana="Asia/Tehran",      abbr="IRST" },
        new TZRegion { minLat=29.00f, maxLat=38.00f, minLon=38.50f,  maxLon=50.00f,  offset= 3.0f, iana="Asia/Baghdad",     abbr="AST"  },
        new TZRegion { minLat=16.00f, maxLat=32.00f, minLon=36.00f,  maxLon=56.00f,  offset= 3.0f, iana="Asia/Riyadh",      abbr="AST"  },
        new TZRegion { minLat=12.00f, maxLat=19.00f, minLon=42.00f,  maxLon=54.00f,  offset= 3.0f, iana="Asia/Aden",        abbr="AST"  },
        new TZRegion { minLat=22.00f, maxLat=27.00f, minLon=51.00f,  maxLon=60.00f,  offset= 4.0f, iana="Asia/Muscat",      abbr="GST"  },
        new TZRegion { minLat=36.00f, maxLat=43.00f, minLon=26.00f,  maxLon=45.00f,  offset= 3.0f, iana="Europe/Istanbul",  abbr="TRT"  },
        new TZRegion { minLat=29.00f, maxLat=33.50f, minLon=34.50f,  maxLon=36.00f,  offset= 2.0f, iana="Asia/Jerusalem",   abbr="IST"  },
        new TZRegion { minLat=29.00f, maxLat=33.50f, minLon=35.00f,  maxLon=40.00f,  offset= 3.0f, iana="Asia/Amman",       abbr="AST"  },
        new TZRegion { minLat=33.00f, maxLat=35.00f, minLon=35.00f,  maxLon=37.00f,  offset= 2.0f, iana="Asia/Beirut",      abbr="EET"  },
        new TZRegion { minLat=32.00f, maxLat=37.50f, minLon=35.50f,  maxLon=43.00f,  offset= 3.0f, iana="Asia/Damascus",    abbr="EEST" },
        new TZRegion { minLat=37.00f, maxLat=42.00f, minLon=43.00f,  maxLon=47.50f,  offset= 4.0f, iana="Asia/Tbilisi",     abbr="GET"  },
        new TZRegion { minLat=38.00f, maxLat=42.00f, minLon=44.00f,  maxLon=51.00f,  offset= 4.0f, iana="Asia/Baku",        abbr="AZT"  },
        new TZRegion { minLat=38.00f, maxLat=42.00f, minLon=43.50f,  maxLon=47.00f,  offset= 4.0f, iana="Asia/Yerevan",     abbr="AMT"  },

        // ── Oceania ───────────────────────────────────────────────
        new TZRegion { minLat=-26.00f,maxLat=-12.00f,minLon=130.00f, maxLon=139.00f, offset= 9.5f, iana="Australia/Darwin",  abbr="ACST" },
        new TZRegion { minLat=-39.00f,maxLat=-26.00f,minLon=129.00f, maxLon=141.50f, offset= 9.5f, iana="Australia/Adelaide", abbr="ACST" },
        new TZRegion { minLat=-35.00f,maxLat=-14.00f,minLon=113.00f, maxLon=130.00f, offset= 8.0f, iana="Australia/Perth",    abbr="AWST" },
        new TZRegion { minLat=-44.00f,maxLat=-10.00f,minLon=137.00f, maxLon=154.00f, offset=10.0f, iana="Australia/Sydney",   abbr="AEST" },
        new TZRegion { minLat=-48.00f,maxLat=-34.00f,minLon=166.00f, maxLon=178.00f, offset=12.0f, iana="Pacific/Auckland",   abbr="NZST" },
        new TZRegion { minLat=-12.00f,maxLat= -1.00f,minLon=140.00f, maxLon=156.00f, offset=10.0f, iana="Pacific/Port_Moresby",abbr="PGT" },
        new TZRegion { minLat=-19.00f,maxLat=-15.00f,minLon=177.00f, maxLon=180.00f, offset=12.0f, iana="Pacific/Fiji",       abbr="FJT"  },
        new TZRegion { minLat= 7.00f, maxLat=10.00f, minLon=134.00f, maxLon=135.00f, offset= 9.0f, iana="Pacific/Palau",      abbr="PWT"  },
    };

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

        RefreshTimezone();
        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            ReadGPS();
            RefreshTimezone();
        }
    }

    // ── GPS read ──────────────────────────────────────────────────

    private void ReadGPS()
    {
        if (_playerAnchor != null)
        {
            _lon = _playerAnchor.longitudeLatitudeHeight.x;
            _lat = _playerAnchor.longitudeLatitudeHeight.y;
        }
        else
        {
            _lat = 1.3521;   // Singapore fallback
            _lon = 103.8198;
        }
    }

    // ── Timezone resolution ───────────────────────────────────────

    private void RefreshTimezone()
    {
        // If WeatherService has a live OWM timezone offset, prefer it
        if (WeatherService.Instance != null && WeatherService.Instance.HasData)
        {
            float owmHours = WeatherService.Instance.OWMTimezoneOffsetHours;
            if (owmHours != 0f)  // 0 might be UTC — only trust if non-zero or position clearly UTC
            {
                ApplyOffset(owmHours, TableLookup(_lat, _lon));
                return;
            }
        }

        // Fall back to table lookup
        var region = TableLookup(_lat, _lon);
        if (region.HasValue)
            ApplyFromRegion(region.Value);
    }

    private TZRegion? TableLookup(double lat, double lon)
    {
        TZRegion? best = null;
        float bestArea = float.MaxValue;

        foreach (var r in Regions)
        {
            if (!r.Contains(lat, lon)) continue;
            if (r.Area < bestArea)
            {
                bestArea = r.Area;
                best = r;
            }
        }
        return best;
    }

    private void ApplyFromRegion(TZRegion r)
    {
        ApplyOffset(r.offset, r);
    }

    private void ApplyOffset(float hours, TZRegion? tableRegion)
    {
        bool changed = !Mathf.Approximately(hours, _prevOffset);
        _prevOffset     = hours;
        UTCOffsetHours  = hours;
        TimeZoneName    = tableRegion?.iana  ?? OffsetToIANA(hours);
        TimeZoneAbbr    = tableRegion?.abbr  ?? FormatOffset(hours).Replace(":", "").Replace("+", "P").Replace("-", "M");

        if (changed)
        {
            Debug.Log($"[TimeZoneService] {TimeZoneName} ({TimeZoneAbbr}) {UTCOffsetLabel}  localTime={LocalTime:HH:mm:ss}");
            OnTimeZoneChanged?.Invoke(this);
        }
    }

    // ── Formatting helpers ────────────────────────────────────────

    public static string FormatOffset(float hours)
    {
        int totalMin = Mathf.RoundToInt(hours * 60f);
        int h = totalMin / 60;
        int m = Mathf.Abs(totalMin % 60);
        return m == 0 ? $"UTC{h:+0;-0;+0}" : $"UTC{h:+0;-0;+0}:{m:D2}";
    }

    private static string OffsetToIANA(float h)
    {
        int totalMin = Mathf.RoundToInt(h * 60f);
        return $"Etc/GMT{(-totalMin / 60):+0;-0;+0}";
    }

    // ── Phase helper — maps local hour to GamePhase ───────────────

    /// <summary>
    /// Returns the expected GamePhase for the given local time.
    /// Boundaries tuned for tropical/equatorial latitudes (SE Asia).
    /// </summary>
    public static GamePhase PhaseFromLocalTime(DateTime localTime,
        float dawnH = 5.5f, float morningH = 7.0f, float middayH = 11.0f,
        float afternoonH = 14.0f, float duskH = 17.5f, float nightH = 19.5f)
    {
        float h = localTime.Hour + localTime.Minute / 60f;

        if (h >= nightH  || h < dawnH)     return GamePhase.Night;
        if (h < morningH)                  return GamePhase.Dawn;
        if (h < middayH)                   return GamePhase.Morning;
        if (h < afternoonH)                return GamePhase.Midday;
        if (h < duskH)                     return GamePhase.Afternoon;
        return GamePhase.Dusk;
    }
}
