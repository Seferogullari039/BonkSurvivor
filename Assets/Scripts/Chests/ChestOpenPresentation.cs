using System.Collections;
using UnityEngine;

public static class ChestOpenPresentation
{
    public static IEnumerator PlayRevealThenOpenUpgradeMenu(Vector3 position, ChestRarity chestRarity)
    {
        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager == null)
        {
            yield break;
        }

        UpgradeRarity rewardRarity = levelUpManager.PrepareChestSingleReward(chestRarity);
        yield return ChestOpenVisualEffect.PlayRoutineForUpgradeReward(position, rewardRarity);
        levelUpManager.PresentChestSingleCardReveal();
    }
}
