using UnityEngine;

/// <summary>
/// Contrôleur joueur 3D. PC : WASD + clic droit. Mobile : joysticks.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Déplacement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float gravity = -18f;
    public float rotationSpeed = 12f;

    [Header("Caméra 3e personne")]
    public Transform cameraPivot;
    public float mouseSensitivity = 2.2f;
    public float minPitch = -20f;
    public float maxPitch = 55f;
    public float cameraDistance = 6f;

    [Header("Interaction")]
    public float interactRange = 8f;
    public KeyCode interactKey = KeyCode.E;

    public System.Action<RestaurantBuilding> OnNearRestaurantChanged;

    private CharacterController controller;
    private float yaw;
    private float pitch = 15f;
    private Vector3 velocity;
    private RestaurantBuilding nearestRestaurant;
    private bool inputEnabled = true;

    [HideInInspector] public Vector2 mobileMoveInput;
    [HideInInspector] public Vector2 mobileLookInput;

    public RestaurantBuilding NearestRestaurant => nearestRestaurant;

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (!inputEnabled) return;
        HandleLook();
        HandleMove();
        DetectNearbyRestaurant();
        HandleInteract();
    }

    private void LateUpdate()
    {
        if (GameCameraDirector.Instance != null &&
            GameCameraDirector.Instance.Mode != CameraGameMode.PlayerFollow)
            return;
        UpdateCamera();
    }

    public void SetInputEnabled(bool enabled) => inputEnabled = enabled;

    /// <summary>Recale immédiatement la caméra derrière le joueur (sortie carte / entrée resto).</summary>
    public void SnapCameraBehind()
    {
        pitch = 18f;
        yaw = transform.eulerAngles.y;
        mobileLookInput = Vector2.zero;
        UpdateCamera();
    }

    private void HandleLook()
    {
        float mx = mobileLookInput.x;
        float my = mobileLookInput.y;

        if (Input.GetMouseButton(1))
        {
            mx += Input.GetAxis("Mouse X");
            my += Input.GetAxis("Mouse Y");
        }

        yaw += mx * mouseSensitivity;
        pitch -= my * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleMove()
    {
        float h = Input.GetAxisRaw("Horizontal") + mobileMoveInput.x;
        float v = Input.GetAxisRaw("Vertical") + mobileMoveInput.y;
        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        Transform cam = Camera.main != null ? Camera.main.transform : cameraPivot;
        Vector3 move = Vector3.zero;
        if (cam != null)
        {
            Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            move = forward * input.z + right * input.x;
        }

        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runSpeed : walkSpeed;

        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        controller.Move((move * speed + Vector3.up * velocity.y) * Time.deltaTime);
    }

    private void UpdateCamera()
    {
        if (cameraPivot == null || Camera.main == null) return;
        cameraPivot.position = transform.position + Vector3.up * 1.6f;
        cameraPivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
        Camera.main.transform.position = cameraPivot.position - cameraPivot.forward * cameraDistance;
        Camera.main.transform.LookAt(cameraPivot.position);
    }

    private void DetectNearbyRestaurant()
    {
        RestaurantBuilding found = null;
        float best = interactRange;
        var buildings = FindObjectsOfType<RestaurantBuilding>();
        for (int i = 0; i < buildings.Length; i++)
        {
            float d = Vector3.Distance(transform.position, buildings[i].EntrancePoint.position);
            if (d < best)
            {
                best = d;
                found = buildings[i];
            }
        }

        if (found != nearestRestaurant)
        {
            nearestRestaurant = found;
            OnNearRestaurantChanged?.Invoke(found);
        }
    }

    private void HandleInteract()
    {
        if (nearestRestaurant == null) return;
        if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Return))
            nearestRestaurant.Enter();
    }

    public void Teleport(Vector3 position, float yRotation)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        yaw = yRotation;
        controller.enabled = true;
    }
}
