using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunBuildHud : MonoBehaviour
{
    private const int SlotCount = RunBuildTracker.MaxSlotsPerCategory;

    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.08f, 0.82f);
    private static readonly Color HeaderColor = new Color(0.9f, 0.92f, 0.96f, 1f);
    private static readonly Color SectionColor = new Color(0.72f, 0.76f, 0.84f, 1f);
    private static readonly Color EmptySlotColor = new Color(0.1f, 0.11f, 0.14f, 0.72f);
    private static readonly Color EmptyTextColor = new Color(0.45f, 0.48f, 0.54f, 0.9f);

    private readonly SlotView[] skillSlotViews = new SlotView[SlotCount];
    private readonly SlotView[] passiveSlotViews = new SlotView[SlotCount];

    private TMP_Text skillsHeaderText;
    private TMP_Text passivesHeaderText;

    private bool isBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RunBuildHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("RunBuildHud");
        hudObject.AddComponent<RunBuildHud>();
    }

    private sealed class SlotView
    {
        public Image Background;
        public TMP_Text Label;
    }

    private void Awake()
    {
        BuildPanel();
        RunBuildTracker tracker = RunBuildTracker.GetOrCreate();
        tracker.OnBuildChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (RunBuildTracker.Instance != null)
        {
            RunBuildTracker.Instance.OnBuildChanged -= Refresh;
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

        GameObject panelObject = new GameObject("RunBuildPanel");
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopLeft(panelRect, 14f, -108f, 236f, 168f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.color = PanelBackground;

        TMP_Text headerText = CreateLeftText(panelObject.transform, "BuildHeader", 10f, -10f, 216f, 22f, 17f, FontStyles.Bold);
        headerText.text = "BUILD";
        headerText.color = HeaderColor;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;

        skillsHeaderText = CreateLeftText(panelObject.transform, "SkillsHeader", 10f, -34f, 216f, 18f, 13f, FontStyles.Bold);
        skillsHeaderText.text = "SKILLS";
        skillsHeaderText.color = SectionColor;
        skillsHeaderText.alignment = TextAlignmentOptions.MidlineLeft;

        BuildSlotRow(panelObject.transform, skillSlotViews, -58f);

        passivesHeaderText = CreateLeftText(panelObject.transform, "PassivesHeader", 10f, -98f, 216f, 18f, 13f, FontStyles.Bold);
        passivesHeaderText.text = "PASSIVES";
        passivesHeaderText.color = SectionColor;
        passivesHeaderText.alignment = TextAlignmentOptions.MidlineLeft;

        BuildSlotRow(panelObject.transform, passiveSlotViews, -122f);

        isBuilt = true;
    }

    private static void BuildSlotRow(Transform parent, SlotView[] slotViews, float rowY)
    {
        const float slotWidth = 72f;
        const float slotHeight = 28f;
        const float gap = 4f;
        float startX = -((slotWidth * SlotCount) + (gap * (SlotCount - 1))) * 0.5f + slotWidth * 0.5f;

        for (int i = 0; i < SlotCount; i++)
        {
            float x = startX + i * (slotWidth + gap);

            GameObject slotObject = new GameObject("Slot_" + i);
            slotObject.transform.SetParent(parent, false);

            RectTransform slotRect = slotObject.AddComponent<RectTransform>();
            UiLayoutUtility.SetAnchorTopLeft(slotRect, 118f + x, rowY, slotWidth, slotHeight);

            Image background = slotObject.AddComponent<Image>();
            background.raycastTarget = false;
            background.color = EmptySlotColor;

            TMP_Text label = CreateText(slotObject.transform, "Label", Vector2.zero, new Vector2(slotWidth - 6f, slotHeight - 4f), 12f, FontStyles.Bold);
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 9f;
            label.fontSizeMax = 12f;
            label.text = "empty";

            slotViews[i] = new SlotView
            {
                Background = background,
                Label = label
            };
        }
    }

    private static TMP_Text CreateLeftText(
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
        textMesh.alignment = TextAlignmentOptions.MidlineLeft;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;
        textMesh.raycastTarget = false;

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
        UiLayoutUtility.SetAnchorTopLeft(rectTransform, 118f + anchoredPosition.x, anchoredPosition.y, size.x, size.y);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private void Refresh()
    {
        if (!isBuilt || RunBuildTracker.Instance == null)
        {
            return;
        }

        RunBuildTracker tracker = RunBuildTracker.Instance;

        for (int i = 0; i < SlotCount; i++)
        {
            ApplySlotView(skillSlotViews[i], tracker.GetSkillSlot(i));
            ApplySlotView(passiveSlotViews[i], tracker.GetPassiveSlot(i));
        }

        if (skillsHeaderText != null)
        {
            skillsHeaderText.text = "SKILLS " + tracker.GetFilledSlotCount(RewardCategory.Skill) + "/" + SlotCount;
        }

        if (passivesHeaderText != null)
        {
            passivesHeaderText.text = "PASSIVES " + tracker.GetFilledSlotCount(RewardCategory.Passive) + "/" + SlotCount;
        }
    }

    private static void ApplySlotView(SlotView slotView, RunBuildSlotEntry entry)
    {
        if (slotView == null || slotView.Background == null || slotView.Label == null)
        {
            return;
        }

        if (entry == null)
        {
            slotView.Background.color = EmptySlotColor;
            slotView.Label.text = "empty";
            slotView.Label.color = EmptyTextColor;
            return;
        }

        Color buildColor = UpgradeOptionCatalog.GetBuildColor(entry.BuildType);
        slotView.Background.color = new Color(buildColor.r, buildColor.g, buildColor.b, 0.28f);
        slotView.Label.text = entry.DisplayName + " Lv." + entry.Level;
        slotView.Label.color = buildColor;
    }
}
