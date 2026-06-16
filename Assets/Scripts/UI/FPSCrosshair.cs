using UnityEngine;
using UnityEngine.UI;

public class FPSCrosshair : MonoBehaviour
{
    public static FPSCrosshair Instance { get; private set; }

    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color TargetColor = new Color(1f, 0.28f, 0.28f, 0.98f);
    private static readonly Color HitFlashColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.45f);

    private GameObject crosshairRoot;
    private Image topLine;
    private Image bottomLine;
    private Image leftLine;
    private Image rightLine;
    private Image centerDot;
    private float hitFeedbackTimer;
    private float hitFeedbackDuration;
    private float hitPeakScale = 1.15f;
    private float baseScale = 1f;

    private void Awake()
    {
        Instance = this;
        BuildCrosshairUi();
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
        if (crosshairRoot == null)
        {
            return;
        }

        bool showCrosshair = FPSPlayerController.IsFpsModeActive
            && MainMenuManager.IsRunActive
            && Time.timeScale > 0f;

        if (crosshairRoot.activeSelf != showCrosshair)
        {
            crosshairRoot.SetActive(showCrosshair);

            if (showCrosshair)
            {
                crosshairRoot.transform.SetAsLastSibling();
            }
        }

        if (!showCrosshair)
        {
            return;
        }

        bool onEnemy = FPSAimUtility.IsEnemyInCrosshair();
        Color lineColor = onEnemy ? TargetColor : NormalColor;

        if (hitFeedbackTimer > 0f)
        {
            float flashProgress = 1f - Mathf.Clamp01(hitFeedbackTimer / hitFeedbackDuration);
            lineColor = Color.Lerp(HitFlashColor, onEnemy ? TargetColor : NormalColor, flashProgress);
        }

        ApplyLineColor(topLine, lineColor);
        ApplyLineColor(bottomLine, lineColor);
        ApplyLineColor(leftLine, lineColor);
        ApplyLineColor(rightLine, lineColor);
        ApplyLineColor(centerDot, lineColor);

        UpdateHitFeedback();
    }

    public void PlayHitFeedback(bool isKill = false)
    {
        hitFeedbackDuration = isKill ? 0.1f : 0.08f;
        hitPeakScale = isKill ? 1.2f : 1.15f;
        hitFeedbackTimer = hitFeedbackDuration;
    }

    public static void HitFeedback()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayHitFeedback(false);
    }

    public static void KillFeedback()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.PlayHitFeedback(true);
    }

    public static void ShowHitFeedback()
    {
        HitFeedback();
    }

    private void UpdateHitFeedback()
    {
        if (crosshairRoot == null)
        {
            return;
        }

        if (hitFeedbackTimer > 0f)
        {
            hitFeedbackTimer -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(hitFeedbackTimer / hitFeedbackDuration);
            float scale = Mathf.Lerp(hitPeakScale, baseScale, progress);
            crosshairRoot.transform.localScale = Vector3.one * scale;
            return;
        }

        crosshairRoot.transform.localScale = Vector3.one * baseScale;
    }

    private void BuildCrosshairUi()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        crosshairRoot = new GameObject("FPSCrosshair");
        crosshairRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = crosshairRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(32f, 32f);
        rootRect.localScale = Vector3.one;

        topLine = CreateCrosshairLine(crosshairRoot.transform, "TopLine", new Vector2(2f, 9f), new Vector2(0f, 10f));
        bottomLine = CreateCrosshairLine(crosshairRoot.transform, "BottomLine", new Vector2(2f, 9f), new Vector2(0f, -10f));
        leftLine = CreateCrosshairLine(crosshairRoot.transform, "LeftLine", new Vector2(9f, 2f), new Vector2(-10f, 0f));
        rightLine = CreateCrosshairLine(crosshairRoot.transform, "RightLine", new Vector2(9f, 2f), new Vector2(10f, 0f));
        centerDot = CreateCrosshairLine(crosshairRoot.transform, "CenterDot", new Vector2(3f, 3f), Vector2.zero);

        crosshairRoot.SetActive(false);
    }

    private static Image CreateCrosshairLine(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        CreateShadowLine(parent, name + "Shadow", size, anchoredPosition + new Vector2(1f, -1f));

        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform rectTransform = lineObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;

        Image image = lineObject.AddComponent<Image>();
        image.color = NormalColor;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateShadowLine(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject shadowObject = new GameObject(name);
        shadowObject.transform.SetParent(parent, false);

        RectTransform rectTransform = shadowObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;

        Image image = shadowObject.AddComponent<Image>();
        image.color = ShadowColor;
        image.raycastTarget = false;
    }

    private static void ApplyLineColor(Image line, Color color)
    {
        if (line != null)
        {
            line.color = color;
        }
    }
}
