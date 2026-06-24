using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ChestSingleCardRevealUI : MonoBehaviour
{
    private const float RevealDuration = 0.48f;
    private const float RouletteDuration = 0.82f;
    private const float SettleDuration = 0.14f;
    private const float PostLockPulseDuration = 0.22f;
    private const float GlowPulseSpeed = 2.8f;
    private const int RouletteCycles = 3;

    private static readonly Color[] RouletteAccents =
    {
        ChestLootRarityPalette.CommonAccent,
        ChestLootRarityPalette.RareAccent,
        ChestLootRarityPalette.EpicAccent,
        ChestLootRarityPalette.LegendaryAccent,
    };

    private static readonly Color[] RouletteBackgrounds =
    {
        ChestLootRarityPalette.CommonBackground,
        ChestLootRarityPalette.RareBackground,
        ChestLootRarityPalette.EpicBackground,
        ChestLootRarityPalette.LegendaryBackground,
    };

    private static readonly Color[] RouletteBorders =
    {
        ChestLootRarityPalette.CommonBorder,
        ChestLootRarityPalette.RareBorder,
        ChestLootRarityPalette.EpicBorder,
        ChestLootRarityPalette.LegendaryBorder,
    };

    private sealed class CardView
    {
        public RectTransform CardRect;
        public Button Button;
        public Image BorderGlow;
        public Image Background;
        public Image BackdropPulse;
        public Image IconFrame;
        public Image IconImage;
        public Image PlaceholderIcon;
        public TMP_Text RarityText;
        public TMP_Text TitleText;
        public TMP_Text DescriptionText;
        public TMP_Text CollectHintText;
    }

    private static Sprite placeholderSprite;

    private GameObject rootPanel;
    private TMP_Text headerText;
    private CardView cardView;
    private Coroutine presentationRoutine;
    private Action collectCallback;
    private bool isBuilt;
    private bool isShowing;
    private bool collectUnlocked;
    private bool collectInvoked;
    private Color currentAccent = ChestLootRarityPalette.CommonAccent;
    private Color lockedAccent = ChestLootRarityPalette.CommonAccent;
    private Color lockedBackground = ChestLootRarityPalette.CommonBackground;
    private Color lockedBorder = ChestLootRarityPalette.CommonBorder;
    private string lockedRarityLabel = "COMMON";
    private bool rouletteLocked;

    public bool IsShowing => isShowing && rootPanel != null && rootPanel.activeSelf;

    public void EnsureBuilt(Canvas canvas, TMP_Text fontSource)
    {
        if (isBuilt)
        {
            return;
        }

        if (canvas == null)
        {
            canvas = UiLayoutUtility.GetGameplayCanvas();
        }

        if (canvas == null)
        {
            return;
        }

        BuildPanel(canvas.transform, fontSource);
        isBuilt = true;
    }

    public void Show(string header, Color headerColor, ChestLootSelectionUI.SlotData cardData, Action onCollect)
    {
        if (rootPanel == null || cardView == null)
        {
            onCollect?.Invoke();
            return;
        }

        collectCallback = onCollect;
        collectInvoked = false;
        collectUnlocked = false;
        rouletteLocked = false;
        isShowing = true;
        rootPanel.SetActive(true);

        ApplyHeader(header, headerColor);
        ApplyCardContent(cardData);

        if (presentationRoutine != null)
        {
            StopCoroutine(presentationRoutine);
        }

        presentationRoutine = StartCoroutine(PresentCardRoutine());
    }

    public void Hide()
    {
        if (presentationRoutine != null)
        {
            StopCoroutine(presentationRoutine);
            presentationRoutine = null;
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        isShowing = false;
        collectUnlocked = false;
        collectInvoked = false;
        rouletteLocked = false;
        collectCallback = null;
    }

    private void Update()
    {
        if (!isShowing || !collectUnlocked || collectInvoked)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            TryCollect();
        }
    }

    private void TryCollect()
    {
        if (!collectUnlocked || collectInvoked)
        {
            return;
        }

        collectInvoked = true;
        collectCallback?.Invoke();
    }

    private IEnumerator PresentCardRoutine()
    {
        if (cardView == null || cardView.CardRect == null)
        {
            yield break;
        }

        RectTransform cardRect = cardView.CardRect;
        Vector2 targetPosition = new Vector2(0f, 8f);
        Vector2 startPosition = targetPosition + new Vector2(0f, -108f);
        cardRect.anchoredPosition = startPosition;
        cardRect.localScale = Vector3.one * 0.42f;
        ApplyRoulettePaletteIndex(0);

        if (cardView.CollectHintText != null)
        {
            Color hintColor = cardView.CollectHintText.color;
            hintColor.a = 0f;
            cardView.CollectHintText.color = hintColor;
        }

        float elapsed = 0f;
        float totalRouletteDuration = RouletteDuration + SettleDuration;

        while (elapsed < RevealDuration || elapsed < totalRouletteDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed <= RevealDuration)
            {
                float progress = Mathf.Clamp01(elapsed / RevealDuration);
                float eased = progress * progress * (3f - 2f * progress);
                float popScale = progress < 0.78f
                    ? Mathf.Lerp(0.42f, 1.08f, eased / 0.78f)
                    : Mathf.Lerp(1.08f, 1f, (progress - 0.78f) / 0.22f);

                cardRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
                cardRect.localScale = Vector3.one * popScale;
            }
            else
            {
                cardRect.anchoredPosition = targetPosition;
                cardRect.localScale = Vector3.one;
            }

            UpdateCardRouletteVisuals(elapsed);
            yield return null;
        }

        cardRect.anchoredPosition = targetPosition;
        cardRect.localScale = Vector3.one;
        LockCardToFinalRarity();

        float pulseElapsed = 0f;

        while (pulseElapsed < PostLockPulseDuration)
        {
            pulseElapsed += Time.unscaledDeltaTime;
            UpdateGlowPulse(pulseElapsed);
            yield return null;
        }

        collectUnlocked = true;

        if (cardView.CollectHintText != null)
        {
            Color hintColor = cardView.CollectHintText.color;
            hintColor.a = 0.82f;
            cardView.CollectHintText.color = hintColor;
        }
    }

    private void UpdateCardRouletteVisuals(float elapsed)
    {
        if (rouletteLocked)
        {
            UpdateGlowPulse(elapsed);
            return;
        }

        if (elapsed >= RouletteDuration + SettleDuration)
        {
            LockCardToFinalRarity();
            UpdateGlowPulse(elapsed);
            return;
        }

        Color accent;
        Color background;
        Color border;

        if (elapsed < RouletteDuration)
        {
            float rouletteProgress = Mathf.Clamp01(elapsed / RouletteDuration);
            int paletteIndex = GetRoulettePaletteIndex(rouletteProgress);
            accent = RouletteAccents[paletteIndex];
            background = RouletteBackgrounds[paletteIndex];
            border = RouletteBorders[paletteIndex];
        }
        else
        {
            float settleProgress = Mathf.Clamp01((elapsed - RouletteDuration) / SettleDuration);
            float settleEase = settleProgress * settleProgress * (3f - 2f * settleProgress);
            int lastIndex = GetRoulettePaletteIndex(1f);
            accent = Color.Lerp(RouletteAccents[lastIndex], lockedAccent, settleEase);
            background = Color.Lerp(RouletteBackgrounds[lastIndex], lockedBackground, settleEase);
            border = Color.Lerp(RouletteBorders[lastIndex], lockedBorder, settleEase);
        }

        ApplyCardVisualColors(accent, background, border, animateRarityLabel: elapsed < RouletteDuration);
        currentAccent = accent;
    }

    private void LockCardToFinalRarity()
    {
        if (rouletteLocked)
        {
            return;
        }

        rouletteLocked = true;
        currentAccent = lockedAccent;
        ApplyCardVisualColors(lockedAccent, lockedBackground, lockedBorder, animateRarityLabel: false);

        if (cardView?.RarityText != null)
        {
            cardView.RarityText.text = lockedRarityLabel;
            cardView.RarityText.color = lockedAccent;
        }

        if (cardView?.Button != null)
        {
            ColorBlock colors = cardView.Button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.Lerp(Color.white, lockedAccent, 0.2f);
            colors.pressedColor = Color.Lerp(Color.white, lockedAccent, 0.34f);
            colors.selectedColor = colors.highlightedColor;
            cardView.Button.colors = colors;
        }
    }

    private void ApplyRoulettePaletteIndex(int paletteIndex)
    {
        paletteIndex = Mathf.Clamp(paletteIndex, 0, RouletteAccents.Length - 1);
        ApplyCardVisualColors(
            RouletteAccents[paletteIndex],
            RouletteBackgrounds[paletteIndex],
            RouletteBorders[paletteIndex],
            animateRarityLabel: true);
        currentAccent = RouletteAccents[paletteIndex];
    }

    private static int GetRoulettePaletteIndex(float rouletteProgress)
    {
        float cycleProgress = rouletteProgress * RouletteCycles;
        int step = Mathf.FloorToInt(cycleProgress * RouletteAccents.Length);
        return step % RouletteAccents.Length;
    }

    private void ApplyCardVisualColors(Color accent, Color background, Color border, bool animateRarityLabel)
    {
        if (cardView == null)
        {
            return;
        }

        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * GlowPulseSpeed) * 0.18f;

        if (cardView.BorderGlow != null)
        {
            cardView.BorderGlow.color = new Color(accent.r, accent.g, accent.b, 0.26f + pulse * 0.22f);
        }

        if (cardView.BackdropPulse != null)
        {
            cardView.BackdropPulse.color = new Color(accent.r, accent.g, accent.b, 0.05f + pulse * 0.07f);
        }

        if (cardView.Background != null)
        {
            cardView.Background.color = background;
        }

        if (cardView.IconFrame != null)
        {
            cardView.IconFrame.color = border;
        }

        if (animateRarityLabel && cardView.RarityText != null)
        {
            cardView.RarityText.color = accent;
        }
    }

    private void UpdateGlowPulse(float timeSeed)
    {
        if (cardView == null)
        {
            return;
        }

        float pulse = 0.5f + Mathf.Sin(timeSeed * GlowPulseSpeed) * 0.18f;

        if (cardView.BorderGlow != null)
        {
            cardView.BorderGlow.color = new Color(currentAccent.r, currentAccent.g, currentAccent.b, 0.26f + pulse * 0.22f);
        }

        if (cardView.BackdropPulse != null)
        {
            cardView.BackdropPulse.color = new Color(currentAccent.r, currentAccent.g, currentAccent.b, 0.05f + pulse * 0.07f);
        }
    }

    private void ApplyHeader(string header, Color headerColor)
    {
        if (headerText == null)
        {
            return;
        }

        headerText.text = string.IsNullOrEmpty(header) ? "Chest Reward" : header;
        headerText.color = headerColor;
    }

    private void ApplyCardContent(ChestLootSelectionUI.SlotData data)
    {
        if (cardView == null)
        {
            return;
        }

        lockedAccent = data.RarityAccent.a > 0.01f ? data.RarityAccent : ChestLootRarityPalette.CommonAccent;
        lockedBackground = data.RarityBackground.a > 0.01f
            ? data.RarityBackground
            : ChestLootRarityPalette.CommonBackground;
        lockedBorder = data.RarityBorder.a > 0.01f
            ? data.RarityBorder
            : ChestLootRarityPalette.CommonBorder;
        lockedRarityLabel = ChestLootSelectionUI.BuildRewardHeaderLabel(
            data.RarityLabel,
            data.CategoryLabel,
            data.BuildLabel);
        currentAccent = lockedAccent;

        if (cardView.RarityText != null)
        {
            cardView.RarityText.text = lockedRarityLabel;
            cardView.RarityText.color = lockedAccent;
        }

        if (cardView.TitleText != null)
        {
            cardView.TitleText.text = data.Title ?? string.Empty;
        }

        if (cardView.DescriptionText != null)
        {
            cardView.DescriptionText.text = data.Description ?? string.Empty;
        }

        ApplyCardIcon(data.IconKey);

        if (cardView.Button != null)
        {
            cardView.Button.onClick.RemoveAllListeners();
            cardView.Button.onClick.AddListener(TryCollect);
        }
    }

    private void ApplyCardIcon(string iconKey)
    {
        if (cardView == null)
        {
            return;
        }

        Sprite iconSprite = UpgradeCardIconUtility.TryLoadSprite(iconKey);

        if (iconSprite != null && cardView.IconImage != null)
        {
            cardView.IconImage.sprite = iconSprite;
            cardView.IconImage.enabled = true;
            cardView.IconImage.color = Color.white;

            if (UpgradeCardIconUtility.TryGetIconFrameColor(iconKey, out Color themeColor) && cardView.IconFrame != null)
            {
                cardView.IconFrame.color = Color.Lerp(cardView.IconFrame.color, themeColor, 0.55f);
            }

            if (cardView.PlaceholderIcon != null)
            {
                cardView.PlaceholderIcon.gameObject.SetActive(false);
            }

            return;
        }

        if (cardView.IconImage != null)
        {
            cardView.IconImage.sprite = null;
            cardView.IconImage.enabled = false;
        }

        if (cardView.PlaceholderIcon != null)
        {
            cardView.PlaceholderIcon.sprite = GetPlaceholderSprite();
            cardView.PlaceholderIcon.gameObject.SetActive(true);

            if (UpgradeCardIconUtility.TryGetIconFrameColor(iconKey, out Color themeColor))
            {
                cardView.PlaceholderIcon.color = themeColor;

                if (cardView.IconFrame != null)
                {
                    cardView.IconFrame.color = Color.Lerp(cardView.IconFrame.color, themeColor, 0.65f);
                }
            }
            else
            {
                cardView.PlaceholderIcon.color = Color.white;
            }
        }
    }

    private static Sprite GetPlaceholderSprite()
    {
        if (placeholderSprite != null)
        {
            return placeholderSprite;
        }

        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        placeholderSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
        return placeholderSprite;
    }

    private void BuildPanel(Transform canvasTransform, TMP_Text fontSource)
    {
        rootPanel = new GameObject("ChestSingleCardPanel");
        rootPanel.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = rootPanel.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(860f, 520f));

        Image panelImage = rootPanel.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);
        panelImage.raycastTarget = true;

        GameObject backdropPulseObject = new GameObject("BackdropPulse");
        backdropPulseObject.transform.SetParent(rootPanel.transform, false);
        backdropPulseObject.transform.SetAsFirstSibling();

        RectTransform backdropRect = backdropPulseObject.AddComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdropPulse = backdropPulseObject.AddComponent<Image>();
        backdropPulse.raycastTarget = false;
        backdropPulse.color = new Color(0.86f, 0.88f, 0.92f, 0.05f);

        headerText = CreateText(rootPanel.transform, fontSource, "ChestRevealHeader", new Vector2(0f, 196f), new Vector2(640f, 42f), 28f, FontStyles.Bold);

        GameObject cardObject = new GameObject("RewardCard");
        cardObject.transform.SetParent(rootPanel.transform, false);

        RectTransform cardRect = cardObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(cardRect, new Vector2(0f, 8f), new Vector2(280f, 340f));

        Image cardButtonImage = cardObject.AddComponent<Image>();
        cardButtonImage.color = ChestLootRarityPalette.CommonBackground;

        Button cardButton = cardObject.AddComponent<Button>();
        cardButton.targetGraphic = cardButtonImage;

        GameObject glowObject = new GameObject("BorderGlow");
        glowObject.transform.SetParent(cardObject.transform, false);
        glowObject.transform.SetAsFirstSibling();

        RectTransform glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-8f, -8f);
        glowRect.offsetMax = new Vector2(8f, 8f);

        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.raycastTarget = false;
        glowImage.color = new Color(0.86f, 0.88f, 0.92f, 0.24f);

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(cardObject.transform, false);

        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = new Vector2(4f, 4f);
        backgroundRect.offsetMax = new Vector2(-4f, -4f);

        Image backgroundImage = backgroundObject.AddComponent<Image>();
        backgroundImage.raycastTarget = false;
        backgroundImage.color = ChestLootRarityPalette.CommonBackground;

        GameObject iconFrameObject = new GameObject("IconFrame");
        iconFrameObject.transform.SetParent(cardObject.transform, false);

        RectTransform iconFrameRect = iconFrameObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(iconFrameRect, new Vector2(0f, 96f), new Vector2(84f, 84f));

        Image iconFrameImage = iconFrameObject.AddComponent<Image>();
        iconFrameImage.raycastTarget = false;
        iconFrameImage.color = ChestLootRarityPalette.CommonBorder;

        GameObject iconImageObject = new GameObject("IconImage");
        iconImageObject.transform.SetParent(iconFrameObject.transform, false);

        RectTransform iconImageRect = iconImageObject.AddComponent<RectTransform>();
        iconImageRect.anchorMin = Vector2.zero;
        iconImageRect.anchorMax = Vector2.one;
        iconImageRect.offsetMin = new Vector2(8f, 8f);
        iconImageRect.offsetMax = new Vector2(-8f, -8f);

        Image iconImage = iconImageObject.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = false;

        GameObject placeholderObject = new GameObject("IconPlaceholder");
        placeholderObject.transform.SetParent(iconFrameObject.transform, false);

        RectTransform placeholderRect = placeholderObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(placeholderRect, Vector2.zero, new Vector2(38f, 38f));
        placeholderObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image placeholderImage = placeholderObject.AddComponent<Image>();
        placeholderImage.raycastTarget = false;
        placeholderImage.color = new Color(0.78f, 0.8f, 0.86f, 0.85f);
        placeholderImage.sprite = GetPlaceholderSprite();

        TMP_Text rarityText = CreateText(cardObject.transform, fontSource, "RarityText", new Vector2(0f, 138f), new Vector2(220f, 26f), 17f, FontStyles.Bold);
        TMP_Text titleText = CreateText(cardObject.transform, fontSource, "TitleText", new Vector2(0f, 36f), new Vector2(232f, 72f), 26f, FontStyles.Bold);
        TMP_Text descriptionText = CreateText(cardObject.transform, fontSource, "DescriptionText", new Vector2(0f, -44f), new Vector2(232f, 96f), 16f, FontStyles.Normal);
        TMP_Text collectHintText = CreateText(rootPanel.transform, fontSource, "CollectHint", new Vector2(0f, -196f), new Vector2(520f, 30f), 18f, FontStyles.Italic);

        if (titleText != null)
        {
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 20f;
            titleText.fontSizeMax = 26f;
            titleText.color = Color.white;
        }

        if (descriptionText != null)
        {
            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMin = 14f;
            descriptionText.fontSizeMax = 16f;
            descriptionText.color = new Color(0.76f, 0.8f, 0.88f, 1f);
        }

        if (collectHintText != null)
        {
            collectHintText.text = "Click / E / Space to collect";
            collectHintText.color = new Color(0.72f, 0.76f, 0.84f, 0f);
        }

        cardView = new CardView
        {
            CardRect = cardRect,
            Button = cardButton,
            BorderGlow = glowImage,
            Background = backgroundImage,
            BackdropPulse = backdropPulse,
            IconFrame = iconFrameImage,
            IconImage = iconImage,
            PlaceholderIcon = placeholderImage,
            RarityText = rarityText,
            TitleText = titleText,
            DescriptionText = descriptionText,
            CollectHintText = collectHintText
        };

        rootPanel.SetActive(false);
    }

    private static TMP_Text CreateText(
        Transform parent,
        TMP_Text fontSource,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, anchoredPosition, size);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        CopyTmpFontFrom(fontSource, textMesh);
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.Normal;
        textMesh.richText = false;
        textMesh.raycastTarget = false;
        textMesh.overflowMode = TextOverflowModes.Ellipsis;

        return textMesh;
    }

    private static void CopyTmpFontFrom(TMP_Text source, TMP_Text target)
    {
        if (source == null || target == null)
        {
            return;
        }

        if (source.font != null)
        {
            target.font = source.font;
        }

        if (source.fontSharedMaterial != null)
        {
            target.fontSharedMaterial = source.fontSharedMaterial;
        }
    }
}
