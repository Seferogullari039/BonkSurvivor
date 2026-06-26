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
    private static int autoBuildRetryCount;
    private const int MaxAutoBuildRetries = 5;

    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PlaytestScenePath = "Assets/BonkSurvivor/Maps/GreenAncientWilds/GreenAncientWilds_Playtest.unity";
    private const string VisualsRootName = "GreenAncientWilds_Visuals";
    private const string MarkersRootName = "GreenAncientWilds_Markers";
    private const string MaterialsFolder = "Assets/BonkSurvivor/Maps/GreenAncientWilds/Materials";
    private const string PlaytestBootstrapName = "GreenAncientWilds_PlaytestBootstrap";
    private const string VisualDressingVersionMarker = "BossBoundary";
    private const string SlopeTestVersionMarker = "SlopeTest_A_GentleRamp";

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

    private class GreenAncientWildsPlaytestAutoRebuild : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            for (int i = 0; i < importedAssets.Length; i++)
            {
                string asset = importedAssets[i];
                if (asset.Contains("GreenAncientWildsPlaytestSceneBuilder.cs")
                    || asset.Contains("GreenAncientWildsPlaytestVisualSuppressor.cs"))
                {
                    autoBuildAttempted = false;
                    EditorApplication.delayCall += TryAutoBuildIfNeeded;
                    return;
                }
            }
        }
    }

    [MenuItem("Tools/BonkSurvivor/Build Green Ancient Wilds Playtest Scene", false, 30)]
    public static void BuildGreenAncientWildsPlaytestSceneMenu()
    {
        BuildGreenAncientWildsPlaytestScene();
    }

    public static void BuildFromCommandLine()
    {
        BuildGreenAncientWildsPlaytestScene();
        EditorApplication.Exit(0);
    }

    private static void TryAutoBuildIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
        {
            EditorApplication.delayCall += TryAutoBuildIfNeeded;
            return;
        }

        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += TryAutoBuildIfNeeded;
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

        bool missingVisualDressing = !sceneText.Contains(VisualDressingVersionMarker);
        bool missingSlopeTests = !sceneText.Contains(SlopeTestVersionMarker);
        bool missingPlaytestSuppressor = !sceneText.Contains("GreenAncientWildsPlaytestVisualSuppressor");

        if (!missingVisuals && !hasLegacyPolytopeMaterials && !hasLegacyVisualNames
            && !missingVisualDressing && !missingSlopeTests && !missingPlaytestSuppressor)
        {
            autoBuildAttempted = true;
            return;
        }

        if (autoBuildAttempted)
        {
            autoBuildAttempted = false;
        }

        BuildGreenAncientWildsPlaytestScene();

        if (!File.Exists(PlaytestScenePath))
        {
            EditorApplication.delayCall += TryAutoBuildIfNeeded;
            return;
        }

        sceneText = File.ReadAllText(PlaytestScenePath);
        if (!sceneText.Contains(VisualDressingVersionMarker)
            || !sceneText.Contains(SlopeTestVersionMarker)
            || !sceneText.Contains("GreenAncientWildsPlaytestVisualSuppressor"))
        {
            autoBuildRetryCount++;
            if (autoBuildRetryCount <= MaxAutoBuildRetries)
            {
                EditorApplication.delayCall += TryAutoBuildIfNeeded;
            }
            else
            {
                Debug.LogWarning(
                    "[GreenAncientWildsPlaytestSceneBuilder] Auto rebuild did not complete. "
                    + "Run Tools/BonkSurvivor/Build Green Ancient Wilds Playtest Scene manually.");
            }

            return;
        }

        autoBuildRetryCount = 0;
        autoBuildAttempted = true;
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
            Scene activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path == PlaytestScenePath)
            {
                EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            }

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
        SuppressLegacySceneVisuals(scene);
        BuildMarkers(scene);
        AttachPlaytestVisualSuppressor(scene);

        GameObject visualsRoot = BuildVisualDressing(scene, materials);
        DisableCollidersRecursive(visualsRoot);
        BuildSlopeTestAreas(scene, materials);

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
        CreateMarker(markersRoot.transform, "SlopeTest_A", new Vector3(22f, 0f, 20f));
        CreateMarker(markersRoot.transform, "SlopeTest_B", new Vector3(-20f, 0f, 22f));
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
        PlaceRockClusters(visualsRoot.transform, materials.Rock);
        PlaceMenhirs(visualsRoot.transform, materials.Ruin);
        PlaceBossArenaBoundary(visualsRoot.transform, materials.Rock, materials.Ruin);
        PlaceChestZoneDecor(visualsRoot.transform, materials, new Vector3(-48f, 0f, -42f), "ChestZoneDecor_A");
        PlaceChestZoneDecor(visualsRoot.transform, materials, new Vector3(48f, 0f, -42f), "ChestZoneDecor_B");
        PlacePortalAreaDecor(visualsRoot.transform, materials);
        PlaceGrassClusters(visualsRoot.transform, materials.Grass);

        return visualsRoot;
    }

    private static void BuildSlopeTestAreas(Scene scene, MapMaterials materials)
    {
        GameObject existing = GameObject.Find("SlopeTestAreas");

        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        GameObject slopeRoot = new GameObject("SlopeTestAreas");
        SceneManager.MoveGameObjectToScene(slopeRoot, scene);

        BuildGentleRampTestArea(slopeRoot.transform, materials, new Vector3(22f, 0f, 20f), 32f);
        BuildRidgeAndRampTestArea(slopeRoot.transform, materials, new Vector3(-20f, 0f, 22f), -28f);
    }

    private static void BuildGentleRampTestArea(Transform parent, MapMaterials materials, Vector3 worldCenter, float yawDegrees)
    {
        GameObject areaRoot = new GameObject("SlopeTest_A_GentleRamp");
        areaRoot.transform.SetParent(parent, false);
        areaRoot.transform.position = worldCenter;
        areaRoot.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        Transform visualRoot = CreateChild(areaRoot.transform, "Visual");
        Transform colliderRoot = CreateChild(areaRoot.transform, "Collider");
        Transform rocksRoot = CreateChild(areaRoot.transform, "Rocks");
        Transform markersRoot = CreateChild(areaRoot.transform, "Markers");

        CreateBoxRamp(
            visualRoot,
            colliderRoot,
            localPosition: new Vector3(0f, 0.58f, 0f),
            localRotation: Quaternion.Euler(-8.5f, 0f, 0f),
            size: new Vector3(8f, 0.22f, 10f),
            material: materials.Grass);

        CreateBoxRamp(
            visualRoot,
            colliderRoot,
            localPosition: new Vector3(0f, 1.12f, 4.35f),
            localRotation: Quaternion.identity,
            size: new Vector3(8f, 0.18f, 2.4f),
            material: materials.Ground);

        PlaceSlopeDecorRock(
            rocksRoot,
            materials.Rock,
            new Vector3(-3.2f, 0.18f, -4.8f),
            Quaternion.Euler(0f, 24f, 0f),
            0.85f);
        PlaceSlopeDecorRock(
            rocksRoot,
            materials.Rock,
            new Vector3(3.4f, 0.16f, -4.4f),
            Quaternion.Euler(0f, -18f, 0f),
            0.78f);

        CreateMarker(markersRoot, "SlopeTestMarker", new Vector3(0f, 1.6f, -5.5f));
    }

    private static void BuildRidgeAndRampTestArea(Transform parent, MapMaterials materials, Vector3 worldCenter, float yawDegrees)
    {
        GameObject areaRoot = new GameObject("SlopeTest_B_RidgeAndRamp");
        areaRoot.transform.SetParent(parent, false);
        areaRoot.transform.position = worldCenter;
        areaRoot.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        Transform visualRoot = CreateChild(areaRoot.transform, "Visual");
        Transform colliderRoot = CreateChild(areaRoot.transform, "Collider");
        Transform rocksRoot = CreateChild(areaRoot.transform, "Rocks");
        Transform markersRoot = CreateChild(areaRoot.transform, "Markers");

        CreateBoxObstacle(
            visualRoot,
            colliderRoot,
            localPosition: new Vector3(-2.4f, 0.68f, 0.8f),
            localRotation: Quaternion.identity,
            size: new Vector3(3.6f, 1.35f, 1.1f),
            material: materials.Rock);

        CreateBoxRamp(
            visualRoot,
            colliderRoot,
            localPosition: new Vector3(2.8f, 0.52f, 1.6f),
            localRotation: Quaternion.Euler(-10.5f, 0f, 0f),
            size: new Vector3(6f, 0.2f, 8f),
            material: materials.Grass);

        CreateBoxObstacle(
            visualRoot,
            colliderRoot,
            localPosition: new Vector3(2.8f, 1.05f, 5.2f),
            localRotation: Quaternion.identity,
            size: new Vector3(6f, 0.16f, 1.8f),
            material: materials.Ground);

        PlaceSlopeDecorRock(
            rocksRoot,
            materials.Ruin,
            new Vector3(-4.2f, 0.22f, 2.4f),
            Quaternion.Euler(0f, 12f, 0f),
            1.05f,
            PolytopePrefabsRoot + "/Rocks/PT_Menhir_Rock_02.prefab");
        PlaceSlopeDecorRock(
            rocksRoot,
            materials.Rock,
            new Vector3(5.1f, 0.14f, -2.6f),
            Quaternion.Euler(0f, -32f, 0f),
            0.72f);

        CreateMarker(markersRoot, "SlopeTestMarker", new Vector3(0f, 1.5f, -4.8f));
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }

    private static void CreateBoxRamp(
        Transform visualParent,
        Transform colliderParent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 size,
        Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "RampSurface";
        visual.transform.SetParent(visualParent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = localRotation;
        visual.transform.localScale = size;
        UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
        ApplyMaterialToRenderers(visual, material);

        GameObject colliderObject = new GameObject("RampBoxCollider");
        colliderObject.transform.SetParent(colliderParent, false);
        colliderObject.transform.localPosition = localPosition;
        colliderObject.transform.localRotation = localRotation;
        BoxCollider boxCollider = colliderObject.AddComponent<BoxCollider>();
        boxCollider.size = size;
        boxCollider.center = Vector3.zero;
    }

    private static void CreateBoxObstacle(
        Transform visualParent,
        Transform colliderParent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 size,
        Material material)
    {
        CreateBoxRamp(visualParent, colliderParent, localPosition, localRotation, size, material);

        Transform lastVisual = visualParent.childCount > 0 ? visualParent.GetChild(visualParent.childCount - 1) : null;

        if (lastVisual != null)
        {
            lastVisual.name = "RidgeWall";
        }

        Transform lastCollider = colliderParent.childCount > 0 ? colliderParent.GetChild(colliderParent.childCount - 1) : null;

        if (lastCollider != null)
        {
            lastCollider.name = "RidgeBoxCollider";
        }
    }

    private static void PlaceSlopeDecorRock(
        Transform parent,
        Material material,
        Vector3 localPosition,
        Quaternion localRotation,
        float scale,
        string prefabPath = null)
    {
        string path = string.IsNullOrEmpty(prefabPath) ? RockPrefabPaths[0] : prefabPath;
        GameObject instance = InstantiatePolytopePrefab(path, parent, localPosition, localRotation, material);

        if (instance == null)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = "DecorRock";
            fallback.transform.SetParent(parent, false);
            fallback.transform.localPosition = localPosition;
            fallback.transform.localRotation = localRotation;
            fallback.transform.localScale = Vector3.one * (0.5f * scale);
            UnityEngine.Object.DestroyImmediate(fallback.GetComponent<Collider>());
            ApplyMaterialToRenderers(fallback, material);
            return;
        }

        instance.transform.localScale = Vector3.one * scale;
        DisableCollidersRecursive(instance);
    }

    private static void SuppressLegacySceneVisuals(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject == null)
            {
                continue;
            }

            if (rootObject.name == "Plane")
            {
                DisableRenderersRecursive(rootObject);
            }
        }

        GameObject skylands = GameObject.Find("SkylandsVisualKit");
        if (skylands != null)
        {
            skylands.SetActive(false);
        }
    }

    private static void AttachPlaytestVisualSuppressor(Scene scene)
    {
        GameObject existing = GameObject.Find(PlaytestBootstrapName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }

        GameObject bootstrap = new GameObject(PlaytestBootstrapName);
        SceneManager.MoveGameObjectToScene(bootstrap, scene);
        bootstrap.AddComponent<GreenAncientWildsPlaytestVisualSuppressor>();
    }

    private static void DisableRenderersRecursive(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
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
        water.transform.localPosition = new Vector3(74f, -0.22f, 78f);
        water.transform.localRotation = Quaternion.identity;
        water.transform.localScale = new Vector3(2.2f, 1f, 1.6f);
        ApplyMaterialToRenderers(water, waterMaterial);
        UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    }

    private static void PlacePerimeterTrees(Transform parent, Material treeMaterial)
    {
        Transform treesRoot = new GameObject("Trees").transform;
        treesRoot.SetParent(parent, false);

        const float innerRadius = 52f;
        const float midRadius = 64f;
        const float outerRadius = 76f;
        const int treeCount = 36;

        for (int i = 0; i < treeCount; i++)
        {
            float angle = i * Mathf.PI * 2f / treeCount;
            float radius = (i % 3) switch
            {
                0 => innerRadius,
                1 => midRadius,
                _ => outerRadius,
            };
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

        float[] angles =
        {
            8f, 22f, 36f, 52f, 68f, 84f, 98f, 114f, 128f, 142f, 158f, 172f,
            188f, 202f, 218f, 232f, 248f, 262f, 276f, 292f,
        };
        const float radius = 48f;

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

    private static void PlaceRockClusters(Transform parent, Material rockMaterial)
    {
        Transform clustersRoot = new GameObject("RockClusters").transform;
        clustersRoot.SetParent(parent, false);

        float[] angles = { 18f, 45f, 72f, 108f, 135f, 162f, 198f, 225f, 252f, 288f, 315f, 342f };
        const float radius = 44f;
        string[] clusterPaths =
        {
            PolytopePrefabsRoot + "/Rocks/PT_River_Rock_Pile_02.prefab",
            PolytopePrefabsRoot + "/Rocks/PT_Ore_Rock_01_split.prefab",
        };

        for (int i = 0; i < angles.Length; i++)
        {
            float radians = angles[i] * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
            string prefabPath = clusterPaths[i % clusterPaths.Length];
            GameObject instance = InstantiatePolytopePrefab(
                prefabPath,
                clustersRoot,
                position,
                Quaternion.Euler(0f, angles[i] + 11f, 0f),
                rockMaterial);

            if (instance != null)
            {
                float scale = 1.05f + (PlacementHash(i, 37) % 31) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private static void PlaceBossArenaBoundary(Transform parent, Material rockMaterial, Material ruinMaterial)
    {
        Transform boundaryRoot = new GameObject("BossBoundary").transform;
        boundaryRoot.SetParent(parent, false);

        Transform rockGroup = new GameObject("BossBoundaryRocks").transform;
        rockGroup.SetParent(boundaryRoot, false);
        Transform ruinGroup = new GameObject("BossBoundaryRuins").transform;
        ruinGroup.SetParent(boundaryRoot, false);

        Vector3 bossCenter = new Vector3(0f, 0f, 58f);
        const float arcRadius = 34f;
        string rockPath = PolytopePrefabsRoot + "/Rocks/PT_Generic_Rock_01.prefab";
        string menhirPath = PolytopePrefabsRoot + "/Rocks/PT_Menhir_Rock_02.prefab";

        for (int i = -5; i <= 5; i++)
        {
            float t = i / 5f;
            float angle = Mathf.Lerp(35f, 145f, (t + 1f) * 0.5f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle) * arcRadius, 0f, Mathf.Sin(angle) * arcRadius);
            Vector3 position = bossCenter + offset;
            bool useMenhir = i == -5 || i == 5 || i == 0;
            string prefabPath = useMenhir ? menhirPath : rockPath;
            Material material = useMenhir ? ruinMaterial : rockMaterial;
            float yaw = angle * Mathf.Rad2Deg + 90f;

            GameObject instance = InstantiatePolytopePrefab(
                prefabPath,
                useMenhir ? ruinGroup : rockGroup,
                position,
                Quaternion.Euler(0f, yaw, 0f),
                material);

            if (instance != null && !useMenhir)
            {
                float scale = 0.95f + (PlacementHash(i, 59) % 21) * 0.01f;
                instance.transform.localScale = Vector3.one * scale;
            }
        }

        Vector3[] flankRocks =
        {
            new Vector3(-30f, 0f, 52f),
            new Vector3(30f, 0f, 52f),
            new Vector3(-26f, 0f, 64f),
            new Vector3(26f, 0f, 64f),
        };

        for (int i = 0; i < flankRocks.Length; i++)
        {
            InstantiatePolytopePrefab(
                PolytopePrefabsRoot + "/Rocks/PT_River_Rock_Pile_02.prefab",
                rockGroup,
                flankRocks[i],
                Quaternion.Euler(0f, i * 41f, 0f),
                rockMaterial);
        }
    }

    private static void PlaceChestZoneDecor(Transform parent, MapMaterials materials, Vector3 zoneCenter, string rootName)
    {
        Transform decorRoot = new GameObject(rootName).transform;
        decorRoot.SetParent(parent, false);

        Transform grassGroup = new GameObject("GrassProps").transform;
        grassGroup.SetParent(decorRoot, false);
        Transform rockGroup = new GameObject("RockProps").transform;
        rockGroup.SetParent(decorRoot, false);
        Transform shrubGroup = new GameObject("ShrubProps").transform;
        shrubGroup.SetParent(decorRoot, false);

        string grassPath = PolytopePrefabsRoot + "/Plants/PT_Grass_02.prefab";
        string rockPath = PolytopePrefabsRoot + "/Rocks/PT_Generic_Rock_01.prefab";
        string shrubPath = PolytopePrefabsRoot + "/Shrubs/PT_Generic_Shrub_01_green.prefab";

        float[] decorAngles = { 20f, 110f, 200f, 290f };
        const float decorRadius = 7f;

        for (int i = 0; i < decorAngles.Length; i++)
        {
            float radians = decorAngles[i] * Mathf.Deg2Rad;
            Vector3 position = zoneCenter + new Vector3(Mathf.Cos(radians) * decorRadius, 0f, Mathf.Sin(radians) * decorRadius);
            switch (i % 3)
            {
                case 0:
                    InstantiatePolytopePrefab(grassPath, grassGroup, position, Quaternion.identity, materials.Grass);
                    break;
                case 1:
                    InstantiatePolytopePrefab(rockPath, rockGroup, position, Quaternion.Euler(0f, decorAngles[i], 0f), materials.Rock);
                    break;
                default:
                    InstantiatePolytopePrefab(shrubPath, shrubGroup, position, Quaternion.Euler(0f, decorAngles[i] + 15f, 0f), materials.Grass);
                    break;
            }
        }
    }

    private static void PlacePortalAreaDecor(Transform parent, MapMaterials materials)
    {
        Transform portalRoot = new GameObject("PortalAreaDecor").transform;
        portalRoot.SetParent(parent, false);

        Transform deadTrees = new GameObject("PortalDeadTrees").transform;
        deadTrees.SetParent(portalRoot, false);
        Transform ruins = new GameObject("PortalRuins").transform;
        ruins.SetParent(portalRoot, false);
        Transform flowers = new GameObject("PortalFlowers").transform;
        flowers.SetParent(portalRoot, false);
        Transform rocks = new GameObject("PortalRocks").transform;
        rocks.SetParent(portalRoot, false);

        Vector3 portalCenter = new Vector3(0f, 0f, -62f);
        string deadTreePath = PolytopePrefabsRoot + "/Trees/PT_Pine_Tree_03_dead.prefab";
        string menhirPath = PolytopePrefabsRoot + "/Rocks/PT_Menhir_Rock_02.prefab";
        string poppyPath = PolytopePrefabsRoot + "/Flowers/PT_Poppy_02.prefab";
        string rockPilePath = PolytopePrefabsRoot + "/Rocks/PT_River_Rock_Pile_02.prefab";

        InstantiatePolytopePrefab(deadTreePath, deadTrees, portalCenter + new Vector3(-11f, 0f, 0f), Quaternion.Euler(0f, 18f, 0f), materials.Tree);
        InstantiatePolytopePrefab(deadTreePath, deadTrees, portalCenter + new Vector3(11f, 0f, 0f), Quaternion.Euler(0f, -18f, 0f), materials.Tree);
        InstantiatePolytopePrefab(menhirPath, ruins, portalCenter + new Vector3(-9f, 0f, 5f), Quaternion.Euler(0f, 12f, 0f), materials.Ruin);
        InstantiatePolytopePrefab(menhirPath, ruins, portalCenter + new Vector3(9f, 0f, 5f), Quaternion.Euler(0f, -12f, 0f), materials.Ruin);
        InstantiatePolytopePrefab(poppyPath, flowers, portalCenter + new Vector3(-4f, 0f, -3f), Quaternion.identity, materials.Grass);
        InstantiatePolytopePrefab(poppyPath, flowers, portalCenter + new Vector3(4f, 0f, -3f), Quaternion.identity, materials.Grass);
        InstantiatePolytopePrefab(rockPilePath, rocks, portalCenter + new Vector3(0f, 0f, -2f), Quaternion.Euler(0f, 33f, 0f), materials.Rock);
        InstantiatePolytopePrefab(
            PolytopePrefabsRoot + "/Shrubs/PT_Generic_Shrub_01_dead.prefab",
            rocks,
            portalCenter + new Vector3(0f, 0f, 6f),
            Quaternion.identity,
            materials.Grass);
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
            new Vector3(22f, 0f, -18f),
            new Vector3(-24f, 0f, 16f),
            new Vector3(18f, 0f, 36f),
            new Vector3(-20f, 0f, -36f),
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

            if (name == "Rocks" || name == "RockClusters" || name == "RockProps" || name == "PortalRocks"
                || name == "BossBoundaryRocks")
            {
                return materials.Rock;
            }

            if (name == "Ruins" || name == "PortalRuins" || name == "BossBoundaryRuins")
            {
                return materials.Ruin;
            }

            if (name == "Grass" || name == "GrassProps" || name == "ShrubProps" || name == "PortalFlowers"
                || name == "ChestZoneDecor_A" || name == "ChestZoneDecor_B" || name == "PortalAreaDecor")
            {
                return materials.Grass;
            }

            if (name == "PortalDeadTrees" || name == "Trees")
            {
                return materials.Tree;
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
