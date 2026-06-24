using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public float ProjectileSpeedMultiplier { get; private set; } = 1f;
    public float XPAttractionMultiplier { get; private set; } = 1f;
    public float PickupRangeMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        Instance = this;
    }

    public static UpgradeManager GetOrCreateInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = new GameObject("UpgradeManager");
        return managerObject.AddComponent<UpgradeManager>();
    }

    public void IncreaseProjectileSpeed(float percent)
    {
        ProjectileSpeedMultiplier *= 1f + percent;
    }

    public void IncreaseXPAttraction(float percent)
    {
        XPAttractionMultiplier *= 1f + percent;
    }

    public void IncreasePickupRange(float percent)
    {
        PickupRangeMultiplier *= 1f + percent;
    }

    public void ApplyMetaPickupRangeBonus(float bonusPercent)
    {
        PickupRangeMultiplier = 1f + Mathf.Max(0f, bonusPercent);
    }

    public void ResetForNewRun()
    {
        ProjectileSpeedMultiplier = 1f;
        XPAttractionMultiplier = 1f;
        PickupRangeMultiplier = 1f;
    }
}
