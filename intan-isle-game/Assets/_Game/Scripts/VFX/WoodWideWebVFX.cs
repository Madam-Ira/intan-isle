using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>
    /// Renders the underground fungal network and drives plant bio-VFX
    /// systems 1–6 from the spec:
    ///   1) Core Setup — root network visibility tied to VeiledWorldManager
    ///   2) Communication — bioluminescent pulse on root renderers
    ///   3) Water Transport — rising glow through trunk on rain/watering
    ///   4) Nutritional Flow — pulsating nutrient dots via shader offset
    ///   5) Photosynthesis — leaf emission tied to sun/day phase
    ///   6) Flowering + Fruit — particle burst at branch tips on conditions
    /// Gracefully degrades if Dreamteck Splines is not installed.
    /// </summary>
    public class WoodWideWebVFX : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int OffsetYId = Shader.PropertyToID("_OffsetY");

        // ── System 1+2: Core network ────────────────────────────────────

        [Header("System 1-2 — Root Network")]
        [SerializeField] private MonoBehaviour[] networkSplines;
        [SerializeField] private Material networkMaterial;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float pulseIntensity = 0.3f;
        [SerializeField] private Color baseNetworkColor = new Color(0.4f, 0.8f, 0.6f, 0.8f);

        // ── System 3: Water Transport ───────────────────────────────────

        [Header("System 3 — Water Transport")]
        [SerializeField] private Renderer[] trunkRenderers;
        [SerializeField] private Color waterGlowColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        [SerializeField] private float waterRiseSpeed = 0.8f;
        [SerializeField] private float waterGlowDuration = 4f;

        // ── System 4: Nutritional Flow ──────────────────────────────────

        [Header("System 4 — Nutritional Flow")]
        [SerializeField] private Renderer[] nutrientRenderers;
        [SerializeField] private float nutrientFlowSpeed = 0.5f;
        [SerializeField] private Color nutrientColor = new Color(0.9f, 0.7f, 0.2f, 1f);

        // ── System 5: Photosynthesis ────────────────────────────────────

        [Header("System 5 — Photosynthesis")]
        [SerializeField] private Renderer[] leafRenderers;
        [SerializeField] private Color photosynthesisGlow = new Color(0.3f, 0.9f, 0.2f, 0.6f);
        [SerializeField] private float photosynthesisIntensity = 0.4f;

        // ── System 6: Flowering ─────────────────────────────────────────

        [Header("System 6 — Flowering + Fruit")]
        [SerializeField] private ParticleSystem flowerBurstParticles;
        [SerializeField] private float floweringThreshold = 0.7f;

        // ── State ───────────────────────────────────────────────────────

        private Coroutine _pulseCoroutine;
        private Coroutine _waterCoroutine;
        private MaterialPropertyBlock _propBlock;
        private Renderer[] _networkRenderers;
        private bool _isVisible;
        private float _treeHealthScore = 0.5f;
        private bool _isDaylight;
        private bool _hasFlowered;

        /// <summary>
        /// Aggregated network health from <see cref="WorldMemoryManager"/>.
        /// </summary>
        public float NetworkHealthScore
        {
            get
            {
                WorldMemoryManager memory = WorldMemoryManager.Instance;
                return memory != null ? memory.TotalWorldScore : 0f;
            }
        }

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            CacheNetworkRenderers();
            SetNetworkRenderersEnabled(false);
        }

        private void OnEnable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
            {
                veil.OnVeiledWorldEntered += ShowNetwork;
                veil.OnVeiledWorldExited += HideNetwork;
            }

            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnTimeOfDayChanged += HandleTimeOfDay;
        }

        private void OnDisable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
            {
                veil.OnVeiledWorldEntered -= ShowNetwork;
                veil.OnVeiledWorldExited -= HideNetwork;
            }

            GameTimeManager time = GameTimeManager.Instance;
            if (time != null)
                time.OnTimeOfDayChanged -= HandleTimeOfDay;
        }

        private void Update()
        {
            if (!_isVisible) return;

            _treeHealthScore = NetworkHealthScore;

            UpdateNutrientFlow();
            UpdatePhotosynthesis();
            CheckFlowering();
        }

        // ── System 1+2: Network visibility + pulse ──────────────────────

        public void ShowNetwork()
        {
            if (_isVisible) return;
            SetNetworkRenderersEnabled(true);
            _isVisible = true;

            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseLoop());
        }

        public void HideNetwork()
        {
            if (!_isVisible) return;
            if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
            SetNetworkRenderersEnabled(false);
            _isVisible = false;
        }

        private IEnumerator PulseLoop()
        {
            while (true)
            {
                float scoreFactor = 1f + Mathf.Clamp(_treeHealthScore / 50f, 0f, 2f);
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity * scoreFactor;
                Color emissionColor = baseNetworkColor * pulse;

                foreach (Renderer r in _networkRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(EmissionColorId, emissionColor);
                    _propBlock.SetColor(BaseColorId, baseNetworkColor);
                    r.SetPropertyBlock(_propBlock);
                }

                yield return null;
            }
        }

        // ── System 3: Water Transport ───────────────────────────────────

        /// <summary>
        /// Triggers the water transport rising glow. Call on rain or
        /// watering events.
        /// </summary>
        public void TriggerWaterTransport()
        {
            if (_waterCoroutine != null) StopCoroutine(_waterCoroutine);
            _waterCoroutine = StartCoroutine(WaterRiseGlow());
        }

        private IEnumerator WaterRiseGlow()
        {
            if (trunkRenderers == null) yield break;

            float elapsed = 0f;
            while (elapsed < waterGlowDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / waterGlowDuration);

                // Rise phase (0→0.5), then fade phase (0.5→1)
                float intensity = t < 0.5f
                    ? Mathf.Lerp(0f, 1f, t * 2f)
                    : Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);

                Color glow = waterGlowColor * intensity * waterRiseSpeed;

                foreach (Renderer r in trunkRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(_propBlock);
                    _propBlock.SetColor(EmissionColorId, glow);
                    r.SetPropertyBlock(_propBlock);
                }

                yield return null;
            }

            // Clear emission
            foreach (Renderer r in trunkRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(EmissionColorId, Color.black);
                r.SetPropertyBlock(_propBlock);
            }

            _waterCoroutine = null;
        }

        // ── System 4: Nutritional Flow ──────────────────────────────────

        private void UpdateNutrientFlow()
        {
            if (nutrientRenderers == null) return;

            float offset = Time.time * nutrientFlowSpeed;
            float healthFactor = Mathf.Clamp01(_treeHealthScore / 100f);
            Color flow = nutrientColor * healthFactor;

            foreach (Renderer r in nutrientRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(OffsetYId, offset);
                _propBlock.SetColor(EmissionColorId, flow);
                r.SetPropertyBlock(_propBlock);
            }
        }

        // ── System 5: Photosynthesis ────────────────────────────────────

        private void UpdatePhotosynthesis()
        {
            if (leafRenderers == null) return;

            float sunFactor = _isDaylight ? photosynthesisIntensity : 0f;
            float shimmer = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
            Color leafGlow = photosynthesisGlow * sunFactor * shimmer;

            foreach (Renderer r in leafRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(EmissionColorId, leafGlow);
                r.SetPropertyBlock(_propBlock);
            }
        }

        // ── System 6: Flowering ─────────────────────────────────────────

        private void CheckFlowering()
        {
            if (_hasFlowered) return;
            if (_treeHealthScore < floweringThreshold * 100f) return;

            _hasFlowered = true;

            if (flowerBurstParticles != null)
                flowerBurstParticles.Play();

            Debug.Log("[WoodWideWeb] Flowering triggered — ecosystem health threshold reached.");
        }

        /// <summary>Resets the flowering flag so it can trigger again.</summary>
        public void ResetFlowering()
        {
            _hasFlowered = false;
        }

        // ── Time of day ─────────────────────────────────────────────────

        private void HandleTimeOfDay(TimeOfDay previous, TimeOfDay current)
        {
            _isDaylight = current == TimeOfDay.Morning
                       || current == TimeOfDay.Midday
                       || current == TimeOfDay.Afternoon;
        }

        // ── Network renderer management ─────────────────────────────────

        private void CacheNetworkRenderers()
        {
            if (networkSplines == null || networkSplines.Length == 0)
            {
                _networkRenderers = new Renderer[0];
                return;
            }

            var list = new List<Renderer>();
            foreach (MonoBehaviour spline in networkSplines)
            {
                if (spline == null) continue;
                Renderer r = spline.GetComponent<Renderer>();
                if (r == null) r = spline.GetComponentInChildren<Renderer>();
                if (r != null) list.Add(r);
            }
            _networkRenderers = list.ToArray();
        }

        private void SetNetworkRenderersEnabled(bool enabled)
        {
            if (_networkRenderers == null) return;
            foreach (Renderer r in _networkRenderers)
            {
                if (r != null) r.enabled = enabled;
            }
        }
    }
}
