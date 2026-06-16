public static class VoidPortalSpawnTracker
{
    private static bool portalActive;

    public static bool CanSpawn => !portalActive;

    public static void RegisterActive()
    {
        portalActive = true;
    }

    public static void UnregisterActive()
    {
        portalActive = false;
    }

    public static void ResetRun()
    {
        portalActive = false;
    }
}
