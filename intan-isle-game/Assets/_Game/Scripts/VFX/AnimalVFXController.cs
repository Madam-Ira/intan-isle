using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>
    /// Per-rabbit visual controller implementing the 4 Animal VFX systems
    /// from the Bio-VFX spec:
    ///   1) Emotional Bond — body shimmer/glow based on bondScore
    ///   2) Nutritional State — color shift green (full) → red (hungry)
    ///   3) Injury + Healing — wound flicker → healing wave after care
    ///   4) Sleep + Wake — breathing animation tied to day/night
    /// Subscribes to <see cref="RabbitHealthStateChangedEvent"/> via EventBus.
    /// </summary>
    public class AnimalVFXController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Identity")]
        [SerializeField] private string rabbitId;

        [Header("Particle Systems")]
        [SerializeField] private ParticleSystem thriveParticles;
        [SerializeField] private ParticleSystem distressParticles;
        [SerializeField] private ParticleSystem healingParticles;

        [Header("Mesh")]
        [SerializeField] private Renderer bodyRenderer;

        [Header("Animation")]
        [SerializeField] private Animator rabbitAnimator;
        [SerializeField] private string thriveAnimParam = "IsThriving";
        [SerializeField] private string distressAnimParam = "IsDistressed";
        [SerializeField] private string sleepAnimParam = "IsSleeping";

        [Header("System 1 — Emotional Bond")]
        [SerializeField] private Color bondGlowColor = new Color(1f, 0.9f, 0.5f, 1f);
        [SerializeField] private float bondGlowIntensity = 0.8f;
        [SerializeField] private float bondPulseSpeed = 2f;

        [Header("System 2 — Nutritional State")]
        [SerializeField] private Color wellFedColor = new Color(0.4f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color hungryColor = new Color(0.9f, 0.2f, 0.15f, 1f);

        [Header("System 3 — Injury")]
        [SerializeField] private Color woundFlickerColor = new Color(0.6f, 0.1f, 0.05f, 1f);
        [SerializeField] private float flickerSpeed = 8f;

        [Header("System 4 — Sleep")]
        [SerializeField] private float breathSpeed = 0.4f;
        [SerializeField] private float breathScale = 0.03f;

        private int _thriveHash;
        private int _distressHash;
        private int _sleepHash;
        private MaterialPropertyBlock _propBlock;
        private RabbitHealthState _currentState = RabbitHealthState.Content;
        private float _currentBond;
        private float _currentFeedLevel = 1f;
        private bool _isSleeping;
        private Vector3 _baseScale;

        private void Awake()
        {
            _thriveHash = Animator.StringToHash(thriveAnimParam);
            _distressHash = Animator.StringToHash(distressAnimParam);
            _sleepHash = Animator.StringToHash(sleepAnimParam);
            _propBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
            {
                bus.Subscribe<RabbitHealthStateChangedEvent>(HandleHealthStateChanged);
                bus.Subscribe<RabbitCareEvent>(HandleCareEvent);
            }

            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnTimeOfDayChanged += HandleTimeOfDayChanged;
        }

        private void OnDisable()
        {
            EventBus bus = EventBus.Instance;
            if (bus != null)
            {
                bus.Unsubscribe<RabbitHealthStateChangedEvent>(HandleHealthStateChanged);
                bus.Unsubscribe<RabbitCareEvent>(HandleCareEvent);
            }

            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
        }

        private void Update()
        {
            if (bodyRenderer == null) return;

            // Poll live data from RabbitCareManager
            RabbitCareManager care = RabbitCareManager.Instance;
            if (care != null && !string.IsNullOrEmpty(rabbitId))
            {
                _currentBond = care.GetBondScore(rabbitId);
                _currentFeedLevel = care.GetFeedLevel(rabbitId);
                _currentState = care.GetHealthState(rabbitId);
            }

            bodyRenderer.GetPropertyBlock(_propBlock);

            // ── System 1: Emotional Bond — glow pulse ───────────────────
            ApplyBondGlow();

            // ── System 2: Nutritional State — color shift ───────────────
            ApplyNutritionalColor();

            // ── System 3: Injury — wound flicker ────────────────────────
            ApplyInjuryVisual();

            // ── System 4: Sleep — breathing scale ───────────────────────
            ApplySleepBreathing();

            bodyRenderer.SetPropertyBlock(_propBlock);

            // Particle state
            UpdateParticleState();

            // Animator state
            UpdateAnimatorState();
        }

        // ── System 1: Emotional Bond ────────────────────────────────────

        private void ApplyBondGlow()
        {
            if (_currentBond <= 0.01f) return;

            float pulse = 1f + Mathf.Sin(Time.time * bondPulseSpeed) * 0.15f;
            float intensity = _currentBond * bondGlowIntensity * pulse;
            Color emission = bondGlowColor * intensity;
            _propBlock.SetColor(EmissionColorId, emission);
        }

        // ── System 2: Nutritional State ─────────────────────────────────

        private void ApplyNutritionalColor()
        {
            Color nutritionTint = Color.Lerp(hungryColor, wellFedColor, _currentFeedLevel);
            Color base_ = Color.Lerp(Color.white, nutritionTint, 0.4f);
            _propBlock.SetColor(BaseColorId, base_);
        }

        // ── System 3: Injury + Healing ──────────────────────────────────

        private void ApplyInjuryVisual()
        {
            if (_currentState != RabbitHealthState.Unwell
                && _currentState != RabbitHealthState.Critical)
                return;

            // Irregular flicker
            float flicker = Mathf.Abs(Mathf.Sin(Time.time * flickerSpeed)
                                    * Mathf.Cos(Time.time * flickerSpeed * 0.7f));
            Color woundEmission = woundFlickerColor * flicker * 0.5f;
            _propBlock.SetColor(EmissionColorId, woundEmission);
        }

        // ── System 4: Sleep + Wake ──────────────────────────────────────

        private void ApplySleepBreathing()
        {
            if (!_isSleeping) return;

            float breath = 1f + Mathf.Sin(Time.time * breathSpeed) * breathScale;
            transform.localScale = _baseScale * breath;
        }

        // ── Particles ───────────────────────────────────────────────────

        private void UpdateParticleState()
        {
            switch (_currentState)
            {
                case RabbitHealthState.Thriving:
                    PlayParticles(thriveParticles);
                    StopParticles(distressParticles);
                    StopParticles(healingParticles);
                    break;

                case RabbitHealthState.Unwell:
                case RabbitHealthState.Critical:
                    StopParticles(thriveParticles);
                    PlayParticles(distressParticles);
                    StopParticles(healingParticles);
                    break;

                case RabbitHealthState.Recovered:
                    StopParticles(thriveParticles);
                    StopParticles(distressParticles);
                    PlayParticles(healingParticles);
                    break;

                default:
                    StopParticles(thriveParticles);
                    StopParticles(distressParticles);
                    StopParticles(healingParticles);
                    break;
            }
        }

        private void UpdateAnimatorState()
        {
            if (rabbitAnimator == null) return;

            rabbitAnimator.SetBool(_thriveHash, _currentState == RabbitHealthState.Thriving);
            rabbitAnimator.SetBool(_distressHash,
                _currentState == RabbitHealthState.Unwell || _currentState == RabbitHealthState.Critical);
            rabbitAnimator.SetBool(_sleepHash, _isSleeping);
        }

        // ── Event handlers ──────────────────────────────────────────────

        private void HandleHealthStateChanged(RabbitHealthStateChangedEvent evt)
        {
            if (evt.RabbitId != rabbitId) return;
            _currentState = evt.Current;
        }

        private void HandleCareEvent(RabbitCareEvent evt)
        {
            if (evt.RabbitId != rabbitId) return;

            // Healing wave: brief green pulse on care
            if (healingParticles != null)
            {
                healingParticles.Play();
                healingParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void HandleTimeOfDayChanged(TimeOfDay previous, TimeOfDay current)
        {
            _isSleeping = current == TimeOfDay.Night || current == TimeOfDay.Midnight;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static void PlayParticles(ParticleSystem ps)
        {
            if (ps != null && !ps.isPlaying) ps.Play();
        }

        private static void StopParticles(ParticleSystem ps)
        {
            if (ps != null && ps.isPlaying) ps.Stop();
        }
    }
}
