using System.Collections;
using UnityEngine;

public static class ChestOpenPresentation
{
    public static IEnumerator PlayRevealThenOpenUpgradeMenu(Vector3 position, ChestRarity chestRarity, Transform chestTransform = null)
    {
        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager == null)
        {
            ChestRevealPause.ForceEnd();
            yield break;
        }

        levelUpManager.PrepareChestSingleReward(chestRarity);
        levelUpManager.PresentChestSingleCardReveal();
    }
}
