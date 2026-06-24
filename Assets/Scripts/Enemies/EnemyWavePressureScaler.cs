using UnityEngine;

public static class EnemyWavePressureScaler
{
    public const float MinSpawnIntervalMultiplier = 0.70f;
    public const int MaxAliveBonusCap = 8;

    public static float GetSpawnIntervalMultiplier(int wave)
    {
        int safeWave = Mathf.Max(1, wave);
        float multiplier = 1f - (safeWave - 1) * 0.015f;
        return Mathf.Clamp(multiplier, MinSpawnIntervalMultiplier, 1f);
    }

    public static int GetMaxAliveBonus(int wave)
    {
        int safeWave = Mathf.Max(1, wave);
        return Mathf.Min((safeWave - 1) / 3, MaxAliveBonusCap);
    }
}
