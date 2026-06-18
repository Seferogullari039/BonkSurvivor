using UnityEngine;

/// <summary>
/// Playtest-only helper: hides legacy procedural/Skylands visuals while keeping gameplay systems active.
/// </summary>
[DefaultExecutionOrder(250)]
public class GreenAncientWildsPlaytestVisualSuppressor : MonoBehaviour
{
    private const string LegacyPlaneName = "Plane";
    private const string SkylandsKitName = "SkylandsVisualKit";
    private const string ProceduralArenaRootName = "ProceduralArena";
    private const string GaVisualsRootName = "GreenAncientWilds_Visuals";
    private const string LegacyWaterDecorName = "GA_WaterDecor";

    private static readonly string[] SkylandsLegacyRootNames =
    {
        SkylandsKitName,
        "Atmosphere",
        "CloudLayer",
        "DistantIslands",
        "ArenaDressing",
    };

    private void OnEnable()
    {
        SuppressLegacyVisuals();
    }

    private void LateUpdate()
    {
        SuppressLegacyVisuals();
    }

    private static void SuppressLegacyVisuals()
    {
        SuppressLegacyPlaneRenderer();
        SuppressLegacyWaterDecorRenderer();
        SuppressSkylandsVisualKit();
        SuppressProceduralArenaRenderers();
    }

    private static void SuppressLegacyPlaneRenderer()
    {
        GameObject plane = GameObject.Find(LegacyPlaneName);
        if (plane == null || IsUnderGaVisuals(plane.transform))
        {
            return;
        }

        DisableRenderersRecursive(plane);
    }

    private static void SuppressLegacyWaterDecorRenderer()
    {
        Transform visualsRoot = FindGaVisualsRoot();
        if (visualsRoot == null)
        {
            return;
        }

        Transform waterDecor = visualsRoot.Find(LegacyWaterDecorName);
        if (waterDecor == null)
        {
            return;
        }

        Vector3 scale = waterDecor.localScale;
        Vector3 position = waterDecor.localPosition;
        bool legacyOverheadLayout = scale.x >= 4f
            || (Mathf.Abs(position.x) < 10f && position.z > 75f && scale.x >= 3f);
        if (!legacyOverheadLayout)
        {
            return;
        }

        Renderer[] renderers = waterDecor.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private static Transform FindGaVisualsRoot()
    {
        GameObject visualsRoot = GameObject.Find(GaVisualsRootName);
        return visualsRoot != null ? visualsRoot.transform : null;
    }

    private static void SuppressSkylandsVisualKit()
    {
        SkylandsVisualKit[] kits = FindObjectsByType<SkylandsVisualKit>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < kits.Length; i++)
        {
            SkylandsVisualKit kit = kits[i];
            if (kit == null)
            {
                continue;
            }

            DisableSkylandsHierarchy(kit.gameObject);
        }

        for (int i = 0; i < SkylandsLegacyRootNames.Length; i++)
        {
            GameObject legacyRoot = GameObject.Find(SkylandsLegacyRootNames[i]);
            if (legacyRoot == null || IsUnderGaVisuals(legacyRoot.transform))
            {
                continue;
            }

            DisableSkylandsHierarchy(legacyRoot);
        }
    }

    private static void DisableSkylandsHierarchy(GameObject root)
    {
        if (root == null || IsUnderGaVisuals(root.transform))
        {
            return;
        }

        DisableRenderersRecursive(root);

        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform child = rootTransform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            DisableRenderersRecursive(child.gameObject);

            string childName = child.name;
            if (childName.StartsWith("Cloud", System.StringComparison.Ordinal)
                || childName.StartsWith("DistantIsland", System.StringComparison.Ordinal)
                || childName == "CloudLayer"
                || childName == "DistantIslands"
                || childName == "Atmosphere"
                || childName == "ArenaDressing")
            {
                DisableRenderersRecursive(child.gameObject);
            }
        }

        root.SetActive(false);
    }

    private static void SuppressProceduralArenaRenderers()
    {
        ProceduralGrassArena arena = FindFirstObjectByType<ProceduralGrassArena>();
        if (arena == null)
        {
            return;
        }

        Transform arenaRoot = arena.transform.Find(ProceduralArenaRootName);
        if (arenaRoot == null)
        {
            return;
        }

        DisableRenderersRecursive(arenaRoot.gameObject);
    }

    private static void DisableRenderersRecursive(GameObject root)
    {
        if (root == null || IsUnderGaVisuals(root.transform))
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private static bool IsUnderGaVisuals(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == GaVisualsRootName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
