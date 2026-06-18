using System.Collections;
using UnityEngine;

public static class ChestOpenVisualEffect
{
    private static readonly Color CommonPulse = new Color(0.88f, 0.89f, 0.92f, 0.28f);
    private static readonly Color RarePulse = new Color(0.40f, 0.68f, 1f, 0.32f);
    private static readonly Color EpicPulse = new Color(0.78f, 0.40f, 1f, 0.34f);
    private static readonly Color LegendaryPulse = new Color(1f, 0.82f, 0.28f, 0.36f);

    public static IEnumerator PlayRoutine(Vector3 position, ChestRarity rarity = ChestRarity.Normal)
    {
        Vector3 effectPosition = position + Vector3.up * 0.55f;
        const float duration = 0.28f;

        Color pulseColor = GetPulseColor(rarity);
        Color ringColor = GetRingColor(rarity);

        GameObject pulse = CreatePulseSphere(effectPosition, 0.10f, pulseColor);
        GameObject ring = CreatePulseRing(effectPosition, 0.16f, ringColor);

        Renderer pulseRenderer = pulse != null ? pulse.GetComponent<Renderer>() : null;
        Material pulseMaterial = pulseRenderer != null ? pulseRenderer.material : null;
        Renderer ringRenderer = ring != null ? ring.GetComponent<Renderer>() : null;
        Material ringMaterial = ringRenderer != null ? ringRenderer.material : null;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float fade = 1f - progress;

            if (pulse != null)
            {
                float size = Mathf.Lerp(0.10f, 0.42f, progress);
                pulse.transform.localScale = Vector3.one * size;
            }

            SetRingScale(ring, Mathf.Lerp(0.16f, 0.62f, progress));

            FadeEmission(pulseMaterial, 0.22f * fade);
            FadeEmission(ringMaterial, 0.18f * fade);
            SetBaseAlpha(pulseMaterial, fade * pulseColor.a);
            SetBaseAlpha(ringMaterial, fade * ringColor.a * 0.75f);

            yield return null;
        }

        DestroyEffect(pulse);
        DestroyEffect(ring);
    }

    public static Color GetLegendaryPulseColor()
    {
        return LegendaryPulse;
    }

    private static Color GetPulseColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => RarePulse,
            ChestRarity.Epic => EpicPulse,
            _ => CommonPulse
        };
    }

    private static Color GetRingColor(ChestRarity rarity)
    {
        return rarity switch
        {
            ChestRarity.Rare => new Color(0.32f, 0.58f, 0.95f, 0.22f),
            ChestRarity.Epic => new Color(0.62f, 0.28f, 0.92f, 0.24f),
            _ => new Color(0.82f, 0.84f, 0.88f, 0.18f)
        };
    }

    private static GameObject CreatePulseSphere(Vector3 position, float size, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "ChestOpenPulse";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * size;
        RemoveCollider(sphere);
        ApplySoftEffectColor(sphere.GetComponent<Renderer>(), color);
        return sphere;
    }

    private static GameObject CreatePulseRing(Vector3 position, float radius, Color color)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "ChestOpenRing";
        ring.transform.position = position;
        SetRingScale(ring, radius);
        RemoveCollider(ring);
        ApplySoftEffectColor(ring.GetComponent<Renderer>(), color);
        return ring;
    }

    private static void ApplySoftEffectColor(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        GameVisualStyle.ApplyColor(renderer, color, 0.18f, true, 0.22f);
    }

    private static void SetRingScale(GameObject ring, float radius)
    {
        if (ring == null)
        {
            return;
        }

        ring.transform.localScale = new Vector3(radius, 0.025f, radius);
    }

    private static void FadeEmission(Material material, float intensity)
    {
        if (material == null || !material.HasProperty("_EmissionColor"))
        {
            return;
        }

        Color emission = material.GetColor("_EmissionColor");
        float maxChannel = Mathf.Max(emission.r, Mathf.Max(emission.g, emission.b));

        if (maxChannel <= 0.001f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white * intensity);
            return;
        }

        material.SetColor("_EmissionColor", emission * (intensity / maxChannel));
    }

    private static void SetBaseAlpha(Material material, float alpha)
    {
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
    }

    private static void RemoveCollider(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }

    private static void DestroyEffect(GameObject target)
    {
        if (target != null)
        {
            Object.Destroy(target);
        }
    }
}
