using UnityEngine;
using UnityEngine.Rendering;

public enum PickupVisualType
{
    Coin,
    XPOrb,
    PlayerProjectile
}

[DisallowMultipleComponent]
public class PickupVisual : MonoBehaviour
{
    private const string VisualChildName = "PickupWorldVisual";

    private static readonly Color XPOrbColor = new Color(0.28f, 0.62f, 0.92f);
    private static readonly Color XPOrbHighlight = new Color(0.42f, 0.76f, 0.98f);
    private static readonly Color CoinBodyColor = new Color(0.92f, 0.72f, 0.16f);
    private static readonly Color CoinRimColor = new Color(0.72f, 0.5f, 0.08f);

    [SerializeField] private PickupVisualType visualType;

    private void Awake()
    {
        Apply(transform, visualType);
    }

    public static void Apply(Transform root, PickupVisualType type)
    {
        if (root == null || HasVisual(root))
        {
            return;
        }

        DisableParentRenderer(root);

        switch (type)
        {
            case PickupVisualType.Coin:
                BuildCoinVisual(root);
                break;
            case PickupVisualType.XPOrb:
                BuildXpOrbVisual(root);
                break;
            case PickupVisualType.PlayerProjectile:
                BuildProjectileVisual(root);
                break;
        }
    }

    private static bool HasVisual(Transform root)
    {
        return root.Find(VisualChildName) != null || root.Find("WorldVisual") != null;
    }

    private static void DisableParentRenderer(Transform root)
    {
        Renderer parentRenderer = root.GetComponent<Renderer>();

        if (parentRenderer != null)
        {
            parentRenderer.enabled = false;
        }
    }

    private static void BuildXpOrbVisual(Transform root)
    {
        const float size = 0.26f;

        GameObject orb = CreateVisualPrimitive(root, PrimitiveType.Sphere, VisualChildName);
        orb.transform.localScale = new Vector3(size, size * 1.12f, size);
        ConfigureRenderer(orb.GetComponent<Renderer>(), XPOrbColor, 0.62f);

        GameObject highlight = CreateVisualPrimitive(orb.transform, PrimitiveType.Sphere, "Highlight");
        highlight.transform.localPosition = new Vector3(0f, size * 0.16f, size * 0.07f);
        highlight.transform.localScale = Vector3.one * 0.38f;
        ConfigureRenderer(highlight.GetComponent<Renderer>(), XPOrbHighlight, 0.7f);
    }

    private static void BuildCoinVisual(Transform root)
    {
        const float diameter = 0.24f;

        GameObject coin = CreateVisualPrimitive(root, PrimitiveType.Cylinder, VisualChildName);
        coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        coin.transform.localScale = new Vector3(diameter, diameter * 0.11f, diameter);
        ConfigureRenderer(coin.GetComponent<Renderer>(), CoinBodyColor, 0.74f);

        GameObject rim = CreateVisualPrimitive(coin.transform, PrimitiveType.Cylinder, "Rim");
        rim.transform.localRotation = Quaternion.identity;
        rim.transform.localScale = new Vector3(1.1f, 1.2f, 1.1f);
        ConfigureRenderer(rim.GetComponent<Renderer>(), CoinRimColor, 0.58f);
    }

    private static void BuildProjectileVisual(Transform root)
    {
        GameVisualStyle.AttachWorldVisual(
            root,
            PrimitiveType.Sphere,
            GameVisualPalette.PlayerProjectile,
            0.32f,
            0.85f,
            true);
    }

    private static GameObject CreateVisualPrimitive(Transform parent, PrimitiveType primitive, string objectName)
    {
        GameObject visualObject = GameObject.CreatePrimitive(primitive);
        visualObject.name = objectName;
        visualObject.transform.SetParent(parent, false);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;

        Collider collider = visualObject.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        return visualObject;
    }

    private static void ConfigureRenderer(Renderer renderer, Color color, float smoothness)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        GameVisualStyle.ApplyColor(renderer, color, smoothness, false, 0f);
    }
}
