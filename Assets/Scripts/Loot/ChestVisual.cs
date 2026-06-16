using UnityEngine;

[DisallowMultipleComponent]
public class ChestVisual : MonoBehaviour
{
    private const string VisualRootName = "ChestVisualRoot";

    public ChestRarity rarity = ChestRarity.Normal;
    public bool buildOnAwake = true;

    private Transform visualRoot;
    private Renderer glowRenderer;
    private Light glowLight;
    private bool bossPresentation;

    private void Awake()
    {
        if (buildOnAwake)
        {
            BuildVisual();
        }
    }

    public void SetRarity(ChestRarity newRarity)
    {
        rarity = newRarity;
    }

    public void ApplyRarity(ChestRarity newRarity)
    {
        SetRarity(newRarity);

        if (visualRoot == null)
        {
            BuildVisual();
        }
        else
        {
            ApplyMaterials();
        }
    }

    public void ApplyBossPresentation()
    {
        bossPresentation = true;
        transform.localScale = Vector3.one * 1.2f;
        ApplyMaterials();
    }

    public void BuildVisual()
    {
        ClearVisualRoot();
        HideLegacyRootMesh();

        visualRoot = CreateVisualRoot();

        CreatePart(visualRoot, "Body", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.9f));
        CreatePart(visualRoot, "Lid", PrimitiveType.Cube, new Vector3(0f, 0.82f, 0f), new Vector3(1.28f, 0.32f, 0.95f));
        CreatePart(visualRoot, "Lock", PrimitiveType.Cube, new Vector3(0f, 0.55f, -0.48f), new Vector3(0.22f, 0.26f, 0.08f));
        CreatePart(visualRoot, "MetalBand_Left", PrimitiveType.Cube, new Vector3(-0.58f, 0.42f, 0f), new Vector3(0.08f, 0.62f, 0.92f));
        CreatePart(visualRoot, "MetalBand_Right", PrimitiveType.Cube, new Vector3(0.58f, 0.42f, 0f), new Vector3(0.08f, 0.62f, 0.92f));
        CreatePart(visualRoot, "MetalBand_Front", PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.46f), new Vector3(1.18f, 0.12f, 0.08f));
        CreatePart(visualRoot, "MetalBand_Top", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(1.22f, 0.08f, 0.9f));

        GameObject glowObject = CreatePart(
            visualRoot,
            "Glow",
            PrimitiveType.Sphere,
            new Vector3(0f, 0.55f, 0f),
            new Vector3(1.55f, 1.1f, 1.55f));
        glowRenderer = glowObject.GetComponent<Renderer>();

        GameObject lightObject = new GameObject("GlowLight");
        lightObject.transform.SetParent(visualRoot, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.range = 2.4f;
        glowLight.shadows = LightShadows.None;

        ApplyMaterials();
    }

    private void ApplyMaterials()
    {
        if (visualRoot == null) return;

        ApplyWoodMaterial(visualRoot, "Body", rarity);
        ApplyWoodMaterial(visualRoot, "Lid", rarity);
        ApplyLockMaterial(visualRoot, "Lock", rarity);
        ApplyMetalMaterial(visualRoot, "MetalBand_Left", rarity);
        ApplyMetalMaterial(visualRoot, "MetalBand_Right", rarity);
        ApplyMetalMaterial(visualRoot, "MetalBand_Front", rarity);
        ApplyMetalMaterial(visualRoot, "MetalBand_Top", rarity);

        if (glowRenderer != null)
        {
            float glowIntensity = rarity switch
            {
                ChestRarity.Epic => bossPresentation ? 0.85f : 0.72f,
                ChestRarity.Rare => bossPresentation ? 0.68f : 0.55f,
                _ => bossPresentation ? 0.52f : 0.42f
            };
            ChestVisualMaterials.ApplyGlow(glowRenderer, rarity, glowIntensity);
        }

        if (glowLight != null)
        {
            glowLight.color = ChestVisualMaterials.GetGlowColor(rarity);
            glowLight.intensity = bossPresentation ? 1.1f : 0.75f;
        }
    }

    private static void ApplyWoodMaterial(Transform root, string partName, ChestRarity chestRarity)
    {
        Transform part = root.Find(partName);
        if (part == null) return;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null) return;

        ChestVisualMaterials.ApplyWood(renderer, chestRarity);
    }

    private static void ApplyMetalMaterial(Transform root, string partName, ChestRarity chestRarity)
    {
        Transform part = root.Find(partName);
        if (part == null) return;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null) return;

        ChestVisualMaterials.ApplyMetal(renderer, chestRarity);
    }

    private static void ApplyLockMaterial(Transform root, string partName, ChestRarity chestRarity)
    {
        Transform part = root.Find(partName);
        if (part == null) return;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null) return;

        ChestVisualMaterials.ApplyLock(renderer, chestRarity);
    }

    private void ClearVisualRoot()
    {
        Transform existing = transform.Find(VisualRootName);
        if (existing == null) return;

        if (Application.isPlaying)
        {
            Destroy(existing.gameObject);
        }
        else
        {
            DestroyImmediate(existing.gameObject);
        }

        visualRoot = null;
        glowRenderer = null;
        glowLight = null;
    }

    private void HideLegacyRootMesh()
    {
        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
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

    private static GameObject CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale)
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
            if (Application.isPlaying)
            {
                Destroy(partCollider);
            }
            else
            {
                DestroyImmediate(partCollider);
            }
        }

        return partObject;
    }
}
