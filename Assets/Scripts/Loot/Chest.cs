using System.Collections;
using TMPro;
using UnityEngine;

public class Chest : MonoBehaviour
{
    [SerializeField] private int basePrice = 10;
    [SerializeField] private TMP_Text priceText;

    private int price;
    private bool isOpened;
    private bool openRoutineStarted;
    private bool isMimic;
    private bool isBossDrop;
    private bool isDroppedRewardChest;
    private bool playerInRange;
    private ChestRarity chestRarity = ChestRarity.Normal;
    private MimicChestController mimicController;
    private PlayerStats cachedPlayerStats;

    public ChestRarity Rarity => chestRarity;
    public bool IsMimic => isMimic;
    public bool IsDroppedRewardChest => isDroppedRewardChest;
    public bool RequiresManualOpen => !isDroppedRewardChest && !isMimic;

    private void Start()
    {
        if (isDroppedRewardChest)
        {
            ApplyDroppedRewardState();
            return;
        }

        EnsurePriceText();
        ApplyVisuals();
        UpdatePrice();
    }

    public void Configure(ChestRarity rarity, bool bossDrop = false)
    {
        ConfigureDroppedReward(rarity, bossDrop);
    }

    public void ConfigureDroppedReward(ChestRarity rarity, bool bossPresentation = false)
    {
        chestRarity = rarity;
        isDroppedRewardChest = true;
        isBossDrop = bossPresentation;
        isMimic = false;
        ApplyVisuals();
        ApplyDroppedRewardState();
    }

    public void ConfigureMapChest(ChestRarity rarity, bool mimic)
    {
        chestRarity = rarity;
        isBossDrop = false;
        isDroppedRewardChest = false;
        isMimic = mimic;

        if (isMimic)
        {
            EnsureMimicController();
            mimicController.Initialize(this, chestRarity);
        }

        ApplyVisuals();
    }

    public void ApplyBossDropTextColor()
    {
        ChestVisual chestVisual = GetComponent<ChestVisual>();

        if (chestVisual != null)
        {
            chestVisual.ApplyBossPresentation();
        }
    }

    private void Update()
    {
        if (isOpened || openRoutineStarted || isDroppedRewardChest || isMimic || !playerInRange)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryOpenNormalChest();
        }
    }

    private void EnsureMimicController()
    {
        if (mimicController != null) return;

        mimicController = GetComponent<MimicChestController>();

        if (mimicController == null)
        {
            mimicController = gameObject.AddComponent<MimicChestController>();
        }
    }

    private void ApplyVisuals()
    {
        if (isMimic)
        {
            return;
        }

        ChestVisual chestVisual = GetComponent<ChestVisual>();

        if (chestVisual != null)
        {
            chestVisual.ApplyRarity(chestRarity);
            return;
        }

        Renderer chestRenderer = GetComponent<Renderer>();

        if (chestRenderer != null)
        {
            Color chestColor = ChestRarityUtility.GetChestColor(chestRarity);
            bool glow = chestRarity != ChestRarity.Normal;
            float smoothness = chestRarity == ChestRarity.Epic ? 0.78f : 0.42f;
            GameVisualStyle.ApplyColor(chestRenderer, chestColor, smoothness, glow);
        }
    }

    private void ApplyDroppedRewardState()
    {
        HidePriceText();

        ChestVisual chestVisual = GetComponent<ChestVisual>();

        if (chestVisual != null)
        {
            chestVisual.ApplyDroppedRewardPresentation(isBossDrop);
        }
        else
        {
            ChestOpeningPresentation.ApplyIdleOpenLid(transform, -28f);
        }

        ChestVisualAnimator visualAnimator = GetComponent<ChestVisualAnimator>();

        if (visualAnimator != null)
        {
            visualAnimator.Configure(chestRarity, isBossDrop);
        }
    }

    private void EnsurePriceText()
    {
        if (priceText != null) return;

        Transform existingPriceText = transform.Find("PriceText");

        if (existingPriceText != null)
        {
            priceText = existingPriceText.GetComponent<TMP_Text>();

            if (priceText != null) return;
        }

        GameObject priceTextObject = new GameObject("PriceText");
        priceTextObject.transform.SetParent(transform, false);
        priceTextObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        priceTextObject.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);

        TextMeshPro textMeshPro = priceTextObject.AddComponent<TextMeshPro>();
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.fontSize = 5f;
        textMeshPro.color = Color.yellow;

        priceText = textMeshPro;
    }

    private void HidePriceText()
    {
        EnsurePriceText();

        if (priceText != null)
        {
            priceText.gameObject.SetActive(false);
        }
    }

    private void UpdatePrice()
    {
        if (isDroppedRewardChest)
        {
            return;
        }

        float runTime = Time.timeSinceLevelLoad;

        if (runTime < 30f)
        {
            price = basePrice;
        }
        else if (runTime < 60f)
        {
            price = 15;
        }
        else if (runTime < 90f)
        {
            price = 20;
        }
        else
        {
            price = 25;
        }

        RefreshPriceLabel(false);
    }

    private void RefreshPriceLabel(bool showInteractHint)
    {
        if (priceText == null || isDroppedRewardChest)
        {
            return;
        }

        priceText.text = showInteractHint
            ? price + " Coin  [E]"
            : price + " Coin";
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened || openRoutineStarted) return;
        if (!other.CompareTag("Player")) return;

        if (isMimic)
        {
            if (mimicController == null)
            {
                EnsureMimicController();
                mimicController.Initialize(this, chestRarity);
            }

            isOpened = true;
            mimicController.Activate();
            return;
        }

        PlayerStats playerStats = other.GetComponent<PlayerStats>();

        if (playerStats == null) return;

        if (isDroppedRewardChest)
        {
            BeginChestOpen();
            return;
        }

        cachedPlayerStats = playerStats;
        playerInRange = true;
        RefreshPriceLabel(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
        cachedPlayerStats = null;
        RefreshPriceLabel(false);
    }

    private void TryOpenNormalChest()
    {
        if (isOpened || openRoutineStarted || isDroppedRewardChest || isMimic) return;

        if (cachedPlayerStats == null)
        {
            cachedPlayerStats = FindPlayerStatsInRange();
        }

        if (cachedPlayerStats == null) return;

        if (!cachedPlayerStats.SpendCoins(price)) return;

        BeginChestOpen();
    }

    private PlayerStats FindPlayerStatsInRange()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        return player != null ? player.GetComponent<PlayerStats>() : null;
    }

    private void BeginChestOpen()
    {
        if (isOpened || openRoutineStarted) return;

        isOpened = true;
        openRoutineStarted = true;
        playerInRange = false;
        ChestRevealPause.Begin();
        DisableOpenInteraction();
        StartCoroutine(CompleteChestOpen());
    }

    private void DisableOpenInteraction()
    {
        Collider chestCollider = GetComponent<Collider>();

        if (chestCollider != null)
        {
            chestCollider.enabled = false;
        }

        HidePriceText();
    }

    private IEnumerator CompleteChestOpen()
    {
        Vector3 openPosition = transform.position;
        ChestRarity rarity = chestRarity;

        AudioManager.Instance?.PlayChestOpen();
        yield return ChestOpenPresentation.PlayRevealThenOpenUpgradeMenu(openPosition, rarity, transform);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (openRoutineStarted && ChestRevealPause.IsPaused)
        {
            ChestRevealPause.ForceEnd();
        }
    }
}
