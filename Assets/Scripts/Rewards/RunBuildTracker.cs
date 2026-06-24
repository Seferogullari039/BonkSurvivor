using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RunBuildSlotEntry
{
    public int UpgradeIndex;
    public string DisplayName;
    public RewardCategory Category;
    public WeaponBuildType BuildType;
    public int Level;
}

public class RunBuildTracker : MonoBehaviour
{
    public const int MaxSlotsPerCategory = 3;

#if UNITY_EDITOR
    public static bool LogOverflowIgnored = false;
    public static bool LogMaxLevelIgnored = false;
    public static bool LogEvolutionUnlocks = true;
#endif

    public static RunBuildTracker Instance { get; private set; }

    public event Action OnBuildChanged;
    public event Action<BuildEvolutionId> OnEvolutionUnlocked;

    private readonly RunBuildSlotEntry[] skillSlots = new RunBuildSlotEntry[MaxSlotsPerCategory];
    private readonly RunBuildSlotEntry[] passiveSlots = new RunBuildSlotEntry[MaxSlotsPerCategory];
    private readonly HashSet<BuildEvolutionId> unlockedEvolutions = new HashSet<BuildEvolutionId>();

    public static RunBuildTracker GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RunBuildTracker existing = FindFirstObjectByType<RunBuildTracker>();

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject trackerObject = new GameObject("RunBuildTracker");
        return trackerObject.AddComponent<RunBuildTracker>();
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

    public void ClearRun()
    {
        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            skillSlots[i] = null;
            passiveSlots[i] = null;
        }

        unlockedEvolutions.Clear();
        OnBuildChanged?.Invoke();
    }

    public void RecordUpgrade(int upgradeIndex)
    {
        if (IsMaxed(upgradeIndex))
        {
#if UNITY_EDITOR
            if (LogMaxLevelIgnored)
            {
                Debug.Log("[RunBuildTracker] Max level ignored for upgrade index " + upgradeIndex + ".");
            }
#endif
            return;
        }

        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        WeaponBuildType buildType = UpgradeOptionCatalog.GetBuildType(upgradeIndex);
        RunBuildSlotEntry[] slots = category == RewardCategory.Skill ? skillSlots : passiveSlots;

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry existing = slots[i];

            if (existing != null && existing.UpgradeIndex == upgradeIndex)
            {
                if (existing.Level >= UpgradeOptionCatalog.GetMaxLevel(upgradeIndex))
                {
#if UNITY_EDITOR
                    if (LogMaxLevelIgnored)
                    {
                        Debug.Log("[RunBuildTracker] Max level ignored for upgrade index " + upgradeIndex + ".");
                    }
#endif
                    return;
                }

                existing.Level++;
                NotifyBuildChanged();
                return;
            }
        }

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            if (slots[i] != null)
            {
                continue;
            }

            slots[i] = new RunBuildSlotEntry
            {
                UpgradeIndex = upgradeIndex,
                DisplayName = UpgradeOptionCatalog.GetDisplayName(upgradeIndex),
                Category = category,
                BuildType = buildType,
                Level = 1
            };

            NotifyBuildChanged();
            return;
        }

#if UNITY_EDITOR
        if (LogOverflowIgnored)
        {
            Debug.Log("[RunBuildTracker] Overflow ignored for upgrade index " + upgradeIndex
                + " (" + category + "). All " + MaxSlotsPerCategory + " slots are full.");
        }
#endif
    }

    public RunBuildSlotEntry GetSkillSlot(int slotIndex)
    {
        return GetSlot(skillSlots, slotIndex);
    }

    public RunBuildSlotEntry GetPassiveSlot(int slotIndex)
    {
        return GetSlot(passiveSlots, slotIndex);
    }

    public bool HasEvolution(BuildEvolutionId id)
    {
        return id != BuildEvolutionId.None && unlockedEvolutions.Contains(id);
    }

    public IReadOnlyCollection<BuildEvolutionId> GetUnlockedEvolutions()
    {
        return unlockedEvolutions;
    }

    public bool HasFreeSlot(RewardCategory category)
    {
        return GetFilledSlotCount(category) < MaxSlotsPerCategory;
    }

    public bool IsTrackedUpgrade(int upgradeIndex)
    {
        return GetTrackedLevel(upgradeIndex) > 0;
    }

    public bool IsMaxed(int upgradeIndex)
    {
        int level = GetTrackedLevel(upgradeIndex);

        if (level <= 0)
        {
            return false;
        }

        return level >= UpgradeOptionCatalog.GetMaxLevel(upgradeIndex);
    }

    public bool CanLevelUpgrade(int upgradeIndex)
    {
        if (!IsTrackedUpgrade(upgradeIndex))
        {
            return true;
        }

        return !IsMaxed(upgradeIndex);
    }

    public bool CanOfferUpgrade(int upgradeIndex)
    {
        if (!UpgradeOptionCatalog.CanOfferInRewardPool(upgradeIndex))
        {
            return false;
        }

        if (IsMaxed(upgradeIndex))
        {
            return false;
        }

        if (IsTrackedUpgrade(upgradeIndex))
        {
            return true;
        }

        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        return HasFreeSlot(category);
    }

    public IReadOnlyList<int> GetTrackedUpgradeIndices(RewardCategory category)
    {
        List<int> indices = new List<int>(MaxSlotsPerCategory);
        RunBuildSlotEntry[] slots = GetSlotsForCategory(category);

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = slots[i];

            if (entry != null)
            {
                indices.Add(entry.UpgradeIndex);
            }
        }

        return indices;
    }

    public int GetTrackedLevel(int upgradeIndex)
    {
        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        RunBuildSlotEntry[] slots = GetSlotsForCategory(category);

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = slots[i];

            if (entry != null && entry.UpgradeIndex == upgradeIndex)
            {
                return entry.Level;
            }
        }

        return 0;
    }

    public int GetFilledSlotCount(RewardCategory category)
    {
        RunBuildSlotEntry[] slots = GetSlotsForCategory(category);
        int count = 0;

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            if (slots[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    public bool IsBuildFullyMaxed()
    {
        if (GetFilledSlotCount(RewardCategory.Skill) < MaxSlotsPerCategory)
        {
            return false;
        }

        if (GetFilledSlotCount(RewardCategory.Passive) < MaxSlotsPerCategory)
        {
            return false;
        }

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry skillEntry = skillSlots[i];

            if (skillEntry != null && skillEntry.Level < UpgradeOptionCatalog.GetMaxLevel(skillEntry.UpgradeIndex))
            {
                return false;
            }

            RunBuildSlotEntry passiveEntry = passiveSlots[i];

            if (passiveEntry != null && passiveEntry.Level < UpgradeOptionCatalog.GetMaxLevel(passiveEntry.UpgradeIndex))
            {
                return false;
            }
        }

        return true;
    }

    private void NotifyBuildChanged()
    {
        OnBuildChanged?.Invoke();
        CheckEvolutionRequirements();
    }

    private void CheckEvolutionRequirements()
    {
        var requirements = UpgradeOptionCatalog.GetEvolutionRequirements();

        for (int i = 0; i < requirements.Count; i++)
        {
            var requirement = requirements[i];

            if (requirement.EvolutionId == BuildEvolutionId.None || HasEvolution(requirement.EvolutionId))
            {
                continue;
            }

            if (GetTrackedLevel(requirement.SkillUpgradeIndex) < requirement.RequiredSkillLevel)
            {
                continue;
            }

            if (GetTrackedLevel(requirement.PassiveUpgradeIndex) < requirement.RequiredPassiveLevel)
            {
                continue;
            }

            unlockedEvolutions.Add(requirement.EvolutionId);
            OnEvolutionUnlocked?.Invoke(requirement.EvolutionId);

#if UNITY_EDITOR
            if (LogEvolutionUnlocks)
            {
                Debug.Log("EVOLUTION UNLOCKED: " + requirement.DisplayName);
            }
#endif
        }
    }

    private RunBuildSlotEntry[] GetSlotsForCategory(RewardCategory category)
    {
        return category == RewardCategory.Skill ? skillSlots : passiveSlots;
    }

    private static RunBuildSlotEntry GetSlot(RunBuildSlotEntry[] slots, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlotsPerCategory)
        {
            return null;
        }

        return slots[slotIndex];
    }
}
