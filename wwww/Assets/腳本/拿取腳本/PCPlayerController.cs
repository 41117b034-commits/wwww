using UnityEngine;

public class PCPlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float gravity = -9.81f;

    [Header("視角設定")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    [Range(0f, 89f)] public float maxLookDown = 80f;

    private CharacterController controller;
    private float verticalVelocity;
    private float xRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Move();
        Look();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Move()
    {
        if (controller == null) return;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        MoveCharacter(new Vector2(x, z), Input.GetKey(KeyCode.LeftShift));
    }

    private void MoveCharacter(Vector2 input, bool running)
    {
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        float speed = running ? runSpeed : walkSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0)
            {
                verticalVelocity = -1f;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void Look()
    {
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        ApplyPitch(xRotation - mouseY);
    }

    public void SetInitialLookAt(Vector3 worldPoint)
    {
        if (!cameraTransform) return;
        Vector3 direction = worldPoint - cameraTransform.position;
        if (cameraTransform.parent)
            direction = cameraTransform.parent.InverseTransformDirection(direction);
        float horizontal = new Vector2(direction.x, direction.z).magnitude;
        if (horizontal < .0001f) return;
        ApplyPitch(-Mathf.Atan2(direction.y, horizontal) * Mathf.Rad2Deg);
    }

    private void ApplyPitch(float degrees)
    {
        xRotation = Mathf.Clamp(degrees, -80f, maxLookDown);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}