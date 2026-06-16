using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public enum EnemyType
    {
        Normal,
        Fast,
        Tank,
        Elite,
        MiniBoss,
        DragonBoss
    }

    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float stopDistanceFromPlayer = 1.2f;
    [SerializeField] private float separationRadius = 1.05f;
    [SerializeField] private float separationStrength = 0.85f;
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float damageCooldown = 1f;
    [SerializeField] private GameObject xpOrbPrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private Renderer enemyRenderer;

    private EnemyType enemyType = EnemyType.Normal;
    private BossAbilityType bossAbility = BossAbilityType.None;
    private int currentHealth;
    private float lastDamageTime = -999f;
    private Transform target;
    private bool movementLocked;
    private Color baseVisualColor = Color.white;
    private float baseVisualSmoothness = 0.42f;
    private bool baseVisualGlow;
    private bool isTelegraphing;
    private Coroutine contactTelegraphRoutine;

    public EnemyType Type => enemyType;
    public bool IsElite { get; private set; }
    public BossAbilityType BossAbility => bossAbility;

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
    }

    private void Awake()
    {
        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponent<Renderer>();
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;

        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            target = player.transform;
        }
    }

    public void Configure(float newMoveSpeed, int newMaxHealth, Color enemyColor, EnemyType type)
    {
        moveSpeed = newMoveSpeed;
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        enemyType = type;
        IsElite = type == EnemyType.Elite;

        if (enemyRenderer != null)
        {
            bool glow = type == EnemyType.Elite || type == EnemyType.MiniBoss || type == EnemyType.DragonBoss;
            float smoothness = type switch
            {
                EnemyType.Elite => 0.88f,
                EnemyType.MiniBoss => 0.78f,
                EnemyType.DragonBoss => 0.72f,
                EnemyType.Fast => 0.48f,
                EnemyType.Tank => 0.38f,
                _ => 0.44f
            };
            GameVisualStyle.ApplyColor(enemyRenderer, enemyColor, smoothness, glow);
            baseVisualColor = enemyColor;
            baseVisualSmoothness = smoothness;
            baseVisualGlow = glow;
        }
    }

    public void SetBossAbility(BossAbilityType ability)
    {
        bossAbility = ability;
    }

    public void ApplyEliteMutation(Color eliteColor)
    {
        if (enemyType == EnemyType.MiniBoss || enemyType == EnemyType.DragonBoss || IsElite)
        {
            return;
        }

        IsElite = true;
        maxHealth = Mathf.Max(1, maxHealth * 2);
        currentHealth = maxHealth;
        enemyType = EnemyType.Elite;

        if (enemyRenderer != null)
        {
            GameVisualStyle.ApplyColor(enemyRenderer, eliteColor, 0.88f, true);
            baseVisualColor = eliteColor;
            baseVisualSmoothness = 0.88f;
            baseVisualGlow = true;
        }
    }

    private void Update()
    {
        if (target == null || movementLocked) return;
        if (enemyType == EnemyType.DragonBoss) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance < 0.01f) return;
        if (distance <= stopDistanceFromPlayer) return;

        Vector3 moveDirection = direction / distance;
        Vector3 separation = GetSeparationOffset();

        if (separation.sqrMagnitude > 0.0001f)
        {
            moveDirection = (moveDirection + separation * separationStrength).normalized;
        }

        float moveStep = Mathf.Min(moveSpeed * Time.deltaTime, distance - stopDistanceFromPlayer);
        transform.position += moveDirection * moveStep;
    }

    private Vector3 GetSeparationOffset()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, separationRadius);
        Vector3 offset = Vector3.zero;
        int neighborCount = 0;

        for (int i = 0; i < nearbyColliders.Length; i++)
        {
            Collider nearbyCollider = nearbyColliders[i];

            if (nearbyCollider == null) continue;

            Enemy nearbyEnemy = nearbyCollider.GetComponent<Enemy>() ?? nearbyCollider.GetComponentInParent<Enemy>();

            if (nearbyEnemy == null || nearbyEnemy == this) continue;

            Vector3 push = transform.position - nearbyEnemy.transform.position;
            push.y = 0f;

            float pushDistance = push.magnitude;

            if (pushDistance < 0.001f)
            {
                push = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                pushDistance = push.magnitude;
            }

            if (pushDistance < 0.001f) continue;

            offset += push.normalized / pushDistance;
            neighborCount++;
        }

        if (neighborCount == 0) return Vector3.zero;

        return offset / neighborCount;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isTelegraphing || contactTelegraphRoutine != null) return;
        if (Time.time < lastDamageTime + damageCooldown) return;

        contactTelegraphRoutine = StartCoroutine(ContactAttackTelegraph(other));
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        CancelContactTelegraph();
    }

    private void CancelContactTelegraph()
    {
        if (contactTelegraphRoutine != null)
        {
            StopCoroutine(contactTelegraphRoutine);
            contactTelegraphRoutine = null;
        }

        isTelegraphing = false;
        RestoreBaseVisual();
    }

    private void OnDestroy()
    {
        contactTelegraphRoutine = null;
        isTelegraphing = false;
    }

    private IEnumerator ContactAttackTelegraph(Collider playerCollider)
    {
        isTelegraphing = true;
        float telegraphDuration = GetContactTelegraphDuration();
        Color telegraphColor = new Color(1f, 0.18f, 0.18f);

        EnemyTelegraphUtility.ApplyFlashColor(enemyRenderer, telegraphColor);
        AudioManager.Instance?.PlayTelegraphWarning();

        yield return EnemyTelegraphUtility.WaitSafely(this, telegraphDuration);

        RestoreBaseVisual();

        if (this == null || playerCollider == null)
        {
            isTelegraphing = false;
            contactTelegraphRoutine = null;
            yield break;
        }

        if (playerCollider.CompareTag("Player"))
        {
            PlayerStats playerStats = playerCollider.GetComponent<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.TakeDamage(contactDamage);
                lastDamageTime = Time.time;
            }
        }

        isTelegraphing = false;
        contactTelegraphRoutine = null;
    }

    private float GetContactTelegraphDuration()
    {
        return enemyType switch
        {
            EnemyType.Fast => 0.15f,
            EnemyType.Tank => 0.4f,
            _ => 0.25f
        };
    }

    private void RestoreBaseVisual()
    {
        EnemyTelegraphUtility.RestoreColor(
            enemyRenderer,
            baseVisualColor,
            baseVisualSmoothness,
            baseVisualGlow
        );
    }

    private Vector3 ResolveHitSource()
    {
        Camera camera = Camera.main;

        if (camera != null)
        {
            return camera.transform.position;
        }

        if (target != null)
        {
            return target.position;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            return player.transform.position;
        }

        return transform.position + transform.forward;
    }

    public void TakeDamage(int damage)
    {
        bool isCrit = damage >= 7 || damage >= currentHealth;

        if (FloatingDamageManager.Instance != null)
        {
            FloatingDamageManager.Instance.SpawnDamage(transform.position + Vector3.up, damage, isCrit);
        }

        EnemyHitFeedback hitFeedback = GetComponent<EnemyHitFeedback>();
        hitFeedback?.PlayHit(ResolveHitSource());

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            CancelContactTelegraph();
            Vector3 dropPosition = transform.position;

            hitFeedback?.PlayDeath(dropPosition);

            if (FPSPlayerController.IsFpsModeActive)
            {
                FPSCrosshair.KillFeedback();
            }

            if (enemyType != EnemyType.Elite
                && enemyType != EnemyType.MiniBoss
                && enemyType != EnemyType.DragonBoss
                && xpOrbPrefab != null
                && LootLimits.CanSpawnXPOrb())
            {
                Instantiate(
                    xpOrbPrefab,
                    dropPosition,
                    Quaternion.identity
                );
            }

            if (enemyType == EnemyType.MiniBoss || enemyType == EnemyType.DragonBoss)
            {
                if (enemyType == EnemyType.DragonBoss)
                {
                    DragonBossController dragon = GetComponent<DragonBossController>();
                    if (dragon != null)
                    {
                        dragon.PlayDeathAudio();
                    }

                    if (JuiceManager.Instance != null)
                    {
                        JuiceManager.Instance.PlayBossSpawn(dropPosition);
                    }
                }

                DropMiniBossRewards(dropPosition);
            }
            else if (enemyType == EnemyType.Elite && IsElite)
            {
                DropEliteRewards(dropPosition);

                if (JuiceManager.Instance != null)
                {
                    JuiceManager.Instance.PlayEliteKill(dropPosition);
                }
            }
            else
            {
                if (coinPrefab != null && LootLimits.CanSpawnCoin())
                {
                    Vector3 coinOffset = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0f,
                        Random.Range(-0.5f, 0.5f)
                    );

                    Instantiate(
                        coinPrefab,
                        dropPosition + coinOffset,
                        Quaternion.identity
                    );
                }

                TryDropChest();
            }

            Destroy(gameObject);
            return;
        }

        if (FPSPlayerController.IsFpsModeActive)
        {
            FPSCrosshair.HitFeedback();
        }
    }

    private void DropEliteRewards(Vector3 dropPosition)
    {
        if (xpOrbPrefab != null && LootLimits.CanSpawnXPOrb())
        {
            Instantiate(
                xpOrbPrefab,
                dropPosition,
                Quaternion.identity
            );
        }

        if (Random.value < 0.5f && xpOrbPrefab != null && LootLimits.CanSpawnXPOrb())
        {
            Vector3 bonusXpOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            );

            Instantiate(
                xpOrbPrefab,
                dropPosition + bonusXpOffset,
                Quaternion.identity
            );
        }

        if (coinPrefab != null && LootLimits.CanSpawnCoin())
        {
            Vector3 coinOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            );

            Instantiate(
                coinPrefab,
                dropPosition + coinOffset,
                Quaternion.identity
            );
        }

        TryDropChest();
    }

    private void DropMiniBossRewards(Vector3 dropPosition)
    {
        Vector3 chestPosition = dropPosition + new Vector3(0f, 0.5f, 1.5f);

        if (coinPrefab != null)
        {
            int coinDropCount = LootLimits.GetBossCoinDropCount(10);

            for (int i = 0; i < coinDropCount; i++)
            {
                Vector2 spreadDirection = Random.insideUnitCircle;

                if (spreadDirection.sqrMagnitude < 0.01f)
                {
                    spreadDirection = Vector2.right;
                }

                spreadDirection = spreadDirection.normalized * Random.Range(2f, 3.5f);

                Vector3 coinPosition = chestPosition + new Vector3(
                    spreadDirection.x,
                    0f,
                    spreadDirection.y
                );

                Instantiate(
                    coinPrefab,
                    coinPosition,
                    Quaternion.identity
                );
            }
        }

        if (!LootLimits.CanSpawnChest())
        {
            return;
        }

        int currentWave = 1;
        EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (enemySpawner != null)
        {
            currentWave = Mathf.Max(1, enemySpawner.CurrentWave);
        }

        SpawnChest(
            chestPosition,
            enemyType == EnemyType.DragonBoss
                ? ChestRarityUtility.RollDragonBossChestRarity(currentWave)
                : ChestRarityUtility.RollBossChestRarity(currentWave),
            enemyType == EnemyType.DragonBoss ? 1.25f : 1.2f,
            true);
    }

    private void SpawnChest(Vector3 position, ChestRarity rarity, float scale = 1f, bool isBossDrop = false)
    {
        GameObject prefab = ChestPrefabUtility.ResolveChestPrefab(rarity);

        if (prefab == null)
        {
            prefab = chestPrefab;
        }

        if (prefab == null)
        {
            Debug.LogWarning("[ChestDrop] Chest prefab is missing, cannot drop chest.");
            return;
        }

        position.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref position);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref position, 6f, 1.2f);

        GameObject chestObject = Instantiate(prefab, position, Quaternion.identity);
        chestObject.transform.localScale = Vector3.one * scale;

        Chest chest = chestObject.GetComponent<Chest>();

        if (chest == null) return;

        chest.Configure(rarity);

        if (isBossDrop)
        {
            Debug.LogWarning("BOSS CHEST DROPPED");
            chest.ApplyBossDropTextColor();
        }
    }

    private void TryDropChest()
    {
        if (!LootLimits.CanSpawnChest()) return;

        float dropChance = enemyType switch
        {
            EnemyType.Fast => 0.005f,
            EnemyType.Tank => 0.08f,
            _ => 0.01f
        };

        if (Random.value > dropChance) return;

        SpawnChest(transform.position, ChestRarityUtility.RollRandomChestRarity());
    }
}
