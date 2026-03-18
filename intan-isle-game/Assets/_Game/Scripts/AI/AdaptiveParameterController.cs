using System;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Data types ──────────────────────────────────────────────────────────

    /// <summary>
    /// Recommended game parameter adjustments derived from the current
    /// <see cref="LearnerProfileVector"/>. Game systems query
    /// <see cref="AdaptiveParameterController.CurrentProfile"/> to adapt
    /// their behaviour to the learner.
    /// </summary>
    [Serializable]
    public struct RecommendedChallengeProfile
    {
        /// <summary>Overall difficulty multiplier (0.5 = easy, 1.0 = normal, 1.5 = hard).</summary>
        public float difficultyMultiplier;

        /// <summary>Scaffolding tier determining hint and guidance frequency.</summary>
        public ScaffoldingTier scaffolding;

        /// <summary>Seconds between automatic hint prompts. Higher = less help.</summary>
        public float hintIntervalSec;

        /// <summary>Multiplier on stamina drain rate (lower = more forgiving).</summary>
        public float staminaDrainMultiplier;

        /// <summary>Multiplier on crop growth speed (higher = faster).</summary>
        public float cropGrowthMultiplier;

        /// <summary>Multiplier on healing cooldown duration (lower = faster).</summary>
        public float healingCooldownMultiplier;

        /// <summary>Multiplier on day-night cycle phase durations.</summary>
        public float dayNightPaceMultiplier;

        /// <summary>Dominant content focus zone type for scenario routing.</summary>
        public ZoneType contentFocusZone;

        /// <summary>Recommended learning style to emphasise in UI/UX.</summary>
        public LearningStyle emphasiseStyle;
    }

    // ── Event struct ────────────────────────────────────────────────────────

    /// <summary>Published when the recommended challenge profile is recalculated.</summary>
    public struct ChallengeProfileChangedEvent
    {
        public RecommendedChallengeProfile Profile;
    }

    // ── Controller ──────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that translates the current <see cref="LearnerProfileVector"/>
    /// into a <see cref="RecommendedChallengeProfile"/> and publishes it via
    /// <see cref="EventBus"/> for consumption by game systems.
    /// </summary>
    public class AdaptiveParameterController : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static AdaptiveParameterController Instance { get; private set; }

        /// <summary>Current recommended challenge profile.</summary>
        public RecommendedChallengeProfile CurrentProfile { get; private set; }

        [Header("Difficulty Range")]
        [SerializeField] private float difficultyMin = 0.5f;
        [SerializeField] private float difficultyMax = 1.5f;

        [Header("Hint Timing")]
        [SerializeField] private float hintIntervalMinSec = 30f;
        [SerializeField] private float hintIntervalMaxSec = 300f;

        [Header("Pacing Range")]
        [SerializeField] private float paceMin = 0.7f;
        [SerializeField] private float paceMax = 1.3f;

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

        private void OnEnable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Subscribe<ProfileEvaluatedEvent>(OnProfileEvaluated);
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Unsubscribe<ProfileEvaluatedEvent>(OnProfileEvaluated);
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Forces a recalculation of the challenge profile from the given
        /// learner profile. Publishes <see cref="ChallengeProfileChangedEvent"/>.
        /// </summary>
        public void Recalculate(LearnerProfileVector learner)
        {
            RecommendedChallengeProfile rcp = new RecommendedChallengeProfile();

            // ── Difficulty ──────────────────────────────────────────────
            float competence = (learner.persistenceIndex
                              + learner.curiosityIndex
                              + learner.ecologicalLiteracy) / 3f;

            switch (learner.challengePref)
            {
                case ChallengePreference.Gentle:
                    rcp.difficultyMultiplier = Mathf.Lerp(difficultyMin,
                        1f, competence);
                    break;
                case ChallengePreference.Ambitious:
                    rcp.difficultyMultiplier = Mathf.Lerp(1f,
                        difficultyMax, competence);
                    break;
                default:
                    rcp.difficultyMultiplier = Mathf.Lerp(
                        difficultyMin + 0.2f,
                        difficultyMax - 0.2f, competence);
                    break;
            }

            // ── Scaffolding ─────────────────────────────────────────────
            if (learner.frustrationTolerance < 0.25f
                || learner.engagementLevel == EngagementLevel.Disengaged)
                rcp.scaffolding = ScaffoldingTier.Full;
            else if (learner.persistenceIndex < 0.3f)
                rcp.scaffolding = ScaffoldingTier.Guided;
            else if (learner.persistenceIndex > 0.7f
                     && learner.challengePref == ChallengePreference.Ambitious)
                rcp.scaffolding = ScaffoldingTier.None;
            else
                rcp.scaffolding = ScaffoldingTier.Minimal;

            // ── Hint interval ───────────────────────────────────────────
            float hintT = Mathf.Clamp01(
                (learner.persistenceIndex + learner.curiosityIndex) * 0.5f);
            rcp.hintIntervalSec = Mathf.Lerp(hintIntervalMinSec,
                hintIntervalMaxSec, hintT);

            // ── Stamina drain ───────────────────────────────────────────
            rcp.staminaDrainMultiplier = learner.challengePref == ChallengePreference.Gentle
                ? 0.6f : learner.challengePref == ChallengePreference.Ambitious
                    ? 1.2f : 1f;

            // ── Crop growth ─────────────────────────────────────────────
            rcp.cropGrowthMultiplier = learner.ecologicalLiteracy < 0.3f
                ? 1.5f : learner.ecologicalLiteracy > 0.7f
                    ? 0.8f : 1f;

            // ── Healing cooldown ────────────────────────────────────────
            rcp.healingCooldownMultiplier = learner.empathyIndex > 0.6f
                ? 0.7f : 1f;

            // ── Day/night pacing ────────────────────────────────────────
            if (learner.engagementLevel == EngagementLevel.FlowState)
                rcp.dayNightPaceMultiplier = paceMax;
            else if (learner.engagementLevel == EngagementLevel.Disengaged)
                rcp.dayNightPaceMultiplier = paceMin;
            else
                rcp.dayNightPaceMultiplier = 1f;

            // ── Content focus ───────────────────────────────────────────
            float lowestIndex = Mathf.Min(
                learner.ecologicalLiteracy,
                Mathf.Min(learner.culturalSensitivity, learner.spatialAwareness));

            if (Mathf.Approximately(lowestIndex, learner.ecologicalLiteracy))
                rcp.contentFocusZone = ZoneType.ANCIENT_FOREST;
            else if (Mathf.Approximately(lowestIndex, learner.culturalSensitivity))
                rcp.contentFocusZone = ZoneType.SACRED_FOREST;
            else
                rcp.contentFocusZone = ZoneType.WATERWAY;

            // ── Emphasise style ─────────────────────────────────────────
            rcp.emphasiseStyle = learner.dominantStyle;

            CurrentProfile = rcp;

            PublishViaBus(new ChallengeProfileChangedEvent { Profile = rcp });

            Debug.Log($"[AdaptiveParams] difficulty={rcp.difficultyMultiplier:F2} " +
                      $"scaffolding={rcp.scaffolding} hint={rcp.hintIntervalSec:F0}s " +
                      $"style={rcp.emphasiseStyle}");
        }

        // ── Event handler ───────────────────────────────────────────────

        private void OnProfileEvaluated(ProfileEvaluatedEvent evt)
        {
            Recalculate(evt.Profile);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
        }
    }
}
