using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChestStatBuffHud : MonoBehaviour
{
    private const int MaxVisibleBadges = 7;

    private const float PanelInsetX = 24f;
    private const float PanelInsetY = 118f;
    private const float PanelHeight = 28f;
    private const float ChipWidth = 52f;
    private const float ChipHeight = 22f;
    private const float ChipGap = 4f;
    private const float ContentPadding = 6f;

    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.08f, 0.72f);
    private static readonly Color ChipBackground = new Color(0.12f, 0.14f, 0.2f, 0.9f);
    private static readonly Color ChipTextColor = new Color(0.88f, 0.9f, 0.95f, 1f);
    private static readonly Color TooltipBackground = new Color(0.06f, 0.07f, 0.1f, 0.96f);

    private static ChestStatBuffHud instance;

    private GameObject panelRoot;
    private Transform chipRow;
    private GameObject tooltipRoot;
    private TMP_Text tooltipText;
    private RectTransform tooltipRect;
    private RectTransform canvasRect;
    private bool isBuilt;
    private bool runHudVisible;
    private ChestStatRewardType? hoveredType;

    private readonly List<BuffChipView> chipViews = new List<BuffChipView>();

    public static void HideHud()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<ChestStatBuffHud>();
        }

        instance?.ApplyRunVisibility(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ChestStatBuffHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("ChestStatBuffHud");
        hudObject.AddComponent<ChestStatBuffHud>();
    }

    private void Awake()
    {
        instance = this;
        BuildPanel();
        ChestStatBuffTracker tracker = ChestStatBuffTracker.GetOrCreate();
        tracker.OnBuffsChanged += Refresh;
        Refresh();
        ApplyRunVisibility(false);
    }

    private void OnDestroy()
    {
        if (ChestStatBuffTracker.Instance != null)
        {
            ChestStatBuffTracker.Instance.OnBuffsChanged -= Refresh;
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void LateUpdate()
    {
        if (!PauseMenuManager.IsGameplayPaused)
        {
            HideTooltip();
            SetChipRaycasts(false);
            return;
        }

        SetChipRaycasts(true);

        if (hoveredType.HasValue && tooltipRoot != null && tooltipRoot.activeSelf)
        {
            UpdateTooltipPosition();
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

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        canvasRect = canvas.GetComponent<RectTransform>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        GameObject panelObject = new GameObject("ChestStatBuffPanel");
        panelObject.transform.SetParent(canvas.transform, false);
        panelRoot = panelObject;

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorBottomLeft(panelRect, PanelInsetX, PanelInsetY, ChipWidth, PanelHeight);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.color = PanelBackground;

        GameObject rowObject = new GameObject("ChipRow");
        rowObject.transform.SetParent(panelObject.transform, false);
        chipRow = rowObject.transform;

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopLeft(rowRect, ContentPadding, -3f, ChipWidth, ChipHeight);

        BuildTooltip(canvas.transform);
        isBuilt = true;
    }

    private void BuildTooltip(Transform canvasTransform)
    {
        tooltipRoot = new GameObject("ChestBuffTooltip");
        tooltipRoot.transform.SetParent(canvasTransform, false);

        tooltipRect = tooltipRoot.AddComponent<RectTransform>();
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 1f);
        tooltipRect.sizeDelta = new Vector2(220f, 110f);

        Image background = tooltipRoot.AddComponent<Image>();
        background.color = TooltipBackground;
        background.raycastTarget = false;

        tooltipText = CreateText(
            tooltipRoot.transform,
            "TooltipText",
            8f,
            -6f,
            204f,
            98f,
            14f,
            FontStyles.Normal);
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.lineSpacing = 2f;

        tooltipRoot.SetActive(false);
    }

    private void Refresh()
    {
        if (!isBuilt)
        {
            return;
        }

        ClearChips();

        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null)
        {
            ApplyPanelVisibility();
            return;
        }

        IReadOnlyList<ChestStatBuffEntry> activeBuffs = tracker.GetActiveBuffs();
        int visibleCount = Mathf.Min(activeBuffs.Count, MaxVisibleBadges);
        ResizePanel(visibleCount);

        for (int i = 0; i < visibleCount; i++)
        {
            CreateChip(activeBuffs[i], i);
        }

        ApplyPanelVisibility();
    }

    private void ApplyPanelVisibility()
    {
        if (panelRoot == null)
        {
            return;
        }

        bool hasBuffs = ChestStatBuffTracker.Instance != null && ChestStatBuffTracker.Instance.GetActiveBuffs().Count > 0;
        panelRoot.SetActive(runHudVisible && hasBuffs && ShouldShowDuringGameplay());
    }

    private void ApplyRunVisibility(bool visible)
    {
        runHudVisible = visible;
        HideTooltip();
        ApplyPanelVisibility();
    }

    public static void OnGameplayRunStarted()
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<ChestStatBuffHud>();
        }

        instance?.ApplyRunVisibility(true);
    }

    private static bool ShouldShowDuringGameplay()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return false;
        }

        if (DevAdminPanel.IsOpen)
        {
            return false;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return false;
        }

        return true;
    }

    private void ResizePanel(int visibleChipCount)
    {
        if (panelRoot == null || visibleChipCount <= 0)
        {
            return;
        }

        float panelWidth = (ContentPadding * 2f)
            + (visibleChipCount * ChipWidth)
            + (Mathf.Max(visibleChipCount - 1, 0) * ChipGap);

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();

        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(panelWidth, PanelHeight);
        }

        if (chipRow != null)
        {
            RectTransform rowRect = chipRow as RectTransform;

            if (rowRect != null)
            {
                rowRect.sizeDelta = new Vector2(panelWidth - (ContentPadding * 2f), ChipHeight);
            }
        }
    }

    private void ClearChips()
    {
        for (int i = 0; i < chipViews.Count; i++)
        {
            if (chipViews[i] != null)
            {
                Destroy(chipViews[i].gameObject);
            }
        }

        chipViews.Clear();
        hoveredType = null;
    }

    private void CreateChip(ChestStatBuffEntry entry, int index)
    {
        GameObject chipObject = new GameObject("BuffChip_" + entry.Type);
        chipObject.transform.SetParent(chipRow, false);

        float x = index * (ChipWidth + ChipGap);

        RectTransform chipRect = chipObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopLeft(chipRect, x, 0f, ChipWidth, ChipHeight);

        Image background = chipObject.AddComponent<Image>();
        background.color = ChipBackground;
        background.raycastTarget = false;

        TMP_Text label = CreateText(chipObject.transform, "Label", 0f, 0f, ChipWidth, ChipHeight, 9f, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.text = ChestStatBuffTracker.FormatHudBadgeText(entry);
        label.color = ChipTextColor;
        label.raycastTarget = false;

        BuffChipView chipView = chipObject.AddComponent<BuffChipView>();
        chipView.Initialize(this, entry.Type, background);
        chipViews.Add(chipView);
    }

    private void SetChipRaycasts(bool enabled)
    {
        for (int i = 0; i < chipViews.Count; i++)
        {
            chipViews[i]?.SetRaycastEnabled(enabled);
        }
    }

    internal void ShowTooltip(ChestStatRewardType type)
    {
        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null || tooltipRoot == null || tooltipText == null)
        {
            return;
        }

        string tooltip = tracker.GetTooltip(type);

        if (string.IsNullOrEmpty(tooltip))
        {
            HideTooltip();
            return;
        }

        hoveredType = type;
        tooltipText.text = tooltip;
        tooltipRoot.SetActive(true);
        tooltipRoot.transform.SetAsLastSibling();
        UpdateTooltipPosition();
    }

    internal void HideTooltip()
    {
        hoveredType = null;

        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRect == null || canvasRect == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Input.mousePosition,
            null,
            out Vector2 localPoint);

        tooltipRect.anchoredPosition = localPoint + new Vector2(14f, -10f);
    }

    private static TMP_Text CreateText(
        Transform parent,
        string objectName,
        float x,
        float y,
        float width,
        float height,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopLeft(rectTransform, x, y, width, height);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private sealed class BuffChipView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ChestStatBuffHud owner;
        private ChestStatRewardType type;
        private Image background;

        public void Initialize(ChestStatBuffHud hudOwner, ChestStatRewardType buffType, Image chipBackground)
        {
            owner = hudOwner;
            type = buffType;
            background = chipBackground;
        }

        public void SetRaycastEnabled(bool enabled)
        {
            if (background != null)
            {
                background.raycastTarget = enabled;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!PauseMenuManager.IsGameplayPaused)
            {
                return;
            }

            owner?.ShowTooltip(type);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HideTooltip();
        }
    }
}
