using UnityEngine;

public class FPSWeaponController : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform fireCamera;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private float spawnForwardOffset = 1.2f;
    [SerializeField] private Vector3 spawnLocalOffset = new Vector3(0.08f, -0.05f, 0f);

    private PlayerStats playerStats;
    private FPSViewModel fpsViewModel;
    private StarterWeaponController starterWeaponController;
    private float nextFireTime;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
        fpsViewModel = GetComponent<FPSViewModel>();
        starterWeaponController = GetComponent<StarterWeaponController>();

        if (fireCamera == null && Camera.main != null)
        {
            fireCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (!MainMenuManager.IsRunActive || Time.timeScale <= 0f) return;
        if (starterWeaponController != null && starterWeaponController.IsHandlingFpsInput) return;
        if (projectilePrefab == null || fireCamera == null) return;
        if (!Input.GetMouseButton(0)) return;
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;
        fpsViewModel?.PlayRecoil();
        Fire();
    }

    private void Fire()
    {
        if (!FPSAimUtility.TryGetAimDirection(out Vector3 direction)) return;

        if (playerStats != null && playerStats.SpreadShotUnlocked)
        {
            float angle = playerStats.SpreadAngle;
            SpawnProjectile(direction);
            SpawnProjectile(RotateDirection(direction, -angle));
            SpawnProjectile(RotateDirection(direction, angle));
            return;
        }

        SpawnProjectile(direction);
    }

    private static Vector3 RotateDirection(Vector3 direction, float degrees)
    {
        return (Quaternion.Euler(0f, degrees, 0f) * direction).normalized;
    }

    private void SpawnProjectile(Vector3 direction)
    {
        Vector3 spawnPosition = fireCamera.position
            + direction * spawnForwardOffset
            + fireCamera.right * spawnLocalOffset.x
            + fireCamera.up * spawnLocalOffset.y
            + fireCamera.forward * spawnLocalOffset.z;
        GameObject projectileObject = Object.Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(direction)
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(direction);
        }
    }
}
