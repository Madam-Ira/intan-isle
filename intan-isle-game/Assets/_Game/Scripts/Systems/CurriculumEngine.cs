using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Event structs ───────────────────────────────────────────────────

    /// <summary>Published when a curriculum pillar's depth score increases.</summary>
    public struct CurriculumPillarDeepenedEvent
    {
        /// <summary>Pillar identifier (e.g. A01_Oral_Communication).</summary>
        public string PillarId;

        /// <summary>Previous depth score (1–3).</summary>
        public int PreviousDepth;

        /// <summary>New depth score (1–3).</summary>
        public int NewDepth;

        /// <summary>Domain letter (A–J).</summary>
        public string Domain;
    }

    // ── Inner data types ────────────────────────────────────────────────

    /// <summary>Tracks exposure and depth for a single curriculum pillar.</summary>
    [Serializable]
    public class PillarExposureRecord
    {
        /// <summary>Pillar identifier (e.g. A01_Oral_Communication).</summary>
        public string pillarId;

        /// <summary>Domain letter derived from the first character of pillarId.</summary>
        public string domain;

        /// <summary>Number of times the player has been exposed to this pillar.</summary>
        public int exposureCount;

        /// <summary>Depth score: 1 = exposed, 2 = engaged, 3 = applied.</summary>
        public int depthScore;

        /// <summary>Unix timestamp in milliseconds of last exposure.</summary>
        public long lastExposedAtMs;

        /// <summary>Identifiers of actions that serve as evidence for this pillar.</summary>
        public List<string> evidenceActionIds = new List<string>();
    }

    // ── Serialisation ───────────────────────────────────────────────────

    [Serializable]
    internal class CurriculumSaveData
    {
        public List<PillarExposureRecord> pillars = new List<PillarExposureRecord>();
    }

    // ── Pillar coverage ─────────────────────────────────────────────────

    /// <summary>
    /// Summary of curriculum pillar coverage across all domains.
    /// Returned by <see cref="CurriculumEngine.GetCurrentPillarCoverage"/>.
    /// </summary>
    [Serializable]
    public struct PillarCoverageVector
    {
        /// <summary>Total pillars with at least one exposure.</summary>
        public int exposedCount;

        /// <summary>Total pillars at Engaged depth (≥2).</summary>
        public int engagedCount;

        /// <summary>Total pillars at Applied depth (3).</summary>
        public int appliedCount;

        /// <summary>Ratio of exposed pillars to total (50).</summary>
        public float coverageRatio;

        /// <summary>Average depth score across all exposed pillars.</summary>
        public float averageDepth;

        /// <summary>Domain letter with the lowest average depth.</summary>
        public string weakestDomain;

        /// <summary>Domain letter with the highest average depth.</summary>
        public string dominantDomain;
    }

    // ── Telemetry constants ─────────────────────────────────────────────

    /// <summary>Telemetry event type constants for the curriculum engine.</summary>
    public static class CurriculumTelemetryEvents
    {
        public const string PillarDeepened = "CURRICULUM_PILLAR_DEEPENED";
        public const string ActivityRecorded = "CURRICULUM_ACTIVITY_RECORDED";
    }

    // ── Engine ──────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that tracks player learning across 50 curriculum pillars
    /// in 10 domains (A–J, 5 pillars each). Records exposure, calculates
    /// depth scores, identifies weak/dominant domains, and awards blessing
    /// on depth milestones.
    /// </summary>
    public class CurriculumEngine : MonoBehaviour
    {
        private const string FilePrefix = "curriculum_";
        private const string FileExtension = ".json";

        private const int DepthExposed = 1;
        private const int DepthEngaged = 2;
        private const int DepthApplied = 3;

        private const int BaseThresholdEngaged = 3;
        private const int BaseThresholdApplied = 8;

        private const float BlessingEngaged = 0.5f;
        private const float BlessingApplied = 1.5f;

        private const int PillarsPerDomain = 5;

        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static CurriculumEngine Instance { get; private set; }

        private readonly Dictionary<string, PillarExposureRecord> _pillars =
            new Dictionary<string, PillarExposureRecord>();

        private int _adaptiveThresholdEngaged = BaseThresholdEngaged;
        private int _adaptiveThresholdApplied = BaseThresholdApplied;

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

        private void Start()
        {
            LoadData();
        }

        private void OnEnable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Subscribe<ChallengeProfileChangedEvent>(OnChallengeProfileChanged);
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Unsubscribe<ChallengeProfileChangedEvent>(OnChallengeProfileChanged);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveData();
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Records a learning activity against a curriculum pillar.
        /// Increments exposure count, recalculates depth, and awards
        /// blessing when depth milestones are reached.
        /// </summary>
        /// <param name="pillarId">
        /// Pillar identifier (e.g. <c>A01_Oral_Communication</c>).
        /// </param>
        /// <param name="competencyDelta">
        /// Positive value indicating activity weight (typically 1.0).
        /// </param>
        public void RecordActivity(string pillarId, float competencyDelta)
        {
            if (string.IsNullOrEmpty(pillarId) || competencyDelta <= 0f) return;

            PillarExposureRecord record = GetOrCreateRecord(pillarId);
            int previousDepth = record.depthScore;

            record.exposureCount++;
            record.lastExposedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            int newDepth = CalculateDepth(record.exposureCount);

            if (newDepth > record.depthScore)
            {
                record.depthScore = newDepth;

                PublishViaBus(new CurriculumPillarDeepenedEvent
                {
                    PillarId = pillarId,
                    PreviousDepth = previousDepth,
                    NewDepth = newDepth,
                    Domain = record.domain
                });

                PublishTelemetry(CurriculumTelemetryEvents.PillarDeepened,
                    $"{{\"pillarId\":\"{pillarId}\",\"from\":{previousDepth},\"to\":{newDepth}}}");

                // Blessing milestones
                if (newDepth == DepthEngaged && previousDepth < DepthEngaged)
                {
                    PublishViaBus(new BlessingDeltaRequest
                    {
                        Delta = BlessingEngaged,
                        Reason = $"Pillar {pillarId} reached Engaged depth"
                    });
                }

                if (newDepth == DepthApplied && previousDepth < DepthApplied)
                {
                    PublishViaBus(new BlessingDeltaRequest
                    {
                        Delta = BlessingApplied,
                        Reason = $"Pillar {pillarId} reached Applied depth"
                    });
                }
            }

            SaveData();
        }

        /// <summary>
        /// Returns the depth score for a pillar.
        /// 1 = exposed, 2 = engaged, 3 = applied.
        /// </summary>
        /// <param name="pillarId">Pillar identifier.</param>
        /// <returns>Depth score (1–3), or 0 if never exposed.</returns>
        public int GetDepthScore(string pillarId)
        {
            if (string.IsNullOrEmpty(pillarId)) return 0;
            return _pillars.TryGetValue(pillarId, out PillarExposureRecord record)
                ? record.depthScore
                : 0;
        }

        /// <summary>
        /// Returns the domain letter (A–J) with the lowest average depth score.
        /// </summary>
        /// <returns>Single-character domain identifier, or <c>A</c> if no data.</returns>
        public string GetWeakestDomain()
        {
            return FindDomainByScore(weakest: true);
        }

        /// <summary>
        /// Returns the domain letter (A–J) with the highest average depth score.
        /// </summary>
        /// <returns>Single-character domain identifier, or <c>A</c> if no data.</returns>
        public string GetDominantDomain()
        {
            return FindDomainByScore(weakest: false);
        }

        /// <summary>
        /// Returns a recommended scenario identifier for a pillar.
        /// Stub implementation — returns <c>scenario_{pillarId}_recommend</c>.
        /// </summary>
        /// <param name="pillarId">Pillar identifier.</param>
        /// <returns>Scenario recommendation string.</returns>
        public string GetRecommendedScenario(string pillarId)
        {
            return $"scenario_{pillarId}_recommend";
        }

        // ── Internals ───────────────────────────────────────────────────

        private PillarExposureRecord GetOrCreateRecord(string pillarId)
        {
            if (_pillars.TryGetValue(pillarId, out PillarExposureRecord existing))
                return existing;

            PillarExposureRecord record = new PillarExposureRecord
            {
                pillarId = pillarId,
                domain = pillarId.Length > 0 ? pillarId.Substring(0, 1) : "A",
                exposureCount = 0,
                depthScore = 0,
                lastExposedAtMs = 0,
                evidenceActionIds = new List<string>()
            };

            _pillars[pillarId] = record;
            return record;
        }

        private int CalculateDepth(int exposureCount)
        {
            if (exposureCount >= _adaptiveThresholdApplied) return DepthApplied;
            if (exposureCount >= _adaptiveThresholdEngaged) return DepthEngaged;
            if (exposureCount >= 1) return DepthExposed;
            return 0;
        }

        private string FindDomainByScore(bool weakest)
        {
            Dictionary<string, float> domainTotals = new Dictionary<string, float>();
            Dictionary<string, int> domainCounts = new Dictionary<string, int>();

            foreach (PillarExposureRecord record in _pillars.Values)
            {
                string d = record.domain;

                if (!domainTotals.ContainsKey(d))
                {
                    domainTotals[d] = 0f;
                    domainCounts[d] = 0;
                }

                domainTotals[d] += record.depthScore;
                domainCounts[d]++;
            }

            if (domainTotals.Count == 0) return "A";

            string best = null;
            float bestAvg = weakest ? float.MaxValue : float.MinValue;

            foreach (var kvp in domainTotals)
            {
                float avg = kvp.Value / Mathf.Max(domainCounts[kvp.Key], 1);

                bool isBetter = weakest ? avg < bestAvg : avg > bestAvg;
                if (isBetter)
                {
                    bestAvg = avg;
                    best = kvp.Key;
                }
            }

            return best ?? "A";
        }

        // ── NGE integration ──────────────────────────────────────────────

        /// <summary>
        /// Returns a summary of pillar coverage across all domains.
        /// </summary>
        public PillarCoverageVector GetCurrentPillarCoverage()
        {
            const int TotalPillars = 50;

            int exposed = 0;
            int engaged = 0;
            int applied = 0;
            float depthSum = 0f;

            foreach (PillarExposureRecord r in _pillars.Values)
            {
                if (r.depthScore >= DepthExposed) exposed++;
                if (r.depthScore >= DepthEngaged) engaged++;
                if (r.depthScore >= DepthApplied) applied++;
                depthSum += r.depthScore;
            }

            return new PillarCoverageVector
            {
                exposedCount = exposed,
                engagedCount = engaged,
                appliedCount = applied,
                coverageRatio = exposed / (float)TotalPillars,
                averageDepth = exposed > 0 ? depthSum / exposed : 0f,
                weakestDomain = GetWeakestDomain(),
                dominantDomain = GetDominantDomain()
            };
        }

        private void OnChallengeProfileChanged(ChallengeProfileChangedEvent evt)
        {
            // Higher scaffolding = lower depth requirement for mastery
            switch (evt.Profile.scaffolding)
            {
                case ScaffoldingTier.Full:
                    _adaptiveThresholdEngaged = Mathf.Max(1, BaseThresholdEngaged - 1);
                    _adaptiveThresholdApplied = Mathf.Max(3, BaseThresholdApplied - 3);
                    break;
                case ScaffoldingTier.Guided:
                    _adaptiveThresholdEngaged = BaseThresholdEngaged;
                    _adaptiveThresholdApplied = Mathf.Max(4, BaseThresholdApplied - 2);
                    break;
                case ScaffoldingTier.Minimal:
                    _adaptiveThresholdEngaged = BaseThresholdEngaged;
                    _adaptiveThresholdApplied = BaseThresholdApplied;
                    break;
                case ScaffoldingTier.None:
                    _adaptiveThresholdEngaged = BaseThresholdEngaged + 1;
                    _adaptiveThresholdApplied = BaseThresholdApplied + 2;
                    break;
            }

            Debug.Log($"[CurriculumEngine] Adaptive thresholds: engaged={_adaptiveThresholdEngaged}, applied={_adaptiveThresholdApplied}");
        }

        // ── Persistence ─────────────────────────────────────────────────

        private void SaveData()
        {
            CurriculumSaveData save = new CurriculumSaveData();
            foreach (PillarExposureRecord record in _pillars.Values)
                save.pillars.Add(record);

            string json = JsonUtility.ToJson(save, true);

            try
            {
                File.WriteAllText(GetSavePath(), json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CurriculumEngine] Failed to save: {e.Message}");
            }
        }

        private void LoadData()
        {
            _pillars.Clear();

            string path = GetSavePath();
            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                CurriculumSaveData save = JsonUtility.FromJson<CurriculumSaveData>(json);
                if (save == null || save.pillars == null) return;

                foreach (PillarExposureRecord record in save.pillars)
                {
                    if (!string.IsNullOrEmpty(record.pillarId))
                        _pillars[record.pillarId] = record;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CurriculumEngine] Failed to load: {e.Message}");
            }
        }

        private string GetSavePath()
        {
            PlayerDataManager player = PlayerDataManager.Instance;
            string playerId = player != null ? player.PlayerId : "unknown";
            return Path.Combine(Application.persistentDataPath,
                FilePrefix + playerId + FileExtension);
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