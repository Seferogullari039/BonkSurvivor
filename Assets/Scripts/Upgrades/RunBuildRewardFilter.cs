using System;
using System.Collections.Generic;

public static class RunBuildRewardFilter
{
    public static bool CanOfferUpgrade(int upgradeIndex)
    {
        return CanOfferUpgrade(GetTracker(), upgradeIndex);
    }

    public static bool CanOfferUpgrade(RunBuildTracker tracker, int upgradeIndex)
    {
        if (!UpgradeOptionCatalog.CanOfferInRewardPool(upgradeIndex))
        {
            return false;
        }

        if (tracker == null)
        {
            return true;
        }

        if (tracker.IsUpgradeMaxed(upgradeIndex))
        {
            return false;
        }

        if (tracker.HasUpgrade(upgradeIndex))
        {
            return true;
        }

        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        return category == RewardCategory.Skill
            ? !tracker.IsSkillSlotFull()
            : !tracker.IsPassiveSlotFull();
    }

    public static List<int> BuildPoolIndices()
    {
        return BuildLevelUpPoolIndices();
    }

    public static List<int> BuildLevelUpPoolIndices()
    {
        List<int> indices = new List<int>(UpgradeOptionCatalog.OptionCount);

        for (int i = 0; i < UpgradeOptionCatalog.OptionCount; i++)
        {
            if (UpgradeOptionCatalog.IsLevelUpEligible(i))
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    public static List<int> BuildChestSpecialPoolIndices()
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < UpgradeOptionCatalog.OptionCount; i++)
        {
            if (UpgradeOptionCatalog.IsChestSpecialEligible(i))
            {
                indices.Add(i);
            }
        }

        return indices;
    }

    public static bool ShouldUseBonusFallback()
    {
        return GetTracker().IsBuildFullyMaxed();
    }

    public static List<int> FilterEligibleCandidates(List<int> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return candidates ?? new List<int>();
        }

        RunBuildTracker tracker = GetTracker();

        if (tracker.IsBuildFullyMaxed())
        {
            return new List<int>();
        }

        List<int> primary = FilterCandidates(candidates, index => CanOfferUpgrade(tracker, index));

        if (primary.Count > 0)
        {
            return primary;
        }

        List<int> buildLockFallback = FilterCandidates(
            candidates,
            index => PassesBuildLockFallback(tracker, index));

        if (buildLockFallback.Count > 0)
        {
            return buildLockFallback;
        }

        List<int> freeSlotFallback = FilterCandidates(
            candidates,
            index => PassesFreeSlotNotMaxed(tracker, index));

        if (freeSlotFallback.Count > 0)
        {
            return freeSlotFallback;
        }

        List<int> trackedNotMaxed = FilterCandidates(
            candidates,
            index => tracker.HasUpgrade(index) && !tracker.IsUpgradeMaxed(index));

        if (trackedNotMaxed.Count > 0)
        {
            return trackedNotMaxed;
        }

        return new List<int>();
    }

    private static RunBuildTracker GetTracker()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        return tracker;
    }

    private static List<int> FilterCandidates(List<int> candidates, Func<int, bool> predicate)
    {
        List<int> filtered = new List<int>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            int candidate = candidates[i];

            if (RewardOfferActionState.IsBanished(candidate))
            {
                continue;
            }

            if (predicate(candidates[i]))
            {
                filtered.Add(candidates[i]);
            }
        }

        return filtered;
    }

    private static bool PassesBuildLockFallback(RunBuildTracker tracker, int upgradeIndex)
    {
        if (tracker.IsUpgradeMaxed(upgradeIndex))
        {
            return false;
        }

        if (tracker.HasUpgrade(upgradeIndex))
        {
            return true;
        }

        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        return category == RewardCategory.Skill
            ? !tracker.IsSkillSlotFull()
            : !tracker.IsPassiveSlotFull();
    }

    private static bool PassesFreeSlotNotMaxed(RunBuildTracker tracker, int upgradeIndex)
    {
        if (tracker.IsUpgradeMaxed(upgradeIndex))
        {
            return false;
        }

        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        return category == RewardCategory.Skill
            ? !tracker.IsSkillSlotFull()
            : !tracker.IsPassiveSlotFull();
    }
}
