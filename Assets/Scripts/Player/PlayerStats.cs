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
    private float arrowRainDamageMultiplier = 1f;
    private float megaMeteorSkillDamageMultiplier = 1f;
    private float swordRmbDamageMultiplier = 1f;
    private float chestMoveSpeedMultiplier = 1f;
    private float chestCoinGainMultiplier = 1f;
    private float chestXpGainMultiplier = 1f;

    public float ChestMoveSpeedMultiplier => chestMoveSpeedMultiplier;
    public float StarterWeaponCooldownMultiplier => starterWeaponCooldownMultiplier;
    public float StarterProjectileSpeedMultiplier => starterProjectileSpeedMultiplier;
    public float MegaMeteorCooldownMultiplier => megaMeteorCooldownMultiplier;
    public float SwordSkillCooldownMultiplier => swordSkillCooldownMultiplier;
    public float ArrowRainDamageMultiplier => arrowRainDamageMultiplier;
    public float MegaMeteorSkillDamageMultiplier => megaMeteorSkillDamageMultiplier;
    public float SwordRmbDamageMultiplier => swordRmbDamageMultiplier;

    public bool IsGodMode => isGodMode;
    public bool IsDead => isDead;

    public int CurrentLevel => currentLevel;
    public int CurrentHealth => currentHealth;
    public int CurrentXP => currentXP;
    public int XPToNextLevel => xpToNextLevel;
    public int Coins => coins;

    public static bool LogEffectiveDamage = false;
    public static bool LogLevelUpDebug = false;

    // Tracks the relic max-HP bonus already folded into currentHealth so mid-run grants apply once.
    private int lastRelicMaxHealthBonus;

    // Runtime final max HP. Relic-aware; relic yoksa MaxHealthBonus 0 -> base maxHealth ayni.
    // maxHealth field meta/upgrade/UI/save tarafindan oldugu gibi kullanilmaya devam eder.
    public int EffectiveMaxHealth
    {
        get
        {
            return Mathf.Max(1, maxHealth + RelicManager.MaxHealthBonus);
        }
    }

    // Runtime final player-to-enemy damage. Relic-aware; relic yoksa multiplier 1.0 -> base damage ayni.
    // damage field meta/upgrade/UI/save tarafindan oldugu gibi kullanilmaya devam eder.
    public int EffectiveDamage
    {
        get
        {
            float multiplier = RelicManager.DamageMultiplier;
            int result = Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));

            if (LogEffectiveDamage)
            {
                Debug.Log("[PlayerStats] EffectiveDamage base=" + damage
                    + " multiplier=" + multiplier.ToString("0.00")
                    + " final=" + result);
            }

            return result;
        }
    }

    // Target-aware damage. Sharp Fang (EffectiveDamage) applies first; Hunter Mark adds an
    // elite-only bonus on top. Non-elite targets (incl. mini/dragon bosses) get base EffectiveDamage.
    public int GetEffectiveDamageAgainst(Enemy target)
    {
        int baseDamage = EffectiveDamage;

        if (target != null && target.IsElite)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseDamage * RelicManager.EliteDamageMultiplier));
        }

        return baseDamage;
    }
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

    public void IncreaseArrowRainDamage(float percent)
    {
        arrowRainDamageMultiplier = Mathf.Clamp(arrowRainDamageMultiplier * (1f + percent), 1f, 5f);
    }

    public void IncreaseMegaMeteorDamage(float percent)
    {
        megaMeteorSkillDamageMultiplier = Mathf.Clamp(megaMeteorSkillDamageMultiplier * (1f + percent), 1f, 5f);
    }

    public void IncreaseSwordSkillDamage(float percent)
    {
        swordRmbDamageMultiplier = Mathf.Clamp(swordRmbDamageMultiplier * (1f + percent), 1f, 5f);
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
        arrowRainDamageMultiplier = 1f;
        megaMeteorSkillDamageMultiplier = 1f;
        swordRmbDamageMultiplier = 1f;
        chestMoveSpeedMultiplier = 1f;
        chestCoinGainMultiplier = 1f;
        chestXpGainMultiplier = 1f;

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ResetForNewRun();
        }
    }

    public void ApplyChestMaxHealthBonus(float percent)
    {
        int bonus = Mathf.Max(1, Mathf.RoundToInt(maxHealth * percent));
        maxHealth += bonus;
        currentHealth = Mathf.Min(currentHealth + bonus, EffectiveMaxHealth);
        RefreshHUD();
    }

    public void ApplyChestMoveSpeedBonus(float percent)
    {
        chestMoveSpeedMultiplier = Mathf.Clamp(chestMoveSpeedMultiplier * (1f + percent), 1f, 3f);

        FPSPlayerController fpsPlayerController = GetComponent<FPSPlayerController>();
        fpsPlayerController?.RefreshMoveSpeedFromStats();
    }

    public void ApplyChestCoinGainBonus(float percent)
    {
        chestCoinGainMultiplier = Mathf.Clamp(chestCoinGainMultiplier * (1f + percent), 1f, 4f);
    }

    public void ApplyChestXpGainBonus(float percent)
    {
        chestXpGainMultiplier = Mathf.Clamp(chestXpGainMultiplier * (1f + percent), 1f, 4f);
    }

    public void HealAmount(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, EffectiveMaxHealth);
        RefreshHUD();
    }

    public void HealToFull()
    {
        currentHealth = EffectiveMaxHealth;
        RefreshHUD();
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void Update()
    {
        RefreshRelicHealthBonus();
    }

    // Applies Vital Core (or future max-HP relics) the moment the bonus changes.
    // Gaining the bonus heals by the delta (clamped); losing it only clamps currentHealth down.
    private void RefreshRelicHealthBonus()
    {
        int bonus = RelicManager.MaxHealthBonus;

        if (bonus == lastRelicMaxHealthBonus)
        {
            return;
        }

        int delta = bonus - lastRelicMaxHealthBonus;
        lastRelicMaxHealthBonus = bonus;

        if (delta > 0)
        {
            currentHealth = Mathf.Min(currentHealth + delta, EffectiveMaxHealth);
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, EffectiveMaxHealth);
        }

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHealth, EffectiveMaxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isGodMode) return;
        if (FPSPlayerController.IsInvulnerable) return;

        currentHealth -= damage;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHealth, EffectiveMaxHealth);
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

        int finalAmount = Mathf.Max(1, Mathf.RoundToInt(amount * metaXpGainMultiplier * chestXpGainMultiplier));
        currentXP += finalAmount;

        if (currentXP >= xpToNextLevel)
        {
            currentLevel++;
            currentXP = 0;
            xpToNextLevel += 5;

            if (LogLevelUpDebug)
            {
                Debug.Log("LEVEL UP");
            }

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

    public static bool LogCoinGain = false;

    public void AddCoins(int amount)
    {
        if (!MainMenuManager.IsRunActive) return;

        float multiplier = RelicManager.CoinGainMultiplier * chestCoinGainMultiplier;
        int finalAmount = amount > 0
            ? Mathf.Max(1, Mathf.RoundToInt(amount * multiplier))
            : amount;
        coins += finalAmount;

        if (LogCoinGain)
        {
            Debug.Log("[PlayerStats] AddCoins base=" + amount
                + " multiplier=" + multiplier.ToString("0.00")
                + " final=" + finalAmount
                + " total=" + coins);
        }

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
        currentHealth = EffectiveMaxHealth;
        isDead = false;

        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateHP(currentHealth, EffectiveMaxHealth);
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

        HUDManager.Instance.UpdateHP(currentHealth, EffectiveMaxHealth);
        HUDManager.Instance.UpdateXP(currentXP, xpToNextLevel);
        HUDManager.Instance.UpdateLevel(currentLevel);
        HUDManager.Instance.UpdateXPBar(currentXP, xpToNextLevel);
        HUDManager.Instance.UpdateCoins(coins);
    }
}
