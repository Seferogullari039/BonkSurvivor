using UnityEngine;

public static class ChestStatRewardCatalog
{
    private static readonly ChestStatRewardType[] RewardPool =
    {
        ChestStatRewardType.MaxHealth,
        ChestStatRewardType.MoveSpeed,
        ChestStatRewardType.AttackCooldown,
        ChestStatRewardType.PickupRange,
        ChestStatRewardType.CoinGain,
        ChestStatRewardType.XpGain,
        ChestStatRewardType.Heal
    };

    public static ChestStatRewardType RollRandomReward()
    {
        return RewardPool[Random.Range(0, RewardPool.Length)];
    }

    public static void GetDisplay(
        ChestStatRewardType rewardType,
        UpgradeRarity rarity,
        out string title,
        out string description)
    {
        float percent = GetPercent(rewardType, rarity);

        switch (rewardType)
        {
            case ChestStatRewardType.MaxHealth:
                title = "Vital Boost";
                description = "Max HP +" + Mathf.RoundToInt(percent * 100f) + "%.";
                break;
            case ChestStatRewardType.MoveSpeed:
                title = "Swift Body";
                description = "Move speed +" + Mathf.RoundToInt(percent * 100f) + "%.";
                break;
            case ChestStatRewardType.AttackCooldown:
                title = "Battle Tempo";
                description = "Attack cooldown -" + Mathf.RoundToInt(percent * 100f) + "%.";
                break;
            case ChestStatRewardType.PickupRange:
                title = "Magnet Pulse";
                description = "Pickup range +" + Mathf.RoundToInt(percent * 100f) + "%.";
                break;
            case ChestStatRewardType.CoinGain:
                title = "Golden Charm";
                description = "Coin gain +" + Mathf.RoundToInt(percent * 100f) + "%.";
                break;
            case ChestStatRewardType.XpGain:
                title = "Wisdom Spark";
                description = "XP gain +" + Mathf.RoundToInt(percent * 100f) + "%.";
                break;
            default:
                title = "Recovery";
                description = GetHealDescription(rarity);
                break;
        }
    }

    public static void Apply(ChestStatRewardType rewardType, UpgradeRarity rarity, PlayerStats playerStats)
    {
        if (playerStats == null)
        {
            return;
        }

        float percent = GetPercent(rewardType, rarity);

        switch (rewardType)
        {
            case ChestStatRewardType.MaxHealth:
                playerStats.ApplyChestMaxHealthBonus(percent);
                break;
            case ChestStatRewardType.MoveSpeed:
                playerStats.ApplyChestMoveSpeedBonus(percent);
                break;
            case ChestStatRewardType.AttackCooldown:
                playerStats.IncreaseStarterWeaponFireRate(percent);
                break;
            case ChestStatRewardType.PickupRange:
                UpgradeManager upgradeManager = UpgradeManager.GetOrCreateInstance();
                upgradeManager?.IncreasePickupRange(percent);
                break;
            case ChestStatRewardType.CoinGain:
                playerStats.ApplyChestCoinGainBonus(percent);
                break;
            case ChestStatRewardType.XpGain:
                playerStats.ApplyChestXpGainBonus(percent);
                break;
            case ChestStatRewardType.Heal:
                ApplyHeal(rarity, playerStats);
                break;
        }
    }

    public static string GetIconKey(ChestStatRewardType rewardType)
    {
        return rewardType switch
        {
            ChestStatRewardType.MaxHealth => "chest_stat_health",
            ChestStatRewardType.MoveSpeed => "chest_stat_speed",
            ChestStatRewardType.AttackCooldown => "chest_stat_tempo",
            ChestStatRewardType.PickupRange => "magnet_sense",
            ChestStatRewardType.CoinGain => "chest_stat_coins",
            ChestStatRewardType.XpGain => "chest_stat_xp",
            _ => "chest_stat_heal"
        };
    }

    private static float GetPercent(ChestStatRewardType rewardType, UpgradeRarity rarity)
    {
        return rewardType switch
        {
            ChestStatRewardType.MaxHealth => rarity switch
            {
                UpgradeRarity.Legendary => 0.25f,
                UpgradeRarity.Epic => 0.15f,
                UpgradeRarity.Rare => 0.10f,
                _ => 0.05f
            },
            ChestStatRewardType.MoveSpeed => rarity switch
            {
                UpgradeRarity.Legendary => 0.15f,
                UpgradeRarity.Epic => 0.10f,
                UpgradeRarity.Rare => 0.06f,
                _ => 0.03f
            },
            ChestStatRewardType.AttackCooldown => rarity switch
            {
                UpgradeRarity.Legendary => 0.15f,
                UpgradeRarity.Epic => 0.10f,
                UpgradeRarity.Rare => 0.06f,
                _ => 0.03f
            },
            ChestStatRewardType.PickupRange => rarity switch
            {
                UpgradeRarity.Legendary => 0.50f,
                UpgradeRarity.Epic => 0.35f,
                UpgradeRarity.Rare => 0.20f,
                _ => 0.10f
            },
            ChestStatRewardType.CoinGain => rarity switch
            {
                UpgradeRarity.Legendary => 0.35f,
                UpgradeRarity.Epic => 0.20f,
                UpgradeRarity.Rare => 0.10f,
                _ => 0.05f
            },
            ChestStatRewardType.XpGain => rarity switch
            {
                UpgradeRarity.Legendary => 0.35f,
                UpgradeRarity.Epic => 0.20f,
                UpgradeRarity.Rare => 0.10f,
                _ => 0.05f
            },
            _ => 0f
        };
    }

    private static void ApplyHeal(UpgradeRarity rarity, PlayerStats playerStats)
    {
        if (rarity == UpgradeRarity.Legendary)
        {
            playerStats.HealToFull();
            return;
        }

        int amount = rarity switch
        {
            UpgradeRarity.Epic => 50,
            UpgradeRarity.Rare => 30,
            _ => 15
        };

        playerStats.HealAmount(amount);
    }

    private static string GetHealDescription(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Legendary => "Restore full HP.",
            UpgradeRarity.Epic => "Heal 50 HP.",
            UpgradeRarity.Rare => "Heal 30 HP.",
            _ => "Heal 15 HP."
        };
    }
}
