using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Event structs ───────────────────────────────────────────────────

    /// <summary>Published when the player reaches a new Blessing milestone.</summary>
    public struct MilestoneUnlockedEvent
    {
        public string MilestoneId;
        public BlessingLevel Level;
        public float ScoreThreshold;
        public string UnlockDescription;
    }

    // ── Milestone definition ────────────────────────────────────────────

    /// <summary>A single milestone with its threshold and unlock payload.</summary>
    [Serializable]
    public class MilestoneConfig
    {
        public string milestoneId;
        public BlessingLevel requiredLevel;
        public float scoreThreshold;
        [TextArea(1, 3)]
        public string unlockDescription;
        public string cinematicKey;
        public bool notifyNurAIN;
    }

    // ── Telemetry constants ─────────────────────────────────────────────

    public static class MilestoneTelemetryEvents
    {
        public const string MilestoneReached = "MILESTONE_REACHED";
    }

    // ── Manager ─────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that listens to <see cref="BlessingUpdatedEvent"/> and fires
    /// <see cref="MilestoneUnlockedEvent"/> when score thresholds are crossed.
    /// Milestones are idempotent — re-checking a passed threshold never
    /// re-fires the event.
    /// </summary>
    public class MilestoneManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static MilestoneManager Instance { get; private set; }

        /// <summary>Read-only access to all milestone configs.</summary>
        public IReadOnlyList<MilestoneConfig> Milestones => _milestones;

        /// <summary>Set of milestone IDs that have been unlocked.</summary>
        public IReadOnlyCollection<string> UnlockedIds => _unlocked;

        private readonly List<MilestoneConfig> _milestones = new List<MilestoneConfig>();
        private readonly HashSet<string> _unlocked = new HashSet<string>();

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

            BuildDefaultMilestones();
            LoadUnlockedFromPrefs();
        }

        private void OnEnable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Subscribe<BlessingUpdatedEvent>(OnBlessingUpdated);
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Unsubscribe<BlessingUpdatedEvent>(OnBlessingUpdated);
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>Returns true if the milestone has been unlocked.</summary>
        public bool IsMilestoneUnlocked(string milestoneId)
        {
            return _unlocked.Contains(milestoneId);
        }

        /// <summary>
        /// Manually checks all milestones against the current score.
        /// Useful on session start to sync state.
        /// </summary>
        public void RecheckAll()
        {
            BlessingMeterController bmc = BlessingMeterController.Instance;
            if (bmc == null) return;

            EvaluateMilestones(bmc.CachedBlessingScore);
        }

        // ── Event handler ───────────────────────────────────────────────

        private void OnBlessingUpdated(BlessingUpdatedEvent evt)
        {
            EvaluateMilestones(evt.Score);
        }

        private void EvaluateMilestones(float score)
        {
            foreach (MilestoneConfig ms in _milestones)
            {
                if (_unlocked.Contains(ms.milestoneId)) continue;
                if (score < ms.scoreThreshold) continue;

                // Unlock — idempotent
                _unlocked.Add(ms.milestoneId);
                SaveUnlockedToPrefs();

                PublishViaBus(new MilestoneUnlockedEvent
                {
                    MilestoneId = ms.milestoneId,
                    Level = ms.requiredLevel,
                    ScoreThreshold = ms.scoreThreshold,
                    UnlockDescription = ms.unlockDescription
                });

                PublishTelemetry(MilestoneTelemetryEvents.MilestoneReached,
                    $"{{\"milestoneId\":\"{ms.milestoneId}\",\"score\":{score:F2}}}");

                if (ms.notifyNurAIN)
                {
                    NurAINConnector connector = NurAINConnector.Instance;
                    if (connector != null)
                        connector.SendCurriculumActivity(
                            $"MILESTONE_{ms.milestoneId}", 1f, $"Score reached {score:F0}");
                }

                Debug.Log($"[MilestoneManager] Unlocked: {ms.milestoneId} at score {score:F0} — {ms.unlockDescription}");
            }
        }

        // ── Default milestones (from spec Section 6) ────────────────────

        private void BuildDefaultMilestones()
        {
            _milestones.Clear();

            // Seedling tier (0-30) — tutorial unlocks
            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "TENDER_FIRST_RABBIT",
                requiredLevel = BlessingLevel.Seedling,
                scoreThreshold = 10f,
                unlockDescription = "Tutorial complete — your first rabbit is entrusted to you.",
                cinematicKey = "cinematic_first_rabbit",
                notifyNurAIN = false
            });

            // Cultivator tier (31-55) — infrastructure
            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "STEWARD_AWF_UNIT",
                requiredLevel = BlessingLevel.Cultivator,
                scoreThreshold = 31f,
                unlockDescription = "AWF Water Unit unlocked — climate-resilient water supply.",
                cinematicKey = "cinematic_awf_unlock",
                notifyNurAIN = true
            });

            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "STEWARD_CROP_EXPANSION",
                requiredLevel = BlessingLevel.Cultivator,
                scoreThreshold = 40f,
                unlockDescription = "Crop grid expanded — more land to tend and feed.",
                cinematicKey = "cinematic_crop_expand",
                notifyNurAIN = false
            });

            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "STEWARD_PAY_IT_FORWARD",
                requiredLevel = BlessingLevel.Cultivator,
                scoreThreshold = 50f,
                unlockDescription = "Pay-It-Forward system unlocked — share your surplus.",
                cinematicKey = "cinematic_pay_forward",
                notifyNurAIN = true
            });

            // Guardian tier (56-75) — Hidden Realm
            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "CULTIVATOR_VEIL_ACCESS",
                requiredLevel = BlessingLevel.Guardian,
                scoreThreshold = 56f,
                unlockDescription = "The Veil thins. You may now step between worlds.",
                cinematicKey = "cinematic_veil_open",
                notifyNurAIN = true
            });

            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "CULTIVATOR_ADVANCED_CROPS",
                requiredLevel = BlessingLevel.Guardian,
                scoreThreshold = 65f,
                unlockDescription = "Advanced crops unlocked — herbs and medicinal plants.",
                cinematicKey = "cinematic_advanced_crops",
                notifyNurAIN = false
            });

            // Shepherd tier (76-90) — deeper mastery
            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "SHEPHERD_SECOND_BREED",
                requiredLevel = BlessingLevel.Shepherd,
                scoreThreshold = 76f,
                unlockDescription = "A new rabbit breed joins your care. Choose wisely.",
                cinematicKey = "cinematic_second_breed",
                notifyNurAIN = true
            });

            // Steward tier (91-100) — Ascension
            _milestones.Add(new MilestoneConfig
            {
                milestoneId = "GUARDIAN_ARK_INSCRIPTION",
                requiredLevel = BlessingLevel.Steward,
                scoreThreshold = 91f,
                unlockDescription = "Your name is inscribed on the Ark wall. The community remembers.",
                cinematicKey = "cinematic_ark_inscription",
                notifyNurAIN = true
            });
        }

        // ── Persistence (PlayerPrefs for MVP, SQLite later) ─────────────

        private const string PrefsKey = "UnlockedMilestones";

        private void SaveUnlockedToPrefs()
        {
            string joined = string.Join(",", _unlocked);
            PlayerPrefs.SetString(PrefsKey, joined);
            PlayerPrefs.Save();
        }

        private void LoadUnlockedFromPrefs()
        {
            string saved = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return;

            foreach (string id in saved.Split(','))
            {
                if (!string.IsNullOrEmpty(id))
                    _unlocked.Add(id);
            }
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
    }
}
