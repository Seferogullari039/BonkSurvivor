using UnityEngine;

public class ChestVisualAnimator : MonoBehaviour
{
    [SerializeField] private Transform animatedRoot;
    [SerializeField] private Renderer glowRenderer;
    [SerializeField] private Light glowLight;
    [SerializeField] private bool enableAnimation = true;

    private Vector3 baseLocalPosition;
    private float phaseOffset;
    private float bobAmplitude = 0.05f;
    private float bobSpeed = 1.6f;
    private float glowPulseSpeed = 1.8f;
    private float glowBaseIntensity = 0.42f;
    private float glowPulseAmount = 0.18f;
    private bool bossBoost;

    private void Awake()
    {
        if (animatedRoot == null)
        {
            Transform visualRoot = transform.Find("ChestVisualRoot");
            animatedRoot = visualRoot != null ? visualRoot : transform;
        }

        baseLocalPosition = animatedRoot.localPosition;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        if (glowRenderer == null)
        {
            Transform glowTransform = animatedRoot.Find("ChestGlow");
            if (glowTransform == null)
            {
                glowTransform = animatedRoot.Find("Glow");
            }

            if (glowTransform != null)
            {
                glowRenderer = glowTransform.GetComponent<Renderer>();
            }
        }

        if (glowLight == null)
        {
            glowLight = animatedRoot.GetComponentInChildren<Light>();
        }
    }

    public void Configure(ChestRarity rarity, bool bossPresentation)
    {
        bossBoost = bossPresentation;

        switch (rarity)
        {
            case ChestRarity.Rare:
                bobAmplitude = 0.05f;
                bobSpeed = 1.7f;
                glowBaseIntensity = 0.22f;
                glowPulseAmount = 0.10f;
                break;
            case ChestRarity.Epic:
                bobAmplitude = 0.06f;
                bobSpeed = 1.9f;
                glowBaseIntensity = 0.28f;
                glowPulseAmount = 0.12f;
                break;
            default:
                bobAmplitude = 0.04f;
                bobSpeed = 1.5f;
                glowBaseIntensity = 0.14f;
                glowPulseAmount = 0.08f;
                break;
        }

        if (bossBoost)
        {
            bobAmplitude *= 1.15f;
            glowBaseIntensity *= 1.25f;
            glowPulseAmount *= 1.2f;
        }
    }

    public void SetIdleEnabled(bool enabled)
    {
        enableAnimation = enabled;

        if (enabled && animatedRoot != null)
        {
            animatedRoot.localPosition = baseLocalPosition;
        }
    }

    private void Update()
    {
        if (!enableAnimation) return;

        float time = Time.time + phaseOffset;

        if (animatedRoot != null)
        {
            float bobOffset = Mathf.Sin(time * bobSpeed) * bobAmplitude;
            animatedRoot.localPosition = baseLocalPosition + new Vector3(0f, bobOffset, 0f);
        }

        float pulse = glowBaseIntensity + Mathf.Sin(time * glowPulseSpeed) * glowPulseAmount;

        if (glowRenderer != null && glowRenderer.material != null)
        {
            Color glowColor = glowRenderer.material.HasProperty("_EmissionColor")
                ? glowRenderer.material.GetColor("_EmissionColor")
                : glowRenderer.material.color;
            glowColor.a = 1f;
            glowRenderer.material.SetColor("_EmissionColor", glowColor * pulse);
        }

        if (glowLight != null)
        {
            glowLight.intensity = pulse * (bossBoost ? 1.35f : 1f);
        }
    }
}
