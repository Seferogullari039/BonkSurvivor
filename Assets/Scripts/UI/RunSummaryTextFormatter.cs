using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RunSummaryTextFormatter
{
    private const int MaxBuffCount = 6;
    private const string EmptySlotLabel = "—";
    private const string NoBuffsLabel = "None";

    public static string BuildBuildSummary()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Skills");
        AppendCategorySlots(builder, tracker, RewardCategory.Skill);
        builder.AppendLine("Passives");
        AppendCategorySlots(builder, tracker, RewardCategory.Passive);
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
        int visibleCount = Mathf.Min(buffs.Count, MaxBuffCount);

        for (int i = 0; i < visibleCount; i++)
        {
            if (i > 0)
            {
                builder.Append("   ");
            }

            builder.Append(ChestStatBuffTracker.FormatHudBadgeText(buffs[i]));
        }

        return builder.ToString();
    }

    private static void AppendCategorySlots(StringBuilder builder, RunBuildTracker tracker, RewardCategory category)
    {
        for (int i = 0; i < RunBuildTracker.MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = category == RewardCategory.Skill
                ? tracker.GetSkillSlot(i)
                : tracker.GetPassiveSlot(i);

            builder.Append("• ");
            builder.AppendLine(FormatSlotLine(entry));
        }
    }

    private static string FormatSlotLine(RunBuildSlotEntry entry)
    {
        if (entry == null)
        {
            return EmptySlotLabel;
        }

        int maxLevel = UpgradeOptionCatalog.GetMaxLevel(entry.UpgradeIndex);
        string displayName = string.IsNullOrEmpty(entry.DisplayName)
            ? UpgradeOptionCatalog.GetDisplayName(entry.UpgradeIndex)
            : entry.DisplayName;

        if (entry.Level >= maxLevel)
        {
            return displayName + " — MAX";
        }

        return displayName + " — Lv. " + entry.Level + "/" + maxLevel;
    }
}
