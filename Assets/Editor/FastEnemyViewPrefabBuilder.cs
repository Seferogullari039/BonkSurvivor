using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class FastEnemyViewPrefabBuilder
{
    private const string BatModelPath =
        "Assets/Art/Characters/Enemies/Quaternius_LowPolyAnimatedMonsters/FBX/Bat.fbx";
    private const string PrefabsFolder = "Assets/Prefabs/Enemies";
    private const string MaterialsFolder = PrefabsFolder + "/Materials";
    private const string PrefabPath = PrefabsFolder + "/FastEnemy_View.prefab";
    private const string BodyMaterialPath = MaterialsFolder + "/FastEnemy_Body_Mat.mat";
    private const float TargetHeight = 0.88f;
    private const float VisualGroundLocalY = -0.456f;

    private static bool autoBuildAttempted;

    static FastEnemyViewPrefabBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingPrefab;
    }

    [MenuItem("Tools/BonkSurvivor/Build Fast Enemy View Prefab", false, 23)]
    public static void BuildFastEnemyViewPrefab()
    {
        if (!AssetPathExists(BatModelPath))
        {
            Debug.LogWarning("[FastEnemyViewPrefabBuilder] Bat model missing at " + BatModelPath);
            return;
        }

        if (!EnsureBatImported(out string importIssue))
        {
            Debug.LogWarning("[FastEnemyViewPrefabBuilder] Bat import not ready: " + importIssue);
            return;
        }

        EnsureFolder("Assets/Prefabs", "Enemies");
        EnsureFolder(PrefabsFolder, "Materials");
        EnsureFallbackMaterials();

        if (!BuildPrefab(out string buildIssue))
        {
            Debug.LogWarning("[FastEnemyViewPrefabBuilder] Prefab build failed: " + buildIssue);
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab != null)
        {
            Debug.Log("[FastEnemyViewPrefabBuilder] FastEnemy_View prefab ready at " + PrefabPath);
            EditorGUIUtility.PingObject(prefab);
        }
    }

    [MenuItem("Tools/BonkSurvivor/Build Fast Enemy View Prefab", true)]
    private static bool ValidateBuildFastEnemyViewPrefab()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    public static void BuildFromCommandLine()
    {
        BuildFastEnemyViewPrefab();
        EditorApplication.Exit(0);
    }

    private static void TryBuildMissingPrefab()
    {
        if (autoBuildAttempted || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        autoBuildAttempted = true;

        if (!AssetPathExists(BatModelPath))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        BuildFastEnemyViewPrefab();
    }

    private static bool BuildPrefab(out string issue)
    {
        issue = string.Empty;
        GameObject source = ResolveModelSource(BatModelPath);

        if (source == null)
        {
            issue = "Could not load Bat FBX source.";
            return false;
        }

        Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);

        if (bodyMaterial == null)
        {
            issue = "Fallback material missing.";
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject prefabRoot = new GameObject("FastEnemy_View");
        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(prefabRoot.transform, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        GameObject modelObject = PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (modelObject == null)
        {
            modelObject = Object.Instantiate(source);
        }

        if (modelObject == null)
        {
            Object.DestroyImmediate(prefabRoot);
            issue = "Could not instantiate Bat model.";
            return false;
        }

        modelObject.name = "Model";
        Transform modelTransform = modelObject.transform;
        modelTransform.SetParent(visualRoot.transform, false);
        modelTransform.localRotation = Quaternion.identity;
        modelTransform.localScale = Vector3.one;

        SanitizeVisualComponents(modelObject);
        AssignFallbackMaterials(modelTransform, bodyMaterial);
        FitModelToCapsuleSize(modelTransform, TargetHeight);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        if (savedPrefab == null)
        {
            issue = "SaveAsPrefabAsset returned null.";
            return false;
        }

        return true;
    }

    private static bool EnsureBatImported(out string issue)
    {
        issue = string.Empty;

        AssetDatabase.ImportAsset(BatModelPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AssetImporter importer = AssetImporter.GetAtPath(BatModelPath);

        if (importer is not ModelImporter modelImporter)
        {
            issue = "Expected ModelImporter.";
            return false;
        }

        modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        modelImporter.materialLocation = ModelImporterMaterialLocation.InPrefab;
        modelImporter.addCollider = false;
        modelImporter.animationType = ModelImporterAnimationType.Generic;
        modelImporter.SaveAndReimport();

        GameObject source = ResolveModelSource(BatModelPath);

        if (source == null)
        {
            issue = "Bat FBX did not import as GameObject.";
            return false;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            issue = "Bat FBX imported without renderers.";
            return false;
        }

        return true;
    }

    private static GameObject ResolveModelSource(string assetPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (source != null)
        {
            return source;
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

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

    private static void EnsureFallbackMaterials()
    {
        CreateOrUpdateMaterial(BodyMaterialPath, new Color(1f, 0.62f, 0.08f), 0.38f, false);
    }

    private static void CreateOrUpdateMaterial(string assetPath, Color baseColor, float smoothness, bool emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveLitShader();

        if (shader == null)
        {
            Debug.LogError("[FastEnemyViewPrefabBuilder] Lit shader not found for " + assetPath);
            return;
        }

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, assetPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.08f);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission ? baseColor : Color.black);
        }

        EditorUtility.SetDirty(material);
    }

    private static Shader ResolveLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Standard");
    }

    private static void SanitizeVisualComponents(GameObject root)
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
            Animator animator = animators[i];

            if (animator == null)
            {
                continue;
            }

            animator.applyRootMotion = false;
        }
    }

    private static void AssignFallbackMaterials(Transform root, Material bodyMaterial)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                sharedMaterials[materialIndex] = bodyMaterial;
            }

            renderer.sharedMaterials = sharedMaterials;
        }
    }

    private static void FitModelToCapsuleSize(Transform modelTransform, float targetHeight)
    {
        modelTransform.localPosition = Vector3.zero;
        modelTransform.localRotation = Quaternion.identity;

        Bounds bounds = CalculateRendererBounds(modelTransform);
        float sourceHeight = Mathf.Max(0.001f, bounds.size.y);
        float uniformScale = targetHeight / sourceHeight;
        modelTransform.localScale = Vector3.one * uniformScale;

        bounds = CalculateRendererBounds(modelTransform);
        modelTransform.localPosition = new Vector3(0f, VisualGroundLocalY - bounds.min.y, 0f);
    }

    private static Bounds CalculateRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
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

        if (!hasBounds)
        {
            bounds = new Bounds(root.position, Vector3.one * 0.1f);
        }

        return bounds;
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string folderPath = parentPath + "/" + folderName;

        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        AssetDatabase.CreateFolder(parentPath, folderName);
    }

    private static bool AssetPathExists(string assetPath)
    {
        return !string.IsNullOrEmpty(assetPath) && File.Exists(assetPath);
    }
}
