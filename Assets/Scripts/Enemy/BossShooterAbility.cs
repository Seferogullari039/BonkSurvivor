using UnityEngine;

public class BossShooterAbility : MonoBehaviour
{
    private const float ShootInterval = 2f;
    private const float ChargeDuration = 0.35f;
    private const float ProjectileSpeed = 10f;
    private const int ProjectileDamage = 1;
    private const float ProjectileScale = 0.35f;

    private static readonly Color ChargeColor = new Color(1f, 0.35f, 0.15f);

    private enum ShooterPhase
    {
        Idle,
        Charging
    }

    private Transform playerTarget;
    private Renderer bossRenderer;
    private Color normalColor;
    private float baseScale;
    private ShooterPhase phase = ShooterPhase.Idle;
    private float phaseTimer;
    private float idleTimer;
    private bool telegraphAudioPlayed;

    private void Awake()
    {
        bossRenderer = GetComponent<Renderer>();
        baseScale = transform.localScale.x;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void Start()
    {
        if (bossRenderer != null)
        {
            normalColor = bossRenderer.material.color;
        }
        else
        {
            normalColor = GameVisualPalette.MiniBoss;
        }
    }

    private void Update()
    {
        if (playerTarget == null) return;

        switch (phase)
        {
            case ShooterPhase.Idle:
                UpdateIdle();
                break;
            case ShooterPhase.Charging:
                UpdateCharge();
                break;
        }
    }

    private void UpdateIdle()
    {
        idleTimer += Time.deltaTime;

        if (idleTimer < ShootInterval) return;

        idleTimer = 0f;
        phase = ShooterPhase.Charging;
        phaseTimer = 0f;
        telegraphAudioPlayed = false;
    }

    private void UpdateCharge()
    {
        phaseTimer += Time.deltaTime;

        if (!telegraphAudioPlayed)
        {
            AudioManager.Instance?.PlayTelegraphWarning();
            telegraphAudioPlayed = true;
        }

        if (bossRenderer != null)
        {
            float pulse = 0.5f + Mathf.PingPong(phaseTimer * 10f, 1f) * 0.5f;
            EnemyTelegraphUtility.ApplyFlashColor(
                bossRenderer,
                Color.Lerp(normalColor, ChargeColor, pulse),
                0.6f + pulse * 0.25f
            );
        }

        float chargeScale = baseScale * (1f + Mathf.Sin(phaseTimer * 18f) * 0.05f);
        transform.localScale = Vector3.one * chargeScale;

        if (phaseTimer < ChargeDuration) return;

        transform.localScale = Vector3.one * baseScale;
        EnemyTelegraphUtility.RestoreColor(bossRenderer, normalColor, 0.88f, true, 0.45f);
        FireProjectile();
        phase = ShooterPhase.Idle;
        phaseTimer = 0f;
    }

    private void FireProjectile()
    {
        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        direction.Normalize();

        GameObject projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectileObject.name = "BossProjectile";
        projectileObject.transform.position = transform.position + Vector3.up * 0.5f;
        projectileObject.transform.localScale = Vector3.one * ProjectileScale;

        Collider defaultCollider = projectileObject.GetComponent<Collider>();

        if (defaultCollider != null)
        {
            Destroy(defaultCollider);
        }

        SphereCollider triggerCollider = projectileObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;

        Rigidbody projectileRigidbody = projectileObject.AddComponent<Rigidbody>();
        projectileRigidbody.isKinematic = true;
        projectileRigidbody.useGravity = false;

        Renderer renderer = projectileObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, GameVisualPalette.BossProjectile, 0.6f, true);
        }

        BossProjectile projectile = projectileObject.AddComponent<BossProjectile>();
        projectile.Initialize(direction, ProjectileSpeed, ProjectileDamage);
    }
}
