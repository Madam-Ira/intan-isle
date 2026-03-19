using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Enums ───────────────────────────────────────────────────────────

    /// <summary>Nine-state health machine for a rabbit.</summary>
    public enum RabbitHealthState
    {
        Thriving,
        Content,
        Hungry,
        Thirsty,
        Stressed,
        Unwell,
        Recovered,
        Critical,
        Deceased
    }

    /// <summary>Rabbit gender.</summary>
    public enum RabbitGender
    {
        Buck,
        Doe
    }

    /// <summary>Rabbit lifecycle mode.</summary>
    public enum RabbitMode
    {
        Wild,
        Farmed,
        Processed
    }

    // ── Event structs ───────────────────────────────────────────────────

    /// <summary>Published when a rabbit enters the <see cref="RabbitHealthState.Unwell"/> state.</summary>
    public struct RabbitUnwellEvent
    {
        /// <summary>Identifier of the affected rabbit.</summary>
        public string RabbitId;

        /// <summary>Breed of the affected rabbit.</summary>
        public RabbitBreed Breed;
    }

    /// <summary>Published when a rabbit's health state changes.</summary>
    public struct RabbitHealthStateChangedEvent
    {
        /// <summary>Identifier of the affected rabbit.</summary>
        public string RabbitId;

        /// <summary>Previous health state.</summary>
        public RabbitHealthState Previous;

        /// <summary>New health state.</summary>
        public RabbitHealthState Current;
    }

    /// <summary>Published when a rabbit has been neglected for more than 4 game hours.</summary>
    public struct RabbitNeglectedEvent
    {
        /// <summary>Identifier of the neglected rabbit.</summary>
        public string RabbitId;

        /// <summary>Game hours since last care.</summary>
        public float HoursSinceLastCare;
    }

    /// <summary>Published to request a blessing delta change.</summary>
    public struct BlessingDeltaRequest
    {
        /// <summary>Delta to apply (negative = penalty).</summary>
        public float Delta;

        /// <summary>Reason for the change.</summary>
        public string Reason;
    }

    /// <summary>
    /// Published on every care action. Schema matches spec's digital twin event.
    /// Batched and sent to NurAIN via TelemetryManager.
    /// </summary>
    public struct RabbitCareEvent
    {
        public string PlayerId;
        public string RabbitId;
        public string EventType;
        public float CareValue;
        public long Timestamp;
        public RabbitBreed BreedId;
        public float HealthBefore;
        public float HealthAfter;
        public string SessionId;
    }

    /// <summary>
    /// Published when a care action deviates from the optimal ratio.
    /// Triggers Elder NPC dialogue — never a blocking pop-up.
    /// </summary>
    public struct EducationalPromptEvent
    {
        public string Topic;
        public string Message;
        public string RabbitId;
    }

    /// <summary>Published when a rabbit is harvested.</summary>
    public struct RabbitHarvestedEvent
    {
        public string RabbitId;
        public RabbitBreed Breed;
        public bool WithGratitude;
        public float MeatYieldKg;
    }

    // ── Telemetry constants ─────────────────────────────────────────────

    /// <summary>Telemetry event type constants for the rabbit care system.</summary>
    public static class RabbitTelemetryEvents
    {
        public const string RabbitFed = "RABBIT_FED";
        public const string RabbitWatered = "RABBIT_WATERED";
        public const string RabbitHealthCheck = "RABBIT_HEALTH_CHECK";
        public const string RabbitHealthStateChanged = "RABBIT_HEALTH_STATE_CHANGED";
        public const string RabbitNeglected = "RABBIT_NEGLECTED";
        public const string RabbitHarvested = "RABBIT_HARVESTED";
        public const string RabbitDeceased = "RABBIT_DECEASED";
    }

    // ── Inner data class ────────────────────────────────────────────────

    /// <summary>Runtime data for a single rabbit instance.</summary>
    [Serializable]
    public class RabbitData
    {
        public string rabbitId;
        public string name;
        public RabbitBreed breed;
        public RabbitGender gender;
        public RabbitMode mode = RabbitMode.Farmed;
        public float feedLevel = 1f;
        public float waterLevel = 1f;
        public float cleanliness = 1f;
        public float socialScore = 1f;
        public float healthScore = 1f;
        public float bondScore;
        public RabbitHealthState healthState = RabbitHealthState.Content;
        public long lastCaredAtMs;
        public float hoursInCurrentState;
        public float hoursSinceUnwellResolved;
    }

    // ── Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that manages the 7-state health machine for every rabbit
    /// in the player's care. Subscribes to <see cref="GameTimeManager.OnGameHourPassed"/>
    /// for passive feed/water drain and state evaluation.
    /// </summary>
    public class RabbitCareManager : MonoBehaviour
    {
        private const float FeedDrainPerHour = 0.04f;
        private const float WaterDrainPerHour = 0.06f;
        private const float MaxOfflineGameHours = 72f;
        private const float NeglectThresholdHours = 4f;
        private const float NeglectBlessingPenalty = -8f;

        private const float HungryThreshold = 0.40f;
        private const float ThirstyThreshold = 0.35f;
        private const float StressedThreshold = 0.30f;
        private const float UnwellThreshold = 0.20f;
        private const float GreenThreshold = 0.60f;
        private const float ThrivingRequiredHours = 2f;
        private const float RecoveredRequiredDays = 1f;

        private const float CriticalHoursToDeceased = 48f;
        private const float BondPerCare = 0.03f;
        private const float BondDecayPerHour = 0.005f;
        private const float ScheduledCareBlessingBonus = 5f;
        private const float HarvestGratitudeBlessing = 6f;
        private const float HarvestWasteBlessing = -3f;

        private const float RatioHay = 0.80f;
        private const float RatioPellets = 0.15f;
        private const float RatioGreens = 0.05f;
        private const float RatioTolerance = 0.10f;

        private const string WaterSourceWell = "well";
        private const string WaterSourceAwf = "awf";

        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static RabbitCareManager Instance { get; private set; }

        [SerializeField] private float feedDrainPerHour = FeedDrainPerHour;
        [SerializeField] private float waterDrainPerHour = WaterDrainPerHour;
        [SerializeField] private float neglectThresholdHours = NeglectThresholdHours;
        [SerializeField] private float neglectBlessingPenalty = NeglectBlessingPenalty;

        private readonly Dictionary<string, RabbitData> _rabbits = new Dictionary<string, RabbitData>();
        private readonly List<string> _idCache = new List<string>();

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

        /// <summary>Returns the current <see cref="RabbitHealthState"/> for a rabbit.</summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <returns>Health state, or <see cref="RabbitHealthState.Content"/> if not found.</returns>
        public RabbitHealthState GetHealthState(string rabbitId)
        {
            return _rabbits.TryGetValue(rabbitId, out RabbitData data)
                ? data.healthState
                : RabbitHealthState.Content;
        }

        /// <summary>Returns the current feed level (0–1) for a rabbit.</summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <returns>Feed level, or 0 if not found.</returns>
        public float GetFeedLevel(string rabbitId)
        {
            return _rabbits.TryGetValue(rabbitId, out RabbitData data) ? data.feedLevel : 0f;
        }

        /// <summary>Returns the current water level (0–1) for a rabbit.</summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <returns>Water level, or 0 if not found.</returns>
        public float GetWaterLevel(string rabbitId)
        {
            return _rabbits.TryGetValue(rabbitId, out RabbitData data) ? data.waterLevel : 0f;
        }

        /// <summary>Returns a read-only list of all registered rabbit identifiers.</summary>
        /// <returns>Read-only list of rabbit IDs.</returns>
        public IReadOnlyList<string> GetAllRabbitIds()
        {
            _idCache.Clear();
            _idCache.AddRange(_rabbits.Keys);
            return _idCache;
        }

        /// <summary>
        /// Registers a new rabbit in the care system.
        /// </summary>
        /// <param name="rabbitId">Unique identifier.</param>
        /// <param name="rabbitName">Display name.</param>
        /// <param name="breed">Breed type.</param>
        /// <param name="gender">Gender (Buck or Doe).</param>
        public void RegisterRabbit(string rabbitId, string rabbitName, RabbitBreed breed, RabbitGender gender)
        {
            if (_rabbits.ContainsKey(rabbitId)) return;

            _rabbits[rabbitId] = new RabbitData
            {
                rabbitId = rabbitId,
                name = rabbitName,
                breed = breed,
                gender = gender,
                feedLevel = 1f,
                waterLevel = 1f,
                cleanliness = 1f,
                socialScore = 1f,
                healthScore = 1f,
                healthState = RabbitHealthState.Content,
                lastCaredAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                hoursInCurrentState = 0f,
                hoursSinceUnwellResolved = 0f
            };
        }

        /// <summary>
        /// Removes a rabbit from the care system.
        /// </summary>
        /// <param name="rabbitId">Unique identifier of the rabbit to remove.</param>
        public void UnregisterRabbit(string rabbitId)
        {
            _rabbits.Remove(rabbitId);
        }

        /// <summary>
        /// Performs a health check on a rabbit. Increases bond, awards
        /// Blessing if done regularly, and emits a care event.
        /// </summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <returns>Current health state after check.</returns>
        public RabbitHealthState PerformHealthCheck(string rabbitId)
        {
            if (!_rabbits.TryGetValue(rabbitId, out RabbitData data))
                return RabbitHealthState.Content;

            float healthBefore = data.healthScore;

            // Health check slightly improves healthScore (early detection)
            data.healthScore = Mathf.Clamp01(data.healthScore + 0.05f);
            data.bondScore = Mathf.Clamp01(data.bondScore + BondPerCare);
            data.lastCaredAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Reward proactive care
            if (data.healthState == RabbitHealthState.Content
                || data.healthState == RabbitHealthState.Thriving)
            {
                PublishViaBus(new BlessingDeltaRequest
                {
                    Delta = ScheduledCareBlessingBonus,
                    Reason = $"Proactive health check on {data.name}"
                });
            }

            EvaluateState(data, 0f);

            EmitCareEvent(data, RabbitTelemetryEvents.RabbitHealthCheck,
                data.healthScore, healthBefore, data.healthScore);

            // Educational prompt if issues detected
            if (data.feedLevel < HungryThreshold)
            {
                PublishViaBus(new EducationalPromptEvent
                {
                    Topic = "RabbitNutrition",
                    Message = $"{data.name} seems hungry. Rabbits need 80% hay, 15% pellets, and 5% greens daily.",
                    RabbitId = data.rabbitId
                });
            }

            return data.healthState;
        }

        /// <summary>
        /// Harvests a rabbit with ethical handling. The rabbit is removed from
        /// the care system and yields resources. Blessing rises for gratitude,
        /// falls for waste.
        /// </summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <param name="withGratitude">True if the player acknowledged the harvest with respect.</param>
        /// <returns>True if the harvest succeeded.</returns>
        public bool HarvestRabbit(string rabbitId, bool withGratitude)
        {
            if (!_rabbits.TryGetValue(rabbitId, out RabbitData data)) return false;
            if (data.healthState == RabbitHealthState.Deceased) return false;

            float blessingDelta = withGratitude ? HarvestGratitudeBlessing : HarvestWasteBlessing;
            string reason = withGratitude
                ? $"Harvest of {data.name} performed with gratitude and acknowledgement"
                : $"Harvest of {data.name} performed without care";

            PublishViaBus(new BlessingDeltaRequest { Delta = blessingDelta, Reason = reason });

            PublishViaBus(new RabbitHarvestedEvent
            {
                RabbitId = data.rabbitId,
                Breed = data.breed,
                WithGratitude = withGratitude,
                MeatYieldKg = 0f // Looked up from breed SO at runtime
            });

            data.mode = RabbitMode.Processed;
            EmitCareEvent(data, RabbitTelemetryEvents.RabbitHarvested,
                withGratitude ? 1f : 0f, data.healthScore, 0f);

            _rabbits.Remove(rabbitId);
            return true;
        }

        /// <summary>Returns the bond score (0-1) for a rabbit.</summary>
        public float GetBondScore(string rabbitId)
        {
            return _rabbits.TryGetValue(rabbitId, out RabbitData data) ? data.bondScore : 0f;
        }

        /// <summary>Returns the full runtime data for a rabbit, or null.</summary>
        public RabbitData GetRabbitData(string rabbitId)
        {
            return _rabbits.TryGetValue(rabbitId, out RabbitData data) ? data : null;
        }

        /// <summary>
        /// Applies offline time degradation to a specific rabbit, capped at 72 game hours.
        /// </summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <param name="elapsedGameHours">Game hours elapsed while offline.</param>
        public void ApplyElapsedTime(string rabbitId, float elapsedGameHours)
        {
            if (!_rabbits.TryGetValue(rabbitId, out RabbitData data)) return;

            float capped = Mathf.Min(elapsedGameHours, MaxOfflineGameHours);
            if (capped <= 0f) return;

            data.feedLevel = Mathf.Clamp01(data.feedLevel - feedDrainPerHour * capped);
            data.waterLevel = Mathf.Clamp01(data.waterLevel - waterDrainPerHour * capped);

            EvaluateState(data, capped);
            CheckNeglect(data, capped);
        }

        /// <summary>
        /// Feeds a rabbit. Validates that the feed type matches the breed's
        /// required ratio (80 % hay, 15 % pellets, 5 % greens ± tolerance).
        /// </summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <param name="feedType">One of <c>hay</c>, <c>pellets</c>, or <c>greens</c>.</param>
        /// <param name="amount">Amount to add (0–1 scale).</param>
        /// <returns>True if the feed was accepted.</returns>
        public bool FeedRabbit(string rabbitId, string feedType, float amount)
        {
            if (!_rabbits.TryGetValue(rabbitId, out RabbitData data)) return false;
            if (string.IsNullOrEmpty(feedType) || amount <= 0f) return false;

            string key = feedType.ToLowerInvariant();
            float expectedRatio;

            switch (key)
            {
                case FeedTypes.Hay:
                    expectedRatio = RatioHay;
                    break;
                case FeedTypes.Pellets:
                    expectedRatio = RatioPellets;
                    break;
                case FeedTypes.Greens:
                    expectedRatio = RatioGreens;
                    break;
                default:
                    return false;
            }

            // Validate ratio: the amount relative to a full portion should be
            // within tolerance of the expected ratio.
            float normalised = Mathf.Clamp01(amount);
            if (Mathf.Abs(normalised - expectedRatio) > RatioTolerance && normalised > expectedRatio + RatioTolerance)
                return false;

            float healthBefore = data.healthScore;
            data.feedLevel = Mathf.Clamp01(data.feedLevel + normalised);
            data.bondScore = Mathf.Clamp01(data.bondScore + BondPerCare);
            data.lastCaredAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Blessing for on-schedule feeding (before rabbit gets hungry)
            if (data.healthState == RabbitHealthState.Content
                || data.healthState == RabbitHealthState.Thriving)
            {
                PublishViaBus(new BlessingDeltaRequest
                {
                    Delta = ScheduledCareBlessingBonus,
                    Reason = $"Fed {data.name} on schedule"
                });
            }

            EvaluateState(data, 0f);
            EmitCareEvent(data, RabbitTelemetryEvents.RabbitFed,
                normalised, healthBefore, data.healthScore);

            // Educational prompt if ratio is off
            if (Mathf.Abs(normalised - expectedRatio) > RatioTolerance)
            {
                PublishViaBus(new EducationalPromptEvent
                {
                    Topic = "FeedRatio",
                    Message = $"The ideal feed for {data.name} is {expectedRatio * 100f:F0}% {key}. What you gave was a bit different.",
                    RabbitId = data.rabbitId
                });
            }

            return true;
        }

        /// <summary>
        /// Waters a rabbit from a valid water source.
        /// </summary>
        /// <param name="rabbitId">Unique rabbit identifier.</param>
        /// <param name="waterSource">Must be <c>well</c> or <c>awf</c>.</param>
        /// <returns>True if watering succeeded.</returns>
        public bool WaterRabbit(string rabbitId, string waterSource)
        {
            if (!_rabbits.TryGetValue(rabbitId, out RabbitData data)) return false;
            if (string.IsNullOrEmpty(waterSource)) return false;

            string src = waterSource.ToLowerInvariant();
            if (src != WaterSourceWell && src != WaterSourceAwf) return false;

            float healthBefore = data.healthScore;
            data.waterLevel = Mathf.Clamp01(data.waterLevel + 0.30f);
            data.bondScore = Mathf.Clamp01(data.bondScore + BondPerCare);
            data.lastCaredAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (data.healthState == RabbitHealthState.Content
                || data.healthState == RabbitHealthState.Thriving)
            {
                PublishViaBus(new BlessingDeltaRequest
                {
                    Delta = ScheduledCareBlessingBonus,
                    Reason = $"Watered {data.name} on schedule"
                });
            }

            EvaluateState(data, 0f);
            EmitCareEvent(data, RabbitTelemetryEvents.RabbitWatered,
                0.30f, healthBefore, data.healthScore);

            return true;
        }

        // ── Hourly tick ─────────────────────────────────────────────────

        private void HandleGameHourPassed(float currentGameHour)
        {
            foreach (RabbitData data in _rabbits.Values)
            {
                if (data.healthState == RabbitHealthState.Deceased) continue;

                data.feedLevel = Mathf.Clamp01(data.feedLevel - feedDrainPerHour);
                data.waterLevel = Mathf.Clamp01(data.waterLevel - waterDrainPerHour);
                data.bondScore = Mathf.Clamp01(data.bondScore - BondDecayPerHour);
                data.hoursInCurrentState += 1f;

                if (data.healthState == RabbitHealthState.Recovered)
                    data.hoursSinceUnwellResolved += 1f;

                EvaluateState(data, 1f);
                CheckNeglect(data, 1f);
            }
        }

        // ── State evaluation ────────────────────────────────────────────

        private void EvaluateState(RabbitData data, float deltaHours)
        {
            RabbitHealthState previous = data.healthState;
            RabbitHealthState next = ComputeState(data);

            if (next == previous) return;

            // Recovered gate: must stay stable for 1 game day (24 hours) after leaving Unwell
            if (previous == RabbitHealthState.Unwell && next != RabbitHealthState.Unwell)
            {
                data.healthState = RabbitHealthState.Recovered;
                data.hoursInCurrentState = 0f;
                data.hoursSinceUnwellResolved = 0f;
                OnStateChanged(data, previous, RabbitHealthState.Recovered);
                return;
            }

            if (previous == RabbitHealthState.Recovered)
            {
                if (data.hoursSinceUnwellResolved < RecoveredRequiredDays * 24f)
                    return; // stay in Recovered until stable
            }

            data.healthState = next;
            data.hoursInCurrentState = 0f;
            OnStateChanged(data, previous, next);
        }

        private RabbitHealthState ComputeState(RabbitData data)
        {
            // Deceased: stays deceased
            if (data.healthState == RabbitHealthState.Deceased)
                return RabbitHealthState.Deceased;

            // Critical → Deceased after prolonged neglect
            if (data.healthState == RabbitHealthState.Critical
                && data.hoursInCurrentState >= CriticalHoursToDeceased)
                return RabbitHealthState.Deceased;

            // Count indicators below Unwell threshold
            int criticalCount = 0;
            if (data.feedLevel < UnwellThreshold) criticalCount++;
            if (data.waterLevel < UnwellThreshold) criticalCount++;
            if (data.cleanliness < UnwellThreshold) criticalCount++;
            if (data.socialScore < UnwellThreshold) criticalCount++;
            if (data.healthScore < UnwellThreshold) criticalCount++;

            // Critical: 3+ indicators critical
            if (criticalCount >= 3)
                return RabbitHealthState.Critical;

            // Unwell: 2 indicators critical
            if (criticalCount >= 2)
                return RabbitHealthState.Unwell;

            // Stressed: temp/noise/space proxy indicators below threshold
            if (data.cleanliness < StressedThreshold
                || data.socialScore < StressedThreshold
                || data.healthScore < StressedThreshold)
                return RabbitHealthState.Stressed;

            if (data.waterLevel < ThirstyThreshold)
                return RabbitHealthState.Thirsty;

            if (data.feedLevel < HungryThreshold)
                return RabbitHealthState.Hungry;

            // Count green indicators (>= GreenThreshold)
            int greenCount = 0;
            if (data.feedLevel >= GreenThreshold) greenCount++;
            if (data.waterLevel >= GreenThreshold) greenCount++;
            if (data.cleanliness >= GreenThreshold) greenCount++;
            if (data.socialScore >= GreenThreshold) greenCount++;
            if (data.healthScore >= GreenThreshold) greenCount++;

            if (greenCount >= 5 && data.hoursInCurrentState >= ThrivingRequiredHours)
                return RabbitHealthState.Thriving;

            if (greenCount >= 3)
                return RabbitHealthState.Content;

            return data.healthState;
        }

        private void OnStateChanged(RabbitData data, RabbitHealthState previous, RabbitHealthState current)
        {
            PublishViaBus(new RabbitHealthStateChangedEvent
            {
                RabbitId = data.rabbitId,
                Previous = previous,
                Current = current
            });

            PublishTelemetry(RabbitTelemetryEvents.RabbitHealthStateChanged,
                $"{{\"rabbitId\":\"{data.rabbitId}\",\"from\":\"{previous}\",\"to\":\"{current}\"}}");

            if (current == RabbitHealthState.Unwell)
            {
                PublishViaBus(new RabbitUnwellEvent
                {
                    RabbitId = data.rabbitId,
                    Breed = data.breed
                });
            }

            if (current == RabbitHealthState.Critical)
            {
                PublishViaBus(new EducationalPromptEvent
                {
                    Topic = "RabbitCritical",
                    Message = $"{data.name} is in critical condition. Immediate care is needed — feed, water, and check on them now.",
                    RabbitId = data.rabbitId
                });
            }

            if (current == RabbitHealthState.Deceased)
            {
                PublishTelemetry(RabbitTelemetryEvents.RabbitDeceased,
                    $"{{\"rabbitId\":\"{data.rabbitId}\",\"breed\":\"{data.breed}\"}}");

                PublishViaBus(new BlessingDeltaRequest
                {
                    Delta = -6f,
                    Reason = $"{data.name} has passed away from neglect"
                });
            }
        }

        // ── Neglect check ───────────────────────────────────────────────

        private void CheckNeglect(RabbitData data, float deltaHours)
        {
            GameTimeManager time = GameTimeManager.Instance;
            if (time == null || data.lastCaredAtMs <= 0) return;

            float hoursSinceCare = time.GetElapsedGameHoursSince(data.lastCaredAtMs);

            if (hoursSinceCare >= neglectThresholdHours)
            {
                PublishViaBus(new RabbitNeglectedEvent
                {
                    RabbitId = data.rabbitId,
                    HoursSinceLastCare = hoursSinceCare
                });

                PublishTelemetry(RabbitTelemetryEvents.RabbitNeglected,
                    $"{{\"rabbitId\":\"{data.rabbitId}\",\"hoursSinceCare\":{hoursSinceCare:F2}}}");

                PublishViaBus(new BlessingDeltaRequest
                {
                    Delta = neglectBlessingPenalty,
                    Reason = $"Rabbit {data.name} neglected for {hoursSinceCare:F1} hours"
                });

                // Reset so penalty fires once per threshold crossing
                data.lastCaredAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        // ── Digital twin event emission ──────────────────────────────────

        private void EmitCareEvent(RabbitData data, string eventType,
            float careValue, float healthBefore, float healthAfter)
        {
            PlayerDataManager player = PlayerDataManager.Instance;
            string playerId = player != null ? player.PlayerId : "unknown";

            PublishViaBus(new RabbitCareEvent
            {
                PlayerId = playerId,
                RabbitId = data.rabbitId,
                EventType = eventType,
                CareValue = careValue,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BreedId = data.breed,
                HealthBefore = healthBefore,
                HealthAfter = healthAfter,
                SessionId = playerId // session ID not yet tracked separately
            });
        }

        // ── Helpers ─────────────────────────────────────────────────────

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

        private static class FeedTypes
        {
            public const string Hay = "hay";
            public const string Pellets = "pellets";
            public const string Greens = "greens";
        }
    }
}