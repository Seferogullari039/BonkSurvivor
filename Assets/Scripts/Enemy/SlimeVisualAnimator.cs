using UnityEngine;

[DisallowMultipleComponent]
public class SlimeVisualAnimator : MonoBehaviour
{
    private const float MinScaleMultiplier = 0.90f;
    private const float MaxScaleMultiplier = 1.10f;
    private const float MaxVerticalOffset = 0.28f;

    private static readonly Color SlimeBodyColor = new Color(0.22f, 0.72f, 0.16f);
    private static readonly Color SlimeEyeWhiteColor = new Color(0.95f, 0.94f, 0.88f);
    private static readonly Color SlimePupilColor = new Color(0.06f, 0.06f, 0.06f);

    [SerializeField] private bool enableDash = false;
    [SerializeField] private float idleAmount = 0.025f;
    [SerializeField] private float idleSpeed = 2.0f;
    [SerializeField] private float jumpHeight = 0.22f;
    [SerializeField] private float jumpInterval = 0.55f;
    [SerializeField] private float jumpDuration = 0.38f;
    [SerializeField] private float jumpForwardArc = 0f;
    [SerializeField] private float dashDistance = 0f;
    [SerializeField] private float dashInterval = 999f;
    [SerializeField] private float dashDuration = 0f;
    [SerializeField] private float animationSpeed = 1f;

    private Transform motionRoot;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Renderer[] slimeRenderers;
    private MaterialPropertyBlock slimeColorBlock;
    private float phaseOffset;
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!initialized && !TryInitialize())
        {
            return;
        }

        if (motionRoot == null)
        {
            enabled = false;
            return;
        }

        float time = Time.time * animationSpeed + phaseOffset;

        float breath = Mathf.Sin(time * idleSpeed) * idleAmount;
        Vector3 scaleMultiplier = new Vector3(
            1f + breath,
            1f - breath * 0.5f,
            1f + breath);

        float hop = EvaluateContinuousHop(time);
        Vector3 animatedPosition = baseLocalPosition;
        animatedPosition.y += hop * jumpHeight;

        float edgeSquash = (1f - hop) * 0.06f;
        scaleMultiplier.x += edgeSquash;
        scaleMultiplier.z += edgeSquash;
        scaleMultiplier.y -= edgeSquash * 0.9f;
        scaleMultiplier.y += hop * 0.04f;

        if (enableDash && dashDistance > 0.001f && dashDuration > 0.001f && dashInterval < 900f)
        {
            float dashPhase = GetCyclePhase(time + jumpInterval * 0.35f, dashInterval, dashDuration);

            if (dashPhase >= 0f)
            {
                float dashCurve = EvaluateEaseInOut(dashPhase);
                animatedPosition.y += dashCurve * 0.02f;
                scaleMultiplier.z += dashCurve * 0.02f;
                scaleMultiplier.y -= dashCurve * 0.01f;
            }
        }

        animatedPosition = ClampAnimatedPosition(animatedPosition);
        Vector3 animatedScale = ClampAnimatedScale(Vector3.Scale(baseLocalScale, scaleMultiplier));

        motionRoot.localPosition = animatedPosition;
        motionRoot.localScale = animatedScale;
        ApplySlimeMaterialColors();
    }

    private void OnDisable()
    {
        RestoreBaseTransform();
    }

    private void OnDestroy()
    {
        RestoreBaseTransform();
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return motionRoot != null;
        }

        Transform visualRoot = ResolveVisualRoot();

        if (visualRoot == null)
        {
            enabled = false;
            return false;
        }

        motionRoot = ResolveMotionRoot(visualRoot);

        if (motionRoot == null || !motionRoot.gameObject.activeInHierarchy)
        {
            enabled = false;
            return false;
        }

        baseLocalPosition = motionRoot.localPosition;
        baseLocalScale = motionRoot.localScale;
        CacheSlimeRenderers();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        initialized = true;
        return true;
    }

    private static Transform ResolveMotionRoot(Transform visualRoot)
    {
        Transform motion = visualRoot.Find("SlimeMotionRoot");

        if (motion != null)
        {
            return motion;
        }

        Transform model = visualRoot.Find("Model");

        if (model != null && model.gameObject.activeInHierarchy)
        {
            return model;
        }

        for (int i = 0; i < visualRoot.childCount; i++)
        {
            Transform child = visualRoot.GetChild(i);

            if (child == null || !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (child.name == "Model" || child.name.Contains("slime") || child.name.Contains("Slime"))
            {
                return child;
            }
        }

        return null;
    }

    private void CacheSlimeRenderers()
    {
        Renderer[] allRenderers = motionRoot.GetComponentsInChildren<Renderer>(true);
        int count = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null && !allRenderers[i].name.Contains("Overlay"))
            {
                count++;
            }
        }

        if (count == 0)
        {
            slimeRenderers = allRenderers;
            return;
        }

        slimeRenderers = new Renderer[count];
        int writeIndex = 0;

        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];

            if (renderer == null || renderer.name.Contains("Overlay"))
            {
                continue;
            }

            slimeRenderers[writeIndex++] = renderer;
        }
    }

    private void RestoreBaseTransform()
    {
        if (motionRoot == null)
        {
            return;
        }

        motionRoot.localPosition = baseLocalPosition;
        motionRoot.localScale = baseLocalScale;
    }

    private Vector3 ClampAnimatedPosition(Vector3 animatedPosition)
    {
        float yOffset = animatedPosition.y - baseLocalPosition.y;
        yOffset = Mathf.Clamp(yOffset, 0f, MaxVerticalOffset);
        return baseLocalPosition + new Vector3(0f, yOffset, 0f);
    }

    private Vector3 ClampAnimatedScale(Vector3 animatedScale)
    {
        animatedScale.x = ClampAxisScale(animatedScale.x, baseLocalScale.x);
        animatedScale.y = ClampAxisScale(animatedScale.y, baseLocalScale.y);
        animatedScale.z = ClampAxisScale(animatedScale.z, baseLocalScale.z);
        return animatedScale;
    }

    private static float ClampAxisScale(float value, float baseAxis)
    {
        float safeBase = Mathf.Max(Mathf.Abs(baseAxis), 0.001f);
        float multiplier = value / safeBase;
        multiplier = Mathf.Clamp(multiplier, MinScaleMultiplier, MaxScaleMultiplier);
        return safeBase * multiplier;
    }

    private float EvaluateContinuousHop(float time)
    {
        if (jumpInterval <= 0.001f)
        {
            return 0f;
        }

        float cycleTime = time % jumpInterval;
        float activeDuration = Mathf.Min(jumpDuration, jumpInterval);

        if (activeDuration <= 0.001f)
        {
            return 0f;
        }

        if (cycleTime > activeDuration)
        {
            return 0f;
        }

        float phase = cycleTime / activeDuration;
        return Mathf.Sin(phase * Mathf.PI);
    }

    private void ApplySlimeMaterialColors()
    {
        if (slimeRenderers == null || slimeRenderers.Length == 0)
        {
            return;
        }

        if (slimeColorBlock == null)
        {
            slimeColorBlock = new MaterialPropertyBlock();
        }

        for (int rendererIndex = 0; rendererIndex < slimeRenderers.Length; rendererIndex++)
        {
            Renderer renderer = slimeRenderers[rendererIndex];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;

            if (sharedMaterials == null || sharedMaterials.Length == 0)
            {
                ApplyMaterialSlotColor(renderer, 0, SlimeBodyColor, 0.55f);
                continue;
            }

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                ResolveSlotVisual(sharedMaterials[materialIndex], materialIndex, out Color color, out float smoothness);
                ApplyMaterialSlotColor(renderer, materialIndex, color, smoothness);
            }
        }
    }

    private static void ResolveSlotVisual(Material material, int materialIndex, out Color color, out float smoothness)
    {
        if (material != null)
        {
            string materialName = material.name;

            if (materialName.Contains("Pupil"))
            {
                color = SlimePupilColor;
                smoothness = 0.25f;
                return;
            }

            if (materialName.Contains("EyeWhite") || materialName.Contains("Eye"))
            {
                color = SlimeEyeWhiteColor;
                smoothness = 0.35f;
                return;
            }

            if (materialName.Contains("Body"))
            {
                color = SlimeBodyColor;
                smoothness = 0.55f;
                return;
            }
        }

        switch (materialIndex)
        {
            case 1:
                color = SlimeEyeWhiteColor;
                smoothness = 0.35f;
                return;
            case 2:
                color = SlimePupilColor;
                smoothness = 0.25f;
                return;
            default:
                color = SlimeBodyColor;
                smoothness = 0.55f;
                return;
        }
    }

    private void ApplyMaterialSlotColor(Renderer renderer, int materialIndex, Color color, float smoothness)
    {
        renderer.GetPropertyBlock(slimeColorBlock, materialIndex);
        slimeColorBlock.SetColor("_BaseColor", color);
        slimeColorBlock.SetColor("_Color", color);
        slimeColorBlock.SetFloat("_Smoothness", smoothness);
        slimeColorBlock.SetColor("_EmissionColor", Color.black);
        renderer.SetPropertyBlock(slimeColorBlock, materialIndex);
    }

    private Transform ResolveVisualRoot()
    {
        if (transform.name == "VisualRoot")
        {
            return transform;
        }

        Transform found = transform.Find("VisualRoot");

        if (found != null)
        {
            return found;
        }

        Transform parent = transform.parent;

        while (parent != null)
        {
            if (parent.name == "VisualRoot")
            {
                return parent;
            }

            parent = parent.parent;
        }

        return null;
    }

    private static float GetCyclePhase(float time, float interval, float duration)
    {
        if (interval <= 0.001f || duration <= 0.001f)
        {
            return -1f;
        }

        float cycleTime = time % interval;

        if (cycleTime > duration)
        {
            return -1f;
        }

        return cycleTime / duration;
    }

    private static float EvaluateEaseInOut(float phase)
    {
        return phase * phase * (3f - 2f * phase);
    }
}
