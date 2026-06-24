using System.Collections.Generic;
using UnityEngine;

public class OrbitOrb : MonoBehaviour
{
    private const float FlameOrbitDamageMultiplier = 1.5f;

    private PlayerStats playerStats;

    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float scanInterval = 0.1f;
    [SerializeField] private float hitCooldown = 0.5f;

    private float scanTimer;
    private readonly Dictionary<Enemy, float> lastHitTimes = new Dictionary<Enemy, float>();
    private readonly List<Enemy> destroyedEnemies = new List<Enemy>();

    public void Init(PlayerStats stats)
    {
        playerStats = stats;
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer > 0f) return;

        scanTimer = scanInterval;
        ScanAndDamage();
    }

    private void ScanAndDamage()
    {
        if (playerStats == null) return;

        CleanupDestroyedEnemies();

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null || !hit.CompareTag("Enemy")) continue;

            Enemy enemy = hit.GetComponent<Enemy>();

            if (enemy == null)
            {
                enemy = hit.GetComponentInParent<Enemy>();
            }

            if (enemy == null) continue;

            TryDamage(enemy);
        }
    }

    private void TryDamage(Enemy enemy)
    {
        if (enemy == null) return;

        float now = Time.time;

        if (lastHitTimes.TryGetValue(enemy, out float lastHitTime))
        {
            if (now - lastHitTime < hitCooldown) return;
        }

        lastHitTimes[enemy] = now;

        int damage = playerStats.GetEffectiveDamageAgainst(enemy);

        if (IsFlameOrbitActive())
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * FlameOrbitDamageMultiplier));
            RunStatsTracker.GetOrCreate().RecordDamageDealt("Flame Orbit", damage);
        }
        else
        {
            RunStatsTracker.GetOrCreate().RecordDamageDealt("Orbiting Orb", damage);
        }

        enemy.TakeDamage(damage);
    }

    private static bool IsFlameOrbitActive()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        return tracker != null && tracker.HasEvolution(BuildEvolutionId.FlameOrbit);
    }

    private void CleanupDestroyedEnemies()
    {
        if (lastHitTimes.Count == 0) return;

        destroyedEnemies.Clear();

        foreach (KeyValuePair<Enemy, float> entry in lastHitTimes)
        {
            if (entry.Key == null)
            {
                destroyedEnemies.Add(entry.Key);
            }
        }

        for (int i = 0; i < destroyedEnemies.Count; i++)
        {
            lastHitTimes.Remove(destroyedEnemies[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
