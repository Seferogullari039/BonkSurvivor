using UnityEngine;
using UnityEngine.UI;

public static class UiLayoutUtility
{
    public static void ConfigureGameplayCanvas(Canvas canvas)
    {
        if (canvas == null) return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.pixelPerfect = false;

        EnsureCanvasScaler(canvas);
        EnsureCanvasRootScale(canvas);
    }

    private static void EnsureCanvasRootScale(Canvas canvas)
    {
        if (canvas == null) return;

        RectTransform rectTransform = canvas.GetComponent<RectTransform>();

        if (rectTransform == null) return;

        if (rectTransform.localScale.sqrMagnitude < 0.0001f)
        {
            rectTransform.localScale = Vector3.one;
        }

        Canvas.ForceUpdateCanvases();
    }

    public static void EnsureCanvasScaler(Canvas canvas)
    {
        if (canvas == null) return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    public static void SetTopLeft(RectTransform rt, float x, float y)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    public static void SetTopRight(RectTransform rt, float x, float y)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    public static void SetBottomLeft(RectTransform rt, float x, float y)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    public static void SetBottomRight(RectTransform rt, float x, float y)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    public static void SetCenter(RectTransform rt, float x = 0f, float y = 0f)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
    }

    public static void SetBottomStretch(RectTransform rt, float left, float right, float y, float height)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(left, y);
        rt.offsetMax = new Vector2(-right, y + height);
    }

    public static void SetStretch(RectTransform rt, float left, float right, float top, float bottom)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void StretchToParent(RectTransform rt, float padding = 0f)
    {
        if (rt == null) return;
        SetStretch(rt, padding, padding, padding, padding);
    }

    public static void SetSize(RectTransform rt, float width, float height)
    {
        if (rt == null) return;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public static void SetAnchorTopLeft(RectTransform rt, float x, float y, float width, float height)
    {
        SetTopLeft(rt, x, y);
        SetSize(rt, width, height);
    }

    public static void SetAnchorTopRight(RectTransform rt, float x, float y, float width, float height)
    {
        SetTopRight(rt, x, y);
        SetSize(rt, width, height);
    }

    public static void SetAnchorBottomLeft(RectTransform rt, float x, float y, float width, float height)
    {
        SetBottomLeft(rt, x, y);
        SetSize(rt, width, height);
    }

    public static void SetAnchorBottomRight(RectTransform rt, float x, float y, float width, float height)
    {
        SetBottomRight(rt, x, y);
        SetSize(rt, width, height);
    }

    public static void SetAnchorCenter(RectTransform rt, float x, float y, float width, float height)
    {
        SetCenter(rt, x, y);
        SetSize(rt, width, height);
    }

    public static void SetAnchorBottomStretch(RectTransform rt, float left, float right, float y, float height)
    {
        SetBottomStretch(rt, left, right, y, height);
    }

    public static void SetAnchorTopLeft(RectTransform rt, Vector2 position, Vector2 size)
    {
        SetAnchorTopLeft(rt, position.x, position.y, size.x, size.y);
    }

    public static void SetAnchorTopRight(RectTransform rt, Vector2 position, Vector2 size)
    {
        SetAnchorTopRight(rt, position.x, position.y, size.x, size.y);
    }

    public static void SetAnchorBottomLeft(RectTransform rt, Vector2 position, Vector2 size)
    {
        SetAnchorBottomLeft(rt, position.x, position.y, size.x, size.y);
    }

    public static void SetAnchorCenter(RectTransform rt, Vector2 position, Vector2 size)
    {
        SetAnchorCenter(rt, position.x, position.y, size.x, size.y);
    }
}
