using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class FPSArmsViewModelAssetBuilder
{
    private const string FpsArmsFolder = "Assets/Art/Characters/FPSArms";
    private const string ModelsFolder = FpsArmsFolder + "/Models";
    private const string PreferredModelPath = ModelsFolder + "/baconhand_smg.fbx";
    private const string PrefabsFolder = "Assets/Prefabs/Characters";
    private const string PrefabPath = PrefabsFolder + "/FPS_Arms_ViewModel.prefab";

    private static readonly Vector3 ArmsRootLocalPosition = new Vector3(0f, -0.25f, 0.35f);

    private static readonly string[] ModelExtensions = { ".fbx", ".obj" };

    private static readonly string[] WeaponMeshKeywords =
    {
        "smg", "gun", "weapon", "rifle", "pistol", "firearm", "magazine", "barrel",
        "trigger", "stock", "sight", "scope", "muzzle", "clip", "cartridge", "ammo"
    };

    private static readonly string[] ArmsMeshKeywords =
    {
        "arm", "hand", "finger", "thumb", "palm", "wrist", "forearm", "bacon", "glove"
    };

    private static bool autoBuildAttempted;

    static FPSArmsViewModelAssetBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingAssets;
    }

    [MenuItem("Tools/BonkSurvivor/Build FPS Arms ViewModel Assets", false, 22)]
    public static void BuildFPSArmsViewModelAssets()
    {
        if (!TryResolveFpsArmsModelPath(out string modelPath))
        {
            Debug.LogWarning(
                "[FPSArmsViewModelAssetBuilder] No FPS arms model found under "
                + FpsArmsFolder
                + ". Drop an FBX or OBJ into "
                + ModelsFolder
                + " (see README).");
            return;
        }

        if (!EnsureFpsArmsSourceImported(modelPath, out string importIssue))
        {
            Debug.LogWarning(
                "[FPSArmsViewModelAssetBuilder] Model import is not ready. "
                + importIssue);
            return;
        }

        EnsureFolder("Assets/Prefabs", "Characters");

        if (!BuildFpsArmsViewModelPrefab(modelPath))
        {
            Debug.LogWarning("[FPSArmsViewModelAssetBuilder] Failed to create FPS_Arms_ViewModel prefab.");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab != null)
        {
            Debug.Log("[FPSArmsViewModelAssetBuilder] FPS_Arms_ViewModel prefab is ready at " + PrefabPath);
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogWarning("[FPSArmsViewModelAssetBuilder] Build finished but prefab was not found at " + PrefabPath);
        }
    }

    [MenuItem("Tools/BonkSurvivor/Build FPS Arms ViewModel Assets", true)]
    private static bool ValidateBuildFPSArmsViewModelAssets()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void TryBuildMissingAssets()
    {
        if (autoBuildAttempted || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        autoBuildAttempted = true;

        if (!TryResolveFpsArmsModelPath(out _))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        BuildFPSArmsViewModelAssets();
    }

    private static bool TryResolveFpsArmsModelPath(out string modelPath)
    {
        if (AssetPathExists(PreferredModelPath))
        {
            modelPath = PreferredModelPath;
            return true;
        }

        modelPath = FindFirstModelAsset(ModelsFolder);

        if (!string.IsNullOrEmpty(modelPath))
        {
            return true;
        }

        modelPath = FindFirstModelAsset(FpsArmsFolder);

        if (!string.IsNullOrEmpty(modelPath))
        {
            return true;
        }

        modelPath = string.Empty;
        return false;
    }

    private static string FindFirstModelAsset(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (IsSupportedModelPath(path))
                {
                    return path;
                }
            }
        }

        string fullFolder = Path.Combine(Application.dataPath, folder.Substring("Assets/".Length));

        if (!Directory.Exists(fullFolder))
        {
            return string.Empty;
        }

        foreach (string extension in ModelExtensions)
        {
            string[] files = Directory.GetFiles(fullFolder, "*" + extension, SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = "Assets" + files[i].Substring(Application.dataPath.Length).Replace('\\', '/');

                if (IsSupportedModelPath(assetPath))
                {
                    return assetPath;
                }
            }
        }

        return string.Empty;
    }

    private static bool IsSupportedModelPath(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        string extension = Path.GetExtension(assetPath).ToLowerInvariant();

        for (int i = 0; i < ModelExtensions.Length; i++)
        {
            if (extension == ModelExtensions[i])
            {
                return true;
            }
        }

        return false;
    }

    private static bool EnsureFpsArmsSourceImported(string modelPath, out string issue)
    {
        issue = string.Empty;

        if (string.IsNullOrEmpty(modelPath) || !AssetPathExists(modelPath))
        {
            issue = "Model asset path does not exist.";
            return false;
        }

        AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AssetImporter importer = AssetImporter.GetAtPath(modelPath);

        if (importer == null)
        {
            issue = "AssetImporter not found.";
            return false;
        }

        if (importer is not ModelImporter modelImporter)
        {
            issue = "Expected ModelImporter but found " + importer.GetType().Name + ".";
            return false;
        }

        modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
        modelImporter.addCollider = false;
        modelImporter.importAnimation = true;
        modelImporter.importCameras = false;
        modelImporter.importLights = false;
        modelImporter.SaveAndReimport();

        GameObject source = ResolveFpsArmsSourceGameObject(modelPath);

        if (source == null)
        {
            issue = "Model did not import as a loadable GameObject.";
            return false;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            issue = "Model imported without renderers.";
            return false;
        }

        return true;
    }

    private static GameObject ResolveFpsArmsSourceGameObject(string modelPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

        if (source != null)
        {
            return source;
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);

        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is GameObject gameObject && gameObject.transform.parent == null)
            {
                return gameObject;
            }
        }

        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is GameObject gameObject)
            {
                return gameObject;
            }
        }

        return null;
    }

    private static bool BuildFpsArmsViewModelPrefab(string modelPath)
    {
        GameObject source = ResolveFpsArmsSourceGameObject(modelPath);

        if (source == null)
        {
            Debug.LogWarning("[FPSArmsViewModelAssetBuilder] Could not load FPS arms model source asset.");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject prefabRoot = new GameObject("FPS_Arms_ViewModel");
        GameObject armsRoot = new GameObject("ArmsRoot");
        Transform armsRootTransform = armsRoot.transform;
        armsRootTransform.SetParent(prefabRoot.transform, false);
        armsRootTransform.localPosition = ArmsRootLocalPosition;
        armsRootTransform.localRotation = Quaternion.identity;
        armsRootTransform.localScale = Vector3.one;

        GameObject armsModel = PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (armsModel == null)
        {
            armsModel = Object.Instantiate(source);
        }

        if (armsModel == null)
        {
            Object.DestroyImmediate(prefabRoot);
            Debug.LogWarning("[FPSArmsViewModelAssetBuilder] Could not instantiate FPS arms model.");
            return false;
        }

        armsModel.name = "ArmsModel";
        Transform armsModelTransform = armsModel.transform;
        armsModelTransform.SetParent(armsRootTransform, false);
        armsModelTransform.localPosition = Vector3.zero;
        armsModelTransform.localRotation = Quaternion.identity;
        armsModelTransform.localScale = Vector3.one;

        DisablePhysicsComponents(armsModel);
        int disabledWeaponMeshes = DisableEmbeddedWeaponMeshes(armsModel);
        AssignFallbackMaterialsIfMissing(armsModel);
        ConfigureViewModelRenderers(armsModel);

        if (disabledWeaponMeshes > 0)
        {
            Debug.Log(
                "[FPSArmsViewModelAssetBuilder] Disabled "
                + disabledWeaponMeshes
                + " embedded weapon renderer(s) so BonkSurvivor weapons stay authoritative.");
        }

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        return savedPrefab != null;
    }

    private static void DisablePhysicsComponents(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];

            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].enabled = false;
            }
        }
    }

    private static int DisableEmbeddedWeaponMeshes(GameObject root)
    {
        int disabledCount = 0;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        List<Renderer> cubeCandidates = new List<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (ShouldDisableWeaponMesh(renderer.gameObject.name))
            {
                renderer.enabled = false;
                disabledCount++;
                continue;
            }

            if (UsesArmSurfaceMaterial(renderer))
            {
                continue;
            }

            if (renderer.gameObject.name.ToLowerInvariant().Contains("cube_"))
            {
                cubeCandidates.Add(renderer);
            }
        }

        if (cubeCandidates.Count == 0)
        {
            return disabledCount;
        }

        List<Renderer> cubesWithoutArmMaterial = new List<Renderer>();

        for (int i = 0; i < cubeCandidates.Count; i++)
        {
            if (!UsesArmSurfaceMaterial(cubeCandidates[i]))
            {
                cubesWithoutArmMaterial.Add(cubeCandidates[i]);
            }
        }

        if (cubesWithoutArmMaterial.Count == 0)
        {
            return disabledCount;
        }

        if (cubesWithoutArmMaterial.Count == 1)
        {
            cubesWithoutArmMaterial[0].enabled = false;
            return disabledCount + 1;
        }

        Renderer largestCube = cubesWithoutArmMaterial[0];
        float largestVolume = CalculateRendererVolume(largestCube);

        for (int i = 1; i < cubesWithoutArmMaterial.Count; i++)
        {
            float volume = CalculateRendererVolume(cubesWithoutArmMaterial[i]);

            if (volume > largestVolume)
            {
                largestVolume = volume;
                largestCube = cubesWithoutArmMaterial[i];
            }
        }

        largestCube.enabled = false;
        return disabledCount + 1;
    }

    private static float CalculateRendererVolume(Renderer renderer)
    {
        if (renderer == null)
        {
            return 0f;
        }

        Bounds bounds = renderer.bounds;
        Vector3 size = bounds.size;

        return size.x * size.y * size.z;
    }

    private static bool UsesArmSurfaceMaterial(Renderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];

            if (material == null)
            {
                continue;
            }

            string materialLabel = material.name.ToLowerInvariant();

            if (materialLabel.Contains("arm")
                || materialLabel.Contains("hand")
                || materialLabel.Contains("skin")
                || materialLabel.Contains("bacon")
                || materialLabel.Contains("glove"))
            {
                return true;
            }

            if (material.mainTexture != null)
            {
                string textureLabel = material.mainTexture.name.ToLowerInvariant();

                if (textureLabel.Contains("arm")
                    || textureLabel.Contains("hand")
                    || textureLabel.Contains("skin")
                    || textureLabel.Contains("bacon")
                    || textureLabel.Contains("glove"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ShouldDisableWeaponMesh(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string label = objectName.ToLowerInvariant();

        for (int i = 0; i < ArmsMeshKeywords.Length; i++)
        {
            if (label.Contains(ArmsMeshKeywords[i]))
            {
                return false;
            }
        }

        for (int i = 0; i < WeaponMeshKeywords.Length; i++)
        {
            if (label.Contains(WeaponMeshKeywords[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static void AssignFallbackMaterialsIfMissing(GameObject root)
    {
        Material fallbackMaterial = LoadFallbackMaterial();

        if (fallbackMaterial == null)
        {
            CreateRuntimeFallbackMaterial(out fallbackMaterial);

            if (fallbackMaterial == null)
            {
                Debug.LogWarning("[FPSArmsViewModelAssetBuilder] Could not resolve fallback material for missing slots.");
                return;
            }
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changed = false;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (!IsMissingMaterial(sharedMaterials[materialIndex]))
                {
                    continue;
                }

                sharedMaterials[materialIndex] = fallbackMaterial;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }
        }
    }

    private static bool IsMissingMaterial(Material material)
    {
        if (material == null)
        {
            return true;
        }

        Shader shader = material.shader;

        return shader == null
            || shader.name == "Hidden/InternalErrorShader"
            || shader.name.StartsWith("Hidden/");
    }

    private static Material LoadFallbackMaterial()
    {
        return LoadMaterial("Assets/Art/Weapons/Firestaff/Materials/FireStaff_Wood.mat")
            ?? LoadMaterial("Assets/Art/Weapons/Firestaff/Materials/M_Staff_Wood.mat");
    }

    private static void CreateRuntimeFallbackMaterial(out Material fallbackMaterial)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (urpLit == null)
        {
            fallbackMaterial = null;
            return;
        }

        fallbackMaterial = new Material(urpLit);
        fallbackMaterial.name = "FPSArmsFallbackSkin";
        fallbackMaterial.SetColor("_BaseColor", new Color(0.72f, 0.58f, 0.48f));
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    private static void ConfigureViewModelRenderers(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static bool AssetPathExists(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return false;
        }

        string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));

        return File.Exists(fullPath);
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = parentPath + "/" + folderName;

        if (AssetDatabase.IsValidFolder(fullPath))
        {
            return;
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
