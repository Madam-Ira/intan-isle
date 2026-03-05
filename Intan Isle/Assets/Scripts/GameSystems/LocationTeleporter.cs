using System.Collections;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Location Teleporter — press T to open.
/// All ~49 UN-recognised Asian sovereign states (capitals ★) + ecology sites.
///
/// Uses CesiumGeoreference.SetOriginLongitudeLatitudeHeight to re-centre the
/// floating origin before placing the player, preventing float-precision jitter.
/// </summary>
public class LocationTeleporter : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    // Preset catalogue
    // ★ = national capital   |   altitude = metres above ellipsoid
    // High-altitude capitals include city elevation in the offset.
    // ─────────────────────────────────────────────────────────────────────
    private static readonly Location[] Presets =
    {
        // ════════════════════════════════════════════════════════════════
        // OVERVIEW
        // ════════════════════════════════════════════════════════════════
        new Location("Asia — Continental View",             35.00,   90.00, 3_000_000, "OVERVIEW"),
        new Location("East Asia — Regional View",           38.00,  120.00, 1_500_000, "OVERVIEW"),
        new Location("Southeast Asia — Regional View",       5.00,  110.00,   800_000, "OVERVIEW"),
        new Location("South Asia — Regional View",          25.00,   78.00, 1_000_000, "OVERVIEW"),
        new Location("Central Asia — Regional View",        43.00,   65.00, 1_200_000, "OVERVIEW"),
        new Location("Western Asia — Regional View",        32.00,   45.00, 1_500_000, "OVERVIEW"),
        new Location("South China Sea",                      8.00,  113.00,   300_000, "OVERVIEW"),
        new Location("Indian Ocean",                        -5.00,   73.00,   500_000, "OVERVIEW"),

        // ════════════════════════════════════════════════════════════════
        // EAST ASIA
        // ════════════════════════════════════════════════════════════════
        // China
        new Location("China — Beijing ★",                  39.9042, 116.4074,   500, "East Asia"),
        new Location("China — Yangtze River Pollution",    30.50,   114.30,     300, "East Asia"),
        new Location("China — Yellow River (Huang He)",    34.00,   110.00,     400, "East Asia"),
        new Location("China — Pearl River Delta Smog",     22.50,   113.50,     300, "East Asia"),
        new Location("China — Loess Plateau Erosion",      36.00,   110.00,     600, "East Asia"),
        new Location("China — Three Gorges Dam",           30.82,   111.00,     400, "East Asia"),
        new Location("China — Gobi Desert",                42.00,   105.00,     500, "East Asia"),
        new Location("China — Tibetan Plateau",            31.00,    90.00,    5200, "East Asia"),
        new Location("China — Jiuzhaigou Valley",          33.27,   103.92,    2500, "East Asia"),
        // Japan
        new Location("Japan — Tokyo ★",                   35.6762, 139.6503,   400, "East Asia"),
        new Location("Japan — Mt. Fuji (Sacred)",         35.36,   138.73,    3776, "East Asia"),
        new Location("Japan — Sea of Japan Overfishing",  38.00,   135.00,     300, "East Asia"),
        new Location("Japan — Shiretoko Peninsula",       44.10,   145.00,     500, "East Asia"),
        new Location("Japan — Aokigahara Forest",         35.43,   138.63,     500, "East Asia"),
        // North Korea
        new Location("North Korea — Pyongyang ★",         39.0392, 125.7625,   400, "East Asia"),
        new Location("North Korea — Mt. Paektu (Baekdu)", 41.99,   128.07,    2744, "East Asia"),
        new Location("North Korea — Demilitarized Zone",  38.00,   127.00,     300, "East Asia"),
        // South Korea
        new Location("South Korea — Seoul ★",             37.5665, 126.9780,   400, "East Asia"),
        new Location("South Korea — DMZ Wild Corridor",   38.10,   127.10,     400, "East Asia"),
        new Location("South Korea — Jeju Island Volcano", 33.36,   126.53,    1950, "East Asia"),
        // Mongolia
        new Location("Mongolia — Ulaanbaatar ★",          47.8864, 106.9057,   400, "East Asia"),
        new Location("Mongolia — Gobi Desert",            44.00,   104.00,     600, "East Asia"),
        new Location("Mongolia — Khuvsgul Lake",          51.00,   100.50,    1700, "East Asia"),
        new Location("Mongolia — Steppe Grasslands",      47.00,   102.00,    1400, "East Asia"),
        // Taiwan
        new Location("Taiwan — Taipei ★ (disputed)",      25.0330, 121.5654,   400, "East Asia"),
        new Location("Taiwan — Taroko Gorge",             24.15,   121.62,    1500, "East Asia"),
        new Location("Taiwan — Yushan (Jade Mountain)",   23.47,   120.96,    3952, "East Asia"),

        // ════════════════════════════════════════════════════════════════
        // SOUTHEAST ASIA
        // ════════════════════════════════════════════════════════════════
        // Brunei
        new Location("Brunei — Bandar Seri Begawan ★",     4.9031, 114.9398,   400, "Southeast Asia"),
        new Location("Brunei — Temburong Rainforest",      4.50,   115.20,     500, "Southeast Asia"),
        new Location("Brunei — Baram River Basin",         4.20,   114.50,     300, "Southeast Asia"),
        // Cambodia
        new Location("Cambodia — Phnom Penh ★",           11.5564, 104.9282,   300, "Southeast Asia"),
        new Location("Cambodia — Tonle Sap Lake",         12.50,   104.00,     200, "Southeast Asia"),
        new Location("Cambodia — Cardamom Mountains",     11.50,   103.20,    1000, "Southeast Asia"),
        new Location("Cambodia — Mekong Irrawaddy Dolphin",13.50,  106.00,     200, "Southeast Asia"),
        // Indonesia
        new Location("Indonesia — Jakarta ★",             -6.2088, 106.8456,   300, "Southeast Asia"),
        new Location("Indonesia — Raja Ampat Coral",      -0.50,   130.50,     300, "Southeast Asia"),
        new Location("Indonesia — Kalimantan Deforestation",0.00,  114.00,     400, "Southeast Asia"),
        new Location("Indonesia — Sumatra — Leuser",       3.62,    97.48,     600, "Southeast Asia"),
        new Location("Indonesia — Riau Peatlands Fire",    0.50,   102.50,     200, "Southeast Asia"),
        new Location("Indonesia — Mt. Rinjani",           -8.41,   116.47,    3726, "Southeast Asia"),
        new Location("Indonesia — Komodo Island",         -8.55,   119.48,     400, "Southeast Asia"),
        // Laos
        new Location("Laos — Vientiane ★",                17.9757, 102.6331,   300, "Southeast Asia"),
        new Location("Laos — Mekong River",               18.00,   102.50,     300, "Southeast Asia"),
        new Location("Laos — Nam Ha Forest",              20.80,   101.90,     900, "Southeast Asia"),
        new Location("Laos — Plain of Jars",              19.45,   103.18,    1000, "Southeast Asia"),
        // Malaysia
        new Location("Malaysia — Kuala Lumpur ★",          3.1390, 101.6869,   400, "Southeast Asia"),
        new Location("Malaysia — Taman Negara Rainforest",  4.50,  102.50,     600, "Southeast Asia"),
        new Location("Malaysia — Kinabatangan River",       5.41,  118.00,     300, "Southeast Asia"),
        new Location("Malaysia — Danum Valley",             4.96,  117.79,     500, "Southeast Asia"),
        new Location("Malaysia — Coral Triangle Sabah",     6.00,  118.50,     300, "Southeast Asia"),
        // Myanmar
        new Location("Myanmar — Naypyidaw ★",             19.7633,  96.0785,   300, "Southeast Asia"),
        new Location("Myanmar — Irrawaddy River",         18.80,    95.20,     200, "Southeast Asia"),
        new Location("Myanmar — Hukawng Valley Tiger",    26.50,    96.50,     600, "Southeast Asia"),
        new Location("Myanmar — Inle Lake",               20.50,    96.90,     900, "Southeast Asia"),
        new Location("Myanmar — Bago Yoma Forests",       18.00,    96.00,     400, "Southeast Asia"),
        // Philippines
        new Location("Philippines — Manila ★",            14.5995, 120.9842,   300, "Southeast Asia"),
        new Location("Philippines — Tubbataha Reef",       9.00,   119.90,     300, "Southeast Asia"),
        new Location("Philippines — Mt. Apo",              6.99,   125.27,    2954, "Southeast Asia"),
        new Location("Philippines — Palawan Rainforest",   9.50,   118.50,     400, "Southeast Asia"),
        new Location("Philippines — Mayon Volcano",        13.25,  123.68,    2462, "Southeast Asia"),
        // Singapore
        new Location("Singapore — City State ★",           1.3521, 103.8198,   300, "Southeast Asia"),
        new Location("Singapore — Jurong Island Industrial",1.265, 103.670,    200, "Southeast Asia"),
        new Location("Singapore — Pulau Ubin (wild coast)", 1.404, 103.960,    200, "Southeast Asia"),
        // Thailand
        new Location("Thailand — Bangkok ★",              13.7563, 100.5018,   300, "Southeast Asia"),
        new Location("Thailand — Chao Phraya Pollution",  14.00,   100.50,     300, "Southeast Asia"),
        new Location("Thailand — Doi Inthanon Peak",      18.58,    98.49,    2565, "Southeast Asia"),
        new Location("Thailand — Phang Nga Bay Karst",     8.30,    98.50,     300, "Southeast Asia"),
        new Location("Thailand — Gulf of Thailand Reef",  10.00,   101.00,     300, "Southeast Asia"),
        // Timor-Leste
        new Location("Timor-Leste — Dili ★",             -8.5569, 125.5786,   300, "Southeast Asia"),
        new Location("Timor-Leste — Coral Coast",        -8.80,   125.00,     300, "Southeast Asia"),
        new Location("Timor-Leste — Nino Konis Santana", -8.60,   127.10,     600, "Southeast Asia"),
        // Vietnam
        new Location("Vietnam — Hanoi ★",                21.0285, 105.8542,   300, "Southeast Asia"),
        new Location("Vietnam — Ha Long Bay",             20.90,   107.10,     300, "Southeast Asia"),
        new Location("Vietnam — Mekong Delta",             9.75,   105.60,      80, "Southeast Asia"),
        new Location("Vietnam — Phong Nha Caves",         17.55,   106.28,     400, "Southeast Asia"),
        new Location("Vietnam — Cat Tien National Park",  11.42,   107.43,     400, "Southeast Asia"),
        // Regional ecology
        new Location("Strait of Malacca — Haze Corridor", 2.50,   101.00,    1000, "Southeast Asia"),
        new Location("Sundarbans — Mangrove Coast",       21.93,    89.18,      80, "Southeast Asia"),
        new Location("Coral Triangle — Sulawesi Sea",     -1.50,   120.00,     400, "Southeast Asia"),

        // ════════════════════════════════════════════════════════════════
        // SOUTH ASIA
        // ════════════════════════════════════════════════════════════════
        // Afghanistan
        new Location("Afghanistan — Kabul ★",             34.5553,  69.2075,  1900, "South Asia"),
        new Location("Afghanistan — Hindu Kush Mountains", 35.50,    70.00,   3500, "South Asia"),
        new Location("Afghanistan — Wakhan Corridor",      37.00,    73.50,   4000, "South Asia"),
        new Location("Afghanistan — Hamoun Wetlands",      31.00,    61.50,    500, "South Asia"),
        // Bangladesh
        new Location("Bangladesh — Dhaka ★",              23.8103,  90.4125,   300, "South Asia"),
        new Location("Bangladesh — Sundarbans Mangrove",  21.90,    89.20,      80, "South Asia"),
        new Location("Bangladesh — Ganges-Brahmaputra Delta",22.50, 91.00,      80, "South Asia"),
        new Location("Bangladesh — Haor Wetlands",        24.50,    91.20,     200, "South Asia"),
        // Bhutan
        new Location("Bhutan — Thimphu ★",               27.4728,  89.6390,  2700, "South Asia"),
        new Location("Bhutan — Phobjikha Valley (Cranes)",27.70,    89.90,   2900, "South Asia"),
        new Location("Bhutan — Jigme Dorji Nat. Park",   27.50,    89.40,   3000, "South Asia"),
        new Location("Bhutan — Manas Biosphere Reserve", 26.80,    91.00,    300, "South Asia"),
        // India
        new Location("India — New Delhi ★",               28.6139,  77.2090,   400, "South Asia"),
        new Location("India — Western Ghats Biodiversity",11.00,    76.50,    800, "South Asia"),
        new Location("India — Thar Desert",               26.90,    71.00,    400, "South Asia"),
        new Location("India — Kaziranga (One-Horned Rhino)",26.57,  93.17,    300, "South Asia"),
        new Location("India — Sundarbans Tiger Reserve",  21.90,    88.90,     80, "South Asia"),
        new Location("India — Gangetic Plain Pollution",  25.50,    82.00,    300, "South Asia"),
        new Location("India — Brahmaputra Valley",        27.00,    93.50,    300, "South Asia"),
        new Location("India — Andaman Islands Reef",      12.00,    92.70,    300, "South Asia"),
        // Maldives
        new Location("Maldives — Malé ★",                 4.1755,  73.5093,   300, "South Asia"),
        new Location("Maldives — North Atoll Coral Reef",  7.00,   73.00,     300, "South Asia"),
        new Location("Maldives — Baa Atoll Biosphere",     5.00,   73.00,     300, "South Asia"),
        // Nepal
        new Location("Nepal — Kathmandu ★",              27.7172,  85.3240,  1700, "South Asia"),
        new Location("Nepal — Everest Base Camp",         27.98,    86.92,   5600, "South Asia"),
        new Location("Nepal — Chitwan National Park",     27.50,    84.30,    300, "South Asia"),
        new Location("Nepal — Annapurna Conservation",    28.50,    84.00,   4000, "South Asia"),
        // Pakistan
        new Location("Pakistan — Islamabad ★",            33.7294,  73.0931,   800, "South Asia"),
        new Location("Pakistan — Indus River Delta",      24.00,    68.00,     300, "South Asia"),
        new Location("Pakistan — Karakoram Range",        36.00,    75.00,   5000, "South Asia"),
        new Location("Pakistan — Thar Desert (east)",     25.00,    70.00,    300, "South Asia"),
        new Location("Pakistan — Indus Dolphin Reserve",  28.50,    70.00,    300, "South Asia"),
        // Sri Lanka
        new Location("Sri Lanka — Sri Jayawardenepura ★",  6.9271,  79.8612,  300, "South Asia"),
        new Location("Sri Lanka — Sinharaja Rainforest",    6.40,   80.40,    800, "South Asia"),
        new Location("Sri Lanka — Adam's Peak",             6.81,   80.50,   2243, "South Asia"),
        new Location("Sri Lanka — Yala National Park",      6.30,   81.60,    300, "South Asia"),
        new Location("Sri Lanka — Coral Reefs (Hikkaduwa)",  6.14,   80.10,   300, "South Asia"),

        // ════════════════════════════════════════════════════════════════
        // CENTRAL ASIA
        // ════════════════════════════════════════════════════════════════
        // Kazakhstan
        new Location("Kazakhstan — Astana ★",             51.1801,  71.4460,   400, "Central Asia"),
        new Location("Kazakhstan — Kazakh Steppe",         51.00,   66.00,     400, "Central Asia"),
        new Location("Kazakhstan — Aral Sea (North)",      46.00,   60.00,     300, "Central Asia"),
        new Location("Kazakhstan — Altai Mountains",       49.50,   86.00,    2500, "Central Asia"),
        new Location("Kazakhstan — Caspian Coast",         44.00,   51.00,     300, "Central Asia"),
        // Kyrgyzstan
        new Location("Kyrgyzstan — Bishkek ★",            42.8746,  74.5698,   800, "Central Asia"),
        new Location("Kyrgyzstan — Issyk-Kul Lake",        42.50,   77.30,    1600, "Central Asia"),
        new Location("Kyrgyzstan — Tian Shan Mountains",  42.00,    80.00,    4000, "Central Asia"),
        // Tajikistan
        new Location("Tajikistan — Dushanbe ★",           38.5598,  68.7738,   800, "Central Asia"),
        new Location("Tajikistan — Pamir Plateau",         38.50,   74.00,    4500, "Central Asia"),
        new Location("Tajikistan — Fedchenko Glacier",     38.80,   72.20,    5000, "Central Asia"),
        // Turkmenistan
        new Location("Turkmenistan — Ashgabat ★",         37.9601,  58.3261,   300, "Central Asia"),
        new Location("Turkmenistan — Karakum Desert",      40.00,   58.00,     300, "Central Asia"),
        new Location("Turkmenistan — Darvaza Gas Crater",  40.25,   58.44,     300, "Central Asia"),
        new Location("Turkmenistan — Aral Sea (South)",    43.00,   58.00,     300, "Central Asia"),
        // Uzbekistan
        new Location("Uzbekistan — Tashkent ★",           41.2995,  69.2401,   500, "Central Asia"),
        new Location("Uzbekistan — Aral Sea (dried bed)",  44.50,   59.50,     300, "Central Asia"),
        new Location("Uzbekistan — Ferghana Valley",       40.50,   71.50,     500, "Central Asia"),
        new Location("Uzbekistan — Kyzylkum Desert",       41.50,   62.00,     300, "Central Asia"),

        // ════════════════════════════════════════════════════════════════
        // WESTERN ASIA
        // ════════════════════════════════════════════════════════════════
        // Armenia
        new Location("Armenia — Yerevan ★",               40.1872,  44.5152,   700, "Western Asia"),
        new Location("Armenia — Lake Sevan",               40.30,    45.30,   1900, "Western Asia"),
        new Location("Armenia — Mt. Aragats",              40.53,    44.21,   4090, "Western Asia"),
        new Location("Armenia — Dilijan Forest",           40.74,    44.86,   1500, "Western Asia"),
        // Azerbaijan
        new Location("Azerbaijan — Baku ★",               40.4093,  49.8671,   400, "Western Asia"),
        new Location("Azerbaijan — Caspian Sea Coast",     40.00,    50.00,    300, "Western Asia"),
        new Location("Azerbaijan — Gobustan Mud Volcanoes",40.10,    49.40,    300, "Western Asia"),
        new Location("Azerbaijan — Greater Caucasus",      41.50,    47.00,   3000, "Western Asia"),
        // Bahrain
        new Location("Bahrain — Manama ★",                26.2235,  50.5876,   300, "Western Asia"),
        new Location("Bahrain — Hawar Islands Marine",     25.60,    50.60,    300, "Western Asia"),
        new Location("Bahrain — Al Areen Wildlife",        26.00,    50.50,    300, "Western Asia"),
        // Cyprus
        new Location("Cyprus — Nicosia ★",                35.1856,  33.3823,   500, "Western Asia"),
        new Location("Cyprus — Akamas Peninsula",          35.10,    32.30,    400, "Western Asia"),
        new Location("Cyprus — Troodos Mountains",         34.93,    32.87,   1952, "Western Asia"),
        // Georgia
        new Location("Georgia — Tbilisi ★",               41.6938,  44.8015,   700, "Western Asia"),
        new Location("Georgia — Greater Caucasus Range",   42.50,    44.00,   3000, "Western Asia"),
        new Location("Georgia — Colchic Rainforest",       42.00,    42.50,    800, "Western Asia"),
        new Location("Georgia — Borjomi-Kharagauli Park",  41.90,    43.30,   1500, "Western Asia"),
        // Iran
        new Location("Iran — Tehran ★",                   35.6892,  51.3890,  1800, "Western Asia"),
        new Location("Iran — Dasht-e Kavir (Salt Desert)", 34.50,   54.00,   1000, "Western Asia"),
        new Location("Iran — Zagros Mountains",            33.00,    50.00,   2500, "Western Asia"),
        new Location("Iran — Caspian Hyrcanian Forest",   37.00,    51.00,    800, "Western Asia"),
        new Location("Iran — Dasht-e Lut (Hot Desert)",   31.00,    58.50,    300, "Western Asia"),
        // Iraq
        new Location("Iraq — Baghdad ★",                  33.3152,  44.3661,   400, "Western Asia"),
        new Location("Iraq — Mesopotamian Marshes",        31.00,    47.00,    200, "Western Asia"),
        new Location("Iraq — Euphrates River",             34.00,    43.50,    300, "Western Asia"),
        new Location("Iraq — Tigris River",                33.50,    44.50,    300, "Western Asia"),
        // Israel
        new Location("Israel — Jerusalem ★",              31.7683,  35.2137,  1100, "Western Asia"),
        new Location("Israel — Negev Desert",              30.50,    34.90,    500, "Western Asia"),
        new Location("Israel — Dead Sea Coast",            31.50,    35.50,   -200, "Western Asia"),
        new Location("Israel — Hula Valley Wetlands",      33.10,    35.60,    100, "Western Asia"),
        // Jordan
        new Location("Jordan — Amman ★",                  31.9539,  35.9106,  1100, "Western Asia"),
        new Location("Jordan — Wadi Rum Desert",           29.60,    35.40,    900, "Western Asia"),
        new Location("Jordan — Dana Biosphere Reserve",   30.70,     35.60,   1500, "Western Asia"),
        new Location("Jordan — Wadi Mujib Canyon",        31.47,     35.59,    300, "Western Asia"),
        // Kuwait
        new Location("Kuwait — Kuwait City ★",            29.3759,  47.9774,   300, "Western Asia"),
        new Location("Kuwait — Kuwait Bay Marine",         29.50,    48.00,    300, "Western Asia"),
        new Location("Kuwait — Bubiyan Island Wetland",   29.80,     48.20,    200, "Western Asia"),
        // Lebanon
        new Location("Lebanon — Beirut ★",                33.8938,  35.5018,   300, "Western Asia"),
        new Location("Lebanon — Cedars of God (Bsharri)", 34.25,    36.05,   2000, "Western Asia"),
        new Location("Lebanon — Bekaa Valley",            33.80,     35.90,    900, "Western Asia"),
        new Location("Lebanon — Qadisha Valley",          34.20,     36.00,   1500, "Western Asia"),
        // Oman
        new Location("Oman — Muscat ★",                   23.5880,  58.3829,   300, "Western Asia"),
        new Location("Oman — Musandam Fjords",            26.00,     56.50,    300, "Western Asia"),
        new Location("Oman — Wahiba Sands Desert",        22.00,     58.50,    300, "Western Asia"),
        new Location("Oman — Dhofar Monsoon Coast",       17.00,     54.50,    400, "Western Asia"),
        new Location("Oman — Wadi Shab Canyon",           22.85,     59.20,    300, "Western Asia"),
        // Palestine
        new Location("Palestine — East Jerusalem ★",      31.7683,  35.2137,  1100, "Western Asia"),
        new Location("Palestine — Jordan River Valley",   31.80,     35.50,    300, "Western Asia"),
        new Location("Palestine — Gaza Coast",            31.50,     34.47,    300, "Western Asia"),
        // Qatar
        new Location("Qatar — Doha ★",                   25.2854,  51.5310,   300, "Western Asia"),
        new Location("Qatar — Al Khor Mangroves",         25.70,    51.50,     300, "Western Asia"),
        new Location("Qatar — Khor Al Adaid Inland Sea",  24.65,    51.40,     300, "Western Asia"),
        // Saudi Arabia
        new Location("Saudi Arabia — Riyadh ★",          24.7136,  46.6753,   600, "Western Asia"),
        new Location("Saudi Arabia — Rub' al Khali (Empty Quarter)",22.00, 55.00, 300, "Western Asia"),
        new Location("Saudi Arabia — Asir Mountains",     18.20,    42.50,    2500, "Western Asia"),
        new Location("Saudi Arabia — Red Sea Reef",       22.00,    38.50,     300, "Western Asia"),
        new Location("Saudi Arabia — Al Ula Ancient Landscape",26.62, 37.93,  800, "Western Asia"),
        // Syria
        new Location("Syria — Damascus ★",                33.5138,  36.2765,  1000, "Western Asia"),
        new Location("Syria — Syrian Desert (Badia)",     34.00,    38.00,     500, "Western Asia"),
        new Location("Syria — Euphrates Valley",          36.00,    38.50,     300, "Western Asia"),
        new Location("Syria — Jabal al-Druze",            32.70,    36.60,    1800, "Western Asia"),
        // Turkey
        new Location("Turkey — Ankara ★",                39.9334,  32.8597,  1200, "Western Asia"),
        new Location("Turkey — Cappadocia",               38.65,    34.83,    1400, "Western Asia"),
        new Location("Turkey — Pamukkale Travertine",     37.92,    29.12,     400, "Western Asia"),
        new Location("Turkey — Bosphorus Strait",         41.10,    29.05,     300, "Western Asia"),
        new Location("Turkey — Taurus Mountains",         37.00,    32.50,    2500, "Western Asia"),
        new Location("Turkey — Göreme Valley",            38.64,    34.83,    1300, "Western Asia"),
        // UAE
        new Location("UAE — Abu Dhabi ★",                24.4539,  54.3773,   300, "Western Asia"),
        new Location("UAE — Hajar Mountains",             25.00,    56.00,    1000, "Western Asia"),
        new Location("UAE — Abu Dhabi Mangroves",         24.50,    54.50,     300, "Western Asia"),
        new Location("UAE — Rub' al Khali Edge (Liwa)",  23.10,    53.60,     300, "Western Asia"),
        // Yemen
        new Location("Yemen — Sana'a ★",                 15.3694,  44.1910,  2800, "Western Asia"),
        new Location("Yemen — Socotra Island (Dragon Trees)",12.50, 54.00,    400, "Western Asia"),
        new Location("Yemen — Hadhramaut Valley",         15.50,    48.50,    1000, "Western Asia"),
        new Location("Yemen — Bab-el-Mandeb Strait",     12.60,    43.50,     300, "Western Asia"),

        // ════════════════════════════════════════════════════════════════
        // NORTH ASIA
        // ════════════════════════════════════════════════════════════════
        new Location("Russia — Moscow ★",                 55.7558,  37.6173,   400, "North Asia"),
        new Location("Russia — Lake Baikal (Deepest Lake)",53.50,  108.00,    600, "North Asia"),
        new Location("Russia — Lena River Delta",          72.00,  126.00,    300, "North Asia"),
        new Location("Russia — Siberian Taiga Boreal",     60.00,   90.00,    400, "North Asia"),
        new Location("Russia — Kamchatka Volcanoes",       54.00,  160.00,   1500, "North Asia"),
        new Location("Russia — Ural Mountains",            60.00,   60.00,   1000, "North Asia"),
        new Location("Russia — Yenisei River",             62.00,   87.00,    400, "North Asia"),
        new Location("Russia — Amur River (China border)", 50.00,  135.00,    300, "North Asia"),
    };

    // ── Runtime ───────────────────────────────────────────────────
    private bool               _open      = false;
    private Vector2            _scroll    = Vector2.zero;
    private string             _customLat = "";
    private string             _customLon = "";
    private string             _customAlt = "500";
    private string             _statusMsg = "";
    private string             _filter    = "";

    private CesiumGeoreference _geo;
    private CesiumGlobeAnchor  _playerAnchor;
    private Transform          _playerRig;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _geo = FindObjectOfType<CesiumGeoreference>();
    }

    void Start()
    {
        var rig = GameObject.Find("PlayerRig");
        if (rig != null)
        {
            _playerRig    = rig.transform;
            _playerAnchor = rig.GetComponent<CesiumGlobeAnchor>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            _open = !_open;
    }

    // ── GUI ───────────────────────────────────────────────────────

    void OnGUI()
    {
        if (!_open) return;

        float w = 420f, h = Mathf.Min(Screen.height * 0.92f, 680f);
        float x = Screen.width  - w - 20f;
        float y = (Screen.height - h) * 0.5f;

        GUI.Box(new Rect(x - 4, y - 4, w + 8, h + 8), GUIContent.none);
        GUILayout.BeginArea(new Rect(x, y, w, h));

        GUILayout.Label("LOCATION TELEPORTER  [T]  —  ~49 Countries · All Ecology Sites", GUI.skin.box);

        // ── Search filter ─────────────────────────────────────────
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        _filter = GUILayout.TextField(_filter);
        if (GUILayout.Button("✕", GUILayout.Width(24))) _filter = "";
        GUILayout.EndHorizontal();

        // ── Preset list ───────────────────────────────────────────
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(h - 178f));

        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.4f, 1f, 0.55f) }
        };
        var capitalStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = new Color(1f, 0.95f, 0.6f) }
        };
        var ecologyStyle = new GUIStyle(GUI.skin.label)
        {
            normal = { textColor = Color.white }
        };

        string lastRegion = null;
        string lf = _filter.ToLowerInvariant();
        bool   filtering = !string.IsNullOrEmpty(_filter);

        foreach (var loc in Presets)
        {
            if (filtering &&
                !loc.label.ToLowerInvariant().Contains(lf) &&
                !loc.zone.ToLowerInvariant().Contains(lf))
                continue;

            // Region header when zone changes
            if (loc.zone != lastRegion)
            {
                lastRegion = loc.zone;
                GUILayout.Space(5f);
                GUILayout.Label($"── {loc.zone} ──", headerStyle);
            }

            bool isCapital = loc.label.Contains("★");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {loc.label}",
                            isCapital ? capitalStyle : ecologyStyle,
                            GUILayout.Width(330f));
            if (GUILayout.Button("Go", GUILayout.Width(38f)))
                StartCoroutine(TeleportTo(loc.lat, loc.lon, loc.height));
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        // ── Legend ────────────────────────────────────────────────
        var legendStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            normal   = { textColor = new Color(0.6f, 0.6f, 0.6f) }
        };
        GUILayout.Label("★ = national capital  (yellow)     ecology sites in white", legendStyle);

        // ── Custom coordinates ────────────────────────────────────
        GUILayout.Space(2f);
        GUILayout.Label("Custom Coordinates:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Lat:",   GUILayout.Width(28)); _customLat = GUILayout.TextField(_customLat, GUILayout.Width(66));
        GUILayout.Label("Lon:",   GUILayout.Width(28)); _customLon = GUILayout.TextField(_customLon, GUILayout.Width(66));
        GUILayout.Label("Alt m:", GUILayout.Width(38)); _customAlt = GUILayout.TextField(_customAlt, GUILayout.Width(55));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Teleport to Custom"))
        {
            if (double.TryParse(_customLat, out double lat) &&
                double.TryParse(_customLon, out double lon) &&
                double.TryParse(_customAlt, out double alt))
                StartCoroutine(TeleportTo(lat, lon, alt));
            else
                _statusMsg = "Invalid coordinates.";
        }

        if (!string.IsNullOrEmpty(_statusMsg))
            GUILayout.Label(_statusMsg);

        GUILayout.EndArea();
    }

    // ── Teleport ──────────────────────────────────────────────────

    private IEnumerator TeleportTo(double lat, double lon, double heightMetres)
    {
        _statusMsg = "Teleporting...";

        if (_geo != null)
            _geo.SetOriginLongitudeLatitudeHeight(lon, lat, 0.0);

        yield return null;

        if (_playerAnchor != null)
        {
            _playerAnchor.longitudeLatitudeHeight = new double3(lon, lat, heightMetres);
        }
        else if (_playerRig != null)
        {
            var cc = _playerRig.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            _playerRig.position = new Vector3(0f, (float)heightMetres, 0f);
            yield return null;
            if (cc != null) cc.enabled = true;
        }

        _statusMsg = $"At {(lat >= 0 ? "N" : "S")}{Mathf.Abs((float)lat):F2}°  " +
                     $"{(lon >= 0 ? "E" : "W")}{Mathf.Abs((float)lon):F2}°  " +
                     $"{heightMetres:F0} m";

        Debug.Log($"[Teleport] → lat={lat:F4} lon={lon:F4} alt={heightMetres:F0}m");
    }

    // ── Data ──────────────────────────────────────────────────────

    private struct Location
    {
        public string label, zone;
        public double lat, lon, height;
        public Location(string label, double lat, double lon, double height, string zone)
        {
            this.label  = label;
            this.lat    = lat;
            this.lon    = lon;
            this.height = height;
            this.zone   = zone;
        }
    }
}
