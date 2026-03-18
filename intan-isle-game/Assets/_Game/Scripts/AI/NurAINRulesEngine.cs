using System;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── NGE Enums ──────────────────────────────────────────────────────────

    /// <summary>Dominant learning style archetype.</summary>
    public enum LearningStyle
    {
        Explorer  = 0,
        Achiever  = 1,
        Nurturer  = 2,
        Scholar   = 3
    }

    /// <summary>Preferred challenge level as determined by the rules engine.</summary>
    public enum ChallengePreference
    {
        Gentle    = 0,
        Moderate  = 1,
        Ambitious = 2
    }

    /// <summary>Current engagement level derived from active play ratio.</summary>
    public enum EngagementLevel
    {
        Disengaged = 0,
        Passive    = 1,
        Active     = 2,
        FlowState  = 3
    }

    /// <summary>Scaffolding tier controlling how much guidance the player receives.</summary>
    public enum ScaffoldingTier
    {
        Full    = 0,
        Guided  = 1,
        Minimal = 2,
        None    = 3
    }

    // ── NGE Data Vectors ───────────────────────────────────────────────────

    /// <summary>
    /// Captures quantified player behaviour signals within a single game session.
    /// Built by <see cref="LearnerProfileTracker"/> and consumed by
    /// <see cref="NurAINRulesEngine"/>.
    /// </summary>
    [Serializable]
    public struct SessionBehaviourVector
    {
        // Exploration
        public float explorationRadiusKm;
        public int   uniqueZonesVisited;
        public int   zoneHealingActions;

        // Engagement
        public float sessionDurationMin;
        public float activePlayRatio;
        public float veiledWorldTimeRatio;
        public int   flightActivations;

        // Production
        public int   cropsHarvested;
        public int   harvestProductsProcessed;
        public int   payItForwardGifts;

        // Animal care
        public int   rabbitCareActions;
        public float rabbitHealthAvg;

        // Performance
        public float blessingGainRate;
        public float taskCompletionRate;
        public int   helpRequests;
        public int   retryCount;

        // Curriculum
        public int   curriculumPillarsDeepened;

        // Restraint
        public float restraintRatio;
    }

    /// <summary>
    /// Accumulated learner profile built from session behaviour across time.
    /// Persisted by <see cref="LearnerProfileTracker"/> and consumed by
    /// <see cref="IEPGoalManager"/> and <see cref="AdaptiveParameterController"/>.
    /// </summary>
    [Serializable]
    public struct LearnerProfileVector
    {
        // Core indices (0-1 each)
        public float curiosityIndex;
        public float persistenceIndex;
        public float empathyIndex;
        public float spatialAwareness;
        public float ecologicalLiteracy;
        public float culturalSensitivity;
        public float spiritualResonance;

        // Preferences
        public LearningStyle     dominantStyle;
        public ChallengePreference challengePref;
        public EngagementLevel   engagementLevel;

        // Growth tracking
        public float frustrationTolerance;
        public float attentionSpanMin;
        public int   sessionCount;
        public float totalPlayTimeMin;
    }

    // ── Event struct ───────────────────────────────────────────────────────

    /// <summary>Published when the rules engine completes an evaluation pass.</summary>
    public struct ProfileEvaluatedEvent
    {
        public LearnerProfileVector Profile;
        public SessionBehaviourVector Session;
    }

    // ── Rules Engine ───────────────────────────────────────────────────────

    /// <summary>
    /// Deterministic rules engine that evaluates a
    /// <see cref="SessionBehaviourVector"/> against the current
    /// <see cref="LearnerProfileVector"/> and returns an updated profile.
    /// Contains 25 IF/THEN rules covering curiosity, persistence, empathy,
    /// spatial awareness, ecological literacy, cultural sensitivity,
    /// spiritual resonance, learning style classification, and challenge
    /// calibration.
    /// </summary>
    public class NurAINRulesEngine : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static NurAINRulesEngine Instance { get; private set; }

        // ── Tuning ──────────────────────────────────────────────────────

        [Header("Curiosity Thresholds")]
        [SerializeField] private float explorationRadiusHighKm = 2f;
        [SerializeField] private int   zoneVisitDiverseMin     = 3;
        [SerializeField] private float explorationStagnantKm   = 0.5f;

        [Header("Persistence Thresholds")]
        [SerializeField] private int   retryPersistentMin       = 3;
        [SerializeField] private float sessionLongMin           = 30f;
        [SerializeField] private int   retryFrustrationMin      = 5;
        [SerializeField] private float taskCompFrustrationMax   = 0.3f;

        [Header("Empathy Thresholds")]
        [SerializeField] private int   giftsEmpathyMin    = 2;
        [SerializeField] private float restraintHighRatio = 0.6f;
        [SerializeField] private int   healingActionsMin  = 2;

        [Header("Index Deltas")]
        [SerializeField] private float indexSmallDelta   = 0.03f;
        [SerializeField] private float indexMediumDelta  = 0.04f;
        [SerializeField] private float indexLargeDelta   = 0.05f;
        [SerializeField] private float indexPenaltyDelta = 0.02f;

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

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Evaluates <paramref name="session"/> against <paramref name="current"/>
        /// and returns the updated profile. Publishes a
        /// <see cref="ProfileEvaluatedEvent"/> via <see cref="EventBus"/>.
        /// </summary>
        public LearnerProfileVector Evaluate(
            SessionBehaviourVector session,
            LearnerProfileVector current)
        {
            LearnerProfileVector p = current;
            p.sessionCount++;
            p.totalPlayTimeMin += session.sessionDurationMin;
            p.attentionSpanMin = Mathf.Lerp(p.attentionSpanMin, session.sessionDurationMin, 0.3f);

            // ── Rule  1: Curiosity — wide exploration radius ─────────────
            if (session.explorationRadiusKm > explorationRadiusHighKm)
                p.curiosityIndex = C01(p.curiosityIndex + indexMediumDelta);

            // ── Rule  2: Curiosity — diverse zone visits ─────────────────
            if (session.uniqueZonesVisited >= zoneVisitDiverseMin)
                p.curiosityIndex = C01(p.curiosityIndex + indexSmallDelta);

            // ── Rule  3: Curiosity — harvest chain participation ─────────
            if (session.harvestProductsProcessed >= 2)
                p.curiosityIndex = C01(p.curiosityIndex + indexSmallDelta);

            // ── Rule  4: Curiosity — stagnation penalty ──────────────────
            if (session.explorationRadiusKm < explorationStagnantKm
                && session.sessionDurationMin > 10f)
                p.curiosityIndex = C01(p.curiosityIndex - indexPenaltyDelta);

            // ── Rule  5: Persistence — retry after failure ───────────────
            if (session.retryCount >= retryPersistentMin
                && session.taskCompletionRate > 0.5f)
                p.persistenceIndex = C01(p.persistenceIndex + indexLargeDelta);

            // ── Rule  6: Persistence — long session engagement ───────────
            if (session.sessionDurationMin > sessionLongMin)
                p.persistenceIndex = C01(p.persistenceIndex + indexSmallDelta);

            // ── Rule  7: Persistence — independent problem solving ───────
            if (session.helpRequests == 0 && session.taskCompletionRate > 0.5f)
                p.persistenceIndex = C01(p.persistenceIndex + indexMediumDelta);

            // ── Rule  8: Persistence — frustration detection ─────────────
            if (session.retryCount >= retryFrustrationMin
                && session.taskCompletionRate < taskCompFrustrationMax)
                p.frustrationTolerance = C01(p.frustrationTolerance - indexSmallDelta);

            // ── Rule  9: Empathy — Pay It Forward generosity ─────────────
            if (session.payItForwardGifts >= giftsEmpathyMin)
                p.empathyIndex = C01(p.empathyIndex + indexLargeDelta);

            // ── Rule 10: Empathy — restraint in resource use ─────────────
            if (session.restraintRatio > restraintHighRatio)
                p.empathyIndex = C01(p.empathyIndex + indexSmallDelta);

            // ── Rule 11: Empathy — animal care quality ───────────────────
            if (session.rabbitCareActions >= 2 && session.rabbitHealthAvg > 0.7f)
                p.empathyIndex = C01(p.empathyIndex + indexMediumDelta);

            // ── Rule 12: Empathy — healing action completion ─────────────
            if (session.zoneHealingActions >= healingActionsMin)
                p.empathyIndex = C01(p.empathyIndex + indexMediumDelta);

            // ── Rule 13: Spatial — flight usage ──────────────────────────
            if (session.flightActivations >= 2)
                p.spatialAwareness = C01(p.spatialAwareness + indexSmallDelta);

            // ── Rule 14: Spatial — zone navigation ───────────────────────
            if (session.uniqueZonesVisited >= 4)
                p.spatialAwareness = C01(p.spatialAwareness + indexMediumDelta);

            // ── Rule 15: Spatial — exploration radius growth ─────────────
            if (session.explorationRadiusKm > 5f && session.flightActivations > 0)
                p.spatialAwareness = C01(p.spatialAwareness + indexLargeDelta);

            // ── Rule 16: Ecological — crop diversity ─────────────────────
            if (session.cropsHarvested >= 3)
                p.ecologicalLiteracy = C01(p.ecologicalLiteracy + indexMediumDelta);

            // ── Rule 17: Ecological — zone healing ───────────────────────
            if (session.zoneHealingActions >= 1)
                p.ecologicalLiteracy = C01(p.ecologicalLiteracy + indexLargeDelta);

            // ── Rule 18: Ecological — curriculum pillar depth ────────────
            if (session.curriculumPillarsDeepened >= 1)
                p.ecologicalLiteracy = C01(p.ecologicalLiteracy + indexSmallDelta);

            // ── Rule 19: Cultural — veiled world engagement ──────────────
            if (session.veiledWorldTimeRatio > 0.2f)
                p.culturalSensitivity = C01(p.culturalSensitivity + indexSmallDelta);

            // ── Rule 20: Cultural — blessing consistency ─────────────────
            if (session.blessingGainRate > 1.5f && session.restraintRatio > 0.4f)
                p.culturalSensitivity = C01(p.culturalSensitivity + indexMediumDelta);

            // ── Rule 21: Spiritual — veiled world time investment ────────
            if (session.veiledWorldTimeRatio > 0.4f)
                p.spiritualResonance = C01(p.spiritualResonance + indexLargeDelta);

            // ── Rule 22: Style — Explorer archetype detection ────────────
            if (session.explorationRadiusKm > 3f && session.flightActivations >= 3)
                p.dominantStyle = LearningStyle.Explorer;

            // ── Rule 23: Style — Achiever archetype detection ────────────
            if (session.taskCompletionRate > 0.8f
                && session.helpRequests <= 1
                && session.cropsHarvested >= 2)
                p.dominantStyle = LearningStyle.Achiever;

            // ── Rule 24: Style — Nurturer archetype detection ────────────
            if (p.empathyIndex > 0.6f
                && session.payItForwardGifts >= 1
                && session.rabbitCareActions >= 1)
                p.dominantStyle = LearningStyle.Nurturer;

            // ── Rule 25: Challenge — dynamic difficulty calibration ──────
            if (session.taskCompletionRate > 0.85f && session.retryCount < 2)
                p.challengePref = ChallengePreference.Ambitious;
            else if (session.taskCompletionRate < 0.4f
                     || p.frustrationTolerance < 0.3f)
                p.challengePref = ChallengePreference.Gentle;
            else
                p.challengePref = ChallengePreference.Moderate;

            // ── Engagement level (derived from active play ratio) ────────
            if (session.activePlayRatio > 0.8f)
                p.engagementLevel = EngagementLevel.FlowState;
            else if (session.activePlayRatio > 0.5f)
                p.engagementLevel = EngagementLevel.Active;
            else if (session.activePlayRatio > 0.2f)
                p.engagementLevel = EngagementLevel.Passive;
            else
                p.engagementLevel = EngagementLevel.Disengaged;

            PublishViaBus(new ProfileEvaluatedEvent
            {
                Profile = p,
                Session = session
            });

            return p;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static float C01(float v) => Mathf.Clamp01(v);

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
        }
    }
}
