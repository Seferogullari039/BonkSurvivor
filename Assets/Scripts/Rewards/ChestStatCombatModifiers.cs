using UnityEngine;

public static class ChestStatCombatModifiers
{
    private const float MaxCritChance = 0.25f;
    private const float MaxCritDamageBonus = 1.5f;
    private const float MaxDashCooldownReduction = 0.35f;
    private const float MaxSkillAreaBonus = 0.60f;
    private const float MaxDamagePercentBonus = 1.0f;
    private const float MaxDamageReduction = 0.50f;
    private const float MaxHpRegenPerSecond = 3f;

    public static float GetDamageMultiplier()
    {
        float total = GetTotalValue(ChestStatRewardType.DamagePercent);
        return 1f + Mathf.Clamp(total, 0f, MaxDamagePercentBonus);
    }

    public static float GetDamageTakenMultiplier()
    {
        float reduction = Mathf.Clamp(GetTotalValue(ChestStatRewardType.DamageReduction), 0f, MaxDamageReduction);
        return 1f - reduction;
    }

    public static float GetHpRegenPerSecond()
    {
        float total = GetTotalValue(ChestStatRewardType.HpRegen);
        return Mathf.Clamp(total, 0f, MaxHpRegenPerSecond);
    }

    public static float GetCritChance()
    {
        float total = GetTotalValue(ChestStatRewardType.CritChance);
        return Mathf.Clamp(total, 0f, MaxCritChance);
    }

    public static float GetCritDamageMultiplier()
    {
        float total = GetTotalValue(ChestStatRewardType.CritDamage);
        return 1f + Mathf.Clamp(total, 0f, MaxCritDamageBonus);
    }

    public static float GetDashCooldownMultiplier()
    {
        float reduction = Mathf.Clamp(GetTotalValue(ChestStatRewardType.DashCooldown), 0f, MaxDashCooldownReduction);
        return 1f - reduction;
    }

    public static float GetSkillAreaMultiplier()
    {
        float total = GetTotalValue(ChestStatRewardType.SkillArea);
        return 1f + Mathf.Clamp(total, 0f, MaxSkillAreaBonus);
    }

    public static float ScaleSkillRadius(float radius)
    {
        if (radius <= 0f)
        {
            return radius;
        }

        return radius * GetSkillAreaMultiplier();
    }

    public static int ApplyCritToDamage(int damage)
    {
        if (damage <= 0)
        {
            return damage;
        }

        float critChance = GetCritChance();

        if (critChance <= 0f)
        {
            return damage;
        }

        if (Random.value >= critChance)
        {
            return damage;
        }

        return Mathf.Max(1, Mathf.RoundToInt(damage * GetCritDamageMultiplier()));
    }

    private static float GetTotalValue(ChestStatRewardType type)
    {
        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null)
        {
            return 0f;
        }

        return tracker.GetTotalValue(type);
    }
}
