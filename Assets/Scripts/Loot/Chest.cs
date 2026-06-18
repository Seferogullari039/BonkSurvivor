using System.Collections;
using TMPro;
using UnityEngine;

public class Chest : MonoBehaviour
{
    [SerializeField] private int basePrice = 10;
    [SerializeField] private TMP_Text priceText;

    private int price;
    private bool isOpened;
    private bool isMimic;
    private bool isBossDrop;
    private ChestRarity chestRarity = ChestRarity.Normal;
    private MimicChestController mimicController;

    public ChestRarity Rarity => chestRarity;
    public bool IsMimic => isMimic;

    private void Start()
    {
        EnsurePriceText();
        ApplyVisuals();
        UpdatePrice();
    }

    public void Configure(ChestRarity rarity, bool bossDrop = false)
    {
        chestRarity = rarity;
        isBossDrop = bossDrop;
        isMimic = false;
        ApplyVisuals();
    }

    public void ConfigureMapChest(ChestRarity rarity, bool mimic)
    {
        chestRarity = rarity;
        isBossDrop = false;
        isMimic = mimic;

        if (isMimic)
        {
            EnsureMimicController();
            mimicController.Initialize(this, chestRarity);
        }

        ApplyVisuals();
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

    public void ApplyBossDropTextColor()
    {
        EnsurePriceText();

        if (priceText != null)
        {
            priceText.color = new Color(1f, 0.95f, 0.65f);
        }

        ChestVisual chestVisual = GetComponent<ChestVisual>();

        if (chestVisual != null)
        {
            chestVisual.ApplyBossPresentation();
        }
    }

    private void UpdatePrice()
    {
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

        if (priceText != null)
        {
            priceText.text = price + " Coin";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOpened) return;
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

        if (!playerStats.SpendCoins(price)) return;

        isOpened = true;
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

        if (priceText != null)
        {
            priceText.gameObject.SetActive(false);
        }
    }

    private IEnumerator CompleteChestOpen()
    {
        Vector3 openPosition = transform.position;
        ChestRarity rarity = chestRarity;

        AudioManager.Instance?.PlayChestOpen();
        yield return ChestOpenPresentation.PlayRevealThenOpenUpgradeMenu(openPosition, rarity, transform);
        Destroy(gameObject);
    }
}
