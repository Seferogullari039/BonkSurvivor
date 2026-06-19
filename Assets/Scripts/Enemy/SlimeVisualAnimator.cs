using UnityEngine;

[DisallowMultipleComponent]
public class SlimeVisualAnimator : MonoBehaviour
{
    private const float MinScaleMultiplier = 0.75f;
    private const float MaxScaleMultiplier = 1.28f;
    private const float MaxVerticalOffset = 0.75f;
    private const float MaxHorizontalOffset = 0.9f;

    private static readonly Color SlimeBodyColor = new Color(0.22f, 0.72f, 0.16f);
    private static readonly Color SlimeEyeWhiteColor = new Color(0.95f, 0.94f, 0.88f);
    private static readonly Color SlimePupilColor = new Color(0.06f, 0.06f, 0.06f);

    [SerializeField] private float idleAmount = 0.035f;
    [SerializeField] private float idleSpeed = 2.4f;
    [SerializeField] private float jumpHeight = 0.48f;
    [SerializeField] private float jumpInterval = 0.78f;
    [SerializeField] private float jumpDuration = 0.58f;
    [SerializeField] private float jumpForwardArc = 0.16f;
    [SerializeField] private float dashDistance = 0.9f;
    [SerializeField] private float dashInterval = 1.65f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float animationSpeed = 1f;

    private Transform animatedModel;
    private Transform movementRoot;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Renderer[] slimeRenderers;
    private MaterialPropertyBlock slimeColorBlock;
    private Vector3 cachedMoveDirection = Vector3.forward;
    private Vector3 lastMovementRootPosition;
    private float phaseOffset;
    private bool initialized;
    private bool hasMovementSample;

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

        if (animatedModel == null)
        {
            enabled = false;
            return;
        }

        UpdateMovementDirection();

        float time = Time.time * animationSpeed + phaseOffset;
        Vector3 localMoveDirection = GetLocalMoveDirection();

        float breath = Mathf.Sin(time * idleSpeed) * idleAmount;
        Vector3 scaleMultiplier = new Vector3(
            1f + breath,
            1f - breath * 0.5f,
            1f + breath);

        float hop = EvaluateContinuousHop(time);
        Vector3 animatedPosition = baseLocalPosition;
        animatedPosition.y += hop * jumpHeight;
        animatedPosition += localMoveDirection * (hop * jumpForwardArc);

        float edgeSquash = (1f - hop) * 0.08f;
        scaleMultiplier.x += edgeSquash;
        scaleMultiplier.z += edgeSquash;
        scaleMultiplier.y -= edgeSquash * 1.1f;
        scaleMultiplier.y += hop * 0.07f;

        float dashPhase = GetCyclePhase(time + jumpInterval * 0.35f, dashInterval, dashDuration);

        if (dashPhase >= 0f)
        {
            float dashCurve = EvaluateEaseInOut(dashPhase);
            animatedPosition += localMoveDirection * (dashCurve * dashDistance);
            scaleMultiplier.z += dashCurve * 0.06f;
            scaleMultiplier.x -= dashCurve * 0.025f;
            scaleMultiplier.y -= dashCurve * 0.015f;
        }

        animatedPosition = ClampAnimatedPosition(animatedPosition);
        Vector3 animatedScale = ClampAnimatedScale(Vector3.Scale(baseLocalScale, scaleMultiplier));

        animatedModel.localPosition = animatedPosition;
        animatedModel.localScale = animatedScale;
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
            return animatedModel != null;
        }

        Transform visualRoot = ResolveVisualRoot();

        if (visualRoot == null)
        {
            enabled = false;
            return false;
        }

        animatedModel = visualRoot.Find("Model");

        if (animatedModel == null || !animatedModel.gameObject.activeInHierarchy)
        {
            enabled = false;
            return false;
        }

        movementRoot = ResolveMovementRoot(visualRoot);
        baseLocalPosition = animatedModel.localPosition;
        baseLocalScale = animatedModel.localScale;
        CacheSlimeRenderers();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        if (movementRoot != null)
        {
            lastMovementRootPosition = movementRoot.position;
            hasMovementSample = true;
        }

        initialized = true;
        return true;
    }

    private void CacheSlimeRenderers()
    {
        Renderer[] allRenderers = animatedModel.GetComponentsInChildren<Renderer>(true);
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
        if (animatedModel == null)
        {
            return;
        }

        animatedModel.localPosition = baseLocalPosition;
        animatedModel.localScale = baseLocalScale;
    }

    private Vector3 ClampAnimatedPosition(Vector3 animatedPosition)
    {
        Vector3 delta = animatedPosition - baseLocalPosition;
        delta.y = Mathf.Clamp(delta.y, 0f, MaxVerticalOffset);

        Vector3 horizontal = new Vector3(delta.x, 0f, delta.z);

        if (horizontal.sqrMagnitude > MaxHorizontalOffset * MaxHorizontalOffset)
        {
            horizontal = horizontal.normalized * MaxHorizontalOffset;
        }

        return baseLocalPosition + new Vector3(horizontal.x, delta.y, horizontal.z);
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
            float tail = (cycleTime - activeDuration) / Mathf.Max(jumpInterval - activeDuration, 0.001f);
            return Mathf.Max(0f, Mathf.Sin(tail * Mathf.PI * 0.5f) * 0.12f);
        }

        float phase = cycleTime / activeDuration;
        return Mathf.Sin(phase * Mathf.PI);
    }

    private void UpdateMovementDirection()
    {
        if (movementRoot == null)
        {
            return;
        }

        Vector3 worldDelta = movementRoot.position - lastMovementRootPosition;
        lastMovementRootPosition = movementRoot.position;
        worldDelta.y = 0f;

        if (worldDelta.sqrMagnitude > 0.0004f)
        {
            cachedMoveDirection = worldDelta.normalized;
            hasMovementSample = true;
        }
    }

    private Vector3 GetWorldMoveDirection()
    {
        if (hasMovementSample && cachedMoveDirection.sqrMagnitude > 0.0001f)
        {
            return cachedMoveDirection;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null && movementRoot != null)
        {
            Vector3 toPlayer = playerObject.transform.position - movementRoot.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                return toPlayer.normalized;
            }
        }

        Vector3 fallbackForward = animatedModel.parent != null
            ? animatedModel.parent.forward
            : animatedModel.forward;
        fallbackForward.y = 0f;

        if (fallbackForward.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return fallbackForward.normalized;
    }

    private Vector3 GetLocalMoveDirection()
    {
        Vector3 worldDirection = GetWorldMoveDirection();
        Transform space = animatedModel.parent != null ? animatedModel.parent : animatedModel;
        Vector3 localDirection = space.InverseTransformDirection(worldDirection);
        localDirection.y = 0f;

        if (localDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return localDirection.normalized;
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

    private static Transform ResolveMovementRoot(Transform visualRoot)
    {
        Transform current = visualRoot;

        while (current != null)
        {
            if (current.GetComponent<Enemy>() != null)
            {
                return current;
            }

            current = current.parent;
        }

        return visualRoot.root;
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
