using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketProjectile : MonoBehaviour
{
    private const float Speed = 5.5f;
    private const float LifeTime = 4f;
    private const float DefaultExplosionRadius = 3f;
    private const float ShakeDuration = 0.16f;
    private const float ShakeIntensity = 0.09f;
    private const float ExplosionVisualLifetime = 0.45f;
    private const float ExplosionWorldVolume = 2.0f;
    private const float ExplosionCameraLayerVolume = 0.85f;
    private const float ExplosionSpatialBlend = 0.5f;
    private const float ExplosionMinDistance = 10f;
    private const float ExplosionMaxDistance = 70f;

    [SerializeField] private AudioClip explosionClip;
    [SerializeField] private float explosionVolume = 1.75f;

    private static AudioClip cachedExplosionPlaceholder;

    private Vector3 direction;
    private PlayerStats playerStats;
    private bool initialized;
    private bool hasExploded;

    public void Initialize(Vector3 shootDirection, PlayerStats stats, AudioClip assignedExplosionClip = null)
    {
        direction = shootDirection.normalized;
        playerStats = stats;

        if (assignedExplosionClip != null)
        {
            explosionClip = assignedExplosionClip;
        }

        EnsurePhysics();
        IgnorePlayerCollisions();
        initialized = true;
        hasExploded = false;
        CancelInvoke(nameof(ExplodeOnLifetime));
        Invoke(nameof(ExplodeOnLifetime), LifeTime);
    }

    private void EnsurePhysics()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
        }

        sphereCollider.isTrigger = false;
        sphereCollider.radius = 0.5f;

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void IgnorePlayerCollisions()
    {
        Collider rocketCollider = GetComponent<Collider>();

        if (rocketCollider == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];

            if (playerCollider != null)
            {
                Physics.IgnoreCollision(rocketCollider, playerCollider, true);
            }
        }
    }

    private void Update()
    {
        if (!initialized || hasExploded) return;

        float stepDistance = Speed * Time.deltaTime;
        Vector3 currentPosition = transform.position;

        if (TryGetMovementHit(currentPosition, stepDistance, out RaycastHit hit))
        {
            Explode(hit.point);
            return;
        }

        transform.position = currentPosition + direction * stepDistance;
    }

    private float GetCastRadius()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();

        if (sphereCollider == null) return 0.14f;

        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
        return sphereCollider.radius * scale;
    }

    private bool TryGetMovementHit(Vector3 currentPosition, float stepDistance, out RaycastHit hit)
    {
        hit = default;
        float castRadius = GetCastRadius();
        float castDistance = stepDistance + castRadius * 0.1f;

        if (Physics.SphereCast(
            currentPosition,
            castRadius,
            direction,
            out hit,
            castDistance,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore))
        {
            return ShouldExplodeFrom(hit.collider);
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ShouldExplodeFrom(other)) return;

        Explode(transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || !ShouldExplodeFrom(collision.collider)) return;

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : transform.position;
        Explode(hitPoint);
    }

    private bool ShouldExplodeFrom(Collider other)
    {
        if (!initialized || hasExploded) return false;
        if (other == null) return false;
        if (other.CompareTag("Player")) return false;
        if (other.transform == transform || other.transform.IsChildOf(transform)) return false;

        return true;
    }

    private void ExplodeOnLifetime()
    {
        if (hasExploded) return;

        Explode(transform.position);
    }

    private void Explode(Vector3 explosionPoint)
    {
        if (hasExploded) return;

        hasExploded = true;
        initialized = false;
        CancelInvoke(nameof(ExplodeOnLifetime));

        transform.position = explosionPoint;
        Vector3 center = explosionPoint;
        float explosionRadius = playerStats != null ? playerStats.RocketAoERadius : DefaultExplosionRadius;
        int damageAmount = playerStats != null ? playerStats.damage : 1;

        Collider[] hits = Physics.OverlapSphere(center, explosionRadius);
        HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null || !hits[i].CompareTag("Enemy")) continue;

            Enemy enemy = hits[i].GetComponent<Enemy>();

            if (enemy == null)
            {
                enemy = hits[i].GetComponentInParent<Enemy>();
            }

            if (enemy == null || damagedEnemies.Contains(enemy)) continue;

            damagedEnemies.Add(enemy);
            enemy.TakeDamage(damageAmount);
        }

        PlayRocketExplosionSound(center);
        StartRocketCameraShake();
        SpawnExplosionVisual(center, explosionRadius);
        Destroy(gameObject);
    }

    private void PlayRocketExplosionSound(Vector3 position)
    {
        AudioClip clip = ResolveExplosionClip();

        if (clip == null) return;

        float worldVolume = explosionVolume > 0f ? explosionVolume : ExplosionWorldVolume;

        PlayExplosionClipAt(
            position,
            clip,
            worldVolume,
            ExplosionSpatialBlend,
            ExplosionMinDistance,
            ExplosionMaxDistance);

        Camera camera = Camera.main;

        if (camera != null)
        {
            PlayExplosionClipAt(
                camera.transform.position,
                clip,
                ExplosionCameraLayerVolume,
                0f,
                1f,
                500f);
        }
    }

    private static void PlayExplosionClipAt(
        Vector3 position,
        AudioClip clip,
        float volume,
        float spatialBlend,
        float minDistance,
        float maxDistance)
    {
        GameObject audioObject = new GameObject("RocketExplosionAudio");
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Object.Destroy(audioObject, clip.length + 0.15f);
    }

    private AudioClip ResolveExplosionClip()
    {
        if (explosionClip != null) return explosionClip;

        if (WeaponManager.DefaultRocketExplosionClip != null)
        {
            return WeaponManager.DefaultRocketExplosionClip;
        }

        return GetExplosionPlaceholderClip();
    }

    private static AudioClip GetExplosionPlaceholderClip()
    {
        if (cachedExplosionPlaceholder != null) return cachedExplosionPlaceholder;

        const int sampleRate = 44100;
        const float duration = 0.32f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(17);

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float progress = time / duration;
            float frequency = Mathf.Lerp(95f, 38f, progress);
            float sine = Mathf.Sin(Mathf.PI * 2f * frequency * time);
            float noise = ((float)random.NextDouble() * 2f - 1f) * 0.42f;
            float envelope = Mathf.Exp(-progress * 6.5f) * (1f - progress * 0.25f);
            samples[i] = Mathf.Clamp((sine * 0.62f + noise * 0.48f) * envelope, -1f, 1f);
        }

        cachedExplosionPlaceholder = AudioClip.Create("RocketExplosionPlaceholder", sampleCount, 1, sampleRate, false);
        cachedExplosionPlaceholder.SetData(samples, 0);
        return cachedExplosionPlaceholder;
    }

    private void StartRocketCameraShake()
    {
        Camera camera = Camera.main;

        if (camera == null) return;

        RocketCameraShakeRunner runner = camera.GetComponent<RocketCameraShakeRunner>();

        if (runner == null)
        {
            runner = camera.gameObject.AddComponent<RocketCameraShakeRunner>();
        }

        runner.StartCoroutine(RocketCameraShake());
    }

    private static IEnumerator RocketCameraShake()
    {
        Camera camera = Camera.main;

        if (camera == null) yield break;

        Transform cameraTransform = camera.transform;
        Vector3 baseLocalPosition = cameraTransform.localPosition;
        float timer = ShakeDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float fade = ShakeDuration > 0f ? Mathf.Clamp01(timer / ShakeDuration) : 0f;
            Vector3 offset = Random.insideUnitSphere * ShakeIntensity * fade;
            offset.z *= 0.25f;
            cameraTransform.localPosition = baseLocalPosition + offset;
            yield return null;
        }

        cameraTransform.localPosition = baseLocalPosition;
    }

    private static void SpawnExplosionVisual(Vector3 center, float explosionRadius)
    {
        float visualMaxScale = Mathf.Max(3f, explosionRadius * 2.2f);
        float startScale = Mathf.Max(0.35f, explosionRadius * 0.18f);

        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "RocketExplosionFx";
        flash.transform.position = center + Vector3.up * 0.15f;
        flash.transform.localScale = Vector3.one * startScale;

        Collider collider = flash.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        Renderer renderer = flash.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, new Color(1f, 0.55f, 0.1f), 0.72f, true, 0.7f);
        }

        ExplosionFlashRunner runner = flash.AddComponent<ExplosionFlashRunner>();
        runner.Play(ExplosionVisualLifetime, visualMaxScale, startScale);
    }

    private sealed class RocketCameraShakeRunner : MonoBehaviour
    {
    }

    private sealed class ExplosionFlashRunner : MonoBehaviour
    {
        public void Play(float duration, float maxScale, float startScale)
        {
            StartCoroutine(FlashRoutine(duration, maxScale, startScale));
        }

        private IEnumerator FlashRoutine(float duration, float maxScale, float startScale)
        {
            Renderer renderer = GetComponent<Renderer>();
            Material material = renderer != null ? renderer.material : null;
            Color baseColor = material != null ? material.color : new Color(1f, 0.55f, 0.1f, 1f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = Mathf.Lerp(startScale, maxScale, progress);
                transform.localScale = Vector3.one * scale;

                if (material != null)
                {
                    Color fadeColor = baseColor;
                    fadeColor.a = 1f - progress;
                    material.color = fadeColor;

                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", fadeColor);
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", baseColor * (0.65f * (1f - progress)));
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
