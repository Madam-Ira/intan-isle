using System;
using System.Collections.Generic;
using UnityEngine;

// ════════════════════════════════════════════════════════════════
// FLORA ENUMS
// ════════════════════════════════════════════════════════════════

public enum FloraCategory
{
    FLOWER,
    PLANT,
    TREE,
    FERN,
    MOSS,
    FUNGUS,
    SEAGRASS,
    MANGROVE,
    VINE,
    GRASS,
    ALGAE,
    LICHEN,
}

/// <summary>
/// Gameplay grouping used for encounter tables, UI filters, and
/// reward tiers.  Expand as new biodiversity batches arrive.
/// </summary>
public enum FloraGameCategory
{
    CARNIVOROUS_SPECIALIST,   // Rafflesia, Pitcher plants, Sundew, Corpse Flower
    CANOPY_GIANT,             // Dipterocarp, Tualang, Kapur
    MANGROVE_PIONEER,         // Avicennia, Rhizophora, Sonneratia
    HIGHLAND_ENDEMIC,         // Alpine meadow & cloud-forest specialists
    AQUATIC_FLOATING,         // Lotus, water hyacinth, Victoria amazonica
    MEDICINAL_CULTURAL,       // Turmeric, Morinda, Andrographis
    KEYSTONE_PRODUCER,        // Durian, Rambutan, Fig trees
    FUNGAL_NETWORK,           // Mycorrhizal, bracket fungi, bioluminescent
    COASTAL_PIONEER,          // Beach Morning Glory, Casuarina, Screw Pine
    ALPINE_SURVIVOR,          // Edelweiss, Gentian, Siberian bog plants
    RIPARIAN_EDGE,            // Bank stabilisers, reed beds, riverside palms
    STRANGLER_CLIMBER,        // Ficus strangler, rattan, climbing aroids
}

public enum ConservationStatus
{
    EXTINCT,
    CRITICALLY_ENDANGERED,
    ENDANGERED,
    VULNERABLE,
    NEAR_THREATENED,
    LEAST_CONCERN,
    DATA_DEFICIENT,
}

public enum FloraHabitat
{
    DEEP_LOWLAND_FOREST,
    HIGHLAND_PEAT,
    SANDY_COASTAL,
    BOG_SPHAGNUM,
    MANGROVE_COAST,
    MONTANE_CLOUD_FOREST,
    FRESHWATER_WETLAND,
    CORAL_ADJACENT,
    DRY_DECIDUOUS_FOREST,
    ALPINE_MEADOW,
    SECONDARY_GROWTH,
    RIPARIAN,
    ULTRAMAFIC_SOIL,       // special chemically-extreme soils (Sabah)
    LIMESTONE_KARST,       // karst forest
    PEAT_SWAMP,
    TIDAL_FLAT,
}

public enum FloraRegion
{
    SEA,          // Southeast Asia
    EAST_ASIA,    // China, Japan, Korea, Taiwan
    SOUTH_ASIA,   // India, Sri Lanka, Nepal, Bangladesh
    CENTRAL_ASIA, // Kazakhstan, Uzbekistan, Mongolia steppe
    NORTH_ASIA,   // Russia / Siberia / Far East
    MIDDLE_EAST,
    OCEANIA,      // Australia, PNG, Pacific islands
}

/// <summary>
/// Controls how the flora entry is experienced in the Veiled World.
/// Drives which shader/particle mode the FloraEncounterManager activates.
/// </summary>
public enum FloraInteractionMode
{
    FULL_IMMERSION,    // Full Veiled World particle bloom + bioluminescent glow
    LIVING_WATER,      // Experienced near active water bodies (WaterBodyManager)
    CANOPY_HOVER,      // Aerial observation from flight (BunianFlightController)
    GROUND_LEVEL,      // Walking-speed encounter — no flight required
    CAVE_INTERIOR,     // Found inside cave systems (CaveManager)
    TIDAL_ZONE,        // Exposed at low tide (TideService.CurrentHeightM check)
}

/// <summary>
/// Animation type required from the asset pipeline.
/// </summary>
public enum FloraAssetAnimation
{
    STATIC,
    ANIMATED_BLOOM,           // slow petals unfurling
    ANIMATED_TENTACLE_CURL,   // Sundew / Venus flytrap snap
    ANIMATED_LID_OPEN_CLOSE,  // Pitcher plant lid
    ANIMATED_HEAT_SHIMMER,    // Corpse flower thermogenesis glow
    ANIMATED_SPORE_BURST,     // Fungi / fern spore release
    ANIMATED_SWAY,            // Seagrass / reeds in water current
    ANIMATED_ROOT_GROW,       // Mangrove prop-root expansion
    ANIMATED_CANOPY_RUSTLE,   // Large tree crown
}

// ════════════════════════════════════════════════════════════════
// FLORA ENTRY
// ════════════════════════════════════════════════════════════════

[Serializable]
public class FloraEntry
{
    [Header("Identity")]
    public int    id;
    public string name;
    public string scientific;
    public FloraCategory     category;
    public string            subCategory;
    public FloraGameCategory gameCategory;

    [Header("Geography")]
    public FloraRegion region;
    public string      countries;           // human-readable range string
    public FloraHabitat habitat;
    /// <summary>Primary GPS centre for proximity encounter checks.</summary>
    public double latitude;
    public double longitude;
    /// <summary>Approximate encounter radius in km (extent of wild range).</summary>
    public float  spawnRadiusKm = 500f;

    [Header("Ecology")]
    public ConservationStatus conservation;
    [TextArea(1, 3)]
    public string ecologicalRole;
    [TextArea(1, 3)]
    public string significance;
    /// <summary>"None", "TOXIC", or short edible description.</summary>
    public string edible;

    [Header("Asset")]
    public FloraAssetAnimation assetAnimation;
    public string size;
    /// <summary>Describes which part of the mesh/material receives the TeamLab glow effect.</summary>
    public string glowLocation;
    /// <summary>Describes the TeamLab visual mood cue for shader/VFX artists.</summary>
    public string teamlabMood;
    public FloraInteractionMode interactionMode;

    [Header("Barakah")]
    /// <summary>
    /// One-time Barakah bonus awarded on first discovery (scaled by conservation tier).
    /// 0 = use default tier scaling.
    /// </summary>
    public float discoveryBarakahBonus = 0f;
}

// ════════════════════════════════════════════════════════════════
// CONSERVATION EXTENSIONS
// ════════════════════════════════════════════════════════════════

public static class ConservationStatusExtensions
{
    /// <summary>One-time Barakah discovery bonus by threat tier.</summary>
    public static float DiscoveryBarakah(this ConservationStatus s) => s switch
    {
        ConservationStatus.EXTINCT                => 0f,    // can't be discovered
        ConservationStatus.CRITICALLY_ENDANGERED  => 8f,
        ConservationStatus.ENDANGERED             => 5f,
        ConservationStatus.VULNERABLE             => 3f,
        ConservationStatus.NEAR_THREATENED        => 2f,
        ConservationStatus.LEAST_CONCERN          => 1f,
        ConservationStatus.DATA_DEFICIENT         => 1.5f,
        _                                         => 1f,
    };

    /// <summary>Passive Barakah while player is within encounter radius (per 0.5 s tick).</summary>
    public static float ProximityBarakahRate(this ConservationStatus s) => s switch
    {
        ConservationStatus.CRITICALLY_ENDANGERED  =>  1.5f,
        ConservationStatus.ENDANGERED             =>  1.0f,
        ConservationStatus.VULNERABLE             =>  0.6f,
        ConservationStatus.NEAR_THREATENED        =>  0.3f,
        ConservationStatus.LEAST_CONCERN          =>  0.1f,
        ConservationStatus.DATA_DEFICIENT         =>  0.4f,
        _                                         =>  0f,
    };

    public static string DisplayName(this ConservationStatus s) => s switch
    {
        ConservationStatus.EXTINCT                => "Extinct",
        ConservationStatus.CRITICALLY_ENDANGERED  => "Critically Endangered",
        ConservationStatus.ENDANGERED             => "Endangered",
        ConservationStatus.VULNERABLE             => "Vulnerable",
        ConservationStatus.NEAR_THREATENED        => "Near Threatened",
        ConservationStatus.LEAST_CONCERN          => "Least Concern",
        ConservationStatus.DATA_DEFICIENT         => "Data Deficient",
        _                                         => "Unknown",
    };

    /// <summary>HUD colour for conservation badge.</summary>
    public static Color BadgeColor(this ConservationStatus s) => s switch
    {
        ConservationStatus.CRITICALLY_ENDANGERED  => new Color(0.90f, 0.15f, 0.10f, 1f),  // red
        ConservationStatus.ENDANGERED             => new Color(0.95f, 0.45f, 0.05f, 1f),  // orange
        ConservationStatus.VULNERABLE             => new Color(0.95f, 0.80f, 0.10f, 1f),  // amber
        ConservationStatus.NEAR_THREATENED        => new Color(0.60f, 0.80f, 0.20f, 1f),  // lime
        ConservationStatus.LEAST_CONCERN          => new Color(0.25f, 0.75f, 0.40f, 1f),  // green
        ConservationStatus.DATA_DEFICIENT         => new Color(0.60f, 0.60f, 0.65f, 1f),  // grey
        _                                         => Color.white,
    };
}

// ════════════════════════════════════════════════════════════════
// ASIA FLORA DATA  —  BATCH 01: CARNIVOROUS & SPECIALIST
// ════════════════════════════════════════════════════════════════

public static class AsiaFloraData
{
    // ── Lookup ────────────────────────────────────────────────────

    private static List<FloraEntry> _all;

    public static List<FloraEntry> All
    {
        get
        {
            if (_all == null) _all = BuildAll();
            return _all;
        }
    }

    /// <summary>Returns all entries whose GPS centre is within radiusKm of the given position.</summary>
    public static List<FloraEntry> GetNearby(double lat, double lon, float radiusKm)
    {
        var result = new List<FloraEntry>();
        foreach (var e in All)
            if (DistKm(lat, lon, e.latitude, e.longitude) <= radiusKm + e.spawnRadiusKm)
                result.Add(e);
        return result;
    }

    /// <summary>Returns all entries matching the game category.</summary>
    public static List<FloraEntry> GetByCategory(FloraGameCategory cat)
    {
        var result = new List<FloraEntry>();
        foreach (var e in All)
            if (e.gameCategory == cat) result.Add(e);
        return result;
    }

    /// <summary>Returns all entries matching the habitat.</summary>
    public static List<FloraEntry> GetByHabitat(FloraHabitat h)
    {
        var result = new List<FloraEntry>();
        foreach (var e in All)
            if (e.habitat == h) result.Add(e);
        return result;
    }

    /// <summary>Returns all entries at or above the given threat level.</summary>
    public static List<FloraEntry> GetByConservation(ConservationStatus minStatus)
    {
        var result = new List<FloraEntry>();
        foreach (var e in All)
            if ((int)e.conservation <= (int)minStatus) result.Add(e);
        return result;
    }

    public static FloraEntry GetById(int id)
        => All.Find(e => e.id == id);

    private static double DistKm(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = lat1 - lat2, dLon = lon1 - lon2;
        return Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
    }

    // ── Data ──────────────────────────────────────────────────────

    private static List<FloraEntry> BuildAll() => new List<FloraEntry>
    {
        // ── BATCH 01: CARNIVOROUS & SPECIALIST ───────────────────

        new FloraEntry
        {
            id            = 22,
            name          = "Rafflesia",
            scientific    = "Rafflesia arnoldii",
            category      = FloraCategory.FLOWER,
            subCategory   = "Parasitic Flower",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.SEA,
            countries     = "Borneo, Sumatra (Indonesia, Malaysia)",
            habitat       = FloraHabitat.DEEP_LOWLAND_FOREST,
            latitude      =  1.0,   longitude = 114.5,   // Borneo centre
            spawnRadiusKm = 700f,
            conservation  = ConservationStatus.CRITICALLY_ENDANGERED,
            ecologicalRole= "Parasitic on Tetrastigma vine; no roots, no leaves — " +
                            "depends entirely on host for nutrients",
            significance  = "World's largest flower; ecological wonder of SEA lowland forest",
            edible        = "None (parasitic)",
            assetAnimation= FloraAssetAnimation.ANIMATED_BLOOM,
            size          = "Up to 1 m diameter",
            glowLocation  = "Deep crimson pulsing glow on petal spots",
            teamlabMood   = "Petal spots",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id            = 23,
            name          = "Pitcher Plant",
            scientific    = "Nepenthes rafflesiana",
            category      = FloraCategory.FLOWER,
            subCategory   = "Carnivorous Plant",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.SEA,
            countries     = "Borneo, Sumatra, Peninsular Malaysia",
            habitat       = FloraHabitat.HIGHLAND_PEAT,
            latitude      =  2.3,   longitude = 112.8,
            spawnRadiusKm = 600f,
            conservation  = ConservationStatus.VULNERABLE,
            ecologicalRole= "Carnivorous insect trap; provides micro-habitat " +
                            "for specialised invertebrates; water storage for fauna",
            significance  = "Carnivorous plant education; key ecological indicator of peat health",
            edible        = "None (carnivorous; digestive liquid toxic)",
            assetAnimation= FloraAssetAnimation.ANIMATED_LID_OPEN_CLOSE,
            size          = "Pitcher 15–35 cm",
            glowLocation  = "Purple-red glowing digestive liquid inside pitcher",
            teamlabMood   = "Pitcher interior",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id            = 119,
            name          = "Rafflesia kerrii",
            scientific    = "Rafflesia kerrii",
            category      = FloraCategory.FLOWER,
            subCategory   = "Parasitic Flower",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.SEA,
            countries     = "Thailand, Malaysia",
            habitat       = FloraHabitat.DEEP_LOWLAND_FOREST,
            latitude      =  7.0,   longitude = 100.5,   // southern Thailand
            spawnRadiusKm = 400f,
            conservation  = ConservationStatus.CRITICALLY_ENDANGERED,
            ecologicalRole= "Parasitic on Tetrastigma vine; no photosynthesis",
            significance  = "Thai Rafflesia; smaller cousin of R. arnoldii; " +
                            "endemic to a narrow highland corridor",
            edible        = "None",
            assetAnimation= FloraAssetAnimation.ANIMATED_BLOOM,
            size          = "~40 cm diameter",
            glowLocation  = "Deep red–cream petal shimmer",
            teamlabMood   = "Petal surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id            = 120,
            name          = "Rafflesia patma",
            scientific    = "Rafflesia patma",
            category      = FloraCategory.FLOWER,
            subCategory   = "Parasitic Flower",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.SEA,
            countries     = "Java, Indonesia",
            habitat       = FloraHabitat.DEEP_LOWLAND_FOREST,
            latitude      = -7.0,   longitude = 107.5,   // West Java
            spawnRadiusKm = 300f,
            conservation  = ConservationStatus.ENDANGERED,
            ecologicalRole= "Parasitic on Tetrastigma vine in Javan lowland remnants",
            significance  = "Java Rafflesia; red + white pattern; " +
                            "critically important as Java's lowland forest is nearly gone",
            edible        = "None",
            assetAnimation= FloraAssetAnimation.ANIMATED_BLOOM,
            size          = "50–60 cm diameter",
            glowLocation  = "Red–white spotted petal shimmer",
            teamlabMood   = "Petal spots",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id            = 153,
            name          = "Borneo Pitcher (Rajah)",
            scientific    = "Nepenthes rajah",
            category      = FloraCategory.PLANT,
            subCategory   = "Giant Carnivorous",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.SEA,
            countries     = "Borneo — Sabah (ultramafic soil only)",
            habitat       = FloraHabitat.ULTRAMAFIC_SOIL,
            latitude      =  5.9,   longitude = 116.4,   // Mt Kinabalu area
            spawnRadiusKm = 80f,    // very narrow endemic range
            conservation  = ConservationStatus.ENDANGERED,
            ecologicalRole= "Carnivorous nutrient cycling on nutrient-poor ultramafic substrate; " +
                            "pitchers host specialised commensals (mosquito larvae, crabs)",
            significance  = "World's largest pitcher plant — holds 3.5 L; documented trapping " +
                            "rats and drowned vertebrates; extraordinary ecological outlier",
            edible        = "TOXIC — liquid in pitcher",
            assetAnimation= FloraAssetAnimation.ANIMATED_LID_OPEN_CLOSE,
            size          = "Pitcher up to 40 cm tall",
            glowLocation  = "Deep wine-red / maroon pitcher interior luminous glow",
            teamlabMood   = "Pitcher interior",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id            = 154,
            name          = "Corpse Flower",
            scientific    = "Amorphophallus titanum",
            category      = FloraCategory.FLOWER,
            subCategory   = "Giant Flower",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.SEA,
            countries     = "Sumatra (Indonesia)",
            habitat       = FloraHabitat.DEEP_LOWLAND_FOREST,
            latitude      = -0.6,   longitude = 101.8,   // Central Sumatra
            spawnRadiusKm = 350f,
            conservation  = ConservationStatus.ENDANGERED,
            ecologicalRole= "Insect-pollinated via thermogenesis and carrion scent; " +
                            "attracts carrion flies and beetles for cross-pollination",
            significance  = "World's tallest inflorescence (up to 3 m); " +
                            "mimics rotting flesh via heat; blooms once every 7–10 years",
            edible        = "None (toxic corm)",
            assetAnimation= FloraAssetAnimation.ANIMATED_HEAT_SHIMMER,
            size          = "Up to 3 m inflorescence height",
            glowLocation  = "Deep maroon spathe outer glow; internal gold-cream shimmer from spadix",
            teamlabMood   = "Spathe surface + spadix interior",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id            = 82,
            name          = "Siberian Sundew",
            scientific    = "Drosera rotundifolia",
            category      = FloraCategory.PLANT,
            subCategory   = "Carnivorous Plant",
            gameCategory  = FloraGameCategory.CARNIVOROUS_SPECIALIST,
            region        = FloraRegion.NORTH_ASIA,
            countries     = "Russia, Siberia (also circumpolar)",
            habitat       = FloraHabitat.BOG_SPHAGNUM,
            latitude      = 56.0,   longitude = 82.0,    // West Siberian peatlands
            spawnRadiusKm = 2500f,  // circumpolar range
            conservation  = ConservationStatus.LEAST_CONCERN,
            ecologicalRole= "Carnivorous insect trap in nitrogen-poor sphagnum bogs; " +
                            "indicator species for intact peatland hydrology",
            significance  = "Tiny carnivorous plant; red sticky tentacles trap insects; " +
                            "thrives where soil nutrients are near zero — pure ecological adaptation",
            edible        = "None",
            assetAnimation= FloraAssetAnimation.ANIMATED_TENTACLE_CURL,
            size          = "2–10 cm rosette",
            glowLocation  = "Red sticky tentacle glow; dewdrop bead light refraction",
            teamlabMood   = "Tentacles + dewdrops",
            interactionMode = FloraInteractionMode.LIVING_WATER,
        },

        // ── MORE BATCHES APPENDED HERE AS DATA ARRIVES ───────────
    };
}
