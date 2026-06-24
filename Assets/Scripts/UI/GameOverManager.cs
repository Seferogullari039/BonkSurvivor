using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private TMP_Text summaryText;
    private Button quitButton;
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

        EnsureMainMenuButton();
        EnsureQuitButton();
        EnsureSummaryUi();
        ApplyGameOverPanelLayout();
    }

    private void ApplyGameOverPanelLayout()
    {
        if (gameOverPanel == null) return;

        Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(760f, 620f));

        LayoutGameOverText(titleText, new Vector2(0f, 250f), new Vector2(700f, 48f), 34f);

        if (summaryText != null)
        {
            LayoutGameOverText(summaryText, new Vector2(0f, 20f), new Vector2(700f, 430f), 17f);
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.lineSpacing = 2f;
        }

        if (restartButton != null)
        {
            UiLayoutUtility.SetAnchorCenter(restartButton.GetComponent<RectTransform>(), new Vector2(0f, -250f), new Vector2(220f, 44f));
        }

        if (mainMenuButton != null)
        {
            UiLayoutUtility.SetAnchorCenter(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0f, -300f), new Vector2(220f, 44f));
        }

        if (quitButton != null)
        {
            UiLayoutUtility.SetAnchorCenter(quitButton.GetComponent<RectTransform>(), new Vector2(0f, -350f), new Vector2(220f, 44f));
        }
    }

    private static void LayoutGameOverText(TMP_Text text, Vector2 position, Vector2 size, float fontSize)
    {
        if (text == null) return;

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

        if (gameOverPanel == null) return;

        Transform panelRoot = gameOverPanel.transform;

        titleText ??= FindText(panelRoot, "GameOverTitleText");
        waveText ??= FindText(panelRoot, "GameOverWaveText");
        levelText ??= FindText(panelRoot, "GameOverLevelText");
        coinsText ??= FindText(panelRoot, "GameOverCoinsText");
        summaryText ??= FindText(panelRoot, "RunSummaryText");

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
    }

    private void EnsureSummaryUi()
    {
        if (summaryUiBuilt || gameOverPanel == null)
        {
            return;
        }

        Transform panelRoot = gameOverPanel.transform;

        if (summaryText == null)
        {
            summaryText = CreateSummaryText(panelRoot);
        }

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
        if (isShown) return;

        isShown = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        if (summaryText != null)
        {
            summaryText.text = snapshot.BuildSummaryText();
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }

        AudioManager.Instance?.PlayGameOver();
    }

    public void HideGameOver()
    {
        isShown = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null) return;

        Transform overlay = canvas.transform.Find("GameOverDimOverlay");

        if (overlay != null)
        {
            overlay.gameObject.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void EnsureMainMenuButton()
    {
        if (gameOverPanel == null) return;

        Transform panelRoot = gameOverPanel.transform;

        if (mainMenuButton == null)
        {
            Transform existingButton = panelRoot.Find("MainMenuButton");

            if (existingButton != null)
            {
                mainMenuButton = existingButton.GetComponent<Button>();
            }
            else
            {
                mainMenuButton = CreatePanelButton(panelRoot, "MainMenuButton", "Main Menu", new Color(0.2f, 0.35f, 0.55f, 0.95f));
            }
        }

        if (mainMenuButton == null) return;

        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClick();
            GoToMainMenu();
        });
    }

    private void EnsureQuitButton()
    {
        if (gameOverPanel == null) return;

        Transform panelRoot = gameOverPanel.transform;

        if (quitButton == null)
        {
            Transform existingButton = panelRoot.Find("QuitButton");

            if (existingButton != null)
            {
                quitButton = existingButton.GetComponent<Button>();
            }
            else
            {
                quitButton = CreatePanelButton(panelRoot, "QuitButton", "Quit", new Color(0.35f, 0.18f, 0.18f, 0.95f));
            }
        }

        if (quitButton == null) return;

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

    private static TMP_Text CreateSummaryText(Transform panelRoot)
    {
        GameObject textObject = new GameObject("RunSummaryText");
        textObject.transform.SetParent(panelRoot, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 20f);
        rectTransform.sizeDelta = new Vector2(700f, 430f);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.fontSize = 17f;
        textMesh.alignment = TextAlignmentOptions.TopLeft;
        textMesh.color = Color.white;
        textMesh.raycastTarget = false;
        textMesh.textWrappingMode = TextWrappingModes.Normal;
        textMesh.overflowMode = TextOverflowModes.Overflow;

        return textMesh;
    }

    private static Button CreatePanelButton(Transform panelRoot, string name, string label, Color color)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(panelRoot, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
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
        labelText.fontSize = 20f;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return button;
    }
}
