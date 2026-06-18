using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ChestPrefabBuilder
{
    private const string ChestsFolder = "Assets/Prefabs/Chests";
    private const string EnemyPrefabPath = "Assets/Enemy.prefab";
    private const string DragonBossPrefabPath = "Assets/Prefabs/Bosses/DragonBoss.prefab";

    private static readonly string[] RequiredPrefabPaths =
    {
        "Assets/Chest.prefab",
        $"{ChestsFolder}/Chest_Normal.prefab",
        $"{ChestsFolder}/Chest_Rare.prefab",
        $"{ChestsFolder}/Chest_Epic.prefab"
    };

    private const string CommonMaterialPath = "Assets/Prefabs/Chests/Materials/Chest_Common_Mat.mat";

    static ChestPrefabBuilder()
    {
        EditorApplication.delayCall += TryRebuildMissingPrefabs;
    }

    [MenuItem("Tools/BonkSurvivor/Rebuild Chest Prefabs")]
    public static void RebuildChestPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Chests");
        ChestMaterialFactory.EnsureChestMaterials();

        SaveChestPrefab("Assets/Chest.prefab", ChestRarity.Normal, "Chest");
        SaveChestPrefab($"{ChestsFolder}/Chest_Normal.prefab", ChestRarity.Normal, "Chest_Normal");
        SaveChestPrefab($"{ChestsFolder}/Chest_Rare.prefab", ChestRarity.Rare, "Chest_Rare");
        SaveChestPrefab($"{ChestsFolder}/Chest_Epic.prefab", ChestRarity.Epic, "Chest_Epic");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ChestPrefabBuilder] Rebuilt Chest, Chest_Normal, Chest_Rare, and Chest_Epic prefabs.");
    }

    [MenuItem("Tools/BonkSurvivor/Auto Assign Chest Prefabs")]
    public static void AutoAssignChestPrefabs()
    {
        GameObject defaultChest = ChestPrefabUtility.LoadEditorPrefab(ChestPrefabUtility.DefaultChestPath);
        GameObject normalChest = ChestPrefabUtility.LoadEditorPrefab(ChestPrefabUtility.NormalChestPath);
        GameObject rareChest = ChestPrefabUtility.LoadEditorPrefab(ChestPrefabUtility.RareChestPath);
        GameObject epicChest = ChestPrefabUtility.LoadEditorPrefab(ChestPrefabUtility.EpicChestPath);

        if (defaultChest == null)
        {
            Debug.LogWarning("[ChestPrefabBuilder] Default chest prefab missing. Run Rebuild Chest Prefabs first.");
            return;
        }

        if (normalChest == null) normalChest = defaultChest;
        if (rareChest == null) rareChest = defaultChest;
        if (epicChest == null) epicChest = defaultChest;

        AssignSceneChestSpawner(defaultChest, normalChest, rareChest, epicChest);
        AssignPrefabChestReference(EnemyPrefabPath, defaultChest);
        AssignPrefabChestReference(DragonBossPrefabPath, defaultChest);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ChestPrefabBuilder] Auto assigned chest prefab references.");
    }

    private static void AssignSceneChestSpawner(
        GameObject defaultChest,
        GameObject normalChest,
        GameObject rareChest,
        GameObject epicChest)
    {
        ChestSpawner spawner = Object.FindFirstObjectByType<ChestSpawner>(FindObjectsInactive.Include);
        if (spawner == null)
        {
            Debug.LogWarning("[ChestPrefabBuilder] ChestSpawner not found in open scenes.");
            return;
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        SetPrefabReference(serializedSpawner, "chestPrefab", defaultChest);
        SetPrefabReference(serializedSpawner, "chestNormalPrefab", normalChest);
        SetPrefabReference(serializedSpawner, "chestRarePrefab", rareChest);
        SetPrefabReference(serializedSpawner, "chestEpicPrefab", epicChest);
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(spawner);

        if (spawner.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }
    }

    private static void AssignPrefabChestReference(string prefabPath, GameObject chestPrefab)
    {
        if (!File.Exists(prefabPath))
        {
            return;
        }

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabContents == null)
        {
            return;
        }

        try
        {
            Enemy enemy = prefabContents.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = prefabContents.GetComponentInChildren<Enemy>(true);
            }

            if (enemy == null)
            {
                return;
            }

            SerializedObject serializedEnemy = new SerializedObject(enemy);
            SetPrefabReference(serializedEnemy, "chestPrefab", chestPrefab);
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void SetPrefabReference(SerializedObject serializedObject, string propertyName, GameObject prefab)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = prefab;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string combined = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(combined))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void SaveChestPrefab(string assetPath, ChestRarity rarity, string objectName)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }

        GameObject root = new GameObject(objectName);
        root.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        root.transform.localScale = Vector3.one;

        BoxCollider collider = root.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = Vector3.one;
        collider.center = Vector3.zero;

        root.AddComponent<Chest>();

        ChestVisual visual = root.AddComponent<ChestVisual>();
        visual.rarity = rarity;
        visual.buildOnAwake = false;
        visual.BuildVisual();
        BakePrefabMaterialReferences(root, rarity);

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);

        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (savedPrefab == null)
        {
            Debug.LogError("[ChestPrefabBuilder] Failed to save prefab at " + assetPath);
            return;
        }

        Debug.Log("[ChestPrefabBuilder] Saved " + assetPath + " (" + rarity + ")");
    }

    public static void BuildFromCommandLine()
    {
        RebuildChestPrefabs();
        EditorApplication.Exit(0);
    }

    private static void BakePrefabMaterialReferences(GameObject root, ChestRarity rarity)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string partName = renderer.gameObject.name;

            if (partName == "ChestGlow" || partName == "Glow")
            {
                ChestVisualMaterials.ApplyGlow(renderer, rarity, 0.42f);
            }
            else if (partName == "Lock")
            {
                ChestVisualMaterials.ApplyLock(renderer, rarity);
            }
            else if (partName.StartsWith("MetalBand"))
            {
                ChestVisualMaterials.ApplyTrim(renderer, rarity);
            }
            else if (partName == "ChestBase" || partName == "ChestLid" || partName == "Body" || partName == "Lid")
            {
                ChestVisualMaterials.ApplyBody(renderer, rarity);
            }
        }
    }

    private static void TryRebuildMissingPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        for (int i = 0; i < RequiredPrefabPaths.Length; i++)
        {
            if (!File.Exists(RequiredPrefabPaths[i]))
            {
                Debug.LogWarning("[ChestPrefabBuilder] Missing prefab detected. Rebuilding chest prefabs.");
                RebuildChestPrefabs();
                return;
            }
        }

        if (!File.Exists(CommonMaterialPath) || PrefabsHaveMissingMaterials() || PrefabsUseLegacyVisualHierarchy())
        {
            Debug.LogWarning("[ChestPrefabBuilder] Chest prefabs need visual refresh. Rebuilding chest prefabs.");
            RebuildChestPrefabs();
        }
    }

    private static bool PrefabsUseLegacyVisualHierarchy()
    {
        for (int i = 0; i < RequiredPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RequiredPrefabPaths[i]);

            if (prefab == null)
            {
                continue;
            }

            Transform visualRoot = prefab.transform.Find("ChestVisualRoot");

            if (visualRoot == null)
            {
                return true;
            }

            if (visualRoot.Find("ChestBase") == null && visualRoot.Find("Body") != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PrefabsHaveMissingMaterials()
    {
        for (int i = 0; i < RequiredPrefabPaths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RequiredPrefabPaths[i]);

            if (prefab == null)
            {
                continue;
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];

                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    if (materials[materialIndex] == null)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
