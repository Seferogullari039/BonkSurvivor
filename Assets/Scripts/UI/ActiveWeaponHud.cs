using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActiveWeaponHud : MonoBehaviour
{
    private const float PanelInsetX = -18f;
    private const float PanelInsetY = 82f;
    private const float PanelWidth = 290f;
    private const float PanelHeight = 104f;
    private const float ContentPadding = 10f;
    private const float RowHeight = 24f;
    private const float HeaderHeight = 22f;
    private const float LabelWidth = 168f;
    private const float BarWidth = 72f;
    private const float BarHeight = 7f;
    private const float BarOffsetX = 174f;
    private const float TimeWidth = 38f;
    private const float HeaderFontSize = 14f;
    private const float RowFontSize = 11f;
    private const float TimeFontSize = 10.5f;

    private static readonly Color PanelBackground = new Color(0.04f, 0.05f, 0.08f, 0.8f);
    private static readonly Color PanelBorderColor = new Color(0.22f, 0.28f, 0.38f, 0.42f);
    private static readonly Color HeaderColor = new Color(0.94f, 0.95f, 0.98f, 1f);
    private static readonly Color AbilityLabelColor = new Color(0.78f, 0.82f, 0.9f, 1f);
    private static readonly Color TimeReadyColor = new Color(0.52f, 0.86f, 0.58f, 1f);
    private static readonly Color TimeCooldownColor = new Color(0.72f, 0.76f, 0.84f, 0.95f);
    private static readonly Color BarBackgroundColor = new Color(0.1f, 0.11f, 0.14f, 0.85f);
    private static readonly Color BarReadyColor = new Color(0.35f, 0.75f, 0.45f, 0.9f);
    private static readonly Color BarCooldownColor = new Color(0.45f, 0.5f, 0.58f, 0.65f);

    private static ActiveWeaponHud instance;

    private GameObject panelRoot;
    private TMP_Text weaponHeaderText;
    private AbilityRowView primaryRow;
    private AbilityRowView secondaryRow;
    private StarterWeaponController weaponController;
    private bool isBuilt;
    private bool runHudVisible;

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
            instance = FindFirstObjectByType<ActiveWeaponHud>();
        }

        instance?.ApplyRunVisibility(visible);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ActiveWeaponHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("ActiveWeaponHud");
        hudObject.AddComponent<ActiveWeaponHud>();
    }

    private sealed class AbilityRowView
    {
        public TMP_Text LabelText;
        public Image BarFill;
        public TMP_Text TimeText;
    }

    private void Awake()
    {
        instance = this;
        BuildPanel();
        ApplyRunVisibility(false);
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
        if (!isBuilt || panelRoot == null)
        {
            return;
        }

        bool shouldShow = runHudVisible && ShouldShowDuringGameplay();
        panelRoot.SetActive(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        RefreshWeaponController();
        RefreshDisplay();
    }

    private void ApplyRunVisibility(bool visible)
    {
        runHudVisible = visible;

        if (panelRoot != null)
        {
            panelRoot.SetActive(visible && ShouldShowDuringGameplay());
        }
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

        if (PauseMenuManager.IsGameplayPaused)
        {
            return false;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.IsLevelUpOpen)
        {
            return false;
        }

        return true;
    }

    private void RefreshWeaponController()
    {
        if (weaponController != null && weaponController.isActiveAndEnabled)
        {
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        weaponController = player != null ? player.GetComponent<StarterWeaponController>() : null;
    }

    private void RefreshDisplay()
    {
        if (weaponController == null)
        {
            if (weaponHeaderText != null)
            {
                weaponHeaderText.text = "[1] HUNTER BOW";
            }

            ApplyAbilityRow(primaryRow, "LMB", "Quick Shot", 1f, 0f);
            ApplyAbilityRow(secondaryRow, "RMB", "Arrow Rain", 1f, 0f);
            return;
        }

        WeaponHudDisplay display = GetDisplay(weaponController.CurrentWeapon);

        if (weaponHeaderText != null)
        {
            weaponHeaderText.text = "[" + display.Hotkey + "] " + display.WeaponName.ToUpperInvariant();
        }

        ApplyAbilityRow(
            primaryRow,
            "LMB",
            display.PrimaryAbility,
            weaponController.PrimaryCooldownRemaining01,
            weaponController.PrimaryCooldownRemainingSeconds);

        ApplyAbilityRow(
            secondaryRow,
            "RMB",
            display.SecondaryAbility,
            weaponController.SecondaryCooldownRemaining01,
            weaponController.SecondaryCooldownRemainingSeconds);
    }

    private static void ApplyAbilityRow(AbilityRowView row, string inputLabel, string abilityName, float ready01, float remainingSeconds)
    {
        if (row == null)
        {
            return;
        }

        if (row.LabelText != null)
        {
            row.LabelText.text = inputLabel + "  " + abilityName;
        }

        bool onCooldown = remainingSeconds > 0.05f;

        if (row.BarFill != null)
        {
            row.BarFill.fillAmount = Mathf.Clamp01(ready01);
            row.BarFill.color = onCooldown ? BarCooldownColor : BarReadyColor;
        }

        if (row.TimeText != null)
        {
            if (onCooldown)
            {
                row.TimeText.text = remainingSeconds.ToString("0.0") + "s";
                row.TimeText.color = TimeCooldownColor;
                row.TimeText.fontStyle = FontStyles.Normal;
            }
            else
            {
                row.TimeText.text = "READY";
                row.TimeText.color = TimeReadyColor;
                row.TimeText.fontStyle = FontStyles.Bold;
            }
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

        GameObject panelObject = new GameObject("ActiveWeaponPanel");
        panelObject.transform.SetParent(canvas.transform, false);
        panelRoot = panelObject;

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorBottomRight(panelRect, PanelInsetX, PanelInsetY, PanelWidth, PanelHeight);

        Image borderImage = CreatePanelBorder(panelObject.transform);
        borderImage.color = PanelBorderColor;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.color = PanelBackground;

        float headerBottom = PanelHeight - HeaderHeight - 6f;
        weaponHeaderText = CreateLeftText(
            panelObject.transform,
            "WeaponHeader",
            ContentPadding,
            headerBottom,
            PanelWidth - (ContentPadding * 2f),
            HeaderHeight,
            HeaderFontSize,
            FontStyles.Bold);
        weaponHeaderText.color = HeaderColor;

        primaryRow = BuildAbilityRow(panelObject.transform, "PrimaryRow", 44f);
        secondaryRow = BuildAbilityRow(panelObject.transform, "SecondaryRow", 14f);

        isBuilt = true;
    }

    private AbilityRowView BuildAbilityRow(Transform parent, string rowName, float bottomOffset)
    {
        GameObject rowObject = new GameObject(rowName);
        rowObject.transform.SetParent(parent, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorBottomLeft(rowRect, ContentPadding, bottomOffset, PanelWidth - (ContentPadding * 2f), RowHeight);

        TMP_Text labelText = CreateLeftText(rowObject.transform, "Label", 0f, 0f, LabelWidth, RowHeight, RowFontSize, FontStyles.Normal);
        labelText.color = AbilityLabelColor;

        Image barBackground = CreateBarBackground(rowObject.transform, BarOffsetX);
        Image barFill = CreateBarFill(barBackground.transform);

        float timeOffsetX = PanelWidth - (ContentPadding * 2f) - TimeWidth;
        TMP_Text timeText = CreateLeftText(rowObject.transform, "Time", timeOffsetX, 0f, TimeWidth, RowHeight, TimeFontSize, FontStyles.Normal);
        timeText.alignment = TextAlignmentOptions.MidlineRight;
        timeText.color = TimeReadyColor;

        return new AbilityRowView
        {
            LabelText = labelText,
            BarFill = barFill,
            TimeText = timeText
        };
    }

    private static Image CreateBarBackground(Transform parent, float x)
    {
        GameObject barObject = new GameObject("CooldownBar");
        barObject.transform.SetParent(parent, false);

        RectTransform barRect = barObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorBottomLeft(barRect, x, 8f, BarWidth, BarHeight);

        Image background = barObject.AddComponent<Image>();
        background.raycastTarget = false;
        background.color = BarBackgroundColor;
        return background;
    }

    private static Image CreateBarFill(Transform barBackground)
    {
        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(barBackground, false);

        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fill = fillObject.AddComponent<Image>();
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = BarReadyColor;
        fill.fillAmount = 1f;
        return fill;
    }

    private static Image CreatePanelBorder(Transform parent)
    {
        GameObject borderObject = new GameObject("PanelBorder");
        borderObject.transform.SetParent(parent, false);
        borderObject.transform.SetAsFirstSibling();

        RectTransform borderRect = borderObject.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-1f, -1f);
        borderRect.offsetMax = new Vector2(1f, 1f);

        Image borderImage = borderObject.AddComponent<Image>();
        borderImage.raycastTarget = false;
        return borderImage;
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
        UiLayoutUtility.SetAnchorBottomLeft(rectTransform, x, y, width, height);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.MidlineLeft;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.NoWrap;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private readonly struct WeaponHudDisplay
    {
        public WeaponHudDisplay(int hotkey, string weaponName, string primaryAbility, string secondaryAbility)
        {
            Hotkey = hotkey;
            WeaponName = weaponName;
            PrimaryAbility = primaryAbility;
            SecondaryAbility = secondaryAbility;
        }

        public int Hotkey { get; }
        public string WeaponName { get; }
        public string PrimaryAbility { get; }
        public string SecondaryAbility { get; }
    }

    private static WeaponHudDisplay GetDisplay(StarterWeaponType weaponType)
    {
        switch (weaponType)
        {
            case StarterWeaponType.FireStaff:
                return new WeaponHudDisplay(2, "Fire Staff", "Fireball", "Meteor");
            case StarterWeaponType.KnightSword:
                return new WeaponHudDisplay(3, "Knight Sword", "Slash", "Whirlwind");
            case StarterWeaponType.Blunderbuss:
                return new WeaponHudDisplay(4, "Blunderbuss", "Scatter Shot", "Blast Shell");
            case StarterWeaponType.ThunderSpear:
                return new WeaponHudDisplay(5, "Thunder Spear", "Lightning Thrust", "Thunder Javelin");
            default:
                return new WeaponHudDisplay(1, "Hunter Bow", "Quick Shot", "Arrow Rain");
        }
    }
}
