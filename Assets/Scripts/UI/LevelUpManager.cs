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

        RectTransform panelRect = levelUpPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(920f, 460f));

        Image panelImage = levelUpPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = new Color(0.03f, 0.04f, 0.07f, 0.96f);
        }

        LayoutUpgradeButton(optionButton1, -290f);
        LayoutUpgradeButton(optionButton2, 0f);
        LayoutUpgradeButton(optionButton3, 290f);

        if (chestHeaderText != null)
        {
            UiLayoutUtility.SetAnchorCenter(chestHeaderText.rectTransform, new Vector2(0f, 170f), new Vector2(680f, 44f));
            chestHeaderText.fontSize = 30f;
        }
    }

    private static void LayoutUpgradeButton(Button button, float xOffset)
    {
        if (button == null) return;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(xOffset, -6f), new Vector2(260f, 320f));

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
            RarityText = CreateCardText(buttonRect, legacyText, "RarityText", new Vector2(0f, 98f), new Vector2(228f, 24f), 19f, FontStyles.Bold),
            CategoryText = CreateCardText(buttonRect, legacyText, "CategoryText", new Vector2(0f, 74f), new Vector2(228f, 20f), 15f, FontStyles.Bold),
            BuildText = CreateCardText(buttonRect, legacyText, "BuildText", new Vector2(0f, 52f), new Vector2(228f, 20f), 14f, FontStyles.Bold),
            TitleText = CreateCardText(buttonRect, legacyText, "TitleText", new Vector2(0f, 18f), new Vector2(228f, 88f), 32f, FontStyles.Bold),
            DescriptionText = CreateCardText(buttonRect, legacyText, "DescriptionText", new Vector2(0f, -68f), new Vector2(228f, 120f), 20f, FontStyles.Normal)
        };

        ConfigureDescriptionText(upgradeCards[index].DescriptionText);
        ConfigureTitleText(upgradeCards[index].TitleText);
        ConfigureRarityText(upgradeCards[index].RarityText);
        ConfigureCategoryText(upgradeCards[index].CategoryText);
        ConfigureBuildText(upgradeCards[index].BuildText);
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

            Transform existingFallback = existingIconRoot.Find("IconFallbackText");

            if (existingFallback != null)
            {
                existingFallback.gameObject.SetActive(false);
            }

            iconRoot.gameObject.SetActive(false);
            return;
        }

        GameObject iconRootObject = new GameObject("CardIconRoot");
        iconRootObject.transform.SetParent(buttonRect, false);
        iconRootObject.SetActive(false);

        RectTransform iconRootRect = iconRootObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(iconRootRect, new Vector2(0f, 124f), new Vector2(44f, 44f));

        GameObject iconImageObject = new GameObject("IconImage");
        iconImageObject.transform.SetParent(iconRootObject.transform, false);

        RectTransform iconImageRect = iconImageObject.AddComponent<RectTransform>();
        iconImageRect.anchorMin = Vector2.zero;
        iconImageRect.anchorMax = Vector2.one;
        iconImageRect.offsetMin = Vector2.zero;
        iconImageRect.offsetMax = Vector2.zero;

        iconImage = iconImageObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = true;

        iconRoot = iconRootObject.transform;
    }

    private static TMP_Text CreateCardText(
        RectTransform parent,
        TMP_Text fontSource,
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
        CopyTmpFontFrom(fontSource, textMesh);
        textMesh.alignment = TextAlignmentOptions.Center;
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
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 26f;
        titleText.fontSizeMax = 34f;
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
        categoryText.fontSizeMax = 15f;
        categoryText.characterSpacing = 3f;
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
        rarityText.fontSizeMin = 16f;
        rarityText.fontSizeMax = 20f;
        rarityText.characterSpacing = 4f;
        TryApplyTextOutline(rarityText, 0.08f, new Color(0f, 0f, 0f, 0.35f));
    }

    private static void ConfigureDescriptionText(TMP_Text descriptionText)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.color = new Color(0.78f, 0.82f, 0.9f, 1f);
        descriptionText.lineSpacing = 6f;
        descriptionText.paragraphSpacing = 2f;
        descriptionText.enableAutoSizing = true;
        descriptionText.fontSizeMin = 16f;
        descriptionText.fontSizeMax = 20f;
        descriptionText.margin = new Vector4(8f, 0f, 8f, 0f);
    }

    public void OnPlayerLevelUp(int newLevel)
    {
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
        ApplySelectedUpgrade(shownUpgradeIndices[0], shownUpgradeRarities[0]);

        if (chestSingleCardRevealUI != null)
        {
            chestSingleCardRevealUI.Hide();
        }

        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        remainingUpgradeSelections = 0;
        ChestRevealPause.End();
    }

    private void AssignSingleChestReward()
    {
        List<int> availableIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        List<int> unpurchasedWeapons = GetUnpurchasedWeaponIndices();
        List<int> offerableIndices = GetBuildLockEligibleCandidates(availableIndices);
        int pick = PickWeightedUpgradeIndex(offerableIndices, unpurchasedWeapons, 0);
        shownUpgradeIndices[0] = pick;
        shownUpgradeRarities[0] = RollUpgradeRarity();
    }

    private ChestLootSelectionUI.SlotData BuildChestSingleCardData()
    {
        PlayerStats playerStats = FindPlayerStats();
        int upgradeIndex = shownUpgradeIndices[0];
        UpgradeRarity rarity = shownUpgradeRarities[0];
        int multiplier = GetRarityMultiplier(rarity);
        UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier, playerStats);

        return ChestLootSelectionUI.SlotData.FromUpgrade(
            rarity,
            UpgradeOptionCatalog.GetCategory(upgradeIndex),
            UpgradeOptionCatalog.GetBuildType(upgradeIndex),
            content.Title,
            content.Description,
            content.IconKey);
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

        EnsureUpgradeCards();
        RefreshUpgradeOptionTexts();
        UpdateChestHeaderText();
        RefreshButtonListeners();
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

        if (card == null)
        {
            TMP_Text fallbackText = cardIndex switch
            {
                0 => optionText1,
                1 => optionText2,
                _ => optionText3
            };

            SetOptionText(fallbackText, BuildLegacyOptionLabel(upgradeIndex, rarity));
            return;
        }

        int multiplier = GetRarityMultiplier(rarity);
        PlayerStats playerStats = FindPlayerStats();
        UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier, playerStats);
        RewardCategory category = UpgradeOptionCatalog.GetCategory(upgradeIndex);
        WeaponBuildType buildType = UpgradeOptionCatalog.GetBuildType(upgradeIndex);

        if (card.RarityText != null)
        {
            card.RarityText.text = UpgradeOptionCatalog.GetRarityLabel(rarity);
            card.RarityText.color = UpgradeOptionCatalog.GetRarityColor(rarity);
        }

        if (card.CategoryText != null)
        {
            card.CategoryText.text = UpgradeOptionCatalog.GetCategoryLabel(category);
            card.CategoryText.color = UpgradeOptionCatalog.GetCategoryColor(category);
        }

        if (card.BuildText != null)
        {
            card.BuildText.text = UpgradeOptionCatalog.GetBuildLabel(buildType);
            card.BuildText.color = UpgradeOptionCatalog.GetBuildColor(buildType);
        }

        if (card.TitleText != null)
        {
            card.TitleText.text = content.Title;
        }

        if (card.DescriptionText != null)
        {
            card.DescriptionText.text = content.Description;
        }

        ApplyCardIcon(card, content.IconKey);
        ApplyCardRarityVisuals(card, rarity);
    }

    private static void ApplyCardIcon(UpgradeCardView card, string iconKey)
    {
        if (card == null)
        {
            return;
        }

        Sprite iconSprite = UpgradeCardIconUtility.TryLoadSprite(iconKey);

        if (iconSprite != null && card.IconRoot != null && card.IconImage != null)
        {
            card.IconImage.sprite = iconSprite;
            card.IconImage.enabled = true;
            card.IconRoot.gameObject.SetActive(true);
            return;
        }

        if (card.IconImage != null)
        {
            card.IconImage.sprite = null;
        }

        if (card.IconRoot != null)
        {
            card.IconRoot.gameObject.SetActive(false);
        }
    }

    private static void ApplyCardRarityVisuals(UpgradeCardView card, UpgradeRarity rarity)
    {
        if (card == null)
        {
            return;
        }

        Color accent = UpgradeOptionCatalog.GetRarityColor(rarity);
        Color background = UpgradeOptionCatalog.GetRarityBackgroundColor(rarity);
        float glowAlpha = rarity switch
        {
            UpgradeRarity.Legendary => 0.3f,
            UpgradeRarity.Epic => 0.24f,
            UpgradeRarity.Rare => 0.2f,
            _ => 0.1f
        };

        if (card.BackgroundImage != null)
        {
            card.BackgroundImage.color = background;
        }

        if (card.GlowImage != null)
        {
            card.GlowImage.color = new Color(accent.r, accent.g, accent.b, glowAlpha);
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
        List<int> availableIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        List<int> unpurchasedWeapons = GetUnpurchasedWeaponIndices();
        WeaponBuildType activeBuild = GetPlayerWeaponBuild();

        if (!TryAssignBuildAwareOption(0, availableIndices, activeBuild, RewardCategory.Skill))
        {
            AssignWeightedOption(0, availableIndices, unpurchasedWeapons);
        }

        if (!TryAssignBuildAwareOption(1, availableIndices, WeaponBuildType.General, RewardCategory.Passive))
        {
            AssignWeightedOption(1, availableIndices, unpurchasedWeapons);
        }

        AssignWeightedOption(2, availableIndices, unpurchasedWeapons);
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
        List<int> offerableIndices = GetBuildLockEligibleCandidates(availableIndices);

        if (!UpgradeOptionCatalog.TryPickEligibleUpgradeByBuild(offerableIndices, preferredBuild, category, out int pick))
        {
            return false;
        }

        shownUpgradeIndices[slotIndex] = pick;
        shownUpgradeRarities[slotIndex] = RollUpgradeRarity();
        availableIndices.Remove(pick);
        return true;
    }

    private void AssignWeightedOption(int slotIndex, List<int> availableIndices, List<int> unpurchasedWeapons)
    {
        List<int> offerableIndices = GetBuildLockEligibleCandidates(availableIndices);
        int pick = PickWeightedUpgradeIndex(offerableIndices, unpurchasedWeapons, slotIndex);
        shownUpgradeIndices[slotIndex] = pick;
        shownUpgradeRarities[slotIndex] = RollUpgradeRarity();
        availableIndices.Remove(pick);
    }

    private static List<int> GetBuildLockEligibleCandidates(List<int> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return candidates;
        }

        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        List<int> filtered = new List<int>(candidates.Count);

        for (int i = 0; i < candidates.Count; i++)
        {
            if (tracker.CanOfferUpgrade(candidates[i]))
            {
                filtered.Add(candidates[i]);
            }
        }

        return filtered.Count > 0 ? filtered : candidates;
    }

    private int PickWeightedUpgradeIndex(List<int> candidates, List<int> unpurchasedWeapons, int slotIndex)
    {
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
            case 7:
            case 8:
            case 9:
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

    private bool HasAnySupportWeaponOnMenu(int slotIndex)
    {
        for (int i = 0; i < slotIndex; i++)
        {
            int picked = shownUpgradeIndices[i];

            if (picked >= 7 && picked <= 9)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasSupportWeaponAlreadyPicked(int candidateIndex, int slotIndex)
    {
        if (candidateIndex < 7 || candidateIndex > 9)
        {
            return false;
        }

        for (int i = 0; i < slotIndex; i++)
        {
            int picked = shownUpgradeIndices[i];

            if (picked >= 7 && picked <= 9 && picked != candidateIndex)
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

    private static string BuildLegacyOptionLabel(int upgradeIndex, UpgradeRarity rarity)
    {
        int multiplier = GetRarityMultiplier(rarity);
        UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier, null);
        string header = ChestLootSelectionUI.BuildRewardHeaderLabel(
            UpgradeOptionCatalog.GetRarityLabel(rarity),
            UpgradeOptionCatalog.GetCategoryLabel(UpgradeOptionCatalog.GetCategory(upgradeIndex)),
            UpgradeOptionCatalog.GetBuildLabel(upgradeIndex));
        return $"{header}\n{content.Title}\n{content.Description}";
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

        if (playerStats != null && !playerStats.RocketLauncherUnlocked)
        {
            weaponIndices.Add(7);
        }

        if (playerStats != null && !playerStats.ChainLightningUnlocked)
        {
            weaponIndices.Add(8);
        }

        if (playerStats != null && !playerStats.LaserBeamUnlocked)
        {
            weaponIndices.Add(9);
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

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        if (chestHeaderText != null)
        {
            chestHeaderText.gameObject.SetActive(false);
        }

        isChestUpgradeMenu = false;
        useChestSingleCardReveal = false;
        Time.timeScale = 1f;
    }

    private void ApplySelectedUpgrade(int upgradeIndex, UpgradeRarity rarity)
    {
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
            case 7:
                ApplyRocketLauncherUpgrade(multiplier);
                break;
            case 8:
                ApplyChainLightningUpgrade(multiplier);
                break;
            case 9:
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
        }

        RunBuildTracker.GetOrCreate().RecordUpgrade(upgradeIndex);
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
