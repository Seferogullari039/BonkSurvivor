using System.Collections.Generic;
using UnityEngine;

public static class UpgradeOptionCatalog
{
    public const int DefaultMaxLevel = 10;
    public const int DefaultEvolutionRequiredSkillLevel = 10;
    public const int DefaultEvolutionRequiredPassiveLevel = 5;

    public const int RocketLauncherIndex = 7;
    public const int LaserBeamIndex = 9;
    public const int FrostSigilIndex = 15;
    public const int CryoCoreIndex = 16;
    public const int ShadowRiftIndex = 17;
    public const int VoidCatalystIndex = 18;
    public const int ShrapnelStormIndex = 19;
    public const int PowderKegIndex = 20;
    public const int StormConduitIndex = 21;
    public const int ConductiveCoreIndex = 22;
    public const int GoldenMagnetIndex = 23;
    public const int StormCrownIndex = 24;
    public const int DeathMarkIndex = 25;
    public const int HuntersEyeIndex = 26;
    public const int GravityStoneIndex = 27;
    public const int VoidBellIndex = 28;
    public const int KeyIndex = 29;
    public const int LuckIndex = 30;
    public const int DragonHeartIndex = 31;
    public const int TitanGauntletIndex = 32;
    public const int StarfallSigilIndex = 33;
    public const int CelestialShieldIndex = 34;
    public const int BloodPactIndex = 35;
    public const int VitalityIndex = 36;
    public const int BattleFocusIndex = 37;
    public const int WindRunnerIndex = 38;
    public const int TreasureInstinctIndex = 39;

    public static int OptionCount => Options.Length;

    public readonly struct OptionMetadata
    {
        public OptionMetadata(
            RewardCategory category,
            UpgradeRarity assignedRarity,
            WeaponBuildType buildType,
            int maxLevel = DefaultMaxLevel)
        {
            Category = category;
            AssignedRarity = assignedRarity;
            BuildType = buildType;
            MaxLevel = maxLevel;
        }

        public RewardCategory Category { get; }
        public UpgradeRarity AssignedRarity { get; }
        public WeaponBuildType BuildType { get; }
        public int MaxLevel { get; }
    }

    public readonly struct EvolutionRequirement
    {
        public EvolutionRequirement(
            BuildEvolutionId evolutionId,
            int skillUpgradeIndex,
            int requiredSkillLevel,
            int passiveUpgradeIndex,
            int requiredPassiveLevel,
            string displayName)
        {
            EvolutionId = evolutionId;
            SkillUpgradeIndex = skillUpgradeIndex;
            RequiredSkillLevel = requiredSkillLevel;
            PassiveUpgradeIndex = passiveUpgradeIndex;
            RequiredPassiveLevel = requiredPassiveLevel;
            DisplayName = displayName;
        }

        public BuildEvolutionId EvolutionId { get; }
        public int SkillUpgradeIndex { get; }
        public int RequiredSkillLevel { get; }
        public int PassiveUpgradeIndex { get; }
        public int RequiredPassiveLevel { get; }
        public string DisplayName { get; }
    }

    private static readonly OptionMetadata[] Options =
    {
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General),     // 0 Rapid Mechanism
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
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Epic, WeaponBuildType.Sword),         // 14 Blade Tempest
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.General),       // 15 Frost Sigil
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Rare, WeaponBuildType.General, 5), // 16 Cryo Core
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.General),       // 17 Shadow Rift
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Rare, WeaponBuildType.General, 5), // 18 Void Catalyst
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.Blunderbuss),       // 19 Shrapnel Storm
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Rare, WeaponBuildType.Blunderbuss, 5), // 20 Powder Keg
        new OptionMetadata(RewardCategory.Skill, UpgradeRarity.Rare, WeaponBuildType.ThunderSpear),     // 21 Storm Conduit
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Rare, WeaponBuildType.ThunderSpear, 5), // 22 Conductive Core
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 23 Golden Magnet
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 24 Storm Crown
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 25 Death Mark
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Epic, WeaponBuildType.General, 3), // 26 Hunter's Eye
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Epic, WeaponBuildType.General, 3), // 27 Gravity Stone
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 28 Void Bell
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Rare, WeaponBuildType.General, 30), // 29 Key
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Epic, WeaponBuildType.General, 10), // 30 Luck
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 31 Dragon Heart
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 32 Titan Gauntlet
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 33 Starfall Sigil
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1), // 34 Celestial Shield
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Legendary, WeaponBuildType.General, 1) // 35 Blood Pact
        ,
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General, 5), // 36 Vitality
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General, 5), // 37 Battle Focus
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Common, WeaponBuildType.General, 5), // 38 Wind Runner
        new OptionMetadata(RewardCategory.Passive, UpgradeRarity.Rare, WeaponBuildType.General, 5) // 39 Treasure Instinct
    };

    private static readonly EvolutionRequirement[] EvolutionRequirements =
    {
        new EvolutionRequirement(
            BuildEvolutionId.FlameOrbit,
            6,
            DefaultEvolutionRequiredSkillLevel,
            0,
            DefaultEvolutionRequiredPassiveLevel,
            "Flame Orbit"),
        new EvolutionRequirement(
            BuildEvolutionId.CataclysmMeteor,
            10,
            DefaultEvolutionRequiredSkillLevel,
            3,
            DefaultEvolutionRequiredPassiveLevel,
            "Cataclysm Meteor"),
        new EvolutionRequirement(
            BuildEvolutionId.StormArrows,
            4,
            DefaultEvolutionRequiredSkillLevel,
            3,
            DefaultEvolutionRequiredPassiveLevel,
            "Storm Arrows"),
        new EvolutionRequirement(
            BuildEvolutionId.BladeTempestEvolution,
            11,
            DefaultEvolutionRequiredSkillLevel,
            3,
            DefaultEvolutionRequiredPassiveLevel,
            "Blade Tempest Evolution"),
        new EvolutionRequirement(
            BuildEvolutionId.GlacialPrison,
            FrostSigilIndex,
            DefaultEvolutionRequiredSkillLevel,
            CryoCoreIndex,
            DefaultEvolutionRequiredPassiveLevel,
            "Glacial Prison"),
        new EvolutionRequirement(
            BuildEvolutionId.AbyssSingularity,
            ShadowRiftIndex,
            DefaultEvolutionRequiredSkillLevel,
            VoidCatalystIndex,
            DefaultEvolutionRequiredPassiveLevel,
            "Abyss Singularity"),
        new EvolutionRequirement(
            BuildEvolutionId.DragonmouthBlunderbuss,
            ShrapnelStormIndex,
            DefaultEvolutionRequiredSkillLevel,
            PowderKegIndex,
            DefaultEvolutionRequiredPassiveLevel,
            "Dragonmouth Blunderbuss"),
        new EvolutionRequirement(
            BuildEvolutionId.StormcallerSpear,
            StormConduitIndex,
            DefaultEvolutionRequiredSkillLevel,
            ConductiveCoreIndex,
            DefaultEvolutionRequiredPassiveLevel,
            "Stormcaller Spear")
    };

    public static bool CanOfferInRewardPool(int upgradeIndex)
    {
        // Legacy/disabled for now. Kept for possible future reuse.
        if (upgradeIndex == RocketLauncherIndex || upgradeIndex == LaserBeamIndex)
        {
            return false;
        }

        if (upgradeIndex < 0 || upgradeIndex >= Options.Length)
        {
            return false;
        }

        return true;
    }

    public static bool IsLevelUpEligible(int upgradeIndex)
    {
        if (!CanOfferInRewardPool(upgradeIndex))
        {
            return false;
        }

        return (upgradeIndex >= 0 && upgradeIndex <= ConductiveCoreIndex)
            || upgradeIndex == VitalityIndex
            || upgradeIndex == BattleFocusIndex
            || upgradeIndex == WindRunnerIndex
            || upgradeIndex == TreasureInstinctIndex;
    }

    public static bool IsChestSpecialEligible(int upgradeIndex)
    {
        if (!CanOfferInRewardPool(upgradeIndex))
        {
            return false;
        }

        return upgradeIndex >= GoldenMagnetIndex && upgradeIndex <= BloodPactIndex;
    }

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

    public static int GetMaxLevel(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= Options.Length)
        {
            return DefaultMaxLevel;
        }

        return Options[upgradeIndex].MaxLevel;
    }

    public static IReadOnlyList<EvolutionRequirement> GetEvolutionRequirements()
    {
        return EvolutionRequirements;
    }

    public static string GetEvolutionDisplayName(BuildEvolutionId id)
    {
        if (id == BuildEvolutionId.None)
        {
            return "None";
        }

        for (int i = 0; i < EvolutionRequirements.Length; i++)
        {
            EvolutionRequirement requirement = EvolutionRequirements[i];

            if (requirement.EvolutionId == id)
            {
                return requirement.DisplayName;
            }
        }

        return id.ToString();
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
            WeaponBuildType.Blunderbuss => "BLUNDERBUSS",
            WeaponBuildType.ThunderSpear => "THUNDER SPEAR",
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
            WeaponBuildType.Blunderbuss => new Color(0.85f, 0.55f, 0.28f, 1f),
            WeaponBuildType.ThunderSpear => new Color(0.35f, 0.82f, 1f, 1f),
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
            StarterWeaponType.Blunderbuss => WeaponBuildType.Blunderbuss,
            StarterWeaponType.ThunderSpear => WeaponBuildType.ThunderSpear,
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

    public static UpgradeRarity ResolveOfferRarity(int upgradeIndex)
    {
        UpgradeRarity assigned = GetAssignedRarity(upgradeIndex);

        if (assigned != UpgradeRarity.Common)
        {
            return assigned;
        }

        return RollDisplayRarity();
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

    public static string GetDisplayName(int upgradeIndex)
    {
        if (upgradeIndex < 0 || upgradeIndex >= DisplayNames.Length)
        {
            return "Upgrade";
        }

        return DisplayNames[upgradeIndex];
    }

    private static readonly string[] DisplayNames =
    {
        "Rapid",
        "Swift",
        "Magnet",
        "Sharp",
        "Spread",
        "Pierce",
        "Orbit",
        "Rocket",
        "Chain",
        "Laser",
        "Meteor",
        "Whirlwind",
        "Arrow",
        "Inferno",
        "Blade",
        "Frost Sigil",
        "Cryo Core",
        "Shadow Rift",
        "Void Catalyst",
        "Shrapnel Storm",
        "Powder Keg",
        "Storm Conduit",
        "Conductive Core",
        "Golden Magnet",
        "Storm Crown",
        "Death Mark",
        "Hunter's Eye",
        "Gravity Stone",
        "Void Bell",
        "Key",
        "Luck",
        "Dragon Heart",
        "Titan Gauntlet",
        "Starfall Sigil",
        "Celestial Shield",
        "Blood Pact",
        "Vitality",
        "Battle Focus",
        "Wind Runner",
        "Treasure Instinct"
    };
}
