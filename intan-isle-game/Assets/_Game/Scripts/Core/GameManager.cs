using System;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>Published once after all managers are bootstrapped and ready.</summary>
    public struct GameReadyEvent { }

    /// <summary>
    /// Top-level singleton that bootstraps all system managers in a
    /// deterministic order on Awake. Handles graceful degradation — if any
    /// manager fails to initialise, a warning is logged and the remaining
    /// managers continue. Fires <see cref="GameReadyEvent"/> via
    /// <see cref="EventBus"/> once all managers are confirmed present.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static GameManager Instance { get; private set; }

        // ── Scene references (assigned by Bootstrap editor script) ──────

        [Header("Scene References")]
        [SerializeField] private ZoneShaderLinker zoneShaderLinkerRef;
        [SerializeField] private EmotionalDayNightCycle emotionalDayNightCycleRef;
        [SerializeField] private BlessingMeterController blessingMeterControllerRef;
        [SerializeField] private VeiledWorldManager veiledWorldManagerRef;

        // ── Public properties ───────────────────────────────────────────

        /// <summary>
        /// True after all managers have been confirmed present (either
        /// found in scene or auto-created).
        /// </summary>
        public bool IsInitialised { get; private set; }

        /// <summary>Scene reference to the zone shader linker.</summary>
        public ZoneShaderLinker ZoneShaderLinkerRef => zoneShaderLinkerRef;

        /// <summary>Scene reference to the emotional day/night cycle.</summary>
        public EmotionalDayNightCycle EmotionalDayNightCycleRef => emotionalDayNightCycleRef;

        /// <summary>Scene reference to the blessing meter controller.</summary>
        public BlessingMeterController BlessingMeterControllerRef => blessingMeterControllerRef;

        /// <summary>Scene reference to the veiled world manager.</summary>
        public VeiledWorldManager VeiledWorldManagerRef => veiledWorldManagerRef;

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

            BootstrapManagers();
        }

        private void Start()
        {
            IsInitialised = true;

            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(new GameReadyEvent());

            Debug.Log("[GameManager] All managers initialised. GameReadyEvent published.");

            StartNewSession();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PlayerDataManager player = PlayerDataManager.Instance;
                if (player != null)
                    player.SaveLocal();
            }
        }

        private void OnApplicationQuit()
        {
            EndSession();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Called on game launch. Flushes any queued data from prior
        /// sessions and fires <c>SESSION_STARTED</c> telemetry.
        /// </summary>
        public void StartNewSession()
        {
            BlessingMeterController blessing = BlessingMeterController.Instance;
            if (blessing != null)
                blessing.FlushQueue();

            NurAINConnector connector = NurAINConnector.Instance;
            if (connector != null)
                connector.FlushQueue();

            TelemetryManager telemetry = TelemetryManager.Instance;
            if (telemetry != null)
            {
                telemetry.FlushImmediate();
                telemetry.RecordEvent(SessionTelemetryEvents.SessionStarted, "{}");
            }
        }

        /// <summary>
        /// Called on quit. Saves player data, flushes telemetry, and fires
        /// <c>SESSION_ENDED</c> telemetry.
        /// </summary>
        public void EndSession()
        {
            TelemetryManager telemetry = TelemetryManager.Instance;
            if (telemetry != null)
                telemetry.RecordEvent(SessionTelemetryEvents.SessionEnded, "{}");

            PlayerDataManager player = PlayerDataManager.Instance;
            if (player != null)
                player.SaveLocal();

            if (telemetry != null)
                telemetry.FlushImmediate();
        }

        // ── Bootstrap ───────────────────────────────────────────────────

        private void BootstrapManagers()
        {
            // Exact initialisation order — dependencies flow downward.
            // Phase 1: Core infrastructure
            EnsureManager<PlayerDataManager>();
            EnsureManager<EventBus>();

            // Phase 2: NGE adaptive system
            EnsureManager<LearnerProfileTracker>();
            EnsureManager<NurAINRulesEngine>();
            EnsureManager<IEPGoalManager>();
            EnsureManager<AdaptiveParameterController>();
            EnsureManager<OnboardingQuizManager>();

            // Phase 3: Curriculum, blessing, milestones, dashboard
            EnsureManager<CurriculumEngine>();
            EnsureManager<CurriculumEventMapper>();
            EnsureManager<BlessingMeterController>();
            EnsureManager<MilestoneManager>();
            EnsureManager<DashboardDataBridge>();

            // Phase 4: Gameplay systems
            EnsureManager<RabbitCareManager>();
            EnsureManager<WaterManager>();
            EnsureManager<CropSystem>();
            EnsureManager<HarvestChainManager>();

            // Phase 5: Scene-based world managers (must already exist)
            EnsureSceneManager<VeiledWorldManager>();
            EnsureSceneManager<ZoneShaderLinker>();
            EnsureSceneManager<EmotionalDayNightCycle>();
        }

        /// <summary>
        /// Finds an existing manager of type <typeparamref name="T"/> in
        /// the scene, or auto-creates a new GameObject with the component.
        /// Logs a warning and continues if initialisation fails.
        /// </summary>
        private void EnsureManager<T>() where T : MonoBehaviour
        {
            string typeName = typeof(T).Name;

            try
            {
                if (FindObjectOfType<T>() != null)
                    return;

                GameObject go = new GameObject(typeName);
                go.AddComponent<T>();
                Debug.Log($"[GameManager] {typeName} auto-created.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameManager] {typeName} failed to initialise: {e.Message}");
            }
        }

        /// <summary>
        /// Verifies a scene-based manager exists. Does NOT auto-create
        /// because scene managers typically have serialised references
        /// that cannot be set at runtime.
        /// </summary>
        private void EnsureSceneManager<T>() where T : MonoBehaviour
        {
            string typeName = typeof(T).Name;

            if (FindObjectOfType<T>() == null)
                Debug.LogWarning($"[GameManager] {typeName} not found in scene — add it via Bootstrap.");
        }

        // ── Telemetry constants ─────────────────────────────────────────

        private static class SessionTelemetryEvents
        {
            public const string SessionStarted = "SESSION_STARTED";
            public const string SessionEnded = "SESSION_ENDED";
        }
    }
}
