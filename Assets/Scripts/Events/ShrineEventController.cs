using System.Collections;
using System.Collections.Generic;
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
    private const float RelicRewardChance = 0.20f;
    private const int CoinReward = 25;
    private const int XpReward = 3;
    private const float CompletionDuration = 0.62f;
    private const float PromptBaseLocalY = 1.35f;
    private const float PromptWaveOverlapLocalY = 1.12f;
    private const float PromptMinScale = 0.55f;
    private const float PromptMaxScale = 0.82f;
    private const float PromptCloseDistance = 3.5f;
    private const float PromptFarDistance = 18f;
    private const float PromptFontSize = 1.45f;
    private const float WaveBannerSuppressDuration = 2.1f;
    private const int OuterRingSegmentCount = 44;
    private const float InnerFillOutsideAlpha = 0.04f;
    private const float InnerFillInsideAlpha = 0.085f;
    private const float OuterRingOutsideAlpha = 0.3f;
    private const float OuterRingInsideAlpha = 0.4f;
    private const float BeaconHeight = 11f;
    private const float BeaconRadius = 0.18f;
    private const float BeaconBaseAlpha = 0.22f;
    private const float BeaconPulseAlpha = 0.05f;
    private const float BeaconNearFadeMultiplier = 0.55f;

    private static readonly Color BaseStoneColor = new Color(0.42f, 0.44f, 0.4f);
    private static readonly Color AccentColor = new Color(0.18f, 0.52f, 0.46f);
    private static readonly Color GlowColor = new Color(0.28f, 0.72f, 0.62f);
    private static readonly Color PromptColor = new Color(0.92f, 0.98f, 0.96f, 0.95f);
    private static readonly Color InnerFillTeal = new Color(0.14f, 0.42f, 0.4f, 1f);
    private static readonly Color OuterRingTeal = new Color(0.2f, 0.72f, 0.64f, 1f);
    private static readonly Color BurstGoldColor = new Color(0.92f, 0.78f, 0.28f);
    private static readonly Color BurstTealColor = new Color(0.28f, 0.82f, 0.68f);
    private static readonly Color BeaconTeal = new Color(0.22f, 0.78f, 0.7f, 1f);

    private ShrineEventManager eventManager;
    private Transform playerTransform;
    private Transform visualRoot;
    private Transform radiusVisualRoot;
    private Renderer innerFillRenderer;
    private Transform outerRingRoot;
    private readonly List<Renderer> outerRingRenderers = new List<Renderer>(OuterRingSegmentCount);
    private TextMeshPro promptText;
    private SphereCollider triggerCollider;
    private Transform beaconRoot;
    private Renderer beaconRenderer;
    private Material beaconMaterial;
    private Light completionLight;
    private float holdProgress;
    private bool isCompleted;
    private bool isFadingOut;
    private bool playerInsideRadiusLastFrame;
    private PlayerStats cachedPlayerStats;
    private int lastShrineCoinGain;
    private bool lastRelicGranted;
    private RelicType lastGrantedRelic;
    private EnemySpawner cachedSpawner;
    private int cachedWaveBannerWave = -1;
    private float waveBannerEndTime;
    private static bool loggedBonusChestSkipWarning;
    private static bool loggedBonusChestSpawnResult;

    public void Initialize(ShrineEventManager manager)
    {
        eventManager = manager;
        CachePlayer();
        BuildVisual();
        BuildRadiusVisual();
        BuildBeacon();
        BuildPromptText();
        BuildTrigger();
    }

    private void Update()
    {
        if (isCompleted || isFadingOut) return;

        CachePlayer();
        UpdateWaveBannerState();

        if (playerTransform == null)
        {
            UpdatePrompt(false, 0f);
            UpdateRadiusVisual(false);
            UpdateBeacon(false);
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
        UpdateRadiusVisual(playerInsideRadius);
        UpdateBeacon(playerInsideRadius);
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
        int displayedCoins = lastShrineCoinGain > 0 ? lastShrineCoinGain : CoinReward;
        string relicName = lastRelicGranted ? RelicManager.GetDisplayName(lastGrantedRelic) : null;
        ShrineRewardPopup.Show(displayedCoins, XpReward, bonusChestGranted, relicName);
        VisualBugInspector.ReportLargeRenderersNearOnce(
            transform.position,
            10f,
            "[ShrineEvent] Post-completion visual scan");
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

        int coinsBefore = cachedPlayerStats.Coins;
        cachedPlayerStats.AddCoins(CoinReward);
        lastShrineCoinGain = Mathf.Max(0, cachedPlayerStats.Coins - coinsBefore);
        cachedPlayerStats.AddXP(XpReward);

        bool bonusChestGranted = false;

        if (Random.value <= BonusChestChance)
        {
            bonusChestGranted = TrySpawnBonusChest(transform.position);
        }

        lastRelicGranted = false;

        if (Random.value <= RelicRewardChance && RelicManager.Instance != null)
        {
            if (RelicManager.Instance.TryGrantRandomRelic(out RelicType grantedRelic))
            {
                lastRelicGranted = true;
                lastGrantedRelic = grantedRelic;
            }
        }

        return bonusChestGranted;
    }

    private bool TrySpawnBonusChest(Vector3 shrineCenter)
    {
        if (!LootLimits.CanSpawnChest())
        {
            return false;
        }

        GameObject prefab = ResolveBonusChestPrefab();

        if (prefab == null)
        {
            LogBonusChestSkipOnce("valid chest prefab not found");
            return false;
        }

        if (!TryFindBonusChestSpawnPosition(shrineCenter, out Vector3 spawnPosition))
        {
            LogBonusChestSkipOnce("safe spawn not found");
            return false;
        }

        GameObject chestObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Chest chest = chestObject.GetComponent<Chest>();

        if (chest == null)
        {
            Destroy(chestObject);
            LogBonusChestSkipOnce("valid chest prefab not found");
            return false;
        }

        chest.ConfigureDroppedReward(ChestRarity.Normal, false);

        if (!IsValidSpawnedBonusChest(chestObject))
        {
            Destroy(chestObject);
            LogBonusChestSkipOnce("spawned chest failed validation");
            return false;
        }

        LogBonusChestSpawnOnce(chestObject, spawnPosition);
        return true;
    }

    private static GameObject ResolveBonusChestPrefab()
    {
        GameObject prefab = ChestPrefabUtility.ResolveChestPrefab(ChestRarity.Normal);

        if (IsValidBonusChestPrefab(prefab))
        {
            return prefab;
        }

        ChestSpawner chestSpawner = Object.FindFirstObjectByType<ChestSpawner>();

        if (chestSpawner != null)
        {
            GameObject spawnerPrefab = chestSpawner.GetChestPrefabForRarity(ChestRarity.Normal);

            if (IsValidBonusChestPrefab(spawnerPrefab))
            {
                return spawnerPrefab;
            }
        }

        return null;
    }

    private static bool IsValidBonusChestPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        if (prefab.GetComponent<Chest>() == null)
        {
            return false;
        }

        if (prefab.GetComponent<ChestVisual>() == null
            && prefab.transform.Find("ChestVisualRoot") == null)
        {
            return false;
        }

        if (prefab.name.IndexOf("Chest", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        Renderer rootRenderer = prefab.GetComponent<Renderer>();

        if (rootRenderer != null && rootRenderer.enabled)
        {
            return false;
        }

        return true;
    }

    private static bool IsValidSpawnedBonusChest(GameObject chestObject)
    {
        if (chestObject == null)
        {
            return false;
        }

        Chest chest = chestObject.GetComponent<Chest>();

        if (chest == null)
        {
            return false;
        }

        ChestVisual chestVisual = chestObject.GetComponent<ChestVisual>();
        Transform visualRoot = chestObject.transform.Find("ChestVisualRoot");

        if (chestVisual == null && visualRoot == null)
        {
            return false;
        }

        Renderer rootRenderer = chestObject.GetComponent<Renderer>();

        if (rootRenderer != null && rootRenderer.enabled)
        {
            return false;
        }

        Vector3 scale = chestObject.transform.localScale;

        if (scale.x > 1.5f || scale.y > 1.5f || scale.z > 1.5f)
        {
            return false;
        }

        if (LooksLikeStandaloneFallbackCube(chestObject))
        {
            return false;
        }

        Transform resolvedVisualRoot = visualRoot;

        if (resolvedVisualRoot == null && chestVisual != null)
        {
            resolvedVisualRoot = chestObject.transform.Find("ChestVisualRoot");
        }

        if (resolvedVisualRoot == null)
        {
            return false;
        }

        Transform chestBase = resolvedVisualRoot.Find("ChestBase");
        Transform chestLid = resolvedVisualRoot.Find("ChestLid");

        if (chestBase == null || chestLid == null)
        {
            return false;
        }

        return true;
    }

    private static bool LooksLikeStandaloneFallbackCube(GameObject chestObject)
    {
        if (chestObject.transform.childCount > 0)
        {
            return false;
        }

        MeshFilter meshFilter = chestObject.GetComponent<MeshFilter>();
        Renderer renderer = chestObject.GetComponent<Renderer>();

        if (meshFilter == null || renderer == null || !renderer.enabled)
        {
            return false;
        }

        if (meshFilter.sharedMesh == null)
        {
            return true;
        }

        string meshName = meshFilter.sharedMesh.name;

        return meshName.IndexOf("Cube", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TryFindBonusChestSpawnPosition(Vector3 shrineCenter, out Vector3 spawnPosition)
    {
        spawnPosition = shrineCenter;
        Vector3 flatCenter = shrineCenter;
        flatCenter.y = 0f;

        if (ProceduralGrassArena.TryGetSafeChestSpawnPoint(flatCenter, 3.5f, 5f, 1.2f, out spawnPosition)
            && IsBonusChestSpawnCandidateValid(flatCenter, spawnPosition))
        {
            spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
            return true;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float distance = Random.Range(3.5f, 5f);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
            Vector3 candidate = flatCenter + offset;
            candidate.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
            ProceduralGrassArena.TryClampHorizontal(ref candidate);
            ProceduralGrassArena.TryResolveBlockedSpawn(ref candidate, 3.5f, 1.2f);

            if (!IsBonusChestSpawnCandidateValid(flatCenter, candidate))
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        return false;
    }

    private bool IsBonusChestSpawnCandidateValid(Vector3 flatShrineCenter, Vector3 candidate)
    {
        Vector3 flatCandidate = candidate;
        flatCandidate.y = 0f;

        if ((flatCandidate - flatShrineCenter).sqrMagnitude < 3.2f * 3.2f)
        {
            return false;
        }

        if (ProceduralGrassArena.Instance != null
            && ProceduralGrassArena.Instance.IsPositionBlocked(candidate, 1.2f))
        {
            return false;
        }

        if (playerTransform != null)
        {
            Vector3 flatPlayer = playerTransform.position;
            flatPlayer.y = 0f;

            if ((flatCandidate - flatPlayer).sqrMagnitude < 2.5f * 2.5f)
            {
                return false;
            }
        }

        return true;
    }

    private static void LogBonusChestSkipOnce(string reason)
    {
        if (loggedBonusChestSkipWarning)
        {
            return;
        }

        loggedBonusChestSkipWarning = true;
        Debug.LogWarning("[ShrineEvent] Bonus chest skipped: " + reason + ".");
    }

    private static void LogBonusChestSpawnOnce(GameObject chestObject, Vector3 position)
    {
        if (loggedBonusChestSpawnResult || chestObject == null)
        {
            return;
        }

        loggedBonusChestSpawnResult = true;

        Chest chest = chestObject.GetComponent<Chest>();
        ChestVisual chestVisual = chestObject.GetComponent<ChestVisual>();
        Renderer rootRenderer = chestObject.GetComponent<Renderer>();
        MeshFilter rootMeshFilter = chestObject.GetComponent<MeshFilter>();
        string parentName = chestObject.transform.parent != null
            ? chestObject.transform.parent.name
            : "none";
        string materialName = rootRenderer != null && rootRenderer.sharedMaterial != null
            ? rootRenderer.sharedMaterial.name
            : "none";
        string meshName = rootMeshFilter != null && rootMeshFilter.sharedMesh != null
            ? rootMeshFilter.sharedMesh.name
            : "none";
        Vector3 scale = chestObject.transform.localScale;

        Debug.Log("[ShrineEvent] Bonus chest result: object="
            + chestObject.name
            + " parent="
            + parentName
            + " hasChest="
            + (chest != null)
            + " hasChestVisual="
            + (chestVisual != null)
            + " material="
            + materialName
            + " mesh="
            + meshName
            + " scale="
            + scale
            + " position="
            + position);
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

        const float completionRingDiameter = 3.6f;

        GameObject burstRing = CreateTransientPart(
            visualRoot,
            "ShrineCompletionBurstRing",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.05f, 0f),
            new Vector3(completionRingDiameter, 0.02f, completionRingDiameter),
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
        Vector3 startRadiusVisualScale = radiusVisualRoot != null ? radiusVisualRoot.localScale : Vector3.one;
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

            if (radiusVisualRoot != null)
            {
                radiusVisualRoot.localScale = startRadiusVisualScale * (1f + t * 0.35f);
                ApplyRadiusVisual(true, 1f - t);
            }

            if (burstOrb != null)
            {
                burstOrb.transform.localPosition = new Vector3(0f, 1.05f + t * 1.2f, 0f);
                burstOrb.transform.localScale = Vector3.one * (0.28f + burst * 0.18f);
                FadeTransientPart(burstOrb, 1f - t);
            }

            if (burstRing != null)
            {
                float ringDiameter = completionRingDiameter * (1f + t * 0.35f);
                burstRing.transform.localScale = new Vector3(ringDiameter, 0.02f, ringDiameter);
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

            if (beaconRenderer != null && beaconMaterial != null)
            {
                SetBeaconAlpha(Mathf.Max(0f, BeaconBaseAlpha * (1f - t)));
            }

            yield return null;
        }

        if (beaconRoot != null)
        {
            Destroy(beaconRoot.gameObject);
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

    private void BuildBeacon()
    {
        GameObject rootObject = new GameObject("ShrineBeacon");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        beaconRoot = rootObject.transform;

        GameObject columnObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        columnObject.name = "ShrineBeaconColumn";
        columnObject.transform.SetParent(beaconRoot, false);
        // Default cylinder is 2 units tall (center origin); scale.y = height / 2 lifts the top to BeaconHeight.
        columnObject.transform.localPosition = new Vector3(0f, BeaconHeight * 0.5f, 0f);
        columnObject.transform.localRotation = Quaternion.identity;
        columnObject.transform.localScale = new Vector3(BeaconRadius * 2f, BeaconHeight * 0.5f, BeaconRadius * 2f);
        DestroyCollider(columnObject);

        beaconRenderer = columnObject.GetComponent<Renderer>();

        if (beaconRenderer != null)
        {
            Material template = CreateRadiusTransparentMaterial();
            beaconMaterial = template != null ? new Material(template) : beaconRenderer.material;
            beaconRenderer.material = beaconMaterial;
            beaconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beaconRenderer.receiveShadows = false;
            SetBeaconAlpha(BeaconBaseAlpha);
        }
    }

    private void UpdateBeacon(bool playerInside)
    {
        if (beaconRenderer == null || beaconMaterial == null)
        {
            return;
        }

        float pulse = Mathf.Sin(Time.time * 2.1f) * BeaconPulseAlpha;
        float alpha = BeaconBaseAlpha + pulse;

        if (playerInside)
        {
            alpha *= BeaconNearFadeMultiplier;
        }

        SetBeaconAlpha(Mathf.Max(0f, alpha));
    }

    private void SetBeaconAlpha(float alpha)
    {
        if (beaconMaterial == null)
        {
            return;
        }

        Color color = BeaconTeal;
        color.a = alpha;

        if (beaconMaterial.HasProperty("_BaseColor"))
        {
            beaconMaterial.SetColor("_BaseColor", color);
        }

        if (beaconMaterial.HasProperty("_Color"))
        {
            beaconMaterial.SetColor("_Color", color);
        }

        if (beaconMaterial.HasProperty("_EmissionColor"))
        {
            beaconMaterial.EnableKeyword("_EMISSION");
            beaconMaterial.SetColor("_EmissionColor", new Color(BeaconTeal.r, BeaconTeal.g, BeaconTeal.b, 1f) * (alpha * 0.6f));
        }
    }

    private void BuildRadiusVisual()
    {
        GameObject rootObject = new GameObject("ShrineRadiusVisual");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        radiusVisualRoot = rootObject.transform;

        float innerFillDiameter = (HoldRadius * 2f) - 0.85f;
        GameObject innerFillObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        innerFillObject.name = "ShrineInnerFill";
        innerFillObject.transform.SetParent(radiusVisualRoot, false);
        innerFillObject.transform.localPosition = new Vector3(0f, 0.035f, 0f);
        innerFillObject.transform.localRotation = Quaternion.identity;
        innerFillObject.transform.localScale = new Vector3(innerFillDiameter, 0.003f, innerFillDiameter);
        DestroyCollider(innerFillObject);
        innerFillRenderer = innerFillObject.GetComponent<Renderer>();
        SetupRadiusRenderer(innerFillRenderer);

        GameObject outerRingObject = new GameObject("ShrineOuterRing");
        outerRingObject.transform.SetParent(radiusVisualRoot, false);
        outerRingObject.transform.localPosition = new Vector3(0f, 0.045f, 0f);
        outerRingObject.transform.localRotation = Quaternion.identity;
        outerRingObject.transform.localScale = Vector3.one;
        outerRingRoot = outerRingObject.transform;

        const float ringThickness = 0.14f;
        float segmentLength = (Mathf.PI * 2f * HoldRadius / OuterRingSegmentCount) * 1.12f;

        for (int i = 0; i < OuterRingSegmentCount; i++)
        {
            float angle = (Mathf.PI * 2f * i) / OuterRingSegmentCount;
            float x = Mathf.Cos(angle) * HoldRadius;
            float z = Mathf.Sin(angle) * HoldRadius;

            GameObject segmentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segmentObject.name = "ShrineOuterRingSegment";
            segmentObject.transform.SetParent(outerRingRoot, false);
            segmentObject.transform.localPosition = new Vector3(x, 0f, z);
            segmentObject.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            segmentObject.transform.localScale = new Vector3(ringThickness, 0.012f, segmentLength);
            DestroyCollider(segmentObject);

            Renderer segmentRenderer = segmentObject.GetComponent<Renderer>();

            if (segmentRenderer == null)
            {
                continue;
            }

            SetupRadiusRenderer(segmentRenderer);
            outerRingRenderers.Add(segmentRenderer);
        }

        if (innerFillRenderer != null || outerRingRenderers.Count > 0)
        {
            ApplyRadiusVisual(false, 1f);
        }
    }

    private void BuildPromptText()
    {
        GameObject textObject = new GameObject("ShrinePrompt");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, PromptBaseLocalY, 0f);

        promptText = textObject.AddComponent<TextMeshPro>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = PromptFontSize;
        promptText.lineSpacing = -2f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = PromptColor;
        promptText.text = string.Empty;
        promptText.enabled = false;
        promptText.outlineWidth = 0.28f;
        promptText.outlineColor = new Color(0.02f, 0.08f, 0.08f, 0.92f);

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

    private void UpdateWaveBannerState()
    {
        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null)
        {
            return;
        }

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave == cachedWaveBannerWave)
        {
            return;
        }

        cachedWaveBannerWave = currentWave;
        waveBannerEndTime = Time.time + WaveBannerSuppressDuration;
    }

    private void UpdatePrompt(bool visible, float normalizedProgress)
    {
        if (promptText == null)
        {
            return;
        }

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

        bool waveBannerActive = Time.time < waveBannerEndTime;
        float localY = waveBannerActive ? PromptWaveOverlapLocalY : PromptBaseLocalY;
        promptText.transform.localPosition = new Vector3(0f, localY, 0f);

        Camera camera = Camera.main;

        if (camera == null)
        {
            promptText.transform.localScale = Vector3.one * PromptMinScale;
            promptText.color = PromptColor;
            return;
        }

        Vector3 lookDirection = promptText.transform.position - camera.transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            promptText.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        float cameraDistance = Vector3.Distance(camera.transform.position, promptText.transform.position);
        float distanceT = Mathf.InverseLerp(PromptCloseDistance, PromptFarDistance, cameraDistance);
        float scale = Mathf.Lerp(PromptMinScale, PromptMaxScale, distanceT);

        if (waveBannerActive)
        {
            scale *= 0.88f;
        }

        promptText.transform.localScale = Vector3.one * scale;

        Color textColor = PromptColor;
        textColor.a = waveBannerActive ? PromptColor.a * 0.78f : PromptColor.a;
        promptText.color = textColor;
    }

    private void UpdateRadiusVisual(bool playerInside)
    {
        ApplyRadiusVisual(playerInside, 1f);
    }

    private void ApplyRadiusVisual(bool playerInside, float alphaMultiplier)
    {
        if (radiusVisualRoot == null) return;

        float pulse = 1f + Mathf.Sin(Time.time * 2.8f) * 0.025f;

        if (outerRingRoot != null)
        {
            outerRingRoot.localScale = Vector3.one * pulse;
        }

        float innerAlpha = (playerInside ? InnerFillInsideAlpha : InnerFillOutsideAlpha) * alphaMultiplier;
        Color innerColor = InnerFillTeal;
        innerColor.a = innerAlpha;
        ApplyRadiusRenderer(innerFillRenderer, innerColor, 0.04f, 0f);

        float outerAlpha = (playerInside ? OuterRingInsideAlpha : OuterRingOutsideAlpha) * alphaMultiplier;
        Color outerColor = OuterRingTeal;
        outerColor.a = outerAlpha;
        float outerEmission = playerInside ? 0.14f : 0.06f;

        for (int i = 0; i < outerRingRenderers.Count; i++)
        {
            Renderer segmentRenderer = outerRingRenderers[i];

            if (segmentRenderer == null)
            {
                continue;
            }

            ApplyRadiusRenderer(segmentRenderer, outerColor, 0.12f, outerEmission);
        }
    }

    private static void SetupRadiusRenderer(Renderer renderer)
    {
        if (renderer == null) return;

        Material template = CreateRadiusTransparentMaterial();
        renderer.material = template != null ? new Material(template) : renderer.material;
    }

    private static Material radiusTransparentMaterialTemplate;

    private static Material CreateRadiusTransparentMaterial()
    {
        if (radiusTransparentMaterialTemplate != null)
        {
            return radiusTransparentMaterialTemplate;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        radiusTransparentMaterialTemplate = material;
        return material;
    }

    private static void ApplyRadiusRenderer(Renderer renderer, Color color, float smoothness, float emission)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.material;

        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (!material.HasProperty("_EmissionColor"))
        {
            return;
        }

        if (emission <= 0.001f)
        {
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            return;
        }

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * emission);
    }

    private static void DestroyCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }
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
            Material effectMaterial = CreateTransientEffectMaterial(color, smoothness, glow, emission);

            if (effectMaterial != null)
            {
                renderer.material = effectMaterial;
            }
        }

        return partObject;
    }

    private static Material CreateTransientEffectMaterial(Color color, float smoothness, bool glow, float emission)
    {
        Material template = ChestVisualMaterials.GetGlowBaseMaterial();

        if (template != null)
        {
            Material instance = new Material(template);
            ApplyTransientEffectColor(instance, color, smoothness, glow, emission);
            return instance;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader);
        ApplyTransientEffectColor(material, color, smoothness, glow, emission);
        return material;
    }

    private static void ApplyTransientEffectColor(
        Material material,
        Color color,
        float smoothness,
        bool glow,
        float emission)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (!material.HasProperty("_EmissionColor"))
        {
            return;
        }

        if (glow)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            return;
        }

        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");
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

    public static void Show(int coins, int xp, bool bonusChest, string relicName = null)
    {
        ShrineRewardPopupHost host = new GameObject("ShrineRewardPopupHost").AddComponent<ShrineRewardPopupHost>();
        host.Begin(coins, xp, bonusChest, relicName);
    }
}

internal sealed class ShrineRewardPopupHost : MonoBehaviour
{
    private const float Duration = 2.6f;

    private TextMeshProUGUI popupText;
    private Image panelImage;
    private float elapsed;

    public void Begin(int coins, int xp, bool bonusChest, string relicName)
    {
        BuildPopup(coins, xp, bonusChest, relicName);
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

    private void BuildPopup(int coins, int xp, bool bonusChest, string relicName)
    {
        bool hasRelic = !string.IsNullOrEmpty(relicName);

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
        float panelHeight = 118f + (bonusChest ? 32f : 0f) + (hasRelic ? 32f : 0f);
        UiLayoutUtility.SetAnchorCenter(panelRect, new Vector2(0f, -40f), new Vector2(420f, panelHeight));

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

        if (hasRelic)
        {
            popupText.text += "\nNEW RELIC: " + relicName.ToUpperInvariant();
        }

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
