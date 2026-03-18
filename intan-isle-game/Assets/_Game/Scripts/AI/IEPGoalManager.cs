using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Enums ───────────────────────────────────────────────────────────────

    /// <summary>Status of an IEP goal.</summary>
    public enum IEPGoalStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Achieved   = 2,
        Exceeded   = 3
    }

    /// <summary>Metric type used to track goal progress.</summary>
    public enum IEPGoalMetric
    {
        CuriosityIndex       = 0,
        PersistenceIndex     = 1,
        EmpathyIndex         = 2,
        SpatialAwareness     = 3,
        EcologicalLiteracy   = 4,
        CulturalSensitivity  = 5,
        SpiritualResonance   = 6,
        SessionCount         = 7,
        TotalPlayTimeMin     = 8,
        CropsHarvested       = 9,
        ZonesHealed          = 10,
        PayItForwardGifts    = 11,
        PillarsDeepened      = 12
    }

    // ── Data types ──────────────────────────────────────────────────────────

    /// <summary>A milestone checkpoint within an IEP goal.</summary>
    [Serializable]
    public struct IEPMilestone
    {
        public string label;
        public float  threshold;
        public bool   reached;
    }

    /// <summary>An Individualized Education Plan goal with measurable targets.</summary>
    [Serializable]
    public class IEPGoal
    {
        public string       goalId;
        public string       description;
        public IEPGoalMetric metric;
        public float        targetValue;
        public float        currentValue;
        public IEPGoalStatus status;
        public long         createdAtMs;
        public long         achievedAtMs;
        public List<IEPMilestone> milestones = new List<IEPMilestone>();
    }

    // ── Event structs ───────────────────────────────────────────────────────

    /// <summary>Published when an IEP goal is created.</summary>
    public struct IEPGoalCreatedEvent
    {
        public string GoalId;
        public string Description;
        public IEPGoalMetric Metric;
        public float TargetValue;
    }

    /// <summary>Published when progress is recorded against an IEP goal.</summary>
    public struct IEPGoalProgressEvent
    {
        public string GoalId;
        public float CurrentValue;
        public float TargetValue;
        public IEPGoalStatus Status;
    }

    /// <summary>Published when an IEP goal reaches Achieved status.</summary>
    public struct IEPGoalCompletedEvent
    {
        public string GoalId;
        public float FinalValue;
    }

    // ── ETDashboardDataBridge ───────────────────────────────────────────────

    /// <summary>Payload published to the ET dashboard for external consumption.</summary>
    [Serializable]
    public struct ETDashboardPayload
    {
        public string playerId;
        public string snapshotTimestamp;
        public string goalsJson;
        public string profileJson;
    }

    /// <summary>Published when new data is pushed to the ET dashboard bridge.</summary>
    public struct ETDashboardUpdatedEvent
    {
        public ETDashboardPayload Payload;
    }

    /// <summary>
    /// Static bridge that formats IEP and learner profile data for
    /// external Educational Technology dashboards. Publishes structured
    /// <see cref="ETDashboardUpdatedEvent"/> payloads via <see cref="EventBus"/>
    /// and forwards them to <see cref="NurAINConnector"/>.
    /// </summary>
    public static class ETDashboardDataBridge
    {
        /// <summary>
        /// Pushes a snapshot of IEP goals and learner profile to the
        /// dashboard and NurAIN connector.
        /// </summary>
        public static void PushSnapshot(
            List<IEPGoal> goals,
            LearnerProfileVector profile)
        {
            PlayerDataManager player = PlayerDataManager.Instance;
            string playerId = player != null ? player.PlayerId : "unknown";

            string goalsJson = JsonUtility.ToJson(new IEPGoalCollection { goals = goals }, false);
            string profileJson = JsonUtility.ToJson(profile, false);

            ETDashboardPayload payload = new ETDashboardPayload
            {
                playerId = playerId,
                snapshotTimestamp = DateTimeOffset.UtcNow.ToString("o"),
                goalsJson = goalsJson,
                profileJson = profileJson
            };

            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(new ETDashboardUpdatedEvent { Payload = payload });

            NurAINConnector connector = NurAINConnector.Instance;
            if (connector != null)
                connector.SendCurriculumActivity("IEP_SNAPSHOT", 0f, goalsJson);
        }
    }

    // ── Save data ───────────────────────────────────────────────────────────

    [Serializable]
    internal class IEPGoalCollection
    {
        public List<IEPGoal> goals = new List<IEPGoal>();
    }

    // ── Manager ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that creates, tracks, and reports Individualized Education Plan
    /// goals. Evaluates learner profile changes against goal metrics and pushes
    /// updates to <see cref="ETDashboardDataBridge"/>.
    /// </summary>
    public class IEPGoalManager : MonoBehaviour
    {
        private const string FilePrefix = "iep_goals_";
        private const string FileExtension = ".json";
        private const float DashboardPushIntervalSec = 120f;

        /// <summary>Singleton instance.</summary>
        public static IEPGoalManager Instance { get; private set; }

        /// <summary>Read-only access to all current IEP goals.</summary>
        public IReadOnlyList<IEPGoal> Goals => _goals;

        [Header("Tuning")]
        [SerializeField] private float dashboardPushInterval = DashboardPushIntervalSec;

        private readonly List<IEPGoal> _goals = new List<IEPGoal>();
        private float _lastPushTime;
        private int _cumulativeCrops;
        private int _cumulativeZonesHealed;
        private int _cumulativeGifts;
        private int _cumulativePillars;

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

            if (_goals.Count == 0)
                CreateDefaultGoals();

            SubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveData();
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup - _lastPushTime > dashboardPushInterval)
            {
                _lastPushTime = Time.realtimeSinceStartup;
                PushDashboard();
            }
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>Creates a new IEP goal with milestones at 25%, 50%, 75%.</summary>
        public IEPGoal CreateGoal(
            string goalId,
            string description,
            IEPGoalMetric metric,
            float targetValue)
        {
            IEPGoal goal = new IEPGoal
            {
                goalId = goalId,
                description = description,
                metric = metric,
                targetValue = targetValue,
                currentValue = 0f,
                status = IEPGoalStatus.NotStarted,
                createdAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                milestones = new List<IEPMilestone>
                {
                    new IEPMilestone { label = "25%", threshold = targetValue * 0.25f },
                    new IEPMilestone { label = "50%", threshold = targetValue * 0.50f },
                    new IEPMilestone { label = "75%", threshold = targetValue * 0.75f }
                }
            };

            _goals.Add(goal);
            SaveData();

            PublishViaBus(new IEPGoalCreatedEvent
            {
                GoalId = goalId,
                Description = description,
                Metric = metric,
                TargetValue = targetValue
            });

            return goal;
        }

        /// <summary>Returns the goal with the given ID, or null.</summary>
        public IEPGoal GetGoal(string goalId)
        {
            for (int i = 0; i < _goals.Count; i++)
            {
                if (_goals[i].goalId == goalId)
                    return _goals[i];
            }

            return null;
        }

        /// <summary>Forces an immediate dashboard push.</summary>
        public void PushDashboard()
        {
            LearnerProfileTracker tracker = LearnerProfileTracker.Instance;
            LearnerProfileVector profile = tracker != null
                ? tracker.CurrentProfile
                : new LearnerProfileVector();

            ETDashboardDataBridge.PushSnapshot(_goals, profile);
        }

        // ── Event handlers ──────────────────────────────────────────────

        private void SubscribeEvents()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Subscribe<LearnerProfileChangedEvent>(OnProfileChanged);
            bus.Subscribe<CropHarvestedEvent>(OnCropHarvested);
            bus.Subscribe<ZoneHealedEvent>(OnZoneHealed);
            bus.Subscribe<PayItForwardSentEvent>(OnPayItForward);
            bus.Subscribe<CurriculumPillarDeepenedEvent>(OnPillarDeepened);
        }

        private void UnsubscribeEvents()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Unsubscribe<LearnerProfileChangedEvent>(OnProfileChanged);
            bus.Unsubscribe<CropHarvestedEvent>(OnCropHarvested);
            bus.Unsubscribe<ZoneHealedEvent>(OnZoneHealed);
            bus.Unsubscribe<PayItForwardSentEvent>(OnPayItForward);
            bus.Unsubscribe<CurriculumPillarDeepenedEvent>(OnPillarDeepened);
        }

        private void OnProfileChanged(LearnerProfileChangedEvent evt)
        {
            LearnerProfileVector p = evt.Profile;

            EvaluateGoals(IEPGoalMetric.CuriosityIndex, p.curiosityIndex);
            EvaluateGoals(IEPGoalMetric.PersistenceIndex, p.persistenceIndex);
            EvaluateGoals(IEPGoalMetric.EmpathyIndex, p.empathyIndex);
            EvaluateGoals(IEPGoalMetric.SpatialAwareness, p.spatialAwareness);
            EvaluateGoals(IEPGoalMetric.EcologicalLiteracy, p.ecologicalLiteracy);
            EvaluateGoals(IEPGoalMetric.CulturalSensitivity, p.culturalSensitivity);
            EvaluateGoals(IEPGoalMetric.SpiritualResonance, p.spiritualResonance);
            EvaluateGoals(IEPGoalMetric.SessionCount, p.sessionCount);
            EvaluateGoals(IEPGoalMetric.TotalPlayTimeMin, p.totalPlayTimeMin);
        }

        private void OnCropHarvested(CropHarvestedEvent evt)
        {
            _cumulativeCrops++;
            EvaluateGoals(IEPGoalMetric.CropsHarvested, _cumulativeCrops);
        }

        private void OnZoneHealed(ZoneHealedEvent evt)
        {
            _cumulativeZonesHealed++;
            EvaluateGoals(IEPGoalMetric.ZonesHealed, _cumulativeZonesHealed);
        }

        private void OnPayItForward(PayItForwardSentEvent evt)
        {
            _cumulativeGifts++;
            EvaluateGoals(IEPGoalMetric.PayItForwardGifts, _cumulativeGifts);
        }

        private void OnPillarDeepened(CurriculumPillarDeepenedEvent evt)
        {
            _cumulativePillars++;
            EvaluateGoals(IEPGoalMetric.PillarsDeepened, _cumulativePillars);
        }

        // ── Goal evaluation ─────────────────────────────────────────────

        private void EvaluateGoals(IEPGoalMetric metric, float value)
        {
            bool changed = false;

            for (int i = 0; i < _goals.Count; i++)
            {
                IEPGoal goal = _goals[i];
                if (goal.metric != metric) continue;
                if (goal.status == IEPGoalStatus.Exceeded) continue;

                goal.currentValue = value;

                // Update milestones
                for (int m = 0; m < goal.milestones.Count; m++)
                {
                    IEPMilestone ms = goal.milestones[m];
                    if (!ms.reached && value >= ms.threshold)
                    {
                        ms.reached = true;
                        goal.milestones[m] = ms;
                    }
                }

                // Update status
                IEPGoalStatus prev = goal.status;

                if (value >= goal.targetValue * 1.2f)
                    goal.status = IEPGoalStatus.Exceeded;
                else if (value >= goal.targetValue)
                    goal.status = IEPGoalStatus.Achieved;
                else if (value > 0f)
                    goal.status = IEPGoalStatus.InProgress;

                if (goal.status != prev)
                {
                    changed = true;

                    PublishViaBus(new IEPGoalProgressEvent
                    {
                        GoalId = goal.goalId,
                        CurrentValue = goal.currentValue,
                        TargetValue = goal.targetValue,
                        Status = goal.status
                    });

                    if (goal.status == IEPGoalStatus.Achieved && prev != IEPGoalStatus.Achieved)
                    {
                        goal.achievedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                        PublishViaBus(new IEPGoalCompletedEvent
                        {
                            GoalId = goal.goalId,
                            FinalValue = goal.currentValue
                        });

                        PublishViaBus(new BlessingDeltaRequest
                        {
                            Delta = 5f,
                            Reason = $"IEP goal achieved: {goal.goalId}"
                        });
                    }
                }
            }

            if (changed)
                SaveData();
        }

        // ── Default goals ───────────────────────────────────────────────

        private void CreateDefaultGoals()
        {
            CreateGoal("IEP_CURIOSITY_01",
                "Demonstrate curiosity by exploring diverse zones",
                IEPGoalMetric.CuriosityIndex, 0.5f);

            CreateGoal("IEP_PERSIST_01",
                "Show persistence by completing tasks across multiple sessions",
                IEPGoalMetric.PersistenceIndex, 0.4f);

            CreateGoal("IEP_EMPATHY_01",
                "Express empathy through Pay It Forward gifts and animal care",
                IEPGoalMetric.EmpathyIndex, 0.5f);

            CreateGoal("IEP_ECOLOGY_01",
                "Build ecological literacy through farming and zone healing",
                IEPGoalMetric.EcologicalLiteracy, 0.4f);

            CreateGoal("IEP_CULTURE_01",
                "Develop cultural sensitivity through Veiled World engagement",
                IEPGoalMetric.CulturalSensitivity, 0.3f);

            CreateGoal("IEP_SPATIAL_01",
                "Strengthen spatial awareness via flight and navigation",
                IEPGoalMetric.SpatialAwareness, 0.4f);

            CreateGoal("IEP_HARVEST_01",
                "Harvest 20 crops to sustain the community",
                IEPGoalMetric.CropsHarvested, 20f);

            CreateGoal("IEP_PILLAR_01",
                "Deepen 5 curriculum pillars through active learning",
                IEPGoalMetric.PillarsDeepened, 5f);
        }

        // ── Persistence ─────────────────────────────────────────────────

        private void SaveData()
        {
            IEPGoalCollection save = new IEPGoalCollection();
            save.goals.AddRange(_goals);

            string json = JsonUtility.ToJson(save, true);

            try
            {
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[IEPGoalManager] Save failed: {e.Message}");
            }
        }

        private void LoadData()
        {
            _goals.Clear();

            string path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                IEPGoalCollection save = JsonUtility.FromJson<IEPGoalCollection>(json);
                if (save != null && save.goals != null)
                    _goals.AddRange(save.goals);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[IEPGoalManager] Load failed: {e.Message}");
            }
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
