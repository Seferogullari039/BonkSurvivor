using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemOfferLayoutUI
{
    private const float LeftPanelWidth = 400f;
    private const float LeftPanelHeight = 640f;
    private const float RightPanelWidth = 340f;
    private const float RightPanelHeight = 640f;
    private const float CenterPanelWidth = 800f;
    private const float CenterPanelHeight = 700f;
    private const float SidePanelOffsetX = 620f;
    private const float TitleBarHeight = 48f;

    private static readonly Color DimColor = new Color(0.01f, 0.02f, 0.04f, 0.78f);
    private static readonly Color PanelBackground = new Color(0.045f, 0.055f, 0.095f, 0.96f);
    private static readonly Color PanelBorder = new Color(0.32f, 0.34f, 0.40f, 0.95f);
    private static readonly Color PanelInnerLine = new Color(0.18f, 0.20f, 0.26f, 0.85f);
    private static readonly Color TitleBarBackground = new Color(0.06f, 0.07f, 0.11f, 1f);
    private static readonly Color TitleBarAccent = new Color(0.62f, 0.52f, 0.28f, 0.75f);
    private static readonly Color TitleColor = new Color(0.96f, 0.91f, 0.78f, 1f);
    private static readonly Color SubtitleColor = new Color(0.66f, 0.70f, 0.78f, 1f);
    private static readonly Color SectionHeaderColor = new Color(0.88f, 0.90f, 0.96f, 1f);
    private static readonly Color BodyColor = new Color(0.76f, 0.80f, 0.88f, 1f);

    private GameObject rootObject;
    private TMP_Text inventoryBodyText;
    private TMP_Text statsBodyText;
    private TMP_Text centerTitleText;
    private TMP_Text centerSubtitleText;
    private bool isBuilt;

    public void EnsureBuilt(Canvas canvas, GameObject centerPanel, TMP_Text fontSource)
    {
        if (isBuilt || canvas == null || centerPanel == null)
        {
            return;
        }

        rootObject = new GameObject("ItemOfferLayoutRoot");
        rootObject.transform.SetParent(canvas.transform, false);
        rootObject.transform.SetAsFirstSibling();

        RectTransform rootRect = rootObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(rootRect);

        CreateDimOverlay(rootObject.transform);
        CreateSidePanel(
            rootObject.transform,
            "ItemOfferInventoryPanel",
            -SidePanelOffsetX,
            LeftPanelWidth,
            LeftPanelHeight,
            "INVENTORY",
            fontSource,
            out inventoryBodyText);
        CreateSidePanel(
            rootObject.transform,
            "ItemOfferStatsPanel",
            SidePanelOffsetX,
            RightPanelWidth,
            RightPanelHeight,
            "STATS",
            fontSource,
            out statsBodyText);

        EnsureCenterPanelChrome(centerPanel.transform, fontSource);
        ConfigureCenterPanel(centerPanel);

        rootObject.SetActive(false);
        isBuilt = true;
    }

    public void SetVisible(bool visible)
    {
        if (rootObject != null)
        {
            rootObject.SetActive(visible);
        }

        if (visible)
        {
            ItemOfferHudVisibility.HideForItemOffer();
        }
        else
        {
            ItemOfferHudVisibility.RestoreAfterItemOffer();
        }
    }

    public void RefreshSidePanels(PlayerStats playerStats)
    {
        if (inventoryBodyText != null)
        {
            inventoryBodyText.text = RewardCardTextFormatter.BuildInventoryPanelText();
        }

        if (statsBodyText != null)
        {
            statsBodyText.text = RewardCardTextFormatter.BuildStatsPanelText(playerStats);
        }
    }

    private void ConfigureCenterPanel(GameObject centerPanel)
    {
        RectTransform panelRect = centerPanel.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            UiLayoutUtility.SetAnchorCenter(panelRect, new Vector2(0f, -6f), new Vector2(CenterPanelWidth, CenterPanelHeight));
        }

        Image panelImage = centerPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = PanelBackground;
        }

        EnsurePanelChrome(centerPanel.transform, CenterPanelWidth, CenterPanelHeight);
    }

    private void EnsureCenterPanelChrome(Transform centerPanel, TMP_Text fontSource)
    {
        EnsureTitleBar(centerPanel, CenterPanelWidth, TitleBarHeight + 8f, 312f);

        centerTitleText = EnsureCenterText(
            centerPanel,
            "ItemOfferTitle",
            new Vector2(0f, 300f),
            new Vector2(740f, 40f),
            32f,
            FontStyles.Bold,
            TitleColor,
            TextAlignmentOptions.Center,
            fontSource);
        centerTitleText.text = "ITEM OFFERS";
        centerTitleText.characterSpacing = 4f;

        centerSubtitleText = EnsureCenterText(
            centerPanel,
            "ItemOfferSubtitle",
            new Vector2(0f, 262f),
            new Vector2(700f, 30f),
            18f,
            FontStyles.Italic,
            SubtitleColor,
            TextAlignmentOptions.Center,
            fontSource);
        centerSubtitleText.text = "Choose one upgrade to shape your run.";
        centerSubtitleText.lineSpacing = 2f;
    }

    private static void EnsureTitleBar(Transform panelTransform, float width, float height, float centerY)
    {
        Transform existingBar = panelTransform.Find("ItemOfferTitleBar");

        if (existingBar == null)
        {
            GameObject barObject = new GameObject("ItemOfferTitleBar");
            barObject.transform.SetParent(panelTransform, false);
            barObject.transform.SetAsFirstSibling();

            RectTransform barRect = barObject.AddComponent<RectTransform>();
            UiLayoutUtility.SetAnchorCenter(barRect, new Vector2(0f, centerY), new Vector2(width - 6f, height));

            Image barImage = barObject.AddComponent<Image>();
            barImage.raycastTarget = false;
            barImage.color = TitleBarBackground;
        }

        EnsureHorizontalAccent(panelTransform, "ItemOfferTitleAccent", centerY - (height * 0.5f) - 2f, width - 24f);
    }

    private static TMP_Text EnsureCenterText(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment,
        TMP_Text fontSource)
    {
        Transform existing = parent.Find(objectName);

        if (existing != null)
        {
            TMP_Text existingText = existing.GetComponent<TMP_Text>();

            if (existingText != null)
            {
                existingText.fontSize = fontSize;
                existingText.color = color;
                return existingText;
            }
        }

        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, position, size);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        CopyFont(fontSource, textMesh);
        textMesh.alignment = alignment;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.color = color;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private static void CreateDimOverlay(Transform parent)
    {
        GameObject dimObject = new GameObject("ItemOfferDimOverlay");
        dimObject.transform.SetParent(parent, false);

        RectTransform dimRect = dimObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(dimRect);

        Image dimImage = dimObject.AddComponent<Image>();
        dimImage.raycastTarget = false;
        dimImage.color = DimColor;
    }

    private static void CreateSidePanel(
        Transform parent,
        string panelName,
        float centerX,
        float width,
        float height,
        string headerLabel,
        TMP_Text fontSource,
        out TMP_Text bodyText)
    {
        GameObject panelObject = new GameObject(panelName);
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, new Vector2(centerX, 0f), new Vector2(width, height));

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.color = PanelBackground;

        EnsurePanelChrome(panelObject.transform, width, height);
        EnsureTitleBar(panelObject.transform, width, TitleBarHeight, height * 0.5f - (TitleBarHeight * 0.5f) - 4f);

        TMP_Text headerText = CreatePanelText(
            panelObject.transform,
            "Header",
            new Vector2(0f, height * 0.5f - 30f),
            new Vector2(width - 36f, 30f),
            22f,
            FontStyles.Bold,
            SectionHeaderColor,
            TextAlignmentOptions.MidlineLeft,
            fontSource);
        headerText.text = headerLabel;
        headerText.characterSpacing = 2f;

        bodyText = CreatePanelText(
            panelObject.transform,
            "Body",
            new Vector2(0f, -18f),
            new Vector2(width - 36f, height - 88f),
            16f,
            FontStyles.Normal,
            BodyColor,
            TextAlignmentOptions.TopLeft,
            fontSource);
        bodyText.lineSpacing = 6f;
        bodyText.paragraphSpacing = 4f;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Truncate;
        bodyText.margin = new Vector4(4f, 8f, 4f, 8f);
    }

    private static void EnsurePanelChrome(Transform panelTransform, float width, float height)
    {
        EnsurePanelBorder(panelTransform, width, height);
        EnsureHorizontalAccent(panelTransform, "PanelTopAccent", height * 0.5f - 2f, width - 20f);
        EnsureHorizontalAccent(panelTransform, "PanelBottomAccent", -(height * 0.5f) + 2f, width - 20f, PanelInnerLine);
    }

    private static void EnsurePanelBorder(Transform panelTransform, float width, float height)
    {
        Transform existingBorder = panelTransform.Find("PanelBorder");

        if (existingBorder != null)
        {
            RectTransform existingRect = existingBorder as RectTransform;

            if (existingRect != null)
            {
                UiLayoutUtility.SetAnchorCenter(existingRect, Vector2.zero, new Vector2(width + 6f, height + 6f));
            }

            return;
        }

        GameObject borderObject = new GameObject("PanelBorder");
        borderObject.transform.SetParent(panelTransform, false);
        borderObject.transform.SetAsFirstSibling();

        RectTransform borderRect = borderObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(borderRect, Vector2.zero, new Vector2(width + 6f, height + 6f));

        Image borderImage = borderObject.AddComponent<Image>();
        borderImage.raycastTarget = false;
        borderImage.color = PanelBorder;
    }

    private static void EnsureHorizontalAccent(
        Transform panelTransform,
        string objectName,
        float centerY,
        float width,
        Color? colorOverride = null)
    {
        Transform existing = panelTransform.Find(objectName);

        if (existing != null)
        {
            return;
        }

        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(panelTransform, false);

        RectTransform lineRect = lineObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(lineRect, new Vector2(0f, centerY), new Vector2(width, 2f));

        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.raycastTarget = false;
        lineImage.color = colorOverride ?? TitleBarAccent;
    }

    private static TMP_Text CreatePanelText(
        Transform parent,
        string objectName,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment,
        TMP_Text fontSource)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, position, size);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        CopyFont(fontSource, textMesh);
        textMesh.alignment = alignment;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.color = color;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private static void CopyFont(TMP_Text source, TMP_Text target)
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
}
