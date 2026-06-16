using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ChestPrefabUtility
{
    public const string DefaultChestPath = "Assets/Chest.prefab";
    public const string NormalChestPath = "Assets/Prefabs/Chests/Chest_Normal.prefab";
    public const string RareChestPath = "Assets/Prefabs/Chests/Chest_Rare.prefab";
    public const string EpicChestPath = "Assets/Prefabs/Chests/Chest_Epic.prefab";

    private static bool loggedMissingPrefab;

    public static GameObject ResolveChestPrefab(ChestRarity rarity)
    {
        GameObject prefab = ChestRarityUtility.GetChestPrefab(rarity);
        if (prefab != null)
        {
            return prefab;
        }

        ChestSpawner spawner = Object.FindFirstObjectByType<ChestSpawner>();
        if (spawner != null)
        {
            prefab = spawner.GetChestPrefabForRarity(rarity);
            if (prefab != null)
            {
                return prefab;
            }
        }

#if UNITY_EDITOR
        prefab = LoadEditorPrefab(GetPathForRarity(rarity));
        if (prefab != null)
        {
            return prefab;
        }

        return LoadEditorPrefab(DefaultChestPath);
#else
        LogMissingPrefabOnce();
        return null;
#endif
    }

    public static GameObject ResolveDefaultChestPrefab()
    {
        return ResolveChestPrefab(ChestRarity.Normal);
    }

    public static bool HasAnyChestPrefab()
    {
        return ResolveDefaultChestPrefab() != null;
    }

    public static void EnsureEditorPrefabsLoaded(
        ref GameObject defaultPrefab,
        ref GameObject normalPrefab,
        ref GameObject rarePrefab,
        ref GameObject epicPrefab)
    {
#if UNITY_EDITOR
        if (defaultPrefab == null)
        {
            defaultPrefab = LoadEditorPrefab(DefaultChestPath);
        }

        if (normalPrefab == null)
        {
            normalPrefab = LoadEditorPrefab(NormalChestPath);
        }

        if (rarePrefab == null)
        {
            rarePrefab = LoadEditorPrefab(RareChestPath);
        }

        if (epicPrefab == null)
        {
            epicPrefab = LoadEditorPrefab(EpicChestPath);
        }

        if (normalPrefab == null)
        {
            normalPrefab = defaultPrefab;
        }

        if (rarePrefab == null)
        {
            rarePrefab = defaultPrefab;
        }

        if (epicPrefab == null)
        {
            epicPrefab = defaultPrefab;
        }
#endif
    }

#if UNITY_EDITOR
    public static GameObject LoadEditorPrefab(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }
#endif

    public static string GetPathForRarity(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RareChestPath,
            ChestRarity.Epic => EpicChestPath,
            _ => NormalChestPath
        };
    }

    private static void LogMissingPrefabOnce()
    {
        if (loggedMissingPrefab)
        {
            return;
        }

        loggedMissingPrefab = true;
        Debug.LogWarning("[ChestDrop] Chest prefab is missing, cannot drop chest.");
    }
}
