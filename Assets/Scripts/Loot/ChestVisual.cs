using UnityEngine;

[DisallowMultipleComponent]
public class ChestVisual : MonoBehaviour
{
    private const string VisualRootName = "ChestVisualRoot";
    private const string BaseName = "ChestBase";
    private const string LidName = "ChestLid";
    private const string TrimName = "ChestTrim";
    private const string GlowName = "ChestGlow";

    public ChestRarity rarity = ChestRarity.Normal;
    public bool buildOnAwake = true;

    private Transform visualRoot;
    private Transform trimRoot;
    private Renderer glowRenderer;
    private Light glowLight;
    private bool bossPresentation;
    private bool droppedRewardPresentation;
    private bool proximityHighlight;
    private bool openingHighlight;
    private float proximityPulseTime;
    private float storedGlowIntensity;
    private float storedLightIntensity;

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

    public void SetProximityHighlight(bool active)
    {
        proximityHighlight = active;

        if (!active)
        {
            proximityPulseTime = 0f;
            ApplyMaterials();
        }
    }

    public void SetOpeningHighlight(bool active)
    {
        openingHighlight = active;

        if (active)
        {
            proximityHighlight = false;
            proximityPulseTime = 0f;
            return;
        }

        ApplyMaterials();
    }

    private void Update()
    {
        if (openingHighlight)
        {
            UpdateOpeningPulse();
            return;
        }

        if (!proximityHighlight || glowLight == null)
        {
            return;
        }

        proximityPulseTime += Time.deltaTime * 3.6f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(proximityPulseTime);
        float intensityBoost = 1f + pulse * 0.38f;

        glowLight.intensity = storedLightIntensity * intensityBoost;

        if (glowRenderer != null)
        {
            ChestVisualMaterials.ApplyGlow(glowRenderer, rarity, storedGlowIntensity * intensityBoost);
        }
    }

    private void UpdateOpeningPulse()
    {
        if (glowLight == null)
        {
            return;
        }

        proximityPulseTime += Time.unscaledDeltaTime * 5.2f;
        float pulse = 0.5f + 0.5f * Mathf.Sin(proximityPulseTime);
        float intensityBoost = 1f + pulse * 0.55f;

        glowLight.intensity = storedLightIntensity * intensityBoost;

        if (glowRenderer != null)
        {
            ChestVisualMaterials.ApplyGlow(glowRenderer, rarity, storedGlowIntensity * intensityBoost);
        }
    }

    public void ApplyDroppedRewardPresentation(bool bossPresentationOverride = false)
    {
        droppedRewardPresentation = true;

        if (visualRoot == null)
        {
            BuildVisual();
        }

        if (bossPresentationOverride)
        {
            ApplyBossPresentation();
        }

        SetLidIdleOpen(-28f);
        ApplyMaterials();
    }

    private void SetLidIdleOpen(float openAngle)
    {
        if (visualRoot == null)
        {
            return;
        }

        Transform lid = visualRoot.Find(LidName);

        if (lid == null)
        {
            lid = visualRoot.Find("Lid");
        }

        if (lid != null)
        {
            lid.localRotation = Quaternion.Euler(openAngle, 0f, 0f);
        }
    }

    public void BuildVisual()
    {
        ClearVisualRoot();
        HideLegacyRootMesh();

        visualRoot = CreateVisualRoot();
        trimRoot = CreateTrimRoot(visualRoot);

        CreatePart(visualRoot, BaseName, PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(1.1f, 0.64f, 0.85f));
        CreatePart(visualRoot, LidName, PrimitiveType.Cube, new Vector3(0f, 0.78f, 0f), new Vector3(1.16f, 0.28f, 0.88f));
        CreatePart(trimRoot, "Lock", PrimitiveType.Cube, new Vector3(0f, 0.52f, -0.44f), new Vector3(0.18f, 0.22f, 0.08f));
        CreatePart(trimRoot, "MetalBand_Left", PrimitiveType.Cube, new Vector3(-0.54f, 0.38f, 0f), new Vector3(0.07f, 0.58f, 0.88f));
        CreatePart(trimRoot, "MetalBand_Right", PrimitiveType.Cube, new Vector3(0.54f, 0.38f, 0f), new Vector3(0.07f, 0.58f, 0.88f));
        CreatePart(trimRoot, "MetalBand_Front", PrimitiveType.Cube, new Vector3(0f, 0.38f, -0.42f), new Vector3(1.08f, 0.10f, 0.07f));
        CreatePart(trimRoot, "MetalBand_Top", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), new Vector3(1.12f, 0.07f, 0.86f));

        GameObject glowObject = CreatePart(
            visualRoot,
            GlowName,
            PrimitiveType.Sphere,
            new Vector3(0f, 0.58f, 0f),
            new Vector3(0.52f, 0.34f, 0.52f));
        glowRenderer = glowObject.GetComponent<Renderer>();

        GameObject lightObject = new GameObject("GlowLight");
        lightObject.transform.SetParent(visualRoot, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        glowLight = lightObject.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.range = 1.15f;
        glowLight.shadows = LightShadows.None;

        ApplyMaterials();
    }

    private void ApplyMaterials()
    {
        if (visualRoot == null)
        {
            return;
        }

        ApplyBodyMaterial(visualRoot, BaseName, rarity);
        ApplyBodyMaterial(visualRoot, LidName, rarity);
        ApplyBodyMaterial(visualRoot, "Body", rarity);
        ApplyBodyMaterial(visualRoot, "Lid", rarity);

        Transform trim = trimRoot != null ? trimRoot : visualRoot.Find(TrimName);
        if (trim != null)
        {
            ApplyTrimMaterial(trim, "Lock", rarity);
            ApplyTrimMaterial(trim, "MetalBand_Left", rarity);
            ApplyTrimMaterial(trim, "MetalBand_Right", rarity);
            ApplyTrimMaterial(trim, "MetalBand_Front", rarity);
            ApplyTrimMaterial(trim, "MetalBand_Top", rarity);
        }

        ApplyTrimMaterial(visualRoot, "Lock", rarity);
        ApplyTrimMaterial(visualRoot, "MetalBand_Left", rarity);
        ApplyTrimMaterial(visualRoot, "MetalBand_Right", rarity);
        ApplyTrimMaterial(visualRoot, "MetalBand_Front", rarity);
        ApplyTrimMaterial(visualRoot, "MetalBand_Top", rarity);

        if (glowRenderer == null)
        {
            Transform glowTransform = visualRoot.Find(GlowName);
            if (glowTransform == null)
            {
                glowTransform = visualRoot.Find("Glow");
            }

            if (glowTransform != null)
            {
                glowRenderer = glowTransform.GetComponent<Renderer>();
            }
        }

        if (glowRenderer != null)
        {
            glowRenderer.transform.localScale = droppedRewardPresentation
                ? new Vector3(0.58f, 0.38f, 0.58f)
                : new Vector3(0.52f, 0.34f, 0.52f);

            float glowIntensity = rarity switch
            {
                ChestRarity.Epic => bossPresentation ? 0.38f : 0.30f,
                ChestRarity.Rare => bossPresentation ? 0.32f : 0.24f,
                _ => bossPresentation ? 0.18f : 0.12f
            };

            if (droppedRewardPresentation)
            {
                glowIntensity *= 1.5f;
            }

            storedGlowIntensity = glowIntensity;
            ChestVisualMaterials.ApplyGlow(glowRenderer, rarity, glowIntensity);
        }

        if (glowLight != null)
        {
            glowLight.color = ChestVisualMaterials.GetGlowColor(rarity);

            if (droppedRewardPresentation)
            {
                storedLightIntensity = bossPresentation ? 0.52f : 0.36f;
                glowLight.range = 1.35f;
            }
            else
            {
                storedLightIntensity = bossPresentation ? 0.42f : 0.22f;
                glowLight.range = 1.15f;
            }

            glowLight.intensity = storedLightIntensity;
        }
    }

    private static void ApplyBodyMaterial(Transform root, string partName, ChestRarity chestRarity)
    {
        Transform part = root.Find(partName);
        if (part == null)
        {
            return;
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        ChestVisualMaterials.ApplyBody(renderer, chestRarity);
    }

    private static void ApplyTrimMaterial(Transform root, string partName, ChestRarity chestRarity)
    {
        Transform part = root.Find(partName);
        if (part == null)
        {
            return;
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        if (partName == "Lock")
        {
            ChestVisualMaterials.ApplyLock(renderer, chestRarity);
            return;
        }

        ChestVisualMaterials.ApplyTrim(renderer, chestRarity);
    }

    private void ClearVisualRoot()
    {
        Transform existing = transform.Find(VisualRootName);
        if (existing == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(existing.gameObject);
        }
        else
        {
            DestroyImmediate(existing.gameObject);
        }

        visualRoot = null;
        trimRoot = null;
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

    private static Transform CreateTrimRoot(Transform parent)
    {
        GameObject trimObject = new GameObject(TrimName);
        trimObject.transform.SetParent(parent, false);
        trimObject.transform.localPosition = Vector3.zero;
        trimObject.transform.localRotation = Quaternion.identity;
        trimObject.transform.localScale = Vector3.one;
        return trimObject.transform;
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
