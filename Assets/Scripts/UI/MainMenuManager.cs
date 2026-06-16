using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }
    public static bool IsRunActive { get; private set; }

    private GameObject mainMenuPanel;
    private GameObject upgradesPanel;
    private TMP_Text mainMenuCoinsText;
    private TMP_Text upgradesCoinsText;
    private TMP_Text[] upgradeDescriptionTexts = new TMP_Text[4];
    private Button[] buyButtons = new Button[4];
    private bool gameStarted;

    private void Awake()
    {
        Instance = this;

        if (GetComponent<MainMenuCameraController>() == null)
        {
            gameObject.AddComponent<MainMenuCameraController>();
        }

        BuildMenuUI();
    }

    private void Start()
    {
        IsRunActive = false;
        Time.timeScale = 0f;
        gameStarted = false;
        CloseUpgrades();
        ShowMainMenuPanel();

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SetGameplayHUDVisible(false);
        }

        ApplyMenuPresentationState();
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

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (proceduralArena != null)
        {
            proceduralArena.EnsureGenerated();
            proceduralArena.MovePlayerToSelectedSpawn(player);
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

        ApplyGameplayPresentationState();

        FPSPlayerController fpsPlayerController = FindFirstObjectByType<FPSPlayerController>();
        fpsPlayerController?.ForceGameplayCameraReady();

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.OnGameplayStarted();
            HUDManager.Instance.UpdateWave(1);
        }

        FPSViewModel.Instance?.RefreshForGameplayStart();

        StarterWeaponController starterWeaponController = FindFirstObjectByType<StarterWeaponController>();
        starterWeaponController?.RefreshWeaponVisual();
    }

    public void ReturnToMainMenu()
    {
        gameStarted = false;
        IsRunActive = false;
        Time.timeScale = 0f;

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.HideGameOver();
        }

        ResetGameplayWorld();
        CloseUpgrades();
        ShowMainMenuPanel();

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.SetGameplayHUDVisible(false);
        }

        ApplyMenuPresentationState();
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
        Canvas canvas = FindFirstObjectByType<Canvas>();

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
            CreateMenuButton(mainMenuPanel.transform, "UpgradesButton", "Upgrades", new Vector2(0f, -60f), OpenUpgrades);
            CreateMenuButton(mainMenuPanel.transform, "QuitButton", "Quit", new Vector2(0f, -140f), QuitGame);
        }
        else
        {
            mainMenuCoinsText = mainMenuPanel.transform.Find("TotalCoinsText")?.GetComponent<TMP_Text>();
            WireExistingButton(mainMenuPanel.transform, "PlayButton", PlayGame);
            WireExistingButton(mainMenuPanel.transform, "UpgradesButton", OpenUpgrades);
            WireExistingButton(mainMenuPanel.transform, "QuitButton", QuitGame);
        }

        if (upgradesPanel == null)
        {
            upgradesPanel = CreatePanel(canvas.transform, "UpgradesPanel");
            CreateText(upgradesPanel.transform, "UpgradeTitleText", "META UPGRADES", 40, new Vector2(0f, 220f));
            upgradesCoinsText = CreateText(upgradesPanel.transform, "TotalCoinsText", "", 26, new Vector2(0f, 160f));

            string[] upgradeNames =
            {
                "Max HP +1",
                "Damage +1",
                "Move Speed +5%",
                "XP Gain +10%"
            };

            for (int i = 0; i < upgradeNames.Length; i++)
            {
                float y = 80f - i * 80f;
                int index = i;

                upgradeDescriptionTexts[i] = CreateText(
                    upgradesPanel.transform,
                    "UpgradeText" + i,
                    upgradeNames[i],
                    22,
                    new Vector2(-120f, y),
                    new Vector2(420f, 50f),
                    TextAlignmentOptions.MidlineLeft
                );

                buyButtons[i] = CreateMenuButton(
                    upgradesPanel.transform,
                    "BuyButton" + i,
                    "Buy",
                    new Vector2(260f, y),
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
        Canvas canvas = FindFirstObjectByType<Canvas>();
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
        LayoutCenteredButton(panel, "UpgradesButton", new Vector2(0f, -50f));
        LayoutCenteredButton(panel, "QuitButton", new Vector2(0f, -130f));
    }

    private static void LayoutUpgradesContent(Transform panel)
    {
        LayoutCenteredText(panel, "UpgradeTitleText", new Vector2(0f, 220f), new Vector2(760f, 70f), 40f);
        LayoutCenteredText(panel, "TotalCoinsText", new Vector2(0f, 160f), new Vector2(700f, 44f), 28f);
        LayoutCenteredButton(panel, "BackButton", new Vector2(0f, -220f));

        for (int i = 0; i < 4; i++)
        {
            float y = 80f - i * 80f;
            LayoutCenteredText(panel, "UpgradeText" + i, new Vector2(-120f, y), new Vector2(420f, 50f), 22f);
            LayoutCenteredButton(panel, "BuyButton" + i, new Vector2(260f, y), new Vector2(120f, 46f));
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
        if (mainMenuCoinsText != null)
        {
            mainMenuCoinsText.text = "Total Coins: " + MetaProgressionData.TotalCoins;
        }
    }

    private void RefreshUpgradePanel()
    {
        if (upgradesCoinsText != null)
        {
            upgradesCoinsText.text = "Total Coins: " + MetaProgressionData.TotalCoins;
        }

        SetUpgradeRow(0, MetaProgressionData.UpgradeLevelHP, MetaProgressionData.MaxHPUpgradeLevel, MetaProgressionData.GetNextHPCost(), "Max HP +1");
        SetUpgradeRow(1, MetaProgressionData.UpgradeLevelDamage, MetaProgressionData.MaxDamageUpgradeLevel, MetaProgressionData.GetNextDamageCost(), "Damage +1");
        SetUpgradeRow(2, MetaProgressionData.UpgradeLevelSpeed, MetaProgressionData.MaxSpeedUpgradeLevel, MetaProgressionData.GetNextSpeedCost(), "Move Speed +5%");
        SetUpgradeRow(3, MetaProgressionData.UpgradeLevelXP, MetaProgressionData.MaxXPUpgradeLevel, MetaProgressionData.GetNextXPCost(), "XP Gain +10%");
    }

    private void SetUpgradeRow(int index, int level, int maxLevel, int nextCost, string label)
    {
        if (upgradeDescriptionTexts[index] == null) return;

        if (level >= maxLevel)
        {
            upgradeDescriptionTexts[index].text = label + "  |  MAX";
        }
        else
        {
            upgradeDescriptionTexts[index].text = label + "  |  Lv " + level + "  |  Cost " + nextCost;
        }

        if (buyButtons[index] != null)
        {
            buyButtons[index].interactable = level < maxLevel && nextCost >= 0 && MetaProgressionData.TotalCoins >= nextCost;
        }
    }

    private void OnBuyUpgradeClicked(int index)
    {
        bool purchased = index switch
        {
            0 => MetaProgressionData.TryPurchaseHPUpgrade(),
            1 => MetaProgressionData.TryPurchaseDamageUpgrade(),
            2 => MetaProgressionData.TryPurchaseSpeedUpgrade(),
            3 => MetaProgressionData.TryPurchaseXPUpgrade(),
            _ => false
        };

        if (!purchased) return;

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
