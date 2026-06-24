using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChestStatBuffEntry
{
    public ChestStatRewardType Type;
    public int Stacks;
    public float TotalValue;
    public UpgradeRarity BestRarity;
    public string DisplayName;
    public string LastDescription;
}

public class ChestStatBuffTracker : MonoBehaviour
{
    public static ChestStatBuffTracker Instance { get; private set; }

    public event Action OnBuffsChanged;

    private readonly Dictionary<ChestStatRewardType, ChestStatBuffEntry> buffs = new Dictionary<ChestStatRewardType, ChestStatBuffEntry>();
    private readonly List<ChestStatBuffEntry> activeBuffCache = new List<ChestStatBuffEntry>();

    public static ChestStatBuffTracker GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        ChestStatBuffTracker existing = FindFirstObjectByType<ChestStatBuffTracker>();

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject trackerObject = new GameObject("ChestStatBuffTracker");
        return trackerObject.AddComponent<ChestStatBuffTracker>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RecordBuff(
        ChestStatRewardType type,
        UpgradeRarity rarity,
        float value,
        string displayName,
        string description)
    {
        if (type == ChestStatRewardType.Heal)
        {
            return;
        }

        if (!buffs.TryGetValue(type, out ChestStatBuffEntry entry))
        {
            entry = new ChestStatBuffEntry
            {
                Type = type,
                Stacks = 0,
                TotalValue = 0f,
                BestRarity = rarity,
                DisplayName = displayName ?? string.Empty,
                LastDescription = description ?? string.Empty
            };

            buffs[type] = entry;
        }

        entry.Stacks++;
        entry.TotalValue += value;
        entry.DisplayName = displayName ?? entry.DisplayName;
        entry.LastDescription = description ?? entry.LastDescription;

        if ((int)rarity > (int)entry.BestRarity)
        {
            entry.BestRarity = rarity;
        }

        OnBuffsChanged?.Invoke();
    }

    public int GetStacks(ChestStatRewardType type)
    {
        return buffs.TryGetValue(type, out ChestStatBuffEntry entry) ? entry.Stacks : 0;
    }

    public float GetTotalValue(ChestStatRewardType type)
    {
        return buffs.TryGetValue(type, out ChestStatBuffEntry entry) ? entry.TotalValue : 0f;
    }

    public IReadOnlyList<ChestStatBuffEntry> GetActiveBuffs()
    {
        activeBuffCache.Clear();

        foreach (KeyValuePair<ChestStatRewardType, ChestStatBuffEntry> pair in buffs)
        {
            if (pair.Value != null && pair.Value.Stacks > 0)
            {
                activeBuffCache.Add(pair.Value);
            }
        }

        activeBuffCache.Sort((a, b) => a.Type.CompareTo(b.Type));
        return activeBuffCache;
    }

    public string GetTooltip(ChestStatRewardType type)
    {
        if (!buffs.TryGetValue(type, out ChestStatBuffEntry entry) || entry.Stacks <= 0)
        {
            return string.Empty;
        }

        return entry.DisplayName + "\n"
            + FormatTotalLine(entry) + "\n"
            + "Stacks: " + entry.Stacks + "\n"
            + "Total: " + FormatTotalSummary(entry) + "\n"
            + "Source: Chest rewards";
    }

    public static string GetShortLabel(ChestStatRewardType type)
    {
        return type switch
        {
            ChestStatRewardType.MaxHealth => "HP",
            ChestStatRewardType.MoveSpeed => "SPD",
            ChestStatRewardType.AttackCooldown => "CD",
            ChestStatRewardType.PickupRange => "MAG",
            ChestStatRewardType.CoinGain => "COIN",
            ChestStatRewardType.XpGain => "XP",
            _ => "?"
        };
    }

    public static string FormatPauseSummaryLine(ChestStatBuffEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        return GetShortLabel(entry.Type) + " x" + entry.Stacks + " — " + FormatTotalSummary(entry);
    }

    public static string FormatTotalSummary(ChestStatBuffEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        int percent = Mathf.RoundToInt(entry.TotalValue * 100f);

        return entry.Type switch
        {
            ChestStatRewardType.AttackCooldown => "Attack cooldown -" + percent + "%",
            ChestStatRewardType.MaxHealth => "Max HP +" + percent + "%",
            ChestStatRewardType.MoveSpeed => "Move speed +" + percent + "%",
            ChestStatRewardType.PickupRange => "Pickup range +" + percent + "%",
            ChestStatRewardType.CoinGain => "Coin gain +" + percent + "%",
            ChestStatRewardType.XpGain => "XP gain +" + percent + "%",
            _ => "+" + percent + "%"
        };
    }

    public void ClearRun()
    {
        buffs.Clear();
        activeBuffCache.Clear();
        OnBuffsChanged?.Invoke();
    }

    private static string FormatTotalLine(ChestStatBuffEntry entry)
    {
        return entry.LastDescription?.TrimEnd('.') ?? FormatTotalSummary(entry);
    }
}
