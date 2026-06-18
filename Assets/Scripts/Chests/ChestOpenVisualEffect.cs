using System.Collections;
using UnityEngine;

public static class ChestOpenVisualEffect
{
    private static readonly Color CommonPulse = new Color(0.88f, 0.89f, 0.92f, 0.28f);
    private static readonly Color RarePulse = new Color(0.40f, 0.68f, 1f, 0.32f);
    private static readonly Color EpicPulse = new Color(0.78f, 0.40f, 1f, 0.34f);
    private static readonly Color LegendaryPulse = new Color(1f, 0.82f, 0.28f, 0.36f);

    private static readonly Color[] RoulettePalette =
    {
        CommonPulse,
        RarePulse,
        EpicPulse,
        LegendaryPulse,
    };

    private const float TotalDuration = 0.75f;
    private const float SettleDuration = 0.25f;
    private const float RouletteDuration = TotalDuration - SettleDuration;
    private const int RouletteCycles = 3;

    public static IEnumerator PlayRoutine(Vector3 position, ChestRarity rarity = ChestRarity.Normal)
    {
        Vector3 innerPosition = position + Vector3.up * 0.42f;
        Vector3 ringPosition = position + Vector3.up * 0.48f;

        Color finalPulse = GetPulseColor(rarity);
        Color finalRing = GetRingColor(rarity);

        GameObject innerGlow = CreatePulseSphere(innerPosition, 0.08f, CommonPulse);
        GameObject spark = CreatePulseSphere(innerPosition + Vector3.up * 0.04f, 0.05f, CommonPulse);
        GameObject ring = CreatePulseRing(ringPosition, 0.12f, GetRingColorForPalette(0));

        Renderer innerRenderer = innerGlow != null ? innerGlow.GetComponent<Renderer>() : null;
        Material innerMaterial = innerRenderer != null ? innerRenderer.material : null;
        Renderer sparkRenderer = spark != null ? spark.GetComponent<Renderer>() : null;
        Material sparkMaterial = sparkRenderer != null ? sparkRenderer.material : null;
        Renderer ringRenderer = ring != null ? ring.GetComponent<Renderer>() : null;
        Material ringMaterial = ringRenderer != null ? ringRenderer.material : null;

        Light revealLight = CreateRevealLight(innerPosition);
        Color lastRouletteColor = CommonPulse;

        float elapsed = 0f;

        while (elapsed < RouletteDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float rouletteProgress = Mathf.Clamp01(elapsed / RouletteDuration);
            int paletteIndex = GetRoulettePaletteIndex(rouletteProgress);
            Color pulseColor = RoulettePalette[paletteIndex];
            Color ringColor = GetRingColorForPalette(paletteIndex);
            lastRouletteColor = pulseColor;

            float breathe = 0.08f + Mathf.Sin(rouletteProgress * RouletteCycles * Mathf.PI * 2f) * 0.015f;
            float ringRadius = 0.12f + rouletteProgress * 0.06f;
            float emission = 0.16f + rouletteProgress * 0.06f;
            float alpha = pulseColor.a;

            ApplyInnerGlow(innerGlow, breathe);
            ApplyInnerGlow(spark, breathe * 0.65f);
            SetRingScale(ring, ringRadius);
            UpdateEffectMaterial(innerMaterial, pulseColor, emission, alpha);
            UpdateEffectMaterial(sparkMaterial, pulseColor, emission * 0.85f, alpha * 0.75f);
            UpdateEffectMaterial(ringMaterial, ringColor, emission * 0.7f, ringColor.a);
            UpdateRevealLight(revealLight, pulseColor, 0.12f + rouletteProgress * 0.08f);

            yield return null;
        }

        while (elapsed < TotalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float settleProgress = Mathf.Clamp01((elapsed - RouletteDuration) / SettleDuration);
            float settleEase = settleProgress * settleProgress * (3f - 2f * settleProgress);

            Color pulseColor = Color.Lerp(lastRouletteColor, finalPulse, settleEase);
            Color ringColor = Color.Lerp(GetRingColorForPalette(GetRoulettePaletteIndex(1f)), finalRing, settleEase);

            float innerScale = Mathf.Lerp(0.10f, 0.20f, settleEase);
            float ringRadius = Mathf.Lerp(0.18f, 0.28f, settleEase);
            float emission = Mathf.Lerp(0.22f, 0.30f, settleEase);
            float alpha = Mathf.Lerp(pulseColor.a, finalPulse.a, settleEase);

            ApplyInnerGlow(innerGlow, innerScale);
            ApplyInnerGlow(spark, innerScale * 0.55f);
            SetRingScale(ring, ringRadius);
            UpdateEffectMaterial(innerMaterial, pulseColor, emission, alpha);
            UpdateEffectMaterial(sparkMaterial, pulseColor, emission * 0.9f, alpha * 0.8f);
            UpdateEffectMaterial(ringMaterial, ringColor, emission * 0.75f, ringColor.a);
            UpdateRevealLight(revealLight, pulseColor, Mathf.Lerp(0.20f, 0.28f, settleEase));

            yield return null;
        }

        float fadeElapsed = 0f;
        const float fadeDuration = 0.12f;

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(fadeElapsed / fadeDuration);

            UpdateEffectMaterial(innerMaterial, finalPulse, 0.30f * fade, finalPulse.a * fade);
            UpdateEffectMaterial(sparkMaterial, finalPulse, 0.24f * fade, finalPulse.a * fade * 0.8f);
            UpdateEffectMaterial(ringMaterial, finalRing, 0.22f * fade, finalRing.a * fade);
            UpdateRevealLight(revealLight, finalPulse, 0.28f * fade);

            yield return null;
        }

        DestroyEffect(innerGlow);
        DestroyEffect(spark);
        DestroyEffect(ring);
        DestroyEffect(revealLight != null ? revealLight.gameObject : null);
    }

    public static Color GetLegendaryPulseColor()
    {
        return LegendaryPulse;
    }

    private static int GetRoulettePaletteIndex(float rouletteProgress)
    {
        float cycleProgress = rouletteProgress * RouletteCycles;
        int step = Mathf.FloorToInt(cycleProgress * RoulettePalette.Length);
        return step % RoulettePalette.Length;
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

    private static Color GetRingColorForPalette(int paletteIndex)
    {
        return paletteIndex switch
        {
            1 => new Color(0.32f, 0.58f, 0.95f, 0.20f),
            2 => new Color(0.62f, 0.28f, 0.92f, 0.22f),
            3 => new Color(0.95f, 0.78f, 0.22f, 0.22f),
            _ => new Color(0.82f, 0.84f, 0.88f, 0.16f)
        };
    }

    private static Light CreateRevealLight(Vector3 position)
    {
        GameObject lightObject = new GameObject("ChestRevealLight");
        lightObject.transform.position = position + Vector3.up * 0.06f;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 0.85f;
        light.intensity = 0.12f;
        light.shadows = LightShadows.None;
        return light;
    }

    private static void UpdateRevealLight(Light light, Color color, float intensity)
    {
        if (light == null)
        {
            return;
        }

        light.color = new Color(color.r, color.g, color.b, 1f);
        light.intensity = intensity;
    }

    private static void ApplyInnerGlow(GameObject glow, float scale)
    {
        if (glow == null)
        {
            return;
        }

        glow.transform.localScale = Vector3.one * scale;
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

    private static void UpdateEffectMaterial(Material material, Color color, float emissionIntensity, float alpha)
    {
        if (material == null)
        {
            return;
        }

        Color baseColor = color;
        baseColor.a = alpha;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b, 1f) * emissionIntensity);
        }
    }

    private static void SetRingScale(GameObject ring, float radius)
    {
        if (ring == null)
        {
            return;
        }

        ring.transform.localScale = new Vector3(radius, 0.022f, radius);
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
