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

    public static void BindToViewInstance(
        GameObject viewInstance,
        Transform enemyMovementRoot,
        Enemy.EnemyType enemyType)
    {
        if (viewInstance == null || enemyMovementRoot == null)
        {
            return;
        }

        Transform pivot = ResolveViewFacingPivot(viewInstance.transform, enemyType);

        if (pivot != null && pivot.GetComponent<Enemy>() != null)
        {
            Debug.LogWarning("[EnemyVisualFacing] Refusing to rotate enemy root.", viewInstance);
            return;
        }

        EnemyVisualFacingController facing = viewInstance.GetComponentInChildren<EnemyVisualFacingController>(true);

        if (facing == null)
        {
            GameObject host = pivot != null ? pivot.gameObject : viewInstance;
            facing = host.AddComponent<EnemyVisualFacingController>();
        }

        facing.Configure(pivot, enemyMovementRoot, ResolveFacingOffset(enemyType));
    }

    public void Configure(Transform pivot, Transform enemyMovementRoot, Vector3 eulerOffset)
    {
        if (pivot != null && pivot.GetComponent<Enemy>() != null)
        {
            Debug.LogWarning("[EnemyVisualFacing] Refusing to rotate enemy root.", this);
            refusedEnemyRoot = true;
            enabled = false;
            return;
        }

        visualPivot = pivot != null ? pivot : transform;
        movementRoot = enemyMovementRoot;
        localEulerOffset = eulerOffset;
        cachedTargetTransform = null;
        nextTargetSearchTime = 0f;
        initialized = false;
        refusedEnemyRoot = false;
        enabled = true;
        TryInitialize();
    }

    private void Awake()
    {
        TryInitialize();
    }

    private void Start()
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
            MaybeLogDebug(false, worldDirection, visualPivot.rotation.eulerAngles.y, 0f);
            return;
        }

        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < minDirectionMagnitude * minDirectionMagnitude)
        {
            MaybeLogDebug(cachedTargetTransform != null, worldDirection, visualPivot.rotation.eulerAngles.y, 0f);
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

        if (visualPivot == null)
        {
            visualPivot = ResolveVisualPivotFallback(transform);
        }

        if (visualPivot != null && visualPivot.GetComponent<Enemy>() != null)
        {
            Debug.LogWarning("[EnemyVisualFacing] Refusing to rotate enemy root.", this);
            refusedEnemyRoot = true;
            enabled = false;
            return false;
        }

        if (movementRoot == null)
        {
            movementRoot = ResolveMovementRootFallback();
        }

        if (!initialized)
        {
            if (movementRoot != null)
            {
                lastMovementSamplePosition = movementRoot.position;
                hasMovementSample = true;
            }

            initialized = true;
        }

        return visualPivot != null;
    }

    private void ApplyFacingRotation(Vector3 worldDirection)
    {
        float pivotYawBefore = visualPivot.rotation.eulerAngles.y;

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
            pivotYawBefore,
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

    private Transform ResolveMovementRootFallback()
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

    private static Transform ResolveVisualPivotFallback(Transform start)
    {
        Transform slimeMotion = FindDeepChild(start, "SlimeMotionRoot");

        if (slimeMotion != null)
        {
            return slimeMotion;
        }

        Transform visualRoot = FindDeepChild(start, "VisualRoot");

        if (visualRoot != null)
        {
            return visualRoot;
        }

        return start;
    }

    public static Transform ResolveViewFacingPivot(Transform viewRoot, Enemy.EnemyType enemyType)
    {
        if (viewRoot == null)
        {
            return null;
        }

        if (enemyType == Enemy.EnemyType.Normal)
        {
            Transform slimeMotion = FindDeepChild(viewRoot, "SlimeMotionRoot");

            if (slimeMotion != null)
            {
                return slimeMotion;
            }
        }

        Transform visualRoot = FindDeepChild(viewRoot, "VisualRoot");

        if (visualRoot != null)
        {
            return visualRoot;
        }

        return viewRoot;
    }

    public static Vector3 ResolveFacingOffset(Enemy.EnemyType enemyType)
    {
        if (enemyType == Enemy.EnemyType.Normal)
        {
            return new Vector3(0f, 120f, 0f);
        }

        if (enemyType == Enemy.EnemyType.Tank)
        {
            return new Vector3(0f, 90f, 0f);
        }

        return Vector3.zero;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void MaybeLogDebug(bool targetFound, Vector3 direction, float pivotYaw, float targetYaw)
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

        float currentPivotYaw = visualPivot != null ? visualPivot.rotation.eulerAngles.y : 0f;
        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(currentPivotYaw, targetYaw));
        string targetName = cachedTargetTransform != null ? cachedTargetTransform.name : "missing";
        string pivotName = visualPivot != null ? visualPivot.name : "null";
        string rootName = movementRoot != null ? movementRoot.name : "null";

        Debug.Log(
            "[EnemyVisualFacing] enabled="
            + enabled
            + " target="
            + (targetFound ? targetName : "missing")
            + " pivot="
            + pivotName
            + " movementRoot="
            + rootName
            + " dirMag="
            + direction.magnitude.ToString("F3")
            + " pivotYaw="
            + currentPivotYaw.ToString("F1")
            + " targetYaw="
            + targetYaw.ToString("F1")
            + " yawDelta="
            + yawDelta.ToString("F1")
            + " offset="
            + localEulerOffset,
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
