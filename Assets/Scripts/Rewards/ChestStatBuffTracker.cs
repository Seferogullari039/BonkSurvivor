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

        activeBuffCache.Sort((a, b) => GetHudSortOrder(a.Type).CompareTo(GetHudSortOrder(b.Type)));
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

    public static int GetHudSortOrder(ChestStatRewardType type)
    {
        return type switch
        {
            ChestStatRewardType.AttackCooldown => 0,
            ChestStatRewardType.MoveSpeed => 1,
            ChestStatRewardType.SkillArea => 2,
            ChestStatRewardType.CritChance => 3,
            ChestStatRewardType.CritDamage => 4,
            ChestStatRewardType.DamagePercent => 5,
            ChestStatRewardType.DamageReduction => 6,
            ChestStatRewardType.HpRegen => 7,
            ChestStatRewardType.DashCooldown => 8,
            ChestStatRewardType.PickupRange => 9,
            ChestStatRewardType.MaxHealth => 10,
            ChestStatRewardType.CoinGain => 11,
            ChestStatRewardType.XpGain => 12,
            _ => 99
        };
    }

    public static string GetShortLabel(ChestStatRewardType type)
    {
        return type switch
        {
            ChestStatRewardType.MaxHealth => "HP",
            ChestStatRewardType.MoveSpeed => "SPD",
            ChestStatRewardType.AttackCooldown => "CD",
            ChestStatRewardType.PickupRange => "PICK",
            ChestStatRewardType.CoinGain => "COIN",
            ChestStatRewardType.XpGain => "XP",
            ChestStatRewardType.CritChance => "CRIT",
            ChestStatRewardType.CritDamage => "CDMG",
            ChestStatRewardType.DamagePercent => "DMG",
            ChestStatRewardType.DamageReduction => "DR",
            ChestStatRewardType.HpRegen => "REGEN",
            ChestStatRewardType.DashCooldown => "DASH",
            ChestStatRewardType.SkillArea => "AREA",
            _ => "?"
        };
    }

    public static bool UsesPercentBadgeFormat(ChestStatRewardType type)
    {
        return type == ChestStatRewardType.CritChance
            || type == ChestStatRewardType.CritDamage
            || type == ChestStatRewardType.DashCooldown
            || type == ChestStatRewardType.SkillArea
            || type == ChestStatRewardType.DamagePercent
            || type == ChestStatRewardType.DamageReduction;
    }

    public static string FormatHudBadgeText(ChestStatBuffEntry entry)
    {
        if (entry == null || entry.Stacks <= 0)
        {
            return string.Empty;
        }

        if (UsesPercentBadgeFormat(entry.Type))
        {
            return FormatPercentBadgeText(entry);
        }

        if (entry.Type == ChestStatRewardType.HpRegen)
        {
            return GetShortLabel(entry.Type) + " +" + entry.TotalValue.ToString("0.0");
        }

        return GetShortLabel(entry.Type) + " +" + entry.Stacks;
    }

    private static string FormatPercentBadgeText(ChestStatBuffEntry entry)
    {
        int percent = Mathf.RoundToInt(entry.TotalValue * 100f);
        string sign = entry.Type == ChestStatRewardType.DashCooldown || entry.Type == ChestStatRewardType.DamageReduction
            ? "-"
            : "+";
        return GetShortLabel(entry.Type) + " " + sign + percent + "%";
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
            ChestStatRewardType.CritChance => "Crit chance +" + percent + "%",
            ChestStatRewardType.CritDamage => "Crit damage +" + percent + "%",
            ChestStatRewardType.DashCooldown => "Dash cooldown -" + percent + "%",
            ChestStatRewardType.SkillArea => "Skill area +" + percent + "%",
            ChestStatRewardType.DamagePercent => "Damage +" + percent + "%",
            ChestStatRewardType.DamageReduction => "Damage taken -" + percent + "%",
            ChestStatRewardType.HpRegen => "HP regen +" + entry.TotalValue.ToString("0.0") + "/s",
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
