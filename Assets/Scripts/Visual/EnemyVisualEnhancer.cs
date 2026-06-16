using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyVisualEnhancer : MonoBehaviour
{
    private const string VisualRootName = "EnemyVisualRoot";

    private static readonly Color EyeColor = new Color(0.95f, 0.92f, 0.35f);
    private static readonly Color FrontGlowColor = new Color(1f, 0.35f, 0.2f);
    private static readonly Color ArmorDarkRed = new Color(0.42f, 0.08f, 0.1f);
    private static readonly Color ArmorPlateColor = new Color(0.28f, 0.06f, 0.08f);
    private static readonly Color EliteRingColor = new Color(1f, 0.82f, 0.18f);
    private static readonly Color BossHornColor = new Color(0.35f, 0.05f, 0.28f);
    private static readonly Color BossArmorColor = new Color(0.22f, 0.04f, 0.18f);
    private static readonly Color FastWingColor = new Color(0.95f, 0.42f, 0.12f);

    private Enemy enemy;
    private Renderer rootRenderer;
    private Renderer bodyRenderer;
    private Transform playerTarget;
    private Color bodyBaseColor = Color.white;
    private float bodySmoothness = 0.42f;
    private bool bodyGlow;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        rootRenderer = GetComponent<Renderer>();

        if (transform.Find(VisualRootName) != null) return;

        GameObject visualRoot = new GameObject(VisualRootName);
        visualRoot.transform.SetParent(transform, false);

        BuildVisuals(visualRoot.transform, enemy.Type);

        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    private void LateUpdate()
    {
        UpdateFacing();
        SyncTelegraphVisual();
    }

    private void UpdateFacing()
    {
        if (playerTarget == null) return;

        Vector3 direction = playerTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void SyncTelegraphVisual()
    {
        if (rootRenderer == null || bodyRenderer == null) return;

        Color rootColor = rootRenderer.material.color;

        if (IsTelegraphFlash(rootColor))
        {
            GameVisualStyle.ApplyColor(bodyRenderer, rootColor, 0.55f, true, 0.65f);
            return;
        }

        GameVisualStyle.ApplyColor(bodyRenderer, bodyBaseColor, bodySmoothness, bodyGlow);
    }

    private static bool IsTelegraphFlash(Color color)
    {
        return color.r > 0.85f && color.g < 0.35f && color.b < 0.35f;
    }

    private void BuildVisuals(Transform visualRoot, Enemy.EnemyType enemyType)
    {
        switch (enemyType)
        {
            case Enemy.EnemyType.Fast:
                BuildFastVisuals(visualRoot);
                break;
            case Enemy.EnemyType.Tank:
                BuildTankVisuals(visualRoot);
                break;
            case Enemy.EnemyType.Elite:
                BuildEliteVisuals(visualRoot);
                break;
            case Enemy.EnemyType.MiniBoss:
                BuildBossVisuals(visualRoot);
                break;
            default:
                BuildNormalVisuals(visualRoot);
                break;
        }

        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }
    }

    private void BuildNormalVisuals(Transform visualRoot)
    {
        bodyBaseColor = GameVisualPalette.NormalEnemy;
        bodyRenderer = CreateBody(visualRoot, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.82f, 0.58f, 0.82f), bodyBaseColor, 0.42f, false);
        CreatePart(visualRoot, "FrontMarker", PrimitiveType.Cube, new Vector3(0f, 0f, 0.42f), new Vector3(0.18f, 0.12f, 0.08f), FrontGlowColor, 0.62f, true, 0.35f);
        CreateEyes(visualRoot, 0.34f, 0.14f, 0.08f);
    }

    private void BuildFastVisuals(Transform visualRoot)
    {
        bodyBaseColor = GameVisualPalette.FastEnemy;
        bodyRenderer = CreateBody(visualRoot, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.62f, 0.92f, 0.62f), bodyBaseColor, 0.48f, false);
        CreatePart(visualRoot, "Wing_L", PrimitiveType.Cube, new Vector3(-0.34f, 0.04f, 0.02f), new Vector3(0.08f, 0.16f, 0.42f), FastWingColor, 0.55f, false);
        CreatePart(visualRoot, "Wing_R", PrimitiveType.Cube, new Vector3(0.34f, 0.04f, 0.02f), new Vector3(0.08f, 0.16f, 0.42f), FastWingColor, 0.55f, false);
        CreatePart(visualRoot, "Spike_L", PrimitiveType.Cube, new Vector3(-0.2f, 0.18f, 0.28f), new Vector3(0.06f, 0.22f, 0.06f), FastWingColor, 0.5f, true, 0.25f);
        CreatePart(visualRoot, "Spike_R", PrimitiveType.Cube, new Vector3(0.2f, 0.18f, 0.28f), new Vector3(0.06f, 0.22f, 0.06f), FastWingColor, 0.5f, true, 0.25f);
        CreateEyes(visualRoot, 0.28f, 0.18f, 0.06f);
    }

    private void BuildTankVisuals(Transform visualRoot)
    {
        bodyBaseColor = new Color(0.52f, 0.1f, 0.12f);
        bodyGlow = false;
        bodySmoothness = 0.38f;
        bodyRenderer = CreateBody(visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.92f, 0.72f, 0.92f), bodyBaseColor, bodySmoothness, bodyGlow);
        CreatePart(visualRoot, "ArmorTop", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.72f, 0.12f, 0.72f), ArmorPlateColor, 0.45f, false);
        CreatePart(visualRoot, "ArmorFront", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.38f), new Vector3(0.62f, 0.48f, 0.12f), ArmorDarkRed, 0.42f, false);
        CreatePart(visualRoot, "ArmorSide_L", PrimitiveType.Cube, new Vector3(-0.42f, 0.02f, 0f), new Vector3(0.12f, 0.42f, 0.62f), ArmorPlateColor, 0.4f, false);
        CreatePart(visualRoot, "ArmorSide_R", PrimitiveType.Cube, new Vector3(0.42f, 0.02f, 0f), new Vector3(0.12f, 0.42f, 0.62f), ArmorPlateColor, 0.4f, false);
        CreateEyes(visualRoot, 0.36f, 0.08f, 0.07f);
    }

    private void BuildEliteVisuals(Transform visualRoot)
    {
        bodyBaseColor = GameVisualPalette.EliteEnemy;
        bodyGlow = true;
        bodySmoothness = 0.82f;
        bodyRenderer = CreateBody(visualRoot, PrimitiveType.Capsule, Vector3.zero, new Vector3(0.88f, 0.66f, 0.88f), bodyBaseColor, bodySmoothness, bodyGlow);
        CreatePart(visualRoot, "EliteRing", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(1.15f, 0.015f, 1.15f), EliteRingColor, 0.85f, true, 0.45f);
        CreatePart(visualRoot, "EliteCrown", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0.12f), new Vector3(0.22f, 0.08f, 0.14f), EliteRingColor, 0.78f, true, 0.4f);
        CreateEyes(visualRoot, 0.34f, 0.12f, 0.08f);
    }

    private void BuildBossVisuals(Transform visualRoot)
    {
        bodyBaseColor = GameVisualPalette.MiniBoss;
        bodyGlow = true;
        bodySmoothness = 0.72f;
        bodyRenderer = CreateBody(visualRoot, PrimitiveType.Capsule, new Vector3(0f, 0.04f, 0f), new Vector3(1f, 0.78f, 1f), bodyBaseColor, bodySmoothness, bodyGlow);
        CreatePart(visualRoot, "BossArmorFront", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.42f), new Vector3(0.72f, 0.56f, 0.16f), BossArmorColor, 0.48f, false);
        CreatePart(visualRoot, "BossArmorSide_L", PrimitiveType.Cube, new Vector3(-0.46f, 0.04f, 0f), new Vector3(0.14f, 0.52f, 0.72f), BossArmorColor, 0.45f, false);
        CreatePart(visualRoot, "BossArmorSide_R", PrimitiveType.Cube, new Vector3(0.46f, 0.04f, 0f), new Vector3(0.14f, 0.52f, 0.72f), BossArmorColor, 0.45f, false);
        CreatePart(visualRoot, "Horn_L", PrimitiveType.Cylinder, new Vector3(-0.18f, 0.42f, 0.18f), Quaternion.Euler(-35f, 0f, -18f), new Vector3(0.08f, 0.22f, 0.08f), BossHornColor, 0.52f, true, 0.28f);
        CreatePart(visualRoot, "Horn_R", PrimitiveType.Cylinder, new Vector3(0.18f, 0.42f, 0.18f), Quaternion.Euler(-35f, 0f, 18f), new Vector3(0.08f, 0.22f, 0.08f), BossHornColor, 0.52f, true, 0.28f);
        CreatePart(visualRoot, "BossSpike", PrimitiveType.Cube, new Vector3(0f, 0.52f, 0.28f), new Vector3(0.12f, 0.18f, 0.12f), BossHornColor, 0.55f, true, 0.32f);
        CreateEyes(visualRoot, 0.38f, 0.16f, 0.1f);
    }

    private void CreateEyes(Transform parent, float forward, float verticalOffset, float eyeSize)
    {
        CreatePart(parent, "Eye_L", PrimitiveType.Sphere, new Vector3(-eyeSize * 0.9f, verticalOffset, forward), Vector3.one * eyeSize, EyeColor, 0.72f, true, 0.35f);
        CreatePart(parent, "Eye_R", PrimitiveType.Sphere, new Vector3(eyeSize * 0.9f, verticalOffset, forward), Vector3.one * eyeSize, EyeColor, 0.72f, true, 0.35f);
    }

    private Renderer CreateBody(
        Transform parent,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow)
    {
        bodySmoothness = smoothness;
        bodyGlow = glow;
        GameObject bodyObject = CreatePart(parent, "Body", primitive, localPosition, localScale, color, smoothness, glow);
        return bodyObject.GetComponent<Renderer>();
    }

    private static GameObject CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow,
        float emissionIntensity = 0.3f)
    {
        return CreatePart(parent, partName, primitive, localPosition, Quaternion.identity, localScale, color, smoothness, glow, emissionIntensity);
    }

    private static GameObject CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color,
        float smoothness,
        bool glow,
        float emissionIntensity = 0.3f)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = partName;
        partObject.transform.SetParent(parent, false);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localRotation = localRotation;
        partObject.transform.localScale = localScale;

        Collider collider = partObject.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emissionIntensity);
        }

        return partObject;
    }
}
