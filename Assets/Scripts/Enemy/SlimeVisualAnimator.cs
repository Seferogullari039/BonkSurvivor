using UnityEngine;

[DisallowMultipleComponent]
public class SlimeVisualAnimator : MonoBehaviour
{
    private static readonly Color SlimeBodyColor = new Color(0.22f, 0.72f, 0.16f);

    [SerializeField] private float idleAmount = 0.035f;
    [SerializeField] private float idleSpeed = 2.2f;
    [SerializeField] private float jumpHeight = 0.18f;
    [SerializeField] private float jumpInterval = 2.2f;
    [SerializeField] private float jumpDuration = 0.42f;
    [SerializeField] private float dashDistance = 0.12f;
    [SerializeField] private float dashInterval = 3.8f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float animationSpeed = 1f;

    private Transform animatedModel;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
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

        if (animatedModel == null)
        {
            enabled = false;
            return;
        }

        float time = Time.time * animationSpeed + phaseOffset;

        float breath = Mathf.Sin(time * idleSpeed) * idleAmount;
        Vector3 animatedScale = baseLocalScale;
        animatedScale.x += breath;
        animatedScale.z += breath;
        animatedScale.y -= breath * 0.5f;

        float jumpPhase = GetCyclePhase(time, jumpInterval, jumpDuration);
        float dashPhase = GetCyclePhase(time + jumpInterval * 0.35f, dashInterval, dashDuration);

        Vector3 animatedPosition = baseLocalPosition;

        if (jumpPhase >= 0f)
        {
            float hop = Mathf.Sin(jumpPhase * Mathf.PI);
            animatedPosition.y += hop * jumpHeight;

            float edgeSquash = (1f - hop) * 0.08f;
            animatedScale.x += edgeSquash;
            animatedScale.z += edgeSquash;
            animatedScale.y -= edgeSquash * 1.2f;
            animatedScale.y += hop * 0.06f;
        }

        if (dashPhase >= 0f)
        {
            animatedPosition.z += Mathf.Sin(dashPhase * Mathf.PI) * dashDistance;
        }

        animatedModel.localPosition = animatedPosition;
        animatedModel.localScale = animatedScale;
        ApplySlimeMaterialColor();
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

        baseLocalPosition = animatedModel.localPosition;
        baseLocalScale = animatedModel.localScale;
        baseLocalRotation = animatedModel.localRotation;
        slimeRenderers = animatedModel.GetComponentsInChildren<Renderer>(true);
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
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

    private void ApplySlimeMaterialColor()
    {
        if (slimeRenderers == null || slimeRenderers.Length == 0)
        {
            return;
        }

        if (slimeColorBlock == null)
        {
            slimeColorBlock = new MaterialPropertyBlock();
        }

        for (int i = 0; i < slimeRenderers.Length; i++)
        {
            Renderer renderer = slimeRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(slimeColorBlock);
            slimeColorBlock.SetColor("_BaseColor", SlimeBodyColor);
            slimeColorBlock.SetColor("_Color", SlimeBodyColor);
            slimeColorBlock.SetFloat("_Smoothness", 0.55f);
            slimeColorBlock.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(slimeColorBlock);
        }
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
}
