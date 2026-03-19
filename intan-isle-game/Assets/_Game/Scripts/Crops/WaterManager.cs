using System;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>Water source types for the crop system.</summary>
    public enum WaterSource
    {
        Well,
        Rainwater,
        AWFUnit
    }

    /// <summary>
    /// Singleton that manages the daily water budget. Replenishes at dawn.
    /// Well is always available. Rainwater is seasonal. AWF Unit doubles
    /// the budget and is unlocked at the Cultivator milestone.
    /// </summary>
    public class WaterManager : MonoBehaviour
    {
        private const float BaseWellBudget = 6f;
        private const float RainwaterBonus = 3f;
        private const float AWFMultiplier = 2f;

        /// <summary>Singleton instance.</summary>
        public static WaterManager Instance { get; private set; }

        [Header("Budget")]
        [SerializeField] private float baseWellBudget = BaseWellBudget;
        [SerializeField] private float rainwaterBonus = RainwaterBonus;

        /// <summary>Water units remaining for today.</summary>
        public float DailyBudgetRemaining { get; private set; }

        /// <summary>Total budget at dawn (well + rain + AWF).</summary>
        public float DailyBudgetTotal { get; private set; }

        /// <summary>Whether the AWF Unit is unlocked (Cultivator milestone).</summary>
        public bool AWFUnlocked { get; private set; }

        /// <summary>Whether it is currently raining (seasonal).</summary>
        public bool IsRaining { get; set; }

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

            RefreshBudget();
        }

        private void OnEnable()
        {
            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnTimeOfDayChanged += HandleTimeOfDayChanged;

            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Subscribe<MilestoneUnlockedEvent>(HandleMilestoneUnlocked);
        }

        private void OnDisable()
        {
            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnTimeOfDayChanged -= HandleTimeOfDayChanged;

            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Unsubscribe<MilestoneUnlockedEvent>(HandleMilestoneUnlocked);
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Consumes water from the daily budget. Returns the amount
        /// actually consumed (may be less than requested if budget low).
        /// </summary>
        /// <param name="amount">Requested water units.</param>
        /// <param name="source">Water source to draw from.</param>
        /// <returns>Actual amount consumed.</returns>
        public float ConsumeWater(float amount, WaterSource source)
        {
            if (source == WaterSource.AWFUnit && !AWFUnlocked)
                return 0f;

            float consumed = Mathf.Min(amount, DailyBudgetRemaining);
            DailyBudgetRemaining -= consumed;
            return consumed;
        }

        /// <summary>
        /// Manually unlocks the AWF Unit (called by MilestoneManager or test code).
        /// </summary>
        public void UnlockAWF()
        {
            AWFUnlocked = true;
            RefreshBudget();
            Debug.Log("[WaterManager] AWF Unit unlocked — budget doubled.");
        }

        // ── Budget refresh ──────────────────────────────────────────────

        private void RefreshBudget()
        {
            float budget = baseWellBudget;

            if (IsRaining)
                budget += rainwaterBonus;

            if (AWFUnlocked)
                budget *= AWFMultiplier;

            DailyBudgetTotal = budget;
            DailyBudgetRemaining = budget;
        }

        private void HandleTimeOfDayChanged(TimeOfDay previous, TimeOfDay current)
        {
            if (current == TimeOfDay.Dawn)
                RefreshBudget();
        }

        private void HandleMilestoneUnlocked(MilestoneUnlockedEvent evt)
        {
            if (evt.MilestoneId == "STEWARD_AWF_UNIT")
                UnlockAWF();
        }
    }
}
