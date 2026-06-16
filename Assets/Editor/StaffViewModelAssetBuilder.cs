using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class StaffViewModelAssetBuilder
{
    private const string StaffFbxPath = "Assets/Art/Weapons/Firestaff/KY Staff to SketchFab.fbx";
    private const string MaterialsFolder = "Assets/Art/Weapons/Firestaff/Materials";
    private const string MapsFolder = "Assets/Art/Weapons/Firestaff/Materials/Maps";
    private const string TexturesFolder = "Assets/Art/Weapons/Firestaff/Textures/textures";
    private const string SourceTexturesFolder = "Assets/Art/Weapons/Firestaff/Textures/source/KY Staff to SketchFab";
    private const string PrefabsFolder = "Assets/Prefabs/Weapons";
    private const string PrefabPath = PrefabsFolder + "/Staff_ViewModel.prefab";
    private const string SceneTestObjectName = "Staff_WorldTest";

    private const float ViewModelTargetHeight = 0.42f;
    private static readonly Vector3 WorldPreviewPosition = new Vector3(2f, 1f, 2f);

    private static float lastAppliedStaffScale = 1f;
    private static Vector3 lastStaffBoundsSize = Vector3.one;

    private const string WoodMatPath = MaterialsFolder + "/FireStaff_Wood.mat";
    private const string MetalMatPath = MaterialsFolder + "/FireStaff_Metal.mat";
    private const string CrystalMatPath = MaterialsFolder + "/FireStaff_Crystal_Emissive.mat";
    private const string DarkTrimMatPath = MaterialsFolder + "/FireStaff_DarkTrim.mat";

    private const string LegacyWoodMatPath = MaterialsFolder + "/M_Staff_Wood.mat";
    private const string LegacyMetalMatPath = MaterialsFolder + "/M_Staff_Metal.mat";
    private const string LegacyCrystalMatPath = MaterialsFolder + "/M_Staff_Crystal.mat";

    static StaffViewModelAssetBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingAssets;
    }

    [MenuItem("Tools/BonkSurvivor/Build Staff ViewModel Assets")]
    public static void BuildStaffViewModelAssets()
    {
        Shader urpLit = FindUrpLitShader();

        if (urpLit == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] URP Lit shader not found.");
            return;
        }

        EnsureFolder("Assets/Art/Weapons/Firestaff", "Materials");
        EnsureFolder(MaterialsFolder, "Maps");
        EnsureFolder("Assets/Prefabs", "Weapons");

        Material woodMaterial = CreateWoodMaterial(urpLit);
        Material metalMaterial = CreateMetalMaterial(urpLit);
        Material crystalMaterial = CreateCrystalMaterial(urpLit);
        Material darkTrimMaterial = CreateDarkTrimMaterial(urpLit);

        if (woodMaterial == null || metalMaterial == null || crystalMaterial == null || darkTrimMaterial == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] Failed to create one or more staff materials.");
            return;
        }

        RemapFbxMaterials(woodMaterial, metalMaterial, crystalMaterial, darkTrimMaterial);
        BuildStaffViewModelPrefab(woodMaterial, metalMaterial, crystalMaterial, darkTrimMaterial);
        SetupStaffWorldTestInOpenScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[StaffViewModelAssetBuilder] Staff materials and Staff_ViewModel prefab are ready."
            + " scale="
            + lastAppliedStaffScale.ToString("F3")
            + " bounds="
            + lastStaffBoundsSize);
    }

    [MenuItem("Tools/BonkSurvivor/Setup Staff World Test")]
    public static void SetupStaffWorldTestInOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        DetachEmbeddedStaffFromPlayer();
        RemoveExistingSceneTestStaff();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning("[StaffViewModelAssetBuilder] Staff_ViewModel prefab missing. Build assets first.");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] Failed to instantiate Staff_ViewModel prefab.");
            return;
        }

        instance.name = SceneTestObjectName;
        instance.transform.SetPositionAndRotation(WorldPreviewPosition, Quaternion.identity);
        instance.transform.localScale = Vector3.one;

        UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();

        if (activeScene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
        }

        Debug.Log("[StaffViewModelAssetBuilder] Placed " + SceneTestObjectName + " at " + WorldPreviewPosition);
    }

    [MenuItem("Tools/BonkSurvivor/Repair Staff ViewModel Prefab References")]
    public static void RepairStaffViewModelPrefabReferences()
    {
        if (!LoadOrCreateStaffMaterials(out Material woodMaterial, out Material metalMaterial, out Material crystalMaterial, out Material darkTrimMaterial))
        {
            return;
        }

        if (!File.Exists(PrefabPath))
        {
            Debug.LogError("[StaffViewModelAssetBuilder] Staff_ViewModel prefab not found at " + PrefabPath);
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

        if (prefabRoot == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] Could not load Staff_ViewModel prefab contents.");
            return;
        }

        Transform fireballSpawn = FindDeepChild(prefabRoot.transform, "FireballSpawnPoint");
        Transform meteorCast = FindDeepChild(prefabRoot.transform, "MeteorCastPoint");

        if (fireballSpawn == null || meteorCast == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] FireballSpawnPoint or MeteorCastPoint missing on prefab.");
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            return;
        }

        Vector3 fireballLocalPosition = fireballSpawn.localPosition;
        Vector3 meteorLocalPosition = meteorCast.localPosition;
        Quaternion fireballLocalRotation = fireballSpawn.localRotation;
        Quaternion meteorLocalRotation = meteorCast.localRotation;

        int repairedSlots = AssignMaterialsToRenderers(
            prefabRoot.transform,
            woodMaterial,
            metalMaterial,
            crystalMaterial,
            darkTrimMaterial,
            forceRepair: true);

        fireballSpawn.localPosition = fireballLocalPosition;
        fireballSpawn.localRotation = fireballLocalRotation;
        meteorCast.localPosition = meteorLocalPosition;
        meteorCast.localRotation = meteorLocalRotation;

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[StaffViewModelAssetBuilder] Repaired Staff_ViewModel prefab material references."
            + " slots="
            + repairedSlots
            + " wood="
            + AssetDatabase.GetAssetPath(woodMaterial)
            + " crystal="
            + AssetDatabase.GetAssetPath(crystalMaterial));
    }

    [MenuItem("Tools/BonkSurvivor/Refresh Staff ViewModel Materials")]
    public static void RefreshStaffViewModelMaterials()
    {
        if (!LoadOrCreateStaffMaterials(out Material woodMaterial, out Material metalMaterial, out Material crystalMaterial, out Material darkTrimMaterial))
        {
            return;
        }

        RemapFbxMaterials(woodMaterial, metalMaterial, crystalMaterial, darkTrimMaterial);
        RepairStaffViewModelPrefabReferences();
    }

    private static bool LoadOrCreateStaffMaterials(
        out Material woodMaterial,
        out Material metalMaterial,
        out Material crystalMaterial,
        out Material darkTrimMaterial)
    {
        woodMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoodMatPath);
        metalMaterial = AssetDatabase.LoadAssetAtPath<Material>(MetalMatPath);
        crystalMaterial = AssetDatabase.LoadAssetAtPath<Material>(CrystalMatPath);
        darkTrimMaterial = AssetDatabase.LoadAssetAtPath<Material>(DarkTrimMatPath);

        if (woodMaterial != null && metalMaterial != null && crystalMaterial != null && darkTrimMaterial != null)
        {
            return true;
        }

        Shader urpLit = FindUrpLitShader();

        if (urpLit == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] URP Lit shader not found.");
            return false;
        }

        woodMaterial = woodMaterial ?? CreateWoodMaterial(urpLit);
        metalMaterial = metalMaterial ?? CreateMetalMaterial(urpLit);
        crystalMaterial = crystalMaterial ?? CreateCrystalMaterial(urpLit);
        darkTrimMaterial = darkTrimMaterial ?? CreateDarkTrimMaterial(urpLit);

        if (woodMaterial == null || metalMaterial == null || crystalMaterial == null || darkTrimMaterial == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] Failed to load or create staff materials.");
            return false;
        }

        AssetDatabase.SaveAssets();
        return true;
    }

    private static void TryBuildMissingAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!File.Exists(StaffFbxPath))
        {
            return;
        }

        if (!File.Exists(WoodMatPath)
            || !File.Exists(MetalMatPath)
            || !File.Exists(CrystalMatPath)
            || !File.Exists(DarkTrimMatPath)
            || !File.Exists(PrefabPath))
        {
            Debug.Log("[StaffViewModelAssetBuilder] Missing staff art assets. Building URP materials and prefab.");
            BuildStaffViewModelAssets();
            return;
        }

        if (SceneNeedsStaffTestFix())
        {
            SetupStaffWorldTestInOpenScene();
        }
    }

    private static bool SceneNeedsStaffTestFix()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return false;
        }

        if (GameObject.Find(SceneTestObjectName) != null)
        {
            return false;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return true;
        }

        Transform[] transforms = player.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null || candidate == player.transform)
            {
                continue;
            }

            if (IsStaffHierarchy(candidate.gameObject))
            {
                return true;
            }
        }

        return true;
    }

    private static Shader FindUrpLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader != null)
        {
            return shader;
        }

        return AssetDatabase.LoadAssetAtPath<Shader>(
            "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader");
    }

    private static Material CreateWoodMaterial(Shader shader)
    {
        Material material = CreateOrUpdateMaterial(
            WoodMatPath,
            shader,
            "StrongWood",
            metallicScalar: 0.04f,
            smoothnessFallback: 0.38f,
            enableEmission: false);

        if (material != null)
        {
            material.SetColor("_BaseColor", new Color(1.04f, 0.92f, 0.78f, 1f));
        }

        return material;
    }

    private static Material CreateMetalMaterial(Shader shader)
    {
        Material material = CreateOrUpdateMaterial(
            MetalMatPath,
            shader,
            "Frame",
            metallicScalar: 0.9f,
            smoothnessFallback: 0.62f,
            enableEmission: false);

        if (material != null)
        {
            material.SetColor("_BaseColor", new Color(0.92f, 0.88f, 0.82f, 1f));
        }

        return material;
    }

    private static Material CreateCrystalMaterial(Shader shader)
    {
        Texture2D amberBody = LoadTexture(SourceTexturesFolder, "AmberBody", "BaseColor")
            ?? LoadTexture(TexturesFolder, "Flame_Emission", "BaseColor");

        Material material = CreateOrUpdateMaterial(
            CrystalMatPath,
            shader,
            "Flame_Emission",
            metallicScalar: 0.15f,
            smoothnessFallback: 0.42f,
            enableEmission: true,
            baseColorFallbackPath: SourceTexturesFolder + "/KY Assignment 8_AmberBody_BaseColor.png",
            emissionTexturePath: SourceTexturesFolder + "/KY Assignment 8_Flame_Emission_BaseColor.png",
            emissionColor: new Color(2.4f, 1.15f, 0.28f, 1f),
            baseColorTint: new Color(1.08f, 0.82f, 0.42f, 1f));

        if (material != null && amberBody != null)
        {
            material.SetTexture("_BaseMap", amberBody);
        }

        return material;
    }

    private static Material CreateDarkTrimMaterial(Shader shader)
    {
        Material material = CreateOrUpdateMaterial(
            DarkTrimMatPath,
            shader,
            "PanelColor",
            metallicScalar: 0.35f,
            smoothnessFallback: 0.28f,
            enableEmission: false,
            baseColorFallbackPath: TexturesFolder + "/KY Assignment 8_PanelColor_BaseColor.png");

        if (material != null)
        {
            material.SetColor("_BaseColor", new Color(0.42f, 0.38f, 0.34f, 1f));
        }

        return material;
    }

    private static bool PrefabNeedsMaterialRefresh()
    {
        if (!File.Exists(PrefabPath))
        {
            return false;
        }

        string prefabText = File.ReadAllText(PrefabPath);
        int metalCount = CountOccurrences(prefabText, "guid: " + LoadMaterialGuid(MetalMatPath));
        int woodCount = CountOccurrences(prefabText, "guid: " + LoadMaterialGuid(WoodMatPath));
        int crystalCount = CountOccurrences(prefabText, "guid: " + LoadMaterialGuid(CrystalMatPath));

        if (metalCount > 40 && woodCount == 0 && crystalCount == 0)
        {
            return true;
        }

        return false;
    }

    private static bool PrefabHasInvalidMaterialGuids()
    {
        if (!File.Exists(PrefabPath))
        {
            return false;
        }

        string prefabText = File.ReadAllText(PrefabPath);

        if (prefabText.Contains("guid: 00000000000000000000000000000000")
            || prefabText.Contains("guid: b7f4a9"))
        {
            return true;
        }

        System.Text.RegularExpressions.MatchCollection matches =
            System.Text.RegularExpressions.Regex.Matches(prefabText, @"guid:\s+([0-9a-fA-F]+)");

        for (int i = 0; i < matches.Count; i++)
        {
            string guid = matches[i].Groups[1].Value;

            if (guid.Length != 32)
            {
                return true;
            }
        }

        return false;
    }

    private static string LoadMaterialGuid(string materialPath)
    {
        string metaPath = materialPath + ".meta";

        if (!File.Exists(metaPath))
        {
            return string.Empty;
        }

        string[] lines = File.ReadAllLines(metaPath);

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("guid: "))
            {
                return lines[i].Substring("guid: ".Length).Trim();
            }
        }

        return string.Empty;
    }

    private static int CountOccurrences(string source, string needle)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(needle))
        {
            return 0;
        }

        int count = 0;
        int index = 0;

        while ((index = source.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static Material CreateOrUpdateMaterial(
        string materialPath,
        Shader shader,
        string texturePrefix,
        float metallicScalar,
        float smoothnessFallback,
        bool enableEmission,
        string baseColorFallbackPath = null,
        string emissionTexturePath = null,
        Color? emissionColor = null,
        Color? baseColorTint = null)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D baseColor = LoadTexture(TexturesFolder, texturePrefix, "BaseColor")
            ?? LoadTexture(SourceTexturesFolder, texturePrefix, "BaseColor");

        if (baseColor == null && !string.IsNullOrEmpty(baseColorFallbackPath))
        {
            baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorFallbackPath);
        }

        Texture2D normalMap = LoadTexture(TexturesFolder, texturePrefix, "Normal")
            ?? LoadTexture(SourceTexturesFolder, texturePrefix, "Normal");

        Texture2D metallicMap = LoadTexture(TexturesFolder, texturePrefix, "Metallic")
            ?? LoadTexture(SourceTexturesFolder, texturePrefix, "Metallic");

        Texture2D roughnessMap = LoadTexture(TexturesFolder, texturePrefix, "Roughness")
            ?? LoadTexture(SourceTexturesFolder, texturePrefix, "Roughness");

        ConfigureTextureImport(baseColor, TextureImporterType.Default, sRgb: true);
        ConfigureTextureImport(normalMap, TextureImporterType.NormalMap, sRgb: false);
        ConfigureTextureImport(metallicMap, TextureImporterType.Default, sRgb: false);
        ConfigureTextureImport(roughnessMap, TextureImporterType.Default, sRgb: false);

        if (baseColor != null)
        {
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", baseColorTint ?? Color.white);
        }

        Texture2D emissionTexture = null;

        if (!string.IsNullOrEmpty(emissionTexturePath))
        {
            emissionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionTexturePath);
        }

        emissionTexture = emissionTexture ?? baseColor;

        if (normalMap != null)
        {
            material.SetTexture("_BumpMap", normalMap);
            material.EnableKeyword("_NORMALMAP");
        }
        else
        {
            material.DisableKeyword("_NORMALMAP");
        }

        Texture2D metallicSmoothnessMap = BuildMetallicSmoothnessMap(
            materialPath,
            metallicMap,
            roughnessMap,
            smoothnessFallback);

        if (metallicSmoothnessMap != null)
        {
            material.SetTexture("_MetallicGlossMap", metallicSmoothnessMap);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
        }
        else
        {
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_Metallic", metallicScalar);
            material.SetFloat("_Smoothness", smoothnessFallback);
        }

        if (enableEmission && emissionTexture != null)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetColor("_EmissionColor", emissionColor ?? (Color.white * 1.35f));
            material.SetTexture("_EmissionMap", emissionTexture);
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string folder, string prefix, string suffix)
    {
        string path = folder + "/KY Assignment 8_" + prefix + "_" + suffix + ".png";

        if (!File.Exists(path))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void ConfigureTextureImport(Texture2D texture, TextureImporterType type, bool sRgb)
    {
        if (texture == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            return;
        }

        bool changed = false;

        if (importer.textureType != type)
        {
            importer.textureType = type;
            changed = true;
        }

        if (importer.sRGBTexture != sRgb)
        {
            importer.sRGBTexture = sRgb;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static Texture2D BuildMetallicSmoothnessMap(
        string materialPath,
        Texture2D metallicMap,
        Texture2D roughnessMap,
        float smoothnessFallback)
    {
        if (metallicMap == null && roughnessMap == null)
        {
            return null;
        }

        string mapName = Path.GetFileNameWithoutExtension(materialPath) + "_MS.png";
        string mapPath = MapsFolder + "/" + mapName;

        int width = Mathf.Max(metallicMap != null ? metallicMap.width : 0, roughnessMap != null ? roughnessMap.width : 0);
        int height = Mathf.Max(metallicMap != null ? metallicMap.height : 0, roughnessMap != null ? roughnessMap.height : 0);

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        Color[] metallicPixels = ReadTexturePixels(metallicMap, width, height, Color.black);
        Color[] roughnessPixels = ReadTexturePixels(roughnessMap, width, height, Color.white);

        Color[] combined = new Color[width * height];

        for (int i = 0; i < combined.Length; i++)
        {
            float metallic = metallicPixels[i].r;
            float smoothness = 1f - roughnessPixels[i].r;
            combined[i] = new Color(metallic, metallic, metallic, smoothness);
        }

        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, true, true);
        output.SetPixels(combined);
        output.Apply();

        File.WriteAllBytes(mapPath, output.EncodeToPNG());
        Object.DestroyImmediate(output);

        AssetDatabase.ImportAsset(mapPath, ImportAssetOptions.ForceUpdate);
        ConfigureTextureImport(AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath), TextureImporterType.Default, sRgb: false);

        return AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath);
    }

    private static Color[] ReadTexturePixels(Texture2D texture, int width, int height, Color fallback)
    {
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = fallback;
        }

        if (texture == null)
        {
            return pixels;
        }

        EnsureReadable(texture);
        Color[] source = texture.GetPixels();

        if (source.Length == pixels.Length)
        {
            return source;
        }

        for (int y = 0; y < height; y++)
        {
            float v = height <= 1 ? 0f : y / (float)(height - 1);
            int sourceY = Mathf.Clamp(Mathf.RoundToInt(v * (texture.height - 1)), 0, texture.height - 1);

            for (int x = 0; x < width; x++)
            {
                float u = width <= 1 ? 0f : x / (float)(width - 1);
                int sourceX = Mathf.Clamp(Mathf.RoundToInt(u * (texture.width - 1)), 0, texture.width - 1);
                pixels[y * width + x] = source[sourceY * texture.width + sourceX];
            }
        }

        return pixels;
    }

    private static void EnsureReadable(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null || importer.isReadable)
        {
            return;
        }

        importer.isReadable = true;
        importer.SaveAndReimport();
    }

    private static void RemapFbxMaterials(
        Material woodMaterial,
        Material metalMaterial,
        Material crystalMaterial,
        Material darkTrimMaterial)
    {
        ModelImporter importer = AssetImporter.GetAtPath(StaffFbxPath) as ModelImporter;

        if (importer == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] ModelImporter not found for staff FBX.");
            return;
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(StaffFbxPath);

        for (int i = 0; i < subAssets.Length; i++)
        {
            if (!(subAssets[i] is Material embeddedMaterial))
            {
                continue;
            }

            Material replacement = ResolveReplacementMaterial(
                embeddedMaterial.name,
                woodMaterial,
                metalMaterial,
                crystalMaterial,
                darkTrimMaterial);
            AssetImporter.SourceAssetIdentifier identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), embeddedMaterial.name);
            importer.AddRemap(identifier, replacement);
        }

        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.SaveAndReimport();
    }

    private static Material ResolveReplacementMaterial(
        string materialName,
        Material woodMaterial,
        Material metalMaterial,
        Material crystalMaterial,
        Material darkTrimMaterial)
    {
        string normalized = materialName.ToLowerInvariant();

        if (IsAssignedWoodMaterial(normalized))
        {
            return woodMaterial;
        }

        if (IsAssignedCrystalMaterial(normalized))
        {
            return crystalMaterial;
        }

        if (IsAssignedDarkTrimMaterial(normalized))
        {
            return darkTrimMaterial;
        }

        if (IsAssignedMetalMaterial(normalized))
        {
            return metalMaterial;
        }

        if (IsCrystalMaterialName(normalized))
        {
            return crystalMaterial;
        }

        if (IsWoodMaterialName(normalized) && !IsMetalStructuralName(normalized))
        {
            return woodMaterial;
        }

        if (IsDarkTrimMaterialName(normalized))
        {
            return darkTrimMaterial;
        }

        return metalMaterial;
    }

    private static bool IsAssignedWoodMaterial(string normalizedName)
    {
        return normalizedName.Contains("firestaff_wood")
            || normalizedName.Contains("m_staff_wood");
    }

    private static bool IsAssignedCrystalMaterial(string normalizedName)
    {
        return normalizedName.Contains("firestaff_crystal")
            || normalizedName.Contains("m_staff_crystal")
            || normalizedName.Contains("crystal_emissive");
    }

    private static bool IsAssignedMetalMaterial(string normalizedName)
    {
        return normalizedName.Contains("firestaff_metal")
            || normalizedName.Contains("m_staff_metal");
    }

    private static bool IsAssignedDarkTrimMaterial(string normalizedName)
    {
        return normalizedName.Contains("firestaff_dark")
            || normalizedName.Contains("darktrim");
    }

    private static bool IsWoodMaterialName(string normalizedName)
    {
        return normalizedName.Contains("strongwood")
            || normalizedName.Contains("grip")
            || normalizedName.Contains("lambert1")
            || normalizedName.Contains("vertical")
            || normalizedName.Contains("horizontal");
    }

    private static bool IsCrystalMaterialName(string normalizedName)
    {
        return normalizedName.Contains("amber")
            || normalizedName.Contains("flame")
            || normalizedName.Contains("lightbeam")
            || normalizedName.Contains("beamreciever")
            || normalizedName.Contains("beamreceiver")
            || normalizedName.Contains("electric_beam")
            || normalizedName.Contains("electricbeam")
            || normalizedName.Contains("energysphere")
            || normalizedName.Contains("energy")
            || normalizedName.Contains("scanring")
            || normalizedName.Contains("ignitorflame");
    }

    private static bool IsDarkTrimMaterialName(string normalizedName)
    {
        return normalizedName.Contains("wire")
            || normalizedName.Contains("panel")
            || normalizedName.Contains("trim")
            || (normalizedName.Contains("lever") && !normalizedName.Contains("leveroutline"))
            || normalizedName.Contains("phonge1")
            || normalizedName.Contains("phonge")
            || normalizedName.Contains("leverinside")
            || normalizedName.Contains("lighter");
    }

    private static bool IsMetalStructuralName(string normalizedName)
    {
        return normalizedName.Contains("frame")
            || normalizedName.Contains("hook")
            || normalizedName.Contains("component")
            || normalizedName.Contains("piece")
            || normalizedName.Contains("lock")
            || normalizedName.Contains("button")
            || normalizedName.Contains("leveroutline")
            || normalizedName.Contains("bodyframe")
            || normalizedName.Contains("electricframe")
            || normalizedName.Contains("scan")
            || normalizedName.Contains("ignitorbutton");
    }

    private static void BuildStaffViewModelPrefab(
        Material woodMaterial,
        Material metalMaterial,
        Material crystalMaterial,
        Material darkTrimMaterial)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StaffFbxPath);

        if (fbxAsset == null)
        {
            Debug.LogError("[StaffViewModelAssetBuilder] Could not load staff FBX asset.");
            return;
        }

        GameObject prefabRoot = new GameObject("Staff_ViewModel");
        GameObject staffRoot = new GameObject("StaffRoot");
        staffRoot.transform.SetParent(prefabRoot.transform, false);

        GameObject staffModel = Object.Instantiate(fbxAsset);
        staffModel.name = "StaffModel";
        staffModel.transform.SetParent(staffRoot.transform, false);
        staffModel.transform.localPosition = Vector3.zero;
        staffModel.transform.localRotation = Quaternion.identity;
        staffModel.transform.localScale = Vector3.one;

        Transform staffComponents = FindDeepChild(staffModel.transform, "Staff_Components") ?? staffModel.transform;
        AssignMaterialsToRenderers(
            staffComponents,
            woodMaterial,
            metalMaterial,
            crystalMaterial,
            darkTrimMaterial);

        lastAppliedStaffScale = FitStaffModelTransform(staffRoot.transform, staffModel.transform, ViewModelTargetHeight);

        Bounds staffBounds = CalculateRendererBounds(staffRoot.transform);
        lastStaffBoundsSize = staffBounds.size;
        Bounds crystalBounds = CalculateCrystalBounds(staffRoot.transform, staffBounds);

        GameObject fireballSpawn = new GameObject("FireballSpawnPoint");
        fireballSpawn.transform.SetParent(staffRoot.transform, false);
        fireballSpawn.transform.position = crystalBounds.center + staffRoot.transform.forward * crystalBounds.extents.magnitude * 0.18f;

        GameObject meteorCast = new GameObject("MeteorCastPoint");
        meteorCast.transform.SetParent(staffRoot.transform, false);
        meteorCast.transform.position = GetStaffTipPosition(staffRoot.transform, staffBounds);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        Debug.Log(
            "[StaffViewModelAssetBuilder] Saved prefab to "
            + PrefabPath
            + " | StaffModel scale="
            + lastAppliedStaffScale.ToString("F3")
            + " | bounds="
            + lastStaffBoundsSize);
    }

    private static float FitStaffModelTransform(Transform staffRoot, Transform staffModel, float targetHeight)
    {
        Bounds bounds = CalculateRendererBounds(staffModel);
        float sourceHeight = Mathf.Max(0.001f, bounds.size.y);
        float uniformScale = targetHeight / sourceHeight;
        staffModel.localScale = Vector3.one * uniformScale;

        bounds = CalculateRendererBounds(staffModel);
        staffModel.position += staffRoot.position - bounds.center;

        return uniformScale;
    }

    private static void DetachEmbeddedStaffFromPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return;
        }

        List<GameObject> toRemove = new List<GameObject>();
        Transform[] transforms = player.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate == null || candidate == player.transform)
            {
                continue;
            }

            if (!IsStaffHierarchy(candidate.gameObject))
            {
                continue;
            }

            toRemove.Add(candidate.gameObject);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            Object.DestroyImmediate(toRemove[i]);
        }
    }

    private static void RemoveExistingSceneTestStaff()
    {
        GameObject[] sceneObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < sceneObjects.Length; i++)
        {
            GameObject sceneObject = sceneObjects[i];

            if (sceneObject == null)
            {
                continue;
            }

            if (sceneObject.name == SceneTestObjectName
                || (sceneObject.name.Contains("KY Staff") && sceneObject.transform.parent == null))
            {
                Object.DestroyImmediate(sceneObject);
            }
        }
    }

    private static bool IsStaffHierarchy(GameObject candidate)
    {
        if (candidate.name.Contains("Staff")
            || candidate.name.Contains("SketchFab")
            || candidate.name.Contains("Firestaff"))
        {
            return true;
        }

        MeshFilter meshFilter = candidate.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            string meshPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);

            if (!string.IsNullOrEmpty(meshPath) && meshPath == StaffFbxPath)
            {
                return true;
            }
        }

        return false;
    }

    private static int AssignMaterialsToRenderers(
        Transform root,
        Material woodMaterial,
        Material metalMaterial,
        Material crystalMaterial,
        Material darkTrimMaterial,
        bool forceRepair = false)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        int repairedSlots = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changed = false;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                Material current = sharedMaterials[materialIndex];
                string lookupLabel = BuildMaterialLookupLabel(current, renderer.gameObject.name);
                Material replacement = ResolveReplacementMaterial(
                    lookupLabel,
                    woodMaterial,
                    metalMaterial,
                    crystalMaterial,
                    darkTrimMaterial);

                if (replacement == null)
                {
                    replacement = metalMaterial;
                }

                if (forceRepair || current != replacement || IsBrokenMaterialReference(current))
                {
                    sharedMaterials[materialIndex] = replacement;
                    changed = true;
                    repairedSlots++;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }
        }

        return repairedSlots;
    }

    private static bool IsBrokenMaterialReference(Material material)
    {
        if (material == null)
        {
            return true;
        }

        string assetPath = AssetDatabase.GetAssetPath(material);

        return string.IsNullOrEmpty(assetPath);
    }

    private static Bounds CalculateRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(root.position, Vector3.one * 0.25f);
        }

        return bounds;
    }

    private static Bounds CalculateCrystalBounds(Transform root, Bounds fallbackBounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            string label = renderer.gameObject.name;

            if (renderer.sharedMaterial != null)
            {
                label += renderer.sharedMaterial.name;
            }

            if (!IsCrystalMaterialName(label.ToLowerInvariant()))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds ? bounds : fallbackBounds;
    }

    private static Vector3 GetStaffTipPosition(Transform staffRoot, Bounds staffBounds)
    {
        Renderer[] renderers = staffRoot.GetComponentsInChildren<Renderer>(true);
        Vector3 tipLocal = staffRoot.InverseTransformPoint(staffBounds.center);
        float bestScore = float.MinValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            Vector3 localCenter = staffRoot.InverseTransformPoint(renderer.bounds.center);
            Vector3 localExtents = staffRoot.InverseTransformVector(renderer.bounds.extents);
            float score = localCenter.y + localExtents.y + localCenter.z * 0.35f;

            if (score > bestScore)
            {
                bestScore = score;
                tipLocal = localCenter + new Vector3(0f, localExtents.y, localExtents.z * 0.65f);
            }
        }

        return staffRoot.TransformPoint(tipLocal);
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static string BuildMaterialLookupLabel(Material currentMaterial, string objectName)
    {
        string materialPart = currentMaterial != null ? currentMaterial.name : string.Empty;
        return materialPart + " " + objectName;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string combined = parent + "/" + folderName;

        if (!AssetDatabase.IsValidFolder(combined))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
