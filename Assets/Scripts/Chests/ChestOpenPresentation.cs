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

        DisableChestIdleFeedback(chestTransform);
        AudioManager.Instance?.PlayChestOpen();

        yield return ChestOpeningPresentation.PlayAnticipationShake(chestTransform);

        BeginOpeningGlow(chestTransform);
        yield return ChestOpeningPresentation.PlayPhysicalOpening(chestTransform);

        UpgradeRarity rewardRarity = levelUpManager.PrepareChestSingleReward(chestRarity);

        yield return ChestOpenVisualEffect.PlayRoutineForUpgradeReward(position, rewardRarity, chestTransform);

        EndOpeningGlow(chestTransform);
        levelUpManager.PresentChestSingleCardReveal();
    }

    private static void DisableChestIdleFeedback(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return;
        }

        ChestVisual visual = chestTransform.GetComponent<ChestVisual>();

        if (visual != null)
        {
            visual.SetProximityHighlight(false);
        }

        ChestVisualAnimator animator = chestTransform.GetComponent<ChestVisualAnimator>();

        if (animator != null)
        {
            animator.SetIdleEnabled(false);
        }
    }

    private static void BeginOpeningGlow(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return;
        }

        ChestVisual visual = chestTransform.GetComponent<ChestVisual>();
        visual?.SetOpeningHighlight(true);
    }

    private static void EndOpeningGlow(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return;
        }

        ChestVisual visual = chestTransform.GetComponent<ChestVisual>();
        visual?.SetOpeningHighlight(false);
    }
}
