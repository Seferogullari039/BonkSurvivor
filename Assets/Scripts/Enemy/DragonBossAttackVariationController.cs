using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Isolated gameplay-only attack scheduler for the Dragon Boss. It does NOT touch the boss visual,
// movement, death, drop, spawn or audio systems. MouthFirePoint is read-only via DragonBossVisual.
[DisallowMultipleComponent]
public class DragonBossAttackVariationController : MonoBehaviour
{
    [Header("Scheduler")]
    [SerializeField] private float startDelay = 2.5f;
    [SerializeField] private float minInterval = 4.5f;
    [SerializeField] private float maxInterval = 6.5f;

    [Header("Fire Breath")]
    [SerializeField] private float fireBreathRange = 9f;
    [SerializeField] private float fireBreathAngle = 55f;
    [SerializeField] private float fireBreathTelegraph = 0.7f;
    [SerializeField] private int fireBreathDamage = 14;

    [Header("Meteor Rain")]
    [SerializeField] private float meteorTelegraph = 0.9f;
    [SerializeField] private int meteorCount = 3;
    [SerializeField] private float meteorRadius = 2.4f;
    [SerializeField] private int meteorDamage = 18;
    [SerializeField] private float meteorSpreadAroundPlayer = 5.0f;

    [Header("Shockwave")]
    [SerializeField] private float shockwaveTelegraph = 0.8f;
    [SerializeField] private float shockwaveRadius = 6.5f;
    [SerializeField] private int shockwaveDamage = 16;

    private Transform player;
    private PlayerStats playerStats;
    private DragonBossVisual dragonVisual;
    private readonly List<GameObject> spawnedVfx = new List<GameObject>();
    private bool isAttacking;

    private static Material fireMaterial;
    private static Material warningMaterial;
    private static Material impactMaterial;
    private static Material shockMaterial;

    private void OnEnable()
    {
        StartCoroutine(AttackScheduler());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
        CleanupVfx();
    }

    private void OnDestroy()
    {
        CleanupVfx();
    }

    private IEnumerator AttackScheduler()
    {
        yield return new WaitForSeconds(startDelay);

        int attackIndex = 0;

        while (true)
        {
            if (!isAttacking && EnsurePlayer())
            {
                isAttacking = true;
                yield return StartCoroutine(RunAttack(attackIndex % 3));
                attackIndex++;
                isAttacking = false;
            }

            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    private IEnumerator RunAttack(int index)
    {
        switch (index)
        {
            case 0:
                yield return StartCoroutine(FireBreathRoutine());
                break;
            case 1:
                yield return StartCoroutine(MeteorRainRoutine());
                break;
            default:
                yield return StartCoroutine(ShockwaveRoutine());
                break;
        }
    }

    private IEnumerator FireBreathRoutine()
    {
        if (!EnsurePlayer())
        {
            yield break;
        }

        Vector3 origin = GetMouthOrFallbackPoint();
        Vector3 direction = player.position - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();

        float length = Mathf.Max(0.5f, fireBreathRange);
        float width = Mathf.Max(0.5f, 2f * length * Mathf.Tan(fireBreathAngle * 0.5f * Mathf.Deg2Rad));
        float groundY = ResolveGroundY(origin) + 0.05f;

        GameObject telegraph = CreateGroundSlab(
            "DragonBoss_FireBreathTelegraph",
            new Vector3(origin.x, groundY, origin.z) + direction * (length * 0.5f),
            Quaternion.LookRotation(direction, Vector3.up),
            new Vector3(width, 0.04f, length),
            GetFireMaterial());

        yield return new WaitForSeconds(fireBreathTelegraph);

        Vector3 impactOrigin = GetMouthOrFallbackPoint();

        if (IsPlayerInCone(impactOrigin, direction, fireBreathRange, fireBreathAngle))
        {
            TryDamagePlayer(fireBreathDamage);
        }

        DestroySafe(telegraph);
    }

    private IEnumerator MeteorRainRoutine()
    {
        if (!EnsurePlayer())
        {
            yield break;
        }

        List<Vector3> impactPoints = new List<Vector3>();
        List<GameObject> warnings = new List<GameObject>();
        int count = Mathf.Max(1, meteorCount);

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * meteorSpreadAroundPlayer;
            Vector3 around = player.position + new Vector3(offset.x, 0f, offset.y);
            Vector3 point = ResolveGroundPoint(around);
            impactPoints.Add(point);

            warnings.Add(CreateGroundSlab(
                "DragonBoss_MeteorWarning",
                point + Vector3.up * 0.05f,
                Quaternion.identity,
                new Vector3(meteorRadius * 2f, 0.04f, meteorRadius * 2f),
                GetWarningMaterial(),
                PrimitiveType.Cylinder));
        }

        yield return new WaitForSeconds(meteorTelegraph);

        bool damagedThisCast = false;

        for (int i = 0; i < impactPoints.Count; i++)
        {
            Vector3 point = impactPoints[i];

            GameObject flash = CreateGroundSlab(
                "DragonBoss_MeteorImpact",
                point + Vector3.up * 0.06f,
                Quaternion.identity,
                new Vector3(meteorRadius * 2.1f, 0.06f, meteorRadius * 2.1f),
                GetImpactMaterial(),
                PrimitiveType.Cylinder);

            if (!damagedThisCast && player != null)
            {
                Vector3 flat = player.position - point;
                flat.y = 0f;

                if (flat.magnitude <= meteorRadius)
                {
                    TryDamagePlayer(meteorDamage);
                    damagedThisCast = true;
                }
            }

            StartCoroutine(DestroyAfter(flash, 0.4f));
        }

        for (int i = 0; i < warnings.Count; i++)
        {
            DestroySafe(warnings[i]);
        }
    }

    private IEnumerator ShockwaveRoutine()
    {
        Vector3 center = transform.position;

        GameObject telegraph = CreateGroundSlab(
            "DragonBoss_ShockwaveTelegraph",
            center + Vector3.up * 0.05f,
            Quaternion.identity,
            new Vector3(shockwaveRadius * 2f, 0.04f, shockwaveRadius * 2f),
            GetShockMaterial(),
            PrimitiveType.Cylinder);

        yield return new WaitForSeconds(shockwaveTelegraph);

        if (player != null)
        {
            Vector3 flat = player.position - center;
            flat.y = 0f;

            if (flat.magnitude <= shockwaveRadius)
            {
                TryDamagePlayer(shockwaveDamage);
            }
        }

        GameObject wave = CreateGroundSlab(
            "DragonBoss_Shockwave",
            center + Vector3.up * 0.06f,
            Quaternion.identity,
            new Vector3(0.5f, 0.05f, 0.5f),
            GetShockMaterial(),
            PrimitiveType.Cylinder);

        DestroySafe(telegraph);

        float elapsed = 0f;
        float expandTime = 0.45f;

        while (elapsed < expandTime && wave != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / expandTime);
            float diameter = Mathf.Lerp(0.5f, shockwaveRadius * 2f, t);
            wave.transform.localScale = new Vector3(diameter, 0.05f, diameter);
            yield return null;
        }

        DestroySafe(wave);
    }

    private bool IsPlayerInCone(Vector3 origin, Vector3 direction, float range, float angle)
    {
        if (player == null)
        {
            return false;
        }

        Vector3 toPlayer = player.position - origin;
        toPlayer.y = 0f;

        if (toPlayer.magnitude > range)
        {
            return false;
        }

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        return Vector3.Angle(direction, toPlayer.normalized) <= angle * 0.5f;
    }

    private bool EnsurePlayer()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                playerStats = playerObject.GetComponent<PlayerStats>();
            }
        }

        return player != null;
    }

    private void TryDamagePlayer(int damage)
    {
        if (playerStats == null && player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }

        if (playerStats != null && !playerStats.IsDead)
        {
            playerStats.TakeDamage(damage);
        }
    }

    private Vector3 GetMouthOrFallbackPoint()
    {
        if (dragonVisual == null)
        {
            dragonVisual = GetComponent<DragonBossVisual>();
        }

        if (dragonVisual != null && dragonVisual.MouthFirePoint != null)
        {
            return dragonVisual.MouthFirePoint.position;
        }

        return transform.position + Vector3.up * 2.2f + transform.forward * 1.8f;
    }

    private Vector3 ResolveGroundPoint(Vector3 around)
    {
        Vector3 rayStart = around + Vector3.up * 25f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 80f))
        {
            return hit.point;
        }

        float fallbackY = player != null ? player.position.y : transform.position.y;
        return new Vector3(around.x, fallbackY, around.z);
    }

    private float ResolveGroundY(Vector3 around)
    {
        Vector3 rayStart = around + Vector3.up * 25f;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 80f))
        {
            return hit.point.y;
        }

        return player != null ? player.position.y : transform.position.y;
    }

    private GameObject CreateGroundSlab(
        string vfxName,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        Material material,
        PrimitiveType primitive = PrimitiveType.Cube)
    {
        GameObject vfx = GameObject.CreatePrimitive(primitive);
        vfx.name = vfxName;
        vfx.transform.SetPositionAndRotation(position, rotation);
        vfx.transform.localScale = scale;

        Collider collider = vfx.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = vfx.GetComponent<Renderer>();

        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        spawnedVfx.Add(vfx);
        return vfx;
    }

    private IEnumerator DestroyAfter(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroySafe(target);
    }

    private void DestroySafe(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        spawnedVfx.Remove(target);
        Destroy(target);
    }

    private void CleanupVfx()
    {
        for (int i = 0; i < spawnedVfx.Count; i++)
        {
            if (spawnedVfx[i] != null)
            {
                Destroy(spawnedVfx[i]);
            }
        }

        spawnedVfx.Clear();
    }

    private static Material GetFireMaterial()
    {
        if (fireMaterial == null)
        {
            fireMaterial = CreateRuntimeMaterial(new Color(1f, 0.4f, 0.08f), 0.5f);
        }

        return fireMaterial;
    }

    private static Material GetWarningMaterial()
    {
        if (warningMaterial == null)
        {
            warningMaterial = CreateRuntimeMaterial(new Color(0.95f, 0.18f, 0.12f), 0.42f);
        }

        return warningMaterial;
    }

    private static Material GetImpactMaterial()
    {
        if (impactMaterial == null)
        {
            impactMaterial = CreateRuntimeMaterial(new Color(1f, 0.62f, 0.2f), 0.72f);
        }

        return impactMaterial;
    }

    private static Material GetShockMaterial()
    {
        if (shockMaterial == null)
        {
            shockMaterial = CreateRuntimeMaterial(new Color(0.3f, 0.75f, 1f), 0.5f);
        }

        return shockMaterial;
    }

    private static Material CreateRuntimeMaterial(Color color, float alpha)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        Color tinted = new Color(color.r, color.g, color.b, alpha);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tinted);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", tinted);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 0.65f);
            material.EnableKeyword("_EMISSION");
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }
}
