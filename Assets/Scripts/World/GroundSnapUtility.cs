using UnityEngine;

public static class GroundSnapUtility
{
    private const float RayOriginHeight = 4f;
    private const float RayDistance = 16f;
    private const float MinGroundNormalY = 0.35f;

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[24];

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
            RaycastHit hit = HitBuffer[i];

            if (!IsValidGroundHit(hit, ignoreCollider))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                groundPoint = hit.point;
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
        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(worldPosition, heightOffset);
        return false;
    }

    private static bool IsValidGroundHit(RaycastHit hit, Collider ignoreCollider)
    {
        if (hit.collider == null)
        {
            return false;
        }

        if (hit.normal.y < MinGroundNormalY)
        {
            return false;
        }

        return !ShouldIgnoreCollider(hit.collider, ignoreCollider);
    }

    private static bool ShouldIgnoreCollider(Collider hitCollider, Collider ignoreCollider)
    {
        if (hitCollider.isTrigger)
        {
            return true;
        }

        if (ignoreCollider != null && IsSameColliderHierarchy(hitCollider, ignoreCollider))
        {
            return true;
        }

        if (hitCollider.CompareTag("Enemy") || hitCollider.CompareTag("Player"))
        {
            return true;
        }

        if (hitCollider.GetComponentInParent<Enemy>() != null)
        {
            return true;
        }

        if (hitCollider.GetComponentInParent<FPSPlayerController>() != null)
        {
            return true;
        }

        if (hitCollider.GetComponentInParent<Chest>() != null
            || hitCollider.GetComponentInParent<MimicChestController>() != null)
        {
            return true;
        }

        if (hitCollider.GetComponentInParent<XPOrb>() != null
            || hitCollider.GetComponentInParent<Coin>() != null
            || hitCollider.GetComponentInParent<HeartPickup>() != null)
        {
            return true;
        }

        if (IsDecorativeWorldCollider(hitCollider))
        {
            return true;
        }

        return false;
    }

    private static bool IsDecorativeWorldCollider(Collider hitCollider)
    {
        Transform current = hitCollider.transform;

        while (current != null)
        {
            string objectName = current.name;

            if (objectName == "Trees"
                || objectName == "Rocks"
                || objectName == "Bushes"
                || objectName == "Logs")
            {
                return true;
            }

            if (objectName.StartsWith("TreeTrunk_")
                || objectName.StartsWith("TreeLeaf_")
                || objectName.StartsWith("Rock_")
                || objectName.StartsWith("RockCluster_")
                || objectName.StartsWith("Bush_")
                || objectName.StartsWith("Log_")
                || objectName.StartsWith("Landmark_"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsSameColliderHierarchy(Collider hitCollider, Collider ignoreCollider)
    {
        if (hitCollider == ignoreCollider)
        {
            return true;
        }

        Transform hitTransform = hitCollider.transform;
        Transform ignoreTransform = ignoreCollider.transform;

        return hitTransform.IsChildOf(ignoreTransform) || ignoreTransform.IsChildOf(hitTransform);
    }
}
