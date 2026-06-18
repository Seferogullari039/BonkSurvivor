using System.Collections;
using UnityEngine;

public static class ChestOpenPresentation
{
    public static IEnumerator PlayRevealThenOpenUpgradeMenu(Vector3 position, ChestRarity rarity)
    {
        yield return ChestOpenVisualEffect.PlayRoutine(position, rarity);

        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.OpenChestUpgradeMenu(rarity);
        }
    }
}
