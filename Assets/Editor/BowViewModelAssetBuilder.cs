using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class BowViewModelAssetBuilder
{
    private const string BowObjPath = "Assets/Art/Weapons/Bow/CompoundBow_wert/CompoundBow.obj";
    private const string PrefabsFolder = "Assets/Prefabs/Weapons";
    private const string PrefabPath = PrefabsFolder + "/Bow_ViewModel.prefab";
    private const float ViewModelTargetWidth = 0.34f;

    private static readonly Vector3 BowRootLocalPosition = new Vector3(0.32f, -0.18f, 0.52f);
    private static readonly Vector3 BowRootLocalRotation = new Vector3(6f, -22f, 4f);
    private static readonly Vector3 BowModelLocalRotation = new Vector3(-12f, 90f, 8f);

    private static bool autoBuildAttempted;

    static BowViewModelAssetBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingAssets;
    }

    [MenuItem("Tools/BonkSurvivor/Build Bow ViewModel Assets", false, 21)]
    public static void BuildBowViewModelAssets()
    {
        if (!TryResolveBowObjPath(out string bowObjPath))
        {
            Debug.LogWarning(
                "[BowViewModelAssetBuilder] Missing OBJ at "
                + BowObjPath
                + ". Add the Compound Bow OBJ export to that path.");
            return;
        }

        if (!EnsureBowSourceImported(bowObjPath, out string importIssue))
        {
            Debug.LogWarning(
                "[BowViewModelAssetBuilder] OBJ import is not ready. "
                + importIssue);
            return;
        }

        EnsureFolder("Assets/Prefabs", "Weapons");

        if (!LoadFallbackMaterials(out Material bodyMaterial, out Material trimMaterial))
        {
            Debug.LogWarning("[BowViewModelAssetBuilder] Could not resolve fallback materials. Using basic defaults.");
            CreateRuntimeFallbackMaterials(out bodyMaterial, out trimMaterial);

            if (bodyMaterial == null || trimMaterial == null)
            {
                Debug.LogWarning("[BowViewModelAssetBuilder] Shader fallback unavailable. Aborting prefab build.");
                return;
            }
        }

        if (!BuildBowViewModelPrefab(bowObjPath, bodyMaterial, trimMaterial))
        {
            Debug.LogWarning("[BowViewModelAssetBuilder] Failed to create Bow_ViewModel prefab.");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

        if (prefab != null)
        {
            Debug.Log("[BowViewModelAssetBuilder] Bow_ViewModel prefab is ready at " + PrefabPath);
            EditorGUIUtility.PingObject(prefab);
        }
        else
        {
            Debug.LogWarning("[BowViewModelAssetBuilder] Build finished but prefab was not found at " + PrefabPath);
        }
    }

    [MenuItem("Tools/BonkSurvivor/Build Bow ViewModel Assets", true)]
    private static bool ValidateBuildBowViewModelAssets()
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

        if (!TryResolveBowObjPath(out _))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        BuildBowViewModelAssets();
    }

    private static bool TryResolveBowObjPath(out string bowObjPath)
    {
        bowObjPath = BowObjPath;

        if (AssetPathExists(BowObjPath))
        {
            return true;
        }

        bowObjPath = string.Empty;
        return false;
    }

    private static bool EnsureBowSourceImported(string bowObjPath, out string issue)
    {
        issue = string.Empty;

        if (string.IsNullOrEmpty(bowObjPath) || !AssetPathExists(bowObjPath))
        {
            issue = "OBJ asset path does not exist.";
            return false;
        }

        AssetDatabase.ImportAsset(bowObjPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AssetImporter importer = AssetImporter.GetAtPath(bowObjPath);

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
        modelImporter.importAnimation = false;
        modelImporter.SaveAndReimport();

        GameObject source = ResolveBowSourceGameObject(bowObjPath);

        if (source == null)
        {
            issue = "OBJ did not import as a loadable GameObject.";
            return false;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            issue = "OBJ imported without renderers.";
            return false;
        }

        return true;
    }

    private static GameObject ResolveBowSourceGameObject(string bowObjPath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(bowObjPath);

        if (source != null)
        {
            return source;
        }

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(bowObjPath);

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

    private static bool BuildBowViewModelPrefab(string bowObjPath, Material bodyMaterial, Material trimMaterial)
    {
        GameObject source = ResolveBowSourceGameObject(bowObjPath);

        if (source == null)
        {
            Debug.LogWarning("[BowViewModelAssetBuilder] Could not load bow OBJ source asset.");
            return false;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject prefabRoot = new GameObject("Bow_ViewModel");
        GameObject bowRoot = new GameObject("BowRoot");
        Transform bowRootTransform = bowRoot.transform;
        bowRootTransform.SetParent(prefabRoot.transform, false);
        bowRootTransform.localPosition = BowRootLocalPosition;
        bowRootTransform.localRotation = Quaternion.Euler(BowRootLocalRotation);
        bowRootTransform.localScale = Vector3.one;

        GameObject bowModel = PrefabUtility.InstantiatePrefab(source) as GameObject;

        if (bowModel == null)
        {
            bowModel = Object.Instantiate(source);
        }

        if (bowModel == null)
        {
            Object.DestroyImmediate(prefabRoot);
            Debug.LogWarning("[BowViewModelAssetBuilder] Could not instantiate bow model from OBJ.");
            return false;
        }

        bowModel.name = "BowModel";
        Transform bowModelTransform = bowModel.transform;
        bowModelTransform.SetParent(bowRootTransform, false);
        bowModelTransform.localPosition = Vector3.zero;
        bowModelTransform.localRotation = Quaternion.Euler(BowModelLocalRotation);
        bowModelTransform.localScale = Vector3.one;

        DisablePhysicsComponents(bowModel);
        AssignFallbackMaterials(bowModelTransform, bodyMaterial, trimMaterial);
        FitBowModelTransform(bowRootTransform, bowModelTransform, ViewModelTargetWidth);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);

        return savedPrefab != null;
    }

    private static void FitBowModelTransform(Transform bowRoot, Transform bowModel, float targetWidth)
    {
        Bounds bounds = CalculateRendererBounds(bowModel);
        float sourceWidth = Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z));
        float uniformScale = targetWidth / sourceWidth;
        bowModel.localScale = Vector3.one * uniformScale;

        bounds = CalculateRendererBounds(bowModel);
        Vector3 worldOffset = bowRoot.position - bounds.center;
        bowModel.position += worldOffset;
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

    private static void AssignFallbackMaterials(Transform root, Material bodyMaterial, Material trimMaterial)
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
            bool changed = false;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (sharedMaterials[materialIndex] != null)
                {
                    continue;
                }

                string label = renderer.gameObject.name.ToLowerInvariant();
                sharedMaterials[materialIndex] = label.Contains("string")
                    || label.Contains("wire")
                    || label.Contains("metal")
                    || label.Contains("steel")
                    || label.Contains("sight")
                    ? trimMaterial
                    : bodyMaterial;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private static bool LoadFallbackMaterials(out Material bodyMaterial, out Material trimMaterial)
    {
        bodyMaterial = LoadMaterial("Assets/Art/Weapons/Firestaff/Materials/FireStaff_Wood.mat")
            ?? LoadMaterial("Assets/Art/Weapons/Firestaff/Materials/M_Staff_Wood.mat");
        trimMaterial = LoadMaterial("Assets/Art/Weapons/Firestaff/Materials/FireStaff_Metal.mat")
            ?? LoadMaterial("Assets/Art/Weapons/Firestaff/Materials/M_Staff_Metal.mat");

        return bodyMaterial != null && trimMaterial != null;
    }

    private static void CreateRuntimeFallbackMaterials(out Material bodyMaterial, out Material trimMaterial)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (urpLit == null)
        {
            bodyMaterial = null;
            trimMaterial = null;
            return;
        }

        bodyMaterial = new Material(urpLit);
        bodyMaterial.name = "BowFallbackBody";
        bodyMaterial.SetColor("_BaseColor", new Color(0.42f, 0.26f, 0.13f));

        trimMaterial = new Material(urpLit);
        trimMaterial.name = "BowFallbackTrim";
        trimMaterial.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.66f));
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
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
