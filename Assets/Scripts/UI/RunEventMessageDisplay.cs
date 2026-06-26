using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunEventMessageDisplay : MonoBehaviour
{
    public enum Priority
    {
        Normal = 0,
        Wave = 10,
        Elite = 15,
        Event = 20,
        Boss = 30
    }

    public static RunEventMessageDisplay Instance { get; private set; }

    private static readonly Color WaveColor = new Color(0.82f, 0.94f, 1f, 1f);
    private static readonly Color EliteColor = new Color(1f, 0.84f, 0.24f, 1f);
    private static readonly Color MiniBossColor = new Color(1f, 0.46f, 0.18f, 1f);
    private static readonly Color BossColor = new Color(1f, 0.34f, 0.34f, 1f);
    private static readonly Color DragonBossColor = new Color(0.98f, 0.28f, 0.42f, 1f);
    private static readonly Color EventColor = new Color(0.92f, 0.88f, 1f, 1f);
    private static readonly Color BloodMoonColor = new Color(0.95f, 0.18f, 0.16f, 1f);
    private static readonly Color GoldenDragonColor = new Color(1f, 0.82f, 0.18f, 1f);
    private static readonly Color VoidPortalColor = new Color(0.78f, 0.38f, 1f, 1f);
    private static readonly Color MimicColor = new Color(1f, 0.22f, 0.18f, 1f);

    private readonly List<PendingMessage> pendingMessages = new List<PendingMessage>();
    private TextMeshProUGUI messageText;
    private RectTransform messageRoot;
    private Image backdropImage;
    private Coroutine displayRoutine;
    private bool isProcessing;
    private PendingMessage? activeMessage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RunEventMessageDisplay>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject host = new GameObject("RunEventMessageDisplay");
        host.AddComponent<RunEventMessageDisplay>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildDisplay();
        HideImmediate();
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
        if (!MainMenuManager.IsRunActive && (isProcessing || pendingMessages.Count > 0))
        {
            ResetRun();
        }
    }

    public static void ShowWave(int wave)
    {
        Show("WAVE " + wave + " START", WaveColor, 1.55f, Priority.Wave, 46f);
    }

    public static void ShowEliteMutation()
    {
        Show("ELITE MUTATION", EliteColor, 1.35f, Priority.Elite, 42f);
    }

    public static void ShowMiniBossApproaching()
    {
        Show("MINI BOSS APPROACHING", MiniBossColor, 2.2f, Priority.Boss, 54f);
    }

    public static void ShowBossIncoming()
    {
        Show("BOSS INCOMING", BossColor, 2.4f, Priority.Boss, 54f);
    }

    public static void ShowDragonBossIncoming()
    {
        Show("DRAGON BOSS", DragonBossColor, 2.75f, Priority.Boss, 58f);
    }

    public static void ShowBloodMoon()
    {
        Show("BLOOD MOON RISES", BloodMoonColor, 2.35f, Priority.Event, 50f);
    }

    public static void ShowGoldenDragonAppears()
    {
        Show("GOLDEN DRAGON", GoldenDragonColor, 2.4f, Priority.Event, 50f);
    }

    public static void ShowGoldenDragonEscaped()
    {
        Show("DRAGON ESCAPED", GoldenDragonColor, 1.9f, Priority.Event, 46f);
    }

    public static void ShowVoidPortalOpens()
    {
        Show("VOID PORTAL OPENED", VoidPortalColor, 2.15f, Priority.Event, 48f);
    }

    public static void ShowVoidPortalClosed()
    {
        Show("VOID PORTAL CLOSED", VoidPortalColor, 1.85f, Priority.Event, 44f);
    }

    public static void ShowMimicChest()
    {
        Show("MIMIC CHEST!", MimicColor, 1.95f, Priority.Event, 48f);
    }

    public static void Show(string message, Color color, float duration, Priority priority = Priority.Event)
    {
        Show(message, color, duration, priority, GetDefaultFontSize(priority));
    }

    public static void Show(string message, Color color, float duration, Priority priority, float fontSize)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        EnsureInstance();

        if (Instance == null)
        {
            return;
        }

        if (!CanShowAnnouncement())
        {
            return;
        }

        Instance.Enqueue(message, color, duration, priority, fontSize);
    }

    public void ResetRun()
    {
        pendingMessages.Clear();
        isProcessing = false;
        activeMessage = null;

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        HideImmediate();
    }

    private static void EnsureInstance()
    {
        if (Instance != null)
        {
            return;
        }

        Bootstrap();
    }

    private static bool CanShowAnnouncement()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            return false;
        }

        if (PauseMenuManager.IsGameplayPaused)
        {
            return false;
        }

        if (SettingsMenuUI.IsOpen)
        {
            return false;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return false;
        }

        if (DevAdminPanel.IsOpen)
        {
            return false;
        }

        if (MerchantShrineUI.IsOpen)
        {
            return false;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksGameplayPause)
        {
            return false;
        }

        HUDManager hudManager = HUDManager.Instance;

        if (hudManager != null && hudManager.IsLevelUpFeedbackVisible)
        {
            return false;
        }

        return true;
    }

    private static float GetDefaultFontSize(Priority priority)
    {
        return priority switch
        {
            Priority.Boss => 54f,
            Priority.Event => 48f,
            Priority.Elite => 42f,
            Priority.Wave => 46f,
            _ => 44f
        };
    }

    private void Enqueue(string message, Color color, float duration, Priority priority, float fontSize)
    {
        PendingMessage pending = new PendingMessage(message, color, duration, priority, fontSize);

        if (isProcessing && activeMessage.HasValue && pending.Priority > activeMessage.Value.Priority)
        {
            pendingMessages.Clear();

            if (displayRoutine != null)
            {
                StopCoroutine(displayRoutine);
                displayRoutine = null;
            }

            isProcessing = false;
            activeMessage = null;
            HideImmediate();
        }

        int insertIndex = pendingMessages.Count;

        for (int i = 0; i < pendingMessages.Count; i++)
        {
            if (pending.Priority > pendingMessages[i].Priority)
            {
                insertIndex = i;
                break;
            }
        }

        pendingMessages.Insert(insertIndex, pending);

        if (!isProcessing)
        {
            displayRoutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (pendingMessages.Count > 0)
        {
            if (!CanShowAnnouncement())
            {
                yield return null;
                continue;
            }

            PendingMessage pending = pendingMessages[0];
            pendingMessages.RemoveAt(0);
            activeMessage = pending;
            yield return DisplayMessage(pending);
            activeMessage = null;
        }

        isProcessing = false;
        displayRoutine = null;
    }

    private IEnumerator DisplayMessage(PendingMessage pending)
    {
        if (messageText == null || messageRoot == null)
        {
            yield break;
        }

        messageText.gameObject.SetActive(true);
        messageText.text = pending.Message;
        messageText.fontSize = pending.FontSize;

        if (backdropImage != null)
        {
            backdropImage.gameObject.SetActive(true);
            Color backdropColor = pending.Color;
            backdropColor.a = 0.08f;
            backdropImage.color = backdropColor;
        }

        Color color = pending.Color;
        color.a = 0f;
        messageText.color = color;

        const float fadeInDuration = 0.18f;
        const float fadeOutDuration = 0.32f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeInDuration);
            float eased = progress * progress * (3f - 2f * progress);
            float scale = progress < 0.72f
                ? Mathf.Lerp(0.84f, 1.06f, eased / 0.72f)
                : Mathf.Lerp(1.06f, 1f, (progress - 0.72f) / 0.28f);

            color.a = eased;
            messageText.color = color;
            messageRoot.localScale = Vector3.one * scale;

            if (backdropImage != null)
            {
                Color backdropColor = backdropImage.color;
                backdropColor.a = 0.08f * eased;
                backdropImage.color = backdropColor;
            }

            yield return null;
        }

        color.a = 1f;
        messageText.color = color;
        messageRoot.localScale = Vector3.one;

        float holdDuration = Mathf.Max(0f, pending.Duration - fadeInDuration - fadeOutDuration);
        float holdElapsed = 0f;

        while (holdElapsed < holdDuration)
        {
            if (!CanShowAnnouncement())
            {
                break;
            }

            holdElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeOutDuration);
            color.a = 1f - progress;
            messageText.color = color;

            if (backdropImage != null)
            {
                Color backdropColor = backdropImage.color;
                backdropColor.a = 0.08f * (1f - progress);
                backdropImage.color = backdropColor;
            }

            messageRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.96f, progress);
            yield return null;
        }

        HideImmediate();
    }

    private void HideImmediate()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }

        if (backdropImage != null)
        {
            backdropImage.gameObject.SetActive(false);
        }

        if (messageRoot != null)
        {
            messageRoot.localScale = Vector3.one;
        }
    }

    private void BuildDisplay()
    {
        GameObject canvasObject = new GameObject("RunEventMessageCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 37;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rootObject = new GameObject("RunEventMessageRoot");
        rootObject.transform.SetParent(canvasObject.transform, false);
        messageRoot = rootObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(messageRoot, new Vector2(0f, 278f), new Vector2(960f, 88f));

        GameObject backdropObject = new GameObject("RunEventMessageBackdrop");
        backdropObject.transform.SetParent(messageRoot, false);
        RectTransform backdropRect = backdropObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(backdropRect, 0f);
        backdropImage = backdropObject.AddComponent<Image>();
        backdropImage.color = new Color(0.04f, 0.05f, 0.08f, 0.08f);
        backdropImage.raycastTarget = false;

        GameObject textObject = new GameObject("RunEventMessageText");
        textObject.transform.SetParent(messageRoot, false);
        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 46f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.color = EventColor;
        messageText.raycastTarget = false;
        UiLayoutUtility.StretchToParent(messageText.rectTransform, 8f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            messageText.font = font;
        }
    }

    private readonly struct PendingMessage
    {
        public readonly string Message;
        public readonly Color Color;
        public readonly float Duration;
        public readonly Priority Priority;
        public readonly float FontSize;

        public PendingMessage(string message, Color color, float duration, Priority priority, float fontSize)
        {
            Message = message;
            Color = color;
            Duration = duration;
            Priority = priority;
            FontSize = fontSize;
        }
    }
}
