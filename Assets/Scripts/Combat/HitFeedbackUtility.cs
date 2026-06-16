using UnityEngine;

public static class HitFeedbackUtility
{
    private const float HitSoundCooldown = 0.04f;

    private static bool assetsResolved;
    private static GameObject deathVfxPrefab;
    private static AudioClip hitClip;
    private static float lastHitSoundTime;
    private static bool warnedDeathVfx;
    private static bool warnedHitSound;

    private static readonly string[] DeathVfxCandidates =
    {
        "Assets/Prefabs/VFX/EnemyDeath.prefab",
        "Assets/Prefabs/VFX/HitImpact.prefab",
        "Assets/Prefabs/VFX/SmallExplosion.prefab",
        "Assets/Prefabs/VFX/Burst.prefab",
        "Assets/Prefabs/VFX/SmokePuff.prefab",
        "Assets/Prefabs/VFX/Poof.prefab",
        "Assets/VFX/EnemyDeath.prefab",
        "Assets/VFX/EnemyHit.prefab",
        "Assets/Particles/EnemyDeath.prefab",
        "Assets/Imported/EnemyDeath.prefab",
        "Assets/AssetStore/EnemyDeath.prefab"
    };

    private static readonly string[] HitClipCandidates =
    {
        "Assets/Audio/SFX/hit.wav",
        "Assets/Audio/SFX/enemy_hit.wav",
        "Assets/Audio/SFX/impact.wav",
        "Assets/Audio/SFX/slash_hit.wav",
        "Assets/Audio/SFX/Weapons/hit.wav",
        "Assets/Audio/SFX/Weapons/enemy_hit.wav"
    };

    public static void TryPlayHitSound()
    {
        if (Time.time - lastHitSoundTime < HitSoundCooldown)
        {
            return;
        }

        EnsureAssetsResolved();

        if (hitClip == null)
        {
            return;
        }

        lastHitSoundTime = Time.time;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(hitClip);
        }
    }

    public static bool TrySpawnDeathVfx(Vector3 position)
    {
        EnsureAssetsResolved();

        if (deathVfxPrefab == null)
        {
            WarnOnce(ref warnedDeathVfx, "death VFX");
            return false;
        }

        GameObject instance = Object.Instantiate(deathVfxPrefab, position, Quaternion.identity);
        PrepareVfxInstance(instance);
        Object.Destroy(instance, 0.5f);
        return true;
    }

    private static void EnsureAssetsResolved()
    {
        if (assetsResolved)
        {
            return;
        }

        assetsResolved = true;

#if UNITY_EDITOR
        deathVfxPrefab = LoadFirstPrefab(DeathVfxCandidates);
        hitClip = LoadFirstClip(HitClipCandidates);
#endif

        if (deathVfxPrefab == null)
        {
            deathVfxPrefab = Resources.Load<GameObject>("VFX/EnemyDeath");
        }

        if (hitClip == null)
        {
            hitClip = Resources.Load<AudioClip>("SFX/hit");
        }
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

    private static void WarnOnce(ref bool warned, string label)
    {
        if (warned)
        {
            return;
        }

        warned = true;
        Debug.LogWarning("[HitFeedback] Optional " + label + " asset not found, skipping.");
    }
}
