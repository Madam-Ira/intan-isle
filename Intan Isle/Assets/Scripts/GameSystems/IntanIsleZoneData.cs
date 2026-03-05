using System;
using System.Collections.Generic;
using UnityEngine;

// ════════════════════════════════════════════════════════════════
// INTAN ISLE — ZONE TYPE ENUM
// Drives shader coloring, vegetation density, spirit behaviour,
// day/night tone, and haze particle systems.
// ════════════════════════════════════════════════════════════════

public enum ZoneType
{
    FOREST            = 0,
    ANCIENT_FOREST    = 1,
    PROTECTED_FOREST  = 2,
    WATERWAY          = 3,
    MANGROVE          = 4,
    KAMPUNG_HERITAGE  = 5,
    FOOD_SECURITY     = 6,
    DEFORESTATION     = 7,
    CORAL_DEGRADATION = 8,
    POLLUTION         = 9,
    TRANSBOUNDARY_HAZE= 10,
    RIVER_POLLUTION   = 11,
    SACRED_FOREST     = 12,
    TOXIC             = 13,
}

public static class ZoneTypeExtensions
{
    public static bool IsHealthy(this ZoneType z)
        => z == ZoneType.FOREST || z == ZoneType.ANCIENT_FOREST
        || z == ZoneType.PROTECTED_FOREST || z == ZoneType.SACRED_FOREST
        || z == ZoneType.MANGROVE || z == ZoneType.KAMPUNG_HERITAGE
        || z == ZoneType.FOOD_SECURITY || z == ZoneType.WATERWAY;

    public static bool IsToxic(this ZoneType z)
        => z == ZoneType.POLLUTION || z == ZoneType.TOXIC
        || z == ZoneType.TRANSBOUNDARY_HAZE || z == ZoneType.RIVER_POLLUTION
        || z == ZoneType.CORAL_DEGRADATION || z == ZoneType.DEFORESTATION;

    public static string DisplayName(this ZoneType z) => z switch
    {
        ZoneType.FOREST             => "Forest",
        ZoneType.ANCIENT_FOREST     => "Ancient Forest",
        ZoneType.PROTECTED_FOREST   => "Protected Forest",
        ZoneType.WATERWAY           => "Waterway",
        ZoneType.MANGROVE           => "Mangrove",
        ZoneType.KAMPUNG_HERITAGE   => "Kampung Heritage",
        ZoneType.FOOD_SECURITY      => "Food Security Zone",
        ZoneType.DEFORESTATION      => "Deforestation Zone",
        ZoneType.CORAL_DEGRADATION  => "Coral Degradation Zone",
        ZoneType.POLLUTION          => "Pollution Zone",
        ZoneType.TRANSBOUNDARY_HAZE => "Transboundary Haze Corridor",
        ZoneType.RIVER_POLLUTION    => "River Pollution Zone",
        ZoneType.SACRED_FOREST      => "Sacred Forest",
        ZoneType.TOXIC              => "Toxic Zone",
        _                           => "Unknown Zone",
    };
}

// ════════════════════════════════════════════════════════════════
// ZONE ENTRY — One ecological zone on the globe
// ════════════════════════════════════════════════════════════════

[Serializable]
public class ZoneEntry
{
    [Header("Identity")]
    public string   zoneName;
    public ZoneType zoneType;

    [Header("Geography")]
    public double latitude;
    public double longitude;
    [Tooltip("Zone influence radius in km")]
    public float  radiusKm = 5f;

    [Header("Narrative")]
    [TextArea(2, 4)]
    public string description;

    // Runtime-computed world-space XZ offset from Singapore origin
    // Populated by ZoneShaderLinker.ComputeWorldPositions()
    [NonSerialized] public Vector2 worldXZ;
    [NonSerialized] public bool    worldPosReady;
}

// ════════════════════════════════════════════════════════════════
// INTAN ISLE ZONE DATA — ScriptableObject
// Create via: Assets > Create > Intan Isle > Zone Data
// Or call IntanIsleZoneData.Default() for hardcoded baseline.
// ════════════════════════════════════════════════════════════════

[CreateAssetMenu(fileName = "IntanIsleZones", menuName = "Intan Isle/Zone Data")]
public class IntanIsleZoneData : ScriptableObject
{
    [SerializeField] public ZoneEntry[] zones = BuildDefaultZones();

    // ── Singleton for runtime access ──────────────────────────────
    private static IntanIsleZoneData _instance;

    public static IntanIsleZoneData Get()
    {
        if (_instance != null) return _instance;
        _instance = Resources.Load<IntanIsleZoneData>("Zones/IntanIsleZones");
        if (_instance == null)
        {
            _instance = CreateInstance<IntanIsleZoneData>();
            _instance.zones = BuildDefaultZones();
            Debug.Log("[IntanIsleZoneData] No asset found in Resources/Zones/. Using hardcoded baseline.");
        }
        return _instance;
    }

    // ── Query: dominant zone at lat/lon ──────────────────────────
    public ZoneEntry GetDominantZone(double lat, double lon)
    {
        ZoneEntry best      = null;
        double    bestScore = double.MaxValue;

        foreach (var z in zones)
        {
            double dLat   = lat - z.latitude;
            double dLon   = lon - z.longitude;
            double distKm = Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;

            if (distKm < z.radiusKm)
            {
                double score = distKm / z.radiusKm;
                if (score < bestScore) { bestScore = score; best = z; }
            }
        }
        return best; // null → caller treats as FOREST
    }

    // ── Query: blend factor (0=fully inside, 1=at edge) ──────────
    public float GetBlendFactor(double lat, double lon, ZoneEntry zone)
    {
        if (zone == null) return 1f;
        double dLat   = lat - zone.latitude;
        double dLon   = lon - zone.longitude;
        double distKm = Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
        return Mathf.Clamp01((float)(distKm / zone.radiusKm));
    }

    // ════════════════════════════════════════════════════════════════
    // HARDCODED BASELINE — 21 zones across Singapore, Malaysia, Asia
    // ════════════════════════════════════════════════════════════════

    static ZoneEntry[] BuildDefaultZones() => new ZoneEntry[]
    {
        // ── Singapore & Malaysia ────────────────────────────────────
        new ZoneEntry
        {
            zoneName    = "Jurong Island",
            latitude    =  1.265, longitude = 103.670,
            zoneType    = ZoneType.POLLUTION,
            radiusKm    = 5f,
            description = "Industrial petrochemical hub — heavy particulate + NOx emissions",
        },
        new ZoneEntry
        {
            zoneName    = "Tuas",
            latitude    =  1.296, longitude = 103.621,
            zoneType    = ZoneType.POLLUTION,
            radiusKm    = 5f,
            description = "Waste treatment and incineration zone",
        },
        new ZoneEntry
        {
            zoneName    = "Southern Islands",
            latitude    =  1.210, longitude = 103.840,
            zoneType    = ZoneType.CORAL_DEGRADATION,
            radiusKm    = 8f,
            description = "Coral bleaching from shipping traffic and thermal stress",
        },
        new ZoneEntry
        {
            zoneName    = "Sungei Kadut",
            latitude    =  1.410, longitude = 103.750,
            zoneType    = ZoneType.RIVER_POLLUTION,
            radiusKm    = 5f,
            description = "Industrial river corridor — chemical discharge",
        },
        new ZoneEntry
        {
            zoneName    = "Sungai Kim Kim",
            latitude    =  1.470, longitude = 103.890,
            zoneType    = ZoneType.TOXIC,
            radiusKm    = 4f,
            description = "Illegal toxic waste discharge — severe acute contamination",
        },
        new ZoneEntry
        {
            zoneName    = "Cameron Highlands",
            latitude    =  4.470, longitude = 101.380,
            zoneType    = ZoneType.ANCIENT_FOREST,
            radiusKm    = 50f,
            description = "Ancient cloud forest ecosystem — mossy highland dipterocarp",
        },
        new ZoneEntry
        {
            zoneName    = "Riau Corridor",
            latitude    =  1.350, longitude = 103.700,
            zoneType    = ZoneType.TRANSBOUNDARY_HAZE,
            radiusKm    = 10f,
            description = "Transboundary haze entry from Sumatra peatland fires",
        },
        new ZoneEntry
        {
            zoneName    = "Belum-Temengor",
            latitude    =  5.470, longitude = 101.330,
            zoneType    = ZoneType.ANCIENT_FOREST,
            radiusKm    = 40f,
            description = "One of Earth's oldest rainforests — 130 million years old",
        },
        new ZoneEntry
        {
            zoneName    = "Johor River",
            latitude    =  1.730, longitude = 103.900,
            zoneType    = ZoneType.WATERWAY,
            radiusKm    = 8f,
            description = "Primary freshwater lifeline linking Johor to Singapore",
        },
        new ZoneEntry
        {
            zoneName    = "Lim Chu Kang",
            latitude    =  1.430, longitude = 103.710,
            zoneType    = ZoneType.FOOD_SECURITY,
            radiusKm    = 6f,
            description = "Singapore's last working farm belt — urban food sovereignty",
        },
        new ZoneEntry
        {
            zoneName    = "Bukit Timah",
            latitude    =  1.350, longitude = 103.780,
            zoneType    = ZoneType.PROTECTED_FOREST,
            radiusKm    = 3f,
            description = "Primary rainforest reserve — Singapore's living ecological memory",
        },
        new ZoneEntry
        {
            zoneName    = "Kampong Buangkok",
            latitude    =  1.383, longitude = 103.878,
            zoneType    = ZoneType.KAMPUNG_HERITAGE,
            radiusKm    = 1f,
            description = "Last surviving kampung in Singapore — multigenerational living",
        },

        // ── Wider Asia ──────────────────────────────────────────────
        new ZoneEntry
        {
            zoneName    = "Borneo Rainforest",
            latitude    =  1.000, longitude = 114.000,
            zoneType    = ZoneType.ANCIENT_FOREST,
            radiusKm    = 200f,
            description = "Heart of Borneo — apex of ancient biodiversity on Earth",
        },
        new ZoneEntry
        {
            zoneName    = "Mekong Delta",
            latitude    = 10.000, longitude = 105.800,
            zoneType    = ZoneType.WATERWAY,
            radiusKm    = 80f,
            description = "Southeast Asia's great river delta — life-source for millions",
        },
        new ZoneEntry
        {
            zoneName    = "Sundarbans",
            latitude    = 21.900, longitude =  89.200,
            zoneType    = ZoneType.MANGROVE,
            radiusKm    = 60f,
            description = "World's largest mangrove forest — guardian of the delta",
        },
        new ZoneEntry
        {
            zoneName    = "Himalayan Foothills",
            latitude    = 27.700, longitude =  85.300,
            zoneType    = ZoneType.SACRED_FOREST,
            radiusKm    = 100f,
            description = "Sacred groves and ancient forest traditions — living ecological memory",
        },
        new ZoneEntry
        {
            zoneName    = "Jakarta Bay",
            latitude    = -6.100, longitude = 106.800,
            zoneType    = ZoneType.POLLUTION,
            radiusKm    = 30f,
            description = "Severe coastal pollution — plastics accumulation and industrial discharge",
        },
        new ZoneEntry
        {
            zoneName    = "Citarum River",
            latitude    = -6.900, longitude = 107.600,
            zoneType    = ZoneType.TOXIC,
            radiusKm    = 20f,
            description = "One of Earth's most polluted rivers — textile and industrial waste",
        },
        new ZoneEntry
        {
            zoneName    = "Mekong Dams",
            latitude    = 15.000, longitude = 105.000,
            zoneType    = ZoneType.RIVER_POLLUTION,
            radiusKm    = 40f,
            description = "Hydropower dam cascade disrupting fish migration and sediment flow",
        },
        new ZoneEntry
        {
            zoneName    = "Yangtze Basin",
            latitude    = 30.000, longitude = 111.000,
            zoneType    = ZoneType.WATERWAY,
            radiusKm    = 120f,
            description = "Great river of China — ecological memory of a continental watershed",
        },
        new ZoneEntry
        {
            zoneName    = "Phi Phi Islands",
            latitude    =  7.740, longitude =  98.770,
            zoneType    = ZoneType.CORAL_DEGRADATION,
            radiusKm    = 15f,
            description = "Mass tourism coral bleaching — marine ecosystem under terminal stress",
        },
    };
}
