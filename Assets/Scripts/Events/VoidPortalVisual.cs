using UnityEngine;

[DisallowMultipleComponent]
public class VoidPortalVisual : MonoBehaviour
{
    private const string VisualRootName = "VoidPortalVisualRoot";

    private static readonly Color CoreColor = new Color(0.42f, 0.08f, 0.72f);
    private static readonly Color RingColor = new Color(0.58f, 0.12f, 0.88f);
    private static readonly Color GlowColor = new Color(0.72f, 0.22f, 1f);

    private Transform visualRoot;
    private Transform glowTransform;
    private Transform ringTransform;

    public void BuildVisual()
    {
        Transform existing = transform.Find(VisualRootName);

        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        visualRoot = CreateVisualRoot();

        CreatePart(visualRoot, "VoidCore", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.05f, 1.1f), CoreColor, 0.72f, true, 0.62f);
        CreatePart(visualRoot, "VoidRing", PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0f), new Vector3(1.45f, 0.04f, 1.45f), RingColor, 0.68f, true, 0.72f);
        CreatePart(visualRoot, "VoidGlow", PrimitiveType.Sphere, new Vector3(0f, 0.72f, 0f), new Vector3(0.75f, 0.75f, 0.75f), GlowColor, 0.78f, true, 0.85f);
        CreatePart(visualRoot, "VoidSpire_L", PrimitiveType.Cylinder, new Vector3(-0.42f, 0.95f, 0f), new Vector3(0.08f, 0.35f, 0.08f), RingColor, 0.62f, true, 0.55f);
        CreatePart(visualRoot, "VoidSpire_R", PrimitiveType.Cylinder, new Vector3(0.42f, 0.95f, 0f), new Vector3(0.08f, 0.35f, 0.08f), RingColor, 0.62f, true, 0.55f);

        glowTransform = visualRoot.Find("VoidGlow");
        ringTransform = visualRoot.Find("VoidRing");
    }

    private void Update()
    {
        if (visualRoot == null) return;

        visualRoot.Rotate(Vector3.up, 95f * Time.deltaTime, Space.World);

        float pulse = 1f + Mathf.Sin(Time.time * 4.2f) * 0.08f;
        visualRoot.localScale = new Vector3(pulse, 1f, pulse);

        if (glowTransform != null)
        {
            float glowPulse = 1f + Mathf.Sin(Time.time * 5.8f) * 0.12f;
            glowTransform.localScale = Vector3.one * (0.75f * glowPulse);
            glowTransform.localPosition = new Vector3(0f, 0.72f + Mathf.Sin(Time.time * 4.8f) * 0.05f, 0f);
        }

        if (ringTransform != null)
        {
            float ringPulse = 1f + Mathf.Sin(Time.time * 3.6f + 0.8f) * 0.05f;
            ringTransform.localScale = new Vector3(1.45f * ringPulse, 0.04f, 1.45f * ringPulse);
        }
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
        float emission)
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

        renderer.sharedMaterial = ChestVisualMaterials.GetGlowBaseMaterial();
        GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
    }
}
