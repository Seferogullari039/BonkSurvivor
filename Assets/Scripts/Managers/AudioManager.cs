using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioClip coinPickupClip;
    [SerializeField] private AudioClip xpPickupClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip chestOpenClip;
    [SerializeField] private AudioClip upgradeSelectClip;
    [SerializeField] private AudioClip bossSpawnClip;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip telegraphWarningClip;
    [SerializeField] private AudioClip rocketExplosionClip;

    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 0.8f;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void PlayCoinPickup() => PlaySfx(coinPickupClip);

    public void PlayXpPickup() => PlaySfx(xpPickupClip);

    public void PlayLevelUp() => PlaySfx(levelUpClip);

    public void PlayChestOpen() => PlaySfx(chestOpenClip);

    public void PlayUpgradeSelect() => PlaySfx(upgradeSelectClip);

    public void PlayBossSpawn() => PlaySfx(bossSpawnClip);

    public void PlayGameOver() => PlaySfx(gameOverClip);

    public void PlayButtonClick() => PlaySfx(buttonClickClip);

    public void PlayTelegraphWarning()
    {
        if (telegraphWarningClip == null || sfxSource == null) return;

        sfxSource.PlayOneShot(telegraphWarningClip, 0.35f);
    }

    public void PlayRocketExplosion()
    {
        if (sfxSource == null) return;

        if (rocketExplosionClip != null)
        {
            sfxSource.PlayOneShot(rocketExplosionClip, 0.85f);
            return;
        }

        if (bossSpawnClip != null)
        {
            sfxSource.PlayOneShot(bossSpawnClip, 0.55f);
            return;
        }

        PlayTelegraphWarning();
    }
}
