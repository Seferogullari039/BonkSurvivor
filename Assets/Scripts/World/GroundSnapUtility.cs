using UnityEngine;

public static class GroundSnapUtility
{
    private const float RayOriginHeight = 4f;
    private const float RayDistance = 8f;

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[12];

    public static bool TryGetGroundPoint(Vector3 worldPosition, Collider ignoreCollider, out Vector3 groundPoint)
    {
        Vector3 origin = worldPosition + Vector3.up * RayOriginHeight;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            HitBuffer,
            RayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        bool found = false;
        groundPoint = worldPosition;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = HitBuffer[i].collider;

            if (hitCollider == null || ShouldIgnoreCollider(hitCollider, ignoreCollider))
            {
                continue;
            }

            if (HitBuffer[i].distance < bestDistance)
            {
                bestDistance = HitBuffer[i].distance;
                groundPoint = HitBuffer[i].point;
                found = true;
            }
        }

        return found;
    }

    public static bool TryGetGroundY(
        Vector3 worldPosition,
        float footOffset,
        out float groundY,
        Collider ignoreCollider = null)
    {
        if (TryGetGroundPoint(worldPosition, ignoreCollider, out Vector3 groundPoint))
        {
            groundY = groundPoint.y + footOffset;
            return true;
        }

        groundY = worldPosition.y;
        return false;
    }

    public static bool TryGetLootSpawnPosition(
        Vector3 worldPosition,
        float heightOffset,
        Collider ignoreCollider,
        out Vector3 spawnPosition)
    {
        if (TryGetGroundPoint(worldPosition, ignoreCollider, out Vector3 groundPoint))
        {
            spawnPosition = groundPoint + Vector3.up * heightOffset;
            return true;
        }

        spawnPosition = worldPosition;
        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(heightOffset);
        return false;
    }

    private static bool ShouldIgnoreCollider(Collider hitCollider, Collider ignoreCollider)
    {
        if (hitCollider.isTrigger)
        {
            return true;
        }

        if (ignoreCollider != null && hitCollider == ignoreCollider)
        {
            return true;
        }

        if (hitCollider.CompareTag("Enemy") || hitCollider.CompareTag("Player"))
        {
            return true;
        }

        return false;
    }
}
