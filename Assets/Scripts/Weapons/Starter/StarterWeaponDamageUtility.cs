using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class StarterWeaponDamageUtility
{
    private const float DefaultMaxRayDistance = 60f;

    public static bool TryGetAimGroundPoint(out Vector3 point, float maxDistance = DefaultMaxRayDistance)
    {
        point = Vector3.zero;

        if (!FPSAimUtility.TryGetCameraAim(out Vector3 origin, out Vector3 direction))
        {
            return false;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            point = hit.point;
            return true;
        }

        point = origin + direction.normalized * maxDistance;
        return true;
    }

    public static int GetBaseDamage(PlayerStats stats, int fallbackDamage = 1)
    {
        if (stats == null) return fallbackDamage;

        return Mathf.Max(1, stats.EffectiveDamage);
    }

    public static void DamageEnemy(Enemy enemy, int damage)
    {
        if (enemy == null || damage <= 0) return;

        TryApplyDamage(enemy, damage);
    }

    public static void DamageEnemiesInRadius(Vector3 center, float radius, int damage)
    {
        DamageEnemiesInRadiusWithCount(center, radius, damage);
    }

    public static int DamageEnemiesInRadiusWithCount(Vector3 center, float radius, int damage)
    {
        if (radius <= 0f || damage <= 0) return 0;

        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
        HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i];

            if (hitCollider == null) continue;
            if (hitCollider.CompareTag("Player")) continue;

            Enemy enemy = ResolveEnemy(hitCollider);

            if (enemy == null || damagedEnemies.Contains(enemy)) continue;

            damagedEnemies.Add(enemy);
            TryApplyDamage(enemy, damage);
        }

        return damagedEnemies.Count;
    }

    public static int DamageEnemiesInCone(
        Vector3 origin,
        Vector3 forward,
        float range,
        float halfAngleDegrees,
        int damage,
        int maxTargets = 8)
    {
        if (range <= 0f || damage <= 0 || forward.sqrMagnitude < 0.001f) return 0;

        forward.Normalize();

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int hitCount = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (hitCount >= maxTargets) break;

            GameObject enemyObject = enemies[i];

            if (enemyObject == null) continue;

            Vector3 toEnemy = enemyObject.transform.position + Vector3.up * 0.5f - origin;
            float distance = toEnemy.magnitude;

            if (distance > range || distance < 0.01f) continue;

            float angle = Vector3.Angle(forward, toEnemy);

            if (angle > halfAngleDegrees) continue;

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (enemy == null) continue;

            TryApplyDamage(enemy, damage);
            hitCount++;
        }

        return hitCount;
    }

    public static bool TryDamageSingleMeleeTarget(
        Transform aimCamera,
        float range,
        float assistRadius,
        int damage,
        out int candidateCount)
    {
        candidateCount = 0;

        if (aimCamera == null || range <= 0f || damage <= 0)
        {
            return false;
        }

        Vector3 origin = aimCamera.position;
        Vector3 forward = aimCamera.forward;

        if (Physics.Raycast(origin, forward, out RaycastHit hit, range + 1f, ~0, QueryTriggerInteraction.Collide))
        {
            Enemy rayEnemy = ResolveEnemy(hit.collider);

            if (rayEnemy != null)
            {
                candidateCount = 1;
                TryApplyDamage(rayEnemy, damage);
                LogLmbCombat(rayEnemy, damage, candidateCount);
                return true;
            }
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Enemy bestEnemy = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (enemy == null)
            {
                continue;
            }

            Vector3 aimPoint = enemyObject.transform.position + Vector3.up * 0.5f;
            Vector3 toEnemy = aimPoint - origin;
            float distance = toEnemy.magnitude;

            if (distance > range || distance < 0.01f)
            {
                continue;
            }

            float forwardDistance = Vector3.Dot(toEnemy, forward);

            if (forwardDistance <= 0f)
            {
                continue;
            }

            Vector3 closestPointOnRay = origin + forward * forwardDistance;
            float perpendicularDistance = Vector3.Distance(aimPoint, closestPointOnRay);

            if (perpendicularDistance > assistRadius)
            {
                continue;
            }

            candidateCount++;

            float angle = Vector3.Angle(forward, toEnemy);
            float dot = Vector3.Dot(forward, toEnemy.normalized);
            float score = angle - dot * 45f + distance * 0.08f;

            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestEnemy = enemy;
        }

        if (bestEnemy == null)
        {
            return false;
        }

        TryApplyDamage(bestEnemy, damage);
        LogLmbCombat(bestEnemy, damage, candidateCount);
        return true;
    }

    // Default off: [Combat] LMB diagnostics are opt-in only. Damage/targeting logic is unaffected.
    public static bool LogCombatDebug = false;

    public static void LogLmbCombat(Enemy enemy, int damage, int candidateCount)
    {
        if (!LogCombatDebug)
        {
            return;
        }

        string enemyName = enemy != null ? enemy.name : "none";
        Debug.Log("[Combat] LMB target=" + enemyName + " damage=" + damage);

        if (candidateCount > 1)
        {
            Debug.Log("[Combat] LMB candidates=" + candidateCount + " selected=" + enemyName);
        }
    }

    public static int DamageMeleeHybrid(
        Vector3 origin,
        Vector3 forward,
        float range,
        float halfAngleDegrees,
        float sphereRadius,
        int damage,
        int maxTargets = 10)
    {
        if (range <= 0f || damage <= 0 || forward.sqrMagnitude < 0.001f) return 0;

        forward.Normalize();
        HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();
        int hitCount = 0;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (int i = 0; i < enemies.Length; i++)
        {
            if (hitCount >= maxTargets) break;

            GameObject enemyObject = enemies[i];

            if (enemyObject == null) continue;

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (enemy == null || damagedEnemies.Contains(enemy)) continue;

            Vector3 toEnemy = enemyObject.transform.position + Vector3.up * 0.5f - origin;
            float distance = toEnemy.magnitude;

            if (distance > range || distance < 0.01f) continue;

            float angle = Vector3.Angle(forward, toEnemy);

            if (angle > halfAngleDegrees) continue;

            damagedEnemies.Add(enemy);
            TryApplyDamage(enemy, damage);
            hitCount++;
        }

        Vector3 sphereCenter = origin + forward * (range * 0.55f);
        sphereCenter.y = origin.y - 0.85f;

        Collider[] nearbyHits = Physics.OverlapSphere(sphereCenter, sphereRadius, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < nearbyHits.Length; i++)
        {
            if (hitCount >= maxTargets) break;

            Collider hitCollider = nearbyHits[i];

            if (hitCollider == null || hitCollider.CompareTag("Player")) continue;

            Enemy enemy = ResolveEnemy(hitCollider);

            if (enemy == null || damagedEnemies.Contains(enemy)) continue;

            damagedEnemies.Add(enemy);
            TryApplyDamage(enemy, damage);
            hitCount++;
        }

        return hitCount;
    }

    public static bool TryGetSkillTargetPoint(Transform aimCamera, out Vector3 targetPoint, float fallbackDistance = 18f)
    {
        if (TryGetAimGroundPoint(out targetPoint))
        {
            return true;
        }

        if (aimCamera == null)
        {
            targetPoint = Vector3.zero;
            return false;
        }

        targetPoint = aimCamera.position + aimCamera.forward * fallbackDistance;
        return true;
    }

    private static Enemy ResolveEnemy(Collider hitCollider)
    {
        if (hitCollider == null) return null;

        Enemy enemy = hitCollider.GetComponent<Enemy>();

        if (enemy != null) return enemy;

        return hitCollider.GetComponentInParent<Enemy>();
    }

    private static void TryApplyDamage(Enemy enemy, int damage, string sourceName = "Starter Weapon")
    {
        if (enemy == null || damage <= 0) return;

        // Hunter Mark: elite-only bonus applied once at this terminal funnel.
        // damage already includes Sharp Fang (EffectiveDamage), so no double-dip.
        int finalDamage = damage;

        if (enemy.IsElite)
        {
            finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * RelicManager.EliteDamageMultiplier));
        }

        RunStatsTracker.GetOrCreate().RecordDamageDealt(sourceName, finalDamage);

        MethodInfo takeDamageInt = typeof(Enemy).GetMethod(
            "TakeDamage",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(int) },
            null);

        if (takeDamageInt != null)
        {
            takeDamageInt.Invoke(enemy, new object[] { finalDamage });
            return;
        }

        MethodInfo takeDamageFloat = typeof(Enemy).GetMethod(
            "TakeDamage",
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(float) },
            null);

        if (takeDamageFloat != null)
        {
            takeDamageFloat.Invoke(enemy, new object[] { (float)finalDamage });
        }
    }
}
