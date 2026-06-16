using UnityEngine;

public class ChestSpawner : MonoBehaviour
{
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private GameObject chestNormalPrefab;
    [SerializeField] private GameObject chestRarePrefab;
    [SerializeField] private GameObject chestEpicPrefab;
    [SerializeField] private Transform player;

    private float timer;
    private const float SpawnInterval = 20f;
    private const int MaxChestCount = 3;
    private const float MinSpawnDistance = 8f;
    private const float MaxSpawnDistance = 18f;
    private const float ChestSpawnHeight = 0.5f;

    private void Awake()
    {
        EnsureChestPrefabReferences();
        RegisterChestPrefabs();
    }

    public void ResetRun()
    {
        timer = 0f;
    }

    public GameObject GetChestPrefabForRarity(ChestRarity rarity)
    {
        switch (rarity)
        {
            case ChestRarity.Rare:
                return chestRarePrefab != null ? chestRarePrefab : chestPrefab;
            case ChestRarity.Epic:
                return chestEpicPrefab != null ? chestEpicPrefab : chestPrefab;
            default:
                return chestNormalPrefab != null ? chestNormalPrefab : chestPrefab;
        }
    }

    private void Update()
    {
        if (!MainMenuManager.IsRunActive) return;
        if (player == null) return;
        if (!HasAnySpawnPrefab())
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= SpawnInterval)
        {
            TrySpawnChest();
            timer = 0f;
        }
    }

    private void EnsureChestPrefabReferences()
    {
        ChestPrefabUtility.EnsureEditorPrefabsLoaded(
            ref chestPrefab,
            ref chestNormalPrefab,
            ref chestRarePrefab,
            ref chestEpicPrefab);
    }

    private void RegisterChestPrefabs()
    {
        ChestRarityUtility.RegisterChestPrefabs(
            chestNormalPrefab,
            chestRarePrefab,
            chestEpicPrefab,
            chestPrefab);
    }

    private bool HasAnySpawnPrefab()
    {
        return chestPrefab != null
            || chestNormalPrefab != null
            || chestRarePrefab != null
            || chestEpicPrefab != null
            || ChestPrefabUtility.HasAnyChestPrefab();
    }

    private void TrySpawnChest()
    {
        if (LootLimits.GetChestCount() >= MaxChestCount)
        {
            return;
        }

        Debug.Log("[ChestSpawner] Trying random chest spawn...");

        if (!ProceduralGrassArena.TryGetSafeChestSpawnPoint(
                player.position,
                MinSpawnDistance,
                MaxSpawnDistance,
                1.2f,
                out Vector3 spawnPosition))
        {
            Debug.LogWarning("[ChestSpawner] Failed to find safe chest position");
            return;
        }

        ChestRarity rarity = ChestRarityUtility.RollRandomChestRarity();
        GameObject prefab = ChestPrefabUtility.ResolveChestPrefab(rarity);

        if (prefab == null)
        {
            Debug.LogWarning("[ChestSpawner] Chest prefab missing for rarity " + rarity);
            return;
        }

        GameObject chestObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Chest chest = chestObject.GetComponent<Chest>();

        if (chest != null)
        {
            int currentWave = 1;
            EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();

            if (enemySpawner != null)
            {
                currentWave = Mathf.Max(1, enemySpawner.CurrentWave);
            }

            bool isMimic = MimicChestUtility.RollMimicForMapChest(currentWave);
            chest.ConfigureMapChest(rarity, isMimic);
        }

        Debug.Log("[ChestSpawner] Spawned " + rarity + " chest at " + spawnPosition);
    }
}
