using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Enums ───────────────────────────────────────────────────────────────

    /// <summary>Personality dimension measured by onboarding scenarios.</summary>
    public enum PersonalityDimension
    {
        ExplorerDrive     = 0,
        AchieverDrive     = 1,
        NurturerDrive     = 2,
        ScholarDrive      = 3,
        RiskTolerance     = 4,
        SocialPreference  = 5,
        SpiritualOpenness = 6,
        ReflectiveDepth   = 7
    }

    // ── Data types ──────────────────────────────────────────────────────────

    /// <summary>Player personality vector built from the onboarding quiz.</summary>
    [Serializable]
    public struct PlayerPersonalityVector
    {
        public float explorerDrive;
        public float achieverDrive;
        public float nurturerDrive;
        public float scholarDrive;
        public float riskTolerance;
        public float socialPreference;
        public float spiritualOpenness;
        public float reflectiveDepth;
    }

    /// <summary>A single choice within an onboarding scenario.</summary>
    [Serializable]
    public struct ScenarioChoice
    {
        public string label;
        public PersonalityDimension primaryDimension;
        public float primaryWeight;
        public PersonalityDimension secondaryDimension;
        public float secondaryWeight;
    }

    /// <summary>
    /// An onboarding scenario presented as an in-game situation with four choices.
    /// Each choice maps to personality dimensions with weighted contributions.
    /// </summary>
    [Serializable]
    public class OnboardingScenario
    {
        public string scenarioId;
        public string title;
        public string narrative;
        public List<ScenarioChoice> choices = new List<ScenarioChoice>();
    }

    // ── Event structs ───────────────────────────────────────────────────────

    /// <summary>Published when a new scenario is presented to the player.</summary>
    public struct OnboardingScenarioPresentedEvent
    {
        public int ScenarioIndex;
        public string Title;
    }

    /// <summary>Published when the player selects a choice in a scenario.</summary>
    public struct OnboardingChoiceMadeEvent
    {
        public int ScenarioIndex;
        public int ChoiceIndex;
    }

    /// <summary>Published when the full onboarding quiz is complete.</summary>
    public struct OnboardingQuizCompletedEvent
    {
        public PlayerPersonalityVector Personality;
    }

    // ── Telemetry constants ─────────────────────────────────────────────────

    /// <summary>Telemetry event type constants for the onboarding quiz.</summary>
    public static class OnboardingTelemetryEvents
    {
        public const string ScenarioPresented = "ONBOARDING_SCENARIO_PRESENTED";
        public const string ChoiceMade = "ONBOARDING_CHOICE_MADE";
        public const string QuizCompleted = "ONBOARDING_QUIZ_COMPLETED";
    }

    // ── Manager ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Singleton that delivers 8 onboarding scenario questions through gameplay,
    /// accumulates weighted dimension scores per choice, and builds a
    /// <see cref="PlayerPersonalityVector"/> that seeds the initial
    /// <see cref="LearnerProfileVector"/> via <see cref="LearnerProfileTracker"/>.
    /// </summary>
    public class OnboardingQuizManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static OnboardingQuizManager Instance { get; private set; }

        /// <summary>Whether the quiz is currently in progress.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Index of the current scenario (0-7).</summary>
        public int CurrentScenarioIndex { get; private set; }

        /// <summary>Total number of scenarios.</summary>
        public int ScenarioCount => _scenarios.Count;

        /// <summary>Current scenario, or null if quiz is not active.</summary>
        public OnboardingScenario CurrentScenario =>
            IsActive && CurrentScenarioIndex < _scenarios.Count
                ? _scenarios[CurrentScenarioIndex]
                : null;

        /// <summary>Built personality vector. Valid after quiz completion.</summary>
        public PlayerPersonalityVector Result { get; private set; }

        private readonly List<OnboardingScenario> _scenarios = new List<OnboardingScenario>();
        private PlayerPersonalityVector _accumulator;

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildScenarios();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Begins the onboarding quiz from scenario 0. Skips if the player
        /// has already completed onboarding.
        /// </summary>
        public void BeginQuiz()
        {
            LearnerProfileTracker tracker = LearnerProfileTracker.Instance;
            if (tracker != null && tracker.OnboardingComplete)
            {
                Debug.Log("[OnboardingQuiz] Already completed, skipping.");
                return;
            }

            IsActive = true;
            CurrentScenarioIndex = 0;
            _accumulator = new PlayerPersonalityVector();

            PresentCurrentScenario();
        }

        /// <summary>
        /// Records the player's choice for the current scenario and advances
        /// to the next. Completes the quiz after all 8 scenarios.
        /// </summary>
        /// <param name="choiceIndex">Index of the chosen option (0-3).</param>
        public void SubmitChoice(int choiceIndex)
        {
            if (!IsActive) return;
            if (CurrentScenarioIndex >= _scenarios.Count) return;

            OnboardingScenario scenario = _scenarios[CurrentScenarioIndex];
            if (choiceIndex < 0 || choiceIndex >= scenario.choices.Count) return;

            ScenarioChoice choice = scenario.choices[choiceIndex];

            ApplyWeight(choice.primaryDimension, choice.primaryWeight);
            ApplyWeight(choice.secondaryDimension, choice.secondaryWeight);

            PublishViaBus(new OnboardingChoiceMadeEvent
            {
                ScenarioIndex = CurrentScenarioIndex,
                ChoiceIndex = choiceIndex
            });

            PublishTelemetry(OnboardingTelemetryEvents.ChoiceMade,
                $"{{\"scenario\":\"{scenario.scenarioId}\",\"choice\":{choiceIndex}}}");

            CurrentScenarioIndex++;

            if (CurrentScenarioIndex >= _scenarios.Count)
                CompleteQuiz();
            else
                PresentCurrentScenario();
        }

        // ── Internals ───────────────────────────────────────────────────

        private void PresentCurrentScenario()
        {
            OnboardingScenario scenario = _scenarios[CurrentScenarioIndex];

            PublishViaBus(new OnboardingScenarioPresentedEvent
            {
                ScenarioIndex = CurrentScenarioIndex,
                Title = scenario.title
            });

            PublishTelemetry(OnboardingTelemetryEvents.ScenarioPresented,
                $"{{\"scenario\":\"{scenario.scenarioId}\",\"index\":{CurrentScenarioIndex}}}");
        }

        private void CompleteQuiz()
        {
            IsActive = false;

            // Normalise to 0-1 range (8 scenarios, max ~1.0 per dimension)
            _accumulator.explorerDrive     = Mathf.Clamp01(_accumulator.explorerDrive);
            _accumulator.achieverDrive     = Mathf.Clamp01(_accumulator.achieverDrive);
            _accumulator.nurturerDrive     = Mathf.Clamp01(_accumulator.nurturerDrive);
            _accumulator.scholarDrive      = Mathf.Clamp01(_accumulator.scholarDrive);
            _accumulator.riskTolerance     = Mathf.Clamp01(_accumulator.riskTolerance);
            _accumulator.socialPreference  = Mathf.Clamp01(_accumulator.socialPreference);
            _accumulator.spiritualOpenness = Mathf.Clamp01(_accumulator.spiritualOpenness);
            _accumulator.reflectiveDepth   = Mathf.Clamp01(_accumulator.reflectiveDepth);

            Result = _accumulator;

            LearnerProfileTracker tracker = LearnerProfileTracker.Instance;
            if (tracker != null)
                tracker.SetPersonality(Result);

            PublishViaBus(new OnboardingQuizCompletedEvent
            {
                Personality = Result
            });

            PublishTelemetry(OnboardingTelemetryEvents.QuizCompleted,
                JsonUtility.ToJson(Result));

            Debug.Log("[OnboardingQuiz] Complete. " +
                      $"explorer={Result.explorerDrive:F2} " +
                      $"achiever={Result.achieverDrive:F2} " +
                      $"nurturer={Result.nurturerDrive:F2} " +
                      $"scholar={Result.scholarDrive:F2}");
        }

        private void ApplyWeight(PersonalityDimension dim, float weight)
        {
            switch (dim)
            {
                case PersonalityDimension.ExplorerDrive:
                    _accumulator.explorerDrive += weight;
                    break;
                case PersonalityDimension.AchieverDrive:
                    _accumulator.achieverDrive += weight;
                    break;
                case PersonalityDimension.NurturerDrive:
                    _accumulator.nurturerDrive += weight;
                    break;
                case PersonalityDimension.ScholarDrive:
                    _accumulator.scholarDrive += weight;
                    break;
                case PersonalityDimension.RiskTolerance:
                    _accumulator.riskTolerance += weight;
                    break;
                case PersonalityDimension.SocialPreference:
                    _accumulator.socialPreference += weight;
                    break;
                case PersonalityDimension.SpiritualOpenness:
                    _accumulator.spiritualOpenness += weight;
                    break;
                case PersonalityDimension.ReflectiveDepth:
                    _accumulator.reflectiveDepth += weight;
                    break;
            }
        }

        // ── 8 Onboarding Scenarios ──────────────────────────────────────

        private void BuildScenarios()
        {
            // ── Scenario 1: The Lost Seedling ───────────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_01_LOST_SEEDLING",
                title = "The Lost Seedling",
                narrative = "A rare seedling struggles at the edge of a crumbling path. Its roots are exposed and the soil around it is dry.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Carefully replant it deeper in the forest",
                        primaryDimension = PersonalityDimension.NurturerDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.ExplorerDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Mark its location and study it later",
                        primaryDimension = PersonalityDimension.ScholarDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.ReflectiveDepth, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Bring water and nurture it right here",
                        primaryDimension = PersonalityDimension.NurturerDrive, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.AchieverDrive, secondaryWeight = 0.15f
                    },
                    new ScenarioChoice
                    {
                        label = "Keep exploring to find more like it",
                        primaryDimension = PersonalityDimension.ExplorerDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.RiskTolerance, secondaryWeight = 0.1f
                    }
                }
            });

            // ── Scenario 2: The Fork in the Path ────────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_02_FORK_PATH",
                title = "The Fork in the Path",
                narrative = "Two paths diverge ahead: one leads to a known kampung village, the other disappears into unexplored mist.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Take the misty unknown path",
                        primaryDimension = PersonalityDimension.ExplorerDrive, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.RiskTolerance, secondaryWeight = 0.2f
                    },
                    new ScenarioChoice
                    {
                        label = "Visit the village to gather information first",
                        primaryDimension = PersonalityDimension.ScholarDrive, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.SocialPreference, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Split your time between both paths",
                        primaryDimension = PersonalityDimension.AchieverDrive, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.ExplorerDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Ask a local spirit for guidance",
                        primaryDimension = PersonalityDimension.SpiritualOpenness, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.SocialPreference, secondaryWeight = 0.1f
                    }
                }
            });

            // ── Scenario 3: The Wounded Creature ────────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_03_WOUNDED_CREATURE",
                title = "The Wounded Creature",
                narrative = "A small forest creature is tangled in thorns beside the trail, whimpering softly.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Free it immediately",
                        primaryDimension = PersonalityDimension.NurturerDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.RiskTolerance, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Observe carefully before acting",
                        primaryDimension = PersonalityDimension.ScholarDrive, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.ReflectiveDepth, secondaryWeight = 0.15f
                    },
                    new ScenarioChoice
                    {
                        label = "Call out for someone to help",
                        primaryDimension = PersonalityDimension.SocialPreference, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.NurturerDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Document the incident to prevent it happening again",
                        primaryDimension = PersonalityDimension.AchieverDrive, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.ScholarDrive, secondaryWeight = 0.15f
                    }
                }
            });

            // ── Scenario 4: The Ancient Inscription ─────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_04_ANCIENT_INSCRIPTION",
                title = "The Ancient Inscription",
                narrative = "Deep inside a cave, you find carved text on a stone wall, glowing faintly with an inner light.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Try to decipher it yourself",
                        primaryDimension = PersonalityDimension.ScholarDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.AchieverDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Sketch it carefully to study later",
                        primaryDimension = PersonalityDimension.ReflectiveDepth, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.ScholarDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Touch the inscription reverently",
                        primaryDimension = PersonalityDimension.SpiritualOpenness, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.RiskTolerance, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Search the rest of the cave for more inscriptions",
                        primaryDimension = PersonalityDimension.ExplorerDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.ScholarDrive, secondaryWeight = 0.05f
                    }
                }
            });

            // ── Scenario 5: The Approaching Storm ───────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_05_STORM",
                title = "The Approaching Storm",
                narrative = "Dark clouds gather swiftly over the canopy. Thunder rumbles in the distance and the wind picks up.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Seek shelter and wait it out",
                        primaryDimension = PersonalityDimension.ReflectiveDepth, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.NurturerDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Press on toward the summit",
                        primaryDimension = PersonalityDimension.RiskTolerance, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.AchieverDrive, secondaryWeight = 0.15f
                    },
                    new ScenarioChoice
                    {
                        label = "Find others and shelter together",
                        primaryDimension = PersonalityDimension.SocialPreference, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.NurturerDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Observe the storm patterns carefully",
                        primaryDimension = PersonalityDimension.ScholarDrive, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.ExplorerDrive, secondaryWeight = 0.1f
                    }
                }
            });

            // ── Scenario 6: The Blessing Offering ───────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_06_BLESSING_OFFERING",
                title = "The Blessing Offering",
                narrative = "You have gathered enough blessing to either upgrade your tools or donate it to restore a withering grove nearby.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Upgrade your tools for efficiency",
                        primaryDimension = PersonalityDimension.AchieverDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.ExplorerDrive, secondaryWeight = 0.05f
                    },
                    new ScenarioChoice
                    {
                        label = "Donate everything to save the grove",
                        primaryDimension = PersonalityDimension.NurturerDrive, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.SpiritualOpenness, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Split the blessing between both",
                        primaryDimension = PersonalityDimension.ReflectiveDepth, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.AchieverDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Look for another way to help the grove",
                        primaryDimension = PersonalityDimension.ScholarDrive, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.ExplorerDrive, secondaryWeight = 0.15f
                    }
                }
            });

            // ── Scenario 7: The Veiled Threshold ────────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_07_VEILED_THRESHOLD",
                title = "The Veiled Threshold",
                narrative = "A shimmering portal to the Veiled World appears between two ancient trees, pulsing with soft light.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Step through immediately",
                        primaryDimension = PersonalityDimension.RiskTolerance, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.ExplorerDrive, secondaryWeight = 0.15f
                    },
                    new ScenarioChoice
                    {
                        label = "Prepare carefully before entering",
                        primaryDimension = PersonalityDimension.AchieverDrive, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.ReflectiveDepth, secondaryWeight = 0.15f
                    },
                    new ScenarioChoice
                    {
                        label = "Find a companion to enter with",
                        primaryDimension = PersonalityDimension.SocialPreference, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.NurturerDrive, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Study the threshold from this side first",
                        primaryDimension = PersonalityDimension.SpiritualOpenness, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.ScholarDrive, secondaryWeight = 0.2f
                    }
                }
            });

            // ── Scenario 8: The Returning Dawn ──────────────────────────
            _scenarios.Add(new OnboardingScenario
            {
                scenarioId = "ONB_08_RETURNING_DAWN",
                title = "The Returning Dawn",
                narrative = "Dawn breaks after a long night session. Golden light filters through the canopy and the forest awakens around you.",
                choices = new List<ScenarioChoice>
                {
                    new ScenarioChoice
                    {
                        label = "Continue exploring the night creatures before they vanish",
                        primaryDimension = PersonalityDimension.ExplorerDrive, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.RiskTolerance, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Greet the dawn with quiet meditation",
                        primaryDimension = PersonalityDimension.SpiritualOpenness, primaryWeight = 0.3f,
                        secondaryDimension = PersonalityDimension.ReflectiveDepth, secondaryWeight = 0.1f
                    },
                    new ScenarioChoice
                    {
                        label = "Share your discoveries with others",
                        primaryDimension = PersonalityDimension.SocialPreference, primaryWeight = 0.2f,
                        secondaryDimension = PersonalityDimension.NurturerDrive, secondaryWeight = 0.15f
                    },
                    new ScenarioChoice
                    {
                        label = "Return home to rest and journal your reflections",
                        primaryDimension = PersonalityDimension.ReflectiveDepth, primaryWeight = 0.25f,
                        secondaryDimension = PersonalityDimension.ScholarDrive, secondaryWeight = 0.1f
                    }
                }
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
    }
}
