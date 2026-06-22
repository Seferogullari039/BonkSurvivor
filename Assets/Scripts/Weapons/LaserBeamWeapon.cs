using UnityEngine;

public class LaserBeamWeapon : WeaponBase
{
    private const float DamageInterval = 0.18f;
    private const float LaserSpatialBlend = 0.3f;

    private AudioClip laserFireClip;
    private float laserVolume = 1.1f;
    private static AudioClip cachedLaserFirePlaceholder;

    private Transform fireOrigin;
    private float damageTimer;

    public void Configure(Transform origin, AudioClip fireClip = null, float volume = 1.1f)
    {
        fireOrigin = origin;
        SetAudioClip(fireClip, volume);
    }

    public void SetAudioClip(AudioClip clip, float volume = -1f)
    {
        if (clip != null)
        {
            laserFireClip = clip;
        }
        else if (laserFireClip == null && WeaponManager.DefaultLaserFireClip != null)
        {
            laserFireClip = WeaponManager.DefaultLaserFireClip;
        }

        if (volume > 0f)
        {
            laserVolume = volume;
        }
    }

    public override void Tick()
    {
        if (playerStats == null || !playerStats.LaserBeamUnlocked) return;

        damage = playerStats.EffectiveDamage;
        damageTimer += Time.deltaTime;

        if (damageTimer < DamageInterval) return;

        damageTimer = 0f;
        ApplyLaserDamage();
    }

    public override void Fire()
    {
    }

    private void ApplyLaserDamage()
    {
        if (fireOrigin == null || playerStats == null) return;

        Vector3 start;
        Vector3 end;
        Enemy hitEnemy = null;

        if (FPSAimUtility.TryGetCameraAim(out Vector3 aimOrigin, out Vector3 aimDirection))
        {
            start = aimOrigin;
            aimDirection.Normalize();

            Transform target = FPSAimUtility.FindEnemyAlongRay(aimOrigin, aimDirection, playerStats.LaserBeamRange);

            if (target != null)
            {
                hitEnemy = target.GetComponent<Enemy>();
                end = target.position + Vector3.up * 0.5f;
            }
            else
            {
                end = aimOrigin + aimDirection * playerStats.LaserBeamRange;
            }
        }
        else
        {
            Transform target = FindClosestEnemyInRange(fireOrigin.position, playerStats.LaserBeamRange);

            if (target == null) return;

            hitEnemy = target.GetComponent<Enemy>();
            start = fireOrigin.position + Vector3.up * 0.5f;
            end = target.position + Vector3.up * 0.5f;
        }

        if (hitEnemy != null)
        {
            hitEnemy.TakeDamage(playerStats.GetEffectiveDamageAgainst(hitEnemy));
        }

        Vector3 visualStart = GetLaserVisualStart();
        Vector3 visualEnd = GetLaserVisualEnd(playerStats.LaserBeamRange);
        SpawnBeamLine(visualStart, visualEnd);
        PlayLaserFireSound();
    }

    private void PlayLaserFireSound()
    {
        Debug.Log("LASER AUDIO TRIGGER");
        Debug.Log("CLIP NULL = " + (laserFireClip == null));

        AudioClip clip = ResolveLaserFireClip();

        if (clip == null) return;

        Vector3 soundPosition = fireOrigin != null ? fireOrigin.position : Vector3.zero;
        Camera camera = Camera.main;

        if (camera != null)
        {
            soundPosition = camera.transform.position;
        }

        GameObject audioObject = new GameObject("LaserFireAudio");
        audioObject.transform.position = soundPosition;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = laserVolume;
        source.spatialBlend = 0f;
        source.minDistance = 1f;
        source.maxDistance = 40f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.playOnAwake = false;
        source.loop = false;
        source.Play();

        Object.Destroy(audioObject, clip.length + 0.1f);
    }

    private AudioClip ResolveLaserFireClip()
    {
        if (laserFireClip != null) return laserFireClip;

        if (WeaponManager.DefaultLaserFireClip != null)
        {
            return WeaponManager.DefaultLaserFireClip;
        }

        return GetLaserFirePlaceholderClip();
    }

    private static AudioClip GetLaserFirePlaceholderClip()
    {
        if (cachedLaserFirePlaceholder != null) return cachedLaserFirePlaceholder;

        const int sampleRate = 44100;
        const float duration = 0.11f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float progress = time / duration;
            float frequency = Mathf.Lerp(2400f, 720f, progress * progress);
            float sine = Mathf.Sin(Mathf.PI * 2f * frequency * time);
            float harmonic = Mathf.Sin(Mathf.PI * 2f * frequency * 1.47f * time) * 0.22f;
            float envelope = Mathf.Exp(-progress * 16f);
            samples[i] = Mathf.Clamp((sine + harmonic) * envelope * 0.72f, -1f, 1f);
        }

        cachedLaserFirePlaceholder = AudioClip.Create("LaserFirePlaceholder", sampleCount, 1, sampleRate, false);
        cachedLaserFirePlaceholder.SetData(samples, 0);
        return cachedLaserFirePlaceholder;
    }

    private static Vector3 GetLaserVisualStart()
    {
        Camera camera = Camera.main;

        if (camera == null) return Vector3.zero;

        Transform viewModelRoot = camera.transform.Find("ViewModelRoot");

        if (viewModelRoot != null)
        {
            Transform weaponMount = viewModelRoot.Find("WeaponMount");

            if (weaponMount != null)
            {
                Transform muzzle = weaponMount.Find("WeaponMuzzle");

                if (muzzle != null)
                {
                    return muzzle.position;
                }

                Transform barrel = weaponMount.Find("WeaponBarrel");

                if (barrel != null)
                {
                    return barrel.position;
                }
            }
        }

        Transform cam = camera.transform;
        return cam.position + cam.right * 0.35f + cam.up * -0.28f + cam.forward * 0.55f;
    }

    private static Vector3 GetLaserVisualEnd(float maxRange)
    {
        Camera camera = Camera.main;

        if (camera == null) return Vector3.forward * maxRange;

        Transform cam = camera.transform;
        Vector3 origin = cam.position;
        Vector3 direction = cam.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRange))
        {
            return hit.point;
        }

        return origin + direction * maxRange;
    }

    private static void SpawnBeamLine(Vector3 start, Vector3 end)
    {
        SpawnLaserVisual(start, end);
    }

    private static void SpawnLaserVisual(Vector3 start, Vector3 end)
    {
        if ((end - start).sqrMagnitude < 0.001f) return;

        GameObject lineObject = new GameObject("LaserBeamVisual");
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = 0.04f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.startColor = new Color(0.25f, 0.92f, 1f, 1f);
        lineRenderer.endColor = new Color(0.25f, 0.92f, 1f, 0.15f);
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        Shader shader = Shader.Find("Sprites/Default")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }

        Object.Destroy(lineObject, 0.08f);
    }

    private Transform FindClosestEnemyInRange(Vector3 origin, float range)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return null;

        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemy = enemies[i];

            if (enemy == null) continue;

            float distance = Vector3.Distance(origin, enemy.transform.position);

            if (distance > range || distance >= closestDistance) continue;

            closestDistance = distance;
            closest = enemy.transform;
        }

        return closest;
    }
}
