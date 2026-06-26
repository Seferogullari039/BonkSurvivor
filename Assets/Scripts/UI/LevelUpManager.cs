using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private Button optionButton1;
    [SerializeField] private Button optionButton2;
    [SerializeField] private Button optionButton3;
    [SerializeField] private TMP_Text optionText1;
    [SerializeField] private TMP_Text optionText2;
    [SerializeField] private TMP_Text optionText3;

    private readonly int[] shownUpgradeIndices = new int[3];
    private readonly UpgradeRarity[] shownUpgradeRarities = new UpgradeRarity[3];
    private readonly UpgradeCardView[] upgradeCards = new UpgradeCardView[3];
    private int menuPlayerLevel = 1;
    private int remainingUpgradeSelections = 1;
    private bool isChestUpgradeMenu;
    private bool useChestSingleCardReveal;
    private bool chestRewardCollected;
    private ChestRarity currentChestRarity = ChestRarity.Normal;
    private TMP_Text chestHeaderText;
    private ChestLootSelectionUI chestLootSelectionUI;
    private ChestSingleCardRevealUI chestSingleCardRevealUI;
    private readonly ItemOfferLayoutUI itemOfferLayout = new ItemOfferLayoutUI();

    private const int BonusRewardCoin = -1;
    private const int BonusRewardHeal = -2;
    private ChestStatRewardType shownChestStatReward;
    private bool chestRewardIsSpecialUpgrade;

    private GameObject offerActionRow;
    private Button skipOfferButton;
    private Button refreshOfferButton;
    private TMP_Text skipOfferButtonText;
    private TMP_Text refreshOfferButtonText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
            ApplyUpgradePanelLayout();
            EnsureUpgradeCards();
        }
    }

    private sealed class UpgradeCardView
    {
        public Button Button;
        public Button BanishButton;
        public TMP_Text BanishButtonText;
        public Image BackgroundImage;
        public Image GlowImage;
        public Transform IconRoot;
        public Image IconImage;
        public TMP_Text RarityText;
        public TMP_Text CategoryText;
        public TMP_Text BuildText;
        public TMP_Text TitleText;
        public TMP_Text DescriptionText;
    }

    private readonly struct UpgradeCardContent
    {
        public UpgradeCardContent(string title, string description, string iconKey = "")
        {
            Title = title;
            Description = description;
            IconKey = iconKey ?? string.Empty;
        }

        public string Title { get; }
        public string Description { get; }
        public string IconKey { get; }
    }

    private void ApplyUpgradePanelLayout()
    {
        if (levelUpPanel == null) return;

        Canvas canvas = levelUpPanel.GetComponentInParent<Canvas>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        itemOfferLayout.EnsureBuilt(canvas, levelUpPanel, optionText1);

        RectTransform panelRect = levelUpPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, new Vector2(0f, -6f), new Vector2(800f, 700f));

        Image panelImage = levelUpPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = new Color(0.045f, 0.055f, 0.095f, 0.96f);
        }

        LayoutUpgradeButton(optionButton1, 108f);
        LayoutUpgradeButton(optionButton2, -48f);
        LayoutUpgradeButton(optionButton3, -204f);

        if (chestHeaderText != null)
        {
            UiLayoutUtility.SetAnchorCenter(chestHeaderText.rectTransform, new Vector2(0f, 286f), new Vector2(680f, 44f));
            chestHeaderText.fontSize = 30f;
        }

        EnsureOfferActionButtons();
    }

    private static void LayoutUpgradeButton(Button button, float yOffset)
    {
        if (button == null) return;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(0f, yOffset), new Vector2(720f, 142f));

        Image buttonImage = button.GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.98f, 1f);
        colors.selectedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private void EnsureUpgradeCards()
    {
        EnsureUpgradeCard(0, optionButton1, optionText1);
        EnsureUpgradeCard(1, optionButton2, optionText2);
        EnsureUpgradeCard(2, optionButton3, optionText3);
        ApplyUpgradeCardLayout(0, optionButton1);
        ApplyUpgradeCardLayout(1, optionButton2);
        ApplyUpgradeCardLayout(2, optionButton3);
    }

    private void EnsureUpgradeCard(int index, Button button, TMP_Text legacyText)
    {
        if (button == null || upgradeCards[index] != null)
        {
            return;
        }

        if (legacyText != null)
        {
            legacyText.gameObject.SetActive(false);
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();

        if (buttonRect == null)
        {
            return;
        }

        Image backgroundImage = button.GetComponent<Image>();
        Image glowImage = EnsureCardGlow(buttonRect);
        EnsureCardIconSlot(buttonRect, out Transform iconRoot, out Image iconImage);

        upgradeCards[index] = new UpgradeCardView
        {
            Button = button,
            BackgroundImage = backgroundImage,
            GlowImage = glowImage,
            IconRoot = iconRoot,
            IconImage = iconImage,
            RarityText = CreateCardText(buttonRect, legacyText, "RarityText", new Vector2(98f, 42f), new Vector2(430f, 22f), 15f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft),
            CategoryText = CreateCardText(buttonRect, legacyText, "CategoryText", new Vector2(98f, 18f), new Vector2(430f, 20f), 13f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft),
            BuildText = CreateCardText(buttonRect, legacyText, "BuildText", new Vector2(98f, 2f), new Vector2(430f, 18f), 12f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft),
            TitleText = CreateCardText(buttonRect, legacyText, "TitleText", new Vector2(98f, -20f), new Vector2(430f, 32f), 26f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft),
            DescriptionText = CreateCardText(buttonRect, legacyText, "DescriptionText", new Vector2(98f, -56f), new Vector2(430f, 46f), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft)
        };

        ConfigureDescriptionText(upgradeCards[index].DescriptionText);
        ConfigureTitleText(upgradeCards[index].TitleText);
        ConfigureRarityText(upgradeCards[index].RarityText);
        ConfigureCategoryText(upgradeCards[index].CategoryText);
        ConfigureBuildText(upgradeCards[index].BuildText);
        EnsureCardBanishButton(index, buttonRect, legacyText);
    }

    private void EnsureCardBanishButton(int cardIndex, RectTransform cardRect, TMP_Text fontSource)
    {
        UpgradeCardView card = upgradeCards[cardIndex];

        if (card == null || card.BanishButton != null)
        {
            return;
        }

        GameObject buttonObject = new GameObject("BanishButton");
        buttonObject.transform.SetParent(cardRect, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-10f, -8f);
        buttonRect.sizeDelta = new Vector2(78f, 24f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.16f, 0.10f, 0.12f, 0.94f);

        Button button = buttonObject.AddComponent<Button>();
        ApplyOfferActionButtonColors(button);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        CopyTmpFontFrom(fontSource, labelText);
        labelText.text = "BANISH";
        labelText.fontSize = 11f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.92f, 0.78f, 0.80f, 1f);
        labelText.raycastTarget = false;

        card.BanishButton = button;
        card.BanishButtonText = labelText;
    }

    private void EnsureOfferActionButtons()
    {
        if (levelUpPanel == null || offerActionRow != null)
        {
            return;
        }

        offerActionRow = new GameObject("OfferActionRow");
        offerActionRow.transform.SetParent(levelUpPanel.transform, false);

        RectTransform rowRect = offerActionRow.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rowRect, new Vector2(0f, -318f), new Vector2(520f, 40f));

        skipOfferButton = CreateOfferActionButton(
            offerActionRow.transform,
            "SkipOfferButton",
            new Vector2(-110f, 0f),
            out skipOfferButtonText);
        refreshOfferButton = CreateOfferActionButton(
            offerActionRow.transform,
            "RefreshOfferButton",
            new Vector2(110f, 0f),
            out refreshOfferButtonText);

        skipOfferButton.onClick.AddListener(OnSkipOffer);
        refreshOfferButton.onClick.AddListener(OnRefreshOffer);
        offerActionRow.SetActive(false);
    }

    private Button CreateOfferActionButton(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        out TMP_Text labelText)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(buttonRect, anchoredPosition, new Vector2(180f, 36f));

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.10f, 0.11f, 0.15f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        ApplyOfferActionButtonColors(button);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textMesh = labelObject.AddComponent<TextMeshProUGUI>();
        CopyTmpFontFrom(optionText1, textMesh);
        textMesh.fontSize = 16f;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = new Color(0.88f, 0.90f, 0.96f, 1f);
        textMesh.raycastTarget = false;

        labelText = textMesh;
        return button;
    }

    private static void ApplyOfferActionButtonColors(Button button)
    {
        if (button == null)
        {
            return;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.pressedColor = new Color(0.82f, 0.88f, 0.98f, 1f);
        colors.selectedColor = new Color(0.92f, 0.95f, 1f, 1f);
        colors.disabledColor = new Color(0.45f, 0.47f, 0.52f, 0.65f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void ApplyUpgradeCardLayout(int index, Button button)
    {
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();

        if (buttonRect == null)
        {
            return;
        }

        Transform iconRootTransform = buttonRect.Find("CardIconRoot");

        if (iconRootTransform is RectTransform iconRootRect)
        {
            UiLayoutUtility.SetAnchorCenter(iconRootRect, new Vector2(-278f, 0f), new Vector2(92f, 92f));
            iconRootTransform.gameObject.SetActive(true);
        }

        RepositionCardText(buttonRect, "RarityText", new Vector2(98f, 42f), new Vector2(430f, 22f));
        RepositionCardText(buttonRect, "CategoryText", new Vector2(98f, 18f), new Vector2(430f, 20f));
        RepositionCardText(buttonRect, "BuildText", new Vector2(98f, 2f), new Vector2(430f, 18f));
        RepositionCardText(buttonRect, "TitleText", new Vector2(98f, -20f), new Vector2(430f, 32f));
        RepositionCardText(buttonRect, "DescriptionText", new Vector2(98f, -56f), new Vector2(430f, 46f));
    }

    private static void RepositionCardText(RectTransform parent, string childName, Vector2 position, Vector2 size)
    {
        Transform child = parent.Find(childName);

        if (child is RectTransform childRect)
        {
            UiLayoutUtility.SetAnchorCenter(childRect, position, size);
        }
    }

    private static Image EnsureCardGlow(RectTransform buttonRect)
    {
        Transform existingGlow = buttonRect.Find("CardGlow");

        if (existingGlow != null)
        {
            Image existingImage = existingGlow.GetComponent<Image>();

            if (existingImage != null)
            {
                return existingImage;
            }
        }

        GameObject glowObject = new GameObject("CardGlow");
        glowObject.transform.SetParent(buttonRect, false);
        glowObject.transform.SetAsFirstSibling();

        RectTransform glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-6f, -6f);
        glowRect.offsetMax = new Vector2(6f, 6f);

        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.raycastTarget = false;
        glowImage.color = new Color(0.86f, 0.88f, 0.92f, 0.12f);

        return glowImage;
    }

    private static void EnsureCardIconSlot(RectTransform buttonRect, out Transform iconRoot, out Image iconImage)
    {
        Transform existingIconRoot = buttonRect.Find("CardIconRoot");

        if (existingIconRoot != null)
        {
            iconRoot = existingIconRoot;
            iconImage = existingIconRoot.Find("IconImage")?.GetComponent<Image>();
            EnsureIconPlaceholder(existingIconRoot);
            iconRoot.gameObject.SetActive(true);
            return;
        }

        GameObject iconRootObject = new GameObject("CardIconRoot");
        iconRootObject.transform.SetParent(buttonRect, false);
        iconRootObject.SetActive(true);

        RectTransform iconRootRect = iconRootObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(iconRootRect, new Vector2(-278f, 0f), new Vector2(92f, 92f));

        EnsureIconPlaceholder(iconRootObject.transform);

        GameObject iconImageObject = new GameObject("IconImage");
        iconImageObject.transform.SetParent(iconRootObject.transform, false);

        RectTransform iconImageRect = iconImageObject.AddComponent<RectTransform>();
        iconImageRect.anchorMin = Vector2.zero;
        iconImageRect.anchorMax = Vector2.one;
        iconImageRect.offsetMin = new Vector2(6f, 6f);
        iconImageRect.offsetMax = new Vector2(-6f, -6f);

        iconImage = iconImageObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = true;

        iconRoot = iconRootObject.transform;
    }

    private static void EnsureIconPlaceholder(Transform iconRootTransform)
    {
        EnsureIconFrame(iconRootTransform);

        Transform existingPlaceholder = iconRootTransform.Find("IconPlaceholder");

        if (existingPlaceholder == null)
        {
            GameObject placeholderObject = new GameObject("IconPlaceholder");
            placeholderObject.transform.SetParent(iconRootTransform, false);
            placeholderObject.transform.SetAsFirstSibling();

            RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

            Image placeholderImage = placeholderObject.AddComponent<Image>();
            placeholderImage.raycastTarget = false;
            placeholderImage.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        }

        Transform existingLetter = iconRootTransform.Find("IconPlaceholderLetter");

        if (existingLetter != null)
        {
            return;
        }

        GameObject letterObject = new GameObject("IconPlaceholderLetter");
        letterObject.transform.SetParent(iconRootTransform, false);

        RectTransform letterRect = letterObject.AddComponent<RectTransform>();
        letterRect.anchorMin = Vector2.zero;
        letterRect.anchorMax = Vector2.one;
        letterRect.offsetMin = Vector2.zero;
        letterRect.offsetMax = Vector2.zero;

        TextMeshProUGUI letterText = letterObject.AddComponent<TextMeshProUGUI>();
        letterText.alignment = TextAlignmentOptions.Center;
        letterText.fontSize = 34f;
        letterText.fontStyle = FontStyles.Bold;
        letterText.color = new Color(0.62f, 0.66f, 0.74f, 0.95f);
        letterText.raycastTarget = false;
        letterText.text = "?";
    }

    private static void EnsureIconFrame(Transform iconRootTransform)
    {
        Transform existingFrame = iconRootTransform.Find("IconFrame");

        if (existingFrame != null)
        {
            return;
        }

        GameObject frameObject = new GameObject("IconFrame");
        frameObject.transform.SetParent(iconRootTransform, false);
        frameObject.transform.SetAsFirstSibling();

        RectTransform frameRect = frameObject.AddComponent<RectTransform>();
        frameRect.anchorMin = Vector2.zero;
        frameRect.anchorMax = Vector2.one;
        frameRect.offsetMin = new Vector2(-3f, -3f);
        frameRect.offsetMax = new Vector2(3f, 3f);

        Image frameImage = frameObject.AddComponent<Image>();
        frameImage.raycastTarget = false;
        frameImage.color = new Color(0.28f, 0.30f, 0.36f, 0.95f);
    }

    private static TMP_Text CreateCardText(
        RectTransform parent,
        TMP_Text fontSource,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, anchoredPosition, size);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        CopyTmpFontFrom(fontSource, textMesh);
        textMesh.alignment = alignment;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.Normal;
        textMesh.richText = false;
        textMesh.raycastTarget = false;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;

        return textMesh;
    }

    private static void CopyTmpFontFrom(TMP_Text source, TMP_Text target)
    {
        if (source == null || target == null)
        {
            return;
        }

        if (source.font != null)
        {
            target.font = source.font;
        }

        if (source.fontSharedMaterial != null)
        {
            target.fontSharedMaterial = source.fontSharedMaterial;
        }
    }

    private static void ConfigureTitleText(TMP_Text titleText)
    {
        if (titleText == null)
        {
            return;
        }

        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 22f;
        titleText.fontSizeMax = 30f;
        titleText.characterSpacing = 2f;
        TryApplyTextOutline(titleText, 0.1f, new Color(0f, 0f, 0f, 0.45f));
    }

    private static void TryApplyTextOutline(TMP_Text text, float outlineWidth, Color outlineColor)
    {
        if (text == null || text.font == null)
        {
            return;
        }

        try
        {
            text.outlineWidth = outlineWidth;
            text.outlineColor = outlineColor;
        }
        catch
        {
            // Unity 6 TMP outline can throw if material is not ready; skip outline polish only.
        }
    }

    private static void ConfigureCategoryText(TMP_Text categoryText)
    {
        if (categoryText == null)
        {
            return;
        }

        categoryText.enableAutoSizing = true;
        categoryText.fontSizeMin = 12f;
        categoryText.fontSizeMax = 14f;
        categoryText.alignment = TextAlignmentOptions.MidlineLeft;
        categoryText.characterSpacing = 1f;
        TryApplyTextOutline(categoryText, 0.06f, new Color(0f, 0f, 0f, 0.3f));
    }

    private static void ConfigureBuildText(TMP_Text buildText)
    {
        if (buildText == null)
        {
            return;
        }

        buildText.enableAutoSizing = true;
        buildText.fontSizeMin = 11f;
        buildText.fontSizeMax = 14f;
        buildText.characterSpacing = 2f;
        TryApplyTextOutline(buildText, 0.06f, new Color(0f, 0f, 0f, 0.3f));
    }

    private static void ConfigureRarityText(TMP_Text rarityText)
    {
        if (rarityText == null)
        {
            return;
        }

        rarityText.enableAutoSizing = true;
        rarityText.fontSizeMin = 14f;
        rarityText.fontSizeMax = 18f;
        rarityText.alignment = TextAlignmentOptions.MidlineLeft;
        rarityText.characterSpacing = 2f;
        TryApplyTextOutline(rarityText, 0.08f, new Color(0f, 0f, 0f, 0.35f));
    }

    private static void ConfigureDescriptionText(TMP_Text descriptionText)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.color = new Color(0.78f, 0.82f, 0.9f, 1f);
        descriptionText.alignment = TextAlignmentOptions.TopLeft;
        descriptionText.lineSpacing = 2f;
        descriptionText.paragraphSpacing = 0f;
        descriptionText.enableAutoSizing = true;
        descriptionText.fontSizeMin = 13f;
        descriptionText.fontSizeMax = 15f;
        descriptionText.margin = new Vector4(0f, 0f, 8f, 6f);
        descriptionText.overflowMode = TextOverflowModes.Ellipsis;
        descriptionText.maxVisibleLines = 3;
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        MerchantShrineUI.ForceClose();

        menuPlayerLevel = newLevel;
        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        remainingUpgradeSelections = 1;
        OpenUpgradeMenuInternal();
    }

    public void OpenUpgradeMenu()
    {
        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        remainingUpgradeSelections = 1;
        OpenUpgradeMenuInternal();
    }

    public void OpenChestUpgradeMenu(ChestRarity chestRarity)
    {
        OpenChestSingleCardReveal(chestRarity);
    }

    public void OpenChestLootSelection(ChestRarity chestRarity)
    {
        OpenChestSingleCardReveal(chestRarity);
    }

    public void OpenChestSingleCardReveal(ChestRarity chestRarity)
    {
        PrepareChestSingleReward(chestRarity);
        PresentChestSingleCardReveal();
    }

    public UpgradeRarity PrepareChestSingleReward(ChestRarity chestRarity)
    {
        isChestUpgradeMenu = true;
        useChestSingleCardReveal = true;
        chestRewardCollected = false;
        currentChestRarity = chestRarity;
        remainingUpgradeSelections = 1;

        PlayerStats playerStats = FindPlayerStats();

        if (playerStats != null)
        {
            menuPlayerLevel = playerStats.CurrentLevel;
        }

        AssignSingleChestReward();
        return shownUpgradeRarities[0];
    }

    public void PresentChestSingleCardReveal()
    {
        if (!ChestRevealPause.IsPaused)
        {
            ChestRevealPause.Begin();
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        itemOfferLayout.SetVisible(false);

        if (chestHeaderText != null)
        {
            chestHeaderText.gameObject.SetActive(false);
        }

        if (chestLootSelectionUI != null)
        {
            chestLootSelectionUI.Hide();
        }

        EnsureChestSingleCardRevealUi();

        if (chestSingleCardRevealUI == null)
        {
            CollectChestSingleReward();
            return;
        }

        ChestLootSelectionUI.SlotData cardData = BuildChestSingleCardData();
        string header = ChestRarityUtility.GetHeaderText(currentChestRarity);
        Color headerColor = ChestRarityUtility.GetHeaderColor(currentChestRarity);
        chestSingleCardRevealUI.Show(header, headerColor, cardData, CollectChestSingleReward);
    }

    public bool IsLevelUpOpen => levelUpPanel != null && levelUpPanel.activeSelf;

    public bool BlocksGameplayPause
    {
        get
        {
            if (IsLevelUpOpen)
            {
                return true;
            }

            if (IsAwaitingChestRewardCollect())
            {
                return true;
            }

            if (chestLootSelectionUI != null && chestLootSelectionUI.IsShowing)
            {
                return true;
            }

            if (chestSingleCardRevealUI != null && chestSingleCardRevealUI.IsShowing)
            {
                return true;
            }

            return false;
        }
    }

    public bool BlocksMerchantTrade => BlocksGameplayPause;

    public bool IsAwaitingChestRewardCollect()
    {
        return useChestSingleCardReveal && !chestRewardCollected;
    }

    public void CollectChestSingleReward()
    {
        if (!useChestSingleCardReveal || chestRewardCollected)
        {
            return;
        }

        chestRewardCollected = true;
        AudioManager.Instance?.PlayUpgradeSelect();

        PlayerStats playerStats = FindPlayerStats();

        if (chestRewardIsSpecialUpgrade)
        {
            ApplySelectedUpgrade(shownUpgradeIndices[0], shownUpgradeRarities[0]);
        }
        else
        {
            ChestStatRewardCatalog.Apply(shownChestStatReward, shownUpgradeRarities[0], playerStats);
        }

        RunStatsTracker.GetOrCreate().RecordChestOpened();

        if (chestSingleCardRevealUI != null)
        {
            chestSingleCardRevealUI.Hide();
        }

        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        chestRewardIsSpecialUpgrade = false;
        remainingUpgradeSelections = 0;
        ChestRevealPause.End();
    }

    private void AssignSingleChestReward()
    {
        chestRewardIsSpecialUpgrade = ChestSpecialRewardRoller.TryRollSpecialUpgrade(
            currentChestRarity,
            out int specialUpgradeIndex,
            out UpgradeRarity specialRarity);

        if (chestRewardIsSpecialUpgrade)
        {
            shownUpgradeIndices[0] = specialUpgradeIndex;
            shownUpgradeRarities[0] = specialRarity;
            return;
        }

        shownChestStatReward = ChestStatRewardCatalog.RollRandomReward();
        UpgradeRarity baseRarity = MapChestRarityToUpgradeRarity(currentChestRarity);
        shownUpgradeRarities[0] = ChestEconomyModifiers.ApplyLuckToChestStatRarity(baseRarity);
    }

    private ChestLootSelectionUI.SlotData BuildChestSingleCardData()
    {
        if (chestRewardIsSpecialUpgrade)
        {
            int upgradeIndex = shownUpgradeIndices[0];
            UpgradeRarity rarity = shownUpgradeRarities[0];
            int multiplier = GetRarityMultiplier(rarity);
            UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier, FindPlayerStats());
            string displayTitle = RewardCardTextFormatter.GetDisplayTitle(upgradeIndex, content.Title);
            string description = content.Description;

            if (RewardCardTextFormatter.TryGetEvolutionRequirementLine(upgradeIndex, out string requirementLine))
            {
                description = requirementLine + "\n" + description;
            }

            return ChestLootSelectionUI.SlotData.FromUpgrade(
                rarity,
                UpgradeOptionCatalog.GetCategory(upgradeIndex),
                UpgradeOptionCatalog.GetBuildType(upgradeIndex),
                displayTitle,
                description,
                content.IconKey);
        }

        return ChestLootSelectionUI.SlotData.FromChestStat(shownChestStatReward, shownUpgradeRarities[0]);
    }

    private void EnsureChestSingleCardRevealUi()
    {
        if (chestSingleCardRevealUI != null)
        {
            return;
        }

        chestSingleCardRevealUI = GetComponent<ChestSingleCardRevealUI>();

        if (chestSingleCardRevealUI == null)
        {
            chestSingleCardRevealUI = gameObject.AddComponent<ChestSingleCardRevealUI>();
        }

        Canvas canvas = levelUpPanel != null ? levelUpPanel.GetComponentInParent<Canvas>() : null;

        if (canvas == null)
        {
            canvas = UiLayoutUtility.GetGameplayCanvas();
        }

        TMP_Text fontSource = optionText1 != null ? optionText1 : chestHeaderText;
        chestSingleCardRevealUI.EnsureBuilt(canvas, fontSource);
    }

    private void OpenUpgradeMenuInternal()
    {
        Time.timeScale = 0f;

        PlayerStats playerStats = FindPlayerStats();

        if (playerStats != null)
        {
            menuPlayerLevel = playerStats.CurrentLevel;
        }

        AssignRandomUpgradeOptions();
        PresentLevelUpCards();
    }

    private void PresentLevelUpCards()
    {
        if (chestLootSelectionUI != null)
        {
            chestLootSelectionUI.Hide();
        }

        if (chestSingleCardRevealUI != null)
        {
            chestSingleCardRevealUI.Hide();
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
        }

        itemOfferLayout.SetVisible(true);
        itemOfferLayout.RefreshSidePanels(FindPlayerStats());

        EnsureUpgradeCards();
        RefreshUpgradeOptionTexts();
        UpdateChestHeaderText();
        RefreshButtonListeners();
        RefreshOfferActionButtons();
    }

    private void RefreshOfferActionButtons()
    {
        if (offerActionRow != null)
        {
            offerActionRow.SetActive(!isChestUpgradeMenu && !useChestSingleCardReveal);
        }

        if (skipOfferButtonText != null)
        {
            skipOfferButtonText.text = "SKIP " + RewardOfferActionState.SkipsRemaining;
        }

        if (refreshOfferButtonText != null)
        {
            refreshOfferButtonText.text = "REFRESH " + RewardOfferActionState.RefreshesRemaining;
        }

        if (skipOfferButton != null)
        {
            skipOfferButton.interactable = RewardOfferActionState.CanSkip();
        }

        if (refreshOfferButton != null)
        {
            refreshOfferButton.interactable = RewardOfferActionState.CanRefresh();
        }

        for (int i = 0; i < upgradeCards.Length; i++)
        {
            RefreshCardBanishButton(i);
        }
    }

    private void RefreshCardBanishButton(int cardIndex)
    {
        UpgradeCardView card = upgradeCards[cardIndex];

        if (card == null || card.BanishButton == null)
        {
            return;
        }

        int upgradeIndex = shownUpgradeIndices[cardIndex];
        bool supportsBanish = RewardOfferActionState.SupportsBanish(upgradeIndex);
        card.BanishButton.gameObject.SetActive(supportsBanish);

        if (!supportsBanish)
        {
            return;
        }

        card.BanishButton.interactable = RewardOfferActionState.CanBanish(upgradeIndex);

        if (card.BanishButtonText != null)
        {
            card.BanishButtonText.text = "BANISH";
            card.BanishButtonText.color = card.BanishButton.interactable
                ? new Color(0.92f, 0.78f, 0.80f, 1f)
                : new Color(0.55f, 0.52f, 0.56f, 0.75f);
        }
    }

    private void OnSkipOffer()
    {
        if (!RewardOfferActionState.TryConsumeSkip())
        {
            return;
        }

        AudioManager.Instance?.PlayButtonClick();
        remainingUpgradeSelections = 0;
        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        HideLevelUpPresentation();
        Time.timeScale = 1f;
        RefreshOfferActionButtons();
    }

    private void OnRefreshOffer()
    {
        if (!RewardOfferActionState.TryConsumeRefresh())
        {
            return;
        }

        AudioManager.Instance?.PlayButtonClick();
        AssignRandomUpgradeOptions();
        RefreshUpgradeOptionTexts();
        RefreshButtonListeners();
        RefreshOfferActionButtons();
        itemOfferLayout.RefreshSidePanels(FindPlayerStats());
    }

    private void OnBanishCard(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= shownUpgradeIndices.Length)
        {
            return;
        }

        int upgradeIndex = shownUpgradeIndices[cardIndex];

        if (!RewardOfferActionState.SupportsBanish(upgradeIndex))
        {
            return;
        }

        if (!RewardOfferActionState.TryBanish(upgradeIndex))
        {
            return;
        }

        AudioManager.Instance?.PlayButtonClick();
        RerollOfferSlot(cardIndex);
        RefreshUpgradeOptionTexts();
        RefreshButtonListeners();
        RefreshOfferActionButtons();
        itemOfferLayout.RefreshSidePanels(FindPlayerStats());
    }

    private void RerollOfferSlot(int slotIndex)
    {
        List<int> availableIndices = RunBuildRewardFilter.BuildPoolIndices();

        for (int i = 0; i < shownUpgradeIndices.Length; i++)
        {
            if (i == slotIndex)
            {
                continue;
            }

            int shownIndex = shownUpgradeIndices[i];

            if (shownIndex >= 0)
            {
                availableIndices.Remove(shownIndex);
            }
        }

        List<int> unpurchasedWeapons = GetUnpurchasedWeaponIndices();
        AssignWeightedOption(slotIndex, availableIndices, unpurchasedWeapons);
    }

    private void RefreshUpgradeOptionTexts()
    {
        SetCardText(0, shownUpgradeIndices[0], shownUpgradeRarities[0]);
        SetCardText(1, shownUpgradeIndices[1], shownUpgradeRarities[1]);
        SetCardText(2, shownUpgradeIndices[2], shownUpgradeRarities[2]);
    }

    private void SetCardText(int cardIndex, int upgradeIndex, UpgradeRarity rarity)
    {
        EnsureUpgradeCards();

        UpgradeCardView card = upgradeCards[cardIndex];

        if (IsBonusReward(upgradeIndex))
        {
            string bonusTitle = RewardCardTextFormatter.BuildBonusTitle(upgradeIndex, rarity);
            string bonusDescription = RewardCardTextFormatter.BuildBonusDescription(upgradeIndex, rarity);
            string iconKey = upgradeIndex == BonusRewardCoin ? "bonus_coins" : "bonus_heal";

            if (card == null)
            {
                TMP_Text fallbackText = cardIndex switch
                {
                    0 => optionText1,
                    1 => optionText2,
                    _ => optionText3
                };

                SetOptionText(fallbackText, RewardCardTextFormatter.BuildLegacyBonusLabel(upgradeIndex, rarity));
                return;
            }

            if (card.RarityText != null)
            {
                card.RarityText.text = RewardCardTextFormatter.BuildBonusHeader();
                card.RarityText.color = RewardCardTextFormatter.GetBonusHeaderColor();
            }

            if (card.CategoryText != null)
            {
                card.CategoryText.text = string.Empty;
            }

            if (card.BuildText != null)
            {
                card.BuildText.text = string.Empty;
            }

            if (card.TitleText != null)
            {
                card.TitleText.text = bonusTitle;
            }

            if (card.DescriptionText != null)
            {
                card.DescriptionText.text = bonusDescription;
            }

            ApplyCardIcon(card, iconKey, bonusTitle);
            ApplyCardRarityVisuals(card, rarity, iconKey);
            return;
        }

        if (card == null)
        {
            TMP_Text fallbackText = cardIndex switch
            {
                0 => optionText1,
                1 => optionText2,
                _ => optionText3
            };

            int rarityMultiplier = GetRarityMultiplier(rarity);
            UpgradeCardContent legacyContent = GetUpgradeCardContent(upgradeIndex, rarityMultiplier, null);

            SetOptionText(
                fallbackText,
                RewardCardTextFormatter.BuildLegacyCardLabel(
                    upgradeIndex,
                    rarity,
                    RewardCardTextFormatter.GetDisplayTitle(upgradeIndex, legacyContent.Title),
                    legacyContent.Description));
            return;
        }

        int multiplier = GetRarityMultiplier(rarity);
        PlayerStats playerStats = FindPlayerStats();
        UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier, playerStats);
        string displayTitle = RewardCardTextFormatter.GetDisplayTitle(upgradeIndex, content.Title);
        string description = content.Description;

        if (RewardCardTextFormatter.TryGetEvolutionRequirementLine(upgradeIndex, out string requirementLine))
        {
            description = requirementLine + "\n" + description;
        }

        if (card.RarityText != null)
        {
            card.RarityText.text = RewardCardTextFormatter.BuildHeader(upgradeIndex, rarity);
            card.RarityText.color = UpgradeOptionCatalog.GetRarityColor(rarity);
        }

        if (card.CategoryText != null)
        {
            card.CategoryText.text = RewardCardTextFormatter.BuildLevelLine(upgradeIndex);
            card.CategoryText.color = RewardCardTextFormatter.GetLevelLineColor();
        }

        if (card.BuildText != null)
        {
            card.BuildText.text = string.Empty;
        }

        if (card.TitleText != null)
        {
            card.TitleText.text = displayTitle;
        }

        if (card.DescriptionText != null)
        {
            card.DescriptionText.text = description;
        }

        ApplyCardIcon(card, content.IconKey, displayTitle);
        ApplyCardRarityVisuals(card, rarity, content.IconKey);
    }

    private static void ApplyCardIcon(UpgradeCardView card, string iconKey, string displayTitle)
    {
        if (card == null)
        {
            return;
        }

        if (card.IconRoot != null)
        {
            EnsureIconPlaceholder(card.IconRoot);
            CopyIconPlaceholderFont(card);
        }

        Sprite iconSprite = UpgradeCardIconUtility.TryLoadSprite(iconKey);

        if (iconSprite != null && card.IconRoot != null && card.IconImage != null)
        {
            card.IconImage.sprite = iconSprite;
            card.IconImage.enabled = true;
            card.IconImage.color = Color.white;
            card.IconRoot.gameObject.SetActive(true);
            SetIconPlaceholderLetter(card.IconRoot, string.Empty, Color.white);
            return;
        }

        if (card.IconImage != null)
        {
            card.IconImage.sprite = null;
            card.IconImage.enabled = false;
        }

        if (card.IconRoot != null)
        {
            card.IconRoot.gameObject.SetActive(true);
            SetIconPlaceholderLetter(
                card.IconRoot,
                GetIconPlaceholderLetter(displayTitle),
                new Color(0.68f, 0.72f, 0.80f, 0.95f));
        }
    }

    private static string GetIconPlaceholderLetter(string displayTitle)
    {
        if (string.IsNullOrWhiteSpace(displayTitle))
        {
            return "?";
        }

        for (int i = 0; i < displayTitle.Length; i++)
        {
            char character = displayTitle[i];

            if (char.IsLetterOrDigit(character))
            {
                return char.ToUpperInvariant(character).ToString();
            }
        }

        return "?";
    }

    private static void SetIconPlaceholderLetter(Transform iconRoot, string letter, Color color)
    {
        if (iconRoot == null)
        {
            return;
        }

        Transform letterTransform = iconRoot.Find("IconPlaceholderLetter");
        TMP_Text letterText = letterTransform != null ? letterTransform.GetComponent<TMP_Text>() : null;

        if (letterText == null)
        {
            return;
        }

        letterText.text = string.IsNullOrEmpty(letter) ? string.Empty : letter;
        letterText.color = color;
        letterText.gameObject.SetActive(!string.IsNullOrEmpty(letter));
    }

    private static void CopyIconPlaceholderFont(UpgradeCardView card)
    {
        if (card?.IconRoot == null || card.TitleText == null)
        {
            return;
        }

        Transform letterTransform = card.IconRoot.Find("IconPlaceholderLetter");
        TMP_Text letterText = letterTransform != null ? letterTransform.GetComponent<TMP_Text>() : null;

        if (letterText == null || card.TitleText.font == null)
        {
            return;
        }

        letterText.font = card.TitleText.font;

        if (card.TitleText.fontSharedMaterial != null)
        {
            letterText.fontSharedMaterial = card.TitleText.fontSharedMaterial;
        }
    }

    private static void ApplyCardRarityVisuals(UpgradeCardView card, UpgradeRarity rarity, string iconKey = null)
    {
        if (card == null)
        {
            return;
        }

        Color accent = UpgradeOptionCatalog.GetRarityColor(rarity);
        Color background = UpgradeOptionCatalog.GetRarityBackgroundColor(rarity);
        float glowAlpha = rarity switch
        {
            UpgradeRarity.Legendary => 0.34f,
            UpgradeRarity.Epic => 0.26f,
            UpgradeRarity.Rare => 0.22f,
            _ => 0.14f
        };

        if (rarity == UpgradeRarity.Common)
        {
            background = new Color(0.11f, 0.13f, 0.12f, 0.98f);
            accent = new Color(0.72f, 0.78f, 0.68f, 1f);
        }

        if (UpgradeCardIconUtility.TryGetIconFrameColor(iconKey, out Color themeColor))
        {
            accent = Color.Lerp(accent, themeColor, 0.32f);
            background = Color.Lerp(background, new Color(themeColor.r * 0.12f, themeColor.g * 0.12f, themeColor.b * 0.14f, 1f), 0.45f);
            glowAlpha = Mathf.Max(glowAlpha, 0.22f);
        }

        if (card.BackgroundImage != null)
        {
            card.BackgroundImage.color = background;
        }

        if (card.GlowImage != null)
        {
            card.GlowImage.color = new Color(accent.r, accent.g, accent.b, glowAlpha);
        }

        if (card.IconRoot != null)
        {
            Transform frameTransform = card.IconRoot.Find("IconFrame");
            Image frameImage = frameTransform != null ? frameTransform.GetComponent<Image>() : null;

            if (frameImage != null)
            {
                frameImage.color = new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.55f, 0.95f);
            }
        }

        if (card.Button == null)
        {
            return;
        }

        ColorBlock colors = card.Button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.Lerp(Color.white, accent, 0.18f);
        colors.pressedColor = Color.Lerp(Color.white, accent, 0.3f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        card.Button.colors = colors;
    }

    private void EnsureChestHeaderText()
    {
        if (chestHeaderText != null) return;
        if (levelUpPanel == null) return;

        Transform existingHeader = levelUpPanel.transform.Find("ChestHeaderText");

        if (existingHeader != null)
        {
            chestHeaderText = existingHeader.GetComponent<TMP_Text>();

            if (chestHeaderText != null) return;
        }

        GameObject headerObject = new GameObject("ChestHeaderText");
        headerObject.transform.SetParent(levelUpPanel.transform, false);

        RectTransform rectTransform = headerObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(0f, 170f), new Vector2(680f, 44f));

        TextMeshProUGUI textMesh = headerObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 28f;

        chestHeaderText = textMesh;
    }

    private void UpdateChestHeaderText()
    {
        EnsureChestHeaderText();

        if (chestHeaderText == null) return;

        if (!isChestUpgradeMenu)
        {
            chestHeaderText.gameObject.SetActive(false);
            return;
        }

        chestHeaderText.gameObject.SetActive(true);
        chestHeaderText.text = ChestRarityUtility.GetHeaderText(currentChestRarity);
        chestHeaderText.color = ChestRarityUtility.GetHeaderColor(currentChestRarity);
    }

    private void AssignRandomUpgradeOptions()
    {
        if (RunBuildRewardFilter.ShouldUseBonusFallback())
        {
            AssignBonusFallbackOptions();
            return;
        }

        List<int> availableIndices = RunBuildRewardFilter.BuildPoolIndices();
        List<int> unpurchasedWeapons = GetUnpurchasedWeaponIndices();
        WeaponBuildType activeBuild = GetPlayerWeaponBuild();

        if (!TryAssignBuildAwareOption(0, availableIndices, activeBuild, RewardCategory.Skill))
        {
            if (!TryAssignBuildAwareOption(0, availableIndices, WeaponBuildType.General, RewardCategory.Passive))
            {
                AssignWeightedOption(0, availableIndices, unpurchasedWeapons);
            }
        }

        if (!TryAssignBuildAwareOption(1, availableIndices, WeaponBuildType.General, RewardCategory.Passive))
        {
            AssignWeightedOption(1, availableIndices, unpurchasedWeapons);
        }

        AssignWeightedOption(2, availableIndices, unpurchasedWeapons);
    }

    private void AssignBonusFallbackOptions()
    {
        for (int i = 0; i < shownUpgradeIndices.Length; i++)
        {
            shownUpgradeIndices[i] = Random.value < 0.5f ? BonusRewardCoin : BonusRewardHeal;
            shownUpgradeRarities[i] = RollUpgradeRarity();
        }
    }

    private WeaponBuildType GetPlayerWeaponBuild()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return WeaponBuildType.General;
        }

        StarterWeaponController weaponController = player.GetComponent<StarterWeaponController>();

        if (weaponController == null)
        {
            return WeaponBuildType.General;
        }

        return UpgradeOptionCatalog.MapStarterWeaponToBuild(weaponController.ActiveWeapon);
    }

    private bool TryAssignBuildAwareOption(
        int slotIndex,
        List<int> availableIndices,
        WeaponBuildType preferredBuild,
        RewardCategory category)
    {
        List<int> offerableIndices = RunBuildRewardFilter.FilterEligibleCandidates(availableIndices);

        if (!UpgradeOptionCatalog.TryPickEligibleUpgradeByBuild(offerableIndices, preferredBuild, category, out int pick))
        {
            return false;
        }

        shownUpgradeIndices[slotIndex] = pick;
        shownUpgradeRarities[slotIndex] = UpgradeOptionCatalog.ResolveOfferRarity(pick);
        availableIndices.Remove(pick);
        return true;
    }

    private void AssignWeightedOption(int slotIndex, List<int> availableIndices, List<int> unpurchasedWeapons)
    {
        List<int> offerableIndices = RunBuildRewardFilter.FilterEligibleCandidates(availableIndices);

        if (offerableIndices.Count == 0)
        {
            shownUpgradeIndices[slotIndex] = Random.value < 0.5f ? BonusRewardCoin : BonusRewardHeal;
            shownUpgradeRarities[slotIndex] = RollUpgradeRarity();
            return;
        }

        int pick = PickWeightedUpgradeIndex(offerableIndices, unpurchasedWeapons, slotIndex);
        shownUpgradeIndices[slotIndex] = pick;
        shownUpgradeRarities[slotIndex] = UpgradeOptionCatalog.ResolveOfferRarity(pick);
        availableIndices.Remove(pick);
    }

    private int PickWeightedUpgradeIndex(List<int> candidates, List<int> unpurchasedWeapons, int slotIndex)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return Random.value < 0.5f ? BonusRewardCoin : BonusRewardHeal;
        }

        int totalWeight = 0;
        int[] weights = new int[candidates.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = GetUpgradePickWeight(candidates[i], unpurchasedWeapons, slotIndex);
            totalWeight += weights[i];
        }

        int roll = Random.Range(0, totalWeight);

        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];

            if (roll < 0)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private int GetUpgradePickWeight(int upgradeIndex, List<int> unpurchasedWeapons, int slotIndex)
    {
        bool earlyGame = menuPlayerLevel <= 5;
        bool midGame = menuPlayerLevel >= 6 && menuPlayerLevel <= 10;
        bool isUnpurchasedWeapon = unpurchasedWeapons.Contains(upgradeIndex);
        int weight;

        switch (upgradeIndex)
        {
            case 0:
            case 2:
            case 3:
                weight = earlyGame ? 10 : (midGame ? 6 : 5);
                break;
            case 1:
                weight = earlyGame ? 7 : (midGame ? 5 : 5);
                break;
            case 4:
            case 5:
            case 6:
                weight = earlyGame ? 4 : 5;
                if (earlyGame && isUnpurchasedWeapon)
                {
                    weight += 2;
                }

                break;
            case 7: // Legacy/disabled for now. Kept for possible future reuse.
            case 8:
            case 15:
            case 17:
                weight = earlyGame ? 2 : (midGame ? 3 : 4);

                if (earlyGame && HasAnySupportWeaponOnMenu(slotIndex))
                {
                    weight = 1;
                }
                else if (HasSupportWeaponAlreadyPicked(upgradeIndex, slotIndex))
                {
                    weight = Mathf.Max(1, weight / 2);
                }

                break;
            case 9: // Legacy/disabled for now. Kept for possible future reuse.
                weight = 1;
                break;
            case 16:
            case 18:
            case 20:
            case 22:
                weight = earlyGame ? 8 : (midGame ? 6 : 5);
                break;
            case 19:
            case 21:
                weight = earlyGame ? 2 : (midGame ? 3 : 4);
                break;
            case UpgradeOptionCatalog.GoldenMagnetIndex:
            case UpgradeOptionCatalog.StormCrownIndex:
            case UpgradeOptionCatalog.DeathMarkIndex:
            case UpgradeOptionCatalog.VoidBellIndex:
            case UpgradeOptionCatalog.DragonHeartIndex:
            case UpgradeOptionCatalog.TitanGauntletIndex:
            case UpgradeOptionCatalog.StarfallSigilIndex:
            case UpgradeOptionCatalog.CelestialShieldIndex:
            case UpgradeOptionCatalog.BloodPactIndex:
                weight = 1;
                break;
            case UpgradeOptionCatalog.HuntersEyeIndex:
            case UpgradeOptionCatalog.GravityStoneIndex:
                weight = earlyGame ? 6 : (midGame ? 5 : 4);
                break;
            case UpgradeOptionCatalog.KeyIndex:
                weight = earlyGame ? 5 : (midGame ? 4 : 3);
                break;
            case UpgradeOptionCatalog.LuckIndex:
                weight = earlyGame ? 4 : (midGame ? 3 : 2);
                break;
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
                weight = earlyGame ? 1 : (midGame ? 3 : 4);

                if (HasSkillUpgradeAlreadyPicked(slotIndex))
                {
                    weight = earlyGame ? 1 : Mathf.Max(1, weight / 2);
                }

                break;
            default:
                weight = 4;
                break;
        }

        return Mathf.Max(1, weight);
    }

    private static bool IsGeneralSupportSkill(int upgradeIndex)
    {
        return upgradeIndex == 8
            || upgradeIndex == UpgradeOptionCatalog.FrostSigilIndex
            || upgradeIndex == UpgradeOptionCatalog.ShadowRiftIndex;
    }

    private bool HasAnySupportWeaponOnMenu(int slotIndex)
    {
        for (int i = 0; i < slotIndex; i++)
        {
            int picked = shownUpgradeIndices[i];

            if (IsGeneralSupportSkill(picked))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSupportWeaponAlreadyPicked(int candidateIndex, int slotIndex)
    {
        if (!IsGeneralSupportSkill(candidateIndex))
        {
            return false;
        }

        for (int i = 0; i < slotIndex; i++)
        {
            int picked = shownUpgradeIndices[i];

            if (IsGeneralSupportSkill(picked) && picked != candidateIndex)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSkillUpgradeAlreadyPicked(int slotIndex)
    {
        for (int i = 0; i < slotIndex; i++)
        {
            if (shownUpgradeIndices[i] >= 10)
            {
                return true;
            }
        }

        return false;
    }

    private UpgradeRarity RollUpgradeRarity()
    {
        return UpgradeOptionCatalog.RollDisplayRarity();
    }

    private static int GetRarityMultiplier(UpgradeRarity rarity)
    {
        return UpgradeOptionCatalog.GetRarityMultiplier(rarity);
    }

    private static UpgradeCardContent MakeContent(string title, string description, string iconKey)
    {
        return new UpgradeCardContent(title, description, iconKey);
    }

    private static UpgradeCardContent GetUpgradeCardContent(int upgradeIndex, int multiplier, PlayerStats playerStats)
    {
        switch (upgradeIndex)
        {
            case 0:
                return MakeContent(
                    "Rapid Mechanism",
                    GetFireRateDescription(multiplier),
                    "rapid_mechanism");
            case 1:
                return MakeContent(
                    "Swift Projectiles",
                    GetProjectileSpeedDescription(multiplier),
                    "swift_projectiles");
            case 2:
                return MakeContent(
                    "Magnet Sense",
                    GetXpAttractionDescription(multiplier),
                    "magnet_sense");
            case 3:
                return MakeContent(
                    "Sharp Instinct",
                    GetDamageDescription(multiplier),
                    "sharp_instinct");
            case 4:
                return GetSpreadShotContent(playerStats);
            case 5:
                return GetPiercingShotContent(playerStats);
            case 6:
                return GetOrbitingOrbContent(playerStats);
            case 7:
                return GetRocketLauncherContent(multiplier, playerStats);
            case 8:
                return GetChainLightningContent(multiplier, playerStats);
            case 9:
                return GetLaserBeamContent(multiplier, playerStats);
            case 10:
                return MakeContent(
                    "Meteor Focus",
                    GetMegaMeteorCooldownDescription(multiplier),
                    "meteor_focus");
            case 11:
                return MakeContent(
                    "Whirlwind Training",
                    GetSwordSkillCooldownDescription(multiplier),
                    "whirlwind_training");
            case 12:
                return MakeContent(
                    "Arrow Storm",
                    GetArrowRainDamageDescription(multiplier),
                    "arrow_storm");
            case 13:
                return MakeContent(
                    "Inferno Ritual",
                    GetMegaMeteorDamageDescription(multiplier),
                    "inferno_ritual");
            case 14:
                return MakeContent(
                    "Blade Tempest",
                    GetSwordSkillDamageDescription(multiplier),
                    "blade_tempest");
            case 15:
                return MakeContent(
                    "Frost Sigil",
                    "Drops an icy circle under a nearby enemy cluster. Deals damage and slows.",
                    "frost_sigil");
            case 16:
                return MakeContent(
                    "Cryo Core",
                    "Boosts Frost Sigil damage, radius, and slow strength.",
                    "cryo_core");
            case 17:
                return MakeContent(
                    "Shadow Rift",
                    "Opens a void field on nearby enemies that deals tick damage.",
                    "shadow_rift");
            case 18:
                return MakeContent(
                    "Void Catalyst",
                    "Boosts Shadow Rift damage, duration, and radius.",
                    "void_catalyst");
            case 19:
                return MakeContent(
                    "Shrapnel Storm",
                    "Strengthens Blunderbuss attacks with extra shrapnel and pellet pressure.",
                    "shrapnel_storm");
            case 20:
                return MakeContent(
                    "Powder Keg",
                    "Supports Blast Shell and explosive Blunderbuss upgrades.",
                    "powder_keg");
            case 21:
                return MakeContent(
                    "Storm Conduit",
                    "Strengthens Thunder Spear pierce and chain potential.",
                    "storm_conduit");
            case 22:
                return MakeContent(
                    "Conductive Core",
                    "Supports Thunder Spear chain and shock upgrades.",
                    "conductive_core");
            case 23:
                return MakeContent(
                    "Golden Magnet",
                    "Pulls all EXP and coins from across the map.",
                    "magnet_sense");
            case 24:
                return MakeContent(
                    "Storm Crown",
                    "Every 6s, lightning strikes up to 3 nearby enemies.",
                    "chain");
            case 25:
                return MakeContent(
                    "Death Mark",
                    "Your attacks have a 2% chance to instantly kill nearby normal enemies.",
                    "sharp_instinct");
            case 26:
                return MakeContent(
                    "Hunter's Eye",
                    "Improves precision and mark-based effects.",
                    "sharp_instinct");
            case 27:
                return MakeContent(
                    "Gravity Stone",
                    "Strengthens area and gravity-based effects.",
                    "void_catalyst");
            case 28:
                return MakeContent(
                    "Void Bell",
                    "Every 10s, releases a void pulse that damages nearby enemies.",
                    "shadow_rift");
            case UpgradeOptionCatalog.KeyIndex:
                return MakeContent(
                    "Key",
                    "Grants a small chance to open paid chests for free.",
                    "magnet_sense");
            case UpgradeOptionCatalog.LuckIndex:
                return MakeContent(
                    "Luck",
                    "Improves chest reward odds.",
                    "sharp_instinct");
            case UpgradeOptionCatalog.DragonHeartIndex:
                return MakeContent(
                    "Dragon Heart",
                    "Every 8s, unleashes a fire burst in front of you.",
                    "inferno");
            case UpgradeOptionCatalog.TitanGauntletIndex:
                return MakeContent(
                    "Titan Gauntlet",
                    "Every 9s, stomps nearby enemies with crushing force.",
                    "blade");
            case UpgradeOptionCatalog.StarfallSigilIndex:
                return MakeContent(
                    "Starfall Sigil",
                    "Every 12s, calls down star strikes on nearby enemies.",
                    "meteor");
            case UpgradeOptionCatalog.CelestialShieldIndex:
                return MakeContent(
                    "Celestial Shield",
                    "Periodically protects you from danger.",
                    "orbit");
            case UpgradeOptionCatalog.BloodPactIndex:
                return MakeContent(
                    "Blood Pact",
                    "Gain more power as your health gets lower.",
                    "sharp_instinct");
            default:
                return MakeContent(string.Empty, string.Empty, string.Empty);
        }
    }

    private static string GetFireRateDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Basic attack cooldowns -40%.",
            3 => "Basic attack cooldowns -60%.",
            _ => "Basic attack cooldowns -20%."
        };
    }

    private static string GetProjectileSpeedDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Bow and projectile speed +50%.",
            3 => "Bow and projectile speed +75%.",
            _ => "Bow and projectile speed +25%."
        };
    }

    private static string GetXpAttractionDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "General pickup range +60%.",
            3 => "General pickup range +90%.",
            _ => "General pickup range +30%."
        };
    }

    private static string GetDamageDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "General damage +2.",
            3 => "General damage +3.",
            _ => "General damage +1."
        };
    }

    private static string GetMegaMeteorCooldownDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Fire Staff Mega Meteor cooldown -24%.",
            3 => "Fire Staff Mega Meteor cooldown -36%.",
            _ => "Fire Staff Mega Meteor cooldown -12%."
        };
    }

    private static string GetSwordSkillCooldownDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Sword Whirlwind cooldown -24%.",
            3 => "Sword Whirlwind cooldown -36%.",
            _ => "Sword Whirlwind cooldown -12%."
        };
    }

    private static string GetArrowRainDamageDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Bow Arrow Rain damage +30%.",
            3 => "Bow Arrow Rain damage +45%.",
            _ => "Bow Arrow Rain damage +15%."
        };
    }

    private static string GetMegaMeteorDamageDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Fire Staff Mega Meteor damage +30%.",
            3 => "Fire Staff Mega Meteor damage +45%.",
            _ => "Fire Staff Mega Meteor damage +15%."
        };
    }

    private static string GetSwordSkillDamageDescription(int multiplier)
    {
        return multiplier switch
        {
            2 => "Sword Whirlwind damage +30%.",
            3 => "Sword Whirlwind damage +45%.",
            _ => "Sword Whirlwind damage +15%."
        };
    }

    private static string GetStackLevelDescription(string upgradeName, int multiplier)
    {
        return multiplier switch
        {
            4 => $"Legendary: {upgradeName} level +4.",
            3 => $"Epic: {upgradeName} level +3.",
            2 => $"Rare: {upgradeName} level +2.",
            _ => $"Common: {upgradeName} level +1."
        };
    }

    private static UpgradeCardContent GetSpreadShotContent(PlayerStats playerStats)
    {
        if (playerStats == null || !playerStats.SpreadShotUnlocked)
        {
            return MakeContent(
                "Spread Shot",
                "Bow projectiles split into multiple shots.",
                "spread_shot");
        }

        return MakeContent(
            "Spread Shot+",
            "Bow projectile spread improves.",
            "spread_shot");
    }

    private static UpgradeCardContent GetPiercingShotContent(PlayerStats playerStats)
    {
        if (playerStats == null || playerStats.PierceCount <= 0)
        {
            return MakeContent(
                "Piercing Shot",
                "Support projectiles pierce through more enemies.",
                "piercing_shot");
        }

        return MakeContent(
            "Piercing Shot+",
            "Support projectile pierce improves.",
            "piercing_shot");
    }

    private static UpgradeCardContent GetOrbitingOrbContent(PlayerStats playerStats)
    {
        if (playerStats == null || playerStats.OrbitOrbCount <= 0)
        {
            return MakeContent(
                "Orbiting Orb",
                "Fire Staff gains orbiting flame power.",
                "orbiting_orb");
        }

        return MakeContent(
            "Orbiting Orb+",
            "Adds +1 orbiting flame orb.",
            "orbiting_orb");
    }

    private static UpgradeCardContent GetRocketLauncherContent(int multiplier, PlayerStats playerStats)
    {
        bool isUnlock = playerStats == null || !playerStats.RocketLauncherUnlocked;

        if (isUnlock)
        {
            string description = "Unlocks a slow rocket that explodes on impact.";

            if (multiplier > 1)
            {
                description += "\n" + GetStackLevelDescription("Rocket", multiplier);
            }

            return MakeContent("Rocket Launcher", description, "rocket_launcher");
        }

        if (multiplier > 1)
        {
            return MakeContent(
                "Rocket Launcher+",
                "Improves rocket level and explosion power.\n" + GetStackLevelDescription("Rocket", multiplier),
                "rocket_launcher");
        }

        return MakeContent(
            "Rocket Launcher+",
            "Improves rocket level and explosion power.",
            "rocket_launcher");
    }

    private static UpgradeCardContent GetChainLightningContent(int multiplier, PlayerStats playerStats)
    {
        bool isUnlock = playerStats == null || !playerStats.ChainLightningUnlocked;

        if (isUnlock)
        {
            string description = "Unlocks lightning that jumps between nearby enemies.";

            if (multiplier > 1)
            {
                description += "\n" + GetStackLevelDescription("Chain Lightning", multiplier);
            }

            return MakeContent("Chain Lightning", description, "chain_lightning");
        }

        if (multiplier > 1)
        {
            return MakeContent(
                "Chain Lightning+",
                "Increases chain lightning level and target count.\n" + GetStackLevelDescription("Chain Lightning", multiplier),
                "chain_lightning");
        }

        return MakeContent(
            "Chain Lightning+",
            "Increases chain lightning level and target count.",
            "chain_lightning");
    }

    private static UpgradeCardContent GetLaserBeamContent(int multiplier, PlayerStats playerStats)
    {
        bool isUnlock = playerStats == null || !playerStats.LaserBeamUnlocked;

        if (isUnlock)
        {
            string description = "Unlocks a short-range beam that pierces enemies.";

            if (multiplier > 1)
            {
                description += "\n" + GetStackLevelDescription("Laser Beam", multiplier);
            }

            return MakeContent("Laser Beam", description, "laser_beam");
        }

        if (multiplier > 1)
        {
            return MakeContent(
                "Laser Beam+",
                "Increases laser level and beam range.\n" + GetStackLevelDescription("Laser Beam", multiplier),
                "laser_beam");
        }

        return MakeContent(
            "Laser Beam+",
            "Increases laser level and beam range.",
            "laser_beam");
    }

    private List<int> GetUnpurchasedWeaponIndices()
    {
        List<int> weaponIndices = new List<int>();
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats != null && !playerStats.SpreadShotUnlocked)
        {
            weaponIndices.Add(4);
        }

        if (playerStats != null && playerStats.PierceCount <= 0)
        {
            weaponIndices.Add(5);
        }

        if (playerStats != null && playerStats.OrbitOrbCount <= 0)
        {
            weaponIndices.Add(6);
        }

        if (playerStats != null && !playerStats.ChainLightningUnlocked)
        {
            weaponIndices.Add(8);
        }

        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null || !tracker.IsTrackedUpgrade(UpgradeOptionCatalog.FrostSigilIndex))
        {
            weaponIndices.Add(UpgradeOptionCatalog.FrostSigilIndex);
        }

        if (tracker == null || !tracker.IsTrackedUpgrade(UpgradeOptionCatalog.ShadowRiftIndex))
        {
            weaponIndices.Add(UpgradeOptionCatalog.ShadowRiftIndex);
        }

        return weaponIndices;
    }

    private PlayerStats FindPlayerStats()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        return player != null ? player.GetComponent<PlayerStats>() : null;
    }

    private void RefreshButtonListeners()
    {
        if (optionButton1 != null)
        {
            optionButton1.onClick.RemoveAllListeners();
            optionButton1.onClick.AddListener(() => SelectUpgrade(0));
        }

        if (optionButton2 != null)
        {
            optionButton2.onClick.RemoveAllListeners();
            optionButton2.onClick.AddListener(() => SelectUpgrade(1));
        }

        if (optionButton3 != null)
        {
            optionButton3.onClick.RemoveAllListeners();
            optionButton3.onClick.AddListener(() => SelectUpgrade(2));
        }

        for (int i = 0; i < upgradeCards.Length; i++)
        {
            UpgradeCardView card = upgradeCards[i];

            if (card?.BanishButton == null)
            {
                continue;
            }

            int cardIndex = i;
            card.BanishButton.onClick.RemoveAllListeners();
            card.BanishButton.onClick.AddListener(() => OnBanishCard(cardIndex));
        }
    }

    private void SetOptionText(TMP_Text text, string value)
    {
        if (text == null) return;

        text.text = value;
    }

    private void SelectUpgrade(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= shownUpgradeIndices.Length) return;

        int upgradeIndex = shownUpgradeIndices[optionIndex];
        UpgradeRarity rarity = shownUpgradeRarities[optionIndex];
        AudioManager.Instance?.PlayUpgradeSelect();
        ApplySelectedUpgrade(upgradeIndex, rarity);

        remainingUpgradeSelections--;

        if (remainingUpgradeSelections > 0)
        {
            AssignRandomUpgradeOptions();
            RefreshUpgradeOptionTexts();
            RefreshButtonListeners();
            RefreshOfferActionButtons();
            itemOfferLayout.RefreshSidePanels(FindPlayerStats());
            return;
        }

        if (chestLootSelectionUI != null)
        {
            chestLootSelectionUI.Hide();
        }

        if (chestSingleCardRevealUI != null)
        {
            chestSingleCardRevealUI.Hide();
        }

        HideLevelUpPresentation();

        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        Time.timeScale = 1f;
    }

    private void HideLevelUpPresentation()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        itemOfferLayout.SetVisible(false);

        if (chestHeaderText != null)
        {
            chestHeaderText.gameObject.SetActive(false);
        }

        if (offerActionRow != null)
        {
            offerActionRow.SetActive(false);
        }
    }

    private void ApplySelectedUpgrade(int upgradeIndex, UpgradeRarity rarity)
    {
        if (upgradeIndex == BonusRewardCoin)
        {
            ApplyBonusCoins(rarity);
            return;
        }

        if (upgradeIndex == BonusRewardHeal)
        {
            ApplyBonusHeal(rarity);
            return;
        }

        int multiplier = GetRarityMultiplier(rarity);

        switch (upgradeIndex)
        {
            case 0:
                ApplyFireRateUpgrade(0.2f * multiplier);
                break;
            case 1:
                ApplyProjectileSpeedUpgrade(0.25f * multiplier);
                break;
            case 2:
                ApplyXPAttractionUpgrade(0.30f * multiplier);
                break;
            case 3:
                ApplyDamageUpgrade(multiplier);
                break;
            case 4:
                ApplySpreadShotUpgrade();
                break;
            case 5:
                ApplyPiercingShotUpgrade();
                break;
            case 6:
                ApplyOrbitingOrbUpgrade();
                break;
            case 7: // Legacy/disabled for now. Kept for possible future reuse.
                ApplyRocketLauncherUpgrade(multiplier);
                break;
            case 8:
                ApplyChainLightningUpgrade(multiplier);
                break;
            case 9: // Legacy/disabled for now. Kept for possible future reuse.
                ApplyLaserBeamUpgrade(multiplier);
                break;
            case 10:
                ApplyMegaMeteorCooldownUpgrade(0.12f * multiplier);
                break;
            case 11:
                ApplySwordSkillCooldownUpgrade(0.12f * multiplier);
                break;
            case 12:
                ApplyArrowRainDamageUpgrade(0.15f * multiplier);
                break;
            case 13:
                ApplyMegaMeteorDamageUpgrade(0.15f * multiplier);
                break;
            case 14:
                ApplySwordSkillDamageUpgrade(0.15f * multiplier);
                break;
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case UpgradeOptionCatalog.KeyIndex:
            case UpgradeOptionCatalog.LuckIndex:
            case UpgradeOptionCatalog.DragonHeartIndex:
            case UpgradeOptionCatalog.TitanGauntletIndex:
            case UpgradeOptionCatalog.StarfallSigilIndex:
            case UpgradeOptionCatalog.CelestialShieldIndex:
            case UpgradeOptionCatalog.BloodPactIndex:
                break;
        }

        if (upgradeIndex == UpgradeOptionCatalog.StormCrownIndex
            || upgradeIndex == UpgradeOptionCatalog.VoidBellIndex
            || upgradeIndex == UpgradeOptionCatalog.DragonHeartIndex
            || upgradeIndex == UpgradeOptionCatalog.TitanGauntletIndex
            || upgradeIndex == UpgradeOptionCatalog.StarfallSigilIndex
            || upgradeIndex == UpgradeOptionCatalog.CelestialShieldIndex)
        {
            LegendaryPassiveEffectManager.GetOrCreate();
        }

        if (upgradeIndex >= 0)
        {
            RunBuildTracker.GetOrCreate().RecordUpgrade(upgradeIndex);
        }
    }

    private static bool IsBonusReward(int upgradeIndex)
    {
        return upgradeIndex == BonusRewardCoin || upgradeIndex == BonusRewardHeal;
    }

    private void ApplyBonusCoins(UpgradeRarity rarity)
    {
        PlayerStats playerStats = FindPlayerStats();
        int amount = 25 * GetRarityMultiplier(rarity);
        playerStats?.AddCoins(amount);
    }

    private void ApplyBonusHeal(UpgradeRarity rarity)
    {
        PlayerStats playerStats = FindPlayerStats();
        int amount = 25 * GetRarityMultiplier(rarity);
        playerStats?.HealAmount(amount);
    }

    private static UpgradeRarity MapChestRarityToUpgradeRarity(ChestRarity chestRarity)
    {
        return chestRarity switch
        {
            ChestRarity.Epic => UpgradeRarity.Epic,
            ChestRarity.Rare => UpgradeRarity.Rare,
            _ => UpgradeRarity.Common
        };
    }

    private void ApplyFireRateUpgrade(float percent)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();

        if (weaponManager != null)
        {
            weaponManager.IncreaseFireRate(percent);
        }

        PlayerStats stats = player.GetComponent<PlayerStats>();
        stats?.IncreaseStarterWeaponFireRate(percent);
    }

    private void ApplyProjectileSpeedUpgrade(float percent)
    {
        UpgradeManager upgradeManager = UpgradeManager.GetOrCreateInstance();

        if (upgradeManager != null)
        {
            upgradeManager.IncreaseProjectileSpeed(percent);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            return;
        }

        PlayerStats stats = player.GetComponent<PlayerStats>();
        stats?.IncreaseStarterProjectileSpeed(percent);
    }

    private void ApplyXPAttractionUpgrade(float percent)
    {
        UpgradeManager upgradeManager = UpgradeManager.GetOrCreateInstance();

        if (upgradeManager == null)
        {
            Debug.LogError("UpgradeManager bulunamadı");
            return;
        }

        upgradeManager.IncreaseXPAttraction(percent);
    }

    private void ApplyDamageUpgrade(int amount)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats == null) return;

        playerStats.damage += amount;
    }

    private void ApplyMegaMeteorCooldownUpgrade(float percent)
    {
        PlayerStats playerStats = FindPlayerStats();
        playerStats?.ReduceMegaMeteorCooldown(percent);
    }

    private void ApplySwordSkillCooldownUpgrade(float percent)
    {
        PlayerStats playerStats = FindPlayerStats();
        playerStats?.ReduceSwordSkillCooldown(percent);
    }

    private void ApplyArrowRainDamageUpgrade(float percent)
    {
        PlayerStats playerStats = FindPlayerStats();
        playerStats?.IncreaseArrowRainDamage(percent);
    }

    private void ApplyMegaMeteorDamageUpgrade(float percent)
    {
        PlayerStats playerStats = FindPlayerStats();
        playerStats?.IncreaseMegaMeteorDamage(percent);
    }

    private void ApplySwordSkillDamageUpgrade(float percent)
    {
        PlayerStats playerStats = FindPlayerStats();
        playerStats?.IncreaseSwordSkillDamage(percent);
    }

    private void ApplySpreadShotUpgrade()
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeSpreadShot();
    }

    private void ApplyPiercingShotUpgrade()
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradePiercingShot();
    }

    private void ApplyOrbitingOrbUpgrade()
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeOrbitingOrb();

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();

        if (weaponManager == null) return;

        weaponManager.RefreshOrbitWeapon();
    }

    private void ApplyRocketLauncherUpgrade(int multiplier)
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        for (int i = 0; i < multiplier; i++)
        {
            playerStats.UpgradeRocketLauncher();
        }
    }

    private void ApplyChainLightningUpgrade(int multiplier)
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        for (int i = 0; i < multiplier; i++)
        {
            playerStats.UpgradeChainLightning();
        }
    }

    private void ApplyLaserBeamUpgrade(int multiplier)
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        for (int i = 0; i < multiplier; i++)
        {
            playerStats.UpgradeLaserBeam();
        }
    }
}
