using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>A single Elder dialogue line with topic context.</summary>
    [Serializable]
    public class ElderDialogueLine
    {
        public string topic;
        public string line;
        public string[] pillarIds;
    }

    /// <summary>Published when an Elder dialogue should be shown to the player.</summary>
    public struct ElderDialogueEvent
    {
        public string Topic;
        public string Line;
        public string ElderName;
    }

    /// <summary>
    /// Manages the Village Elder NPC dialogue system. Listens for
    /// <see cref="EducationalPromptEvent"/> from game systems and translates
    /// them into contextual Elder dialogue. The Elder never lectures — he
    /// asks questions. Dialogue is warm, unhurried, and purposeful.
    ///
    /// UI integration: subscribe to <see cref="ElderDialogueEvent"/> via EventBus
    /// and present the dialogue in a non-blocking panel.
    /// </summary>
    public class ElderDialogueManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static ElderDialogueManager Instance { get; private set; }

        [Header("Elder Identity")]
        [SerializeField] private string elderName = "Tok Wan";

        // ── Dialogue bank ───────────────────────────────────────────────
        // The Elder never lectures. He asks questions.

        private static readonly Dictionary<string, string[]> DialogueBank =
            new Dictionary<string, string[]>
            {
                { "RabbitNutrition", new[]
                {
                    "What do you think happens when a rabbit eats too much of one thing?",
                    "Have you noticed how {name} looks when the feed balance is right?",
                    "The old farmers used to say: eighty parts hay, the rest is kindness.",
                }},
                { "FeedRatio", new[]
                {
                    "Every creature has a rhythm to how it eats. What do you think {name}'s is?",
                    "Too much of anything — even good things — can cause harm. What would you adjust?",
                }},
                { "RabbitCritical", new[]
                {
                    "{name} needs you now. Not tomorrow. Now.",
                    "When a creature depends on you, their pain is your responsibility. What will you do?",
                }},
                { "SoilHealth", new[]
                {
                    "The soil is tired. What do you notice about the colour of the leaves?",
                    "The land remembers what you give back. Composting is not waste — it is return.",
                    "Three harvests, no rest. What would you tell a friend who never slept?",
                }},
                { "WaterScarcity", new[]
                {
                    "Water does not belong to us. We borrow it. How will you use today's share?",
                    "The AWF unit collects what the sky gives freely. Have you earned it yet?",
                }},
                { "Pollution", new[]
                {
                    "This land was not always sick. What do you think happened here?",
                    "Healing takes longer than harming. Are you willing to stay?",
                }},
                { "Gratitude", new[]
                {
                    "This animal gave so others could eat. That matters.",
                    "Before you take, do you stop to acknowledge what is given?",
                }},
                { "Temptation", new[]
                {
                    "The easy path is always wider. But where does it lead?",
                    "Someone is offering you a shortcut. What do they gain from your haste?",
                }},
                { "General", new[]
                {
                    "What did you learn today that surprised you?",
                    "If the land could speak, what do you think it would say to you?",
                    "Care is not a task. It is a practice. How are you practising?",
                }},
            };

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
                bus.Subscribe<EducationalPromptEvent>(HandlePrompt);
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Unsubscribe<EducationalPromptEvent>(HandlePrompt);
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Triggers Elder dialogue for a specific topic. Picks a random
        /// line from the dialogue bank and publishes an <see cref="ElderDialogueEvent"/>.
        /// </summary>
        public void TriggerDialogue(string topic, string rabbitName = "")
        {
            string line = PickLine(topic);
            if (!string.IsNullOrEmpty(rabbitName))
                line = line.Replace("{name}", rabbitName);

            PublishViaBus(new ElderDialogueEvent
            {
                Topic = topic,
                Line = line,
                ElderName = elderName
            });

            // Tag curriculum
            CurriculumEngine engine = CurriculumEngine.Instance;
            if (engine != null)
                engine.RecordActivity("A01_Oral_Communication", 1f);
        }

        // ── Event handler ───────────────────────────────────────────────

        private void HandlePrompt(EducationalPromptEvent evt)
        {
            string rabbitName = "";
            if (!string.IsNullOrEmpty(evt.RabbitId))
            {
                RabbitCareManager care = RabbitCareManager.Instance;
                if (care != null)
                {
                    RabbitData data = care.GetRabbitData(evt.RabbitId);
                    if (data != null) rabbitName = data.name;
                }
            }

            TriggerDialogue(evt.Topic, rabbitName);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static string PickLine(string topic)
        {
            if (DialogueBank.TryGetValue(topic, out string[] lines) && lines.Length > 0)
                return lines[UnityEngine.Random.Range(0, lines.Length)];

            if (DialogueBank.TryGetValue("General", out string[] fallback) && fallback.Length > 0)
                return fallback[UnityEngine.Random.Range(0, fallback.Length)];

            return "What do you notice about the world around you today?";
        }

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
        }
    }
}
