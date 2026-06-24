using UnityEngine;

public static class EnemyDifficultyScaler
{
    public const float MaxHealthMultiplier = 4.0f;
    public const float MaxDamageMultiplier = 2.5f;
    public const float MaxSpeedMultiplier = 1.25f;

    public static EnemyDifficultyMultipliers GetMultipliersForWave(int wave)
    {
        int safeWave = Mathf.Max(1, wave);

        float healthMultiplier = 1f + (safeWave - 1) * 0.08f;
        float damageMultiplier = 1f + (safeWave - 1) * 0.035f;
        float speedMultiplier = 1f + Mathf.Min((safeWave - 1) * 0.012f, 0.25f);

        return new EnemyDifficultyMultipliers(
            Mathf.Min(healthMultiplier, MaxHealthMultiplier),
            Mathf.Min(damageMultiplier, MaxDamageMultiplier),
            Mathf.Min(speedMultiplier, MaxSpeedMultiplier));
    }
}

public readonly struct EnemyDifficultyMultipliers
{
    public readonly float Health;
    public readonly float Damage;
    public readonly float Speed;

    public EnemyDifficultyMultipliers(float health, float damage, float speed)
    {
        Health = health;
        Damage = damage;
        Speed = speed;
    }
}
