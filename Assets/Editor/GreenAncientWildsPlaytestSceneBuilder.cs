using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GreenAncientWildsPlaytestSceneBuilder
{
    private static bool autoBuildAttempted;

    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PlaytestScenePath = "Assets/BonkSurvivor/Maps/GreenAncientWilds/GreenAncientWilds_Playtest.unity";
    private const string VisualsRootName = "GreenAncientWilds_Visuals";
    private const string MarkersRootName = "GreenAncientWilds_Markers";

    private const string PolytopePrefabsRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
    private const string TerrainMaterialPath = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Terrain_mat.mat";
    private const string WaterMaterialPath = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Water_mat.mat";

    private static readonly string[] TreePrefabPaths =
    {
        PolytopePrefabsRoot + "/Trees/PT_Pine_Tree_03_green.prefab",
        PolytopePrefabsRoot + "/Trees/PT_Fruit_Tree_01_green.prefab",
        PolytopePrefabsRoot + "/Trees/PT_Pine_Tree_03_dead.prefab",
    };

    private static readonly string[] RockPrefabPaths =
    {
        PolytopePrefabsRoot + "/Rocks/PT_Generic_Rock_01.prefab",
        PolytopePrefabsRoot + "/Rocks/PT_Ore_Rock_01.prefab",
        PolytopePrefabsRoot + "/Rocks/PT_River_Rock_Pile_02.prefab",
    };

    static GreenAncientWildsPlaytestSceneBuilder()
    {
        EditorApplication.delayCall += TryAutoBuildIfNeeded;
    }

    [MenuItem("Tools/BonkSurvivor/Build Green Ancient Wilds Playtest Scene", false, 30)]
    public static void BuildGreenAncientWildsPlaytestSceneMenu()
    {
        BuildGreenAncientWildsPlaytestScene();
    }

    private static void TryAutoBuildIfNeeded()
    {
        if (autoBuildAttempted || EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            return;
        }

        if (!File.Exists(PlaytestScenePath))
        {
            return;
        }

        if (File.ReadAllText(PlaytestScenePath).Contains(VisualsRootName))
        {
            return;
        }

        autoBuildAttempted = true;
        BuildGreenAncientWildsPlaytestScene();
    }

    public static void BuildGreenAncientWildsPlaytestScene()
    {
        if (!File.Exists(SampleScenePath))
        {
            Debug.LogError("[GreenAncientWildsPlaytestSceneBuilder] SampleScene not found at " + SampleScenePath);
            return;
        }

        EnsureFolder("Assets/BonkSurvivor", "Maps");
        EnsureFolder("Assets/BonkSurvivor/Maps", "GreenAncientWilds");

        if (File.Exists(PlaytestScenePath))
        {
            AssetDatabase.DeleteAsset(PlaytestScenePath);
        }

        if (!AssetDatabase.CopyAsset(SampleScenePath, PlaytestScenePath))
        {
            Debug.LogError("[GreenAncientWildsPlaytestSceneBuilder] Failed to copy SampleScene.");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.OpenScene(PlaytestScenePath, OpenSceneMode.Single);
        RemoveStagingRoots(scene);
        BuildMarkers(scene);
        BuildVisualDressing(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GreenAncientWildsPlaytestSceneBuilder] Playtest scene ready at " + PlaytestScenePath);
    }

    private static void RemoveStagingRoots(Scene scene)
    {
        foreach (string rootName in new[] { VisualsRootName, MarkersRootName })
        {
            GameObject existing = GameObject.Find(rootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }

    private static void BuildMarkers(Scene scene)
    {
        GameObject markersRoot = new GameObject(MarkersRootName);
        SceneManager.MoveGameObjectToScene(markersRoot, scene);

        CreateMarker(markersRoot.transform, "PlayerStart", new Vector3(0f, 0.5f, 0f));
        CreateMarker(markersRoot.transform, "BossArena", new Vector3(0f, 0f, 58f));
        CreateMarker(markersRoot.transform, "ChestZone_A", new Vector3(-48f, 0f, -42f));
        CreateMarker(markersRoot.transform, "ChestZone_B", new Vector3(48f, 0f, -42f));
        CreateMarker(markersRoot.transform, "PortalEventArea", new Vector3(0f, 0f, -62f));
    }

    private static void CreateMarker(Transform parent, string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localRotation = Quaternion.identity;
        marker.transform.localScale = Vector3.one;
    }

    private static void BuildVisualDressing(Scene scene)
    {
        GameObject visualsRoot = new GameObject(VisualsRootName);
        SceneManager.MoveGameObjectToScene(visualsRoot, scene);

        CreateGroundVisual(visualsRoot.transform);
        CreateWaterDecor(visualsRoot.transform);
        PlacePerimeterTrees(visualsRoot.transform);
        PlacePerimeterRocks(visualsRoot.transform);
        PlaceMenhirs(visualsRoot.transform);
        PlaceGrassClusters(visualsRoot.transform);
        DisableCollidersRecursive(visualsRoot);
    }

    private static void CreateGroundVisual(Transform parent)
    {
        Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "PT_GroundVisual";
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        ground.transform.localRotation = Quaternion.identity;
        ground.transform.localScale = new Vector3(18f, 1f, 18f);

        if (terrainMaterial != null)
        {
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = terrainMaterial;
            }
        }

        Object.DestroyImmediate(ground.GetComponent<Collider>());
    }

    private static void CreateWaterDecor(Transform parent)
    {
        Material waterMaterial = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "PT_WaterDecor";
        water.transform.SetParent(parent, false);
        water.transform.localPosition = new Vector3(0f, -0.15f, 82f);
        water.transform.localRotation = Quaternion.identity;
        water.transform.localScale = new Vector3(5f, 1f, 3f);

        if (waterMaterial != null)
        {
            Renderer renderer = water.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = waterMaterial;
            }
        }

        Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    private static void PlacePerimeterTrees(Transform parent)
    {
        Transform treesRoot = new GameObject("Trees").transform;
        treesRoot.SetParent(parent, false);

        const float innerRadius = 58f;
        const float outerRadius = 72f;
        const int treeCount = 24;

        for (int i = 0; i < treeCount; i++)
        {
            float angle = i * Mathf.PI * 2f / treeCount;
            float radius = (i % 2 == 0) ? innerRadius : outerRadius;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            float yaw = -angle * Mathf.Rad2Deg + 90f;

            string prefabPath = TreePrefabPaths[i % TreePrefabPaths.Length];
            GameObject instance = InstantiatePolytopePrefab(prefabPath, treesRoot, position, Quaternion.Euler(0f, yaw, 0f));
            if (instance != null)
            {
                float scale = 0.9f + (PlacementHash(i, 11) % 26) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static void PlacePerimeterRocks(Transform parent)
    {
        Transform rocksRoot = new GameObject("Rocks").transform;
        rocksRoot.SetParent(parent, false);

        float[] angles = { 12f, 38f, 64f, 98f, 132f, 168f, 204f, 238f, 272f, 306f, 332f, 358f };
        const float radius = 50f;

        for (int i = 0; i < angles.Length; i++)
        {
            float radians = angles[i] * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
            string prefabPath = RockPrefabPaths[i % RockPrefabPaths.Length];
            GameObject instance = InstantiatePolytopePrefab(
                prefabPath,
                rocksRoot,
                position,
                Quaternion.Euler(0f, angles[i], 0f));

            if (instance != null)
            {
                float scale = 0.85f + (PlacementHash(i, 23) % 41) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static void PlaceMenhirs(Transform parent)
    {
        Transform ruinsRoot = new GameObject("Ruins").transform;
        ruinsRoot.SetParent(parent, false);

        string menhirPath = PolytopePrefabsRoot + "/Rocks/PT_Menhir_Rock_02.prefab";
        Vector3[] positions =
        {
            new Vector3(0f, 0f, 66f),
            new Vector3(66f, 0f, 0f),
            new Vector3(0f, 0f, -66f),
            new Vector3(-66f, 0f, 0f),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            InstantiatePolytopePrefab(menhirPath, ruinsRoot, positions[i], Quaternion.Euler(0f, i * 27f, 0f));
        }
    }

    private static void PlaceGrassClusters(Transform parent)
    {
        Transform grassRoot = new GameObject("Grass").transform;
        grassRoot.SetParent(parent, false);

        string grassPath = PolytopePrefabsRoot + "/Plants/PT_Grass_02.prefab";
        Vector3[] positions =
        {
            new Vector3(38f, 0f, 30f),
            new Vector3(-36f, 0f, 28f),
            new Vector3(34f, 0f, -32f),
            new Vector3(-38f, 0f, -30f),
            new Vector3(28f, 0f, 44f),
            new Vector3(-30f, 0f, -44f),
        };

        foreach (Vector3 position in positions)
        {
            GameObject instance = InstantiatePolytopePrefab(grassPath, grassRoot, position, Quaternion.identity);
            if (instance != null)
            {
                int hash = PlacementHash((int)position.x, (int)position.z);
                float scale = 1.1f + (hash % 41) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static GameObject InstantiatePolytopePrefab(
        string prefabPath,
        Transform parent,
        Vector3 position,
        Quaternion rotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[GreenAncientWildsPlaytestSceneBuilder] Missing prefab: " + prefabPath);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
        return instance;
    }

    private static void DisableCollidersRecursive(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private static void EnsureFolder(string parent, string child)
    {
        string combined = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(combined))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static int PlacementHash(int a, int b)
    {
        unchecked
        {
            return (a * 73856093) ^ (b * 19349663);
        }
    }
}
