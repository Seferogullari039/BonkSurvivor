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
        Event = 20,
        Boss = 30
    }

    public static RunEventMessageDisplay Instance { get; private set; }

    private static readonly Color WaveColor = new Color(0.82f, 0.94f, 1f, 1f);
    private static readonly Color BossColor = new Color(1f, 0.34f, 0.34f, 1f);
    private static readonly Color DragonBossColor = new Color(0.95f, 0.22f, 0.58f, 1f);
    private static readonly Color EventColor = new Color(0.92f, 0.88f, 1f, 1f);
    private static readonly Color BloodMoonColor = new Color(0.95f, 0.18f, 0.16f, 1f);
    private static readonly Color GoldenDragonColor = new Color(1f, 0.82f, 0.18f, 1f);
    private static readonly Color VoidPortalColor = new Color(0.78f, 0.38f, 1f, 1f);
    private static readonly Color MimicColor = new Color(1f, 0.22f, 0.18f, 1f);

    private readonly List<PendingMessage> pendingMessages = new List<PendingMessage>();
    private TextMeshProUGUI messageText;
    private Coroutine displayRoutine;
    private bool isProcessing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RunEventMessageDisplay>() != null) return;

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
        Show("WAVE " + wave + " START", WaveColor, 1.7f, Priority.Wave);
    }

    public static void ShowBossIncoming()
    {
        Show("BOSS INCOMING", BossColor, 2.8f, Priority.Boss);
    }

    public static void ShowDragonBossIncoming()
    {
        Show("DRAGON BOSS INCOMING", DragonBossColor, 3.1f, Priority.Boss);
    }

    public static void ShowBloodMoon()
    {
        Show("BLOOD MOON RISES", BloodMoonColor, 2.5f, Priority.Event);
    }

    public static void ShowGoldenDragonAppears()
    {
        Show("GOLDEN DRAGON APPEARS", GoldenDragonColor, 2.6f, Priority.Event);
    }

    public static void ShowGoldenDragonEscaped()
    {
        Show("DRAGON ESCAPED", GoldenDragonColor, 2.0f, Priority.Event);
    }

    public static void ShowVoidPortalOpens()
    {
        Show("VOID PORTAL OPENS", VoidPortalColor, 2.3f, Priority.Event);
    }

    public static void ShowVoidPortalClosed()
    {
        Show("VOID PORTAL CLOSED", VoidPortalColor, 2.0f, Priority.Event);
    }

    public static void ShowMimicChest()
    {
        Show("MIMIC CHEST!", MimicColor, 2.1f, Priority.Event);
    }

    public static void Show(string message, Color color, float duration, Priority priority = Priority.Event)
    {
        if (string.IsNullOrEmpty(message)) return;

        EnsureInstance();

        if (Instance == null) return;

        Instance.Enqueue(message, color, duration, priority);
    }

    public void ResetRun()
    {
        pendingMessages.Clear();
        isProcessing = false;

        if (displayRoutine != null)
        {
            StopCoroutine(displayRoutine);
            displayRoutine = null;
        }

        HideImmediate();
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;

        Bootstrap();
    }

    private void Enqueue(string message, Color color, float duration, Priority priority)
    {
        PendingMessage pending = new PendingMessage(message, color, duration, priority);

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
            PendingMessage pending = pendingMessages[0];
            pendingMessages.RemoveAt(0);
            yield return DisplayMessage(pending);
        }

        isProcessing = false;
        displayRoutine = null;
    }

    private IEnumerator DisplayMessage(PendingMessage pending)
    {
        if (messageText == null) yield break;

        messageText.gameObject.SetActive(true);
        messageText.text = pending.Message;
        Color color = pending.Color;
        color.a = 0f;
        messageText.color = color;

        const float fadeInDuration = 0.22f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeInDuration);
            messageText.color = color;
            yield return null;
        }

        color.a = 1f;
        messageText.color = color;

        const float fadeOutDuration = 0.4f;
        float holdDuration = Mathf.Max(0f, pending.Duration - fadeInDuration - fadeOutDuration);
        float holdElapsed = 0f;

        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            messageText.color = color;
            yield return null;
        }

        messageText.gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
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

        GameObject textObject = new GameObject("RunEventMessageText");
        textObject.transform.SetParent(canvasObject.transform, false);
        messageText = textObject.AddComponent<TextMeshProUGUI>();
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 52f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.color = EventColor;
        messageText.raycastTarget = false;
        UiLayoutUtility.SetAnchorCenter(messageText.rectTransform, new Vector2(0f, 250f), new Vector2(960f, 72f));

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

        public PendingMessage(string message, Color color, float duration, Priority priority)
        {
            Message = message;
            Color = color;
            Duration = duration;
            Priority = priority;
        }
    }
}
