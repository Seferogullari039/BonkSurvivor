using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // Default off: [Recovery] HUD diagnostic logs are opt-in only. Does not affect HUD visibility/recovery behavior.
    public static bool LogRecoveryDiagnostics = false;

    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image xpBarFill;

    private GameObject hpBarBackground;
    private GameObject xpBarBackground;
    private static bool missingReferenceWarningShown;

    private GameObject polishedHudRoot;
    private TMP_Text levelBadgeText;
    private TMP_Text coinValueText;
    private Image coinIconImage;
    private TMP_Text levelUpFeedbackText;
    private Coroutine levelUpFeedbackRoutine;
    private int lastPolishedLevel = -1;
    private bool polishedHudBuilt;
    private bool gameplayHudVisible = true;
    private static Sprite coinCircleSprite;
    private static Texture2D uiKnobFallbackTexture;
    private static Sprite uiKnobFallbackSprite;

    private static readonly Color LevelBadgeBackground = new Color(0.05f, 0.07f, 0.12f, 0.84f);
    private static readonly Color LevelBadgeBorder = new Color(0.72f, 0.58f, 0.18f, 0.9f);
    private static readonly Color LevelBadgeTextColor = new Color(0.96f, 0.92f, 0.78f, 1f);
    private static readonly Color CoinIconColor = new Color(1f, 0.78f, 0.18f, 1f);
    private static readonly Color CoinIconRingColor = new Color(0.82f, 0.58f, 0.08f, 1f);
    private static readonly Color CoinValueColor = new Color(0.96f, 0.94f, 0.88f, 1f);
    private static readonly Color LevelUpFeedbackColor = new Color(1f, 0.82f, 0.24f, 1f);

    private void Awake()
    {
        Instance = this;
        gameplayHudVisible = ShouldShowGameplayHud();
        EnsureHUDVisible();
    }

    private void OnEnable()
    {
        ApplyGameplayHudVisibility(gameplayHudVisible);
    }

    private void Start()
    {
        ResolveReferences();
        UpdateHP(10, 10);
        UpdateXP(0, 5);
        UpdateLevel(1);
        UpdateWave(1);
        UpdateXPBar(0, 5);
        UpdateCoins(0);
        ApplyGameplayHudVisibility(gameplayHudVisible);
    }

    public static void HideGameplayHud()
    {
        SetGameplayHudVisible(false);
    }

    public static void ShowGameplayHud()
    {
        SetGameplayHudVisible(true);
    }

    public static void SetGameplayHudVisible(bool visible)
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<HUDManager>();
        }

        Instance?.SetGameplayHUDVisible(visible);
    }

    private static bool ShouldShowGameplayHud()
    {
        return MainMenuManager.Instance == null || MainMenuManager.IsRunActive;
    }

    private void ResolveReferences()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        Transform canvasTransform = canvas.transform;

        hpText ??= FindText(canvasTransform, "HPText");
        xpText ??= FindText(canvasTransform, "XPText");
        levelText ??= FindText(canvasTransform, "LevelText");
        waveText ??= FindText(canvasTransform, "WaveText");
        coinText ??= FindText(canvasTransform, "CoinText");

        hpBarBackground ??= canvasTransform.Find("HPBarBackground")?.gameObject;
        xpBarBackground ??= canvasTransform.Find("XPBarBackground")?.gameObject;

        hpBarFill ??= FindBarFill(canvasTransform, "HPBarBackground", "HPBarFill");
        xpBarFill ??= FindBarFill(canvasTransform, "XPBarBackground", "XPBarFill");

        if (missingReferenceWarningShown)
        {
            return;
        }

        if (hpBarFill == null || xpBarFill == null)
        {
            Debug.LogWarning("HUDManager: HPBarFill veya XPBarFill bulunamadi. Canvas referanslarini kontrol edin.");
            missingReferenceWarningShown = true;
        }
    }

    private void SetupHudLayout()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        canvas.gameObject.SetActive(true);
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        Transform canvasTransform = canvas.transform;

        SetupHudText(hpText, canvasTransform, new Vector2(20f, -20f));
        SetupHudText(xpText, canvasTransform, new Vector2(20f, -60f));
        HideLegacyPrototypeTexts();
        SetupHudBars(canvasTransform);
        BuildPolishedLevelCoinHud(canvas);

        Transform hudPanel = canvasTransform.Find("HUDPanel");

        if (hudPanel != null)
        {
            NeutralizeOverlayPanel(canvasTransform, "HUDPanel");
        }
    }

    private static void SetupHudText(TMP_Text text, Transform canvasTransform, Vector2 anchoredPosition)
    {
        if (text == null)
        {
            return;
        }

        text.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(400f, 40f);
        rectTransform.anchoredPosition = anchoredPosition;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
    }

    private void SetupHudBars(Transform canvasTransform)
    {
        SetupBarBackground(hpBarBackground, canvasTransform, new Vector2(20f, -210f), new Vector2(260f, 18f));
        SetupBarBackground(xpBarBackground, canvasTransform, new Vector2(20f, -235f), new Vector2(260f, 14f));
        EnsureBarFillVisible(hpBarFill, new Color(0.88f, 0.22f, 0.22f, 1f));
        EnsureBarFillVisible(xpBarFill, new Color(0.28f, 0.58f, 1f, 1f));
    }

    private static void SetupBarBackground(GameObject barBackground, Transform canvasTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (barBackground == null || canvasTransform == null)
        {
            return;
        }

        barBackground.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = barBackground.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        barBackground.SetActive(true);

        Image backgroundImage = barBackground.GetComponent<Image>();

        if (backgroundImage != null)
        {
            Color backgroundColor = backgroundImage.color;
            backgroundColor.a = 0.9f;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
        }
    }

    private static void EnsureBarFillVisible(Image barFill, Color fillColor)
    {
        if (barFill == null)
        {
            return;
        }

        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.color = fillColor;
        barFill.raycastTarget = false;
        barFill.gameObject.SetActive(true);
    }

    private void EnsureGameplayHudElementsVisible()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        Transform levelUpPanel = canvas.transform.Find("LevelUpPanel");

        if (levelUpPanel != null && MainMenuManager.IsRunActive && Time.timeScale > 0f)
        {
            levelUpPanel.gameObject.SetActive(false);
        }

        Transform mainMenuPanel = canvas.transform.Find("MainMenuPanel");

        if (mainMenuPanel != null && MainMenuManager.IsRunActive)
        {
            mainMenuPanel.gameObject.SetActive(false);
        }

        EnsureTextVisible(hpText);
        EnsureTextVisible(xpText);
        HideLegacyPrototypeTexts();
        EnsurePolishedHudVisible();
    }

    private static void EnsureTextVisible(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        text.color = Color.white;
        text.alpha = 1f;
        text.gameObject.SetActive(true);
    }

    private TMP_Text FindText(Transform canvasTransform, string objectName)
    {
        Transform target = canvasTransform.Find(objectName);

        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindBarFill(Transform canvasTransform, string backgroundName, string fillName)
    {
        Transform background = canvasTransform.Find(backgroundName);

        if (background == null)
        {
            return null;
        }

        Transform fill = background.Find(fillName);

        return fill != null ? fill.GetComponent<Image>() : null;
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        if (hpText != null)
        {
            hpText.text = "HP " + currentHP + " / " + maxHP;
        }

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        }
    }

    public void UpdateXPBar(int currentXP, int xpToNextLevel)
    {
        if (xpBarFill == null)
        {
            return;
        }

        if (xpToNextLevel <= 0)
        {
            xpBarFill.fillAmount = 0f;
            return;
        }

        xpBarFill.fillAmount = (float)currentXP / xpToNextLevel;
    }

    public void UpdateXP(int currentXP, int xpToNextLevel)
    {
        if (xpText == null)
        {
            return;
        }

        xpText.text = "XP " + currentXP + " / " + xpToNextLevel;
    }

    public void UpdateLevel(int currentLevel)
    {
        if (lastPolishedLevel > 0 && currentLevel > lastPolishedLevel)
        {
            ShowLevelUpFeedback();
        }

        lastPolishedLevel = currentLevel;
        UpdateLevelBadge(currentLevel);

        if (levelText != null)
        {
            levelText.text = "LEVEL " + currentLevel;
        }
    }

    public void UpdateWave(int wave)
    {
        if (waveText != null)
        {
            waveText.text = "WAVE " + wave;
        }
    }

    public void UpdateCoins(int coins)
    {
        UpdateCoinDisplay(coins);

        if (coinText != null)
        {
            coinText.text = "COINS " + coins;
        }
    }

    public void EnsureHUDVisible()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;
        canvas.gameObject.SetActive(true);

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        if (canvasRect != null && canvasRect.localScale.sqrMagnitude < 0.0001f)
        {
            canvasRect.localScale = Vector3.one;
        }

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        ResolveReferences();
        SetupHudLayout();

        gameObject.SetActive(true);
        ApplyGameplayHudVisibility(gameplayHudVisible);

        if (LogRecoveryDiagnostics)
        {
            Debug.Log("[Recovery] HUD visible");
        }
    }

    public void OnGameplayStarted()
    {
        EnsureHUDVisible();
    }

    public void SetGameplayHUDVisible(bool visible)
    {
        ApplyGameplayHudVisibility(visible);
    }

    private void ApplyGameplayHudVisibility(bool visible)
    {
        gameplayHudVisible = visible;

        if (visible)
        {
            Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

            if (canvas != null)
            {
                canvas.gameObject.SetActive(true);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                RectTransform canvasRect = canvas.GetComponent<RectTransform>();

                if (canvasRect != null && canvasRect.localScale.sqrMagnitude < 0.0001f)
                {
                    canvasRect.localScale = Vector3.one;
                }
            }

            ResolveReferences();
            SetupHudLayout();
            EnsureGameplayHudElementsVisible();
        }

        SetElementVisible(hpText, visible);
        SetElementVisible(xpText, visible);
        HideLegacyPrototypeTexts();
        SetElementVisible(polishedHudRoot, visible);
        SetElementVisible(hpBarBackground, visible);
        SetElementVisible(xpBarBackground, visible);
        SetElementVisible(hpBarFill, visible);
        SetElementVisible(xpBarFill, visible);
        SetElementVisible(levelUpFeedbackText, visible);
    }

    public void ForceHudElementsVisibleForRecovery()
    {
        ResolveReferences();

        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        canvas.gameObject.SetActive(true);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 100;
        canvas.overrideSorting = true;

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        Transform canvasTransform = canvas.transform;

        SetCanvasChildActive(canvasTransform, "MainMenuPanel", false);

        if (MainMenuManager.IsRunActive && Time.timeScale > 0f)
        {
            SetCanvasChildActive(canvasTransform, "LevelUpPanel", false);
        }

        NeutralizeOverlayPanel(canvasTransform, "HUDPanel");
        NeutralizeOverlayPanel(canvasTransform, "StatsPanel");

        BuildPolishedLevelCoinHud(canvas);
        HideLegacyPrototypeTexts();

        ForceBottomLeftText(hpText, canvasTransform, new Vector2(24f, 52f));
        ForceBottomLeftText(xpText, canvasTransform, new Vector2(24f, 88f));

        ForceHpBarLayout(canvasTransform);
        ForceXpBarLayout(canvasTransform);

        BringHudElementsToFront(canvasTransform);

        if (LogRecoveryDiagnostics)
        {
            Debug.Log("[Recovery] Gameplay HUD forced visible after PlayGame");
            LogRectDiagnostic("HP", hpBarBackground);
            LogRectDiagnostic("XP", xpBarBackground);
            LogHudPanelDiagnostic(canvasTransform);
        }
    }

    private static void NeutralizeOverlayPanel(Transform canvasTransform, string panelName)
    {
        if (canvasTransform == null)
        {
            return;
        }

        Transform panel = canvasTransform.Find(panelName);

        if (panel == null)
        {
            return;
        }

        panel.gameObject.SetActive(true);

        Image panelImage = panel.GetComponent<Image>();

        if (panelImage != null)
        {
            Color panelColor = panelImage.color;
            panelColor.a = 0f;
            panelImage.color = panelColor;
            panelImage.raycastTarget = false;
            panelImage.enabled = false;
        }
    }

    private static void ForceTopLeftText(TMP_Text text, Transform parent, Vector2 anchoredPosition)
    {
        if (text == null || parent == null)
        {
            return;
        }

        text.transform.SetParent(parent, false);

        RectTransform rectTransform = text.rectTransform;
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(420f, 32f);
        rectTransform.anchoredPosition = anchoredPosition;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.alpha = 1f;
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
    }

    private static void ForceBottomLeftText(TMP_Text text, Transform parent, Vector2 anchoredPosition)
    {
        if (text == null || parent == null)
        {
            return;
        }

        text.transform.SetParent(parent, false);

        RectTransform rectTransform = text.rectTransform;
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.sizeDelta = new Vector2(420f, 28f);
        rectTransform.anchoredPosition = anchoredPosition;
        text.fontSize = 22f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.alpha = 1f;
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
    }

    private void ForceHpBarLayout(Transform canvasTransform)
    {
        if (hpBarBackground == null || canvasTransform == null)
        {
            return;
        }

        hpBarBackground.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = hpBarBackground.GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.anchoredPosition = new Vector2(24f, 24f);
        rectTransform.sizeDelta = new Vector2(260f, 22f);
        hpBarBackground.SetActive(true);

        Image backgroundImage = hpBarBackground.GetComponent<Image>();

        if (backgroundImage != null)
        {
            Color backgroundColor = backgroundImage.color;
            backgroundColor.a = 0.45f;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
            backgroundImage.enabled = true;
        }

        if (hpBarFill != null)
        {
            EnsureBarFillVisible(hpBarFill, new Color(0.88f, 0.22f, 0.22f, 1f));
        }
    }

    private void ForceXpBarLayout(Transform canvasTransform)
    {
        if (xpBarBackground == null || canvasTransform == null)
        {
            return;
        }

        xpBarBackground.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = xpBarBackground.GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(1f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.offsetMin = new Vector2(32f, 8f);
        rectTransform.offsetMax = new Vector2(-32f, 14f);
        xpBarBackground.SetActive(true);

        Image backgroundImage = xpBarBackground.GetComponent<Image>();

        if (backgroundImage != null)
        {
            Color backgroundColor = backgroundImage.color;
            backgroundColor.a = 0.35f;
            backgroundImage.color = backgroundColor;
            backgroundImage.raycastTarget = false;
            backgroundImage.enabled = true;
        }

        if (xpBarFill != null)
        {
            EnsureBarFillVisible(xpBarFill, new Color(0.28f, 0.58f, 1f, 1f));
        }
    }

    private static void SetCanvasChildActive(Transform canvasTransform, string childName, bool active)
    {
        if (canvasTransform == null)
        {
            return;
        }

        Transform child = canvasTransform.Find(childName);

        if (child != null)
        {
            child.gameObject.SetActive(active);
        }
    }

    private void BringHudElementsToFront(Transform canvasTransform)
    {
        BringToFront(polishedHudRoot != null ? polishedHudRoot.transform : null);
        BringToFront(hpText != null ? hpText.transform : null);
        BringToFront(xpText != null ? xpText.transform : null);
        BringToFront(hpBarBackground != null ? hpBarBackground.transform : null);
        BringToFront(xpBarBackground != null ? xpBarBackground.transform : null);
        BringToFront(levelUpFeedbackText != null ? levelUpFeedbackText.transform : null);
    }

    private static void LogRectDiagnostic(string label, GameObject target)
    {
        if (target == null)
        {
            Debug.Log("[Recovery] " + label + " rect missing");
            return;
        }

        RectTransform rectTransform = target.GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.Log("[Recovery] " + label + " rect missing");
            return;
        }

        Debug.Log(
            "[Recovery] "
            + label
            + " anchoredPosition="
            + rectTransform.anchoredPosition
            + " sizeDelta="
            + rectTransform.sizeDelta
            + " active="
            + target.activeSelf);
    }

    private static void LogTextDiagnostic(string label, TMP_Text text)
    {
        if (text == null)
        {
            Debug.Log("[Recovery] " + label + " text missing");
            return;
        }

        Debug.Log(
            "[Recovery] "
            + label
            + " anchoredPosition="
            + text.rectTransform.anchoredPosition
            + " textAlpha="
            + text.alpha);
    }

    private static void LogHudPanelDiagnostic(Transform canvasTransform)
    {
        Transform hudPanel = canvasTransform != null ? canvasTransform.Find("HUDPanel") : null;

        if (hudPanel == null)
        {
            Debug.Log("[Recovery] HUDPanel imageAlpha=missing");
            return;
        }

        Image panelImage = hudPanel.GetComponent<Image>();
        float alpha = panelImage != null ? panelImage.color.a : -1f;

        Debug.Log("[Recovery] HUDPanel imageAlpha=" + alpha + " imageEnabled=" + (panelImage != null && panelImage.enabled));
    }

    private static void BringToFront(Transform target)
    {
        if (target != null)
        {
            target.SetAsLastSibling();
        }
    }

    private static void SetElementVisible(Component component, bool visible)
    {
        if (component != null)
        {
            component.gameObject.SetActive(visible);
        }
    }

    private static void SetElementVisible(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }

    private void HideLegacyPrototypeTexts()
    {
        SetElementVisible(levelText != null ? levelText.gameObject : null, false);
        SetElementVisible(coinText != null ? coinText.gameObject : null, false);
        SetElementVisible(waveText != null ? waveText.gameObject : null, false);
    }

    private void EnsurePolishedHudVisible()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();
        BuildPolishedLevelCoinHud(canvas);

        if (polishedHudRoot != null)
        {
            polishedHudRoot.SetActive(gameplayHudVisible);
        }
    }

    private void BuildPolishedLevelCoinHud(Canvas canvas)
    {
        if (polishedHudBuilt || canvas == null)
        {
            return;
        }

        Transform parent = canvas.transform.Find("StatsPanel") ?? canvas.transform;

        polishedHudRoot = new GameObject("PolishedLevelCoinHud");
        polishedHudRoot.transform.SetParent(parent, false);

        RectTransform rootRect = polishedHudRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(220f, 72f);

        GameObject levelBadgeRoot = CreateHudPanel(
            polishedHudRoot.transform,
            "LevelBadge",
            new Vector2(24f, -22f),
            new Vector2(92f, 30f),
            LevelBadgeBackground,
            LevelBadgeBorder);

        levelBadgeText = CreateHudLabel(
            levelBadgeRoot.transform,
            "LevelBadgeText",
            Vector2.zero,
            new Vector2(92f, 30f),
            18f,
            FontStyles.Bold,
            LevelBadgeTextColor,
            TextAlignmentOptions.Center);

        GameObject coinRowRoot = new GameObject("CoinRow");
        coinRowRoot.transform.SetParent(polishedHudRoot.transform, false);

        RectTransform coinRowRect = coinRowRoot.AddComponent<RectTransform>();
        coinRowRect.anchorMin = new Vector2(0f, 1f);
        coinRowRect.anchorMax = new Vector2(0f, 1f);
        coinRowRect.pivot = new Vector2(0f, 1f);
        coinRowRect.anchoredPosition = new Vector2(24f, -58f);
        coinRowRect.sizeDelta = new Vector2(160f, 24f);

        coinIconImage = CreateCoinIcon(coinRowRoot.transform);

        coinValueText = CreateHudLabel(
            coinRowRoot.transform,
            "CoinValueText",
            new Vector2(28f, 0f),
            new Vector2(120f, 24f),
            22f,
            FontStyles.Bold,
            CoinValueColor,
            TextAlignmentOptions.MidlineLeft);

        levelUpFeedbackText = CreateHudLabel(
            canvas.transform,
            "LevelUpFeedbackText",
            new Vector2(0f, -168f),
            new Vector2(360f, 48f),
            34f,
            FontStyles.Bold,
            LevelUpFeedbackColor,
            TextAlignmentOptions.Center);

        RectTransform feedbackRect = levelUpFeedbackText.rectTransform;
        feedbackRect.anchorMin = new Vector2(0.5f, 1f);
        feedbackRect.anchorMax = new Vector2(0.5f, 1f);
        feedbackRect.pivot = new Vector2(0.5f, 1f);
        levelUpFeedbackText.text = "LEVEL UP";
        levelUpFeedbackText.gameObject.SetActive(false);

        polishedHudBuilt = true;
        UpdateLevelBadge(lastPolishedLevel > 0 ? lastPolishedLevel : 1);
    }

    private void UpdateLevelBadge(int currentLevel)
    {
        if (levelBadgeText == null)
        {
            return;
        }

        levelBadgeText.text = "LVL " + currentLevel;
    }

    private void UpdateCoinDisplay(int coins)
    {
        if (coinValueText == null)
        {
            return;
        }

        coinValueText.text = coins.ToString();
    }

    private void ShowLevelUpFeedback()
    {
        if (levelUpFeedbackText == null)
        {
            return;
        }

        if (levelUpFeedbackRoutine != null)
        {
            StopCoroutine(levelUpFeedbackRoutine);
        }

        levelUpFeedbackRoutine = StartCoroutine(LevelUpFeedbackRoutine());
    }

    private IEnumerator LevelUpFeedbackRoutine()
    {
        RectTransform feedbackRect = levelUpFeedbackText.rectTransform;
        Vector2 startPosition = new Vector2(0f, -168f);
        Color color = LevelUpFeedbackColor;

        levelUpFeedbackText.gameObject.SetActive(true);
        levelUpFeedbackText.text = "LEVEL UP";
        feedbackRect.anchoredPosition = startPosition;
        color.a = 0f;
        levelUpFeedbackText.color = color;

        const float fadeInDuration = 0.16f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeInDuration);
            levelUpFeedbackText.color = color;
            yield return null;
        }

        color.a = 1f;
        levelUpFeedbackText.color = color;

        const float holdDuration = 0.42f;
        float holdElapsed = 0f;

        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        const float fadeOutDuration = 0.42f;
        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            color.a = 1f - progress;
            levelUpFeedbackText.color = color;
            feedbackRect.anchoredPosition = startPosition + new Vector2(0f, progress * 28f);
            yield return null;
        }

        color.a = 0f;
        levelUpFeedbackText.color = color;
        feedbackRect.anchoredPosition = startPosition;
        levelUpFeedbackText.gameObject.SetActive(false);
        levelUpFeedbackRoutine = null;
    }

    private TMP_Text CreateHudLabel(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        CopyTmpFontFrom(hpText ?? xpText ?? levelText, textMesh);
        textMesh.alignment = alignment;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.color = color;
        textMesh.richText = false;
        textMesh.raycastTarget = false;
        textMesh.overflowMode = TextOverflowModes.Overflow;

        return textMesh;
    }

    private static GameObject CreateHudPanel(
        Transform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        Color backgroundColor,
        Color borderColor)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image backgroundImage = panelObject.AddComponent<Image>();
        backgroundImage.sprite = GetUiKnobSprite();
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;

        GameObject borderObject = new GameObject("Border");
        borderObject.transform.SetParent(panelObject.transform, false);

        RectTransform borderRect = borderObject.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-1f, -1f);
        borderRect.offsetMax = new Vector2(1f, 1f);
        borderObject.transform.SetAsFirstSibling();

        Image borderImage = borderObject.AddComponent<Image>();
        borderImage.sprite = GetUiKnobSprite();
        borderImage.type = Image.Type.Sliced;
        borderImage.color = borderColor;
        borderImage.raycastTarget = false;

        return panelObject;
    }

    private static Image CreateCoinIcon(Transform parent)
    {
        GameObject ringObject = new GameObject("CoinIconRing");
        ringObject.transform.SetParent(parent, false);

        RectTransform ringRect = ringObject.AddComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0f, 1f);
        ringRect.anchorMax = new Vector2(0f, 1f);
        ringRect.pivot = new Vector2(0f, 1f);
        ringRect.anchoredPosition = Vector2.zero;
        ringRect.sizeDelta = new Vector2(22f, 22f);

        Image ringImage = ringObject.AddComponent<Image>();
        ringImage.sprite = GetCoinCircleSprite();
        ringImage.color = CoinIconRingColor;
        ringImage.raycastTarget = false;

        GameObject iconObject = new GameObject("CoinIcon");
        iconObject.transform.SetParent(ringObject.transform, false);

        RectTransform iconRect = iconObject.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(16f, 16f);

        Image iconImage = iconObject.AddComponent<Image>();
        iconImage.sprite = GetCoinCircleSprite();
        iconImage.color = CoinIconColor;
        iconImage.raycastTarget = false;

        return iconImage;
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

    private static Sprite GetUiKnobSprite()
    {
        if (uiKnobFallbackSprite != null)
        {
            return uiKnobFallbackSprite;
        }

        uiKnobFallbackTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        uiKnobFallbackTexture.SetPixel(0, 0, Color.white);
        uiKnobFallbackTexture.Apply(false, true);

        uiKnobFallbackSprite = Sprite.Create(
            uiKnobFallbackTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        uiKnobFallbackSprite.name = "HUD_FallbackKnobSprite";

        return uiKnobFallbackSprite;
    }

    private static Sprite GetCoinCircleSprite()
    {
        if (coinCircleSprite != null)
        {
            return coinCircleSprite;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radius = size * 0.42f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float alpha = distance <= radius ? 1f : Mathf.Clamp01(radius + 1.2f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        coinCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f);
        return coinCircleSprite;
    }
}
