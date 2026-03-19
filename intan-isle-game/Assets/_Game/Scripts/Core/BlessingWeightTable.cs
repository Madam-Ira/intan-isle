using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>
    /// A single blessing event weight entry. Used by the offline fallback
    /// and for designer tuning. The canonical calculation is server-side.
    /// </summary>
    [Serializable]
    public class BlessingWeightEntry
    {
        public string eventType;
        public float baseWeight;
        [TextArea(1, 2)]
        public string description;
    }

    /// <summary>
    /// ScriptableObject containing all Blessing Meter event weights.
    /// Server-authoritative in production — this table drives the offline
    /// fallback and provides UI feedback context.
    /// Create via: Create > Intan Isle > Blessing > Weight Table
    /// </summary>
    [CreateAssetMenu(fileName = "BlessingWeightTable", menuName = "Intan Isle/Blessing/Weight Table")]
    public class BlessingWeightTable : ScriptableObject
    {
        [SerializeField] private List<BlessingWeightEntry> weights = new List<BlessingWeightEntry>();

        private Dictionary<string, float> _lookup;

        /// <summary>Read-only access to all weight entries.</summary>
        public IReadOnlyList<BlessingWeightEntry> Weights => weights;

        /// <summary>
        /// Returns the base weight for an event type, or 0 if not defined.
        /// </summary>
        public float GetWeight(string eventType)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(eventType, out float w) ? w : 0f;
        }

        /// <summary>
        /// Returns true if the event type has a defined weight.
        /// </summary>
        public bool HasWeight(string eventType)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.ContainsKey(eventType);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, float>();
            foreach (BlessingWeightEntry entry in weights)
            {
                if (!string.IsNullOrEmpty(entry.eventType))
                    _lookup[entry.eventType] = entry.baseWeight;
            }
        }

        private void OnValidate()
        {
            _lookup = null; // rebuild on next access after Inspector change
        }

        /// <summary>
        /// Populates default weights matching the spec. Called from editor script.
        /// </summary>
        public void PopulateDefaults()
        {
            weights.Clear();

            // ── RISES (+) ────────────────────────────────────────────────
            weights.Add(new BlessingWeightEntry
                { eventType = "RABBIT_FED_ON_SCHEDULE", baseWeight = 5f,
                  description = "Rabbit fed/watered on schedule" });
            weights.Add(new BlessingWeightEntry
                { eventType = "RABBIT_WATERED_ON_SCHEDULE", baseWeight = 5f,
                  description = "Rabbit watered on schedule" });
            weights.Add(new BlessingWeightEntry
                { eventType = "CROPS_TENDED_NO_WASTE", baseWeight = 4f,
                  description = "Crops tended without waste" });
            weights.Add(new BlessingWeightEntry
                { eventType = "PAY_IT_FORWARD", baseWeight = 8f,
                  description = "Surplus shared via Pay-It-Forward" });
            weights.Add(new BlessingWeightEntry
                { eventType = "EDUCATION_CHALLENGE_COMPLETE", baseWeight = 6f,
                  description = "Educational challenge completed" });
            weights.Add(new BlessingWeightEntry
                { eventType = "REFLECTION_JOURNAL_SUBMITTED", baseWeight = 5f,
                  description = "Reflection journal submitted" });
            weights.Add(new BlessingWeightEntry
                { eventType = "COMMUNITY_HELPED", baseWeight = 4f,
                  description = "Community helped" });
            weights.Add(new BlessingWeightEntry
                { eventType = "HEALING_ACTION_COMPLETED", baseWeight = 7f,
                  description = "Healing action completed" });
            weights.Add(new BlessingWeightEntry
                { eventType = "HARVEST_WITH_GRATITUDE", baseWeight = 6f,
                  description = "Harvest with gratitude" });
            weights.Add(new BlessingWeightEntry
                { eventType = "RABBIT_HEALTH_CHECK", baseWeight = 5f,
                  description = "Proactive health check" });

            // ── FALLS (-) ────────────────────────────────────────────────
            weights.Add(new BlessingWeightEntry
                { eventType = "ANIMAL_CARE_NEGLECTED", baseWeight = -4f,
                  description = "Animal care neglected (unresponded)" });
            weights.Add(new BlessingWeightEntry
                { eventType = "RESOURCES_WASTED", baseWeight = -3f,
                  description = "Resources wasted" });
            weights.Add(new BlessingWeightEntry
                { eventType = "ETHICAL_DILEMMA_CARELESS", baseWeight = -5f,
                  description = "Ethical dilemma answered carelessly" });
            weights.Add(new BlessingWeightEntry
                { eventType = "REFLECTION_SKIPPED", baseWeight = -2f,
                  description = "Reflection skipped repeatedly" });
            weights.Add(new BlessingWeightEntry
                { eventType = "OVER_HARVEST_NO_COMPOST", baseWeight = -3f,
                  description = "Over-harvest without compost" });
            weights.Add(new BlessingWeightEntry
                { eventType = "SHORTCUT_DETECTED", baseWeight = -6f,
                  description = "Shortcut used (detected pattern)" });
            weights.Add(new BlessingWeightEntry
                { eventType = "RABBIT_DECEASED_NEGLECT", baseWeight = -6f,
                  description = "Rabbit died from neglect" });
            weights.Add(new BlessingWeightEntry
                { eventType = "HARVEST_WITHOUT_CARE", baseWeight = -3f,
                  description = "Harvest performed without care" });

            BuildLookup();
        }
    }
}
