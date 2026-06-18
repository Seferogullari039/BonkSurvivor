using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyVisualController : MonoBehaviour
{
    public const string VisualRootName = "EnemyVisualRoot";

    private const float HitFlashDuration = 0.12f;
    private const float DeathPuffDuration = 0.24f;
    private const float DeathPuffScale = 0.1f;

    private static readonly Color HitFlashColor = new Color(1f, 0.58f, 0.48f);
    private static readonly Color DeathPuffColor = new Color(0.95f, 0.25f, 0.2f);
    private static readonly Color EliteRingColor = new Color(1f, 0.86f, 0.12f);
    private static MaterialPropertyBlock sharedFlashBlock;

    private EnemyVisualEnhancer visualEnhancer;
    private Renderer rootRenderer;
    private Transform visualRoot;
    private Renderer[] flashRenderers;
    private Color[] baseRendererColors;
    private GameObject eliteRing;
    private Enemy.EnemyType currentType = Enemy.EnemyType.Normal;
    private Color baseColor = Color.white;
    private float baseSmoothness = 0.44f;
    private bool baseGlow;
    private bool usingPrefabView;
    private float hitFlashTimer;

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
            SanitizeVisualInstance(viewInstance);
            ApplySilhouetteScale(visualRoot, currentType);
            ApplyBaseColorsToRenderers(GetVisualRenderers(visualRoot));
            EnsureEliteRing(visualRoot);
            CacheFlashRenderers(GetVisualRenderers(visualRoot));
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
            if (rootRenderer != null)
            {
                rootRenderer.enabled = false;
            }

            ApplyBaseColorsToRenderers(GetVisualRenderers(enhancerRoot));
            CacheFlashRenderers(GetVisualRenderers(enhancerRoot));
            return;
        }

        if (rootRenderer != null)
        {
            rootRenderer.enabled = true;
            GameVisualStyle.ApplyColor(rootRenderer, baseColor, baseSmoothness, baseGlow);
        }
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

        CacheFlashRenderers(renderers);
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

    private void EnsureEliteRing(Transform parent)
    {
        if (parent == null || currentType != Enemy.EnemyType.Elite)
        {
            return;
        }

        if (eliteRing != null)
        {
            Destroy(eliteRing);
        }

        eliteRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        eliteRing.name = "EliteGlowRing";
        eliteRing.transform.SetParent(parent, false);
        eliteRing.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        eliteRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        eliteRing.transform.localScale = new Vector3(0.92f, 0.012f, 0.92f);

        Collider collider = eliteRing.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = eliteRing.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, EliteRingColor, 0.88f, true, 0.55f);
        }
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

    private void ClearControllerVisualRoot()
    {
        eliteRing = null;

        Transform existingRoot = transform.Find(VisualRootName);

        if (existingRoot != null)
        {
            Destroy(existingRoot.gameObject);
        }

        visualRoot = null;
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

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (flashObject != null)
                {
                    flashObject.transform.localScale = Vector3.one * (scale * (0.85f + fade * 0.35f));
                    SetRendererAlpha(flashRenderer, fade);
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
