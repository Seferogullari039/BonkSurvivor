using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-90)]
public class SkylandsVisualKit : MonoBehaviour
{
    private const string KitRootName = "SkylandsVisualKit";
    private const string ArenaDressingName = "ArenaDressing";
    private const float WallHeight = 4f;
    private const float BorderThickness = 2f;

    private static readonly Color SkyTint = new Color(0.52f, 0.74f, 0.98f);
    private static readonly Color SkyGroundColor = new Color(0.82f, 0.76f, 0.68f);
    private static readonly Color AmbientSky = new Color(0.62f, 0.76f, 0.94f);
    private static readonly Color AmbientEquator = new Color(0.58f, 0.66f, 0.78f);
    private static readonly Color AmbientGround = new Color(0.34f, 0.4f, 0.44f);
    private static readonly Color FogColor = new Color(0.72f, 0.78f, 0.86f);
    private static readonly Color CloudColor = new Color(0.88f, 0.9f, 0.94f, 0.24f);
    private static readonly Color CloudHighlight = new Color(0.94f, 0.95f, 0.98f, 0.3f);
    private static readonly Color IslandShadow = new Color(0.3f, 0.36f, 0.44f);
    private static readonly Color IslandMid = new Color(0.38f, 0.44f, 0.52f);
    private static readonly Color IslandHighlight = new Color(0.46f, 0.52f, 0.58f);
    private static readonly Color IslandUnderShadow = new Color(0.2f, 0.24f, 0.3f);
    private static readonly Color GrassPatchDark = new Color(0.22f, 0.48f, 0.18f);
    private static readonly Color GrassPatchLight = new Color(0.34f, 0.62f, 0.26f);
    private static readonly Color GrassPatchMuted = new Color(0.28f, 0.54f, 0.21f);
    private static readonly Color CliffRock = new Color(0.34f, 0.38f, 0.42f);
    private static readonly Color CliffDeep = new Color(0.22f, 0.26f, 0.3f);
    private static readonly Color GrassRim = new Color(0.36f, 0.68f, 0.28f);
    private static readonly Color GrassCenter = new Color(0.38f, 0.66f, 0.3f);
    private static readonly Color TurfHighlight = new Color(0.34f, 0.62f, 0.26f);

    private Transform kitRoot;
    private Transform atmosphereRoot;
    private Transform cloudRoot;
    private Transform distantIslandsRoot;
    private Transform arenaDressingRoot;

    private Material skyboxMaterial;
    private Material cloudMaterial;
    private Material islandMaterial;
    private Material cliffMaterial;
    private Material grassMaterial;

    private Material originalSkybox;
    private AmbientMode originalAmbientMode;
    private Color originalAmbientSky;
    private Color originalAmbientEquator;
    private Color originalAmbientGround;
    private bool originalFog;
    private FogMode originalFogMode;
    private float originalFogDensity;
    private Color originalFogColor;
    private Color originalSunColor;
    private float originalSunIntensity;
    private Light cachedSun;

    private int lastArenaSeed = int.MinValue;
    private bool atmosphereBuilt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<SkylandsVisualKit>() != null) return;
        if (FindFirstObjectByType<ProceduralGrassArena>() == null) return;

        GameObject host = new GameObject(KitRootName);
        host.AddComponent<SkylandsVisualKit>();
    }

    private void Awake()
    {
        kitRoot = transform;
        CacheRenderDefaults();
        BuildAtmosphere();
    }

    private void LateUpdate()
    {
        ProceduralGrassArena arena = ProceduralGrassArena.Instance;

        if (arena == null) return;

        if (arena.CurrentRunSeed == lastArenaSeed) return;

        lastArenaSeed = arena.CurrentRunSeed;
        RebuildArenaDressing(arena);
    }

    private void OnDestroy()
    {
        RestoreRenderDefaults();

        if (skyboxMaterial != null) Destroy(skyboxMaterial);
        if (cloudMaterial != null) Destroy(cloudMaterial);
        if (islandMaterial != null) Destroy(islandMaterial);
        if (cliffMaterial != null) Destroy(cliffMaterial);
        if (grassMaterial != null) Destroy(grassMaterial);
    }

    private void CacheRenderDefaults()
    {
        originalSkybox = RenderSettings.skybox;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientSky = RenderSettings.ambientSkyColor;
        originalAmbientEquator = RenderSettings.ambientEquatorColor;
        originalAmbientGround = RenderSettings.ambientGroundColor;
        originalFog = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogColor = RenderSettings.fogColor;

        cachedSun = FindSunLight();

        if (cachedSun != null)
        {
            originalSunColor = cachedSun.color;
            originalSunIntensity = cachedSun.intensity;
        }
    }

    private void RestoreRenderDefaults()
    {
        RenderSettings.skybox = originalSkybox;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientSkyColor = originalAmbientSky;
        RenderSettings.ambientEquatorColor = originalAmbientEquator;
        RenderSettings.ambientGroundColor = originalAmbientGround;
        RenderSettings.fog = originalFog;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogColor = originalFogColor;

        if (cachedSun != null)
        {
            cachedSun.color = originalSunColor;
            cachedSun.intensity = originalSunIntensity;
        }
    }

    private void BuildAtmosphere()
    {
        if (atmosphereBuilt) return;

        atmosphereRoot = CreateRoot("Atmosphere", kitRoot);
        cloudRoot = CreateRoot("CloudLayer", atmosphereRoot);
        distantIslandsRoot = CreateRoot("DistantIslands", atmosphereRoot);

        ApplySkyAndAmbient();
        ApplySunTint();
        BuildCloudLayer();
        BuildDistantIslands();

        cloudRoot.gameObject.AddComponent<SkylandsCloudDrift>();

        atmosphereBuilt = true;
    }

    private void ApplySkyAndAmbient()
    {
        Shader skyShader = Shader.Find("Skybox/Procedural");

        if (skyShader != null)
        {
            skyboxMaterial = new Material(skyShader);
            skyboxMaterial.SetColor("_SkyTint", SkyTint);
            skyboxMaterial.SetColor("_GroundColor", SkyGroundColor);
            skyboxMaterial.SetFloat("_SunSize", 0.028f);
            skyboxMaterial.SetFloat("_SunSizeConvergence", 5f);
            skyboxMaterial.SetFloat("_AtmosphereThickness", 1.05f);
            skyboxMaterial.SetFloat("_Exposure", 1.02f);
            RenderSettings.skybox = skyboxMaterial;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = AmbientSky;
        RenderSettings.ambientEquatorColor = AmbientEquator;
        RenderSettings.ambientGroundColor = AmbientGround;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0019f;
        RenderSettings.fogColor = FogColor;
    }

    private void ApplySunTint()
    {
        if (cachedSun == null) return;

        cachedSun.color = new Color(1f, 0.94f, 0.84f);
        cachedSun.intensity = 1.06f;
    }

    private void BuildCloudLayer()
    {
        cloudMaterial = CreateCloudMaterial();

        Vector3[] cloudPositions =
        {
            new Vector3(-88f, 52f, 64f),
            new Vector3(-36f, 58f, -28f),
            new Vector3(24f, 49f, 78f),
            new Vector3(78f, 55f, 22f),
            new Vector3(112f, 47f, -58f),
            new Vector3(-124f, 51f, -34f),
            new Vector3(12f, 63f, -92f),
            new Vector3(-58f, 44f, 104f),
            new Vector3(68f, 60f, 96f),
            new Vector3(-148f, 53f, 18f),
            new Vector3(142f, 48f, -36f),
            new Vector3(-8f, 66f, 12f),
            new Vector3(46f, 42f, -110f),
            new Vector3(-102f, 57f, 88f)
        };

        float[] cloudSizes =
        {
            14f, 11f, 16f, 12f, 13f, 10f, 15f, 9f, 14f, 12f, 11f, 17f, 10f, 13f
        };

        for (int i = 0; i < cloudPositions.Length; i++)
        {
            CreateCloudCluster(cloudRoot, "Cloud_" + i, cloudPositions[i], cloudSizes[i], i % 4 == 0);
        }
    }

    private void CreateCloudCluster(Transform parent, string name, Vector3 position, float size, bool highlight)
    {
        GameObject clusterRoot = new GameObject(name);
        clusterRoot.transform.SetParent(parent, false);
        clusterRoot.transform.position = position;
        clusterRoot.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 180f), 0f);
        clusterRoot.isStatic = false;

        Vector3[] puffOffsets =
        {
            Vector3.zero,
            new Vector3(size * 0.22f, size * 0.03f, size * 0.08f),
            new Vector3(-size * 0.18f, -size * 0.02f, size * 0.12f),
            new Vector3(size * 0.08f, size * 0.04f, -size * 0.16f)
        };

        Vector3[] puffScales =
        {
            new Vector3(size * 1.1f, size * 0.28f, size * 0.72f),
            new Vector3(size * 0.72f, size * 0.22f, size * 0.55f),
            new Vector3(size * 0.64f, size * 0.2f, size * 0.48f),
            new Vector3(size * 0.58f, size * 0.18f, size * 0.42f)
        };

        for (int i = 0; i < puffOffsets.Length; i++)
        {
            CreateCloudPuff(
                clusterRoot.transform,
                name + "_Puff" + i,
                puffOffsets[i],
                puffScales[i],
                i == 0 && highlight);
        }
    }

    private void CreateCloudPuff(Transform parent, string name, Vector3 localPosition, Vector3 scale, bool highlight)
    {
        GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        puff.name = name;
        puff.transform.SetParent(parent, false);
        puff.transform.localPosition = localPosition;
        puff.transform.localScale = scale;
        RemoveCollider(puff);

        Renderer puffRenderer = puff.GetComponent<Renderer>();

        if (puffRenderer != null)
        {
            puffRenderer.sharedMaterial = cloudMaterial;
            puffRenderer.shadowCastingMode = ShadowCastingMode.Off;
            puffRenderer.receiveShadows = false;
            ApplyMaterialColor(puffRenderer, highlight ? CloudHighlight : CloudColor, 0f);
        }
    }

    private void BuildDistantIslands()
    {
        islandMaterial = CreateOpaqueMaterial();

        System.Random random = new System.Random(0x5F3759DF);

        for (int i = 0; i < 9; i++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2d);
            float radius = Mathf.Lerp(420f, 560f, (float)random.NextDouble());
            float height = Mathf.Lerp(-72f, 8f, (float)random.NextDouble());
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            float islandScale = Mathf.Lerp(0.42f, 0.68f, (float)random.NextDouble());

            CreateDistantIsland(distantIslandsRoot, "DistantIsland_" + i, position, islandScale, random);
        }
    }

    private void CreateDistantIsland(Transform parent, string name, Vector3 position, float scale, System.Random random)
    {
        GameObject islandRoot = new GameObject(name);
        islandRoot.transform.SetParent(parent, false);
        islandRoot.transform.position = position;
        islandRoot.transform.rotation = Quaternion.Euler(0f, (float)(random.NextDouble() * 360d), 0f);
        islandRoot.isStatic = true;

        float baseWidth = Mathf.Lerp(8f, 14f, (float)random.NextDouble()) * scale;
        float baseHeight = Mathf.Lerp(6f, 11f, (float)random.NextDouble()) * scale;
        float topRadius = baseWidth * Mathf.Lerp(0.38f, 0.52f, (float)random.NextDouble());

        CreateVisualPrimitive(
            islandRoot.transform,
            "Base",
            PrimitiveType.Cylinder,
            Vector3.zero,
            new Vector3(baseWidth, baseHeight * 0.5f, baseWidth),
            IslandShadow,
            0.04f,
            islandMaterial);

        CreateVisualPrimitive(
            islandRoot.transform,
            "Top",
            PrimitiveType.Sphere,
            new Vector3(0f, baseHeight * 0.38f, 0f),
            new Vector3(topRadius * 2f, topRadius * 0.82f, topRadius * 2f),
            IslandMid,
            0.05f,
            islandMaterial);

        if (random.NextDouble() > 0.55d)
        {
            CreateVisualPrimitive(
                islandRoot.transform,
                "Spire",
                PrimitiveType.Cylinder,
                new Vector3(0f, baseHeight * 0.62f, 0f),
                new Vector3(topRadius * 0.28f, baseHeight * 0.22f, topRadius * 0.28f),
                IslandHighlight,
                0.06f,
                islandMaterial);
        }

        float stemDepth = Mathf.Lerp(8f, 16f, (float)random.NextDouble()) * scale;

        CreateVisualPrimitive(
            islandRoot.transform,
            "Stem",
            PrimitiveType.Cylinder,
            new Vector3(0f, -stemDepth * 0.5f - 1.5f, 0f),
            new Vector3(baseWidth * 0.16f, stemDepth, baseWidth * 0.16f),
            IslandUnderShadow,
            0.03f,
            islandMaterial);

        CreateVisualPrimitive(
            islandRoot.transform,
            "UnderCliff",
            PrimitiveType.Cylinder,
            new Vector3(0f, -1.8f, 0f),
            new Vector3(baseWidth * 1.18f, 2.4f, baseWidth * 1.18f),
            IslandUnderShadow,
            0.03f,
            islandMaterial);

        CreateVisualPrimitive(
            islandRoot.transform,
            "ShadowCap",
            PrimitiveType.Sphere,
            new Vector3(0f, -3.6f, 0f),
            new Vector3(baseWidth * 1.35f, 1.2f, baseWidth * 1.35f),
            new Color(0.16f, 0.2f, 0.26f),
            0.02f,
            islandMaterial);
    }

    private void RebuildArenaDressing(ProceduralGrassArena arena)
    {
        if (arenaDressingRoot != null)
        {
            Destroy(arenaDressingRoot.gameObject);
        }

        arenaDressingRoot = CreateRoot(ArenaDressingName, kitRoot);
        cliffMaterial = cliffMaterial ?? CreateOpaqueMaterial();
        grassMaterial = grassMaterial ?? CreateOpaqueMaterial();

        float halfX = arena.HalfSizeX;
        float halfZ = arena.HalfSizeZ;

        EnhanceGroundReadability(arena);
        BuildGrassVariation(arena);
        BuildGrassRim(halfX, halfZ);
        BuildCliffUndersides(halfX, halfZ);
        BuildCornerOutcrops(halfX, halfZ);
    }

    private void EnhanceGroundReadability(ProceduralGrassArena arena)
    {
        Transform ground = FindArenaTransform(arena, "ProceduralArena/Ground/Ground_Main");

        if (ground != null)
        {
            Renderer groundRenderer = ground.GetComponent<Renderer>();

            if (groundRenderer != null)
            {
                GameVisualStyle.ApplyColor(groundRenderer, TurfHighlight, 0.22f, false, 0f);
            }
        }

        float centerRadius = Mathf.Min(arena.HalfSizeX, arena.HalfSizeZ) * 0.34f;

        CreateVisualPrimitive(
            arenaDressingRoot,
            "TurfHighlight",
            PrimitiveType.Cylinder,
            new Vector3(0f, 0.03f, 0f),
            new Vector3(centerRadius * 2f, 0.04f, centerRadius * 2f),
            GrassCenter,
            0.14f,
            grassMaterial,
            false);
    }

    private void BuildGrassVariation(ProceduralGrassArena arena)
    {
        System.Random random = new System.Random(arena.CurrentRunSeed ^ 0x7F4A7C15);
        float halfX = arena.HalfSizeX - 10f;
        float halfZ = arena.HalfSizeZ - 10f;
        const int patchCount = 28;
        int placed = 0;
        int safety = 0;

        while (placed < patchCount && safety < patchCount * 6)
        {
            safety++;

            float x = RandomRange(random, -halfX, halfX);
            float z = RandomRange(random, -halfZ, halfZ);

            if (x * x + z * z < 400f)
            {
                continue;
            }

            float patchWidth = RandomRange(random, 1.8f, 4.2f);
            float patchDepth = RandomRange(random, 1.6f, 3.8f);
            float patchHeight = RandomRange(random, 0.03f, 0.06f);
            float yaw = RandomRange(random, 0f, 180f);

            Color patchColor = (placed % 3) switch
            {
                0 => GrassPatchDark,
                1 => GrassPatchLight,
                _ => GrassPatchMuted
            };

            GameObject patchObject = CreateVisualPrimitive(
                arenaDressingRoot,
                "GrassPatch_" + placed,
                PrimitiveType.Cube,
                new Vector3(x, 0.045f, z),
                new Vector3(patchWidth, patchHeight, patchDepth),
                patchColor,
                0.1f,
                grassMaterial,
                false);

            patchObject.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            placed++;
        }
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        return (float)(min + random.NextDouble() * (max - min));
    }

    private void BuildGrassRim(float halfX, float halfZ)
    {
        float inset = 2.4f;
        float rimHeight = 0.18f;
        float rimWidth = 2.8f;
        float y = 0.08f;

        CreateVisualPrimitive(
            arenaDressingRoot,
            "GrassRim_North",
            PrimitiveType.Cube,
            new Vector3(0f, y, halfZ - inset),
            new Vector3(halfX * 2f - inset * 2f, rimHeight, rimWidth),
            GrassRim,
            0.12f,
            grassMaterial,
            false);

        CreateVisualPrimitive(
            arenaDressingRoot,
            "GrassRim_South",
            PrimitiveType.Cube,
            new Vector3(0f, y, -halfZ + inset),
            new Vector3(halfX * 2f - inset * 2f, rimHeight, rimWidth),
            GrassRim,
            0.12f,
            grassMaterial,
            false);

        CreateVisualPrimitive(
            arenaDressingRoot,
            "GrassRim_East",
            PrimitiveType.Cube,
            new Vector3(halfX - inset, y, 0f),
            new Vector3(rimWidth, rimHeight, halfZ * 2f - inset * 2f),
            GrassRim,
            0.12f,
            grassMaterial,
            false);

        CreateVisualPrimitive(
            arenaDressingRoot,
            "GrassRim_West",
            PrimitiveType.Cube,
            new Vector3(-halfX + inset, y, 0f),
            new Vector3(rimWidth, rimHeight, halfZ * 2f - inset * 2f),
            GrassRim,
            0.12f,
            grassMaterial,
            false);
    }

    private void BuildCliffUndersides(float halfX, float halfZ)
    {
        float dropDepth = 28f;
        float outerOffset = 3.5f;
        float y = -dropDepth * 0.5f + 0.5f;

        CreateVisualPrimitive(
            arenaDressingRoot,
            "Cliff_North",
            PrimitiveType.Cube,
            new Vector3(0f, y, halfZ + outerOffset),
            new Vector3(halfX * 2f + outerOffset * 2f, dropDepth, 6f),
            CliffRock,
            0.16f,
            cliffMaterial,
            false);

        CreateVisualPrimitive(
            arenaDressingRoot,
            "Cliff_South",
            PrimitiveType.Cube,
            new Vector3(0f, y, -halfZ - outerOffset),
            new Vector3(halfX * 2f + outerOffset * 2f, dropDepth, 6f),
            CliffDeep,
            0.14f,
            cliffMaterial,
            false);

        CreateVisualPrimitive(
            arenaDressingRoot,
            "Cliff_East",
            PrimitiveType.Cube,
            new Vector3(halfX + outerOffset, y, 0f),
            new Vector3(6f, dropDepth, halfZ * 2f + outerOffset * 2f),
            CliffRock,
            0.16f,
            cliffMaterial,
            false);

        CreateVisualPrimitive(
            arenaDressingRoot,
            "Cliff_West",
            PrimitiveType.Cube,
            new Vector3(-halfX - outerOffset, y, 0f),
            new Vector3(6f, dropDepth, halfZ * 2f + outerOffset * 2f),
            CliffDeep,
            0.14f,
            cliffMaterial,
            false);
    }

    private void BuildCornerOutcrops(float halfX, float halfZ)
    {
        Vector3[] corners =
        {
            new Vector3(halfX - BorderThickness, 0.9f, halfZ - BorderThickness),
            new Vector3(-halfX + BorderThickness, 0.9f, halfZ - BorderThickness),
            new Vector3(halfX - BorderThickness, 0.9f, -halfZ + BorderThickness),
            new Vector3(-halfX + BorderThickness, 0.9f, -halfZ + BorderThickness)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            CreateVisualPrimitive(
                arenaDressingRoot,
                "CornerOutcrop_" + i,
                PrimitiveType.Cylinder,
                corners[i],
                new Vector3(2.4f, 1.6f, 2.4f),
                CliffRock,
                0.18f,
                cliffMaterial,
                false);
        }
    }

    private static Transform CreateRoot(string name, Transform parent)
    {
        GameObject rootObject = new GameObject(name);
        rootObject.transform.SetParent(parent, false);
        return rootObject.transform;
    }

    private static Transform FindArenaTransform(ProceduralGrassArena arena, string path)
    {
        if (arena == null) return null;

        return arena.transform.Find(path);
    }

    private static GameObject CreateVisualPrimitive(
        Transform parent,
        string name,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 scale,
        Color color,
        float smoothness,
        Material sharedMaterial,
        bool castShadow = false)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = name;
        partObject.transform.SetParent(parent, false);
        partObject.transform.position = position;
        partObject.transform.localScale = scale;
        partObject.isStatic = true;

        RemoveCollider(partObject);

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            if (sharedMaterial != null)
            {
                renderer.sharedMaterial = sharedMaterial;
            }

            renderer.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadow;
            ApplyMaterialColor(renderer, color, smoothness);
        }

        return partObject;
    }

    private static void RemoveCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private static void ApplyMaterialColor(Renderer renderer, Color color, float smoothness)
    {
        if (renderer == null) return;

        GameVisualStyle.ApplyColor(renderer, color, smoothness, false, 0f);
    }

    private static Material CreateCloudMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Transparent")
            ?? Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        ConfigureTransparent(material, CloudColor);
        return material;
    }

    private static Material CreateOpaqueMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("Universal Render Pipeline/Unlit");

        return new Material(shader);
    }

    private static void ConfigureTransparent(Material material, Color color)
    {
        if (material == null) return;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        material.renderQueue = (int)RenderQueue.Transparent;

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
    }

    private static Light FindSunLight()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].type == LightType.Directional)
            {
                return lights[i];
            }
        }

        return null;
    }
}

public class SkylandsCloudDrift : MonoBehaviour
{
    private Vector3 basePosition;
    private float phase;

    private void Awake()
    {
        basePosition = transform.localPosition;
        phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float time = Time.time * 0.08f + phase;
        transform.localPosition = basePosition + new Vector3(Mathf.Sin(time) * 2.5f, 0f, Mathf.Cos(time * 0.85f) * 2f);
    }
}
