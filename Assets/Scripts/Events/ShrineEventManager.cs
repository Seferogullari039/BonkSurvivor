using UnityEngine;

public class ShrineEventManager : MonoBehaviour
{
    public static ShrineEventManager Instance { get; private set; }

    private const int MinWave = 5;
    private const float TriggerChance = 0.12f;
    private const int CooldownWaves = 2;
    private const float MinSpawnDistance = 18f;
    private const float MaxSpawnDistance = 45f;
    private const float ShrineObjectRadius = 1.4f;

    private int lastRollWave;
    private int cooldownUntilWave;
    private ShrineEventController activeShrine;
    private EnemySpawner cachedSpawner;
    private Transform cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ShrineEventManager>() != null) return;

        GameObject host = new GameObject("ShrineEventManager");
        host.AddComponent<ShrineEventManager>();
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
            if (lastRollWave != 0 || activeShrine != null)
            {
                ResetRunState();
            }

            return;
        }

        if (activeShrine != null) return;

        TryRollSpawn();
    }

    public void ResetRunState()
    {
        lastRollWave = 0;
        cooldownUntilWave = 0;
        activeShrine = null;
        cachedSpawner = null;
        cachedPlayer = null;
    }

    public void NotifyShrineResolved(ShrineEventController shrine)
    {
        if (activeShrine != shrine)
        {
            return;
        }

        activeShrine = null;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        int currentWave = cachedSpawner != null ? Mathf.Max(1, cachedSpawner.CurrentWave) : MinWave;
        cooldownUntilWave = currentWave + CooldownWaves;
    }

    public bool DevSpawnShrine()
    {
        if (activeShrine != null || !MainMenuManager.IsRunActive) return false;

        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                cachedPlayer = playerObject.transform;
            }
        }

        if (cachedPlayer == null) return false;

        return TrySpawnShrineAt(cachedPlayer.position);
    }

    private void TryRollSpawn()
    {
        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return;

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave) return;
        if (lastRollWave == currentWave) return;
        if (currentWave < cooldownUntilWave) return;

        lastRollWave = currentWave;

        if (Random.value > TriggerChance) return;

        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                cachedPlayer = playerObject.transform;
            }
        }

        if (cachedPlayer == null) return;

        TrySpawnShrineAt(cachedPlayer.position);
    }

    private bool TrySpawnShrineAt(Vector3 playerPosition)
    {
        if (activeShrine != null) return false;

        if (!ProceduralGrassArena.TryGetSafeChestSpawnPoint(
                playerPosition,
                MinSpawnDistance,
                MaxSpawnDistance,
                ShrineObjectRadius,
                out Vector3 spawnPosition))
        {
            return false;
        }

        Vector3 flatPlayer = playerPosition;
        flatPlayer.y = 0f;
        Vector3 flatSpawn = spawnPosition;
        flatSpawn.y = 0f;
        float distanceSqr = (flatSpawn - flatPlayer).sqrMagnitude;
        float minDistanceSqr = MinSpawnDistance * MinSpawnDistance;
        float maxDistanceSqr = MaxSpawnDistance * MaxSpawnDistance;

        if (distanceSqr < minDistanceSqr || distanceSqr > maxDistanceSqr)
        {
            return false;
        }

        GameObject shrineObject = new GameObject("ShrineEvent");
        shrineObject.transform.position = spawnPosition;

        ShrineEventController controller = shrineObject.AddComponent<ShrineEventController>();
        controller.Initialize(this);

        activeShrine = controller;
        return true;
    }
}
