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
    VEGETABLES,               // Cultivated & wild food crops across Asia
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
    CULTIVATED,               // Domesticated crop species; wild relatives may differ
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
    // ── Added Batch 02 ────────────────────────────────────────────
    TROPICAL_GARDEN,       // Cultivated tropical gardens & kitchen plots
    FLOODED_PADDY_FIELD,   // Wet rice paddies (lowland irrigated)
    DRY_TROPICAL,          // Dryland tropical cultivation / open scrub
    MOUNTAIN_VALLEY,       // Upland valley cultivation & terraced fields
    STEPPE_DRY,            // Central Asian dry steppe & semi-arid grassland
    TEMPERATE_FIELD,       // Temperate arable farmland
    IRRIGATED_PADDY,       // South Asian canal-irrigated paddy systems
    RIVER_PLAIN,           // Indo-Gangetic & Mekong floodplain alluvium
    MOUNTAIN_FIELD,        // High-altitude terraced fields & hill farms
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
    WESTERN_ASIA, // Turkey, Fertile Crescent, Caucasus, Levant
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
    ANIMATED_FIELD_SWAY,      // Crop field wind-sway (grains, grasses)
    ANIMATED_VINE_CLIMB,      // Climbing vine / bean tendril growth
    ANIMATED_LEAF_UNFURL,     // Broad-leaf tropical unfurling
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
        ConservationStatus.CULTIVATED             => 0.5f,  // food crops — small gratitude bonus
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
        ConservationStatus.CULTIVATED             =>  0.05f, // sustenance proximity
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
        ConservationStatus.CULTIVATED             => "Cultivated",
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
        ConservationStatus.CULTIVATED             => new Color(0.75f, 0.60f, 0.20f, 1f),  // warm gold
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

        // ── BATCH 02: VEGETABLES — SOUTHEAST ASIA (IDs 1001–1014) ──

        new FloraEntry
        {
            id = 1001, name = "Long Bean", scientific = "Vigna unguiculata sesquipedalis",
            category = FloraCategory.VINE, subCategory = "Legume Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Vietnam, Indonesia, Philippines",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 13.0, longitude = 101.0, spawnRadiusKm = 1800f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixing legume; supports soil health in tropical gardens",
            significance   = "Staple climbing bean of SEA; eaten young as vegetable; " +
                             "drought-tolerant crop with cultural importance across the region",
            edible = "Yes — pods, seeds",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–4 m; pods up to 90 cm",
            glowLocation = "Pale green pod luminescence; white-violet flower glow",
            teamlabMood   = "Climbing tendrils + pods",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1002, name = "Bitter Gourd", scientific = "Momordica charantia",
            category = FloraCategory.VINE, subCategory = "Cucurbit Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Southeast Asia, South Asia (pan-tropical)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 10.0, longitude = 106.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Fast-growing vine; insect-pollinated; attracts bees and beetles",
            significance   = "Bitter taste from momordicin; medicinal hypoglycaemic use; " +
                             "anti-diabetic traditional remedy across Asia",
            edible = "Yes — fruit (bitter); leaves medicinal",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–5 m; fruit 10–30 cm",
            glowLocation = "Warty green fruit with gold-green bioluminescent ridges",
            teamlabMood   = "Ridged warty surface",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1003, name = "SEA Eggplant", scientific = "Solanum melongena var. esculentum",
            category = FloraCategory.PLANT, subCategory = "Solanaceous Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Vietnam, Indonesia, Philippines",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 14.0, longitude = 101.0, spawnRadiusKm = 1800f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Insect-pollinated; attracts native bees; food plant for caterpillars",
            significance   = "Southeast Asian varieties differ markedly — long purple Thai types, " +
                             "small round green Philippine varieties; rich cultural palette",
            edible = "Yes — fruit",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Shrub 0.5–1.5 m",
            glowLocation = "Deep violet–purple fruit glow; pale lavender flower shimmer",
            teamlabMood   = "Fruit surface",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1004, name = "Sweet Potato", scientific = "Ipomoea batatas",
            category = FloraCategory.VINE, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Philippines, Indonesia, PNG, Vietnam (pan-tropical)",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 10.0, longitude = 124.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Ground-cover vine; erosion control; provides food for wildlife",
            significance   = "Philippines & PNG major producers; coloured flesh varieties " +
                             "(purple, orange) rich in anthocyanins and beta-carotene",
            edible = "Yes — tubers, leaves",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 1–3 m sprawling; tuber 10–30 cm",
            glowLocation = "Purple-orange tuber glow under soil; morning glory flower shimmer",
            teamlabMood   = "Underground tuber + trumpet flower",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1005, name = "Cassava", scientific = "Manihot esculenta",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Indonesia, Thailand, Vietnam, Philippines, Laos",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 3.0, longitude = 113.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Hardy drought-tolerant crop; canopy shade for understory weeds",
            significance   = "Third largest carbohydrate source in tropical world; " +
                             "HCN in bitter varieties — detoxified by fermentation & cooking",
            edible = "Yes — tubers (must be cooked); leaves edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Shrub 1–3 m",
            glowLocation = "Pale cream tuber-root luminescence beneath dark soil surface",
            teamlabMood   = "Star-shaped leaves + white root glow",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1006, name = "Winged Bean", scientific = "Psophocarpus tetragonolobus",
            category = FloraCategory.VINE, subCategory = "Legume Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Papua New Guinea, Myanmar, Thailand, Indonesia",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 8.0, longitude = 98.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixing; entire plant edible — remarkable food security crop",
            significance   = "All parts edible — pods, leaves, flowers, tubers, seeds; " +
                             "called the 'one species supermarket'; PNG staple food",
            edible = "Yes — entire plant edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–4 m; winged pod 15–25 cm",
            glowLocation = "Vivid blue flower glow; four-winged pod translucent edge shimmer",
            teamlabMood   = "Blue flowers + geometric winged pods",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1007, name = "Taro", scientific = "Colocasia esculenta",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Southeast Asia, Pacific Islands (pan-tropical cultivated)",
            habitat = FloraHabitat.FRESHWATER_WETLAND,
            latitude = 5.0, longitude = 110.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Wetland cultivar; corm feeds freshwater fauna; leaf litter enriches waterways",
            significance   = "Ancient crop — 10,000 years cultivation; Hawaiian poi and Samoan palusami; " +
                             "elephant-ear leaves iconic across tropical Asia-Pacific",
            edible = "Yes — corms, leaves (must be cooked; raw causes throat irritation)",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Plant 0.5–1.5 m; elephant-ear leaf 30–80 cm",
            glowLocation = "Dark-green giant leaf with silver-white water-bead shimmer",
            teamlabMood   = "Giant water-repellent leaf surface",
            interactionMode = FloraInteractionMode.LIVING_WATER,
        },

        new FloraEntry
        {
            id = 1008, name = "Okra", scientific = "Abelmoschus esculentus",
            category = FloraCategory.PLANT, subCategory = "Mucilaginous Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Vietnam, Indonesia, India (pan-tropical)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 15.0, longitude = 100.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Hibiscus family; large showy flowers attract pollinators; mucilage supports soil organisms",
            significance   = "Mucilaginous pod used in soups and gumbos; fibres used in paper; " +
                             "Hibiscus relative with ornamental yellow flowers",
            edible = "Yes — young pods, seeds, leaves",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Upright plant 1–2 m",
            glowLocation = "Pale yellow hibiscus flower glow; ridged green pod shimmer",
            teamlabMood   = "Hibiscus flower + ridged pod",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1009, name = "Chilli", scientific = "Capsicum annuum",
            category = FloraCategory.PLANT, subCategory = "Solanaceous Spice",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Indonesia, Philippines, Vietnam (pan-tropical; from Americas)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 14.0, longitude = 121.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Capsaicin deters mammals but not birds — birds disperse seeds; attracts hummingbirds",
            significance   = "Introduced from Americas in 16th c; transformed Asian cuisines globally; " +
                             "Thai bird's-eye chilli among world's hottest traditional varieties",
            edible = "Yes — fruit (spicy); leaves edible in some traditions",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Compact shrub 0.3–1.2 m",
            glowLocation = "Bright red/orange fruit bioluminescent heat glow; white star flower",
            teamlabMood   = "Glowing red fruit clusters",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1010, name = "Lowland Wet Rice", scientific = "Oryza sativa (indica)",
            category = FloraCategory.GRASS, subCategory = "Cereal Staple",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Cambodia, Vietnam, Thailand, Indonesia, Philippines",
            habitat = FloraHabitat.FLOODED_PADDY_FIELD,
            latitude = 12.0, longitude = 105.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Flooded paddy is artificial wetland — habitat for fish, frogs, waterfowl, herons",
            significance   = "Rice feeds more humans than any other crop; indica long-grain variety " +
                             "dominates Mekong basin and SE Asian cuisine; Tonle Sap floating villages",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.8–1.5 m",
            glowLocation = "Gold ripening panicle glow above flooded silver-mirror paddy",
            teamlabMood   = "Golden wave field + water mirror reflection",
            interactionMode = FloraInteractionMode.LIVING_WATER,
        },

        new FloraEntry
        {
            id = 1011, name = "Hill Rice", scientific = "Oryza sativa (japonica upland)",
            category = FloraCategory.GRASS, subCategory = "Cereal Staple",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Northern Laos, Northern Thailand, Myanmar, Borneo",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 20.0, longitude = 101.0, spawnRadiusKm = 1000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Dryland upland rice; supports montane smallholder biodiversity",
            significance   = "Slash-and-burn hill rice; culturally central to indigenous upland communities; " +
                             "requires no flooding; grown in rotation with fallow forest",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.6–1.2 m",
            glowLocation = "Pale amber grain shimmer on hillside terraces at dusk",
            teamlabMood   = "Hillside terrace sway",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1012, name = "Black Glutinous Rice", scientific = "Oryza sativa var. glutinosa (black)",
            category = FloraCategory.GRASS, subCategory = "Heirloom Cereal",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Northern Thailand, Myanmar, Indonesia, Laos",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 19.0, longitude = 99.0, spawnRadiusKm = 600f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Heirloom variety conserved by highland communities; anthocyanin pigments attract birds",
            significance   = "Deep purple-black husk from anthocyanin; ceremonial rice in northern Thailand; " +
                             "rich antioxidants; ingredient in khao niao dam desserts",
            edible = "Yes — grain (sticky when cooked)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.8–1.2 m",
            glowLocation = "Deep indigo-purple panicle glow; twilight violet shimmer over hillside",
            teamlabMood   = "Purple wave field",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1013, name = "Mung Bean", scientific = "Vigna radiata",
            category = FloraCategory.PLANT, subCategory = "Legume",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Southeast Asia, South Asia (pan-Asian cultivated)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 11.0, longitude = 104.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixer; short-season crop for soil recovery; supports pollinators",
            significance   = "Foundational Asian legume — eaten whole, sprouted, or as flour; " +
                             "basis of mung bean vermicelli, bean sprouts, and sweet desserts across Asia",
            edible = "Yes — seeds, sprouts, pods",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Bushy plant 30–100 cm",
            glowLocation = "Soft yellow-green seed pod shimmer; tiny yellow flower glow",
            teamlabMood   = "Bean pod clusters",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 1014, name = "Sesame", scientific = "Sesamum indicum",
            category = FloraCategory.PLANT, subCategory = "Oilseed Spice",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Myanmar, Thailand, Vietnam (pan-tropical; originally Africa/India)",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 16.0, longitude = 97.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Drought-tolerant oilseed; deep taproot breaks compacted soil; bee-pollinated",
            significance   = "Oldest oilseed crop — 5,500 years cultivation; 'open sesame' references " +
                             "seed pod explosive dehiscence; Myanmar is world's largest exporter",
            edible = "Yes — seeds (oil, paste, tahini); leaves edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Upright plant 0.5–1.5 m",
            glowLocation = "White-pink tubular flower glow; dehiscing seed pod burst shimmer",
            teamlabMood   = "Seed pod burst",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        // ── BATCH 02: VEGETABLES — EAST ASIA (IDs 2001–2009) ───────

        new FloraEntry
        {
            id = 2001, name = "Bok Choy", scientific = "Brassica rapa var. chinensis",
            category = FloraCategory.PLANT, subCategory = "Brassica Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China, Taiwan, Hong Kong (globally cultivated)",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 31.0, longitude = 121.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Fast-growing brassica; supports cabbage white butterflies; tap-soil nutrients",
            significance   = "Most-consumed leafy green in China; central to Cantonese cuisine; " +
                             "succulent stems hold broth beautifully; year-round urban crop",
            edible = "Yes — leaves, stems",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Rosette 15–40 cm",
            glowLocation = "Crisp pale-green stem radiance; deep forest-green leaf shimmer",
            teamlabMood   = "Jade-green leaf rosette",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2002, name = "Daikon", scientific = "Raphanus sativus var. longipinnatus",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "Japan, China, Korea",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 35.0, longitude = 136.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Deep taproot breaks hardpan soil; white butterfly host; decomposes quickly enriching soil",
            significance   = "Giant white radish — up to 50 cm long; Japanese tsukemono pickling staple; " +
                             "Korean kkakdugi (cubed kimchi); Chinese lo bak stew; versatile across East Asian cuisines",
            edible = "Yes — root, leaves, sprouts",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Root 20–50 cm; leaf mound 30–50 cm",
            glowLocation = "Luminous white taproot emerging from dark soil; white flower spray",
            teamlabMood   = "White glow root",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2003, name = "Napa Cabbage", scientific = "Brassica rapa var. pekinensis",
            category = FloraCategory.PLANT, subCategory = "Brassica Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China, Korea, Japan",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 38.0, longitude = 116.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Winter crop; supports overwintering insects in cool climate agriculture",
            significance   = "Soul of Korean kimchi — 2 million tonnes fermented annually; " +
                             "basis of Chinese baechu-kimchi tradition; cooling-season crop",
            edible = "Yes — leaves",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Dense head 30–50 cm",
            glowLocation = "Pale cream-yellow heart with jade outer leaves; morning frost shimmer",
            teamlabMood   = "Tightly wrapped pale heart",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2004, name = "East Asian Taro", scientific = "Colocasia esculenta (East Asian)",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China (Guangdong, Fujian), Taiwan, Japan",
            habitat = FloraHabitat.FRESHWATER_WETLAND,
            latitude = 23.0, longitude = 113.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Paddy-edge wetland plant; shelters amphibians; root channels improve waterway soils",
            significance   = "Chinese yù tou braised pork classic; Japanese satoimo in oden; " +
                             "small hairy-skinned corms with slippery flesh — different cultivar from SEA types",
            edible = "Yes — corms (must be cooked)",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Plant 0.5–1.2 m",
            glowLocation = "Glossy green-silver leaf water droplets; purple-tinged petiole shimmer",
            teamlabMood   = "Water-bead leaf",
            interactionMode = FloraInteractionMode.LIVING_WATER,
        },

        new FloraEntry
        {
            id = 2005, name = "Edamame", scientific = "Glycine max (edamame type)",
            category = FloraCategory.PLANT, subCategory = "Legume",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "Japan, China, Taiwan, Korea",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 36.0, longitude = 138.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixing soybean; attracts predatory wasps for pest control",
            significance   = "Young green soybeans; Japanese beer-garden snack; " +
                             "harvested before maturation unlike dried soya — unique culinary category",
            edible = "Yes — young pods/seeds",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Bushy plant 30–60 cm",
            glowLocation = "Vivid green fuzzy pods with soft phosphorescence; white flower blush",
            teamlabMood   = "Fuzzy pod clusters",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2006, name = "Mung Bean Sprout", scientific = "Vigna radiata (sprouted)",
            category = FloraCategory.PLANT, subCategory = "Sprout / Microgreen",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China, Taiwan, Japan, Korea",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 25.0, longitude = 121.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Sprout stage — rapid growth cycle; nutrient dense concentrated shoot",
            significance   = "Bean sprout — universal Asian stir-fry ingredient; grown in darkness without soil; " +
                             "symbolises renewal and growth in Chinese New Year dishes",
            edible = "Yes — sprouts (raw or cooked)",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Sprout 3–8 cm",
            glowLocation = "Pale white translucent sprout shimmer; tiny green seed cap glow",
            teamlabMood   = "Germinating sprout light",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2007, name = "Foxtail Millet", scientific = "Setaria italica",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China, Korea, Japan (originated northern China)",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 37.0, longitude = 111.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Fast-growing grain on dry loess soils; bird-seed crop attracting seed-eating birds",
            significance   = "Oldest cultivated crop in East Asia — 8,000 years on Chinese Loess Plateau; " +
                             "predecessor to rice as northern China staple; basis of ancient Zhou and Shang civilisations",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.6–1.5 m; drooping seed-head",
            glowLocation = "Bronze-amber drooping seed head; dry loess field twilight glow",
            teamlabMood   = "Nodding bronze seed head",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2008, name = "Sorghum", scientific = "Sorghum bicolor",
            category = FloraCategory.GRASS, subCategory = "Cereal Grain",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "Northeast China (Manchuria), India, Africa (pan-tropical/subtropical)",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 41.0, longitude = 122.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Deep root system prevents erosion; drought-resilient; supports dry-land bird populations",
            significance   = "Kaoliang grain — basis of Chinese baijiu liquor; drought-resilient staple " +
                             "across northeast China; one of world's five cereal grains",
            edible = "Yes — grain; stalks for syrup",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 1.5–4 m; large terminal panicle",
            glowLocation = "Dark burgundy seed head glow against pale gold stalks",
            teamlabMood   = "Burgundy plume field",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 2009, name = "East Asian Bitter Gourd", scientific = "Momordica charantia (East Asian)",
            category = FloraCategory.VINE, subCategory = "Cucurbit Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China, Taiwan, Japan (Okinawa)",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 23.0, longitude = 114.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Climbing vine; attracts pollinators; provides shade trellis microhabitat",
            significance   = "Okinawan gōyā champuru — cornerstone of Okinawan longevity diet; " +
                             "Cantonese bitter melon soup; different cultivar to SEA types — lighter green, longer",
            edible = "Yes — fruit (bitter)",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 3–5 m; pale green elongated fruit",
            glowLocation = "Pale jade ridged fruit luminescence; yellow flower glow",
            teamlabMood   = "Ridged pale jade surface",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        // ── BATCH 02: VEGETABLES — SOUTH ASIA (IDs 3001–3008) ──────

        new FloraEntry
        {
            id = 3001, name = "Indian Bitter Melon", scientific = "Momordica charantia (Indian)",
            category = FloraCategory.VINE, subCategory = "Cucurbit Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India, Bangladesh, Sri Lanka, Pakistan",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 20.0, longitude = 78.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Climbing vine; bee-pollinated; traditional Ayurvedic bitter digestive",
            significance   = "Karela — iconic bitterest vegetable in Indian cuisine; Ayurvedic blood-sugar " +
                             "regulation; smaller, darker, more deeply ridged than East Asian cultivars",
            edible = "Yes — fruit (intensely bitter)",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–4 m; dark green 10–20 cm fruit",
            glowLocation = "Dark-green deeply ridged fruit with gold-green ridge glow",
            teamlabMood   = "Dark ridged warty surface",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 3002, name = "Indian Mustard", scientific = "Brassica juncea",
            category = FloraCategory.PLANT, subCategory = "Oilseed / Leafy Green",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India, Bangladesh, Nepal, Pakistan",
            habitat = FloraHabitat.RIVER_PLAIN,
            latitude = 27.0, longitude = 78.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Winter season oilseed; yellow flowers attract pollinators; enriches soil nitrogen",
            significance   = "Sarson — vast yellow mustard fields of Punjab and Haryana; mustard oil " +
                             "most important cooking fat in eastern India and Bangladesh; saag mustard greens classic",
            edible = "Yes — leaves, seeds (oil, paste), young shoots",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Plant 0.5–1.5 m; yellow flower field",
            glowLocation = "Vivid chrome-yellow flower field glow; winter sun amber haze",
            teamlabMood   = "Yellow flower wave field",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 3003, name = "Indian Okra", scientific = "Abelmoschus esculentus (South Asian)",
            category = FloraCategory.PLANT, subCategory = "Mucilaginous Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India, Bangladesh, Pakistan, Sri Lanka",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 22.0, longitude = 79.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Hibiscus family; supports bee populations; mucilage improves clay soils",
            significance   = "Bhindi masala — quintessential North Indian dry vegetable dish; " +
                             "origins traced to Ethiopia/Horn of Africa; transformed through Indian cultivation",
            edible = "Yes — young pods",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Upright 0.5–2 m",
            glowLocation = "Yellow hibiscus flower glow; emerald-green ridged pod shimmer",
            teamlabMood   = "Yellow flower + green pod",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 3004, name = "Basmati Rice", scientific = "Oryza sativa (basmati)",
            category = FloraCategory.GRASS, subCategory = "Aromatic Cereal",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India (Punjab, Haryana, Uttarakhand), Pakistan (Punjab)",
            habitat = FloraHabitat.IRRIGATED_PADDY,
            latitude = 29.0, longitude = 77.0, spawnRadiusKm = 800f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Flooded paddy habitat; supports waterbirds, frogs, small fish in paddy ecosystem",
            significance   = "Queen of rices — GI-protected Himalayan-foothill aromatic grain; " +
                             "2-acetyl-1-pyrroline aroma compound; biryani and pilaf premium staple",
            edible = "Yes — grain (aromatic)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Tall grass 1.0–1.8 m; extra-long grain",
            glowLocation = "Golden-amber panicle with soft scent-haze shimmer above Himalayan foothills paddy",
            teamlabMood   = "Aromatic gold wave",
            interactionMode = FloraInteractionMode.LIVING_WATER,
        },

        new FloraEntry
        {
            id = 3005, name = "Indian Wheat", scientific = "Triticum aestivum (Indian)",
            category = FloraCategory.GRASS, subCategory = "Cereal Staple",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India (Punjab, Haryana, UP), Pakistan",
            habitat = FloraHabitat.IRRIGATED_PADDY,
            latitude = 31.0, longitude = 75.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Winter staple with deep root system; supports cereal-field bird communities",
            significance   = "Green Revolution wheat transformed Indian food security 1960s–70s; " +
                             "roti, chapati, paratha — daily bread of North India and Pakistan",
            edible = "Yes — grain (flour)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.6–1.2 m",
            glowLocation = "Pale gold wheat ear field; winter morning frost shimmer",
            teamlabMood   = "Gold wheat sea",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 3006, name = "Red Lentil", scientific = "Lens culinaris (red masoor)",
            category = FloraCategory.PLANT, subCategory = "Legume",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India, Pakistan, Bangladesh, Nepal",
            habitat = FloraHabitat.RIVER_PLAIN,
            latitude = 25.0, longitude = 68.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixing winter legume; improves cereal rotation soils",
            significance   = "Masoor dal — most-consumed pulse in South Asia; red-orange split lentil; " +
                             "dal makhani, dal tadka — fundamental proteins of Indian vegetarian diet",
            edible = "Yes — seeds (dried)",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Low plant 20–40 cm",
            glowLocation = "Soft rosy-red seed pod glow; delicate white-pink flower shimmer",
            teamlabMood   = "Red seed pods",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 3007, name = "Ridge Gourd", scientific = "Luffa acutangula",
            category = FloraCategory.VINE, subCategory = "Cucurbit Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India, Sri Lanka, Bangladesh (pan-South/Southeast Asian)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 18.0, longitude = 74.0, spawnRadiusKm = 1800f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Trellis vine; fibrous skeleton becomes loofah sponge for birds to nest in",
            significance   = "Turai / torai — sharp-ridged gourd; eaten young as vegetable; " +
                             "mature fruit dries into natural loofah sponge — zero-waste plant",
            edible = "Yes — young fruit; mature dried loofah for bathing",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 3–5 m; ridged fruit 20–40 cm",
            glowLocation = "Long ridged fruit with golden ridge-line glow; yellow flower clusters",
            teamlabMood   = "Sharp ridged surface",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 3008, name = "Young Jackfruit", scientific = "Artocarpus heterophyllus (unripe)",
            category = FloraCategory.TREE, subCategory = "Tropical Tree Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SOUTH_ASIA,
            countries = "India (Kerala, Tamil Nadu), Bangladesh, Sri Lanka",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 9.0, longitude = 77.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Large canopy tree; fruits feed bats, monkeys, elephants; provides shade and habitat",
            significance   = "Unripe jackfruit — meaty texture used as vegan meat substitute; " +
                             "Kerala kathal curry; world's largest tree fruit (up to 50 kg); " +
                             "national fruit of Bangladesh",
            edible = "Yes — unripe fruit (cooked); ripe fruit, seeds",
            assetAnimation = FloraAssetAnimation.ANIMATED_CANOPY_RUSTLE,
            size = "Tree 10–25 m; fruit up to 50 kg",
            glowLocation = "Massive green spiky fruit hanging from trunk; warm gold-green glow",
            teamlabMood   = "Giant spiky fruit on trunk",
            interactionMode = FloraInteractionMode.CANOPY_HOVER,
        },

        // ── BATCH 02: VEGETABLES — CENTRAL ASIA (IDs 4001–4006) ────

        new FloraEntry
        {
            id = 4001, name = "Wild Carrot", scientific = "Daucus carota subsp. sativus (origin)",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable — Wild Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "Afghanistan, Kazakhstan, Uzbekistan (origin of cultivated carrot)",
            habitat = FloraHabitat.STEPPE_DRY,
            latitude = 33.0, longitude = 65.0, spawnRadiusKm = 1200f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Umbelliferous wildflower; supports parasitic wasps and hoverflies; bird-seeded",
            significance   = "Purple and yellow wild carrots domesticated in Afghanistan 900–1000 CE; " +
                             "orange carrot developed in Netherlands 17th c from purple ancestors; " +
                             "white Queen Anne's Lace flower is the wild form",
            edible = "Wild form — small bitter root edible; cultivated descendant ubiquitous",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Biennial 30–100 cm; white umbel flower",
            glowLocation = "White lace umbel flower with central dark-purple floret; dry steppe gold",
            teamlabMood   = "Lace umbel flower cluster",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 4002, name = "Wild Onion", scientific = "Allium cepa (wild ancestor)",
            category = FloraCategory.PLANT, subCategory = "Allium — Wild Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "Central Asia (Kazakhstan, Uzbekistan, Kyrgyzstan origin)",
            habitat = FloraHabitat.STEPPE_DRY,
            latitude = 42.0, longitude = 63.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Bulb plant prevents soil erosion on steppe; attracts specialist allium bees",
            significance   = "Wild ancestor of all cultivated onions; Central Asian steppe origin; " +
                             "spherical purple flower heads carpet the steppe in spring — striking spectacle",
            edible = "Yes — bulb, leaves, flowers (pungent)",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Plant 30–80 cm; purple globe flower",
            glowLocation = "Purple globe flower with violet starburst bioluminescence",
            teamlabMood   = "Purple allium globe",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 4003, name = "Emmer Wheat", scientific = "Triticum dicoccum",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "Turkey, Iran, Iraq (Fertile Crescent origin); spread to Central Asia",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 39.0, longitude = 42.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.NEAR_THREATENED,
            ecologicalRole = "Hardy ancient grain; wild relatives provide genetic diversity for modern wheat breeding",
            significance   = "One of first domesticated wheats — 10,000 years ago; Egyptian mummy " +
                             "bread; still grown in Ethiopia and parts of Turkey; ancestor of durum wheat",
            edible = "Yes — grain (nutritious, hull-covered)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.5–1.2 m; bristled ear",
            glowLocation = "Amber-bronze bristled ear against blue mountain sky",
            teamlabMood   = "Ancient bristled grain",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 4004, name = "Wild Barley", scientific = "Hordeum vulgare subsp. spontaneum",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain — Wild",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "Fertile Crescent, Iran, Afghanistan (origin); Central Asia",
            habitat = FloraHabitat.STEPPE_DRY,
            latitude = 35.0, longitude = 47.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "First coloniser of disturbed steppe soils; supports granivorous birds",
            significance   = "Ancestor of all cultivated barley — domesticated 10,000 BP; " +
                             "first grain used for Mesopotamian beer (Sumerian hymn to Ninkasi); " +
                             "Tibetan tsampa (roasted barley flour) staple",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.3–0.8 m; long-awned ear",
            glowLocation = "Long golden awn catching steppe wind; amber wave in harsh sunlight",
            teamlabMood   = "Long-awned steppe barley",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 4005, name = "Proso Millet", scientific = "Panicum miliaceum",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "Kazakhstan, Xinjiang (Central Asian origin); spread globally",
            habitat = FloraHabitat.STEPPE_DRY,
            latitude = 46.0, longitude = 72.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Extremely drought-tolerant; shortest growing season of any cereal — 60 days",
            significance   = "First millet domesticated in Central Asia — 10,000 BP; spreads along steppe " +
                             "trade routes; basis of ancient Pontic steppe pastoralist diet; still birdseed today",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.5–1.0 m; drooping panicle",
            glowLocation = "Tan-yellow drooping panicle shimmering in dry steppe wind",
            teamlabMood   = "Drooping open panicle",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 4006, name = "Buckwheat", scientific = "Fagopyrum esculentum",
            category = FloraCategory.PLANT, subCategory = "Pseudocereal",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "China (Yunnan origin), Central Asia, Russia",
            habitat = FloraHabitat.MOUNTAIN_VALLEY,
            latitude = 28.0, longitude = 101.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Fast-growing bee magnet; white flowers rich in nectar; improves poor acidic soils",
            significance   = "Not a true grain — polygonaceous plant related to sorrel; Japanese soba noodles; " +
                             "Russian kasha; gluten-free; cold-hardy; Yunnan origin spread along Silk Road",
            edible = "Yes — seeds (flour); shoots edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Plant 0.3–1.2 m; white flower mass",
            glowLocation = "Mass of tiny white-pink flowers; mountain meadow pink-white shimmer",
            teamlabMood   = "Tiny white flower sea",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        // ── BATCH 02: VEGETABLES — WESTERN ASIA (IDs 5001–5006) ────

        new FloraEntry
        {
            id = 5001, name = "Origin Eggplant", scientific = "Solanum melongena (wild origin)",
            category = FloraCategory.PLANT, subCategory = "Solanaceous Vegetable — Wild Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Turkey, Syria, Iran (domestication corridor)",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 37.0, longitude = 43.0, spawnRadiusKm = 800f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Wild ancestor of cultivated eggplant; supports specialist bee species on solanaceous plants",
            significance   = "Eggplant origin traced to Turkey–Iran border; Sanskrit 'vatinganah' confirms " +
                             "ancient South Asian use; ancestor of the vast cultivated diversity from Asia to Mediterranean",
            edible = "Wild form small and bitter; ancestor of all cultivated varieties",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Shrub 0.5–1 m; small spiny fruit",
            glowLocation = "Pale purple spiny wild fruit; violet star flower glow",
            teamlabMood   = "Purple star flower + wild fruit",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 5002, name = "Lentil", scientific = "Lens culinaris",
            category = FloraCategory.PLANT, subCategory = "Legume — Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Syria, Turkey, Jordan (Fertile Crescent origin)",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 36.0, longitude = 38.0, spawnRadiusKm = 800f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixer; one of first domesticated legumes; wild relatives genetic resource",
            significance   = "Among the oldest cultivated plants — 11,000 BP at Tell Abu Hureyra; " +
                             "Esau's biblical mess of pottage; foundation of Levantine and Indian cuisine",
            edible = "Yes — seeds",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Trailing plant 20–40 cm; tiny round seeds",
            glowLocation = "Delicate blue-violet tiny flower; green-tan seed pod shimmer",
            teamlabMood   = "Tiny blue flower + round seeds",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 5003, name = "Chickpea", scientific = "Cicer arietinum",
            category = FloraCategory.PLANT, subCategory = "Legume",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Turkey, Syria, Iraq, Iran (Fertile Crescent origin)",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 37.0, longitude = 40.0, spawnRadiusKm = 1000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Nitrogen-fixing winter annual; deep root accesses subsoil moisture; drought-adapted",
            significance   = "9,500 years cultivated; hummus, falafel, chana masala — three civilisations " +
                             "built on the chickpea; second most important legume after soybean globally",
            edible = "Yes — seeds (dried/cooked); young shoots",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Bushy plant 20–50 cm; hairy sticky pods",
            glowLocation = "White-pink flower glow; hairy sticky pod shimmer in morning dew",
            teamlabMood   = "Hairy sticky pods + white flower",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 5004, name = "Broad Bean", scientific = "Vicia faba",
            category = FloraCategory.PLANT, subCategory = "Legume",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Levant, Egypt, Iran (ancient origin — Neolithic)",
            habitat = FloraHabitat.RIVER_PLAIN,
            latitude = 34.0, longitude = 36.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Cool-season nitrogen-fixer; attracts bumble bees; overwintering cover crop",
            significance   = "Fava bean — 10,000 years of cultivation; Egyptian ful medames " +
                             "national dish; Roman food staple; oldest European protein before New World beans",
            edible = "Yes — seeds (fresh/dried); young pods, shoots",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Upright plant 30–120 cm; large flat pods",
            glowLocation = "White flower with dark-purple spot; large flat green pod glow",
            teamlabMood   = "White flower + broad pod",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 5005, name = "Einkorn Wheat", scientific = "Triticum monococcum",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain — Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Southeast Turkey (Karacadağ — precise origin), Levant",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 37.5, longitude = 40.5, spawnRadiusKm = 500f,
            conservation = ConservationStatus.NEAR_THREATENED,
            ecologicalRole = "Wild Einkorn is gene bank for modern wheat; rocky limestone terrace specialist",
            significance   = "The first domesticated wheat — 10,000 BP at Karacadağ mountains; " +
                             "Ötzi the Iceman had Einkorn in his stomach; single-grained ear; still grown in Tuscany",
            edible = "Yes — grain (hull-covered, nutritious)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.4–0.8 m; single-grained ear",
            glowLocation = "Single-grain amber ear glowing above limestone terrace; origin-of-civilisation shimmer",
            teamlabMood   = "Ancient single-grain shimmer",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 5006, name = "Original Barley", scientific = "Hordeum vulgare (domesticated origin)",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain — Fertile Crescent",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Fertile Crescent — Iraq, Syria, Jordan, Israel",
            habitat = FloraHabitat.RIVER_PLAIN,
            latitude = 34.0, longitude = 40.0, spawnRadiusKm = 1000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Early ripening grain; foundation of Mesopotamian cereal agriculture",
            significance   = "Basis of Sumerian and Babylonian agriculture and beer production; " +
                             "Ninkasi hymn (1800 BCE) describes barley beer recipe; still used for malt whisky and beer",
            edible = "Yes — grain (flour, malt, beer)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.5–1.0 m; two-row or six-row ear",
            glowLocation = "Gold-bronze ear rows in Mesopotamian plain; ancient harvest amber glow",
            teamlabMood   = "Ancient harvest gold",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        // ── BATCH 02: VEGETABLES — NORTH ASIA (IDs 6001–6005) ──────

        new FloraEntry
        {
            id = 6001, name = "Siberian Buckwheat", scientific = "Fagopyrum esculentum (Siberian)",
            category = FloraCategory.PLANT, subCategory = "Pseudocereal",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.NORTH_ASIA,
            countries = "Russia (Siberia, Ural, Volga), Ukraine",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 55.0, longitude = 85.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Cold-hardy bee plant; white flower fields support bee colonies across Siberia",
            significance   = "Grechnevaya kasha — Russian buckwheat porridge; national comfort food; " +
                             "post-Mongol spread across Russia; gluten-free; frost-tolerant crop",
            edible = "Yes — seeds (kasha porridge, flour)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Plant 0.3–0.8 m; white flower mass",
            glowLocation = "White flower fields shimmering against Siberian birch forest edge",
            teamlabMood   = "White flower + birch border",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 6002, name = "Rye", scientific = "Secale cereale",
            category = FloraCategory.GRASS, subCategory = "Cold-Hardy Cereal",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.NORTH_ASIA,
            countries = "Russia, Ukraine, Belarus, Kazakhstan (main producers)",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 52.0, longitude = 40.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Cover crop for cold climates; deep root prevents soil erosion under snow",
            significance   = "Black bread of Russia and Eastern Europe; the grain that grows where wheat cannot; " +
                             "bread rye — dense dark loaves; ergot fungus on rye caused historical epidemics",
            edible = "Yes — grain (dense dark bread, kvass)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 1.0–2.0 m; slender nodding ear",
            glowLocation = "Tall slender blue-green ear field; northern twilight indigo haze",
            teamlabMood   = "Tall blue-green cold field",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 6003, name = "Wild Beet", scientific = "Beta vulgaris subsp. maritima",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable — Wild Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.NORTH_ASIA,
            countries = "Russia (Black Sea coast), Ukraine, Caucasus",
            habitat = FloraHabitat.STEPPE_DRY,
            latitude = 47.0, longitude = 38.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Wild ancestor of sugar beet, chard, beetroot, spinach beet; salt-tolerant coastal plant",
            significance   = "Sea beet — ancestor of all beet crops; saline steppe and coastal wild plant; " +
                             "sugar beet provides 20% of world's sugar; beetroot iconic in Russian borscht",
            edible = "Yes — leaves; wild root small and fibrous",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Sprawling plant 30–100 cm; small green-red leaves",
            glowLocation = "Red-purple leaf vein glow; coastal steppe salt-wind shimmer",
            teamlabMood   = "Red-veined leaf glow",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 6004, name = "Cabbage", scientific = "Brassica oleracea var. capitata",
            category = FloraCategory.PLANT, subCategory = "Brassica Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.NORTH_ASIA,
            countries = "Russia, Ukraine, Poland (major producers); wild ancestor Mediterranean coast",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 52.0, longitude = 30.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Brassica family host for caterpillars and butterflies; supports cabbage white butterfly lifecycle",
            significance   = "Borsch, sauerkraut, golabki (stuffed cabbage) — Eastern European culinary foundation; " +
                             "wild ancestor is coastal European sea kale; brassica oleracea also gives broccoli, " +
                             "cauliflower, kale, Brussels sprouts through selection",
            edible = "Yes — head leaves",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Dense round head 25–40 cm",
            glowLocation = "Pale blue-green outer leaves with cream-white tight heart shimmer",
            teamlabMood   = "Tight wrapped head",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        new FloraEntry
        {
            id = 6005, name = "Potato (introduced)", scientific = "Solanum tuberosum",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable (introduced Andean)",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.NORTH_ASIA,
            countries = "Russia (world's 3rd largest producer); originated Andes, South America",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 55.0, longitude = 37.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Introduced crop; now embedded in Russian agricultural ecology; supports beetles and moths",
            significance   = "Introduced from Americas 1570s; transformed Russian and European food security; " +
                             "kartoshka — so central to Russian cuisine it feels native; vodka from potato",
            edible = "Yes — tubers (many preparations)",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Sprawling plant 0.5–1.0 m",
            glowLocation = "White-purple star flower; underground pale-gold tuber cluster glow",
            teamlabMood   = "Star flower + underground tuber glow",
            interactionMode = FloraInteractionMode.GROUND_LEVEL,
        },

        // ── MORE BATCHES APPENDED HERE AS DATA ARRIVES ───────────
    };
}

