using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class BowViewModelAssetBuilder
{
    private const string BowGlbPath = "Assets/Art/Weapons/Bow/CompoundBow_wert/compound_bow.glb";
    private const string PrefabsFolder = "Assets/Prefabs/Weapons";
    private const string PrefabPath = PrefabsFolder + "/Bow_ViewModel.prefab";
    private const float ViewModelTargetWidth = 0.34f;

    private static readonly Vector3 BowModelLocalPosition = new Vector3(0f, 0f, 0f);
    private static readonly Vector3 BowModelLocalRotation = new Vector3(-12f, 90f, 8f);
    private static readonly Vector3 BowModelLocalScale = Vector3.one;

    static BowViewModelAssetBuilder()
    {
        EditorApplication.delayCall += TryBuildMissingAssets;
    }

    [MenuItem("Tools/BonkSurvivor/Build Bow ViewModel Assets")]
    public static void BuildBowViewModelAssets()
    {
        if (!File.Exists(BowGlbPath))
        {
            Debug.LogError("[BowViewModelAssetBuilder] Missing GLB at " + BowGlbPath);
            return;
        }

        if (!IsGlbImportValid(out string importIssue))
        {
            Debug.LogError(
                "[BowViewModelAssetBuilder] GLB import is not valid. "
                + importIssue
                + " OBJ format may be required.");
            return;
        }

        EnsureFolder("Assets/Prefabs", "Weapons");

        if (!LoadFallbackMaterials(out Material bodyMaterial, out Material trimMaterial))
        {
            Debug.LogError("[BowViewModelAssetBuilder] Could not resolve fallback materials.");
            return;
        }

        BuildBowViewModelPrefab(bodyMaterial, trimMaterial);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log("[BowViewModelAssetBuilder] Bow_ViewModel prefab is ready at " + PrefabPath);
        }
    }

    private static void TryBuildMissingAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (!File.Exists(BowGlbPath))
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            return;
        }

        if (!IsGlbImportValid(out _))
        {
            return;
        }

        BuildBowViewModelAssets();
    }

    private static bool IsGlbImportValid(out string issue)
    {
        issue = string.Empty;

        AssetImporter importer = AssetImporter.GetAtPath(BowGlbPath);

        if (importer == null)
        {
            issue = "AssetImporter not found.";
            return false;
        }

        if (importer is not ModelImporter)
        {
            issue = "Importer is not ModelImporter.";
            return false;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(BowGlbPath);

        if (source == null)
        {
            issue = "GLB did not import as a GameObject.";
            return false;
        }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            issue = "GLB imported without renderers.";
            return false;
        }

        return true;
    }

    private static void BuildBowViewModelPrefab(Material bodyMaterial, Material trimMaterial)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(BowGlbPath);

        if (source == null)
        {
            Debug.LogError("[BowViewModelAssetBuilder] Could not load bow GLB asset.");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject prefabRoot = new GameObject("Bow_ViewModel");
        GameObject bowRoot = new GameObject("BowRoot");
        bowRoot.transform.SetParent(prefabRoot.transform, false);

        GameObject bowModel = Object.Instantiate(source);
        bowModel.name = "BowModel";
        bowModel.transform.SetParent(bowRoot.transform, false);
        bowModel.transform.localPosition = BowModelLocalPosition;
        bowModel.transform.localRotation = Quaternion.Euler(BowModelLocalRotation);
        bowModel.transform.localScale = BowModelLocalScale;

        DisablePhysicsComponents(bowModel);
        AssignFallbackMaterials(bowModel.transform, bodyMaterial, trimMaterial);
        FitBowModelTransform(bowRoot.transform, bowModel.transform, ViewModelTargetWidth);

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        Object.DestroyImmediate(prefabRoot);
    }

    private static void FitBowModelTransform(Transform bowRoot, Transform bowModel, float targetWidth)
    {
        Bounds bounds = CalculateRendererBounds(bowModel);
        float sourceWidth = Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z));
        float uniformScale = targetWidth / sourceWidth;
        Vector3 combinedScale = bowModel.localScale * uniformScale;
        bowModel.localScale = combinedScale;

        bounds = CalculateRendererBounds(bowModel);
        bowModel.localPosition += bowRoot.position - bounds.center;
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

        if (bodyMaterial != null && trimMaterial != null)
        {
            return true;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (urpLit == null)
        {
            return false;
        }

        if (bodyMaterial == null)
        {
            bodyMaterial = new Material(urpLit);
            bodyMaterial.name = "BowFallbackBody";
            bodyMaterial.SetColor("_BaseColor", new Color(0.42f, 0.26f, 0.13f));
        }

        if (trimMaterial == null)
        {
            trimMaterial = new Material(urpLit);
            trimMaterial.name = "BowFallbackTrim";
            trimMaterial.SetColor("_BaseColor", new Color(0.62f, 0.62f, 0.66f));
        }

        return bodyMaterial != null && trimMaterial != null;
    }

    private static Material LoadMaterial(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(path);
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
