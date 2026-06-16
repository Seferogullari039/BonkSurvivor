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
    private readonly UpgradeCardView[] upgradeCards = new UpgradeCardView[3];
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
            EnsureUpgradeCards();
        }
    }

    private sealed class UpgradeCardView
    {
        public TMP_Text RarityText;
        public TMP_Text TitleText;
        public TMP_Text DescriptionText;
    }

    private readonly struct UpgradeCardContent
    {
        public UpgradeCardContent(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public string Title { get; }
        public string Description { get; }
    }

    private void ApplyUpgradePanelLayout()
    {
        if (levelUpPanel == null) return;

        Canvas canvas = levelUpPanel.GetComponentInParent<Canvas>();
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        RectTransform panelRect = levelUpPanel.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(panelRect, Vector2.zero, new Vector2(860f, 440f));

        Image panelImage = levelUpPanel.GetComponent<Image>();

        if (panelImage != null)
        {
            panelImage.color = new Color(0.05f, 0.06f, 0.09f, 0.94f);
        }

        LayoutUpgradeButton(optionButton1, -270f);
        LayoutUpgradeButton(optionButton2, 0f);
        LayoutUpgradeButton(optionButton3, 270f);

        if (chestHeaderText != null)
        {
            UiLayoutUtility.SetAnchorCenter(chestHeaderText.rectTransform, new Vector2(0f, 170f), new Vector2(680f, 44f));
            chestHeaderText.fontSize = 30f;
        }
    }

    private static void LayoutUpgradeButton(Button button, float xOffset)
    {
        if (button == null) return;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, new Vector2(xOffset, -8f), new Vector2(250f, 300f));

        Image buttonImage = button.GetComponent<Image>();

        if (buttonImage != null)
        {
            buttonImage.color = new Color(0.11f, 0.13f, 0.17f, 0.98f);
        }

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.18f, 0.22f, 0.3f, 1f);
        colors.pressedColor = new Color(0.24f, 0.3f, 0.4f, 1f);
        colors.selectedColor = new Color(0.18f, 0.22f, 0.3f, 1f);
        button.colors = colors;
    }

    private void EnsureUpgradeCards()
    {
        EnsureUpgradeCard(0, optionButton1, optionText1);
        EnsureUpgradeCard(1, optionButton2, optionText2);
        EnsureUpgradeCard(2, optionButton3, optionText3);
    }

    private void EnsureUpgradeCard(int index, Button button, TMP_Text legacyText)
    {
        if (button == null || upgradeCards[index] != null)
        {
            return;
        }

        if (legacyText != null)
        {
            legacyText.gameObject.SetActive(false);
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();

        if (buttonRect == null)
        {
            return;
        }

        upgradeCards[index] = new UpgradeCardView
        {
            RarityText = CreateCardText(buttonRect, "RarityText", new Vector2(0f, 108f), new Vector2(220f, 28f), 17f, FontStyles.Bold),
            TitleText = CreateCardText(buttonRect, "TitleText", new Vector2(0f, 38f), new Vector2(220f, 96f), 30f, FontStyles.Bold),
            DescriptionText = CreateCardText(buttonRect, "DescriptionText", new Vector2(0f, -58f), new Vector2(220f, 118f), 20f, FontStyles.Normal)
        };

        ConfigureDescriptionText(upgradeCards[index].DescriptionText);
        ConfigureTitleText(upgradeCards[index].TitleText);
    }

    private static TMP_Text CreateCardText(
        RectTransform parent,
        string objectName,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(rectTransform, anchoredPosition, size);

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.textWrappingMode = TextWrappingModes.Normal;
        textMesh.richText = false;
        textMesh.raycastTarget = false;

        return textMesh;
    }

    private static void ConfigureTitleText(TMP_Text titleText)
    {
        if (titleText == null)
        {
            return;
        }

        titleText.color = Color.white;
        titleText.enableAutoSizing = true;
        titleText.fontSizeMin = 24f;
        titleText.fontSizeMax = 32f;
        titleText.outlineWidth = 0.12f;
        titleText.outlineColor = new Color(0f, 0f, 0f, 0.35f);
    }

    private static void ConfigureDescriptionText(TMP_Text descriptionText)
    {
        if (descriptionText == null)
        {
            return;
        }

        descriptionText.color = new Color(0.84f, 0.88f, 0.94f, 1f);
        descriptionText.lineSpacing = 4f;
        descriptionText.enableAutoSizing = true;
        descriptionText.fontSizeMin = 17f;
        descriptionText.fontSizeMax = 22f;
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
        EnsureUpgradeCards();
        RefreshUpgradeOptionTexts();
        UpdateChestHeaderText();
        RefreshButtonListeners();
    }

    private void RefreshUpgradeOptionTexts()
    {
        SetCardText(0, shownUpgradeIndices[0], shownUpgradeRarities[0]);
        SetCardText(1, shownUpgradeIndices[1], shownUpgradeRarities[1]);
        SetCardText(2, shownUpgradeIndices[2], shownUpgradeRarities[2]);
    }

    private void SetCardText(int cardIndex, int upgradeIndex, UpgradeRarity rarity)
    {
        EnsureUpgradeCards();

        UpgradeCardView card = upgradeCards[cardIndex];

        if (card == null)
        {
            TMP_Text fallbackText = cardIndex switch
            {
                0 => optionText1,
                1 => optionText2,
                _ => optionText3
            };

            SetOptionText(fallbackText, BuildLegacyOptionLabel(upgradeIndex, rarity));
            return;
        }

        int multiplier = GetRarityMultiplier(rarity);
        UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier);

        if (card.RarityText != null)
        {
            card.RarityText.text = GetRarityLabel(rarity);
            card.RarityText.color = GetRarityColor(rarity);
        }

        if (card.TitleText != null)
        {
            card.TitleText.text = content.Title;
        }

        if (card.DescriptionText != null)
        {
            card.DescriptionText.text = content.Description;
        }
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

    private static Color GetRarityColor(UpgradeRarity rarity)
    {
        return rarity switch
        {
            UpgradeRarity.Rare => new Color(0.36f, 0.72f, 1f, 1f),
            UpgradeRarity.Epic => new Color(0.78f, 0.49f, 1f, 1f),
            _ => new Color(0.86f, 0.88f, 0.92f, 1f)
        };
    }

    private static string BuildLegacyOptionLabel(int upgradeIndex, UpgradeRarity rarity)
    {
        int multiplier = GetRarityMultiplier(rarity);
        UpgradeCardContent content = GetUpgradeCardContent(upgradeIndex, multiplier);
        return $"{GetRarityLabel(rarity)}\n{content.Title}\n{content.Description}";
    }

    private static UpgradeCardContent GetUpgradeCardContent(int upgradeIndex, int multiplier)
    {
        return upgradeIndex switch
        {
            0 => new UpgradeCardContent($"Ateş Hızı +{20 * multiplier}%", "Silah ateş hızını artırır."),
            1 => new UpgradeCardContent($"Mermi Hızı +{25 * multiplier}%", "Mermiler daha hızlı ilerler."),
            2 => new UpgradeCardContent($"XP Çekim Alanı +{30 * multiplier}%", "XP kürelerini daha uzaaktan toplar."),
            3 => new UpgradeCardContent($"Hasar +{multiplier}", "Temel hasarını artırır."),
            4 => new UpgradeCardContent("Spread Shot", "3 mermi aynı anda ateşler."),
            5 => new UpgradeCardContent("Piercing Shot", "Mermiler 1 düşmanı delip geçer."),
            6 => new UpgradeCardContent("Orbiting Orb", "Etrafında dönen hasar küresi oluşturur."),
            7 => new UpgradeCardContent("Rocket Launcher", "Yavaş roket, alan hasarı verir."),
            8 => new UpgradeCardContent("Chain Lightning", "Yakın düşmanlara 3 sekme."),
            9 => new UpgradeCardContent("Laser Beam", "Kısa menzilde sürekli hasar."),
            _ => new UpgradeCardContent(string.Empty, string.Empty)
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
