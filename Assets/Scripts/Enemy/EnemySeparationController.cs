using UnityEngine;

[DisallowMultipleComponent]
public class EnemySeparationController : MonoBehaviour
{
    [SerializeField] private float separationRadius = 0.85f;
    [SerializeField] private float separationStrength = 0.25f;
    [SerializeField] private float updateInterval = 0.08f;
    [SerializeField] private float maxSeparationStep = 0.015f;

    private static readonly Collider[] OverlapBuffer = new Collider[24];

    private Enemy owner;
    private float nextUpdateTime;

    private void Awake()
    {
        owner = GetComponent<Enemy>();

        if (!ShouldRun())
        {
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (owner == null || !enabled)
        {
            return;
        }

        if (Time.time < nextUpdateTime)
        {
            return;
        }

        nextUpdateTime = Time.time + updateInterval;
        ApplySeparation();
    }

    private bool ShouldRun()
    {
        if (owner == null)
        {
            return false;
        }

        if (GetComponent<MimicChestController>() != null || GetComponent<GoldenDragonController>() != null)
        {
            return false;
        }

        Enemy.EnemyType type = owner.Type;

        return type != Enemy.EnemyType.MiniBoss && type != Enemy.EnemyType.DragonBoss;
    }

    private void ApplySeparation()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            OverlapBuffer);

        if (hitCount <= 0)
        {
            return;
        }

        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider nearbyCollider = OverlapBuffer[i];

            if (nearbyCollider == null)
            {
                continue;
            }

            Enemy nearbyEnemy = nearbyCollider.GetComponent<Enemy>() ?? nearbyCollider.GetComponentInParent<Enemy>();

            if (nearbyEnemy == null || nearbyEnemy == owner)
            {
                continue;
            }

            if (nearbyEnemy.Type == Enemy.EnemyType.MiniBoss || nearbyEnemy.Type == Enemy.EnemyType.DragonBoss)
            {
                continue;
            }

            Vector3 push = transform.position - nearbyEnemy.transform.position;
            push.y = 0f;

            float distance = push.magnitude;

            if (distance < 0.001f)
            {
                push = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                distance = push.magnitude;
            }

            if (distance < 0.001f || distance > separationRadius)
            {
                continue;
            }

            float weight = 1f - (distance / separationRadius);
            separation += push.normalized * weight;
            neighborCount++;
        }

        if (neighborCount == 0 || separation.sqrMagnitude < 0.0001f)
        {
            return;
        }

        separation /= neighborCount;
        separation = separation.normalized * (separationStrength * maxSeparationStep);
        separation.y = 0f;

        if (separation.sqrMagnitude > maxSeparationStep * maxSeparationStep)
        {
            separation = separation.normalized * maxSeparationStep;
        }

        transform.position += separation;
    }
}
