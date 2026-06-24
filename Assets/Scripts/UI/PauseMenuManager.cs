using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    public static bool IsGameplayPaused => Instance != null && Instance.isPaused;

    private GameObject pausePanel;
    private TMP_Text runInfoText;
    private TMP_Text buildInfoText;
    private TMP_Text chestBuffsText;
    private bool isPaused;
    private bool isBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<PauseMenuManager>() != null)
        {
            return;
        }

        GameObject host = new GameObject("PauseMenuManager");
        host.AddComponent<PauseMenuManager>();
    }

    public static void HidePauseMenuIfExists()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.ForceHide();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildPauseUI();
        ForceHide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (!MainMenuManager.IsRunActive)
        {
            return;
        }

        if (ShouldBlockPauseInput())
        {
            return;
        }

        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    private static bool ShouldBlockPauseInput()
    {
        if (DevAdminPanel.IsOpen)
        {
            return true;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return true;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksGameplayPause)
        {
            return true;
        }

        if (MerchantShrineUI.IsOpen)
        {
            return true;
        }

        return false;
    }

    public void Pause()
    {
        if (isPaused || !MainMenuManager.IsRunActive || ShouldBlockPauseInput())
        {
            return;
        }

        isPaused = true;
        RefreshContent();

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void ForceHide()
    {
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void RefreshContent()
    {
        if (runInfoText != null)
        {
            runInfoText.text = BuildRunInfoText();
        }

        if (buildInfoText != null)
        {
            buildInfoText.text = BuildBuildInfoText();
        }

        if (chestBuffsText != null)
        {
            chestBuffsText.text = BuildChestBuffsText();
        }
    }

    private static string BuildChestBuffsText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("CHEST BUFFS");

        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null)
        {
            tracker = FindFirstObjectByType<ChestStatBuffTracker>();
        }

        if (tracker == null)
        {
            builder.Append("None");
            return builder.ToString();
        }

        IReadOnlyList<ChestStatBuffEntry> buffs = tracker.GetActiveBuffs();

        if (buffs == null || buffs.Count == 0)
        {
            builder.Append("None");
            return builder.ToString();
        }

        for (int i = 0; i < buffs.Count; i++)
        {
            builder.AppendLine(ChestStatBuffTracker.FormatPauseSummaryLine(buffs[i]));
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildRunInfoText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("RUN INFO");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerStats stats = player != null ? player.GetComponent<PlayerStats>() : null;

        if (stats != null)
        {
            builder.AppendLine("Level: " + stats.CurrentLevel);
            builder.AppendLine("HP: " + stats.CurrentHealth + " / " + stats.EffectiveMaxHealth);
            builder.AppendLine("Coins: " + stats.Coins);
            builder.AppendLine("XP: " + stats.CurrentXP + " / " + stats.XPToNextLevel);
        }
        else
        {
            builder.AppendLine("Level: -");
            builder.AppendLine("HP: -");
            builder.AppendLine("Coins: -");
            builder.AppendLine("XP: -");
        }

        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (enemySpawner != null)
        {
            builder.AppendLine("Wave: " + enemySpawner.CurrentWave);
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildBuildInfoText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("BUILD");

        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        builder.AppendLine("SKILLS");
        AppendSlotLines(builder, tracker, RewardCategory.Skill);
        builder.AppendLine();
        builder.AppendLine("PASSIVES");
        AppendSlotLines(builder, tracker, RewardCategory.Passive);

        return builder.ToString().TrimEnd();
    }

    private static void AppendSlotLines(StringBuilder builder, RunBuildTracker tracker, RewardCategory category)
    {
        for (int i = 0; i < RunBuildTracker.MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = category == RewardCategory.Skill
                ? tracker.GetSkillSlot(i)
                : tracker.GetPassiveSlot(i);

            builder.AppendLine(FormatBuildSlotLine(entry));
        }
    }

    private static string FormatBuildSlotLine(RunBuildSlotEntry entry)
    {
        if (entry == null)
        {
            return "empty";
        }

        int maxLevel = UpgradeOptionCatalog.GetMaxLevel(entry.UpgradeIndex);
        bool flameOrbitEvolved = entry.UpgradeIndex == 6
            && RunBuildTracker.Instance != null
            && RunBuildTracker.Instance.HasEvolution(BuildEvolutionId.FlameOrbit);
        string displayName = flameOrbitEvolved ? "Flame Orbit" : entry.DisplayName;

        if (entry.Level >= maxLevel)
        {
            return displayName + " MAX";
        }

        return displayName + " Lv." + entry.Level + "/" + maxLevel;
    }

    private void BuildPauseUI()
    {
        if (isBuilt)
        {
            return;
        }

        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            Debug.LogError("PauseMenuManager: Canvas bulunamadi.");
            return;
        }

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        pausePanel = CreatePanel(canvas.transform, "PauseMenuPanel");
        CreateText(pausePanel.transform, "TitleText", "PAUSED", 44, new Vector2(0f, 300f), new Vector2(500f, 60f), FontStyles.Bold);

        runInfoText = CreateText(
            pausePanel.transform,
            "RunInfoText",
            string.Empty,
            20,
            new Vector2(-170f, 120f),
            new Vector2(340f, 180f),
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);

        buildInfoText = CreateText(
            pausePanel.transform,
            "BuildInfoText",
            string.Empty,
            20,
            new Vector2(170f, 120f),
            new Vector2(340f, 220f),
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);

        chestBuffsText = CreateText(
            pausePanel.transform,
            "ChestBuffsText",
            "CHEST BUFFS\nNone",
            16,
            new Vector2(0f, -70f),
            new Vector2(500f, 90f),
            FontStyles.Normal,
            TextAlignmentOptions.TopLeft);

        CreateMenuButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0f, -150f), OnResumeClicked);
        CreateMenuButton(pausePanel.transform, "RestartButton", "Restart Run", new Vector2(0f, -210f), OnRestartClicked);
        CreateMenuButton(pausePanel.transform, "MainMenuButton", "Main Menu", new Vector2(0f, -270f), OnMainMenuClicked);
        CreateMenuButton(pausePanel.transform, "QuitButton", "Quit", new Vector2(0f, -330f), OnQuitClicked);

        pausePanel.SetActive(false);
        isBuilt = true;
    }

    private void OnResumeClicked()
    {
        Resume();
    }

    private void OnRestartClicked()
    {
        ForceHide();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnMainMenuClicked()
    {
        ForceHide();
        Time.timeScale = 1f;

        if (MainMenuManager.Instance != null)
        {
            MainMenuManager.Instance.ReturnToMainMenu();
            return;
        }

        RunBuildHud.HideHud();
        HUDManager.HideGameplayHud();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnQuitClicked()
    {
        ForceHide();
        Time.timeScale = 1f;
        Application.Quit();
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.78f);
        return panelObject;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyles fontStyle,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.alignment = alignment;
        textMesh.color = Color.white;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private static void CreateMenuButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(290f, 46f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClick();
            onClick.Invoke();
        });

        CreateText(
            buttonObject.transform,
            "Label",
            label,
            22,
            Vector2.zero,
            new Vector2(290f, 46f),
            FontStyles.Normal);
    }
}
