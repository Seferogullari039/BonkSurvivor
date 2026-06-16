using System.Collections;
using UnityEngine;

public static class VoidPortalFeedback
{
    private static readonly Color VoidColor = new Color(0.58f, 0.12f, 0.88f);
    private static readonly Color GlowColor = new Color(0.72f, 0.22f, 1f);
    private static readonly Color CoreColor = new Color(0.42f, 0.08f, 0.72f);

    public static void PlayOpenFeedback(Vector3 position)
    {
        GameObject host = new GameObject("VoidPortalOpenFx");
        VoidPortalOpenFxRunner runner = host.AddComponent<VoidPortalOpenFxRunner>();
        runner.Run(position);
    }

    public static void PlayEnemySpawnFlash(Vector3 position)
    {
        GameObject host = new GameObject("VoidPortalEnemySpawnFx");
        VoidPortalEnemySpawnFxRunner runner = host.AddComponent<VoidPortalEnemySpawnFxRunner>();
        runner.Run(position);
    }

    public static void PlayCloseFeedback(Vector3 position)
    {
        GameObject host = new GameObject("VoidPortalCloseFx");
        VoidPortalCloseFxRunner runner = host.AddComponent<VoidPortalCloseFxRunner>();
        runner.Run(position);
    }

    private static void ApplyBurstColor(Renderer renderer, Color color, float emission)
    {
        if (renderer == null) return;

        Material baseMaterial = ChestVisualMaterials.GetGlowBaseMaterial();

        if (baseMaterial != null)
        {
            renderer.sharedMaterial = baseMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, color, 0.75f, true, emission);
    }

    private static GameObject CreateBurstSphere(Vector3 position, float size, Color color, float emission)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * size;

        Collider collider = sphere.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        ApplyBurstColor(sphere.GetComponent<Renderer>(), color, emission);
        return sphere;
    }

    private static GameObject CreateBurstRing(Vector3 position, float radius, Color color, float emission)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.position = position;
        ring.transform.localScale = new Vector3(radius, 0.04f, radius);

        Collider collider = ring.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        ApplyBurstColor(ring.GetComponent<Renderer>(), color, emission);
        return ring;
    }

    private static void SetBurstAlpha(GameObject target, float alpha)
    {
        if (target == null) return;

        Renderer renderer = target.GetComponent<Renderer>();
        Material material = renderer != null ? renderer.material : null;

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

        if (material.HasProperty("_EmissionColor"))
        {
            Color emission = material.GetColor("_EmissionColor");
            emission *= alpha;
            material.SetColor("_EmissionColor", emission);
        }
    }

    private sealed class VoidPortalOpenFxRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(OpenRoutine(position));
        }

        private IEnumerator OpenRoutine(Vector3 position)
        {
            Vector3 effectPosition = position + Vector3.up * 0.72f;

            GameObject flash = CreateBurstSphere(effectPosition, 0.24f, GlowColor, 0.78f);
            GameObject ring = CreateBurstRing(effectPosition, 0.55f, VoidColor, 0.68f);

            const float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (flash != null)
                {
                    flash.transform.localScale = Vector3.one * Mathf.Lerp(0.24f, 0.95f, progress);
                }

                if (ring != null)
                {
                    float radius = Mathf.Lerp(0.55f, 1.65f, progress);
                    ring.transform.localScale = new Vector3(radius, 0.04f, radius);
                }

                SetBurstAlpha(flash, fade);
                SetBurstAlpha(ring, fade * 0.9f);
                yield return null;
            }

            DestroyBurst(flash);
            DestroyBurst(ring);
            Destroy(gameObject);
        }
    }

    private sealed class VoidPortalEnemySpawnFxRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(SpawnRoutine(position));
        }

        private IEnumerator SpawnRoutine(Vector3 position)
        {
            Vector3 effectPosition = position + Vector3.up * 0.35f;
            GameObject flash = CreateBurstSphere(effectPosition, 0.12f, GlowColor, 0.62f);

            const float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (flash != null)
                {
                    flash.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.28f, progress);
                }

                SetBurstAlpha(flash, fade);
                yield return null;
            }

            DestroyBurst(flash);
            Destroy(gameObject);
        }
    }

    private sealed class VoidPortalCloseFxRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(CloseRoutine(position));
        }

        private IEnumerator CloseRoutine(Vector3 position)
        {
            Vector3 effectPosition = position + Vector3.up * 0.72f;
            GameObject puff = CreateBurstSphere(effectPosition, 0.22f, CoreColor, 0.55f);
            GameObject ring = CreateBurstRing(effectPosition, 0.45f, VoidColor, 0.48f);

            const float duration = 0.34f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (puff != null)
                {
                    puff.transform.localScale = Vector3.one * Mathf.Lerp(0.22f, 0.82f, progress);
                    puff.transform.position = effectPosition + Vector3.up * (progress * 0.45f);
                }

                if (ring != null)
                {
                    float radius = Mathf.Lerp(0.45f, 1.1f, progress);
                    ring.transform.localScale = new Vector3(radius, 0.035f, radius);
                    ring.transform.position = effectPosition + Vector3.up * (progress * 0.35f);
                }

                SetBurstAlpha(puff, fade * 0.85f);
                SetBurstAlpha(ring, fade * 0.75f);
                yield return null;
            }

            DestroyBurst(puff);
            DestroyBurst(ring);
            Destroy(gameObject);
        }
    }

    private static void DestroyBurst(GameObject target)
    {
        if (target != null)
        {
            Object.Destroy(target);
        }
    }
}
