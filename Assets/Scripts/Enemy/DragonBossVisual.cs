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

    private Transform mouthFirePoint;

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

        if (TryBuildPrefabVisual())
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
        GameObject viewInstance = Instantiate(viewPrefab, visualRoot, false);

        if (viewInstance == null)
        {
            ClearExistingVisualRoot();
            return false;
        }

        viewInstance.name = viewPrefab.name + "_View";
        SanitizeVisualInstance(viewInstance);
        mouthFirePoint = ResolveMouthFirePoint(viewInstance.transform, visualRoot);
        return true;
    }

    private void BuildProceduralVisual()
    {
        Transform visualRoot = CreateVisualRoot();
        Color bodyColor = GameVisualPalette.DragonBoss;
        Color wingColor = new Color(0.22f, 0.05f, 0.1f);
        Color hornColor = new Color(0.82f, 0.28f, 0.42f);
        Color eyeColor = new Color(1f, 0.88f, 0.18f);
        Color chestGlowColor = new Color(1f, 0.42f, 0.18f);

        CreatePart(visualRoot, "Body", PrimitiveType.Capsule, new Vector3(0f, 1.2f, 0f), new Vector3(2.4f, 1.6f, 2.2f), bodyColor, 0.44f, true, 0.18f);
        CreatePart(visualRoot, "Neck", PrimitiveType.Capsule, new Vector3(0f, 2.1f, 0.55f), new Vector3(0.9f, 0.55f, 0.9f), bodyColor, 0.42f, false);
        CreatePart(visualRoot, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.55f, 1.15f), new Vector3(1.35f, 1.15f, 1.35f), bodyColor, 0.46f, true, 0.12f);
        CreatePart(visualRoot, "ChestGlow", PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0.72f), new Vector3(0.72f, 0.72f, 0.72f), chestGlowColor, 0.72f, true, 0.72f);
        CreatePart(visualRoot, "Wing_L", PrimitiveType.Cube, new Vector3(-2.1f, 1.8f, 0.1f), new Vector3(2.8f, 0.12f, 1.8f), wingColor, 0.35f, false);
        CreatePart(visualRoot, "Wing_R", PrimitiveType.Cube, new Vector3(2.1f, 1.8f, 0.1f), new Vector3(2.8f, 0.12f, 1.8f), wingColor, 0.35f, false);
        CreatePart(visualRoot, "Horn_L", PrimitiveType.Cylinder, new Vector3(-0.35f, 3.05f, 1.35f), new Vector3(0.18f, 0.35f, 0.18f), hornColor, 0.58f, true, 0.55f);
        CreatePart(visualRoot, "Horn_R", PrimitiveType.Cylinder, new Vector3(0.35f, 3.05f, 1.35f), new Vector3(0.18f, 0.35f, 0.18f), hornColor, 0.58f, true, 0.55f);
        CreatePart(visualRoot, "Eye_L", PrimitiveType.Sphere, new Vector3(-0.28f, 2.75f, 1.72f), new Vector3(0.2f, 0.2f, 0.2f), eyeColor, 0.24f, true, 0.82f);
        CreatePart(visualRoot, "Eye_R", PrimitiveType.Sphere, new Vector3(0.28f, 2.75f, 1.72f), new Vector3(0.2f, 0.2f, 0.2f), eyeColor, 0.24f, true, 0.82f);

        CreatePart(visualRoot, "Tail_1", PrimitiveType.Capsule, new Vector3(0f, 1.05f, -1.1f), new Vector3(0.55f, 0.35f, 0.55f), bodyColor, 0.38f, false);
        CreatePart(visualRoot, "Tail_2", PrimitiveType.Capsule, new Vector3(0f, 0.95f, -1.75f), new Vector3(0.45f, 0.28f, 0.45f), bodyColor, 0.38f, false);
        CreatePart(visualRoot, "Tail_3", PrimitiveType.Sphere, new Vector3(0f, 0.85f, -2.25f), new Vector3(0.38f, 0.38f, 0.38f), hornColor, 0.5f, true, 0.25f);

        mouthFirePoint = CreateFallbackMouthFirePoint(visualRoot);
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
