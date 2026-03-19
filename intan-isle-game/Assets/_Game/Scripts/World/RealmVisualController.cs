using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace IntanIsle.Core
{
    /// <summary>
    /// Manages the visual toggle between Human World and Hidden Realm.
    /// Crossfades: lighting, fog, ambient color, post-process volume,
    /// ambient audio, and global shader properties.
    /// Subscribes to <see cref="VeiledWorldManager"/> enter/exit events.
    /// </summary>
    public class RealmVisualController : MonoBehaviour
    {
        // ── Human World profile ─────────────────────────────────────────

        [Header("Human World — Lighting")]
        [SerializeField] private Color humanAmbientColor = new Color(0.72f, 0.86f, 0.78f, 1f);
        [SerializeField] private Color humanFogColor = new Color(0.75f, 0.82f, 0.68f, 1f);
        [SerializeField] private float humanFogDensity = 0.003f;
        [SerializeField] private float humanSunIntensity = 1.0f;
        [SerializeField] private Color humanSunColor = new Color(1f, 0.95f, 0.85f, 1f);

        // ── Hidden Realm profile ────────────────────────────────────────

        [Header("Hidden Realm — Lighting")]
        [SerializeField] private Color veiledAmbientColor = new Color(0.04f, 0.15f, 0.12f, 1f);
        [SerializeField] private Color veiledFogColor = new Color(0.02f, 0.08f, 0.06f, 1f);
        [SerializeField] private float veiledFogDensity = 0.008f;
        [SerializeField] private float veiledSunIntensity = 0.05f;
        [SerializeField] private Color veiledSunColor = new Color(0.25f, 0.55f, 0.45f, 1f);

        // ── Post-processing ─────────────────────────────────────────────

        [Header("Post-Processing Volumes")]
        [SerializeField] private Volume humanVolume;
        [SerializeField] private Volume veiledVolume;

        // ── Audio ───────────────────────────────────────────────────────

        [Header("Ambient Audio")]
        [SerializeField] private AudioSource humanAmbientAudio;
        [SerializeField] private AudioSource veiledAmbientAudio;
        [SerializeField] private float audioCrossfadeDuration = 2f;

        // ── References ──────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private Light directionalLight;

        // ── Transition ──────────────────────────────────────────────────

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 3f;

        private Coroutine _crossfadeCoroutine;

        // ── Lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            if (directionalLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        directionalLight = l;
                        break;
                    }
                }
            }

            ApplyHumanWorld();
        }

        private void OnEnable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
            {
                veil.OnVeiledWorldEntered += TransitionToVeiled;
                veil.OnVeiledWorldExited += TransitionToHuman;
            }
        }

        private void OnDisable()
        {
            VeiledWorldManager veil = VeiledWorldManager.Instance;
            if (veil != null)
            {
                veil.OnVeiledWorldEntered -= TransitionToVeiled;
                veil.OnVeiledWorldExited -= TransitionToHuman;
            }
        }

        // ── Transitions ─────────────────────────────────────────────────

        private void TransitionToVeiled()
        {
            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = StartCoroutine(CrossfadeRealm(toVeiled: true));
        }

        private void TransitionToHuman()
        {
            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);
            _crossfadeCoroutine = StartCoroutine(CrossfadeRealm(toVeiled: false));
        }

        private IEnumerator CrossfadeRealm(bool toVeiled)
        {
            Color startAmbient = RenderSettings.ambientSkyColor;
            Color startFog = RenderSettings.fogColor;
            float startFogDensity = RenderSettings.fogDensity;
            float startSunIntensity = directionalLight != null ? directionalLight.intensity : 1f;
            Color startSunColor = directionalLight != null ? directionalLight.color : Color.white;

            Color endAmbient = toVeiled ? veiledAmbientColor : humanAmbientColor;
            Color endFog = toVeiled ? veiledFogColor : humanFogColor;
            float endFogDensity = toVeiled ? veiledFogDensity : humanFogDensity;
            float endSunIntensity = toVeiled ? veiledSunIntensity : humanSunIntensity;
            Color endSunColor = toVeiled ? veiledSunColor : humanSunColor;

            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));

                RenderSettings.ambientSkyColor = Color.Lerp(startAmbient, endAmbient, t);
                RenderSettings.fogColor = Color.Lerp(startFog, endFog, t);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, endFogDensity, t);

                if (directionalLight != null)
                {
                    directionalLight.intensity = Mathf.Lerp(startSunIntensity, endSunIntensity, t);
                    directionalLight.color = Color.Lerp(startSunColor, endSunColor, t);
                }

                // Post-process volume crossfade
                if (humanVolume != null)
                    humanVolume.weight = toVeiled ? 1f - t : t;
                if (veiledVolume != null)
                    veiledVolume.weight = toVeiled ? t : 1f - t;

                // Shader globals for material response
                Shader.SetGlobalFloat("_IntanIsle_IsVeiledWorld", toVeiled ? t : 1f - t);
                Shader.SetGlobalFloat("_IntanIsle_BioLum", toVeiled ? t : 1f - t);
                Shader.SetGlobalFloat("_IntanIsle_BioLumIntensity", toVeiled ? t : 0f);

                yield return null;
            }

            // Final snap
            RenderSettings.ambientSkyColor = endAmbient;
            RenderSettings.fogColor = endFog;
            RenderSettings.fogDensity = endFogDensity;

            if (directionalLight != null)
            {
                directionalLight.intensity = endSunIntensity;
                directionalLight.color = endSunColor;
            }

            // Audio crossfade
            StartCoroutine(CrossfadeAudio(toVeiled));

            Shader.SetGlobalFloat("_IntanIsle_IsVeiledWorld", toVeiled ? 1f : 0f);
            Shader.SetGlobalFloat("_IntanIsle_BioLum", toVeiled ? 1f : 0f);
            Shader.SetGlobalFloat("_IntanIsle_BioLumIntensity", toVeiled ? 1f : 0f);

            _crossfadeCoroutine = null;
        }

        private IEnumerator CrossfadeAudio(bool toVeiled)
        {
            AudioSource fadeOut = toVeiled ? humanAmbientAudio : veiledAmbientAudio;
            AudioSource fadeIn = toVeiled ? veiledAmbientAudio : humanAmbientAudio;

            if (fadeIn != null && !fadeIn.isPlaying)
                fadeIn.Play();

            float elapsed = 0f;
            float fadeOutStart = fadeOut != null ? fadeOut.volume : 0f;

            while (elapsed < audioCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / audioCrossfadeDuration);

                if (fadeOut != null) fadeOut.volume = Mathf.Lerp(fadeOutStart, 0f, t);
                if (fadeIn != null) fadeIn.volume = t;

                yield return null;
            }

            if (fadeOut != null)
            {
                fadeOut.volume = 0f;
                fadeOut.Stop();
            }
        }

        // ── Snap apply (no transition) ──────────────────────────────────

        private void ApplyHumanWorld()
        {
            RenderSettings.ambientSkyColor = humanAmbientColor;
            RenderSettings.fogColor = humanFogColor;
            RenderSettings.fogDensity = humanFogDensity;
            RenderSettings.fog = true;

            if (directionalLight != null)
            {
                directionalLight.intensity = humanSunIntensity;
                directionalLight.color = humanSunColor;
            }

            if (humanVolume != null) humanVolume.weight = 1f;
            if (veiledVolume != null) veiledVolume.weight = 0f;

            Shader.SetGlobalFloat("_IntanIsle_IsVeiledWorld", 0f);
            Shader.SetGlobalFloat("_IntanIsle_BioLum", 0f);
            Shader.SetGlobalFloat("_IntanIsle_BioLumIntensity", 0f);
        }
    }
}
