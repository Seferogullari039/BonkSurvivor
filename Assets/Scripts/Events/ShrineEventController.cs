using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShrineEventController : MonoBehaviour
{
    private const float HoldRadius = 7f;
    private const float PromptRadius = 10f;
    private const float HoldDurationSeconds = 10f;
    private const float ProgressFillPerSecond = 1f;
    private const float ProgressDecayPerSecond = 1.5f;
    private const float BonusChestChance = 0.35f;
    private const int CoinReward = 25;
    private const int XpReward = 3;
    private const float CompletionDuration = 0.62f;

    private static readonly Color BaseStoneColor = new Color(0.42f, 0.44f, 0.4f);
    private static readonly Color AccentColor = new Color(0.18f, 0.52f, 0.46f);
    private static readonly Color GlowColor = new Color(0.28f, 0.72f, 0.62f);
    private static readonly Color PromptColor = new Color(0.86f, 0.94f, 0.92f, 1f);
    private static readonly Color RingInsideColor = new Color(0.22f, 0.78f, 0.68f, 0.42f);
    private static readonly Color RingOutsideColor = new Color(0.18f, 0.58f, 0.62f, 0.22f);
    private static readonly Color BurstGoldColor = new Color(0.92f, 0.78f, 0.28f);
    private static readonly Color BurstTealColor = new Color(0.28f, 0.82f, 0.68f);

    private ShrineEventManager eventManager;
    private Transform playerTransform;
    private Transform visualRoot;
    private Transform radiusRingTransform;
    private Renderer radiusRingRenderer;
    private TextMeshPro promptText;
    private SphereCollider triggerCollider;
    private Light completionLight;
    private float holdProgress;
    private bool isCompleted;
    private bool isFadingOut;
    private bool playerInsideRadiusLastFrame;
    private PlayerStats cachedPlayerStats;

    public void Initialize(ShrineEventManager manager)
    {
        eventManager = manager;
        CachePlayer();
        BuildVisual();
        BuildRadiusRing();
        BuildPromptText();
        BuildTrigger();
    }

    private void Update()
    {
        if (isCompleted || isFadingOut) return;

        CachePlayer();

        if (playerTransform == null)
        {
            UpdatePrompt(false, 0f);
            UpdateRadiusRing(false);
            DecayProgress();
            return;
        }

        Vector3 flatPlayer = playerTransform.position;
        flatPlayer.y = 0f;
        Vector3 flatShrine = transform.position;
        flatShrine.y = 0f;
        float distance = Vector3.Distance(flatPlayer, flatShrine);

        bool playerInsideRadius = distance <= HoldRadius;
        bool showPrompt = distance <= PromptRadius;
        playerInsideRadiusLastFrame = playerInsideRadius;

        if (playerInsideRadius)
        {
            holdProgress = Mathf.Min(HoldDurationSeconds, holdProgress + ProgressFillPerSecond * Time.deltaTime);

            if (holdProgress >= HoldDurationSeconds)
            {
                CompleteShrine();
                return;
            }
        }
        else
        {
            DecayProgress();
        }

        UpdatePrompt(showPrompt, holdProgress / HoldDurationSeconds);
        UpdateRadiusRing(playerInsideRadius);
        UpdateGlowPulse();
    }

    private void DecayProgress()
    {
        if (holdProgress <= 0f) return;

        holdProgress = Mathf.Max(0f, holdProgress - ProgressDecayPerSecond * Time.deltaTime);
    }

    private void CompleteShrine()
    {
        if (isCompleted) return;

        isCompleted = true;
        bool bonusChestGranted = GrantRewards();
        UpdatePrompt(false, 1f);
        ShrineRewardPopup.Show(CoinReward, XpReward, bonusChestGranted);
        StartCoroutine(PlayCompletionSequence());
    }

    private bool GrantRewards()
    {
        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = playerTransform != null
                ? playerTransform.GetComponent<PlayerStats>()
                : FindFirstObjectByType<PlayerStats>();
        }

        if (cachedPlayerStats == null) return false;

        cachedPlayerStats.AddCoins(CoinReward);
        cachedPlayerStats.AddXP(XpReward);

        if (Random.value <= BonusChestChance)
        {
            return TrySpawnBonusChest(transform.position);
        }

        return false;
    }

    private static bool TrySpawnBonusChest(Vector3 position)
    {
        if (!LootLimits.CanSpawnChest()) return false;

        GameObject prefab = ChestPrefabUtility.ResolveChestPrefab(ChestRarity.Normal);

        if (prefab == null) return false;

        Vector3 spawnPosition = position;
        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 6f, 1.2f);

        GameObject chestObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Chest chest = chestObject.GetComponent<Chest>();

        if (chest == null) return false;

        chest.ConfigureDroppedReward(ChestRarity.Normal, false);
        return true;
    }

    private IEnumerator PlayCompletionSequence()
    {
        isFadingOut = true;

        if (promptText != null)
        {
            promptText.enabled = false;
        }

        GameObject burstOrb = CreateTransientPart(
            visualRoot,
            "ShrineCompletionOrb",
            PrimitiveType.Sphere,
            new Vector3(0f, 1.05f, 0f),
            new Vector3(0.28f, 0.28f, 0.28f),
            BurstTealColor,
            0.72f,
            true,
            0.85f);

        GameObject burstRing = CreateTransientPart(
            transform,
            "ShrineCompletionBurstRing",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.05f, 0f),
            new Vector3(HoldRadius * 2f, 0.02f, HoldRadius * 2f),
            BurstGoldColor,
            0.58f,
            true,
            0.72f);

        GameObject burstCore = CreateTransientPart(
            visualRoot,
            "ShrineCompletionCore",
            PrimitiveType.Sphere,
            new Vector3(0f, 0.82f, 0f),
            new Vector3(0.55f, 0.55f, 0.55f),
            BurstGoldColor,
            0.66f,
            true,
            0.95f);

        completionLight = gameObject.AddComponent<Light>();
        completionLight.type = LightType.Point;
        completionLight.color = BurstTealColor;
        completionLight.range = HoldRadius * 1.35f;
        completionLight.intensity = 0f;

        Vector3 startVisualScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        Vector3 startRingScale = radiusRingTransform != null ? radiusRingTransform.localScale : Vector3.one;
        float elapsed = 0f;

        while (elapsed < CompletionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / CompletionDuration);
            float burst = Mathf.Sin(t * Mathf.PI);

            if (visualRoot != null)
            {
                visualRoot.localScale = startVisualScale * (1f - t * 0.92f);
            }

            if (radiusRingTransform != null)
            {
                radiusRingTransform.localScale = startRingScale * (1f + t * 0.35f);
                ApplyRingColor(Mathf.Lerp(RingInsideColor.a, 0f, t), true);
            }

            if (burstOrb != null)
            {
                burstOrb.transform.localPosition = new Vector3(0f, 1.05f + t * 1.2f, 0f);
                burstOrb.transform.localScale = Vector3.one * (0.28f + burst * 0.18f);
                FadeTransientPart(burstOrb, 1f - t);
            }

            if (burstRing != null)
            {
                burstRing.transform.localScale = new Vector3(
                    HoldRadius * 2f * (1f + t * 0.55f),
                    0.02f,
                    HoldRadius * 2f * (1f + t * 0.55f));
                FadeTransientPart(burstRing, 1f - t);
            }

            if (burstCore != null)
            {
                burstCore.transform.localScale = Vector3.one * (0.55f + burst * 0.75f);
                FadeTransientPart(burstCore, 1f - t * 0.85f);
            }

            if (completionLight != null)
            {
                completionLight.intensity = burst * 2.4f;
            }

            yield return null;
        }

        eventManager?.NotifyShrineResolved(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!isCompleted && !isFadingOut)
        {
            eventManager?.NotifyShrineResolved(this);
        }
    }

    private void CachePlayer()
    {
        if (playerTransform != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
    }

    private void BuildVisual()
    {
        GameObject rootObject = new GameObject("ShrineVisualRoot");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        visualRoot = rootObject.transform;
        CreatePart(visualRoot, "ShrineBase", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(1.35f, 0.08f, 1.35f), BaseStoneColor, 0.28f, false, 0f);
        CreatePart(visualRoot, "ShrineStep", PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.95f, 0.12f, 0.95f), BaseStoneColor, 0.24f, false, 0f);
        CreatePart(visualRoot, "ShrinePillar_L", PrimitiveType.Cube, new Vector3(-0.28f, 0.52f, -0.28f), new Vector3(0.14f, 0.52f, 0.14f), AccentColor, 0.34f, false, 0.08f);
        CreatePart(visualRoot, "ShrinePillar_R", PrimitiveType.Cube, new Vector3(0.28f, 0.52f, -0.28f), new Vector3(0.14f, 0.52f, 0.14f), AccentColor, 0.34f, false, 0.08f);
        CreatePart(visualRoot, "ShrinePillar_BackL", PrimitiveType.Cube, new Vector3(-0.28f, 0.52f, 0.28f), new Vector3(0.14f, 0.52f, 0.14f), AccentColor, 0.34f, false, 0.08f);
        CreatePart(visualRoot, "ShrinePillar_BackR", PrimitiveType.Cube, new Vector3(0.28f, 0.52f, 0.28f), new Vector3(0.14f, 0.52f, 0.14f), AccentColor, 0.34f, false, 0.08f);
        CreatePart(visualRoot, "ShrineTop", PrimitiveType.Cylinder, new Vector3(0f, 0.82f, 0f), new Vector3(0.72f, 0.05f, 0.72f), AccentColor, 0.38f, true, 0.18f);
        CreatePart(visualRoot, "ShrineGlow", PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), new Vector3(0.22f, 0.22f, 0.22f), GlowColor, 0.62f, true, 0.45f);
    }

    private void BuildRadiusRing()
    {
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "ShrineRadiusRing";
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        ringObject.transform.localRotation = Quaternion.identity;
        ringObject.transform.localScale = new Vector3(HoldRadius * 2f, 0.015f, HoldRadius * 2f);

        Collider ringCollider = ringObject.GetComponent<Collider>();

        if (ringCollider != null)
        {
            Destroy(ringCollider);
        }

        radiusRingTransform = ringObject.transform;
        radiusRingRenderer = ringObject.GetComponent<Renderer>();

        if (radiusRingRenderer != null)
        {
            radiusRingRenderer.sharedMaterial = ChestVisualMaterials.GetGlowBaseMaterial();
            ApplyRingColor(RingOutsideColor.a, false);
        }
    }

    private void BuildPromptText()
    {
        GameObject textObject = new GameObject("ShrinePrompt");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, 1.85f, 0f);

        promptText = textObject.AddComponent<TextMeshPro>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 1.65f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = PromptColor;
        promptText.text = string.Empty;
        promptText.enabled = false;
        promptText.outlineWidth = 0.22f;
        promptText.outlineColor = new Color(0.02f, 0.08f, 0.08f, 0.85f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            promptText.font = font;
        }
    }

    private void BuildTrigger()
    {
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = HoldRadius;
        triggerCollider.center = new Vector3(0f, 0.5f, 0f);
    }

    private void UpdatePrompt(bool visible, float normalizedProgress)
    {
        if (promptText == null) return;

        if (!visible)
        {
            promptText.enabled = false;
            return;
        }

        promptText.enabled = true;

        if (normalizedProgress > 0.001f)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(normalizedProgress) * 100f);
            promptText.text = "HOLD THE SHRINE\n" + percent + "%";
        }
        else
        {
            promptText.text = "HOLD THE SHRINE";
        }

        if (Camera.main != null)
        {
            Vector3 lookDirection = promptText.transform.position - Camera.main.transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                promptText.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }

            float cameraDistance = Vector3.Distance(Camera.main.transform.position, promptText.transform.position);
            float scale = cameraDistance < 6f ? 0.72f : (cameraDistance < 12f ? 0.86f : 1f);
            promptText.transform.localScale = Vector3.one * scale;
        }
    }

    private void UpdateRadiusRing(bool playerInside)
    {
        if (radiusRingTransform == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.03f;
        radiusRingTransform.localScale = new Vector3(
            HoldRadius * 2f * pulse,
            0.015f,
            HoldRadius * 2f * pulse);

        ApplyRingColor(playerInside ? RingInsideColor.a : RingOutsideColor.a, playerInside);
    }

    private void ApplyRingColor(float alpha, bool playerInside)
    {
        if (radiusRingRenderer == null) return;

        Color ringColor = playerInside ? RingInsideColor : RingOutsideColor;
        ringColor.a = alpha;
        GameVisualStyle.ApplyColor(radiusRingRenderer, ringColor, 0.18f, playerInside, playerInside ? 0.35f : 0.12f);
    }

    private void UpdateGlowPulse()
    {
        if (visualRoot == null) return;

        Transform glow = visualRoot.Find("ShrineGlow");

        if (glow == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * 3.2f) * 0.08f;
        float insideBoost = playerInsideRadiusLastFrame ? 1.12f : 1f;
        glow.localScale = Vector3.one * (0.22f * pulse * insideBoost);
    }

    private static GameObject CreateTransientPart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow,
        float emission)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = partName;
        partObject.transform.SetParent(parent, false);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localScale = localScale;
        partObject.transform.localRotation = Quaternion.identity;

        Collider partCollider = partObject.GetComponent<Collider>();

        if (partCollider != null)
        {
            Destroy(partCollider);
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material baseMaterial = ChestVisualMaterials.GetGlowBaseMaterial();
            renderer.material = baseMaterial != null ? new Material(baseMaterial) : renderer.material;
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
        }

        return partObject;
    }

    private static void FadeTransientPart(GameObject partObject, float alphaMultiplier)
    {
        if (partObject == null) return;

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer == null || renderer.material == null) return;

        Color color = renderer.material.HasProperty("_BaseColor")
            ? renderer.material.GetColor("_BaseColor")
            : renderer.material.color;
        color.a *= alphaMultiplier;
        renderer.material.color = color;

        if (renderer.material.HasProperty("_BaseColor"))
        {
            renderer.material.SetColor("_BaseColor", color);
        }
    }

    private void CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow,
        float emission)
    {
        CreateTransientPart(parent, partName, primitive, localPosition, localScale, color, smoothness, glow, emission);
    }
}

internal static class ShrineRewardPopup
{
    internal static readonly Color PanelColor = new Color(0.05f, 0.1f, 0.11f, 0.88f);
    internal static readonly Color TitleColor = new Color(0.72f, 0.92f, 0.86f, 1f);
    internal static readonly Color RewardColor = new Color(0.88f, 0.94f, 0.92f, 1f);
    internal static readonly Color BonusColor = new Color(0.92f, 0.82f, 0.38f, 1f);

    public static void Show(int coins, int xp, bool bonusChest)
    {
        ShrineRewardPopupHost host = new GameObject("ShrineRewardPopupHost").AddComponent<ShrineRewardPopupHost>();
        host.Begin(coins, xp, bonusChest);
    }
}

internal sealed class ShrineRewardPopupHost : MonoBehaviour
{
    private const float Duration = 2.6f;

    private TextMeshProUGUI popupText;
    private Image panelImage;
    private float elapsed;

    public void Begin(int coins, int xp, bool bonusChest)
    {
        BuildPopup(coins, xp, bonusChest);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01((elapsed - (Duration - 0.55f)) / 0.55f);

        if (popupText != null)
        {
            Color textColor = popupText.color;
            textColor.a = fade;
            popupText.color = textColor;
        }

        if (panelImage != null)
        {
            Color panelColor = panelImage.color;
            panelColor.a = ShrineRewardPopup.PanelColor.a * fade;
            panelImage.color = panelColor;
        }

        if (elapsed >= Duration)
        {
            Destroy(gameObject);
        }
    }

    private void BuildPopup(int coins, int xp, bool bonusChest)
    {
        GameObject canvasObject = new GameObject("ShrineRewardPopupCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 36;
        canvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, new Vector2(0f, -40f), new Vector2(420f, bonusChest ? 150f : 118f));

        panelImage = panelObject.AddComponent<Image>();
        panelImage.color = ShrineRewardPopup.PanelColor;
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);

        popupText = textObject.AddComponent<TextMeshProUGUI>();
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.fontSize = 28f;
        popupText.fontStyle = FontStyles.Bold;
        popupText.color = ShrineRewardPopup.TitleColor;
        popupText.raycastTarget = false;
        popupText.text = "SHRINE COMPLETED\n+" + coins + " Coins\n+" + xp + " XP";
        popupText.color = ShrineRewardPopup.RewardColor;

        if (bonusChest)
        {
            popupText.text += "\nBonus Chest!";
        }

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            popupText.font = font;
        }
    }
}
