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

    private const float RouletteDuration = 0.82f;
    private const float SettleDuration = 0.14f;
    private const float LockDuration = 0.30f;
    private const float FadeDuration = 0.06f;
    private const int RouletteCycles = 3;

    private const float InnerScaleMin = 0.16f;
    private const float InnerScaleMax = 0.58f;
    private const float RingRadiusMin = 0.24f;
    private const float RingRadiusMax = 0.88f;

    public static IEnumerator PlayRoutine(Vector3 position, ChestRarity rarity = ChestRarity.Normal)
    {
        yield return PlayRoutineCore(position, GetPulseColor(rarity), GetRingColor(rarity), false);
    }

    public static IEnumerator PlayRoutineForUpgradeReward(Vector3 position, UpgradeRarity rewardRarity, Transform chestTransform = null)
    {
        Vector3 anchor = ChestOpeningPresentation.GetMouthWorldPosition(chestTransform, position);
        bool mouthAnchored = ChestOpeningPresentation.UsesMouthAnchor(chestTransform);
        yield return PlayRoutineCore(
            anchor,
            GetUpgradePulseColor(rewardRarity),
            GetUpgradeRingColor(rewardRarity),
            mouthAnchored);
    }

    private static IEnumerator PlayRoutineCore(Vector3 position, Color finalPulse, Color finalRing, bool mouthAnchored = false)
    {
        float innerLift = mouthAnchored ? 0.04f : 0.42f;
        float ringLift = mouthAnchored ? 0.10f : 0.48f;
        Vector3 innerPosition = position + Vector3.up * innerLift;
        Vector3 ringPosition = position + Vector3.up * ringLift;

        GameObject innerGlow = CreatePulseSphere(innerPosition, InnerScaleMin, CommonPulse);
        GameObject spark = CreatePulseSphere(innerPosition + Vector3.up * 0.04f, InnerScaleMin * 0.65f, CommonPulse);
        GameObject ring = CreatePulseRing(ringPosition, RingRadiusMin, GetRingColorForPalette(0));

        Renderer innerRenderer = innerGlow != null ? innerGlow.GetComponent<Renderer>() : null;
        Material innerMaterial = innerRenderer != null ? innerRenderer.material : null;
        Renderer sparkRenderer = spark != null ? spark.GetComponent<Renderer>() : null;
        Material sparkMaterial = sparkRenderer != null ? sparkRenderer.material : null;
        Renderer ringRenderer = ring != null ? ring.GetComponent<Renderer>() : null;
        Material ringMaterial = ringRenderer != null ? ringRenderer.material : null;

        Light revealLight = CreateRevealLight(innerPosition, mouthAnchored ? 1.35f : 1.1f);
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

            float breathe = InnerScaleMin + Mathf.Sin(rouletteProgress * RouletteCycles * Mathf.PI * 2f) * 0.04f;
            float ringRadius = RingRadiusMin + rouletteProgress * (RingRadiusMax * 0.72f - RingRadiusMin);
            float emission = 0.14f + rouletteProgress * 0.08f;
            float alpha = pulseColor.a;

            ApplyInnerGlow(innerGlow, breathe);
            ApplyInnerGlow(spark, breathe * 0.65f);
            SetRingScale(ring, ringRadius);
            UpdateEffectMaterial(innerMaterial, pulseColor, emission, alpha);
            UpdateEffectMaterial(sparkMaterial, pulseColor, emission * 0.85f, alpha * 0.75f);
            UpdateEffectMaterial(ringMaterial, ringColor, emission * 0.7f, ringColor.a);
            UpdateRevealLight(revealLight, pulseColor, 0.10f + rouletteProgress * 0.12f);

            yield return null;
        }

        while (elapsed < RouletteDuration + SettleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float settleProgress = Mathf.Clamp01((elapsed - RouletteDuration) / SettleDuration);
            float settleEase = settleProgress * settleProgress * (3f - 2f * settleProgress);

            Color pulseColor = Color.Lerp(lastRouletteColor, finalPulse, settleEase);
            Color ringColor = Color.Lerp(GetRingColorForPalette(GetRoulettePaletteIndex(1f)), finalRing, settleEase);

            float innerScale = Mathf.Lerp(InnerScaleMin + 0.06f, InnerScaleMax, settleEase);
            float ringRadius = Mathf.Lerp(RingRadiusMax * 0.72f, RingRadiusMax, settleEase);
            float emission = Mathf.Lerp(0.22f, 0.28f, settleEase);
            float alpha = Mathf.Lerp(pulseColor.a, finalPulse.a, settleEase);

            ApplyInnerGlow(innerGlow, innerScale);
            ApplyInnerGlow(spark, innerScale * 0.55f);
            SetRingScale(ring, ringRadius);
            UpdateEffectMaterial(innerMaterial, pulseColor, emission, alpha);
            UpdateEffectMaterial(sparkMaterial, pulseColor, emission * 0.9f, alpha * 0.8f);
            UpdateEffectMaterial(ringMaterial, ringColor, emission * 0.75f, ringColor.a);
            UpdateRevealLight(revealLight, pulseColor, Mathf.Lerp(0.18f, 0.24f, settleEase));

            yield return null;
        }

        float lockElapsed = 0f;

        while (lockElapsed < LockDuration)
        {
            lockElapsed += Time.unscaledDeltaTime;

            ApplyInnerGlow(innerGlow, InnerScaleMax);
            ApplyInnerGlow(spark, InnerScaleMax * 0.55f);
            SetRingScale(ring, RingRadiusMax);
            UpdateEffectMaterial(innerMaterial, finalPulse, 0.28f, finalPulse.a);
            UpdateEffectMaterial(sparkMaterial, finalPulse, 0.24f, finalPulse.a * 0.8f);
            UpdateEffectMaterial(ringMaterial, finalRing, 0.22f, finalRing.a);
            UpdateRevealLight(revealLight, finalPulse, 0.22f);

            yield return null;
        }

        float fadeElapsed = 0f;

        while (fadeElapsed < FadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(fadeElapsed / FadeDuration);

            UpdateEffectMaterial(innerMaterial, finalPulse, 0.28f * fade, finalPulse.a * fade);
            UpdateEffectMaterial(sparkMaterial, finalPulse, 0.24f * fade, finalPulse.a * fade * 0.8f);
            UpdateEffectMaterial(ringMaterial, finalRing, 0.22f * fade, finalRing.a * fade);
            UpdateRevealLight(revealLight, finalPulse, 0.14f * fade);

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

    private static Color GetUpgradePulseColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => RarePulse,
            UpgradeRarity.Epic => EpicPulse,
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

    private static Color GetUpgradeRingColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => new Color(0.32f, 0.58f, 0.95f, 0.22f),
            UpgradeRarity.Epic => new Color(0.62f, 0.28f, 0.92f, 0.24f),
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

    private static Light CreateRevealLight(Vector3 position, float range = 1.1f)
    {
        GameObject lightObject = new GameObject("ChestRevealLight");
        lightObject.transform.position = position + Vector3.up * 0.06f;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = range;
        light.intensity = 0.1f;
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
