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

    private Transform movementRoot;
    private Transform playerTransform;
    private Vector3 lastMovementSamplePosition;
    private Vector3 cachedMoveDirection = Vector3.forward;
    private bool hasMovementSample;
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void LateUpdate()
    {
        if (!TryInitialize() || visualPivot == null)
        {
            return;
        }

        if (!TryResolveFacingDirection(out Vector3 worldDirection))
        {
            return;
        }

        worldDirection.y = 0f;

        if (worldDirection.sqrMagnitude < minDirectionMagnitude * minDirectionMagnitude)
        {
            return;
        }

        worldDirection.Normalize();
        ApplyFacingRotation(worldDirection);
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return visualPivot != null;
        }

        if (visualPivot == null)
        {
            visualPivot = transform;
        }

        movementRoot = ResolveMovementRoot();
        playerTransform = ResolvePlayerTransform();

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
        float targetYaw = Mathf.Atan2(worldDirection.x, worldDirection.z) * Mathf.Rad2Deg;
        Quaternion targetLocalRotation;

        if (lockPitchRoll)
        {
            targetLocalRotation = Quaternion.Euler(
                localEulerOffset.x,
                targetYaw + localEulerOffset.y,
                localEulerOffset.z);
        }
        else
        {
            Quaternion worldRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            Transform parent = visualPivot.parent;

            if (parent != null)
            {
                worldRotation = Quaternion.Inverse(parent.rotation) * worldRotation;
            }

            targetLocalRotation = worldRotation * Quaternion.Euler(localEulerOffset);
        }

        visualPivot.localRotation = Quaternion.Slerp(
            visualPivot.localRotation,
            targetLocalRotation,
            Time.deltaTime * turnSpeed);
    }

    private bool TryResolveFacingDirection(out Vector3 worldDirection)
    {
        worldDirection = Vector3.zero;

        if (facePlayer)
        {
            playerTransform = ResolvePlayerTransform();

            if (playerTransform != null)
            {
                worldDirection = playerTransform.position - visualPivot.position;
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

    private void UpdateMovementDirection()
    {
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

        while (current != null)
        {
            if (current.GetComponent<Enemy>() != null)
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static Transform ResolvePlayerTransform()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        return playerObject != null ? playerObject.transform : null;
    }
}
