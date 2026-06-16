using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(AudioSource))]
public class DragonBossController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float approachDistance = 16f;
    [SerializeField] private float attackDistance = 12f;
    [SerializeField] private float stopDistance = 8f;

    [Header("Fire Breath")]
    [SerializeField] private float fireBreathCooldown = 6f;
    [SerializeField] private float fireBreathRange = 13f;
    [SerializeField] private float fireBreathAngle = 38f;
    [SerializeField] private int fireBreathTickDamage = 10;
    [SerializeField] private float fireBreathTickInterval = 0.35f;
    [SerializeField] private float fireBreathDuration = 1.5f;

    [Header("Ground Slam")]
    [SerializeField] private float slamCooldown = 9f;
    [SerializeField] private float slamRadius = 7f;
    [SerializeField] private int slamDamage = 22;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnRoarClip;
    [SerializeField] private AudioClip deathRoarClip;
    [SerializeField] private AudioSource dragonAudioSource;
    [SerializeField] private float roarVolume = 1f;
    [SerializeField] private float deathVolume = 1f;

    private Enemy enemy;
    private Transform player;
    private Transform firePoint;
    private float nextFireTime;
    private float nextSlamTime;
    private bool isAttacking;
    private bool hasPlayedSpawnAudio;
    private bool hasPlayedDeathAudio;
    private int spawnWave = 10;

    public void Initialize(int wave)
    {
        spawnWave = Mathf.Max(10, wave);

        int health = spawnWave >= 30 ? 900 : spawnWave >= 20 ? 500 : 250;
        moveSpeed = spawnWave >= 30 ? 3f : spawnWave >= 20 ? 2.6f : 2.2f;
        fireBreathTickDamage = spawnWave >= 30 ? 12 : spawnWave >= 20 ? 10 : 8;
        slamDamage = spawnWave >= 30 ? 25 : spawnWave >= 20 ? 22 : 18;

        enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        Color dragonColor = new Color(0.48f, 0.1f, 0.24f);
        enemy.Configure(moveSpeed, health, dragonColor, Enemy.EnemyType.DragonBoss);
        enemy.SetMovementLocked(true);
    }

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        EnsureAudioSource();

        DragonBossVisual visual = GetComponent<DragonBossVisual>();

        if (visual != null && visual.MouthFirePoint != null)
        {
            firePoint = visual.MouthFirePoint;
        }
    }

    private void Start()
    {
        GameObject playerObject = GameObject.Find("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        if (firePoint == null)
        {
            DragonBossVisual visual = GetComponent<DragonBossVisual>();

            if (visual != null)
            {
                firePoint = visual.MouthFirePoint;
            }
        }

        if (firePoint == null)
        {
            firePoint = transform;
        }

        enemy.SetMovementLocked(true);
        DragonBossSpawnTracker.RegisterAlive();
        nextFireTime = Time.time + 2f;
        nextSlamTime = Time.time + 4f;
        StartCoroutine(PlaySpawnAudioDelayed());
    }

    private IEnumerator PlaySpawnAudioDelayed()
    {
        yield return new WaitForSeconds(0.15f);
        PlaySpawnAudio();
    }

    public void PlaySpawnAudio()
    {
        if (hasPlayedSpawnAudio)
        {
            return;
        }

        hasPlayedSpawnAudio = true;
        Play2DSound(spawnRoarClip, roarVolume);
    }

    public void PlayDeathAudio()
    {
        if (hasPlayedDeathAudio)
        {
            return;
        }

        hasPlayedDeathAudio = true;
        Play2DSound(deathRoarClip, deathVolume);
    }

    private void Play2DSound(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning("[DragonBoss] Audio clip missing.");
            return;
        }

        GameObject audioObject = new GameObject("DragonBoss_OneShotAudio");
        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Destroy(audioObject, clip.length + 0.25f);

        Debug.Log("[DragonBoss] Played 2D sound: " + clip.name);
    }

    private void OnDestroy()
    {
        DragonBossSpawnTracker.UnregisterAlive();
    }

    private void EnsureAudioSource()
    {
        if (dragonAudioSource == null)
        {
            dragonAudioSource = GetComponent<AudioSource>();
        }

        if (dragonAudioSource == null)
        {
            dragonAudioSource = gameObject.AddComponent<AudioSource>();
        }

        dragonAudioSource.playOnAwake = false;
        dragonAudioSource.loop = false;
        dragonAudioSource.spatialBlend = 1f;
        dragonAudioSource.minDistance = 12f;
        dragonAudioSource.maxDistance = 120f;
        dragonAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private void Update()
    {
        if (player == null || isAttacking) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2.5f);
        }

        if (distance > approachDistance)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }
        else if (distance > stopDistance)
        {
            transform.position += transform.forward * moveSpeed * 0.55f * Time.deltaTime;
        }

        if (distance <= attackDistance && Time.time >= nextFireTime)
        {
            StartCoroutine(FireBreathRoutine());
            return;
        }

        if (distance <= slamRadius + 1.5f && Time.time >= nextSlamTime)
        {
            StartCoroutine(GroundSlamRoutine());
        }
    }

    private IEnumerator FireBreathRoutine()
    {
        isAttacking = true;
        nextFireTime = Time.time + fireBreathCooldown + fireBreathDuration;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position + transform.forward * 2f + Vector3.up;
        GameObject breathVisual = CreateBreathVisual(origin, transform.forward);

        float elapsed = 0f;
        float nextTick = 0f;

        while (elapsed < fireBreathDuration)
        {
            elapsed += Time.deltaTime;

            if (Time.time >= nextTick)
            {
                ApplyFireBreathDamage(origin, transform.forward);
                nextTick = Time.time + fireBreathTickInterval;
            }

            yield return null;
        }

        if (breathVisual != null)
        {
            Destroy(breathVisual);
        }

        isAttacking = false;
    }

    private IEnumerator GroundSlamRoutine()
    {
        isAttacking = true;
        nextSlamTime = Time.time + slamCooldown;

        GameObject shockwave = CreateShockwaveVisual(transform.position);

        yield return new WaitForSeconds(0.35f);

        ApplySlamDamage();

        yield return new WaitForSeconds(0.8f);

        if (shockwave != null)
        {
            Destroy(shockwave);
        }

        isAttacking = false;
    }

    private void ApplyFireBreathDamage(Vector3 origin, Vector3 forward)
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - origin;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > fireBreathRange * fireBreathRange) return;
        if (Vector3.Angle(forward, toPlayer.normalized) > fireBreathAngle * 0.5f) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(fireBreathTickDamage);
        }
    }

    private void ApplySlamDamage()
    {
        if (player == null) return;

        Vector3 flatOffset = player.position - transform.position;
        flatOffset.y = 0f;

        if (flatOffset.sqrMagnitude > slamRadius * slamRadius) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(slamDamage);
        }
    }

    private static GameObject CreateBreathVisual(Vector3 origin, Vector3 forward)
    {
        GameObject breathObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        breathObject.name = "DragonFireBreathFx";
        breathObject.transform.position = origin + forward * 2.5f + Vector3.up * 0.2f;
        breathObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        breathObject.transform.localScale = new Vector3(2.4f, 1.2f, 5f);

        Collider collider = breathObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = breathObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, new Color(1f, 0.35f, 0.08f), 0.25f, true, 0.85f);
        }

        return breathObject;
    }

    private static GameObject CreateShockwaveVisual(Vector3 position)
    {
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "DragonSlamShockwaveFx";
        ringObject.transform.position = position + Vector3.up * 0.08f;
        ringObject.transform.localScale = new Vector3(8f, 0.08f, 8f);

        Collider collider = ringObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = ringObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, new Color(0.85f, 0.2f, 0.95f), 0.35f, true, 0.55f);
        }

        return ringObject;
    }
}
