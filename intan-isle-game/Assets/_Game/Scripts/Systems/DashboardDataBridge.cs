using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>
    /// Session data snapshot exported to the teacher/parent dashboard.
    /// Aggregates all learning, care, and progression data from one play session.
    /// </summary>
    [Serializable]
    public class SessionReport
    {
        public string playerId;
        public string sessionId;
        public long sessionStartMs;
        public long sessionEndMs;
        public float sessionDurationMin;

        // Blessing
        public float blessingScoreStart;
        public float blessingScoreEnd;
        public string blessingLevel;

        // Rabbit care
        public int rabbitsFed;
        public int rabbitsWatered;
        public int healthChecksPerformed;
        public int neglectWarnings;

        // Crops
        public int cropsPlanted;
        public int cropsHarvested;
        public int cropsComposted;
        public int wasteDetections;

        // Curriculum
        public int pillarsExposed;
        public int pillarsEngaged;
        public int pillarsApplied;
        public string weakestDomain;
        public string dominantDomain;

        // Reflection
        public int reflectionsSubmitted;
        public int totalWordCount;

        // Healing
        public int healingActionsCompleted;

        // Milestones
        public List<string> milestonesUnlocked = new List<string>();
    }

    /// <summary>Published when a session report is ready for export.</summary>
    public struct SessionReportReadyEvent
    {
        public SessionReport Report;
    }

    /// <summary>
    /// Singleton that aggregates session data from all game systems and
    /// exports a <see cref="SessionReport"/> for the teacher/parent dashboard.
    /// Listens to gameplay events via EventBus and tallies session totals.
    /// On session end, publishes <see cref="SessionReportReadyEvent"/> and
    /// forwards to <see cref="NurAINConnector"/>.
    /// </summary>
    public class DashboardDataBridge : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static DashboardDataBridge Instance { get; private set; }

        /// <summary>Current in-progress session report.</summary>
        public SessionReport CurrentReport { get; private set; }

        private long _sessionStartMs;

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BeginSession();
        }

        private void OnEnable()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Subscribe<RabbitCareEvent>(OnRabbitCare);
            bus.Subscribe<RabbitNeglectedEvent>(OnRabbitNeglected);
            bus.Subscribe<CropHarvestedEvent>(OnCropHarvested);
            bus.Subscribe<WasteDetectedEvent>(OnWasteDetected);
            bus.Subscribe<ReflectionAnalysedEvent>(OnReflection);
            bus.Subscribe<MilestoneUnlockedEvent>(OnMilestone);
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Unsubscribe<RabbitCareEvent>(OnRabbitCare);
            bus.Unsubscribe<RabbitNeglectedEvent>(OnRabbitNeglected);
            bus.Unsubscribe<CropHarvestedEvent>(OnCropHarvested);
            bus.Unsubscribe<WasteDetectedEvent>(OnWasteDetected);
            bus.Unsubscribe<ReflectionAnalysedEvent>(OnReflection);
            bus.Unsubscribe<MilestoneUnlockedEvent>(OnMilestone);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) EndSession();
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>Begins tracking a new session.</summary>
        public void BeginSession()
        {
            _sessionStartMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            PlayerDataManager player = PlayerDataManager.Instance;
            BlessingMeterController blessing = BlessingMeterController.Instance;

            CurrentReport = new SessionReport
            {
                playerId = player != null ? player.PlayerId : "unknown",
                sessionId = Guid.NewGuid().ToString(),
                sessionStartMs = _sessionStartMs,
                blessingScoreStart = blessing != null ? blessing.CachedBlessingScore : 0f,
            };
        }

        /// <summary>
        /// Finalises the session report, populates curriculum coverage,
        /// publishes <see cref="SessionReportReadyEvent"/>, and forwards
        /// the JSON report to NurAIN.
        /// </summary>
        public void EndSession()
        {
            if (CurrentReport == null) return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            CurrentReport.sessionEndMs = now;
            CurrentReport.sessionDurationMin = (now - _sessionStartMs) / 60000f;

            // Final blessing score
            BlessingMeterController blessing = BlessingMeterController.Instance;
            if (blessing != null)
            {
                CurrentReport.blessingScoreEnd = blessing.CachedBlessingScore;
                CurrentReport.blessingLevel = blessing.CachedBlessingLevel.ToString();
            }

            // Curriculum coverage snapshot
            CurriculumEngine engine = CurriculumEngine.Instance;
            if (engine != null)
            {
                PillarCoverageVector coverage = engine.GetCurrentPillarCoverage();
                CurrentReport.pillarsExposed = coverage.exposedCount;
                CurrentReport.pillarsEngaged = coverage.engagedCount;
                CurrentReport.pillarsApplied = coverage.appliedCount;
                CurrentReport.weakestDomain = coverage.weakestDomain;
                CurrentReport.dominantDomain = coverage.dominantDomain;
            }

            // Publish for dashboard consumers
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(new SessionReportReadyEvent { Report = CurrentReport });

            // Forward to NurAIN
            NurAINConnector connector = NurAINConnector.Instance;
            if (connector != null)
            {
                string json = JsonUtility.ToJson(CurrentReport);
                connector.SendCurriculumActivity("SESSION_REPORT", 0f, json);
            }

            Debug.Log($"[DashboardDataBridge] Session report ready — " +
                      $"{CurrentReport.sessionDurationMin:F1}min, " +
                      $"blessing {CurrentReport.blessingScoreStart:F0}→{CurrentReport.blessingScoreEnd:F0}, " +
                      $"pillars {CurrentReport.pillarsExposed}/{CurrentReport.pillarsEngaged}/{CurrentReport.pillarsApplied}");
        }

        // ── Event handlers ──────────────────────────────────────────────

        private void OnRabbitCare(RabbitCareEvent evt)
        {
            if (CurrentReport == null) return;

            switch (evt.EventType)
            {
                case RabbitTelemetryEvents.RabbitFed:
                    CurrentReport.rabbitsFed++;
                    break;
                case RabbitTelemetryEvents.RabbitWatered:
                    CurrentReport.rabbitsWatered++;
                    break;
                case RabbitTelemetryEvents.RabbitHealthCheck:
                    CurrentReport.healthChecksPerformed++;
                    break;
            }
        }

        private void OnRabbitNeglected(RabbitNeglectedEvent evt)
        {
            if (CurrentReport != null)
                CurrentReport.neglectWarnings++;
        }

        private void OnCropHarvested(CropHarvestedEvent evt)
        {
            if (CurrentReport == null) return;

            if (evt.Mode == HarvestMode.Compost)
                CurrentReport.cropsComposted++;
            else
                CurrentReport.cropsHarvested++;
        }

        private void OnWasteDetected(WasteDetectedEvent evt)
        {
            if (CurrentReport != null)
                CurrentReport.wasteDetections++;
        }

        private void OnReflection(ReflectionAnalysedEvent evt)
        {
            if (CurrentReport == null) return;

            CurrentReport.reflectionsSubmitted++;
            CurrentReport.totalWordCount += evt.WordCount;
        }

        private void OnMilestone(MilestoneUnlockedEvent evt)
        {
            if (CurrentReport != null)
                CurrentReport.milestonesUnlocked.Add(evt.MilestoneId);
        }
    }
}
