using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class EnemyHitFeedback : MonoBehaviour
{
    private const float HitFlashDuration = 0.1f;

    private static readonly Color HitFlashColor = new Color(1f, 0.95f, 0.95f);
    private static readonly Color DeathBurstColor = new Color(0.95f, 0.25f, 0.2f);

    private Renderer[] flashRenderers;
    private Color[] baseColors;
    private float[] baseSmoothness;
    private bool[] baseGlow;
    private float hitFlashTimer;

    private void Awake()
    {
        CacheRenderers();
    }

    public void PlayHit()
    {
        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            CacheRenderers();
        }

        hitFlashTimer = HitFlashDuration;
    }

    public void PlayDeath(Vector3 position)
    {
        GameObject burstHost = new GameObject("EnemyDeathBurstFx");
        DeathBurstRunner runner = burstHost.AddComponent<DeathBurstRunner>();
        runner.Run(position);
    }

    private void LateUpdate()
    {
        if (hitFlashTimer <= 0f || flashRenderers == null || baseColors == null) return;

        hitFlashTimer -= Time.deltaTime;
        float flashStrength = Mathf.Clamp01(hitFlashTimer / HitFlashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer renderer = flashRenderers[i];

            if (renderer == null) continue;

            Color color = Color.Lerp(baseColors[i], HitFlashColor, flashStrength);
            GameVisualStyle.ApplyColor(renderer, color, baseSmoothness[i], baseGlow[i]);
        }
    }

    private void CacheRenderers()
    {
        flashRenderers = GetComponentsInChildren<Renderer>(false);
        int count = flashRenderers.Length;
        baseColors = new Color[count];
        baseSmoothness = new float[count];
        baseGlow = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Renderer renderer = flashRenderers[i];

            if (renderer == null) continue;

            Material material = renderer.material;
            baseColors[i] = material.color;
            baseSmoothness[i] = material.HasProperty("_Smoothness")
                ? material.GetFloat("_Smoothness")
                : 0.42f;

            if (material.HasProperty("_EmissionColor"))
            {
                Color emission = material.GetColor("_EmissionColor");
                baseGlow[i] = emission.maxColorComponent > 0.05f;
            }
        }
    }

    private sealed class DeathBurstRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(DeathBurstRoutine(position));
        }

        private IEnumerator DeathBurstRoutine(Vector3 position)
        {
            const int particleCount = 6;
            GameObject[] particles = new GameObject[particleCount];
            Vector3[] velocities = new Vector3[particleCount];
            float[] sizes = new float[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                particle.name = "DeathBurstParticle";
                particle.transform.position = position + Vector3.up * 0.35f;
                sizes[i] = Random.Range(0.08f, 0.16f);
                particle.transform.localScale = Vector3.one * sizes[i];

                Collider collider = particle.GetComponent<Collider>();

                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = particle.GetComponent<Renderer>();

                if (renderer != null)
                {
                    GameVisualStyle.ApplyColor(renderer, DeathBurstColor, 0.65f, true, 0.45f);
                }

                Vector2 spread = Random.insideUnitCircle.normalized;
                velocities[i] = new Vector3(spread.x, Random.Range(0.35f, 0.9f), spread.y) * Random.Range(1.2f, 2.4f);
                particles[i] = particle;
            }

            const float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                for (int i = 0; i < particleCount; i++)
                {
                    GameObject particle = particles[i];

                    if (particle == null) continue;

                    particle.transform.position += velocities[i] * Time.deltaTime;
                    velocities[i] += Vector3.down * 2.5f * Time.deltaTime;
                    particle.transform.localScale = Vector3.one * sizes[i] * fade;

                    Renderer renderer = particle.GetComponent<Renderer>();
                    Material material = renderer != null ? renderer.material : null;

                    if (material != null && material.HasProperty("_BaseColor"))
                    {
                        Color color = material.GetColor("_BaseColor");
                        color.a = fade;
                        material.SetColor("_BaseColor", color);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < particleCount; i++)
            {
                if (particles[i] != null)
                {
                    Destroy(particles[i]);
                }
            }

            Destroy(gameObject, 1.5f);
        }
    }
}
