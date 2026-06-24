using System.Collections;
using UnityEngine;

public class ShadowRiftSkill : MonoBehaviour
{
    private const float TickInterval = 0.35f;
    private const float PlayerSearchRadius = 22f;
    private const float ClusterRadius = 3.5f;
    private const float EndBurstRadiusMultiplier = 0.85f;

    private float cooldownTimer;
    private bool zoneActive;

    private void Update()
    {
        if (!MainMenuManager.IsRunActive || Time.timeScale <= 0f)
        {
            return;
        }

        int level = GetSkillLevel();

        if (level <= 0 || zoneActive)
        {
            return;
        }

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer > 0f)
        {
            return;
        }

        if (!TryFindTargetCenter(out Vector3 center))
        {
            return;
        }

        StartCoroutine(RunVoidZone(center, level));
        cooldownTimer = GetCooldown(level);
    }

    private IEnumerator RunVoidZone(Vector3 center, int level)
    {
        zoneActive = true;

        float radius = GetRadius(level);
        float duration = GetDuration(level);
        int tickDamage = GetTickDamage(level);
        bool evolved = IsAbyssSingularityActive();
        string sourceName = evolved ? "Abyss Singularity" : "Shadow Rift";
        ShadowRiftPortalFx portalFx = SpawnVoidPortal(center, radius, duration, evolved);

        float elapsed = 0f;
        float tickTimer = 0f;

        while (elapsed < duration)
        {
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                tickTimer = TickInterval;
                DamageEnemiesInRadius(center, radius, tickDamage, sourceName);

                if (portalFx != null)
                {
                    portalFx.PulseTick();
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (evolved)
        {
            int burstDamage = Mathf.Max(1, Mathf.RoundToInt(tickDamage * 1.5f));
            DamageEnemiesInRadius(center, radius * EndBurstRadiusMultiplier, burstDamage, sourceName);
            SpawnVoidBurst(center, radius, true);

            if (portalFx != null)
            {
                portalFx.PlayEndBurst();
            }
        }

        if (portalFx != null)
        {
            Destroy(portalFx.gameObject);
        }

        zoneActive = false;
    }

    private static int GetSkillLevel()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(UpgradeOptionCatalog.ShadowRiftIndex);
    }

    private static int GetPassiveLevel()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(UpgradeOptionCatalog.VoidCatalystIndex);
    }

    private static bool IsAbyssSingularityActive()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        return tracker != null && tracker.HasEvolution(BuildEvolutionId.AbyssSingularity);
    }

    private static float GetCooldown(int level)
    {
        return Mathf.Max(3.6f, 6.5f - level * 0.18f);
    }

    private static float GetRadius(int level)
    {
        float radius = 2.1f + level * 0.08f;

        if (IsAbyssSingularityActive())
        {
            radius *= 1.25f;
        }

        return radius;
    }

    private static float GetDuration(int level)
    {
        float duration = 1.8f + level * 0.06f;
        int passiveLevel = GetPassiveLevel();
        duration *= 1f + passiveLevel * 0.04f;

        if (IsAbyssSingularityActive())
        {
            duration *= 1.15f;
        }

        return duration;
    }

    private static int GetTickDamage(int level)
    {
        float damage = 2f + level;
        int passiveLevel = GetPassiveLevel();
        damage *= 1f + passiveLevel * 0.08f;

        if (IsAbyssSingularityActive())
        {
            damage *= 1.3f;
        }

        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    private static bool TryFindTargetCenter(out Vector3 center)
    {
        center = Vector3.zero;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return false;
        }

        Vector3 playerPosition = player.transform.position;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            return false;
        }

        Transform bestEnemy = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            if (enemyObject.GetComponent<Enemy>() == null)
            {
                continue;
            }

            float distanceToPlayer = Vector3.Distance(playerPosition, enemyObject.transform.position);

            if (distanceToPlayer > PlayerSearchRadius)
            {
                continue;
            }

            int neighborCount = CountNeighbors(enemyObject.transform.position, enemies, ClusterRadius);
            float score = neighborCount * 12f - distanceToPlayer;

            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = enemyObject.transform;
            }
        }

        if (bestEnemy == null)
        {
            return false;
        }

        center = bestEnemy.position;
        center.y = playerPosition.y + 0.05f;
        return true;
    }

    private static int CountNeighbors(Vector3 position, GameObject[] enemies, float radius)
    {
        int count = 0;
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            if ((enemyObject.transform.position - position).sqrMagnitude <= radiusSqr)
            {
                count++;
            }
        }

        return count;
    }

    private static void DamageEnemiesInRadius(Vector3 center, float radius, int damage, string sourceName)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float radiusSqr = radius * radius;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null)
            {
                continue;
            }

            if ((enemyObject.transform.position - center).sqrMagnitude > radiusSqr)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt(sourceName, damage);
            enemy.TakeDamage(damage);
        }
    }

    private static ShadowRiftPortalFx SpawnVoidPortal(Vector3 center, float radius, float duration, bool evolved)
    {
        GameObject portalRoot = new GameObject("ShadowRiftPortal");
        portalRoot.transform.position = center + Vector3.up * 0.05f;

        ShadowRiftPortalFx portalFx = portalRoot.AddComponent<ShadowRiftPortalFx>();
        portalFx.Initialize(radius, duration, evolved);
        return portalFx;
    }

    private static void SpawnVoidBurst(Vector3 center, float radius, bool evolved)
    {
        Vector3 ground = center + Vector3.up * 0.06f;

        GameObject burstObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burstObject.name = "ShadowRiftBurst";
        burstObject.transform.position = ground + Vector3.up * 0.12f;
        burstObject.transform.localScale = Vector3.one * radius * (evolved ? 1.25f : 1.05f);

        RemoveCollider(burstObject);
        ApplyColor(burstObject, evolved ? new Color(0.62f, 0.18f, 0.88f, 0.62f) : new Color(0.45f, 0.12f, 0.62f, 0.5f));

        ShadowRiftBurstFx burstFx = burstObject.AddComponent<ShadowRiftBurstFx>();
        burstFx.Initialize(0.28f);
        Destroy(burstObject, 0.32f);

        SpawnGroundRingMesh(
            ground,
            radius * (evolved ? 1.15f : 0.95f),
            evolved ? new Color(0.52f, 0.16f, 0.78f, 0.55f) : new Color(0.38f, 0.1f, 0.58f, 0.42f),
            0.24f);
    }

    private static void SpawnGroundRingMesh(Vector3 center, float radius, Color color, float lifetime)
    {
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "ShadowRiftRing";
        ringObject.transform.position = center;
        ringObject.transform.localScale = new Vector3(radius * 2f, 0.025f, radius * 2f);

        RemoveCollider(ringObject);
        ApplyColor(ringObject, color);
        Destroy(ringObject, lifetime);
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return;
        }

        Material material = new Material(shader);
        material.color = color;
        renderer.material = material;
    }

    private sealed class ShadowRiftPortalFx : MonoBehaviour
    {
        private Transform rotatingRing;
        private Renderer coreRenderer;
        private Renderer haloRenderer;
        private float duration;
        private float elapsed;
        private float radius;
        private bool evolved;
        private Color coreBase;
        private Color haloBase;

        public void Initialize(float portalRadius, float life, bool isEvolved)
        {
            radius = portalRadius;
            duration = Mathf.Max(0.2f, life);
            evolved = isEvolved;
            coreBase = evolved ? new Color(0.24f, 0.05f, 0.36f, 0.72f) : new Color(0.18f, 0.04f, 0.28f, 0.62f);
            haloBase = evolved ? new Color(0.58f, 0.18f, 0.82f, 0.48f) : new Color(0.42f, 0.12f, 0.62f, 0.38f);

            SpawnGroundPortal();
            SpawnRotatingRing();
            SpawnCoreHalo();
        }

        public void PulseTick()
        {
            if (coreRenderer != null && coreRenderer.material != null)
            {
                Color flash = evolved ? new Color(0.72f, 0.28f, 0.95f, 0.82f) : new Color(0.55f, 0.18f, 0.78f, 0.72f);
                coreRenderer.material.color = flash;
            }

            if (haloRenderer != null && haloRenderer.material != null)
            {
                Color flash = evolved ? new Color(0.68f, 0.22f, 0.92f, 0.55f) : new Color(0.5f, 0.14f, 0.72f, 0.45f);
                haloRenderer.material.color = flash;
            }
        }

        public void PlayEndBurst()
        {
            if (rotatingRing != null)
            {
                rotatingRing.localScale *= evolved ? 1.18f : 1.08f;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float lifeT = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            float pulse = 0.82f + Mathf.Sin(elapsed * (evolved ? 9f : 7f)) * 0.18f;

            if (rotatingRing != null)
            {
                rotatingRing.Rotate(0f, evolved ? 95f : 70f, 0f, Space.Self);
                rotatingRing.localScale = Vector3.one * pulse;
            }

            float fade = 1f - Mathf.SmoothStep(0.82f, 1f, lifeT);

            if (coreRenderer != null && coreRenderer.material != null)
            {
                Color color = coreBase;
                color.a = coreBase.a * fade;
                coreRenderer.material.color = color;
            }

            if (haloRenderer != null && haloRenderer.material != null)
            {
                Color color = haloBase;
                color.a = haloBase.a * fade * pulse;
                haloRenderer.material.color = color;
            }
        }

        private void SpawnGroundPortal()
        {
            Vector3 ground = Vector3.zero;

            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "ShadowRiftGroundOuter";
            outerRing.transform.SetParent(transform, false);
            outerRing.transform.localPosition = ground;
            outerRing.transform.localScale = new Vector3(radius * 2.1f, 0.02f, radius * 2.1f);
            RemoveCollider(outerRing);
            ApplyColor(outerRing, evolved ? new Color(0.34f, 0.1f, 0.52f, 0.72f) : new Color(0.26f, 0.07f, 0.4f, 0.62f));

            GameObject innerHole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerHole.name = "ShadowRiftGroundInner";
            innerHole.transform.SetParent(transform, false);
            innerHole.transform.localPosition = ground + Vector3.up * 0.005f;
            innerHole.transform.localScale = new Vector3(radius * 1.2f, 0.022f, radius * 1.2f);
            RemoveCollider(innerHole);
            ApplyColor(innerHole, new Color(0.04f, 0.01f, 0.08f, 0.92f));
        }

        private void SpawnRotatingRing()
        {
            GameObject ringRoot = new GameObject("ShadowRiftRotatingRing");
            ringRoot.transform.SetParent(transform, false);
            ringRoot.transform.localPosition = Vector3.up * 0.08f;
            rotatingRing = ringRoot.transform;

            GameObject ringMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringMesh.name = "ShadowRiftRingMesh";
            ringMesh.transform.SetParent(rotatingRing, false);
            ringMesh.transform.localPosition = Vector3.zero;
            ringMesh.transform.localScale = new Vector3(radius * 1.75f, 0.018f, radius * 1.75f);
            RemoveCollider(ringMesh);
            ApplyColor(ringMesh, evolved ? new Color(0.52f, 0.16f, 0.78f, 0.78f) : new Color(0.4f, 0.11f, 0.6f, 0.68f));
        }

        private void SpawnCoreHalo()
        {
            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "ShadowRiftCore";
            core.transform.SetParent(transform, false);
            core.transform.localPosition = Vector3.up * 0.18f;
            core.transform.localScale = Vector3.one * radius * (evolved ? 0.72f : 0.58f);
            RemoveCollider(core);
            ApplyColor(core, coreBase);
            coreRenderer = core.GetComponent<Renderer>();

            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "ShadowRiftHalo";
            halo.transform.SetParent(transform, false);
            halo.transform.localPosition = Vector3.up * 0.16f;
            halo.transform.localScale = Vector3.one * radius * (evolved ? 0.95f : 0.78f);
            RemoveCollider(halo);
            ApplyColor(halo, haloBase);
            haloRenderer = halo.GetComponent<Renderer>();
        }
    }

    private sealed class ShadowRiftBurstFx : MonoBehaviour
    {
        private float duration;
        private float elapsed;
        private Vector3 startScale;
        private Renderer cachedRenderer;
        private Color baseColor;

        public void Initialize(float life)
        {
            duration = life;
            startScale = transform.localScale;
            cachedRenderer = GetComponent<Renderer>();

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                baseColor = cachedRenderer.material.color;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            transform.localScale = startScale * (1f + t * 0.45f);

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                Color color = baseColor;
                color.a = baseColor.a * (1f - t);
                cachedRenderer.material.color = color;
            }
        }
    }
}
