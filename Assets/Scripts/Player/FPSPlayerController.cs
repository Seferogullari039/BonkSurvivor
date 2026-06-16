using UnityEngine;

public class FPSPlayerController : MonoBehaviour
{
    public static bool IsFpsModeActive { get; private set; }
    public static bool IsInvulnerable { get; private set; }

    private const float DashDistance = 5f;
    private const float DashDuration = 0.15f;
    private const float DashCooldown = 3f;
    private const float DashInvulnerabilityDuration = 0.2f;
    private const float JumpForce = 6f;
    private const float Gravity = -20f;
    private const float GroundedStickVelocity = -2f;
    private const float AirControlMultiplier = 0.55f;
    private const float MaxAirSpeed = 9f;
    private const float DashMomentumRetention = 0.72f;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float eyeHeight = 1.65f;
    [SerializeField] private Transform cameraTransform;

    private CharacterController characterController;
    private PlayerController topDownController;
    private WeaponManager weaponManager;
    private FPSWeaponController fpsWeaponController;
    private StarterWeaponController starterWeaponController;
    private FPSViewModel fpsViewModel;
    private CameraFollow cameraFollow;
    private Rigidbody playerRigidbody;

    private float pitch;
    private float baseMoveSpeed;
    private bool fpsActive;
    private float dashCooldownTimer;
    private float dashTimer;
    private Vector3 dashDirection;
    private bool isDashing;
    private float invulnerabilityTimer;
    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private float dashSpeed;

    private Transform savedCameraParent;
    private Vector3 savedCameraWorldPosition;
    private Quaternion savedCameraWorldRotation;

    private void Awake()
    {
        IsFpsModeActive = false;
        IsInvulnerable = false;
        baseMoveSpeed = moveSpeed;
        topDownController = GetComponent<PlayerController>();
        weaponManager = GetComponent<WeaponManager>();
        fpsWeaponController = GetComponent<FPSWeaponController>();
        starterWeaponController = GetComponent<StarterWeaponController>();
        fpsViewModel = GetComponent<FPSViewModel>();
        playerRigidbody = GetComponent<Rigidbody>();

        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        characterController.height = 1.8f;
        characterController.radius = 0.4f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.enabled = false;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            cameraFollow = cameraTransform.GetComponent<CameraFollow>();
            savedCameraParent = cameraTransform.parent;
            savedCameraWorldPosition = cameraTransform.position;
            savedCameraWorldRotation = cameraTransform.rotation;
        }

        if (fpsWeaponController != null)
        {
            fpsWeaponController.enabled = false;
        }
    }

    private void Update()
    {
        bool shouldUseFps = MainMenuManager.IsRunActive;

        if (shouldUseFps != fpsActive)
        {
            SetFpsMode(shouldUseFps);
        }

        if (!fpsActive) return;

        UpdateCursorState();
        UpdateInvulnerability();

        if (Time.timeScale <= 0f) return;

        HandleMouseLook();

        if (HandleDash())
        {
            return;
        }

        HandleMovement();
    }

    private void SetFpsMode(bool active)
    {
        fpsActive = active;
        IsFpsModeActive = active;

        if (!active)
        {
            isDashing = false;
            dashCooldownTimer = 0f;
            dashTimer = 0f;
            invulnerabilityTimer = 0f;
            IsInvulnerable = false;
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
        }

        if (topDownController != null)
        {
            topDownController.enabled = !active;
        }

        if (fpsWeaponController != null)
        {
            bool starterHandlesWeapons = starterWeaponController != null && starterWeaponController.enabled;
            fpsWeaponController.enabled = active && !starterHandlesWeapons;
        }

        if (playerRigidbody != null)
        {
            if (active)
            {
                playerRigidbody.isKinematic = true;
            }
            else
            {
                playerRigidbody.isKinematic = false;
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }

        if (characterController != null)
        {
            characterController.enabled = active;
        }

        if (cameraTransform == null) return;

        if (cameraFollow != null)
        {
            cameraFollow.enabled = !active && MainMenuManager.IsRunActive;
        }

        if (active)
        {
            RefreshMoveSpeed();
            verticalVelocity = 0f;
            horizontalVelocity = Vector3.zero;
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition = new Vector3(0f, eyeHeight, 0f);
            cameraTransform.localRotation = Quaternion.identity;
            pitch = 0f;
        }
        else
        {
            cameraTransform.SetParent(savedCameraParent);
            cameraTransform.position = savedCameraWorldPosition;
            cameraTransform.rotation = savedCameraWorldRotation;
        }

        UpdateCursorState();
    }

    public void ForceGameplayCameraReady()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return;
        }

        if (!fpsActive)
        {
            SetFpsMode(true);
            return;
        }

        if (cameraTransform == null)
        {
            return;
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }

        if (cameraTransform.parent != transform)
        {
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition = new Vector3(0f, eyeHeight, 0f);
            cameraTransform.localRotation = Quaternion.identity;
            pitch = 0f;
        }
    }

    private void RefreshMoveSpeed()
    {
        moveSpeed = baseMoveSpeed * (1f + 0.05f * MetaProgressionData.UpgradeLevelSpeed);
    }

    private void UpdateCursorState()
    {
        if (DevAdminPanel.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool lockCursor = fpsActive && MainMenuManager.IsRunActive && Time.timeScale > 0f;

        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockCursor;
    }

    private void UpdateInvulnerability()
    {
        if (invulnerabilityTimer <= 0f) return;

        invulnerabilityTimer -= Time.unscaledDeltaTime;

        if (invulnerabilityTimer <= 0f)
        {
            IsInvulnerable = false;
        }
    }

    private void HandleMouseLook()
    {
        if (DevAdminPanel.IsOpen) return;
        if (cameraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up, mouseX, Space.World);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -85f, 85f);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private bool HandleDash()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            ApplyGravity();

            if (characterController != null)
            {
                Vector3 dashMove = dashDirection * dashSpeed * Time.deltaTime;
                dashMove.y = verticalVelocity * Time.deltaTime;
                characterController.Move(dashMove);
            }

            if (dashTimer <= 0f)
            {
                isDashing = false;
                horizontalVelocity = dashDirection * dashSpeed * DashMomentumRetention;
            }

            return true;
        }

        if (!Input.GetKeyDown(KeyCode.LeftShift) || dashCooldownTimer > 0f)
        {
            return false;
        }

        dashDirection = GetDashDirection();

        if (dashDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        isDashing = true;
        dashTimer = DashDuration;
        dashCooldownTimer = DashCooldown;
        dashSpeed = DashDistance / DashDuration;
        horizontalVelocity = dashDirection * dashSpeed;
        IsInvulnerable = true;
        invulnerabilityTimer = DashInvulnerabilityDuration;
        fpsViewModel?.PlayDash();
        return true;
    }

    private Vector3 GetDashDirection()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;

        Vector3 direction = transform.right * horizontal + transform.forward * vertical;

        if (direction.sqrMagnitude < 0.001f)
        {
            if (cameraTransform != null)
            {
                direction = cameraTransform.forward;
            }
            else
            {
                direction = transform.forward;
            }
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    private void HandleMovement()
    {
        if (characterController == null) return;

        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = GroundedStickVelocity;
        }

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = JumpForce;
            PreserveJumpMomentum();
        }

        Vector3 inputDirection = GetMoveInputDirection();
        UpdateHorizontalVelocity(inputDirection, isGrounded);
        ApplyGravity();

        Vector3 move = horizontalVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    private Vector3 GetMoveInputDirection()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;

        if (horizontal == 0f && vertical == 0f)
        {
            return Vector3.zero;
        }

        return (transform.right * horizontal + transform.forward * vertical).normalized;
    }

    private void UpdateHorizontalVelocity(Vector3 inputDirection, bool isGrounded)
    {
        if (isGrounded)
        {
            if (inputDirection.sqrMagnitude > 0.001f)
            {
                horizontalVelocity = inputDirection * moveSpeed;
            }
            else
            {
                horizontalVelocity = Vector3.zero;
            }

            return;
        }

        Vector3 wishVelocity = inputDirection * moveSpeed;
        Vector3 currentFlat = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        Vector3 nextFlat = Vector3.MoveTowards(
            currentFlat,
            wishVelocity,
            moveSpeed * AirControlMultiplier * Time.deltaTime * 12f
        );

        if (nextFlat.magnitude > MaxAirSpeed)
        {
            nextFlat = nextFlat.normalized * MaxAirSpeed;
        }

        horizontalVelocity = new Vector3(nextFlat.x, 0f, nextFlat.z);
    }

    private void PreserveJumpMomentum()
    {
        Vector3 flatVelocity = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);

        if (flatVelocity.sqrMagnitude < moveSpeed * moveSpeed * 0.25f)
        {
            return;
        }

        float boostedSpeed = Mathf.Min(flatVelocity.magnitude * 1.04f, MaxAirSpeed);
        horizontalVelocity = flatVelocity.normalized * boostedSpeed;
    }

    private void ApplyGravity()
    {
        verticalVelocity += Gravity * Time.deltaTime;
    }
}
