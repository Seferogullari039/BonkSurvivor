using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MerchantShrineUI : MonoBehaviour
{
    public const int HealCost = 40;
    public const int ChestBuffCost = 75;
    public const int BuildUpgradeCost = 100;
    public const int HealValue = 25;

    private static MerchantShrineUI instance;

    private GameObject panelRoot;
    private TMP_Text titleText;
    private TMP_Text coinsText;
    private TMP_Text resultText;
    private readonly Button[] optionButtons = new Button[3];
    private readonly TMP_Text[] optionLabels = new TMP_Text[3];
    private readonly TMP_Text[] optionCostLabels = new TMP_Text[3];
    private readonly bool[] purchasedOptions = new bool[3];

    private MerchantShrineController sourceShrine;
    private PlayerStats playerStats;
    private bool isBuilt;
    private bool isOpen;

    public static bool IsOpen => instance != null && instance.isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<MerchantShrineUI>() != null)
        {
            return;
        }

        GameObject host = new GameObject("MerchantShrineUI");
        host.AddComponent<MerchantShrineUI>();
    }

    public static void Open(MerchantShrineController shrine, PlayerStats stats)
    {
        if (instance == null)
        {
            Bootstrap();
        }

        instance?.Show(shrine, stats);
    }

    public static void ForceClose()
    {
        instance?.Close();
    }

    private void Awake()
    {
        instance = this;
        BuildPanel();
        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (ShouldForceClose())
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void Show(MerchantShrineController shrine, PlayerStats stats)
    {
        if (!isBuilt)
        {
            BuildPanel();
        }

        if (ShouldBlockOpen())
        {
            return;
        }

        sourceShrine = shrine;
        playerStats = stats;

        for (int i = 0; i < purchasedOptions.Length; i++)
        {
            purchasedOptions[i] = false;
        }

        if (resultText != null)
        {
            resultText.text = string.Empty;
        }

        isOpen = true;
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();

        ChestRevealPause.Begin();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshDisplay();
    }

    private void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        ChestRevealPause.End();

        if (MainMenuManager.IsRunActive && Time.timeScale > 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        sourceShrine = null;
        playerStats = null;
    }

    private static bool ShouldForceClose()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return true;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return true;
        }

        return false;
    }

    private static bool ShouldBlockOpen()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return true;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return true;
        }

        if (PauseMenuManager.IsGameplayPaused)
        {
            return true;
        }

        if (ChestRevealPause.IsPaused)
        {
            return true;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksGameplayPause)
        {
            return true;
        }

        return false;
    }

    private void RefreshDisplay()
    {
        if (playerStats == null)
        {
            return;
        }

        if (coinsText != null)
        {
            coinsText.text = "Coins: " + playerStats.Coins;
        }

        RefreshOptionButton(
            0,
            "Heal +" + HealValue + " HP",
            HealCost,
            !purchasedOptions[0] && playerStats.CurrentHealth < playerStats.EffectiveMaxHealth);

        RefreshOptionButton(
            1,
            "Random Chest Buff",
            ChestBuffCost,
            !purchasedOptions[1]);

        bool canUpgradeBuild = MerchantShrineShopUtility.CanUpgradeRandomBuildSlot();
        RefreshOptionButton(
            2,
            "Upgrade Random Build Slot",
            BuildUpgradeCost,
            !purchasedOptions[2] && canUpgradeBuild);
    }

    private void RefreshOptionButton(int index, string label, int cost, bool available)
    {
        bool canAfford = playerStats != null && playerStats.Coins >= cost;
        bool interactable = available && canAfford;

        if (optionLabels[index] != null)
        {
            string suffix = purchasedOptions[index] ? " (sold)" : !available ? " (unavailable)" : !canAfford ? " (need coins)" : string.Empty;
            optionLabels[index].text = label + suffix;
            optionLabels[index].color = interactable
                ? new Color(0.9f, 0.92f, 0.96f, 1f)
                : new Color(0.55f, 0.58f, 0.64f, 0.9f);
        }

        if (optionCostLabels[index] != null)
        {
            optionCostLabels[index].text = cost.ToString();
            optionCostLabels[index].color = interactable
                ? new Color(0.92f, 0.82f, 0.42f, 1f)
                : new Color(0.5f, 0.48f, 0.4f, 0.85f);
        }

        if (optionButtons[index] != null)
        {
            optionButtons[index].interactable = interactable;
        }
    }

    private void OnOptionClicked(int optionIndex)
    {
        if (!isOpen || playerStats == null || purchasedOptions[optionIndex])
        {
            return;
        }

        bool success = optionIndex switch
        {
            0 => TryPurchaseHeal(),
            1 => TryPurchaseChestBuff(),
            2 => TryPurchaseBuildUpgrade(),
            _ => false
        };

        if (!success)
        {
            ShowResult("Purchase failed.");
            RefreshDisplay();
            return;
        }

        purchasedOptions[optionIndex] = true;
        sourceShrine?.NotifyPurchaseComplete();
        RefreshDisplay();
    }

    private bool TryPurchaseHeal()
    {
        if (!playerStats.TrySpendCoins(HealCost))
        {
            return false;
        }

        playerStats.HealAmount(HealValue);
        ShowResult("Healed +" + HealValue + " HP.");
        return true;
    }

    private bool TryPurchaseChestBuff()
    {
        if (!playerStats.TrySpendCoins(ChestBuffCost))
        {
            return false;
        }

        ChestStatRewardType reward = ChestStatRewardCatalog.RollRandomReward();
        ChestStatRewardCatalog.Apply(reward, UpgradeRarity.Rare, playerStats);
        ChestStatRewardCatalog.GetDisplay(reward, UpgradeRarity.Rare, out string title, out _);
        ShowResult("Gained: " + title + ".");
        return true;
    }

    private bool TryPurchaseBuildUpgrade()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null || !MerchantShrineShopUtility.TryPickRandomUpgradableIndex(out int upgradeIndex, out string upgradeName))
        {
            return false;
        }

        if (!playerStats.TrySpendCoins(BuildUpgradeCost))
        {
            return false;
        }

        tracker.RecordUpgrade(upgradeIndex);
        ShowResult("Upgraded: " + upgradeName + ".");
        return true;
    }

    private void ShowResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
    }

    private void BuildPanel()
    {
        if (isBuilt)
        {
            return;
        }

        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        GameObject panelObject = new GameObject("MerchantShrinePanel");
        panelObject.transform.SetParent(canvas.transform, false);
        panelRoot = panelObject;

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(460f, 430f));

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.06f, 0.1f, 0.92f);
        panelImage.raycastTarget = true;

        titleText = CreateText(panelObject.transform, "Title", new Vector2(0f, 168f), new Vector2(420f, 34f), 24f, FontStyles.Bold);
        titleText.text = "MYSTIC MERCHANT";
        titleText.color = new Color(0.86f, 0.78f, 0.98f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        coinsText = CreateText(panelObject.transform, "Coins", new Vector2(0f, 132f), new Vector2(420f, 26f), 18f, FontStyles.Normal);
        coinsText.alignment = TextAlignmentOptions.Center;
        coinsText.color = new Color(0.92f, 0.84f, 0.48f, 1f);

        CreateOptionRow(panelObject.transform, 0, "Option0", new Vector2(0f, 72f), () => OnOptionClicked(0));
        CreateOptionRow(panelObject.transform, 1, "Option1", new Vector2(0f, 18f), () => OnOptionClicked(1));
        CreateOptionRow(panelObject.transform, 2, "Option2", new Vector2(0f, -36f), () => OnOptionClicked(2));

        resultText = CreateText(panelObject.transform, "Result", new Vector2(0f, -92f), new Vector2(420f, 24f), 14f, FontStyles.Italic);
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = new Color(0.72f, 0.9f, 0.74f, 1f);

        Button leaveButton = CreateButton(panelObject.transform, "LeaveButton", new Vector2(0f, -142f), new Vector2(180f, 38f), "Leave", Close);
        leaveButton.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.24f, 0.95f);

        isBuilt = true;
    }

    private void CreateOptionRow(Transform parent, int index, string rowName, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject rowObject = new GameObject(rowName);
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rowRect, anchoredPosition, new Vector2(420f, 42f));

        Button button = CreateButton(rowObject.transform, "Button", Vector2.zero, new Vector2(420f, 42f), string.Empty, onClick);
        optionButtons[index] = button;
        button.GetComponent<Image>().color = new Color(0.11f, 0.13f, 0.18f, 0.95f);

        optionLabels[index] = CreateText(rowObject.transform, "Label", new Vector2(-150f, 0f), new Vector2(260f, 34f), 15f, FontStyles.Normal);
        optionLabels[index].alignment = TextAlignmentOptions.MidlineLeft;

        optionCostLabels[index] = CreateText(rowObject.transform, "Cost", new Vector2(170f, 0f), new Vector2(60f, 34f), 16f, FontStyles.Bold);
        optionCostLabels[index].alignment = TextAlignmentOptions.MidlineRight;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, anchoredPosition, size);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.raycastTarget = false;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            textMesh.font = font;
        }

        return textMesh;
    }

    private static Button CreateButton(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, anchoredPosition, size);

        Image image = buttonObject.AddComponent<Image>();
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateText(buttonObject.transform, "Text", Vector2.zero, size, 16f, FontStyles.Bold);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.9f, 0.92f, 0.96f, 1f);

            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        return button;
    }
}

internal static class MerchantShrineShopUtility
{
    public static bool CanUpgradeRandomBuildSlot()
    {
        return TryPickRandomUpgradableIndex(out _, out _);
    }

    public static bool TryPickRandomUpgradableIndex(out int upgradeIndex, out string upgradeName)
    {
        upgradeIndex = -1;
        upgradeName = string.Empty;
        List<int> candidates = new List<int>(6);

        if (!TryCollectUpgradableIndices(candidates) || candidates.Count == 0)
        {
            return false;
        }

        upgradeIndex = candidates[Random.Range(0, candidates.Count)];
        upgradeName = UpgradeOptionCatalog.GetDisplayName(upgradeIndex);
        return true;
    }

    private static bool TryCollectUpgradableIndices(List<int> candidates)
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return false;
        }

        bool found = false;

        for (int slot = 0; slot < RunBuildTracker.MaxSlotsPerCategory; slot++)
        {
            RunBuildSlotEntry skillEntry = tracker.GetSkillSlot(slot);

            if (TryAddCandidate(candidates, tracker, skillEntry))
            {
                found = true;
            }

            RunBuildSlotEntry passiveEntry = tracker.GetPassiveSlot(slot);

            if (TryAddCandidate(candidates, tracker, passiveEntry))
            {
                found = true;
            }
        }

        return found;
    }

    private static bool TryAddCandidate(List<int> candidates, RunBuildTracker tracker, RunBuildSlotEntry entry)
    {
        if (entry == null || tracker.IsMaxed(entry.UpgradeIndex))
        {
            return false;
        }

        if (candidates != null)
        {
            candidates.Add(entry.UpgradeIndex);
        }

        return true;
    }
}
