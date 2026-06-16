using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class EnemyHitFeedback : MonoBehaviour
{
    private const float HitFlashDuration = 0.12f;
    private const float KnockbackDuration = 0.08f;
    private const float NormalKnockbackDistance = 0.35f;
    private const float BossKnockbackDistance = 0.05f;

    private static readonly Color HitFlashColor = new Color(1f, 0.58f, 0.48f);
    private static readonly Color DeathBurstColor = new Color(0.95f, 0.25f, 0.2f);
    private static MaterialPropertyBlock sharedFlashBlock;

    private Enemy enemy;
    private Renderer[] flashRenderers;
    private Color[] baseColors;
    private float hitFlashTimer;
    private Coroutine knockbackRoutine;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        CacheRenderers();
    }

    public void PlayHit(Vector3 hitSource)
    {
        if (NeedsRendererRefresh())
        {
            CacheRenderers();
        }

        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            return;
        }

        hitFlashTimer = HitFlashDuration;
        ApplyKnockback(hitSource);
        HitFeedbackUtility.TryPlayHitSound();
    }

    public void PlayDeath(Vector3 position)
    {
        if (HitFeedbackUtility.TrySpawnDeathVfx(position))
        {
            return;
        }

        GameObject burstHost = new GameObject("EnemyDeathBurstFx");
        DeathBurstRunner runner = burstHost.AddComponent<DeathBurstRunner>();
        runner.Run(position);
    }

    private void LateUpdate()
    {
        if (hitFlashTimer <= 0f || flashRenderers == null || baseColors == null)
        {
            return;
        }

        hitFlashTimer -= Time.deltaTime;
        float flashStrength = Mathf.Clamp01(hitFlashTimer / HitFlashDuration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Renderer renderer = flashRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (flashStrength <= 0.001f)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            ApplyFlashRenderer(renderer, i, flashStrength);
        }
    }

    private void ApplyFlashRenderer(Renderer renderer, int index, float flashStrength)
    {
        if (sharedFlashBlock == null)
        {
            sharedFlashBlock = new MaterialPropertyBlock();
        }

        Color flashColor = Color.Lerp(baseColors[index], HitFlashColor, flashStrength);
        renderer.GetPropertyBlock(sharedFlashBlock);

        Material sharedMaterial = renderer.sharedMaterial;

        if (sharedMaterial != null)
        {
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                sharedFlashBlock.SetColor("_BaseColor", flashColor);
            }

            if (sharedMaterial.HasProperty("_Color"))
            {
                sharedFlashBlock.SetColor("_Color", flashColor);
            }

            if (sharedMaterial.HasProperty("_EmissionColor"))
            {
                sharedFlashBlock.SetColor("_EmissionColor", flashColor * (0.35f + flashStrength * 0.65f));
            }
        }

        renderer.SetPropertyBlock(sharedFlashBlock);
    }

    private void ApplyKnockback(Vector3 hitSource)
    {
        float knockbackDistance = GetKnockbackDistance();

        if (knockbackDistance <= 0.001f)
        {
            return;
        }

        Vector3 direction = transform.position - hitSource;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        direction.Normalize();

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction, knockbackDistance));
    }

    private float GetKnockbackDistance()
    {
        if (enemy == null)
        {
            return NormalKnockbackDistance;
        }

        return enemy.Type switch
        {
            Enemy.EnemyType.MiniBoss => BossKnockbackDistance,
            Enemy.EnemyType.DragonBoss => BossKnockbackDistance,
            _ => NormalKnockbackDistance
        };
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float distance)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + direction * distance;
        float elapsed = 0f;

        while (elapsed < KnockbackDuration)
        {
            if (this == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / KnockbackDuration);
            float eased = 1f - (1f - progress) * (1f - progress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            yield return null;
        }

        knockbackRoutine = null;
    }

    private bool NeedsRendererRefresh()
    {
        Renderer[] currentRenderers = GetComponentsInChildren<Renderer>(false);
        return flashRenderers == null
            || baseColors == null
            || currentRenderers.Length != flashRenderers.Length;
    }

    private void CacheRenderers()
    {
        flashRenderers = GetComponentsInChildren<Renderer>(false);
        int count = flashRenderers.Length;
        baseColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            Renderer renderer = flashRenderers[i];

            if (renderer == null)
            {
                baseColors[i] = Color.white;
                continue;
            }

            Material sharedMaterial = renderer.sharedMaterial;

            if (sharedMaterial == null)
            {
                baseColors[i] = Color.white;
                continue;
            }

            baseColors[i] = sharedMaterial.HasProperty("_BaseColor")
                ? sharedMaterial.GetColor("_BaseColor")
                : sharedMaterial.color;
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
            GameObject flashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flashObject.name = "EnemyDeathFlash";
            flashObject.transform.position = position + Vector3.up * 0.35f;
            flashObject.transform.localScale = Vector3.one * 0.18f;

            Collider flashCollider = flashObject.GetComponent<Collider>();

            if (flashCollider != null)
            {
                Destroy(flashCollider);
            }

            Renderer flashRenderer = flashObject.GetComponent<Renderer>();

            if (flashRenderer != null)
            {
                GameVisualStyle.ApplyColor(flashRenderer, DeathBurstColor, 0.55f, true, 0.65f);
            }

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

            const float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / duration;

                if (flashObject != null)
                {
                    flashObject.transform.localScale = Vector3.one * (0.18f + fade * 0.22f);
                }

                for (int i = 0; i < particleCount; i++)
                {
                    GameObject particle = particles[i];

                    if (particle == null)
                    {
                        continue;
                    }

                    particle.transform.position += velocities[i] * Time.deltaTime;
                    velocities[i] += Vector3.down * 2.5f * Time.deltaTime;
                    particle.transform.localScale = Vector3.one * sizes[i] * fade;
                }

                yield return null;
            }

            if (flashObject != null)
            {
                Destroy(flashObject);
            }

            for (int i = 0; i < particleCount; i++)
            {
                if (particles[i] != null)
                {
                    Destroy(particles[i]);
                }
            }

            Destroy(gameObject, 0.5f);
        }
    }
}
