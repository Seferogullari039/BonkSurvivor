public static class GoldenDragonSpawnTracker
{
    private static bool goldenDragonActive;

    public static bool CanSpawn => !goldenDragonActive;

    public static void RegisterActive()
    {
        goldenDragonActive = true;
    }

    public static void UnregisterActive()
    {
        goldenDragonActive = false;
    }

    public static void ResetRun()
    {
        goldenDragonActive = false;
    }
}
