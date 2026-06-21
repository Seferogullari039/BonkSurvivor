using System.Collections;
using TMPro;
using UnityEngine;

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
    private const float FadeDuration = 0.65f;

    private static readonly Color BaseStoneColor = new Color(0.42f, 0.44f, 0.4f);
    private static readonly Color AccentColor = new Color(0.18f, 0.52f, 0.46f);
    private static readonly Color GlowColor = new Color(0.28f, 0.72f, 0.62f);
    private static readonly Color PromptColor = new Color(0.86f, 0.94f, 0.92f, 1f);

    private ShrineEventManager eventManager;
    private Transform playerTransform;
    private Transform visualRoot;
    private TextMeshPro promptText;
    private SphereCollider triggerCollider;
    private float holdProgress;
    private bool isCompleted;
    private bool isFadingOut;
    private PlayerStats cachedPlayerStats;

    public void Initialize(ShrineEventManager manager)
    {
        eventManager = manager;
        CachePlayer();
        BuildVisual();
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
        GrantRewards();
        UpdatePrompt(false, 1f);
        StartCoroutine(FadeOutAndDestroy());
    }

    private void GrantRewards()
    {
        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = playerTransform != null
                ? playerTransform.GetComponent<PlayerStats>()
                : FindFirstObjectByType<PlayerStats>();
        }

        if (cachedPlayerStats == null) return;

        cachedPlayerStats.AddCoins(CoinReward);
        cachedPlayerStats.AddXP(XpReward);

        if (Random.value <= BonusChestChance)
        {
            TrySpawnBonusChest(transform.position);
        }
    }

    private static void TrySpawnBonusChest(Vector3 position)
    {
        if (!LootLimits.CanSpawnChest()) return;

        GameObject prefab = ChestPrefabUtility.ResolveChestPrefab(ChestRarity.Normal);

        if (prefab == null) return;

        Vector3 spawnPosition = position;
        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 6f, 1.2f);

        GameObject chestObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Chest chest = chestObject.GetComponent<Chest>();

        if (chest == null) return;

        chest.ConfigureDroppedReward(ChestRarity.Normal, false);
    }

    private IEnumerator FadeOutAndDestroy()
    {
        isFadingOut = true;
        float elapsed = 0f;
        Vector3 startScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FadeDuration);

            if (visualRoot != null)
            {
                visualRoot.localScale = startScale * (1f - t);
            }

            if (promptText != null)
            {
                Color color = promptText.color;
                color.a = 1f - t;
                promptText.color = color;
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

    private void BuildPromptText()
    {
        GameObject textObject = new GameObject("ShrinePrompt");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);

        promptText = textObject.AddComponent<TextMeshPro>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = 2.4f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = PromptColor;
        promptText.text = string.Empty;
        promptText.enabled = false;

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
            promptText.text = "Hold the Shrine\n" + percent + "%";
        }
        else
        {
            promptText.text = "Hold the Shrine";
        }

        if (Camera.main != null)
        {
            Vector3 lookDirection = promptText.transform.position - Camera.main.transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                promptText.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }
    }

    private void UpdateGlowPulse()
    {
        if (visualRoot == null) return;

        Transform glow = visualRoot.Find("ShrineGlow");

        if (glow == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * 3.2f) * 0.08f;
        glow.localScale = Vector3.one * (0.22f * pulse);
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

        if (renderer == null) return;

        renderer.sharedMaterial = ChestVisualMaterials.GetGlowBaseMaterial();
        GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
    }
}
