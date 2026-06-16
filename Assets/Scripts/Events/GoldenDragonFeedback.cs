using System.Collections;
using UnityEngine;

public static class GoldenDragonFeedback
{
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.18f);
    private static readonly Color CoinColor = new Color(1f, 0.88f, 0.15f);
    private static readonly Color XpColor = new Color(0.2f, 0.82f, 1f);

    public static void PlaySpawnFeedback(Vector3 position)
    {
        GameObject host = new GameObject("GoldenDragonSpawnFx");
        GoldenDragonSpawnFxRunner runner = host.AddComponent<GoldenDragonSpawnFxRunner>();
        runner.Run(position);
    }

    public static void PlayDeathBurst(Vector3 position)
    {
        GameObject host = new GameObject("GoldenDragonDeathFx");
        GoldenDragonDeathFxRunner runner = host.AddComponent<GoldenDragonDeathFxRunner>();
        runner.Run(position);
    }

    public static void PlayEscapePuff(Vector3 position)
    {
        GameObject host = new GameObject("GoldenDragonEscapeFx");
        GoldenDragonEscapeFxRunner runner = host.AddComponent<GoldenDragonEscapeFxRunner>();
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

    private sealed class GoldenDragonSpawnFxRunner : MonoBehaviour
    {
        public void Run(Vector3 spawnPosition)
        {
            StartCoroutine(SpawnRoutine(spawnPosition));
        }

        private IEnumerator SpawnRoutine(Vector3 spawnPosition)
        {
            Vector3 effectPosition = spawnPosition + Vector3.up * 1.1f;
            GameObject flash = CreateBurstSphere(effectPosition, 0.28f, GoldColor, 0.72f);

            Vector3 trailDirection = Vector3.back;
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                trailDirection = spawnPosition - player.transform.position;
                trailDirection.y = 0f;

                if (trailDirection.sqrMagnitude > 0.001f)
                {
                    trailDirection.Normalize();
                }
            }

            const int trailCount = 4;
            GameObject[] trailSpheres = new GameObject[trailCount];

            for (int i = 0; i < trailCount; i++)
            {
                float trailOffset = 0.55f + i * 0.45f;
                Vector3 trailPosition = effectPosition - trailDirection * trailOffset;
                trailSpheres[i] = CreateBurstSphere(trailPosition, 0.1f + i * 0.02f, GoldColor, 0.55f);
            }

            const float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (flash != null)
                {
                    flash.transform.localScale = Vector3.one * Mathf.Lerp(0.28f, 1.05f, progress);
                }

                for (int i = 0; i < trailCount; i++)
                {
                    GameObject trailSphere = trailSpheres[i];

                    if (trailSphere == null) continue;

                    trailSphere.transform.position += trailDirection * 2.2f * Time.deltaTime;
                    SetBurstAlpha(trailSphere, fade);
                }

                SetBurstAlpha(flash, fade);
                yield return null;
            }

            DestroyBurst(flash);

            for (int i = 0; i < trailCount; i++)
            {
                DestroyBurst(trailSpheres[i]);
            }

            Destroy(gameObject);
        }
    }

    private sealed class GoldenDragonDeathFxRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(DeathRoutine(position));
        }

        private IEnumerator DeathRoutine(Vector3 position)
        {
            Vector3 effectPosition = position + Vector3.up * 1.2f;

            GameObject goldFlash = CreateBurstSphere(effectPosition, 0.24f, GoldColor, 0.78f);
            GameObject coinSpark = CreateBurstSphere(
                effectPosition + new Vector3(-0.35f, 0.12f, 0.18f),
                0.14f,
                CoinColor,
                0.62f);
            GameObject xpSpark = CreateBurstSphere(
                effectPosition + new Vector3(0.35f, 0.12f, -0.18f),
                0.14f,
                XpColor,
                0.62f);

            const int particleCount = 5;
            GameObject[] particles = new GameObject[particleCount];
            Vector3[] velocities = new Vector3[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                particles[i] = CreateBurstSphere(effectPosition, 0.08f, i % 2 == 0 ? CoinColor : XpColor, 0.48f);
                Vector2 spread = Random.insideUnitCircle.normalized;
                velocities[i] = new Vector3(spread.x, Random.Range(0.35f, 0.85f), spread.y) * Random.Range(1.4f, 2.6f);
            }

            const float duration = 0.48f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (goldFlash != null)
                {
                    goldFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.24f, 0.95f, progress);
                }

                SetBurstAlpha(goldFlash, fade);
                SetBurstAlpha(coinSpark, fade);
                SetBurstAlpha(xpSpark, fade);

                if (coinSpark != null)
                {
                    coinSpark.transform.localScale = Vector3.one * (0.14f + progress * 0.1f);
                }

                if (xpSpark != null)
                {
                    xpSpark.transform.localScale = Vector3.one * (0.14f + progress * 0.1f);
                }

                for (int i = 0; i < particleCount; i++)
                {
                    GameObject particle = particles[i];

                    if (particle == null) continue;

                    particle.transform.position += velocities[i] * Time.deltaTime;
                    velocities[i] += Vector3.down * 2.2f * Time.deltaTime;
                    SetBurstAlpha(particle, fade);
                }

                yield return null;
            }

            DestroyBurst(goldFlash);
            DestroyBurst(coinSpark);
            DestroyBurst(xpSpark);

            for (int i = 0; i < particleCount; i++)
            {
                DestroyBurst(particles[i]);
            }

            Destroy(gameObject);
        }
    }

    private sealed class GoldenDragonEscapeFxRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(EscapeRoutine(position));
        }

        private IEnumerator EscapeRoutine(Vector3 position)
        {
            Vector3 effectPosition = position + Vector3.up * 1.1f;
            GameObject puff = CreateBurstSphere(effectPosition, 0.2f, GoldColor, 0.45f);

            const float duration = 0.32f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float fade = 1f - progress;

                if (puff != null)
                {
                    puff.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0.72f, progress);
                    puff.transform.position = effectPosition + Vector3.up * (progress * 0.55f);
                }

                SetBurstAlpha(puff, fade * 0.75f);
                yield return null;
            }

            DestroyBurst(puff);
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
