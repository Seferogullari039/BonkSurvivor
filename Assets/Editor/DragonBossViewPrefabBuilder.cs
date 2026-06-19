using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class DragonBossViewPrefabBuilder
{
    private const string DragonModelPath =
        "Assets/Art/Characters/Enemies/Quaternius_LowPolyAnimatedMonsters/FBX/Dragon.fbx";
    private const string PrefabsFolder = "Assets/Prefabs/Bosses";
    private const string MaterialsFolder = PrefabsFolder + "/Materials";
    private const string PrefabPath = PrefabsFolder + "/DragonBoss_View.prefab";
    private const string BodyMaterialPath = MaterialsFolder + "/DragonBoss_Body_Mat.mat";
    private const float TargetHeight = 2.45f;
    private const float VisualGroundLocalY = -0.456f;
    private const float MouthFireHeightFactor = 0.72f;

    private static readonly Vector3[] ModelRotationCandidates =
    {
        new Vector3(0f, 180f, 0f),
        new Vector3(90f, 180f, 0f),
        new Vector3(0f, 90f, 0f),
        new Vector3(0f, -90f, 0f),
        new Vector3(-90f, 0f, 0f),
        new Vector3(-90f, 90f, 0f),
        new Vector3(-90f, -90f, 0f),
        new Vector3(90f, 90f, 0f),
        new Vector3(90f, -90f, 0f)
    };

    private static readonly Vector3[] PitchedRotationCandidates =
    {
        new Vector3(90f, 180f, 0f),
        new Vector3(-90f, 0f, 0f),
        new Vector3(-90f, 90f, 0f),
        new Vector3(-90f, -90f, 0f),
        new Vector3(90f, 90f, 0f),
        new Vector3(90f, -90f, 0f)
    };

    private static bool autoBuildAttempted;

    static DragonBossViewPrefabBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingPrefab;
    }

    [MenuItem("Tools/BonkSurvivor/Build Dragon Boss View Prefab", false, 26)]
    public static void BuildDragonBossViewPrefab()
    {
        if (!AssetPathExists(DragonModelPath))
        {
            Debug.LogWarning("[DragonBossViewPrefabBuilder] Dragon model missing at " + DragonModelPath);
            return;
        }

        if (!EnsureDragonImported(out string importIssue))
        {
            Debug.LogWarning("[DragonBossViewPrefabBuilder] Dragon import not ready: " + importIssue);
            return;
        }

        EnsureFolder("Assets/Prefabs", "Bosses");
        EnsureFolder(PrefabsFolder, "Materials");
        EnsureFallbackMaterials();

        if (!BuildPrefab(out string buildIssue))
        {
            Debug.LogWarning("[DragonBossViewPrefabBuilder] Prefab build failed: " + buildIssue);
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab != null)
        {
            Debug.Log("[DragonBossViewPrefabBuilder] DragonBoss_View prefab ready at " + PrefabPath);
            EditorGUIUtility.PingObject(prefab);
        }
    }

    [MenuItem("Tools/BonkSurvivor/Build Dragon Boss View Prefab", true)]
    private static bool ValidateBuildDragonBossViewPrefab()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    public static void BuildFromCommandLine()
    {
        BuildDragonBossViewPrefab();
        EditorApplication.Exit(0);
    }

    private static void TryBuildMissingPrefab()
    {
        if (autoBuildAttempted || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        autoBuildAttempted = true;

        if (!AssetPathExists(DragonModelPath))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        BuildDragonBossViewPrefab();
    }

    private static bool BuildPrefab(out string issue)
    {
        issue = string.Empty;
        GameObject source = ResolveModelSource(DragonModelPath);

        if (source == null)
        {
            issue = "Could not load Dragon FBX source.";
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

        GameObject prefabRoot = new GameObject("DragonBoss_View");
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
            issue = "Could not instantiate Dragon model.";
            return false;
        }

        modelObject.name = "Model";
        Transform modelTransform = modelObject.transform;
        modelTransform.SetParent(visualRoot.transform, false);
        modelTransform.localScale = Vector3.one;

        SanitizeVisualComponents(modelObject);
        AssignFallbackMaterials(modelTransform, bodyMaterial);
        FitModelToCapsuleSize(modelTransform, TargetHeight);
        PlaceMouthFirePoint(visualRoot.transform, modelTransform);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        if (savedPrefab == null)
        {
            issue = "SaveAsPrefabAsset returned null.";
            return false;
        }

        return true;
    }

    private static void PlaceMouthFirePoint(Transform visualRoot, Transform modelTransform)
    {
        Bounds bounds = CalculateRendererBounds(modelTransform);
        Vector3 size = bounds.size;
        Vector3 mouthWorld;

        if (size.x >= size.z)
        {
            float forwardX = bounds.max.x - bounds.center.x;
            float backwardX = bounds.center.x - bounds.min.x;
            float snoutX = forwardX >= backwardX
                ? bounds.max.x + bounds.extents.x * 0.04f
                : bounds.min.x - bounds.extents.x * 0.04f;

            mouthWorld = new Vector3(
                snoutX,
                bounds.min.y + bounds.size.y * MouthFireHeightFactor,
                bounds.center.z);
        }
        else
        {
            float forwardZ = bounds.max.z - bounds.center.z;
            float backwardZ = bounds.center.z - bounds.min.z;
            float snoutZ = forwardZ >= backwardZ
                ? bounds.max.z + bounds.extents.z * 0.04f
                : bounds.min.z - bounds.extents.z * 0.04f;

            mouthWorld = new Vector3(
                bounds.center.x,
                bounds.min.y + bounds.size.y * MouthFireHeightFactor,
                snoutZ);
        }

        Vector3 mouthLocal = visualRoot.InverseTransformPoint(mouthWorld);

        GameObject firePointObject = new GameObject("MouthFirePoint");
        firePointObject.transform.SetParent(visualRoot, false);
        firePointObject.transform.localPosition = mouthLocal;
        firePointObject.transform.localRotation = Quaternion.identity;
        firePointObject.transform.localScale = Vector3.one;
    }

    private static bool EnsureDragonImported(out string issue)
    {
        issue = string.Empty;

        AssetDatabase.ImportAsset(DragonModelPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AssetImporter importer = AssetImporter.GetAtPath(DragonModelPath);

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

        GameObject source = ResolveModelSource(DragonModelPath);

        if (source == null)
        {
            issue = "Dragon FBX did not import as GameObject.";
            return false;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            issue = "Dragon FBX imported without renderers.";
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
        CreateOrUpdateMaterial(BodyMaterialPath, new Color(0.58f, 0.1f, 0.24f), 0.72f, true);
    }

    private static void CreateOrUpdateMaterial(string assetPath, Color baseColor, float smoothness, bool emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveLitShader();

        if (shader == null)
        {
            Debug.LogError("[DragonBossViewPrefabBuilder] Lit shader not found for " + assetPath);
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
            material.SetColor("_EmissionColor", emission ? baseColor * 0.35f : Color.black);
        }

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
        }
        else
        {
            material.DisableKeyword("_EMISSION");
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
        Vector3 selectedEuler = SelectBestModelRotation(modelTransform);
        modelTransform.localPosition = Vector3.zero;
        modelTransform.localRotation = Quaternion.Euler(selectedEuler);
        modelTransform.localScale = Vector3.one;

        Bounds bounds = CalculateRendererBounds(modelTransform);
        float sourceHeight = Mathf.Max(0.001f, bounds.size.y);
        float uniformScale = targetHeight / sourceHeight;
        modelTransform.localScale = Vector3.one * uniformScale;

        bounds = CalculateRendererBounds(modelTransform);
        modelTransform.localPosition = new Vector3(0f, VisualGroundLocalY - bounds.min.y, 0f);

        Debug.Log("[DragonBossViewPrefabBuilder] Selected model rotation " + selectedEuler
            + " scale " + uniformScale.ToString("F4")
            + " position " + modelTransform.localPosition);
    }

    private static Vector3 SelectBestModelRotation(Transform modelTransform)
    {
        if (TrySelectBestRotation(modelTransform, PitchedRotationCandidates, out Vector3 pitchedEuler))
        {
            return pitchedEuler;
        }

        if (TrySelectBestRotation(modelTransform, ModelRotationCandidates, out Vector3 fallbackEuler))
        {
            return fallbackEuler;
        }

        return ModelRotationCandidates[0];
    }

    private static bool TrySelectBestRotation(
        Transform modelTransform,
        Vector3[] candidates,
        out Vector3 selectedEuler)
    {
        selectedEuler = candidates[0];
        float bestScore = float.NegativeInfinity;
        float bestAxisAlign = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 candidateEuler = candidates[i];
            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = Quaternion.Euler(candidateEuler);
            modelTransform.localScale = Vector3.one;

            Bounds bounds = CalculateRendererBounds(modelTransform);
            float score = ScoreOrientation(bounds, modelTransform, candidateEuler);
            float axisAlign = ResolveForwardAlignmentScore(modelTransform);

            Debug.Log("[DragonBossViewPrefabBuilder] Rotation candidate "
                + candidateEuler + " score " + score.ToString("F3")
                + " align " + axisAlign.ToString("F3")
                + " bounds " + bounds.size);

            if (score > bestScore || (Mathf.Approximately(score, bestScore) && axisAlign > bestAxisAlign))
            {
                bestScore = score;
                bestAxisAlign = axisAlign;
                selectedEuler = candidateEuler;
                found = true;
            }
        }

        return found;
    }

    private static float ScoreOrientation(Bounds bounds, Transform modelTransform, Vector3 euler)
    {
        Vector3 size = bounds.size;
        float height = Mathf.Max(0.001f, size.y);
        float minAxis = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        float maxAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        float maxHorizontal = Mathf.Max(size.x, size.z);
        float pitchAbs = Mathf.Abs(NormalizeAngle(euler.x));

        float profileScore = maxHorizontal / height;

        if (Mathf.Approximately(minAxis, size.y) && pitchAbs < 45f)
        {
            profileScore += 4f;
        }

        if (size.y >= maxAxis * 0.95f)
        {
            profileScore -= pitchAbs >= 45f ? 4f : 15f;
        }

        if (pitchAbs >= 45f)
        {
            profileScore += 10f;
        }

        if (size.z >= size.x && size.z >= size.y)
        {
            profileScore += 4f;
        }

        float forwardZ = bounds.max.z - bounds.center.z;
        float backwardZ = bounds.center.z - bounds.min.z;
        float forwardScore = forwardZ >= backwardZ
            ? forwardZ / height
            : -(backwardZ / height);

        float wingScore = size.x / height;
        float axisAlignScore = ResolveForwardAlignmentScore(modelTransform);
        float yawBonus = Mathf.Abs(NormalizeAngle(euler.y)) >= 45f ? 1f : 0f;

        return profileScore * 3f + forwardScore * 2f + wingScore + axisAlignScore * 5f + yawBonus;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }

        return angle;
    }

    private static float ResolveForwardAlignmentScore(Transform modelTransform)
    {
        float bestDot = float.NegativeInfinity;
        Vector3[] axes =
        {
            modelTransform.forward,
            modelTransform.up,
            modelTransform.right,
            -modelTransform.forward,
            -modelTransform.up,
            -modelTransform.right
        };

        for (int i = 0; i < axes.Length; i++)
        {
            Vector3 axis = axes[i];
            axis.y = 0f;

            if (axis.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            axis.Normalize();
            bestDot = Mathf.Max(bestDot, Vector3.Dot(axis, Vector3.forward));
        }

        return bestDot;
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
