using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyVisualController : MonoBehaviour
{
    public const string VisualRootName = "EnemyVisualRoot";

    private const float HitFlashDuration = 0.1f;
    private const float ScalePunchDuration = 0.1f;
    private const float DeathPuffDuration = 0.32f;
    private const float DeathPuffScale = 0.12f;

    private static readonly Color HitFlashColor = new Color(1f, 0.58f, 0.48f);
    private static readonly Color DeathPuffColor = new Color(0.95f, 0.25f, 0.2f);
    private static readonly Color EliteRingColor = new Color(1f, 0.86f, 0.12f, 0.95f);
    private static readonly Color EliteRingGlowColor = new Color(1f, 0.78f, 0.08f, 0.55f);
    private static readonly Color GroundShadowColor = new Color(0.04f, 0.06f, 0.08f, 0.24f);
    private static MaterialPropertyBlock sharedFlashBlock;

    private EnemyVisualEnhancer visualEnhancer;
    private Renderer rootRenderer;
    private Transform visualRoot;
    private Renderer[] flashRenderers;
    private Color[] baseRendererColors;
    private GameObject groundShadow;
    private GameObject eliteRingOuter;
    private GameObject eliteRingInner;
    private Enemy.EnemyType currentType = Enemy.EnemyType.Normal;
    private Color baseColor = Color.white;
    private float baseSmoothness = 0.44f;
    private bool baseGlow;
    private bool usingPrefabView;
    private float hitFlashTimer;
    private Coroutine scalePunchRoutine;
    private Vector3 visualRootBaseScale = Vector3.one;

    private void Awake()
    {
        rootRenderer = GetComponent<Renderer>();
        visualEnhancer = GetComponent<EnemyVisualEnhancer>();
    }

    private void Start()
    {
        if (!usingPrefabView)
        {
            StartCoroutine(CacheFallbackRenderersDelayed());
        }
    }

    private IEnumerator CacheFallbackRenderersDelayed()
    {
        yield return null;
        CacheFallbackRenderers();
    }

    private void LateUpdate()
    {
        UpdateHitFlash();
    }

    public void Initialize(Enemy.EnemyType enemyType, Color enemyColor, float smoothness, bool glow)
    {
        currentType = enemyType;
        baseColor = enemyColor;
        baseSmoothness = smoothness;
        baseGlow = glow;
        RebuildVisuals();
    }

    public void RefreshVisual(Enemy.EnemyType enemyType, Color enemyColor, float smoothness, bool glow)
    {
        currentType = enemyType;
        baseColor = enemyColor;
        baseSmoothness = smoothness;
        baseGlow = glow;
        RebuildVisuals();
    }

    public void PlayHitFlash()
    {
        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            CacheFallbackRenderers();
        }

        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            return;
        }

        hitFlashTimer = HitFlashDuration;
        PlayScalePunch();
    }

    private void PlayScalePunch()
    {
        Transform punchTarget = ResolveVisualPunchTarget();

        if (punchTarget == null)
        {
            return;
        }

        if (scalePunchRoutine != null)
        {
            StopCoroutine(scalePunchRoutine);
        }

        visualRootBaseScale = punchTarget.localScale;
        scalePunchRoutine = StartCoroutine(ScalePunchRoutine(punchTarget));
    }

    private Transform ResolveVisualPunchTarget()
    {
        if (visualRoot != null)
        {
            return visualRoot;
        }

        Transform enhancerRoot = transform.Find(VisualRootName);

        if (enhancerRoot != null)
        {
            return enhancerRoot;
        }

        return null;
    }

    private IEnumerator ScalePunchRoutine(Transform punchTarget)
    {
        float elapsed = 0f;

        while (elapsed < ScalePunchDuration)
        {
            if (punchTarget == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / ScalePunchDuration);
            float wave = Mathf.Sin(progress * Mathf.PI);
            float widthScale = 1f + wave * 0.1f;
            float heightScale = 1f - wave * 0.05f;
            punchTarget.localScale = new Vector3(
                visualRootBaseScale.x * widthScale,
                visualRootBaseScale.y * heightScale,
                visualRootBaseScale.z * widthScale);

            yield return null;
        }

        if (punchTarget != null)
        {
            punchTarget.localScale = visualRootBaseScale;
        }

        scalePunchRoutine = null;
    }

    public void PlayDeathPuff(Vector3 position)
    {
        if (position == Vector3.zero)
        {
            position = transform.position;
        }

        GameObject puffHost = new GameObject("EnemyDeathPuffFx");
        EnemyDeathPuffRunner runner = puffHost.AddComponent<EnemyDeathPuffRunner>();
        runner.Run(position, DeathPuffColor, DeathPuffDuration, DeathPuffScale);
    }

    private void RebuildVisuals()
    {
        hitFlashTimer = 0f;
        CleanupCrowdReadabilityDecor(transform);
        Transform existingVisualRoot = transform.Find(VisualRootName);

        if (existingVisualRoot != null)
        {
            CleanupCrowdReadabilityDecor(existingVisualRoot);
        }

        GameObject viewPrefab = EnemyViewPrefabUtility.ResolveViewPrefab(currentType);

        if (viewPrefab != null)
        {
            ClearControllerVisualRoot();
            usingPrefabView = true;

            if (visualEnhancer != null)
            {
                visualEnhancer.enabled = false;
            }

            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            visualRoot = CreateVisualRoot();
            GameObject viewInstance = Instantiate(viewPrefab, visualRoot, false);
            viewInstance.name = viewPrefab.name + "_View";
            viewInstance.SetActive(true);
            SanitizeVisualInstance(viewInstance);
            EnsureActiveVisualRenderers(visualRoot);
            ApplySilhouetteScale(visualRoot, currentType);
            visualRootBaseScale = visualRoot.localScale;
            ApplyPrefabViewColors(viewInstance, visualRoot);
            EnsureCrowdReadabilityDecor();
            CacheFlashRenderers(FilterVisualRenderers(GetVisualRenderers(visualRoot)));
            EnemyVisualFacingController.BindToViewInstance(viewInstance, transform, currentType);
            return;
        }

        if (usingPrefabView)
        {
            ClearControllerVisualRoot();
        }

        usingPrefabView = false;
        ApplyFallbackVisuals();
    }

    private void ApplyFallbackVisuals()
    {
        if (visualEnhancer != null)
        {
            visualEnhancer.enabled = true;
        }

        Transform enhancerRoot = transform.Find(VisualRootName);

        if (enhancerRoot != null)
        {
            visualRoot = enhancerRoot;

            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            ApplyBaseColorsToRenderers(GetVisualRenderers(enhancerRoot));
            EnsureCrowdReadabilityDecor();
            CacheFlashRenderers(FilterVisualRenderers(GetVisualRenderers(enhancerRoot)));
            return;
        }

        visualRoot = transform;

        if (rootRenderer != null)
        {
            rootRenderer.enabled = true;
            GameVisualStyle.ApplyColor(rootRenderer, baseColor, baseSmoothness, baseGlow);
        }

        EnsureCrowdReadabilityDecor();
        CacheFallbackRenderers();
    }

    private void CacheFallbackRenderers()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            flashRenderers = System.Array.Empty<Renderer>();
            baseRendererColors = System.Array.Empty<Color>();
            return;
        }

        CacheFlashRenderers(FilterVisualRenderers(renderers));
    }

    private static bool IsReadabilityDecorRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return true;
        }

        string objectName = renderer.gameObject.name;

        return objectName == "EnemyGroundShadow"
            || objectName == "EliteGlowRingOuter"
            || objectName == "EliteGlowRingInner"
            || objectName == "EliteGlowRing";
    }

    private static Renderer[] FilterVisualRenderers(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
        {
            return System.Array.Empty<Renderer>();
        }

        int keptCount = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (!IsReadabilityDecorRenderer(renderers[i]))
            {
                keptCount++;
            }
        }

        if (keptCount == renderers.Length)
        {
            return renderers;
        }

        if (keptCount == 0)
        {
            return System.Array.Empty<Renderer>();
        }

        Renderer[] filtered = new Renderer[keptCount];
        int writeIndex = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (IsReadabilityDecorRenderer(renderer))
            {
                continue;
            }

            filtered[writeIndex++] = renderer;
        }

        return filtered;
    }

    private void CacheFlashRenderers(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0)
        {
            flashRenderers = System.Array.Empty<Renderer>();
            baseRendererColors = System.Array.Empty<Color>();
            return;
        }

        flashRenderers = renderers;
        baseRendererColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                baseRendererColors[i] = Color.white;
                continue;
            }

            Material sharedMaterial = renderer.sharedMaterial;

            if (sharedMaterial == null)
            {
                baseRendererColors[i] = Color.white;
                continue;
            }

            baseRendererColors[i] = sharedMaterial.HasProperty("_BaseColor")
                ? sharedMaterial.GetColor("_BaseColor")
                : sharedMaterial.color;
        }
    }

    private void UpdateHitFlash()
    {
        if (hitFlashTimer <= 0f || flashRenderers == null || baseRendererColors == null)
        {
            return;
        }

        hitFlashTimer -= Time.deltaTime;
        float flashStrength = Mathf.Clamp01(hitFlashTimer / HitFlashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer renderer = flashRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (flashStrength <= 0.001f)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            ApplyFlashRenderer(renderer, i, flashStrength);
        }
    }

    private void ApplyFlashRenderer(Renderer renderer, int index, float flashStrength)
    {
        if (sharedFlashBlock == null)
        {
            sharedFlashBlock = new MaterialPropertyBlock();
        }

        Color rendererBaseColor = index >= 0 && baseRendererColors != null && index < baseRendererColors.Length
            ? baseRendererColors[index]
            : Color.white;
        Color flashColor = Color.Lerp(rendererBaseColor, HitFlashColor, flashStrength);
        renderer.GetPropertyBlock(sharedFlashBlock);

        Material sharedMaterial = renderer.sharedMaterial;

        if (sharedMaterial != null)
        {
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                sharedFlashBlock.SetColor("_BaseColor", flashColor);
            }

            if (sharedMaterial.HasProperty("_Color"))
            {
                sharedFlashBlock.SetColor("_Color", flashColor);
            }

            if (sharedMaterial.HasProperty("_EmissionColor"))
            {
                sharedFlashBlock.SetColor("_EmissionColor", flashColor * (0.35f + flashStrength * 0.65f));
            }
        }

        renderer.SetPropertyBlock(sharedFlashBlock);
    }

    private void EnsureCrowdReadabilityDecor()
    {
        EnsureGroundShadow();
        EnsureEliteRing();
    }

    private Transform ResolveStableDecorParent()
    {
        return transform;
    }

    private void EnsureGroundShadow()
    {
        RemoveExistingGroundShadow();

        if (!ShouldCreateGroundShadow())
        {
            return;
        }

        Transform shadowParent = ResolveStableDecorParent();

        if (shadowParent == null)
        {
            return;
        }

        float diameter = currentType switch
        {
            Enemy.EnemyType.Tank => 1.08f,
            Enemy.EnemyType.Elite => 1.02f,
            Enemy.EnemyType.Fast => 0.78f,
            Enemy.EnemyType.MiniBoss => 1.2f,
            Enemy.EnemyType.DragonBoss => 1.35f,
            _ => 0.88f
        };

        float alpha = currentType switch
        {
            Enemy.EnemyType.Tank => 0.26f,
            Enemy.EnemyType.Elite => 0.24f,
            Enemy.EnemyType.Fast => 0.2f,
            _ => 0.2f
        };

        groundShadow = CreateFlatDisc(
            shadowParent,
            "EnemyGroundShadow",
            new Vector3(0f, 0.008f, 0f),
            new Vector3(diameter, 0.0035f, diameter),
            new Color(GroundShadowColor.r, GroundShadowColor.g, GroundShadowColor.b, alpha),
            false,
            0f,
            true);
        groundShadow.transform.SetAsFirstSibling();
    }

    private bool ShouldCreateGroundShadow()
    {
        return currentType != Enemy.EnemyType.Normal;
    }

    private void RemoveExistingGroundShadow()
    {
        if (groundShadow != null)
        {
            Destroy(groundShadow);
            groundShadow = null;
        }

        DestroyAllGroundShadows(transform);
    }

    private static void DestroyAllGroundShadows(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);

        for (int i = descendants.Length - 1; i >= 0; i--)
        {
            Transform descendant = descendants[i];

            if (descendant != null && descendant.name == "EnemyGroundShadow")
            {
                Destroy(descendant.gameObject);
            }
        }
    }

    private void EnsureEliteRing()
    {
        Transform ringParent = ResolveStableDecorParent();

        if (eliteRingOuter != null)
        {
            Destroy(eliteRingOuter);
            eliteRingOuter = null;
        }

        if (eliteRingInner != null)
        {
            Destroy(eliteRingInner);
            eliteRingInner = null;
        }

        if (ringParent == null || currentType != Enemy.EnemyType.Elite)
        {
            return;
        }

        eliteRingOuter = CreateFlatDisc(
            ringParent,
            "EliteGlowRingOuter",
            new Vector3(0f, 0.012f, 0f),
            new Vector3(0.98f, 0.006f, 0.98f),
            EliteRingGlowColor,
            true,
            0.32f,
            false);

        eliteRingInner = CreateFlatDisc(
            ringParent,
            "EliteGlowRingInner",
            new Vector3(0f, 0.014f, 0f),
            new Vector3(0.74f, 0.008f, 0.74f),
            EliteRingColor,
            true,
            0.58f,
            false);
    }

    private static GameObject CreateFlatDisc(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        bool emissionGlow,
        float emissionIntensity,
        bool isGroundShadow)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        disc.transform.SetParent(parent, false);
        disc.transform.localPosition = localPosition;
        disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        disc.transform.localScale = localScale;

        Collider collider = disc.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = disc.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, 0.72f, emissionGlow, emissionIntensity);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (isGroundShadow)
        {
            EnemyGroundShadowLock lockComponent = disc.AddComponent<EnemyGroundShadowLock>();
            lockComponent.Configure(localScale);
        }

        return disc;
    }

    private sealed class EnemyGroundShadowLock : MonoBehaviour
    {
        private Vector3 baseLocalScale;

        public void Configure(Vector3 localScale)
        {
            baseLocalScale = localScale;
        }

        private void LateUpdate()
        {
            transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            transform.localScale = baseLocalScale;
        }
    }

    private void ApplyPrefabViewColors(GameObject viewInstance, Transform viewRoot)
    {
        if (viewRoot == null)
        {
            return;
        }

        if (currentType != Enemy.EnemyType.Normal)
        {
            return;
        }

        if (viewInstance != null && viewInstance.GetComponentInChildren<SlimeVisualAnimator>(true) != null)
        {
            return;
        }

        ApplyBaseColorsToRenderers(GetVisualRenderers(viewRoot));
    }

    private void ApplyBaseColorsToRenderers(Renderer[] renderers)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            GameVisualStyle.ApplyColor(renderer, baseColor, baseSmoothness, baseGlow);
        }
    }

    private static Renderer[] GetVisualRenderers(Transform root)
    {
        if (root == null)
        {
            return System.Array.Empty<Renderer>();
        }

        return root.GetComponentsInChildren<Renderer>(true);
    }

    private static void ApplySilhouetteScale(Transform root, Enemy.EnemyType enemyType)
    {
        if (root == null)
        {
            return;
        }

        root.localScale = enemyType switch
        {
            Enemy.EnemyType.Fast => new Vector3(0.92f, 1.04f, 0.92f),
            Enemy.EnemyType.Tank => new Vector3(1.08f, 0.96f, 1.08f),
            Enemy.EnemyType.Elite => new Vector3(1.04f, 1.02f, 1.04f),
            _ => Vector3.one
        };
    }

    private Transform CreateVisualRoot()
    {
        GameObject rootObject = new GameObject(VisualRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private static void CleanupCrowdReadabilityDecor(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        DestroyChildIfExists(parent, "EnemyGroundShadow");
        DestroyChildIfExists(parent, "EliteGlowRingOuter");
        DestroyChildIfExists(parent, "EliteGlowRingInner");
        DestroyChildIfExists(parent, "EliteGlowRing");
    }

    private static void DestroyChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearControllerVisualRoot()
    {
        groundShadow = null;
        eliteRingOuter = null;
        eliteRingInner = null;
        CleanupCrowdReadabilityDecor(transform);

        Transform existingRoot = transform.Find(VisualRootName);

        if (existingRoot != null)
        {
            CleanupCrowdReadabilityDecor(existingRoot);
            Destroy(existingRoot.gameObject);
        }

        visualRoot = null;
    }

    private static void EnsureActiveVisualRenderers(Transform viewRoot)
    {
        if (viewRoot == null)
        {
            return;
        }

        Renderer[] renderers = viewRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || IsIntentionallyInactiveBackup(renderer.transform))
            {
                continue;
            }

            if (!renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!renderer.enabled)
            {
                renderer.enabled = true;
            }
        }
    }

    private static bool IsIntentionallyInactiveBackup(Transform transform)
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.name == "Model_Old_Backup")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void SanitizeVisualInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];

            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
            {
                continue;
            }

            animator.applyRootMotion = false;
        }
    }

    private sealed class EnemyDeathPuffRunner : MonoBehaviour
    {
        public void Run(Vector3 position, Color color, float duration, float scale)
        {
            Destroy(gameObject, duration + 0.1f);
            StartCoroutine(DeathPuffRoutine(position, color, duration, scale));
        }

        private IEnumerator DeathPuffRoutine(Vector3 position, Color color, float duration, float scale)
        {
            transform.position = position + Vector3.up * 0.2f;

            GameObject flashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashObject.name = "EnemyDeathPuff";
            flashObject.transform.SetParent(transform, false);
            flashObject.transform.localScale = Vector3.one * scale;

            Collider flashCollider = flashObject.GetComponent<Collider>();

            if (flashCollider != null)
            {
                Destroy(flashCollider);
            }

            Renderer flashRenderer = flashObject.GetComponent<Renderer>();

            if (flashRenderer != null)
            {
                GameVisualStyle.ApplyColor(flashRenderer, color, 0.55f, true, 0.45f);
            }

            const int particleCount = 4;
            GameObject[] particles = new GameObject[particleCount];
            Vector3[] velocities = new Vector3[particleCount];
            float[] sizes = new float[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                particle.name = "DeathPuffParticle";
                particle.transform.SetParent(transform, false);
                sizes[i] = Random.Range(0.05f, 0.09f);
                particle.transform.localScale = Vector3.one * sizes[i];

                Collider particleCollider = particle.GetComponent<Collider>();

                if (particleCollider != null)
                {
                    Destroy(particleCollider);
                }

                Renderer particleRenderer = particle.GetComponent<Renderer>();

                if (particleRenderer != null)
                {
                    GameVisualStyle.ApplyColor(particleRenderer, color, 0.62f, true, 0.4f);
                }

                Vector2 spread = Random.insideUnitCircle.normalized;
                velocities[i] = new Vector3(spread.x, Random.Range(0.2f, 0.55f), spread.y) * Random.Range(0.9f, 1.6f);
                particles[i] = particle;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (flashObject != null)
                {
                    flashObject.transform.localScale = Vector3.one * (scale * (0.8f + fade * 0.45f));
                    SetRendererAlpha(flashRenderer, fade);
                }

                for (int i = 0; i < particleCount; i++)
                {
                    GameObject particle = particles[i];

                    if (particle == null)
                    {
                        continue;
                    }

                    particle.transform.localPosition += velocities[i] * Time.deltaTime;
                    velocities[i] += Vector3.down * 2.2f * Time.deltaTime;
                    particle.transform.localScale = Vector3.one * sizes[i] * fade;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private static void SetRendererAlpha(Renderer renderer, float alpha)
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
                material.SetColor("_EmissionColor", emission * alpha);
            }
        }
    }
}
