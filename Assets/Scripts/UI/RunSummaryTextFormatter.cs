using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RunSummaryTextFormatter
{
    private const string NoSkillsLabel = "No skills selected";
    private const string NoPassivesLabel = "No passives selected";
    private const string NoBuffsLabel = "No chest buffs";

    public static string BuildBuildSummary()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Skills:");
        AppendCategorySlots(builder, tracker, RewardCategory.Skill, NoSkillsLabel);
        builder.AppendLine("Passives:");
        AppendCategorySlots(builder, tracker, RewardCategory.Passive, NoPassivesLabel);

        string synergySummary = ItemSynergyManager.GetActiveSynergySummary();

        if (!string.IsNullOrEmpty(synergySummary))
        {
            builder.AppendLine("Synergies:");
            builder.AppendLine(synergySummary);
        }

        return builder.ToString().TrimEnd();
    }

    public static string BuildChestBuffSummary()
    {
        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null)
        {
            return NoBuffsLabel;
        }

        IReadOnlyList<ChestStatBuffEntry> buffs = tracker.GetActiveBuffs();

        if (buffs == null || buffs.Count == 0)
        {
            return NoBuffsLabel;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < buffs.Count; i++)
        {
            builder.Append("- ");
            builder.AppendLine(ChestStatBuffTracker.FormatTotalSummary(buffs[i]));
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendCategorySlots(
        StringBuilder builder,
        RunBuildTracker tracker,
        RewardCategory category,
        string emptyLabel)
    {
        bool hasEntries = false;

        for (int i = 0; i < RunBuildTracker.MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = category == RewardCategory.Skill
                ? tracker.GetSkillSlot(i)
                : tracker.GetPassiveSlot(i);

            if (entry == null)
            {
                continue;
            }

            hasEntries = true;
            builder.Append("- ");
            builder.AppendLine(FormatSlotLine(entry));
        }

        if (!hasEntries)
        {
            builder.AppendLine(emptyLabel);
        }
    }

    private static string FormatSlotLine(RunBuildSlotEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        int maxLevel = UpgradeOptionCatalog.GetMaxLevel(entry.UpgradeIndex);
        string displayName = GetDisplayName(entry);

        if (maxLevel <= 1)
        {
            return displayName;
        }

        return displayName + " Lv. " + entry.Level + "/" + maxLevel;
    }

    private static string GetDisplayName(RunBuildSlotEntry entry)
    {
        bool flameOrbitEvolved = entry.UpgradeIndex == 6
            && RunBuildTracker.Instance != null
            && RunBuildTracker.Instance.HasEvolution(BuildEvolutionId.FlameOrbit);

        if (flameOrbitEvolved)
        {
            return "Flame Orbit";
        }

        if (!string.IsNullOrEmpty(entry.DisplayName))
        {
            return entry.DisplayName;
        }

        return UpgradeOptionCatalog.GetDisplayName(entry.UpgradeIndex);
    }
}
