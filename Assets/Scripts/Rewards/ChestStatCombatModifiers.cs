using UnityEngine;

public static class ChestStatCombatModifiers
{
    private const float MaxCritChance = 0.25f;
    private const float MaxCritDamageBonus = 1.5f;
    private const float MaxDashCooldownReduction = 0.35f;
    private const float MaxSkillAreaBonus = 0.60f;

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
