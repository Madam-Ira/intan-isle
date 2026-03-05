using System;
using System.Collections.Generic;
using UnityEngine;

// ════════════════════════════════════════════════════════════════
// CAVE TYPE + STATUS
// ════════════════════════════════════════════════════════════════

public enum CaveType
{
    KARST,          // limestone — stalactites, stalagmites, underground rivers
    LAVA_TUBE,      // volcanic — smooth cylindrical basalt tunnels
    SEA_CAVE,       // coastal erosion — tidal, marine light
    ICE_CAVE,       // glacial / permafrost — blue crystalline
    CRYSTAL,        // mineral formations — gypsum, calcite, selenite
    SACRED_TEMPLE,  // religious rock-cut temples and shrines
    BURIAL,         // ancient burial / ossuary sites
    MINE,           // abandoned industrial / artisanal mine
    GYPSUM,         // salt / gypsum cave — white formations
    TIDAL,          // sea-level caves with tidal access
    VOLCANIC_VENT,  // active or dormant volcanic fumarole
}

public enum CaveStatus
{
    PRISTINE,       // untouched, full bat/cave fauna
    PROTECTED,      // managed natural reserve
    OPEN_TOURISM,   // commercialised but maintained
    SACRED,         // active religious / ceremonial use
    DEGRADED,       // pollution, disturbance, habitat loss
    ABANDONED,      // derelict mine or collapsed site
}

public static class CaveTypeExtensions
{
    /// <summary>Ambient light colour inside the cave (applied to RenderSettings).</summary>
    public static Color AmbientColor(this CaveType t) => t switch
    {
        CaveType.KARST          => new Color(0.05f, 0.08f, 0.10f), // deep teal-black
        CaveType.LAVA_TUBE      => new Color(0.12f, 0.05f, 0.02f), // deep ember red
        CaveType.SEA_CAVE       => new Color(0.02f, 0.08f, 0.14f), // deep ocean blue
        CaveType.ICE_CAVE       => new Color(0.06f, 0.10f, 0.18f), // ice blue
        CaveType.CRYSTAL        => new Color(0.08f, 0.04f, 0.12f), // deep violet
        CaveType.SACRED_TEMPLE  => new Color(0.10f, 0.07f, 0.03f), // warm amber darkness
        CaveType.BURIAL         => new Color(0.06f, 0.03f, 0.08f), // deep ancestral purple
        CaveType.MINE           => new Color(0.04f, 0.04f, 0.04f), // near total black
        CaveType.GYPSUM         => new Color(0.08f, 0.08f, 0.10f), // grey-white
        CaveType.TIDAL          => new Color(0.03f, 0.07f, 0.12f), // wave-filtered teal
        CaveType.VOLCANIC_VENT  => new Color(0.14f, 0.04f, 0.01f), // deep magma red
        _                       => new Color(0.05f, 0.05f, 0.05f),
    };

    /// <summary>Accent point-light colour visible in the cave interior.</summary>
    public static Color AccentLight(this CaveType t) => t switch
    {
        CaveType.KARST          => new Color(0.40f, 0.80f, 0.70f), // bioluminescent cyan
        CaveType.LAVA_TUBE      => new Color(1.00f, 0.35f, 0.05f), // lava orange
        CaveType.SEA_CAVE       => new Color(0.10f, 0.55f, 0.85f), // sea-filtered blue
        CaveType.ICE_CAVE       => new Color(0.50f, 0.80f, 1.00f), // glacier blue
        CaveType.CRYSTAL        => new Color(0.70f, 0.30f, 1.00f), // crystal violet
        CaveType.SACRED_TEMPLE  => new Color(1.00f, 0.70f, 0.20f), // incense-gold
        CaveType.BURIAL         => new Color(0.55f, 0.20f, 0.80f), // ancestral purple
        CaveType.MINE           => new Color(0.60f, 0.55f, 0.40f), // dust lantern
        CaveType.GYPSUM         => new Color(0.90f, 0.90f, 0.95f), // white mineral
        CaveType.TIDAL          => new Color(0.20f, 0.75f, 0.80f), // bioluminescent teal
        CaveType.VOLCANIC_VENT  => new Color(1.00f, 0.25f, 0.00f), // volcanic red
        _                       => new Color(0.50f, 0.50f, 0.50f),
    };

    public static string DisplayName(this CaveType t) => t switch
    {
        CaveType.KARST          => "Karst Cave",
        CaveType.LAVA_TUBE      => "Lava Tube",
        CaveType.SEA_CAVE       => "Sea Cave",
        CaveType.ICE_CAVE       => "Ice Cave",
        CaveType.CRYSTAL        => "Crystal Cave",
        CaveType.SACRED_TEMPLE  => "Sacred Temple Cave",
        CaveType.BURIAL         => "Burial Cave",
        CaveType.MINE           => "Abandoned Mine",
        CaveType.GYPSUM         => "Gypsum Cave",
        CaveType.TIDAL          => "Tidal Cave",
        CaveType.VOLCANIC_VENT  => "Volcanic Vent",
        _                       => "Cave",
    };

    /// <summary>Barakah change per second when inside. Positive = restore.</summary>
    public static float BarakahRate(this CaveStatus s) => s switch
    {
        CaveStatus.SACRED       =>  4.0f,
        CaveStatus.PRISTINE     =>  3.0f,
        CaveStatus.PROTECTED    =>  2.0f,
        CaveStatus.OPEN_TOURISM =>  0.5f,
        CaveStatus.DEGRADED     => -2.0f,
        CaveStatus.ABANDONED    => -3.0f,
        _                       =>  0f,
    };

    /// <summary>Fog density multiplier applied inside this cave type.</summary>
    public static float FogDensity(this CaveType t) => t switch
    {
        CaveType.KARST          => 0.035f,
        CaveType.LAVA_TUBE      => 0.020f,
        CaveType.VOLCANIC_VENT  => 0.060f,
        CaveType.SEA_CAVE       => 0.025f,
        CaveType.ICE_CAVE       => 0.015f,
        CaveType.CRYSTAL        => 0.010f,
        CaveType.SACRED_TEMPLE  => 0.008f, // incense haze, not thick
        CaveType.BURIAL         => 0.040f,
        CaveType.MINE           => 0.050f,
        _                       => 0.025f,
    };
}

// ════════════════════════════════════════════════════════════════
// CAVE ENTRY
// ════════════════════════════════════════════════════════════════

[Serializable]
public class CaveEntry
{
    [Header("Identity")]
    public string     name;
    public CaveType   caveType;
    public CaveStatus status;

    [Header("Geography")]
    public double latitude;
    public double longitude;
    [Tooltip("Approximate cavern radius in metres — used for entrance trigger")]
    public float  radiusM     = 50f;
    [Tooltip("Depth below surface in metres (approx)")]
    public float  depthM      = 0f;
    [Tooltip("Total known passage length in km")]
    public float  passageKm   = 0f;

    [Header("Narrative")]
    [TextArea(2, 5)]
    public string description;

    [Header("Region")]
    public string region;
    public string country;
}

// ════════════════════════════════════════════════════════════════
// ASIA CAVE DATA
// ════════════════════════════════════════════════════════════════

public static class AsiaCaveData
{
    public static readonly List<CaveEntry> All = new List<CaveEntry>
    {
        // ══ MEGA-KARST — SOUTHEAST ASIA ══════════════════════════════

        new CaveEntry {
            name="Son Doong Cave", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=17.55, longitude=106.28, radiusM=100f, depthM=200f, passageKm=9f,
            region="Quang Binh Province", country="Vietnam",
            description="World's largest cave — its own weather system, jungle, and river inside. Passage large enough for a Boeing 747. First explored 2009.",
        },
        new CaveEntry {
            name="Phong Nha Cave", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=17.58, longitude=106.28, radiusM=60f, depthM=83f, passageKm=7.7f,
            region="Quang Binh Province", country="Vietnam",
            description="UNESCO World Heritage — 300 million year old limestone, 14 grottoes. Cham sacred site before Vietnamese Buddhism arrived.",
        },
        new CaveEntry {
            name="Hang En Cave", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=17.52, longitude=106.25, radiusM=80f, depthM=120f, passageKm=1.6f,
            region="Quang Binh Province", country="Vietnam",
            description="Third largest cave on Earth — entrance large enough to contain a 40-storey building; millions of swiftlets nest inside.",
        },
        new CaveEntry {
            name="Deer Cave (Gua Rusa)", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=4.05, longitude=114.83, radiusM=100f, depthM=0f, passageKm=2f,
            region="Gunung Mulu National Park", country="Malaysia (Sarawak)",
            description="World's largest cave passage by cross-section — 3 million wrinkle-lipped bats exit at dusk in a tornado-like column.",
        },
        new CaveEntry {
            name="Clearwater Cave", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=4.07, longitude=114.80, radiusM=80f, depthM=70f, passageKm=107f,
            region="Gunung Mulu National Park", country="Malaysia (Sarawak)",
            description="Longest cave in Southeast Asia (107 km) — a navigable underground river runs 75m below the surface.",
        },
        new CaveEntry {
            name="Gomantong Caves", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=5.52, longitude=118.06, radiusM=60f, depthM=0f, passageKm=0f,
            region="Sabah", country="Malaysia (Borneo)",
            description="Home to 3 million bats — edible bird's nest harvest by Orang Sungai community using 60m rattan poles for 1,500 years.",
        },
        new CaveEntry {
            name="Tham Luang Cave", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=20.38, longitude=99.87, radiusM=40f, depthM=30f, passageKm=10f,
            region="Chiang Rai", country="Thailand",
            description="Site of the 2018 Wild Boars rescue — 12 boys and coach trapped 18 days. Cave now a memorial and ecological study site.",
        },
        new CaveEntry {
            name="Erawan Cave", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=17.49, longitude=101.71, radiusM=30f, depthM=0f, passageKm=0f,
            region="Phetchabun", country="Thailand",
            description="Sacred cave with a 30m reclining Buddha carved inside — local spirit shrine maintained alongside Buddhist iconography.",
        },
        new CaveEntry {
            name="Phraya Nakhon Cave", caveType=CaveType.KARST, status=CaveStatus.SACRED,
            latitude=12.39, longitude=99.97, radiusM=30f, depthM=0f, passageKm=0f,
            region="Prachuap Khiri Khan", country="Thailand",
            description="Royal pavilion inside a collapsed cave cathedral — sunbeams pierce the sinkhole ceiling at dawn. Sacred to Thai royalty.",
        },
        new CaveEntry {
            name="Hpa-An Caves", caveType=CaveType.KARST, status=CaveStatus.SACRED,
            latitude=16.87, longitude=97.63, radiusM=50f, depthM=0f, passageKm=1f,
            region="Kayin State", country="Myanmar",
            description="Karst towers riddled with Buddhist cave temples — Saddan Cave has a 2km passage walked in silence by pilgrims.",
        },
        new CaveEntry {
            name="Saddan Cave", caveType=CaveType.KARST, status=CaveStatus.SACRED,
            latitude=16.83, longitude=97.58, radiusM=40f, depthM=0f, passageKm=2f,
            region="Kayin State", country="Myanmar",
            description="Pilgrims enter on foot, exit by boat through a flooded passage — Buddha statues reflect in the still black water.",
        },
        new CaveEntry {
            name="Pindaya Caves", caveType=CaveType.KARST, status=CaveStatus.SACRED,
            latitude=20.59, longitude=96.65, radiusM=30f, depthM=0f, passageKm=0.5f,
            region="Shan State", country="Myanmar",
            description="Over 8,000 Buddha images fill the chambers, accumulated over 2,500 years by pilgrims — a mountain of gilded memory.",
        },
        new CaveEntry {
            name="Puerto Princesa Underground River", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=10.17, longitude=118.93, radiusM=80f, depthM=0f, passageKm=8.2f,
            region="Palawan", country="Philippines",
            description="UNESCO World Heritage — 8.2 km navigable underground river ending directly at the South China Sea. Monkeys guard the entrance.",
        },
        new CaveEntry {
            name="Callao Cave", caveType=CaveType.KARST, status=CaveStatus.SACRED,
            latitude=17.89, longitude=121.83, radiusM=30f, depthM=0f, passageKm=0.3f,
            region="Cagayan Valley", country="Philippines",
            description="Seven chambers — the first is a natural cathedral with a sunlit altar. 67,000-year-old Homo luzonensis fossils found nearby.",
        },
        new CaveEntry {
            name="Gua Tempurung", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=4.38, longitude=101.10, radiusM=40f, depthM=0f, passageKm=1.6f,
            region="Perak", country="Malaysia",
            description="One of Peninsular Malaysia's largest caves — five domes, an underground river, and flowstone formations spanning 230 million years.",
        },
        new CaveEntry {
            name="Gua Niah (Niah Caves)", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=3.81, longitude=113.77, radiusM=80f, depthM=0f, passageKm=5f,
            region="Sarawak", country="Malaysia (Borneo)",
            description="Continuous human habitation for 40,000 years — cave paintings, burial canoes, and edible-nest swiftlet harvesting still ongoing.",
        },
        new CaveEntry {
            name="Belum Caves", caveType=CaveType.KARST, status=CaveStatus.PRISTINE,
            latitude=5.48, longitude=101.07, radiusM=30f, depthM=0f, passageKm=0.5f,
            region="Perak (Belum-Temengor)", country="Malaysia",
            description="Deep in the ancient Belum rainforest — stalactite formations and cave swiftlets in near-pristine limestone karst.",
        },
        new CaveEntry {
            name="Langkawi Mahsuri Cave", caveType=CaveType.KARST, status=CaveStatus.SACRED,
            latitude=6.38, longitude=99.89, radiusM=20f, depthM=0f, passageKm=0.2f,
            region="Langkawi", country="Malaysia",
            description="Cave associated with Mahsuri's legend — the princess wrongly executed whose white blood cursed Langkawi for 7 generations.",
        },

        // ══ SACRED TEMPLE CAVES — SOUTH ASIA ════════════════════════

        new CaveEntry {
            name="Ajanta Caves", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=20.55, longitude=75.70, radiusM=200f, depthM=0f, passageKm=0f,
            region="Maharashtra", country="India",
            description="30 Buddhist rock-cut monastery caves, 2nd century BCE to 480 CE — finest surviving ancient Indian art. UNESCO World Heritage.",
        },
        new CaveEntry {
            name="Ellora Caves", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=20.02, longitude=75.18, radiusM=300f, depthM=0f, passageKm=0f,
            region="Maharashtra", country="India",
            description="34 monasteries carved from basalt — Buddhist, Hindu, Jain side by side. Kailasa Temple (Cave 16) carved top-down from a single mountain.",
        },
        new CaveEntry {
            name="Elephanta Caves", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=18.96, longitude=72.93, radiusM=100f, depthM=0f, passageKm=0f,
            region="Mumbai Harbour", country="India",
            description="Island of Shiva — 7th century trimurti of Shiva (Sadashiva), 6m high. Portuguese soldiers used it for target practice, damaging it severely.",
        },
        new CaveEntry {
            name="Amarnath Cave", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.SACRED,
            latitude=34.21, longitude=75.50, radiusM=30f, depthM=0f, passageKm=0.1f,
            region="Kashmir", country="India",
            description="Natural ice Shivalinga at 3,888m altitude — Hindu pilgrimage in extreme conditions. Glacier retreat threatens the natural formation.",
        },
        new CaveEntry {
            name="Batu Caves", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.SACRED,
            latitude=3.24, longitude=101.68, radiusM=100f, depthM=0f, passageKm=0.5f,
            region="Selangor", country="Malaysia",
            description="Thaipusam destination for 1.5 million pilgrims annually — 400m limestone caves, 272 rainbow steps to the Cathedral Cave.",
        },
        new CaveEntry {
            name="Pak Ou Caves (Tham Ting)", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.SACRED,
            latitude=20.11, longitude=102.21, radiusM=30f, depthM=0f, passageKm=0.2f,
            region="Luang Prabang", country="Laos",
            description="Two Mekong-cliff caves holding thousands of Buddha images accumulated over centuries — accessible only by boat.",
        },
        new CaveEntry {
            name="Dambulla Cave Temple", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.SACRED,
            latitude=7.86, longitude=80.65, radiusM=80f, depthM=0f, passageKm=0f,
            region="Central Province", country="Sri Lanka",
            description="Five cave temples with 153 Buddha statues, 1st century BCE — largest and best-preserved Buddhist cave temple in Sri Lanka.",
        },
        new CaveEntry {
            name="Kek Lok Tong Cave Temple", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.SACRED,
            latitude=4.59, longitude=101.07, radiusM=60f, depthM=0f, passageKm=0.3f,
            region="Ipoh, Perak", country="Malaysia",
            description="Cathedral cave with Taoist and Buddhist shrines — rear exit opens to a garden of limestone peaks and koi ponds.",
        },
        new CaveEntry {
            name="Ipoh Sam Poh Tong Cave", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.SACRED,
            latitude=4.56, longitude=101.07, radiusM=50f, depthM=0f, passageKm=0.2f,
            region="Ipoh, Perak", country="Malaysia",
            description="Vertical cave monastery with a turtle pond — Buddhist monks have lived inside since 1890.",
        },

        // ══ SACRED TEMPLE CAVES — CHINA (ROCK-CUT BUDDHIST) ═════════

        new CaveEntry {
            name="Mogao Grottoes (Dunhuang)", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=40.04, longitude=94.81, radiusM=200f, depthM=0f, passageKm=0f,
            region="Gansu Province", country="China",
            description="492 caves of Buddhist art on the Silk Road — 45,000 m² of murals spanning 1,000 years. First printed book found here (868 CE).",
        },
        new CaveEntry {
            name="Yungang Grottoes", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=40.11, longitude=113.13, radiusM=150f, depthM=0f, passageKm=0f,
            region="Shanxi Province", country="China",
            description="51,000 Buddhist stone carvings from 460 CE — the largest 17m Buddha wears the face of Emperor Wencheng. Coal mining threatens the cliff.",
        },
        new CaveEntry {
            name="Longmen Grottoes", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=34.55, longitude=112.47, radiusM=200f, depthM=0f, passageKm=0f,
            region="Henan Province", country="China",
            description="100,000 Buddhist images in 2,345 niches carved over Tang Dynasty — Fengxian Temple's 17m Vairochana Buddha face carved to resemble Empress Wu Zetian.",
        },
        new CaveEntry {
            name="Maijishan Grottoes", caveType=CaveType.SACRED_TEMPLE, status=CaveStatus.PROTECTED,
            latitude=34.38, longitude=105.89, radiusM=80f, depthM=0f, passageKm=0f,
            region="Gansu Province", country="China",
            description="Haystack Mountain — 221 caves carved into a sheer cliff face, accessible by perilous timber walkways. 3rd to 19th century CE.",
        },

        // ══ KARST CAVES — CHINA ══════════════════════════════════════

        new CaveEntry {
            name="Reed Flute Cave (Ludi Yan)", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=25.29, longitude=110.25, radiusM=60f, depthM=0f, passageKm=0.24f,
            region="Guilin, Guangxi", country="China",
            description="240m passage through crystalline formations — Tang Dynasty travellers wrote poems on its walls in 792 CE. Multicoloured lights now controversial.",
        },
        new CaveEntry {
            name="Silver Cave (Yinzi Yan)", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=24.74, longitude=110.61, radiusM=50f, depthM=0f, passageKm=2f,
            region="Yangshuo, Guangxi", country="China",
            description="Nine interconnected caves with silver-toned limestone formations — underground river navigable by boat.",
        },
        new CaveEntry {
            name="Huanglong Cave (Yellow Dragon Cave)", caveType=CaveType.KARST, status=CaveStatus.OPEN_TOURISM,
            latitude=29.04, longitude=110.44, radiusM=60f, depthM=0f, passageKm=7.5f,
            region="Zhangjiajie, Hunan", country="China",
            description="Dragon throne stalactite — 19.2m tall, certified world's largest. Waterfalls, rivers, and 11 halls inside.",
        },
        new CaveEntry {
            name="Furong Cave", caveType=CaveType.KARST, status=CaveStatus.PROTECTED,
            latitude=29.29, longitude=107.96, radiusM=50f, depthM=0f, passageKm=2.7f,
            region="Chongqing", country="China",
            description="UNESCO World Heritage — exceptional density of cave formations including aragonite needle-crystals only found in three caves worldwide.",
        },

        // ══ LAVA TUBES ════════════════════════════════════════════════

        new CaveEntry {
            name="Manjanggul Lava Tube", caveType=CaveType.LAVA_TUBE, status=CaveStatus.PROTECTED,
            latitude=33.53, longitude=126.77, radiusM=40f, depthM=10f, passageKm=7.4f,
            region="Jeju Island", country="South Korea",
            description="World's largest lava tube — 7.4 km passage with a 7.6m lava column at its end. UNESCO World Heritage.",
        },
        new CaveEntry {
            name="Hyeopjae Lava Tube", caveType=CaveType.LAVA_TUBE, status=CaveStatus.PROTECTED,
            latitude=33.39, longitude=126.24, radiusM=20f, depthM=5f, passageKm=0.1f,
            region="Jeju Island", country="South Korea",
            description="Sea-connected lava tube — the tube ends at a coastal cliff where light filters in through a sea arch.",
        },
        new CaveEntry {
            name="Aokigahara Lava Tubes", caveType=CaveType.LAVA_TUBE, status=CaveStatus.PRISTINE,
            latitude=35.47, longitude=138.63, radiusM=30f, depthM=15f, passageKm=2f,
            region="Mt Fuji (Yamanashi)", country="Japan",
            description="Ice Cave and Wind Cave — lava tubes where ice persists year-round at the base of Mt Fuji. Sacred in Shinto tradition.",
        },
        new CaveEntry {
            name="Harrat Khaybar Lava Tubes", caveType=CaveType.LAVA_TUBE, status=CaveStatus.PRISTINE,
            latitude=25.90, longitude=40.10, radiusM=50f, depthM=10f, passageKm=1.5f,
            region="Al Madinah Province", country="Saudi Arabia",
            description="Ancient animal trap system — Neolithic hunters drove animals into lava tube pits. Desert bats roost in the dark sections.",
        },
        new CaveEntry {
            name="Undara Lava Tubes", caveType=CaveType.LAVA_TUBE, status=CaveStatus.PROTECTED,
            latitude=-18.20, longitude=143.76, radiusM=40f, depthM=8f, passageKm=160f,
            region="Queensland", country="Australia (Asia-Pacific)",
            description="World's longest lava tube system at 160 km — hollow tubes formed 190,000 years ago when lava crust hardened over molten flow.",
        },

        // ══ ICE CAVES ═════════════════════════════════════════════════

        new CaveEntry {
            name="Kamchatka Ice Cave", caveType=CaveType.ICE_CAVE, status=CaveStatus.PRISTINE,
            latitude=53.90, longitude=160.10, radiusM=30f, depthM=15f, passageKm=0.5f,
            region="Kamchatka Peninsula", country="Russia",
            description="Glacier melt has carved ice tunnels under Mutnovsky volcano — cobalt blue walls, meltwater streams, and steam from fumaroles.",
        },
        new CaveEntry {
            name="Kyrgyz Ice Cave (Ala Archa)", caveType=CaveType.ICE_CAVE, status=CaveStatus.PRISTINE,
            latitude=42.55, longitude=74.48, radiusM=20f, depthM=30f, passageKm=0.2f,
            region="Bishkek surroundings", country="Kyrgyzstan",
            description="Glacial ice cave at 3,500m — turquoise light penetrates the ice above, turning the cave into a stained-glass chamber.",
        },
        new CaveEntry {
            name="Lhasa Plateau Ice Caves", caveType=CaveType.ICE_CAVE, status=CaveStatus.SACRED,
            latitude=30.0, longitude=91.1, radiusM=25f, depthM=20f, passageKm=0.3f,
            region="Tibet", country="China",
            description="Sacred ice formations in high-altitude limestone — Tibetan Buddhist pilgrims read the ice patterns as oracles.",
        },

        // ══ SEA CAVES ═════════════════════════════════════════════════

        new CaveEntry {
            name="James Bond Island Cave (Khao Phing Kan)", caveType=CaveType.SEA_CAVE, status=CaveStatus.OPEN_TOURISM,
            latitude=8.28, longitude=98.50, radiusM=30f, depthM=0f, passageKm=0.1f,
            region="Phang Nga Bay", country="Thailand",
            description="The Man with the Golden Gun cave — tidal sea cave with turquoise flooding at high tide. Kayakers pass through limestone arches.",
        },
        new CaveEntry {
            name="Viking Cave (Tham Phra Nang)", caveType=CaveType.SEA_CAVE, status=CaveStatus.PROTECTED,
            latitude=7.74, longitude=98.75, radiusM=20f, depthM=0f, passageKm=0.1f,
            region="Railay Beach, Krabi", country="Thailand",
            description="Sea cave with ancient boat paintings left by seafarers — edible-nest swiftlet harvesters still use rattan ladders reaching 60m high.",
        },
        new CaveEntry {
            name="Phi Phi Sea Caves", caveType=CaveType.SEA_CAVE, status=CaveStatus.STRESSED,
            latitude=7.72, longitude=98.77, radiusM=20f, depthM=0f, passageKm=0.1f,
            region="Phi Phi Islands, Krabi", country="Thailand",
            description="Tidal sea caves accessible by kayak — massive pre-2000 tsunami reshaped entrances. Coral visible in clear water below.",
        },
        new CaveEntry {
            name="Fingals Cave equivalent — Swiftlet Cave (Palawan)", caveType=CaveType.SEA_CAVE, status=CaveStatus.PRISTINE,
            latitude=10.90, longitude=119.10, radiusM=30f, depthM=0f, passageKm=0.2f,
            region="Palawan", country="Philippines",
            description="Columnar limestone sea cave — sea swiftlets nest in the hexagonal formations, accessible only at low tide.",
        },

        // ══ CRYSTAL / MINERAL CAVES ═══════════════════════════════════

        new CaveEntry {
            name="Dragon Crystal Cave (Longhua Cave)", caveType=CaveType.CRYSTAL, status=CaveStatus.OPEN_TOURISM,
            latitude=26.04, longitude=107.52, radiusM=40f, depthM=0f, passageKm=6f,
            region="Guizhou Province", country="China",
            description="6 km of cave pools with calcite crystal formations — anthodite cave flowers and cave coral visible underwater.",
        },
        new CaveEntry {
            name="Jeita Grotto", caveType=CaveType.CRYSTAL, status=CaveStatus.OPEN_TOURISM,
            latitude=33.94, longitude=35.64, radiusM=40f, depthM=0f, passageKm=9f,
            region="Mount Lebanon", country="Lebanon",
            description="9km cave system — upper gallery has world's largest stalactite (8.2m); lower gallery navigable by boat on an underground river.",
        },
        new CaveEntry {
            name="Marble Caves of Prabang", caveType=CaveType.CRYSTAL, status=CaveStatus.PRISTINE,
            latitude=21.8, longitude=100.9, radiusM=20f, depthM=0f, passageKm=0.5f,
            region="Yunnan Border", country="China / Myanmar",
            description="White marble-lined cave chambers — karst formations smoothed by ancient river action into polished halls.",
        },

        // ══ BURIAL CAVES ══════════════════════════════════════════════

        new CaveEntry {
            name="Tabon Caves", caveType=CaveType.BURIAL, status=CaveStatus.PROTECTED,
            latitude=8.70, longitude=117.62, radiusM=40f, depthM=0f, passageKm=0f,
            region="Palawan", country="Philippines",
            description="22,000-year-old human remains — the oldest in the Philippines. Secondary burial jars and ritual ochre staining still visible.",
        },
        new CaveEntry {
            name="Tana Toraja Burial Caves", caveType=CaveType.BURIAL, status=CaveStatus.SACRED,
            latitude=-3.03, longitude=119.88, radiusM=50f, depthM=0f, passageKm=0f,
            region="Sulawesi", country="Indonesia",
            description="Limestone cliff tombs — tau-tau effigies (life-size wooden guardians) watch the valley. Burial caves reserved by family lineage for centuries.",
        },
        new CaveEntry {
            name="Sangiran Cave Burial Sites", caveType=CaveType.BURIAL, status=CaveStatus.PROTECTED,
            latitude=-7.44, longitude=110.83, radiusM=30f, depthM=0f, passageKm=0f,
            region="Central Java", country="Indonesia",
            description="Homo erectus fossil cave sites — UNESCO World Heritage. 1.5 million year old paleoanthropological record of human migration.",
        },
        new CaveEntry {
            name="Madai Caves", caveType=CaveType.BURIAL, status=CaveStatus.SACRED,
            latitude=4.72, longitude=117.69, radiusM=30f, depthM=0f, passageKm=0f,
            region="Sabah", country="Malaysia (Borneo)",
            description="Ancient Idahan burial caves — ceramic burial jars and brass ornaments inside stalactite chambers. Edible-nest harvest festival held annually.",
        },

        // ══ VOLCANIC VENTS / HYDROTHERMAL ════════════════════════════

        new CaveEntry {
            name="Ijen Volcanic Vent", caveType=CaveType.VOLCANIC_VENT, status=CaveStatus.PRISTINE,
            latitude=-8.06, longitude=114.24, radiusM=20f, depthM=0f, passageKm=0f,
            region="East Java", country="Indonesia",
            description="Sulphur miners descend into volcanic fumes nightly — electric-blue fire from burning sulphur gas. Acid pH 0.3 lake at the base.",
        },
        new CaveEntry {
            name="Kamchatka Valley of Geysers Vents", caveType=CaveType.VOLCANIC_VENT, status=CaveStatus.PROTECTED,
            latitude=54.43, longitude=160.15, radiusM=30f, depthM=0f, passageKm=0f,
            region="Kronotsky Reserve", country="Russia (Kamchatka)",
            description="Fumarole vents between geyser eruptions — superheated steam creates micro-caves in volcanic rock, inhabited by thermophilic bacteria.",
        },

        // ══ GYPSUM / SALT CAVES ═══════════════════════════════════════

        new CaveEntry {
            name="Kungur Ice Cave", caveType=CaveType.GYPSUM, status=CaveStatus.OPEN_TOURISM,
            latitude=57.43, longitude=56.94, radiusM=40f, depthM=30f, passageKm=5.7f,
            region="Perm Krai (Ural)", country="Russia",
            description="5.7 km gypsum cave with 70 grottos and frozen underground lakes — ice formations persist year-round in the permafrost section.",
        },
        new CaveEntry {
            name="Khewra Salt Mine Caves", caveType=CaveType.GYPSUM, status=CaveStatus.OPEN_TOURISM,
            latitude=32.65, longitude=73.01, radiusM=60f, depthM=50f, passageKm=40f,
            region="Punjab", country="Pakistan",
            description="World's second largest salt mine — 40 km of passages; pink Himalayan salt walls glow orange-pink. Mined since the 13th century.",
        },
        new CaveEntry {
            name="Dead Sea Salt Caves", caveType=CaveType.GYPSUM, status=CaveStatus.PRISTINE,
            latitude=31.50, longitude=35.45, radiusM=20f, depthM=0f, passageKm=0.3f,
            region="Jordan Valley", country="Israel / Jordan",
            description="World's longest known salt caves — Malham Cave (10 km) formed inside a salt diapir at the world's lowest elevation.",
        },

        // ══ TIDAL CAVES ═══════════════════════════════════════════════

        new CaveEntry {
            name="Hang Sơn Đoòng Tidal Entrance", caveType=CaveType.TIDAL, status=CaveStatus.PROTECTED,
            latitude=17.55, longitude=106.29, radiusM=20f, depthM=0f, passageKm=0f,
            region="Quang Binh", country="Vietnam",
            description="Seasonal tidal river floods the first 200m of Son Doong's entrance — creating a natural gate that opens and closes each year.",
        },
        new CaveEntry {
            name="Batu Caves Ramayana Tidal Grotto", caveType=CaveType.TIDAL, status=CaveStatus.SACRED,
            latitude=3.23, longitude=101.68, radiusM=15f, depthM=0f, passageKm=0.1f,
            region="Selangor", country="Malaysia",
            description="Lower cave complex below main Batu Caves — once connected to a seasonal water table; Hindu murals painted on the walls.",
        },
    };

    /// <summary>Find all caves within radiusKm of a GPS point.</summary>
    public static List<CaveEntry> GetNearby(double lat, double lon, float radiusKm)
    {
        var result = new List<CaveEntry>();
        foreach (var c in All)
        {
            double dLat   = lat - c.latitude;
            double dLon   = lon - c.longitude;
            double distKm = Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
            if (distKm * 1000.0 < c.radiusM + radiusKm * 1000.0)
                result.Add(c);
        }
        return result;
    }

    /// <summary>Find nearest cave and return distance in km, or null if none within maxKm.</summary>
    public static CaveEntry GetNearest(double lat, double lon, float maxKm, out double distKm)
    {
        CaveEntry best = null;
        distKm = double.MaxValue;
        foreach (var c in All)
        {
            double dLat = lat - c.latitude;
            double dLon = lon - c.longitude;
            double d    = Math.Sqrt(dLat * dLat + dLon * dLon) * 111.0;
            if (d < distKm && d < maxKm) { distKm = d; best = c; }
        }
        return best;
    }
}
