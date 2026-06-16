using UnityEngine;

public static class LootLimits
{
    public const int MaxCoins = 150;
    public const int MaxXPOrbs = 150;
    public const int MaxChests = 10;

    public static int GetCoinCount()
    {
        return Object.FindObjectsByType<Coin>(FindObjectsSortMode.None).Length;
    }

    public static int GetXPOrbCount()
    {
        return Object.FindObjectsByType<XPOrb>(FindObjectsSortMode.None).Length;
    }

    public static int GetChestCount()
    {
        return Object.FindObjectsByType<Chest>(FindObjectsSortMode.None).Length;
    }

    public static bool CanSpawnCoin()
    {
        return GetCoinCount() < MaxCoins;
    }

    public static bool CanSpawnXPOrb()
    {
        return GetXPOrbCount() < MaxXPOrbs;
    }

    public static bool CanSpawnChest()
    {
        return GetChestCount() < MaxChests;
    }

    public static int GetBossCoinDropCount(int desiredCount = 10)
    {
        int available = MaxCoins - GetCoinCount();
        return Mathf.Clamp(available, 0, desiredCount);
    }
}
