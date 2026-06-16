using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private GameObject laserBeamPrefab;
    [SerializeField] private float startingFireRate = 1f;
    [SerializeField] private float startingProjectileSpeed = 12f;
    [SerializeField] private AudioClip rocketExplosionClip;
    [SerializeField] private AudioClip laserFireClip;

    public static AudioClip DefaultRocketExplosionClip { get; private set; }
    public static AudioClip DefaultLaserFireClip { get; private set; }

    private readonly List<WeaponBase> activeWeapons = new List<WeaponBase>();
    private ProjectileWeapon basicProjectileWeapon;
    private OrbitWeapon orbitWeapon;

    private void Awake()
    {
        EnsureWeaponClipsAssigned();
        ResolveLaserAudioFromPrefab();
        DefaultRocketExplosionClip = rocketExplosionClip;
        DefaultLaserFireClip = ResolveLaserFireClipForWeapon();
    }

    private void EnsureWeaponClipsAssigned()
    {
#if UNITY_EDITOR
        if (rocketExplosionClip == null)
        {
            rocketExplosionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/SFX/rocket_explosion.wav");

            if (rocketExplosionClip == null)
            {
                rocketExplosionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/SFX/Weapons/rocket_explosion.wav");
            }
        }

        if (laserFireClip == null)
        {
            laserFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/SFX/laser_fire.wav");

            if (laserFireClip == null)
            {
                laserFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Audio/SFX/Weapons/laser_fire.wav");
            }
        }
#endif
    }

    private void ResolveLaserAudioFromPrefab()
    {
        if (laserBeamPrefab == null) return;

        LaserBeamWeaponProfile profile = laserBeamPrefab.GetComponent<LaserBeamWeaponProfile>();

        if (profile == null) return;

        if (laserFireClip == null)
        {
            laserFireClip = profile.LaserFireClip;
        }
    }

    private AudioClip ResolveLaserFireClipForWeapon()
    {
        if (laserFireClip != null) return laserFireClip;

        if (laserBeamPrefab != null)
        {
            LaserBeamWeaponProfile profile = laserBeamPrefab.GetComponent<LaserBeamWeaponProfile>();

            if (profile != null && profile.LaserFireClip != null)
            {
                return profile.LaserFireClip;
            }
        }

        return null;
    }

    private float GetLaserVolumeFromPrefab()
    {
        if (laserBeamPrefab == null) return 1.1f;

        LaserBeamWeaponProfile profile = laserBeamPrefab.GetComponent<LaserBeamWeaponProfile>();

        return profile != null ? profile.LaserVolume : 1.1f;
    }

    private void Start()
    {
        PlayerStats playerStats = GetComponent<PlayerStats>();

        basicProjectileWeapon = new ProjectileWeapon();
        basicProjectileWeapon.Configure(
            projectilePrefab,
            startingFireRate,
            transform,
            startingProjectileSpeed
        );
        basicProjectileWeapon.Init(playerStats);

        activeWeapons.Add(basicProjectileWeapon);

        orbitWeapon = new OrbitWeapon();
        orbitWeapon.Configure(transform);
        orbitWeapon.Init(playerStats);
        activeWeapons.Add(orbitWeapon);

        RocketLauncherWeapon rocketWeapon = new RocketLauncherWeapon();
        rocketWeapon.Configure(transform, rocketExplosionClip, rocketPrefab);
        rocketWeapon.Init(playerStats);
        activeWeapons.Add(rocketWeapon);

        ChainLightningWeapon chainLightningWeapon = new ChainLightningWeapon();
        chainLightningWeapon.Configure(transform);
        chainLightningWeapon.Init(playerStats);
        activeWeapons.Add(chainLightningWeapon);

        LaserBeamWeapon laserBeamWeapon = new LaserBeamWeapon();
        laserBeamWeapon.Configure(transform);
        laserBeamWeapon.SetAudioClip(ResolveLaserFireClipForWeapon(), GetLaserVolumeFromPrefab());
        laserBeamWeapon.Init(playerStats);
        activeWeapons.Add(laserBeamWeapon);
    }

    private void Update()
    {
        if (!MainMenuManager.IsRunActive) return;

        for (int i = 0; i < activeWeapons.Count; i++)
        {
            activeWeapons[i].Tick();
        }
    }

    public void IncreaseFireRate(float percent)
    {
        if (basicProjectileWeapon == null) return;

        basicProjectileWeapon.IncreaseFireRate(percent);
    }

    public void RefreshOrbitWeapon()
    {
        if (orbitWeapon == null) return;

        orbitWeapon.RefreshOrbs();
    }

    public void ResetRunWeapons()
    {
        if (basicProjectileWeapon != null)
        {
            basicProjectileWeapon.ResetFireRate(startingFireRate);
        }

        RefreshOrbitWeapon();
    }
}
