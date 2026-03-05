using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the two parallel world layers:
///   PhysicalWorld — seen by Human form
///   VeiledWorld   — overlaid in Bunian form
///
/// The VeiledWorld is ALWAYS present. The Bunian sees both;
/// the Human sees only PhysicalWorld.
///
/// Setup:
///   1. Create a root GameObject named "VeiledWorldRoot" in scene.
///   2. Place all VeiledWorld objects as children.
///   3. Assign "VeiledWorldRoot" to the veiledWorldRoot field.
///   4. Objects in VeiledWorld should use IntanIsle_VeiledWorldShader.
///
/// The transition is a 3-second dissolve (never instant).
/// </summary>
public class VeiledWorldManager : MonoBehaviour
{
    public static VeiledWorldManager Instance { get; private set; }

    // ── Public state ──────────────────────────────────────────────
    public bool IsVeiledWorld { get; private set; }

    [Header("World Roots")]
    [SerializeField] private GameObject veiledWorldRoot;

    [Header("Transition")]
    [SerializeField] private float transitionDuration = 3.0f;

    [Header("Post-Processing (VeiledWorld)")]
    [SerializeField] private UnityEngine.Rendering.Volume veiledVolume;

    [Header("Audio")]
    [SerializeField] private AudioSource worldTransitionAudio;
    [SerializeField] private AudioClip   enterVeiledClip;
    [SerializeField] private AudioClip   exitVeiledClip;

    [Header("Linked Systems")]
    [SerializeField] private ZoneShaderLinker shaderLinker;

    // ── Internal ──────────────────────────────────────────────────
    private float          _dissolve        = 0f;   // 0 = physical, 1 = veiled
    private bool           _transitioning   = false;
    private CanvasGroup    _veiledHUDGroup;

    // ── Shader property IDs ───────────────────────────────────────
    private static readonly int ID_Dissolve = Shader.PropertyToID("_IntanIsle_VeilDissolve");

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (shaderLinker == null) shaderLinker = FindObjectOfType<ZoneShaderLinker>();

        // VeiledWorld starts hidden
        SetVeiledRootVisible(false);
        if (veiledVolume != null) veiledVolume.weight = 0f;
        Shader.SetGlobalFloat(ID_Dissolve, 0f);
        Shader.SetGlobalFloat("_IntanIsle_IsVeiledWorld", 0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
            ToggleVeiledWorld();
    }

    // ── Public entry points ───────────────────────────────────────

    /// <summary>Trigger switch: Human ↔ Bunian form.</summary>
    public void ToggleVeiledWorld()
    {
        if (_transitioning) return;
        StartCoroutine(TransitionTo(!IsVeiledWorld));
    }

    public void EnterVeiledWorld()
    {
        if (IsVeiledWorld || _transitioning) return;
        StartCoroutine(TransitionTo(true));
    }

    public void ExitVeiledWorld()
    {
        if (!IsVeiledWorld || _transitioning) return;
        StartCoroutine(TransitionTo(false));
    }

    // ── Transition coroutine ──────────────────────────────────────

    private IEnumerator TransitionTo(bool enterVeiled)
    {
        _transitioning = true;

        // Play audio cue
        if (worldTransitionAudio != null)
        {
            var clip = enterVeiled ? enterVeiledClip : exitVeiledClip;
            if (clip != null) worldTransitionAudio.PlayOneShot(clip, 0.5f);
        }

        float start  = _dissolve;
        float target = enterVeiled ? 1f : 0f;
        float elapsed = 0f;

        // Enable VeiledWorld root at start of enter transition
        if (enterVeiled) SetVeiledRootVisible(true);

        while (elapsed < transitionDuration)
        {
            elapsed   += Time.deltaTime;
            _dissolve  = Mathf.SmoothStep(start, target, elapsed / transitionDuration);

            // Push dissolve to all shaders
            Shader.SetGlobalFloat(ID_Dissolve, _dissolve);

            // Fade VeiledWorld post-processing volume
            if (veiledVolume != null) veiledVolume.weight = _dissolve;

            yield return null;
        }

        _dissolve = target;
        Shader.SetGlobalFloat(ID_Dissolve, _dissolve);

        IsVeiledWorld = enterVeiled;
        Shader.SetGlobalFloat("_IntanIsle_IsVeiledWorld", IsVeiledWorld ? 1f : 0f);
        if (shaderLinker != null) shaderLinker.SetVeiledWorld(IsVeiledWorld);

        // Hide VeiledWorld root at end of exit transition
        if (!enterVeiled) SetVeiledRootVisible(false);

        _transitioning = false;

        Debug.Log("[VeiledWorldManager] Form: " + (IsVeiledWorld ? "Bunian (VeiledWorld visible)" : "Human (PhysicalWorld only)"));
    }

    private void SetVeiledRootVisible(bool visible)
    {
        if (veiledWorldRoot != null) veiledWorldRoot.SetActive(visible);
    }

    // ── Static helper for other systems ──────────────────────────
    public static bool InVeiledWorld
        => Instance != null && Instance.IsVeiledWorld;
}
