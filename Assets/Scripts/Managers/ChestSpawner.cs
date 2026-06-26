using UnityEngine;

public class ChestSpawner : MonoBehaviour
{
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private GameObject chestNormalPrefab;
    [SerializeField] private GameObject chestRarePrefab;
    [SerializeField] private GameObject chestEpicPrefab;
    [SerializeField] private Transform player;

    public void ResetRun()
    {
    }

    public void SpawnSeededMapChestsForRun(int runSeed)
    {
        MapChestSpawner.SpawnSeededMapChestsForRun(this, runSeed);
    }

    public void SpawnMapChestAt(Vector3 position, ChestRarity rarity, int currentWave)
    {
        if (!HasAnySpawnPrefab())
        {
            return;
        }

        GameObject prefab = ChestPrefabUtility.ResolveChestPrefab(rarity);

        if (prefab == null)
        {
            Debug.LogWarning("[ChestSpawner] Chest prefab missing for rarity " + rarity);
            return;
        }

        GameObject chestObject = Instantiate(prefab, position, Quaternion.identity);
        MapChestSpawner.ApplyMapChestWorldTransform(chestObject.transform, position);
        Chest chest = chestObject.GetComponent<Chest>();

        if (chest != null)
        {
            bool isMimic = MimicChestUtility.RollMimicForMapChest(currentWave);
            chest.ConfigureMapChest(rarity, isMimic);
            MapChestSpawner.SnapMapChestToGround(chestObject.transform);
        }
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

    private void Awake()
    {
        EnsureChestPrefabReferences();
        RegisterChestPrefabs();
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
}
