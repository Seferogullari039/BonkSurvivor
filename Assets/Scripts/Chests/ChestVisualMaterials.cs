using UnityEngine;

public static class ChestVisualMaterials
{
    private const string MaterialsFolder = "Assets/Prefabs/Chests/Materials";
    private const string CommonMaterialPath = MaterialsFolder + "/Chest_Common_Mat.mat";
    private const string RareMaterialPath = MaterialsFolder + "/Chest_Rare_Mat.mat";
    private const string EpicMaterialPath = MaterialsFolder + "/Chest_Epic_Mat.mat";
    private const string LegendaryMaterialPath = MaterialsFolder + "/Chest_Legendary_Mat.mat";
    private const string DarkTrimMaterialPath = MaterialsFolder + "/Chest_DarkTrim_Mat.mat";
    private const string GlowMaterialPath = MaterialsFolder + "/Chest_Glow_Mat.mat";

    private static Material commonBodyMaterial;
    private static Material rareBodyMaterial;
    private static Material epicBodyMaterial;
    private static Material legendaryBodyMaterial;
    private static Material darkTrimMaterial;
    private static Material glowBaseMaterial;

    private static readonly Color CommonGlow = new Color(0.82f, 0.84f, 0.88f);
    private static readonly Color RareGlow = new Color(0.35f, 0.65f, 1f);
    private static readonly Color EpicGlow = new Color(0.75f, 0.35f, 1f);
    private static readonly Color LegendaryGlow = new Color(1f, 0.82f, 0.28f);

    public static Color GetGlowColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RareGlow,
            ChestRarity.Epic => EpicGlow,
            _ => CommonGlow
        };
    }

    public static Color GetLegendaryGlowColor()
    {
        return LegendaryGlow;
    }

    public static Color GetWoodColor(ChestRarity rarity)
    {
        return GetBodyTint(rarity);
    }

    public static Color GetMetalColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => new Color(0.14f, 0.28f, 0.42f),
            ChestRarity.Epic => new Color(0.32f, 0.18f, 0.38f),
            _ => new Color(0.18f, 0.18f, 0.20f)
        };
    }

    public static Color GetLockColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RareGlow,
            ChestRarity.Epic => EpicGlow,
            _ => new Color(0.72f, 0.70f, 0.66f)
        };
    }

    public static void ApplyBody(Renderer renderer, ChestRarity rarity, float smoothness = 0.38f)
    {
        if (renderer == null)
        {
            return;
        }

        Material bodyMaterial = GetBodyMaterial(rarity);

        if (bodyMaterial != null)
        {
            renderer.sharedMaterial = bodyMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, GetBodyTint(rarity), smoothness, false, 0f);
    }

    public static void ApplyWood(Renderer renderer, ChestRarity rarity, float smoothness = 0.38f)
    {
        ApplyBody(renderer, rarity, smoothness);
    }

    public static void ApplyTrim(Renderer renderer, ChestRarity rarity, float smoothness = 0.55f)
    {
        if (renderer == null)
        {
            return;
        }

        Material trimMaterial = GetTrimMaterial();

        if (trimMaterial != null)
        {
            renderer.sharedMaterial = trimMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, GetMetalColor(rarity), smoothness, rarity != ChestRarity.Normal, 0.12f);
    }

    public static void ApplyMetal(Renderer renderer, ChestRarity rarity, float smoothness = 0.55f)
    {
        ApplyTrim(renderer, rarity, smoothness);
    }

    public static void ApplyGlow(Renderer renderer, ChestRarity rarity, float intensity = 0.42f)
    {
        if (renderer == null)
        {
            return;
        }

        Material glowMaterial = GetGlowBaseMaterial();

        if (glowMaterial != null)
        {
            renderer.sharedMaterial = glowMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, GetGlowColor(rarity), 0.16f, true, Mathf.Min(intensity, 0.35f));
    }

    public static void ApplyLock(Renderer renderer, ChestRarity rarity)
    {
        if (renderer == null)
        {
            return;
        }

        Material trimMaterial = GetTrimMaterial();

        if (trimMaterial != null)
        {
            renderer.sharedMaterial = trimMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, GetLockColor(rarity), 0.68f, rarity != ChestRarity.Normal, 0.28f);
    }

    public static void ApplyLegendaryBody(Renderer renderer, float smoothness = 0.62f)
    {
        if (renderer == null)
        {
            return;
        }

        Material legendaryMaterial = GetLegendaryBodyMaterial();

        if (legendaryMaterial != null)
        {
            renderer.sharedMaterial = legendaryMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, new Color(0.90f, 0.74f, 0.22f), smoothness, true, 0.18f);
    }

    public static Material GetBodyMaterial(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => GetRareBodyMaterial(),
            ChestRarity.Epic => GetEpicBodyMaterial(),
            _ => GetCommonBodyMaterial()
        };
    }

    public static Material GetWoodBaseMaterial()
    {
        return GetCommonBodyMaterial();
    }

    public static Material GetMetalBaseMaterial()
    {
        return GetTrimMaterial();
    }

    public static Material GetGlowBaseMaterial()
    {
        return LoadMaterialAsset(ref glowBaseMaterial, GlowMaterialPath, true);
    }

    public static Material GetCommonBodyMaterial()
    {
        return LoadMaterialAsset(ref commonBodyMaterial, CommonMaterialPath, false);
    }

    public static Material GetRareBodyMaterial()
    {
        return LoadMaterialAsset(ref rareBodyMaterial, RareMaterialPath, false);
    }

    public static Material GetEpicBodyMaterial()
    {
        return LoadMaterialAsset(ref epicBodyMaterial, EpicMaterialPath, false);
    }

    public static Material GetLegendaryBodyMaterial()
    {
        return LoadMaterialAsset(ref legendaryBodyMaterial, LegendaryMaterialPath, false);
    }

    public static Material GetTrimMaterial()
    {
        return LoadMaterialAsset(ref darkTrimMaterial, DarkTrimMaterialPath, false);
    }

    private static Color GetBodyTint(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => new Color(0.30f, 0.45f, 0.78f),
            ChestRarity.Epic => new Color(0.58f, 0.30f, 0.72f),
            _ => new Color(0.50f, 0.48f, 0.46f)
        };
    }

    private static Material LoadMaterialAsset(ref Material cache, string assetPath, bool emission)
    {
        if (cache != null)
        {
            return cache;
        }

#if UNITY_EDITOR
        cache = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        if (cache != null)
        {
            return cache;
        }
#endif

        cache = CreateRuntimeFallbackMaterial(emission);
        return cache;
    }

    private static Material CreateRuntimeFallbackMaterial(bool emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);

        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return material;
    }
}
