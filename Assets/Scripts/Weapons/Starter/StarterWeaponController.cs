using System.Collections;
using UnityEngine;

public class StarterWeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform fireCamera;
    [SerializeField] private StarterWeaponViewModel weaponViewModel;

    [Header("Spawn")]
    [SerializeField] private float spawnForwardOffset = 1.1f;
    [SerializeField] private Vector3 spawnLocalOffset = new Vector3(0.08f, -0.05f, 0f);

    [Header("Hunter Bow")]
    [SerializeField] private float bowPrimaryCooldown = 0.55f;
    [SerializeField] private float bowSkillCooldown = 8f;
    [SerializeField] private float bowCritMultiplier = 1.35f;
    [SerializeField] private float arrowRainRadius = 4.5f;
    [SerializeField] private float arrowRainImpactRadius = 1.35f;
    [SerializeField] private float arrowRainMarkerDuration = 1f;

    [Header("Fire Staff")]
    [SerializeField] private float staffPrimaryCooldown = 0.85f;
    [SerializeField] private float staffSkillCooldown = 10f;
    [SerializeField] private float fireballExplosionRadius = 2.5f;
    [SerializeField] private float megaMeteorCastDelay = 0.75f;
    [SerializeField] private float megaMeteorHeight = 22f;
    [SerializeField] private float megaMeteorSpeed = 18f;
    [SerializeField] private float megaMeteorDamageRadius = 6f;
    [SerializeField] private float megaMeteorVisualScale = 0.85f;
    [SerializeField] private float megaMeteorDamageMultiplier = 2f;

    [Header("Knight Sword")]
    [SerializeField] private float swordPrimaryCooldown = 0.42f;
    [SerializeField] private float swordSkillCooldown = 8f;
    [SerializeField] private float meleeRange = 3.5f;
    [SerializeField] private float meleeHalfAngle = 47f;
    [SerializeField] private float meleeSphereRadius = 1.45f;
    [SerializeField] private float whirlwindDuration = 2f;
    [SerializeField] private float whirlwindTickInterval = 0.25f;
    [SerializeField] private float whirlwindRadius = 3.25f;

#if UNITY_EDITOR
    private const bool LogStarterWeaponDebug = true;
#endif

    private PlayerStats playerStats;
    private FPSViewModel fpsViewModel;
    private StarterWeaponType activeWeapon = StarterWeaponType.HunterBow;

    private float nextPrimaryTime;
    private float nextSkillTime;
    private int swordComboIndex;
    private float swordComboResetTime;
    private Coroutine whirlwindRoutine;
    private Coroutine skillRoutine;

    public bool IsHandlingFpsInput =>
        enabled && FPSPlayerController.IsFpsModeActive && MainMenuManager.IsRunActive;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        fpsViewModel = GetComponent<FPSViewModel>();

        if (weaponViewModel == null)
        {
            weaponViewModel = GetComponent<StarterWeaponViewModel>();
        }

        if (weaponViewModel == null)
        {
            weaponViewModel = gameObject.AddComponent<StarterWeaponViewModel>();
        }

        if (fireCamera == null && Camera.main != null)
        {
            fireCamera = Camera.main.transform;
        }
    }

    private void Start()
    {
        weaponViewModel?.PrepareSelectedWeapon(activeWeapon);
    }

    public void RefreshWeaponVisual()
    {
        if (weaponViewModel == null)
        {
            return;
        }

        weaponViewModel.ApplyWeapon(activeWeapon);
    }

    private void Update()
    {
        if (!CanProcessWeaponInput()) return;

        HandleWeaponSwitch();

        if (Input.GetMouseButtonDown(0) && Time.time >= nextPrimaryTime)
        {
            TryPrimaryAttack();
        }

        if (Input.GetMouseButtonDown(1) && Time.time >= nextSkillTime && skillRoutine == null)
        {
            TrySignatureSkill();
        }

        if (swordComboIndex > 0 && Time.time > swordComboResetTime)
        {
            swordComboIndex = 0;
        }
    }

    private bool CanProcessWeaponInput()
    {
        if (!IsHandlingFpsInput) return false;
        if (Time.timeScale <= 0f) return false;

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return false;
        }

        if (playerStats != null && playerStats.IsDead)
        {
            return false;
        }

        return true;
    }

    private void HandleWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ApplyWeapon(StarterWeaponType.HunterBow);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ApplyWeapon(StarterWeaponType.FireStaff);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ApplyWeapon(StarterWeaponType.KnightSword);
        }
    }

    private void ApplyWeapon(StarterWeaponType weaponType)
    {
        if (activeWeapon != weaponType)
        {
            CancelActiveSkillCoroutines();
        }

        activeWeapon = weaponType;
        weaponViewModel?.ApplyWeapon(weaponType);
    }

    private void CancelActiveSkillCoroutines()
    {
        if (skillRoutine != null)
        {
            StopCoroutine(skillRoutine);
            skillRoutine = null;
        }

        if (whirlwindRoutine != null)
        {
            StopCoroutine(whirlwindRoutine);
            whirlwindRoutine = null;
        }
    }

#if UNITY_EDITOR
    private static void LogStarterDebug(string message)
    {
        if (LogStarterWeaponDebug)
        {
            Debug.Log(message);
        }
    }
#else
    private static void LogStarterDebug(string message) { }
#endif

    private void TryPrimaryAttack()
    {
        switch (activeWeapon)
        {
            case StarterWeaponType.HunterBow:
                FireBowArrow(false);
                nextPrimaryTime = Time.time + bowPrimaryCooldown;
                break;
            case StarterWeaponType.FireStaff:
                FireFireball();
                nextPrimaryTime = Time.time + staffPrimaryCooldown;
                break;
            case StarterWeaponType.KnightSword:
                PerformSwordComboHit();
                nextPrimaryTime = Time.time + swordPrimaryCooldown;
                break;
        }
    }

    private void TrySignatureSkill()
    {
        switch (activeWeapon)
        {
            case StarterWeaponType.HunterBow:
                skillRoutine = StartCoroutine(ArrowRainRoutine());
                nextSkillTime = Time.time + bowSkillCooldown;
                break;
            case StarterWeaponType.FireStaff:
                LogStarterDebug("[StarterWeapon] FireStaff RMB Mega Meteor");
                skillRoutine = StartCoroutine(MeteorShowerRoutine());
                nextSkillTime = Time.time + staffSkillCooldown;
                break;
            case StarterWeaponType.KnightSword:
                LogStarterDebug("[StarterWeapon] Sword RMB Whirlwind");

                if (whirlwindRoutine != null)
                {
                    StopCoroutine(whirlwindRoutine);
                }

                whirlwindRoutine = StartCoroutine(WhirlwindRoutine());
                nextSkillTime = Time.time + swordSkillCooldown;
                break;
        }
    }

    private void FireBowArrow(bool forceCrit)
    {
        if (!TryGetSpawnData(out Vector3 spawnPosition, out Vector3 direction)) return;

        int damage = StarterWeaponDamageUtility.GetBaseDamage(playerStats);

        if (forceCrit || FPSAimUtility.IsEnemyInCrosshair())
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * bowCritMultiplier));
        }

        fpsViewModel?.PlayRecoil();
        weaponViewModel?.PlayBowStringKick();

        StarterWeaponProjectile.SpawnRuntimeProjectile(
            spawnPosition,
            direction,
            new Color(0.75f, 0.55f, 0.25f),
            0.12f,
            GetStarterProjectileSpeed(28f),
            damage,
            3f,
            false,
            0f);
    }

    private void FireFireball()
    {
        if (!TryGetFireballSpawnData(out Vector3 spawnPosition, out Vector3 direction)) return;

        int damage = StarterWeaponDamageUtility.GetBaseDamage(playerStats);
        fpsViewModel?.PlayRecoil();
        weaponViewModel?.PlayStaffGlowPulse();

        Quaternion castRotation = Quaternion.LookRotation(direction);

        if (weaponViewModel != null && weaponViewModel.FireballSpawnPoint != null)
        {
            Transform spawnPoint = weaponViewModel.FireballSpawnPoint;
            castRotation = spawnPoint.rotation;
            FireStaffPolish.TrySpawnFireCastVfx(spawnPoint.position, castRotation);
        }
        else
        {
            FireStaffPolish.TrySpawnFireCastVfx(spawnPosition, castRotation);
        }

        FireStaffPolish.TryPlayFireCastSound();

        StarterWeaponProjectile.SpawnRuntimeProjectile(
            spawnPosition,
            direction,
            new Color(1f, 0.45f, 0.08f),
            0.18f,
            GetStarterProjectileSpeed(16f),
            damage,
            4f,
            false,
            fireballExplosionRadius,
            useFireStaffPolish: true);
    }

    private float GetStarterProjectileSpeed(float baseSpeed)
    {
        if (playerStats == null)
        {
            return baseSpeed;
        }

        return baseSpeed * playerStats.StarterProjectileSpeedMultiplier;
    }

    private void PerformSwordComboHit()
    {
        if (fireCamera == null) return;

        int baseDamage = StarterWeaponDamageUtility.GetBaseDamage(playerStats);
        float comboMultiplier = 1f + swordComboIndex * 0.12f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * comboMultiplier));

        Vector3 origin = fireCamera.position;
        Vector3 forward = fireCamera.forward;

        int candidateCount;
        bool hitEnemy = StarterWeaponDamageUtility.TryDamageSingleMeleeTarget(
            fireCamera,
            meleeRange,
            0.35f,
            damage,
            out candidateCount);

        LogStarterDebug($"[StarterWeapon] Sword LMB Slash hit={hitEnemy} dmg={damage}");
        SpawnSwordSwingVisual(origin, forward, meleeRange, meleeHalfAngle);
        fpsViewModel?.PlayRecoil();

        swordComboIndex = (swordComboIndex + 1) % 3;
        swordComboResetTime = Time.time + 0.85f;
    }

    private IEnumerator ArrowRainRoutine()
    {
        if (!StarterWeaponDamageUtility.TryGetAimGroundPoint(out Vector3 targetPoint))
        {
            skillRoutine = null;
            yield break;
        }

        const int arrowCount = 18;
        const float rainHeight = 16f;
        const float arrowFallSpeed = 10f;
        int baseDamage = StarterWeaponDamageUtility.GetBaseDamage(playerStats);
        int rainDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.85f));
        int openingTickDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.45f));

        ArrowRainTargetRing.Spawn(targetPoint, arrowRainRadius, arrowRainMarkerDuration);
        StarterWeaponDamageUtility.DamageEnemiesInRadius(targetPoint, arrowRainRadius, openingTickDamage);
        fpsViewModel?.PlayFireGlow();

        for (int i = 0; i < arrowCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * arrowRainRadius;
            Vector3 impactPoint = targetPoint + new Vector3(offset.x, 0f, offset.y);
            Vector3 spawnPoint = impactPoint + Vector3.up * rainHeight;
            Vector3 direction = (impactPoint - spawnPoint).normalized;

            StarterWeaponProjectile.SpawnRuntimeProjectile(
                spawnPoint,
                direction,
                new Color(0.95f, 0.86f, 0.5f),
                0.12f,
                arrowFallSpeed,
                rainDamage,
                3.5f,
                false,
                0f,
                arrowRainImpactRadius,
                useArrowVisual: true,
                useTrailVisual: true,
                showImpactEffect: true);

            yield return new WaitForSeconds(0.14f);
        }

        skillRoutine = null;
    }

    private IEnumerator MeteorShowerRoutine()
    {
        if (!StarterWeaponDamageUtility.TryGetSkillTargetPoint(fireCamera, out Vector3 targetPoint))
        {
            skillRoutine = null;
            yield break;
        }

        int baseDamage = StarterWeaponDamageUtility.GetBaseDamage(playerStats);
        int megaDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * megaMeteorDamageMultiplier));

        ArrowRainTargetRing.Spawn(targetPoint, megaMeteorDamageRadius, megaMeteorCastDelay + 0.15f);
        weaponViewModel?.PlayStaffChargeGlow(megaMeteorCastDelay);
        fpsViewModel?.PlayFireGlow();
        FireStaffPolish.TryPlayMeteorCastSound();

        if (weaponViewModel != null && weaponViewModel.MeteorCastPoint != null)
        {
            Transform castPoint = weaponViewModel.MeteorCastPoint;
            FireStaffPolish.TrySpawnMeteorChargeVfx(castPoint.position, castPoint.rotation);
        }
        else
        {
            FireStaffPolish.TrySpawnMeteorChargeVfx(targetPoint + Vector3.up * 0.5f, Quaternion.identity);
        }

        yield return new WaitForSeconds(megaMeteorCastDelay);

        Vector3 spawnPoint = targetPoint + Vector3.up * megaMeteorHeight;
        Vector3 direction = (targetPoint - spawnPoint).normalized;

        FPSScreenShake.Shake(0.03f, 0.1f);

        StarterWeaponProjectile.SpawnRuntimeProjectile(
            spawnPoint,
            direction,
            new Color(1f, 0.42f, 0.08f),
            megaMeteorVisualScale,
            megaMeteorSpeed,
            megaDamage,
            4f,
            false,
            0f,
            megaMeteorDamageRadius,
            useArrowVisual: false,
            useTrailVisual: true,
            showImpactEffect: false,
            useMegaMeteorVisual: true,
            useMegaMeteorImpact: true);

        skillRoutine = null;
    }

    private IEnumerator WhirlwindRoutine()
    {
        int baseDamage = StarterWeaponDamageUtility.GetBaseDamage(playerStats);
        int tickDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.75f));
        float elapsed = 0f;
        GameObject whirlwindVisual = SpawnWhirlwindVisual(transform.position, whirlwindRadius);

        while (elapsed < whirlwindDuration)
        {
            Vector3 center = transform.position + Vector3.up * 0.5f;
            int hitCount = StarterWeaponDamageUtility.DamageEnemiesInRadiusWithCount(center, whirlwindRadius, tickDamage);
            LogStarterDebug($"[StarterWeapon] Sword RMB Whirlwind tick hits={hitCount} dmg={tickDamage}");
            fpsViewModel?.PlayFireGlow();

            yield return new WaitForSeconds(whirlwindTickInterval);
            elapsed += whirlwindTickInterval;
        }

        if (whirlwindVisual != null)
        {
            Object.Destroy(whirlwindVisual);
        }

        whirlwindRoutine = null;
    }

    private static void SpawnSwordSwingVisual(Vector3 origin, Vector3 forward, float range, float halfAngle)
    {
        if (forward.sqrMagnitude < 0.001f) return;

        forward.Normalize();

        GameObject swingObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        swingObject.name = "SwordSwingArc";
        swingObject.transform.position = origin + forward * (range * 0.45f) + Vector3.down * 0.75f;
        swingObject.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, halfAngle * 0.5f, 90f);
        swingObject.transform.localScale = new Vector3(range * 0.9f, 0.01f, range * 0.45f);

        Collider swingCollider = swingObject.GetComponent<Collider>();

        if (swingCollider != null)
        {
            Object.Destroy(swingCollider);
        }

        Renderer swingRenderer = swingObject.GetComponent<Renderer>();

        if (swingRenderer != null)
        {
            GameVisualStyle.ApplyColor(swingRenderer, new Color(0.85f, 0.9f, 1f), 0.2f, true, 0.35f);
        }

        Object.Destroy(swingObject, 0.22f);
    }

    private static GameObject SpawnWhirlwindVisual(Vector3 playerPosition, float radius)
    {
        GameObject ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringObject.name = "WhirlwindRing";
        ringObject.transform.position = playerPosition + Vector3.up * 0.08f;
        ringObject.transform.localScale = new Vector3(radius * 2f, 0.006f, radius * 2f);

        Collider ringCollider = ringObject.GetComponent<Collider>();

        if (ringCollider != null)
        {
            Object.Destroy(ringCollider);
        }

        Renderer ringRenderer = ringObject.GetComponent<Renderer>();

        if (ringRenderer != null)
        {
            GameVisualStyle.ApplyColor(ringRenderer, new Color(0.75f, 0.85f, 1f), 0.15f, true, 0.25f);
        }

        WhirlwindRingSpin spin = ringObject.AddComponent<WhirlwindRingSpin>();
        spin.Initialize(2f);

        Object.Destroy(ringObject, 2.05f);
        return ringObject;
    }

    private bool TryGetFireballSpawnData(out Vector3 spawnPosition, out Vector3 direction)
    {
        spawnPosition = Vector3.zero;
        direction = Vector3.forward;

        if (fireCamera == null) return false;
        if (!FPSAimUtility.TryGetAimDirection(out direction)) return false;

        if (weaponViewModel != null && weaponViewModel.TryGetFireballSpawnPosition(out spawnPosition))
        {
            return true;
        }

        spawnPosition = fireCamera.position
            + direction * spawnForwardOffset
            + fireCamera.right * spawnLocalOffset.x
            + fireCamera.up * spawnLocalOffset.y
            + fireCamera.forward * spawnLocalOffset.z;

        return true;
    }

    private bool TryGetSpawnData(out Vector3 spawnPosition, out Vector3 direction)
    {
        spawnPosition = Vector3.zero;
        direction = Vector3.forward;

        if (fireCamera == null) return false;
        if (!FPSAimUtility.TryGetAimDirection(out direction)) return false;

        spawnPosition = fireCamera.position
            + direction * spawnForwardOffset
            + fireCamera.right * spawnLocalOffset.x
            + fireCamera.up * spawnLocalOffset.y
            + fireCamera.forward * spawnLocalOffset.z;

        return true;
    }
}
