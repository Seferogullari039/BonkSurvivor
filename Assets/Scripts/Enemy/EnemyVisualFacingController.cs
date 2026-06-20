using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public class EnemyVisualFacingController : MonoBehaviour
{
    [SerializeField] private Transform visualPivot;
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private bool useMovementDirectionFallback = true;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private Vector3 localEulerOffset = Vector3.zero;
    [SerializeField] private bool lockPitchRoll = true;
    [SerializeField] private float minDirectionMagnitude = 0.02f;
    [SerializeField] private bool debugFacing = true;

    private const float TargetSearchInterval = 1f;
    private const float DebugLogInterval = 3f;

    private Transform movementRoot;
    private Transform cachedTargetTransform;
    private float nextTargetSearchTime;
    private float nextDebugLogTime;
    private Vector3 lastMovementSamplePosition;
    private Vector3 cachedMoveDirection = Vector3.forward;
    private bool hasMovementSample;
    private bool initialized;
    private bool refusedEnemyRoot;

    private void Awake()
    {
        TryInitialize();
    }

    private void LateUpdate()
    {
        if (refusedEnemyRoot || !TryInitialize() || visualPivot == null)
        {
            return;
        }

        if (!TryResolveFacingDirection(out Vector3 worldDirection))
        {
            MaybeLogDebug(false, worldDirection, 0f, 0f);
            return;
        }

        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < minDirectionMagnitude * minDirectionMagnitude)
        {
            MaybeLogDebug(cachedTargetTransform != null, worldDirection, 0f, 0f);
            return;
        }

        worldDirection.Normalize();
        ApplyFacingRotation(worldDirection);
    }

    private bool TryInitialize()
    {
        if (refusedEnemyRoot)
        {
            return false;
        }

        if (initialized)
        {
            return visualPivot != null;
        }

        if (visualPivot == null)
        {
            visualPivot = transform;
        }

        if (visualPivot != null && visualPivot.GetComponent<Enemy>() != null)
        {
            Debug.LogWarning("[EnemyVisualFacing] Refusing to rotate enemy root.", this);
            refusedEnemyRoot = true;
            enabled = false;
            return false;
        }

        movementRoot = ResolveMovementRoot();

        if (movementRoot != null)
        {
            lastMovementSamplePosition = movementRoot.position;
            hasMovementSample = true;
        }

        initialized = true;
        return visualPivot != null;
    }

    private void ApplyFacingRotation(Vector3 worldDirection)
    {
        Quaternion targetWorldRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
        Quaternion finalRotation = targetWorldRotation * Quaternion.Euler(localEulerOffset);

        if (lockPitchRoll)
        {
            float yaw = finalRotation.eulerAngles.y;
            finalRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        visualPivot.rotation = Quaternion.Slerp(
            visualPivot.rotation,
            finalRotation,
            Time.deltaTime * turnSpeed);

        MaybeLogDebug(
            cachedTargetTransform != null,
            worldDirection,
            visualPivot.rotation.eulerAngles.y,
            finalRotation.eulerAngles.y);
    }

    private bool TryResolveFacingDirection(out Vector3 worldDirection)
    {
        worldDirection = Vector3.zero;
        Vector3 origin = movementRoot != null ? movementRoot.position : visualPivot.position;

        if (facePlayer)
        {
            Transform targetTransform = GetTargetTransform();

            if (targetTransform != null)
            {
                worldDirection = targetTransform.position - origin;
                worldDirection.y = 0f;

                if (worldDirection.sqrMagnitude >= minDirectionMagnitude * minDirectionMagnitude)
                {
                    return true;
                }
            }
        }

        if (!useMovementDirectionFallback || movementRoot == null)
        {
            return false;
        }

        UpdateMovementDirection();

        if (hasMovementSample && cachedMoveDirection.sqrMagnitude > minDirectionMagnitude * minDirectionMagnitude)
        {
            worldDirection = cachedMoveDirection;
            return true;
        }

        return false;
    }

    private Transform GetTargetTransform()
    {
        if (cachedTargetTransform != null)
        {
            return cachedTargetTransform;
        }

        if (Time.unscaledTime < nextTargetSearchTime)
        {
            return null;
        }

        cachedTargetTransform = ResolveTargetTransform();
        nextTargetSearchTime = Time.unscaledTime + TargetSearchInterval;
        return cachedTargetTransform;
    }

    private static Transform ResolveTargetTransform()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");

        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        FPSPlayerController fpsController = Object.FindFirstObjectByType<FPSPlayerController>();

        if (fpsController != null)
        {
            return fpsController.transform;
        }

        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();

        if (playerController != null)
        {
            return playerController.transform;
        }

        CharacterController[] characterControllers = Object.FindObjectsByType<CharacterController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < characterControllers.Length; i++)
        {
            CharacterController controller = characterControllers[i];

            if (controller == null || controller.GetComponentInParent<Enemy>() != null)
            {
                continue;
            }

            return controller.transform;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return null;
    }

    private void UpdateMovementDirection()
    {
        if (movementRoot == null)
        {
            return;
        }

        Vector3 worldDelta = movementRoot.position - lastMovementSamplePosition;
        lastMovementSamplePosition = movementRoot.position;
        worldDelta.y = 0f;

        if (worldDelta.sqrMagnitude >= minDirectionMagnitude * minDirectionMagnitude)
        {
            cachedMoveDirection = worldDelta.normalized;
            hasMovementSample = true;
        }
    }

    private Transform ResolveMovementRoot()
    {
        Transform current = transform;
        Transform fallback = transform;

        while (current != null)
        {
            if (current.GetComponent<Enemy>() != null)
            {
                return current;
            }

            fallback = current;
            current = current.parent;
        }

        return fallback;
    }

    private void MaybeLogDebug(bool targetFound, Vector3 direction, float currentYaw, float targetYaw)
    {
        if (!ShouldDebugLog())
        {
            return;
        }

        if (Time.unscaledTime < nextDebugLogTime)
        {
            return;
        }

        nextDebugLogTime = Time.unscaledTime + DebugLogInterval;

        string targetName = cachedTargetTransform != null ? cachedTargetTransform.name : "none";
        string pivotName = visualPivot != null ? visualPivot.name : "null";
        string rootName = movementRoot != null ? movementRoot.name : "null";

        Debug.Log(
            "[EnemyVisualFacing] target="
            + (targetFound ? targetName : "missing")
            + " pivot="
            + pivotName
            + " movementRoot="
            + rootName
            + " dirMag="
            + direction.magnitude.ToString("F3")
            + " currentYaw="
            + currentYaw.ToString("F1")
            + " targetYaw="
            + targetYaw.ToString("F1")
            + " offset="
            + localEulerOffset
            + " dir="
            + direction,
            this);
    }

    private bool ShouldDebugLog()
    {
#if UNITY_EDITOR
        return debugFacing;
#else
        return debugFacing && Debug.isDebugBuild;
#endif
    }
}
