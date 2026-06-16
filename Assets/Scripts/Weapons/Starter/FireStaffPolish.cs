using UnityEngine;

public static class FireStaffPolish
{
    private static bool assetsResolved;
    private static GameObject fireCastVfxPrefab;
    private static GameObject fireballTrailPrefab;
    private static GameObject fireImpactVfxPrefab;
    private static GameObject meteorChargeVfxPrefab;
    private static GameObject meteorTrailPrefab;
    private static GameObject meteorImpactVfxPrefab;

    private static AudioClip fireCastClip;
    private static AudioClip fireImpactClip;
    private static AudioClip meteorCastClip;
    private static AudioClip meteorImpactClip;

    private static bool warnedCastVfx;
    private static bool warnedTrailVfx;
    private static bool warnedImpactVfx;
    private static bool warnedChargeVfx;
    private static bool warnedMeteorImpactVfx;

    private static readonly string[] FireCastVfxCandidates =
    {
        "Assets/Prefabs/VFX/FireCast.prefab",
        "Assets/Prefabs/VFX/MuzzleFlash.prefab",
        "Assets/Prefabs/VFX/FlameBurst.prefab",
        "Assets/Prefabs/VFX/MagicCast.prefab",
        "Assets/VFX/FireCast.prefab",
        "Assets/VFX/MuzzleFlash.prefab",
        "Assets/Particles/FireCast.prefab",
        "Assets/Particles/MuzzleFlash.prefab"
    };

    private static readonly string[] FireballTrailCandidates =
    {
        "Assets/Prefabs/VFX/FireballTrail.prefab",
        "Assets/Prefabs/VFX/FireTrail.prefab",
        "Assets/VFX/FireballTrail.prefab",
        "Assets/Particles/FireballTrail.prefab"
    };

    private static readonly string[] FireImpactVfxCandidates =
    {
        "Assets/Prefabs/VFX/FireImpact.prefab",
        "Assets/Prefabs/VFX/FireballImpact.prefab",
        "Assets/Prefabs/VFX/Explosion.prefab",
        "Assets/VFX/FireImpact.prefab",
        "Assets/Particles/FireImpact.prefab"
    };

    private static readonly string[] MeteorChargeVfxCandidates =
    {
        "Assets/Prefabs/VFX/MeteorCharge.prefab",
        "Assets/Prefabs/VFX/MagicCast.prefab",
        "Assets/Prefabs/VFX/StaffGlow.prefab",
        "Assets/VFX/MeteorCharge.prefab"
    };

    private static readonly string[] MeteorTrailCandidates =
    {
        "Assets/Prefabs/VFX/MeteorTrail.prefab",
        "Assets/Prefabs/VFX/FireballTrail.prefab",
        "Assets/VFX/MeteorTrail.prefab"
    };

    private static readonly string[] MeteorImpactVfxCandidates =
    {
        "Assets/Prefabs/VFX/MeteorImpact.prefab",
        "Assets/Prefabs/VFX/Explosion.prefab",
        "Assets/VFX/MeteorImpact.prefab",
        "Assets/Particles/MeteorImpact.prefab"
    };

    private static readonly string[] FireCastClipCandidates =
    {
        "Assets/Audio/SFX/fire_staff_cast.wav",
        "Assets/Audio/SFX/Weapons/fire_staff_cast.wav",
        "Assets/Audio/SFX/magic_fire_loop.wav",
        "Assets/Audio/SFX/laser_fire.wav",
        "Assets/Audio/SFX/Weapons/laser_fire.wav"
    };

    private static readonly string[] FireImpactClipCandidates =
    {
        "Assets/Audio/SFX/fireball_impact.wav",
        "Assets/Audio/SFX/Weapons/fireball_impact.wav",
        "Assets/Audio/SFX/rocket_explosion.wav",
        "Assets/Audio/SFX/Weapons/rocket_explosion.wav"
    };

    private static readonly string[] MeteorCastClipCandidates =
    {
        "Assets/Audio/SFX/meteor_cast.wav",
        "Assets/Audio/SFX/Weapons/meteor_cast.wav",
        "Assets/Audio/SFX/laser_fire.wav",
        "Assets/Audio/SFX/Weapons/laser_fire.wav"
    };

    private static readonly string[] MeteorImpactClipCandidates =
    {
        "Assets/Audio/SFX/meteor_impact.wav",
        "Assets/Audio/SFX/Weapons/meteor_impact.wav",
        "Assets/Audio/SFX/rocket_explosion.wav",
        "Assets/Audio/SFX/Weapons/rocket_explosion.wav"
    };

    public static void TryPlayFireCastSound()
    {
        EnsureAssetsResolved();
        TryPlayClip(fireCastClip, 0.72f);
    }

    public static void TryPlayFireImpactSound(Vector3 position)
    {
        EnsureAssetsResolved();
        TryPlayClipAt(position, fireImpactClip, 0.78f, 0.35f);
    }

    public static void TryPlayMeteorCastSound()
    {
        EnsureAssetsResolved();
        TryPlayClip(meteorCastClip, 0.82f);
    }

    public static void TryPlayMeteorImpactSound(Vector3 position)
    {
        EnsureAssetsResolved();
        TryPlayClipAt(position, meteorImpactClip, 0.95f, 0.45f);
    }

    public static void TrySpawnFireCastVfx(Vector3 position, Quaternion rotation)
    {
        EnsureAssetsResolved();

        if (fireCastVfxPrefab == null)
        {
            WarnOnce(ref warnedCastVfx, "cast VFX");
            return;
        }

        SpawnDetachedVfx(fireCastVfxPrefab, position, rotation, 2f);
    }

    public static void TrySpawnMeteorChargeVfx(Vector3 position, Quaternion rotation)
    {
        EnsureAssetsResolved();

        if (meteorChargeVfxPrefab == null)
        {
            WarnOnce(ref warnedChargeVfx, "meteor charge VFX");
            return;
        }

        SpawnDetachedVfx(meteorChargeVfxPrefab, position, rotation, MeteorChargeVfxLifetime);
    }

    public static void TryAttachFireballTrail(Transform projectileTransform)
    {
        if (projectileTransform == null)
        {
            return;
        }

        EnsureAssetsResolved();

        if (fireballTrailPrefab == null)
        {
            WarnOnce(ref warnedTrailVfx, "fireball trail VFX");
            return;
        }

        AttachChildVfx(fireballTrailPrefab, projectileTransform);
    }

    public static void TryAttachMeteorTrail(Transform projectileTransform)
    {
        if (projectileTransform == null)
        {
            return;
        }

        EnsureAssetsResolved();

        GameObject trailPrefab = meteorTrailPrefab != null ? meteorTrailPrefab : fireballTrailPrefab;

        if (trailPrefab == null)
        {
            return;
        }

        AttachChildVfx(trailPrefab, projectileTransform);
    }

    public static bool TrySpawnFireImpactVfx(Vector3 impactPoint, float radius)
    {
        EnsureAssetsResolved();

        if (fireImpactVfxPrefab == null)
        {
            WarnOnce(ref warnedImpactVfx, "fire impact VFX");
            return false;
        }

        SpawnImpactVfx(fireImpactVfxPrefab, impactPoint, radius, 2.5f);
        return true;
    }

    public static bool TrySpawnMeteorImpactVfx(Vector3 impactPoint, float radius)
    {
        EnsureAssetsResolved();

        if (meteorImpactVfxPrefab == null)
        {
            WarnOnce(ref warnedMeteorImpactVfx, "meteor impact VFX");
            return false;
        }

        SpawnImpactVfx(meteorImpactVfxPrefab, impactPoint, radius, 3.5f);
        return true;
    }

    private const float MeteorChargeVfxLifetime = 1.35f;

    private static void EnsureAssetsResolved()
    {
        if (assetsResolved)
        {
            return;
        }

        assetsResolved = true;

#if UNITY_EDITOR
        fireCastVfxPrefab = LoadFirstPrefab(FireCastVfxCandidates);
        fireballTrailPrefab = LoadFirstPrefab(FireballTrailCandidates);
        fireImpactVfxPrefab = LoadFirstPrefab(FireImpactVfxCandidates);
        meteorChargeVfxPrefab = LoadFirstPrefab(MeteorChargeVfxCandidates);
        meteorTrailPrefab = LoadFirstPrefab(MeteorTrailCandidates);
        meteorImpactVfxPrefab = LoadFirstPrefab(MeteorImpactVfxCandidates);

        fireCastClip = LoadFirstClip(FireCastClipCandidates);
        fireImpactClip = LoadFirstClip(FireImpactClipCandidates);
        meteorCastClip = LoadFirstClip(MeteorCastClipCandidates);
        meteorImpactClip = LoadFirstClip(MeteorImpactClipCandidates);
#endif

        fireCastClip ??= Resources.Load<AudioClip>("SFX/fire_staff_cast");
        fireImpactClip ??= Resources.Load<AudioClip>("SFX/fireball_impact");
        meteorCastClip ??= Resources.Load<AudioClip>("SFX/meteor_cast");
        meteorImpactClip ??= Resources.Load<AudioClip>("SFX/meteor_impact");

        fireCastClip ??= WeaponManager.DefaultLaserFireClip;
        fireImpactClip ??= WeaponManager.DefaultRocketExplosionClip;
        meteorCastClip ??= WeaponManager.DefaultLaserFireClip;
        meteorImpactClip ??= WeaponManager.DefaultRocketExplosionClip;
    }

#if UNITY_EDITOR
    private static GameObject LoadFirstPrefab(string[] assetPaths)
    {
        for (int i = 0; i < assetPaths.Length; i++)
        {
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[i]);

            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private static AudioClip LoadFirstClip(string[] assetPaths)
    {
        for (int i = 0; i < assetPaths.Length; i++)
        {
            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPaths[i]);

            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }
#endif

    private static void SpawnDetachedVfx(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject instance = Object.Instantiate(prefab, position, rotation);
        PrepareVfxInstance(instance);
        Object.Destroy(instance, lifetime);
    }

    private static void SpawnImpactVfx(GameObject prefab, Vector3 impactPoint, float radius, float lifetime)
    {
        GameObject instance = Object.Instantiate(prefab, impactPoint, Quaternion.identity);
        PrepareVfxInstance(instance);
        float scale = Mathf.Max(0.6f, radius * 0.35f);
        instance.transform.localScale = Vector3.one * scale;
        Object.Destroy(instance, lifetime);
    }

    private static void AttachChildVfx(GameObject prefab, Transform parent)
    {
        GameObject instance = Object.Instantiate(prefab, parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        PrepareVfxInstance(instance);
    }

    private static void PrepareVfxInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];

            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }
    }

    private static void TryPlayClip(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(clip);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, volume);
    }

    private static void TryPlayClipAt(Vector3 position, AudioClip clip, float volume, float spatialBlend)
    {
        if (clip == null)
        {
            return;
        }

        if (AudioManager.Instance != null && spatialBlend <= 0.01f)
        {
            AudioManager.Instance.PlaySfx(clip);
            return;
        }

        GameObject audioObject = new GameObject("FireStaffSfx");
        audioObject.transform.position = position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.minDistance = 4f;
        source.maxDistance = 40f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Object.Destroy(audioObject, clip.length + 0.15f);
    }

    private static void WarnOnce(ref bool warned, string label)
    {
        if (warned)
        {
            return;
        }

        warned = true;
        Debug.LogWarning("[FireStaffPolish] Optional " + label + " asset not found, skipping.");
    }
}
