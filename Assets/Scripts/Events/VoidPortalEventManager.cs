using UnityEngine;

public class VoidPortalEventManager : MonoBehaviour
{
    public static VoidPortalEventManager Instance { get; private set; }

    private const int MinWave = 8;
    private const float TriggerChance = 0.04f;

    private int lastRollWave;
    private VoidPortalController activePortal;
    private EnemySpawner cachedSpawner;
    private Transform cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<VoidPortalEventManager>() != null) return;

        GameObject host = new GameObject("VoidPortalEventManager");
        host.AddComponent<VoidPortalEventManager>();
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
            if (lastRollWave != 0 || activePortal != null)
            {
                ResetRunState();
            }

            return;
        }

        if (activePortal != null) return;

        TryRollSpawn();
    }

    public void ResetRunState()
    {
        lastRollWave = 0;
        activePortal = null;
        cachedSpawner = null;
        cachedPlayer = null;
        VoidPortalSpawnTracker.ResetRun();
    }

    public void NotifyPortalClosed(VoidPortalController portal)
    {
        if (activePortal == portal)
        {
            activePortal = null;
        }
    }

    public bool DevTriggerVoidPortal()
    {
        if (activePortal != null || !VoidPortalSpawnTracker.CanSpawn) return false;
        if (!MainMenuManager.IsRunActive) return false;

        return SpawnPortalAtPlayer();
    }

    private void TryRollSpawn()
    {
        if (!CanRollPortalEvent()) return;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return;

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave) return;
        if (lastRollWave == currentWave) return;
        if (IsBlockedWave(currentWave)) return;

        lastRollWave = currentWave;

        if (Random.value > TriggerChance) return;

        TrySpawnPortal(currentWave);
    }

    private static bool CanRollPortalEvent()
    {
        if (!VoidPortalSpawnTracker.CanSpawn) return false;
        if (!GoldenDragonSpawnTracker.CanSpawn) return false;

        if (BloodMoonEventManager.Instance != null && BloodMoonEventManager.Instance.IsActive)
        {
            return false;
        }

        return true;
    }

    private static bool IsBlockedWave(int wave)
    {
        if (wave % 5 == 0) return true;

        return DragonBossSpawnTracker.IsDragonWave(wave);
    }

    private bool TrySpawnPortal(int wave)
    {
        if (!CanRollPortalEvent()) return false;

        return SpawnPortalAtPlayer();
    }

    private bool SpawnPortalAtPlayer()
    {
        if (activePortal != null || !VoidPortalSpawnTracker.CanSpawn) return false;

        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                cachedPlayer = playerObject.transform;
            }
        }

        if (cachedPlayer == null) return false;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return false;

        Vector3 spawnPosition = GetSpawnPosition(cachedPlayer.position);

        GameObject portalObject = new GameObject("VoidPortal");
        portalObject.transform.position = spawnPosition;

        VoidPortalController controller = portalObject.AddComponent<VoidPortalController>();
        controller.Initialize(this, cachedSpawner);

        VoidPortalSpawnTracker.RegisterActive();
        activePortal = controller;
        RunEventMessageDisplay.ShowVoidPortalOpens();
        return true;
    }

    public void ShowCloseWarning()
    {
        RunEventMessageDisplay.ShowVoidPortalClosed();
    }

    private static Vector3 GetSpawnPosition(Vector3 playerPosition)
    {
        const float minDistance = 20f;
        const float maxDistance = 30f;

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
        Vector3 spawnPosition = new Vector3(
            playerPosition.x + randomCircle.x,
            ProceduralGrassArena.GetLootSpawnY(0.5f),
            playerPosition.z + randomCircle.y);

        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition, 4f);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, minDistance, 2f);
        return spawnPosition;
    }
}
