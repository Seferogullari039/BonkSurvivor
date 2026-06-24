using System.Collections.Generic;
using System.Text;
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

    public static string BuildInventoryPanelText()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        StringBuilder builder = new StringBuilder();
        AppendSectionHeader(builder, "Weapons");
        AppendInventorySlotLines(builder, tracker, RewardCategory.Skill);
        builder.AppendLine();
        AppendSectionHeader(builder, "Passives");
        AppendInventorySlotLines(builder, tracker, RewardCategory.Passive);
        builder.AppendLine();
        AppendSectionHeader(builder, "Items");

        string synergySummary = ItemSynergyManager.GetActiveSynergySummary();

        if (string.IsNullOrEmpty(synergySummary))
        {
            builder.AppendLine("  —");
        }
        else
        {
            string[] lines = synergySummary.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                builder.AppendLine("  • " + lines[i].Trim());
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildStatsPanelText(PlayerStats playerStats)
    {
        StringBuilder builder = new StringBuilder();

        if (playerStats != null)
        {
            AppendStatLine(builder, "HP", playerStats.CurrentHealth + " / " + playerStats.EffectiveMaxHealth);
            AppendStatLine(builder, "Coin", playerStats.Coins.ToString());
            AppendStatLine(builder, "Level", playerStats.CurrentLevel.ToString());
        }
        else
        {
            AppendStatLine(builder, "HP", "—");
            AppendStatLine(builder, "Coin", "—");
            AppendStatLine(builder, "Level", "—");
        }

        builder.AppendLine();
        AppendSectionHeader(builder, "Chest Buffs");
        builder.AppendLine("  " + BuildCompactChestBuffSummary());
        builder.AppendLine();
        AppendSectionHeader(builder, "Synergies");

        string synergySummary = ItemSynergyManager.GetActiveSynergySummary();

        if (string.IsNullOrEmpty(synergySummary))
        {
            builder.AppendLine("  —");
        }
        else
        {
            string[] lines = synergySummary.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                {
                    continue;
                }

                builder.AppendLine("  " + lines[i].Trim());
            }
        }

        builder.AppendLine();
        AppendStatLine(builder, "Damage", "—");
        AppendStatLine(builder, "Crit", "—");
        AppendStatLine(builder, "Luck", "—");

        return builder.ToString().TrimEnd();
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

    private static void AppendInventorySlotLines(StringBuilder builder, RunBuildTracker tracker, RewardCategory category)
    {
        for (int i = 0; i < RunBuildTracker.MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = category == RewardCategory.Skill
                ? tracker.GetSkillSlot(i)
                : tracker.GetPassiveSlot(i);

            builder.Append("  ");
            builder.AppendLine(FormatInventorySlotLine(entry));
        }
    }

    private static void AppendSectionHeader(StringBuilder builder, string label)
    {
        builder.AppendLine(label);
    }

    private static void AppendStatLine(StringBuilder builder, string label, string value)
    {
        builder.Append(label.PadRight(10));
        builder.AppendLine(value);
    }

    private static string FormatInventorySlotLine(RunBuildSlotEntry entry)
    {
        if (entry == null)
        {
            return "—";
        }

        int maxLevel = UpgradeOptionCatalog.GetMaxLevel(entry.UpgradeIndex);
        string displayName = GetInventoryDisplayName(entry);

        if (entry.Level >= maxLevel)
        {
            return "• " + displayName + " Lv. MAX";
        }

        return "• " + displayName + " Lv. " + entry.Level;
    }

    private static string GetInventoryDisplayName(RunBuildSlotEntry entry)
    {
        if (entry == null)
        {
            return "—";
        }

        return GetDisplayTitle(entry.UpgradeIndex, entry.DisplayName);
    }

    private static string BuildCompactChestBuffSummary()
    {
        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null)
        {
            return "—";
        }

        IReadOnlyList<ChestStatBuffEntry> buffs = tracker.GetActiveBuffs();

        if (buffs == null || buffs.Count == 0)
        {
            return "—";
        }

        StringBuilder builder = new StringBuilder();
        int visibleCount = Mathf.Min(buffs.Count, 4);

        for (int i = 0; i < visibleCount; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            builder.Append("  • ");
            builder.Append(ChestStatBuffTracker.FormatHudBadgeText(buffs[i]));
        }

        return builder.ToString();
    }
}
