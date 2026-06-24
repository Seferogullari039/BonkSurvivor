using UnityEngine;

public static class ChestEconomyModifiers
{
    private const float KeyChancePerLevel = 0.015f;
    private const float MaxFreeChestChance = 0.45f;
    private const float LuckLegendaryBonusPerLevel = 0.0025f;
    private const float MaxLuckLegendaryBonus = 0.02f;
    private const float LuckTierShiftPerLevel = 0.008f;
    private const float MaxLuckTierShift = 0.08f;

    public static int GetKeyLevel()
    {
        return GetTrackedLevel(UpgradeOptionCatalog.KeyIndex);
    }

    public static float GetFreeChestChance()
    {
        int keyLevel = GetKeyLevel();

        if (keyLevel <= 0)
        {
            return 0f;
        }

        return Mathf.Min(keyLevel * KeyChancePerLevel, MaxFreeChestChance);
    }

    public static bool ShouldOpenChestForFree()
    {
        float chance = GetFreeChestChance();

        if (chance <= 0f)
        {
            return false;
        }

        return Random.value < chance;
    }

    public static int GetLuckLevel()
    {
        return GetTrackedLevel(UpgradeOptionCatalog.LuckIndex);
    }

    public static float GetLuckRarityBonus()
    {
        int luckLevel = GetLuckLevel();

        if (luckLevel <= 0)
        {
            return 0f;
        }

        return Mathf.Min(luckLevel * LuckLegendaryBonusPerLevel, MaxLuckLegendaryBonus);
    }

    public static UpgradeRarity ApplyLuckToChestStatRarity(UpgradeRarity baseRarity)
    {
        int luckLevel = GetLuckLevel();

        if (luckLevel <= 0)
        {
            return baseRarity;
        }

        float tierShift = Mathf.Min(luckLevel * LuckTierShiftPerLevel, MaxLuckTierShift);
        float legendaryBonus = GetLuckRarityBonus();
        float roll = Random.value;

        switch (baseRarity)
        {
            case UpgradeRarity.Common:
                return roll < tierShift ? UpgradeRarity.Rare : baseRarity;
            case UpgradeRarity.Rare:
                return roll < tierShift ? UpgradeRarity.Epic : baseRarity;
            case UpgradeRarity.Epic:
                return roll < legendaryBonus ? UpgradeRarity.Legendary : baseRarity;
            default:
                return baseRarity;
        }
    }

    private static int GetTrackedLevel(int upgradeIndex)
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return 0;
        }

        return tracker.GetTrackedLevel(upgradeIndex);
    }
}
