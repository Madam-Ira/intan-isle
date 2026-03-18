using System;
using System.IO;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Event structs ───────────────────────────────────────────────────────

    /// <summary>Published when a session ends and behaviour has been evaluated.</summary>
    public struct SessionEndedEvent
    {
        public SessionBehaviourVector Session;
        public LearnerProfileVector UpdatedProfile;
    }

    /// <summary>Published when the persisted learner profile changes.</summary>
    public struct LearnerProfileChangedEvent
    {
        public LearnerProfileVector Profile;
    }

    // ── Save data ───────────────────────────────────────────────────────────

    [Serializable]
    internal class LearnerProfileSaveData
    {
        public LearnerProfileVector profile;
        public PlayerPersonalityVector personality;
        public bool onboardingComplete;
        public long lastSessionEndMs;
    }

    // ── Tracker ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that accumulates behavioural signals during gameplay sessions,
    /// builds a <see cref="SessionBehaviourVector"/>, evaluates it through
    /// <see cref="NurAINRulesEngine"/>, and persists the resulting
    /// <see cref="LearnerProfileVector"/> via <see cref="PlayerDataManager"/>.
    /// </summary>
    public class LearnerProfileTracker : MonoBehaviour
    {
        private const string FilePrefix = "learner_profile_";
        private const string FileExtension = ".json";

        /// <summary>Singleton instance.</summary>
        public static LearnerProfileTracker Instance { get; private set; }

        /// <summary>Current learner profile.</summary>
        public LearnerProfileVector CurrentProfile => _save.profile;

        /// <summary>Player personality from onboarding quiz.</summary>
        public PlayerPersonalityVector Personality => _save.personality;

        /// <summary>Whether the onboarding quiz has been completed.</summary>
        public bool OnboardingComplete => _save.onboardingComplete;

        /// <summary>Current in-progress session behaviour vector.</summary>
        public SessionBehaviourVector CurrentSession => _session;

        private LearnerProfileSaveData _save;
        private SessionBehaviourVector _session;
        private float _sessionStartTime;
        private float _activeTimeAccumulator;
        private float _veiledTimeAccumulator;
        private Vector3 _sessionStartPosition;
        private bool _sessionActive;

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            LoadData();
            BeginSession();
            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) EndSession();
            else BeginSession();
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }

        private void Update()
        {
            if (!_sessionActive) return;

            if (Input.anyKey)
                _activeTimeAccumulator += Time.deltaTime;

            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null && veil.IsVeiledWorldActive)
                _veiledTimeAccumulator += Time.deltaTime;

            float dist = Vector3.Distance(transform.position, _sessionStartPosition) / 1000f;
            if (dist > _session.explorationRadiusKm)
                _session.explorationRadiusKm = dist;
        }

        // ── Session lifecycle ───────────────────────────────────────────

        /// <summary>Begins tracking a new session.</summary>
        public void BeginSession()
        {
            if (_sessionActive) return;
            _sessionActive = true;
            _session = new SessionBehaviourVector();
            _sessionStartTime = Time.realtimeSinceStartup;
            _activeTimeAccumulator = 0f;
            _veiledTimeAccumulator = 0f;
            _sessionStartPosition = transform.position;
        }

        /// <summary>Ends the current session, evaluates behaviour, and persists.</summary>
        public void EndSession()
        {
            if (!_sessionActive) return;
            _sessionActive = false;

            float elapsed = Time.realtimeSinceStartup - _sessionStartTime;
            _session.sessionDurationMin = elapsed / 60f;
            _session.activePlayRatio = elapsed > 0f
                ? Mathf.Clamp01(_activeTimeAccumulator / elapsed) : 0f;
            _session.veiledWorldTimeRatio = elapsed > 0f
                ? Mathf.Clamp01(_veiledTimeAccumulator / elapsed) : 0f;

            NurAINRulesEngine engine = NurAINRulesEngine.Instance;
            if (engine != null)
                _save.profile = engine.Evaluate(_session, _save.profile);

            _save.lastSessionEndMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SaveData();

            PublishViaBus(new SessionEndedEvent
            {
                Session = _session,
                UpdatedProfile = _save.profile
            });

            PublishViaBus(new LearnerProfileChangedEvent
            {
                Profile = _save.profile
            });
        }

        /// <summary>Seeds the personality vector from the onboarding quiz.</summary>
        public void SetPersonality(PlayerPersonalityVector personality)
        {
            _save.personality = personality;
            _save.onboardingComplete = true;

            _save.profile.curiosityIndex = personality.explorerDrive;
            _save.profile.empathyIndex = personality.nurturerDrive;
            _save.profile.persistenceIndex =
                Mathf.Lerp(personality.achieverDrive, personality.riskTolerance, 0.5f);
            _save.profile.spiritualResonance = personality.spiritualOpenness;
            _save.profile.culturalSensitivity = personality.reflectiveDepth;

            SaveData();

            PublishViaBus(new LearnerProfileChangedEvent
            {
                Profile = _save.profile
            });
        }

        // ── Signal accumulation ─────────────────────────────────────────

        /// <summary>Records a flight activation during the current session.</summary>
        public void RecordFlightActivation()
        {
            _session.flightActivations++;
        }

        /// <summary>Records a help request during the current session.</summary>
        public void RecordHelpRequest()
        {
            _session.helpRequests++;
        }

        /// <summary>Records a task retry during the current session.</summary>
        public void RecordRetry()
        {
            _session.retryCount++;
        }

        // ── EventBus handlers ───────────────────────────────────────────

        private void SubscribeEvents()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Subscribe<CropHarvestedEvent>(OnCropHarvested);
            bus.Subscribe<HarvestProductProcessedEvent>(OnHarvestProduct);
            bus.Subscribe<PayItForwardSentEvent>(OnPayItForward);
            bus.Subscribe<ZoneHealedEvent>(OnZoneHealed);
            bus.Subscribe<CurriculumPillarDeepenedEvent>(OnPillarDeepened);
            bus.Subscribe<RabbitHealthStateChangedEvent>(OnRabbitHealth);
        }

        private void UnsubscribeEvents()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Unsubscribe<CropHarvestedEvent>(OnCropHarvested);
            bus.Unsubscribe<HarvestProductProcessedEvent>(OnHarvestProduct);
            bus.Unsubscribe<PayItForwardSentEvent>(OnPayItForward);
            bus.Unsubscribe<ZoneHealedEvent>(OnZoneHealed);
            bus.Unsubscribe<CurriculumPillarDeepenedEvent>(OnPillarDeepened);
            bus.Unsubscribe<RabbitHealthStateChangedEvent>(OnRabbitHealth);
        }

        private void OnCropHarvested(CropHarvestedEvent evt)
        {
            _session.cropsHarvested++;
        }

        private void OnHarvestProduct(HarvestProductProcessedEvent evt)
        {
            _session.harvestProductsProcessed++;
        }

        private void OnPayItForward(PayItForwardSentEvent evt)
        {
            _session.payItForwardGifts++;
        }

        private void OnZoneHealed(ZoneHealedEvent evt)
        {
            _session.zoneHealingActions++;
        }

        private void OnPillarDeepened(CurriculumPillarDeepenedEvent evt)
        {
            _session.curriculumPillarsDeepened++;
        }

        private void OnRabbitHealth(RabbitHealthStateChangedEvent evt)
        {
            _session.rabbitCareActions++;
        }

        // ── Persistence ─────────────────────────────────────────────────

        private void SaveData()
        {
            string json = JsonUtility.ToJson(_save, true);

            try
            {
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LearnerProfileTracker] Save failed: {e.Message}");
            }
        }

        private void LoadData()
        {
            string path = GetSavePath();

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _save = JsonUtility.FromJson<LearnerProfileSaveData>(json);
                    if (_save != null) return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LearnerProfileTracker] Load failed: {e.Message}");
                }
            }

            _save = new LearnerProfileSaveData
            {
                profile = new LearnerProfileVector
                {
                    frustrationTolerance = 0.5f,
                    challengePref = ChallengePreference.Moderate,
                    engagementLevel = EngagementLevel.Active
                }
            };
        }

        private string GetSavePath()
        {
            PlayerDataManager player = PlayerDataManager.Instance;
            string playerId = player != null ? player.PlayerId : "unknown";
            return Path.Combine(Application.persistentDataPath,
                FilePrefix + playerId + FileExtension);
        }

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
        }
    }
}
