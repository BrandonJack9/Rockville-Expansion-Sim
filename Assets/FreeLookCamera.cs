using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float sprintSpeedMultiplier = 2f;
    public float mouseSensitivity = 5f;
    public float rotationSmoothing = 0.1f;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private Vector3 _currentRotation;
    private Vector3 _rotationVelocity = Vector3.zero;

    void Start()
    {
        // Lock and hide the cursor for a more immersive experience
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        // Initialize rotation to current camera orientation
        _rotationX = transform.eulerAngles.y;
        _rotationY = transform.eulerAngles.x;
    }

    void Update()
    {
        // Mouse Look
        if (Input.GetMouseButton(1)) // Right-click to look around
        {
            _rotationX += Input.GetAxis("Mouse X") * mouseSensitivity;
            _rotationY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            _rotationY = Mathf.Clamp(_rotationY, -90f, 90f); // Clamp vertical rotation

            Vector3 targetRotation = new Vector3(_rotationY, _rotationX, 0f);
            _currentRotation = Vector3.SmoothDamp(_currentRotation, targetRotation, ref _rotationVelocity, rotationSmoothing);
            transform.eulerAngles = _currentRotation;
        }

        // Keyboard Movement (WASD, Q, E, Shift for sprint)
        float currentMoveSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentMoveSpeed *= sprintSpeedMultiplier;
        }

        Vector3 moveDirection = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) { moveDirection += transform.forward; }
        if (Input.GetKey(KeyCode.S)) { moveDirection -= transform.forward; }
        if (Input.GetKey(KeyCode.A)) { moveDirection -= transform.right; }
        if (Input.GetKey(KeyCode.D)) { moveDirection += transform.right; }
        if (Input.GetKey(KeyCode.Q)) { moveDirection -= transform.up; } // Move down
        if (Input.GetKey(KeyCode.E)) { moveDirection += transform.up; } // Move up

        transform.position += moveDirection.normalized * currentMoveSpeed * Time.deltaTime;
    }

    void OnDisable()
    {
        // Unlock and show the cursor when the script is disabled or Play Mode ends
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}