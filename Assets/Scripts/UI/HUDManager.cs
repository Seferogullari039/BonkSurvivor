using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

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

    private void Awake()
    {
        Instance = this;
        EnsureHUDVisible();
    }

    private void OnEnable()
    {
        EnsureHUDVisible();
    }

    private void Start()
    {
        EnsureHUDVisible();
        UpdateHP(10, 10);
        UpdateXP(0, 5);
        UpdateLevel(1);
        UpdateWave(1);
        UpdateXPBar(0, 5);
        UpdateCoins(0);
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
        SetupHudText(levelText, canvasTransform, new Vector2(20f, -100f));
        SetupHudText(waveText, canvasTransform, new Vector2(20f, -140f));
        SetupHudText(coinText, canvasTransform, new Vector2(20f, -180f));
        SetupHudBars(canvasTransform);

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
        EnsureTextVisible(levelText);
        EnsureTextVisible(waveText);
        EnsureTextVisible(coinText);
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
        if (levelText == null)
        {
            return;
        }

        levelText.text = "LEVEL " + currentLevel;
    }

    public void UpdateWave(int wave)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.text = "WAVE " + wave;
    }

    public void UpdateCoins(int coins)
    {
        if (coinText == null)
        {
            return;
        }

        coinText.text = "COINS " + coins;
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
        EnsureGameplayHudElementsVisible();

        gameObject.SetActive(true);
        SetGameplayHUDVisible(true);

        Debug.Log("[Recovery] HUD visible");
    }

    public void OnGameplayStarted()
    {
        EnsureHUDVisible();
    }

    public void SetGameplayHUDVisible(bool visible)
    {
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
        SetElementVisible(levelText, visible);
        SetElementVisible(waveText, visible);
        SetElementVisible(coinText, visible);
        SetElementVisible(hpBarBackground, visible);
        SetElementVisible(xpBarBackground, visible);
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

        Transform statsParent = canvasTransform.Find("StatsPanel") ?? canvasTransform;

        ForceTopLeftText(waveText, statsParent, new Vector2(20f, -20f));
        ForceTopLeftText(levelText, statsParent, new Vector2(20f, -52f));
        ForceTopLeftText(coinText, statsParent, new Vector2(20f, -84f));
        ForceBottomLeftText(hpText, canvasTransform, new Vector2(24f, 52f));
        ForceBottomLeftText(xpText, canvasTransform, new Vector2(24f, 88f));

        ForceHpBarLayout(canvasTransform);
        ForceXpBarLayout(canvasTransform);

        BringHudElementsToFront(canvasTransform);

        Debug.Log("[Recovery] Gameplay HUD forced visible after PlayGame");
        LogRectDiagnostic("HP", hpBarBackground);
        LogRectDiagnostic("XP", xpBarBackground);
        LogTextDiagnostic("Wave", waveText);
        LogTextDiagnostic("Level", levelText);
        LogTextDiagnostic("Coin", coinText);
        LogHudPanelDiagnostic(canvasTransform);
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
        BringToFront(waveText != null ? waveText.transform : null);
        BringToFront(levelText != null ? levelText.transform : null);
        BringToFront(coinText != null ? coinText.transform : null);
        BringToFront(hpText != null ? hpText.transform : null);
        BringToFront(xpText != null ? xpText.transform : null);
        BringToFront(hpBarBackground != null ? hpBarBackground.transform : null);
        BringToFront(xpBarBackground != null ? xpBarBackground.transform : null);
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
}
