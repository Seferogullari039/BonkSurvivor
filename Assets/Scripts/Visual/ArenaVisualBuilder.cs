using UnityEngine;

public class ArenaVisualBuilder : MonoBehaviour
{
    private const string ArenaRootName = "Arena_Visuals";
    private const float ArenaHalfSize = 46f;
    private const float WallHeight = 4f;
    private const float WallThickness = 1.4f;
    private const float CorridorGap = 22f;

    private static readonly Color FloorColor = new Color(0.14f, 0.15f, 0.17f);
    private static readonly Color WallColor = new Color(0.22f, 0.24f, 0.28f);
    private static readonly Color CoverColor = new Color(0.18f, 0.2f, 0.24f);
    private static readonly Color NeonCyan = new Color(0.12f, 0.82f, 0.95f);
    private static readonly Color NeonMagenta = new Color(0.72f, 0.18f, 0.92f);
    private static readonly Color CoverStripeColor = new Color(0.1f, 0.72f, 0.88f);

    private static bool visualsBuilt;

    private void Awake()
    {
        if (visualsBuilt) return;
        if (ProceduralGrassArena.Instance != null) return;

        BuildArenaVisuals();
        visualsBuilt = true;
    }

    private void BuildArenaVisuals()
    {
        if (GameObject.Find(ArenaRootName) != null) return;

        StyleExistingFloor();

        GameObject arenaRoot = new GameObject(ArenaRootName);
        arenaRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        BuildBoundaryWalls(arenaRoot.transform);
        BuildCornerTowers(arenaRoot.transform);
        BuildNeonLines(arenaRoot.transform);
        BuildObstacles(arenaRoot.transform);
    }

    private static void StyleExistingFloor()
    {
        GameObject plane = GameObject.Find("Plane");

        if (plane == null) return;

        Renderer planeRenderer = plane.GetComponent<Renderer>();

        if (planeRenderer != null)
        {
            GameVisualStyle.ApplyColor(planeRenderer, FloorColor, 0.32f, false, 0f);
        }
    }

    private void BuildBoundaryWalls(Transform parent)
    {
        float segmentLength = (ArenaHalfSize * 2f - CorridorGap) * 0.5f;
        float segmentCenter = (ArenaHalfSize + CorridorGap * 0.5f) * 0.5f;

        CreateWallSegment(parent, "Wall_NorthLeft", new Vector3(-segmentCenter, WallHeight * 0.5f, ArenaHalfSize), new Vector3(segmentLength, WallHeight, WallThickness));
        CreateWallSegment(parent, "Wall_NorthRight", new Vector3(segmentCenter, WallHeight * 0.5f, ArenaHalfSize), new Vector3(segmentLength, WallHeight, WallThickness));
        CreateWallSegment(parent, "Wall_SouthLeft", new Vector3(-segmentCenter, WallHeight * 0.5f, -ArenaHalfSize), new Vector3(segmentLength, WallHeight, WallThickness));
        CreateWallSegment(parent, "Wall_SouthRight", new Vector3(segmentCenter, WallHeight * 0.5f, -ArenaHalfSize), new Vector3(segmentLength, WallHeight, WallThickness));

        CreateWallSegment(parent, "Wall_EastTop", new Vector3(ArenaHalfSize, WallHeight * 0.5f, segmentCenter), new Vector3(WallThickness, WallHeight, segmentLength));
        CreateWallSegment(parent, "Wall_EastBottom", new Vector3(ArenaHalfSize, WallHeight * 0.5f, -segmentCenter), new Vector3(WallThickness, WallHeight, segmentLength));
        CreateWallSegment(parent, "Wall_WestTop", new Vector3(-ArenaHalfSize, WallHeight * 0.5f, segmentCenter), new Vector3(WallThickness, WallHeight, segmentLength));
        CreateWallSegment(parent, "Wall_WestBottom", new Vector3(-ArenaHalfSize, WallHeight * 0.5f, -segmentCenter), new Vector3(WallThickness, WallHeight, segmentLength));
    }

    private void BuildCornerTowers(Transform parent)
    {
        float towerOffset = ArenaHalfSize - 4f;
        Vector3 towerScale = new Vector3(3.5f, 6f, 3.5f);

        CreateTower(parent, "Tower_NE", new Vector3(towerOffset, 3f, towerOffset), towerScale);
        CreateTower(parent, "Tower_NW", new Vector3(-towerOffset, 3f, towerOffset), towerScale);
        CreateTower(parent, "Tower_SE", new Vector3(towerOffset, 3f, -towerOffset), towerScale);
        CreateTower(parent, "Tower_SW", new Vector3(-towerOffset, 3f, -towerOffset), towerScale);
    }

    private void BuildNeonLines(Transform parent)
    {
        float lineY = 0.08f;
        float lineThickness = 0.12f;
        float lineLength = ArenaHalfSize * 2f - 6f;

        CreateNeonStrip(parent, "Neon_North", new Vector3(0f, lineY, ArenaHalfSize - 0.4f), new Vector3(lineLength, lineThickness, lineThickness), NeonCyan);
        CreateNeonStrip(parent, "Neon_South", new Vector3(0f, lineY, -ArenaHalfSize + 0.4f), new Vector3(lineLength, lineThickness, lineThickness), NeonMagenta);
        CreateNeonStrip(parent, "Neon_East", new Vector3(ArenaHalfSize - 0.4f, lineY, 0f), new Vector3(lineThickness, lineThickness, lineLength), NeonCyan);
        CreateNeonStrip(parent, "Neon_West", new Vector3(-ArenaHalfSize + 0.4f, lineY, 0f), new Vector3(lineThickness, lineThickness, lineLength), NeonMagenta);
    }

    private void BuildObstacles(Transform parent)
    {
        CreateCoverBlock(parent, "Cover_A", new Vector3(-18f, 1f, -14f), new Vector3(4f, 2f, 3f));
        CreateCoverBlock(parent, "Cover_B", new Vector3(20f, 0.75f, 12f), new Vector3(3f, 1.5f, 4f));
        CreateCoverBlock(parent, "Cover_C", new Vector3(-8f, 1.25f, 22f), new Vector3(5f, 2.5f, 2f));
        CreateCoverBlock(parent, "Cover_D", new Vector3(15f, 0.6f, -22f), new Vector3(2.5f, 1.2f, 5f));
        CreateCoverBlock(parent, "Cover_E", new Vector3(-25f, 1.5f, 8f), new Vector3(3f, 3f, 3f));
        CreateCoverBlock(parent, "Cover_F", new Vector3(8f, 0.5f, 8f), new Vector3(2f, 1f, 2f));
        CreateCoverBlock(parent, "Cover_G", new Vector3(-12f, 1f, -28f), new Vector3(4f, 2f, 2f));
        CreateCoverBlock(parent, "Cover_H", new Vector3(28f, 1.25f, -8f), new Vector3(2f, 2.5f, 3f));
        CreateCoverBlock(parent, "Cover_I", new Vector3(0f, 0.75f, 24f), new Vector3(3f, 1.5f, 2f));
        CreateCoverBlock(parent, "Cover_J", new Vector3(-30f, 1f, -10f), new Vector3(2.5f, 2f, 4f));
    }

    private static void CreateWallSegment(Transform parent, string partName, Vector3 position, Vector3 size)
    {
        CreateArenaPart(parent, partName, PrimitiveType.Cube, position, size, WallColor, 0.48f, false, true);
    }

    private static void CreateTower(Transform parent, string partName, Vector3 position, Vector3 size)
    {
        CreateArenaPart(parent, partName, PrimitiveType.Cube, position, size, WallColor, 0.52f, false, true);
    }

    private static void CreateNeonStrip(Transform parent, string partName, Vector3 position, Vector3 size, Color color)
    {
        CreateArenaPart(parent, partName, PrimitiveType.Cube, position, size, color, 0.72f, true, false, 0.55f);
    }

    private static void CreateCoverBlock(Transform parent, string partName, Vector3 position, Vector3 size)
    {
        GameObject cover = CreateArenaPart(parent, partName, PrimitiveType.Cube, position, size, CoverColor, 0.42f, false, true);
        float stripeHeight = Mathf.Clamp(size.y * 0.12f, 0.08f, 0.2f);
        Vector3 stripeSize = new Vector3(size.x * 0.92f, stripeHeight, size.z * 0.92f);
        Vector3 stripePosition = position + new Vector3(0f, size.y * 0.5f - stripeHeight * 0.5f, 0f);

        CreateArenaPart(
            parent,
            partName + "_Stripe",
            PrimitiveType.Cube,
            stripePosition,
            stripeSize,
            CoverStripeColor,
            0.68f,
            true,
            false,
            0.35f);
    }

    private static GameObject CreateArenaPart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 size,
        Color color,
        float smoothness,
        bool glow,
        bool collider,
        float emissionIntensity = 0.35f)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = partName;
        partObject.transform.SetParent(parent, false);
        partObject.transform.position = position;
        partObject.transform.localScale = size;
        partObject.isStatic = true;

        Collider partCollider = partObject.GetComponent<Collider>();

        if (partCollider != null)
        {
            if (collider)
            {
                partCollider.enabled = true;
            }
            else
            {
                Object.Destroy(partCollider);
            }
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emissionIntensity);
        }

        return partObject;
    }
}
