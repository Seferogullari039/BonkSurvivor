using UnityEngine;

public static class RewardCardTextFormatter
{
    private const int BonusRewardCoin = -1;
    private const int BonusRewardHeal = -2;

    private static readonly Color LevelLineColor = new Color(0.72f, 0.76f, 0.84f, 1f);
    private static readonly Color BonusHeaderColor = new Color(0.94f, 0.82f, 0.42f, 1f);

    public static Color GetLevelLineColor()
    {
        return LevelLineColor;
    }

    public static Color GetBonusHeaderColor()
    {
        return BonusHeaderColor;
    }

    public static string BuildHeader(int upgradeIndex, UpgradeRarity rarity)
    {
        string rarityLabel = UpgradeOptionCatalog.GetRarityLabel(rarity);

        if (IsEvolutionUpgradeCard(upgradeIndex))
        {
            return JoinHeaderParts(rarityLabel, "EVOLUTION");
        }

        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        string categoryLabel = UpgradeOptionCatalog.GetCategoryLabel(category);
        string buildLabel = GetCompactBuildLabel(UpgradeOptionCatalog.GetBuildType(upgradeIndex));
        return JoinHeaderParts(rarityLabel, categoryLabel, buildLabel);
    }

    public static string BuildLevelLine(int upgradeIndex)
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        int current = tracker.GetUpgradeLevel(upgradeIndex);
        int max = UpgradeOptionCatalog.GetMaxLevel(upgradeIndex);
        int next = Mathf.Min(current + 1, max);
        return "Lv. " + next + "/" + max;
    }

    public static string BuildBonusHeader()
    {
        return "BONUS";
    }

    public static string BuildBonusTitle(int upgradeIndex, UpgradeRarity rarity)
    {
        int multiplier = UpgradeOptionCatalog.GetRarityMultiplier(rarity);

        if (upgradeIndex == BonusRewardCoin)
        {
            return "+" + (25 * multiplier) + " Coins";
        }

        return "Heal +" + (25 * multiplier) + " HP";
    }

    public static string BuildBonusDescription(int upgradeIndex, UpgradeRarity rarity)
    {
        int multiplier = UpgradeOptionCatalog.GetRarityMultiplier(rarity);

        if (upgradeIndex == BonusRewardCoin)
        {
            return "Instant coin bonus.";
        }

        return "Restore " + (25 * multiplier) + " HP instantly.";
    }

    public static string GetDisplayTitle(int upgradeIndex, string defaultTitle)
    {
        if (TryGetEvolvedTitle(upgradeIndex, out string evolvedTitle))
        {
            return evolvedTitle;
        }

        return defaultTitle ?? UpgradeOptionCatalog.GetDisplayName(upgradeIndex);
    }

    public static string BuildLegacyCardLabel(int upgradeIndex, UpgradeRarity rarity, string title, string description)
    {
        string header = BuildHeader(upgradeIndex, rarity);
        string levelLine = BuildLevelLine(upgradeIndex);
        return header + "\n" + title + "\n" + levelLine + "\n" + description;
    }

    public static string BuildLegacyBonusLabel(int upgradeIndex, UpgradeRarity rarity)
    {
        return BuildBonusHeader()
            + "\n"
            + BuildBonusTitle(upgradeIndex, rarity)
            + "\n"
            + BuildBonusDescription(upgradeIndex, rarity);
    }

    public static bool TryGetEvolutionRequirementLine(int upgradeIndex, out string requirementLine)
    {
        requirementLine = string.Empty;

        if (!IsEvolutionUpgradeCard(upgradeIndex))
        {
            return false;
        }

        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return false;
        }

        var requirements = UpgradeOptionCatalog.GetEvolutionRequirements();

        for (int i = 0; i < requirements.Count; i++)
        {
            var requirement = requirements[i];

            if (!tracker.HasEvolution(requirement.EvolutionId))
            {
                continue;
            }

            if (requirement.SkillUpgradeIndex != upgradeIndex
                && requirement.PassiveUpgradeIndex != upgradeIndex)
            {
                continue;
            }

            requirementLine = "Requires: "
                + UpgradeOptionCatalog.GetDisplayName(requirement.SkillUpgradeIndex)
                + " + "
                + UpgradeOptionCatalog.GetDisplayName(requirement.PassiveUpgradeIndex);
            return true;
        }

        return false;
    }

    public static bool IsEvolutionUpgradeCard(int upgradeIndex)
    {
        return TryGetEvolvedTitle(upgradeIndex, out _);
    }

    public static string GetCompactBuildLabel(WeaponBuildType buildType)
    {
        return buildType switch
        {
            WeaponBuildType.FireStaff => "STAFF",
            WeaponBuildType.Bow => "BOW",
            WeaponBuildType.Sword => "SWORD",
            WeaponBuildType.Blunderbuss => "BLUNDER",
            WeaponBuildType.ThunderSpear => "SPEAR",
            _ => "GENERAL"
        };
    }

    private static bool TryGetEvolvedTitle(int upgradeIndex, out string title)
    {
        title = null;
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return false;
        }

        if (upgradeIndex == 6 && tracker.HasEvolution(BuildEvolutionId.FlameOrbit))
        {
            title = "Flame Orbit";
            return true;
        }

        if (upgradeIndex == UpgradeOptionCatalog.FrostSigilIndex
            && tracker.HasEvolution(BuildEvolutionId.GlacialPrison))
        {
            title = "Glacial Prison";
            return true;
        }

        if (upgradeIndex == UpgradeOptionCatalog.ShadowRiftIndex
            && tracker.HasEvolution(BuildEvolutionId.AbyssSingularity))
        {
            title = "Abyss Singularity";
            return true;
        }

        if (upgradeIndex == UpgradeOptionCatalog.ShrapnelStormIndex
            && tracker.HasEvolution(BuildEvolutionId.DragonmouthBlunderbuss))
        {
            title = "Dragonmouth Blunderbuss";
            return true;
        }

        if (upgradeIndex == UpgradeOptionCatalog.StormConduitIndex
            && tracker.HasEvolution(BuildEvolutionId.StormcallerSpear))
        {
            title = "Stormcaller Spear";
            return true;
        }

        return false;
    }

    private static string JoinHeaderParts(string rarityLabel, string categoryLabel, string buildLabel = "")
    {
        string header = string.IsNullOrEmpty(rarityLabel) ? "COMMON" : rarityLabel;

        if (!string.IsNullOrEmpty(categoryLabel))
        {
            header += " · " + categoryLabel;
        }

        if (!string.IsNullOrEmpty(buildLabel))
        {
            header += " · " + buildLabel;
        }

        return header;
    }
}
