using UnityEngine;

public class GoldenDragonEventManager : MonoBehaviour
{
    public static GoldenDragonEventManager Instance { get; private set; }

    private const int MinWave = 6;
    private const float TriggerChance = 0.03f;

    private int lastRollWave;
    private GoldenDragonController activeDragon;
    private EnemySpawner cachedSpawner;
    private Transform cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GoldenDragonEventManager>() != null) return;

        GameObject host = new GameObject("GoldenDragonEventManager");
        host.AddComponent<GoldenDragonEventManager>();
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
        if (!MainMenuManager.IsRunActive)
        {
            if (lastRollWave != 0 || activeDragon != null)
            {
                ResetRunState();
            }

            return;
        }

        if (activeDragon != null) return;

        TryRollSpawn();
    }

    public void ResetRunState()
    {
        lastRollWave = 0;
        activeDragon = null;
        cachedSpawner = null;
        cachedPlayer = null;
        GoldenDragonSpawnTracker.ResetRun();
    }

    public void NotifyDragonResolved(GoldenDragonController dragon)
    {
        if (activeDragon == dragon)
        {
            activeDragon = null;
        }
    }

    public bool DevSpawnGoldenDragon()
    {
        if (activeDragon != null || !GoldenDragonSpawnTracker.CanSpawn) return false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null) return false;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        int wave = cachedSpawner != null ? Mathf.Max(1, cachedSpawner.CurrentWave) : 6;
        cachedPlayer = playerObject.transform;
        return SpawnGoldenDragonAt(wave, cachedPlayer.position, 25f, 35f);
    }

    private void TryRollSpawn()
    {
        if (!GoldenDragonSpawnTracker.CanSpawn) return;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return;

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave) return;
        if (lastRollWave == currentWave) return;
        if (DragonBossSpawnTracker.IsDragonWave(currentWave)) return;
        if (currentWave % 5 == 0) return;

        lastRollWave = currentWave;

        if (Random.value > TriggerChance) return;

        TrySpawnGoldenDragon(currentWave);
    }

    private bool TrySpawnGoldenDragon(int wave)
    {
        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                cachedPlayer = playerObject.transform;
            }
        }

        if (cachedPlayer == null) return false;

        return SpawnGoldenDragonAt(wave, cachedPlayer.position, 28f, 34f);
    }

    private bool SpawnGoldenDragonAt(int wave, Vector3 playerPosition, float minDistance, float maxDistance)
    {
        if (activeDragon != null || !GoldenDragonSpawnTracker.CanSpawn) return false;

        Vector3 spawnPosition = GetSpawnPosition(playerPosition, minDistance, maxDistance);

        GameObject dragonObject = new GameObject("GoldenDragon");
        dragonObject.transform.position = spawnPosition;
        dragonObject.transform.localScale = Vector3.one * 1.65f;

        GoldenDragonController controller = dragonObject.AddComponent<GoldenDragonController>();
        controller.Initialize(this, wave);

        GoldenDragonSpawnTracker.RegisterActive();
        activeDragon = controller;
        RunEventMessageDisplay.ShowGoldenDragonAppears();
        return true;
    }

    public void ShowEscapeWarning()
    {
        RunEventMessageDisplay.ShowGoldenDragonEscaped();
    }

    private static Vector3 GetSpawnPosition(Vector3 playerPosition, float minDistance = 28f, float maxDistance = 34f)
    {
        Vector3 spawnPosition;

        if (ProceduralGrassArena.Instance != null)
        {
            spawnPosition = ProceduralGrassArena.Instance.GetSafePointInsideArena(minDistance, 4f);
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
            spawnPosition = new Vector3(
                playerPosition.x + randomCircle.x,
                ProceduralGrassArena.GetLootSpawnY(0.5f),
                playerPosition.z + randomCircle.y);
        }

        Vector3 awayFromPlayer = spawnPosition - playerPosition;
        awayFromPlayer.y = 0f;

        float minDistanceSqr = minDistance * minDistance;

        if (awayFromPlayer.sqrMagnitude < minDistanceSqr)
        {
            if (awayFromPlayer.sqrMagnitude < 0.001f)
            {
                awayFromPlayer = Random.insideUnitSphere;
                awayFromPlayer.y = 0f;
            }

            awayFromPlayer.Normalize();
            spawnPosition = playerPosition + awayFromPlayer * Random.Range(minDistance, maxDistance);
        }

        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition, 4f);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, minDistance, 2f);
        return spawnPosition;
    }
}
