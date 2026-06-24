using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunBuildHud : MonoBehaviour
{
    private const int SlotCount = RunBuildTracker.MaxSlotsPerCategory;

    private const float PanelX = 10f;
    private const float PanelY = -90f;
    private const float PanelWidth = 264f;
    private const float PanelHeight = 152f;
    private const float ContentPadding = 8f;
    private const float SlotWidth = 74f;
    private const float SlotHeight = 26f;
    private const float SlotGap = 5f;

    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.08f, 0.72f);
    private static readonly Color HeaderColor = new Color(0.9f, 0.92f, 0.96f, 1f);
    private static readonly Color SectionColor = new Color(0.72f, 0.76f, 0.84f, 1f);
    private static readonly Color EmptySlotColor = new Color(0.1f, 0.11f, 0.14f, 0.68f);
    private static readonly Color EmptyTextColor = new Color(0.5f, 0.53f, 0.58f, 0.55f);

    private readonly SlotView[] skillSlotViews = new SlotView[SlotCount];
    private readonly SlotView[] passiveSlotViews = new SlotView[SlotCount];

    private TMP_Text skillsHeaderText;
    private TMP_Text passivesHeaderText;

    private GameObject panelRoot;
    private bool isBuilt;

    private static RunBuildHud instance;

    public static void ShowHud()
    {
        SetVisible(true);
    }

    public static void HideHud()
    {
        SetVisible(false);
    }

    public static void SetVisible(bool visible)
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<RunBuildHud>();
        }

        instance?.ApplyVisibility(visible);
    }

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
        instance = this;
        BuildPanel();
        RunBuildTracker tracker = RunBuildTracker.GetOrCreate();
        tracker.OnBuildChanged += Refresh;
        tracker.OnEvolutionUnlocked += OnEvolutionUnlocked;
        Refresh();
        ApplyVisibility(false);
    }

    private void OnDestroy()
    {
        if (RunBuildTracker.Instance != null)
        {
            RunBuildTracker.Instance.OnBuildChanged -= Refresh;
            RunBuildTracker.Instance.OnEvolutionUnlocked -= OnEvolutionUnlocked;
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnEvolutionUnlocked(BuildEvolutionId evolutionId)
    {
        Refresh();
    }

    private void ApplyVisibility(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
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
        panelRoot = panelObject;

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopLeft(panelRect, PanelX, PanelY, PanelWidth, PanelHeight);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.color = PanelBackground;

        TMP_Text headerText = CreateLeftText(panelObject.transform, "BuildHeader", ContentPadding, -6f, PanelWidth - (ContentPadding * 2f), 18f, 14f, FontStyles.Bold);
        headerText.text = "BUILD";
        headerText.color = HeaderColor;
        headerText.alignment = TextAlignmentOptions.MidlineLeft;

        skillsHeaderText = CreateLeftText(panelObject.transform, "SkillsHeader", ContentPadding, -26f, PanelWidth - (ContentPadding * 2f), 14f, 11f, FontStyles.Bold);
        skillsHeaderText.text = "SKILLS 0/3";
        skillsHeaderText.color = SectionColor;
        skillsHeaderText.alignment = TextAlignmentOptions.MidlineLeft;

        BuildSlotRow(panelObject.transform, skillSlotViews, -42f);

        passivesHeaderText = CreateLeftText(panelObject.transform, "PassivesHeader", ContentPadding, -74f, PanelWidth - (ContentPadding * 2f), 14f, 11f, FontStyles.Bold);
        passivesHeaderText.text = "PASSIVES 0/3";
        passivesHeaderText.color = SectionColor;
        passivesHeaderText.alignment = TextAlignmentOptions.MidlineLeft;

        BuildSlotRow(panelObject.transform, passiveSlotViews, -90f);

        isBuilt = true;
    }

    private static void BuildSlotRow(Transform parent, SlotView[] slotViews, float rowY)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            float x = ContentPadding + i * (SlotWidth + SlotGap);

            GameObject slotObject = new GameObject("Slot_" + i);
            slotObject.transform.SetParent(parent, false);

            RectTransform slotRect = slotObject.AddComponent<RectTransform>();
            UiLayoutUtility.SetAnchorTopLeft(slotRect, x, rowY, SlotWidth, SlotHeight);

            Image background = slotObject.AddComponent<Image>();
            background.raycastTarget = false;
            background.color = EmptySlotColor;

            TMP_Text label = CreateSlotLabel(slotObject.transform);
            label.text = "empty";
            label.color = EmptyTextColor;

            slotViews[i] = new SlotView
            {
                Background = background,
                Label = label
            };
        }
    }

    private static TMP_Text CreateSlotLabel(Transform slotParent)
    {
        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(slotParent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(2f, 1f);
        rectTransform.offsetMax = new Vector2(-2f, -1f);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 10f;
        textMesh.fontStyle = FontStyles.Normal;
        textMesh.enableAutoSizing = true;
        textMesh.fontSizeMin = 7f;
        textMesh.fontSizeMax = 10f;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;
        textMesh.raycastTarget = false;

        return textMesh;
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
            slotView.Label.fontStyle = FontStyles.Normal;
            return;
        }

        Color buildColor = UpgradeOptionCatalog.GetBuildColor(entry.BuildType);
        slotView.Background.color = new Color(buildColor.r, buildColor.g, buildColor.b, 0.28f);
        int maxLevel = UpgradeOptionCatalog.GetMaxLevel(entry.UpgradeIndex);
        string displayName = GetEvolvedDisplayName(entry);

        if (entry.Level >= maxLevel)
        {
            slotView.Label.text = displayName + " MAX";
        }
        else
        {
            slotView.Label.text = displayName + " Lv." + entry.Level + "/" + maxLevel;
        }

        slotView.Label.color = buildColor;
        slotView.Label.fontStyle = FontStyles.Bold;
    }

    private static string GetEvolvedDisplayName(RunBuildSlotEntry entry)
    {
        if (entry == null || RunBuildTracker.Instance == null)
        {
            return entry != null ? entry.DisplayName : "empty";
        }

        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (entry.UpgradeIndex == 6 && tracker.HasEvolution(BuildEvolutionId.FlameOrbit))
        {
            return "Flame Orbit";
        }

        if (entry.UpgradeIndex == UpgradeOptionCatalog.FrostSigilIndex
            && tracker.HasEvolution(BuildEvolutionId.GlacialPrison))
        {
            return "Glacial Prison";
        }

        if (entry.UpgradeIndex == UpgradeOptionCatalog.ShadowRiftIndex
            && tracker.HasEvolution(BuildEvolutionId.AbyssSingularity))
        {
            return "Abyss Singularity";
        }

        return entry.DisplayName;
    }
}
