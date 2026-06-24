using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChestLootSelectionUI : MonoBehaviour
{
    public struct SlotData
    {
        public string RarityLabel;
        public string CategoryLabel;
        public string BuildLabel;
        public Color RarityAccent;
        public Color RarityBackground;
        public Color RarityBorder;
        public string Title;
        public string Description;
        public string IconKey;

        public static SlotData FromUpgrade(
            UpgradeRarity rarity,
            RewardCategory category,
            WeaponBuildType buildType,
            string title,
            string description,
            string iconKey)
        {
            ChestLootRarityPalette.GetStyle(rarity, out Color accent, out Color background, out Color border, out string label);

            return new SlotData
            {
                RarityLabel = label,
                CategoryLabel = UpgradeOptionCatalog.GetCategoryLabel(category),
                BuildLabel = UpgradeOptionCatalog.GetBuildLabel(buildType),
                RarityAccent = accent,
                RarityBackground = background,
                RarityBorder = border,
                Title = title ?? string.Empty,
                Description = description ?? string.Empty,
                IconKey = iconKey ?? string.Empty
            };
        }

        public static SlotData FromChestStat(ChestStatRewardType rewardType, UpgradeRarity rarity)
        {
            ChestStatRewardCatalog.GetDisplay(rewardType, rarity, out string title, out string description);

            ChestLootRarityPalette.GetStyle(rarity, out Color accent, out Color background, out Color border, out string label);

            return new SlotData
            {
                RarityLabel = label,
                CategoryLabel = "CHEST PASSIVE",
                BuildLabel = "GENERAL",
                RarityAccent = accent,
                RarityBackground = background,
                RarityBorder = border,
                Title = title,
                Description = description,
                IconKey = ChestStatRewardCatalog.GetIconKey(rewardType)
            };
        }
    }

    private sealed class LootSlotView
    {
        public Button Button;
        public Image BorderGlow;
        public Image Background;
        public Image IconFrame;
        public Image IconImage;
        public Image PlaceholderIcon;
        public TMP_Text RarityText;
        public TMP_Text TitleText;
        public TMP_Text DescriptionText;
    }

    private static Sprite placeholderSprite;

    private GameObject rootPanel;
    private TMP_Text headerText;
    private readonly LootSlotView[] slots = new LootSlotView[3];
    private Action<int> selectCallback;
    private bool isBuilt;
    private bool isShowing;

    public bool IsShowing => isShowing && rootPanel != null && rootPanel.activeSelf;

    public void EnsureBuilt(Canvas canvas, TMP_Text fontSource)
    {
        if (isBuilt)
        {
            return;
        }

        if (canvas == null)
        {
            canvas = UiLayoutUtility.GetGameplayCanvas();
        }

        if (canvas == null)
        {
            return;
        }

        BuildPanel(canvas.transform, fontSource);
        isBuilt = true;
    }

    public void Show(string header, Color headerColor, SlotData[] slotData, Action<int> onSelect)
    {
        if (rootPanel == null)
        {
            return;
        }

        selectCallback = onSelect;
        rootPanel.SetActive(true);
        isShowing = true;
        ApplyHeader(header, headerColor);
        RefreshSlots(slotData, onSelect);
    }

    public void Refresh(SlotData[] slotData, Action<int> onSelect)
    {
        if (rootPanel == null || !rootPanel.activeSelf)
        {
            return;
        }

        selectCallback = onSelect;
        RefreshSlots(slotData, onSelect);
    }

    public void Hide()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        isShowing = false;
        selectCallback = null;
    }

    private void ApplyHeader(string header, Color headerColor)
    {
        if (headerText == null)
        {
            return;
        }

        headerText.text = string.IsNullOrEmpty(header) ? "Chest Loot" : header;
        headerText.color = headerColor;
    }

    private void RefreshSlots(SlotData[] slotData, Action<int> onSelect)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SlotData data = slotData != null && i < slotData.Length
                ? slotData[i]
                : default;

            ApplySlot(slots[i], data, i, onSelect);
        }
    }

    private static void ApplySlot(LootSlotView slot, SlotData data, int index, Action<int> onSelect)
    {
        if (slot == null)
        {
            return;
        }

        if (slot.RarityText != null)
        {
            slot.RarityText.text = BuildRewardHeaderLabel(data.RarityLabel, data.CategoryLabel, data.BuildLabel);
            slot.RarityText.color = data.RarityAccent;
        }

        if (slot.TitleText != null)
        {
            slot.TitleText.text = data.Title ?? string.Empty;
        }

        if (slot.DescriptionText != null)
        {
            slot.DescriptionText.text = data.Description ?? string.Empty;
        }

        if (slot.Background != null)
        {
            slot.Background.color = data.RarityBackground.a > 0.01f
                ? data.RarityBackground
                : ChestLootRarityPalette.CommonBackground;
        }

        if (slot.BorderGlow != null)
        {
            Color accent = data.RarityAccent.a > 0.01f ? data.RarityAccent : ChestLootRarityPalette.CommonAccent;
            slot.BorderGlow.color = new Color(accent.r, accent.g, accent.b, 0.34f);
        }

        if (slot.IconFrame != null)
        {
            Color border = data.RarityBorder.a > 0.01f ? data.RarityBorder : ChestLootRarityPalette.CommonBorder;
            slot.IconFrame.color = border;
        }

        ApplySlotIcon(slot, data.IconKey);

        if (slot.Button != null)
        {
            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() => onSelect?.Invoke(index));

            Color accent = data.RarityAccent.a > 0.01f ? data.RarityAccent : ChestLootRarityPalette.CommonAccent;
            ColorBlock colors = slot.Button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, accent, 0.22f);
            colors.pressedColor = Color.Lerp(Color.white, accent, 0.34f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            slot.Button.colors = colors;
        }
    }

    private static void ApplySlotIcon(LootSlotView slot, string iconKey)
    {
        if (slot == null)
        {
            return;
        }

        Sprite iconSprite = UpgradeCardIconUtility.TryLoadSprite(iconKey);

        if (iconSprite != null && slot.IconImage != null)
        {
            slot.IconImage.sprite = iconSprite;
            slot.IconImage.enabled = true;
            slot.IconImage.color = Color.white;

            if (slot.PlaceholderIcon != null)
            {
                slot.PlaceholderIcon.gameObject.SetActive(false);
            }

            return;
        }

        if (slot.IconImage != null)
        {
            slot.IconImage.sprite = null;
            slot.IconImage.enabled = false;
        }

        if (slot.PlaceholderIcon != null)
        {
            slot.PlaceholderIcon.sprite = GetPlaceholderSprite();
            slot.PlaceholderIcon.enabled = true;
            slot.PlaceholderIcon.gameObject.SetActive(true);
        }
    }

    private static Sprite GetPlaceholderSprite()
    {
        if (placeholderSprite != null)
        {
            return placeholderSprite;
        }

        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        Color fill = Color.white;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, fill);
            }
        }

        texture.Apply();
        placeholderSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        return placeholderSprite;
    }

    private void BuildPanel(Transform canvasTransform, TMP_Text fontSource)
    {
        rootPanel = new GameObject("ChestLootPanel");
        rootPanel.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = rootPanel.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(760f, 360f));

        Image panelImage = rootPanel.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.94f);
        panelImage.raycastTarget = true;

        headerText = CreateText(rootPanel.transform, fontSource, "ChestLootHeader", new Vector2(0f, 142f), new Vector2(640f, 40f), 28f, FontStyles.Bold);
        headerText.color = new Color(0.92f, 0.88f, 0.72f, 1f);

        BuildLootSlot(0, rootPanel.transform, fontSource, -230f);
        BuildLootSlot(1, rootPanel.transform, fontSource, 0f);
        BuildLootSlot(2, rootPanel.transform, fontSource, 230f);

        rootPanel.SetActive(false);
    }

    private void BuildLootSlot(int index, Transform parent, TMP_Text fontSource, float xOffset)
    {
        GameObject slotObject = new GameObject($"ChestLootSlot{index + 1}");
        slotObject.transform.SetParent(parent, false);

        RectTransform slotRect = slotObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(slotRect, new Vector2(xOffset, -18f), new Vector2(210f, 250f));

        Button button = slotObject.AddComponent<Button>();
        Image buttonImage = button.targetGraphic as Image;

        if (buttonImage == null)
        {
            buttonImage = slotObject.AddComponent<Image>();
            button.targetGraphic = buttonImage;
        }

        buttonImage.color = new Color(0.06f, 0.07f, 0.1f, 0.98f);

        GameObject glowObject = new GameObject("BorderGlow");
        glowObject.transform.SetParent(slotObject.transform, false);
        glowObject.transform.SetAsFirstSibling();

        RectTransform glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-4f, -4f);
        glowRect.offsetMax = new Vector2(4f, 4f);

        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.raycastTarget = false;
        glowImage.color = new Color(0.86f, 0.88f, 0.92f, 0.22f);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(slotObject.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = new Vector2(3f, 3f);
        backgroundRect.offsetMax = new Vector2(-3f, -3f);

        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.raycastTarget = false;
        backgroundImage.color = ChestLootRarityPalette.CommonBackground;

        GameObject iconFrameObject = new GameObject("IconFrame");
        iconFrameObject.transform.SetParent(slotObject.transform, false);

        RectTransform iconFrameRect = iconFrameObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(iconFrameRect, new Vector2(0f, 72f), new Vector2(72f, 72f));

        Image iconFrameImage = iconFrameObject.AddComponent<Image>();
        iconFrameImage.raycastTarget = false;
        iconFrameImage.color = ChestLootRarityPalette.CommonBorder;

        GameObject iconImageObject = new GameObject("IconImage");
        iconImageObject.transform.SetParent(iconFrameObject.transform, false);

        RectTransform iconImageRect = iconImageObject.AddComponent<RectTransform>();
        iconImageRect.anchorMin = Vector2.zero;
        iconImageRect.anchorMax = Vector2.one;
        iconImageRect.offsetMin = new Vector2(7f, 7f);
        iconImageRect.offsetMax = new Vector2(-7f, -7f);

        Image iconImage = iconImageObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = false;

        GameObject placeholderObject = new GameObject("IconPlaceholder");
        placeholderObject.transform.SetParent(iconFrameObject.transform, false);

        RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(placeholderRect, Vector2.zero, new Vector2(34f, 34f));
        placeholderObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image placeholderImage = placeholderObject.AddComponent<Image>();
        placeholderImage.raycastTarget = false;
        placeholderImage.color = new Color(0.78f, 0.8f, 0.86f, 0.85f);
        placeholderImage.sprite = GetPlaceholderSprite();

        TMP_Text rarityText = CreateText(slotObject.transform, fontSource, "RarityText", new Vector2(0f, 108f), new Vector2(180f, 24f), 16f, FontStyles.Bold);
        TMP_Text titleText = CreateText(slotObject.transform, fontSource, "TitleText", new Vector2(0f, 18f), new Vector2(188f, 56f), 22f, FontStyles.Bold);
        TMP_Text descriptionText = CreateText(slotObject.transform, fontSource, "DescriptionText", new Vector2(0f, -58f), new Vector2(188f, 92f), 16f, FontStyles.Normal);

        if (titleText != null)
        {
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 18f;
            titleText.fontSizeMax = 22f;
            titleText.color = Color.white;
        }

        if (descriptionText != null)
        {
            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMin = 14f;
            descriptionText.fontSizeMax = 16f;
            descriptionText.color = new Color(0.76f, 0.8f, 0.88f, 1f);
            descriptionText.lineSpacing = 4f;
        }

        if (rarityText != null)
        {
            rarityText.characterSpacing = 3f;
        }

        slots[index] = new LootSlotView
        {
            Button = button,
            BorderGlow = glowImage,
            Background = backgroundImage,
            IconFrame = iconFrameImage,
            IconImage = iconImage,
            PlaceholderIcon = placeholderImage,
            RarityText = rarityText,
            TitleText = titleText,
            DescriptionText = descriptionText
        };
    }

    private static TMP_Text CreateText(
        Transform parent,
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

    internal static string BuildRewardHeaderLabel(string rarityLabel, string categoryLabel, string buildLabel = "")
    {
        string rarity = string.IsNullOrEmpty(rarityLabel) ? "COMMON" : rarityLabel;
        string header = rarity;

        if (!string.IsNullOrEmpty(categoryLabel))
        {
            header += "  ·  " + categoryLabel;
        }

        if (!string.IsNullOrEmpty(buildLabel))
        {
            header += "  ·  " + buildLabel;
        }

        return header;
    }
}

internal static class ChestLootRarityPalette
{
    public static readonly Color CommonAccent = new Color(0.86f, 0.88f, 0.92f, 1f);
    public static readonly Color CommonBackground = new Color(0.08f, 0.09f, 0.11f, 0.98f);
    public static readonly Color CommonBorder = new Color(0.62f, 0.64f, 0.68f, 0.95f);

    public static readonly Color RareAccent = new Color(0.38f, 0.68f, 1f, 1f);
    public static readonly Color RareBackground = new Color(0.06f, 0.1f, 0.18f, 0.98f);
    public static readonly Color RareBorder = new Color(0.28f, 0.52f, 0.92f, 0.95f);

    public static readonly Color EpicAccent = new Color(0.78f, 0.42f, 1f, 1f);
    public static readonly Color EpicBackground = new Color(0.12f, 0.07f, 0.17f, 0.98f);
    public static readonly Color EpicBorder = new Color(0.58f, 0.24f, 0.9f, 0.95f);

    public static readonly Color LegendaryAccent = new Color(1f, 0.82f, 0.28f, 1f);
    public static readonly Color LegendaryBackground = new Color(0.16f, 0.12f, 0.05f, 0.98f);
    public static readonly Color LegendaryBorder = new Color(0.92f, 0.72f, 0.18f, 0.95f);

    public static void GetStyle(UpgradeRarity rarity, out Color accent, out Color background, out Color border, out string label)
    {
        switch (rarity)
        {
            case UpgradeRarity.Rare:
                accent = RareAccent;
                background = RareBackground;
                border = RareBorder;
                label = "RARE";
                return;
            case UpgradeRarity.Epic:
                accent = EpicAccent;
                background = EpicBackground;
                border = EpicBorder;
                label = "EPIC";
                return;
            case UpgradeRarity.Legendary:
                accent = LegendaryAccent;
                background = LegendaryBackground;
                border = LegendaryBorder;
                label = "LEGENDARY";
                return;
            default:
                accent = CommonAccent;
                background = CommonBackground;
                border = CommonBorder;
                label = "COMMON";
                return;
        }
    }
}
