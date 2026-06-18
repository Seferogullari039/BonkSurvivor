using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MimicChestController : MonoBehaviour
{
    private static readonly Color MimicBodyColor = new Color(0.72f, 0.14f, 0.12f);
    private static readonly Color MimicEyeColor = new Color(1f, 0.92f, 0.2f);
    private static readonly Color MimicWakeLabelColor = new Color(1f, 0.22f, 0.18f, 1f);
    private static readonly Color MimicDeathRedColor = new Color(0.92f, 0.18f, 0.14f);
    private static readonly Color MimicDeathGoldColor = new Color(1f, 0.78f, 0.16f);

    private Chest ownerChest;
    private Transform playerTarget;
    private GameObject hitProxy;
    private int maxHealth = 10;
    private int currentHealth;
    private float moveSpeed = 3.5f;
    private float contactDamage = 1f;
    private float lastContactDamageTime = -999f;
    private bool isActivated;
    private Coroutine wakeFeedbackRoutine;
    private Vector3 awakenedLocalScale;

    public bool IsActivated => isActivated;
    public int CurrentHealth => currentHealth;

    public void Initialize(Chest chest, ChestRarity rarity)
    {
        ownerChest = chest;
        maxHealth = rarity switch
        {
            ChestRarity.Epic => 18,
            ChestRarity.Rare => 14,
            _ => 10
        };
        currentHealth = maxHealth;
        EnsureBaseMaterials(rarity);
        ApplyIdleVisuals();
    }

    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;
        HidePriceText();
        ApplyAwakenedVisuals();
        awakenedLocalScale = transform.localScale;
        RunEventMessageDisplay.ShowMimicChest();
        PlayWakeFeedback();
        EnsureHitProxy();
        CachePlayerTarget();
    }

    public void TakeDamage(int damage)
    {
        if (!isActivated || damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth > 0) return;

        Vector3 deathPosition = transform.position;
        PlayDeathFeedback(deathPosition);
        GrantRewards();
    }

    private void Update()
    {
        if (!isActivated || playerTarget == null) return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        float stopDistance = 1.1f;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        }

        TryDamagePlayer(distance);
    }

    private void TryDamagePlayer(float distance)
    {
        if (distance > 1.35f) return;
        if (Time.time - lastContactDamageTime < 0.9f) return;

        PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();

        if (playerStats == null) return;

        playerStats.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(contactDamage)));
        lastContactDamageTime = Time.time;
    }

    private bool rewardPresentationStarted;

    private void GrantRewards()
    {
        if (ownerChest == null || rewardPresentationStarted) return;

        rewardPresentationStarted = true;
        isActivated = false;
        StartCoroutine(CompleteMimicRewardPresentation());
    }

    private IEnumerator CompleteMimicRewardPresentation()
    {
        Vector3 openPosition = transform.position;
        ChestRarity rarity = ownerChest != null ? ownerChest.Rarity : ChestRarity.Normal;

        ChestRevealPause.Begin();
        AudioManager.Instance?.PlayChestOpen();
        yield return ChestOpenPresentation.PlayRevealThenOpenUpgradeMenu(openPosition, rarity, transform);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!rewardPresentationStarted || !ChestRevealPause.IsPaused)
        {
            return;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.IsAwaitingChestRewardCollect())
        {
            return;
        }

        ChestRevealPause.ForceEnd();
    }

    private void HidePriceText()
    {
        Transform priceTransform = transform.Find("PriceText");

        if (priceTransform != null)
        {
            priceTransform.gameObject.SetActive(false);
        }
    }

    private void ApplyIdleVisuals()
    {
        ApplyTintToRenderers(MimicBodyColor, 0.52f, true, 0.22f);
        EnsureEyes();
    }

    private void ApplyAwakenedVisuals()
    {
        transform.localScale *= 1.08f;
        ApplyTintToRenderers(new Color(0.86f, 0.16f, 0.12f), 0.58f, true, 0.35f);
        EnsureEyes(true);
    }

    private void EnsureEyes(bool awakened = false)
    {
        if (transform.Find("MimicEye_L") != null) return;

        CreateEye("MimicEye_L", new Vector3(-0.18f, 0.95f, 0.42f), awakened ? 0.12f : 0.08f);
        CreateEye("MimicEye_R", new Vector3(0.18f, 0.95f, 0.42f), awakened ? 0.12f : 0.08f);
    }

    private void CreateEye(string eyeName, Vector3 localPosition, float size)
    {
        GameObject eyeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeObject.name = eyeName;
        eyeObject.transform.SetParent(transform, false);
        eyeObject.transform.localPosition = localPosition;
        eyeObject.transform.localScale = Vector3.one * size;

        Collider eyeCollider = eyeObject.GetComponent<Collider>();

        if (eyeCollider != null)
        {
            Destroy(eyeCollider);
        }

        Renderer eyeRenderer = eyeObject.GetComponent<Renderer>();

        if (eyeRenderer != null)
        {
            eyeRenderer.sharedMaterial = ChestVisualMaterials.GetMetalBaseMaterial();
            GameVisualStyle.ApplyColor(eyeRenderer, MimicEyeColor, 0.72f, true, 0.45f);
        }
    }

    private void ApplyTintToRenderers(Color color, float smoothness, bool glow, float emission)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null) continue;
            if (renderer.transform.name.StartsWith("MimicEye")) continue;
            if (renderer.transform.name == "PriceText") continue;
            if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;

            EnsureRendererMaterial(renderer);
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
        }
    }

    private void EnsureBaseMaterials(ChestRarity rarity)
    {
        ChestVisual chestVisual = GetComponent<ChestVisual>();

        if (chestVisual != null)
        {
            chestVisual.ApplyRarity(rarity);
            return;
        }

        Renderer chestRenderer = GetComponent<Renderer>();

        if (chestRenderer == null || !chestRenderer.enabled) return;

        ChestVisualMaterials.ApplyWood(chestRenderer, rarity);
    }

    private static void EnsureRendererMaterial(Renderer renderer)
    {
        if (renderer.sharedMaterial != null) return;

        string partName = renderer.transform.name;

        if (partName == "Glow")
        {
            renderer.sharedMaterial = ChestVisualMaterials.GetGlowBaseMaterial();
            return;
        }

        if (partName.StartsWith("MetalBand") || partName == "Lock")
        {
            renderer.sharedMaterial = ChestVisualMaterials.GetMetalBaseMaterial();
            return;
        }

        renderer.sharedMaterial = ChestVisualMaterials.GetWoodBaseMaterial();
    }

    private void EnsureHitProxy()
    {
        if (hitProxy != null) return;

        hitProxy = new GameObject("MimicHitProxy");
        hitProxy.transform.SetParent(transform, false);
        hitProxy.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        hitProxy.tag = "Enemy";

        CapsuleCollider collider = hitProxy.AddComponent<CapsuleCollider>();
        collider.isTrigger = false;
        collider.radius = 0.55f;
        collider.height = 1.1f;
        collider.center = Vector3.zero;

        Enemy proxyEnemy = hitProxy.AddComponent<Enemy>();
        proxyEnemy.BindMimicChest(this);
        proxyEnemy.SetMovementLocked(true);
        proxyEnemy.Configure(0f, maxHealth, MimicBodyColor, Enemy.EnemyType.Normal);
    }

    private void CachePlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void PlayWakeFeedback()
    {
        if (wakeFeedbackRoutine != null)
        {
            StopCoroutine(wakeFeedbackRoutine);
        }

        wakeFeedbackRoutine = StartCoroutine(WakeFeedbackRoutine());
    }

    private void PlayDeathFeedback(Vector3 position)
    {
        GameObject burstHost = new GameObject("MimicDeathBurstFx");
        MimicDeathBurstRunner runner = burstHost.AddComponent<MimicDeathBurstRunner>();
        runner.Run(position);
    }

    private IEnumerator WakeFeedbackRoutine()
    {
        Vector3 basePosition = transform.position;
        Vector3 labelPosition = basePosition + Vector3.up * 1.4f;
        GameObject wakeLabel = CreateWakeLabel(labelPosition);

        const float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float pulse = 1f + Mathf.Sin(progress * Mathf.PI) * 0.14f;
            transform.localScale = awakenedLocalScale * pulse;

            float shakeFade = 1f - progress;
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)) * 0.08f * shakeFade;
            transform.position = basePosition + shakeOffset;

            yield return null;
        }

        transform.localScale = awakenedLocalScale;
        transform.position = basePosition;
        wakeFeedbackRoutine = null;

        if (wakeLabel != null)
        {
            StartCoroutine(AnimateWakeLabel(wakeLabel));
        }
    }

    private static GameObject CreateWakeLabel(Vector3 position)
    {
        GameObject labelObject = new GameObject("MimicWakeLabel");
        labelObject.transform.position = position;

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = "MIMIC!";
        textMesh.fontSize = 36;
        textMesh.characterSize = 0.07f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = MimicWakeLabelColor;
        textMesh.fontStyle = FontStyle.Bold;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font != null)
        {
            textMesh.font = font;
        }

        FaceWorldLabel(labelObject.transform);
        return labelObject;
    }

    private static IEnumerator AnimateWakeLabel(GameObject labelObject)
    {
        if (labelObject == null) yield break;

        TextMesh textMesh = labelObject.GetComponent<TextMesh>();
        Vector3 startPosition = labelObject.transform.position;
        const float duration = 0.75f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (labelObject == null) yield break;

            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float fade = 1f - progress;

            labelObject.transform.position = startPosition + Vector3.up * (progress * 0.65f);
            FaceWorldLabel(labelObject.transform);

            if (textMesh != null)
            {
                Color labelColor = textMesh.color;
                labelColor.a = fade;
                textMesh.color = labelColor;
            }

            yield return null;
        }

        Destroy(labelObject);
    }

    private static void FaceWorldLabel(Transform labelTransform)
    {
        if (labelTransform == null) return;

        Camera camera = Camera.main;

        if (camera == null) return;

        labelTransform.rotation = Quaternion.LookRotation(
            labelTransform.position - camera.transform.position,
            Vector3.up);
    }

    private static void ApplyBurstColor(Renderer renderer, Color color, float emission)
    {
        if (renderer == null) return;

        Material baseMaterial = ChestVisualMaterials.GetGlowBaseMaterial();

        if (baseMaterial != null)
        {
            renderer.sharedMaterial = baseMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, color, 0.75f, true, emission);
    }

    private sealed class MimicDeathBurstRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(DeathBurstRoutine(position));
        }

        private IEnumerator DeathBurstRoutine(Vector3 position)
        {
            Vector3 effectPosition = position + Vector3.up * 0.55f;

            GameObject redFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            redFlash.name = "MimicDeathFlashRed";
            redFlash.transform.position = effectPosition;
            redFlash.transform.localScale = Vector3.one * 0.22f;
            DestroyCollider(redFlash);
            ApplyBurstColor(redFlash.GetComponent<Renderer>(), MimicDeathRedColor, 0.62f);

            GameObject goldFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            goldFlash.name = "MimicDeathFlashGold";
            goldFlash.transform.position = effectPosition;
            goldFlash.transform.localScale = Vector3.one * 0.14f;
            DestroyCollider(goldFlash);
            ApplyBurstColor(goldFlash.GetComponent<Renderer>(), MimicDeathGoldColor, 0.72f);

            const float duration = 0.38f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (redFlash != null)
                {
                    redFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 0.72f, progress);
                }

                if (goldFlash != null)
                {
                    goldFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.14f, 0.48f, progress);
                    goldFlash.transform.position = effectPosition + Vector3.up * (progress * 0.18f);
                }

                SetBurstAlpha(redFlash, fade);
                SetBurstAlpha(goldFlash, fade * 0.9f);

                yield return null;
            }

            DestroyEffect(redFlash);
            DestroyEffect(goldFlash);
            Destroy(gameObject);
        }

        private static void DestroyCollider(GameObject target)
        {
            if (target == null) return;

            Collider collider = target.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private static void SetBurstAlpha(GameObject target, float alpha)
        {
            if (target == null) return;

            Renderer renderer = target.GetComponent<Renderer>();
            Material material = renderer != null ? renderer.material : null;

            if (material == null) return;

            if (material.HasProperty("_BaseColor"))
            {
                Color baseColor = material.GetColor("_BaseColor");
                baseColor.a = alpha;
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color");
                color.a = alpha;
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = material.GetColor("_EmissionColor");
                emission *= alpha;
                material.SetColor("_EmissionColor", emission);
            }
        }

        private static void DestroyEffect(GameObject target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }
}
