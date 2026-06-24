using System.Collections.Generic;
using UnityEngine;

public static class UpgradeOptionCatalog
{
    public readonly struct OptionMetadata
    {
        public OptionMetadata(RewardCategory category, UpgradeRarity assignedRarity, WeaponBuildType buildType)
        {
            Category = category;
            AssignedRarity = assignedRarity;
            BuildType = buildType;
        }

        public RewardCategory Category { get; }
        public UpgradeRarity AssignedRarity { get; }
        public WeaponBuildType BuildType { get; }
    }

    private static readonly OptionMetadata[] Options =
    {
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Common, WeaponBuildType.General),     // 0 Rapid Mechanism
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Common, WeaponBuildType.Bow),           // 1 Swift Projectiles
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General),   // 2 Magnet Sense
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General),   // 3 Sharp Instinct
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.Bow),           // 4 Spread Shot
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.Bow),           // 5 Piercing Shot
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.FireStaff),     // 6 Orbiting Orb
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.General),       // 7 Rocket Launcher
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.General),       // 8 Chain Lightning
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.General),       // 9 Laser Beam
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Epic, WeaponBuildType.FireStaff),     // 10 Meteor Focus
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Epic, WeaponBuildType.Sword),         // 11 Whirlwind Training
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Epic, WeaponBuildType.Bow),           // 12 Arrow Storm
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Epic, WeaponBuildType.FireStaff),     // 13 Inferno Ritual
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Epic, WeaponBuildType.Sword)          // 14 Blade Tempest
    };

    public static OptionMetadata GetMetadata(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= Options.Length)
        {
            return new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General);
        }

        return Options[upgradeIndex];
    }

    public static RewardCategory GetCategory(int upgradeIndex)
    {
        return GetMetadata(upgradeIndex).Category;
    }

    public static UpgradeRarity GetAssignedRarity(int upgradeIndex)
    {
        return GetMetadata(upgradeIndex).AssignedRarity;
    }

    public static WeaponBuildType GetBuildType(int upgradeIndex)
    {
        return GetMetadata(upgradeIndex).BuildType;
    }

    public static string GetBuildLabel(int upgradeIndex)
    {
        return GetBuildLabel(GetBuildType(upgradeIndex));
    }

    public static string GetBuildLabel(WeaponBuildType buildType)
    {
        return buildType switch
        {
            WeaponBuildType.FireStaff => "FIRE STAFF",
            WeaponBuildType.Bow => "BOW",
            WeaponBuildType.Sword => "SWORD",
            _ => "GENERAL"
        };
    }

    public static Color GetBuildColor(WeaponBuildType buildType)
    {
        return buildType switch
        {
            WeaponBuildType.FireStaff => new Color(1f, 0.52f, 0.22f, 1f),
            WeaponBuildType.Bow => new Color(0.42f, 0.88f, 0.48f, 1f),
            WeaponBuildType.Sword => new Color(0.72f, 0.8f, 0.92f, 1f),
            _ => new Color(0.86f, 0.88f, 0.92f, 1f)
        };
    }

    public static WeaponBuildType MapStarterWeaponToBuild(StarterWeaponType weaponType)
    {
        return weaponType switch
        {
            StarterWeaponType.FireStaff => WeaponBuildType.FireStaff,
            StarterWeaponType.KnightSword => WeaponBuildType.Sword,
            StarterWeaponType.HunterBow => WeaponBuildType.Bow,
            _ => WeaponBuildType.General
        };
    }

    public static bool TryPickEligibleUpgradeByBuild(
        IList<int> candidates,
        WeaponBuildType preferredBuild,
        RewardCategory category,
        out int upgradeIndex)
    {
        upgradeIndex = -1;

        if (candidates == null || candidates.Count == 0)
        {
            return false;
        }

        List<int> matches = null;

        for (int i = 0; i < candidates.Count; i++)
        {
            int candidate = candidates[i];
            OptionMetadata metadata = GetMetadata(candidate);

            if (metadata.BuildType != preferredBuild || metadata.Category != category)
            {
                continue;
            }

            matches ??= new List<int>();
            matches.Add(candidate);
        }

        if (matches == null || matches.Count == 0)
        {
            return false;
        }

        upgradeIndex = matches[Random.Range(0, matches.Count)];
        return true;
    }

    public static string GetCategoryLabel(RewardCategory category)
    {
        return category == RewardCategory.Skill ? "SKILL" : "PASSIVE";
    }

    public static Color GetCategoryColor(RewardCategory category)
    {
        return category == RewardCategory.Skill
            ? new Color(0.82f, 0.88f, 0.96f, 1f)
            : new Color(0.72f, 0.78f, 0.86f, 1f);
    }

    public static UpgradeRarity RollDisplayRarity()
    {
        int roll = Random.Range(0, 100);

        if (roll < 2)
        {
            return UpgradeRarity.Legendary;
        }

        if (roll < 10)
        {
            return UpgradeRarity.Epic;
        }

        if (roll < 35)
        {
            return UpgradeRarity.Rare;
        }

        return UpgradeRarity.Common;
    }

    public static string GetRarityLabel(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Legendary => "LEGENDARY",
            UpgradeRarity.Rare => "RARE",
            UpgradeRarity.Epic => "EPIC",
            _ => "COMMON"
        };
    }

    public static Color GetRarityColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Legendary => ChestLootRarityPalette.LegendaryAccent,
            UpgradeRarity.Rare => new Color(0.42f, 0.78f, 1f, 1f),
            UpgradeRarity.Epic => new Color(0.82f, 0.52f, 1f, 1f),
            _ => new Color(0.78f, 0.8f, 0.84f, 1f)
        };
    }

    public static Color GetRarityBackgroundColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Legendary => ChestLootRarityPalette.LegendaryBackground,
            UpgradeRarity.Rare => new Color(0.07f, 0.11f, 0.18f, 0.98f),
            UpgradeRarity.Epic => new Color(0.13f, 0.08f, 0.18f, 0.98f),
            _ => new Color(0.09f, 0.1f, 0.12f, 0.98f)
        };
    }

    public static int GetRarityMultiplier(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Legendary => 4,
            UpgradeRarity.Epic => 3,
            UpgradeRarity.Rare => 2,
            _ => 1
        };
    }
}
