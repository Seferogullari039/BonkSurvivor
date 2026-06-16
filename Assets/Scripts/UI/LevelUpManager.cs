using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpManager : MonoBehaviour
{
    public static LevelUpManager Instance { get; private set; }

    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private Button optionButton1;
    [SerializeField] private Button optionButton2;
    [SerializeField] private Button optionButton3;
    [SerializeField] private TMP_Text optionText1;
    [SerializeField] private TMP_Text optionText2;
    [SerializeField] private TMP_Text optionText3;

    private readonly int[] shownUpgradeIndices = new int[3];
    private readonly UpgradeRarity[] shownUpgradeRarities = new UpgradeRarity[3];
    private int menuPlayerLevel = 1;
    private int remainingUpgradeSelections = 1;
    private bool isChestUpgradeMenu;
    private ChestRarity currentChestRarity = ChestRarity.Normal;
    private TMP_Text chestHeaderText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
            ApplyUpgradePanelLayout();
        }
    }

    private void ApplyUpgradePanelLayout()
    {
        if (levelUpPanel == null) return;

        Canvas canvas = levelUpPanel.GetComponentInParent<Canvas>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        RectTransform panelRect = levelUpPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(760f, 460f));

        Image panelImage = levelUpPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = new Color(0.06f, 0.07f, 0.1f, 0.94f);
        }

        LayoutUpgradeButton(optionButton1, 110f);
        LayoutUpgradeButton(optionButton2, 0f);
        LayoutUpgradeButton(optionButton3, -110f);

        if (chestHeaderText != null)
        {
            UiLayoutUtility.SetAnchorCenter(chestHeaderText.rectTransform, new Vector2(0f, 170f), new Vector2(680f, 44f));
            chestHeaderText.fontSize = 30f;
        }
    }

    private static void LayoutUpgradeButton(Button button, float yOffset)
    {
        if (button == null) return;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(0f, yOffset), new Vector2(680f, 72f));

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            label.fontSize = 24f;
            label.enableWordWrapping = true;
            label.alignment = TextAlignmentOptions.Center;
        }
    }

    public void OnPlayerLevelUp(int newLevel)
    {
        menuPlayerLevel = newLevel;
        isChestUpgradeMenu = false;
        remainingUpgradeSelections = 1;
        OpenUpgradeMenuInternal();
    }

    public void OpenUpgradeMenu()
    {
        isChestUpgradeMenu = false;
        remainingUpgradeSelections = 1;
        OpenUpgradeMenuInternal();
    }

    public void OpenChestUpgradeMenu(ChestRarity chestRarity)
    {
        isChestUpgradeMenu = true;
        currentChestRarity = chestRarity;
        remainingUpgradeSelections = ChestRarityUtility.GetUpgradePickCount(chestRarity);
        OpenUpgradeMenuInternal();
    }

    private void OpenUpgradeMenuInternal()
    {
        Time.timeScale = 0f;

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
        }

        PlayerStats playerStats = FindPlayerStats();

        if (playerStats != null)
        {
            menuPlayerLevel = playerStats.CurrentLevel;
        }

        AssignRandomUpgradeOptions();
        RefreshUpgradeOptionTexts();
        UpdateChestHeaderText();
        RefreshButtonListeners();
    }

    private void RefreshUpgradeOptionTexts()
    {
        SetOptionText(optionText1, BuildOptionLabel(shownUpgradeIndices[0], shownUpgradeRarities[0]));
        SetOptionText(optionText2, BuildOptionLabel(shownUpgradeIndices[1], shownUpgradeRarities[1]));
        SetOptionText(optionText3, BuildOptionLabel(shownUpgradeIndices[2], shownUpgradeRarities[2]));
    }

    private void EnsureChestHeaderText()
    {
        if (chestHeaderText != null) return;
        if (levelUpPanel == null) return;

        Transform existingHeader = levelUpPanel.transform.Find("ChestHeaderText");

        if (existingHeader != null)
        {
            chestHeaderText = existingHeader.GetComponent<TMP_Text>();

            if (chestHeaderText != null) return;
        }

        GameObject headerObject = new GameObject("ChestHeaderText");
        headerObject.transform.SetParent(levelUpPanel.transform, false);

        RectTransform rectTransform = headerObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(0f, 170f), new Vector2(680f, 44f));

        TextMeshProUGUI textMesh = headerObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 28f;

        chestHeaderText = textMesh;
    }

    private void UpdateChestHeaderText()
    {
        EnsureChestHeaderText();

        if (chestHeaderText == null) return;

        if (!isChestUpgradeMenu)
        {
            chestHeaderText.gameObject.SetActive(false);
            return;
        }

        chestHeaderText.gameObject.SetActive(true);
        chestHeaderText.text = ChestRarityUtility.GetHeaderText(currentChestRarity);
        chestHeaderText.color = ChestRarityUtility.GetHeaderColor(currentChestRarity);
    }

    private void AssignRandomUpgradeOptions()
    {
        List<int> availableIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        List<int> unpurchasedWeapons = GetUnpurchasedWeaponIndices();

        for (int i = 0; i < shownUpgradeIndices.Length; i++)
        {
            int pick;

            if (menuPlayerLevel <= 5 && unpurchasedWeapons.Count > 0 && (i == 0 || Random.value < 0.5f))
            {
                int weaponPickIndex = Random.Range(0, unpurchasedWeapons.Count);
                pick = unpurchasedWeapons[weaponPickIndex];
                unpurchasedWeapons.RemoveAt(weaponPickIndex);
            }
            else
            {
                int pickIndex = Random.Range(0, availableIndices.Count);
                pick = availableIndices[pickIndex];
            }

            shownUpgradeIndices[i] = pick;
            shownUpgradeRarities[i] = RollUpgradeRarity();
            availableIndices.Remove(pick);
            unpurchasedWeapons.Remove(pick);
        }
    }

    private UpgradeRarity RollUpgradeRarity()
    {
        float roll = Random.value;

        if (roll < 0.05f)
        {
            return UpgradeRarity.Epic;
        }

        if (roll < 0.30f)
        {
            return UpgradeRarity.Rare;
        }

        return UpgradeRarity.Common;
    }

    private static int GetRarityMultiplier(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => 2,
            UpgradeRarity.Epic => 3,
            _ => 1
        };
    }

    private static string GetRarityLabel(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => "[Rare]",
            UpgradeRarity.Epic => "[Epic]",
            _ => "[Common]"
        };
    }

    private static string GetRarityColorHex(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => "#5CB8FF",
            UpgradeRarity.Epic => "#C77DFF",
            _ => "#D9D9D9"
        };
    }

    private string BuildOptionLabel(int upgradeIndex, UpgradeRarity rarity)
    {
        int multiplier = GetRarityMultiplier(rarity);
        string body = GetUpgradeBody(upgradeIndex, multiplier);
        string label = $"{GetRarityLabel(rarity)} {body}";

        return $"<color={GetRarityColorHex(rarity)}>{label}</color>";
    }

    private static string GetUpgradeBody(int upgradeIndex, int multiplier)
    {
        return upgradeIndex switch
        {
            0 => $"Ateş Hızı +{20 * multiplier}%",
            1 => $"Mermi Hızı +{25 * multiplier}%",
            2 => $"XP Çekim Alanı +{30 * multiplier}%",
            3 => $"Hasar +{multiplier}",
            4 => "Spread Shot\n3 mermi aynı anda ateşler.",
            5 => "Piercing Shot\nMermiler 1 düşmanı delip geçer.",
            6 => "Orbiting Orb\nEtrafında dönen hasar küresi oluşturur.",
            7 => "Rocket Launcher\nYavaş roket, alan hasarı verir.",
            8 => "Chain Lightning\nYakın düşmanlara 3 sekme.",
            9 => "Laser Beam\nKısa menzilde sürekli hasar.",
            _ => string.Empty
        };
    }

    private List<int> GetUnpurchasedWeaponIndices()
    {
        List<int> weaponIndices = new List<int>();
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats != null && !playerStats.SpreadShotUnlocked)
        {
            weaponIndices.Add(4);
        }

        if (playerStats != null && playerStats.PierceCount <= 0)
        {
            weaponIndices.Add(5);
        }

        if (playerStats != null && playerStats.OrbitOrbCount <= 0)
        {
            weaponIndices.Add(6);
        }

        if (playerStats != null && !playerStats.RocketLauncherUnlocked)
        {
            weaponIndices.Add(7);
        }

        if (playerStats != null && !playerStats.ChainLightningUnlocked)
        {
            weaponIndices.Add(8);
        }

        if (playerStats != null && !playerStats.LaserBeamUnlocked)
        {
            weaponIndices.Add(9);
        }

        return weaponIndices;
    }

    private PlayerStats FindPlayerStats()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        return player != null ? player.GetComponent<PlayerStats>() : null;
    }

    private void RefreshButtonListeners()
    {
        if (optionButton1 != null)
        {
            optionButton1.onClick.RemoveAllListeners();
            optionButton1.onClick.AddListener(() => SelectUpgrade(0));
        }

        if (optionButton2 != null)
        {
            optionButton2.onClick.RemoveAllListeners();
            optionButton2.onClick.AddListener(() => SelectUpgrade(1));
        }

        if (optionButton3 != null)
        {
            optionButton3.onClick.RemoveAllListeners();
            optionButton3.onClick.AddListener(() => SelectUpgrade(2));
        }
    }

    private void SetOptionText(TMP_Text text, string value)
    {
        if (text == null) return;

        text.text = value;
    }

    private void SelectUpgrade(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= shownUpgradeIndices.Length) return;

        int upgradeIndex = shownUpgradeIndices[optionIndex];
        UpgradeRarity rarity = shownUpgradeRarities[optionIndex];
        AudioManager.Instance?.PlayUpgradeSelect();
        ApplySelectedUpgrade(upgradeIndex, rarity);

        remainingUpgradeSelections--;

        if (remainingUpgradeSelections > 0)
        {
            AssignRandomUpgradeOptions();
            RefreshUpgradeOptionTexts();
            RefreshButtonListeners();
            return;
        }

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }

        if (chestHeaderText != null)
        {
            chestHeaderText.gameObject.SetActive(false);
        }

        isChestUpgradeMenu = false;
        Time.timeScale = 1f;
    }

    private void ApplySelectedUpgrade(int upgradeIndex, UpgradeRarity rarity)
    {
        int multiplier = GetRarityMultiplier(rarity);

        switch (upgradeIndex)
        {
            case 0:
                ApplyFireRateUpgrade(0.2f * multiplier);
                break;
            case 1:
                ApplyProjectileSpeedUpgrade(0.25f * multiplier);
                break;
            case 2:
                ApplyXPAttractionUpgrade(0.30f * multiplier);
                break;
            case 3:
                ApplyDamageUpgrade(multiplier);
                break;
            case 4:
                ApplySpreadShotUpgrade();
                break;
            case 5:
                ApplyPiercingShotUpgrade();
                break;
            case 6:
                ApplyOrbitingOrbUpgrade();
                break;
            case 7:
                ApplyRocketLauncherUpgrade(multiplier);
                break;
            case 8:
                ApplyChainLightningUpgrade(multiplier);
                break;
            case 9:
                ApplyLaserBeamUpgrade(multiplier);
                break;
        }
    }

    private void ApplyFireRateUpgrade(float percent)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();

        if (weaponManager == null) return;

        weaponManager.IncreaseFireRate(percent);
    }

    private void ApplyProjectileSpeedUpgrade(float percent)
    {
        UpgradeManager upgradeManager = UpgradeManager.GetOrCreateInstance();

        if (upgradeManager == null)
        {
            Debug.LogError("UpgradeManager bulunamadı");
            return;
        }

        upgradeManager.IncreaseProjectileSpeed(percent);
    }

    private void ApplyXPAttractionUpgrade(float percent)
    {
        UpgradeManager upgradeManager = UpgradeManager.GetOrCreateInstance();

        if (upgradeManager == null)
        {
            Debug.LogError("UpgradeManager bulunamadı");
            return;
        }

        upgradeManager.IncreaseXPAttraction(percent);
    }

    private void ApplyDamageUpgrade(int amount)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        PlayerStats playerStats = player.GetComponent<PlayerStats>();

        if (playerStats == null) return;

        playerStats.damage += amount;
    }

    private void ApplySpreadShotUpgrade()
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeSpreadShot();
    }

    private void ApplyPiercingShotUpgrade()
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradePiercingShot();
    }

    private void ApplyOrbitingOrbUpgrade()
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeOrbitingOrb();

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        WeaponManager weaponManager = player.GetComponent<WeaponManager>();

        if (weaponManager == null) return;

        weaponManager.RefreshOrbitWeapon();
    }

    private void ApplyRocketLauncherUpgrade(int multiplier)
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        for (int i = 0; i < multiplier; i++)
        {
            playerStats.UpgradeRocketLauncher();
        }
    }

    private void ApplyChainLightningUpgrade(int multiplier)
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        for (int i = 0; i < multiplier; i++)
        {
            playerStats.UpgradeChainLightning();
        }
    }

    private void ApplyLaserBeamUpgrade(int multiplier)
    {
        PlayerStats playerStats = FindPlayerStats();

        if (playerStats == null) return;

        for (int i = 0; i < multiplier; i++)
        {
            playerStats.UpgradeLaserBeam();
        }
    }
}
