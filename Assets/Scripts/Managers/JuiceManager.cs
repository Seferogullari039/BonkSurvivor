using System.Collections;
using UnityEngine;

public class JuiceManager : MonoBehaviour
{
    public static JuiceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlayCoinPickup(Vector3 position)
    {
        StartEffect(FlashRoutine(position, new Color(1f, 0.88f, 0.15f), 0.22f, 0.55f, 0.25f));
    }

    public void PlayXPPickup(Vector3 position)
    {
        StartEffect(FlashRoutine(position, new Color(0.2f, 0.82f, 1f), 0.2f, 0.5f, 0.25f));
    }

    public void PlayLevelUp(Vector3 position)
    {
        // FPSScreenShake.Shake(0.035f, 0.18f);
        StartEffect(LevelUpRoutine(position));
    }

    public void PlayChestOpen(Vector3 position)
    {
        // FPSScreenShake.Shake(0.04f, 0.16f);
        StartEffect(ChestOpenRoutine(position));
    }

    public void PlayBossSpawn(Vector3 position)
    {
        // FPSScreenShake.Shake(0.08f, 0.32f);
        StartEffect(BossSpawnRoutine(position));
    }

    public void PlayEliteSpawn(Vector3 position)
    {
        StartEffect(FlashRoutine(position, new Color(1f, 0.86f, 0.12f), 0.35f, 1.1f, 0.4f));
    }

    public void PlayEliteKill(Vector3 position)
    {
        StartEffect(EliteKillRoutine(position));
    }

    public void PlayRocketExplosion(Vector3 position)
    {
        // FPSScreenShake.Shake(0.12f, 0.24f);
        StartEffect(RocketExplosionRoutine(position));
    }

    public void PlayGameOver()
    {
    }

    private void StartEffect(IEnumerator routine)
    {
        if (routine == null) return;

        StartCoroutine(routine);
    }

    private IEnumerator LevelUpRoutine(Vector3 position)
    {
        Vector3 effectPosition = position + Vector3.up * 0.1f;
        const float duration = 0.5f;
        const float startRadius = 0.5f;
        const float endRadius = 3f;

        GameObject ring = CreateRing(effectPosition, startRadius, new Color(1f, 0.92f, 0.35f));
        GameObject innerRing = CreateRing(effectPosition, startRadius * 0.85f, new Color(0.45f, 0.92f, 1f));

        if (ring == null)
        {
            yield break;
        }

        Renderer ringRenderer = ring.GetComponent<Renderer>();
        Material ringMaterial = ringRenderer != null ? ringRenderer.material : null;
        Renderer innerRenderer = innerRing != null ? innerRing.GetComponent<Renderer>() : null;
        Material innerMaterial = innerRenderer != null ? innerRenderer.material : null;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float radius = Mathf.Lerp(startRadius, endRadius, progress);
            float fade = 1f - progress;

            SetRingScale(ring, radius);
            SetRingScale(innerRing, radius * 0.92f);
            FadeEmission(ringMaterial, 0.55f * fade);
            FadeEmission(innerMaterial, 0.45f * fade);

            yield return null;
        }

        DestroyEffect(ring);
        DestroyEffect(innerRing);
    }

    private IEnumerator EliteKillRoutine(Vector3 position)
    {
        Vector3 effectPosition = position + Vector3.up * 0.35f;
        Color goldColor = new Color(1f, 0.82f, 0.12f);
        Color coinColor = new Color(1f, 0.88f, 0.15f);
        Color xpColor = new Color(0.2f, 0.82f, 1f);
        const float duration = 0.55f;

        GameObject flash = CreateSphere(effectPosition, 0.3f, goldColor);
        GameObject ring = CreateRing(effectPosition, 0.5f, goldColor);
        GameObject coinSpark = CreateSphere(effectPosition + new Vector3(-0.22f, 0.08f, 0.12f), 0.12f, coinColor);
        GameObject xpSpark = CreateSphere(effectPosition + new Vector3(0.22f, 0.08f, -0.12f), 0.12f, xpColor);
        GameObject label = CreateEliteDownLabel(effectPosition + Vector3.up * 0.45f);

        Renderer flashRenderer = flash != null ? flash.GetComponent<Renderer>() : null;
        Material flashMaterial = flashRenderer != null ? flashRenderer.material : null;
        Renderer ringRenderer = ring != null ? ring.GetComponent<Renderer>() : null;
        Material ringMaterial = ringRenderer != null ? ringRenderer.material : null;
        Renderer coinRenderer = coinSpark != null ? coinSpark.GetComponent<Renderer>() : null;
        Material coinMaterial = coinRenderer != null ? coinRenderer.material : null;
        Renderer xpRenderer = xpSpark != null ? xpSpark.GetComponent<Renderer>() : null;
        Material xpMaterial = xpRenderer != null ? xpRenderer.material : null;
        TextMesh labelMesh = label != null ? label.GetComponent<TextMesh>() : null;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float fade = 1f - progress;

            if (flash != null)
            {
                flash.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.05f, progress);
            }

            SetRingScale(ring, Mathf.Lerp(0.5f, 2.1f, progress));
            FadeEmission(flashMaterial, 0.8f * fade);
            FadeEmission(ringMaterial, 0.65f * fade);
            SetBaseAlpha(flashMaterial, fade);
            SetBaseAlpha(ringMaterial, fade * 0.85f);

            if (coinSpark != null)
            {
                coinSpark.transform.localScale = Vector3.one * (0.12f + progress * 0.08f);
            }

            if (xpSpark != null)
            {
                xpSpark.transform.localScale = Vector3.one * (0.12f + progress * 0.08f);
            }

            FadeEmission(coinMaterial, 0.55f * fade);
            FadeEmission(xpMaterial, 0.55f * fade);
            SetBaseAlpha(coinMaterial, fade);
            SetBaseAlpha(xpMaterial, fade);

            if (label != null)
            {
                label.transform.position = effectPosition + Vector3.up * (0.45f + progress * 0.35f);
                FaceWorldLabel(label.transform);

                if (labelMesh != null)
                {
                    Color labelColor = labelMesh.color;
                    labelColor.a = fade;
                    labelMesh.color = labelColor;
                }
            }

            yield return null;
        }

        DestroyEffect(flash);
        DestroyEffect(ring);
        DestroyEffect(coinSpark);
        DestroyEffect(xpSpark);
        DestroyEffect(label);
    }

    private static GameObject CreateEliteDownLabel(Vector3 position)
    {
        GameObject labelObject = new GameObject("EliteDownLabel");
        labelObject.transform.position = position;

        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = "ELITE DOWN";
        textMesh.fontSize = 32;
        textMesh.characterSize = 0.06f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(1f, 0.86f, 0.2f, 1f);
        textMesh.fontStyle = FontStyle.Bold;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font != null)
        {
            textMesh.font = font;
        }

        FaceWorldLabel(labelObject.transform);
        return labelObject;
    }

    private static void FaceWorldLabel(Transform labelTransform)
    {
        if (labelTransform == null) return;

        Camera camera = Camera.main;

        if (camera == null) return;

        labelTransform.rotation = Quaternion.LookRotation(
            labelTransform.position - camera.transform.position,
            Vector3.up);
    }

    private IEnumerator FlashRoutine(Vector3 position, Color color, float startSize, float endSize, float duration)
    {
        Vector3 effectPosition = position + Vector3.up * 0.18f;
        GameObject flash = CreateSphere(effectPosition, startSize, color);

        if (flash == null)
        {
            yield break;
        }

        Renderer renderer = flash.GetComponent<Renderer>();
        Material material = renderer != null ? renderer.material : null;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float size = Mathf.Lerp(startSize, endSize, progress);
            float fade = 1f - progress;

            flash.transform.localScale = Vector3.one * size;
            FadeEmission(material, 0.5f * fade);
            SetBaseAlpha(material, fade);

            yield return null;
        }

        DestroyEffect(flash);
    }

    private IEnumerator ChestOpenRoutine(Vector3 position)
    {
        Vector3 effectPosition = position + Vector3.up * 0.25f;
        const float duration = 0.5f;

        GameObject yellowBurst = CreateSphere(effectPosition, 0.25f, new Color(1f, 0.9f, 0.2f));
        GameObject goldPulse = CreateRing(effectPosition, 0.35f, new Color(1f, 0.82f, 0.15f));
        GameObject purpleBurst = CreateSphere(effectPosition, 0.18f, new Color(0.62f, 0.2f, 0.95f));

        Renderer yellowRenderer = yellowBurst != null ? yellowBurst.GetComponent<Renderer>() : null;
        Material yellowMaterial = yellowRenderer != null ? yellowRenderer.material : null;
        Renderer goldRenderer = goldPulse != null ? goldPulse.GetComponent<Renderer>() : null;
        Material goldMaterial = goldRenderer != null ? goldRenderer.material : null;
        Renderer purpleRenderer = purpleBurst != null ? purpleBurst.GetComponent<Renderer>() : null;
        Material purpleMaterial = purpleRenderer != null ? purpleRenderer.material : null;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float fade = 1f - progress;

            if (yellowBurst != null)
            {
                yellowBurst.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.35f, progress);
            }

            SetRingScale(goldPulse, Mathf.Lerp(0.35f, 1.5f, progress));

            if (purpleBurst != null)
            {
                purpleBurst.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.95f, progress);
            }

            FadeEmission(yellowMaterial, 0.6f * fade);
            FadeEmission(goldMaterial, 0.55f * fade);
            FadeEmission(purpleMaterial, 0.5f * fade);
            SetBaseAlpha(yellowMaterial, fade);
            SetBaseAlpha(goldMaterial, fade * 0.85f);
            SetBaseAlpha(purpleMaterial, fade);

            yield return null;
        }

        DestroyEffect(yellowBurst);
        DestroyEffect(goldPulse);
        DestroyEffect(purpleBurst);
    }

    private IEnumerator BossSpawnRoutine(Vector3 position)
    {
        Vector3 effectPosition = position + Vector3.up * 0.12f;
        const float duration = 0.55f;
        Color bossColor = new Color(0.62f, 0.08f, 0.72f);
        Color glowColor = new Color(0.85f, 0.25f, 1f);

        GameObject ring = CreateRing(effectPosition, 0.8f, bossColor);
        GameObject glow = CreateSphere(effectPosition, 0.55f, glowColor);

        Renderer ringRenderer = ring != null ? ring.GetComponent<Renderer>() : null;
        Material ringMaterial = ringRenderer != null ? ringRenderer.material : null;
        Renderer glowRenderer = glow != null ? glow.GetComponent<Renderer>() : null;
        Material glowMaterial = glowRenderer != null ? glowRenderer.material : null;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float fade = 1f - progress;

            SetRingScale(ring, Mathf.Lerp(0.8f, 3.2f, progress));

            if (glow != null)
            {
                glow.transform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.8f, progress);
            }

            FadeEmission(ringMaterial, 0.6f * fade);
            FadeEmission(glowMaterial, 0.55f * fade);
            SetBaseAlpha(ringMaterial, fade * 0.85f);
            SetBaseAlpha(glowMaterial, fade);

            yield return null;
        }

        DestroyEffect(ring);
        DestroyEffect(glow);
    }

    private IEnumerator RocketExplosionRoutine(Vector3 position)
    {
        Vector3 effectPosition = position + Vector3.up * 0.15f;
        const float duration = 0.5f;
        Color fireColor = new Color(1f, 0.45f, 0.1f);
        Color shockColor = new Color(1f, 0.72f, 0.25f, 0.75f);

        GameObject core = CreateSphere(effectPosition, 0.35f, fireColor);
        GameObject shockwave = CreateRing(effectPosition, 0.45f, shockColor);

        Renderer coreRenderer = core != null ? core.GetComponent<Renderer>() : null;
        Material coreMaterial = coreRenderer != null ? coreRenderer.material : null;
        Renderer shockRenderer = shockwave != null ? shockwave.GetComponent<Renderer>() : null;
        Material shockMaterial = shockRenderer != null ? shockRenderer.material : null;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;
            float fade = 1f - progress;

            if (core != null)
            {
                core.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.35f, progress);
            }

            SetRingScale(shockwave, Mathf.Lerp(0.45f, 2.6f, progress));
            FadeEmission(coreMaterial, 0.75f * fade);
            FadeEmission(shockMaterial, 0.55f * fade);
            SetBaseAlpha(coreMaterial, fade);
            SetBaseAlpha(shockMaterial, fade * 0.8f);

            yield return null;
        }

        DestroyEffect(core);
        DestroyEffect(shockwave);
    }

    private static GameObject CreateSphere(Vector3 position, float size, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "JuiceFlash";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * size;
        RemoveCollider(sphere);
        ApplyEffectColor(sphere.GetComponent<Renderer>(), color);
        return sphere;
    }

    private static GameObject CreateRing(Vector3 position, float radius, Color color)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "JuiceRing";
        ring.transform.position = position;
        SetRingScale(ring, radius);
        RemoveCollider(ring);
        ApplyEffectColor(ring.GetComponent<Renderer>(), color);
        return ring;
    }

    private static void SetRingScale(GameObject ring, float radius)
    {
        if (ring == null) return;

        ring.transform.localScale = new Vector3(radius, 0.04f, radius);
    }

    private static void ApplyEffectColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        GameVisualStyle.ApplyColor(renderer, color, 0.82f, true, 0.5f);
    }

    private static void FadeEmission(Material material, float intensity)
    {
        if (material == null || !material.HasProperty("_EmissionColor")) return;

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
        if (material == null) return;

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
        if (target == null) return;

        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static void DestroyEffect(GameObject target)
    {
        if (target != null)
        {
            Destroy(target);
        }
    }
}
