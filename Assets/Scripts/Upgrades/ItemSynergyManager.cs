using System.Text;

public static class ItemSynergyManager
{
    public static bool IsThunderKingActive()
    {
        return HasUpgrade(UpgradeOptionCatalog.StormCrownIndex)
            && HasUpgrade(UpgradeOptionCatalog.ConductiveCoreIndex);
    }

    public static bool IsReaperSightActive()
    {
        return HasUpgrade(UpgradeOptionCatalog.DeathMarkIndex)
            && HasUpgrade(UpgradeOptionCatalog.HuntersEyeIndex);
    }

    public static bool IsBlackHoleBellActive()
    {
        return HasUpgrade(UpgradeOptionCatalog.VoidBellIndex)
            && HasUpgrade(UpgradeOptionCatalog.GravityStoneIndex);
    }

    public static bool HasAnyActiveSynergy()
    {
        return IsThunderKingActive() || IsReaperSightActive() || IsBlackHoleBellActive();
    }

    public static string GetActiveSynergySummary()
    {
        StringBuilder builder = new StringBuilder();

        if (IsThunderKingActive())
        {
            builder.AppendLine("Thunder King");
        }

        if (IsReaperSightActive())
        {
            builder.AppendLine("Reaper Sight");
        }

        if (IsBlackHoleBellActive())
        {
            builder.AppendLine("Black Hole Bell");
        }

        return builder.ToString().TrimEnd();
    }

    private static bool HasUpgrade(int upgradeIndex)
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        return tracker.IsTrackedUpgrade(upgradeIndex);
    }
}
