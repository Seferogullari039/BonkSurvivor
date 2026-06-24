using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ItemOfferLayoutUI
{
    private const float LeftPanelWidth = 390f;
    private const float LeftPanelHeight = 620f;
    private const float RightPanelWidth = 330f;
    private const float RightPanelHeight = 620f;
    private const float CenterPanelWidth = 780f;
    private const float CenterPanelHeight = 680f;
    private const float SidePanelOffsetX = 610f;

    private static readonly Color DimColor = new Color(0.02f, 0.03f, 0.05f, 0.62f);
    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.09f, 0.94f);
    private static readonly Color PanelBorder = new Color(0.24f, 0.26f, 0.32f, 0.9f);
    private static readonly Color TitleBarBackground = new Color(0.07f, 0.08f, 0.12f, 0.98f);
    private static readonly Color TitleColor = new Color(0.93f, 0.89f, 0.78f, 1f);
    private static readonly Color SubtitleColor = new Color(0.58f, 0.62f, 0.70f, 1f);
    private static readonly Color SectionHeaderColor = new Color(0.82f, 0.86f, 0.92f, 1f);
    private static readonly Color BodyColor = new Color(0.72f, 0.76f, 0.84f, 1f);

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
            UiLayoutUtility.SetAnchorCenter(panelRect, new Vector2(0f, -8f), new Vector2(CenterPanelWidth, CenterPanelHeight));
        }

        Image panelImage = centerPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = PanelBackground;
        }

        EnsurePanelBorder(centerPanel.transform, CenterPanelWidth, CenterPanelHeight);
    }

    private void EnsureCenterPanelChrome(Transform centerPanel, TMP_Text fontSource)
    {
        centerTitleText = EnsureCenterText(
            centerPanel,
            "ItemOfferTitle",
            new Vector2(0f, 286f),
            new Vector2(720f, 42f),
            30f,
            FontStyles.Bold,
            TitleColor,
            TextAlignmentOptions.Center,
            fontSource);
        centerTitleText.text = "ITEM OFFERS";

        centerSubtitleText = EnsureCenterText(
            centerPanel,
            "ItemOfferSubtitle",
            new Vector2(0f, 248f),
            new Vector2(680f, 28f),
            17f,
            FontStyles.Italic,
            SubtitleColor,
            TextAlignmentOptions.Center,
            fontSource);
        centerSubtitleText.text = "Choose one upgrade to shape your run.";

        EnsureTitleBar(centerPanel);
    }

    private static void EnsureTitleBar(Transform centerPanel)
    {
        Transform existingBar = centerPanel.Find("ItemOfferTitleBar");

        if (existingBar != null)
        {
            return;
        }

        GameObject barObject = new GameObject("ItemOfferTitleBar");
        barObject.transform.SetParent(centerPanel, false);
        barObject.transform.SetAsFirstSibling();

        RectTransform barRect = barObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(barRect, new Vector2(0f, 310f), new Vector2(CenterPanelWidth - 8f, 56f));

        Image barImage = barObject.AddComponent<Image>();
        barImage.raycastTarget = false;
        barImage.color = TitleBarBackground;
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

        EnsurePanelBorder(panelObject.transform, width, height);

        TMP_Text headerText = CreatePanelText(
            panelObject.transform,
            "Header",
            new Vector2(0f, height * 0.5f - 28f),
            new Vector2(width - 28f, 28f),
            20f,
            FontStyles.Bold,
            SectionHeaderColor,
            TextAlignmentOptions.MidlineLeft,
            fontSource);
        headerText.text = headerLabel;

        bodyText = CreatePanelText(
            panelObject.transform,
            "Body",
            new Vector2(0f, -24f),
            new Vector2(width - 28f, height - 72f),
            15f,
            FontStyles.Normal,
            BodyColor,
            TextAlignmentOptions.TopLeft,
            fontSource);
        bodyText.lineSpacing = 4f;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Truncate;
    }

    private static void EnsurePanelBorder(Transform panelTransform, float width, float height)
    {
        Transform existingBorder = panelTransform.Find("PanelBorder");

        if (existingBorder != null)
        {
            return;
        }

        GameObject borderObject = new GameObject("PanelBorder");
        borderObject.transform.SetParent(panelTransform, false);
        borderObject.transform.SetAsFirstSibling();

        RectTransform borderRect = borderObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(borderRect, Vector2.zero, new Vector2(width + 4f, height + 4f));

        Image borderImage = borderObject.AddComponent<Image>();
        borderImage.raycastTarget = false;
        borderImage.color = PanelBorder;
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
