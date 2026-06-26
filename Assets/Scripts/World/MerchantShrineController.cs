using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class MerchantShrineController : MonoBehaviour
{
    private const float InteractRadius = 4f;
    private const float PromptRadius = 9f;
    private const float PromptBaseLocalY = 1.55f;
    private const float PromptFontSize = 1.35f;

    private static readonly Color StoneColor = new Color(0.38f, 0.36f, 0.42f);
    private static readonly Color AccentColor = new Color(0.72f, 0.58f, 0.18f);
    private static readonly Color GlowColor = new Color(0.62f, 0.38f, 0.92f);
    private static readonly Color PromptColor = new Color(0.96f, 0.92f, 0.78f, 0.95f);

    private MerchantShrineManager eventManager;
    private Transform playerTransform;
    private PlayerStats cachedPlayerStats;
    private TextMeshPro promptText;
    private Light shrineLight;
    private bool shopUsed;

    public void Initialize(MerchantShrineManager manager)
    {
        eventManager = manager;
        CachePlayer();
        BuildVisual();
        BuildPromptText();
        BuildTrigger();
    }

    private void Update()
    {
        if (MerchantShrineUI.IsOpen)
        {
            UpdatePrompt(false);
            return;
        }

        CachePlayer();
        UpdatePrompt(CanShowPrompt());

        if (!CanInteract())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryOpenShop();
        }
    }

    private void TryOpenShop()
    {
        if (!CanInteract())
        {
            return;
        }

        OpenShop();
    }

    private void OnDestroy()
    {
        eventManager?.NotifyShrineClosed(this);
    }

    private void CachePlayer()
    {
        if (playerTransform != null && cachedPlayerStats != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            FPSPlayerController fpsController = FindFirstObjectByType<FPSPlayerController>();

            if (fpsController != null)
            {
                playerObject = fpsController.gameObject;
            }
        }

        if (playerObject == null)
        {
            return;
        }

        playerTransform = playerObject.transform;
        cachedPlayerStats = playerObject.GetComponent<PlayerStats>()
            ?? playerObject.GetComponentInChildren<PlayerStats>(true);
    }

    private bool CanShowPrompt()
    {
        if (!CanInteract())
        {
            return false;
        }

        if (playerTransform == null)
        {
            return false;
        }

        Vector3 flatPlayer = playerTransform.position;
        flatPlayer.y = 0f;
        Vector3 flatShrine = transform.position;
        flatShrine.y = 0f;

        return Vector3.Distance(flatPlayer, flatShrine) <= PromptRadius;
    }

    private bool CanInteract()
    {
        if (!MerchantShrineTradeGuards.CanOpenTrade())
        {
            return false;
        }

        if (playerTransform == null)
        {
            return false;
        }

        Vector3 flatPlayer = playerTransform.position;
        flatPlayer.y = 0f;
        Vector3 flatShrine = transform.position;
        flatShrine.y = 0f;

        return Vector3.Distance(flatPlayer, flatShrine) <= InteractRadius;
    }

    private void OpenShop()
    {
        if (cachedPlayerStats == null)
        {
            CachePlayer();
        }

        if (cachedPlayerStats == null)
        {
            return;
        }

        MerchantShrineUI.Open(this, cachedPlayerStats);
    }

    public void NotifyPurchaseComplete()
    {
        shopUsed = true;
    }

    private void BuildVisual()
    {
        GameObject rootObject = new GameObject("MerchantShrineVisual");
        rootObject.transform.SetParent(transform, false);

        CreatePart(rootObject.transform, "PedestalBase", PrimitiveType.Cylinder,
            new Vector3(0f, 0.1f, 0f), new Vector3(1.1f, 0.08f, 1.1f), StoneColor, 0.22f, false);
        CreatePart(rootObject.transform, "PedestalTop", PrimitiveType.Cylinder,
            new Vector3(0f, 0.28f, 0f), new Vector3(0.72f, 0.12f, 0.72f), StoneColor, 0.26f, false);
        CreatePart(rootObject.transform, "GoldTrim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.38f, 0f), new Vector3(0.78f, 0.03f, 0.78f), AccentColor, 0.42f, true);
        CreatePart(rootObject.transform, "Crystal", PrimitiveType.Sphere,
            new Vector3(0f, 0.62f, 0f), new Vector3(0.24f, 0.24f, 0.24f), GlowColor, 0.55f, true);

        GameObject lightObject = new GameObject("MerchantShrineLight");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        shrineLight = lightObject.AddComponent<Light>();
        shrineLight.type = LightType.Point;
        shrineLight.color = new Color(0.88f, 0.72f, 1f);
        shrineLight.range = 7f;
        shrineLight.intensity = 1.35f;
    }

    private void BuildPromptText()
    {
        GameObject textObject = new GameObject("MerchantPrompt");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localPosition = new Vector3(0f, PromptBaseLocalY, 0f);

        promptText = textObject.AddComponent<TextMeshPro>();
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.fontSize = PromptFontSize;
        promptText.fontStyle = FontStyles.Bold;
        promptText.color = PromptColor;
        promptText.text = string.Empty;
        promptText.enabled = false;
        promptText.outlineWidth = 0.24f;
        promptText.outlineColor = new Color(0.08f, 0.05f, 0.12f, 0.9f);

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            promptText.font = font;
        }
    }

    private void BuildTrigger()
    {
        SphereCollider triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = InteractRadius;
        triggerCollider.center = new Vector3(0f, 0.5f, 0f);
    }

    private void UpdatePrompt(bool visible)
    {
        if (promptText == null)
        {
            return;
        }

        if (!visible)
        {
            promptText.enabled = false;
            return;
        }

        promptText.enabled = true;
        promptText.text = shopUsed ? "MERCHANT\n(traded)" : "Press E to trade";

        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        Vector3 lookDirection = promptText.transform.position - camera.transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            promptText.transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        float cameraDistance = Vector3.Distance(camera.transform.position, promptText.transform.position);
        float scale = Mathf.Lerp(0.62f, 0.9f, Mathf.InverseLerp(3f, 16f, cameraDistance));
        promptText.transform.localScale = Vector3.one * scale;
    }

    private static void CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = partName;
        partObject.transform.SetParent(parent, false);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localScale = localScale;
        partObject.transform.localRotation = Quaternion.identity;

        Collider partCollider = partObject.GetComponent<Collider>();

        if (partCollider != null)
        {
            Destroy(partCollider);
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, glow ? 0.55f : 0f);
        }
    }
}
