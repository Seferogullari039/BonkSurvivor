using System.Collections.Generic;
using UnityEngine;

public class ChainLightningWeapon : WeaponBase
{
    private const float ChainRange = 5f;
    private const float LineDuration = 0.12f;
    private const float FpsAimDistance = 40f;

    private Transform fireOrigin;

    public void Configure(Transform origin)
    {
        fireOrigin = origin;
        fireRate = 1.6f;
    }

    public override void Fire()
    {
        if (playerStats == null || !playerStats.ChainLightningUnlocked) return;
        if (fireOrigin == null) return;

        Vector3 originPoint;
        Transform firstTarget;

        if (FPSAimUtility.TryGetCameraAim(out Vector3 aimOrigin, out Vector3 aimDirection))
        {
            originPoint = aimOrigin;
            firstTarget = FPSAimUtility.FindEnemyAlongRay(aimOrigin, aimDirection, FpsAimDistance);
        }
        else
        {
            originPoint = fireOrigin.position + Vector3.up * 0.5f;
            firstTarget = FindClosestEnemyTransform(fireOrigin.position);
        }

        if (firstTarget == null) return;

        HashSet<int> hitEnemyIds = new HashSet<int>();
        List<Vector3> linePoints = new List<Vector3> { originPoint };

        Vector3 currentPosition = fireOrigin.position;
        Transform currentTarget = firstTarget;
        int maxTargets = playerStats.ChainLightningTargets;

        for (int i = 0; i < maxTargets && currentTarget != null; i++)
        {
            Enemy enemy = currentTarget.GetComponent<Enemy>();

            if (enemy == null) break;

            int enemyId = enemy.GetInstanceID();

            if (hitEnemyIds.Contains(enemyId)) break;

            hitEnemyIds.Add(enemyId);
            int chainDamage = playerStats.GetEffectiveDamageAgainst(enemy);
            RunStatsTracker.GetOrCreate().RecordDamageDealt("Chain Lightning", chainDamage);
            enemy.TakeDamage(chainDamage);
            linePoints.Add(currentTarget.position + Vector3.up * 0.45f);

            currentPosition = currentTarget.position;
            currentTarget = FindNextChainTarget(currentPosition, hitEnemyIds);
        }

        if (linePoints.Count >= 2)
        {
            SpawnLightningLine(linePoints);
        }
    }

    private Transform FindNextChainTarget(Vector3 fromPosition, HashSet<int> hitEnemyIds)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null) continue;

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null) continue;

            if (hitEnemyIds.Contains(enemy.GetInstanceID())) continue;

            float distance = Vector3.Distance(fromPosition, enemyObject.transform.position);

            if (distance > ChainRange || distance >= closestDistance) continue;

            closestDistance = distance;
            closest = enemyObject.transform;
        }

        return closest;
    }

    private static void SpawnLightningLine(List<Vector3> points)
    {
        // WeaponFxUtility.SpawnChainLightning(points, LineDuration);
    }
}
