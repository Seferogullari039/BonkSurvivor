using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class BasicEnemyViewPrefabBuilder
{
    private const string SlimeModelPath =
        "Assets/Art/Characters/Enemies/Quaternius_LowPolyAnimatedMonsters/FBX/Slime.fbx";
    private const string PrefabsFolder = "Assets/Prefabs/Enemies";
    private const string MaterialsFolder = PrefabsFolder + "/Materials";
    private const string PrefabPath = PrefabsFolder + "/BasicEnemy_View.prefab";
    private const string BodyMaterialPath = MaterialsFolder + "/BasicEnemy_Body_Mat.mat";
    private const string EyesMaterialPath = MaterialsFolder + "/BasicEnemy_Eyes_Mat.mat";
    private const float TargetHeight = 1.05f;
    private const float VisualGroundLocalY = -0.32f;

    private static bool autoBuildAttempted;

    static BasicEnemyViewPrefabBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingPrefab;
    }

    [MenuItem("Tools/BonkSurvivor/Build Basic Enemy View Prefab", false, 22)]
    public static void BuildBasicEnemyViewPrefab()
    {
        if (!AssetPathExists(SlimeModelPath))
        {
            Debug.LogWarning("[BasicEnemyViewPrefabBuilder] Slime model missing at " + SlimeModelPath);
            return;
        }

        if (!EnsureSlimeImported(out string importIssue))
        {
            Debug.LogWarning("[BasicEnemyViewPrefabBuilder] Slime import not ready: " + importIssue);
            return;
        }

        EnsureFolder("Assets/Prefabs", "Enemies");
        EnsureFolder(PrefabsFolder, "Materials");
        EnsureFallbackMaterials();

        if (!BuildPrefab(out string buildIssue))
        {
            Debug.LogWarning("[BasicEnemyViewPrefabBuilder] Prefab build failed: " + buildIssue);
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab != null)
        {
            Debug.Log("[BasicEnemyViewPrefabBuilder] BasicEnemy_View prefab ready at " + PrefabPath);
            EditorGUIUtility.PingObject(prefab);
        }
    }

    [MenuItem("Tools/BonkSurvivor/Build Basic Enemy View Prefab", true)]
    private static bool ValidateBuildBasicEnemyViewPrefab()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void TryBuildMissingPrefab()
    {
        if (autoBuildAttempted || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        autoBuildAttempted = true;

        if (!AssetPathExists(SlimeModelPath))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        BuildBasicEnemyViewPrefab();
    }

    private static bool BuildPrefab(out string issue)
    {
        issue = string.Empty;
        GameObject source = ResolveModelSource(SlimeModelPath);

        if (source == null)
        {
            issue = "Could not load Slime FBX source.";
            return false;
        }

        Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
        Material eyesMaterial = AssetDatabase.LoadAssetAtPath<Material>(EyesMaterialPath);

        if (bodyMaterial == null || eyesMaterial == null)
        {
            issue = "Fallback materials missing.";
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject prefabRoot = new GameObject("BasicEnemy_View");
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
            issue = "Could not instantiate Slime model.";
            return false;
        }

        modelObject.name = "Model";
        Transform modelTransform = modelObject.transform;
        modelTransform.SetParent(visualRoot.transform, false);
        modelTransform.localRotation = Quaternion.identity;
        modelTransform.localScale = Vector3.one;

        SanitizeVisualComponents(modelObject);
        AssignFallbackMaterials(modelTransform, bodyMaterial, eyesMaterial);
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

    private static bool EnsureSlimeImported(out string issue)
    {
        issue = string.Empty;

        AssetDatabase.ImportAsset(SlimeModelPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AssetImporter importer = AssetImporter.GetAtPath(SlimeModelPath);

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

        GameObject source = ResolveModelSource(SlimeModelPath);

        if (source == null)
        {
            issue = "Slime FBX did not import as GameObject.";
            return false;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            issue = "Slime FBX imported without renderers.";
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
        CreateOrUpdateMaterial(BodyMaterialPath, new Color(0.22f, 0.64f, 0.12f), 0.42f, false);
        CreateOrUpdateMaterial(EyesMaterialPath, new Color(0.08f, 0.08f, 0.08f), 0.55f, false);
    }

    private static void CreateOrUpdateMaterial(string assetPath, Color baseColor, float smoothness, bool emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveLitShader();

        if (shader == null)
        {
            Debug.LogError("[BasicEnemyViewPrefabBuilder] Lit shader not found for " + assetPath);
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

    private static void AssignFallbackMaterials(Transform root, Material bodyMaterial, Material eyesMaterial)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            string label = renderer.gameObject.name.ToLowerInvariant();
            Material resolvedMaterial = label.Contains("eye") ? eyesMaterial : bodyMaterial;
            Material[] sharedMaterials = renderer.sharedMaterials;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                sharedMaterials[materialIndex] = resolvedMaterial;
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
