using System;
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
#endif

    public static RunBuildTracker Instance { get; private set; }

    public event Action OnBuildChanged;

    private readonly RunBuildSlotEntry[] skillSlots = new RunBuildSlotEntry[MaxSlotsPerCategory];
    private readonly RunBuildSlotEntry[] passiveSlots = new RunBuildSlotEntry[MaxSlotsPerCategory];

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

        OnBuildChanged?.Invoke();
    }

    public void RecordUpgrade(int upgradeIndex)
    {
        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        WeaponBuildType buildType = UpgradeOptionCatalog.GetBuildType(upgradeIndex);
        RunBuildSlotEntry[] slots = category == RewardCategory.Skill ? skillSlots : passiveSlots;

        for (int i = 0; i < MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry existing = slots[i];

            if (existing != null && existing.UpgradeIndex == upgradeIndex)
            {
                existing.Level++;
                OnBuildChanged?.Invoke();
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

            OnBuildChanged?.Invoke();
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

    private static RunBuildSlotEntry GetSlot(RunBuildSlotEntry[] slots, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlotsPerCategory)
        {
            return null;
        }

        return slots[slotIndex];
    }
}
