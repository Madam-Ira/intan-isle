using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Flora entry from JSON database ──────────────────────────────────

    /// <summary>
    /// Runtime data for a single flora species loaded from the JSON database.
    /// Fields match the 17-field schema in Resources/Flora/*.json.
    /// </summary>
    [Serializable]
    public class FloraSpeciesData
    {
        public int id;
        public string name;
        public string scientific;
        public string category;
        public string subCategory;
        public string gameCategory;
        public string region;
        public string countries;
        public string habitat;
        public string conservation;
        public string ecologicalRole;
        public string significance;
        public string edible;
        public string assetType;
        public string size;
        public string glowLocation;
        public string teamlabMood;
        public string unityNotes;
    }

    [Serializable]
    internal class FloraSpeciesList
    {
        public List<FloraSpeciesData> items = new List<FloraSpeciesData>();
    }

    // ── Placed flora instance ───────────────────────────────────────────

    /// <summary>Runtime instance of a placed flora in the world.</summary>
    public class FloraInstance
    {
        public FloraSpeciesData speciesData;
        public GameObject gameObject;
        public GameObject veiledVFXObject;
        public bool isDiscovered;
        public float distanceToPlayer;
    }

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>Published when the player discovers a new flora species.</summary>
    public struct FloraDiscoveredEvent
    {
        public string SpeciesName;
        public string Scientific;
        public string GameCategory;
        public string Region;
        public string Conservation;
        public string Significance;
        public bool IsEndangered;
    }

    /// <summary>Published when the player interacts with a flora.</summary>
    public struct FloraInteractEvent
    {
        public string SpeciesName;
        public string EdibleInfo;
        public string EcologicalRole;
        public string GlowDescription;
    }

    // ── Telemetry ───────────────────────────────────────────────────────

    public static class FloraTelemetryEvents
    {
        public const string FloraDiscovered = "FLORA_DISCOVERED";
        public const string FloraInteracted = "FLORA_INTERACTED";
        public const string FloraVeiledViewed = "FLORA_VEILED_VIEWED";
    }

    // ── Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that loads the 480-species flora database from JSON,
    /// spawns 3D instances at zone-appropriate positions, manages discovery
    /// tracking, and connects each flora to the Wood Wide Web VFX system
    /// in the Veiled World dimension.
    ///
    /// Human World: flora appears as normal 3D assets with zone-appropriate
    /// placement (canopy trees high, ground herbs low, aquatic at water).
    ///
    /// Veiled World: each flora activates its bioluminescent glow, connects
    /// to the Wood Wide Web network, and responds to the player's Blessing
    /// level with varying intensity.
    /// </summary>
    public class FloraAssetManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static FloraAssetManager Instance { get; private set; }

        [Header("Flora Database")]
        [Tooltip("JSON files in Resources/Flora/ are loaded automatically.")]
        [SerializeField] private string[] floraJsonResources = {
            "Flora/IntanIsle_Flora_Orchids_AllRegions",
            "Flora/IntanIsle_Flora_Trees_AllRegions",
            "Flora/IntanIsle_Flora_Flowers_AllRegions",
            "Flora/IntanIsle_Flora_Aquatic_and_Mangrove_AllRegions",
            "Flora/IntanIsle_Flora_Fruits_AllRegions",
            "Flora/IntanIsle_Flora_Shrubs_AllRegions",
            "Flora/IntanIsle_Flora_Herbs_AllRegions",
            "Flora/IntanIsle_Flora_Mushrooms_AllRegions",
            "Flora/IntanIsle_Flora_Vegetables_AllRegions",
            "Flora/IntanIsle_Flora_Carnivorous_and_Specialist_AllRegions"
        };

        [Header("Placement")]
        [SerializeField] private Transform player;
        [SerializeField] private float spawnRadius = 200f;
        [SerializeField] private float cullDistance = 500f;
        [SerializeField] private float discoveryDistance = 5f;
        [SerializeField] private int maxActiveInstances = 30;
        [SerializeField] private float updateInterval = 3f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Prefab Fallbacks")]
        [SerializeField] private GameObject defaultTreePrefab;
        [SerializeField] private GameObject defaultFlowerPrefab;
        [SerializeField] private GameObject defaultHerbPrefab;
        [SerializeField] private GameObject defaultMushroomPrefab;
        [SerializeField] private GameObject defaultAquaticPrefab;

        [Header("Veiled World VFX")]
        [SerializeField] private Material veiledGlowMaterial;
        [SerializeField] private float baseGlowIntensity = 0.5f;
        [SerializeField] private float blessingGlowMultiplier = 1.5f;

        // ── Runtime state ───────────────────────────────────────────────

        private readonly List<FloraSpeciesData> _database = new List<FloraSpeciesData>();
        private readonly List<FloraInstance> _activeInstances = new List<FloraInstance>();
        private readonly HashSet<string> _discoveredSpecies = new HashSet<string>();
        private readonly HashSet<Vector2Int> _occupiedCells = new HashSet<Vector2Int>();
        private float _timer;

        /// <summary>Total species in the database.</summary>
        public int DatabaseCount => _database.Count;

        /// <summary>Number of species the player has discovered.</summary>
        public int DiscoveredCount => _discoveredSpecies.Count;

        /// <summary>All loaded species data.</summary>
        public IReadOnlyList<FloraSpeciesData> Database => _database;

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadDatabase();
            LoadDiscoveredFromPrefs();
        }

        private void OnEnable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
            {
                veil.OnVeiledWorldEntered += OnVeilEnter;
                veil.OnVeiledWorldExited += OnVeilExit;
            }
        }

        private void OnDisable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
            {
                veil.OnVeiledWorldEntered -= OnVeilEnter;
                veil.OnVeiledWorldExited -= OnVeilExit;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;

            CullDistant();
            SpawnNearby();
            CheckDiscovery();
        }

        // ── Database loading ────────────────────────────────────────────

        private void LoadDatabase()
        {
            _database.Clear();

            foreach (string path in floraJsonResources)
            {
                TextAsset json = Resources.Load<TextAsset>(path);
                if (json == null) continue;

                // Unity's JsonUtility needs a wrapper for arrays
                string wrapped = "{\"items\":" + json.text + "}";

                try
                {
                    FloraSpeciesList list = JsonUtility.FromJson<FloraSpeciesList>(wrapped);
                    if (list != null && list.items != null)
                        _database.AddRange(list.items);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FloraAssetManager] Failed to parse {path}: {e.Message}");
                }
            }

            Debug.Log($"[FloraAssetManager] Loaded {_database.Count} flora species from {floraJsonResources.Length} packs.");
        }

        // ── Spawn + cull ────────────────────────────────────────────────

        private void SpawnNearby()
        {
            if (player == null || _database.Count == 0) return;
            if (_activeInstances.Count >= maxActiveInstances) return;

            // Pick a random species appropriate to current zone
            ZoneShaderLinker zsl = FindObjectOfType<ZoneShaderLinker>();
            ZoneType currentZone = zsl != null ? zsl.CurrentZoneType : ZoneType.PROTECTED_FOREST;

            FloraSpeciesData species = PickSpeciesForZone(currentZone);
            if (species == null) return;

            // Random position near player
            Vector2 rnd = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = player.position + new Vector3(rnd.x, 0f, rnd.y);

            // Grid check to avoid overlap
            float spacing = 15f;
            Vector2Int cell = new Vector2Int(
                Mathf.FloorToInt(candidate.x / spacing),
                Mathf.FloorToInt(candidate.z / spacing));

            if (_occupiedCells.Contains(cell)) return;

            // Raycast to ground
            if (Physics.Raycast(candidate + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 400f, groundLayer))
                candidate = hit.point;

            // Adjust Y for habitat type
            candidate.y += GetHeightOffsetForHabitat(species);

            // Instantiate
            GameObject prefab = GetPrefabForCategory(species.gameCategory);
            if (prefab == null) return;

            GameObject go = Instantiate(prefab, candidate,
                Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), transform);
            go.name = $"Flora_{species.name}_{species.region}";

            // Scale variation by size field
            float scale = GetScaleFromSizeField(species.size);
            go.transform.localScale = Vector3.one * scale;

            // Create Veiled World VFX child (hidden by default)
            GameObject vfxChild = CreateVeiledVFXChild(go, species);

            FloraInstance instance = new FloraInstance
            {
                speciesData = species,
                gameObject = go,
                veiledVFXObject = vfxChild,
                isDiscovered = _discoveredSpecies.Contains(GetSpeciesKey(species))
            };

            _activeInstances.Add(instance);
            _occupiedCells.Add(cell);
        }

        private void CullDistant()
        {
            if (player == null) return;
            float sqrCull = cullDistance * cullDistance;

            for (int i = _activeInstances.Count - 1; i >= 0; i--)
            {
                FloraInstance inst = _activeInstances[i];
                if (inst.gameObject == null)
                {
                    _activeInstances.RemoveAt(i);
                    continue;
                }

                float sqrDist = (player.position - inst.gameObject.transform.position).sqrMagnitude;
                if (sqrDist > sqrCull)
                {
                    Vector3 pos = inst.gameObject.transform.position;
                    Vector2Int cell = new Vector2Int(
                        Mathf.FloorToInt(pos.x / 15f),
                        Mathf.FloorToInt(pos.z / 15f));
                    _occupiedCells.Remove(cell);

                    Destroy(inst.gameObject);
                    _activeInstances.RemoveAt(i);
                }
            }
        }

        // ── Discovery ───────────────────────────────────────────────────

        private void CheckDiscovery()
        {
            if (player == null) return;
            float sqrDisc = discoveryDistance * discoveryDistance;

            foreach (FloraInstance inst in _activeInstances)
            {
                if (inst.isDiscovered || inst.gameObject == null) continue;

                float sqrDist = (player.position - inst.gameObject.transform.position).sqrMagnitude;
                if (sqrDist > sqrDisc) continue;

                // Discovered!
                inst.isDiscovered = true;
                string key = GetSpeciesKey(inst.speciesData);
                bool isNew = _discoveredSpecies.Add(key);

                if (isNew)
                {
                    SaveDiscoveredToPrefs();

                    bool isEndangered = inst.speciesData.conservation == "Critically Endangered"
                        || inst.speciesData.conservation == "Endangered"
                        || inst.speciesData.conservation == "Vulnerable";

                    PublishViaBus(new FloraDiscoveredEvent
                    {
                        SpeciesName = inst.speciesData.name,
                        Scientific = inst.speciesData.scientific,
                        GameCategory = inst.speciesData.gameCategory,
                        Region = inst.speciesData.region,
                        Conservation = inst.speciesData.conservation,
                        Significance = inst.speciesData.significance,
                        IsEndangered = isEndangered
                    });

                    // Blessing for discovery
                    float blessing = isEndangered ? 6f : 3f;
                    PublishViaBus(new BlessingDeltaRequest
                    {
                        Delta = blessing,
                        Reason = $"Discovered {inst.speciesData.name} ({inst.speciesData.conservation})"
                    });

                    // Curriculum: ecology + biology
                    CurriculumEngine engine = CurriculumEngine.Instance;
                    if (engine != null)
                    {
                        engine.RecordActivity("B05_Ecology", 1f);
                        engine.RecordActivity("B02_Plant_Biology", 1f);
                    }

                    PublishTelemetry(FloraTelemetryEvents.FloraDiscovered,
                        $"{{\"name\":\"{inst.speciesData.name}\",\"region\":\"{inst.speciesData.region}\",\"conservation\":\"{inst.speciesData.conservation}\"}}");
                }
            }
        }

        // ── Veiled World VFX ────────────────────────────────────────────

        private GameObject CreateVeiledVFXChild(GameObject parent, FloraSpeciesData species)
        {
            // Create a child object with emission for Veiled World glow
            GameObject vfx = new GameObject("VeiledGlow");
            vfx.transform.SetParent(parent.transform, false);
            vfx.SetActive(false); // hidden until Veiled World entered

            // Add a point light for bioluminescent effect
            Light glowLight = vfx.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.range = GetScaleFromSizeField(species.size) * 3f;
            glowLight.intensity = baseGlowIntensity;
            glowLight.color = ParseGlowColor(species.glowLocation);

            return vfx;
        }

        private void OnVeilEnter()
        {
            BlessingMeterController bmc = BlessingMeterController.Instance;
            float blessingFactor = bmc != null
                ? bmc.CachedBlessingScore / 100f * blessingGlowMultiplier
                : 1f;

            foreach (FloraInstance inst in _activeInstances)
            {
                if (inst.veiledVFXObject != null)
                {
                    inst.veiledVFXObject.SetActive(true);

                    // Scale glow by Blessing level
                    Light glow = inst.veiledVFXObject.GetComponent<Light>();
                    if (glow != null)
                        glow.intensity = baseGlowIntensity * blessingFactor;
                }
            }

            // Notify Wood Wide Web
            WoodWideWebVFX www = FindObjectOfType<WoodWideWebVFX>();
            if (www != null)
                www.ShowNetwork();
        }

        private void OnVeilExit()
        {
            foreach (FloraInstance inst in _activeInstances)
            {
                if (inst.veiledVFXObject != null)
                    inst.veiledVFXObject.SetActive(false);
            }

            WoodWideWebVFX www = FindObjectOfType<WoodWideWebVFX>();
            if (www != null)
                www.HideNetwork();
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private FloraSpeciesData PickSpeciesForZone(ZoneType zone)
        {
            // Filter by habitat compatibility with zone type
            string habitat = GetHabitatForZone(zone);

            // Weighted random: prefer species matching current zone's habitat
            List<FloraSpeciesData> candidates = new List<FloraSpeciesData>();
            foreach (FloraSpeciesData sp in _database)
            {
                if (sp.habitat != null && sp.habitat.ToLowerInvariant().Contains(habitat))
                    candidates.Add(sp);
            }

            // Fallback: any species
            if (candidates.Count == 0 && _database.Count > 0)
                return _database[UnityEngine.Random.Range(0, _database.Count)];

            return candidates.Count > 0
                ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
                : null;
        }

        private static string GetHabitatForZone(ZoneType zone)
        {
            switch (zone)
            {
                case ZoneType.ANCIENT_FOREST:
                case ZoneType.PROTECTED_FOREST:
                case ZoneType.SACRED_FOREST: return "forest";
                case ZoneType.MANGROVE: return "mangrove";
                case ZoneType.WATERWAY: return "water";
                case ZoneType.KAMPUNG_HERITAGE: return "garden";
                case ZoneType.FOOD_SECURITY: return "garden";
                case ZoneType.DEFORESTATION: return "dry";
                case ZoneType.POLLUTION:
                case ZoneType.TOXIC: return "disturbed";
                default: return "tropical";
            }
        }

        private GameObject GetPrefabForCategory(string gameCategory)
        {
            switch (gameCategory)
            {
                case "Trees": return defaultTreePrefab;
                case "Flowers":
                case "Orchids": return defaultFlowerPrefab;
                case "Herbs": return defaultHerbPrefab;
                case "Mushrooms": return defaultMushroomPrefab;
                case "Aquatic & Mangrove": return defaultAquaticPrefab;
                default: return defaultFlowerPrefab;
            }
        }

        private static float GetHeightOffsetForHabitat(FloraSpeciesData species)
        {
            string notes = species.unityNotes ?? "";
            if (notes.Contains("Living Water")) return -0.5f;
            if (species.gameCategory == "Mushrooms") return 0f;
            return 0.1f;
        }

        private static float GetScaleFromSizeField(string sizeField)
        {
            if (string.IsNullOrEmpty(sizeField)) return 1f;

            // Parse first number from size field (e.g. "15–30m" → 15)
            string cleaned = "";
            foreach (char c in sizeField)
            {
                if (char.IsDigit(c) || c == '.') cleaned += c;
                else if (cleaned.Length > 0) break;
            }

            if (float.TryParse(cleaned, out float meters))
            {
                // Scale to reasonable Unity units (1 unit ≈ 1m)
                return Mathf.Clamp(meters * 0.1f, 0.2f, 5f);
            }

            return 1f;
        }

        private static Color ParseGlowColor(string glowDesc)
        {
            if (string.IsNullOrEmpty(glowDesc)) return new Color(0.3f, 0.8f, 0.5f);

            string lower = glowDesc.ToLowerInvariant();

            if (lower.Contains("crimson") || lower.Contains("red"))
                return new Color(0.8f, 0.1f, 0.15f);
            if (lower.Contains("blue"))
                return new Color(0.1f, 0.4f, 0.9f);
            if (lower.Contains("violet") || lower.Contains("purple"))
                return new Color(0.5f, 0.1f, 0.8f);
            if (lower.Contains("gold") || lower.Contains("amber"))
                return new Color(0.9f, 0.7f, 0.2f);
            if (lower.Contains("pink"))
                return new Color(0.9f, 0.4f, 0.6f);
            if (lower.Contains("jade") || lower.Contains("green"))
                return new Color(0.2f, 0.8f, 0.4f);
            if (lower.Contains("white"))
                return new Color(0.9f, 0.95f, 1f);
            if (lower.Contains("orange"))
                return new Color(0.9f, 0.5f, 0.1f);

            // Default: bioluminescent teal
            return new Color(0.3f, 0.8f, 0.6f);
        }

        private static string GetSpeciesKey(FloraSpeciesData species)
        {
            return $"{species.region}_{species.id}_{species.name}";
        }

        // ── Persistence ─────────────────────────────────────────────────

        private const string PrefsKey = "DiscoveredFlora";

        private void SaveDiscoveredToPrefs()
        {
            string joined = string.Join("|", _discoveredSpecies);
            PlayerPrefs.SetString(PrefsKey, joined);
            PlayerPrefs.Save();
        }

        private void LoadDiscoveredFromPrefs()
        {
            string saved = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(saved)) return;

            foreach (string key in saved.Split('|'))
            {
                if (!string.IsNullOrEmpty(key))
                    _discoveredSpecies.Add(key);
            }
        }

        // ── EventBus ────────────────────────────────────────────────────

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null) bus.Publish(eventData);
        }

        private void PublishTelemetry(string eventType, string jsonPayload)
        {
            PublishViaBus(new TelemetryRequestEvent
            {
                EventType = eventType,
                JsonPayload = jsonPayload
            });
        }
    }
}
