using UnityEngine;

[DisallowMultipleComponent]
public class GoldenDragonVisual : MonoBehaviour
{
    private const string VisualRootName = "GoldenDragonVisualRoot";

    private static readonly Color BodyColor = new Color(0.92f, 0.72f, 0.14f);
    private static readonly Color WingColor = new Color(0.78f, 0.58f, 0.08f);
    private static readonly Color HornColor = new Color(1f, 0.86f, 0.28f);
    private static readonly Color EyeColor = new Color(1f, 0.95f, 0.42f);
    private static readonly Color GlowColor = new Color(1f, 0.82f, 0.18f);

    public void BuildVisual()
    {
        Transform existing = transform.Find(VisualRootName);

        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        Renderer rootRenderer = GetComponent<Renderer>();

        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        Transform visualRoot = CreateVisualRoot();

        CreatePart(visualRoot, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(1.35f, 0.9f, 1.25f), BodyColor, 0.58f, true, 0.28f);
        CreatePart(visualRoot, "Neck", PrimitiveType.Capsule, new Vector3(0f, 1.45f, 0.35f), new Vector3(0.55f, 0.32f, 0.55f), BodyColor, 0.52f, true, 0.22f);
        CreatePart(visualRoot, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0.72f), new Vector3(0.78f, 0.68f, 0.78f), BodyColor, 0.6f, true, 0.32f);
        CreatePart(visualRoot, "ChestGlow", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0.42f), new Vector3(0.42f, 0.42f, 0.42f), GlowColor, 0.72f, true, 0.82f);
        CreatePart(visualRoot, "Wing_L", PrimitiveType.Cube, new Vector3(-1.15f, 1.2f, 0.05f), new Vector3(1.55f, 0.08f, 1.05f), WingColor, 0.42f, true, 0.18f);
        CreatePart(visualRoot, "Wing_R", PrimitiveType.Cube, new Vector3(1.15f, 1.2f, 0.05f), new Vector3(1.55f, 0.08f, 1.05f), WingColor, 0.42f, true, 0.18f);
        CreatePart(visualRoot, "Horn_L", PrimitiveType.Cylinder, new Vector3(-0.2f, 2.05f, 0.85f), new Vector3(0.1f, 0.2f, 0.1f), HornColor, 0.68f, true, 0.55f);
        CreatePart(visualRoot, "Horn_R", PrimitiveType.Cylinder, new Vector3(0.2f, 2.05f, 0.85f), new Vector3(0.1f, 0.2f, 0.1f), HornColor, 0.68f, true, 0.55f);
        CreatePart(visualRoot, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.16f, 1.9f, 1.05f), new Vector3(0.12f, 0.12f, 0.12f), EyeColor, 0.24f, true, 0.72f);
        CreatePart(visualRoot, "Eye_R", PrimitiveType.Sphere, new Vector3(0.16f, 1.9f, 1.05f), new Vector3(0.12f, 0.12f, 0.12f), EyeColor, 0.24f, true, 0.72f);
        CreatePart(visualRoot, "Tail", PrimitiveType.Capsule, new Vector3(0f, 0.72f, -0.72f), new Vector3(0.32f, 0.22f, 0.32f), WingColor, 0.45f, true, 0.16f);
    }

    private Transform CreateVisualRoot()
    {
        GameObject rootObject = new GameObject(VisualRootName);
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.identity;
        rootObject.transform.localScale = Vector3.one;
        return rootObject.transform;
    }

    private static void CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow,
        float emission = 0f)
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

        if (renderer == null) return;

        Material baseMaterial = partName.Contains("Glow") || partName.Contains("Eye")
            ? ChestVisualMaterials.GetGlowBaseMaterial()
            : ChestVisualMaterials.GetMetalBaseMaterial();

        if (baseMaterial != null)
        {
            renderer.sharedMaterial = baseMaterial;
        }

        GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
    }
}
