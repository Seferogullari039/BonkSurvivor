using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    private const float PanelWidth = 900f;
    private const float PanelHeight = 660f;
    private const float ButtonBarHeight = 58f;
    private const float TitleAreaHeight = 78f;
    private const float ContentPadding = 20f;
    private const float ColumnWidth = 410f;
    private const float DimOverlayAlpha = 0.62f;
    private const float TransitionDimAlpha = 0.78f;
    private const float PanelBackgroundAlpha = 0.90f;
    private const float RedFlashPeakAlpha = 0.30f;
    private const float RunEndedFadeInDelay = 0.15f;
    private const float RunEndedFadeInDuration = 0.22f;
    private const float RunEndedFadeOutStart = 0.90f;
    private const float RunEndedFadeOutDuration = 0.15f;
    private const float SummaryRevealStart = 1.05f;
    private const float SummaryFadeDuration = 0.25f;

    private const float SummaryScrollFallbackHeight = 520f;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private GameObject dimOverlay;
    private Image dimOverlayImage;
    private GameObject deathTransitionRoot;
    private CanvasGroup deathTransitionGroup;
    private Image redFlashImage;
    private TMP_Text runEndedTitleText;
    private TMP_Text runEndedSubtitleText;
    private RectTransform runEndedTitleRect;
    private CanvasGroup summaryPanelGroup;
    private Coroutine transitionRoutine;
    private bool summaryRevealed;
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
        SetSummaryPanelVisible(false, 0f, false);
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
            LayoutGameOverText(titleText, new Vector2(0f, PanelHeight * 0.5f - TitleAreaHeight * 0.5f + 10f), new Vector2(PanelWidth - 40f, 44f), 40f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(0.96f, 0.94f, 0.90f, 1f);
            titleText.characterSpacing = 4f;
        }

        if (subtitleText != null)
        {
            LayoutGameOverText(subtitleText, new Vector2(0f, PanelHeight * 0.5f - TitleAreaHeight * 0.5f - 28f), new Vector2(PanelWidth - 40f, 24f), 15f);
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.fontStyle = FontStyles.Normal;
            subtitleText.color = new Color(0.62f, 0.66f, 0.72f, 1f);
            subtitleText.characterSpacing = 6f;
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
        subtitleText ??= FindText(panelRoot, "GameOverSubtitleText");
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
        EnsureDeathTransitionUi(canvas);
        EnsurePanelBackground(panelRoot);
        EnsureTitleTexts(panelRoot);
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
        EnsureSummaryPanelGroup();
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
            dimOverlayImage = dimOverlay.GetComponent<Image>();

            if (dimOverlayImage != null)
            {
                dimOverlayImage.color = new Color(0f, 0f, 0f, DimOverlayAlpha);
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

        dimOverlayImage = dimOverlay.AddComponent<Image>();
        dimOverlayImage.color = new Color(0f, 0f, 0f, DimOverlayAlpha);
        dimOverlayImage.raycastTarget = true;
        dimOverlay.SetActive(false);
    }

    private void EnsureDeathTransitionUi(Canvas canvas)
    {
        if (canvas == null || deathTransitionRoot != null)
        {
            return;
        }

        Transform existing = canvas.transform.Find("DeathTransitionRoot");

        if (existing != null)
        {
            deathTransitionRoot = existing.gameObject;
            deathTransitionGroup = deathTransitionRoot.GetComponent<CanvasGroup>();
            redFlashImage = deathTransitionRoot.transform.Find("RedFlashOverlay")?.GetComponent<Image>();
            runEndedTitleText = deathTransitionRoot.transform.Find("RunEndedTitle")?.GetComponent<TMP_Text>();
            runEndedSubtitleText = deathTransitionRoot.transform.Find("RunEndedSubtitle")?.GetComponent<TMP_Text>();
            runEndedTitleRect = runEndedTitleText != null ? runEndedTitleText.rectTransform : null;
            deathTransitionRoot.SetActive(false);
            return;
        }

        deathTransitionRoot = new GameObject("DeathTransitionRoot");
        deathTransitionRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = deathTransitionRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        deathTransitionGroup = deathTransitionRoot.AddComponent<CanvasGroup>();
        deathTransitionGroup.alpha = 0f;
        deathTransitionGroup.interactable = false;
        deathTransitionGroup.blocksRaycasts = true;

        GameObject flashObject = new GameObject("RedFlashOverlay");
        flashObject.transform.SetParent(deathTransitionRoot.transform, false);

        RectTransform flashRect = flashObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(flashRect);

        redFlashImage = flashObject.AddComponent<Image>();
        redFlashImage.color = new Color(0.55f, 0.08f, 0.08f, 0f);
        redFlashImage.raycastTarget = false;

        runEndedTitleText = CreateCenteredTransitionText(
            deathTransitionRoot.transform,
            "RunEndedTitle",
            "RUN ENDED",
            58f,
            new Vector2(0f, 24f),
            FontStyles.Bold,
            new Color(0.96f, 0.96f, 0.98f, 1f));

        runEndedSubtitleText = CreateCenteredTransitionText(
            deathTransitionRoot.transform,
            "RunEndedSubtitle",
            "The run is over.",
            22f,
            new Vector2(0f, -34f),
            FontStyles.Normal,
            new Color(0.72f, 0.74f, 0.78f, 1f));

        runEndedTitleRect = runEndedTitleText.rectTransform;
        deathTransitionRoot.SetActive(false);
    }

    private static TMP_Text CreateCenteredTransitionText(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Vector2 position,
        FontStyles fontStyle,
        Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, position, new Vector2(720f, fontSize + 24f));

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.raycastTarget = false;
        EnsureTmpFont(textMesh);

        return textMesh;
    }

    private void EnsureSummaryPanelGroup()
    {
        if (gameOverPanel == null || summaryPanelGroup != null)
        {
            return;
        }

        summaryPanelGroup = gameOverPanel.GetComponent<CanvasGroup>();

        if (summaryPanelGroup == null)
        {
            summaryPanelGroup = gameOverPanel.AddComponent<CanvasGroup>();
        }
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

    private void EnsureTitleTexts(Transform panelRoot)
    {
        if (titleText == null)
        {
            titleText = CreateHeaderText(
                panelRoot,
                "GameOverTitleText",
                "GAME OVER",
                40f,
                new Vector2(0f, PanelHeight * 0.5f - TitleAreaHeight * 0.5f + 10f),
                new Color(0.96f, 0.94f, 0.90f, 1f),
                FontStyles.Bold,
                4f);
        }

        if (subtitleText == null)
        {
            subtitleText = CreateHeaderText(
                panelRoot,
                "GameOverSubtitleText",
                "RUN SUMMARY",
                15f,
                new Vector2(0f, PanelHeight * 0.5f - TitleAreaHeight * 0.5f - 28f),
                new Color(0.62f, 0.66f, 0.72f, 1f),
                FontStyles.Normal,
                6f);
        }
    }

    private TMP_Text CreateHeaderText(
        Transform parent,
        string objectName,
        string text,
        float fontSize,
        Vector2 position,
        Color color,
        FontStyles fontStyle,
        float characterSpacing)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, position, new Vector2(PanelWidth - 40f, fontSize + 12f));

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.characterSpacing = characterSpacing;
        textMesh.raycastTarget = false;
        textMesh.richText = true;
        EnsureTmpFont(textMesh);

        return textMesh;
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
            restartButton = CreatePanelButton(barObject.transform, "RestartButton", "RESTART RUN", new Color(0.72f, 0.28f, 0.18f, 0.96f));
        }
        else
        {
            SetButtonLabel(restartButton, "RESTART RUN");
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = CreatePanelButton(barObject.transform, "MainMenuButton", "MAIN MENU", new Color(0.22f, 0.38f, 0.58f, 0.96f));
        }
        else
        {
            SetButtonLabel(mainMenuButton, "MAIN MENU");
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
        ActiveWeaponHud.HideHud();

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayGameOver();
        }

        EnsureSummaryUi();

        try
        {
            PrepareSummaryContent(snapshot);
            BeginDeathTransition(snapshot);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameOverManager] Death transition failed, showing summary fallback. {ex.Message}");
            ShowGameOverPanel(snapshot);
        }
    }

    private void BeginDeathTransition(RunStatsSnapshot snapshot)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        summaryRevealed = false;

        if (dimOverlay != null)
        {
            dimOverlay.SetActive(true);

            if (dimOverlayImage != null)
            {
                dimOverlayImage.color = new Color(0f, 0f, 0f, DimOverlayAlpha);
            }
        }

        SetSummaryPanelVisible(false, 0f, false);

        if (deathTransitionRoot != null)
        {
            deathTransitionRoot.SetActive(true);
            deathTransitionRoot.transform.SetAsLastSibling();
        }

        ResetTransitionVisuals();

        if (deathTransitionRoot == null)
        {
            ShowGameOverPanel(snapshot);
            return;
        }

        transitionRoutine = StartCoroutine(PlayDeathTransitionThenSummary(snapshot));
    }

    private void PrepareSummaryContent(RunStatsSnapshot snapshot)
    {
        EnsureTmpFont(titleText);
        EnsureTmpFont(leftSummaryText);
        EnsureTmpFont(rightSummaryText);

        if (titleText != null)
        {
            titleText.text = "GAME OVER";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "RUN SUMMARY";
        }

        if (leftSummaryText != null)
        {
            leftSummaryText.text = RunStatsSummaryFormatter.FormatLeftColumn(snapshot);
        }

        if (rightSummaryText != null)
        {
            rightSummaryText.text = RunStatsSummaryFormatter.FormatRightColumn(snapshot);
        }

        if (summaryScrollRect != null)
        {
            summaryScrollRect.verticalNormalizedPosition = 1f;
        }

        UpdateScrollContentHeight();
        ApplyGameOverPanelLayout();
    }

    private IEnumerator PlayDeathTransitionThenSummary(RunStatsSnapshot snapshot)
    {
        float elapsed = 0f;
        float totalDuration = SummaryRevealStart + SummaryFadeDuration;

        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateDeathTransitionVisuals(elapsed);

            if (!summaryRevealed && elapsed >= SummaryRevealStart)
            {
                summaryRevealed = true;
                RevealSummaryPanel();
            }

            if (summaryRevealed)
            {
                float summaryT = Mathf.Clamp01((elapsed - SummaryRevealStart) / SummaryFadeDuration);
                SetSummaryPanelVisible(true, summaryT, summaryT >= 1f);
            }

            yield return null;
        }

        if (!summaryRevealed)
        {
            RevealSummaryPanel();
        }

        SetSummaryPanelVisible(true, 1f, true);
        HideDeathTransition();
        transitionRoutine = null;
        AudioManager.Instance?.PlayGameOver();
    }

    private void UpdateDeathTransitionVisuals(float elapsed)
    {
        if (dimOverlayImage != null)
        {
            float dimT = Mathf.Clamp01(elapsed / 0.25f);
            float dimAlpha = Mathf.Lerp(DimOverlayAlpha, TransitionDimAlpha, dimT);
            dimOverlayImage.color = new Color(0f, 0f, 0f, dimAlpha);
        }

        if (redFlashImage != null)
        {
            float flashDuration = 0.22f;
            float flashT = Mathf.Clamp01(elapsed / flashDuration);
            float flashAlpha = Mathf.Sin(flashT * Mathf.PI) * RedFlashPeakAlpha;
            redFlashImage.color = new Color(0.55f, 0.08f, 0.08f, flashAlpha);
        }

        if (deathTransitionGroup != null)
        {
            float titleAlpha = 0f;
            float titleScale = 0.92f;

            if (elapsed >= RunEndedFadeInDelay)
            {
                float fadeInT = Mathf.Clamp01((elapsed - RunEndedFadeInDelay) / RunEndedFadeInDuration);
                titleAlpha = fadeInT;

                if (elapsed < RunEndedFadeOutStart)
                {
                    titleScale = Mathf.Lerp(0.92f, 1f, fadeInT);
                }
            }

            if (elapsed >= RunEndedFadeOutStart)
            {
                float fadeOutT = Mathf.Clamp01((elapsed - RunEndedFadeOutStart) / RunEndedFadeOutDuration);
                titleAlpha = 1f - fadeOutT;
                titleScale = Mathf.Lerp(1f, 0.98f, fadeOutT);
            }

            deathTransitionGroup.alpha = 1f;

            if (runEndedTitleText != null)
            {
                Color titleColor = runEndedTitleText.color;
                titleColor.a = titleAlpha;
                runEndedTitleText.color = titleColor;
            }

            if (runEndedSubtitleText != null)
            {
                Color subtitleColor = runEndedSubtitleText.color;
                subtitleColor.a = titleAlpha * 0.85f;
                runEndedSubtitleText.color = subtitleColor;
            }

            if (runEndedTitleRect != null)
            {
                runEndedTitleRect.localScale = Vector3.one * titleScale;
            }
        }
    }

    private void ResetTransitionVisuals()
    {
        if (deathTransitionGroup != null)
        {
            deathTransitionGroup.alpha = 1f;
        }

        if (redFlashImage != null)
        {
            redFlashImage.color = new Color(0.55f, 0.08f, 0.08f, 0f);
        }

        if (runEndedTitleText != null)
        {
            Color titleColor = runEndedTitleText.color;
            titleColor.a = 0f;
            runEndedTitleText.color = titleColor;
        }

        if (runEndedSubtitleText != null)
        {
            Color subtitleColor = runEndedSubtitleText.color;
            subtitleColor.a = 0f;
            runEndedSubtitleText.color = subtitleColor;
        }

        if (runEndedTitleRect != null)
        {
            runEndedTitleRect.localScale = Vector3.one * 0.92f;
        }
    }

    private void HideDeathTransition()
    {
        if (deathTransitionRoot != null)
        {
            deathTransitionRoot.SetActive(false);
        }
    }

    private void RevealSummaryPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        if (dimOverlay != null)
        {
            dimOverlay.transform.SetAsLastSibling();
            gameOverPanel?.transform.SetAsLastSibling();
        }
    }

    private void SetSummaryPanelVisible(bool visible, float alpha, bool interactable)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(visible || alpha > 0f);
        }

        if (summaryPanelGroup != null)
        {
            summaryPanelGroup.alpha = alpha;
            summaryPanelGroup.interactable = interactable;
            summaryPanelGroup.blocksRaycasts = interactable;
        }
    }

    private void ShowGameOverPanel(RunStatsSnapshot snapshot)
    {
        PrepareSummaryContent(snapshot);

        if (dimOverlay != null)
        {
            dimOverlay.SetActive(true);
            dimOverlay.transform.SetAsLastSibling();
        }

        SetSummaryPanelVisible(true, 1f, true);
        RevealSummaryPanel();
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

        const float fallbackColumnHeight = 360f;
        float contentHeight = SummaryScrollFallbackHeight;

        try
        {
            Canvas.ForceUpdateCanvases();

            float leftHeight = TryGetPreferredHeight(leftSummaryText, fallbackColumnHeight);
            float rightHeight = TryGetPreferredHeight(rightSummaryText, fallbackColumnHeight);
            contentHeight = Mathf.Max(fallbackColumnHeight, Mathf.Max(leftHeight, rightHeight) + 24f);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameOverManager] Failed to update summary scroll height, using fallback. {ex.Message}");
        }

        summaryScrollRect.content.sizeDelta = new Vector2(0f, contentHeight);
    }

    private static void EnsureTmpFont(TMP_Text text)
    {
        if (text == null || text.font != null)
        {
            return;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        if (text.font == null)
        {
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (font != null)
            {
                text.font = font;
            }
        }
    }

    private static float TryGetPreferredHeight(TMP_Text text, float fallback)
    {
        if (text == null)
        {
            return 0f;
        }

        EnsureTmpFont(text);

        try
        {
            return text.preferredHeight;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameOverManager] Failed to measure TMP preferred height on '{text.name}', using fallback. {ex.Message}");
            return fallback;
        }
    }

    public void HideGameOver()
    {
        isShown = false;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        HideDeathTransition();
        SetSummaryPanelVisible(false, 0f, false);

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
        ChestRevealPause.ResetForNewRun();
        WorldInteractionPromptUI.EnsureReadyForRun();
        Time.timeScale = 1f;
        MainMenuManager.RequestPlayAfterSceneLoad = true;
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
        textMesh.fontSize = 17f;
        textMesh.alignment = TextAlignmentOptions.TopLeft;
        textMesh.color = new Color(0.90f, 0.92f, 0.96f, 1f);
        textMesh.raycastTarget = false;
        textMesh.textWrappingMode = TextWrappingModes.Normal;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        textMesh.lineSpacing = 4f;
        textMesh.richText = true;

        ContentSizeFitter fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        EnsureTmpFont(textMesh);

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
        labelText.fontSize = 17f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        EnsureTmpFont(labelText);

        return button;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>();

        if (labelText != null)
        {
            labelText.text = label;
            labelText.fontStyle = FontStyles.Bold;
        }
    }
}
