using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class DragonBossVisual : MonoBehaviour
{
    private const string VisualRootName = "DragonVisualRoot";
    private const string ViewPrefabPath = "Assets/Prefabs/Bosses/DragonBoss_View.prefab";
    private const string ViewResourcePath = "Prefabs/Bosses/DragonBoss_View";

    [SerializeField] private bool buildOnAwake = true;

    // Visual-only orientation fix for the imported Dragon view prefab.
    // The boss root (DragonBossController) already yaws toward the player, so the model only needs
    // to stand upright with its face on +Z (root forward). These do NOT touch gameplay/collider/root.
    [SerializeField] private float modelFacingYaw = 0f;
    // Kept for grounding/upright at the Model level only (default 0). The real visible flip is owned
    // by DragonActualVisibleRoot below so it works regardless of branch (prefab or procedural).
    [SerializeField] private float visualYawCorrection = 0f;
    // FINAL visual-only yaw applied to the active visible root that parents EVERY rendered child
    // (prefab instance OR procedural body/wings/head). This is the single source of truth for the
    // rendered dragon facing. It does NOT touch gameplay/root/collider/scale. Only test 180 <-> 0.
    [SerializeField] private float forcedVisibleYaw = 0f;
    [SerializeField] private float modelGroundLocalY = -0.456f;
    [SerializeField] private float mouthHeightFactor = 0.72f;
    [SerializeField] private bool debugLogOrientation = true;
    // Use the real imported FBX dragon view as the main visual. Procedural primitive dragon is only a
    // fallback when the prefab fails to load. Default true => real FBX dragon is the boss visual.
    [SerializeField] private bool usePrefabVisual = true;
    // Boss-size multiplier for the procedural dragon so it reads as a large boss without becoming a blob.
    [SerializeField] private float proceduralVisualScale = 1.6f;

    private Transform mouthFirePoint;
    private Transform actualVisibleRoot;

    public Transform MouthFirePoint => mouthFirePoint;

    private void Awake()
    {
        HideRootRenderer();

        if (buildOnAwake)
        {
            BuildVisual();
        }
    }

    public void BuildVisual()
    {
        ClearExistingVisualRoot();
        HideRootRenderer();

        if (usePrefabVisual && TryBuildPrefabVisual())
        {
            return;
        }

        BuildProceduralVisual();
    }

    private bool TryBuildPrefabVisual()
    {
        GameObject viewPrefab = ResolveViewPrefab();

        if (viewPrefab == null)
        {
            return false;
        }

        Transform visualRoot = CreateVisualRoot();
        Transform visibleRoot = CreateActualVisibleRoot(visualRoot);
        GameObject viewInstance = Instantiate(viewPrefab, visibleRoot, false);

        if (viewInstance == null)
        {
            ClearExistingVisualRoot();
            return false;
        }

        viewInstance.name = viewPrefab.name + "_View";
        viewInstance.transform.localPosition = Vector3.zero;
        viewInstance.transform.localRotation = Quaternion.identity;

        SanitizeVisualInstance(viewInstance);
        NormalizeViewOrientation(viewInstance);
        mouthFirePoint = ResolveMouthFirePoint(viewInstance.transform, visibleRoot);
        RepositionMouthFirePoint(viewInstance.transform, mouthFirePoint);

        ApplyForcedVisibleYaw();
        LogActiveVisualMode("Prefab", visibleRoot);
        return true;
    }

    // Single visible parent that wraps EVERY rendered child (prefab instance or procedural parts).
    // Rotating this root is the final authority over the rendered dragon facing; the boss root
    // (DragonBossController) still owns movement/facing and is untouched.
    private Transform CreateActualVisibleRoot(Transform visualRoot)
    {
        GameObject rootObject = new GameObject("DragonActualVisibleRoot");
        rootObject.transform.SetParent(visualRoot, false);
        rootObject.transform.localPosition = Vector3.zero;
        rootObject.transform.localRotation = Quaternion.Euler(0f, forcedVisibleYaw, 0f);
        rootObject.transform.localScale = Vector3.one;
        actualVisibleRoot = rootObject.transform;
        return rootObject.transform;
    }

    private void ApplyForcedVisibleYaw()
    {
        if (actualVisibleRoot != null)
        {
            actualVisibleRoot.localRotation = Quaternion.Euler(0f, forcedVisibleYaw, 0f);
        }
    }

    private void LogActiveVisualMode(string mode, Transform visibleRoot)
    {
        if (!debugLogOrientation || visibleRoot == null)
        {
            return;
        }

        int rendererCount = visibleRoot.GetComponentsInChildren<Renderer>(true).Length;
        Debug.Log("[DragonBossVisual] ActiveVisualMode=" + mode
            + " | VisibleRoot=" + visibleRoot.name
            + " | RendererCount=" + rendererCount
            + " | ForcedYaw=" + forcedVisibleYaw.ToString("F1"));
    }

    private void LateUpdate()
    {
        // Visual-only safety net: keep the rendered dragon facing locked even if another helper or
        // frame writes to it. Only the visible child root is touched, never the boss root/collider.
        if (actualVisibleRoot != null)
        {
            actualVisibleRoot.localRotation = Quaternion.Euler(0f, forcedVisibleYaw, 0f);
        }
    }

    private void NormalizeViewOrientation(GameObject viewInstance)
    {
        if (viewInstance == null)
        {
            return;
        }

        Transform innerRoot = viewInstance.transform.Find("VisualRoot");

        if (innerRoot == null)
        {
            innerRoot = viewInstance.transform;
        }

        Transform model = ResolveVisibleModelRoot(innerRoot);

        if (model == null)
        {
            return;
        }

        float effectiveYaw = modelFacingYaw + visualYawCorrection;
        Vector3 beforeEuler = model.localEulerAngles;

        // Stand the dragon upright (no pitch/roll) and face the boss root forward (+Z toward player).
        model.localRotation = Quaternion.Euler(0f, effectiveYaw, 0f);

        Bounds localBounds = CalculateLocalBounds(innerRoot, model);
        float groundShift = modelGroundLocalY - localBounds.min.y;
        model.localPosition += new Vector3(0f, groundShift, 0f);

        if (debugLogOrientation)
        {
            int rendererCount = model.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log("[DragonBossVisual] Orientation root=" + model.name
                + " yaw=" + effectiveYaw.ToString("F1")
                + " before=" + beforeEuler
                + " after=" + model.localEulerAngles
                + " renderers=" + rendererCount);
        }
    }

    // Returns the transform that actually parents the visible meshes so the yaw correction is applied
    // to the rendered model rather than an empty pivot. Prefers the "Model" child when it carries
    // renderers, otherwise falls back to the common ancestor of all renderers under the view root.
    private static Transform ResolveVisibleModelRoot(Transform innerRoot)
    {
        if (innerRoot == null)
        {
            return null;
        }

        Transform model = innerRoot.Find("Model");

        if (model == null)
        {
            model = FindDeepChild(innerRoot, "Model");
        }

        if (model != null && model.GetComponentInChildren<Renderer>(true) != null)
        {
            return model;
        }

        Renderer[] renderers = innerRoot.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return model;
        }

        // Find the highest direct child of innerRoot that contains all renderers, so we rotate the
        // whole visible model in one place without touching MouthFirePoint (a sibling pivot).
        Transform candidate = null;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            Transform topChild = GetTopChildUnder(innerRoot, renderer.transform);

            if (topChild == null)
            {
                continue;
            }

            if (candidate == null)
            {
                candidate = topChild;
            }
            else if (candidate != topChild)
            {
                // Renderers live under multiple direct children, rotate them together via innerRoot.
                return innerRoot;
            }
        }

        return candidate != null ? candidate : model;
    }

    private static Transform GetTopChildUnder(Transform ancestor, Transform descendant)
    {
        if (ancestor == null || descendant == null)
        {
            return null;
        }

        Transform current = descendant;

        while (current.parent != null && current.parent != ancestor)
        {
            current = current.parent;
        }

        return current.parent == ancestor ? current : null;
    }

    private void RepositionMouthFirePoint(Transform viewRoot, Transform mouth)
    {
        if (viewRoot == null || mouth == null)
        {
            return;
        }

        Transform innerRoot = viewRoot.Find("VisualRoot");

        if (innerRoot == null)
        {
            innerRoot = viewRoot;
        }

        Transform model = ResolveVisibleModelRoot(innerRoot);

        if (model == null || mouth.parent != innerRoot)
        {
            return;
        }

        Bounds localBounds = CalculateLocalBounds(innerRoot, model);
        float mouthY = localBounds.min.y + localBounds.size.y * mouthHeightFactor;
        float mouthZ = localBounds.max.z + localBounds.size.z * 0.04f;
        mouth.localPosition = new Vector3(localBounds.center.x, mouthY, mouthZ);
        mouth.localRotation = Quaternion.identity;
    }

    private static Bounds CalculateLocalBounds(Transform space, Transform modelRoot)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;

            for (int cornerX = 0; cornerX < 2; cornerX++)
            {
                for (int cornerY = 0; cornerY < 2; cornerY++)
                {
                    for (int cornerZ = 0; cornerZ < 2; cornerZ++)
                    {
                        Vector3 corner = new Vector3(
                            cornerX == 0 ? min.x : max.x,
                            cornerY == 0 ? min.y : max.y,
                            cornerZ == 0 ? min.z : max.z);

                        Vector3 localCorner = space.InverseTransformPoint(corner);

                        if (!hasBounds)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }

        if (!hasBounds)
        {
            localBounds = new Bounds(Vector3.zero, Vector3.one);
        }

        return localBounds;
    }

    private void BuildProceduralVisual()
    {
        Transform visualRoot = CreateVisualRoot();
        Transform visibleRoot = CreateActualVisibleRoot(visualRoot);
        visibleRoot.localScale = Vector3.one * Mathf.Max(0.1f, proceduralVisualScale);
        Color bodyColor = GameVisualPalette.DragonBoss;
        Color wingColor = new Color(0.22f, 0.05f, 0.1f);
        Color hornColor = new Color(0.82f, 0.28f, 0.42f);
        Color eyeColor = new Color(1f, 0.88f, 0.18f);
        Color chestGlowColor = new Color(1f, 0.42f, 0.18f);

        CreatePart(visibleRoot, "Body", PrimitiveType.Capsule, new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 1.6f, 2.2f), bodyColor, 0.44f, true, 0.18f);
        CreatePart(visibleRoot, "Neck", PrimitiveType.Capsule, new Vector3(0f, 2.1f, 0.55f), new Vector3(0.9f, 0.55f, 0.9f), bodyColor, 0.42f, false);
        CreatePart(visibleRoot, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.55f, 1.15f), new Vector3(1.35f, 1.15f, 1.35f), bodyColor, 0.46f, true, 0.12f);
        CreatePart(visibleRoot, "ChestGlow", PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0.72f), new Vector3(0.72f, 0.72f, 0.72f), chestGlowColor, 0.72f, true, 0.72f);
        CreatePart(visibleRoot, "Wing_L", PrimitiveType.Cube, new Vector3(-2.1f, 1.8f, 0.1f), new Vector3(2.8f, 0.12f, 1.8f), wingColor, 0.35f, false);
        CreatePart(visibleRoot, "Wing_R", PrimitiveType.Cube, new Vector3(2.1f, 1.8f, 0.1f), new Vector3(2.8f, 0.12f, 1.8f), wingColor, 0.35f, false);
        CreatePart(visibleRoot, "Horn_L", PrimitiveType.Cylinder, new Vector3(-0.35f, 3.05f, 1.35f), new Vector3(0.18f, 0.35f, 0.18f), hornColor, 0.58f, true, 0.55f);
        CreatePart(visibleRoot, "Horn_R", PrimitiveType.Cylinder, new Vector3(0.35f, 3.05f, 1.35f), new Vector3(0.18f, 0.35f, 0.18f), hornColor, 0.58f, true, 0.55f);
        CreatePart(visibleRoot, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.28f, 2.75f, 1.72f), new Vector3(0.2f, 0.2f, 0.2f), eyeColor, 0.24f, true, 0.82f);
        CreatePart(visibleRoot, "Eye_R", PrimitiveType.Sphere, new Vector3(0.28f, 2.75f, 1.72f), new Vector3(0.2f, 0.2f, 0.2f), eyeColor, 0.24f, true, 0.82f);

        CreatePart(visibleRoot, "Tail_1", PrimitiveType.Capsule, new Vector3(0f, 1.05f, -1.1f), new Vector3(0.55f, 0.35f, 0.55f), bodyColor, 0.38f, false);
        CreatePart(visibleRoot, "Tail_2", PrimitiveType.Capsule, new Vector3(0f, 0.95f, -1.75f), new Vector3(0.45f, 0.28f, 0.45f), bodyColor, 0.38f, false);
        CreatePart(visibleRoot, "Tail_3", PrimitiveType.Sphere, new Vector3(0f, 0.85f, -2.25f), new Vector3(0.38f, 0.38f, 0.38f), hornColor, 0.5f, true, 0.25f);

        mouthFirePoint = CreateFallbackMouthFirePoint(visibleRoot);

        GroundVisibleRoot(visualRoot, visibleRoot);
        ApplyForcedVisibleYaw();
        LogActiveVisualMode("Procedural", visibleRoot);
    }

    // Keeps the procedural dragon's feet near the boss root base so it neither sinks into the ground
    // nor floats. Visual only; does not touch the boss root/collider.
    private void GroundVisibleRoot(Transform visualRoot, Transform visibleRoot)
    {
        if (visualRoot == null || visibleRoot == null)
        {
            return;
        }

        Bounds localBounds = CalculateLocalBounds(visualRoot, visibleRoot);
        float groundShift = modelGroundLocalY - localBounds.min.y;
        visibleRoot.localPosition += new Vector3(0f, groundShift, 0f);
    }

    private static GameObject ResolveViewPrefab()
    {
#if UNITY_EDITOR
        GameObject editorPrefab = LoadEditorPrefab(ViewPrefabPath);

        if (editorPrefab != null)
        {
            return editorPrefab;
        }
#endif

        if (string.IsNullOrEmpty(ViewResourcePath))
        {
            return null;
        }

        return Resources.Load<GameObject>(ViewResourcePath);
    }

#if UNITY_EDITOR
    private static GameObject LoadEditorPrefab(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
    }
#endif

    private static Transform ResolveMouthFirePoint(Transform viewRoot, Transform visualRoot)
    {
        if (viewRoot != null)
        {
            Transform firePoint = viewRoot.Find("VisualRoot/MouthFirePoint");

            if (firePoint == null)
            {
                firePoint = FindDeepChild(viewRoot, "MouthFirePoint");
            }

            if (firePoint != null)
            {
                return firePoint;
            }
        }

        return CreateFallbackMouthFirePoint(visualRoot);
    }

    private static Transform CreateFallbackMouthFirePoint(Transform visualRoot)
    {
        GameObject firePointObject = new GameObject("MouthFirePoint");
        firePointObject.transform.SetParent(visualRoot, false);
        firePointObject.transform.localPosition = new Vector3(0f, 2.35f, 2.05f);
        return firePointObject.transform;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindDeepChild(parent.GetChild(i), childName);

            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void SanitizeVisualInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];

            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
            {
                continue;
            }

            animator.applyRootMotion = false;
        }
    }

    private void ClearExistingVisualRoot()
    {
        Transform existing = transform.Find(VisualRootName);

        if (existing != null)
        {
            if (Application.isPlaying)
            {
                Destroy(existing.gameObject);
            }
            else
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        mouthFirePoint = null;
        actualVisibleRoot = null;
    }

    private void HideRootRenderer()
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
            if (Application.isPlaying)
            {
                Destroy(partCollider);
            }
            else
            {
                DestroyImmediate(partCollider);
            }
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emission);
        }
    }
}
