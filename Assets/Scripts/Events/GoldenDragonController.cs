using UnityEngine;

[DisallowMultipleComponent]
public class GoldenDragonController : MonoBehaviour
{
    private static readonly Color HitProxyColor = new Color(0.92f, 0.72f, 0.14f);

    private GoldenDragonEventManager eventManager;
    private Transform playerTarget;
    private GameObject hitProxy;
    private int maxHealth;
    private int currentHealth;
    private int spawnWave;
    private float lifeTimer;
    private float lifeDuration = 55f;
    private float fleeSpeed = 4.1f;
    private float driftSpeed = 2.4f;
    private bool isResolved;

    public int CurrentHealth => currentHealth;

    public void Initialize(GoldenDragonEventManager manager, int wave)
    {
        eventManager = manager;
        spawnWave = Mathf.Max(6, wave);
        maxHealth = 70 + spawnWave * 12;
        currentHealth = maxHealth;
        lifeDuration = 55f;
        lifeTimer = 0f;
        isResolved = false;

        GoldenDragonVisual visual = GetComponent<GoldenDragonVisual>();

        if (visual == null)
        {
            visual = gameObject.AddComponent<GoldenDragonVisual>();
        }

        visual.BuildVisual();
        EnsureHitProxy();
        CachePlayerTarget();
    }

    private void Update()
    {
        if (isResolved) return;

        lifeTimer += Time.deltaTime;

        if (lifeTimer >= lifeDuration)
        {
            ResolveEscape();
            return;
        }

        UpdateFleeMovement();
    }

    public void TakeDamage(int damage)
    {
        if (isResolved || damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth > 0) return;

        ResolveKill();
    }

    private void UpdateFleeMovement()
    {
        if (playerTarget == null)
        {
            CachePlayerTarget();
        }

        if (playerTarget == null) return;

        Vector3 awayDirection = transform.position - playerTarget.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.0001f)
        {
            awayDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        }

        awayDirection.Normalize();

        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(playerTarget.position.x, 0f, playerTarget.position.z));

        float speed = distance < 18f ? fleeSpeed : driftSpeed;
        Vector3 nextPosition = transform.position + awayDirection * speed * Time.deltaTime;
        nextPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref nextPosition);
        transform.position = nextPosition;
    }

    private void ResolveKill()
    {
        if (isResolved) return;

        isResolved = true;
        GrantRewards();
        GoldenDragonSpawnTracker.UnregisterActive();
        eventManager?.NotifyDragonResolved(this);

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayEliteKill(transform.position);
        }

        Destroy(gameObject);
    }

    private void ResolveEscape()
    {
        if (isResolved) return;

        isResolved = true;
        GoldenDragonSpawnTracker.UnregisterActive();
        eventManager?.NotifyDragonResolved(this);
        Destroy(gameObject);
    }

    private void GrantRewards()
    {
        int coinReward = 120 + spawnWave * 30;
        int xpReward = 35 + spawnWave * 8;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        PlayerStats playerStats = playerObject != null ? playerObject.GetComponent<PlayerStats>() : null;

        if (playerStats != null)
        {
            playerStats.AddCoins(coinReward);
            playerStats.AddXP(xpReward);
        }
    }

    private void EnsureHitProxy()
    {
        if (hitProxy != null) return;

        hitProxy = new GameObject("GoldenDragonHitProxy");
        hitProxy.transform.SetParent(transform, false);
        hitProxy.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        hitProxy.tag = "Enemy";

        CapsuleCollider collider = hitProxy.AddComponent<CapsuleCollider>();
        collider.isTrigger = false;
        collider.radius = 0.95f;
        collider.height = 2.2f;
        collider.center = Vector3.zero;

        Enemy proxyEnemy = hitProxy.AddComponent<Enemy>();
        proxyEnemy.BindGoldenDragon(this);
        proxyEnemy.SetMovementLocked(true);
        proxyEnemy.Configure(0f, maxHealth, HitProxyColor, Enemy.EnemyType.Normal);
    }

    private void CachePlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void OnDestroy()
    {
        if (!isResolved)
        {
            GoldenDragonSpawnTracker.UnregisterActive();
            eventManager?.NotifyDragonResolved(this);
        }
    }
}
