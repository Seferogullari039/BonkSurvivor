using UnityEngine;
using UnityEngine.UI;

public class FPSCrosshair : MonoBehaviour
{
    public static FPSCrosshair Instance { get; private set; }

    private static readonly Color NormalColor = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color TargetColor = new Color(1f, 0.28f, 0.28f, 0.95f);
    private static readonly Color HitFlashColor = new Color(1f, 1f, 1f, 1f);

    private GameObject crosshairRoot;
    private Image horizontalLine;
    private Image verticalLine;
    private Image centerDot;
    private float hitFeedbackTimer;
    private float hitFeedbackDuration;
    private float hitPeakScale = 1.25f;
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
        if (crosshairRoot == null) return;

        bool showCrosshair = FPSPlayerController.IsFpsModeActive
            && MainMenuManager.IsRunActive
            && Time.timeScale > 0f;

        if (crosshairRoot.activeSelf != showCrosshair)
        {
            crosshairRoot.SetActive(showCrosshair);
        }

        if (!showCrosshair) return;

        bool onEnemy = FPSAimUtility.IsEnemyInCrosshair();
        Color lineColor = onEnemy ? TargetColor : NormalColor;

        if (hitFeedbackTimer > 0f)
        {
            float flashProgress = 1f - Mathf.Clamp01(hitFeedbackTimer / hitFeedbackDuration);
            lineColor = Color.Lerp(HitFlashColor, onEnemy ? TargetColor : NormalColor, flashProgress);
        }

        if (horizontalLine != null) horizontalLine.color = lineColor;
        if (verticalLine != null) verticalLine.color = lineColor;
        if (centerDot != null) centerDot.color = lineColor;

        UpdateHitFeedback();
    }

    public void PlayHitFeedback(bool isKill = false)
    {
        hitFeedbackDuration = isKill ? 0.1f : 0.08f;
        hitPeakScale = isKill ? 1.55f : 1.28f;
        hitFeedbackTimer = hitFeedbackDuration;
    }

    public static void HitFeedback()
    {
        if (Instance == null) return;

        Instance.PlayHitFeedback(false);
    }

    public static void KillFeedback()
    {
        if (Instance == null) return;

        Instance.PlayHitFeedback(true);
    }

    public static void ShowHitFeedback()
    {
        HitFeedback();
    }

    private void UpdateHitFeedback()
    {
        if (crosshairRoot == null) return;

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
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null) return;

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        crosshairRoot = new GameObject("FPSCrosshair");
        crosshairRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = crosshairRoot.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rootRect, Vector2.zero, new Vector2(24f, 24f));

        horizontalLine = CreateLine(crosshairRoot.transform, "HorizontalLine", new Vector2(14f, 2f), Vector2.zero);
        verticalLine = CreateLine(crosshairRoot.transform, "VerticalLine", new Vector2(2f, 14f), Vector2.zero);
        centerDot = CreateLine(crosshairRoot.transform, "CenterDot", new Vector2(3f, 3f), Vector2.zero);

        crosshairRoot.SetActive(false);
    }

    private static Image CreateLine(Transform parent, string name, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);

        RectTransform rectTransform = lineObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = lineObject.AddComponent<Image>();
        image.color = NormalColor;
        image.raycastTarget = false;
        return image;
    }
}
