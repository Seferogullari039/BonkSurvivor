using UnityEngine;

public class StarterWeaponProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float lifeTime;
    private int damage;
    private bool explodeOnHit;
    private float explosionRadius;
    private float impactRadius;
    private bool spawnImpactEffect;
    private bool useMegaMeteorImpactFx;
    private bool initialized;
    private bool hasImpacted;

    public void Initialize(
        Vector3 shootDirection,
        float projectileSpeed,
        int projectileDamage,
        float projectileLifeTime,
        bool shouldExplodeOnHit,
        float aoeRadius,
        float landingImpactRadius = 0f,
        bool showImpactEffect = false,
        bool useMegaMeteorVisual = false,
        bool useMegaMeteorImpact = false)
    {
        if (shootDirection.sqrMagnitude < 0.001f)
        {
            shootDirection = Vector3.forward;
        }

        direction = shootDirection.normalized;
        speed = Mathf.Max(1f, projectileSpeed);
        damage = Mathf.Max(1, projectileDamage);
        lifeTime = Mathf.Max(0.25f, projectileLifeTime);
        explodeOnHit = shouldExplodeOnHit;
        explosionRadius = Mathf.Max(0.5f, aoeRadius);
        impactRadius = Mathf.Max(0f, landingImpactRadius);
        spawnImpactEffect = showImpactEffect && !useMegaMeteorImpact;
        useMegaMeteorImpactFx = useMegaMeteorImpact;
        initialized = true;
        hasImpacted = false;

        EnsurePhysics(useMegaMeteorVisual ? Mathf.Max(0.35f, landingImpactRadius * 0.08f) : 0.22f);
        IgnorePlayerCollisions();
        CancelInvoke(nameof(Expire));
        Invoke(nameof(Expire), lifeTime);
    }

    private void EnsurePhysics(float triggerRadius = 0.22f)
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        sphereCollider.isTrigger = true;
        sphereCollider.radius = triggerRadius;

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
    }

    private void IgnorePlayerCollisions()
    {
        Collider projectileCollider = GetComponent<Collider>();

        if (projectileCollider == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];

            if (playerCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, playerCollider, true);
            }
        }
    }

    private void Update()
    {
        if (!initialized || hasImpacted) return;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || hasImpacted) return;
        if (other == null || other.CompareTag("Player")) return;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return;

        if (explodeOnHit)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Explode(hitPoint);
            return;
        }

        if (impactRadius > 0f)
        {
            Impact(other.ClosestPoint(transform.position));
            return;
        }

        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>() ?? other.GetComponentInParent<Enemy>();

        if (enemy == null) return;

        hasImpacted = true;
        initialized = false;
        CancelInvoke(nameof(Expire));
        StarterWeaponDamageUtility.DamageEnemy(enemy, damage);
        StarterWeaponDamageUtility.LogLmbCombat(enemy, damage, 1);
        Destroy(gameObject);
    }

    private void Expire()
    {
        if (hasImpacted) return;

        if (explodeOnHit)
        {
            Explode(transform.position);
            return;
        }

        if (impactRadius > 0f)
        {
            Impact(transform.position);
            return;
        }

        Destroy(gameObject);
    }

    private void Explode(Vector3 explosionPoint)
    {
        if (hasImpacted) return;

        hasImpacted = true;
        initialized = false;
        CancelInvoke(nameof(Expire));

        StarterWeaponDamageUtility.DamageEnemiesInRadius(explosionPoint, explosionRadius, damage);

        if (spawnImpactEffect)
        {
            SpawnImpactVisual(explosionPoint, explosionRadius);
        }

        Destroy(gameObject);
    }

    private void Impact(Vector3 impactPoint)
    {
        if (hasImpacted) return;

        hasImpacted = true;
        initialized = false;
        CancelInvoke(nameof(Expire));

        StarterWeaponDamageUtility.DamageEnemiesInRadius(impactPoint, impactRadius, damage);

        if (spawnImpactEffect)
        {
            SpawnImpactVisual(impactPoint, impactRadius);
        }
        else if (useMegaMeteorImpactFx)
        {
            SpawnMegaMeteorImpactVisual(impactPoint, impactRadius);
        }

        Destroy(gameObject);
    }

    public static StarterWeaponProjectile SpawnRuntimeProjectile(
        Vector3 position,
        Vector3 shootDirection,
        Color color,
        float scale,
        float projectileSpeed,
        int projectileDamage,
        float projectileLifeTime,
        bool shouldExplodeOnHit,
        float aoeRadius,
        float landingImpactRadius = 0f,
        bool useArrowVisual = false,
        bool useTrailVisual = false,
        bool showImpactEffect = false,
        bool useMegaMeteorVisual = false,
        bool useMegaMeteorImpact = false)
    {
        GameObject projectileObject;

        if (useArrowVisual)
        {
            projectileObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            projectileObject.name = "StarterRainArrow";
            projectileObject.transform.localScale = new Vector3(scale * 0.18f, scale * 1.15f, scale * 0.18f);
        }
        else if (useMegaMeteorVisual)
        {
            projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = "StarterMegaMeteor";
            float meteorScale = Mathf.Max(0.45f, scale);
            projectileObject.transform.localScale = Vector3.one * meteorScale;
        }
        else
        {
            projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileObject.name = shouldExplodeOnHit ? "StarterFireball" : "StarterArrow";
            projectileObject.transform.localScale = Vector3.one * Mathf.Max(0.08f, scale);
        }

        projectileObject.transform.position = position;

        if (shootDirection.sqrMagnitude > 0.001f)
        {
            projectileObject.transform.rotation = Quaternion.LookRotation(shootDirection.normalized);
        }

        Collider collider = projectileObject.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = true;
        }

        Renderer renderer = projectileObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            bool glow = shouldExplodeOnHit || useMegaMeteorVisual;
            float emission = useMegaMeteorVisual ? 0.75f : shouldExplodeOnHit ? 0.35f : 0.1f;
            GameVisualStyle.ApplyColor(renderer, color, 0.45f, glow, emission);
        }

        if (useMegaMeteorVisual)
        {
            CreateMegaMeteorTrail(projectileObject.transform, color, scale);
            projectileObject.AddComponent<MegaMeteorFallShake>();
        }
        else if (useTrailVisual)
        {
            CreateTrailStreak(projectileObject.transform, color, scale);
        }

        StarterWeaponProjectile projectile = projectileObject.AddComponent<StarterWeaponProjectile>();
        projectile.Initialize(
            shootDirection,
            projectileSpeed,
            projectileDamage,
            projectileLifeTime,
            shouldExplodeOnHit,
            aoeRadius,
            landingImpactRadius,
            showImpactEffect,
            useMegaMeteorVisual,
            useMegaMeteorImpact);

        return projectile;
    }

    private static void CreateMegaMeteorTrail(Transform meteorTransform, Color color, float scale)
    {
        if (meteorTransform == null) return;

        GameObject trailObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trailObject.name = "MegaMeteorTrail";
        trailObject.transform.SetParent(meteorTransform, false);
        trailObject.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        trailObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        trailObject.transform.localScale = new Vector3(scale * 0.22f, scale * 1.35f, scale * 0.22f);

        Collider trailCollider = trailObject.GetComponent<Collider>();

        if (trailCollider != null)
        {
            trailCollider.enabled = false;
        }

        Renderer trailRenderer = trailObject.GetComponent<Renderer>();

        if (trailRenderer != null)
        {
            Color streakColor = Color.Lerp(color, new Color(1f, 0.85f, 0.2f), 0.65f);
            GameVisualStyle.ApplyColor(trailRenderer, streakColor, 0.15f, true, 0.55f);
        }
    }

    private static void CreateTrailStreak(Transform arrowTransform, Color color, float scale)
    {
        if (arrowTransform == null) return;

        GameObject trailObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trailObject.name = "ArrowTrail";
        trailObject.transform.SetParent(arrowTransform, false);
        trailObject.transform.localPosition = new Vector3(0f, 0f, -0.45f);
        trailObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        trailObject.transform.localScale = new Vector3(scale * 0.05f, scale * 0.75f, scale * 0.05f);

        Collider trailCollider = trailObject.GetComponent<Collider>();

        if (trailCollider != null)
        {
            trailCollider.enabled = false;
        }

        Renderer trailRenderer = trailObject.GetComponent<Renderer>();

        if (trailRenderer != null)
        {
            Color streakColor = Color.Lerp(color, Color.white, 0.55f);
            GameVisualStyle.ApplyColor(trailRenderer, streakColor, 0.2f, true, 0.35f);
        }
    }

    public static void SpawnImpactVisual(Vector3 impactPoint, float radius)
    {
        float flashSize = Mathf.Max(0.28f, radius * 0.55f);

        GameObject flashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flashObject.name = "ArrowRainImpactFlash";
        flashObject.transform.position = impactPoint + Vector3.up * 0.1f;
        flashObject.transform.localScale = Vector3.one * flashSize;

        Collider flashCollider = flashObject.GetComponent<Collider>();

        if (flashCollider != null)
        {
            Object.Destroy(flashCollider);
        }

        Renderer flashRenderer = flashObject.GetComponent<Renderer>();

        if (flashRenderer != null)
        {
            GameVisualStyle.ApplyColor(flashRenderer, new Color(1f, 0.95f, 0.65f), 0.2f, true, 0.65f);
        }

        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "ArrowRainImpactRing";
        ringObject.transform.position = impactPoint + Vector3.up * 0.06f;
        ringObject.transform.localScale = new Vector3(Mathf.Max(0.5f, radius * 1.6f), 0.004f, Mathf.Max(0.5f, radius * 1.6f));

        Collider ringCollider = ringObject.GetComponent<Collider>();

        if (ringCollider != null)
        {
            Object.Destroy(ringCollider);
        }

        Renderer ringRenderer = ringObject.GetComponent<Renderer>();

        if (ringRenderer != null)
        {
            GameVisualStyle.ApplyColor(ringRenderer, new Color(1f, 0.72f, 0.28f), 0.15f, true, 0.35f);
        }

        Object.Destroy(flashObject, 0.18f);
        Object.Destroy(ringObject, 0.45f);
    }

    public static void SpawnMegaMeteorImpactVisual(Vector3 impactPoint, float radius)
    {
        float flashSize = Mathf.Max(1.2f, radius * 0.55f);

        GameObject flashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flashObject.name = "MegaMeteorImpactFlash";
        flashObject.transform.position = impactPoint + Vector3.up * 0.35f;
        flashObject.transform.localScale = Vector3.one * flashSize;

        Collider flashCollider = flashObject.GetComponent<Collider>();

        if (flashCollider != null)
        {
            Object.Destroy(flashCollider);
        }

        Renderer flashRenderer = flashObject.GetComponent<Renderer>();

        if (flashRenderer != null)
        {
            GameVisualStyle.ApplyColor(flashRenderer, new Color(1f, 0.55f, 0.12f), 0.15f, true, 0.85f);
        }

        GameObject shockwaveObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shockwaveObject.name = "MegaMeteorShockwave";
        shockwaveObject.transform.position = impactPoint + Vector3.up * 0.08f;
        shockwaveObject.transform.localScale = new Vector3(Mathf.Max(2f, radius * 2.2f), 0.006f, Mathf.Max(2f, radius * 2.2f));

        Collider shockwaveCollider = shockwaveObject.GetComponent<Collider>();

        if (shockwaveCollider != null)
        {
            Object.Destroy(shockwaveCollider);
        }

        Renderer shockwaveRenderer = shockwaveObject.GetComponent<Renderer>();

        if (shockwaveRenderer != null)
        {
            GameVisualStyle.ApplyColor(shockwaveRenderer, new Color(1f, 0.35f, 0.08f), 0.1f, true, 0.45f);
        }

        FPSScreenShake.ShakeBig();
        Object.Destroy(flashObject, 0.35f);
        Object.Destroy(shockwaveObject, 0.6f);
    }
}

public class MegaMeteorFallShake : MonoBehaviour
{
    private float nextShakeTime;

    private void Start()
    {
        nextShakeTime = Time.time + 0.18f;
    }

    private void Update()
    {
        if (Time.time < nextShakeTime) return;

        FPSScreenShake.Shake(0.022f, 0.07f);
        nextShakeTime = Time.time + 0.22f;
    }
}
