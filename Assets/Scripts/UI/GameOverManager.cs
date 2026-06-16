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

    private bool isShown;

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
        ApplyGameOverPanelLayout();
    }

    private void ApplyGameOverPanelLayout()
    {
        if (gameOverPanel == null) return;

        Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(520f, 360f));

        LayoutGameOverText(titleText, new Vector2(0f, 110f), new Vector2(460f, 56f), 40f);
        LayoutGameOverText(waveText, new Vector2(0f, 50f), new Vector2(460f, 40f), 26f);
        LayoutGameOverText(levelText, new Vector2(0f, 5f), new Vector2(460f, 40f), 26f);
        LayoutGameOverText(coinsText, new Vector2(0f, -40f), new Vector2(460f, 40f), 26f);

        if (restartButton != null)
        {
            UiLayoutUtility.SetAnchorCenter(restartButton.GetComponent<RectTransform>(), new Vector2(0f, -110f), new Vector2(280f, 50f));
        }

        if (mainMenuButton != null)
        {
            UiLayoutUtility.SetAnchorCenter(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0f, -170f), new Vector2(280f, 50f));
        }
    }

    private static void LayoutGameOverText(TMP_Text text, Vector2 position, Vector2 size, float fontSize)
    {
        if (text == null) return;

        UiLayoutUtility.SetAnchorCenter(text.rectTransform, position, size);
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
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
    }

    private TMP_Text FindText(Transform parent, string objectName)
    {
        Transform target = parent.Find(objectName);

        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    public void ShowGameOver(int wave, int level, int coins)
    {
        if (isShown) return;

        isShown = true;
        Time.timeScale = 0f;

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayGameOver();
        }

        ShowGameOverPanel(wave, level, coins);
    }

    private void ShowGameOverPanel(int wave, int level, int coins)
    {
        if (titleText != null)
        {
            titleText.text = "GAME OVER";
        }

        if (waveText != null)
        {
            waveText.text = "Wave: " + wave;
        }

        if (levelText != null)
        {
            levelText.text = "Level: " + level;
        }

        if (coinsText != null)
        {
            coinsText.text = "Coins: " + coins;
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
                mainMenuButton = CreateMainMenuButton(panelRoot);
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

    private void GoToMainMenu()
    {
        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.ReturnToMainMenu();
        }
    }

    private Button CreateMainMenuButton(Transform panelRoot)
    {
        GameObject buttonObject = new GameObject("MainMenuButton");
        buttonObject.transform.SetParent(panelRoot, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, -155f);
        rectTransform.sizeDelta = new Vector2(180f, 40f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.35f, 0.55f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = "Main Menu";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;

        return button;
    }
}
