using UnityEngine;

public enum ChestRarity
{
    Normal,
    Rare,
    Epic
}

public static class ChestRarityUtility
{
    private static GameObject defaultChestPrefab;
    private static GameObject normalChestPrefab;
    private static GameObject rareChestPrefab;
    private static GameObject epicChestPrefab;

    public static void RegisterChestPrefabs(
        GameObject normalPrefab,
        GameObject rarePrefab,
        GameObject epicPrefab,
        GameObject fallbackPrefab)
    {
        normalChestPrefab = normalPrefab;
        rareChestPrefab = rarePrefab;
        epicChestPrefab = epicPrefab;
        defaultChestPrefab = fallbackPrefab;
    }

    public static GameObject GetChestPrefab(ChestRarity rarity)
    {
        switch (rarity)
        {
            case ChestRarity.Rare:
                return rareChestPrefab != null ? rareChestPrefab : defaultChestPrefab;
            case ChestRarity.Epic:
                return epicChestPrefab != null ? epicChestPrefab : defaultChestPrefab;
            default:
                return normalChestPrefab != null ? normalChestPrefab : defaultChestPrefab;
        }
    }

    public static ChestRarity RollRandomChestRarity()
    {
        float roll = Random.value;

        if (roll < 0.05f)
        {
            return ChestRarity.Epic;
        }

        if (roll < 0.30f)
        {
            return ChestRarity.Rare;
        }

        return ChestRarity.Normal;
    }

    public static ChestRarity RollDragonBossChestRarity(int wave = 1)
    {
        float epicChance = 0.75f;

        if (wave >= 30)
        {
            epicChance = 0.9f;
        }
        else if (wave >= 20)
        {
            epicChance = 0.82f;
        }

        return Random.value < epicChance ? ChestRarity.Epic : ChestRarity.Rare;
    }

    public static ChestRarity RollBossChestRarity(int wave = 1)
    {
        float epicChance = 0.40f;

        if (wave >= 20)
        {
            epicChance += 0.20f;
        }
        else if (wave >= 10)
        {
            epicChance += 0.10f;
        }

        epicChance = Mathf.Clamp(epicChance, 0f, 0.95f);
        return Random.value < epicChance ? ChestRarity.Epic : ChestRarity.Rare;
    }

    public static Color GetChestColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => GameVisualPalette.ChestRare,
            ChestRarity.Epic => GameVisualPalette.ChestEpic,
            _ => GameVisualPalette.ChestNormal
        };
    }

    public static Color GetHeaderColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => new Color(0.36f, 0.72f, 1f),
            ChestRarity.Epic => new Color(0.78f, 0.49f, 1f),
            _ => new Color(0.85f, 0.75f, 0.65f)
        };
    }

    public static string GetHeaderText(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => "RARE CHEST",
            ChestRarity.Epic => "EPIC CHEST",
            _ => "NORMAL CHEST"
        };
    }

    public static int GetUpgradePickCount(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => 2,
            ChestRarity.Epic => 3,
            _ => 1
        };
    }
}
