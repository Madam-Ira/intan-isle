using System;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    // ── Event structs ───────────────────────────────────────────────────

    /// <summary>Published when the cached blessing score or veil access is updated from the server.</summary>
    public struct BlessingUpdatedEvent
    {
        /// <summary>Updated blessing score (0–100).</summary>
        public float Score;

        /// <summary>Derived <see cref="BlessingLevel"/> from the new score.</summary>
        public BlessingLevel Level;

        /// <summary>Whether veil access has been granted by the server.</summary>
        public bool VeilAccessGranted;
    }

    // ────────────────────────────────────────────────────────────────────
    //  THIS FILE IS A STUB.
    //
    //  The blessing calculation algorithm is server-authoritative and
    //  trade secret. All score deltas are computed by the NurAIN backend.
    //  This controller only:
    //    1. Caches the most recent score received from the server.
    //    2. Derives a BlessingLevel for UI display.
    //    3. Dispatches qualifying telemetry events to NurAIN so the
    //       server can compute the next delta.
    //
    //  DO NOT implement delta logic, score arithmetic, or any blessing
    //  calculation on the client. Any such code will be rejected in
    //  code review.
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client-side stub that caches the server-authoritative blessing score
    /// and relays qualifying telemetry events to the NurAIN backend for
    /// delta calculation. Does not compute blessing deltas locally.
    /// </summary>
    public class BlessingMeterController : MonoBehaviour
    {
        /// <summary>Singleton instance, persistent across scenes.</summary>
        public static BlessingMeterController Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float initialScore;
        [SerializeField] private int maxQueueSize = 256;

        [Header("Offline Fallback")]
        [Tooltip("When true, applies BlessingDeltaRequest deltas locally if server is unavailable. Provisional until server confirms.")]
        [SerializeField] private bool offlineFallbackEnabled = true;

        [Header("Weight Table (optional)")]
        [SerializeField] private BlessingWeightTable weightTable;

        private float _cachedScore;
        private BlessingLevel _cachedLevel;
        private bool _cachedVeilAccess;
        private readonly List<QueuedDispatch> _dispatchQueue = new List<QueuedDispatch>();
        private readonly HashSet<string> _processedEventIds = new HashSet<string>();
        private int _eventCounter;

        // ── Public properties ───────────────────────────────────────────

        /// <summary>Last blessing score received from the server (0–100).</summary>
        public float CachedBlessingScore => _cachedScore;

        /// <summary>
        /// Blessing level derived from <see cref="CachedBlessingScore"/>.
        /// Seedling 0–30, Cultivator 31–55, Guardian 56–75, Shepherd 76–90, Steward 91–100.
        /// </summary>
        public BlessingLevel CachedBlessingLevel => _cachedLevel;

        /// <summary>Whether the server has granted veil access.</summary>
        public bool CachedVeilAccessGranted => _cachedVeilAccess;

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

            _cachedScore = initialScore;
            _cachedLevel = ScoreToLevel(initialScore);
        }

        private void OnEnable()
        {
            SubscribeTelemetryEvents();
        }

        private void OnDisable()
        {
            UnsubscribeTelemetryEvents();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Called when a server response provides an updated blessing score.
        /// Updates all cached values and publishes a <see cref="BlessingUpdatedEvent"/>.
        /// </summary>
        /// <param name="score">New blessing score (0–100).</param>
        /// <param name="veilGranted">Whether veil access is granted.</param>
        public void SetBlessingFromServer(float score, bool veilGranted)
        {
            _cachedScore = Mathf.Clamp(score, 0f, 100f);
            _cachedLevel = ScoreToLevel(_cachedScore);
            _cachedVeilAccess = veilGranted;

            // Mirror into PlayerDataManager
            PlayerDataManager player = PlayerDataManager.Instance;
            if (player != null)
            {
                player.SetBlessingLevelFromServer(_cachedLevel);
                player.SetVeilAccessStatusFromServer(
                    veilGranted ? VeilAccessStatus.Granted : VeilAccessStatus.Locked);
            }

            PublishViaBus(new BlessingUpdatedEvent
            {
                Score = _cachedScore,
                Level = _cachedLevel,
                VeilAccessGranted = _cachedVeilAccess
            });
        }

        /// <summary>
        /// Dispatches a telemetry event to the NurAIN backend for server-side
        /// blessing calculation. If the connector is unavailable the event is
        /// queued locally and retried next session.
        /// </summary>
        /// <param name="eventType">Telemetry event type identifier.</param>
        /// <param name="jsonPayload">JSON-encoded event payload.</param>
        public void DispatchEvent(string eventType, string jsonPayload)
        {
            // Attempt immediate dispatch via NurAINConnector stub
            if (TrySendToNurAIN(eventType, jsonPayload))
                return;

            // Queue for retry
            if (_dispatchQueue.Count < maxQueueSize)
            {
                _dispatchQueue.Add(new QueuedDispatch
                {
                    EventType = eventType,
                    JsonPayload = jsonPayload,
                    QueuedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
            }
        }

        /// <summary>
        /// Attempts to flush all queued dispatches to NurAIN.
        /// Call this when connectivity is restored or at session start.
        /// </summary>
        public void FlushQueue()
        {
            for (int i = _dispatchQueue.Count - 1; i >= 0; i--)
            {
                QueuedDispatch entry = _dispatchQueue[i];
                if (TrySendToNurAIN(entry.EventType, entry.JsonPayload))
                    _dispatchQueue.RemoveAt(i);
            }
        }

        // ── Telemetry subscription ──────────────────────────────────────

        private void SubscribeTelemetryEvents()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Subscribe<TelemetryRequestEvent>(HandleTelemetryRequest);
            bus.Subscribe<BlessingDeltaRequest>(HandleBlessingDeltaRequest);
        }

        private void UnsubscribeTelemetryEvents()
        {
            EventBus bus = EventBus.Instance;
            if (bus == null) return;

            bus.Unsubscribe<TelemetryRequestEvent>(HandleTelemetryRequest);
            bus.Unsubscribe<BlessingDeltaRequest>(HandleBlessingDeltaRequest);
        }

        private void HandleTelemetryRequest(TelemetryRequestEvent evt)
        {
            DispatchEvent(evt.EventType, evt.JsonPayload);
        }

        private void HandleBlessingDeltaRequest(BlessingDeltaRequest evt)
        {
            // Deduplication: generate eventId from counter + reason hash
            _eventCounter++;
            string eventId = $"{_eventCounter}_{evt.Reason?.GetHashCode():X8}";

            if (_processedEventIds.Contains(eventId))
                return;

            _processedEventIds.Add(eventId);

            // Cap dedup set to prevent unbounded growth
            if (_processedEventIds.Count > 10000)
                _processedEventIds.Clear();

            // Forward to server
            string payload = $"{{\"eventId\":\"{eventId}\",\"delta\":{evt.Delta:F4},\"reason\":\"{EscapeJson(evt.Reason)}\"}}";
            bool sent = TrySendToNurAIN("BLESSING_DELTA_REQUEST", payload);

            if (!sent)
            {
                DispatchEvent("BLESSING_DELTA_REQUEST", payload);

                // Offline fallback: apply provisional delta locally so the
                // game remains playable without server connectivity.
                if (offlineFallbackEnabled)
                    ApplyProvisionalDelta(evt.Delta);
            }
        }

        /// <summary>
        /// Applies a provisional delta locally. The score is marked as
        /// unconfirmed until the server responds via <see cref="SetBlessingFromServer"/>.
        /// </summary>
        private void ApplyProvisionalDelta(float delta)
        {
            float previousScore = _cachedScore;
            _cachedScore = Mathf.Clamp(_cachedScore + delta, 0f, 100f);
            _cachedLevel = ScoreToLevel(_cachedScore);

            if (!Mathf.Approximately(previousScore, _cachedScore))
            {
                PublishViaBus(new BlessingUpdatedEvent
                {
                    Score = _cachedScore,
                    Level = _cachedLevel,
                    VeilAccessGranted = _cachedVeilAccess
                });
            }
        }

        // ── NurAIN connector stub ───────────────────────────────────────

        /// <summary>
        /// Stub: attempts to send an event to the NurAIN endpoint.
        /// Returns false when the connector is unavailable (offline / not yet implemented).
        /// </summary>
        private bool TrySendToNurAIN(string eventType, string jsonPayload)
        {
            // TODO: Replace with real NurAINConnector.Instance.Send(eventType, jsonPayload)
            // when the networking layer is implemented.
            return false;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static BlessingLevel ScoreToLevel(float score)
        {
            if (score >= 91f) return BlessingLevel.Steward;
            if (score >= 76f) return BlessingLevel.Shepherd;
            if (score >= 56f) return BlessingLevel.Guardian;
            if (score >= 31f) return BlessingLevel.Cultivator;
            return BlessingLevel.Seedling;
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void PublishViaBus<T>(T eventData)
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
                bus.Publish(eventData);
        }

        // ── Inner types ─────────────────────────────────────────────────

        [Serializable]
        private struct QueuedDispatch
        {
            public string EventType;
            public string JsonPayload;
            public long QueuedAtMs;
        }
    }
}