using UnityEngine;

[DisallowMultipleComponent]
public class VoidPortalController : MonoBehaviour
{
    private const float OpenDuration = 20f;

    private VoidPortalEventManager eventManager;
    private EnemySpawner enemySpawner;
    private float lifeTimer;
    private int enemiesToSpawn;
    private int enemiesSpawned;
    private float nextSpawnTime;
    private float spawnInterval;
    private bool isClosed;

    public void Initialize(VoidPortalEventManager manager, EnemySpawner spawner)
    {
        eventManager = manager;
        enemySpawner = spawner;
        lifeTimer = 0f;
        isClosed = false;
        enemiesToSpawn = Random.Range(3, 7);
        enemiesSpawned = 0;
        spawnInterval = OpenDuration / Mathf.Max(1, enemiesToSpawn);
        nextSpawnTime = Time.time + 0.45f;

        VoidPortalVisual visual = GetComponent<VoidPortalVisual>();

        if (visual == null)
        {
            visual = gameObject.AddComponent<VoidPortalVisual>();
        }

        visual.BuildVisual();
        VoidPortalFeedback.PlayOpenFeedback(transform.position);
    }

    private void Update()
    {
        if (isClosed) return;

        lifeTimer += Time.deltaTime;

        TrySpawnPortalEnemy();

        if (lifeTimer >= OpenDuration)
        {
            ClosePortal();
        }
    }

    private void TrySpawnPortalEnemy()
    {
        if (enemiesSpawned >= enemiesToSpawn) return;
        if (Time.time < nextSpawnTime) return;
        if (enemySpawner == null) return;

        if (enemySpawner.TrySpawnPortalEnemy(transform.position))
        {
            enemiesSpawned++;

            Vector2 offset = Random.insideUnitCircle * Random.Range(2f, 5f);
            Vector3 flashPosition = transform.position + new Vector3(offset.x, 0.5f, offset.y);
            VoidPortalFeedback.PlayEnemySpawnFlash(flashPosition);
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    private void ClosePortal()
    {
        if (isClosed) return;

        isClosed = true;
        Vector3 closePosition = transform.position;
        VoidPortalFeedback.PlayCloseFeedback(closePosition);
        eventManager?.ShowCloseWarning();
        VoidPortalSpawnTracker.UnregisterActive();
        eventManager?.NotifyPortalClosed(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!isClosed)
        {
            VoidPortalSpawnTracker.UnregisterActive();
            eventManager?.NotifyPortalClosed(this);
        }
    }
}
