using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject dragonBossPrefab;
    [SerializeField] private Transform player;

    private float spawnInterval = 2.5f;
    private float spawnDistance = 14f;
    private float timer;
    private float runTimer;
    private int currentWave = 0;
    private int spawnedBossWave = 0;
    private const int MaxEnemies = 65;

    public int CurrentWave => currentWave;

    public void DevAdvanceWave()
    {
        currentWave = Mathf.Max(1, currentWave + 1);
        runTimer = (currentWave - 1) * 10f;

        if (currentWave <= 1)
        {
            spawnInterval = 2.5f;
            spawnDistance = 14f;
        }
        else if (currentWave == 2)
        {
            spawnInterval = 2f;
            spawnDistance = 15f;
        }
        else if (currentWave == 3)
        {
            spawnInterval = 1.65f;
            spawnDistance = 16f;
        }
        else if (currentWave <= 5)
        {
            spawnInterval = 1.35f;
            spawnDistance = 17f;
        }
        else if (currentWave <= 10)
        {
            spawnInterval = 1.05f;
            spawnDistance = 18f;
        }
        else if (currentWave <= 20)
        {
            spawnInterval = 0.85f;
            spawnDistance = 19f;
        }
        else
        {
            spawnInterval = 0.75f;
            spawnDistance = 20f;
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateWave(currentWave);
        }
    }

    public void DevSpawnBoss()
    {
        if (enemyPrefab == null || player == null) return;

        SpawnMiniBoss(Mathf.Max(1, currentWave));
    }

    public void ResetRun()
    {
        timer = 0f;
        runTimer = 0f;
        currentWave = 0;
        spawnedBossWave = 0;
        spawnInterval = 2.5f;
        spawnDistance = 14f;
        DragonBossSpawnTracker.ResetRun();
    }

    private void Update()
    {
        if (!MainMenuManager.IsRunActive) return;
        if (enemyPrefab == null || player == null) return;

        runTimer += Time.deltaTime;
        UpdateWaveSettings();

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void UpdateWaveSettings()
    {
        int newWave = Mathf.FloorToInt(runTimer / 10f) + 1;
        float newSpawnInterval;
        float newSpawnDistance;

        if (newWave <= 1)
        {
            newSpawnInterval = 2.5f;
            newSpawnDistance = 14f;
        }
        else if (newWave == 2)
        {
            newSpawnInterval = 2f;
            newSpawnDistance = 15f;
        }
        else if (newWave == 3)
        {
            newSpawnInterval = 1.65f;
            newSpawnDistance = 16f;
        }
        else if (newWave <= 5)
        {
            newSpawnInterval = 1.35f;
            newSpawnDistance = 17f;
        }
        else if (newWave <= 10)
        {
            newSpawnInterval = 1.05f;
            newSpawnDistance = 18f;
        }
        else if (newWave <= 20)
        {
            newSpawnInterval = 0.85f;
            newSpawnDistance = 19f;
        }
        else
        {
            newSpawnInterval = 0.75f;
            newSpawnDistance = 20f;
        }

        spawnInterval = newSpawnInterval;
        spawnDistance = newSpawnDistance;

        if (newWave != currentWave)
        {
            currentWave = newWave;

            Debug.Log("===== WAVE " + currentWave + " =====");

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateWave(currentWave);
            }

            TrySpawnMiniBoss();
        }
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
        Debug.LogWarning("DRAGON BOSS SPAWNED - WAVE " + wave);
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
        Debug.LogWarning("MINI BOSS SPAWNED - WAVE " + wave);
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

    private void SpawnEnemy()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= MaxEnemies)
        {
            return;
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

        if (enemy == null) return;

        if (ShouldSpawnElite() && !IsBlockedEliteSpawn(fpsSpawnZone))
        {
            ApplyEliteEnemy(enemy, enemyObject.transform);
            return;
        }

        Enemy.EnemyType enemyType = GetRandomEnemyType(fpsSpawnZone);
        ApplyEnemyType(enemy, enemyObject.transform, enemyType);
    }

    private static bool IsBlockedEliteSpawn(FPSSpawnZone fpsSpawnZone)
    {
        return FPSPlayerController.IsFpsModeActive && fpsSpawnZone == FPSSpawnZone.Back;
    }

    private bool ShouldSpawnElite()
    {
        if (FPSPlayerController.IsFpsModeActive && currentWave <= 3) return false;
        if (currentWave <= 5) return false;

        float eliteChance = 0.02f + (currentWave - 5) * 0.012f;
        eliteChance = Mathf.Min(eliteChance, 0.18f);

        return Random.value < eliteChance;
    }

    private void ApplyEliteEnemy(Enemy enemy, Transform enemyTransform)
    {
        const float normalSpeed = 4f;
        const int normalHealth = 3;

        enemy.Configure(
            normalSpeed * 1.3f,
            normalHealth * 2,
            GameVisualPalette.EliteEnemy,
            Enemy.EnemyType.Elite
        );
        enemyTransform.localScale = Vector3.one * 1.1f;

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
                enemy.Configure(2f, 10, GameVisualPalette.TankEnemy, enemyType);
                enemyTransform.localScale = Vector3.one * 1.5f;
                break;
            default:
                enemy.Configure(4f, 3, GameVisualPalette.NormalEnemy, enemyType);
                enemyTransform.localScale = Vector3.one;
                break;
        }
    }
}
