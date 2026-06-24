using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 1;

    private Vector3 direction;
    private bool initialized;
    private int remainingPierce;
    private readonly HashSet<int> hitEnemyIds = new HashSet<int>();

    public void Initialize(Vector3 shootDirection)
    {
        direction = shootDirection.normalized;
        hitEnemyIds.Clear();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerStats playerStats = player != null ? player.GetComponent<PlayerStats>() : null;
        remainingPierce = playerStats != null ? playerStats.PierceCount : 0;

        ConfigureProjectilePhysics(player);
        initialized = true;
    }

    private void ConfigureProjectilePhysics(GameObject player)
    {
        Rigidbody projectileRigidbody = GetComponent<Rigidbody>();

        if (projectileRigidbody != null)
        {
            projectileRigidbody.useGravity = false;

            if (!projectileRigidbody.isKinematic)
            {
                projectileRigidbody.linearVelocity = Vector3.zero;
                projectileRigidbody.angularVelocity = Vector3.zero;
            }

            projectileRigidbody.isKinematic = true;
            projectileRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        Collider projectileCollider = GetComponent<Collider>();

        if (projectileCollider == null || player == null) return;

        Collider[] playerColliders = player.GetComponentsInChildren<Collider>();

        for (int i = 0; i < playerColliders.Length; i++)
        {
            Collider playerCollider = playerColliders[i];

            if (playerCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, playerCollider, true);
            }
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized) return;

        float finalSpeed = speed;

        if (UpgradeManager.Instance != null)
        {
            finalSpeed *= UpgradeManager.Instance.ProjectileSpeedMultiplier;
        }

        transform.position += direction * finalSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy == null) return;

        int enemyId = enemy.GetInstanceID();

        if (hitEnemyIds.Contains(enemyId)) return;

        hitEnemyIds.Add(enemyId);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        PlayerStats playerStats = player != null ? player.GetComponent<PlayerStats>() : null;
        int damageAmount = playerStats != null ? playerStats.GetEffectiveDamageAgainst(enemy) : damage;

        RunStatsTracker.GetOrCreate().RecordDamageDealt("Projectile", damageAmount);
        enemy.TakeDamage(damageAmount);

        if (remainingPierce > 0)
        {
            remainingPierce--;
            return;
        }

        Destroy(gameObject);
    }
}
