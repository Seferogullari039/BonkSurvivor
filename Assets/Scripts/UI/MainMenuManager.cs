using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }
    public static bool IsRunActive { get; private set; }
    public static bool RequestPlayAfterSceneLoad { get; set; }

    private GameObject mainMenuPanel;
    private GameObject upgradesPanel;
    private TMP_Text mainMenuCoinsText;
    private TMP_Text upgradesCoinsText;
    private TMP_Text[] upgradeDescriptionTexts = new TMP_Text[4];
    private Button[] buyButtons = new Button[4];
    private bool gameStarted;
    private Coroutine forceGameplayHudRoutine;

    private void Awake()
    {
        Instance = this;

        if (GetComponent<MainMenuCameraController>() == null)
        {
            gameObject.AddComponent<MainMenuCameraController>();
        }

        BuildMenuUI();
        MetaProgressionManager.GetOrCreate();
    }

    private void Start()
    {
        IsRunActive = false;
        Time.timeScale = 0f;
        gameStarted = false;
        ItemOfferHudVisibility.ResetStateForNewRun();
        CloseUpgrades();
        ShowMainMenuPanel();
        HUDManager.HideGameplayHud();
        RunBuildHud.HideHud();
        ChestStatBuffHud.HideHud();
        ActiveWeaponHud.HideHud();

        ApplyMenuPresentationState();
        HideMenuWeaponVisuals();
        PauseMenuManager.HidePauseMenuIfExists();

        if (RequestPlayAfterSceneLoad)
        {
            RequestPlayAfterSceneLoad = false;
            StartCoroutine(AutoStartRunAfterSceneLoadRoutine());
        }
    }

    private IEnumerator AutoStartRunAfterSceneLoadRoutine()
    {
        yield return null;
        PlayGame();
    }

    public void PlayGame()
    {
        ProceduralGrassArena proceduralArena = FindFirstObjectByType<ProceduralGrassArena>();

        if (proceduralArena != null)
        {
            proceduralArena.RegenerateArenaForNewRun();
        }

        ResetGameplayWorld();
        ApplyMetaBonusesToPlayer();

        RunStatsTracker.GetOrCreate().StartRun();

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (proceduralArena != null)
        {
            proceduralArena.EnsureGenerated();
            proceduralArena.MovePlayerToSelectedSpawn(player);
        }

        ChestSpawner chestSpawner = FindFirstObjectByType<ChestSpawner>();

        if (chestSpawner != null && proceduralArena != null)
        {
            chestSpawner.SpawnSeededMapChestsForRun(proceduralArena.CurrentRunSeed);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (upgradesPanel != null)
        {
            upgradesPanel.SetActive(false);
        }

        gameStarted = true;
        IsRunActive = true;
        Time.timeScale = 1f;
        ItemOfferHudVisibility.ResetStateForNewRun();
        PauseMenuManager.HidePauseMenuIfExists();
        SettingsMenuUI.CloseIfOpen();
        BigMapOverlay.EnsureReadyForRun();

        ApplyGameplayPresentationState();

        FPSPlayerController fpsPlayerController = FindFirstObjectByType<FPSPlayerController>();
        fpsPlayerController?.ForceGameplayCameraReady();

        if (forceGameplayHudRoutine != null)
        {
            StopCoroutine(forceGameplayHudRoutine);
        }

        forceGameplayHudRoutine = StartCoroutine(RestoreGameplayHudAfterRunRoutine());
    }

    private IEnumerator RestoreGameplayHudAfterRunRoutine()
    {
        yield return null;
        RestoreGameplayHudFrame();
        yield return null;
        RestoreGameplayHudFrame();
        yield return null;
        RestoreGameplayHudFrame();
        yield return new WaitForEndOfFrame();
        RestoreGameplayHudFrame();

        forceGameplayHudRoutine = null;
    }

    private static void RestoreGameplayHudFrame()
    {
        BigMapOverlay.EnsureReadyForRun();

        HUDManager hud = FindFirstObjectByType<HUDManager>();

        if (hud != null)
        {
            hud.EnsureHUDVisible();
            HUDManager.ShowGameplayHud();
            hud.ForceHudElementsVisibleForRecovery();
        }
        else
        {
            HUDManager.ShowGameplayHud();
        }

        RunBuildHud.EnsureVisibleForRun();
        ChestStatBuffHud.OnGameplayRunStarted();
        ActiveWeaponHud.EnsureVisibleForRun();
    }

    private static void HideLevelUpPanelForGameplay()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        Transform levelUpPanel = canvas.transform.Find("LevelUpPanel");

        if (levelUpPanel != null)
        {
            levelUpPanel.gameObject.SetActive(false);
        }
    }

    public void ReturnToMainMenu()
    {
        gameStarted = false;
        IsRunActive = false;
        Time.timeScale = 0f;
        ItemOfferHudVisibility.ResetStateForNewRun();

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.HideGameOver();
        }

        ResetGameplayWorld();
        CloseUpgrades();
        ShowMainMenuPanel();
        HUDManager.HideGameplayHud();
        PauseMenuManager.HidePauseMenuIfExists();
        SettingsMenuUI.CloseIfOpen();

        ApplyMenuPresentationState();
        HideMenuWeaponVisuals();
    }

    public void OpenSettings()
    {
        SettingsMenuUI.OpenFromMainMenu();
    }

    public void OpenUpgrades()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (upgradesPanel != null)
        {
            upgradesPanel.SetActive(true);
        }

        RefreshUpgradePanel();
    }

    public void CloseUpgrades()
    {
        if (upgradesPanel != null)
        {
            upgradesPanel.SetActive(false);
        }

        if (!gameStarted && mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            RefreshMainMenuCoinsText();
        }
    }

    private void ApplyMenuPresentationState()
    {
        MainMenuCameraController menuCameraController = FindMenuCameraController();

        if (menuCameraController != null)
        {
            menuCameraController.ShowMenuCamera();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ApplyGameplayPresentationState()
    {
        MainMenuCameraController menuCameraController = FindMenuCameraController();

        if (menuCameraController != null)
        {
            menuCameraController.ShowGameplayCamera();
        }
    }

    private static void HideMenuWeaponVisuals()
    {
        FPSViewModel.Instance?.HideViewModelForMenu();

        StarterWeaponViewModel[] weaponViewModels = FindObjectsByType<StarterWeaponViewModel>(FindObjectsSortMode.None);

        for (int i = 0; i < weaponViewModels.Length; i++)
        {
            if (weaponViewModels[i] != null)
            {
                weaponViewModels[i].HideWeaponVisualForMenu();
            }
        }
    }

    private static MainMenuCameraController FindMenuCameraController()
    {
        if (MainMenuCameraController.Instance != null)
        {
            return MainMenuCameraController.Instance;
        }

        return FindFirstObjectByType<MainMenuCameraController>();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BuildMenuUI()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            Debug.LogError("MainMenuManager: Canvas bulunamadi.");
            return;
        }

        Transform existingMainMenu = canvas.transform.Find("MainMenuPanel");
        Transform existingUpgrades = canvas.transform.Find("UpgradesPanel");

        if (existingMainMenu != null)
        {
            mainMenuPanel = existingMainMenu.gameObject;
        }

        if (existingUpgrades != null)
        {
            upgradesPanel = existingUpgrades.gameObject;
        }

        if (mainMenuPanel == null)
        {
            mainMenuPanel = CreatePanel(canvas.transform, "MainMenuPanel");
            CreateText(mainMenuPanel.transform, "TitleText", "BONKSURVIVOR", 48, new Vector2(0f, 180f));
            mainMenuCoinsText = CreateText(mainMenuPanel.transform, "TotalCoinsText", "", 28, new Vector2(0f, 110f));
            CreateMenuButton(mainMenuPanel.transform, "PlayButton", "Play", new Vector2(0f, 20f), PlayGame);
            CreateMenuButton(mainMenuPanel.transform, "SettingsButton", "Settings", new Vector2(0f, -40f), OpenSettings);
            CreateMenuButton(mainMenuPanel.transform, "UpgradesButton", "Upgrades", new Vector2(0f, -100f), OpenUpgrades);
            CreateMenuButton(mainMenuPanel.transform, "QuitButton", "Quit", new Vector2(0f, -180f), QuitGame);
        }
        else
        {
            mainMenuCoinsText = mainMenuPanel.transform.Find("TotalCoinsText")?.GetComponent<TMP_Text>();
            WireExistingButton(mainMenuPanel.transform, "PlayButton", PlayGame);
            EnsureSettingsButton(mainMenuPanel.transform);
            WireExistingButton(mainMenuPanel.transform, "SettingsButton", OpenSettings);
            WireExistingButton(mainMenuPanel.transform, "UpgradesButton", OpenUpgrades);
            WireExistingButton(mainMenuPanel.transform, "QuitButton", QuitGame);
        }

        if (upgradesPanel == null)
        {
            upgradesPanel = CreatePanel(canvas.transform, "UpgradesPanel");
            CreateText(upgradesPanel.transform, "UpgradeTitleText", "UPGRADES", 40, new Vector2(0f, 220f));
            upgradesCoinsText = CreateText(upgradesPanel.transform, "TotalCoinsText", "", 26, new Vector2(0f, 160f));

            string[] upgradeNames =
            {
                "Vital Training",
                "Sharp Training",
                "Swift Training",
                "Magnet Training"
            };

            for (int i = 0; i < upgradeNames.Length; i++)
            {
                float y = 80f - i * 80f;
                int index = i;

                upgradeDescriptionTexts[i] = CreateText(
                    upgradesPanel.transform,
                    "UpgradeText" + i,
                    upgradeNames[i],
                    20,
                    new Vector2(-40f, y),
                    new Vector2(520f, 72f),
                    TextAlignmentOptions.MidlineLeft
                );

                buyButtons[i] = CreateMenuButton(
                    upgradesPanel.transform,
                    "BuyButton" + i,
                    "BUY",
                    new Vector2(300f, y),
                    () => OnBuyUpgradeClicked(index)
                ).GetComponent<Button>();
            }

            CreateMenuButton(upgradesPanel.transform, "BackButton", "Back", new Vector2(0f, -220f), CloseUpgrades);
        }
        else
        {
            upgradesCoinsText = upgradesPanel.transform.Find("TotalCoinsText")?.GetComponent<TMP_Text>();
            WireExistingButton(upgradesPanel.transform, "BackButton", CloseUpgrades);

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                upgradeDescriptionTexts[i] = upgradesPanel.transform.Find("UpgradeText" + i)?.GetComponent<TMP_Text>();
                WireExistingButton(upgradesPanel.transform, "BuyButton" + i, () => OnBuyUpgradeClicked(index));

                Transform buyTransform = upgradesPanel.transform.Find("BuyButton" + i);

                if (buyTransform != null)
                {
                    buyButtons[i] = buyTransform.GetComponent<Button>();
                }
            }
        }

        mainMenuPanel.transform.SetAsLastSibling();
        upgradesPanel.transform.SetAsLastSibling();
        upgradesPanel.SetActive(false);
        ApplyMenuLayout();
    }

    private void ApplyMenuLayout()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        if (mainMenuPanel != null)
        {
            LayoutMainMenuContent(mainMenuPanel.transform);
        }

        if (upgradesPanel != null)
        {
            LayoutUpgradesContent(upgradesPanel.transform);
        }
    }

    private static void LayoutMainMenuContent(Transform panel)
    {
        LayoutCenteredText(panel, "TitleText", new Vector2(0f, 200f), new Vector2(900f, 80f), 52f);
        LayoutCenteredText(panel, "TotalCoinsText", new Vector2(0f, 120f), new Vector2(700f, 50f), 30f);
        LayoutCenteredButton(panel, "PlayButton", new Vector2(0f, 30f));
        LayoutCenteredButton(panel, "SettingsButton", new Vector2(0f, -30f));
        LayoutCenteredButton(panel, "UpgradesButton", new Vector2(0f, -90f));
        LayoutCenteredButton(panel, "QuitButton", new Vector2(0f, -170f));
    }

    private static void LayoutUpgradesContent(Transform panel)
    {
        LayoutCenteredText(panel, "UpgradeTitleText", new Vector2(0f, 220f), new Vector2(760f, 70f), 40f);
        LayoutCenteredText(panel, "TotalCoinsText", new Vector2(0f, 160f), new Vector2(700f, 44f), 28f);
        LayoutCenteredButton(panel, "BackButton", new Vector2(0f, -220f));

        for (int i = 0; i < 4; i++)
        {
            float y = 80f - i * 80f;
            LayoutCenteredText(panel, "UpgradeText" + i, new Vector2(-40f, y), new Vector2(520f, 72f), 20f);
            LayoutCenteredButton(panel, "BuyButton" + i, new Vector2(300f, y), new Vector2(120f, 46f));
        }
    }

    private static void LayoutCenteredText(
        Transform panel,
        string childName,
        Vector2 position,
        Vector2 size,
        float fontSize)
    {
        Transform child = panel.Find(childName);

        if (child == null) return;

        RectTransform rectTransform = child.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, position, size);

        TMP_Text text = child.GetComponent<TMP_Text>();

        if (text != null)
        {
            text.fontSize = fontSize;
        }
    }

    private static void LayoutCenteredButton(Transform panel, string childName, Vector2 position, Vector2? size = null)
    {
        Transform child = panel.Find(childName);

        if (child == null) return;

        RectTransform rectTransform = child.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, position, size ?? new Vector2(290f, 50f));
    }

    private void ShowMainMenuPanel()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
            mainMenuPanel.transform.SetAsLastSibling();
        }

        RefreshMainMenuCoinsText();
        RunBuildHud.HideHud();
        ChestStatBuffHud.HideHud();
        ActiveWeaponHud.HideHud();
    }

    private void EnsureSettingsButton(Transform parent)
    {
        if (parent.Find("SettingsButton") != null)
        {
            return;
        }

        CreateMenuButton(parent, "SettingsButton", "Settings", new Vector2(0f, -40f), OpenSettings);
    }

    private void WireExistingButton(Transform parent, string buttonName, UnityEngine.Events.UnityAction onClick)
    {
        Transform buttonTransform = parent.Find(buttonName);

        if (buttonTransform == null) return;

        Button button = buttonTransform.GetComponent<Button>();

        if (button == null) return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClick();
            onClick.Invoke();
        });
    }

    private GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);
        return panelObject;
    }

    private TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2? size = null,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size ?? new Vector2(700f, 60f);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.alignment = alignment;
        textMesh.color = Color.white;

        return textMesh;
    }

    private GameObject CreateMenuButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(290f, 50f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClick();
            onClick.Invoke();
        });

        CreateText(buttonObject.transform, "Label", label, 24, Vector2.zero, new Vector2(290f, 50f));

        return buttonObject;
    }

    private void RefreshMainMenuCoinsText()
    {
        MetaProgressionManager manager = MetaProgressionManager.GetOrCreate();

        if (mainMenuCoinsText != null)
        {
            mainMenuCoinsText.text = "Total Coins: " + manager.TotalCoins;
        }
    }

    private void RefreshUpgradePanel()
    {
        MetaProgressionManager manager = MetaProgressionManager.GetOrCreate();

        if (upgradesCoinsText != null)
        {
            upgradesCoinsText.text = "Total Coins: " + manager.TotalCoins;
        }

        Transform titleTransform = upgradesPanel != null ? upgradesPanel.transform.Find("UpgradeTitleText") : null;

        if (titleTransform != null)
        {
            TMP_Text titleText = titleTransform.GetComponent<TMP_Text>();

            if (titleText != null)
            {
                titleText.text = "UPGRADES";
            }
        }

        SetUpgradeRow(0, MetaUpgradeType.MaxHealth, manager);
        SetUpgradeRow(1, MetaUpgradeType.Damage, manager);
        SetUpgradeRow(2, MetaUpgradeType.MoveSpeed, manager);
        SetUpgradeRow(3, MetaUpgradeType.PickupRange, manager);
    }

    private void SetUpgradeRow(int index, MetaUpgradeType type, MetaProgressionManager manager)
    {
        if (upgradeDescriptionTexts[index] == null)
        {
            return;
        }

        int level = manager.GetUpgradeLevel(type);
        int maxLevel = manager.GetMaxUpgradeLevel();
        int nextCost = manager.GetUpgradeCost(type);
        string displayName = MetaProgressionManager.GetUpgradeDisplayName(type);
        string bonusSummary = manager.GetUpgradeBonusSummary(type);

        if (level >= maxLevel)
        {
            upgradeDescriptionTexts[index].text = displayName + " Lv." + level + "/" + maxLevel + "\n"
                + bonusSummary + "\nMAX";
        }
        else
        {
            upgradeDescriptionTexts[index].text = displayName + " Lv." + level + "/" + maxLevel + "\n"
                + bonusSummary + "\nCost: " + nextCost;
        }

        if (buyButtons[index] != null)
        {
            bool canBuy = manager.CanBuyUpgrade(type);
            buyButtons[index].interactable = canBuy;

            TMP_Text buttonLabel = buyButtons[index].transform.Find("Label")?.GetComponent<TMP_Text>();

            if (buttonLabel != null)
            {
                buttonLabel.text = level >= maxLevel ? "MAX" : "BUY";
            }
        }
    }

    private void OnBuyUpgradeClicked(int index)
    {
        MetaUpgradeType type = index switch
        {
            0 => MetaUpgradeType.MaxHealth,
            1 => MetaUpgradeType.Damage,
            2 => MetaUpgradeType.MoveSpeed,
            3 => MetaUpgradeType.PickupRange,
            _ => MetaUpgradeType.MaxHealth
        };

        if (!MetaProgressionManager.GetOrCreate().TryBuyUpgrade(type))
        {
            return;
        }

        RefreshUpgradePanel();
        RefreshMainMenuCoinsText();
    }

    private void ApplyMetaBonusesToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.ApplyMetaProgressionBonuses();
        }
    }

    private void ResetGameplayWorld()
    {
        ClearRunEntities();
        ResetPlayerRun();

        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (enemySpawner != null)
        {
            enemySpawner.ResetRun();
        }

        ChestSpawner chestSpawner = FindFirstObjectByType<ChestSpawner>();

        if (chestSpawner != null)
        {
            chestSpawner.ResetRun();
        }

        RunBuildTracker.GetOrCreate().ClearRun();
        ChestStatBuffTracker.GetOrCreate().ClearRun();
        RewardOfferActionState.ResetForNewRun();
        LegendaryPassiveEffectManager.ResetRun();
        RunStatsTracker.GetOrCreate().ClearRun();
        MerchantShrineUI.ForceClose();

        MerchantShrineManager merchantManager = FindFirstObjectByType<MerchantShrineManager>();

        if (merchantManager != null)
        {
            merchantManager.ResetRunState();
        }
    }

    private void ResetPlayerRun()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        PlayerController playerController = player.GetComponent<PlayerController>();

        if (playerController != null)
        {
            playerController.ResetMoveSpeed();
        }

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.ResetRunState();
        }

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();

        if (weaponManager != null)
        {
            weaponManager.ResetRunWeapons();
        }
    }

    private static void ClearRunEntities()
    {
        DestroyObjectsWithTag("Enemy");

        Chest[] chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);

        for (int i = 0; i < chests.Length; i++)
        {
            if (chests[i] != null)
            {
                Destroy(chests[i].gameObject);
            }
        }

        Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);

        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] != null)
            {
                Destroy(projectiles[i].gameObject);
            }
        }
    }

    private static void DestroyObjectsWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                Destroy(objects[i]);
            }
        }
    }
}
