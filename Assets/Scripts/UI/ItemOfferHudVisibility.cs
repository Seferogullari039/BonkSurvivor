public static class ItemOfferHudVisibility
{
    private static bool isSuppressed;

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

        HUDManager.ShowGameplayHud();
        RunBuildHud.ShowHud();
        ActiveWeaponHud.ShowHud();
        ChestStatBuffHud.OnGameplayRunStarted();
    }

    private static bool ShouldRestoreGameplayHud()
    {
        return MainMenuManager.Instance == null || MainMenuManager.IsRunActive;
    }
}
