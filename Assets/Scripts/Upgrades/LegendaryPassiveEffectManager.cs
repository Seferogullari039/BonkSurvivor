using System.Collections.Generic;
using UnityEngine;

public class LegendaryPassiveEffectManager : MonoBehaviour
{
    public static LegendaryPassiveEffectManager Instance { get; private set; }

    private const float StormCrownCooldown = 6f;
    private const float StormCrownRadius = 14f;
    private const int StormCrownMaxTargets = 3;
    private const int StormCrownThunderKingMaxTargets = 4;
    private const int StormCrownDamage = 18;
    private const float DeathMarkProcChance = 0.02f;
    private const float DeathMarkReaperSightProcChance = 0.025f;
    private const float DeathMarkRadius = 8f;
    private const int DeathMarkMaxTargets = 4;
    private const int DeathMarkReaperSightMaxTargets = 5;
    private const int DeathMarkExecuteDamage = 9999;
    private const float GoldenMagnetPickupRange = 9999f;
    private const float VoidBellCooldown = 10f;
    private const float VoidBellRadius = 8f;
    private const float VoidBellBlackHoleRadius = 10f;
    private const int VoidBellDamage = 22;
    private const int VoidBellBlackHoleDamage = 26;
    private const float DragonHeartCooldown = 8f;
    private const float DragonHeartForwardOffset = 4f;
    private const float DragonHeartRadius = 5f;
    private const int DragonHeartDamage = 24;
    private const float TitanGauntletCooldown = 9f;
    private const float TitanGauntletRadius = 6f;
    private const int TitanGauntletDamage = 28;
    private const float StarfallSigilCooldown = 12f;
    private const float StarfallSigilRadius = 16f;
    private const int StarfallSigilMaxTargets = 3;
    private const int StarfallSigilDamage = 20;

    private float stormCrownTimer;
    private float voidBellTimer;
    private float dragonHeartTimer;
    private float titanGauntletTimer;
    private float starfallSigilTimer;

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
    public static bool HasVoidBell => HasUpgrade(UpgradeOptionCatalog.VoidBellIndex);
    public static bool HasDragonHeart => HasUpgrade(UpgradeOptionCatalog.DragonHeartIndex);
    public static bool HasTitanGauntlet => HasUpgrade(UpgradeOptionCatalog.TitanGauntletIndex);
    public static bool HasStarfallSigil => HasUpgrade(UpgradeOptionCatalog.StarfallSigilIndex);
    public static bool HasCelestialShield => HasUpgrade(UpgradeOptionCatalog.CelestialShieldIndex);
    public static bool HasBloodPact => HasUpgrade(UpgradeOptionCatalog.BloodPactIndex);

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

        if (Random.value >= GetDeathMarkProcChance())
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

        for (int i = 0; i < candidates.Count && hitCount < GetDeathMarkMaxTargets(); i++)
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
        if (!MainMenuManager.IsRunActive || Time.timeScale <= 0f)
        {
            return;
        }

        if (HasStormCrown)
        {
            stormCrownTimer += Time.deltaTime;

            if (stormCrownTimer >= StormCrownCooldown)
            {
                stormCrownTimer = 0f;
                TriggerStormCrown();
            }
        }

        if (HasVoidBell)
        {
            voidBellTimer += Time.deltaTime;

            if (voidBellTimer >= VoidBellCooldown)
            {
                voidBellTimer = 0f;
                TriggerVoidBell();
            }
        }

        if (HasDragonHeart)
        {
            dragonHeartTimer += Time.deltaTime;

            if (dragonHeartTimer >= DragonHeartCooldown)
            {
                dragonHeartTimer = 0f;
                TriggerDragonHeart();
            }
        }

        if (HasTitanGauntlet)
        {
            titanGauntletTimer += Time.deltaTime;

            if (titanGauntletTimer >= TitanGauntletCooldown)
            {
                titanGauntletTimer = 0f;
                TriggerTitanGauntlet();
            }
        }

        if (HasStarfallSigil)
        {
            starfallSigilTimer += Time.deltaTime;

            if (starfallSigilTimer >= StarfallSigilCooldown)
            {
                starfallSigilTimer = 0f;
                TriggerStarfallSigil();
            }
        }
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

        for (int i = 0; i < candidates.Count && hitCount < GetStormCrownMaxTargets(); i++)
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

    private void TriggerVoidBell()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        Vector3 origin = playerObject.transform.position;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float pulseRadius = GetVoidBellRadius();
        int pulseDamage = GetVoidBellDamage();

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (!IsLegendaryPulseTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(origin, enemyObject.transform.position);

            if (distance > pulseRadius)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt("Void Bell", pulseDamage);
            enemy.TakeDamage(pulseDamage);
        }
    }

    private void TriggerDragonHeart()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        Vector3 forward = playerObject.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        Vector3 origin = playerObject.transform.position + forward * DragonHeartForwardOffset;
        DamageEnemiesInRadius(origin, DragonHeartRadius, DragonHeartDamage, "Dragon Heart");
    }

    private void TriggerTitanGauntlet()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        DamageEnemiesInRadius(playerObject.transform.position, TitanGauntletRadius, TitanGauntletDamage, "Titan Gauntlet");
    }

    private void TriggerStarfallSigil()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        DamageNearestEnemies(
            playerObject.transform.position,
            StarfallSigilRadius,
            StarfallSigilMaxTargets,
            StarfallSigilDamage,
            "Starfall Sigil");
    }

    private static void DamageEnemiesInRadius(Vector3 origin, float radius, int damage, string source)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null)
            {
                continue;
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>() ?? enemyObject.GetComponentInParent<Enemy>();

            if (!IsLegendaryPulseTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(origin, enemyObject.transform.position);

            if (distance > radius)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt(source, damage);
            enemy.TakeDamage(damage);
        }
    }

    private static void DamageNearestEnemies(
        Vector3 origin,
        float radius,
        int maxTargets,
        int damage,
        string source)
    {
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

            if (!IsLegendaryPulseTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(origin, enemyObject.transform.position);

            if (distance > radius)
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

        for (int i = 0; i < candidates.Count && hitCount < maxTargets; i++)
        {
            Enemy enemy = candidates[i].Enemy;

            if (enemy == null)
            {
                continue;
            }

            RunStatsTracker.GetOrCreate().RecordDamageDealt(source, damage);
            enemy.TakeDamage(damage);
            hitCount++;
        }
    }

    private static float GetDeathMarkProcChance()
    {
        return ItemSynergyManager.IsReaperSightActive()
            ? DeathMarkReaperSightProcChance
            : DeathMarkProcChance;
    }

    private static int GetDeathMarkMaxTargets()
    {
        return ItemSynergyManager.IsReaperSightActive()
            ? DeathMarkReaperSightMaxTargets
            : DeathMarkMaxTargets;
    }

    private static int GetStormCrownMaxTargets()
    {
        return ItemSynergyManager.IsThunderKingActive()
            ? StormCrownThunderKingMaxTargets
            : StormCrownMaxTargets;
    }

    private static float GetVoidBellRadius()
    {
        return ItemSynergyManager.IsBlackHoleBellActive()
            ? VoidBellBlackHoleRadius
            : VoidBellRadius;
    }

    private static int GetVoidBellDamage()
    {
        return ItemSynergyManager.IsBlackHoleBellActive()
            ? VoidBellBlackHoleDamage
            : VoidBellDamage;
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
        return IsLegendaryPulseTarget(enemy);
    }

    private static bool IsLegendaryPulseTarget(Enemy enemy)
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
