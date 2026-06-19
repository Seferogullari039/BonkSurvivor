using UnityEngine;

[DisallowMultipleComponent]
public class SlimeVisualAnimator : MonoBehaviour
{
    private const float JumpForwardFactor = 0.22f;

    private static readonly Color SlimeBodyColor = new Color(0.22f, 0.72f, 0.16f);
    private static readonly Color SlimeEyeWhiteColor = new Color(0.95f, 0.94f, 0.88f);
    private static readonly Color SlimePupilColor = new Color(0.06f, 0.06f, 0.06f);

    [SerializeField] private float idleAmount = 0.04f;
    [SerializeField] private float idleSpeed = 2.4f;
    [SerializeField] private float jumpHeight = 0.28f;
    [SerializeField] private float jumpInterval = 1.6f;
    [SerializeField] private float jumpDuration = 0.48f;
    [SerializeField] private float dashDistance = 0.35f;
    [SerializeField] private float dashInterval = 2.2f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float animationSpeed = 1f;

    private Transform animatedModel;
    private Transform movementRoot;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
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

        float breath = Mathf.Sin(time * idleSpeed) * idleAmount;
        Vector3 animatedScale = baseLocalScale;
        animatedScale.x += breath;
        animatedScale.z += breath;
        animatedScale.y -= breath * 0.5f;

        Vector3 animatedPosition = baseLocalPosition;
        Vector3 localMoveDirection = GetLocalMoveDirection();

        float dashPhase = GetCyclePhase(time + jumpInterval * 0.35f, dashInterval, dashDuration);
        float jumpPhase = GetCyclePhase(time, jumpInterval, jumpDuration);

        if (dashPhase >= 0f)
        {
            float dashCurve = EvaluateEaseInOut(dashPhase);
            animatedPosition += localMoveDirection * (dashCurve * dashDistance);
            animatedScale.z += dashCurve * 0.08f;
            animatedScale.x -= dashCurve * 0.03f;
            animatedScale.y -= dashCurve * 0.02f;
        }
        else if (jumpPhase >= 0f)
        {
            float hop = Mathf.Sin(jumpPhase * Mathf.PI);
            animatedPosition.y += hop * jumpHeight;
            animatedPosition += localMoveDirection * (hop * jumpHeight * JumpForwardFactor);

            float edgeSquash = (1f - hop) * 0.1f;
            animatedScale.x += edgeSquash;
            animatedScale.z += edgeSquash;
            animatedScale.y -= edgeSquash * 1.25f;
            animatedScale.y += hop * 0.08f;
        }

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
        baseLocalRotation = animatedModel.localRotation;
        slimeRenderers = animatedModel.GetComponentsInChildren<Renderer>(true);
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        if (movementRoot != null)
        {
            lastMovementRootPosition = movementRoot.position;
            hasMovementSample = true;
        }

        initialized = true;
        return true;
    }

    private void RestoreBaseTransform()
    {
        if (animatedModel == null)
        {
            return;
        }

        animatedModel.localPosition = baseLocalPosition;
        animatedModel.localScale = baseLocalScale;
        animatedModel.localRotation = baseLocalRotation;
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

    private Vector3 GetLocalMoveDirection()
    {
        Vector3 worldDirection = cachedMoveDirection;

        if (!hasMovementSample || worldDirection.sqrMagnitude < 0.0001f)
        {
            worldDirection = animatedModel.parent != null
                ? animatedModel.parent.forward
                : animatedModel.forward;
            worldDirection.y = 0f;
        }

        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        worldDirection.Normalize();

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

            if (renderer == null)
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
