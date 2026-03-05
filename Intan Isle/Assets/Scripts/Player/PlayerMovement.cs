using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float walkSpeed   =  8f;
    [SerializeField] private float sprintSpeed = 24f;  // Shift
    [SerializeField] private float fastSpeed   = 80f;  // Shift + Alt  (Cesium scouting)

    private CharacterController _cc;
    private float _verticalVelocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        bool shift = Input.GetKey(KeyCode.LeftShift);
        bool alt   = Input.GetKey(KeyCode.LeftAlt);
        float speed = alt && shift ? fastSpeed
                    : shift        ? sprintSpeed
                    :                walkSpeed;

        Vector3 move = (transform.right * h + transform.forward * v) * speed;

        if (_cc.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity -= 9.81f * Time.deltaTime;
        _verticalVelocity  = Mathf.Max(_verticalVelocity, -20f);
        move.y = _verticalVelocity;

        _cc.Move(move * Time.deltaTime);
    }
}
