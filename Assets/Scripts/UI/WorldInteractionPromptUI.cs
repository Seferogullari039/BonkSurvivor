using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldInteractionPromptUI : MonoBehaviour
{
    private struct PromptCandidate
    {
        public string Text;
        public float DistanceSq;
    }

    private static WorldInteractionPromptUI instance;

    private readonly Dictionary<MonoBehaviour, PromptCandidate> candidates = new Dictionary<MonoBehaviour, PromptCandidate>();
    private string directText;
    private GameObject panelRoot;
    private TextMeshProUGUI promptText;
    private Image backgroundImage;

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

    public static void Register(MonoBehaviour source, string text, float distanceSqr)
    {
        if (source == null || string.IsNullOrEmpty(text))
        {
            return;
        }

        EnsureInstance().candidates[source] = new PromptCandidate
        {
            Text = text,
            DistanceSq = Mathf.Max(0f, distanceSqr)
        };
    }

    public static void Clear(MonoBehaviour source)
    {
        if (instance == null || source == null)
        {
            return;
        }

        instance.candidates.Remove(source);
    }

    public static void Show(string text)
    {
        WorldInteractionPromptUI ui = EnsureInstance();
        ui.directText = text;
    }

    public static void Hide()
    {
        if (instance == null)
        {
            return;
        }

        instance.directText = null;
        instance.candidates.Clear();
        instance.SetVisible(false);
    }

    private static WorldInteractionPromptUI EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<WorldInteractionPromptUI>(FindObjectsInactive.Include);

        if (instance != null)
        {
            return instance;
        }

        GameObject host = new GameObject("WorldInteractionPromptUI");
        return host.AddComponent<WorldInteractionPromptUI>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildPanel();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void LateUpdate()
    {
        PruneDestroyedSources();

        if (!CanShowPrompts())
        {
            SetVisible(false);
            return;
        }

        string textToShow = ResolveBestPromptText();

        if (string.IsNullOrEmpty(textToShow))
        {
            SetVisible(false);
            return;
        }

        EnsurePanel();
        promptText.text = FormatRichText(textToShow);
        SetVisible(true);
    }

    private string ResolveBestPromptText()
    {
        if (!string.IsNullOrEmpty(directText))
        {
            return directText;
        }

        string bestText = null;
        float bestDistanceSq = float.MaxValue;

        foreach (KeyValuePair<MonoBehaviour, PromptCandidate> entry in candidates)
        {
            if (entry.Value.DistanceSq >= bestDistanceSq)
            {
                continue;
            }

            bestDistanceSq = entry.Value.DistanceSq;
            bestText = entry.Value.Text;
        }

        return bestText;
    }

    private void PruneDestroyedSources()
    {
        if (candidates.Count == 0)
        {
            return;
        }

        List<MonoBehaviour> staleSources = null;

        foreach (MonoBehaviour source in candidates.Keys)
        {
            if (source != null)
            {
                continue;
            }

            staleSources ??= new List<MonoBehaviour>();
            staleSources.Add(source);
        }

        if (staleSources == null)
        {
            return;
        }

        for (int i = 0; i < staleSources.Count; i++)
        {
            candidates.Remove(staleSources[i]);
        }
    }

    private static bool CanShowPrompts()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return false;
        }

        if (MerchantShrineUI.IsOpen)
        {
            return false;
        }

        if (MerchantShrineTradeGuards.IsRewardFlowBlockingTrade())
        {
            return false;
        }

        if (PauseMenuManager.IsGameplayPaused)
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

        if (SettingsMenuUI.IsOpen)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            return false;
        }

        return true;
    }

    private static string FormatRichText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.StartsWith("E - "))
        {
            return "<color=#E8C547><b>E</b></color>" + text.Substring(1);
        }

        if (text.StartsWith("E "))
        {
            return "<color=#E8C547><b>E</b></color>" + text.Substring(1);
        }

        return text;
    }

    private void EnsurePanel()
    {
        if (panelRoot != null && promptText != null)
        {
            return;
        }

        BuildPanel();
    }

    private void BuildPanel()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }

        panelRoot = new GameObject("WorldInteractionPrompt");
        panelRoot.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -92f);
        panelRect.sizeDelta = new Vector2(360f, 42f);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(panelRoot.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(backgroundRect, 0f);

        backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
        backgroundImage.raycastTarget = false;

        GameObject textObject = new GameObject("PromptText");
        textObject.transform.SetParent(panelRoot.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(textRect, 10f);

        promptText = textObject.AddComponent<TextMeshProUGUI>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 22f;
        promptText.color = new Color(0.96f, 0.96f, 0.98f, 0.98f);
        promptText.raycastTarget = false;
        promptText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            promptText.font = font;
        }
    }

    private void SetVisible(bool visible)
    {
        if (panelRoot != null && panelRoot.activeSelf != visible)
        {
            panelRoot.SetActive(visible);
        }
    }
}
