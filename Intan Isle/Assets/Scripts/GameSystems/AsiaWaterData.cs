using System;
using System.Collections.Generic;
using UnityEngine;

// ════════════════════════════════════════════════════════════════
// WATER TYPE + HEALTH
// ════════════════════════════════════════════════════════════════

public enum WaterType
{
    OCEAN,       // open ocean
    SEA,         // enclosed / semi-enclosed sea
    STRAIT,      // narrow passage
    GULF,        // bay or gulf
    RIVER,       // flowing river
    LAKE,        // standing lake
    RESERVOIR,   // man-made reservoir
    HOT_SPRING,  // geothermal
    ESTUARY,     // river mouth / tidal
    WETLAND,     // swamp, peatland, mangrove water
    CRATER_LAKE, // volcanic lake
}

public enum WaterHealth
{
    PRISTINE,   // untouched wilderness water
    HEALTHY,    // good ecological status
    STRESSED,   // showing degradation
    POLLUTED,   // severe pollution
    DEPLETED,   // critically diminished (Aral Sea etc.)
}

public static class WaterTypeExtensions
{
    public static bool IsRenderable(this WaterType t)
        // Only spawn Suimono surface for bodies the player might walk next to
        => t == WaterType.RIVER || t == WaterType.LAKE || t == WaterType.RESERVOIR
        || t == WaterType.HOT_SPRING || t == WaterType.CRATER_LAKE
        || t == WaterType.WETLAND || t == WaterType.ESTUARY;

    public static string DisplayName(this WaterType t) => t switch
    {
        WaterType.OCEAN       => "Ocean",
        WaterType.SEA         => "Sea",
        WaterType.STRAIT      => "Strait",
        WaterType.GULF        => "Gulf",
        WaterType.RIVER       => "River",
        WaterType.LAKE        => "Lake",
        WaterType.RESERVOIR   => "Reservoir",
        WaterType.HOT_SPRING  => "Hot Spring",
        WaterType.ESTUARY     => "Estuary",
        WaterType.WETLAND     => "Wetland",
        WaterType.CRATER_LAKE => "Crater Lake",
        _                     => "Water",
    };

    /// <summary>Barakah change per second when player is within 1 km. Positive = restore.</summary>
    public static float BarakahRate(this WaterHealth h) => h switch
    {
        WaterHealth.PRISTINE  =>  2.5f,
        WaterHealth.HEALTHY   =>  1.5f,
        WaterHealth.STRESSED  =>  0.3f,
        WaterHealth.POLLUTED  => -1.5f,
        WaterHealth.DEPLETED  => -2.5f,
        _                     =>  0f,
    };

    /// <summary>Suimono water surface tint for each health tier.</summary>
    public static Color SurfaceTint(this WaterHealth h) => h switch
    {
        WaterHealth.PRISTINE  => new Color(0.08f, 0.55f, 0.80f, 0.85f), // clear blue
        WaterHealth.HEALTHY   => new Color(0.10f, 0.50f, 0.65f, 0.80f),
        WaterHealth.STRESSED  => new Color(0.25f, 0.48f, 0.40f, 0.75f), // greenish
        WaterHealth.POLLUTED  => new Color(0.35f, 0.35f, 0.15f, 0.80f), // murky
        WaterHealth.DEPLETED  => new Color(0.50f, 0.42f, 0.20f, 0.85f), // brown/grey
        _                     => new Color(0.10f, 0.50f, 0.65f, 0.80f),
    };
}

// ════════════════════════════════════════════════════════════════
// WATER BODY ENTRY
// ════════════════════════════════════════════════════════════════

[Serializable]
public class WaterBodyEntry
{
    [Header("Identity")]
    public string      name;
    public WaterType   waterType;
    public WaterHealth health;

    [Header("Geography")]
    public double latitude;
    public double longitude;
    [Tooltip("Approximate radius in km")]
    public float  radiusKm;

    [Header("Narrative")]
    [TextArea(2, 4)]
    public string description;

    [Header("Country / Region")]
    public string region;
}

// ════════════════════════════════════════════════════════════════
// ASIA WATER DATA — static database
// ════════════════════════════════════════════════════════════════

public static class AsiaWaterData
{
    public static readonly List<WaterBodyEntry> All = new List<WaterBodyEntry>
    {
        // ══ OCEANS ══════════════════════════════════════════════════

        new WaterBodyEntry {
            name="Indian Ocean", waterType=WaterType.OCEAN,
            latitude=-20.0, longitude=80.0, radiusKm=3000f,
            health=WaterHealth.STRESSED,
            region="South Asia / Africa",
            description="Third largest ocean — warming faster than global average; acidification threatens coral systems.",
        },
        new WaterBodyEntry {
            name="Pacific Ocean (West)", waterType=WaterType.OCEAN,
            latitude=15.0, longitude=150.0, radiusKm=4000f,
            health=WaterHealth.STRESSED,
            region="East / Southeast Asia",
            description="Western Pacific contains highest marine biodiversity on Earth — the Coral Triangle.",
        },

        // ══ SEAS ════════════════════════════════════════════════════

        new WaterBodyEntry {
            name="South China Sea", waterType=WaterType.SEA,
            latitude=12.0, longitude=113.0, radiusKm=1500f,
            health=WaterHealth.STRESSED,
            region="Southeast Asia",
            description="Contains over 30% of global marine biodiversity; overfished and contested. Coral reefs under severe pressure.",
        },
        new WaterBodyEntry {
            name="Bay of Bengal", waterType=WaterType.SEA,
            latitude=15.0, longitude=87.0, radiusKm=800f,
            health=WaterHealth.STRESSED,
            region="South Asia",
            description="Receives India's and Bangladesh's river discharge — critical for monsoon moisture cycling.",
        },
        new WaterBodyEntry {
            name="Arabian Sea", waterType=WaterType.SEA,
            latitude=17.0, longitude=64.0, radiusKm=1000f,
            health=WaterHealth.STRESSED,
            region="Western Asia / South Asia",
            description="World's largest oxygen-minimum zone expanding due to warming — dead zone doubling since 1990.",
        },
        new WaterBodyEntry {
            name="East China Sea", waterType=WaterType.SEA,
            latitude=29.0, longitude=124.0, radiusKm=600f,
            health=WaterHealth.POLLUTED,
            region="East Asia",
            description="Heavily polluted by Yangtze River discharge — eutrophication and hypoxia in coastal zones.",
        },
        new WaterBodyEntry {
            name="Sea of Japan (East Sea)", waterType=WaterType.SEA,
            latitude=40.0, longitude=134.0, radiusKm=500f,
            health=WaterHealth.HEALTHY,
            region="East Asia",
            description="Relatively clean sea with strong thermohaline circulation; deep-water oxygen still intact.",
        },
        new WaterBodyEntry {
            name="Yellow Sea", waterType=WaterType.SEA,
            latitude=35.0, longitude=122.0, radiusKm=400f,
            health=WaterHealth.POLLUTED,
            region="East Asia",
            description="Shallow continental sea; 40% of its tidal flats lost to land reclamation since 1950.",
        },
        new WaterBodyEntry {
            name="Andaman Sea", waterType=WaterType.SEA,
            latitude=10.5, longitude=97.0, radiusKm=400f,
            health=WaterHealth.HEALTHY,
            region="Southeast Asia",
            description="Home to dugongs, whale sharks, and some of the last intact coral reefs in Southeast Asia.",
        },
        new WaterBodyEntry {
            name="Java Sea", waterType=WaterType.SEA,
            latitude=-5.0, longitude=110.0, radiusKm=500f,
            health=WaterHealth.STRESSED,
            region="Southeast Asia",
            description="Shallow tropical sea — fishing pressure and plastic pollution threatening seagrass meadows.",
        },
        new WaterBodyEntry {
            name="Celebes Sea", waterType=WaterType.SEA,
            latitude=4.0, longitude=123.0, radiusKm=400f,
            health=WaterHealth.HEALTHY,
            region="Southeast Asia",
            description="Deep, clear water of the Coral Triangle; high endemism and intact reef systems.",
        },
        new WaterBodyEntry {
            name="Philippine Sea", waterType=WaterType.SEA,
            latitude=20.0, longitude=130.0, radiusKm=900f,
            health=WaterHealth.STRESSED,
            region="Southeast Asia",
            description="Deepest point on Earth — Mariana Trench (10,994 m). Microplastics now found at full depth.",
        },
        new WaterBodyEntry {
            name="Persian Gulf", waterType=WaterType.GULF,
            latitude=26.5, longitude=52.0, radiusKm=450f,
            health=WaterHealth.STRESSED,
            region="Western Asia",
            description="Shallow, hypersaline gulf — warming 0.5°C per decade. Desalination brine discharge stressing corals.",
        },
        new WaterBodyEntry {
            name="Red Sea", waterType=WaterType.SEA,
            latitude=20.0, longitude=38.0, radiusKm=600f,
            health=WaterHealth.STRESSED,
            region="Western Asia / East Africa",
            description="Warmest sea with coral reefs; hosts some of the most heat-resistant corals. Shipping and oil risk.",
        },
        new WaterBodyEntry {
            name="Caspian Sea", waterType=WaterType.LAKE,
            latitude=42.0, longitude=51.0, radiusKm=600f,
            health=WaterHealth.STRESSED,
            region="Central Asia",
            description="World's largest landlocked body of water — critically endangered beluga sturgeon; levels dropping 3m since 1990s.",
        },
        new WaterBodyEntry {
            name="Arafura Sea", waterType=WaterType.SEA,
            latitude=-9.0, longitude=135.0, radiusKm=600f,
            health=WaterHealth.HEALTHY,
            region="Southeast Asia / Australia",
            description="Remote, largely undisturbed sea — home to the most biodiverse marine life in the Indo-Pacific.",
        },

        // ══ STRAITS & PASSAGES ══════════════════════════════════════

        new WaterBodyEntry {
            name="Strait of Malacca", waterType=WaterType.STRAIT,
            latitude=3.5, longitude=102.0, radiusKm=200f,
            health=WaterHealth.STRESSED,
            region="Southeast Asia",
            description="World's busiest shipping lane — 80,000+ ships/year. Oil spill risk and mangrove loss ongoing.",
        },
        new WaterBodyEntry {
            name="Lombok Strait", waterType=WaterType.STRAIT,
            latitude=-8.5, longitude=115.7, radiusKm=50f,
            health=WaterHealth.HEALTHY,
            region="Southeast Asia",
            description="Wallace Line — dramatic biodiversity boundary between Asian and Australian fauna.",
        },
        new WaterBodyEntry {
            name="Sunda Strait", waterType=WaterType.STRAIT,
            latitude=-6.0, longitude=105.8, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="Southeast Asia",
            description="Site of Krakatau — seismically active. Industrial shipping from Java's west coast.",
        },

        // ══ MAJOR RIVERS ════════════════════════════════════════════

        new WaterBodyEntry {
            name="Yangtze River", waterType=WaterType.RIVER,
            latitude=30.5, longitude=111.5, radiusKm=120f,
            health=WaterHealth.STRESSED,
            region="China",
            description="Asia's longest river (6,300 km). Three Gorges Dam blocked sediment flow; Yangtze dolphin declared extinct 2006.",
        },
        new WaterBodyEntry {
            name="Yellow River (Huang He)", waterType=WaterType.RIVER,
            latitude=36.0, longitude=107.0, radiusKm=150f,
            health=WaterHealth.STRESSED,
            region="China",
            description="China's sorrow — carries world's highest sediment load. Runs dry to the sea for months each year.",
        },
        new WaterBodyEntry {
            name="Mekong River", waterType=WaterType.RIVER,
            latitude=17.0, longitude=102.5, radiusKm=200f,
            health=WaterHealth.STRESSED,
            region="Southeast Asia",
            description="Life-source for 60 million people. 11 mainstream dams in China blocking 40% of natural sediment flow.",
        },
        new WaterBodyEntry {
            name="Irrawaddy River", waterType=WaterType.RIVER,
            latitude=21.0, longitude=95.8, radiusKm=150f,
            health=WaterHealth.HEALTHY,
            region="Myanmar",
            description="Myanmar's mother river — still relatively intact, home to the endangered Irrawaddy dolphin.",
        },
        new WaterBodyEntry {
            name="Ganges River", waterType=WaterType.RIVER,
            latitude=25.0, longitude=84.0, radiusKm=200f,
            health=WaterHealth.POLLUTED,
            region="India / Bangladesh",
            description="Sacred river of South Asia — carries 3 billion litres of sewage daily. Gangetic dolphin critically endangered.",
        },
        new WaterBodyEntry {
            name="Brahmaputra River", waterType=WaterType.RIVER,
            latitude=27.0, longitude=91.0, radiusKm=150f,
            health=WaterHealth.HEALTHY,
            region="India / Bangladesh",
            description="Descends from Tibetan Plateau through the world's deepest gorge. One of the last un-dammed Himalayan rivers.",
        },
        new WaterBodyEntry {
            name="Indus River", waterType=WaterType.RIVER,
            latitude=29.0, longitude=70.0, radiusKm=200f,
            health=WaterHealth.STRESSED,
            region="Pakistan / India",
            description="Ancient civilisation river — 90% of its water diverted for agriculture; Indus delta is collapsing.",
        },
        new WaterBodyEntry {
            name="Salween River (Nu Jiang)", waterType=WaterType.RIVER,
            latitude=23.0, longitude=99.0, radiusKm=100f,
            health=WaterHealth.PRISTINE,
            region="Myanmar / China",
            description="One of the world's last undammed major rivers — WWF designated global 200 ecoregion.",
        },
        new WaterBodyEntry {
            name="Tigris River", waterType=WaterType.RIVER,
            latitude=34.0, longitude=43.5, radiusKm=100f,
            health=WaterHealth.STRESSED,
            region="Iraq / Turkey",
            description="Cradle of civilisation — flow reduced 75% by upstream dams. Mesopotamian marshes partially restored.",
        },
        new WaterBodyEntry {
            name="Euphrates River", waterType=WaterType.RIVER,
            latitude=34.5, longitude=40.0, radiusKm=100f,
            health=WaterHealth.STRESSED,
            region="Syria / Iraq",
            description="Biblical river — reduced to a fraction of historical flow. Syrian drought accelerated desertification.",
        },
        new WaterBodyEntry {
            name="Amur River", waterType=WaterType.RIVER,
            latitude=51.0, longitude=126.0, radiusKm=150f,
            health=WaterHealth.HEALTHY,
            region="Russia / China",
            description="World's 10th longest river — supports Siberian tiger habitat; relatively intact ecosystem.",
        },
        new WaterBodyEntry {
            name="Ob River", waterType=WaterType.RIVER,
            latitude=60.0, longitude=73.0, radiusKm=200f,
            health=WaterHealth.HEALTHY,
            region="Russia (Siberia)",
            description="Flows through the world's largest boreal peatland — critical carbon store and migratory bird corridor.",
        },
        new WaterBodyEntry {
            name="Yenisei River", waterType=WaterType.RIVER,
            latitude=63.0, longitude=89.0, radiusKm=200f,
            health=WaterHealth.HEALTHY,
            region="Russia (Siberia)",
            description="World's largest river by discharge — feeds Lake Baikal; pristine sub-arctic watershed.",
        },
        new WaterBodyEntry {
            name="Lena River", waterType=WaterType.RIVER,
            latitude=65.0, longitude=127.0, radiusKm=200f,
            health=WaterHealth.PRISTINE,
            region="Russia (Siberia)",
            description="One of the largest undisturbed river systems on Earth — permafrost melt is altering its delta.",
        },
        new WaterBodyEntry {
            name="Johor River", waterType=WaterType.RIVER,
            latitude=1.73, longitude=103.90, radiusKm=20f,
            health=WaterHealth.STRESSED,
            region="Malaysia / Singapore",
            description="Primary freshwater source for Singapore — Linggiu Reservoir catches its flow.",
        },
        new WaterBodyEntry {
            name="Pahang River", waterType=WaterType.RIVER,
            latitude=3.8, longitude=103.0, radiusKm=50f,
            health=WaterHealth.HEALTHY,
            region="Malaysia",
            description="Longest river in Peninsular Malaysia — drains the Cameron Highlands ancient forest.",
        },
        new WaterBodyEntry {
            name="Rajang River", waterType=WaterType.RIVER,
            latitude=2.5, longitude=112.5, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="Malaysia (Sarawak / Borneo)",
            description="Longest river in Malaysia — palm oil and logging runoff threatening Penan river communities.",
        },
        new WaterBodyEntry {
            name="Citarum River", waterType=WaterType.RIVER,
            latitude=-6.9, longitude=107.6, radiusKm=40f,
            health=WaterHealth.POLLUTED,
            region="Indonesia (Java)",
            description="One of Earth's most polluted rivers — 2,000+ textile factories discharge directly into it.",
        },
        new WaterBodyEntry {
            name="Chao Phraya River", waterType=WaterType.RIVER,
            latitude=14.5, longitude=100.5, radiusKm=60f,
            health=WaterHealth.STRESSED,
            region="Thailand",
            description="Bangkok's lifeline — severe microplastic contamination; freshwater fisheries severely degraded.",
        },
        new WaterBodyEntry {
            name="Ayeyarwady (Irrawaddy) Delta", waterType=WaterType.ESTUARY,
            latitude=16.5, longitude=95.5, radiusKm=60f,
            health=WaterHealth.HEALTHY,
            region="Myanmar",
            description="One of Southeast Asia's last intact river deltas — Irrawaddy dolphins use its tidal channels.",
        },
        new WaterBodyEntry {
            name="Ganges Delta (Sundarbans)", waterType=WaterType.ESTUARY,
            latitude=22.0, longitude=89.5, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="Bangladesh / India",
            description="World's largest river delta — rising sea levels threatening 30 million people and the Bengal tiger.",
        },

        // ══ LAKES ════════════════════════════════════════════════════

        new WaterBodyEntry {
            name="Lake Baikal", waterType=WaterType.LAKE,
            latitude=53.5, longitude=108.0, radiusKm=200f,
            health=WaterHealth.STRESSED,
            region="Russia (Siberia)",
            description="World's deepest lake — holds 20% of all unfrozen surface freshwater. Algal blooms increasing with warming.",
        },
        new WaterBodyEntry {
            name="Aral Sea (remnant)", waterType=WaterType.LAKE,
            latitude=45.5, longitude=60.0, radiusKm=150f,
            health=WaterHealth.DEPLETED,
            region="Kazakhstan / Uzbekistan",
            description="Ecological catastrophe — once 4th largest lake, now 10% of original size. Soviet irrigation destroyed it.",
        },
        new WaterBodyEntry {
            name="Lake Balkhash", waterType=WaterType.LAKE,
            latitude=46.5, longitude=75.0, radiusKm=250f,
            health=WaterHealth.STRESSED,
            region="Kazakhstan",
            description="Unique lake — western half fresh, eastern half saline. Shrinking due to upstream diversions.",
        },
        new WaterBodyEntry {
            name="Tonle Sap Lake", waterType=WaterType.LAKE,
            latitude=12.8, longitude=104.0, radiusKm=60f,
            health=WaterHealth.STRESSED,
            region="Cambodia",
            description="Southeast Asia's largest lake — doubles in size each monsoon season. Upstream Mekong dams reducing flood pulse.",
        },
        new WaterBodyEntry {
            name="Dal Lake", waterType=WaterType.LAKE,
            latitude=34.1, longitude=74.9, radiusKm=12f,
            health=WaterHealth.POLLUTED,
            region="India (Kashmir)",
            description="Iconic Himalayan lake — severe eutrophication from sewage and houseboat waste; area shrunken 40%.",
        },
        new WaterBodyEntry {
            name="Lake Toba", waterType=WaterType.CRATER_LAKE,
            latitude=2.6, longitude=98.8, radiusKm=50f,
            health=WaterHealth.HEALTHY,
            region="Indonesia (Sumatra)",
            description="World's largest volcanic crater lake — formed by supervolcano eruption 74,000 years ago. Near-ancient endemic fish.",
        },
        new WaterBodyEntry {
            name="Qinghai Lake (Kokonor)", waterType=WaterType.LAKE,
            latitude=36.9, longitude=100.2, radiusKm=100f,
            health=WaterHealth.HEALTHY,
            region="China (Tibet Plateau)",
            description="China's largest lake — rising due to glacial melt on Tibetan Plateau. Critical migratory bird stopover.",
        },
        new WaterBodyEntry {
            name="Issyk-Kul", waterType=WaterType.LAKE,
            latitude=42.4, longitude=77.2, radiusKm=100f,
            health=WaterHealth.HEALTHY,
            region="Kyrgyzstan",
            description="Second-largest alpine lake globally — never freezes despite altitude; sacred to nomadic Kyrgyz people.",
        },
        new WaterBodyEntry {
            name="Poyang Lake", waterType=WaterType.LAKE,
            latitude=29.0, longitude=116.2, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="China",
            description="China's largest freshwater lake — critical Siberian crane wintering ground. Sand mining collapsed 30% of habitat.",
        },
        new WaterBodyEntry {
            name="Dongting Lake", waterType=WaterType.LAKE,
            latitude=29.2, longitude=112.9, radiusKm=70f,
            health=WaterHealth.STRESSED,
            region="China",
            description="Key Yangtze floodplain lake — reduced by 40% through land reclamation. Baiji dolphin habitat lost.",
        },
        new WaterBodyEntry {
            name="Van Lake", waterType=WaterType.LAKE,
            latitude=38.6, longitude=42.8, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="Turkey",
            description="Alkaline soda lake — endemic Van cat drinks its water. Threatened by agricultural runoff.",
        },
        new WaterBodyEntry {
            name="Dead Sea", waterType=WaterType.LAKE,
            latitude=31.5, longitude=35.5, radiusKm=40f,
            health=WaterHealth.DEPLETED,
            region="Jordan / Israel / Palestine",
            description="Earth's lowest point — shrinking by 1 metre per year as Jordan River is almost entirely diverted.",
        },
        new WaterBodyEntry {
            name="Sea of Galilee (Kinneret)", waterType=WaterType.LAKE,
            latitude=32.8, longitude=35.6, radiusKm=15f,
            health=WaterHealth.STRESSED,
            region="Israel / Palestine",
            description="Freshwater lake critical for Israel's water supply — water level has dropped to danger thresholds.",
        },

        // ══ RESERVOIRS ══════════════════════════════════════════════

        new WaterBodyEntry {
            name="Three Gorges Reservoir", waterType=WaterType.RESERVOIR,
            latitude=30.8, longitude=108.5, radiusKm=120f,
            health=WaterHealth.STRESSED,
            region="China",
            description="World's largest hydroelectric dam — displaced 1.2 million people; landslides and sedimentation ongoing.",
        },
        new WaterBodyEntry {
            name="Linggiu Reservoir", waterType=WaterType.RESERVOIR,
            latitude=1.95, longitude=103.93, radiusKm=5f,
            health=WaterHealth.HEALTHY,
            region="Malaysia (Johor)",
            description="Captures Johor River flow — supplies 60% of Singapore's raw water. Surrounded by protected forest.",
        },
        new WaterBodyEntry {
            name="Kranji Reservoir", waterType=WaterType.RESERVOIR,
            latitude=1.43, longitude=103.73, radiusKm=2f,
            health=WaterHealth.HEALTHY,
            region="Singapore",
            description="Singapore's tidal-gate reservoir near Lim Chu Kang — surrounded by wetland bird sanctuary.",
        },
        new WaterBodyEntry {
            name="MacRitchie Reservoir", waterType=WaterType.RESERVOIR,
            latitude=1.35, longitude=103.83, radiusKm=2f,
            health=WaterHealth.PRISTINE,
            region="Singapore",
            description="Oldest reservoir in Singapore — surrounded by primary rainforest with long-tailed macaques and flying lemurs.",
        },
        new WaterBodyEntry {
            name="Ataturk Dam Reservoir", waterType=WaterType.RESERVOIR,
            latitude=37.5, longitude=38.5, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="Turkey",
            description="Turkey's largest reservoir — drastically reduced Euphrates flow into Syria and Iraq.",
        },
        new WaterBodyEntry {
            name="Bratsk Reservoir", waterType=WaterType.RESERVOIR,
            latitude=57.0, longitude=101.5, radiusKm=200f,
            health=WaterHealth.HEALTHY,
            region="Russia (Siberia)",
            description="One of the world's largest man-made lakes — pristine Siberian taiga surroundings.",
        },
        new WaterBodyEntry {
            name="Indira Sagar Reservoir", waterType=WaterType.RESERVOIR,
            latitude=22.3, longitude=76.5, radiusKm=50f,
            health=WaterHealth.STRESSED,
            region="India",
            description="Narmada River dam — displaced 250,000 tribal people; river dolphin habitat severely fragmented.",
        },

        // ══ HOT SPRINGS ═════════════════════════════════════════════

        new WaterBodyEntry {
            name="Beppu Hot Springs", waterType=WaterType.HOT_SPRING,
            latitude=33.3, longitude=131.5, radiusKm=5f,
            health=WaterHealth.PRISTINE,
            region="Japan",
            description="Earth's highest discharge of hot spring water — the 'hells of Beppu.' Steam rises from city streets.",
        },
        new WaterBodyEntry {
            name="Pamukkale Thermal Springs", waterType=WaterType.HOT_SPRING,
            latitude=37.9, longitude=29.1, radiusKm=3f,
            health=WaterHealth.HEALTHY,
            region="Turkey",
            description="Cotton Castle — calcium carbonate terraces formed by 35°C geothermal spring. UNESCO World Heritage.",
        },
        new WaterBodyEntry {
            name="Tengchong Hot Springs", waterType=WaterType.HOT_SPRING,
            latitude=25.0, longitude=98.5, radiusKm=8f,
            health=WaterHealth.PRISTINE,
            region="China (Yunnan)",
            description="Geothermal field near extinct volcanoes — over 90 springs including boiling mud pools.",
        },
        new WaterBodyEntry {
            name="Deildartunguhver (Oman equivalent — Ain Hamam)", waterType=WaterType.HOT_SPRING,
            latitude=23.6, longitude=58.2, radiusKm=2f,
            health=WaterHealth.HEALTHY,
            region="Oman",
            description="Warm spring used by Bedouin for centuries — sacred freshwater in the desert.",
        },
        new WaterBodyEntry {
            name="Dalhousie Hot Spring", waterType=WaterType.HOT_SPRING,
            latitude=-26.4, longitude=135.5, radiusKm=1f,
            health=WaterHealth.PRISTINE,
            region="Australia (edge of Asia range)",
            description="Australia's only known naturally hot spring — unique fish endemic to this single thermal pool.",
        },
        new WaterBodyEntry {
            name="Jigokudani Monkey Hot Springs", waterType=WaterType.HOT_SPRING,
            latitude=36.7, longitude=138.5, radiusKm=2f,
            health=WaterHealth.PRISTINE,
            region="Japan",
            description="Japanese macaques bathe in geothermal spring in winter snow — one of the world's iconic ecological images.",
        },
        new WaterBodyEntry {
            name="Rotorua Geothermal Area", waterType=WaterType.HOT_SPRING,
            latitude=-38.1, longitude=176.2, radiusKm=15f,
            health=WaterHealth.PRISTINE,
            region="New Zealand (Pacific edge)",
            description="Polynesian volcanic lake district — sacred geothermal waters, boiling mud pools, silica terraces.",
        },

        // ══ CRATER LAKES ════════════════════════════════════════════

        new WaterBodyEntry {
            name="Kawah Ijen Crater Lake", waterType=WaterType.CRATER_LAKE,
            latitude=-8.06, longitude=114.24, radiusKm=3f,
            health=WaterHealth.PRISTINE,
            region="Indonesia (Java)",
            description="World's largest highly acidic crater lake (pH < 0.3). Electric-blue fire from burning sulphur gas at night.",
        },
        new WaterBodyEntry {
            name="Kelimutu Crater Lakes", waterType=WaterType.CRATER_LAKE,
            latitude=-8.77, longitude=121.83, radiusKm=2f,
            health=WaterHealth.PRISTINE,
            region="Indonesia (Flores)",
            description="Three lakes of different colours on one volcano — colours change due to volcanic minerals. Sacred to local Lio people.",
        },
        new WaterBodyEntry {
            name="Segara Anak Lake (Rinjani)", waterType=WaterType.CRATER_LAKE,
            latitude=-8.41, longitude=116.46, radiusKm=5f,
            health=WaterHealth.PRISTINE,
            region="Indonesia (Lombok)",
            description="Sacred crater lake at 2,000m inside Rinjani volcano — Balinese Hindu pilgrimage site.",
        },

        // ══ WETLANDS ════════════════════════════════════════════════

        new WaterBodyEntry {
            name="Mesopotamian Marshes", waterType=WaterType.WETLAND,
            latitude=31.0, longitude=47.0, radiusKm=120f,
            health=WaterHealth.STRESSED,
            region="Iraq",
            description="Garden of Eden — ancient wetlands drained 90% by Saddam Hussein; partially restored post-2003. UNESCO site.",
        },
        new WaterBodyEntry {
            name="Indus Delta Wetlands", waterType=WaterType.WETLAND,
            latitude=23.5, longitude=68.0, radiusKm=80f,
            health=WaterHealth.DEPLETED,
            region="Pakistan",
            description="Once South Asia's most productive fishery — now a ghost delta as the Indus runs dry before the sea.",
        },
        new WaterBodyEntry {
            name="Kalimantan Peatlands", waterType=WaterType.WETLAND,
            latitude=0.5, longitude=113.5, radiusKm=300f,
            health=WaterHealth.STRESSED,
            region="Indonesia (Borneo)",
            description="World's largest tropical peatland — 57,000 km² drained for palm oil. Fires release ancient carbon.",
        },
        new WaterBodyEntry {
            name="Okavango-equivalent: Khanka Lake Wetland", waterType=WaterType.WETLAND,
            latitude=44.9, longitude=132.4, radiusKm=40f,
            health=WaterHealth.STRESSED,
            region="Russia / China border",
            description="Transboundary wetland — critical stopover for 400+ migratory bird species on the East Asian Flyway.",
        },
        new WaterBodyEntry {
            name="Chilika Lake", waterType=WaterType.WETLAND,
            latitude=19.7, longitude=85.3, radiusKm=40f,
            health=WaterHealth.STRESSED,
            region="India (Odisha)",
            description="Asia's largest brackish water lagoon — Ramsar site; winter home of 160,000 flamingos and Irrawaddy dolphins.",
        },
        new WaterBodyEntry {
            name="Sundarbans Mangrove Wetland", waterType=WaterType.WETLAND,
            latitude=22.0, longitude=89.2, radiusKm=80f,
            health=WaterHealth.STRESSED,
            region="Bangladesh / India",
            description="World's largest mangrove — last stronghold of the Bengal tiger; sea levels rising 8mm/year here.",
        },
    };

    /// <summary>Find all water bodies within radiusKm of a lat/lon point.</summary>
    public static List<WaterBodyEntry> GetNearby(double lat, double lon, float radiusKm)
    {
        var result = new List<WaterBodyEntry>();
        foreach (var w in All)
        {
            double dLat   = lat - w.latitude;
            double dLon   = lon - w.longitude;
            double distKm = Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
            if (distKm < w.radiusKm + radiusKm)
                result.Add(w);
        }
        return result;
    }
}
