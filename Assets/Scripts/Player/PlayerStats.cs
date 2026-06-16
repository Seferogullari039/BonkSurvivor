using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpToNextLevel = 5;
    private int coins = 0;
    public int damage = 1;

    private bool spreadShotUnlocked = false;
    private int spreadShotLevel = 0;
    private float spreadAngle = 15f;
    private int pierceCount = 0;
    private int orbitOrbCount = 0;
    private float orbitOrbDamageMultiplier = 1f;
    private float orbitRadius = 1.8f;
    private float orbitSpeed = 180f;

    private bool rocketLauncherUnlocked;
    private int rocketLauncherLevel;
    private bool chainLightningUnlocked;
    private int chainLightningLevel;
    private bool laserBeamUnlocked;
    private int laserBeamLevel;

    private bool isDead;
    private bool isGodMode;
    private float metaXpGainMultiplier = 1f;
    private float starterWeaponCooldownMultiplier = 1f;
    private float starterProjectileSpeedMultiplier = 1f;
    private float megaMeteorCooldownMultiplier = 1f;
    private float swordSkillCooldownMultiplier = 1f;

    public float StarterWeaponCooldownMultiplier => starterWeaponCooldownMultiplier;
    public float StarterProjectileSpeedMultiplier => starterProjectileSpeedMultiplier;
    public float MegaMeteorCooldownMultiplier => megaMeteorCooldownMultiplier;
    public float SwordSkillCooldownMultiplier => swordSkillCooldownMultiplier;

    public bool IsGodMode => isGodMode;
    public bool IsDead => isDead;

    public int CurrentLevel => currentLevel;
    public int Coins => coins;
    public bool SpreadShotUnlocked => spreadShotUnlocked;
    public int SpreadShotLevel => spreadShotLevel;
    public float SpreadAngle => spreadAngle;
    public int PierceCount => pierceCount;
    public int OrbitOrbCount => orbitOrbCount;
    public float OrbitOrbDamageMultiplier => orbitOrbDamageMultiplier;
    public float OrbitRadius => orbitRadius;
    public float OrbitSpeed => orbitSpeed;
    public bool RocketLauncherUnlocked => rocketLauncherUnlocked;
    public float RocketAoERadius => rocketLauncherLevel > 0 ? 1.6f + (rocketLauncherLevel - 1) * 0.35f : 1.6f;
    public bool ChainLightningUnlocked => chainLightningUnlocked;
    public int ChainLightningTargets => 3 + Mathf.Max(0, chainLightningLevel - 1);
    public bool LaserBeamUnlocked => laserBeamUnlocked;
    public float LaserBeamRange => laserBeamLevel > 0 ? 4f + (laserBeamLevel - 1) * 0.6f : 4f;

    public void UpgradeSpreadShot()
    {
        if (!spreadShotUnlocked)
        {
            spreadShotUnlocked = true;
            spreadShotLevel = 1;
            return;
        }

        spreadShotLevel++;
        spreadAngle += 2f;
    }

    public void UpgradePiercingShot()
    {
        if (pierceCount <= 0)
        {
            pierceCount = 1;
            return;
        }

        pierceCount++;
    }

    public void UpgradeOrbitingOrb()
    {
        if (orbitOrbCount <= 0)
        {
            orbitOrbCount = 1;
            return;
        }

        orbitOrbCount++;
    }

    public void UpgradeRocketLauncher()
    {
        if (!rocketLauncherUnlocked)
        {
            rocketLauncherUnlocked = true;
            rocketLauncherLevel = 1;
            return;
        }

        rocketLauncherLevel++;
    }

    public void UpgradeChainLightning()
    {
        if (!chainLightningUnlocked)
        {
            chainLightningUnlocked = true;
            chainLightningLevel = 1;
            return;
        }

        chainLightningLevel++;
    }

    public void UpgradeLaserBeam()
    {
        if (!laserBeamUnlocked)
        {
            laserBeamUnlocked = true;
            laserBeamLevel = 1;
            return;
        }

        laserBeamLevel++;
    }

    public void ApplyMetaRunBonuses(int hpBonus, int damageBonus)
    {
        maxHealth += hpBonus;
        currentHealth = maxHealth;
        damage += damageBonus;
    }

    public void SetMetaXpGainMultiplier(float multiplier)
    {
        metaXpGainMultiplier = Mathf.Max(1f, multiplier);
    }

    public void IncreaseStarterWeaponFireRate(float percent)
    {
        starterWeaponCooldownMultiplier *= Mathf.Max(0.05f, 1f - percent);
        starterWeaponCooldownMultiplier = Mathf.Clamp(starterWeaponCooldownMultiplier, 0.25f, 1f);
    }

    public void IncreaseStarterProjectileSpeed(float percent)
    {
        starterProjectileSpeedMultiplier *= 1f + percent;
        starterProjectileSpeedMultiplier = Mathf.Clamp(starterProjectileSpeedMultiplier, 1f, 4f);
    }

    public void ReduceMegaMeteorCooldown(float percent)
    {
        megaMeteorCooldownMultiplier *= Mathf.Max(0.05f, 1f - percent);
        megaMeteorCooldownMultiplier = Mathf.Clamp(megaMeteorCooldownMultiplier, 0.35f, 1f);
    }

    public void ReduceSwordSkillCooldown(float percent)
    {
        swordSkillCooldownMultiplier *= Mathf.Max(0.05f, 1f - percent);
        swordSkillCooldownMultiplier = Mathf.Clamp(swordSkillCooldownMultiplier, 0.35f, 1f);
    }

    public void ApplyMetaProgressionBonuses()
    {
        MetaProgressionData.ApplyRunBonuses(this, GetComponent<PlayerController>());
        RefreshHUD();
    }

    public void ResetRunState()
    {
        maxHealth = 10;
        currentHealth = 10;
        currentLevel = 1;
        currentXP = 0;
        xpToNextLevel = 5;
        coins = 0;
        damage = 1;
        spreadShotUnlocked = false;
        spreadShotLevel = 0;
        spreadAngle = 15f;
        pierceCount = 0;
        orbitOrbCount = 0;
        orbitOrbDamageMultiplier = 1f;
        orbitRadius = 1.8f;
        orbitSpeed = 180f;
        rocketLauncherUnlocked = false;
        rocketLauncherLevel = 0;
        chainLightningUnlocked = false;
        chainLightningLevel = 0;
        laserBeamUnlocked = false;
        laserBeamLevel = 0;
        isDead = false;
        isGodMode = false;
        metaXpGainMultiplier = 1f;
        starterWeaponCooldownMultiplier = 1f;
        starterProjectileSpeedMultiplier = 1f;
        megaMeteorCooldownMultiplier = 1f;
        swordSkillCooldownMultiplier = 1f;
    }

    private void Start()
    {
        RefreshHUD();
    }

    public void TakeDamage(int damage)
    {
        if (isGodMode) return;
        if (FPSPlayerController.IsInvulnerable) return;

        currentHealth -= damage;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHealth, maxHealth);
        }

        // FPSScreenShake.Shake(0.025f, 0.12f);

        if (currentHealth <= 0)
        {
            if (isDead) return;

            isDead = true;
            Debug.Log("GAME OVER");

            MetaProgressionData.AddRunCoinsToTotal(coins);

            if (GameOverManager.Instance != null)
            {
                int wave = 1;
                EnemySpawner enemySpawner = FindFirstObjectByType<EnemySpawner>();

                if (enemySpawner != null)
                {
                    wave = Mathf.Max(1, enemySpawner.CurrentWave);
                }

                GameOverManager.Instance.ShowGameOver(wave, currentLevel, coins);
            }
            else
            {
                Time.timeScale = 0f;
            }
        }
    }

    public void AddXP(int amount)
    {
        if (!MainMenuManager.IsRunActive) return;

        int finalAmount = Mathf.Max(1, Mathf.RoundToInt(amount * metaXpGainMultiplier));
        currentXP += finalAmount;

        if (currentXP >= xpToNextLevel)
        {
            currentLevel++;
            currentXP = 0;
            xpToNextLevel += 5;

            Debug.Log("LEVEL UP");
            AudioManager.Instance?.PlayLevelUp();
            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.PlayLevelUp(transform.position);
            }

            if (HUDManager.Instance != null)
            {
                HUDManager.Instance.UpdateLevel(currentLevel);
                HUDManager.Instance.UpdateXP(currentXP, xpToNextLevel);
                HUDManager.Instance.UpdateXPBar(currentXP, xpToNextLevel);
            }

            if (LevelUpManager.Instance != null)
            {
                LevelUpManager.Instance.OnPlayerLevelUp(currentLevel);
            }
        }
        else if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateXP(currentXP, xpToNextLevel);
            HUDManager.Instance.UpdateXPBar(currentXP, xpToNextLevel);
        }
    }

    public void AddCoins(int amount)
    {
        if (!MainMenuManager.IsRunActive) return;

        coins += amount;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateCoins(coins);
        }
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount)
        {
            return false;
        }

        coins -= amount;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateCoins(coins);
        }

        return true;
    }

    public void SetGodMode(bool enabled)
    {
        isGodMode = enabled;
    }

    public void DevForceLevelUp()
    {
        if (!MainMenuManager.IsRunActive) return;

        currentLevel++;
        currentXP = 0;
        xpToNextLevel += 5;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateLevel(currentLevel);
            HUDManager.Instance.UpdateXP(currentXP, xpToNextLevel);
            HUDManager.Instance.UpdateXPBar(currentXP, xpToNextLevel);
        }

        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.OnPlayerLevelUp(currentLevel);
        }
    }

    public void DevHealFull()
    {
        currentHealth = maxHealth;
        isDead = false;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHealth, maxHealth);
        }
    }

    public void DevUnlockAllWeapons()
    {
        if (!spreadShotUnlocked)
        {
            UpgradeSpreadShot();
        }

        if (pierceCount <= 0)
        {
            UpgradePiercingShot();
        }

        if (orbitOrbCount <= 0)
        {
            UpgradeOrbitingOrb();
        }

        if (!rocketLauncherUnlocked)
        {
            UpgradeRocketLauncher();
        }

        if (!laserBeamUnlocked)
        {
            UpgradeLaserBeam();
        }

        if (!chainLightningUnlocked)
        {
            UpgradeChainLightning();
        }
    }

    private void RefreshHUD()
    {
        if (HUDManager.Instance == null) return;

        HUDManager.Instance.UpdateHP(currentHealth, maxHealth);
        HUDManager.Instance.UpdateXP(currentXP, xpToNextLevel);
        HUDManager.Instance.UpdateLevel(currentLevel);
        HUDManager.Instance.UpdateXPBar(currentXP, xpToNextLevel);
        HUDManager.Instance.UpdateCoins(coins);
    }
}
