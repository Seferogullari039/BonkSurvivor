using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual-only Polytope environment dressing for Green Ancient Wilds.
/// Colliders are stripped; gameplay collision stays on procedural terrain/safety floor.
/// </summary>
public static class ImportedEnvironmentVisualLayer
{
    private const string PolytopePrefabsRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
    private const string GaMaterialsFolder = "Assets/BonkSurvivor/Maps/GreenAncientWilds/Materials";
    private const float MinDistanceFromSpawnCenter = 25f;
    private const float SmallDecorMinCenterDistance = 35f;
    private const float LargeTreeMinCenterDistance = 50f;
    private const float LandmarkTreeMinCenterDistance = 62f;

    private static readonly string[] TreePrefabPaths =
    {
        PolytopePrefabsRoot + "/Trees/PT_Pine_Tree_03_green.prefab",
        PolytopePrefabsRoot + "/Trees/PT_Fruit_Tree_01_green.prefab",
        PolytopePrefabsRoot + "/Trees/PT_Pine_Tree_03_dead.prefab",
    };

    private static readonly string[] RockPrefabPaths =
    {
        PolytopePrefabsRoot + "/Rocks/PT_Generic_Rock_01.prefab",
        PolytopePrefabsRoot + "/Rocks/PT_Ore_Rock_01.prefab",
        PolytopePrefabsRoot + "/Rocks/PT_River_Rock_Pile_02.prefab",
    };

    private struct GaMaterialSet
    {
        public Material Grass;
        public Material Tree;
        public Material Rock;
        public Material Ruin;
    }

    public static Transform Build(ProceduralGrassArena arena, Transform parent, System.Random random)
    {
        if (arena == null || parent == null)
        {
            return null;
        }

        GaMaterialSet materials = LoadGaMaterials();
        if (!HasAnyPrefabAvailable())
        {
            return null;
        }

        Transform root = new GameObject("ImportedEnvironmentVisuals").transform;
        root.SetParent(parent, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;

        int placed = 0;
        placed += PlaceNorthernRidgeSilhouette(arena, root, random, materials);
        placed += PlaceEasternForestHillField(arena, root, random, materials);
        placed += PlaceWesternValleyPath(arena, root, random, materials);
        placed += PlaceSouthernOpenRoute(arena, root, random, materials);
        placed += PlaceNorthLandmark(arena, root, materials);

        if (placed == 0)
        {
            Object.Destroy(root.gameObject);
            return null;
        }

        return root;
    }

    private static bool HasAnyPrefabAvailable()
    {
        return LoadEnvironmentPrefab(TreePrefabPaths[0]) != null
            || LoadEnvironmentPrefab(RockPrefabPaths[0]) != null;
    }

    private static int PlaceNorthernRidgeSilhouette(
        ProceduralGrassArena arena,
        Transform parent,
        System.Random random,
        GaMaterialSet materials)
    {
        Transform zone = CreateZone(parent, "NorthernRidgeSilhouette");
        int placed = 0;
        float halfZ = arena.HalfSizeZ;

        for (int i = 0; i < 12; i++)
        {
            float t = (i + 0.5f) / 12f;
            float x = Mathf.Lerp(-arena.HalfSizeX * 0.72f, arena.HalfSizeX * 0.72f, t);
            float z = Mathf.Lerp(halfZ * 0.62f, halfZ * 0.88f, 0.35f + (i % 3) * 0.22f);
            Vector3 flat = new Vector3(x, 0f, z);

            if (!IsValidPlacement(arena, flat, path))
            {
                continue;
            }

            string path = i % 4 == 0
                ? RockPrefabPaths[2]
                : i % 3 == 0
                    ? TreePrefabPaths[2]
                    : TreePrefabPaths[0];
            Material material = path.Contains("Rock") ? materials.Rock : materials.Tree;
            float yaw = PlacementYaw(random, i, 17);
            float scale = PlacementScale(random, i, 19, 0.92f, 1.18f);

            if (TryPlacePrefab(arena, zone, path, flat, yaw, scale, material))
            {
                placed++;
            }
        }

        return placed;
    }

    private static int PlaceEasternForestHillField(
        ProceduralGrassArena arena,
        Transform parent,
        System.Random random,
        GaMaterialSet materials)
    {
        Transform zone = CreateZone(parent, "EasternForestHillField");
        int placed = 0;
        Vector3[] clusterCenters =
        {
            new Vector3(arena.HalfSizeX * 0.72f, 0f, arena.HalfSizeZ * 0.42f),
            new Vector3(arena.HalfSizeX * 0.78f, 0f, arena.HalfSizeZ * 0.18f),
            new Vector3(arena.HalfSizeX * 0.64f, 0f, arena.HalfSizeZ * 0.58f),
        };

        for (int cluster = 0; cluster < clusterCenters.Length; cluster++)
        {
            Vector3 center = clusterCenters[cluster];
            int treesInCluster = 4 + cluster;

            for (int i = 0; i < treesInCluster; i++)
            {
                float angle = RandomRange(random, 0f, Mathf.PI * 2f);
                float distance = RandomRange(random, 2.5f, 9f);
                Vector3 flat = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

                if (!IsValidPlacement(arena, flat, path))
                {
                    continue;
                }

                string path = TreePrefabPaths[i % TreePrefabPaths.Length];
                float yaw = PlacementYaw(random, cluster * 10 + i, 23);
                float scale = PlacementScale(random, cluster * 10 + i, 29, 0.88f, 1.08f);

                if (TryPlacePrefab(arena, zone, path, flat, yaw, scale, materials.Tree))
                {
                    placed++;
                }
            }

            Vector3 rockFlat = center + new Vector3(-5f, 0f, -4f);
            if (IsValidPlacement(arena, rockFlat, RockPrefabPaths[1]))
            {
                if (TryPlacePrefab(
                        arena,
                        zone,
                        RockPrefabPaths[1],
                        rockFlat,
                        PlacementYaw(random, cluster, 31),
                        PlacementScale(random, cluster, 37, 0.9f, 1.15f),
                        materials.Rock))
                {
                    placed++;
                }
            }
        }

        return placed;
    }

    private static int PlaceWesternValleyPath(
        ProceduralGrassArena arena,
        Transform parent,
        System.Random random,
        GaMaterialSet materials)
    {
        Transform zone = CreateZone(parent, "WesternValleyPath");
        int placed = 0;
        string grassPath = PolytopePrefabsRoot + "/Plants/PT_Grass_02.prefab";
        string shrubPath = PolytopePrefabsRoot + "/Shrubs/PT_Generic_Shrub_01_green.prefab";
        string menhirPath = PolytopePrefabsRoot + "/Rocks/PT_Menhir_Rock_02.prefab";

        for (int i = 0; i < 10; i++)
        {
            float t = i / 9f;
            float x = Mathf.Lerp(-arena.HalfSizeX * 0.42f, -arena.HalfSizeX * 0.72f, t);
            float z = Mathf.Lerp(-arena.HalfSizeZ * 0.2f, arena.HalfSizeZ * 0.28f, t);
            Vector3 flat = new Vector3(x, 0f, z);

            if (!IsValidPlacement(arena, flat, path))
            {
                continue;
            }

            string path;
            Material material;
            float scale;

            switch (i % 4)
            {
                case 0:
                    path = menhirPath;
                    material = materials.Ruin;
                    scale = PlacementScale(random, i, 41, 0.95f, 1.1f);
                    break;
                case 1:
                    path = RockPrefabPaths[0];
                    material = materials.Rock;
                    scale = PlacementScale(random, i, 43, 0.82f, 1.05f);
                    break;
                case 2:
                    path = shrubPath;
                    material = materials.Grass;
                    scale = PlacementScale(random, i, 47, 0.9f, 1.12f);
                    break;
                default:
                    path = grassPath;
                    material = materials.Grass;
                    scale = PlacementScale(random, i, 53, 1f, 1.2f);
                    break;
            }

            if (TryPlacePrefab(arena, zone, path, flat, PlacementYaw(random, i, 59), scale, material))
            {
                placed++;
            }
        }

        return placed;
    }

    private static int PlaceSouthernOpenRoute(
        ProceduralGrassArena arena,
        Transform parent,
        System.Random random,
        GaMaterialSet materials)
    {
        Transform zone = CreateZone(parent, "SouthernOpenRoute");
        int placed = 0;
        string grassPath = PolytopePrefabsRoot + "/Plants/PT_Grass_02.prefab";

        for (int i = 0; i < 8; i++)
        {
            float angle = -Mathf.PI * 0.55f + i * (Mathf.PI * 0.14f);
            float radius = RandomRange(random, arena.HalfSizeZ * 0.45f, arena.HalfSizeZ * 0.58f);
            Vector3 flat = new Vector3(Mathf.Cos(angle) * radius * 0.75f, 0f, -Mathf.Abs(Mathf.Sin(angle)) * radius);
            string path = i % 4 == 0 ? RockPrefabPaths[0] : grassPath;
            Material material = path.Contains("Rock") ? materials.Rock : materials.Grass;

            if (!IsValidPlacement(arena, flat, path))
            {
                continue;
            }

            if (TryPlacePrefab(
                    arena,
                    zone,
                    path,
                    flat,
                    PlacementYaw(random, i, 61),
                    PlacementScale(random, i, 67, 0.85f, 1.1f),
                    material))
            {
                placed++;
            }
        }

        return placed;
    }

    private static int PlaceNorthLandmark(ProceduralGrassArena arena, Transform parent, GaMaterialSet materials)
    {
        Transform zone = CreateZone(parent, "NorthLandmark");
        string menhirPath = PolytopePrefabsRoot + "/Rocks/PT_Menhir_Rock_02.prefab";
        string pinePath = TreePrefabPaths[0];
        Vector3 center = new Vector3(0f, 0f, arena.HalfSizeZ * 0.72f);

        if (!IsValidPlacement(arena, center, pinePath))
        {
            return 0;
        }

        int placed = 0;
        float[] angles = { 0f, 72f, 144f, 216f, 288f };

        for (int i = 0; i < angles.Length; i++)
        {
            float radians = angles[i] * Mathf.Deg2Rad;
            Vector3 flat = center + new Vector3(Mathf.Cos(radians) * 7f, 0f, Mathf.Sin(radians) * 7f);
            if (!IsValidPlacement(arena, flat, menhirPath))
            {
                continue;
            }

            if (TryPlacePrefab(arena, zone, menhirPath, flat, angles[i], 1f, materials.Ruin))
            {
                placed++;
            }
        }

        Vector3[] treeOffsets =
        {
            new Vector3(-4f, 0f, 5f),
            new Vector3(4f, 0f, 5f),
            new Vector3(0f, 0f, -5f),
        };

        for (int i = 0; i < treeOffsets.Length; i++)
        {
            Vector3 flat = center + treeOffsets[i];
            if (!IsValidPlacement(arena, flat, pinePath))
            {
                continue;
            }

            if (TryPlacePrefab(arena, zone, pinePath, flat, i * 40f, 1.0f, materials.Tree))
            {
                placed++;
            }
        }

        return placed;
    }

    private static Transform CreateZone(Transform parent, string zoneName)
    {
        GameObject zoneObject = new GameObject(zoneName);
        zoneObject.transform.SetParent(parent, false);
        return zoneObject.transform;
    }

    private static bool IsValidPlacement(ProceduralGrassArena arena, Vector3 flatPosition, string prefabPath)
    {
        float minCenterDistance = GetMinCenterDistanceForPrefab(prefabPath);
        float minCenterDistanceSq = minCenterDistance * minCenterDistance;

        if (flatPosition.sqrMagnitude < minCenterDistanceSq)
        {
            return false;
        }

        Vector3 spawn = arena.SelectedPlayerSpawn;
        Vector3 flatSpawn = new Vector3(spawn.x, 0f, spawn.z);
        Vector3 flatCandidate = new Vector3(flatPosition.x, 0f, flatPosition.z);

        if ((flatCandidate - flatSpawn).sqrMagnitude < MinDistanceFromSpawnCenter * MinDistanceFromSpawnCenter)
        {
            return false;
        }

        if (IsLargeTreePrefab(prefabPath)
            && (flatCandidate - flatSpawn).sqrMagnitude < LargeTreeMinCenterDistance * LargeTreeMinCenterDistance)
        {
            return false;
        }

        float margin = 8f;
        if (Mathf.Abs(flatPosition.x) > arena.HalfSizeX - margin
            || Mathf.Abs(flatPosition.z) > arena.HalfSizeZ - margin)
        {
            return false;
        }

        return true;
    }

    private static float GetMinCenterDistanceForPrefab(string prefabPath)
    {
        if (IsLargeTreePrefab(prefabPath))
        {
            return LargeTreeMinCenterDistance;
        }

        if (prefabPath != null && prefabPath.Contains("Menhir"))
        {
            return LandmarkTreeMinCenterDistance;
        }

        return SmallDecorMinCenterDistance;
    }

    private static bool IsLargeTreePrefab(string prefabPath)
    {
        return prefabPath != null
            && prefabPath.Contains("/Trees/")
            && (prefabPath.Contains("PT_Pine_Tree") || prefabPath.Contains("PT_Fruit_Tree"));
    }

    private static bool TryPlacePrefab(
        ProceduralGrassArena arena,
        Transform parent,
        string prefabPath,
        Vector3 flatPosition,
        float yawDegrees,
        float uniformScale,
        Material overrideMaterial)
    {
        GameObject prefab = LoadEnvironmentPrefab(prefabPath);
        if (prefab == null)
        {
            return false;
        }

        if (!IsValidPlacement(arena, flatPosition, prefabPath))
        {
            return false;
        }

        float surfaceY = arena.GetSurfaceHeightAt(flatPosition.x, flatPosition.z);
        Vector3 position = new Vector3(flatPosition.x, surfaceY, flatPosition.z);
        GameObject instance = Object.Instantiate(prefab, position, Quaternion.Euler(0f, yawDegrees, 0f), parent);
        instance.name = prefab.name;
        instance.transform.localScale = Vector3.one * uniformScale;
        instance.isStatic = true;

        ApplyMaterialFallback(instance, overrideMaterial);
        DisablePhysicsComponents(instance);
        return true;
    }

    private static void ApplyMaterialFallback(GameObject instance, Material overrideMaterial)
    {
        if (instance == null || overrideMaterial == null)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] slots = renderer.sharedMaterials;
            for (int slot = 0; slot < slots.Length; slot++)
            {
                if (IsSuspiciousMaterial(slots[slot]))
                {
                    slots[slot] = overrideMaterial;
                }
            }

            renderer.sharedMaterials = slots;
        }
    }

    private static bool IsSuspiciousMaterial(Material material)
    {
        if (material == null)
        {
            return true;
        }

        string name = material.name;
        return name.StartsWith("PT_")
            || name.Contains("Polytope")
            || name.Contains("GrabPass")
            || name.Contains("Hidden");
    }

    private static void DisablePhysicsComponents(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] != null)
            {
                Object.Destroy(rigidbodies[i]);
            }
        }
    }

    private static GaMaterialSet LoadGaMaterials()
    {
        return new GaMaterialSet
        {
            Grass = LoadGaMaterial("Grass", new Color(0.4f, 0.72f, 0.3f, 1f)),
            Tree = LoadGaMaterial("Tree", new Color(0.2f, 0.46f, 0.22f, 1f)),
            Rock = LoadGaMaterial("Rock", new Color(0.45f, 0.43f, 0.4f, 1f)),
            Ruin = LoadGaMaterial("Ruin", new Color(0.52f, 0.5f, 0.46f, 1f)),
        };
    }

    private static Material LoadGaMaterial(string materialName, Color fallbackColor)
    {
#if UNITY_EDITOR
        string path = $"{GaMaterialsFolder}/GA_{materialName}_Mat.mat";
        Material loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
        if (loaded != null)
        {
            return loaded;
        }
#endif

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material runtimeMaterial = new Material(shader);

        if (runtimeMaterial.HasProperty("_BaseColor"))
        {
            runtimeMaterial.SetColor("_BaseColor", fallbackColor);
        }

        if (runtimeMaterial.HasProperty("_Color"))
        {
            runtimeMaterial.SetColor("_Color", fallbackColor);
        }

        if (runtimeMaterial.HasProperty("_Smoothness"))
        {
            runtimeMaterial.SetFloat("_Smoothness", 0.16f);
        }

        return runtimeMaterial;
    }

    private static GameObject LoadEnvironmentPrefab(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }

    private static float PlacementYaw(System.Random random, int salt, int multiplier)
    {
        return RandomRange(random, 0f, 360f) + salt * multiplier * 0.17f;
    }

    private static float PlacementScale(System.Random random, int salt, int multiplier, float min, float max)
    {
        float t = Mathf.Abs(Mathf.Sin(salt * multiplier * 0.113f + (float)random.NextDouble() * 0.35f));
        return min + t * (max - min);
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        return (float)(min + random.NextDouble() * (max - min));
    }
}
