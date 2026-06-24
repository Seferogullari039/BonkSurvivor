using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    private const float PanelWidth = 860f;
    private const float PanelHeight = 680f;
    private const float ButtonBarHeight = 58f;
    private const float TitleAreaHeight = 56f;
    private const float ContentPadding = 20f;
    private const float ColumnWidth = 390f;
    private const float DimOverlayAlpha = 0.78f;
    private const float PanelBackgroundAlpha = 0.92f;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private GameObject dimOverlay;
    private TMP_Text leftSummaryText;
    private TMP_Text rightSummaryText;
    private RectTransform buttonBarRect;
    private Button quitButton;
    private ScrollRect summaryScrollRect;
    private bool isShown;
    private bool summaryUiBuilt;

    public bool IsGameOverActive => isShown;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayButtonClick();
                RestartGame();
            });
        }

        EnsureSummaryUi();
        EnsureMainMenuButton();
        EnsureQuitButton();
        ApplyGameOverPanelLayout();
    }

    private void ApplyGameOverPanelLayout()
    {
        if (gameOverPanel == null)
        {
            return;
        }

        Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(PanelWidth, PanelHeight));

        Image panelImage = gameOverPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = new Color(0.05f, 0.06f, 0.09f, PanelBackgroundAlpha);
            panelImage.raycastTarget = true;
        }

        if (titleText != null)
        {
            LayoutGameOverText(titleText, new Vector2(0f, PanelHeight * 0.5f - TitleAreaHeight * 0.5f - 8f), new Vector2(PanelWidth - 40f, TitleAreaHeight), 34f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
        }

        if (summaryScrollRect != null)
        {
            RectTransform scrollRect = summaryScrollRect.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(ContentPadding, ButtonBarHeight + ContentPadding);
            scrollRect.offsetMax = new Vector2(-ContentPadding, -(TitleAreaHeight + ContentPadding));
        }

        if (buttonBarRect != null)
        {
            UiLayoutUtility.SetAnchorBottomStretch(buttonBarRect, ContentPadding, ContentPadding, ContentPadding, ButtonBarHeight);
        }

        LayoutButtonBarButtons();
    }

    private void LayoutButtonBarButtons()
    {
        if (buttonBarRect == null)
        {
            return;
        }

        float buttonWidth = 220f;
        float buttonHeight = 44f;
        float gap = 16f;
        float totalWidth = buttonWidth * 3f + gap * 2f;
        float startX = -totalWidth * 0.5f + buttonWidth * 0.5f;

        LayoutBarButton(restartButton, startX, buttonWidth, buttonHeight);
        LayoutBarButton(mainMenuButton, startX + buttonWidth + gap, buttonWidth, buttonHeight);
        LayoutBarButton(quitButton, startX + (buttonWidth + gap) * 2f, buttonWidth, buttonHeight);
    }

    private static void LayoutBarButton(Button button, float x, float width, float height)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(x, ButtonBarHeight * 0.5f), new Vector2(width, height));
    }

    private static void LayoutGameOverText(TMP_Text text, Vector2 position, Vector2 size, float fontSize)
    {
        if (text == null)
        {
            return;
        }

        UiLayoutUtility.SetAnchorCenter(text.rectTransform, position, size);
        text.fontSize = fontSize;
    }

    private void ResolveReferences()
    {
        if (gameOverPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)
            {
                Transform panelTransform = canvas.transform.Find("GameOverPanel");

                if (panelTransform != null)
                {
                    gameOverPanel = panelTransform.gameObject;
                }
            }
        }

        if (gameOverPanel == null)
        {
            return;
        }

        Transform panelRoot = gameOverPanel.transform;

        titleText ??= FindText(panelRoot, "GameOverTitleText");
        waveText ??= FindText(panelRoot, "GameOverWaveText");
        levelText ??= FindText(panelRoot, "GameOverLevelText");
        coinsText ??= FindText(panelRoot, "GameOverCoinsText");
        leftSummaryText ??= FindText(panelRoot, "RunSummaryLeftText");
        rightSummaryText ??= FindText(panelRoot, "RunSummaryRightText");

        if (restartButton == null)
        {
            Transform buttonTransform = panelRoot.Find("RestartButton");

            if (buttonTransform != null)
            {
                restartButton = buttonTransform.GetComponent<Button>();
            }
        }

        if (mainMenuButton == null)
        {
            Transform mainMenuTransform = panelRoot.Find("MainMenuButton");

            if (mainMenuTransform != null)
            {
                mainMenuButton = mainMenuTransform.GetComponent<Button>();
            }
        }

        if (quitButton == null)
        {
            Transform quitTransform = panelRoot.Find("QuitButton");

            if (quitTransform != null)
            {
                quitButton = quitTransform.GetComponent<Button>();
            }
        }

        Transform buttonBarTransform = panelRoot.Find("GameOverButtonBar");

        if (buttonBarTransform != null)
        {
            buttonBarRect = buttonBarTransform.GetComponent<RectTransform>();
        }

        Transform scrollTransform = panelRoot.Find("RunSummaryScroll");

        if (scrollTransform != null)
        {
            summaryScrollRect = scrollTransform.GetComponent<ScrollRect>();
        }
    }

    private void EnsureSummaryUi()
    {
        if (summaryUiBuilt || gameOverPanel == null)
        {
            return;
        }

        Transform panelRoot = gameOverPanel.transform;
        Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();

        EnsureDimOverlay(canvas);
        EnsurePanelBackground(panelRoot);
        EnsureScrollSummary(panelRoot);
        EnsureButtonBar(panelRoot);

        if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }

        if (levelText != null)
        {
            levelText.gameObject.SetActive(false);
        }

        if (coinsText != null)
        {
            coinsText.gameObject.SetActive(false);
        }

        summaryUiBuilt = true;
    }

    private void EnsureDimOverlay(Canvas canvas)
    {
        if (canvas == null || dimOverlay != null)
        {
            return;
        }

        Transform existing = canvas.transform.Find("GameOverDimOverlay");

        if (existing != null)
        {
            dimOverlay = existing.gameObject;
            Image existingImage = dimOverlay.GetComponent<Image>();

            if (existingImage != null)
            {
                existingImage.color = new Color(0f, 0f, 0f, DimOverlayAlpha);
            }

            dimOverlay.SetActive(false);
            return;
        }

        dimOverlay = new GameObject("GameOverDimOverlay");
        dimOverlay.transform.SetParent(canvas.transform, false);
        dimOverlay.transform.SetAsFirstSibling();

        RectTransform rectTransform = dimOverlay.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = dimOverlay.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, DimOverlayAlpha);
        image.raycastTarget = true;
        dimOverlay.SetActive(false);
    }

    private static void EnsurePanelBackground(Transform panelRoot)
    {
        Image panelImage = panelRoot.GetComponent<Image>();

        if (panelImage == null)
        {
            panelImage = panelRoot.gameObject.AddComponent<Image>();
        }

        panelImage.color = new Color(0.05f, 0.06f, 0.09f, PanelBackgroundAlpha);
        panelImage.raycastTarget = true;
    }

    private void EnsureScrollSummary(Transform panelRoot)
    {
        if (summaryScrollRect != null)
        {
            return;
        }

        GameObject scrollObject = new GameObject("RunSummaryScroll");
        scrollObject.transform.SetParent(panelRoot, false);

        RectTransform scrollRectTransform = scrollObject.AddComponent<RectTransform>();
        summaryScrollRect = scrollObject.AddComponent<ScrollRect>();
        summaryScrollRect.horizontal = false;
        summaryScrollRect.vertical = true;
        summaryScrollRect.movementType = ScrollRect.MovementType.Clamped;
        summaryScrollRect.scrollSensitivity = 24f;

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);

        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(viewportRect);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.12f);
        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);

        RectTransform contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 520f);

        ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        leftSummaryText = CreateColumnText(contentObject.transform, "RunSummaryLeftText", 0f);
        rightSummaryText = CreateColumnText(contentObject.transform, "RunSummaryRightText", ColumnWidth + 16f);

        summaryScrollRect.viewport = viewportRect;
        summaryScrollRect.content = contentRect;
    }

    private void EnsureButtonBar(Transform panelRoot)
    {
        if (buttonBarRect != null)
        {
            MoveButtonsToBar(buttonBarRect);
            return;
        }

        GameObject barObject = new GameObject("GameOverButtonBar");
        barObject.transform.SetParent(panelRoot, false);
        buttonBarRect = barObject.AddComponent<RectTransform>();

        Image barImage = barObject.AddComponent<Image>();
        barImage.color = new Color(0.03f, 0.04f, 0.06f, 0.55f);
        barImage.raycastTarget = false;

        if (restartButton == null)
        {
            restartButton = CreatePanelButton(barObject.transform, "RestartButton", "Restart Run", new Color(0.72f, 0.28f, 0.18f, 0.96f));
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = CreatePanelButton(barObject.transform, "MainMenuButton", "Main Menu", new Color(0.22f, 0.38f, 0.58f, 0.96f));
        }

        if (quitButton == null)
        {
            quitButton = CreatePanelButton(barObject.transform, "QuitButton", "Quit", new Color(0.18f, 0.18f, 0.2f, 0.96f));
        }

        MoveButtonsToBar(buttonBarRect);
    }

    private void MoveButtonsToBar(RectTransform bar)
    {
        MoveChildToBar(restartButton, bar);
        MoveChildToBar(mainMenuButton, bar);
        MoveChildToBar(quitButton, bar);
    }

    private static void MoveChildToBar(Button button, RectTransform bar)
    {
        if (button == null || bar == null)
        {
            return;
        }

        button.transform.SetParent(bar, false);
    }

    private TMP_Text FindText(Transform parent, string objectName)
    {
        Transform target = parent.Find(objectName);

        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    public void ShowGameOver(int wave, int level, int coins)
    {
        RunStatsTracker tracker = RunStatsTracker.GetOrCreate();
        tracker.RecordWaveReached(wave);
        tracker.RecordLevelReached(level);
        tracker.EndRun();
        ShowGameOver(tracker.CreateSnapshot());
    }

    public void ShowGameOver(RunStatsSnapshot snapshot)
    {
        if (isShown)
        {
            return;
        }

        isShown = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PauseMenuManager.HidePauseMenuIfExists();
        HUDManager.HideGameplayHud();
        RunBuildHud.HideHud();
        ChestStatBuffHud.HideHud();

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayGameOver();
        }

        EnsureSummaryUi();
        ShowGameOverPanel(snapshot);
    }

    private void ShowGameOverPanel(RunStatsSnapshot snapshot)
    {
        if (titleText != null)
        {
            titleText.text = "RUN SUMMARY";
        }

        if (leftSummaryText != null)
        {
            leftSummaryText.text = RunStatsSummaryFormatter.FormatLeftColumn(snapshot);
        }

        if (rightSummaryText != null)
        {
            rightSummaryText.text = RunStatsSummaryFormatter.FormatRightColumn(snapshot);
        }

        if (dimOverlay != null)
        {
            dimOverlay.SetActive(true);
            dimOverlay.transform.SetAsLastSibling();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        if (summaryScrollRect != null)
        {
            summaryScrollRect.verticalNormalizedPosition = 1f;
        }

        UpdateScrollContentHeight();
        ApplyGameOverPanelLayout();
        AudioManager.Instance?.PlayGameOver();
    }

    private void UpdateScrollContentHeight()
    {
        if (summaryScrollRect == null || summaryScrollRect.content == null)
        {
            return;
        }

        if (leftSummaryText == null && rightSummaryText == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        float leftHeight = leftSummaryText != null ? leftSummaryText.preferredHeight : 0f;
        float rightHeight = rightSummaryText != null ? rightSummaryText.preferredHeight : 0f;
        float contentHeight = Mathf.Max(360f, Mathf.Max(leftHeight, rightHeight) + 24f);
        summaryScrollRect.content.sizeDelta = new Vector2(0f, contentHeight);
    }

    public void HideGameOver()
    {
        isShown = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (dimOverlay != null)
        {
            dimOverlay.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void EnsureMainMenuButton()
    {
        if (gameOverPanel == null)
        {
            return;
        }

        if (mainMenuButton == null)
        {
            return;
        }

        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClick();
            GoToMainMenu();
        });
    }

    private void EnsureQuitButton()
    {
        if (quitButton == null)
        {
            return;
        }

        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClick();
            QuitGame();
        });
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.ReturnToMainMenu();
        }
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

    private static TMP_Text CreateColumnText(Transform parent, string objectName, float x)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopLeft(rectTransform, x, 0f, ColumnWidth, 100f);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.fontSize = 16f;
        textMesh.alignment = TextAlignmentOptions.TopLeft;
        textMesh.color = new Color(0.92f, 0.94f, 0.97f, 1f);
        textMesh.raycastTarget = false;
        textMesh.textWrappingMode = TextWrappingModes.Normal;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.lineSpacing = 2f;

        ContentSizeFitter fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return textMesh;
    }

    private static Button CreatePanelButton(Transform parent, string name, string label, Color color)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(220f, 44f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 19f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return button;
    }
}
