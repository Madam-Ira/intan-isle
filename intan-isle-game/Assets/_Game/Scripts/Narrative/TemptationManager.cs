using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>The Four Temptations from the spec — antagonist archetypes.</summary>
    public enum TemptationType
    {
        /// <summary>Quick reward — over-harvest now. Costs: soil depletion, rabbit stress, Blessing fall.</summary>
        Sugar,
        /// <summary>Make things beautiful first. Costs: neglected animals, failed crops.</summary>
        Prism,
        /// <summary>Automate everything — why care manually? Costs: loss of understanding.</summary>
        GigaByte,
        /// <summary>Others are doing better — cut corners. Costs: broken trust, Blessing collapse.</summary>
        Whisper
    }

    /// <summary>Published when a temptation is offered to the player.</summary>
    public struct TemptationOfferedEvent
    {
        public TemptationType Type;
        public string Offer;
        public string Consequence;
    }

    /// <summary>Published when the player responds to a temptation.</summary>
    public struct TemptationRespondedEvent
    {
        public TemptationType Type;
        public bool Resisted;
    }

    /// <summary>
    /// Manages the Four Temptations antagonist system. Temptations are
    /// contextual offers that appear during gameplay — shortcuts that
    /// trade ethical behaviour for short-term gain.
    ///
    /// Day 1 scope: Sugar only (over-harvest prompt).
    ///
    /// The Temptation is never a blocking pop-up. It is a gentle offer
    /// that the player can accept or ignore. Consequences are shown
    /// through gameplay, not punishment text.
    /// </summary>
    public class TemptationManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static TemptationManager Instance { get; private set; }

        [Header("Tuning")]
        [SerializeField] private float sugarCooldownMinutes = 10f;
        [SerializeField] private float sugarBlessingPenalty = -6f;

        private float _lastSugarOfferTime = -999f;
        private int _sugarAcceptCount;
        private int _sugarResistCount;

        /// <summary>Number of times the player accepted Sugar's offer.</summary>
        public int SugarAcceptCount => _sugarAcceptCount;

        /// <summary>Number of times the player resisted Sugar.</summary>
        public int SugarResistCount => _sugarResistCount;

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
            {
                bus.Subscribe<CropHarvestedEvent>(HandleCropHarvested);
                bus.Subscribe<WasteDetectedEvent>(HandleWasteDetected);
            }
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
            {
                bus.Unsubscribe<CropHarvestedEvent>(HandleCropHarvested);
                bus.Unsubscribe<WasteDetectedEvent>(HandleWasteDetected);
            }
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Called when the player accepts a temptation's offer. Applies the
        /// consequences: Blessing penalty, shortcut detection, curriculum tag.
        /// </summary>
        public void AcceptTemptation(TemptationType type)
        {
            switch (type)
            {
                case TemptationType.Sugar:
                    _sugarAcceptCount++;
                    ApplySugarConsequences();
                    break;
            }

            PublishViaBus(new TemptationRespondedEvent
            {
                Type = type,
                Resisted = false
            });

            PublishTelemetry("TEMPTATION_ACCEPTED",
                $"{{\"type\":\"{type}\",\"acceptCount\":{_sugarAcceptCount}}}");
        }

        /// <summary>
        /// Called when the player resists a temptation. Awards a small
        /// Blessing bonus and tags the Resilience curriculum pillar.
        /// </summary>
        public void ResistTemptation(TemptationType type)
        {
            switch (type)
            {
                case TemptationType.Sugar:
                    _sugarResistCount++;
                    break;
            }

            PublishViaBus(new TemptationRespondedEvent
            {
                Type = type,
                Resisted = true
            });

            // Resisting temptation is an ethical choice = engagement depth 2
            CurriculumEngine engine = CurriculumEngine.Instance;
            if (engine != null)
            {
                engine.RecordActivity("C02_Environmental_Ethics", 2f);
                engine.RecordActivity("H03_Resilience", 2f);
            }

            PublishTelemetry("TEMPTATION_RESISTED",
                $"{{\"type\":\"{type}\",\"resistCount\":{_sugarResistCount}}}");
        }

        // ── Event handlers ──────────────────────────────────────────────

        private void HandleCropHarvested(CropHarvestedEvent evt)
        {
            // Sugar temptation: after a Full harvest, offer to harvest again immediately
            if (evt.Mode != HarvestMode.Full) return;
            if (Time.realtimeSinceStartup - _lastSugarOfferTime < sugarCooldownMinutes * 60f) return;

            _lastSugarOfferTime = Time.realtimeSinceStartup;
            OfferSugar();
        }

        private void HandleWasteDetected(WasteDetectedEvent evt)
        {
            // Sugar thrives on waste patterns — offer again
            if (Time.realtimeSinceStartup - _lastSugarOfferTime < sugarCooldownMinutes * 60f) return;

            _lastSugarOfferTime = Time.realtimeSinceStartup;
            OfferSugar();
        }

        // ── Sugar temptation ────────────────────────────────────────────

        private void OfferSugar()
        {
            PublishViaBus(new TemptationOfferedEvent
            {
                Type = TemptationType.Sugar,
                Offer = "The soil still has life in it. You could harvest again right now — double the yield, double the food.",
                Consequence = "Soil depletion. Rabbit stress from rushed feed. The land remembers."
            });

            // Elder warns (not blocks)
            PublishViaBus(new EducationalPromptEvent
            {
                Topic = "Temptation",
                Message = "Someone is offering you a shortcut. What do they gain from your haste?",
                RabbitId = string.Empty
            });

            Debug.Log("[Temptation] Sugar offered — over-harvest shortcut.");
        }

        private void ApplySugarConsequences()
        {
            // Blessing penalty
            PublishViaBus(new BlessingDeltaRequest
            {
                Delta = sugarBlessingPenalty,
                Reason = "Accepted Sugar's temptation — shortcut used"
            });

            // Soil damage on all tiles
            CropSystem crops = CropSystem.Instance;
            if (crops != null)
            {
                for (int r = 0; r < crops.GridRows; r++)
                {
                    for (int c = 0; c < crops.GridCols; c++)
                    {
                        CropTile tile = crops.GetTile(r, c);
                        if (tile != null)
                            tile.soilHealth = Mathf.Clamp01(tile.soilHealth - 0.15f);
                    }
                }
            }

            // Rabbit stress: reduce healthScore on all rabbits
            RabbitCareManager rabbits = RabbitCareManager.Instance;
            if (rabbits != null)
            {
                foreach (string id in rabbits.GetAllRabbitIds())
                {
                    RabbitData data = rabbits.GetRabbitData(id);
                    if (data != null)
                        data.healthScore = Mathf.Clamp01(data.healthScore - 0.10f);
                }
            }

            Debug.Log("[Temptation] Sugar accepted — soil depleted, rabbits stressed, Blessing -6.");
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
