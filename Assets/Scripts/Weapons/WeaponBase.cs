using UnityEngine;

public abstract class WeaponBase
{
    protected float fireRate = 1f;
    protected float damage = 1f;
    protected float projectileSpeed = 12f;
    protected PlayerStats playerStats;

    private float timer;

    public virtual void Init(PlayerStats stats)
    {
        playerStats = stats;
        damage = stats != null ? stats.damage : 1f;
    }

    public virtual void Tick()
    {
        if (playerStats == null) return;

        damage = playerStats.damage;

        timer += Time.deltaTime;

        if (timer >= fireRate)
        {
            Fire();
            timer = 0f;
        }
    }

    public abstract void Fire();

    public void IncreaseFireRate(float percent)
    {
        fireRate *= 1f - percent;
        fireRate = Mathf.Max(fireRate, 0.1f);
    }

    public void ResetFireRate(float rate)
    {
        fireRate = rate;
    }

    protected static Transform FindClosestEnemyTransform(Vector3 origin)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return null;

        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];

            if (enemy == null) continue;

            float distance = Vector3.Distance(origin, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy.transform;
            }
        }

        return closest;
    }
}
