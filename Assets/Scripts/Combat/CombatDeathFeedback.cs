using System.Collections;
using UnityEngine;

public static class CombatDeathFeedback
{
    private static readonly Color BurstColor = new Color(0.95f, 0.25f, 0.2f);

    public static void PlayBurst(Vector3 position)
    {
        GameObject burstHost = new GameObject("CombatDeathBurstFx");
        DeathBurstRunner runner = burstHost.AddComponent<DeathBurstRunner>();
        runner.Run(position);
    }

    private sealed class DeathBurstRunner : MonoBehaviour
    {
        public void Run(Vector3 position)
        {
            StartCoroutine(DeathBurstRoutine(position));
        }

        private IEnumerator DeathBurstRoutine(Vector3 position)
        {
            Vector3 burstOrigin = position + Vector3.up * 0.32f;

            GameObject flashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashObject.name = "EnemyDeathFlash";
            flashObject.transform.position = burstOrigin;
            flashObject.transform.localScale = Vector3.one * 0.16f;

            Collider flashCollider = flashObject.GetComponent<Collider>();

            if (flashCollider != null)
            {
                Object.Destroy(flashCollider);
            }

            Renderer flashRenderer = flashObject.GetComponent<Renderer>();

            if (flashRenderer != null)
            {
                GameVisualStyle.ApplyColor(flashRenderer, BurstColor, 0.55f, true, 0.65f);
            }

            const int particleCount = 5;
            GameObject[] particles = new GameObject[particleCount];
            Vector3[] velocities = new Vector3[particleCount];
            float[] sizes = new float[particleCount];

            for (int i = 0; i < particleCount; i++)
            {
                GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                particle.name = "DeathBurstParticle";
                particle.transform.position = burstOrigin;
                sizes[i] = Random.Range(0.06f, 0.12f);
                particle.transform.localScale = Vector3.one * sizes[i];

                Collider collider = particle.GetComponent<Collider>();

                if (collider != null)
                {
                    Object.Destroy(collider);
                }

                Renderer renderer = particle.GetComponent<Renderer>();

                if (renderer != null)
                {
                    GameVisualStyle.ApplyColor(renderer, BurstColor, 0.65f, true, 0.45f);
                }

                Vector2 spread = Random.insideUnitCircle.normalized;
                velocities[i] = new Vector3(spread.x, Random.Range(0.3f, 0.75f), spread.y) * Random.Range(1.1f, 2.1f);
                particles[i] = particle;
            }

            const float duration = 0.34f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (flashObject != null)
                {
                    flashObject.transform.localScale = Vector3.one * (0.16f + fade * 0.18f);
                }

                for (int i = 0; i < particleCount; i++)
                {
                    GameObject particle = particles[i];

                    if (particle == null)
                    {
                        continue;
                    }

                    particle.transform.position += velocities[i] * Time.deltaTime;
                    velocities[i] += Vector3.down * 2.8f * Time.deltaTime;
                    particle.transform.localScale = Vector3.one * sizes[i] * fade;
                }

                yield return null;
            }

            if (flashObject != null)
            {
                Object.Destroy(flashObject);
            }

            for (int i = 0; i < particleCount; i++)
            {
                if (particles[i] != null)
                {
                    Object.Destroy(particles[i]);
                }
            }

            Object.Destroy(gameObject);
        }
    }
}
