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
    private const float CrouchCameraOffset = -0.35f;
    private const float CrouchHeightMultiplier = 0.62f;
    private const float CrouchSpeedMultiplier = 0.65f;
    private const float CrouchSmoothSpeed = 10f;
    private const float SlideDuration = 0.45f;
    private const float SlideCooldown = 0.9f;
    private const float SlideSpeedMultiplier = 1.35f;
    private const float MinSlideInputThreshold = 0.2f;
    private const float SlideEndSpeedRetention = 0.85f;

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
    private float standingHeight;
    private float standingCenterY;
    private float standingEyeHeight;
    private float stanceBlend;
    private bool isSliding;
    private float slideTimer;
    private float slideCooldownTimer;
    private Vector3 slideDirection;

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

        standingHeight = characterController.height;
        standingCenterY = characterController.center.y;
        standingEyeHeight = eyeHeight;

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

        RefreshMoveSpeed();
        HandleMouseLook();

        if (HandleDash())
        {
            UpdateStance();
            return;
        }

        if (HandleSlide())
        {
            UpdateStance();
            return;
        }

        UpdateStance();
        HandleMovement();
    }

    private static bool ShouldBlockMovementInput()
    {
        if (DevAdminPanel.IsOpen)
        {
            return true;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return true;
        }

        if (PauseMenuManager.IsGameplayPaused)
        {
            return true;
        }

        if (ChestRevealPause.IsPaused)
        {
            return true;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksGameplayPause)
        {
            return true;
        }

        return false;
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
            isSliding = false;
            slideTimer = 0f;
            slideCooldownTimer = 0f;
            stanceBlend = 0f;
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
            stanceBlend = 0f;
            isSliding = false;
            slideTimer = 0f;
            slideCooldownTimer = 0f;
            ResetStandingCollider();
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition = new Vector3(0f, standingEyeHeight, 0f);
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
            cameraTransform.localPosition = new Vector3(0f, standingEyeHeight + CrouchCameraOffset * stanceBlend, 0f);
            cameraTransform.localRotation = Quaternion.identity;
            pitch = 0f;
        }
    }

    private void RefreshMoveSpeed()
    {
        float chestMoveSpeedMultiplier = 1f;
        PlayerStats playerStats = GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            chestMoveSpeedMultiplier = playerStats.ChestMoveSpeedMultiplier;
        }

        // Always recomputed from baseMoveSpeed so relic add/clear never permanently stacks.
        moveSpeed = baseMoveSpeed
            * (1f + MetaProgressionManager.GetOrCreate().GetMoveSpeedBonusPercent())
            * RelicManager.MoveSpeedMultiplier
            * chestMoveSpeedMultiplier;
    }

    public void RefreshMoveSpeedFromStats()
    {
        RefreshMoveSpeed();
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

        if (!Input.GetKeyDown(KeyCode.LeftShift) || dashCooldownTimer > 0f || IsCrouchKeyHeld())
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

    private bool HandleSlide()
    {
        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            ApplyGravity();

            float slideSpeed = moveSpeed * SlideSpeedMultiplier;
            Vector3 slideMove = slideDirection * slideSpeed;
            slideMove.y = verticalVelocity;
            characterController.Move(slideMove * Time.deltaTime);

            if (slideTimer <= 0f)
            {
                EndSlide();
            }

            return true;
        }

        TryStartSlide();
        return false;
    }

    private void TryStartSlide()
    {
        if (ShouldBlockMovementInput()
            || characterController == null
            || !characterController.isGrounded
            || isDashing
            || slideCooldownTimer > 0f)
        {
            return;
        }

        if (!Input.GetKey(KeyCode.LeftShift) || !IsCrouchKeyHeld())
        {
            return;
        }

        Vector3 inputDirection = GetMoveInputDirection();

        if (inputDirection.sqrMagnitude < MinSlideInputThreshold * MinSlideInputThreshold)
        {
            return;
        }

        bool controlPressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.C);
        bool shiftPressed = Input.GetKeyDown(KeyCode.LeftShift);

        if (!controlPressed && !shiftPressed)
        {
            return;
        }

        slideDirection = inputDirection;
        isSliding = true;
        slideTimer = SlideDuration;
        horizontalVelocity = slideDirection * moveSpeed * SlideSpeedMultiplier;
    }

    private void EndSlide()
    {
        isSliding = false;
        slideCooldownTimer = SlideCooldown;

        float endSpeed = moveSpeed * SlideEndSpeedRetention;
        horizontalVelocity = slideDirection * endSpeed;

        if (horizontalVelocity.magnitude > moveSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * moveSpeed;
        }
    }

    private static bool IsCrouchKeyHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
    }

    private bool WantsCrouch()
    {
        if (ShouldBlockMovementInput())
        {
            return stanceBlend > 0.5f;
        }

        if (isSliding)
        {
            return true;
        }

        if (IsCrouchKeyHeld())
        {
            return true;
        }

        return !CanStandUp();
    }

    private void UpdateStance()
    {
        if (characterController == null || cameraTransform == null)
        {
            return;
        }

        float targetBlend = WantsCrouch() ? 1f : 0f;
        stanceBlend = Mathf.MoveTowards(stanceBlend, targetBlend, CrouchSmoothSpeed * Time.deltaTime);

        float targetHeight = Mathf.Lerp(standingHeight, standingHeight * CrouchHeightMultiplier, stanceBlend);
        ApplyControllerHeight(targetHeight);

        float targetEyeHeight = standingEyeHeight + CrouchCameraOffset * stanceBlend;
        Vector3 cameraLocalPosition = cameraTransform.localPosition;
        cameraLocalPosition.y = Mathf.Lerp(cameraLocalPosition.y, targetEyeHeight, 1f - Mathf.Exp(-CrouchSmoothSpeed * Time.deltaTime));
        cameraTransform.localPosition = cameraLocalPosition;
    }

    private void ApplyControllerHeight(float targetHeight)
    {
        float previousHeight = characterController.height;

        if (Mathf.Approximately(previousHeight, targetHeight))
        {
            return;
        }

        float bottomY = transform.position.y + characterController.center.y - previousHeight * 0.5f;
        characterController.height = targetHeight;
        characterController.center = new Vector3(0f, targetHeight * 0.5f, 0f);

        float newPositionY = bottomY + targetHeight * 0.5f - characterController.center.y;
        Vector3 position = transform.position;
        position.y = newPositionY;
        transform.position = position;
    }

    private void ResetStandingCollider()
    {
        if (characterController == null)
        {
            return;
        }

        characterController.height = standingHeight;
        characterController.center = new Vector3(0f, standingCenterY, 0f);
    }

    private bool CanStandUp()
    {
        if (characterController == null)
        {
            return true;
        }

        float radius = Mathf.Max(0.05f, characterController.radius - 0.05f);
        Vector3 sphereOrigin = transform.position + Vector3.up * (standingHeight - radius);
        return !Physics.CheckSphere(sphereOrigin, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    }

    private float GetCurrentMoveSpeed()
    {
        if (isSliding)
        {
            return moveSpeed * SlideSpeedMultiplier;
        }

        if (stanceBlend > 0.01f || WantsCrouch())
        {
            return moveSpeed * CrouchSpeedMultiplier;
        }

        return moveSpeed;
    }

    private void HandleMovement()
    {
        if (characterController == null) return;

        if (ShouldBlockMovementInput())
        {
            return;
        }

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
        float currentMoveSpeed = GetCurrentMoveSpeed();
        UpdateHorizontalVelocity(inputDirection, isGrounded, currentMoveSpeed);
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

    private void UpdateHorizontalVelocity(Vector3 inputDirection, bool isGrounded, float currentMoveSpeed)
    {
        if (isGrounded)
        {
            if (inputDirection.sqrMagnitude > 0.001f)
            {
                horizontalVelocity = inputDirection * currentMoveSpeed;
            }
            else
            {
                horizontalVelocity = Vector3.zero;
            }

            return;
        }

        Vector3 wishVelocity = inputDirection * currentMoveSpeed;
        Vector3 currentFlat = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        Vector3 nextFlat = Vector3.MoveTowards(
            currentFlat,
            wishVelocity,
            currentMoveSpeed * AirControlMultiplier * Time.deltaTime * 12f
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
