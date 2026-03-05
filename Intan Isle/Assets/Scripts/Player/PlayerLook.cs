using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CameraRoot child — receives pitch rotation")]
    public Transform cameraRoot;

    [Header("Settings")]
    public float sensitivity = 1.8f;
    [Range(0.01f, 1f)] public float smoothing = 0.08f;

    private float _pitch;
    private float _yaw;
    private Vector2 _smoothedDelta;
    private Vector2 _smoothDampRef;

    private bool _cursorLocked = true;

    private const float PitchMin = -75f;
    private const float PitchMax =  75f;

    private void Start()
    {
        SetCursorLock(true);

        // Initialise yaw from PlayerRig's current world rotation so we don't snap
        _yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        HandleCursorToggle();
        if (!_cursorLocked) return;

        // Raw mouse delta
        Vector2 rawDelta = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")) * sensitivity;

        // Smooth
        _smoothedDelta = Vector2.SmoothDamp(
            _smoothedDelta, rawDelta, ref _smoothDampRef, smoothing);

        _yaw   += _smoothedDelta.x;
        _pitch -= _smoothedDelta.y;
        _pitch  = Mathf.Clamp(_pitch, PitchMin, PitchMax);

        // Yaw rotates the whole PlayerRig (which carries the CharacterController)
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleCursorToggle()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SetCursorLock(!_cursorLocked);
    }

    private void SetCursorLock(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
