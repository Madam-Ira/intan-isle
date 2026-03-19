using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>Published when the Captainess appears in the distance.</summary>
    public struct CaptainessAppearanceEvent
    {
        public string Context;
        public int SightingNumber;
    }

    /// <summary>
    /// Manages the Captainess mystery narrative. She appears and disappears
    /// — never reachable, never explained. A veiled woman in flowing white.
    /// Her presence anchors curiosity across arcs.
    ///
    /// First sighting: triggered when the player enters the Hidden Realm for
    /// the first time. Distant, unexplained. No dialogue. No interaction.
    ///
    /// Subsequent sightings: triggered at specific Blessing milestones and
    /// ecological restoration thresholds. Each sighting is briefer than the
    /// last. She is always watching — never approaching.
    ///
    /// PERSONA RIGHTS: The Captainess persona is protected. Do not use real
    /// names. Always "The Captainess" or "The Arcane Figure" in code/UI.
    /// </summary>
    public class CaptainessEventManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static CaptainessEventManager Instance { get; private set; }

        [Header("Sighting Conditions")]
        [SerializeField] private float firstSightingDelaySec = 5f;
        [SerializeField] private float sightingDurationSec = 3f;

        private int _sightingCount;
        private bool _firstVeilEntryHandled;
        private bool _sightingActive;

        private const string PrefsKey = "CaptainessSightings";

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _sightingCount = PlayerPrefs.GetInt(PrefsKey, 0);
        }

        private void OnEnable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
                veil.OnVeiledWorldEntered += HandleVeilEntered;

            EventBus bus = EventBus.Instance;
            if (bus != null)
            {
                bus.Subscribe<MilestoneUnlockedEvent>(HandleMilestone);
                bus.Subscribe<ZoneHealedEvent>(HandleZoneHealed);
            }
        }

        private void OnDisable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
                veil.OnVeiledWorldEntered -= HandleVeilEntered;

            EventBus bus = EventBus.Instance;
            if (bus != null)
            {
                bus.Unsubscribe<MilestoneUnlockedEvent>(HandleMilestone);
                bus.Unsubscribe<ZoneHealedEvent>(HandleZoneHealed);
            }
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>Total number of Captainess sightings across all sessions.</summary>
        public int SightingCount => _sightingCount;

        /// <summary>Whether a sighting is currently active.</summary>
        public bool IsSightingActive => _sightingActive;

        // ── Trigger conditions ──────────────────────────────────────────

        private void HandleVeilEntered()
        {
            if (_firstVeilEntryHandled) return;
            _firstVeilEntryHandled = true;

            // First sighting: delayed appearance after entering Hidden Realm
            if (_sightingCount == 0)
                Invoke(nameof(TriggerFirstSighting), firstSightingDelaySec);
        }

        private void HandleMilestone(MilestoneUnlockedEvent evt)
        {
            // Appear at Guardian-tier milestones
            if (evt.Level == BlessingLevel.Guardian || evt.Level == BlessingLevel.Steward)
                TriggerSighting($"milestone_{evt.MilestoneId}");
        }

        private void HandleZoneHealed(ZoneHealedEvent evt)
        {
            // Appear when a zone is fully healed — she witnessed your work
            TriggerSighting($"zone_healed_{evt.ZoneId}");
        }

        // ── Sighting logic ──────────────────────────────────────────────

        private void TriggerFirstSighting()
        {
            TriggerSighting("first_veil_entry");
        }

        private void TriggerSighting(string context)
        {
            if (_sightingActive) return;

            _sightingCount++;
            _sightingActive = true;

            PlayerPrefs.SetInt(PrefsKey, _sightingCount);
            PlayerPrefs.Save();

            EventBus bus = EventBus.Instance;
            if (bus != null)
            {
                bus.Publish(new CaptainessAppearanceEvent
                {
                    Context = context,
                    SightingNumber = _sightingCount
                });
            }

            Debug.Log($"[Captainess] Sighting #{_sightingCount} — {context}. " +
                      "She stands in the distance. Silent. Watching. Then she is gone.");

            // Auto-dismiss after duration (each sighting briefer)
            float duration = sightingDurationSec / Mathf.Max(_sightingCount, 1);
            Invoke(nameof(EndSighting), duration);
        }

        private void EndSighting()
        {
            _sightingActive = false;
        }
    }
}
