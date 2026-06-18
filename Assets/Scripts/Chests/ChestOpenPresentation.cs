using System.Collections;
using UnityEngine;

public static class ChestOpenPresentation
{
    public static IEnumerator PlayRevealThenOpenUpgradeMenu(Vector3 position, ChestRarity chestRarity, Transform chestTransform = null)
    {
        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager == null)
        {
            ChestRevealPause.End();
            yield break;
        }

        yield return ChestOpeningPresentation.PlayPhysicalOpening(chestTransform);

        UpgradeRarity rewardRarity = levelUpManager.PrepareChestSingleReward(chestRarity);
        yield return ChestOpenVisualEffect.PlayRoutineForUpgradeReward(position, rewardRarity, chestTransform);
        levelUpManager.PresentChestSingleCardReveal();
    }
}
