using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class GreenAncientWildsPlaytestSceneBuilder
{
    private static bool autoBuildAttempted;

    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PlaytestScenePath = "Assets/BonkSurvivor/Maps/GreenAncientWilds/GreenAncientWilds_Playtest.unity";
    private const string VisualsRootName = "GreenAncientWilds_Visuals";
    private const string MarkersRootName = "GreenAncientWilds_Markers";
    private const string MaterialsFolder = "Assets/BonkSurvivor/Maps/GreenAncientWilds/Materials";

    private const string PolytopePrefabsRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";

    private const string LegacyPolytopeWaterMatGuid = "3e36df56c6fa5f64085c14d9d4f6f8d9";
    private const string LegacyPolytopeTerrainMatGuid = "48c26796e9175d14a9b2eccded92bb92";

    private static readonly string[] SuspiciousMaterialTokens =
    {
        "PT_",
        "Polytope",
        "GrabPass",
        "Water_mat",
        "Terrain_mat",
    };

    private static readonly string[] LegacyVisualObjectNames =
    {
        "PT_GroundVisual",
        "PT_WaterDecor",
    };

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

    private struct MapMaterials
    {
        public Material Ground;
        public Material Grass;
        public Material Tree;
        public Material Rock;
        public Material Ruin;
        public Material Water;
    }

    private struct VisualValidationResult
    {
        public int RendererCount;
        public int MaterialsReplaced;
        public int SuspiciousRemaining;
    }

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

        string sceneText = File.ReadAllText(PlaytestScenePath);
        bool missingVisuals = !sceneText.Contains(VisualsRootName);
        bool hasLegacyPolytopeMaterials = sceneText.Contains(LegacyPolytopeWaterMatGuid)
            || sceneText.Contains(LegacyPolytopeTerrainMatGuid);
        bool hasLegacyVisualNames = sceneText.Contains("PT_GroundVisual") || sceneText.Contains("PT_WaterDecor");

        if (!missingVisuals && !hasLegacyPolytopeMaterials && !hasLegacyVisualNames)
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
        EnsureFolder("Assets/BonkSurvivor/Maps/GreenAncientWilds", "Materials");

        MapMaterials materials = LoadMapMaterials();
        if (!ValidateMapMaterials(materials))
        {
            return;
        }

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
        RemoveLegacyVisualObjects(scene);
        BuildMarkers(scene);

        GameObject visualsRoot = BuildVisualDressing(scene, materials);
        DisableCollidersRecursive(visualsRoot);

        VisualValidationResult validation = ValidateAndSanitizeVisuals(visualsRoot, materials);
        LogValidationResult(validation);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GreenAncientWildsPlaytestSceneBuilder] Playtest scene ready at " + PlaytestScenePath);
    }

    private static MapMaterials LoadMapMaterials()
    {
        return new MapMaterials
        {
            Ground = LoadOrCreateMapMaterial("GA_Ground_Mat", new Color(0.34f, 0.54f, 0.28f, 1f), false),
            Grass = LoadOrCreateMapMaterial("GA_Grass_Mat", new Color(0.4f, 0.72f, 0.3f, 1f), false),
            Tree = LoadOrCreateMapMaterial("GA_Tree_Mat", new Color(0.2f, 0.46f, 0.22f, 1f), false),
            Rock = LoadOrCreateMapMaterial("GA_Rock_Mat", new Color(0.45f, 0.43f, 0.4f, 1f), false),
            Ruin = LoadOrCreateMapMaterial("GA_Ruin_Mat", new Color(0.52f, 0.5f, 0.46f, 1f), false),
            Water = LoadOrCreateMapMaterial("GA_Water_Mat", new Color(0.18f, 0.42f, 0.62f, 0.62f), true),
        };
    }

    private static bool ValidateMapMaterials(MapMaterials materials)
    {
        if (materials.Ground == null || materials.Grass == null || materials.Tree == null
            || materials.Rock == null || materials.Ruin == null || materials.Water == null)
        {
            Debug.LogError("[GreenAncientWildsPlaytestSceneBuilder] Failed to load GA map materials.");
            return false;
        }

        return true;
    }

    private static Material LoadOrCreateMapMaterial(string materialName, Color baseColor, bool transparent)
    {
        string path = MaterialsFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = CreateLitMaterial(baseColor, transparent);
            material.name = materialName;
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
        }

        ConfigureLitMaterial(material, baseColor, transparent);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateLitMaterial(Color baseColor, bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError("[GreenAncientWildsPlaytestSceneBuilder] No URP Lit or Standard shader found.");
            return null;
        }

        Material material = new Material(shader);
        ConfigureLitMaterial(material, baseColor, transparent);
        return material;
    }

    private static void ConfigureLitMaterial(Material material, Color baseColor, bool transparent)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }

        if (!transparent)
        {
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            material.renderQueue = -1;
            return;
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void RemoveStagingRoots(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null)
            {
                continue;
            }

            if (root.name == VisualsRootName || root.name == MarkersRootName)
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
    }

    private static void RemoveLegacyVisualObjects(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            RemoveLegacyVisualObjectsRecursive(root);
        }
    }

    private static void RemoveLegacyVisualObjectsRecursive(GameObject current)
    {
        if (current == null)
        {
            return;
        }

        Transform[] children = new Transform[current.transform.childCount];
        for (int i = 0; i < children.Length; i++)
        {
            children[i] = current.transform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            RemoveLegacyVisualObjectsRecursive(child.gameObject);
        }

        for (int i = 0; i < LegacyVisualObjectNames.Length; i++)
        {
            if (current.name == LegacyVisualObjectNames[i])
            {
                UnityEngine.Object.DestroyImmediate(current);
                return;
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

    private static GameObject BuildVisualDressing(Scene scene, MapMaterials materials)
    {
        GameObject visualsRoot = new GameObject(VisualsRootName);
        SceneManager.MoveGameObjectToScene(visualsRoot, scene);

        CreateGroundVisual(visualsRoot.transform, materials.Ground);
        CreateWaterDecor(visualsRoot.transform, materials.Water);
        PlacePerimeterTrees(visualsRoot.transform, materials.Tree);
        PlacePerimeterRocks(visualsRoot.transform, materials.Rock);
        PlaceMenhirs(visualsRoot.transform, materials.Ruin);
        PlaceGrassClusters(visualsRoot.transform, materials.Grass);

        return visualsRoot;
    }

    private static void CreateGroundVisual(Transform parent, Material groundMaterial)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GA_GroundVisual";
        ground.transform.SetParent(parent, false);
        ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        ground.transform.localRotation = Quaternion.identity;
        ground.transform.localScale = new Vector3(18f, 1f, 18f);
        ApplyMaterialToRenderers(ground, groundMaterial);
        UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
    }

    private static void CreateWaterDecor(Transform parent, Material waterMaterial)
    {
        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "GA_WaterDecor";
        water.transform.SetParent(parent, false);
        water.transform.localPosition = new Vector3(0f, -0.15f, 82f);
        water.transform.localRotation = Quaternion.identity;
        water.transform.localScale = new Vector3(5f, 1f, 3f);
        ApplyMaterialToRenderers(water, waterMaterial);
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    private static void PlacePerimeterTrees(Transform parent, Material treeMaterial)
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
            GameObject instance = InstantiatePolytopePrefab(
                prefabPath,
                treesRoot,
                position,
                Quaternion.Euler(0f, yaw, 0f),
                treeMaterial);

            if (instance != null)
            {
                float scale = 0.9f + (PlacementHash(i, 11) % 26) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static void PlacePerimeterRocks(Transform parent, Material rockMaterial)
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
                Quaternion.Euler(0f, angles[i], 0f),
                rockMaterial);

            if (instance != null)
            {
                float scale = 0.85f + (PlacementHash(i, 23) % 41) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static void PlaceMenhirs(Transform parent, Material ruinMaterial)
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
            InstantiatePolytopePrefab(
                menhirPath,
                ruinsRoot,
                positions[i],
                Quaternion.Euler(0f, i * 27f, 0f),
                ruinMaterial);
        }
    }

    private static void PlaceGrassClusters(Transform parent, Material grassMaterial)
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
            GameObject instance = InstantiatePolytopePrefab(
                grassPath,
                grassRoot,
                position,
                Quaternion.identity,
                grassMaterial);

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
        Quaternion rotation,
        Material overrideMaterial)
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

        if (PrefabUtility.IsPartOfPrefabInstance(instance))
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        ApplyMaterialToRenderers(instance, overrideMaterial);
        return instance;
    }

    private static VisualValidationResult ValidateAndSanitizeVisuals(GameObject visualsRoot, MapMaterials materials)
    {
        VisualValidationResult result = new VisualValidationResult();
        if (visualsRoot == null)
        {
            return result;
        }

        Renderer[] renderers = visualsRoot.GetComponentsInChildren<Renderer>(true);
        result.RendererCount = renderers.Length;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material targetMaterial = ResolveGaMaterial(renderer.transform, materials);
            Material[] slots = renderer.sharedMaterials;
            bool replacedAny = false;

            for (int i = 0; i < slots.Length; i++)
            {
                Material slot = slots[i];
                if (slot == targetMaterial && !IsSuspiciousMaterial(slot))
                {
                    continue;
                }

                slots[i] = targetMaterial;
                replacedAny = true;
                result.MaterialsReplaced++;
            }

            if (replacedAny)
            {
                renderer.sharedMaterials = slots;
                EditorUtility.SetDirty(renderer);
            }

            foreach (Material slot in renderer.sharedMaterials)
            {
                if (IsSuspiciousMaterial(slot))
                {
                    result.SuspiciousRemaining++;
                    LogSuspiciousMaterialWarning(renderer, slot);
                }
            }
        }

        return result;
    }

    private static Material ResolveGaMaterial(Transform rendererTransform, MapMaterials materials)
    {
        Transform current = rendererTransform;
        while (current != null)
        {
            string name = current.name;
            if (name == "GA_GroundVisual")
            {
                return materials.Ground;
            }

            if (name == "GA_WaterDecor")
            {
                return materials.Water;
            }

            if (name == "Trees")
            {
                return materials.Tree;
            }

            if (name == "Rocks")
            {
                return materials.Rock;
            }

            if (name == "Ruins")
            {
                return materials.Ruin;
            }

            if (name == "Grass")
            {
                return materials.Grass;
            }

            current = current.parent;
        }

        return materials.Rock;
    }

    private static bool IsSuspiciousMaterial(Material material)
    {
        if (material == null)
        {
            return true;
        }

        if (material.name.StartsWith("GA_", StringComparison.Ordinal))
        {
            return false;
        }

        string shaderName = material.shader != null ? material.shader.name : string.Empty;
        string assetPath = AssetDatabase.GetAssetPath(material);
        string combined = material.name + " " + shaderName + " " + assetPath;

        for (int i = 0; i < SuspiciousMaterialTokens.Length; i++)
        {
            if (combined.IndexOf(SuspiciousMaterialTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void LogSuspiciousMaterialWarning(Renderer renderer, Material material)
    {
        string shaderName = material != null && material.shader != null ? material.shader.name : "<null>";
        string materialName = material != null ? material.name : "<null>";
        string path = material != null ? AssetDatabase.GetAssetPath(material) : "<null>";

        Debug.LogWarning(
            "[GreenAncientWildsPlaytestSceneBuilder] Suspicious material remains on '"
            + renderer.gameObject.name
            + "': material="
            + materialName
            + ", shader="
            + shaderName
            + ", path="
            + path);
    }

    private static void LogValidationResult(VisualValidationResult result)
    {
        Debug.Log(
            "[GreenAncientWildsPlaytestSceneBuilder] Validation: renderers="
            + result.RendererCount
            + ", materialsReplaced="
            + result.MaterialsReplaced
            + ", suspiciousRemaining="
            + result.SuspiciousRemaining);
    }

    private static void ApplyMaterialToRenderers(GameObject root, Material material)
    {
        if (root == null || material == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = material;
            }

            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
        }
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
