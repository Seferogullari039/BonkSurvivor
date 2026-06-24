using System.Collections.Generic;
using UnityEngine;

public class LegendaryPassiveEffectManager : MonoBehaviour
{
    public static LegendaryPassiveEffectManager Instance { get; private set; }

    private const float StormCrownCooldown = 6f;
    private const float StormCrownRadius = 14f;
    private const int StormCrownMaxTargets = 3;
    private const int StormCrownDamage = 18;
    private const float DeathMarkProcChance = 0.02f;
    private const float DeathMarkRadius = 8f;
    private const int DeathMarkMaxTargets = 4;
    private const int DeathMarkExecuteDamage = 9999;
    private const float GoldenMagnetPickupRange = 9999f;

    private float stormCrownTimer;

    public static LegendaryPassiveEffectManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LegendaryPassiveEffectManager existing = FindFirstObjectByType<LegendaryPassiveEffectManager>();

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("LegendaryPassiveEffectManager");
        return host.AddComponent<LegendaryPassiveEffectManager>();
    }

    public static bool HasGoldenMagnet => HasUpgrade(UpgradeOptionCatalog.GoldenMagnetIndex);
    public static bool HasStormCrown => HasUpgrade(UpgradeOptionCatalog.StormCrownIndex);
    public static bool HasDeathMark => HasUpgrade(UpgradeOptionCatalog.DeathMarkIndex);

    public static float ResolvePickupRange(float calculatedRange)
    {
        if (HasGoldenMagnet)
        {
            return GoldenMagnetPickupRange;
        }

        return calculatedRange;
    }

    public static void TryProcDeathMarkOnPlayerHit(Enemy struckEnemy)
    {
        if (!HasDeathMark || struckEnemy == null)
        {
            return;
        }

        if (Random.value >= DeathMarkProcChance)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        Vector3 origin = playerObject.transform.position;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<EnemyDistanceCandidate> candidates = new List<EnemyDistanceCandidate>(enemies.Length);

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (!IsDeathMarkTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(origin, enemyObject.transform.position);

            if (distance > DeathMarkRadius)
            {
                continue;
            }

            candidates.Add(new EnemyDistanceCandidate(enemy, distance));
        }

        if (candidates.Count == 0)
        {
            return;
        }

        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        int hitCount = 0;

        for (int i = 0; i < candidates.Count && hitCount < DeathMarkMaxTargets; i++)
        {
            Enemy enemy = candidates[i].Enemy;

            if (enemy == null)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt("Death Mark", DeathMarkExecuteDamage);
            enemy.TakeDamage(DeathMarkExecuteDamage);
            hitCount++;
        }
    }

    public static void ResetRun()
    {
        if (Instance == null)
        {
            return;
        }

        Destroy(Instance.gameObject);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!HasStormCrown || !MainMenuManager.IsRunActive || Time.timeScale <= 0f)
        {
            return;
        }

        stormCrownTimer += Time.deltaTime;

        if (stormCrownTimer < StormCrownCooldown)
        {
            return;
        }

        stormCrownTimer = 0f;
        TriggerStormCrown();
    }

    private void TriggerStormCrown()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        Vector3 origin = playerObject.transform.position;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        List<EnemyDistanceCandidate> candidates = new List<EnemyDistanceCandidate>(enemies.Length);

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (!IsStormCrownTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(origin, enemyObject.transform.position);

            if (distance > StormCrownRadius)
            {
                continue;
            }

            candidates.Add(new EnemyDistanceCandidate(enemy, distance));
        }

        if (candidates.Count == 0)
        {
            return;
        }

        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        int hitCount = 0;

        for (int i = 0; i < candidates.Count && hitCount < StormCrownMaxTargets; i++)
        {
            Enemy enemy = candidates[i].Enemy;

            if (enemy == null)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt("Storm Crown", StormCrownDamage);
            enemy.TakeDamage(StormCrownDamage);
            hitCount++;
        }
    }

    private static bool HasUpgrade(int upgradeIndex)
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        return tracker.IsTrackedUpgrade(upgradeIndex);
    }

    private static bool IsStormCrownTarget(Enemy enemy)
    {
        if (enemy == null)
        {
            return false;
        }

        Enemy.EnemyType type = enemy.Type;
        return type != Enemy.EnemyType.MiniBoss && type != Enemy.EnemyType.DragonBoss;
    }

    private static bool IsDeathMarkTarget(Enemy enemy)
    {
        if (enemy == null || enemy.IsElite)
        {
            return false;
        }

        Enemy.EnemyType type = enemy.Type;
        return type == Enemy.EnemyType.Normal
            || type == Enemy.EnemyType.Fast
            || type == Enemy.EnemyType.Tank;
    }

    private readonly struct EnemyDistanceCandidate
    {
        public EnemyDistanceCandidate(Enemy enemy, float distance)
        {
            Enemy = enemy;
            Distance = distance;
        }

        public Enemy Enemy { get; }
        public float Distance { get; }
    }
}
