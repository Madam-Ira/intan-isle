using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>
    /// Subscribes to all gameplay events via <see cref="EventBus"/> and
    /// automatically maps them to curriculum pillar tags. For each tagged
    /// event, calls <see cref="CurriculumEngine.RecordActivity"/> with the
    /// appropriate pillar IDs and competency delta.
    ///
    /// Pillar ID format: {Domain}{Number}_{Name}
    /// Domains A–J, 5 pillars each = 50 pillars.
    /// Depth: 1=exposure (saw), 2=engagement (chose), 3=mastery (correct+reflected).
    /// </summary>
    public class CurriculumEventMapper : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static CurriculumEventMapper Instance { get; private set; }

        // ── Pillar ID constants ─────────────────────────────────────────

        // Domain A — Communication & Language
        private const string A01 = "A01_Oral_Communication";
        private const string A02 = "A02_Written_Expression";

        // Domain B — Science & Biology
        private const string B01 = "B01_Animal_Biology";
        private const string B02 = "B02_Plant_Biology";
        private const string B03 = "B03_Food_Science";
        private const string B04 = "B04_Water_Science";
        private const string B05 = "B05_Ecology";

        // Domain C — Ethics & Values
        private const string C01 = "C01_Animal_Welfare";
        private const string C02 = "C02_Environmental_Ethics";
        private const string C03 = "C03_Resource_Stewardship";
        private const string C04 = "C04_Community_Responsibility";

        // Domain D — Economy & Trade
        private const string D01 = "D01_Financial_Literacy";
        private const string D02 = "D02_Circular_Economy";
        private const string D03 = "D03_Supply_Chain";

        // Domain E — Engineering & Systems
        private const string E01 = "E01_Water_Engineering";
        private const string E02 = "E02_Soil_Engineering";
        private const string E03 = "E03_Systems_Thinking";

        // Domain F — Geography & Environment
        private const string F01 = "F01_Sustainability_Literacy";
        private const string F02 = "F02_Climate_Science";

        // Domain G — Health & Nutrition
        private const string G01 = "G01_Nutrition";
        private const string G02 = "G02_Food_Safety";

        // Domain H — Social & Emotional
        private const string H01 = "H01_Empathy";
        private const string H02 = "H02_Reflection";
        private const string H03 = "H03_Resilience";

        // ── Event → Pillar mapping table ────────────────────────────────

        private static readonly Dictionary<string, string[]> EventToPillars =
            new Dictionary<string, string[]>
            {
                // Rabbit care → FoodScience, AnimalWelfare, Biology
                { RabbitTelemetryEvents.RabbitFed, new[] { B03, C01, B01 } },
                { RabbitTelemetryEvents.RabbitWatered, new[] { B04, C01 } },
                { RabbitTelemetryEvents.RabbitHealthCheck, new[] { B01, C01, G01 } },
                { RabbitTelemetryEvents.RabbitNeglected, new[] { C01, H01 } },
                { RabbitTelemetryEvents.RabbitHarvested, new[] { C01, B03, G01, D03 } },

                // Crops → PlantBiology, SoilEngineering, WaterScience
                { CropTelemetryEvents.CropPlanted, new[] { B02, E02 } },
                { CropTelemetryEvents.CropWatered, new[] { B04, E01 } },
                { CropTelemetryEvents.CropHarvested, new[] { B02, D03, G01 } },
                { CropTelemetryEvents.CropComposted, new[] { D02, E02, B05 } },
                { CropTelemetryEvents.WasteDetected, new[] { C03, D02 } },

                // Harvest chain → CircularEconomy, FinancialLiteracy
                { HarvestTelemetryEvents.HarvestProcessed, new[] { D02, D01, D03 } },
                { HarvestTelemetryEvents.PayItForwardEligible, new[] { C04, D01, H01 } },

                // Healing → Ecology, Sustainability
                { HealingTelemetryEvents.HealingCompleted, new[] { B05, F01, E02 } },

                // Reflection → Reflection, WrittenExpression
                { ReflectionTelemetryEvents.ReflectionSubmitted, new[] { H02, A02 } },

                // Milestones → Resilience, SystemsThinking
                { MilestoneTelemetryEvents.MilestoneReached, new[] { H03, E03 } },

                // Blessing → Ethics, Stewardship
                { "BLESSING_DELTA_REQUEST", new[] { C02, C03 } },

                // Veil → Sustainability, Climate
                { VeilTelemetryEvents.VeilAccessed, new[] { F01, F02, B05 } },
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
            if (bus == null) return;

            bus.Subscribe<TelemetryRequestEvent>(HandleTelemetryEvent);
            bus.Subscribe<CropHarvestedEvent>(HandleCropHarvested);
            bus.Subscribe<RabbitCareEvent>(HandleRabbitCare);
            bus.Subscribe<MilestoneUnlockedEvent>(HandleMilestone);
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Unsubscribe<TelemetryRequestEvent>(HandleTelemetryEvent);
            bus.Unsubscribe<CropHarvestedEvent>(HandleCropHarvested);
            bus.Unsubscribe<RabbitCareEvent>(HandleRabbitCare);
            bus.Unsubscribe<MilestoneUnlockedEvent>(HandleMilestone);
        }

        // ── Event handlers ──────────────────────────────────────────────

        private void HandleTelemetryEvent(TelemetryRequestEvent evt)
        {
            MapAndRecord(evt.EventType, 1f);
        }

        private void HandleCropHarvested(CropHarvestedEvent evt)
        {
            // Engagement depth (player made a choice about harvest mode)
            float delta = evt.Mode == HarvestMode.Compost ? 2f : 1f;
            MapAndRecord(CropTelemetryEvents.CropHarvested, delta);
        }

        private void HandleRabbitCare(RabbitCareEvent evt)
        {
            MapAndRecord(evt.EventType, evt.CareValue > 0 ? 1f : 0.5f);
        }

        private void HandleMilestone(MilestoneUnlockedEvent evt)
        {
            MapAndRecord(MilestoneTelemetryEvents.MilestoneReached, 2f);
        }

        // ── Core mapping logic ──────────────────────────────────────────

        private void MapAndRecord(string eventType, float competencyDelta)
        {
            if (string.IsNullOrEmpty(eventType)) return;
            if (!EventToPillars.TryGetValue(eventType, out string[] pillars)) return;

            CurriculumEngine engine = CurriculumEngine.Instance;
            if (engine == null) return;

            foreach (string pillarId in pillars)
            {
                engine.RecordActivity(pillarId, competencyDelta);
            }
        }
    }
}
