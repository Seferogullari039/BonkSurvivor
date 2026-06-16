using UnityEngine;

public static class MetaProgressionData
{
    private const string TotalCoinsKey = "totalCoins";
    private const string UpgradeLevelHPKey = "upgradeLevelHP";
    private const string UpgradeLevelDamageKey = "upgradeLevelDamage";
    private const string UpgradeLevelSpeedKey = "upgradeLevelSpeed";
    private const string UpgradeLevelXPKey = "upgradeLevelXP";

    private static readonly int[] HpCosts = { 50, 100, 150, 200 };
    private static readonly int[] DamageCosts = { 75, 150, 225 };
    private static readonly int[] SpeedCosts = { 50, 100, 150 };
    private static readonly int[] XpCosts = { 100, 200, 300 };

    public static int TotalCoins
    {
        get => PlayerPrefs.GetInt(TotalCoinsKey, 0);
        set
        {
            PlayerPrefs.SetInt(TotalCoinsKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    public static int UpgradeLevelHP => PlayerPrefs.GetInt(UpgradeLevelHPKey, 0);
    public static int UpgradeLevelDamage => PlayerPrefs.GetInt(UpgradeLevelDamageKey, 0);
    public static int UpgradeLevelSpeed => PlayerPrefs.GetInt(UpgradeLevelSpeedKey, 0);
    public static int UpgradeLevelXP => PlayerPrefs.GetInt(UpgradeLevelXPKey, 0);

    public static int MaxHPUpgradeLevel => HpCosts.Length;
    public static int MaxDamageUpgradeLevel => DamageCosts.Length;
    public static int MaxSpeedUpgradeLevel => SpeedCosts.Length;
    public static int MaxXPUpgradeLevel => XpCosts.Length;

    public static void AddRunCoinsToTotal(int runCoins)
    {
        if (runCoins <= 0) return;

        TotalCoins += runCoins;
    }

    public static int GetNextHPCost()
    {
        return GetCost(HpCosts, UpgradeLevelHP);
    }

    public static int GetNextDamageCost()
    {
        return GetCost(DamageCosts, UpgradeLevelDamage);
    }

    public static int GetNextSpeedCost()
    {
        return GetCost(SpeedCosts, UpgradeLevelSpeed);
    }

    public static int GetNextXPCost()
    {
        return GetCost(XpCosts, UpgradeLevelXP);
    }

    public static bool TryPurchaseHPUpgrade()
    {
        return TryPurchaseUpgrade(UpgradeLevelHPKey, HpCosts, UpgradeLevelHP);
    }

    public static bool TryPurchaseDamageUpgrade()
    {
        return TryPurchaseUpgrade(UpgradeLevelDamageKey, DamageCosts, UpgradeLevelDamage);
    }

    public static bool TryPurchaseSpeedUpgrade()
    {
        return TryPurchaseUpgrade(UpgradeLevelSpeedKey, SpeedCosts, UpgradeLevelSpeed);
    }

    public static bool TryPurchaseXPUpgrade()
    {
        return TryPurchaseUpgrade(UpgradeLevelXPKey, XpCosts, UpgradeLevelXP);
    }

    public static void ApplyRunBonuses(PlayerStats playerStats, PlayerController playerController)
    {
        if (playerStats != null)
        {
            playerStats.ApplyMetaRunBonuses(UpgradeLevelHP, UpgradeLevelDamage);
            playerStats.SetMetaXpGainMultiplier(1f + 0.10f * UpgradeLevelXP);
        }

        if (playerController != null)
        {
            playerController.ApplyMetaMoveSpeedBonus(UpgradeLevelSpeed);
        }
    }

    private static int GetCost(int[] costs, int currentLevel)
    {
        if (currentLevel >= costs.Length) return -1;

        return costs[currentLevel];
    }

    private static bool TryPurchaseUpgrade(string key, int[] costs, int currentLevel)
    {
        if (currentLevel >= costs.Length) return false;

        int cost = costs[currentLevel];

        if (TotalCoins < cost) return false;

        TotalCoins -= cost;
        PlayerPrefs.SetInt(key, currentLevel + 1);
        PlayerPrefs.Save();
        return true;
    }
}
