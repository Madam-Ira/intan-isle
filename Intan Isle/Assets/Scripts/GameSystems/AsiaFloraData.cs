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
    SHRUB,
    SUCCULENT,
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
    MUSHROOMS,                // Edible, medicinal & toxic mushrooms and bracket fungi
    HERBS,                    // Culinary, medicinal & sacred herbs, spices and aromatic plants
    SHRUBS,                   // Woody shrubs, bamboo, tundra mats, epiphytes and ground cover
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
    // ── Added Batch 03 ────────────────────────────────────────────
    DECAYING_WOOD,         // Fallen logs, dead trunks and stumps (mushroom substrate)
    HARDWOOD_LOG,          // Cultivated mushroom logs (shiitake, oyster)
    FOREST_FLOOR,          // General forest floor duff, litter and soil
    BIRCH_FOREST,          // Birch-dominated boreal forest (chaga host trees)
    PINE_FOREST,           // Pine-dominated forest (milk cap mycorrhizal zone)
    BOREAL_MOUNTAIN_FOREST,// Boreal and montane mixed conifer–birch forest
    WOODLAND_FIELD,        // Woodland edge and agricultural field margin
    // ── Added Batch 04 ────────────────────────────────────────────
    ALPINE_STREAM,         // Pure cold mountain stream (Wasabi habitat)
    HIGHLAND_GARDEN,       // Highland cultivation plots and mountain gardens
    DRY_SCRUB,             // Dry rocky scrub, garrigue and Mediterranean maquis
    SUBTROPICAL_FOREST,    // Warm subtropical forest (Star Anise, Cinnamon)
    ROCKY_COASTAL,         // Rocky coastal cliffs and sea-facing limestone slopes
    // ── Added Batch 05 ────────────────────────────────────────────
    RIVERBANK_FOREST_EDGE, // Tropical riverbank and forest edge interface (Giant Bamboo)
    COASTAL_GARDEN,        // Coastal cultivation gardens and shoreline plots (Pandanus)
    TROPICAL_FOREST_EPIPHYTE, // Epiphytic zone on tropical rainforest trees
    TEMPERATE_FOREST,      // Broadleaf and mixed temperate forest (Moso Bamboo)
    DARK_HUMID_FOREST,     // Deep dark humid forest floor with dense canopy (Indian Pipe)
    ROCKY_DESERT,          // Rocky desert pavement and gravel plains (Kazakh Ephedra)
    ROCKY_ALPINE,          // Bare rocky alpine scree and cliff faces (Stonecrop)
    TUNDRA,                // Arctic and subarctic tundra (Willow, Lichen, Dwarf Pine)
    WET_TUNDRA,            // Waterlogged Arctic tundra and frost-heave bog (Cottongrass)
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
    QUIET_FOREST,      // Medicinal / contemplative — calm aura, no Veiled World required
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
    ANIMATED_GLOW_PULSE,      // Bioluminescent pulse cycle (Ghost Fungus)
    ANIMATED_EMERGENCE,       // Dramatic ground emergence + veil deployment (Stinkhorn)
    ANIMATED_BARK_PEEL,       // Spiral bark-stripping animation (Cinnamon harvest)
    ANIMATED_FEATHER_PLUME,   // Long silver feather plumes cascading in steppe wind
    ANIMATED_MASS_BLOOM,      // Mass simultaneous petal bloom covering whole shrub
    ANIMATED_CATKIN_SWAY,     // Catkin pendulums trembling in Arctic wind
    ANIMATED_WIND_SCULPTED,   // Wind-sculpted prostrate growth direction animation
    ANIMATED_COTTON_SWAY,     // White cotton-ball seed heads swaying in tundra wind
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
            id = 1001, name = "Long Bean", scientific = "Vigna unguiculata",
            category = FloraCategory.VINE, subCategory = "Legume Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Vietnam, Indonesia, Philippines",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 13.0, longitude = 101.0, spawnRadiusKm = 1800f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Nitrogen-fixing legume; supports soil health in tropical gardens",
            significance   = "Staple climbing bean of SEA; eaten young as vegetable; " +
                             "drought-tolerant crop with cultural importance across the region",
            edible = "Yes — pods, seeds",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–4 m; pods up to 90 cm",
            glowLocation = "Pale green pod luminescence; white-violet flower glow",
            teamlabMood   = "Climbing tendrils + pods",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1002, name = "Bitter Gourd", scientific = "Momordica charantia",
            category = FloraCategory.VINE, subCategory = "Cucurbit Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Southeast Asia, South Asia (pan-tropical)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 10.0, longitude = 106.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Fast-growing vine; insect-pollinated; attracts bees and beetles",
            significance   = "Bitter taste from momordicin; medicinal hypoglycaemic use; " +
                             "anti-diabetic traditional remedy across Asia",
            edible = "Yes — fruit (bitter); leaves medicinal",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–5 m; fruit 10–30 cm",
            glowLocation = "Warty green fruit with gold-green bioluminescent ridges",
            teamlabMood   = "Ridged warty surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1003, name = "SEA Eggplant", scientific = "Solanum melongena var. esculentum",
            category = FloraCategory.PLANT, subCategory = "Solanaceous Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Vietnam, Indonesia, Philippines",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 14.0, longitude = 101.0, spawnRadiusKm = 1800f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Insect-pollinated; attracts native bees; food plant for caterpillars",
            significance   = "Southeast Asian varieties differ markedly — long purple Thai types, " +
                             "small round green Philippine varieties; rich cultural palette",
            edible = "Yes — fruit",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Shrub 0.5–1.5 m",
            glowLocation = "Deep violet–purple fruit glow; pale lavender flower shimmer",
            teamlabMood   = "Fruit surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1004, name = "Sweet Potato", scientific = "Ipomoea batatas",
            category = FloraCategory.VINE, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Philippines, Indonesia, PNG, Vietnam (pan-tropical)",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 10.0, longitude = 124.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Ground-cover vine; erosion control; provides food for wildlife",
            significance   = "Philippines & PNG major producers; coloured flesh varieties " +
                             "(purple, orange) rich in anthocyanins and beta-carotene",
            edible = "Yes — tubers, leaves",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 1–3 m sprawling; tuber 10–30 cm",
            glowLocation = "Purple-orange tuber glow under soil; morning glory flower shimmer",
            teamlabMood   = "Underground tuber + trumpet flower",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1005, name = "Cassava", scientific = "Manihot esculenta",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Indonesia, Thailand, Vietnam, Philippines, Laos",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 3.0, longitude = 113.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Hardy drought-tolerant crop; canopy shade for understory weeds",
            significance   = "Third largest carbohydrate source in tropical world; " +
                             "HCN in bitter varieties — detoxified by fermentation & cooking",
            edible = "Yes — tubers (must be cooked); leaves edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Shrub 1–3 m",
            glowLocation = "Pale cream tuber-root luminescence beneath dark soil surface",
            teamlabMood   = "Star-shaped leaves + white root glow",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1006, name = "Winged Bean", scientific = "Psophocarpus tetragonolobus",
            category = FloraCategory.VINE, subCategory = "Legume Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Papua New Guinea, Myanmar, Thailand, Indonesia",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 8.0, longitude = 98.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Nitrogen-fixing; entire plant edible — remarkable food security crop",
            significance   = "All parts edible — pods, leaves, flowers, tubers, seeds; " +
                             "called the 'one species supermarket'; PNG staple food",
            edible = "Yes — entire plant edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–4 m; winged pod 15–25 cm",
            glowLocation = "Vivid blue flower glow; four-winged pod translucent edge shimmer",
            teamlabMood   = "Blue flowers + geometric winged pods",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1007, name = "Taro", scientific = "Colocasia esculenta",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Southeast Asia, Pacific Islands (pan-tropical cultivated)",
            habitat = FloraHabitat.FRESHWATER_WETLAND,
            latitude = 5.0, longitude = 110.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
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
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Hibiscus family; large showy flowers attract pollinators; mucilage supports soil organisms",
            significance   = "Mucilaginous pod used in soups and gumbos; fibres used in paper; " +
                             "Hibiscus relative with ornamental yellow flowers",
            edible = "Yes — young pods, seeds, leaves",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Upright plant 1–2 m",
            glowLocation = "Pale yellow hibiscus flower glow; ridged green pod shimmer",
            teamlabMood   = "Hibiscus flower + ridged pod",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1009, name = "Chilli", scientific = "Capsicum annuum",
            category = FloraCategory.PLANT, subCategory = "Solanaceous Spice",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Thailand, Indonesia, Philippines, Vietnam (pan-tropical; from Americas)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 14.0, longitude = 121.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Capsaicin deters mammals but not birds — birds disperse seeds; attracts hummingbirds",
            significance   = "Introduced from Americas in 16th c; transformed Asian cuisines globally; " +
                             "Thai bird's-eye chilli among world's hottest traditional varieties",
            edible = "Yes — fruit (spicy); leaves edible in some traditions",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Compact shrub 0.3–1.2 m",
            glowLocation = "Bright red/orange fruit bioluminescent heat glow; white star flower",
            teamlabMood   = "Glowing red fruit clusters",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1010, name = "Lowland Wet Rice", scientific = "Oryza sativa",
            category = FloraCategory.GRASS, subCategory = "Cereal Staple",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Cambodia, Vietnam, Thailand, Indonesia, Philippines",
            habitat = FloraHabitat.FLOODED_PADDY_FIELD,
            latitude = 12.0, longitude = 105.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Flooded paddy is artificial wetland — habitat for fish, frogs, waterfowl, herons",
            significance   = "Rice feeds more humans than any other crop; indica long-grain variety " +
                             "dominates Mekong basin and SE Asian cuisine; Tonle Sap floating villages",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.8–1.5 m",
            glowLocation = "Gold ripening panicle glow above flooded silver-mirror paddy",
            teamlabMood   = "Golden wave field + water mirror reflection",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1011, name = "Hill Rice", scientific = "Oryza sativa (japonica upland)",
            category = FloraCategory.GRASS, subCategory = "Cereal Staple",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Northern Laos, Northern Thailand, Myanmar, Borneo",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 20.0, longitude = 101.0, spawnRadiusKm = 1000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Dryland upland rice; supports montane smallholder biodiversity",
            significance   = "Slash-and-burn hill rice; culturally central to indigenous upland communities; " +
                             "requires no flooding; grown in rotation with fallow forest",
            edible = "Yes — grain",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.6–1.2 m",
            glowLocation = "Pale amber grain shimmer on hillside terraces at dusk",
            teamlabMood   = "Hillside terrace sway",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1012, name = "Black Glutinous Rice", scientific = "Oryza sativa var. glutinosa (black)",
            category = FloraCategory.GRASS, subCategory = "Heirloom Cereal",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Northern Thailand, Myanmar, Indonesia, Laos",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 19.0, longitude = 99.0, spawnRadiusKm = 600f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Heirloom variety conserved by highland communities; anthocyanin pigments attract birds",
            significance   = "Deep purple-black husk from anthocyanin; ceremonial rice in northern Thailand; " +
                             "rich antioxidants; ingredient in khao niao dam desserts",
            edible = "Yes — grain (sticky when cooked)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.8–1.2 m",
            glowLocation = "Deep indigo-purple panicle glow; twilight violet shimmer over hillside",
            teamlabMood   = "Purple wave field",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1013, name = "Mung Bean", scientific = "Vigna radiata",
            category = FloraCategory.PLANT, subCategory = "Legume",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Southeast Asia, South Asia (pan-Asian cultivated)",
            habitat = FloraHabitat.TROPICAL_GARDEN,
            latitude = 11.0, longitude = 104.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Nitrogen-fixer; short-season crop for soil recovery; supports pollinators",
            significance   = "Foundational Asian legume — eaten whole, sprouted, or as flour; " +
                             "basis of mung bean vermicelli, bean sprouts, and sweet desserts across Asia",
            edible = "Yes — seeds, sprouts, pods",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Bushy plant 30–100 cm",
            glowLocation = "Soft yellow-green seed pod shimmer; tiny yellow flower glow",
            teamlabMood   = "Bean pod clusters",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 1014, name = "Sesame", scientific = "Sesamum indicum",
            category = FloraCategory.PLANT, subCategory = "Oilseed Spice",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.SEA,
            countries = "Myanmar, Thailand, Vietnam (pan-tropical; originally Africa/India)",
            habitat = FloraHabitat.DRY_TROPICAL,
            latitude = 16.0, longitude = 97.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Drought-tolerant oilseed; deep taproot breaks compacted soil; bee-pollinated",
            significance   = "Oldest oilseed crop — 5,500 years cultivation; 'open sesame' references " +
                             "seed pod explosive dehiscence; Myanmar is world's largest exporter",
            edible = "Yes — seeds (oil, paste, tahini); leaves edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Upright plant 0.5–1.5 m",
            glowLocation = "White-pink tubular flower glow; dehiscing seed pod burst shimmer",
            teamlabMood   = "Seed pod burst",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 2009, name = "East Asian Bitter Gourd", scientific = "Momordica charantia",
            category = FloraCategory.VINE, subCategory = "Cucurbit Vegetable",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.EAST_ASIA,
            countries = "China, Taiwan, Japan (Okinawa)",
            habitat = FloraHabitat.TEMPERATE_FIELD,
            latitude = 23.0, longitude = 114.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Climbing vine; attracts pollinators; provides shade trellis microhabitat",
            significance   = "Okinawan gōyā champuru — cornerstone of Okinawan longevity diet; " +
                             "Cantonese bitter melon soup; different cultivar to SEA types — lighter green, longer",
            edible = "Yes — fruit (bitter)",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 3–5 m; pale green elongated fruit",
            glowLocation = "Pale jade ridged fruit luminescence; yellow flower glow",
            teamlabMood   = "Ridged pale jade surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Climbing vine; bee-pollinated; traditional Ayurvedic bitter digestive",
            significance   = "Karela — iconic bitterest vegetable in Indian cuisine; Ayurvedic blood-sugar " +
                             "regulation; smaller, darker, more deeply ridged than East Asian cultivars",
            edible = "Yes — fruit (intensely bitter)",
            assetAnimation = FloraAssetAnimation.ANIMATED_VINE_CLIMB,
            size = "Vine 2–4 m; dark green 10–20 cm fruit",
            glowLocation = "Dark-green deeply ridged fruit with gold-green ridge glow",
            teamlabMood   = "Dark ridged warty surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 4003, name = "Emmer Wheat", scientific = "Triticum dicoccum",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.CENTRAL_ASIA,
            countries = "Turkey, Iran, Iraq (Fertile Crescent origin); spread to Central Asia",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 39.0, longitude = 42.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Hardy ancient grain; wild relatives provide genetic diversity for modern wheat breeding",
            significance   = "One of first domesticated wheats — 10,000 years ago; Egyptian mummy " +
                             "bread; still grown in Ethiopia and parts of Turkey; ancestor of durum wheat",
            edible = "Yes — grain (nutritious, hull-covered)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.5–1.2 m; bristled ear",
            glowLocation = "Amber-bronze bristled ear against blue mountain sky",
            teamlabMood   = "Ancient bristled grain",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Wild ancestor of cultivated eggplant; supports specialist bee species on solanaceous plants",
            significance   = "Eggplant origin traced to Turkey–Iran border; Sanskrit 'vatinganah' confirms " +
                             "ancient South Asian use; ancestor of the vast cultivated diversity from Asia to Mediterranean",
            edible = "Wild form small and bitter; ancestor of all cultivated varieties",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Shrub 0.5–1 m; small spiny fruit",
            glowLocation = "Pale purple spiny wild fruit; violet star flower glow",
            teamlabMood   = "Purple star flower + wild fruit",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 5005, name = "Einkorn Wheat", scientific = "Triticum monococcum",
            category = FloraCategory.GRASS, subCategory = "Ancient Grain — Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.WESTERN_ASIA,
            countries = "Southeast Turkey (Karacadağ — precise origin), Levant",
            habitat = FloraHabitat.MOUNTAIN_FIELD,
            latitude = 37.5, longitude = 40.5, spawnRadiusKm = 500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Wild Einkorn is gene bank for modern wheat; rocky limestone terrace specialist",
            significance   = "The first domesticated wheat — 10,000 BP at Karacadağ mountains; " +
                             "Ötzi the Iceman had Einkorn in his stomach; single-grained ear; still grown in Tuscany",
            edible = "Yes — grain (hull-covered, nutritious)",
            assetAnimation = FloraAssetAnimation.ANIMATED_FIELD_SWAY,
            size = "Grass 0.4–0.8 m; single-grained ear",
            glowLocation = "Single-grain amber ear glowing above limestone terrace; origin-of-civilisation shimmer",
            teamlabMood   = "Ancient single-grain shimmer",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 6003, name = "Wild Beet", scientific = "Beta vulgaris subsp. maritima",
            category = FloraCategory.PLANT, subCategory = "Root Vegetable — Wild Origin",
            gameCategory = FloraGameCategory.VEGETABLES, region = FloraRegion.NORTH_ASIA,
            countries = "Russia (Black Sea coast), Ukraine, Caucasus",
            habitat = FloraHabitat.STEPPE_DRY,
            latitude = 47.0, longitude = 38.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Wild ancestor of sugar beet, chard, beetroot, spinach beet; salt-tolerant coastal plant",
            significance   = "Sea beet — ancestor of all beet crops; saline steppe and coastal wild plant; " +
                             "sugar beet provides 20% of world's sugar; beetroot iconic in Russian borscht",
            edible = "Yes — leaves; wild root small and fibrous",
            assetAnimation = FloraAssetAnimation.ANIMATED_LEAF_UNFURL,
            size = "Sprawling plant 30–100 cm; small green-red leaves",
            glowLocation = "Red-purple leaf vein glow; coastal steppe salt-wind shimmer",
            teamlabMood   = "Red-veined leaf glow",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
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
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        // ── BATCH 03: MUSHROOMS — SEA (IDs 7001–7007) ─────────────

        new FloraEntry
        {
            id = 7001, name = "Ghost Fungus", scientific = "Omphalotus nidiformis",
            category = FloraCategory.FUNGUS, subCategory = "Bioluminescent Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "Borneo, Sumatra (Indonesia, Malaysia), Australia",
            habitat = FloraHabitat.DECAYING_WOOD,
            latitude = 3.0, longitude = 118.0, spawnRadiusKm = 3000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Wood decomposer; bioluminescence driven by luciferin-luciferase reaction — " +
                             "exact ecological function of glow still under research (possible spore dispersal lure)",
            significance   = "One of the few genuinely bioluminescent fungi — blue-green glow visible in complete darkness; " +
                             "mimics edible oyster mushroom appearance — key toxic lookalike education plant",
            edible = "TOXIC — causes severe gastroenteritis despite inviting appearance",
            assetAnimation = FloraAssetAnimation.ANIMATED_GLOW_PULSE,
            size = "Cap 5–15 cm; clustered on stumps and logs",
            glowLocation = "Entire mushroom surface — vivid blue-green self-luminescence from gills and cap underside",
            teamlabMood   = "Entire mushroom",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7002, name = "Lingzhi / Reishi", scientific = "Ganoderma lucidum",
            category = FloraCategory.FUNGUS, subCategory = "Medicinal Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "Southeast Asia, China, Japan, Korea (pan-East Asian)",
            habitat = FloraHabitat.DECAYING_WOOD,
            latitude = 5.0, longitude = 115.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Wood rot decomposer on hardwood stumps; bracket form persists for years; spores wind-dispersed",
            significance   = "Sacred mushroom of immortality — 4,000 years in Chinese materia medica; " +
                             "polysaccharide beta-glucans studied for immune modulation; " +
                             "found in Malay traditional medicine (kulat susu) and Chinese TCM alike",
            edible = "Medicinal (tea, extract) — not culinary; too tough and bitter to eat directly",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 5–20 cm; kidney-shaped lacquered bracket",
            glowLocation = "Deep red-gold lacquer cap surface; concentric ring shimmer in Veiled World light",
            teamlabMood   = "Cap surface",
            interactionMode = FloraInteractionMode.QUIET_FOREST,
        },

        new FloraEntry
        {
            id = 7003, name = "Shiitake", scientific = "Lentinula edodes",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "Malaysia, Indonesia (wild); Japan, China (cultivated)",
            habitat = FloraHabitat.HARDWOOD_LOG,
            latitude = 5.0, longitude = 118.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Hardwood decomposer; mycelium breaks down lignin in dead shiia, oak and beech logs",
            significance   = "World's second most cultivated mushroom; rich umami (glutamate + guanylate synergy); " +
                             "lentinan beta-glucan studied for immune support; central to Japanese and Chinese cuisine",
            edible = "Yes — cap (sauté, dried, dashi stock); gills and stem usable",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 5–15 cm; broad brown umbrella cap with white veil remnants",
            glowLocation = "Warm amber-brown cap with faint inner glow at veil ring",
            teamlabMood   = "Cap surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7004, name = "Wood Ear / Black Fungus", scientific = "Auricularia auricula-judae",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "Southeast Asia, China, Korea, Japan (pan-Asian)",
            habitat = FloraHabitat.DECAYING_WOOD,
            latitude = 20.0, longitude = 110.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Jelly fungus — decomposes wet elder and hardwood; fruits year-round in humid conditions",
            significance   = "Mok yee / mu'er — staple of Chinese hot and sour soup, claypot dishes, wood ear salad; " +
                             "gelatinous texture unique in Asian cuisine; blood-thinning compounds studied medicinally",
            edible = "Yes — gelatinous jelly texture when soaked; virtually flavourless — absorbs broth",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "3–12 cm ear-shaped lobes; clusters on wet dead wood",
            glowLocation = "Translucent deep-brown ear lobe with rim luminescence like backlit amber glass",
            teamlabMood   = "Mushroom rim",
            interactionMode = FloraInteractionMode.LIVING_WATER,
        },

        new FloraEntry
        {
            id = 7005, name = "Oyster Mushroom", scientific = "Pleurotus ostreatus",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "Southeast Asia, China, Japan (pan-temperate/subtropical)",
            habitat = FloraHabitat.DECAYING_WOOD,
            latitude = 5.0, longitude = 110.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Aggressive wood decomposer; carnivorous — mycelium traps and digests nematodes; " +
                             "fast coloniser of dead wood, cardboard, straw",
            significance   = "Easiest cultivated mushroom — colonises straw in 10 days; " +
                             "used in myco-remediation of oil spills and polluted soil; " +
                             "fan-shaped fruitbodies emerge in dramatic clusters",
            edible = "Yes — delicate flavour; gills, cap, stem all edible",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 5–20 cm; overlapping fan clusters",
            glowLocation = "Pale grey-white fan shimmer with silver-blue gill underside glow",
            teamlabMood   = "Cap fan surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7006, name = "Bamboo Fungus / Stinkhorn", scientific = "Phallus indusiatus",
            category = FloraCategory.FUNGUS, subCategory = "Exotic Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "China, Southeast Asia, India (tropical forests)",
            habitat = FloraHabitat.FOREST_FLOOR,
            latitude = 23.0, longitude = 108.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Emerges from egg in hours; carrion-scent gleba attracts flies for spore dispersal; " +
                             "lacy net veil (indusium) extends rapidly downward from cap",
            significance   = "Chinese delicacy — dried veil skirt used in soups; " +
                             "Yunnan mushroom markets sell it fresh and dried; dramatic emergence watched in time-lapse; " +
                             "called 'the veil lady' in China for its ethereal lace-net skirt",
            edible = "Yes — dried veil skirt in soups and stir-fries; young egg stage also edible",
            assetAnimation = FloraAssetAnimation.ANIMATED_EMERGENCE,
            size = "15–25 cm tall; white lace veil drapes 10–15 cm below cap",
            glowLocation = "White lace-veil net aglow — translucent lacework skirt in forest dim",
            teamlabMood   = "Lattice veil skirt",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7007, name = "Fly Agaric (Asian)", scientific = "Amanita muscaria",
            category = FloraCategory.FUNGUS, subCategory = "Toxic Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.SEA,
            countries = "Pan-Asia (boreal zones, mountain forest)",
            habitat = FloraHabitat.FOREST_FLOOR,
            latitude = 50.0, longitude = 100.0, spawnRadiusKm = 4000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Obligate mycorrhizal partner of birch, pine, spruce — cannot grow without host tree; " +
                             "mycelium extends tree root network; spores dispersed by small mammals",
            significance   = "Most iconic mushroom in human history — appears in fairy tales, Alice in Wonderland, " +
                             "Siberian shamanic ceremonies; ibotenic acid / muscimol toxins cause delirium; " +
                             "fly-killing use (milk + mushroom = insecticide, hence 'fly agaric')",
            edible = "TOXIC — muscimol / ibotenic acid cause hallucination, nausea; used historically in shamanic rituals",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 8–20 cm; brilliant scarlet with white wart spots",
            glowLocation = "Red cap with white spot-ring vivid glow; crimson bioluminescent pulse in Veiled World",
            teamlabMood   = "Cap spots",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        // ── BATCH 03: MUSHROOMS — EAST ASIA (ID 7011) ─────────────

        new FloraEntry
        {
            id = 7011, name = "Shiitake (cultivated)", scientific = "Lentinula edodes",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.EAST_ASIA,
            countries = "Japan, China, Korea (primary cultivation zones)",
            habitat = FloraHabitat.HARDWOOD_LOG,
            latitude = 35.0, longitude = 136.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.CULTIVATED,
            ecologicalRole = "Cultivated on oak (konara) and shiia logs; mycelium breaks down heartwood",
            significance   = "Donko shiitake — Japanese premium thick-capped winter variety; " +
                             "dashi broth base; central to Buddhist shojin ryori (temple cuisine); " +
                             "first mushroom intentionally cultivated (Song Dynasty China, 960–1279 CE)",
            edible = "Yes — cap; dried shiitake has concentrated umami exceeding fresh",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 5–15 cm; thick donko domed form",
            glowLocation = "Warm amber-brown cap glow with pale cream gill undersurface",
            teamlabMood   = "Cap surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        // ── BATCH 03: MUSHROOMS — CENTRAL ASIA (IDs 7021–7024) ────

        new FloraEntry
        {
            id = 7021, name = "Fly Agaric", scientific = "Amanita muscaria",
            category = FloraCategory.FUNGUS, subCategory = "Toxic Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.CENTRAL_ASIA,
            countries = "Russia, Kazakhstan, Kyrgyzstan (boreal and mountain forests)",
            habitat = FloraHabitat.BOREAL_MOUNTAIN_FOREST,
            latitude = 52.0, longitude = 80.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Mycorrhizal obligate with birch and pine; cannot grow in disturbed habitats without intact tree network",
            significance   = "Siberian shamanic use — Khanty, Evenki, Chukchi traditions use it for vision quests; " +
                             "reindeer seek it and consume urine of those who have eaten it (ibotenic acid concentrates); " +
                             "possible origin of Santa Claus myth (red+white, reindeer, flying sensation)",
            edible = "TOXIC — muscimol hallucinations; NOT safe to consume",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 8–20 cm; brilliant red with white spots",
            glowLocation = "Crimson cap with white spot ring radiating glow; taiga forest floor phosphor pulse",
            teamlabMood   = "Cap spots",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7022, name = "Porcini / Cep", scientific = "Boletus edulis",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.CENTRAL_ASIA,
            countries = "Russia, Kazakhstan, Kyrgyzstan (boreal forest)",
            habitat = FloraHabitat.FOREST_FLOOR,
            latitude = 50.0, longitude = 75.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Mycorrhizal with spruce, pine, oak and beech; indicator of old-growth forest health; " +
                             "sponge pore-bearing (bolete) not gilled — distinct evolutionary lineage",
            significance   = "King of mushrooms — most prized edible in European and Central Asian tradition; " +
                             "buttery flavour; dried form intensifies to deep earth and hazelnut; " +
                             "key ingredient in risotto porcini and Russian mushroom soup",
            edible = "Yes — cap and pores (not stem base if infested); superb dried",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 8–25 cm; broad brown bun cap with ivory sponge pores below",
            glowLocation = "Deep brown cap with ivory pore underside glow; forest floor amber leaf-litter haze",
            teamlabMood   = "Cap surface",
            interactionMode = FloraInteractionMode.QUIET_FOREST,
        },

        new FloraEntry
        {
            id = 7023, name = "Morel", scientific = "Morchella esculenta",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.CENTRAL_ASIA,
            countries = "Kazakhstan, Uzbekistan, Kyrgyzstan (spring woodland)",
            habitat = FloraHabitat.WOODLAND_FIELD,
            latitude = 43.0, longitude = 68.0, spawnRadiusKm = 1500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Spring ephemeral; associated with elm, ash, old apple orchards; " +
                             "sac fungus (ascomycete) — entirely different lineage from gilled mushrooms",
            significance   = "Most prized spring mushroom in Central Asian and European tradition; " +
                             "honeycomb hollow cap; nutty flavour; cannot be cultivated commercially; " +
                             "appears after snow melt — fleeting spring harvest that demands knowledge",
            edible = "Yes — cooked only (raw toxic); must be thoroughly heated",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 5–20 cm; conical honeycomb ridged cap on hollow stem",
            glowLocation = "Honeycomb gold-amber cap grid glow; spring forest light catching each cell",
            teamlabMood   = "Cap honeycomb",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7024, name = "Yellow Chanterelle", scientific = "Cantharellus cibarius",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.CENTRAL_ASIA,
            countries = "Russia, Kazakhstan (boreal forest, old growth)",
            habitat = FloraHabitat.FOREST_FLOOR,
            latitude = 52.0, longitude = 73.0, spawnRadiusKm = 2000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Mycorrhizal indicator of old-growth forest — disappears when forest is disturbed; " +
                             "false gills (forking ridges) distinguish it from toxic lookalikes; " +
                             "associated with spruce, pine, beech and oak",
            significance   = "Apricot aroma and flavour; bright egg-yolk yellow; old forest sentinel — " +
                             "a patch of chanterelles marks an intact ecosystem; " +
                             "major export from Russia and Scandinavia to European fine-dining markets",
            edible = "Yes — false-gilled cap and stem; best with butter",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 4–12 cm; wavy-edged egg-yolk funnel",
            glowLocation = "Vivid yellow-gold funnel cap shimmer; entire mushroom lit from within like amber glass",
            teamlabMood   = "Cap + gills",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        // ── BATCH 03: MUSHROOMS — NORTH ASIA (IDs 7031–7033) ───────

        new FloraEntry
        {
            id = 7031, name = "Chaga Mushroom", scientific = "Inonotus obliquus",
            category = FloraCategory.FUNGUS, subCategory = "Medicinal Fungus",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.NORTH_ASIA,
            countries = "Russia (Siberia, Ural, Karelia), Finland, Canada",
            habitat = FloraHabitat.BIRCH_FOREST,
            latitude = 62.0, longitude = 70.0, spawnRadiusKm = 3000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Parasitic on living birch — causes white heart rot; the visible black mass is sterile mycelium; " +
                             "true fertile layer forms beneath birch bark when tree dies; may take 20 years to produce",
            significance   = "Siberian immune tonic — Khanty and Mansi peoples brew birch mushroom tea for centuries; " +
                             "Alexander Solzhenitsyn's novel Cancer Ward mentions chaga tea; " +
                             "betulinic acid (from birch) and melanin studied for anti-tumour properties",
            edible = "Medicinal (tea, tincture) — black outer crust + orange interior ground into powder",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "10–50 cm irregular black mass on birch trunk",
            glowLocation = "Deep black-orange charcoal outer crust glow; warm gold-amber interior crack luminescence",
            teamlabMood   = "Outer crust + crack",
            interactionMode = FloraInteractionMode.QUIET_FOREST,
        },

        new FloraEntry
        {
            id = 7032, name = "King Bolete / Cep", scientific = "Boletus edulis",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.NORTH_ASIA,
            countries = "Russia (Siberia, Ural, Far East)",
            habitat = FloraHabitat.BOREAL_MOUNTAIN_FOREST,
            latitude = 58.0, longitude = 65.0, spawnRadiusKm = 3000f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Mycorrhizal with Siberian spruce, pine and larch; indicator of forest health in boreal biome; " +
                             "Russia's most exported wild mushroom (dried form) to Europe and Japan",
            significance   = "Beliy grib — 'white mushroom' of Russia; national foraging obsession; " +
                             "September mushroom season brings entire Russian families into the taiga; " +
                             "dried version intensifies to concentrated earth, butter and nut aromas",
            edible = "Yes — all parts edible; superb dried; no toxic lookalikes when pores are white/yellow",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 8–25 cm; fat bun-shaped brown cap; white pore sponge",
            glowLocation = "Brown cap with ivory pore sponge glow; birch-dappled taiga light shimmer",
            teamlabMood   = "Cap surface",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        new FloraEntry
        {
            id = 7033, name = "Saffron Milk Cap", scientific = "Lactarius deliciosus",
            category = FloraCategory.FUNGUS, subCategory = "Edible Mushroom",
            gameCategory = FloraGameCategory.MUSHROOMS, region = FloraRegion.NORTH_ASIA,
            countries = "Russia, Kazakhstan (pine-forest zones)",
            habitat = FloraHabitat.PINE_FOREST,
            latitude = 55.0, longitude = 80.0, spawnRadiusKm = 2500f,
            conservation = ConservationStatus.LEAST_CONCERN,
            ecologicalRole = "Mycorrhizal obligate with Scots pine and Siberian pine; cannot grow without pine root network; " +
                             "when damaged, oozes saffron-orange latex (milk) — distinctive field identifier",
            significance   = "Ryzhik — Russia's most beloved pickled mushroom; " +
                             "vivid orange-saffron colour throughout cap, gills and milk; " +
                             "pickled cold without cooking — traditional Siberian preservation method; " +
                             "sold at forest roadside stalls across the taiga belt",
            edible = "Yes — raw (pickled in salt), sautéed, roasted; gills turn greenish when old",
            assetAnimation = FloraAssetAnimation.STATIC,
            size = "Cap 5–15 cm; depressed orange cap with in-rolled rim; saffron-dripping gills",
            glowLocation = "Vivid orange-saffron cap shimmer; dripping milk glow from cut gill surface",
            teamlabMood   = "Cap + gills",
            interactionMode = FloraInteractionMode.FULL_IMMERSION,
        },

        // ── BATCH 04: HERBS — SEA (IDs 8001–8013) ─────────────────

        new FloraEntry { id=8001, name="Lemongrass", scientific="Cymbopogon citratus",
            category=FloraCategory.GRASS, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide", habitat=FloraHabitat.SECONDARY_GROWTH,
            latitude=13.0, longitude=100.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Aromatic tussock grass; natural insect repellent (citronellal); borders field habitats",
            significance="Essential SEA culinary herb — tom yum, rendang, lemongrass tea; citronella oil; fever and pain remedy",
            edible="Yes — stalk base (bruised for cooking); leaf tea",
            assetAnimation=FloraAssetAnimation.ANIMATED_FIELD_SWAY, size="Tussock 1–2 m",
            glowLocation="Lemon-yellow shimmer at stalk base where essential oil concentrates",
            teamlabMood="Leaf base", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8002, name="Galangal / Lengkuas", scientific="Alpinia galanga",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide", habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=5.0, longitude=102.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Rhizome groundcover; flowers attract insects; creates dense clumping habitat",
            significance="Foundation spice of rendang, laksa, tom kha gai — SEA's dominant rhizome; " +
                         "sharper and more aromatic than ginger; rhizome harvested year-round",
            edible="Yes — rhizome (sliced, bruised, ground)",
            assetAnimation=FloraAssetAnimation.ANIMATED_LEAF_UNFURL, size="1–2 m; red-white flowers",
            glowLocation="Warm amber glow on rhizome cross-section underground",
            teamlabMood="Underground rhizome", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8003, name="Kaffir Lime / Limau Purut", scientific="Citrus hystrix",
            category=FloraCategory.TREE, subCategory="Culinary Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide", habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=14.0, longitude=102.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Host plant for Lime Butterfly (Papilio demoleus) larvae; citrus flowers attract bees",
            significance="Iconic double-leaf citrus — unique hourglass double-lobed leaf shape; " +
                         "leaves used in Thai, Malay and Indonesian cooking; zest in curry paste; " +
                         "fruit rind for hair and medicinal infusions",
            edible="Yes — leaves (cooking), zest (curry paste); juice used medicinally",
            assetAnimation=FloraAssetAnimation.STATIC, size="Small tree 3–6 m",
            glowLocation="Bright green paired-leaf vein glow; leaf junction point shimmer",
            teamlabMood="Leaf veins", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8004, name="Turmeric", scientific="Curcuma longa",
            category=FloraCategory.PLANT, subCategory="Medicinal/Culinary",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide", habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=10.0, longitude=100.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Rhizome ground cover; pale yellow flowers attract pollinators; enriches garden soil",
            significance="Golden spice of SEA — nasi kuning (yellow rice), kunyit-tamarind paste, jamu tonic; " +
                         "curcumin anti-inflammatory widely studied; natural yellow dye for textiles",
            edible="Yes — rhizome (ground, fresh); leaves for wrapping",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1 m; decorative lily-like foliage",
            glowLocation="Deep saffron-orange rhizome glow underground; golden shimmer through soil",
            teamlabMood="Underground rhizome", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8005, name="Ginger", scientific="Zingiber officinale",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide (cultivated pan-tropical)",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=5.0, longitude=115.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Rhizome plant; bee-pollinated; rotting rhizome enriches tropical soil",
            significance="Universal spice of SEA — ginger tea, jahe, bao ginger drink; " +
                         "anti-nausea and anti-emetic properties; Silk Road's original 'hot spice'; " +
                         "candied ginger, ginger beer, gingerbread all trace to this single rhizome",
            edible="Yes — rhizome (fresh, dried, powdered, candied)",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1 m; tropical foliage",
            glowLocation="Warm ginger-amber rhizome glow; heat-shimmer emanating from root",
            teamlabMood="Rhizome", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8006, name="Pandan / Screwpine", scientific="Pandanus amaryllifolius",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide", habitat=FloraHabitat.SANDY_COASTAL,
            latitude=3.0, longitude=101.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Coastal and riparian ground stabiliser; leaves used as natural food wrapping",
            significance="Vanilla of Southeast Asia — 2-acetyl-1-pyrroline aroma (same compound as basmati rice); " +
                         "pandan layer cake, onde-onde, kuih, rice dishes; natural green dye; leaf basket weaving",
            edible="Yes — leaves (cooking, aroma, wrapping, colouring)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="1–2 m; long arching spiralled leaves",
            glowLocation="Vivid jade-green parallel vein lines running full leaf length",
            teamlabMood="Leaf vein lines", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8007, name="Betel Leaf / Sirih", scientific="Piper betle",
            category=FloraCategory.VINE, subCategory="Medicinal Vine",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide (India to Pacific)",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=12.0, longitude=100.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Climbing vine on trees or support; moist tropical understorey; insect habitat",
            significance="Central to Malay adat ceremony — sirih junjung wedding offering; " +
                         "betel quid (sirih + gambir + pinang + lime) chewed across Asia for 3,000 years; " +
                         "mouth antiseptic properties; deeply embedded in cultural ceremony",
            edible="Medicinal/ceremonial — chewed with betel nut; leaves have mild stimulant properties",
            assetAnimation=FloraAssetAnimation.ANIMATED_VINE_CLIMB, size="Climbing vine 1–10 m",
            glowLocation="Deep forest-green heart-shaped leaf pulse; ceremonial silver-green shimmer",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=8008, name="Thai Basil / Selasih", scientific="Ocimum basilicum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Thailand, Malaysia, Indonesia, Vietnam",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=15.0, longitude=101.0, spawnRadiusKm=1800f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; companion planting pest deterrent; attracts beneficial insects",
            significance="Essential aromatic in pho bo, Thai green curry, Vietnamese fresh spring rolls; " +
                         "anise-clove flavour profile distinct from Italian basil; " +
                         "purple-stemmed varieties used in Malaysian bunga raya ceremonies",
            edible="Yes — leaves (fresh, cooked); seeds (biji selasih in drinks)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm; purple-green aromatic",
            glowLocation="Soft purple-green shimmer on leaves; purple stem glow beneath",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8009, name="Moringa / Kelor", scientific="Moringa oleifera",
            category=FloraCategory.TREE, subCategory="Superfood Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide (pan-tropical; originated India)",
            habitat=FloraHabitat.DRY_TROPICAL,
            latitude=10.0, longitude=78.0, spawnRadiusKm=3000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Fast-growing pioneer; nitrogen-fixer; seeds purify water via coagulation",
            significance="Miracle tree — every part edible or useful; leaves (gram-for-gram) higher protein than " +
                         "eggs, Vitamin C than oranges, calcium than milk; drumstick pods (sai khua); " +
                         "daun kelor (moringa leaves) in Javanese ritual cleansing of the deceased",
            edible="Yes — leaves, pods (drumsticks), flowers, seeds, oil (ben oil)",
            assetAnimation=FloraAssetAnimation.ANIMATED_CANOPY_RUSTLE, size="5–12 m; feathery compound leaves",
            glowLocation="Pale gold shimmer on long hanging seed pods; delicate feather-leaf glow",
            teamlabMood="Seed pods", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8010, name="Ulam Raja / King Salad", scientific="Cosmos caudatus",
            category=FloraCategory.PLANT, subCategory="Wild Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Malaysia, Indonesia", habitat=FloraHabitat.SECONDARY_GROWTH,
            latitude=3.0, longitude=110.0, spawnRadiusKm=800f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Pioneer plant on disturbed ground; insect-pollinated pink daisy flowers attract butterflies",
            significance="Traditional Malaysian wild salad herb — eaten raw with sambal; " +
                         "high quercetin antioxidant; mild tangy flavour; endemic Malay-Indonesian culinary tradition; " +
                         "'king salad' for its position of honour at traditional Malay feast tables",
            edible="Yes — leaves (raw salad, ulam), young shoots",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1.5 m; pink cosmos-like flowers",
            glowLocation="Soft pink flower glow + bright green young leaf shimmer",
            teamlabMood="Flower petals", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8011, name="Pegaga / Gotu Kola", scientific="Centella asiatica",
            category=FloraCategory.PLANT, subCategory="Medicinal Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Southeast Asia-wide (pan-tropical)",
            habitat=FloraHabitat.RIPARIAN,
            latitude=5.0, longitude=103.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Ground cover on moist banks and roadsides; colonises disturbed wet areas",
            significance="Traditional memory and brain tonic herb — penghijau otak (greening the brain); " +
                         "Ayurvedic and TCM memory herb; asiaticoside compounds studied for wound healing; " +
                         "pegaga juice popular street drink in Malaysia; served fresh in salad",
            edible="Yes — leaves (raw salad, juice drink)",
            assetAnimation=FloraAssetAnimation.STATIC, size="5–15 cm creeping mat",
            glowLocation="Pale jade circular coin-shaped leaf glow; low ground shimmer",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8012, name="Curry Leaf (SEA)", scientific="Murraya koenigii",
            category=FloraCategory.TREE, subCategory="Culinary Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Malaysia, Indonesia, Thailand (South Indian diaspora cultivation)",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=6.0, longitude=100.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Host plant for Lime Butterfly; small white flowers attract bees; berries eaten by birds",
            significance="Fragrant leaf releasing aroma only when bruised — essential in mamak curry, " +
                         "dhal tadka, Malaysian fish curry; brought across Indian Ocean by South Indian diaspora; " +
                         "leaves cannot be dried without losing flavour — must be used fresh",
            edible="Yes — leaves (fresh, tempered in hot oil)",
            assetAnimation=FloraAssetAnimation.STATIC, size="4–6 m aromatic tree",
            glowLocation="Deep forest-green aromatic leaf shimmer; bruise-release glow",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8013, name="Oc Chau Ancient Tea", scientific="Camellia sinensis var.",
            category=FloraCategory.TREE, subCategory="Tea Plant",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SEA,
            countries="Vietnam (Ha Giang, Oc Chau highlands)",
            habitat=FloraHabitat.HIGHLAND_GARDEN,
            latitude=22.5, longitude=104.0, spawnRadiusKm=200f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Highland forest understory; ancient cultivars support diverse insect communities",
            significance="Ancient Vietnamese tea trees over 1,000 years old — wild-harvested in Ha Giang highlands; " +
                         "shan tuyet (snow mountain) teas from centuries-old trees; each tree an heirloom; " +
                         "Vietnamese tea tradition predating recorded history",
            edible="Yes — leaves (green tea, compressed cake tea)",
            assetAnimation=FloraAssetAnimation.STATIC, size="2–5 m ancient cultivar (can reach 15 m wild)",
            glowLocation="Pale jade young tea bud and first-leaf shimmer; morning dew on fresh flush",
            teamlabMood="Young leaves", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── BATCH 04: HERBS — EAST ASIA (IDs 8101–8108) ────────────

        new FloraEntry { id=8101, name="Wasabi", scientific="Eutrema japonicum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="Japan (mountain streams of Honshu, Shikoku, Kyushu)",
            habitat=FloraHabitat.ALPINE_STREAM,
            latitude=35.5, longitude=136.0, spawnRadiusKm=300f,
            conservation=ConservationStatus.NEAR_THREATENED,
            ecologicalRole="Aquatic rhizome plant of pure cold mountain streams; indicator of pristine water quality",
            significance="Most demanding crop in Japanese cuisine — requires 18°C pure running water year-round; " +
                         "isothiocyanate compounds give unique nasal heat; 99% of 'wasabi' worldwide is horseradish dye; " +
                         "real wasabi loses flavour within 15 minutes of grating",
            edible="Yes — rhizome (freshly grated only)",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–60 cm aquatic rhizome plant",
            glowLocation="Bright jade-green rhizome in crystal-clear cold stream; green-white glow",
            teamlabMood="Rhizome", interactionMode=FloraInteractionMode.LIVING_WATER },

        new FloraEntry { id=8102, name="Chinese Chive / Garlic Chive", scientific="Allium tuberosum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="China, Japan, Korea", habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=35.0, longitude=114.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; white star-flower umbels attract bees and hoverflies",
            significance="Jiucai — essential Chinese dumpling (jiaozi) filling with egg and pork; " +
                         "Korean buchu jeon (chive pancake); Japanese nira gyoza; " +
                         "flat-leafed garlic-scented allium; blanched yellow variety (huangjiucai) for soup",
            edible="Yes — leaves, flowers, young buds",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–50 cm; white umbel flowers",
            glowLocation="Pale white flower + vivid green flat leaf shimmer",
            teamlabMood="Leaf + flower", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8103, name="Spring Onion / Scallion", scientific="Allium fistulosum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="China, Japan, Korea (pan-East Asian)", habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=37.0, longitude=120.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Fast-growing allium; insect-pollinated; self-seeding garden plant",
            significance="Universal East Asian garnish and flavouring — ramen negi, pho hanh, " +
                         "bibimbap, mapo tofu; hollow tubular leaf is botanically distinct from onion; " +
                         "originated northeastern Asia; Chinese name 'cong' ancient since Han dynasty",
            edible="Yes — entire plant (leaf, white base, roots)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm",
            glowLocation="Bright hollow-green stalk shimmer; white base glow",
            teamlabMood="Stalk", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8104, name="Perilla / Shiso", scientific="Perilla frutescens",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="Japan, Korea, China", habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=35.0, longitude=137.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; companion planting benefits; self-seeds prolifically",
            significance="Japanese sushi and sashimi herb — green (aoshiso) and purple (akashiso); " +
                         "Korean ssam (wrap leaf) culture; perilla oil from seeds used in Korean cuisine; " +
                         "two-tone purple-green underside unique to this genus; used in umeboshi pickling",
            edible="Yes — leaf (raw, tempura, wrap); seed (oil, spice)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–80 cm",
            glowLocation="Deep purple-green two-tone leaf shimmer; purple underside glow",
            teamlabMood="Leaf surface (2 colour)", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8105, name="Mitsuba / Japanese Parsley", scientific="Cryptotaenia japonica",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="Japan, Korea, China", habitat=FloraHabitat.SECONDARY_GROWTH,
            latitude=36.0, longitude=138.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Shade-tolerant understorey herb; moist forest floor coloniser",
            significance="Mitsuba — 'three leaves'; quintessential Japanese soup and chawanmushi herb; " +
                         "delicate parsley-like flavour with mild anise note; spring seasonal ingredient; " +
                         "appears in traditional New Year zoni soup",
            edible="Yes — leaves, stems",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm",
            glowLocation="Pale green triple-leaf shimmer; delicate translucent leaf glow",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8106, name="Mugwort / Artemisia", scientific="Artemisia argyi",
            category=FloraCategory.PLANT, subCategory="Medicinal Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="China, Japan, Korea", habitat=FloraHabitat.MOUNTAIN_FIELD,
            latitude=36.0, longitude=128.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Hardy pioneer on disturbed mountain slopes; aromatic foliage deters insects",
            significance="Moxa herb — dried mugwort burned in moxibustion acupuncture for 3,000 years; " +
                         "Korean tteok (rice cake) and Japanese mochi use young leaves for green colour and flavour; " +
                         "silver-grey leaf undersurface distinctive; associated with Dragon Boat Festival",
            edible="Yes — young leaves (rice cake, soup); dried for moxa therapy",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="0.5–1.5 m; silver-grey aromatic",
            glowLocation="Silver-grey aromatic leaf shimmer; silvery underside in moonlight",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8107, name="Sichuan Pepper", scientific="Zanthoxylum simulans",
            category=FloraCategory.TREE, subCategory="Spice Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="China (Sichuan, Yunnan)", habitat=FloraHabitat.BOREAL_MOUNTAIN_FOREST,
            latitude=30.0, longitude=104.0, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; fruit eaten by birds; prickly stems provide bird nesting cover",
            significance="Hua jiao — not a true pepper; produces unique numbing sensation (mala) from hydroxy-alpha-sanshool; " +
                         "Silk Road luxury spice; key to mapo tofu and dan dan noodles; " +
                         "aroma of citrus + pine + pepper combined",
            edible="Yes — dried pericarp (husk only, not seed)",
            assetAnimation=FloraAssetAnimation.STATIC, size="2–7 m thorny shrub or small tree",
            glowLocation="Vivid red-pink pepper hull shimmer; numbing-sensation node visualised as sparking glow",
            teamlabMood="Hull", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8108, name="Star Anise", scientific="Illicium verum",
            category=FloraCategory.TREE, subCategory="Spice Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.EAST_ASIA,
            countries="China (Guangxi, Yunnan), Vietnam", habitat=FloraHabitat.SUBTROPICAL_FOREST,
            latitude=22.0, longitude=107.0, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Subtropical forest understory; insect-pollinated cream flowers; fruit eaten by birds",
            significance="Chinese five-spice star — geometric 8-pointed star fruit iconic; " +
                         "primary source of shikimic acid used to synthesise Tamiflu antiviral; " +
                         "pho broth, char siu marinade, mulled wine — anethole aroma spans Asian and European cuisine",
            edible="Yes — dried star fruit/spice; oil for medicine and flavouring",
            assetAnimation=FloraAssetAnimation.STATIC, size="6–15 m; waxy leaves",
            glowLocation="Deep red-brown star-shaped fruit with eight-point light rays; geometric glow",
            teamlabMood="Star fruit", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── BATCH 04: HERBS — SOUTH ASIA (IDs 8201–8215) ───────────

        new FloraEntry { id=8201, name="Cardamom", scientific="Elettaria cardamomum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Kerala, Western Ghats), Sri Lanka",
            habitat=FloraHabitat.SECONDARY_GROWTH,
            latitude=10.5, longitude=77.0, spawnRadiusKm=400f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Forest floor shade crop; tubular flowers pollinated by specialist bees; " +
                           "pods attract birds and small mammals",
            significance="Queen of spices — Kerala's most valuable crop; " +
                         "chai masala, biryani, halva, Nordic cardamom buns; " +
                         "GI-protected Idukki (Kerala) cardamom; " +
                         "third most expensive spice after saffron and vanilla",
            edible="Yes — seeds and pod (ground, whole in cooking)",
            assetAnimation=FloraAssetAnimation.STATIC, size="2–4 m; tropical ginger family",
            glowLocation="Warm green seed pod cluster glow near base of stem",
            teamlabMood="Pods", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8202, name="Coriander", scientific="Coriandrum sativum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India, Pakistan, Bangladesh, Sri Lanka",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=22.0, longitude=78.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; companion plant deterring aphids; lacy white flowers attract hoverflies",
            significance="Dhaniya — both leaf and seed used in Indian cooking unlike most herbs; " +
                         "seed = aromatic earthy flavour (cumin's partner); leaf = fresh green brightness; " +
                         "found in Egyptian tombs — 3,500 years of continuous cultivation",
            edible="Yes — leaves (fresh), seeds (whole, ground), roots",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm; white lacy umbels",
            glowLocation="Bright green leaf glow + white umbel flower shimmer",
            teamlabMood="Leaf + flower", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8203, name="Cumin", scientific="Cuminum cyminum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Rajasthan, Gujarat), Pakistan, Iran",
            habitat=FloraHabitat.DRY_TROPICAL,
            latitude=24.0, longitude=72.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; companion plant; drought-adapted short-season crop",
            significance="Jeera — most used spice in Indian cooking after chilli; " +
                         "biryani, dal, sabzi — cumin tempering (tadka) releases aldehyde aromatics into oil; " +
                         "digestive properties in Ayurveda; India produces 70% of world's cumin",
            edible="Yes — seeds (whole tempered in oil, ground)",
            assetAnimation=FloraAssetAnimation.STATIC, size="20–50 cm; delicate umbellifer",
            glowLocation="Warm amber-gold seed cluster glow; elongated seed shimmer",
            teamlabMood="Seed heads", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8204, name="Fenugreek", scientific="Trigonella foenum-graecum",
            category=FloraCategory.PLANT, subCategory="Culinary/Medicinal",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Rajasthan, Punjab), Pakistan",
            habitat=FloraHabitat.DRY_TROPICAL,
            latitude=25.0, longitude=73.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Nitrogen-fixing legume; companion planting benefit; bee-pollinated",
            significance="Methi — unique bitter-sweet maple-like flavour; methi roti, methi saag, " +
                         "panch phoron blend; Ayurvedic blood-sugar regulation; " +
                         "seeds sprout into methi microgreens eaten fresh; " +
                         "kasuri methi (dried leaf) concentrated flavour for butter chicken sauce",
            edible="Yes — seeds (spice, sprout), leaves (vegetable, dried)",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–60 cm",
            glowLocation="Pale yellow leaf + golden rhomboid seed glow",
            teamlabMood="Leaf + seeds", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8205, name="Holy Basil / Tulsi", scientific="Ocimum tenuiflorum",
            category=FloraCategory.PLANT, subCategory="Sacred Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (pan-subcontinental), Nepal, Sri Lanka",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=20.0, longitude=78.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; aromatic leaves deter mosquitoes; companion plant",
            significance="Most sacred plant of Hinduism — tulsi vivah (marriage ceremony with Vishnu); " +
                         "tulsi pot in every Hindu household courtyard; " +
                         "Ayurvedic adaptogen; eugenol-rich leaves; " +
                         "krishna tulsi (dark purple) and rama tulsi (green) are distinct types",
            edible="Medicinal — leaves chewed raw, made into kadha (decoction), tulsi tea",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm",
            glowLocation="Deep purple-green sacred shimmer; divine aura glow in Veiled World",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8206, name="Curry Leaf (South Asia)", scientific="Murraya koenigii",
            category=FloraCategory.TREE, subCategory="Culinary Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Tamil Nadu, Kerala, Karnataka), Sri Lanka",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=11.0, longitude=78.0, spawnRadiusKm=1000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Lime Butterfly (Papilio demoleus) host plant; small white flowers attract bees; berries feed birds",
            significance="Kadi patta — impossible to recreate South Indian curry without it; " +
                         "tadka in coconut oil with mustard seeds; chettinad cuisine, Kerala fish curry, Sri Lankan kiri hodi; " +
                         "tree must be planted in South Indian homes for culinary self-sufficiency",
            edible="Yes — leaves (tempered fresh); not dried",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="4–6 m",
            glowLocation="Deep forest-green aromatic pinnate leaf shimmer",
            teamlabMood="Leaf", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8207, name="Ginger (South Asia)", scientific="Zingiber officinale",
            category=FloraCategory.PLANT, subCategory="Culinary/Medicinal",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Kerala, Meghalaya), Bangladesh, Sri Lanka",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=10.0, longitude=77.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Rhizome ground cover; tropical forest floor coloniser",
            significance="Adrak (fresh) and sonth (dried) — adrak chai is national drink of India; " +
                         "ginger paste is one of the two bases of North Indian cooking (ginger-garlic paste); " +
                         "Kerala's Wayanad and Meghalaya produce premium rhizome exports; anti-nausea proven in clinical studies",
            edible="Yes — fresh rhizome (grated, juiced, sliced); dried powder",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1 m",
            glowLocation="Warm amber-gold rhizome shimmer; spicy heat aura glow",
            teamlabMood="Rhizome", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8208, name="Turmeric (South Asia)", scientific="Curcuma longa",
            category=FloraCategory.PLANT, subCategory="Sacred/Culinary/Medicinal",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Odisha, Bengal, Tamil Nadu), Bangladesh, Sri Lanka",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=21.0, longitude=86.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Rhizome crop; bee-pollinated tubular flowers; natural soil conditioner",
            significance="Haldi — used in Hindu wedding haldi ceremony applied to bride and groom; " +
                         "curcumin compound most studied natural anti-inflammatory; " +
                         "Odisha's Lakadong turmeric has highest curcumin content (7–12% vs 2–3% typical); " +
                         "used as thread-yellowing dye, face mask, and antibacterial",
            edible="Yes — fresh rhizome, dried powder; leaves for wrapping and flavouring",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1 m",
            glowLocation="Deep saffron-gold rhizome glow; warm ceremonial gold shimmer",
            teamlabMood="Rhizome", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8209, name="Black Pepper", scientific="Piper nigrum",
            category=FloraCategory.VINE, subCategory="Spice Vine",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Kerala — Malabar origin), Sri Lanka",
            habitat=FloraHabitat.TROPICAL_GARDEN,
            latitude=10.0, longitude=76.0, spawnRadiusKm=500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Climbing vine on support trees; insect-pollinated; berries eaten by birds",
            significance="Black gold — drove the entire Age of Exploration; Vasco da Gama reached Calicut (1498) for pepper; " +
                         "Malabar pepper the original Silk Road luxury; same berry = green, black, white or red pepper " +
                         "depending on harvest stage; piperine compound responsible for heat",
            edible="Yes — berry at all stages (green, black, white, red)",
            assetAnimation=FloraAssetAnimation.ANIMATED_VINE_CLIMB, size="Climbing 4–8 m vine",
            glowLocation="Black-red berry cluster shimmer; ancient spice trade aura glow",
            teamlabMood="Berry cluster", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8210, name="Kashmiri Saffron", scientific="Crocus sativus",
            category=FloraCategory.FLOWER, subCategory="Spice",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Kashmir Valley — Pampore crocus fields)",
            habitat=FloraHabitat.ALPINE_MEADOW,
            latitude=34.0, longitude=74.5, spawnRadiusKm=100f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated autumn crocus; carpets Kashmir meadows in October-November bloom",
            significance="World's most expensive spice by weight (€10,000/kg); " +
                         "Kashmir saffron GI-protected; three red stigmas hand-picked at dawn from each flower; " +
                         "75,000 flowers = 1 pound of dried saffron; used in noon chai, Kashmiri wazwan, Persian rice",
            edible="Yes — dried stigmas (colour, aroma, flavour in rice, dessert, tea)",
            assetAnimation=FloraAssetAnimation.ANIMATED_BLOOM, size="10–30 cm; autumn-blooming purple crocus",
            glowLocation="Deep violet petal glow with gold-red stamen radiance; Kashmir meadow autumn shimmer",
            teamlabMood="Stamen + petals", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8211, name="Cinnamon (Ceylon)", scientific="Cinnamomum verum",
            category=FloraCategory.TREE, subCategory="Spice Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="Sri Lanka (origin), southern India",
            habitat=FloraHabitat.SUBTROPICAL_FOREST,
            latitude=7.5, longitude=80.7, spawnRadiusKm=300f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; berries eaten by birds; shade tree in agroforestry",
            significance="True cinnamon — Ceylon cinnamon vs cassia (Chinese) has thinner, more delicate bark; " +
                         "Silk Road prize; Portuguese fought for Sri Lanka control (1505) specifically for cinnamon; " +
                         "inner bark peeled and sun-dried into quills; low coumarin levels (safer for daily use)",
            edible="Yes — inner bark (quills, ground); leaf oil; unripe berry flavouring",
            assetAnimation=FloraAssetAnimation.ANIMATED_BARK_PEEL, size="8–15 m; aromatic bark",
            glowLocation="Warm cinnamon-brown bark curl shimmer; inner pale-gold bark layer glow",
            teamlabMood="Bark peeling", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8212, name="Ashwagandha", scientific="Withania somnifera",
            category=FloraCategory.PLANT, subCategory="Ayurvedic Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Rajasthan, Madhya Pradesh), Pakistan, Sri Lanka",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=20.0, longitude=76.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Dry scrub pioneer; small orange-red berry attracts birds; deep root in rocky soil",
            significance="Ashwagandha = 'smell of horse' (promises strength of stallion); " +
                         "Ayurvedic rasayana (rejuvenating tonic) for 3,000 years; " +
                         "withanolide compounds studied for cortisol reduction (adaptogen); " +
                         "now the world's fastest-growing herbal supplement",
            edible="Medicinal — root powder in warm milk (ashwagandha golden milk)",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–150 cm; dry scrub shrub",
            glowLocation="Pale green-gold root aura + small orange-red berry shimmer in dry scrub",
            teamlabMood="Root + berry", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8213, name="Neem", scientific="Azadirachta indica",
            category=FloraCategory.TREE, subCategory="Medicinal Tree",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (pan-subcontinent), Sri Lanka, Pakistan",
            habitat=FloraHabitat.DRY_TROPICAL,
            latitude=23.0, longitude=79.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Air purifier; shade tree; natural pesticide (azadirachtin repels 200+ insect species); " +
                           "nitrogen-fixing leaf litter enriches soil",
            significance="Village pharmacy tree — neem twig toothbrush (datun); neem oil pesticide; " +
                         "neem leaf medicinal; antifungal bark; air purification under neem canopy; " +
                         "Indian patent battles over traditional knowledge (neem patent controversy 1994)",
            edible="Medicinal — leaves (bitter tonic), oil (topical), bark (infusion); not culinary",
            assetAnimation=FloraAssetAnimation.STATIC, size="15–20 m",
            glowLocation="Silver-green aromatic leaf medicinal shimmer; anti-pest aura field",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=8214, name="Indian Mustard (field)", scientific="Brassica juncea",
            category=FloraCategory.PLANT, subCategory="Oil Crop / Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Punjab, Haryana, UP), Bangladesh, Pakistan",
            habitat=FloraHabitat.TEMPERATE_FIELD,
            latitude=28.0, longitude=76.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Winter season oil crop; yellow flowers blanket fields attracting bees; " +
                           "nectar source in cool months",
            significance="Sarson — yellow mustard fields of Haryana and Punjab; Lohri winter festival; " +
                         "sarson da saag (mustard greens) with makki di roti = Punjab's most iconic dish; " +
                         "mustard oil (kachi ghani) essential cooking fat of eastern India and Bangladesh",
            edible="Yes — oil, leaves (saag), seeds",
            assetAnimation=FloraAssetAnimation.ANIMATED_FIELD_SWAY, size="0.5–1.5 m",
            glowLocation="Brilliant chrome-yellow flower field shimmer; sun-saturated gold wave",
            teamlabMood="Flower heads", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8215, name="Kashmir Lavender", scientific="Lavandula angustifolia",
            category=FloraCategory.FLOWER, subCategory="Aromatic Flower",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Kashmir Valley — introduced cultivation)",
            habitat=FloraHabitat.MOUNTAIN_VALLEY,
            latitude=34.0, longitude=74.5, spawnRadiusKm=200f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Bee forage in high-altitude meadows; aromatic field reduces insect pests",
            significance="Kashmir lavender cultivation growing since 1980s — now competes with French Provence; " +
                         "purple-blue fragrant fields photographed heavily by tourists; " +
                         "essential oil export; integrated with saffron tourism economy of Pampore region",
            edible="Yes — essential oil (culinary), tea; dried buds as garnish",
            assetAnimation=FloraAssetAnimation.ANIMATED_FIELD_SWAY, size="30–80 cm",
            glowLocation="Vivid purple-blue fragrant field shimmer; lavender haze aura",
            teamlabMood="Flower spike field", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── BATCH 04: HERBS — CENTRAL ASIA (IDs 8301–8310) ─────────

        new FloraEntry { id=8301, name="Wormwood", scientific="Artemisia absinthium",
            category=FloraCategory.PLANT, subCategory="Steppe Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Kyrgyzstan, Mongolia, Russia",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=47.0, longitude=72.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Steppe ground cover; aromatic foliage deters grazing; drought-resistant pioneer",
            significance="Absinthe plant — 19th century absinthe liqueur (Van Gogh, Hemingway); " +
                         "nomadic Central Asian medicine; thujone compound gives intoxicating effect; " +
                         "bitter digestive tonic; scent permeates the Kazakh steppe in summer",
            edible="Medicinal — bitter tonic, digestive; absinthe liqueur base",
            assetAnimation=FloraAssetAnimation.STATIC, size="50–120 cm; silver-grey aromatic shrub",
            glowLocation="Silver-grey aromatic leaf shimmer; bitter ghost-like pale luminescence",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=8302, name="Wild Garlic (origin)", scientific="Allium sativum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Uzbekistan (origin of all cultivated garlic)",
            habitat=FloraHabitat.MOUNTAIN_VALLEY,
            latitude=43.0, longitude=70.0, spawnRadiusKm=800f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Mountain valley ground cover; natural pest deterrent; bee-pollinated globe flowers",
            significance="Origin of all cultivated garlic — wild Allium sativum ancestor in Tian Shan/Fergana mountains; " +
                         "4,500 years cultivated; Egyptian pyramid builders fed garlic; " +
                         "vampires, evil spirits, physicians — garlic appears in every civilization's medicine",
            edible="Yes — bulb, greens, flowers",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–60 cm; small purple globe flower",
            glowLocation="White-gold layered bulb glow underground; origin-point luminescence",
            teamlabMood="Bulb layers", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8303, name="Wild Leek", scientific="Allium ampeloprasum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Kyrgyzstan (mountain meadows)",
            habitat=FloraHabitat.ALPINE_MEADOW,
            latitude=42.0, longitude=73.0, spawnRadiusKm=600f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; mountain meadow grassland ecology",
            significance="Wild ancestor of cultivated leek; Silk Road mountain staple; " +
                         "used by nomadic Kyrgyz peoples in stews; mild onion-garlic flavour; " +
                         "National emblem of Wales (Cymru) traces to this Caucasus-origin wild plant",
            edible="Yes — stem, leaf, bulb (mild)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–90 cm; round globe flower",
            glowLocation="Pale green-white stalk shimmer; round purple-white flower head glow",
            teamlabMood="Stalk", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8304, name="Cumin (Wild)", scientific="Cuminum cyminum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Uzbekistan, Tajikistan, Iran (cultivation origin)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=40.0, longitude=67.0, spawnRadiusKm=1000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; drought-adapted; enriches arid soil structure",
            significance="Zira — the essential spice of Central Asian plov (pilaf); " +
                         "Uzbek plov without zira is unthinkable; Silk Road trade item from Fergana Valley; " +
                         "toasted zira is first step in every plov recipe",
            edible="Yes — seeds (whole, toasted, ground)",
            assetAnimation=FloraAssetAnimation.STATIC, size="20–50 cm; delicate umbellifer",
            glowLocation="Warm amber seed head glow; dry steppe golden shimmer",
            teamlabMood="Seed heads", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8305, name="Wild Coriander", scientific="Coriandrum sativum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Iran (wild form, Silk Road spread)",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=43.0, longitude=75.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; self-seeding on disturbed steppe; attracts parasitic wasps",
            significance="Oldest herb in recorded history — found in Tutankhamun's tomb (1325 BCE); " +
                         "Sanskrit name dhanyaka; spread along all Silk Road routes; " +
                         "seed and leaf have different chemical profiles",
            edible="Yes — leaves (fresh), seeds (aromatic spice)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm",
            glowLocation="White flower + bright green lacy leaf shimmer",
            teamlabMood="Flower + leaf", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8306, name="Dill", scientific="Anethum graveolens",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Russia, Iran (origin region)",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=50.0, longitude=68.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; excellent companion plant for tomatoes and cucumbers; attracts beneficial wasps",
            significance="Feathery aromatic herb of Kazakh, Uzbek and Russian cuisine; " +
                         "pickling agent for cucumbers (dill pickles); Scandinavian gravlax; " +
                         "Persian sabzi herbs mix; anethole and carvone aromatic compounds",
            edible="Yes — feathery leaves (fresh), seeds (spice), flower heads (pickling)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–150 cm; feathery blue-green fronds",
            glowLocation="Pale yellow-green feathery leaf shimmer; delicate frond luminescence",
            teamlabMood="Feathery leaves", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8307, name="Wild Fenugreek", scientific="Trigonella foenum-graecum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Uzbekistan (wild-type origin)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=41.0, longitude=64.0, spawnRadiusKm=1000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Nitrogen-fixer; drought-adapted legume of dry Central Asian habitats",
            significance="Silk Road spice origin — wild form ancestor of all cultivated fenugreek; " +
                         "Uzbek and Tajik bread flavouring (shambalid); ancient Roman cuisine used it; " +
                         "maple-syrup aroma from sotolon compound",
            edible="Yes — seeds (bitter-sweet spice), leaves (fresh herb)",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–60 cm",
            glowLocation="Warm gold seed pod glow; clover-like leaf shimmer",
            teamlabMood="Pods", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8308, name="Wild Mint", scientific="Mentha longifolia",
            category=FloraCategory.PLANT, subCategory="Aromatic Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Central Asia-wide (Kazakhstan, Uzbekistan, Kyrgyzstan)",
            habitat=FloraHabitat.RIPARIAN,
            latitude=46.0, longitude=70.0, spawnRadiusKm=2500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; insect repellent; colonises moist riverbanks and meadows",
            significance="Wild long-leaf mint of the steppe; Silk Road tea herb; " +
                         "ancestor of spearmint cultivation; cool menthol in hot dry climate was prized luxury; " +
                         "Central Asian green tea with mint still served at hospitality ceremonies",
            edible="Yes — leaves (fresh tea, salad)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–80 cm",
            glowLocation="Cool green-white mint leaf shimmer; refreshing cold-light glow",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8309, name="Sorrel", scientific="Rumex acetosa",
            category=FloraCategory.PLANT, subCategory="Edible Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Central Asia-wide (Kazakhstan, Kyrgyzstan, Russia)",
            habitat=FloraHabitat.ALPINE_MEADOW,
            latitude=49.0, longitude=70.0, spawnRadiusKm=3000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Meadow ground cover; oxalic acid deters grazing; seed head feeds birds through winter",
            significance="Sour dock — sharp oxalic acid taste; Kazakh sorpa (soup) uses fresh leaves; " +
                         "Russian shchi (sorrel soup) national dish; " +
                         "emerald-green spring carpet across steppe meadows; high Vitamin C source for nomads",
            edible="Yes — young leaves (raw, soup, salad); sour taste",
            assetAnimation=FloraAssetAnimation.STATIC, size="20–60 cm",
            glowLocation="Bright acid-green leaf glow; spring-shoot vivid emerald shimmer",
            teamlabMood="Leaf", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8310, name="Hemp", scientific="Cannabis sativa",
            category=FloraCategory.PLANT, subCategory="Fibre Crop",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Central Asia (domestication origin)",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=48.0, longitude=73.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Soil improver; deep root aerates compacted steppe soil; seed heads feed birds in winter",
            significance="Industrial hemp — first domesticated 8,000 BCE in Central Asian steppe; " +
                         "oldest known woven fabric (hemp cloth, 8,000 BCE); Silk Road paper and rope; " +
                         "hemp seed oil nutritious; THC-low industrial varieties legally cultivated today",
            edible="Yes — seeds (oil, protein); industrial (fibre, paper); legal varieties only",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="1–3 m; distinctive palm-leaf form",
            glowLocation="Pale green crystalline trichome leaf shimmer; palmate leaf glow",
            teamlabMood="Leaf surface + seed", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── BATCH 04: HERBS — WESTERN ASIA (IDs 8401–8416) ─────────

        new FloraEntry { id=8401, name="Za'atar Thyme", scientific="Thymus spp.",
            category=FloraCategory.PLANT, subCategory="Aromatic Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Lebanon, Israel, Jordan, Syria", habitat=FloraHabitat.DRY_SCRUB,
            latitude=33.0, longitude=35.5, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; aromatic ground cover on limestone hillsides; drought-resistant",
            significance="Za'atar spice blend ingredient — dried thyme, sumac, sesame, salt; " +
                         "Levantine breakfast dip with olive oil; sacred in Islamic tradition; " +
                         "hillside thyme carpets visible from Jerusalem to Beirut in spring bloom",
            edible="Yes — leaves (dried spice blend, fresh tea)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="20–40 cm",
            glowLocation="Pale purple flower shimmer + silver-green aromatic leaf glow",
            teamlabMood="Flower tips + leaves", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8402, name="Giant Fennel", scientific="Ferula communis",
            category=FloraCategory.FLOWER, subCategory="Umbellifer",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Lebanon, Israel, Jordan", habitat=FloraHabitat.DRY_SCRUB,
            latitude=36.0, longitude=35.0, spawnRadiusKm=800f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated giant umbel; hollow stem provides nesting habitat for solitary bees",
            significance="Prometheus myth — fire stolen from the gods carried in the hollow stem of giant fennel; " +
                         "ancient Greek thyrsos (staff) made from it; raw root toxic (furanocoumarins); " +
                         "dried stem used as tinder; dramatic landscape element of Mediterranean hillsides",
            edible="TOXIC raw — root toxic; Medicinal uses only (cooked young shoot in some traditions)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="1–3 m; towering yellow umbel",
            glowLocation="Pale gold flower umbrella shimmer; mythic fire-carrier amber glow",
            teamlabMood="Flower head", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=8403, name="Za'atar Levantine", scientific="Origanum syriacum",
            category=FloraCategory.PLANT, subCategory="Sacred Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Lebanon, Israel, Jordan, Syria (Levant endemic)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=33.5, longitude=36.0, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; aromatic ground cover on rocky limestone slopes",
            significance="The true za'atar plant — Origanum syriacum, not thyme; " +
                         "national herb of Lebanon; za'atar blend uses this; " +
                         "mentioned in Mishnah and possibly the 'hyssop' of Old Testament; " +
                         "harvesting wild za'atar now protected in Israel — over-harvesting threat",
            edible="Yes — leaves (za'atar blend, dried, fresh tea)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm",
            glowLocation="Pale purple-white aromatic leaf shimmer; Levantine hillside glow",
            teamlabMood="Leaf + flower", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8404, name="Sumac", scientific="Rhus coriaria",
            category=FloraCategory.PLANT, subCategory="Spice Shrub",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Lebanon, Syria, Iran, Jordan",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=36.0, longitude=37.0, spawnRadiusKm=1000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bird-dispersed berries; insect-pollinated; drought-tolerant rocky slope coloniser",
            significance="Levantine sour spice — replaced lemon before citrus arrived in the region; " +
                         "fattoush, shawarma, kebab sprinkle; deep crimson berry powder; " +
                         "tanning agent from bark and leaf (sumac = 'red' in Aramaic)",
            edible="Yes — dried berry powder (tart, fruity sour spice)",
            assetAnimation=FloraAssetAnimation.STATIC, size="1–3 m; dense red berry clusters",
            glowLocation="Deep crimson berry cluster glow; sour-light ruby shimmer",
            teamlabMood="Berry clusters", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8405, name="Anise", scientific="Pimpinella anisum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Egypt, Iran (cultivation origin)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=36.0, longitude=32.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated delicate umbellifer; companion planting benefit",
            significance="Anise — arak (Lebanon), ouzo (Greece), sambuca (Italy), pastis (France) all based on this plant; " +
                         "ancient Roman digestive spice; anethole compound identical to star anise; " +
                         "Egyptian and Turkish medicine for 4,000 years",
            edible="Yes — seeds (liqueur flavouring, bread, digestive)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm; feathery umbelliferous",
            glowLocation="White flower + pale gold seed shimmer; anise-sweet glow",
            teamlabMood="Seeds", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8406, name="Cumin (Western Asia)", scientific="Cuminum cyminum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Iran, Turkey, Iraq (core cultivation zone)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=36.0, longitude=45.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; drought-adapted annual",
            significance="Kemoun — Middle Eastern cooking foundation; baharat spice blend; " +
                         "hummus, falafel, shawarma all rely on cumin; Persian cuisine (khoresh, polo); " +
                         "Ayurvedic and Islamic medicine for digestion",
            edible="Yes — seeds (whole, ground, toasted)",
            assetAnimation=FloraAssetAnimation.STATIC, size="20–50 cm",
            glowLocation="Warm amber seed cluster shimmer; spice-market golden glow",
            teamlabMood="Seed head", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8407, name="Coriander (Western Asia)", scientific="Coriandrum sativum",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Iran, Turkey, Lebanon (ancient use)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=35.0, longitude=51.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; self-seeding annual; companion planting",
            significance="Gishnak (Persian) — Persian cuisine cornerstone; ghormeh sabzi herb mix; " +
                         "ancient Egyptian tomb herb; Levantine tabouleh fresh coriander; " +
                         "seed found in 3,000-year-old Hittite archaeological sites in Anatolia",
            edible="Yes — leaves (cilantro), seeds (coriander spice), roots",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–60 cm",
            glowLocation="White flower + leaf green shimmer; ancient-herb glow",
            teamlabMood="Flower + leaf", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8408, name="Fenugreek (Western Asia)", scientific="Trigonella foenum-graecum",
            category=FloraCategory.PLANT, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Iran, Iraq, Egypt (ancient cultivation)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=37.0, longitude=39.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Nitrogen-fixing legume; drought-adapted",
            significance="Hilba — Middle Eastern bread spice (Turkish çemen paste on pastirma); " +
                         "ancient Egyptian medicinal; Arabic qalb helba (heart of fenugreek) tea; " +
                         "Yemenite hilba sauce with zhug for bread dipping",
            edible="Yes — seeds (spice, paste), leaves (herb)",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–60 cm",
            glowLocation="Warm gold seed pod shimmer; maple-aroma spice glow",
            teamlabMood="Pods", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8409, name="Black Cumin / Nigella", scientific="Nigella sativa",
            category=FloraCategory.FLOWER, subCategory="Spice Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Iran, Egypt, Saudi Arabia, Syria",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=37.0, longitude=37.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.CULTIVATED,
            ecologicalRole="Insect-pollinated; ornamental blue flowers attract native bees",
            significance="Habbatus sauda — 'the blessed seed' in authentic Hadith; " +
                         "described as 'cure for everything except death' in Islamic medicine; " +
                         "thymoquinone compound studied for anti-inflammatory properties; " +
                         "sprinkled on naan, Turkish bread, manakish; onion-pepper flavour",
            edible="Yes — tiny black seeds (bread topping, spice blend)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="20–60 cm; delicate blue flower",
            glowLocation="Black seed shimmer nestled in pale blue-white flower; sacred seed glow",
            teamlabMood="Flower + seed", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8410, name="Persian Rose", scientific="Rosa persica",
            category=FloraCategory.FLOWER, subCategory="Wild Rose",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Iran, Afghanistan, Central Asia (endemic)",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=32.0, longitude=52.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; desert and steppe pioneer; thorny stems provide bird habitat",
            significance="Only rose with a yellow centre spot on yellow-white petals — bicolor; " +
                         "parent of Rosa x persica hybrids in modern rose breeding; " +
                         "Persian rose water (golab) different variety — Rosa damascena used for distillation; " +
                         "wild ancestor of garden roses found in arid Iran",
            edible="Decorative; hips nutritious (rose hip tea)",
            assetAnimation=FloraAssetAnimation.ANIMATED_BLOOM, size="0.5–1 m; low spreading",
            glowLocation="Yellow-red bicolour petal shimmer; bright centre-spot radiance",
            teamlabMood="Petals", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8411, name="Garden Sage", scientific="Salvia officinalis",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Lebanon, Levant (cultivated pan-Mediterranean)",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=38.0, longitude=32.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; aromatic grey-green leaves deter herbivores",
            significance="Wisdom herb (salvare = to save in Latin); medieval European and Ottoman medicine; " +
                         "sage honey from Turkish mountain beekeepers; " +
                         "burnt sage (smudging) for purification in multiple traditions; " +
                         "Silk Road medicinal export",
            edible="Yes — leaves (culinary, tea, infusion)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–80 cm; grey-green velvet leaf",
            glowLocation="Silver-green aromatic leaf + purple flower spike shimmer",
            teamlabMood="Leaf + flower", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8412, name="Spearmint / Nana Mint", scientific="Mentha spicata",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Lebanon, Iran, Morocco (pan-Mediterranean/Middle East)",
            habitat=FloraHabitat.RIPARIAN,
            latitude=37.0, longitude=36.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; ground cover on moist stream banks",
            significance="Nana (Arab world) — Moroccan atay bi nana (mint tea) hospitality ritual; " +
                         "Lebanese fattoush and tabouleh use fresh spearmint; " +
                         "Iranian salad herbs (sabzi khordan); gentler than peppermint — suitable for children's tea",
            edible="Yes — leaves (fresh tea, salad, cooking)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="30–80 cm",
            glowLocation="Cool green-white mint leaf shimmer; refreshing blue-green aura",
            teamlabMood="Leaf surface", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8413, name="Salvia fruticosa (Greek Sage)", scientific="Salvia fruticosa",
            category=FloraCategory.PLANT, subCategory="Aromatic Shrub",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Lebanon, Israel, Cyprus, Greece",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=36.5, longitude=34.0, spawnRadiusKm=800f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; aromatic maquis shrub; drought-adapted Mediterranean landscape",
            significance="Greek mountain tea (faskomilo) — most consumed herbal tea in Greece and Turkey; " +
                         "harvested by hand from wild shrubs on Aegean hillsides; " +
                         "three-lobed leaf distinctive; silver-grey leaf carpet on rocky slopes",
            edible="Yes — leaves (tea, infusion)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="0.5–1.5 m",
            glowLocation="Silver-grey leaf + pale lavender flower shimmer; aromatic hillside shimmer",
            teamlabMood="Leaf + flower", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8414, name="Phrygian Sage", scientific="Salvia phlomoides",
            category=FloraCategory.PLANT, subCategory="Aromatic Shrub",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Iran (Anatolian highlands)", habitat=FloraHabitat.DRY_SCRUB,
            latitude=38.0, longitude=36.0, spawnRadiusKm=600f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; pink-purple whorled flowers tower above dry rocky slopes",
            significance="Towering whorled sage of Turkish Phrygian highlands; " +
                         "ancient Phrygian medicinal herb used since Bronze Age Anatolia; " +
                         "dramatic pink-purple flower whorls rising 1 m on grey-green stems; " +
                         "little known outside Turkey but ecologically important",
            edible="Medicinal tea",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="50–120 cm; whorled pink-purple spikes",
            glowLocation="Pink-purple whorl shimmer; grey-green velvet leaf aura",
            teamlabMood="Flower whorls", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8415, name="Phoenician Thyme", scientific="Thymus phoeniceus",
            category=FloraCategory.PLANT, subCategory="Aromatic Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Lebanon, Israel, Syria (coastal Levant endemic)",
            habitat=FloraHabitat.ROCKY_COASTAL,
            latitude=33.0, longitude=35.5, spawnRadiusKm=300f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; coastal rocky cliff endemic; critical nectar source for coastal bees",
            significance="Phoenician coastal thyme — carpets limestone sea-cliffs of Lebanon and Israel; " +
                         "ancient Phoenician sailors used it in navigation ceremonies; " +
                         "rare endemic of the Eastern Mediterranean coast; purple carpet blooms in April",
            edible="Yes — leaves (tea, culinary herb)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="5–20 cm; low coastal carpet",
            glowLocation="Purple flower carpet + grey-green leaf shimmer over coastal cliff",
            teamlabMood="Carpet", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8416, name="Alkanet", scientific="Alkanna tinctoria",
            category=FloraCategory.FLOWER, subCategory="Dye Plant",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Greece, Iran, Syria", habitat=FloraHabitat.DRY_SCRUB,
            latitude=38.0, longitude=31.0, spawnRadiusKm=1500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; deep blue flowers attract specialist bees",
            significance="Ancient Silk Road dye plant — root contains alkannin giving deep red-purple dye; " +
                         "used to colour textiles, cosmetics, wine (Cleopatra's lip colour tradition); " +
                         "vivid deep-blue flowers contrast with red root; Roman and Greek dye trade",
            edible="Medicinal (root dye); not culinary",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="20–60 cm",
            glowLocation="Deep indigo-blue flower shimmer; crimson-red root glow underground",
            teamlabMood="Flower + root", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── BATCH 04: HERBS — NORTH ASIA (IDs 8501–8505) ───────────

        new FloraEntry { id=8501, name="Siberian Ginseng", scientific="Eleutherococcus senticosus",
            category=FloraCategory.PLANT, subCategory="Medicinal Shrub",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.NORTH_ASIA,
            countries="Russia (Amur, Primorsky), China, Korea, Japan",
            habitat=FloraHabitat.BOREAL_MOUNTAIN_FOREST,
            latitude=50.0, longitude=137.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Forest floor shrub; thorny stems provide cover for birds; berries eaten by bears",
            significance="Eleuthero — used by Soviet athletes and cosmonauts as performance adaptogen; " +
                         "different species from true Panax ginseng but similar eleutherosides; " +
                         "Nanai and Udege peoples of Russian Far East traditional medicine; " +
                         "stress resistance tonic studied in Soviet space programme",
            edible="Medicinal — root tincture, tea",
            assetAnimation=FloraAssetAnimation.STATIC, size="1–3 m prickly shrub",
            glowLocation="Soft gold root structure glow beneath forest floor; taiga understory shimmer",
            teamlabMood="Root system", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=8502, name="Wild Garlic / Ramsons", scientific="Allium ursinum",
            category=FloraCategory.PLANT, subCategory="Wild Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.NORTH_ASIA,
            countries="Russia (European Russia to Siberia)",
            habitat=FloraHabitat.FOREST_FLOOR,
            latitude=55.0, longitude=45.0, spawnRadiusKm=3000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Spring ephemeral carpet — emerges before canopy closes; provides early nectar for bees",
            significance="Bear's garlic (Bärlauch) — bears eat it after hibernation (hence 'ursinum'); " +
                         "Russian spring forage — cheremsha (wild garlic) pickled and eaten across Russia; " +
                         "white star flower carpets flood across deciduous forest floors in May; " +
                         "pungent smell fills entire forest — scent navigation marker",
            edible="Yes — leaves (salad, soup, pickle), bulb, flowers",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="15–40 cm; white star flower mass",
            glowLocation="White star flower + vivid green leaf carpet shimmer; spring forest floor",
            teamlabMood="Flower + leaf", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8503, name="Horseradish", scientific="Armoracia rusticana",
            category=FloraCategory.PLANT, subCategory="Culinary Herb",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.NORTH_ASIA,
            countries="Russia (pan-Russian; Caucasus origin)",
            habitat=FloraHabitat.RIPARIAN,
            latitude=52.0, longitude=40.0, spawnRadiusKm=3000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Insect-pollinated; deep taproot stabilises riverbanks; spreads aggressively once established",
            significance="Khren — Russian condiment for pelmeni, holodets, roast beef; " +
                         "isothiocyanate compounds (like wasabi) give nasal heat; " +
                         "Passover seder maror (bitter herb) in Ashkenazi Jewish tradition; " +
                         "impossible to eradicate once planted — every fragment regrows",
            edible="Yes — freshly grated root (condiment, sauce)",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1.5 m; large crinkled leaves",
            glowLocation="White pungent root glow underground; heat-shimmer from grated root",
            teamlabMood="Root", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8504, name="Ural Licorice", scientific="Glycyrrhiza uralensis",
            category=FloraCategory.PLANT, subCategory="Medicinal Shrub",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.NORTH_ASIA,
            countries="Russia (Ural, Kazakhstan, Western Siberia)",
            habitat=FloraHabitat.RIPARIAN,
            latitude=51.0, longitude=60.0, spawnRadiusKm=2000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Nitrogen-fixer on steppe riverbanks; deep root prevents bank erosion",
            significance="Wild licorice root — 50× sweeter than sugar (glycyrrhizin); " +
                         "TCM and Russian traditional medicine; root chewed by steppe nomads; " +
                         "basis of licorice candy flavouring; anti-inflammatory cortisol precursor compound studied",
            edible="Medicinal/edible — root chewed, tea, extract",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1.5 m; purple spike flowers",
            glowLocation="Purple flower spike + warm gold root glow; sweet-amber underground shimmer",
            teamlabMood="Flower + root", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=8505, name="Roseroot / Golden Root", scientific="Rhodiola rosea",
            category=FloraCategory.PLANT, subCategory="Alpine Succulent",
            gameCategory=FloraGameCategory.HERBS, region=FloraRegion.NORTH_ASIA,
            countries="Russia (Arctic, Ural, Altai, Siberia)",
            habitat=FloraHabitat.ALPINE_MEADOW,
            latitude=68.0, longitude=55.0, spawnRadiusKm=3000f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Alpine pollinator; rocky ground coloniser; succulent rosette survives freeze-thaw",
            significance="Zolotoy koren (golden root) — Viking and Siberian adaptogen; " +
                         "rosavins and salidroside studied for fatigue resistance and altitude adaptation; " +
                         "traded along ancient Arctic Silk Route; scent of fresh rose from roots when cut; " +
                         "used by Soviet cosmonauts and military for cognitive resilience",
            edible="Medicinal — root tincture, tea",
            assetAnimation=FloraAssetAnimation.STATIC, size="5–35 cm; gold-pink succulent rosette",
            glowLocation="Gold-rose succulent rosette shimmer; rocky Arctic anchor glow",
            teamlabMood="Rosette + root", interactionMode=FloraInteractionMode.QUIET_FOREST },

        // ══════════════════════════════════════════════════════════════
        // BATCH 05 — SHRUBS  (IDs 9001–9505)
        // ══════════════════════════════════════════════════════════════

        // ── SEA (9001–9003) ───────────────────────────────────────────
        new FloraEntry { id=9001, name="Giant Bamboo", scientific="Dendrocalamus giganteus",
            category=FloraCategory.GRASS, subCategory="Bamboo",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.SEA,
            countries="Malaysia, Myanmar, Thailand, Indonesia",
            habitat=FloraHabitat.RIVERBANK_FOREST_EDGE,
            latitude=4.5, longitude=114.5, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Rapid erosion control; wildlife habitat; carbon sequestration",
            significance="World's tallest bamboo at 20–30 m; culms grow 30 cm/day; " +
                         "hollow internodes glow jade-green from within; used for construction, food " +
                         "(young shoots), and sustainable material across maritime SEA; ancient sacred groves",
            edible="Edible (young shoots)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="20–30 m culms",
            glowLocation="Jade-green inner wall glow visible through hollow culm cross-section",
            teamlabMood="Culm walls", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9002, name="Pandanus / Pandan", scientific="Pandanus amaryllifolius",
            category=FloraCategory.PLANT, subCategory="Culinary Plant",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.SEA,
            countries="SEA-wide",
            habitat=FloraHabitat.COASTAL_GARDEN,
            latitude=1.3, longitude=103.8, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Ground stabiliser; bird nesting microhabitat",
            significance="Iconic SEA culinary flavouring; spiral rosette of jade-green leaves " +
                         "releases sweet pandan aroma; used in rice, cakes, sweets, and coconut milk; " +
                         "leaf extracts dye food green; rosette veins pulse electric jade in Veiled World",
            edible="Edible (leaves for flavour and colouring)",
            assetAnimation=FloraAssetAnimation.STATIC, size="1–3 m",
            glowLocation="Bright jade emission from leaf veins; spiral rosette luminous pulse",
            teamlabMood="Leaf vein lines", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=9003, name="Borneo Birdnest Fern", scientific="Asplenium nidus",
            category=FloraCategory.FERN, subCategory="Epiphytic Fern",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.SEA,
            countries="Borneo, SEA-wide",
            habitat=FloraHabitat.TROPICAL_FOREST_EPIPHYTE,
            latitude=1.0, longitude=114.0, spawnRadiusKm=350f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Epiphytic water collector; wildlife microhabitat in forest canopy",
            significance="Enormous nest-shaped fern living high on rainforest trees; " +
                         "catches leaf litter and rainwater forming a rich basket ecosystem; " +
                         "feeds tree-climbing invertebrates and lizards; " +
                         "dark glossy midrib channels glow deep emerald in quiet forest light",
            edible="None",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–1.5 m basket",
            glowLocation="Deep glossy green frond shimmer; dark midrib line emission",
            teamlabMood="Frond surface", interactionMode=FloraInteractionMode.QUIET_FOREST },

        // ── East Asia (9101–9102) ─────────────────────────────────────
        new FloraEntry { id=9101, name="Bamboo (Moso)", scientific="Phyllostachys edulis",
            category=FloraCategory.GRASS, subCategory="Bamboo",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.EAST_ASIA,
            countries="China, Japan, Korea",
            habitat=FloraHabitat.TEMPERATE_FOREST,
            latitude=30.0, longitude=120.0, spawnRadiusKm=400f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Giant panda food; rapid carbon sequestration; erosion control",
            significance="Fastest growing plant on Earth (91 cm/day); primary food for giant pandas; " +
                         "revered in East Asian art and philosophy as symbol of resilience and virtue; " +
                         "edible young shoots; culm walls glow jade-green at each internode",
            edible="Edible (young shoots)",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="20–28 m",
            glowLocation="Jade-green inner wall glow at culm internodes",
            teamlabMood="Culm internodes", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9102, name="Tea Plant", scientific="Camellia sinensis",
            category=FloraCategory.SHRUB, subCategory="Crop Plant",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.EAST_ASIA,
            countries="China, Japan, Korea, Taiwan, India",
            habitat=FloraHabitat.HIGHLAND_GARDEN,
            latitude=29.5, longitude=102.5, spawnRadiusKm=300f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Butterfly habitat; shade understorey crop",
            significance="Source of the world's most consumed beverage; 5,000 yr cultivation in Yunnan; " +
                         "young bud + two-leaf flush becomes green tea, oolong, and black tea; " +
                         "Chan and Zen tea ceremony culture; Silk Road trade commodity; " +
                         "jade-soft bud tips glow in morning highland mist",
            edible="Edible (leaves — all tea varieties)",
            assetAnimation=FloraAssetAnimation.STATIC, size="1–3 m cultivated bush",
            glowLocation="Soft jade shimmer on young bud and two-leaf flush",
            teamlabMood="Young leaf tips", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── South Asia (9201) ─────────────────────────────────────────
        new FloraEntry { id=9201, name="Indian Pipe Plant", scientific="Monotropa uniflora",
            category=FloraCategory.PLANT, subCategory="Parasitic Plant",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.SOUTH_ASIA,
            countries="India (Himalayan forests)",
            habitat=FloraHabitat.DARK_HUMID_FOREST,
            latitude=27.5, longitude=88.5, spawnRadiusKm=200f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Mycorrhizal network parasite; fungal conduit in forest ecosystem",
            significance="Ghostly white plant with zero chlorophyll; taps mycorrhizal fungal networks " +
                         "to steal photosynthates from host trees without producing any; " +
                         "translucent ivory stalks emerge from total forest shade; " +
                         "Himalayan legend calls it 'spirit pipe'; entire plant body pulses ghost-white",
            edible="None",
            assetAnimation=FloraAssetAnimation.ANIMATED_GLOW_PULSE, size="10–30 cm",
            glowLocation="Ghost-white translucent full-body glow; icy luminous pulse cycle",
            teamlabMood="Entire plant", interactionMode=FloraInteractionMode.QUIET_FOREST },

        // ── Central Asia (9301–9306) ──────────────────────────────────
        new FloraEntry { id=9301, name="Ephedra", scientific="Ephedra sinica",
            category=FloraCategory.SHRUB, subCategory="Medicinal Plant",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.CENTRAL_ASIA,
            countries="China, Mongolia, Kazakhstan",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=44.0, longitude=103.0, spawnRadiusKm=400f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Drought-tolerant ground cover; Central Asian steppe anchor",
            significance="Ancient Silk Road medicinal; source of ephedrine (modern asthma and decongestant); " +
                         "jointed leafless stems photosynthesise directly; 5,000 yr use in Chinese medicine; " +
                         "Ma Huang — among the oldest documented pharmaceuticals in Asia",
            edible="Medicinal — traditional decoction",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–80 cm",
            glowLocation="Pale yellow-green joint node glow at each stem segment",
            teamlabMood="Stem nodes", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=9302, name="Sea Buckthorn", scientific="Hippophae rhamnoides",
            category=FloraCategory.SHRUB, subCategory="Fruit Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Mongolia, China, Russia",
            habitat=FloraHabitat.RIPARIAN,
            latitude=47.5, longitude=82.0, spawnRadiusKm=350f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Riverbank stabiliser; critical bird food source across steppe",
            significance="Superfood berry with highest vitamin C of any known plant; " +
                         "orange berry oil in traditional Tibetan and Mongol medicine; " +
                         "Genghis Khan's preferred horse feed on campaign; " +
                         "silver-thorned stems cradle vivid orange berry clusters blazing in autumn",
            edible="Edible (berry, seed oil); medicinal",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="1–6 m",
            glowLocation="Vivid orange berry cluster glow; thorny silver stem shimmer",
            teamlabMood="Berry clusters", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=9303, name="Kazakh Ephedra", scientific="Ephedra equisetina",
            category=FloraCategory.SHRUB, subCategory="Desert Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Uzbekistan",
            habitat=FloraHabitat.ROCKY_DESERT,
            latitude=41.5, longitude=62.0, spawnRadiusKm=300f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Rocky desert soil stabiliser; drought pioneer",
            significance="Blue-grey jointed photosynthetic stems; completely leafless; " +
                         "survives in rocky desert where almost nothing else endures; " +
                         "plant lineage 300 million years old predating flowering plants; " +
                         "stem joints pulse blue-grey in Veiled World encounter",
            edible="Medicinal — traditional",
            assetAnimation=FloraAssetAnimation.STATIC, size="0.5–2 m",
            glowLocation="Blue-grey photosynthetic stem glow; joint nodes pulse blue-grey",
            teamlabMood="Stem joints", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9304, name="Siberian Stonecrop", scientific="Sedum hybridum",
            category=FloraCategory.SUCCULENT, subCategory="Alpine Succulent",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Siberia",
            habitat=FloraHabitat.ROCKY_ALPINE,
            latitude=49.0, longitude=85.5, spawnRadiusKm=250f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Rocky ground cover; drought and freeze-thaw tolerant cushion former",
            significance="Fleshy succulent cushions on bare Altai rock faces with no visible soil; " +
                         "yellow star-flowers bloom directly from stone; " +
                         "thrives where ice splits granite and nothing else can anchor; " +
                         "gold shimmer on packed leaf pads catching morning alpine light",
            edible="None",
            assetAnimation=FloraAssetAnimation.STATIC, size="5–15 cm cushion",
            glowLocation="Fleshy leaf pad yellow-gold shimmer; star-flower yellow emission",
            teamlabMood="Leaf cushion + flower", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9305, name="Steppe Feathergrass", scientific="Stipa pennata",
            category=FloraCategory.GRASS, subCategory="Steppe Grass",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Russia, Uzbekistan",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=49.5, longitude=73.0, spawnRadiusKm=450f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Steppe soil anchor; wind-mediated seed dispersal across open plains",
            significance="Silver-white feather plumes 30 cm long cascade in endless steppe wind; " +
                         "a sea of moving silver across Kazakhstan in June; otherworldly landscape; " +
                         "awn drills seed into soil by spiralling with humidity changes; " +
                         "ancient steppe icon in Kazakh and Russian poetry and folk art",
            edible="None",
            assetAnimation=FloraAssetAnimation.ANIMATED_FEATHER_PLUME, size="50–100 cm",
            glowLocation="Silver-white feathery plume wave shimmer in wind; awn tip glow",
            teamlabMood="Plumes", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9306, name="Dwarf Almond", scientific="Prunus tenella",
            category=FloraCategory.SHRUB, subCategory="Spring Flower Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.CENTRAL_ASIA,
            countries="Kazakhstan, Russia, Balkans",
            habitat=FloraHabitat.STEPPE_DRY,
            latitude=51.0, longitude=79.0, spawnRadiusKm=350f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Early spring bee forage; first steppe pollinator resource",
            significance="Deep pink flowers carpet the steppe before any other plant wakes; " +
                         "first harbinger of spring visible from hilltops across North Kazakhstan; " +
                         "bees emerge from winter to find nothing but this shrub; " +
                         "mass petal bloom shimmers cerise-pink across the awakening steppe",
            edible="None (ornamental/ecological value)",
            assetAnimation=FloraAssetAnimation.ANIMATED_MASS_BLOOM, size="0.5–1.5 m",
            glowLocation="Deep pink petal mass shimmer; first steppe spring bloom",
            teamlabMood="Petals", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── Western Asia (9401–9403) ──────────────────────────────────
        new FloraEntry { id=9401, name="Thorny Burnet", scientific="Sarcopoterium spinosum",
            category=FloraCategory.SHRUB, subCategory="Mediterranean Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.WESTERN_ASIA,
            countries="Israel, Jordan, Turkey, Lebanon",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=31.5, longitude=35.0, spawnRadiusKm=200f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; bird shelter; degraded Mediterranean hillside anchor",
            significance="Low thorny cushion of the Levant; leading candidate for Jesus's crown of thorns; " +
                         "bright red autumn berries against silver-grey thorn tips; " +
                         "dominates rocky hillsides from the Negev to Anatolian garrigue; " +
                         "biblical significance makes it Veiled World entry point for pilgrims",
            edible="None",
            assetAnimation=FloraAssetAnimation.STATIC, size="30–60 cm cushion",
            glowLocation="Silver-grey thorn tip shimmer; red autumn berry glow",
            teamlabMood="Thorn tips + berry", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9402, name="Rockrose", scientific="Cistus creticus",
            category=FloraCategory.SHRUB, subCategory="Mediterranean Flower",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.WESTERN_ASIA,
            countries="Turkey, Cyprus, Lebanon, Jordan",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=36.0, longitude=33.0, spawnRadiusKm=250f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bee forage; resin source; fire-adapted hillside regrowth pioneer",
            significance="Vivid pink crinkled petals; source of labdanum resin — " +
                         "ancient Egyptian and Greek sacred perfume traded for millennia; " +
                         "collected from goat fleeces in antiquity; fire-adapted; " +
                         "first to recolonise burnt Mediterranean hillsides after wildfire",
            edible="Medicinal/resin extract",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="0.5–1.5 m",
            glowLocation="Vivid cerise-pink crinkled petal shimmer",
            teamlabMood="Petals", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        new FloraEntry { id=9403, name="Crown of Thorns Plant", scientific="Euphorbia milii",
            category=FloraCategory.SHRUB, subCategory="Desert Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.WESTERN_ASIA,
            countries="Arabia, Iran, Yemen",
            habitat=FloraHabitat.DRY_SCRUB,
            latitude=23.5, longitude=45.0, spawnRadiusKm=300f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Desert anchor; toxic latex deters all grazers",
            significance="Vivid red bracts on dense thorn-armoured stems; " +
                         "long associated with Christian passion iconography; " +
                         "latex highly toxic and caustic; thrives in near-zero rainfall Arabian desert; " +
                         "deep red bract glow blazes in the Veiled World like desert embers",
            edible="TOXIC — caustic latex",
            assetAnimation=FloraAssetAnimation.ANIMATED_SWAY, size="0.3–1.8 m",
            glowLocation="Vivid red bract shimmer; thorn base warm ember glow",
            teamlabMood="Bracts + thorns", interactionMode=FloraInteractionMode.FULL_IMMERSION },

        // ── North Asia (9501–9505) ────────────────────────────────────
        new FloraEntry { id=9501, name="Arctic Willow", scientific="Salix arctica",
            category=FloraCategory.SHRUB, subCategory="Tundra Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.NORTH_ASIA,
            countries="Arctic Russia",
            habitat=FloraHabitat.TUNDRA,
            latitude=72.0, longitude=100.0, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Permafrost anchor; catkin protein source for Arctic birds in spring",
            significance="Lowest-growing woody plant on Earth; 2 cm tall yet decades old; " +
                         "creeps across permafrost where no upright growth survives; " +
                         "pale silver catkins emerge in Arctic spring as first available food; " +
                         "entire mat shivers silver in tundra wind across kilometre-wide carpets",
            edible="None",
            assetAnimation=FloraAssetAnimation.ANIMATED_CATKIN_SWAY, size="Ground level–20 cm",
            glowLocation="Pale silver catkin glow trembling in Arctic wind",
            teamlabMood="Catkins", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9502, name="Reindeer Lichen", scientific="Cladonia rangiferina",
            category=FloraCategory.LICHEN, subCategory="Tundra Lichen",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.NORTH_ASIA,
            countries="Arctic Russia, Siberia",
            habitat=FloraHabitat.TUNDRA,
            latitude=68.0, longitude=97.0, spawnRadiusKm=600f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Primary tundra producer; sole reindeer winter food; slow peat former",
            significance="Single mat 300+ years old; sustains all Arctic reindeer herds through winter; " +
                         "has no roots — absorbs everything from air and rain; " +
                         "grows only 3–5 mm per year; pale grey-white crystalline branching " +
                         "glows frost-white in Veiled World like a living snowdrift",
            edible="None (reindeer food)",
            assetAnimation=FloraAssetAnimation.STATIC, size="5–10 cm mat",
            glowLocation="Pale grey-white crystalline branching glow in frost shimmer",
            teamlabMood="Entire mat", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9503, name="Dwarf Pine", scientific="Pinus pumila",
            category=FloraCategory.SHRUB, subCategory="Alpine Shrub",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.NORTH_ASIA,
            countries="Siberia, Kamchatka, Russia",
            habitat=FloraHabitat.TUNDRA,
            latitude=54.0, longitude=162.0, spawnRadiusKm=300f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Permafrost anchor; pine nut food source for birds and Kamchatka bears",
            significance="Prostrate pine sculpted by centuries of wind into horizontal mats; " +
                         "200-year-old trunk may stand only 1 m tall; " +
                         "dense pine nut cones are survival food for Kamchatka brown bears in autumn; " +
                         "warm gold cone clusters glow at dusk across volcanic ridgelines",
            edible="Edible (pine nuts)",
            assetAnimation=FloraAssetAnimation.ANIMATED_WIND_SCULPTED, size="0.5–3 m",
            glowLocation="Warm gold pine cone cluster glow; needle tips silver shimmer",
            teamlabMood="Cone clusters", interactionMode=FloraInteractionMode.QUIET_FOREST },

        new FloraEntry { id=9504, name="Sphagnum Moss", scientific="Sphagnum spp.",
            category=FloraCategory.MOSS, subCategory="Bog Moss",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.NORTH_ASIA,
            countries="Russia-wide",
            habitat=FloraHabitat.BOG_SPHAGNUM,
            latitude=60.0, longitude=60.0, spawnRadiusKm=600f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Carbon storage; water retention; bog builder; natural antibiotic",
            significance="Single-handedly built the great Russian peat bogs storing centuries of carbon; " +
                         "holds 20× its own weight in water; natural antibiotic properties; " +
                         "used as wound dressing in WWI; pale red-green luminous carpet " +
                         "spreads across the entire West Siberian lowland basin",
            edible="Medicinal — antiseptic wound packing",
            assetAnimation=FloraAssetAnimation.STATIC, size="5–40 cm living carpet",
            glowLocation="Pale red-green luminous bog carpet shimmer",
            teamlabMood="Entire mat", interactionMode=FloraInteractionMode.LIVING_WATER },

        new FloraEntry { id=9505, name="Arctic Cottongrass", scientific="Eriophorum angustifolium",
            category=FloraCategory.GRASS, subCategory="Tundra Grass",
            gameCategory=FloraGameCategory.SHRUBS, region=FloraRegion.NORTH_ASIA,
            countries="Arctic Russia, Siberia",
            habitat=FloraHabitat.WET_TUNDRA,
            latitude=71.0, longitude=72.0, spawnRadiusKm=500f,
            conservation=ConservationStatus.LEAST_CONCERN,
            ecologicalRole="Bog builder; lemming and snow goose food source",
            significance="White cotton-ball seed heads sway in unison across wet tundra horizons; " +
                         "ghostly spectacle in Arctic summer; each white puff disperses on wind; " +
                         "critical lemming nesting material; the tundra's most iconic visual signature; " +
                         "kilometre-wide rippling white fields visible across the Yamal Peninsula",
            edible="None",
            assetAnimation=FloraAssetAnimation.ANIMATED_COTTON_SWAY, size="20–60 cm",
            glowLocation="White cotton-ball seed head shimmer; tundra wind wave pattern",
            teamlabMood="Cotton heads", interactionMode=FloraInteractionMode.QUIET_FOREST },

        // ── MORE BATCHES APPENDED HERE AS DATA ARRIVES ───────────
    };
}

