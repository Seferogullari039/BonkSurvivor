using System.Collections.Generic;

public static class RewardOfferActionState
{
    private const int MaxActionsPerRun = 2;

    private static int skipsUsed;
    private static int refreshesUsed;
    private static int banishesUsed;
    private static readonly HashSet<int> banishedUpgradeIndices = new HashSet<int>();

    public static int SkipsRemaining => MaxActionsPerRun - skipsUsed;
    public static int RefreshesRemaining => MaxActionsPerRun - refreshesUsed;
    public static int BanishesRemaining => MaxActionsPerRun - banishesUsed;

    public static void ResetForNewRun()
    {
        skipsUsed = 0;
        refreshesUsed = 0;
        banishesUsed = 0;
        banishedUpgradeIndices.Clear();
    }

    public static bool IsBanished(int upgradeIndex)
    {
        return upgradeIndex >= 0 && banishedUpgradeIndices.Contains(upgradeIndex);
    }

    public static bool CanBanish(int upgradeIndex)
    {
        return upgradeIndex >= 0
            && banishesUsed < MaxActionsPerRun
            && !banishedUpgradeIndices.Contains(upgradeIndex);
    }

    public static bool TryBanish(int upgradeIndex)
    {
        if (!CanBanish(upgradeIndex))
        {
            return false;
        }

        banishedUpgradeIndices.Add(upgradeIndex);
        banishesUsed++;
        return true;
    }

    public static bool CanSkip()
    {
        return skipsUsed < MaxActionsPerRun;
    }

    public static bool TryConsumeSkip()
    {
        if (!CanSkip())
        {
            return false;
        }

        skipsUsed++;
        return true;
    }

    public static bool CanRefresh()
    {
        return refreshesUsed < MaxActionsPerRun;
    }

    public static bool TryConsumeRefresh()
    {
        if (!CanRefresh())
        {
            return false;
        }

        refreshesUsed++;
        return true;
    }
}
