using UnityEngine;

public static class FPSAimUtility
{
    private const float DefaultAimAssistAngle = 4f;
    private const float DefaultMaxDistance = 60f;

    public static bool TryGetCameraAim(out Vector3 origin, out Vector3 direction)
    {
        origin = Vector3.zero;
        direction = Vector3.forward;

        if (!FPSPlayerController.IsFpsModeActive) return false;

        Camera camera = Camera.main;

        if (camera == null) return false;

        origin = camera.transform.position;
        direction = camera.transform.forward;

        return direction.sqrMagnitude > 0.001f;
    }

    public static bool TryGetAimDirection(out Vector3 direction, float aimAssistAngle = DefaultAimAssistAngle)
    {
        direction = Vector3.forward;

        if (!TryGetCameraAim(out Vector3 origin, out direction)) return false;

        direction.Normalize();

        Transform assistedTarget = FindEnemyAlongRay(origin, direction, DefaultMaxDistance, aimAssistAngle);

        if (assistedTarget != null)
        {
            Vector3 toTarget = assistedTarget.position + Vector3.up * 0.5f - origin;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                direction = toTarget.normalized;
            }
        }

        return true;
    }

    public static bool IsEnemyInCrosshair(float maxDistance = DefaultMaxDistance, float maxAngle = DefaultAimAssistAngle)
    {
        if (!TryGetCameraAim(out Vector3 origin, out Vector3 direction)) return false;

        direction.Normalize();

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                return true;
            }
        }

        return FindEnemyAlongRay(origin, direction, maxDistance, maxAngle) != null;
    }

    public static Transform FindEnemyAlongRay(Vector3 origin, Vector3 direction, float maxDistance, float maxAngle = DefaultAimAssistAngle)
    {
        if (direction.sqrMagnitude < 0.001f) return null;

        direction.Normalize();

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            if (hit.collider != null && hit.collider.CompareTag("Enemy"))
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>() ?? hit.collider.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    return enemy.transform;
                }
            }
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform bestTarget = null;
        float bestAngle = maxAngle;
        float bestDistance = maxDistance;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null) continue;

            Vector3 toEnemy = enemyObject.transform.position + Vector3.up * 0.5f - origin;
            float distance = toEnemy.magnitude;

            if (distance > maxDistance || distance < 0.01f) continue;

            float angle = Vector3.Angle(direction, toEnemy);

            if (angle > maxAngle) continue;

            if (angle < bestAngle - 0.001f || (Mathf.Approximately(angle, bestAngle) && distance < bestDistance))
            {
                bestAngle = angle;
                bestDistance = distance;
                bestTarget = enemyObject.transform;
            }
        }

        return bestTarget;
    }
}
