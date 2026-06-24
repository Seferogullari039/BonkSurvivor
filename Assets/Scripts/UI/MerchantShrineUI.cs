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

    private const float PanelWidth = 480f;
    private const float PanelHeight = 360f;
    private const float OptionRowWidth = 432f;
    private const float OptionRowHeight = 50f;
    private const float RowLeftPadding = 28f;
    private const float RowRightPadding = 24f;
    private const float RowCostWidth = 64f;

    private static readonly Color PanelBackgroundColor = new Color(0.04f, 0.05f, 0.09f, 0.94f);
    private static readonly Color OptionRowBackgroundColor = new Color(0.09f, 0.11f, 0.16f, 0.98f);
    private static readonly Color EnabledLabelColor = new Color(0.93f, 0.95f, 0.98f, 1f);
    private static readonly Color DisabledLabelColor = new Color(0.74f, 0.78f, 0.84f, 1f);
    private static readonly Color EnabledCostColor = new Color(0.94f, 0.84f, 0.44f, 1f);
    private static readonly Color DisabledCostColor = new Color(0.62f, 0.6f, 0.54f, 1f);
    private static readonly Color EnabledStatusColor = new Color(0.62f, 0.88f, 0.66f, 1f);
    private static readonly Color DisabledStatusColor = new Color(0.78f, 0.7f, 0.58f, 1f);
    private static readonly Color UnavailableStatusColor = new Color(0.7f, 0.76f, 0.86f, 1f);

    private static MerchantShrineUI instance;

    private GameObject panelRoot;
    private TMP_Text titleText;
    private TMP_Text coinsText;
    private TMP_Text resultText;
    private readonly Button[] optionButtons = new Button[3];
    private readonly TMP_Text[] optionLabels = new TMP_Text[3];
    private readonly TMP_Text[] optionCostLabels = new TMP_Text[3];
    private readonly TMP_Text[] optionStatusLabels = new TMP_Text[3];
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
        MerchantShrineTradeGuards.RestoreGameplayCursorAfterTradeClose();

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

        if (MerchantShrineTradeGuards.IsRewardFlowBlockingTrade())
        {
            return true;
        }

        return false;
    }

    private static bool ShouldBlockOpen()
    {
        return !MerchantShrineTradeGuards.CanOpenTrade();
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
            !purchasedOptions[0] && playerStats.CurrentHealth < playerStats.EffectiveMaxHealth,
            "Full HP");

        RefreshOptionButton(
            1,
            "Random Chest Buff",
            ChestBuffCost,
            !purchasedOptions[1],
            string.Empty);

        bool canUpgradeBuild = MerchantShrineShopUtility.CanUpgradeRandomBuildSlot();
        RefreshOptionButton(
            2,
            "Upgrade Build Slot",
            BuildUpgradeCost,
            !purchasedOptions[2] && canUpgradeBuild,
            GetBuildUpgradeDisabledReason());
    }

    private static string GetBuildUpgradeDisabledReason()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            return "No build upgrade";
        }

        bool hasAnyBuild = false;
        bool hasUpgradable = false;

        for (int slot = 0; slot < RunBuildTracker.MaxSlotsPerCategory; slot++)
        {
            RunBuildSlotEntry skillEntry = tracker.GetSkillSlot(slot);
            RunBuildSlotEntry passiveEntry = tracker.GetPassiveSlot(slot);

            if (skillEntry != null)
            {
                hasAnyBuild = true;

                if (!tracker.IsMaxed(skillEntry.UpgradeIndex))
                {
                    hasUpgradable = true;
                }
            }

            if (passiveEntry != null)
            {
                hasAnyBuild = true;

                if (!tracker.IsMaxed(passiveEntry.UpgradeIndex))
                {
                    hasUpgradable = true;
                }
            }
        }

        if (!hasAnyBuild)
        {
            return "No build upgrade";
        }

        if (!hasUpgradable)
        {
            return "All maxed";
        }

        return "No build upgrade";
    }

    private void RefreshOptionButton(int index, string optionName, int cost, bool available, string unavailableReason)
    {
        bool canAfford = playerStats != null && playerStats.Coins >= cost;
        bool interactable = available && canAfford && !purchasedOptions[index];

        if (optionLabels[index] != null)
        {
            optionLabels[index].text = optionName;
            optionLabels[index].color = interactable ? EnabledLabelColor : DisabledLabelColor;
        }

        if (optionCostLabels[index] != null)
        {
            optionCostLabels[index].text = cost.ToString();
            optionCostLabels[index].color = interactable ? EnabledCostColor : DisabledCostColor;
        }

        if (optionStatusLabels[index] != null)
        {
            string statusText = string.Empty;

            if (purchasedOptions[index])
            {
                statusText = "Sold";
                optionStatusLabels[index].color = EnabledStatusColor;
            }
            else if (!available)
            {
                statusText = string.IsNullOrEmpty(unavailableReason) ? "Unavailable" : unavailableReason;
                optionStatusLabels[index].color = UnavailableStatusColor;
            }
            else if (!canAfford)
            {
                statusText = "Need " + cost + " coins";
                optionStatusLabels[index].color = DisabledStatusColor;
            }

            optionStatusLabels[index].text = statusText;
            optionStatusLabels[index].gameObject.SetActive(!string.IsNullOrEmpty(statusText));
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
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

        Image panelBorder = CreatePanelBorder(panelObject.transform);
        panelBorder.color = new Color(0.14f, 0.16f, 0.22f, 0.98f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = PanelBackgroundColor;
        panelImage.raycastTarget = true;

        titleText = CreateText(panelObject.transform, "Title", new Vector2(0f, 142f), new Vector2(OptionRowWidth, 34f), 24f, FontStyles.Bold);
        titleText.text = "MYSTIC MERCHANT";
        titleText.color = new Color(0.86f, 0.78f, 0.98f, 1f);
        titleText.alignment = TextAlignmentOptions.Center;

        coinsText = CreateText(panelObject.transform, "Coins", new Vector2(0f, 108f), new Vector2(OptionRowWidth, 26f), 18f, FontStyles.Normal);
        coinsText.alignment = TextAlignmentOptions.Center;
        coinsText.color = new Color(0.92f, 0.84f, 0.48f, 1f);

        CreateOptionRow(panelObject.transform, 0, "Option0", new Vector2(0f, 52f), () => OnOptionClicked(0));
        CreateOptionRow(panelObject.transform, 1, "Option1", new Vector2(0f, -4f), () => OnOptionClicked(1));
        CreateOptionRow(panelObject.transform, 2, "Option2", new Vector2(0f, -60f), () => OnOptionClicked(2));

        resultText = CreateText(panelObject.transform, "Result", new Vector2(0f, -118f), new Vector2(OptionRowWidth, 24f), 14f, FontStyles.Italic);
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = new Color(0.72f, 0.9f, 0.74f, 1f);
        resultText.overflowMode = TextOverflowModes.Overflow;

        Button leaveButton = CreateButton(panelObject.transform, "LeaveButton", new Vector2(0f, -158f), new Vector2(200f, 40f), "Leave", Close);
        leaveButton.GetComponent<Image>().color = new Color(0.16f, 0.18f, 0.24f, 1f);

        isBuilt = true;
    }

    private static Image CreatePanelBorder(Transform parent)
    {
        GameObject borderObject = new GameObject("PanelBorder");
        borderObject.transform.SetParent(parent, false);
        borderObject.transform.SetAsFirstSibling();

        RectTransform borderRect = borderObject.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2f, -2f);
        borderRect.offsetMax = new Vector2(2f, 2f);

        Image borderImage = borderObject.AddComponent<Image>();
        borderImage.raycastTarget = false;
        return borderImage;
    }

    private void CreateOptionRow(Transform parent, int index, string rowName, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject rowObject = new GameObject(rowName);
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rowRect, anchoredPosition, new Vector2(OptionRowWidth, OptionRowHeight));

        GameObject buttonObject = new GameObject("Button");
        buttonObject.transform.SetParent(rowObject.transform, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        StretchFillParent(buttonRect);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.raycastTarget = true;
        buttonImage.color = OptionRowBackgroundColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);
        optionButtons[index] = button;

        optionLabels[index] = CreateRowText(rowObject.transform, "Label", 16f, FontStyles.Bold);
        optionLabels[index].alignment = TextAlignmentOptions.MidlineLeft;
        optionLabels[index].overflowMode = TextOverflowModes.Ellipsis;
        SetStretchedRect(
            optionLabels[index].rectTransform,
            new Vector2(0f, 0.52f),
            new Vector2(1f, 1f),
            RowLeftPadding,
            RowRightPadding + RowCostWidth + 4f,
            4f,
            0f);

        optionCostLabels[index] = CreateRowText(rowObject.transform, "Cost", 17f, FontStyles.Bold);
        optionCostLabels[index].alignment = TextAlignmentOptions.MidlineRight;
        optionCostLabels[index].overflowMode = TextOverflowModes.Overflow;

        RectTransform costRect = optionCostLabels[index].rectTransform;
        costRect.anchorMin = new Vector2(1f, 0.52f);
        costRect.anchorMax = new Vector2(1f, 1f);
        costRect.pivot = new Vector2(1f, 0.5f);
        costRect.offsetMin = new Vector2(-(RowRightPadding + RowCostWidth), 2f);
        costRect.offsetMax = new Vector2(-RowRightPadding, -4f);

        optionStatusLabels[index] = CreateRowText(rowObject.transform, "Status", 12.5f, FontStyles.Normal);
        optionStatusLabels[index].alignment = TextAlignmentOptions.MidlineLeft;
        optionStatusLabels[index].overflowMode = TextOverflowModes.Overflow;
        SetStretchedRect(
            optionStatusLabels[index].rectTransform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0.48f),
            RowLeftPadding,
            RowRightPadding,
            0f,
            2f);
        optionStatusLabels[index].gameObject.SetActive(false);
    }

    private static void StretchFillParent(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetStretchedRect(
        RectTransform rectTransform,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float left,
        float right,
        float top,
        float bottom)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private static TMP_Text CreateRowText(Transform parent, string objectName, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);
        textObject.AddComponent<RectTransform>();

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.raycastTarget = false;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            textMesh.font = font;
        }

        return textMesh;
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

internal static class MerchantShrineTradeGuards
{
    public static bool CanOpenTrade()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return false;
        }

        if (MerchantShrineUI.IsOpen)
        {
            return false;
        }

        if (IsRewardFlowBlockingTrade())
        {
            return false;
        }

        if (PauseMenuManager.IsGameplayPaused)
        {
            return false;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return false;
        }

        if (DevAdminPanel.IsOpen)
        {
            return false;
        }

        if (ChestRevealPause.IsPaused)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            return false;
        }

        return true;
    }

    public static bool IsRewardFlowBlockingTrade()
    {
        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksMerchantTrade)
        {
            return true;
        }

        HUDManager hudManager = HUDManager.Instance;

        if (hudManager != null && hudManager.IsLevelUpFeedbackVisible)
        {
            return true;
        }

        return false;
    }

    public static void RestoreGameplayCursorAfterTradeClose()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return;
        }

        if (IsRewardFlowBlockingTrade())
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (ChestRevealPause.IsPaused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (PauseMenuManager.IsGameplayPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Time.timeScale > 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
