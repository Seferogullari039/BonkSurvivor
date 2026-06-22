using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Manual Play Mode helper for identifying large visible bug objects (dark cubes, giant spheres).
/// Editor and Development builds only. Never logs every frame.
/// </summary>
public static class VisualBugInspector
{
    private const float DefaultMinBoundsSize = 1.75f;
    private const float DefaultNearRadius = 14f;
    private const int MaxReportCount = 24;

    private static bool loggedOnceThisSession;

    // Default off: automatic post-event scans (e.g. shrine completion) are opt-in.
    // F8 / Admin Panel manual scans always run regardless of this flag.
    public static bool AutoScanEnabled = false;

    public static bool ShouldRun()
    {
#if UNITY_EDITOR
        return Application.isPlaying;
#else
        return Debug.isDebugBuild && Application.isPlaying;
#endif
    }

    public static void ReportLargeRenderersInScene(string context = "[VisualBugInspector]")
    {
        if (!ShouldRun())
        {
            return;
        }

        ReportLargeRenderersInternal(null, 0f, DefaultMinBoundsSize, context, false);
    }

    public static void ReportLargeRenderersNear(Vector3 center, float radius, string context = "[VisualBugInspector]")
    {
        if (!ShouldRun())
        {
            return;
        }

        float searchRadius = radius > 0.01f ? radius : DefaultNearRadius;
        ReportLargeRenderersInternal(center, searchRadius, DefaultMinBoundsSize, context, true);
    }

    public static void ReportLargeRenderersNearOnce(Vector3 center, float radius, string context)
    {
        if (!AutoScanEnabled || loggedOnceThisSession)
        {
            return;
        }

        loggedOnceThisSession = true;
        ReportLargeRenderersNear(center, radius, context);
    }

    private static void ReportLargeRenderersInternal(
        Vector3? center,
        float radius,
        float minBoundsSize,
        string context,
        bool useRadiusFilter)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        StringBuilder builder = new StringBuilder(4096);
        builder.Append(context);
        builder.Append(" Large renderer scan | minBounds=");
        builder.Append(minBoundsSize.ToString("0.##"));

        if (useRadiusFilter && center.HasValue)
        {
            builder.Append(" | near=");
            builder.Append(center.Value);
            builder.Append(" radius=");
            builder.Append(radius.ToString("0.##"));
        }

        builder.Append(" | matches=");

        int matchCount = 0;

        for (int i = 0; i < renderers.Length && matchCount < MaxReportCount; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = renderer.bounds;
            Vector3 boundsSize = bounds.size;
            float maxExtent = Mathf.Max(boundsSize.x, Mathf.Max(boundsSize.y, boundsSize.z));

            if (maxExtent < minBoundsSize)
            {
                continue;
            }

            if (useRadiusFilter && center.HasValue)
            {
                float distance = Vector3.Distance(center.Value, bounds.center);

                if (distance > radius + maxExtent * 0.5f)
                {
                    continue;
                }
            }

            matchCount++;
            AppendRendererReport(builder, renderer, bounds, boundsSize);
        }

        if (matchCount == 0)
        {
            builder.Append("none");
        }

        Debug.Log(builder.ToString());
    }

    private static void AppendRendererReport(
        StringBuilder builder,
        Renderer renderer,
        Bounds bounds,
        Vector3 boundsSize)
    {
        GameObject target = renderer.gameObject;
        Transform transform = target.transform;
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        Material sharedMaterial = renderer.sharedMaterial;

        builder.Append("\n- object=");
        builder.Append(target.name);
        builder.Append(" | parent=");
        builder.Append(GetTransformPath(transform));
        builder.Append(" | position=");
        builder.Append(transform.position);
        builder.Append(" | localScale=");
        builder.Append(transform.localScale);
        builder.Append(" | boundsSize=");
        builder.Append(boundsSize);
        builder.Append(" | mesh=");
        builder.Append(meshFilter != null && meshFilter.sharedMesh != null ? meshFilter.sharedMesh.name : "none");
        builder.Append(" | material=");
        builder.Append(sharedMaterial != null ? sharedMaterial.name : "none");
        builder.Append(" | prefab=");
        builder.Append(ResolvePrefabPath(target));
        builder.Append(" | tags=");
        builder.Append(BuildComponentTags(target));
        builder.Append(" | components=");
        builder.Append(BuildComponentSummary(target));
    }

    private static string GetTransformPath(Transform transform)
    {
        if (transform == null)
        {
            return "none";
        }

        StringBuilder path = new StringBuilder(transform.name);
        Transform current = transform.parent;

        while (current != null)
        {
            path.Insert(0, current.name + "/");
            current = current.parent;
        }

        return path.ToString();
    }

    private static string BuildComponentTags(GameObject target)
    {
        bool hasChest = target.GetComponent<Chest>() != null || target.GetComponentInParent<Chest>() != null;
        bool hasChestVisual = target.GetComponent<ChestVisual>() != null || target.GetComponentInParent<ChestVisual>() != null;
        bool hasRocket = target.GetComponent<RocketProjectile>() != null
            || target.name.IndexOf("Rocket", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasExplosion = target.name.IndexOf("Explosion", System.StringComparison.OrdinalIgnoreCase) >= 0
            || target.name.IndexOf("RocketExplosion", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasShrine = target.GetComponent<ShrineEventController>() != null
            || target.GetComponentInParent<ShrineEventController>() != null
            || target.name.IndexOf("Shrine", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool hasLevelUp = target.name.IndexOf("LevelUp", System.StringComparison.OrdinalIgnoreCase) >= 0
            || target.name.IndexOf("Juice", System.StringComparison.OrdinalIgnoreCase) >= 0;

        return "Chest=" + hasChest
            + " ChestVisual=" + hasChestVisual
            + " Rocket=" + hasRocket
            + " Explosion=" + hasExplosion
            + " Shrine=" + hasShrine
            + " LevelUp=" + hasLevelUp;
    }

    private static string BuildComponentSummary(GameObject target)
    {
        Component[] components = target.GetComponents<Component>();
        StringBuilder builder = new StringBuilder(128);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(component.GetType().Name);
        }

        return builder.Length > 0 ? builder.ToString() : "none";
    }

    private static string ResolvePrefabPath(GameObject target)
    {
#if UNITY_EDITOR
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(target);

        if (source != null)
        {
            return AssetDatabase.GetAssetPath(source);
        }
#endif
        return "runtime";
    }
}

public sealed class VisualBugInspectorHost : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!VisualBugInspector.ShouldRun())
        {
            return;
        }

        if (Object.FindFirstObjectByType<VisualBugInspectorHost>() != null)
        {
            return;
        }

        GameObject host = new GameObject("VisualBugInspectorHost");
        host.AddComponent<VisualBugInspectorHost>();
    }

    private void Awake()
    {
        if (!VisualBugInspector.ShouldRun())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!VisualBugInspector.ShouldRun())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            VisualBugInspector.ReportLargeRenderersInScene("[VisualBugInspector] F8 manual scan");
        }
    }
}
