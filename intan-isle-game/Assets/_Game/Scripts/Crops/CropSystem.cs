using System;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Event structs ───────────────────────────────────────────────────

    /// <summary>Published when a crop tile reaches growth stage 4 (harvest-ready).</summary>
    public struct CropReadyEvent
    {
        /// <summary>Grid row of the ready tile.</summary>
        public int Row;

        /// <summary>Grid column of the ready tile.</summary>
        public int Col;

        /// <summary>Crop type planted in the tile.</summary>
        public string CropType;
    }

    /// <summary>Published when a crop is successfully harvested.</summary>
    public struct CropHarvestedEvent
    {
        /// <summary>Grid row.</summary>
        public int Row;

        /// <summary>Grid column.</summary>
        public int Col;

        /// <summary>Crop type that was harvested.</summary>
        public string CropType;

        /// <summary>Final yield value.</summary>
        public float Yield;

        /// <summary>Harvest mode used.</summary>
        public HarvestMode Mode;
    }

    /// <summary>Published when over-harvest waste is detected.</summary>
    public struct WasteDetectedEvent
    {
        public int ConsecutiveFullHarvests;
        public string Message;
    }

    /// <summary>Harvest mode determines yield, sustainability, and soil impact.</summary>
    public enum HarvestMode
    {
        /// <summary>Full: crop removed, 100% yield, soilHealth -0.10.</summary>
        Full,
        /// <summary>Partial: 60% yield, plant regrows, soilHealth -0.04 (sustainable).</summary>
        Partial,
        /// <summary>Compost: failed/excess crop → CompostBin, soilHealth +0.15 (circular).</summary>
        Compost
    }

    /// <summary>MVP crop types that map to rabbit feed.</summary>
    public static class CropTypes
    {
        public const string Hay = "hay";
        public const string LeafyGreens = "leafy_greens";
        public const string Barley = "barley";
        public const string Herbs = "herbs";

        /// <summary>
        /// Maps crop type → rabbit feed type for the supply chain link.
        /// </summary>
        public static string ToRabbitFeedType(string cropType)
        {
            switch (cropType)
            {
                case Hay: return "hay";
                case LeafyGreens: return "greens";
                case Barley: return "pellets";
                case Herbs: return "greens";
                default: return "hay";
            }
        }
    }

    // ── Inner data class ────────────────────────────────────────────────

    /// <summary>Runtime state for a single crop tile.</summary>
    [Serializable]
    public class CropTile
    {
        public string tileId;
        public int row;
        public int col;
        public string cropType;
        public int growthStage;
        public float waterLevel;
        public float soilHealth = 1f;
        public float waterConsistencyBonus = 1f;
        public long plantedAtMs;
        public long harvestedAtMs;
        public bool isReady;
        public float growthProgress;
        public int wateredHourCount;
        public int totalGrowthHours;
        public bool isWaterlogged;
        public int consecutiveFullHarvests;
    }

    // ── Telemetry constants ─────────────────────────────────────────────

    /// <summary>Telemetry event type constants for the crop system.</summary>
    public static class CropTelemetryEvents
    {
        public const string CropPlanted = "CROP_PLANTED";
        public const string CropWatered = "CROP_WATERED";
        public const string CropReady = "CROP_READY";
        public const string CropHarvested = "CROP_HARVESTED";
        public const string CropComposted = "CROP_COMPOSTED";
        public const string WasteDetected = "WASTE_DETECTED";
        public const string SoilWaterlogged = "SOIL_WATERLOGGED";
    }

    // ── Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that manages the 3×4 crop grid. Growth advances each game hour
    /// when adequately watered and outside Night/Midnight phases. Pollution
    /// above 0.6 halves the growth rate.
    /// </summary>
    public class CropSystem : MonoBehaviour
    {
        private const int Rows = 3;
        private const int Cols = 4;
        private const int MaxGrowthStage = 4;
        private const float WaterThreshold = 0.3f;
        private const float PollutionGrowthThreshold = 0.6f;
        private const float PollutionGrowthMultiplier = 0.5f;
        private const float GrowthPerHour = 1f;
        private const float HoursPerStage = 6f;
        private const float WaterDrainPerHour = 0.05f;
        private const float MaxOfflineGameHours = 72f;
        private const float WaterlogThreshold = 0.9f;
        private const float WaterlogSoilDamage = 0.05f;
        private const float FullHarvestSoilDamage = 0.10f;
        private const float PartialHarvestSoilDamage = 0.04f;
        private const float CompostSoilBonus = 0.15f;
        private const float PartialYieldMultiplier = 0.6f;
        private const int OverHarvestThreshold = 3;
        private const float OverHarvestBlessingPenalty = -3f;

        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static CropSystem Instance { get; private set; }

        [Header("Tuning")]
        [SerializeField] private float baseYield = 1f;
        [SerializeField] private float waterDrainPerHour = WaterDrainPerHour;
        [SerializeField] private float hoursPerStage = HoursPerStage;
        [SerializeField] private float pollutionGrowthThreshold = PollutionGrowthThreshold;

        /// <summary>
        /// Current zone pollution level (0–1). Set externally by PollutionZoneManager.
        /// </summary>
        [SerializeField] private float zonePollutionLevel;

        private readonly CropTile[,] _grid = new CropTile[Rows, Cols];

        // ── Public properties ───────────────────────────────────────────

        /// <summary>Number of rows in the crop grid.</summary>
        public int GridRows => Rows;

        /// <summary>Number of columns in the crop grid.</summary>
        public int GridCols => Cols;

        /// <summary>
        /// Current zone pollution level (0–1). Set by PollutionZoneManager.
        /// When above 0.6 growth rate is halved.
        /// </summary>
        public float ZonePollutionLevel
        {
            get => zonePollutionLevel;
            set => zonePollutionLevel = Mathf.Clamp01(value);
        }

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitialiseGrid();
        }

        private void OnEnable()
        {
            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnGameHourPassed += HandleGameHourPassed;
        }

        private void OnDisable()
        {
            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnGameHourPassed -= HandleGameHourPassed;
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Plants a crop in the specified tile. Fails if the tile is already occupied.
        /// </summary>
        /// <param name="row">Grid row (0-based).</param>
        /// <param name="col">Grid column (0-based).</param>
        /// <param name="cropType">Identifier of the crop to plant.</param>
        /// <returns>True if planting succeeded.</returns>
        public bool PlantCrop(int row, int col, string cropType)
        {
            if (!IsValidTile(row, col)) return false;
            if (string.IsNullOrEmpty(cropType)) return false;

            CropTile tile = _grid[row, col];
            if (!string.IsNullOrEmpty(tile.cropType)) return false;

            tile.cropType = cropType;
            tile.growthStage = 0;
            tile.growthProgress = 0f;
            tile.waterLevel = 0.5f;
            tile.soilHealth = 1f;
            tile.waterConsistencyBonus = 1f;
            tile.isReady = false;
            tile.plantedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            tile.harvestedAtMs = 0;
            tile.wateredHourCount = 0;
            tile.totalGrowthHours = 0;

            PublishTelemetry(CropTelemetryEvents.CropPlanted,
                $"{{\"row\":{row},\"col\":{col},\"cropType\":\"{cropType}\"}}");

            return true;
        }

        /// <summary>
        /// Adds water to a tile from the specified source. Consumes from
        /// <see cref="WaterManager"/>'s daily budget. Clamped to 0–1.
        /// </summary>
        /// <param name="row">Grid row.</param>
        /// <param name="col">Grid column.</param>
        /// <param name="amount">Requested water amount (0–1 scale).</param>
        /// <param name="source">Water source to draw from.</param>
        /// <returns>Actual amount of water added.</returns>
        public float WaterTile(int row, int col, float amount, WaterSource source = WaterSource.Well)
        {
            if (!IsValidTile(row, col)) return 0f;

            // Consume from WaterManager budget
            float consumed = amount;
            WaterManager wm = WaterManager.Instance;
            if (wm != null)
                consumed = wm.ConsumeWater(amount, source);

            if (consumed <= 0f) return 0f;

            CropTile tile = _grid[row, col];
            tile.waterLevel = Mathf.Clamp01(tile.waterLevel + consumed);

            PublishTelemetry(CropTelemetryEvents.CropWatered,
                $"{{\"row\":{row},\"col\":{col},\"waterLevel\":{tile.waterLevel:F4},\"source\":\"{source}\"}}");

            return consumed;
        }

        /// <summary>
        /// Harvests the crop at the given tile using the specified mode.
        /// Full: 100% yield, soil -0.10, resets tile.
        /// Partial: 60% yield, soil -0.04, plant regrows from stage 2.
        /// Compost: 0% yield, soil +0.15 (circular economy).
        /// </summary>
        public (string cropType, float yield) HarvestTile(int row, int col, HarvestMode mode = HarvestMode.Full)
        {
            if (!IsValidTile(row, col)) return (string.Empty, 0f);

            CropTile tile = _grid[row, col];

            // Compost works on any planted tile (even failed crops)
            if (mode == HarvestMode.Compost)
                return CompostTile(tile, row, col);

            // Full and Partial require harvest-ready
            if (string.IsNullOrEmpty(tile.cropType) || tile.growthStage < MaxGrowthStage)
                return (string.Empty, 0f);

            float pollutionPenalty = zonePollutionLevel > pollutionGrowthThreshold
                ? zonePollutionLevel : 0f;

            float yieldMultiplier = mode == HarvestMode.Partial ? PartialYieldMultiplier : 1f;

            float yieldValue = baseYield
                * tile.soilHealth
                * tile.waterConsistencyBonus
                * (1f - pollutionPenalty)
                * yieldMultiplier;

            yieldValue = Mathf.Max(0f, yieldValue);

            string harvested = tile.cropType;
            tile.harvestedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Soil impact
            float soilDamage = mode == HarvestMode.Partial ? PartialHarvestSoilDamage : FullHarvestSoilDamage;
            tile.soilHealth = Mathf.Clamp01(tile.soilHealth - soilDamage);

            PublishViaBus(new CropHarvestedEvent
            {
                Row = row, Col = col,
                CropType = harvested,
                Yield = yieldValue,
                Mode = mode
            });

            PublishTelemetry(CropTelemetryEvents.CropHarvested,
                $"{{\"row\":{row},\"col\":{col},\"cropType\":\"{harvested}\",\"yield\":{yieldValue:F4},\"mode\":\"{mode}\"}}");

            // Blessing for crops tended without waste
            PublishViaBus(new BlessingDeltaRequest
            {
                Delta = 4f,
                Reason = $"Crop {harvested} harvested ({mode})"
            });

            if (mode == HarvestMode.Full)
            {
                tile.consecutiveFullHarvests++;
                CheckOverHarvest(tile, row, col);
                ResetTile(tile);
            }
            else // Partial: regrow from stage 2
            {
                tile.consecutiveFullHarvests = 0;
                tile.growthStage = 2;
                tile.growthProgress = 0f;
                tile.isReady = false;
            }

            return (harvested, yieldValue);
        }

        /// <summary>
        /// Composts the crop in a tile. No yield, but restores soil health +0.15.
        /// Resets the consecutive full-harvest counter (circular economy).
        /// </summary>
        private (string cropType, float yield) CompostTile(CropTile tile, int row, int col)
        {
            if (string.IsNullOrEmpty(tile.cropType))
                return (string.Empty, 0f);

            string composted = tile.cropType;
            tile.soilHealth = Mathf.Clamp01(tile.soilHealth + CompostSoilBonus);
            tile.consecutiveFullHarvests = 0;

            PublishTelemetry(CropTelemetryEvents.CropComposted,
                $"{{\"row\":{row},\"col\":{col},\"cropType\":\"{composted}\",\"soilHealth\":{tile.soilHealth:F4}}}");

            PublishViaBus(new BlessingDeltaRequest
            {
                Delta = 4f,
                Reason = $"Composted {composted} — soil restored"
            });

            ResetTile(tile);
            return (composted, 0f);
        }

        /// <summary>Returns the CropTile data for inspection. Null if invalid.</summary>
        public CropTile GetTile(int row, int col)
        {
            return IsValidTile(row, col) ? _grid[row, col] : null;
        }

        /// <summary>
        /// Returns the current growth stage (0–4) of a tile.
        /// </summary>
        /// <param name="row">Grid row.</param>
        /// <param name="col">Grid column.</param>
        /// <returns>Growth stage, or -1 if tile is invalid or empty.</returns>
        public int GetGrowthStage(int row, int col)
        {
            if (!IsValidTile(row, col)) return -1;
            CropTile tile = _grid[row, col];
            return string.IsNullOrEmpty(tile.cropType) ? -1 : tile.growthStage;
        }

        /// <summary>
        /// Applies offline time degradation to all tiles, capped at 72 game hours.
        /// Water drains and growth advances for each elapsed hour.
        /// </summary>
        /// <param name="elapsedGameHours">Game hours elapsed while offline.</param>
        public void ApplyElapsedTime(float elapsedGameHours)
        {
            float capped = Mathf.Min(elapsedGameHours, MaxOfflineGameHours);
            if (capped <= 0f) return;

            int wholeHours = Mathf.FloorToInt(capped);
            for (int h = 0; h < wholeHours; h++)
            {
                TickAllTiles(TimeOfDay.Morning);
            }
        }

        // ── Hourly tick ─────────────────────────────────────────────────

        private void HandleGameHourPassed(float currentGameHour)
        {
            GameTimeManager time = GameTimeManager.Instance;
            if (time == null) return;

            TickAllTiles(time.CurrentTimeOfDay);
        }

        private void TickAllTiles(TimeOfDay phase)
        {
            bool isNight = phase == TimeOfDay.Night || phase == TimeOfDay.Midnight;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    CropTile tile = _grid[r, c];
                    if (string.IsNullOrEmpty(tile.cropType)) continue;
                    if (tile.isReady) continue;

                    // Drain water
                    tile.waterLevel = Mathf.Clamp01(tile.waterLevel - waterDrainPerHour);

                    // Over-watering: waterLevel > 0.9 damages soil
                    if (tile.waterLevel > WaterlogThreshold)
                    {
                        if (!tile.isWaterlogged)
                        {
                            tile.isWaterlogged = true;
                            PublishTelemetry(CropTelemetryEvents.SoilWaterlogged,
                                $"{{\"row\":{r},\"col\":{c}}}");
                        }
                        tile.soilHealth = Mathf.Clamp01(tile.soilHealth - WaterlogSoilDamage);
                    }
                    else
                    {
                        tile.isWaterlogged = false;
                    }

                    // Growth paused during Night and Midnight
                    if (isNight) continue;

                    // Growth requires adequate water
                    if (tile.waterLevel <= WaterThreshold) continue;

                    tile.totalGrowthHours++;
                    tile.wateredHourCount++;

                    // Water consistency bonus: ratio of watered hours to total hours
                    tile.waterConsistencyBonus = tile.totalGrowthHours > 0
                        ? (float)tile.wateredHourCount / tile.totalGrowthHours
                        : 1f;

                    float rate = GrowthPerHour;
                    if (zonePollutionLevel > pollutionGrowthThreshold)
                        rate *= PollutionGrowthMultiplier;

                    tile.growthProgress += rate;

                    // Advance stages
                    while (tile.growthProgress >= hoursPerStage && tile.growthStage < MaxGrowthStage)
                    {
                        tile.growthProgress -= hoursPerStage;
                        tile.growthStage++;
                    }

                    // Cap at max stage
                    if (tile.growthStage >= MaxGrowthStage)
                    {
                        tile.growthStage = MaxGrowthStage;
                        tile.growthProgress = 0f;

                        if (!tile.isReady)
                        {
                            tile.isReady = true;

                            PublishViaBus(new CropReadyEvent
                            {
                                Row = tile.row,
                                Col = tile.col,
                                CropType = tile.cropType
                            });

                            PublishTelemetry(CropTelemetryEvents.CropReady,
                                $"{{\"row\":{tile.row},\"col\":{tile.col},\"cropType\":\"{tile.cropType}\"}}");
                        }
                    }
                }
            }
        }

        // ── Over-harvest detection ───────────────────────────────────────

        private void CheckOverHarvest(CropTile tile, int row, int col)
        {
            if (tile.consecutiveFullHarvests < OverHarvestThreshold) return;

            PublishViaBus(new WasteDetectedEvent
            {
                ConsecutiveFullHarvests = tile.consecutiveFullHarvests,
                Message = $"Tile ({row},{col}): {tile.consecutiveFullHarvests} full harvests without composting."
            });

            PublishViaBus(new BlessingDeltaRequest
            {
                Delta = OverHarvestBlessingPenalty,
                Reason = $"Over-harvest detected — {tile.consecutiveFullHarvests} consecutive full harvests, no compost"
            });

            // Educational prompt (Elder NPC)
            PublishViaBus(new EducationalPromptEvent
            {
                Topic = "SoilHealth",
                Message = "The soil is tired. Composting returns nutrients to the earth. The land remembers what you give back.",
                RabbitId = string.Empty
            });

            PublishTelemetry(CropTelemetryEvents.WasteDetected,
                $"{{\"row\":{row},\"col\":{col},\"consecutive\":{tile.consecutiveFullHarvests}}}");
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void InitialiseGrid()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    _grid[r, c] = new CropTile
                    {
                        tileId = $"tile_{r}_{c}",
                        row = r,
                        col = c
                    };
                }
            }
        }

        private void ResetTile(CropTile tile)
        {
            tile.cropType = null;
            tile.growthStage = 0;
            tile.growthProgress = 0f;
            tile.waterLevel = 0f;
            tile.soilHealth = 1f;
            tile.waterConsistencyBonus = 1f;
            tile.isReady = false;
            tile.plantedAtMs = 0;
            tile.harvestedAtMs = 0;
            tile.wateredHourCount = 0;
            tile.totalGrowthHours = 0;
        }

        private static bool IsValidTile(int row, int col)
        {
            return row >= 0 && row < Rows && col >= 0 && col < Cols;
        }

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
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