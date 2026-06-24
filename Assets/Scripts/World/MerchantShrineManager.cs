using UnityEngine;

public class MerchantShrineManager : MonoBehaviour
{
    public static MerchantShrineManager Instance { get; private set; }

    private const int MinWave = 3;
    private const float TriggerChance = 0.12f;
    private const float MinSpawnDistance = 18f;
    private const float MaxSpawnDistance = 28f;
    private const float ShrineObjectRadius = 1.1f;

    private int lastRollWave;
    private MerchantShrineController activeShrine;
    private EnemySpawner cachedSpawner;
    private Transform cachedPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<MerchantShrineManager>() != null)
        {
            return;
        }

        GameObject host = new GameObject("MerchantShrineManager");
        host.AddComponent<MerchantShrineManager>();
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

        if (activeShrine != null || MerchantShrineUI.IsOpen)
        {
            return;
        }

        TryRollSpawn();
    }

    public void ResetRunState()
    {
        lastRollWave = 0;
        cachedSpawner = null;
        cachedPlayer = null;
        DestroyActiveShrine();
    }

    public void NotifyShrineClosed(MerchantShrineController shrine)
    {
        if (activeShrine == shrine)
        {
            activeShrine = null;
        }
    }

    public bool DevSpawnMerchant()
    {
        if (activeShrine != null || !MainMenuManager.IsRunActive || MerchantShrineUI.IsOpen)
        {
            return false;
        }

        CachePlayer();

        if (cachedPlayer == null)
        {
            return false;
        }

        return TrySpawnShrineAt(cachedPlayer.position);
    }

    private void TryRollSpawn()
    {
        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null)
        {
            return;
        }

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave)
        {
            return;
        }

        if (IsBossWave(currentWave))
        {
            return;
        }

        if (lastRollWave == currentWave)
        {
            return;
        }

        lastRollWave = currentWave;

        if (Random.value > TriggerChance)
        {
            return;
        }

        CachePlayer();

        if (cachedPlayer == null)
        {
            return;
        }

        TrySpawnShrineAt(cachedPlayer.position);
    }

    private static bool IsBossWave(int wave)
    {
        if (DragonBossSpawnTracker.IsDragonWave(wave))
        {
            return true;
        }

        return wave > 0 && wave % 5 == 0;
    }

    private void CachePlayer()
    {
        if (cachedPlayer != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            cachedPlayer = playerObject.transform;
        }
    }

    private bool TrySpawnShrineAt(Vector3 playerPosition)
    {
        if (activeShrine != null)
        {
            return false;
        }

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

        GameObject shrineObject = new GameObject("MerchantShrine");
        shrineObject.transform.position = spawnPosition;

        MerchantShrineController controller = shrineObject.AddComponent<MerchantShrineController>();
        controller.Initialize(this);

        activeShrine = controller;
        ShowSpawnNotification();
        return true;
    }

    private void DestroyActiveShrine()
    {
        if (activeShrine == null)
        {
            return;
        }

        if (activeShrine.gameObject != null)
        {
            Destroy(activeShrine.gameObject);
        }

        activeShrine = null;
    }

    private static void ShowSpawnNotification()
    {
        RunEventMessageDisplay.Show(
            "MYSTIC MERCHANT ARRIVED",
            new Color(0.78f, 0.62f, 0.95f, 1f),
            2.4f,
            RunEventMessageDisplay.Priority.Event);
    }
}
