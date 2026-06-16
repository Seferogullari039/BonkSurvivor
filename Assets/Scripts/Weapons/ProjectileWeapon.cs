using UnityEngine;

public class ProjectileWeapon : WeaponBase
{
    private GameObject projectilePrefab;
    private Transform fireOrigin;

    public void Configure(GameObject prefab, float startingFireRate, Transform origin, float startingProjectileSpeed = 12f)
    {
        projectilePrefab = prefab;
        fireRate = startingFireRate;
        fireOrigin = origin;
        projectileSpeed = startingProjectileSpeed;
    }

    public override void Fire()
    {
        if (FPSPlayerController.IsFpsModeActive) return;
        if (projectilePrefab == null || fireOrigin == null) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return;

        GameObject closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(fireOrigin.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy == null) return;

        Vector3 direction = closestEnemy.transform.position - fireOrigin.position;
        direction.y = 0f;
        direction.Normalize();

        if (playerStats != null && playerStats.SpreadShotUnlocked)
        {
            float angle = playerStats.SpreadAngle;
            SpawnProjectile(direction);
            SpawnProjectile(RotateDirection(direction, -angle));
            SpawnProjectile(RotateDirection(direction, angle));
            return;
        }

        SpawnProjectile(direction);
    }

    private Vector3 RotateDirection(Vector3 dir, float degrees)
    {
        return (Quaternion.Euler(0f, degrees, 0f) * dir).normalized;
    }

    private void SpawnProjectile(Vector3 direction)
    {
        GameObject projectile = Object.Instantiate(
            projectilePrefab,
            fireOrigin.position + Vector3.up * 0.5f,
            Quaternion.identity
        );

        Projectile projectileScript = projectile.GetComponent<Projectile>();

        if (projectileScript != null)
        {
            projectileScript.Initialize(direction);
        }
    }
}
