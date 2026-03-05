using UnityEngine;
using CesiumForUnity;

/// <summary>
/// Bunian Flight Controller — spiritual gliding across the whole globe.
///
/// WASD: horizontal movement
/// Space: ascend | C / Ctrl+Space: descend
/// Shift: sprint (3× speed)
/// Shift + Alt: ultra-fast globe-scouting (10× speed)
///
/// ALTITUDE LAYERS (Unity Y, metres above georeference origin):
///   0–100 m         Ground exploration, walkSpeed, FOV 60, far=500 m
///   100–5 000 m     Low flight, FOV 65, far=50 km
///   5 000–100 000 m Regional flight, FOV 75 lerp→85, far=500 km
///   100 000–6 000 000 m Globe view, FOV 85, far=20 000 km, fog off
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BunianFlightController : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float groundSpeed       =  8f;
    [SerializeField] private float flightSpeed       = 60f;
    [SerializeField] private float sprintMultiplier  =  3f;   // Shift
    [SerializeField] private float scoutMultiplier   = 10f;   // Shift + Alt
    [SerializeField] private float verticalSpeed     = 20f;
    [SerializeField] private float acceleration      =  0.18f;
    [SerializeField] private float deceleration      =  0.12f;

    // ── FOV ───────────────────────────────────────────────────────
    [Header("FOV by Altitude")]
    [SerializeField] private float fovGround         = 60f;
    [SerializeField] private float fovLow            = 65f;
    [SerializeField] private float fovRegional       = 75f;
    [SerializeField] private float fovGlobe          = 85f;
    [SerializeField] private float fovTransitionSpeed= 1.5f;

    // ── Far clip ──────────────────────────────────────────────────
    [Header("Camera Far Clip by Altitude")]
    [SerializeField] private float farGround         =      500f;   // 0.5 km
    [SerializeField] private float farLow            =    50000f;   // 50 km
    [SerializeField] private float farRegional       =   500000f;   // 500 km
    [SerializeField] private float farGlobe          = 20000000f;   // 20 000 km

    // ── Linked ────────────────────────────────────────────────────
    [Header("Linked Components")]
    [SerializeField] private Camera         mainCamera;
    [SerializeField] private AudioSource    windAudio;
    [SerializeField] private float          windMaxVolume = 0.6f;

    // ── Cesium LOD ────────────────────────────────────────────────
    [Header("Cesium Tileset (for altitude LOD)")]
    [SerializeField] private Cesium3DTileset cesiumTerrain;

    // ── Private state ─────────────────────────────────────────────
    private CharacterController _cc;
    private PlayerMovement      _playerMovement;
    private Vector3             _velocity      = Vector3.zero;
    private float               _targetFOV     = 60f;
    private float               _targetFar     = 500f;
    private double              _altitude      = 0;      // metres, from CesiumGlobeAnchor or Y
    private bool                _flightActive  = false;
    private CesiumGlobeAnchor   _anchor;

    // Altitude breakpoints
    private const double ALT_LOW      =      100.0;
    private const double ALT_REGIONAL =    5_000.0;
    private const double ALT_GLOBE    =  100_000.0;
    private const double ALT_MAX      = 6_000_000.0;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _cc             = GetComponent<CharacterController>();
        _playerMovement = GetComponent<PlayerMovement>();
        _anchor         = GetComponent<CesiumGlobeAnchor>();

        if (mainCamera == null)
        {
            var camRoot = transform.Find("CameraRoot");
            if (camRoot != null) mainCamera = camRoot.GetComponentInChildren<Camera>();
        }
    }

    // ── Form activation ───────────────────────────────────────────

    public void ActivateFlight()
    {
        _flightActive = true;
        if (_playerMovement != null) _playerMovement.enabled = false;
        if (_cc != null) _cc.enabled = false; // no gravity in Bunian form
        Debug.Log("[BunianFlight] Flight activated.");
    }

    public void DeactivateFlight()
    {
        _flightActive = false;
        if (_playerMovement != null) _playerMovement.enabled = true;
        if (_cc != null) _cc.enabled = true;
        _velocity = Vector3.zero;
        Debug.Log("[BunianFlight] Flight deactivated — returned to ground form.");
    }

    // ─────────────────────────────────────────────────────────────

    void Update()
    {
        if (!_flightActive) return;

        // Use globe anchor height if available, else fallback to Unity Y
        _altitude = (_anchor != null && _anchor.enabled)
            ? _anchor.longitudeLatitudeHeight.z
            : transform.position.y;

        HandleMovement();
        HandleFOV();
        HandleFarClip();
        HandleFog();
        HandleWindAudio();
        HandleCesiumLOD();
    }

    // ── Movement ──────────────────────────────────────────────────

    private void HandleMovement()
    {
        bool shift = Input.GetKey(KeyCode.LeftShift);
        bool alt   = Input.GetKey(KeyCode.LeftAlt);

        // Speed tier — also scales with altitude for globe scouting
        float baseSpeed  = _altitude < ALT_LOW ? groundSpeed : flightSpeed;

        // Altitude bonus: at globe level speed ramps up dramatically
        float altBonus   = 1f;
        if (_altitude > ALT_GLOBE)
            altBonus = Mathf.Lerp(1f, 100f,
                           (float)((_altitude - ALT_GLOBE) / (ALT_MAX - ALT_GLOBE)));
        else if (_altitude > ALT_REGIONAL)
            altBonus = Mathf.Lerp(1f, 10f,
                           (float)((_altitude - ALT_REGIONAL) / (ALT_GLOBE - ALT_REGIONAL)));

        float speed = baseSpeed * altBonus
                    * (alt && shift ? scoutMultiplier
                       : shift      ? sprintMultiplier
                       :              1f);

        // Horizontal input (camera-relative)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 camForward = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized
            : transform.forward;
        Vector3 camRight = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized
            : transform.right;

        Vector3 inputDir = (camForward * v + camRight * h).normalized;

        // Vertical input
        float vert = 0f;
        bool ascend  = Input.GetKey(KeyCode.Space);
        bool descend = Input.GetKey(KeyCode.C)
                    || (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Space));
        if (ascend && !descend)  vert =  verticalSpeed * (shift ? sprintMultiplier : 1f) * (float)altBonus;
        if (descend && !ascend)  vert = -verticalSpeed * (shift ? sprintMultiplier : 1f) * (float)altBonus;

        // Smooth toward target velocity
        Vector3 targetVel = inputDir * speed + Vector3.up * vert;
        float   accel     = targetVel.magnitude > _velocity.magnitude ? acceleration : deceleration;
        _velocity         = Vector3.Lerp(_velocity, targetVel, accel);

        // Direct transform move — no physics gravity in Bunian form
        transform.position += _velocity * Time.deltaTime;

        // Clamp — never below 0.5 m
        if (transform.position.y < 0.5f)
        {
            var p  = transform.position;
            p.y    = 0.5f;
            transform.position = p;
            _velocity.y = Mathf.Max(0f, _velocity.y);
        }
    }

    // ── FOV ───────────────────────────────────────────────────────

    private void HandleFOV()
    {
        if (mainCamera == null) return;

        if      (_altitude < ALT_LOW)
            _targetFOV = fovGround;
        else if (_altitude < ALT_REGIONAL)
            _targetFOV = fovLow;
        else if (_altitude < ALT_GLOBE)
            _targetFOV = Mathf.Lerp(fovRegional, fovGlobe,
                             (float)((_altitude - ALT_REGIONAL) / (ALT_GLOBE - ALT_REGIONAL)));
        else
            _targetFOV = fovGlobe;

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView,
                                             _targetFOV,
                                             Time.deltaTime * fovTransitionSpeed);
    }

    // ── Far clip — must scale with altitude for globe ─────────────

    private void HandleFarClip()
    {
        if (mainCamera == null) return;

        if      (_altitude < ALT_LOW)
            _targetFar = farGround;
        else if (_altitude < ALT_REGIONAL)
            _targetFar = Mathf.Lerp(farGround, farLow,
                             (float)((_altitude - ALT_LOW) / (ALT_REGIONAL - ALT_LOW)));
        else if (_altitude < ALT_GLOBE)
            _targetFar = Mathf.Lerp(farLow, farRegional,
                             (float)((_altitude - ALT_REGIONAL) / (ALT_GLOBE - ALT_REGIONAL)));
        else
            _targetFar = farGlobe;

        mainCamera.farClipPlane = Mathf.Lerp(mainCamera.farClipPlane,
                                              _targetFar,
                                              Time.deltaTime * 0.5f);
    }

    // ── Fog — disable at extreme altitude (globe views) ───────────

    private void HandleFog()
    {
        bool wantFog = _altitude < ALT_GLOBE;
        if (RenderSettings.fog != wantFog)
            RenderSettings.fog = wantFog;
    }

    // ── Wind audio ────────────────────────────────────────────────

    private void HandleWindAudio()
    {
        if (windAudio == null) return;

        float targetVol = Mathf.Clamp01((float)(_altitude / ALT_REGIONAL)) * windMaxVolume;
        windAudio.volume = Mathf.MoveTowards(windAudio.volume, targetVol, Time.deltaTime * 0.3f);

        if (targetVol > 0.01f && !windAudio.isPlaying) windAudio.Play();
        else if (targetVol < 0.01f && windAudio.isPlaying) windAudio.Stop();
    }

    // ── Cesium LOD from altitude ──────────────────────────────────

    private void HandleCesiumLOD()
    {
        if (cesiumTerrain == null) return;

        // Lower SSE = more detail | Higher SSE = less detail (faster streaming)
        float targetSSE;
        if      (_altitude <      100) targetSSE =   4f;   // maximum detail at ground
        else if (_altitude <    1_000) targetSSE =   8f;
        else if (_altitude <   10_000) targetSSE =  16f;
        else if (_altitude <   50_000) targetSSE =  32f;
        else if (_altitude <  100_000) targetSSE =  64f;   // regional
        else if (_altitude <  500_000) targetSSE = 128f;   // continental
        else                           targetSSE = 256f;   // globe

        cesiumTerrain.maximumScreenSpaceError =
            Mathf.Lerp(cesiumTerrain.maximumScreenSpaceError, targetSSE, Time.deltaTime * 0.5f);
    }

    // ── Status ────────────────────────────────────────────────────
    public double Altitude     => _altitude;
    public bool   IsFlying     => _flightActive;
    public float  CurrentSpeed => _velocity.magnitude;
}
