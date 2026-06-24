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
        string sourceName = IsAbyssSingularityActive() ? "Abyss Singularity" : "Shadow Rift";
        GameObject zoneVisual = SpawnVoidVisual(center, radius, duration);

        float elapsed = 0f;
        float tickTimer = 0f;

        while (elapsed < duration)
        {
            tickTimer -= Time.deltaTime;

            if (tickTimer <= 0f)
            {
                tickTimer = TickInterval;
                DamageEnemiesInRadius(center, radius, tickDamage, sourceName);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (IsAbyssSingularityActive())
        {
            int burstDamage = Mathf.Max(1, Mathf.RoundToInt(tickDamage * 1.5f));
            DamageEnemiesInRadius(center, radius * EndBurstRadiusMultiplier, burstDamage, sourceName);
            SpawnVoidBurst(center, radius);
            // TODO: Add pull effect when enemy displacement system exists.
        }

        if (zoneVisual != null)
        {
            Destroy(zoneVisual);
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

    private static GameObject SpawnVoidVisual(Vector3 center, float radius, float duration)
    {
        GameObject zoneObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        zoneObject.name = "ShadowRiftZone";
        zoneObject.transform.position = center + Vector3.up * 0.35f;
        zoneObject.transform.localScale = Vector3.one * radius * 1.35f;

        Collider collider = zoneObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        ApplyColor(zoneObject, new Color(0.28f, 0.08f, 0.42f, 0.42f));

        ShadowRiftFade fade = zoneObject.AddComponent<ShadowRiftFade>();
        fade.Initialize(duration);

        return zoneObject;
    }

    private static void SpawnVoidBurst(Vector3 center, float radius)
    {
        GameObject burstObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burstObject.name = "ShadowRiftBurst";
        burstObject.transform.position = center + Vector3.up * 0.2f;
        burstObject.transform.localScale = Vector3.one * radius * 1.1f;

        Collider collider = burstObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        ApplyColor(burstObject, new Color(0.45f, 0.12f, 0.62f, 0.55f));
        Destroy(burstObject, 0.3f);
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

    private sealed class ShadowRiftFade : MonoBehaviour
    {
        private float duration;
        private float elapsed;
        private Renderer cachedRenderer;
        private Color baseColor;

        public void Initialize(float life)
        {
            duration = Mathf.Max(0.2f, life);
            cachedRenderer = GetComponent<Renderer>();

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                baseColor = cachedRenderer.material.color;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (cachedRenderer == null || cachedRenderer.material == null)
            {
                return;
            }

            float remaining = 1f - Mathf.Clamp01(elapsed / duration);
            Color color = baseColor;
            color.a = baseColor.a * remaining;
            cachedRenderer.material.color = color;

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}
