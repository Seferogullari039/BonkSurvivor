using UnityEngine;

[DisallowMultipleComponent]
public class MimicChestController : MonoBehaviour
{
    private static readonly Color MimicBodyColor = new Color(0.72f, 0.14f, 0.12f);
    private static readonly Color MimicEyeColor = new Color(1f, 0.92f, 0.2f);

    private Chest ownerChest;
    private Transform playerTarget;
    private GameObject hitProxy;
    private int maxHealth = 10;
    private int currentHealth;
    private float moveSpeed = 3.5f;
    private float contactDamage = 1f;
    private float lastContactDamageTime = -999f;
    private bool isActivated;

    public bool IsActivated => isActivated;
    public int CurrentHealth => currentHealth;

    public void Initialize(Chest chest, ChestRarity rarity)
    {
        ownerChest = chest;
        maxHealth = rarity switch
        {
            ChestRarity.Epic => 18,
            ChestRarity.Rare => 14,
            _ => 10
        };
        currentHealth = maxHealth;
        EnsureBaseMaterials(rarity);
        ApplyIdleVisuals();
    }

    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;
        HidePriceText();
        ApplyAwakenedVisuals();
        EnsureHitProxy();
        CachePlayerTarget();
    }

    public void TakeDamage(int damage)
    {
        if (!isActivated || damage <= 0) return;

        currentHealth -= damage;

        if (currentHealth > 0) return;

        GrantRewards();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!isActivated || playerTarget == null) return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f) return;

        float stopDistance = 1.1f;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        }

        TryDamagePlayer(distance);
    }

    private void TryDamagePlayer(float distance)
    {
        if (distance > 1.35f) return;
        if (Time.time - lastContactDamageTime < 0.9f) return;

        PlayerStats playerStats = playerTarget.GetComponent<PlayerStats>();

        if (playerStats == null) return;

        playerStats.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(contactDamage)));
        lastContactDamageTime = Time.time;
    }

    private void GrantRewards()
    {
        if (ownerChest == null) return;

        ChestRarity rarity = ownerChest.Rarity;
        AudioManager.Instance?.PlayChestOpen();

        if (LevelUpManager.Instance != null)
        {
            LevelUpManager.Instance.OpenChestUpgradeMenu(rarity);
        }

        if (JuiceManager.Instance != null)
        {
            JuiceManager.Instance.PlayChestOpen(transform.position);
        }
    }

    private void HidePriceText()
    {
        Transform priceTransform = transform.Find("PriceText");

        if (priceTransform != null)
        {
            priceTransform.gameObject.SetActive(false);
        }
    }

    private void ApplyIdleVisuals()
    {
        ApplyTintToRenderers(MimicBodyColor, 0.52f, true, 0.22f);
        EnsureEyes();
    }

    private void ApplyAwakenedVisuals()
    {
        transform.localScale *= 1.08f;
        ApplyTintToRenderers(new Color(0.86f, 0.16f, 0.12f), 0.58f, true, 0.35f);
        EnsureEyes(true);
    }

    private void EnsureEyes(bool awakened = false)
    {
        if (transform.Find("MimicEye_L") != null) return;

        CreateEye("MimicEye_L", new Vector3(-0.18f, 0.95f, 0.42f), awakened ? 0.12f : 0.08f);
        CreateEye("MimicEye_R", new Vector3(0.18f, 0.95f, 0.42f), awakened ? 0.12f : 0.08f);
    }

    private void CreateEye(string eyeName, Vector3 localPosition, float size)
    {
        GameObject eyeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeObject.name = eyeName;
        eyeObject.transform.SetParent(transform, false);
        eyeObject.transform.localPosition = localPosition;
        eyeObject.transform.localScale = Vector3.one * size;

        Collider eyeCollider = eyeObject.GetComponent<Collider>();

        if (eyeCollider != null)
        {
            Destroy(eyeCollider);
        }

        Renderer eyeRenderer = eyeObject.GetComponent<Renderer>();

        if (eyeRenderer != null)
        {
            eyeRenderer.sharedMaterial = ChestVisualMaterials.GetMetalBaseMaterial();
            GameVisualStyle.ApplyColor(eyeRenderer, MimicEyeColor, 0.72f, true, 0.45f);
        }
    }

    private void ApplyTintToRenderers(Color color, float smoothness, bool glow, float emission)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null) continue;
            if (renderer.transform.name.StartsWith("MimicEye")) continue;
            if (renderer.transform.name == "PriceText") continue;
            if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;

            EnsureRendererMaterial(renderer);
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
        }
    }

    private void EnsureBaseMaterials(ChestRarity rarity)
    {
        ChestVisual chestVisual = GetComponent<ChestVisual>();

        if (chestVisual != null)
        {
            chestVisual.ApplyRarity(rarity);
            return;
        }

        Renderer chestRenderer = GetComponent<Renderer>();

        if (chestRenderer == null || !chestRenderer.enabled) return;

        ChestVisualMaterials.ApplyWood(chestRenderer, rarity);
    }

    private static void EnsureRendererMaterial(Renderer renderer)
    {
        if (renderer.sharedMaterial != null) return;

        string partName = renderer.transform.name;

        if (partName == "Glow")
        {
            renderer.sharedMaterial = ChestVisualMaterials.GetGlowBaseMaterial();
            return;
        }

        if (partName.StartsWith("MetalBand") || partName == "Lock")
        {
            renderer.sharedMaterial = ChestVisualMaterials.GetMetalBaseMaterial();
            return;
        }

        renderer.sharedMaterial = ChestVisualMaterials.GetWoodBaseMaterial();
    }

    private void EnsureHitProxy()
    {
        if (hitProxy != null) return;

        hitProxy = new GameObject("MimicHitProxy");
        hitProxy.transform.SetParent(transform, false);
        hitProxy.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        hitProxy.tag = "Enemy";

        CapsuleCollider collider = hitProxy.AddComponent<CapsuleCollider>();
        collider.isTrigger = false;
        collider.radius = 0.55f;
        collider.height = 1.1f;
        collider.center = Vector3.zero;

        Enemy proxyEnemy = hitProxy.AddComponent<Enemy>();
        proxyEnemy.BindMimicChest(this);
        proxyEnemy.SetMovementLocked(true);
        proxyEnemy.Configure(0f, maxHealth, MimicBodyColor, Enemy.EnemyType.Normal);
    }

    private void CachePlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }
}
