using UnityEngine;

public static class PassiveBuildModifiers
{
    private const float BattleFocusPercentPerLevel = 0.05f;
    private const float WindRunnerPercentPerLevel = 0.04f;
    private const float TreasureInstinctPercentPerLevel = 0.08f;

    public static float GetBattleFocusDamageMultiplier()
    {
        return 1f + BattleFocusPercentPerLevel * GetTrackedLevel(UpgradeOptionCatalog.BattleFocusIndex);
    }

    public static float GetWindRunnerMoveSpeedMultiplier()
    {
        return 1f + WindRunnerPercentPerLevel * GetTrackedLevel(UpgradeOptionCatalog.WindRunnerIndex);
    }

    public static float GetTreasureInstinctCoinMultiplier()
    {
        return 1f + TreasureInstinctPercentPerLevel * GetTrackedLevel(UpgradeOptionCatalog.TreasureInstinctIndex);
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
