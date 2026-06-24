using UnityEngine;

public class FrostSigilSkill : MonoBehaviour
{
    private const float WarningDuration = 0.35f;
    private const float PlayerSearchRadius = 22f;
    private const float ClusterRadius = 3.5f;

    private float cooldownTimer;
    private bool warningActive;
    private float warningTimer;
    private Vector3 pendingCenter;
    private float pendingRadius;
    private int pendingDamage;
    private string pendingDamageSource;
    private GameObject warningVisual;

    private void Update()
    {
        if (!MainMenuManager.IsRunActive || Time.timeScale <= 0f)
        {
            return;
        }

        int level = GetSkillLevel();

        if (level <= 0)
        {
            return;
        }

        if (warningActive)
        {
            warningTimer -= Time.deltaTime;

            if (warningTimer <= 0f)
            {
                warningActive = false;
                DetonateFrost();
            }

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

        BeginWarning(center, level);
        cooldownTimer = GetCooldown(level);
    }

    private void BeginWarning(Vector3 center, int level)
    {
        pendingCenter = center;
        pendingRadius = GetRadius(level);
        pendingDamage = GetDamage(level);
        pendingDamageSource = IsGlacialPrisonActive() ? "Glacial Prison" : "Frost Sigil";
        warningActive = true;
        warningTimer = WarningDuration;

        if (warningVisual != null)
        {
            Destroy(warningVisual);
        }

        warningVisual = SpawnGroundRing(
            pendingCenter,
            pendingRadius,
            new Color(0.55f, 0.92f, 1f, 0.42f),
            WarningDuration + 0.05f);
    }

    private void DetonateFrost()
    {
        if (warningVisual != null)
        {
            Destroy(warningVisual);
            warningVisual = null;
        }

        SpawnGroundDisc(
            pendingCenter,
            pendingRadius,
            new Color(0.65f, 0.95f, 1f, 0.55f),
            0.45f);

        if (IsGlacialPrisonActive())
        {
            SpawnGroundRing(
                pendingCenter,
                pendingRadius * 1.1f,
                new Color(0.85f, 1f, 1f, 0.7f),
                0.25f);
        }

        DamageEnemiesInRadius(pendingCenter, pendingRadius, pendingDamage, pendingDamageSource);

        // TODO: Apply movement slow when enemy slow system exists.
    }

    private static int GetSkillLevel()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(UpgradeOptionCatalog.FrostSigilIndex);
    }

    private static int GetPassiveLevel()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(UpgradeOptionCatalog.CryoCoreIndex);
    }

    private static bool IsGlacialPrisonActive()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        return tracker != null && tracker.HasEvolution(BuildEvolutionId.GlacialPrison);
    }

    private static float GetCooldown(int level)
    {
        return Mathf.Max(2.8f, 5f - level * 0.15f);
    }

    private static float GetRadius(int level)
    {
        float radius = 2.2f + level * 0.08f;
        int passiveLevel = GetPassiveLevel();
        radius *= 1f + passiveLevel * 0.03f;

        if (IsGlacialPrisonActive())
        {
            radius *= 1.25f;
        }

        return radius;
    }

    private static int GetDamage(int level)
    {
        float damage = 6f + level * 2f;
        int passiveLevel = GetPassiveLevel();
        damage *= 1f + passiveLevel * 0.08f;

        if (IsGlacialPrisonActive())
        {
            damage *= 1.35f;
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

    private static GameObject SpawnGroundRing(Vector3 center, float radius, Color color, float lifetime)
    {
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "FrostSigilRing";
        ringObject.transform.position = center + Vector3.up * 0.04f;
        ringObject.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

        Collider collider = ringObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        ApplyColor(ringObject, color);
        Destroy(ringObject, Mathf.Max(0.1f, lifetime));
        return ringObject;
    }

    private static void SpawnGroundDisc(Vector3 center, float radius, Color color, float lifetime)
    {
        GameObject discObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discObject.name = "FrostSigilDisc";
        discObject.transform.position = center + Vector3.up * 0.03f;
        discObject.transform.localScale = new Vector3(radius * 1.85f, 0.025f, radius * 1.85f);

        Collider collider = discObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        ApplyColor(discObject, color);
        Destroy(discObject, Mathf.Max(0.1f, lifetime));
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
}
