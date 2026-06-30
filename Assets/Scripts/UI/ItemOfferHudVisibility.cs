public static class ItemOfferHudVisibility
{
    private static bool isSuppressed;

    public static bool IsGameplaySuppressed => isSuppressed;

    public static void ResetStateForNewRun()
    {
        isSuppressed = false;

        if (!ShouldRestoreGameplayHud())
        {
            return;
        }

        HUDManager.ShowGameplayHud();
        RunBuildHud.EnsureVisibleForRun();
        ActiveWeaponHud.ShowHud();
        ChestStatBuffHud.OnGameplayRunStarted();
    }

    public static void HideForItemOffer()
    {
        if (isSuppressed)
        {
            return;
        }

        isSuppressed = true;
        RunBuildHud.HideHud();
        ActiveWeaponHud.HideHud();
        ChestStatBuffHud.HideHud();
        HUDManager.HideLevelUpFeedbackImmediate();
        HUDManager.HideGameplayHud();
    }

    public static void RestoreAfterItemOffer()
    {
        if (!isSuppressed)
        {
            return;
        }

        isSuppressed = false;

        if (!ShouldRestoreGameplayHud())
        {
            return;
        }

        ResetStateForNewRun();
    }

    private static bool ShouldRestoreGameplayHud()
    {
        return MainMenuManager.Instance == null || MainMenuManager.IsRunActive;
    }
}
