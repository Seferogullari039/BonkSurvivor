using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject dragonBossPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int absoluteMaxEnemies = 65;
    [SerializeField] private float waveBurstSpawnDelay = 0.12f;
    [SerializeField] private float minimumSpawnInterval = 0.4f;

    private float spawnInterval = 0.95f;
    private float spawnDistance = 14f;
    private float timer;
    private float runTimer;
    private int currentWave = 0;
    private int spawnedBossWave = 0;
    private int pendingWaveBurstCount;
    private float waveBurstTimer;

    public int CurrentWave => currentWave;

    public void DevAdvanceWave()
    {
        currentWave = Mathf.Max(1, currentWave + 1);
        runTimer = (currentWave - 1) * 10f;
        ApplyWaveSpawnSettings(currentWave, out spawnInterval, out spawnDistance);
        QueueWaveBurst(currentWave);

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateWave(currentWave);
        }

        RunEventMessageDisplay.ShowWave(currentWave);
    }

    public void DevSpawnBoss()
    {
        if (enemyPrefab == null || player == null) return;

        SpawnMiniBoss(Mathf.Max(1, currentWave));
    }

    public void DevSpawnElite()
    {
        if (enemyPrefab == null || player == null) return;

        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= GetMaxAliveEnemies(currentWave))
        {
            return;
        }

        Vector3 spawnPosition;

        if (FPSPlayerController.IsFpsModeActive)
        {
            Vector3 offset = FPSSpawnUtility.GetSpawnOffset(player, FPSSpawnZone.Front);
            spawnPosition = new Vector3(
                player.position.x + offset.x,
                0.5f,
                player.position.z + offset.z
            );
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;

            spawnPosition = new Vector3(
                player.position.x + randomCircle.x,
                0.5f,
                player.position.z + randomCircle.y
            );
        }

        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 12f, 1.5f);

        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy == null) return;

        ApplyEnemyType(enemy, enemyObject.transform, Enemy.EnemyType.Normal);
        enemy.ApplyDifficultyScaling(Mathf.Max(1, currentWave));
        ApplyEliteMutation(enemy, enemyObject.transform);
    }

    public bool TrySpawnPortalEnemy(Vector3 portalPosition)
    {
        if (enemyPrefab == null || player == null) return false;

        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= GetMaxAliveEnemies(currentWave))
        {
            return false;
        }

        Vector2 offset = Random.insideUnitCircle * Random.Range(2f, 5f);
        Vector3 spawnPosition = new Vector3(
            portalPosition.x + offset.x,
            0.5f,
            portalPosition.z + offset.y
        );

        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 4f, 1.2f);

        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy == null) return false;

        ApplyEnemyType(enemy, enemyObject.transform, Enemy.EnemyType.Normal);
        enemy.ApplyDifficultyScaling(Mathf.Max(1, currentWave));
        return true;
    }

    public void ResetRun()
    {
        timer = 0f;
        runTimer = 0f;
        currentWave = 0;
        spawnedBossWave = 0;
        pendingWaveBurstCount = 0;
        waveBurstTimer = 0f;
        spawnInterval = ResolveSpawnInterval(1);
        spawnDistance = ResolveSpawnDistance(1);
        DragonBossSpawnTracker.ResetRun();
    }

    private void Update()
    {
        if (!MainMenuManager.IsRunActive) return;
        if (enemyPrefab == null || player == null) return;

        runTimer += Time.deltaTime;
        UpdateWaveSettings();
        ProcessWaveBurst();

        timer += Time.deltaTime;

        int pressureWave = Mathf.Max(1, currentWave);
        float effectiveSpawnInterval = spawnInterval * EnemyWavePressureScaler.GetSpawnIntervalMultiplier(pressureWave);

        if (BloodMoonEventManager.Instance != null && BloodMoonEventManager.Instance.IsActive)
        {
            effectiveSpawnInterval *= BloodMoonEventManager.Instance.SpawnIntervalMultiplier;
        }

        effectiveSpawnInterval = Mathf.Max(minimumSpawnInterval, effectiveSpawnInterval);

        if (timer >= effectiveSpawnInterval)
        {
            if (TrySpawnEnemy())
            {
                timer = 0f;
            }
        }
    }

    private void ProcessWaveBurst()
    {
        if (pendingWaveBurstCount <= 0)
        {
            return;
        }

        waveBurstTimer += Time.deltaTime;

        while (pendingWaveBurstCount > 0 && waveBurstTimer >= waveBurstSpawnDelay)
        {
            waveBurstTimer -= waveBurstSpawnDelay;

            if (!TrySpawnEnemy())
            {
                return;
            }

            pendingWaveBurstCount--;
        }
    }

    private void QueueWaveBurst(int wave)
    {
        pendingWaveBurstCount = ResolveWaveBurstCount(wave);
        waveBurstTimer = waveBurstSpawnDelay;
    }

    private void UpdateWaveSettings()
    {
        int newWave = Mathf.FloorToInt(runTimer / 10f) + 1;
        ApplyWaveSpawnSettings(newWave, out float newSpawnInterval, out float newSpawnDistance);

        spawnInterval = newSpawnInterval;
        spawnDistance = newSpawnDistance;

        if (newWave != currentWave)
        {
            currentWave = newWave;
            RunStatsTracker.GetOrCreate().RecordWaveReached(currentWave);

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateWave(currentWave);
            }

            RunEventMessageDisplay.ShowWave(currentWave);
            QueueWaveBurst(currentWave);
            TrySpawnMiniBoss();
        }
    }

    private static void ApplyWaveSpawnSettings(int wave, out float interval, out float distance)
    {
        interval = ResolveSpawnInterval(wave);
        distance = ResolveSpawnDistance(wave);
    }

    private static float ResolveSpawnInterval(int wave)
    {
        if (wave <= 1)
        {
            return 0.95f;
        }

        if (wave == 2)
        {
            return 0.88f;
        }

        if (wave == 3)
        {
            return 0.82f;
        }

        if (wave <= 5)
        {
            return 0.75f;
        }

        if (wave <= 10)
        {
            return 0.62f;
        }

        if (wave <= 20)
        {
            return 0.52f;
        }

        return 0.48f;
    }

    private static float ResolveSpawnDistance(int wave)
    {
        if (wave <= 1)
        {
            return 14f;
        }

        if (wave == 2)
        {
            return 15f;
        }

        if (wave == 3)
        {
            return 16f;
        }

        if (wave <= 5)
        {
            return 17f;
        }

        if (wave <= 10)
        {
            return 18f;
        }

        if (wave <= 20)
        {
            return 19f;
        }

        return 20f;
    }

    private int GetMaxAliveEnemies(int wave)
    {
        int waveCap;

        if (wave <= 1)
        {
            waveCap = 20;
        }
        else if (wave == 2)
        {
            waveCap = 22;
        }
        else if (wave == 3)
        {
            waveCap = 24;
        }
        else if (wave <= 5)
        {
            waveCap = 28;
        }
        else if (wave <= 10)
        {
            waveCap = 34;
        }
        else if (wave <= 20)
        {
            waveCap = 45;
        }
        else
        {
            waveCap = 60;
        }

        int pressureBonus = EnemyWavePressureScaler.GetMaxAliveBonus(wave);
        return Mathf.Min(waveCap + pressureBonus, absoluteMaxEnemies);
    }

    private static int ResolveWaveBurstCount(int wave)
    {
        if (wave <= 1)
        {
            return 6;
        }

        if (wave == 2)
        {
            return 7;
        }

        if (wave == 3)
        {
            return 8;
        }

        return Mathf.Clamp(8 + (wave - 4), 8, 10);
    }

    private void TrySpawnMiniBoss()
    {
        if (currentWave % 5 != 0) return;
        if (spawnedBossWave == currentWave) return;

        if (DragonBossSpawnTracker.IsDragonWave(currentWave))
        {
            if (TrySpawnDragonBoss(currentWave))
            {
                spawnedBossWave = currentWave;
            }

            return;
        }

        SpawnMiniBoss(currentWave);
        spawnedBossWave = currentWave;
    }

    private bool TrySpawnDragonBoss(int wave)
    {
        if (!DragonBossSpawnTracker.CanSpawn(wave)) return false;

        GameObject prefab = ResolveDragonBossPrefab();
        if (prefab == null || player == null) return false;

        Vector3 spawnPosition = GetDragonSpawnPosition();
        GameObject dragonObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        dragonObject.transform.localScale = Vector3.one * 3.5f;

        DragonBossController controller = dragonObject.GetComponent<DragonBossController>();
        if (controller != null)
        {
            controller.Initialize(wave);
        }
        else
        {
            Enemy fallbackEnemy = dragonObject.GetComponent<Enemy>();
            fallbackEnemy?.Configure(2.6f, wave >= 30 ? 900 : wave >= 20 ? 500 : 250, new Color(0.48f, 0.1f, 0.24f), Enemy.EnemyType.DragonBoss);
        }

        DragonBossSpawnTracker.MarkSpawned(wave);

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayBossSpawn(dragonObject.transform.position);
        }

        AudioManager.Instance?.PlayBossSpawn();
        RunEventMessageDisplay.ShowDragonBossIncoming();
        return true;
    }

    private GameObject ResolveDragonBossPrefab()
    {
        return dragonBossPrefab;
    }

    private Vector3 GetDragonSpawnPosition()
    {
        Vector3 spawnPosition;

        if (ProceduralGrassArena.Instance != null)
        {
            spawnPosition = ProceduralGrassArena.Instance.GetSafePointInsideArena(35f, 3f);
        }
        else
        {
            float spawnDistance = Random.Range(35f, 45f);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;

            spawnPosition = new Vector3(
                player.position.x + randomCircle.x,
                ProceduralGrassArena.GetLootSpawnY(0.5f),
                player.position.z + randomCircle.y
            );
        }

        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition, 4f);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 35f, 3f);
        return spawnPosition;
    }

    private void SpawnMiniBoss(int wave)
    {
        Vector3 spawnPosition;

        if (FPSPlayerController.IsFpsModeActive)
        {
            Vector3 offset = FPSSpawnUtility.GetBossSpawnOffset(player);
            spawnPosition = new Vector3(
                player.position.x + offset.x,
                0.5f,
                player.position.z + offset.z
            );
        }
        else
        {
            float bossSpawnDistance = Random.Range(18f, 22f);
            Vector2 randomCircle = Random.insideUnitCircle.normalized * bossSpawnDistance;

            spawnPosition = new Vector3(
                player.position.x + randomCircle.x,
                0.5f,
                player.position.z + randomCircle.y
            );
        }

        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 18f, 1.5f);

        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy == null) return;

        int bossHealth = 20 + wave * 4;
        Color bossColor = GameVisualPalette.MiniBoss;

        enemy.Configure(
            2.2f,
            bossHealth,
            bossColor,
            Enemy.EnemyType.MiniBoss
        );

        enemyObject.transform.localScale = Vector3.one * 2.2f;

        BossAbilityType abilityType = GetBossAbilityType(wave);
        enemy.SetBossAbility(abilityType);
        AttachBossAbility(enemyObject, abilityType, bossColor);

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayBossSpawn(enemyObject.transform.position);
        }

        AudioManager.Instance?.PlayBossSpawn();
        RunEventMessageDisplay.ShowBossIncoming();
    }

    private BossAbilityType GetBossAbilityType(int wave)
    {
        if (DragonBossSpawnTracker.IsDragonWave(wave))
        {
            return BossAbilityType.None;
        }

        switch (wave)
        {
            case 10:
                return BossAbilityType.Dash;
            case 20:
                return BossAbilityType.Summoner;
            case 30:
                return BossAbilityType.Shooter;
            default:
                return BossAbilityType.None;
        }
    }

    private void AttachBossAbility(GameObject bossObject, BossAbilityType abilityType, Color bossColor)
    {
        switch (abilityType)
        {
            case BossAbilityType.Dash:
                BossDashAbility dashAbility = bossObject.AddComponent<BossDashAbility>();
                dashAbility.Initialize(bossColor);
                break;
            case BossAbilityType.Summoner:
                BossSummonerAbility summonerAbility = bossObject.AddComponent<BossSummonerAbility>();
                summonerAbility.Initialize(enemyPrefab);
                break;
            case BossAbilityType.Shooter:
                bossObject.AddComponent<BossShooterAbility>();
                break;
        }
    }

    private bool TrySpawnEnemy()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= GetMaxAliveEnemies(currentWave))
        {
            return false;
        }

        Vector3 spawnPosition;
        FPSSpawnZone fpsSpawnZone = FPSSpawnZone.Front;

        if (FPSPlayerController.IsFpsModeActive)
        {
            fpsSpawnZone = FPSSpawnUtility.RollSpawnZone(currentWave);

            if (!FPSSpawnUtility.IsBackSpawnAllowed(currentWave) && fpsSpawnZone == FPSSpawnZone.Back)
            {
                fpsSpawnZone = FPSSpawnZone.Front;
            }

            Vector3 offset = FPSSpawnUtility.GetSpawnOffset(player, fpsSpawnZone);
            spawnPosition = new Vector3(
                player.position.x + offset.x,
                0.5f,
                player.position.z + offset.z
            );
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnDistance;

            spawnPosition = new Vector3(
                player.position.x + randomCircle.x,
                0.5f,
                player.position.z + randomCircle.y
            );
        }

        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 12f, 1.5f);

        GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        Enemy enemy = enemyObject.GetComponent<Enemy>();

        if (enemy == null)
        {
            return false;
        }

        Enemy.EnemyType enemyType = GetRandomEnemyType(fpsSpawnZone);
        ApplyEnemyType(enemy, enemyObject.transform, enemyType);
        enemy.ApplyDifficultyScaling(Mathf.Max(1, currentWave));

        if (ShouldSpawnElite() && !IsBlockedEliteSpawn(fpsSpawnZone))
        {
            ApplyEliteMutation(enemy, enemyObject.transform);
        }

        return true;
    }

    private static bool IsBlockedEliteSpawn(FPSSpawnZone fpsSpawnZone)
    {
        return FPSPlayerController.IsFpsModeActive && fpsSpawnZone == FPSSpawnZone.Back;
    }

    private bool ShouldSpawnElite()
    {
        if (currentWave <= 2) return false;

        float eliteChance = 0.05f + (currentWave - 3) * 0.004f;
        eliteChance = Mathf.Min(eliteChance, 0.15f);

        return Random.value < eliteChance;
    }

    private static void ApplyEliteMutation(Enemy enemy, Transform enemyTransform)
    {
        if (enemy == null || enemyTransform == null) return;

        enemy.ApplyEliteMutation(GameVisualPalette.EliteEnemy);
        enemyTransform.localScale *= 1.2f;

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayEliteSpawn(enemyTransform.position);
        }
    }

    private Enemy.EnemyType GetRandomEnemyType(FPSSpawnZone fpsSpawnZone = FPSSpawnZone.Front)
    {
        if (FPSPlayerController.IsFpsModeActive)
        {
            return GetFpsRandomEnemyType(fpsSpawnZone);
        }

        float roll = Random.value;
        int enemyWave = Mathf.Clamp(currentWave, 1, 4);

        switch (enemyWave)
        {
            case 1:
                return Enemy.EnemyType.Normal;
            case 2:
                return roll < 0.82f ? Enemy.EnemyType.Normal : Enemy.EnemyType.Fast;
            case 3:
                if (roll < 0.62f) return Enemy.EnemyType.Normal;
                if (roll < 0.92f) return Enemy.EnemyType.Fast;
                return Enemy.EnemyType.Tank;
            default:
                if (roll < 0.4f) return Enemy.EnemyType.Normal;
                if (roll < 0.75f) return Enemy.EnemyType.Fast;
                return Enemy.EnemyType.Tank;
        }
    }

    private Enemy.EnemyType GetFpsRandomEnemyType(FPSSpawnZone fpsSpawnZone)
    {
        float roll = Random.value;
        Enemy.EnemyType enemyType;

        if (currentWave <= 1)
        {
            enemyType = Enemy.EnemyType.Normal;
        }
        else if (currentWave == 2)
        {
            enemyType = roll < 0.9f ? Enemy.EnemyType.Normal : Enemy.EnemyType.Fast;
        }
        else if (currentWave == 3)
        {
            enemyType = roll < 0.78f ? Enemy.EnemyType.Normal : Enemy.EnemyType.Fast;
        }
        else
        {
            if (roll < 0.4f) enemyType = Enemy.EnemyType.Normal;
            else if (roll < 0.75f) enemyType = Enemy.EnemyType.Fast;
            else enemyType = Enemy.EnemyType.Tank;
        }

        if (fpsSpawnZone == FPSSpawnZone.Back && enemyType == Enemy.EnemyType.Fast)
        {
            enemyType = Enemy.EnemyType.Normal;
        }

        return enemyType;
    }

    private void ApplyEnemyType(Enemy enemy, Transform enemyTransform, Enemy.EnemyType enemyType)
    {
        switch (enemyType)
        {
            case Enemy.EnemyType.Fast:
                enemy.Configure(6f, 1, GameVisualPalette.FastEnemy, enemyType);
                enemyTransform.localScale = Vector3.one * 0.8f;
                break;
            case Enemy.EnemyType.Tank:
                enemy.Configure(2.4f, 10, GameVisualPalette.TankEnemy, enemyType);
                enemyTransform.localScale = Vector3.one * 1.5f;
                break;
            default:
                enemy.Configure(4f, 3, GameVisualPalette.NormalEnemy, enemyType);
                enemyTransform.localScale = Vector3.one;
                break;
        }
    }
}
