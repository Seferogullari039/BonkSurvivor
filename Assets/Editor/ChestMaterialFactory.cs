using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ChestMaterialFactory
{
    public const string MaterialsFolder = "Assets/Prefabs/Chests/Materials";

    public const string CommonMaterialPath = MaterialsFolder + "/Chest_Common_Mat.mat";
    public const string RareMaterialPath = MaterialsFolder + "/Chest_Rare_Mat.mat";
    public const string EpicMaterialPath = MaterialsFolder + "/Chest_Epic_Mat.mat";
    public const string LegendaryMaterialPath = MaterialsFolder + "/Chest_Legendary_Mat.mat";
    public const string DarkTrimMaterialPath = MaterialsFolder + "/Chest_DarkTrim_Mat.mat";
    public const string GlowMaterialPath = MaterialsFolder + "/Chest_Glow_Mat.mat";

    public static void EnsureChestMaterials()
    {
        EnsureFolder("Assets/Prefabs", "Chests");
        EnsureFolder("Assets/Prefabs/Chests", "Materials");

        CreateOrUpdateMaterial(CommonMaterialPath, new Color(0.50f, 0.48f, 0.46f), 0.38f, false);
        CreateOrUpdateMaterial(RareMaterialPath, new Color(0.30f, 0.45f, 0.78f), 0.48f, false);
        CreateOrUpdateMaterial(EpicMaterialPath, new Color(0.58f, 0.30f, 0.72f), 0.52f, false);
        CreateOrUpdateMaterial(LegendaryMaterialPath, new Color(0.90f, 0.74f, 0.22f), 0.62f, false);
        CreateOrUpdateMaterial(DarkTrimMaterialPath, new Color(0.18f, 0.18f, 0.20f), 0.55f, false);
        CreateOrUpdateMaterial(GlowMaterialPath, Color.white, 0.18f, true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrUpdateMaterial(string assetPath, Color baseColor, float smoothness, bool emission)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        Shader shader = ResolveLitShader();

        if (shader == null)
        {
            Debug.LogError("[ChestMaterialFactory] No Lit shader found for " + assetPath);
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
            material.SetFloat("_Metallic", emission ? 0f : 0.08f);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emission ? baseColor : Color.black);
        }

        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        EditorUtility.SetDirty(material);
    }

    private static Shader ResolveLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return shader;
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
