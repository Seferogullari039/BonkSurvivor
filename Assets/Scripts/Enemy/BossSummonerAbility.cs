using System.Collections;
using UnityEngine;

public class BossSummonerAbility : MonoBehaviour
{
    private const float SummonInterval = 6f;
    private const float TelegraphDuration = 0.4f;
    private const int SummonCount = 3;
    private const float SummonRadius = 2.5f;

    private static readonly Color RingColor = new Color(0.55f, 0.2f, 0.95f, 0.85f);

    private GameObject enemyPrefab;
    private Renderer bossRenderer;
    private Color normalColor;
    private float summonTimer;
    private bool isTelegraphing;
    private Coroutine telegraphRoutine;

    public void Initialize(GameObject prefab)
    {
        enemyPrefab = prefab;
    }

    private void Awake()
    {
        bossRenderer = GetComponent<Renderer>();
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
        if (enemyPrefab == null || isTelegraphing) return;

        summonTimer += Time.deltaTime;

        if (summonTimer < SummonInterval) return;

        summonTimer = 0f;
        telegraphRoutine = StartCoroutine(SummonTelegraphRoutine());
    }

    private void OnDestroy()
    {
        telegraphRoutine = null;
        isTelegraphing = false;
    }

    private IEnumerator SummonTelegraphRoutine()
    {
        isTelegraphing = true;

        EnemyTelegraphUtility.ApplyFlashColor(bossRenderer, new Color(0.7f, 0.35f, 1f), 0.7f);
        EnemyTelegraphUtility.SpawnRingFlash(transform.position, RingColor, SummonRadius * 0.9f, TelegraphDuration);
        AudioManager.Instance?.PlayTelegraphWarning();

        yield return EnemyTelegraphUtility.WaitSafely(this, TelegraphDuration);

        if (this == null)
        {
            yield break;
        }

        EnemyTelegraphUtility.RestoreColor(bossRenderer, normalColor, 0.88f, true, 0.45f);
        SummonEnemies();

        isTelegraphing = false;
        telegraphRoutine = null;
    }

    private void SummonEnemies()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length >= 80) return;

        for (int i = 0; i < SummonCount; i++)
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length >= 80) break;

            float angle = i * (360f / SummonCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SummonRadius;
            Vector3 spawnPosition = transform.position + offset;
            spawnPosition.y = 0.5f;
            ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
            ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 8f, 1.2f);

            GameObject enemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null) continue;

            enemy.Configure(4f, 3, GameVisualPalette.NormalEnemy, Enemy.EnemyType.Normal);
            enemyObject.transform.localScale = Vector3.one;
        }
    }
}
