using System.Collections.Generic;
using UnityEngine;

public static class ChestSpecialRewardRoller
{
    private const float NormalChestSpecialChance = 0.12f;
    private const float RareChestSpecialChance = 0.18f;
    private const float EpicChestSpecialChance = 0.25f;
    private const float LuckSpecialChancePerLevel = 0.005f;
    private const float MaxLuckSpecialChanceBonus = 0.05f;

    private const int LegendaryPickWeight = 1;
    private const int EpicPickWeight = 3;
    private const int RarePickWeight = 5;

    public static bool TryRollSpecialUpgrade(
        ChestRarity chestRarity,
        out int upgradeIndex,
        out UpgradeRarity rarity)
    {
        upgradeIndex = -1;
        rarity = UpgradeRarity.Common;

        float chance = GetSpecialItemChance(chestRarity);

        if (Random.value >= chance)
        {
            return false;
        }

        List<int> pool = RunBuildRewardFilter.BuildChestSpecialPoolIndices();
        List<int> eligible = RunBuildRewardFilter.FilterEligibleCandidates(pool);

        if (eligible.Count == 0)
        {
            return false;
        }

        upgradeIndex = PickWeightedSpecialUpgrade(eligible);

        if (upgradeIndex < 0)
        {
            return false;
        }

        rarity = UpgradeOptionCatalog.GetAssignedRarity(upgradeIndex);
        rarity = ChestEconomyModifiers.ApplyLuckToChestStatRarity(rarity);
        return true;
    }

    private static float GetSpecialItemChance(ChestRarity chestRarity)
    {
        float chance = chestRarity switch
        {
            ChestRarity.Epic => EpicChestSpecialChance,
            ChestRarity.Rare => RareChestSpecialChance,
            _ => NormalChestSpecialChance
        };

        int luckLevel = ChestEconomyModifiers.GetLuckLevel();

        if (luckLevel > 0)
        {
            chance += Mathf.Min(luckLevel * LuckSpecialChancePerLevel, MaxLuckSpecialChanceBonus);
        }

        return Mathf.Clamp01(chance);
    }

    private static int PickWeightedSpecialUpgrade(List<int> eligible)
    {
        int totalWeight = 0;
        int[] weights = new int[eligible.Count];

        for (int i = 0; i < eligible.Count; i++)
        {
            weights[i] = GetSpecialPickWeight(eligible[i]);
            totalWeight += weights[i];
        }

        if (totalWeight <= 0)
        {
            return eligible[Random.Range(0, eligible.Count)];
        }

        int roll = Random.Range(0, totalWeight);

        for (int i = 0; i < eligible.Count; i++)
        {
            roll -= weights[i];

            if (roll < 0)
            {
                return eligible[i];
            }
        }

        return eligible[eligible.Count - 1];
    }

    private static int GetSpecialPickWeight(int upgradeIndex)
    {
        UpgradeRarity assigned = UpgradeOptionCatalog.GetAssignedRarity(upgradeIndex);

        return assigned switch
        {
            UpgradeRarity.Legendary => LegendaryPickWeight,
            UpgradeRarity.Epic => EpicPickWeight,
            UpgradeRarity.Rare => RarePickWeight,
            _ => RarePickWeight
        };
    }
}
