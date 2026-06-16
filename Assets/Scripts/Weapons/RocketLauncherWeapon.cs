using UnityEngine;

public class RocketLauncherWeapon : WeaponBase
{
    private Transform fireOrigin;
    private AudioClip explosionClip;
    private GameObject rocketPrefab;

    public void Configure(Transform origin, AudioClip rocketExplosionClip = null, GameObject prefab = null)
    {
        fireOrigin = origin;
        fireRate = 2.2f;
        explosionClip = rocketExplosionClip;
        rocketPrefab = prefab;
    }

    public override void Fire()
    {
        if (playerStats == null || !playerStats.RocketLauncherUnlocked) return;
        if (fireOrigin == null) return;

        Vector3 spawnPosition;
        Vector3 direction;

        if (FPSAimUtility.TryGetCameraAim(out Vector3 aimOrigin, out Vector3 cameraForward))
        {
            if (FPSPlayerController.IsFpsModeActive && FPSAimUtility.TryGetAimDirection(out Vector3 aimDirection))
            {
                direction = aimDirection;
            }
            else
            {
                direction = cameraForward.normalized;
            }

            spawnPosition = aimOrigin + direction * 0.5f;
        }
        else
        {
            Transform target = FindClosestEnemyTransform(fireOrigin.position);

            if (target == null) return;

            direction = target.position - fireOrigin.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f) return;

            direction.Normalize();
            spawnPosition = fireOrigin.position + Vector3.up * 0.5f;
        }

        GameObject rocketObject;
        RocketProjectile rocket;

        if (rocketPrefab != null)
        {
            rocketObject = Object.Instantiate(rocketPrefab, spawnPosition, Quaternion.identity);
            rocket = rocketObject.GetComponent<RocketProjectile>();

            if (rocket == null)
            {
                rocket = rocketObject.AddComponent<RocketProjectile>();
            }

            Renderer prefabRenderer = rocketObject.GetComponent<Renderer>();

            if (prefabRenderer != null)
            {
                GameVisualStyle.ApplyColor(prefabRenderer, new Color(1f, 0.45f, 0.1f), 0.7f, true);
            }
        }
        else
        {
            rocketObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rocketObject.name = "Rocket";
            rocketObject.transform.position = spawnPosition;
            rocketObject.transform.localScale = Vector3.one * 0.28f;

            Collider defaultCollider = rocketObject.GetComponent<Collider>();

            if (defaultCollider != null)
            {
                Object.Destroy(defaultCollider);
            }

            SphereCollider triggerCollider = rocketObject.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.radius = 0.5f;

            Renderer renderer = rocketObject.GetComponent<Renderer>();

            if (renderer != null)
            {
                GameVisualStyle.ApplyColor(renderer, new Color(1f, 0.45f, 0.1f), 0.7f, true);
            }

            rocket = rocketObject.AddComponent<RocketProjectile>();
            AudioClip resolvedExplosionClip = explosionClip != null
                ? explosionClip
                : WeaponManager.DefaultRocketExplosionClip;
            rocket.Initialize(direction, playerStats, resolvedExplosionClip);
            return;
        }

        rocket.Initialize(direction, playerStats);
    }
}
