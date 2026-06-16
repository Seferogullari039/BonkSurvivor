public static class DragonBossSpawnTracker
{
    private static bool dragonAlive;
    private static int lastDragonWaveSpawned;

    public static bool IsDragonWave(int wave)
    {
        return wave == 10 || wave == 20 || wave == 30;
    }

    public static bool CanSpawn(int wave)
    {
        if (!IsDragonWave(wave)) return false;
        if (dragonAlive) return false;
        if (lastDragonWaveSpawned == wave) return false;
        return true;
    }

    public static void MarkSpawned(int wave)
    {
        lastDragonWaveSpawned = wave;
        dragonAlive = true;
    }

    public static void RegisterAlive()
    {
        dragonAlive = true;
    }

    public static void UnregisterAlive()
    {
        dragonAlive = false;
    }

    public static void ResetRun()
    {
        dragonAlive = false;
        lastDragonWaveSpawned = 0;
    }
}
