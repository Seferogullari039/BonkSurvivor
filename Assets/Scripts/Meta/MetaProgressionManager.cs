using UnityEngine;

public class MetaProgressionManager : MonoBehaviour
{
    private const string TotalCoinsKey = "totalCoins";
    private const string MaxHealthLevelKey = "metaUpgrade_MaxHealth";
    private const string DamageLevelKey = "metaUpgrade_Damage";
    private const string MoveSpeedLevelKey = "metaUpgrade_MoveSpeed";
    private const string PickupRangeLevelKey = "metaUpgrade_PickupRange";
    private const int MaxUpgradeLevel = 10;

    public static MetaProgressionManager Instance { get; private set; }

    public int TotalCoins { get; private set; }

    public static MetaProgressionManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        MetaProgressionManager existing = FindFirstObjectByType<MetaProgressionManager>();

        if (existing != null)
        {
            Instance = existing;
            existing.Load();
            return existing;
        }

        GameObject managerObject = new GameObject("MetaProgressionManager");
        DontDestroyOnLoad(managerObject);
        return managerObject.AddComponent<MetaProgressionManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void Load()
    {
        TotalCoins = Mathf.Max(0, PlayerPrefs.GetInt(TotalCoinsKey, 0));
    }

    public void Save()
    {
        PlayerPrefs.SetInt(TotalCoinsKey, Mathf.Max(0, TotalCoins));
        PlayerPrefs.Save();
    }

    public void AddMetaCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        TotalCoins += amount;
        Save();
    }

    public int GetMaxUpgradeLevel()
    {
        return MaxUpgradeLevel;
    }

    public int GetUpgradeLevel(MetaUpgradeType type)
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(GetUpgradeLevelKey(type), 0));
    }

    public int GetUpgradeCost(MetaUpgradeType type)
    {
        int level = GetUpgradeLevel(type);

        if (level >= MaxUpgradeLevel)
        {
            return -1;
        }

        return type switch
        {
            MetaUpgradeType.MaxHealth => 50 + level * 35,
            MetaUpgradeType.Damage => 60 + level * 40,
            MetaUpgradeType.MoveSpeed => 50 + level * 35,
            MetaUpgradeType.PickupRange => 45 + level * 30,
            _ => -1
        };
    }

    public bool CanBuyUpgrade(MetaUpgradeType type)
    {
        int cost = GetUpgradeCost(type);
        return cost >= 0 && TotalCoins >= cost;
    }

    public bool TryBuyUpgrade(MetaUpgradeType type)
    {
        if (!CanBuyUpgrade(type))
        {
            return false;
        }

        int cost = GetUpgradeCost(type);
        TotalCoins -= cost;
        PlayerPrefs.SetInt(GetUpgradeLevelKey(type), GetUpgradeLevel(type) + 1);
        Save();
        return true;
    }

    public float GetMaxHealthBonusPercent()
    {
        return GetUpgradeLevel(MetaUpgradeType.MaxHealth) * 0.03f;
    }

    public float GetDamageBonusPercent()
    {
        return GetUpgradeLevel(MetaUpgradeType.Damage) * 0.02f;
    }

    public float GetMoveSpeedBonusPercent()
    {
        return GetUpgradeLevel(MetaUpgradeType.MoveSpeed) * 0.015f;
    }

    public float GetPickupRangeBonusPercent()
    {
        return GetUpgradeLevel(MetaUpgradeType.PickupRange) * 0.04f;
    }

    public static string GetUpgradeDisplayName(MetaUpgradeType type)
    {
        return type switch
        {
            MetaUpgradeType.MaxHealth => "Vital Training",
            MetaUpgradeType.Damage => "Sharp Training",
            MetaUpgradeType.MoveSpeed => "Swift Training",
            MetaUpgradeType.PickupRange => "Magnet Training",
            _ => type.ToString()
        };
    }

    public string GetUpgradeBonusSummary(MetaUpgradeType type)
    {
        int level = GetUpgradeLevel(type);

        return type switch
        {
            MetaUpgradeType.MaxHealth => "Max HP +" + (level * 3) + "%",
            MetaUpgradeType.Damage => "Damage +" + (level * 2) + "%",
            MetaUpgradeType.MoveSpeed => "Move Speed +" + FormatPercent(level * 1.5f) + "%",
            MetaUpgradeType.PickupRange => "Pickup Range +" + (level * 4) + "%",
            _ => string.Empty
        };
    }

    private static string GetUpgradeLevelKey(MetaUpgradeType type)
    {
        return type switch
        {
            MetaUpgradeType.MaxHealth => MaxHealthLevelKey,
            MetaUpgradeType.Damage => DamageLevelKey,
            MetaUpgradeType.MoveSpeed => MoveSpeedLevelKey,
            MetaUpgradeType.PickupRange => PickupRangeLevelKey,
            _ => string.Empty
        };
    }

    private static string FormatPercent(float value)
    {
        if (Mathf.Approximately(value % 1f, 0f))
        {
            return ((int)value).ToString();
        }

        return value.ToString("0.#");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug Reset Meta Progression")]
    private void DebugResetMetaProgression()
    {
        TotalCoins = 0;
        PlayerPrefs.DeleteKey(MaxHealthLevelKey);
        PlayerPrefs.DeleteKey(DamageLevelKey);
        PlayerPrefs.DeleteKey(MoveSpeedLevelKey);
        PlayerPrefs.DeleteKey(PickupRangeLevelKey);
        Save();
    }
#endif
}
