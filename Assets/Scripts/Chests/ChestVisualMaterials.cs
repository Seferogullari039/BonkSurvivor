using UnityEngine;

public static class ChestVisualMaterials
{
    private const string WoodMaterialPath = "Assets/Materials/Chest/M_Chest_Wood.mat";
    private const string MetalMaterialPath = "Assets/Materials/Chest/M_Chest_Metal.mat";
    private const string GlowMaterialPath = "Assets/Materials/Chest/M_Chest_Glow.mat";

    private static Material woodBaseMaterial;
    private static Material metalBaseMaterial;
    private static Material glowBaseMaterial;

    private static readonly Color NormalWood = new Color(0.46f, 0.28f, 0.12f);
    private static readonly Color NormalMetal = new Color(0.34f, 0.34f, 0.36f);
    private static readonly Color NormalGlow = new Color(1f, 0.86f, 0.35f);
    private static readonly Color RareWood = new Color(0.22f, 0.24f, 0.28f);
    private static readonly Color RareMetal = new Color(0.12f, 0.58f, 0.62f);
    private static readonly Color RareGlow = new Color(0.28f, 0.78f, 1f);
    private static readonly Color EpicWood = new Color(0.28f, 0.12f, 0.34f);
    private static readonly Color EpicMetal = new Color(0.82f, 0.66f, 0.18f);
    private static readonly Color EpicGlow = new Color(0.72f, 0.28f, 1f);
    private static readonly Color Gold = new Color(0.92f, 0.74f, 0.18f);

    public static Color GetWoodColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RareWood,
            ChestRarity.Epic => EpicWood,
            _ => NormalWood
        };
    }

    public static Color GetMetalColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RareMetal,
            ChestRarity.Epic => EpicMetal,
            _ => NormalMetal
        };
    }

    public static Color GetGlowColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RareGlow,
            ChestRarity.Epic => EpicGlow,
            _ => NormalGlow
        };
    }

    public static Color GetLockColor(ChestRarity rarity)
    {
        return rarity == ChestRarity.Epic ? Gold : GetGlowColor(rarity);
    }

    public static void ApplyWood(Renderer renderer, ChestRarity rarity, float smoothness = 0.34f)
    {
        if (renderer == null) return;

        renderer.sharedMaterial = GetWoodBaseMaterial();
        GameVisualStyle.ApplyColor(renderer, GetWoodColor(rarity), smoothness, false, 0f);
    }

    public static void ApplyMetal(Renderer renderer, ChestRarity rarity, float smoothness = 0.62f)
    {
        if (renderer == null) return;

        renderer.sharedMaterial = GetMetalBaseMaterial();
        GameVisualStyle.ApplyColor(renderer, GetMetalColor(rarity), smoothness, rarity != ChestRarity.Normal, 0.18f);
    }

    public static void ApplyGlow(Renderer renderer, ChestRarity rarity, float intensity = 0.42f)
    {
        if (renderer == null) return;

        renderer.sharedMaterial = GetGlowBaseMaterial();
        GameVisualStyle.ApplyColor(renderer, GetGlowColor(rarity), 0.2f, true, intensity);
    }

    public static void ApplyLock(Renderer renderer, ChestRarity rarity)
    {
        if (renderer == null) return;

        renderer.sharedMaterial = GetMetalBaseMaterial();
        GameVisualStyle.ApplyColor(renderer, GetLockColor(rarity), 0.72f, true, 0.35f);
    }

    public static Material GetWoodBaseMaterial()
    {
        return GetOrLoadBaseMaterial(ref woodBaseMaterial, WoodMaterialPath, false);
    }

    public static Material GetMetalBaseMaterial()
    {
        return GetOrLoadBaseMaterial(ref metalBaseMaterial, MetalMaterialPath, false);
    }

    public static Material GetGlowBaseMaterial()
    {
        return GetOrLoadBaseMaterial(ref glowBaseMaterial, GlowMaterialPath, true);
    }

    private static Material GetOrLoadBaseMaterial(ref Material cache, string assetPath, bool emission)
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

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            return null;
        }

        cache = new Material(shader);

        if (emission)
        {
            cache.EnableKeyword("_EMISSION");
            cache.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        return cache;
    }
}
