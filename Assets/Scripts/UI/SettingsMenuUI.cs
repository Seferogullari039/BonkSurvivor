using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuUI : MonoBehaviour
{
    private const float PanelWidth = 560f;
    private const float PanelHeight = 520f;

    private static SettingsMenuUI instance;

    private GameObject panelRoot;
    private Slider masterVolumeSlider;
    private Slider mouseSensitivitySlider;
    private TMP_Text masterVolumeValueText;
    private TMP_Text mouseSensitivityValueText;
    private TMP_Text fullscreenValueText;
    private bool openedFromPauseMenu;
    private bool isBuilt;

    public static bool IsOpen => instance != null && instance.panelRoot != null && instance.panelRoot.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void OpenFromPauseMenu()
    {
        EnsureInstance().OpenInternal(true);
    }

    public static void OpenFromMainMenu()
    {
        EnsureInstance().OpenInternal(false);
    }

    public static void CloseIfOpen()
    {
        if (instance != null)
        {
            instance.CloseInternal();
        }
    }

    private static SettingsMenuUI EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);

        if (instance != null)
        {
            return instance;
        }

        GameObject host = new GameObject("SettingsMenuUI");
        instance = host.AddComponent<SettingsMenuUI>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildUi();
        HidePanel();
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
        if (!IsOpen)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInternal();
        }
    }

    private void OpenInternal(bool fromPauseMenu)
    {
        if (ShouldBlockOpen())
        {
            return;
        }

        if (!isBuilt)
        {
            BuildUi();
        }

        openedFromPauseMenu = fromPauseMenu;
        GameSettingsManager.Load();
        RefreshControlsFromSettings();
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseInternal()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (openedFromPauseMenu && PauseMenuManager.IsGameplayPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (MainMenuManager.IsRunActive && Time.timeScale > 0f)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private static bool ShouldBlockOpen()
    {
        if (DevAdminPanel.IsOpen)
        {
            return true;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return true;
        }

        if (MerchantShrineUI.IsOpen)
        {
            return true;
        }

        if (ItemOfferHudVisibility.IsGameplaySuppressed)
        {
            return true;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksGameplayPause)
        {
            return true;
        }

        return false;
    }

    private void BuildUi()
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

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        panelRoot = CreatePanel(canvas.transform, "SettingsMenuPanel");
        CreateText(panelRoot.transform, "TitleText", "SETTINGS", 40f, new Vector2(0f, 200f), new Vector2(420f, 56f), FontStyles.Bold);

        CreateText(panelRoot.transform, "MasterVolumeLabel", "Master Volume", 22f, new Vector2(-120f, 110f), new Vector2(220f, 32f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        masterVolumeSlider = CreateSlider(panelRoot.transform, "MasterVolumeSlider", new Vector2(80f, 110f), OnMasterVolumeChanged);
        masterVolumeValueText = CreateText(panelRoot.transform, "MasterVolumeValue", "100%", 20f, new Vector2(250f, 110f), new Vector2(80f, 32f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

        CreateText(panelRoot.transform, "MouseSensitivityLabel", "Mouse Sensitivity", 22f, new Vector2(-120f, 40f), new Vector2(220f, 32f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        mouseSensitivitySlider = CreateSlider(panelRoot.transform, "MouseSensitivitySlider", new Vector2(80f, 40f), OnMouseSensitivityChanged);
        mouseSensitivitySlider.minValue = GameSettingsManager.MinMouseSensitivity;
        mouseSensitivitySlider.maxValue = GameSettingsManager.MaxMouseSensitivity;
        mouseSensitivityValueText = CreateText(panelRoot.transform, "MouseSensitivityValue", "1.00", 20f, new Vector2(250f, 40f), new Vector2(80f, 32f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);

        CreateMenuButton(panelRoot.transform, "FullscreenButton", "Fullscreen: ON", new Vector2(0f, -40f), OnFullscreenClicked);
        fullscreenValueText = panelRoot.transform.Find("FullscreenButton/Label")?.GetComponent<TMP_Text>();

        CreateMenuButton(panelRoot.transform, "ResetDefaultsButton", "Reset Defaults", new Vector2(0f, -110f), OnResetDefaultsClicked);
        CreateMenuButton(panelRoot.transform, "BackButton", "Back", new Vector2(0f, -180f), () => CloseInternal());

        isBuilt = true;
        HidePanel();
    }

    private void RefreshControlsFromSettings()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(GameSettingsManager.MasterVolume);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(GameSettingsManager.MouseSensitivity);
        }

        UpdateValueLabels();
        UpdateFullscreenLabel();
    }

    private void OnMasterVolumeChanged(float value)
    {
        GameSettingsManager.SetMasterVolume(value);
        UpdateValueLabels();
    }

    private void OnMouseSensitivityChanged(float value)
    {
        GameSettingsManager.SetMouseSensitivity(value);
        UpdateValueLabels();
    }

    private void OnFullscreenClicked()
    {
        GameSettingsManager.SetFullscreen(!GameSettingsManager.Fullscreen);
        UpdateFullscreenLabel();
    }

    private void OnResetDefaultsClicked()
    {
        GameSettingsManager.ResetDefaults();
        RefreshControlsFromSettings();
    }

    private void UpdateValueLabels()
    {
        if (masterVolumeValueText != null)
        {
            masterVolumeValueText.text = Mathf.RoundToInt(GameSettingsManager.MasterVolume * 100f) + "%";
        }

        if (mouseSensitivityValueText != null)
        {
            mouseSensitivityValueText.text = GameSettingsManager.MouseSensitivity.ToString("0.00");
        }
    }

    private void UpdateFullscreenLabel()
    {
        if (fullscreenValueText != null)
        {
            fullscreenValueText.text = GameSettingsManager.Fullscreen ? "Fullscreen: ON" : "Fullscreen: OFF";
        }
    }

    private void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private static GameObject CreatePanel(Transform parent, string name)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, 0f, 0f, PanelWidth, PanelHeight);

        Image image = panelObject.AddComponent<Image>();
        image.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
        image.raycastTarget = true;

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.62f, 0.34f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

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
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
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

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);

        RectTransform rectTransform = sliderObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(220f, 24f);

        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        UiLayoutUtility.SetStretch(fillAreaRect, 8f, 8f, 6f, 6f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(fillRect);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.72f, 0.62f, 0.34f, 0.95f);

        GameObject handleSlideArea = new GameObject("Handle Slide Area");
        handleSlideArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleSlideArea.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(handleAreaRect);

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleSlideArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 18f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.9f, 0.92f, 0.96f, 1f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.onValueChanged.AddListener(onValueChanged);

        return slider;
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
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(290f, 46f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.17f, 0.2f, 0.96f);

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
            22f,
            Vector2.zero,
            new Vector2(290f, 46f),
            FontStyles.Normal);
    }
}
