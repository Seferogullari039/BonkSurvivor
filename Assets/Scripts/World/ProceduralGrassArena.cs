using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ProceduralGrassArena : MonoBehaviour
{
    public static ProceduralGrassArena Instance { get; private set; }

    private const string ArenaRootName = "ProceduralArena";
    private const float BorderInset = 3f;
    private const float CenterClearRadius = 25f;
    private const float PlayerSpawnHeightOffset = 0.5f;
    private const int MaxPlacementAttempts = 32;
    private const int MaxPlayerSpawnAttempts = 48;
    private const int MaxChestSpawnAttempts = 80;

    public const float DefaultMapSize = 220f;
    public const float DefaultMapHalfSize = DefaultMapSize * 0.5f;
    private const float LegacyMapSize = 160f;
    private const float ReferenceHalfSize = 80f;

    [Header("Map Size")]
    [SerializeField] private float mapSizeX = DefaultMapSize;
    [SerializeField] private float mapSizeZ = DefaultMapSize;
    [SerializeField] private float groundHeight = 0f;

    [Header("Population")]
    [SerializeField] private int obstacleCount = 55;
    [SerializeField] private int rockCount = 55;
    [SerializeField] private int treeCount = 90;
    [SerializeField] private int bushCount = 120;
    [SerializeField] private int grassPatchCount = 220;
    [SerializeField] private int landmarkCount = 10;
    [SerializeField] private float minDistanceBetweenLargeObjects = 6f;
    [SerializeField] private float maxTreeCanopyRadius = 0.9f;
    [SerializeField] private float maxRockScale = 1.4f;
    [SerializeField] private float maxMountainScale = 1.8f;
    [SerializeField] private float largeObstacleMinDistanceFromPlayer = 42f;

    [Header("Interior Hills & Mountains")]
    [SerializeField] private bool generateInteriorHills = true;
    [SerializeField] private bool generateInteriorMountains = true;
    [SerializeField] private int interiorHillCount = 20;
    [SerializeField] private int interiorMountainCount = 10;
    [SerializeField] private float mountainSafeRadiusFromPlayer = 50f;
    [SerializeField] private float mountainSafeRadiusFromSpawnCenter = 18f;
    [SerializeField] private float minDistanceBetweenMountains = 18f;
    [SerializeField] private float minDistanceFromMapBorder = 18f;
    [SerializeField] private float mountainColliderRadiusMultiplier = 0.55f;
    [SerializeField] private bool useMountainColliders = true;

    [Header("Borders")]
    [SerializeField] private float wallHeight = 4f;
    [SerializeField] private float borderThickness = 2f;

    [Header("Random")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool useRandomSeed = false;
    [SerializeField] private bool randomizeSeedOnStart = true;
    [SerializeField] private bool randomizePlayerSpawnOnStart = true;
    [SerializeField] private bool randomizePlayerYawOnStart = true;
    [SerializeField] private float playerSpawnSafeRadius = 25f;
    [SerializeField] private float playerViewSafeRadius = 25f;
    [SerializeField] private float playerForwardClearRadius = 35f;
    [SerializeField] private float cameraEyeHeight = 1.65f;
    [SerializeField] private float playerSpawnMinDistanceFromCenter = 0f;
    [SerializeField] private float playerSpawnMaxDistanceFromCenter = 65f;

    [Header("Run Debug")]
    [SerializeField] private bool debugLargeSpawnLogs = false;
    [SerializeField] private bool debugArenaLogs = false;
    [SerializeField] private int currentRunSeed;
    [SerializeField] private Vector3 selectedPlayerSpawn;

    private static readonly Color GroundColor = new Color(0.28f, 0.55f, 0.22f);
    private static readonly Color GroundPatchDark = new Color(0.22f, 0.48f, 0.18f);
    private static readonly Color GroundPatchLight = new Color(0.34f, 0.62f, 0.26f);
    private static readonly Color WallColor = new Color(0.32f, 0.38f, 0.34f);
    private static readonly Color WallAccentColor = new Color(0.08f, 0.52f, 0.48f);
    private static readonly Color ObstacleColor = new Color(0.36f, 0.34f, 0.3f);
    private static readonly Color RockColor = new Color(0.45f, 0.47f, 0.5f);
    private static readonly Color TrunkColor = new Color(0.45f, 0.28f, 0.15f);
    private static readonly Color LeafColor = new Color(0.15f, 0.42f, 0.18f);
    private static readonly Color BushColor = new Color(0.12f, 0.38f, 0.16f);
    private static readonly Color LogColor = new Color(0.38f, 0.24f, 0.12f);
    private static readonly Color LandmarkColor = new Color(0.42f, 0.44f, 0.46f);
    private static readonly Color LandmarkAccentColor = new Color(0.1f, 0.5f, 0.46f);
    private static readonly Color MountainBaseColor = new Color(0.38f, 0.4f, 0.42f);
    private static readonly Color MountainTopColor = new Color(0.72f, 0.74f, 0.78f);
    private static readonly Color HillMoundColor = new Color(0.18f, 0.46f, 0.2f);
    private static readonly Color GroundPatchDirt = new Color(0.34f, 0.3f, 0.22f);
    private static readonly Color GroundPatchDry = new Color(0.42f, 0.52f, 0.28f);
    private static readonly Color GroundPathColor = new Color(0.38f, 0.34f, 0.26f);

    private struct BlockedArea
    {
        public Vector3 Center;
        public float Radius;
    }

    private Transform arenaRoot;
    private Transform groundRoot;
    private Transform bordersRoot;
    private Transform obstaclesRoot;
    private Transform treesRoot;
    private Transform rocksRoot;
    private Transform bushesRoot;
    private Transform grassPatchesRoot;
    private Transform landmarksRoot;
    private Transform interiorHillsRoot;
    private Transform interiorMountainsRoot;
    private Transform playabilityTerrainRoot;
    private readonly List<Vector3> placedLargeObjectPositions = new List<Vector3>();
    private readonly List<BlockedArea> blockedAreas = new List<BlockedArea>();
    private readonly List<Vector3> placedMountainCenters = new List<Vector3>();
    private int placedHillCount;
    private int placedMountainCount;
    private bool spawnSelected;

    public float HalfSizeX => mapSizeX * 0.5f;
    public float HalfSizeZ => mapSizeZ * 0.5f;
    public Vector3 SelectedPlayerSpawn => selectedPlayerSpawn;
    public int CurrentRunSeed => currentRunSeed;
    public Bounds ArenaBounds => GetArenaBounds();

    private void Awake()
    {
        Instance = this;
        EnsureExpandedMapDefaults();
        PrepareRunSeed(forceNewSeed: randomizeSeedOnStart || useRandomSeed);
        RegenerateArenaInternal(applyPlayerSpawn: randomizePlayerSpawnOnStart);
    }

    private void EnsureExpandedMapDefaults()
    {
        if (mapSizeX > LegacyMapSize + 0.01f && mapSizeZ > LegacyMapSize + 0.01f)
        {
            return;
        }

        mapSizeX = DefaultMapSize;
        mapSizeZ = DefaultMapSize;
    }

    private float PlayabilityScale => Mathf.Min(HalfSizeX, HalfSizeZ) / ReferenceHalfSize;

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.35f, 0.9f);
        Gizmos.DrawWireSphere(selectedPlayerSpawn, playerSpawnSafeRadius);
    }

    [ContextMenu("Regenerate Arena")]
    public void RegenerateArena()
    {
        if (Application.isPlaying && (randomizeSeedOnStart || useRandomSeed))
        {
            PrepareRunSeed(forceNewSeed: true);
        }
        else
        {
            currentRunSeed = seed;
        }

        RegenerateArenaInternal(applyPlayerSpawn: Application.isPlaying && randomizePlayerSpawnOnStart);
    }

    public void RegenerateArenaForNewRun()
    {
        gameObject.SetActive(true);
        PrepareRunSeed(forceNewSeed: true);
        RegenerateArenaInternal(applyPlayerSpawn: false);
    }

    public bool EnsureGenerated()
    {
        gameObject.SetActive(true);
        HideLegacyArena();

        if (HasValidGeneratedArena())
        {
            return true;
        }

        if (currentRunSeed == 0)
        {
            PrepareRunSeed(forceNewSeed: randomizeSeedOnStart || useRandomSeed);
        }

        RegenerateArenaInternal(applyPlayerSpawn: false);
        LogArenaDebug("EnsureGenerated");
        return true;
    }

    public Vector3 GetSafePlayerSpawn()
    {
        if (spawnSelected)
        {
            return selectedPlayerSpawn;
        }

        return new Vector3(0f, groundHeight + PlayerSpawnHeightOffset, 0f);
    }

    public void MovePlayerToSelectedSpawn(GameObject player)
    {
        if (player == null) return;

        Vector3 spawn = GetSafePlayerSpawn();
        selectedPlayerSpawn = spawn;

        CharacterController characterController = player.GetComponent<CharacterController>();
        Rigidbody rigidbody = player.GetComponent<Rigidbody>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.transform.position = spawn;
        Physics.SyncTransforms();

        if (characterController != null)
        {
            characterController.enabled = MainMenuManager.IsRunActive && FPSPlayerController.IsFpsModeActive;
        }

        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        if (debugArenaLogs)
        {
            Debug.Log("[ProceduralGrassArena] Player moved to procedural spawn " + spawn);
        }
    }

    public bool IsPositionBlocked(Vector3 position, float radius)
    {
        Vector3 flatPosition = position;
        flatPosition.y = 0f;

        for (int i = 0; i < blockedAreas.Count; i++)
        {
            BlockedArea area = blockedAreas[i];
            Vector3 flatCenter = area.Center;
            flatCenter.y = 0f;
            float combinedRadius = area.Radius + radius;

            if ((flatPosition - flatCenter).sqrMagnitude < combinedRadius * combinedRadius)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetSafePointInsideArena(float minDistanceFromPlayer, float objectRadius)
    {
        Transform playerTransform = FindPlayerObject()?.transform;
        Vector3 playerPosition = playerTransform != null ? playerTransform.position : selectedPlayerSpawn;
        float margin = minDistanceFromMapBorder;
        float maxX = HalfSizeX - margin;
        float maxZ = HalfSizeZ - margin;

        for (int attempt = 0; attempt < MaxPlayerSpawnAttempts; attempt++)
        {
            float x = UnityEngine.Random.Range(-maxX, maxX);
            float z = UnityEngine.Random.Range(-maxZ, maxZ);
            Vector3 candidate = new Vector3(x, groundHeight + PlayerSpawnHeightOffset, z);

            if (IsPositionBlocked(candidate, objectRadius))
            {
                continue;
            }

            Vector3 flatPlayer = playerPosition;
            flatPlayer.y = 0f;
            Vector3 flatCandidate = candidate;
            flatCandidate.y = 0f;

            if ((flatCandidate - flatPlayer).sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
            {
                continue;
            }

            ClampHorizontal(candidate, margin);
            return candidate;
        }

        Vector3 fallback = selectedPlayerSpawn;
        ClampHorizontal(fallback, margin);
        return fallback;
    }

    public static bool TryGetSafeChestSpawnPoint(
        Vector3 playerPosition,
        float minDistanceFromPlayer,
        float maxDistanceFromPlayer,
        float objectRadius,
        out Vector3 spawnPosition)
    {
        spawnPosition = playerPosition;

        for (int attempt = 0; attempt < MaxChestSpawnAttempts; attempt++)
        {
            float distance = UnityEngine.Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);
            Vector2 offset = UnityEngine.Random.insideUnitCircle;
            if (offset.sqrMagnitude < 0.001f)
            {
                offset = Vector2.right;
            }

            offset = offset.normalized * distance;
            Vector3 candidate = new Vector3(
                playerPosition.x + offset.x,
                GetLootSpawnY(0.5f),
                playerPosition.z + offset.y);

            TryClampHorizontal(ref candidate);
            TryResolveBlockedSpawn(ref candidate, minDistanceFromPlayer, objectRadius);

            Vector3 flatPlayer = playerPosition;
            flatPlayer.y = 0f;
            Vector3 flatCandidate = candidate;
            flatCandidate.y = 0f;

            if ((flatCandidate - flatPlayer).sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
            {
                continue;
            }

            if (Instance != null && Instance.IsPositionBlocked(candidate, objectRadius))
            {
                continue;
            }

            spawnPosition = candidate;
            return true;
        }

        if (Instance != null)
        {
            spawnPosition = Instance.GetSafePointInsideArena(minDistanceFromPlayer, objectRadius);
            spawnPosition.y = GetLootSpawnY(0.5f);
            TryClampHorizontal(ref spawnPosition);
            return true;
        }

        float fallbackDistance = Mathf.Max(minDistanceFromPlayer, 10f);
        Vector2 fallbackDirection = UnityEngine.Random.insideUnitCircle.normalized;
        if (fallbackDirection.sqrMagnitude < 0.001f)
        {
            fallbackDirection = Vector2.right;
        }

        spawnPosition = new Vector3(
            playerPosition.x + fallbackDirection.x * fallbackDistance,
            GetLootSpawnY(0.5f),
            playerPosition.z + fallbackDirection.y * fallbackDistance);
        return true;
    }

    public static bool TryResolveBlockedSpawn(ref Vector3 position, float minDistanceFromPlayer, float objectRadius)
    {
        if (Instance == null) return false;
        if (!Instance.IsPositionBlocked(position, objectRadius)) return false;

        position = Instance.GetSafePointInsideArena(minDistanceFromPlayer, objectRadius);
        return true;
    }

    public static bool TryClampHorizontal(ref Vector3 position, float margin = 2f)
    {
        if (Instance == null) return false;

        position = Instance.ClampHorizontal(position, margin);
        return true;
    }

    public static float GetLootSpawnY(float heightOffset = 0.5f)
    {
        return Instance != null ? Instance.groundHeight + heightOffset : heightOffset;
    }

    public Vector3 ClampHorizontal(Vector3 position, float margin)
    {
        float halfX = HalfSizeX - margin;
        float halfZ = HalfSizeZ - margin;
        position.x = Mathf.Clamp(position.x, -halfX, halfX);
        position.z = Mathf.Clamp(position.z, -halfZ, halfZ);
        return position;
    }

    public Bounds GetArenaBounds(float margin = 0f)
    {
        float clampedMargin = Mathf.Max(0f, margin);
        Vector3 center = new Vector3(0f, groundHeight + wallHeight * 0.5f, 0f);
        Vector3 size = new Vector3(
            Mathf.Max(1f, mapSizeX - clampedMargin * 2f),
            wallHeight,
            Mathf.Max(1f, mapSizeZ - clampedMargin * 2f));
        return new Bounds(center, size);
    }

    private void PrepareRunSeed(bool forceNewSeed)
    {
        if (forceNewSeed)
        {
            RollNewRunSeed();
            return;
        }

        currentRunSeed = seed;
    }

    private void RollNewRunSeed()
    {
        long ticks = DateTime.UtcNow.Ticks;
        int randomPart = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        currentRunSeed = unchecked((int)(ticks ^ randomPart));

        if (currentRunSeed == 0)
        {
            currentRunSeed = randomPart != 0 ? randomPart : 1;
        }

        seed = currentRunSeed;
    }

    private void RegenerateArenaInternal(bool applyPlayerSpawn)
    {
        EnsureExpandedMapDefaults();
        gameObject.SetActive(true);
        HideLegacyArena();
        ClearGeneratedChildren();
        placedLargeObjectPositions.Clear();
        blockedAreas.Clear();
        placedMountainCenters.Clear();
        placedHillCount = 0;
        placedMountainCount = 0;
        spawnSelected = false;

        arenaRoot = EnsureChildRoot(ArenaRootName);
        groundRoot = EnsureChildRoot("Ground", arenaRoot);
        bordersRoot = EnsureChildRoot("Borders", arenaRoot);
        obstaclesRoot = EnsureChildRoot("Obstacles", arenaRoot);
        treesRoot = EnsureChildRoot("Trees", arenaRoot);
        rocksRoot = EnsureChildRoot("Rocks", arenaRoot);
        bushesRoot = EnsureChildRoot("Bushes", arenaRoot);
        grassPatchesRoot = EnsureChildRoot("GrassPatches", arenaRoot);
        landmarksRoot = EnsureChildRoot("Landmarks", arenaRoot);
        interiorHillsRoot = EnsureChildRoot("InteriorHills", arenaRoot);
        interiorMountainsRoot = EnsureChildRoot("InteriorMountains", arenaRoot);
        playabilityTerrainRoot = EnsureChildRoot("PlayabilityTerrain", arenaRoot);

        System.Random random = CreateRandom();
        SelectPlayerSpawn(random);

        BuildGround();
        BuildBorders();
        BuildInteriorHills(random);
        BuildInteriorMountains(random);
        BuildPlayabilityTerrain();
        BuildGroundVariationPatches();
        BuildHorizonSilhouettes();
        EnsureValidPlayerSpawn(random);
        BuildNearFieldDressing(random);
        BuildObstacles(random);
        BuildTrees(random);
        BuildRocks(random);
        BuildRockClusters(random);
        BuildFallenLogs(random);
        BuildBushes(random);
        BuildGrassPatches(random);
        BuildLandmarks(random);

        if (applyPlayerSpawn)
        {
            ApplyPlayerSpawn(random);
        }

        LogArenaDebug("RegenerateArenaInternal");
    }

    private System.Random CreateRandom()
    {
        UnityEngine.Random.InitState(currentRunSeed);
        return new System.Random(currentRunSeed);
    }

    private void SelectPlayerSpawn(System.Random random)
    {
        float spawnY = groundHeight + PlayerSpawnHeightOffset;
        float borderMargin = BorderInset + playerSpawnSafeRadius;
        float maxX = HalfSizeX - borderMargin;
        float maxZ = HalfSizeZ - borderMargin;
        float minDistance = Mathf.Max(0f, playerSpawnMinDistanceFromCenter);
        float maxDistance = Mathf.Max(minDistance, playerSpawnMaxDistanceFromCenter);

        if (!randomizePlayerSpawnOnStart || maxX <= 0f || maxZ <= 0f)
        {
            selectedPlayerSpawn = new Vector3(0f, spawnY, 0f);
            spawnSelected = true;
            return;
        }

        for (int attempt = 0; attempt < MaxPlayerSpawnAttempts; attempt++)
        {
            float distance = RandomRange(random, minDistance, maxDistance);
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;

            if (Mathf.Abs(x) > maxX || Mathf.Abs(z) > maxZ)
            {
                continue;
            }

            Vector3 candidate = new Vector3(x, spawnY, z);

            if (IsPositionBlocked(candidate, 1.2f))
            {
                continue;
            }

            selectedPlayerSpawn = candidate;
            spawnSelected = true;
            return;
        }

        selectedPlayerSpawn = new Vector3(0f, spawnY, 0f);
        spawnSelected = true;
    }

    private void ApplyPlayerSpawn(System.Random random)
    {
        GameObject player = FindPlayerObject();
        if (player == null) return;

        MovePlayerToSelectedSpawn(player);

        if (!randomizePlayerYawOnStart) return;

        float yaw = RandomRange(random, 0f, 360f);
        player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private static GameObject FindPlayerObject()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player;

        FPSPlayerController fpsController = FindFirstObjectByType<FPSPlayerController>();
        return fpsController != null ? fpsController.gameObject : null;
    }

    private void HideLegacyArena()
    {
        GameObject plane = GameObject.Find("Plane");

        if (plane != null)
        {
            plane.SetActive(false);
        }

        GameObject legacyVisuals = GameObject.Find("Arena_Visuals");

        if (legacyVisuals != null)
        {
            legacyVisuals.SetActive(false);
        }
    }

    private void ClearGeneratedChildren()
    {
        arenaRoot = null;
        groundRoot = null;
        bordersRoot = null;
        obstaclesRoot = null;
        treesRoot = null;
        rocksRoot = null;
        bushesRoot = null;
        grassPatchesRoot = null;
        landmarksRoot = null;
        interiorHillsRoot = null;
        interiorMountainsRoot = null;
        playabilityTerrainRoot = null;

        Transform existingRoot = transform.Find(ArenaRootName);

        if (existingRoot == null) return;

        DestroyImmediate(existingRoot.gameObject);
    }

    private bool HasValidGeneratedArena()
    {
        Transform existingRoot = transform.Find(ArenaRootName);

        if (existingRoot == null) return false;

        Transform ground = existingRoot.Find("Ground");

        return ground != null && ground.childCount > 0;
    }

    private void LogArenaDebug(string context)
    {
        if (!debugArenaLogs)
        {
            return;
        }

        Bounds bounds = GetArenaBounds();
        Debug.Log(
            "[ProceduralGrassArena] " + context
            + " | CurrentRunSeed=" + currentRunSeed
            + " | SelectedPlayerSpawn=" + selectedPlayerSpawn
            + " | Hills=" + placedHillCount
            + " | Mountains=" + placedMountainCount
            + " | ArenaBounds center=" + bounds.center
            + " size=" + bounds.size);
    }

    private Transform EnsureChildRoot(string rootName, Transform parent = null)
    {
        Transform parentTransform = parent != null ? parent : transform;
        Transform existing = parentTransform.Find(rootName);

        if (existing != null)
        {
            return existing;
        }

        GameObject rootObject = new GameObject(rootName);
        rootObject.transform.SetParent(parentTransform, false);
        return rootObject.transform;
    }

    private void BuildGround()
    {
        Vector3 groundCenter = new Vector3(0f, groundHeight - 0.05f, 0f);
        Vector3 groundScale = new Vector3(mapSizeX, 0.1f, mapSizeZ);

        CreatePrimitivePart(
            groundRoot,
            "Ground_Main",
            PrimitiveType.Cube,
            groundCenter,
            groundScale,
            GroundColor,
            0.18f,
            true,
            true);
    }

    private void BuildBorders()
    {
        float halfX = HalfSizeX;
        float halfZ = HalfSizeZ;
        float y = groundHeight + wallHeight * 0.5f;

        CreateBorderWall("Border_North", new Vector3(0f, y, halfZ - borderThickness * 0.5f), new Vector3(mapSizeX, wallHeight, borderThickness), true);
        CreateBorderWall("Border_South", new Vector3(0f, y, -halfZ + borderThickness * 0.5f), new Vector3(mapSizeX, wallHeight, borderThickness), false);
        CreateBorderWall("Border_East", new Vector3(halfX - borderThickness * 0.5f, y, 0f), new Vector3(borderThickness, wallHeight, mapSizeZ), true);
        CreateBorderWall("Border_West", new Vector3(-halfX + borderThickness * 0.5f, y, 0f), new Vector3(borderThickness, wallHeight, mapSizeZ), false);
    }

    private void CreateBorderWall(string wallName, Vector3 position, Vector3 size, bool accentStrip)
    {
        CreatePrimitivePart(
            bordersRoot,
            wallName,
            PrimitiveType.Cube,
            position,
            size,
            WallColor,
            0.35f,
            true,
            true);

        if (!accentStrip) return;

        float stripHeight = Mathf.Clamp(wallHeight * 0.08f, 0.12f, 0.35f);
        Vector3 stripSize = new Vector3(size.x * 0.94f, stripHeight, size.z * 0.55f);
        Vector3 stripPosition = position + new Vector3(0f, wallHeight * 0.5f - stripHeight * 0.5f - 0.05f, 0f);

        CreatePrimitivePart(
            bordersRoot,
            wallName + "_Accent",
            PrimitiveType.Cube,
            stripPosition,
            stripSize,
            WallAccentColor,
            0.55f,
            true,
            false,
            0.25f);
    }

    private void BuildObstacles(System.Random random)
    {
        for (int i = 0; i < obstacleCount; i++)
        {
            if (!TryGetLargePlacement(random, 2.5f, 4.5f, true, out Vector3 position, out Vector3 scale, out float yaw))
            {
                continue;
            }

            CreatePrimitivePart(
                obstaclesRoot,
                $"Obstacle_{i}",
                PrimitiveType.Cube,
                position,
                scale,
                ObstacleColor,
                0.28f,
                true,
                true,
                0f,
                Quaternion.Euler(0f, yaw, 0f));

#if UNITY_EDITOR
            LogLargeObjectSpawn("large_obstacle", $"Obstacle_{i}", position, Mathf.Max(scale.x, scale.z) * 0.5f);
#endif
        }
    }

    private void BuildTrees(System.Random random)
    {
        for (int i = 0; i < treeCount; i++)
        {
            if (!TryGetLargePlacement(random, 1.2f, 2.4f, true, out Vector3 position, out _, out float yaw))
            {
                continue;
            }

            float trunkHeight = RandomRange(random, 1.4f, 2.2f);
            float trunkRadius = RandomRange(random, 0.16f, 0.24f);
            Vector3 trunkScale = new Vector3(trunkRadius * 2f, trunkHeight, trunkRadius * 2f);
            Vector3 trunkPosition = new Vector3(position.x, groundHeight + trunkHeight * 0.5f, position.z);

            float leafHeight = RandomRange(random, 0.75f, 1.15f);
            float leafRadius = RandomRange(random, 0.5f, maxTreeCanopyRadius);
            Vector3 leafScale = new Vector3(leafRadius * 2f, leafHeight, leafRadius * 2f);
            Vector3 leafPosition = trunkPosition + new Vector3(0f, trunkHeight * 0.58f + leafHeight * 0.42f, 0f);

            Vector3 flatCanopy = new Vector3(leafPosition.x, 0f, leafPosition.z);
            float canopyRadius = Mathf.Max(leafRadius, leafHeight * 0.45f);
            float canopyMinY = leafPosition.y - leafHeight * 0.5f;
            float canopyMaxY = leafPosition.y + leafHeight * 0.5f;
            if (IsInsidePlayerViewSafeZone(flatCanopy, canopyRadius)
                || IsInsidePlayerForwardClearZone(flatCanopy, canopyRadius)
                || WouldBlockPlayerCameraView(flatCanopy, canopyRadius, canopyMinY, canopyMaxY))
            {
                continue;
            }

            CreatePrimitivePart(
                treesRoot,
                $"TreeTrunk_{i}",
                PrimitiveType.Cylinder,
                trunkPosition,
                trunkScale,
                TrunkColor,
                0.2f,
                true,
                true,
                0f,
                Quaternion.Euler(0f, yaw, 0f));

            CreatePrimitivePart(
                treesRoot,
                $"TreeLeaf_{i}",
                PrimitiveType.Sphere,
                leafPosition,
                leafScale,
                LeafColor,
                0.16f,
                true,
                false,
                0f,
                Quaternion.Euler(0f, yaw * 0.5f, 0f));
        }
    }

    private void BuildRocks(System.Random random)
    {
        int singleRockCount = Mathf.Max(0, rockCount / 2);

        for (int i = 0; i < singleRockCount; i++)
        {
            if (!TryGetLargePlacement(random, 0.8f, 2.2f, true, out Vector3 position, out Vector3 scale, out float yaw))
            {
                continue;
            }

            scale.y *= RandomRange(random, 0.55f, 0.95f);
            scale.x = Mathf.Min(scale.x, maxRockScale);
            scale.y = Mathf.Min(scale.y, maxRockScale);
            scale.z = Mathf.Min(scale.z, maxRockScale);
            position.y = groundHeight + scale.y * 0.5f;

            CreatePrimitivePart(
                rocksRoot,
                $"Rock_{i}",
                PrimitiveType.Cube,
                position,
                scale,
                RockColor,
                0.22f,
                true,
                true,
                0f,
                Quaternion.Euler(RandomRange(random, -8f, 8f), yaw, RandomRange(random, -8f, 8f)));
        }
    }

    private void BuildRockClusters(System.Random random)
    {
        int clusterCount = Mathf.Max(0, rockCount - (rockCount / 2));

        for (int i = 0; i < clusterCount; i++)
        {
            if (!TryGetLargePlacement(random, 1.5f, 2.8f, true, out Vector3 center, out _, out float yaw))
            {
                continue;
            }

            int rockPieces = random.Next(2, 5);
            Vector3 clusterCenter = new Vector3(center.x, groundHeight, center.z);
            placedLargeObjectPositions.Add(clusterCenter);

#if UNITY_EDITOR
            LogLargeObjectSpawn("rock", $"RockCluster_{i}", clusterCenter, 1.4f);
#endif

            for (int piece = 0; piece < rockPieces; piece++)
            {
                float offsetX = RandomRange(random, -1.2f, 1.2f);
                float offsetZ = RandomRange(random, -1.2f, 1.2f);
                Vector3 pieceScale = new Vector3(
                    RandomRange(random, 0.5f, 1.1f),
                    RandomRange(random, 0.35f, 0.9f),
                    RandomRange(random, 0.5f, 1.1f));
                Vector3 piecePosition = clusterCenter + new Vector3(offsetX, pieceScale.y * 0.5f, offsetZ);

                CreatePrimitivePart(
                    rocksRoot,
                    $"RockCluster_{i}_{piece}",
                    PrimitiveType.Cube,
                    piecePosition,
                    pieceScale,
                    RockColor,
                    0.22f,
                    true,
                    piece == 0,
                    0f,
                    Quaternion.Euler(RandomRange(random, -12f, 12f), yaw + RandomRange(random, -25f, 25f), RandomRange(random, -12f, 12f)));
            }
        }
    }

    private void BuildFallenLogs(System.Random random)
    {
        int logCount = Mathf.Max(6, obstacleCount / 6);

        for (int i = 0; i < logCount; i++)
        {
            if (!TryGetLargePlacement(random, 1.8f, 3.5f, false, out Vector3 position, out _, out float yaw))
            {
                continue;
            }

            float length = RandomRange(random, 2.4f, 4.2f);
            float radius = RandomRange(random, 0.18f, 0.28f);
            Vector3 logScale = new Vector3(radius * 2f, radius * 2f, length);
            Vector3 logPosition = new Vector3(position.x, groundHeight + radius, position.z);

            CreatePrimitivePart(
                obstaclesRoot,
                $"FallenLog_{i}",
                PrimitiveType.Cylinder,
                logPosition,
                logScale,
                LogColor,
                0.18f,
                true,
                true,
                0f,
                Quaternion.Euler(0f, yaw, 90f));
        }
    }

    private void BuildBushes(System.Random random)
    {
        for (int i = 0; i < bushCount; i++)
        {
            if (!TryGetDecorPlacement(random, true, out Vector3 position, out float yaw))
            {
                continue;
            }

            float bushHeight = RandomRange(random, 0.55f, 1.1f);
            float bushRadius = RandomRange(random, 0.45f, 0.95f);
            Vector3 bushScale = new Vector3(bushRadius * 2f, bushHeight, bushRadius * 2f);
            Vector3 bushPosition = new Vector3(position.x, groundHeight + bushHeight * 0.45f, position.z);

            CreatePrimitivePart(
                bushesRoot,
                $"Bush_{i}",
                PrimitiveType.Sphere,
                bushPosition,
                bushScale,
                BushColor,
                0.14f,
                true,
                false,
                0f,
                Quaternion.Euler(0f, yaw, 0f));
        }
    }

    private void BuildGrassPatches(System.Random random)
    {
        for (int i = 0; i < grassPatchCount; i++)
        {
            if (!TryGetDecorPlacement(random, random.NextDouble() < 0.55, out Vector3 position, out float yaw))
            {
                continue;
            }

            int patchPieces = random.Next(2, 5);

            for (int piece = 0; piece < patchPieces; piece++)
            {
                float offsetX = RandomRange(random, -0.8f, 0.8f);
                float offsetZ = RandomRange(random, -0.8f, 0.8f);
                float width = RandomRange(random, 0.7f, 1.4f);
                float depth = RandomRange(random, 0.7f, 1.4f);
                Vector3 patchScale = new Vector3(width, 0.03f, depth);
                Vector3 patchPosition = new Vector3(
                    position.x + offsetX,
                    groundHeight + 0.015f,
                    position.z + offsetZ);

                Color patchColor = random.NextDouble() < 0.22f ? GroundPatchDirt :
                    random.NextDouble() < 0.42f ? GroundPatchDark :
                    random.NextDouble() < 0.58f ? GroundPatchDry :
                    random.NextDouble() < 0.72f ? GroundPatchLight : GroundColor;

                CreatePrimitivePart(
                    grassPatchesRoot,
                    $"GrassPatch_{i}_{piece}",
                    PrimitiveType.Cube,
                    patchPosition,
                    patchScale,
                    patchColor,
                    0.1f,
                    false,
                    false,
                    0f,
                    Quaternion.Euler(0f, yaw + RandomRange(random, -18f, 18f), 0f));
            }
        }
    }

    private void BuildLandmarks(System.Random random)
    {
        for (int i = 0; i < landmarkCount; i++)
        {
            if (!TryGetLargePlacement(random, 2.5f, 4.5f, true, out Vector3 position, out _, out float yaw))
            {
                continue;
            }

            bool usePillar = random.NextDouble() < 0.5;
            float height = RandomRange(random, 5f, 8.5f);
            float width = RandomRange(random, 1.4f, 2.4f);
            Vector3 landmarkScale = usePillar
                ? new Vector3(width, height, width)
                : new Vector3(width * 1.6f, height * 0.55f, width * 1.2f);
            Vector3 landmarkPosition = new Vector3(position.x, groundHeight + landmarkScale.y * 0.5f, position.z);
            Vector3 flatLandmark = new Vector3(landmarkPosition.x, 0f, landmarkPosition.z);
            float landmarkRadius = Mathf.Max(landmarkScale.x, landmarkScale.z) * 0.5f;
            float landmarkMinY = landmarkPosition.y - landmarkScale.y * 0.5f;
            float landmarkMaxY = landmarkPosition.y + landmarkScale.y * 0.5f;
            if (IsLargeObjectPlacementBlocked(flatLandmark, landmarkRadius, landmarkMinY, landmarkMaxY))
            {
                continue;
            }

            string landmarkName = usePillar ? $"LandmarkPillar_{i}" : $"BrokenStone_{i}";
            CreatePrimitivePart(
                landmarksRoot,
                landmarkName,
                PrimitiveType.Cube,
                landmarkPosition,
                landmarkScale,
                LandmarkColor,
                0.3f,
                true,
                true,
                0f,
                Quaternion.Euler(0f, yaw, 0f));

            float accentHeight = Mathf.Clamp(landmarkScale.y * 0.08f, 0.2f, 0.45f);
            Vector3 accentScale = new Vector3(landmarkScale.x * 0.92f, accentHeight, landmarkScale.z * 0.92f);
            Vector3 accentPosition = landmarkPosition + new Vector3(0f, landmarkScale.y * 0.5f - accentHeight * 0.5f, 0f);

            CreatePrimitivePart(
                landmarksRoot,
                $"LandmarkAccent_{i}",
                PrimitiveType.Cube,
                accentPosition,
                accentScale,
                LandmarkAccentColor,
                0.55f,
                true,
                false,
                0.2f,
                Quaternion.Euler(0f, yaw, 0f));

#if UNITY_EDITOR
            LogLargeObjectSpawn("landmark", landmarkName, landmarkPosition, landmarkRadius);
#endif
        }
    }

    private void BuildInteriorHills(System.Random random)
    {
        if (!generateInteriorHills) return;

        for (int i = 0; i < interiorHillCount; i++)
        {
            if (!TryGetMountainPlacement(random, out Vector3 center))
            {
                continue;
            }

            float horizontalRadius = RandomRange(random, 2.5f, 6f);
            float height = RandomRange(random, 1.5f, 4f);
            float scaleXZ = horizontalRadius * 2f;
            Vector3 hillScale = new Vector3(scaleXZ, height, scaleXZ);
            Vector3 hillPosition = new Vector3(center.x, groundHeight + height * 0.5f, center.z);
            float yaw = RandomRange(random, 0f, 360f);

            CreateTerrainMound(
                interiorHillsRoot,
                $"InteriorHill_{i}",
                hillPosition,
                hillScale,
                HillMoundColor,
                0.16f,
                false,
                false,
                Quaternion.Euler(0f, yaw, 0f));

            RegisterBlockedArea(center, horizontalRadius * 0.72f);
            placedMountainCenters.Add(center);
            placedHillCount++;
        }
    }

    private void BuildInteriorMountains(System.Random random)
    {
        if (!generateInteriorMountains) return;

        for (int i = 0; i < interiorMountainCount; i++)
        {
            if (!TryGetMountainPlacement(random, out Vector3 center))
            {
                continue;
            }

            bool isLargeLandmark = random.NextDouble() < 0.35f;
            float horizontalRadius;
            float height;

            if (isLargeLandmark)
            {
                horizontalRadius = RandomRange(random, 5.5f, 8f);
                height = RandomRange(random, 4.5f, 7.5f);
            }
            else
            {
                horizontalRadius = RandomRange(random, 3f, 6f);
                height = RandomRange(random, 2.5f, 5.5f);
            }

            ApplyMountainScaleLimits(ref horizontalRadius, ref height);

            float minY = groundHeight;
            float maxY = groundHeight + height;
            if (IsLargeObjectPlacementBlocked(center, horizontalRadius * 0.92f, minY, maxY))
            {
                continue;
            }

            float scaleXZ = horizontalRadius * 2f;
            Vector3 baseScale = new Vector3(scaleXZ, height, scaleXZ);
            Vector3 basePosition = new Vector3(center.x, groundHeight + height * 0.5f, center.z);
            float yaw = RandomRange(random, 0f, 360f);
            Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

            GameObject mountainRoot = CreateTerrainMound(
                interiorMountainsRoot,
                $"InteriorMountain_{i}",
                basePosition,
                baseScale,
                MountainBaseColor,
                0.24f,
                useMountainColliders,
                useMountainColliders,
                rotation,
                mountainColliderRadiusMultiplier);

            if (mountainRoot != null && random.NextDouble() < 0.65f)
            {
                float capRadius = horizontalRadius * RandomRange(random, 0.38f, 0.55f);
                float capHeight = height * RandomRange(random, 0.22f, 0.35f);
                Vector3 capScale = new Vector3(capRadius * 2f, capHeight, capRadius * 2f);
                Vector3 capPosition = basePosition + new Vector3(0f, height * 0.38f, 0f);

                CreateTerrainMound(
                    mountainRoot.transform,
                    "SnowCap",
                    capPosition,
                    capScale,
                    MountainTopColor,
                    0.18f,
                    false,
                    false,
                    rotation);
            }

            if (mountainRoot != null)
            {
                int rockPieces = random.Next(2, 4);

                for (int piece = 0; piece < rockPieces; piece++)
                {
                    float offsetX = RandomRange(random, -horizontalRadius * 0.7f, horizontalRadius * 0.7f);
                    float offsetZ = RandomRange(random, -horizontalRadius * 0.7f, horizontalRadius * 0.7f);
                    Vector3 rockScale = new Vector3(
                        RandomRange(random, 0.5f, 1.2f),
                        RandomRange(random, 0.35f, 0.8f),
                        RandomRange(random, 0.5f, 1.2f));
                    Vector3 rockPosition = new Vector3(
                        center.x + offsetX,
                        groundHeight + rockScale.y * 0.5f,
                        center.z + offsetZ);

                    CreatePrimitivePart(
                        mountainRoot.transform,
                        $"MountainRock_{piece}",
                        PrimitiveType.Cube,
                        rockPosition,
                        rockScale,
                        RockColor,
                        0.2f,
                        true,
                        false,
                        0f,
                        Quaternion.Euler(RandomRange(random, -10f, 10f), yaw + RandomRange(random, -20f, 20f), 0f));
                }
            }

            RegisterBlockedArea(center, horizontalRadius * 0.92f);
            placedMountainCenters.Add(center);
            placedLargeObjectPositions.Add(center);
            placedMountainCount++;

#if UNITY_EDITOR
            string mountainCategory = isLargeLandmark ? "floating_island" : "mountain";
            LogLargeObjectSpawn(mountainCategory, $"InteriorMountain_{i}", center, horizontalRadius);
#endif
        }
    }

    private void BuildPlayabilityTerrain()
    {
        if (playabilityTerrainRoot == null)
        {
            return;
        }

        float scale = PlayabilityScale;

        PlaceProminentHill(playabilityTerrainRoot, "PlayMound_NE", ScaleFlat(new Vector3(54f, 0f, 46f), scale), 18f * scale, 6.2f, 24f, true);
        PlaceProminentHill(playabilityTerrainRoot, "PlayMound_SW", ScaleFlat(new Vector3(-58f, 0f, -52f), scale), 19f * scale, 6.5f, 112f, true);
        PlaceProminentHill(playabilityTerrainRoot, "PlayMound_NW", ScaleFlat(new Vector3(-48f, 0f, 56f), scale), 17f * scale, 5.8f, 196f, true);

        PlaceValleyDepression(playabilityTerrainRoot, "PlayValley_SE", ScaleFlat(new Vector3(42f, 0f, -58f), scale), 22f * scale, 0.45f);
        PlaceValleyDepression(playabilityTerrainRoot, "PlayValley_West", ScaleFlat(new Vector3(-72f, 0f, 8f), scale), 20f * scale, 0.4f);

        PlaceRidgeCrossing(playabilityTerrainRoot, "PlayRidge_East", ScaleFlat(new Vector3(74f, 0f, 12f), scale), 28f * scale, 9f * scale, 8f);
        PlaceRidgeCrossing(playabilityTerrainRoot, "PlayRidge_North", ScaleFlat(new Vector3(8f, 0f, 74f), scale), 26f * scale, 8.5f * scale, 98f);
        PlaceRidgeCrossing(playabilityTerrainRoot, "PlayRidge_SW", ScaleFlat(new Vector3(-56f, 0f, -68f), scale), 24f * scale, 8f * scale, 142f);

        PlaceRuinCluster(playabilityTerrainRoot, ScaleFlat(new Vector3(62f, 0f, -28f), scale), 18f);
        PlaceStoneArch(playabilityTerrainRoot, ScaleFlat(new Vector3(-32f, 0f, 64f), scale), 22f);
        PlaceTreeCluster(playabilityTerrainRoot, ScaleFlat(new Vector3(34f, 0f, 62f), scale), 16f, 7, 2.4f);
        PlaceHilltopShrine(playabilityTerrainRoot, ScaleFlat(new Vector3(0f, 0f, 82f), scale), 13f * scale, 4.8f);
    }

    private void BuildGroundVariationPatches()
    {
        if (playabilityTerrainRoot == null)
        {
            return;
        }

        Transform patchRoot = EnsureChildRoot("GroundVariation", playabilityTerrainRoot);
        float scale = PlayabilityScale;

        Vector3 moundNe = ScaleFlat(new Vector3(54f, 0f, 46f), scale);
        Vector3 moundSw = ScaleFlat(new Vector3(-58f, 0f, -52f), scale);
        Vector3 moundNw = ScaleFlat(new Vector3(-48f, 0f, 56f), scale);
        Vector3 valleySe = ScaleFlat(new Vector3(42f, 0f, -58f), scale);
        Vector3 valleyWest = ScaleFlat(new Vector3(-72f, 0f, 8f), scale);

        CreateFlatGroundPatch(patchRoot, "HillFoot_NE", moundNe + new Vector3(-10f * scale, 0f, -8f * scale), new Vector3(16f * scale, 0.05f, 14f * scale), GroundPatchLight);
        CreateFlatGroundPatch(patchRoot, "HillFoot_SW", moundSw + new Vector3(9f * scale, 0f, 7f * scale), new Vector3(15f * scale, 0.05f, 13f * scale), GroundPatchDry);
        CreateFlatGroundPatch(patchRoot, "HillFoot_NW", moundNw + new Vector3(8f * scale, 0f, -9f * scale), new Vector3(14f * scale, 0.05f, 12f * scale), GroundPatchLight);

        CreateFlatGroundPatch(patchRoot, "ValleyFloor_SE", valleySe, new Vector3(24f * scale, 0.05f, 20f * scale), GroundPatchDirt);
        CreateFlatGroundPatch(patchRoot, "ValleyFloor_West", valleyWest, new Vector3(22f * scale, 0.05f, 18f * scale), GroundPatchDirt);

        CreateFlatGroundPatch(patchRoot, "Path_NE_SW", Vector3.Lerp(moundNe, moundSw, 0.5f), new Vector3(10f * scale, 0.04f, 28f * scale), GroundPathColor);
        CreateFlatGroundPatch(patchRoot, "Path_Center_North", ScaleFlat(new Vector3(0f, 0f, 40f), scale), new Vector3(12f * scale, 0.04f, 22f * scale), GroundPathColor);
        CreateFlatGroundPatch(patchRoot, "DryPatch_East", ScaleFlat(new Vector3(58f, 0f, -10f), scale), new Vector3(18f * scale, 0.04f, 16f * scale), GroundPatchDry);
        CreateFlatGroundPatch(patchRoot, "DryPatch_SouthWest", ScaleFlat(new Vector3(-36f, 0f, -36f), scale), new Vector3(16f * scale, 0.04f, 14f * scale), GroundPatchDry);
    }

    private void BuildHorizonSilhouettes()
    {
        if (playabilityTerrainRoot == null)
        {
            return;
        }

        Transform horizonRoot = EnsureChildRoot("HorizonDressing", playabilityTerrainRoot);
        float half = Mathf.Min(HalfSizeX, HalfSizeZ);
        float[] angles = { 12f, 38f, 64f, 98f, 128f, 156f, 188f, 214f, 246f, 278f, 306f, 334f };

        for (int i = 0; i < angles.Length; i++)
        {
            float radians = angles[i] * Mathf.Deg2Rad;
            float distance = half * RandomRangeDeterministic(i, 0.78f, 0.9f);
            Vector3 flatCenter = new Vector3(Mathf.Cos(radians) * distance, 0f, Mathf.Sin(radians) * distance);

            if (IsPlayabilityPlacementBlocked(flatCenter, 8f))
            {
                continue;
            }

            if (i % 3 == 0)
            {
                PlaceDistantRidgeSilhouette(horizonRoot, $"HorizonRidge_{i}", flatCenter, angles[i]);
            }
            else if (i % 3 == 1)
            {
                PlaceTreeCluster(horizonRoot, flatCenter, 11f + (i % 4), 6, 3.2f);
            }
            else
            {
                PlaceDistantRockSilhouette(horizonRoot, $"HorizonRock_{i}", flatCenter, angles[i]);
            }
        }
    }

    private void BuildNearFieldDressing(System.Random random)
    {
        if (playabilityTerrainRoot == null)
        {
            return;
        }

        Transform nearRoot = EnsureChildRoot("NearFieldDressing", playabilityTerrainRoot);
        int decorCount = 42;

        for (int i = 0; i < decorCount; i++)
        {
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float distance = RandomRange(random, 22f, 42f);
            Vector3 flatCenter = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);

            if (flatCenter.sqrMagnitude < (CenterClearRadius + 4f) * (CenterClearRadius + 4f))
            {
                continue;
            }

            if (spawnSelected)
            {
                Vector3 flatSpawn = selectedPlayerSpawn;
                flatSpawn.y = 0f;

                if ((flatCenter - flatSpawn).sqrMagnitude < (playerSpawnSafeRadius + 6f) * (playerSpawnSafeRadius + 6f))
                {
                    continue;
                }
            }

            if (IsPositionBlocked(flatCenter, 1.2f))
            {
                continue;
            }

            int decorType = random.Next(0, 5);

            switch (decorType)
            {
                case 0:
                    PlaceSmallBushCluster(nearRoot, flatCenter, $"NearBush_{i}", random);
                    break;
                case 1:
                    PlaceSmallRockGroup(nearRoot, flatCenter, $"NearRock_{i}", random);
                    break;
                case 2:
                    PlaceSmallRuinMarker(nearRoot, flatCenter, $"NearRuin_{i}", random);
                    break;
                case 3:
                    PlaceFallenLogDecor(nearRoot, flatCenter, $"NearLog_{i}", random);
                    break;
                default:
                    PlaceGrassTuftCluster(nearRoot, flatCenter, $"NearGrass_{i}", random);
                    break;
            }
        }
    }

    private static float RandomRangeDeterministic(int salt, float min, float max)
    {
        float t = Mathf.Abs(Mathf.Sin(salt * 12.9898f) * 43758.5453f) % 1f;
        return min + t * (max - min);
    }

    private void PlaceProminentHill(
        Transform parent,
        string name,
        Vector3 flatCenter,
        float radius,
        float peakHeight,
        float yawDegrees,
        bool addHilltopLandmark)
    {
        PlaceWalkableMound(parent, name, flatCenter, radius, peakHeight, yawDegrees, addWideRamps: true, addVisualCrown: true);

        if (!addHilltopLandmark)
        {
            return;
        }

        float moundTopY = groundHeight + peakHeight;
        PlaceHilltopLandmark(parent, name + "_Top", flatCenter, moundTopY, yawDegrees);
    }

    private void PlaceHilltopLandmark(Transform parent, string prefix, Vector3 flatCenter, float moundTopY, float yawDegrees)
    {
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        float pillarHeight = 4.2f;
        float pillarWidth = 1f;
        Vector3 pillarScale = new Vector3(pillarWidth, pillarHeight, pillarWidth);
        Vector3[] offsets =
        {
            new Vector3(-2f, 0f, -1.2f),
            new Vector3(2f, 0f, -1f),
            new Vector3(0f, 0f, 1.6f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 offset = rotation * offsets[i];
            Vector3 position = new Vector3(
                flatCenter.x + offset.x,
                moundTopY + pillarHeight * 0.5f,
                flatCenter.z + offset.z);

            CreatePrimitivePart(
                parent,
                $"{prefix}_Pillar_{i}",
                PrimitiveType.Cube,
                position,
                pillarScale,
                LandmarkColor,
                0.3f,
                true,
                false,
                0f,
                rotation);
        }

        float trunkHeight = 3.4f;
        float trunkRadius = 0.28f;
        Vector3 trunkOffset = rotation * new Vector3(0.4f, 0f, -0.3f);
        Vector3 trunkPosition = new Vector3(
            flatCenter.x + trunkOffset.x,
            moundTopY + trunkHeight * 0.5f,
            flatCenter.z + trunkOffset.z);

        CreatePrimitivePart(
            parent,
            prefix + "_TreeTrunk",
            PrimitiveType.Cylinder,
            trunkPosition,
            new Vector3(trunkRadius * 2f, trunkHeight, trunkRadius * 2f),
            TrunkColor,
            0.2f,
            true,
            false,
            0f,
            rotation);

        float leafHeight = 2.2f;
        float leafRadius = 1.35f;
        Vector3 leafPosition = trunkPosition + new Vector3(0f, trunkHeight * 0.55f + leafHeight * 0.35f, 0f);

        CreatePrimitivePart(
            parent,
            prefix + "_TreeCrown",
            PrimitiveType.Sphere,
            leafPosition,
            new Vector3(leafRadius * 2f, leafHeight, leafRadius * 2f),
            LeafColor,
            0.15f,
            true,
            false,
            0f,
            rotation);
    }

    private static Vector3 ScaleFlat(Vector3 position, float scale)
    {
        return new Vector3(position.x * scale, position.y, position.z * scale);
    }

    private bool IsPlayabilityPlacementBlocked(Vector3 flatCenter, float radius)
    {
        if (!IsInsideMapWithBorderMargin(flatCenter))
        {
            return true;
        }

        float minSpawnClearance = CenterClearRadius + radius;

        if (flatCenter.sqrMagnitude < minSpawnClearance * minSpawnClearance)
        {
            return true;
        }

        if (spawnSelected)
        {
            Vector3 flatSpawn = selectedPlayerSpawn;
            flatSpawn.y = 0f;
            float spawnClearance = playerSpawnSafeRadius + radius;

            if ((flatCenter - flatSpawn).sqrMagnitude < spawnClearance * spawnClearance)
            {
                return true;
            }
        }

        return false;
    }

    private void PlaceWalkableMound(
        Transform parent,
        string name,
        Vector3 flatCenter,
        float radius,
        float peakHeight,
        float yawDegrees,
        bool addWideRamps = false,
        bool addVisualCrown = false)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, radius))
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 mainScale = new Vector3(radius * 2f, peakHeight, radius * 1.85f);
        Vector3 mainPosition = new Vector3(flatCenter.x, groundHeight + peakHeight * 0.5f, flatCenter.z);

        CreateTerrainMound(
            parent,
            name,
            mainPosition,
            mainScale,
            HillMoundColor,
            0.16f,
            true,
            true,
            rotation);

        Vector3 blendScale = new Vector3(radius * 1.75f, peakHeight * 0.62f, radius * 1.55f);
        Vector3 blendOffset = rotation * new Vector3(-radius * 0.3f, -peakHeight * 0.14f, -radius * 0.24f);
        Vector3 blendPosition = mainPosition + blendOffset;

        CreateTerrainMound(
            parent,
            name + "_Blend",
            blendPosition,
            blendScale,
            GroundPatchLight,
            0.14f,
            true,
            true,
            rotation);

        if (addVisualCrown)
        {
            float crownHeight = peakHeight * 0.42f;
            float crownRadius = radius * 0.62f;
            Vector3 crownScale = new Vector3(crownRadius * 2f, crownHeight, crownRadius * 1.6f);
            Vector3 crownPosition = new Vector3(flatCenter.x, groundHeight + peakHeight + crownHeight * 0.42f, flatCenter.z);

            CreateTerrainMound(
                parent,
                name + "_Crown",
                crownPosition,
                crownScale,
                GroundPatchDry,
                0.12f,
                true,
                false,
                rotation);
        }

        if (addWideRamps)
        {
            float rampWidth = radius * 1.35f;
            float rampDepth = radius * 0.55f;
            float rampThickness = 0.38f;

            CreateWalkableRamp(parent, name + "_RampN", flatCenter + rotation * new Vector3(0f, 0f, radius * 0.72f), new Vector3(rampWidth, rampThickness, rampDepth), -12f, yawDegrees);
            CreateWalkableRamp(parent, name + "_RampS", flatCenter + rotation * new Vector3(0f, 0f, -radius * 0.72f), new Vector3(rampWidth, rampThickness, rampDepth), 12f, yawDegrees + 180f);
            CreateWalkableRamp(parent, name + "_RampE", flatCenter + rotation * new Vector3(radius * 0.72f, 0f, 0f), new Vector3(rampDepth, rampThickness, rampWidth), -12f, yawDegrees + 90f);
            CreateWalkableRamp(parent, name + "_RampW", flatCenter + rotation * new Vector3(-radius * 0.72f, 0f, 0f), new Vector3(rampDepth, rampThickness, rampWidth), 12f, yawDegrees - 90f);
        }

        RegisterBlockedArea(flatCenter, radius * 0.65f);
    }

    private void PlaceValleyDepression(
        Transform parent,
        string name,
        Vector3 flatCenter,
        float width,
        float depth)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, width * 0.55f))
        {
            return;
        }

        float basinHeight = Mathf.Max(0.12f, depth);
        Vector3 basinPosition = new Vector3(flatCenter.x, groundHeight - basinHeight * 0.35f, flatCenter.z);
        Vector3 basinScale = new Vector3(width * 1.5f, basinHeight, width * 1.15f);

        CreatePrimitivePart(
            parent,
            name + "_Basin",
            PrimitiveType.Cube,
            basinPosition,
            basinScale,
            GroundPatchDark,
            0.18f,
            true,
            false);

        CreateFlatGroundPatch(parent, name + "_EarthFloor", flatCenter, new Vector3(width * 1.35f, 0.05f, width * 1.05f), GroundPatchDirt);

        float rampWidth = width * 1.05f;
        float rampDepth = width * 0.48f;
        float rampThickness = 0.36f;

        CreateWalkableRamp(
            parent,
            name + "_RampNorth",
            flatCenter + new Vector3(0f, 0f, width * 0.34f),
            new Vector3(rampWidth, rampThickness, rampDepth),
            -11f,
            0f);

        CreateWalkableRamp(
            parent,
            name + "_RampSouth",
            flatCenter + new Vector3(0f, 0f, -width * 0.34f),
            new Vector3(rampWidth, rampThickness, rampDepth),
            11f,
            180f);

        CreateWalkableRamp(
            parent,
            name + "_RampEast",
            flatCenter + new Vector3(width * 0.34f, 0f, 0f),
            new Vector3(rampDepth, rampThickness, rampWidth),
            -11f,
            90f);

        CreateWalkableRamp(
            parent,
            name + "_RampWest",
            flatCenter + new Vector3(-width * 0.34f, 0f, 0f),
            new Vector3(rampDepth, rampThickness, rampWidth),
            11f,
            -90f);

        PlaceValleyDecor(parent, name, flatCenter, width);
    }

    private void PlaceValleyDecor(Transform parent, string name, Vector3 flatCenter, float width)
    {
        System.Random valleyRandom = new System.Random(unchecked((int)(flatCenter.x * 41f) ^ (int)(flatCenter.z * 19f)));
        int rockCount = 4;

        for (int i = 0; i < rockCount; i++)
        {
            float angle = RandomRange(valleyRandom, 0f, Mathf.PI * 2f);
            float distance = RandomRange(valleyRandom, width * 0.12f, width * 0.38f);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            Vector3 position = flatCenter + offset;
            Vector3 rockScale = new Vector3(
                RandomRange(valleyRandom, 0.6f, 1.1f),
                RandomRange(valleyRandom, 0.35f, 0.7f),
                RandomRange(valleyRandom, 0.6f, 1.1f));
            Vector3 rockPosition = new Vector3(position.x, groundHeight + rockScale.y * 0.5f, position.z);

            CreatePrimitivePart(
                parent,
                $"{name}_Rock_{i}",
                PrimitiveType.Cube,
                rockPosition,
                rockScale,
                RockColor,
                0.2f,
                true,
                false,
                0f,
                Quaternion.Euler(RandomRange(valleyRandom, -8f, 8f), RandomRange(valleyRandom, 0f, 360f), 0f));
        }

        for (int i = 0; i < 3; i++)
        {
            float angle = RandomRange(valleyRandom, 0f, Mathf.PI * 2f);
            float distance = RandomRange(valleyRandom, width * 0.2f, width * 0.45f);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            Vector3 position = flatCenter + offset;
            float bushHeight = RandomRange(valleyRandom, 0.5f, 0.9f);
            float bushRadius = RandomRange(valleyRandom, 0.45f, 0.8f);

            CreatePrimitivePart(
                parent,
                $"{name}_Bush_{i}",
                PrimitiveType.Sphere,
                new Vector3(position.x, groundHeight + bushHeight * 0.45f, position.z),
                new Vector3(bushRadius * 2f, bushHeight, bushRadius * 2f),
                BushColor,
                0.14f,
                true,
                false);
        }
    }

    private void PlaceRidgeCrossing(
        Transform parent,
        string name,
        Vector3 flatCenter,
        float length,
        float width,
        float yawDegrees)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, length * 0.45f))
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        float ridgeHeight = 4.4f;
        Vector3 ridgeScale = new Vector3(width, ridgeHeight, length);
        Vector3 ridgePosition = new Vector3(flatCenter.x, groundHeight + ridgeHeight * 0.5f, flatCenter.z);

        CreateTerrainMound(
            parent,
            name,
            ridgePosition,
            ridgeScale,
            MountainBaseColor,
            0.2f,
            true,
            true,
            rotation);

        float capHeight = ridgeHeight * 0.28f;
        float capWidth = width * 0.72f;
        float capLength = length * 0.55f;
        Vector3 capScale = new Vector3(capWidth, capHeight, capLength);
        Vector3 capPosition = ridgePosition + new Vector3(0f, ridgeHeight * 0.42f, 0f);

        CreateTerrainMound(
            parent,
            name + "_Cap",
            capPosition,
            capScale,
            MountainTopColor,
            0.16f,
            true,
            false,
            rotation);

        Vector3 forward = rotation * Vector3.forward;
        float rampLength = length * 0.42f;
        float rampWidth = width * 1.35f;

        CreateWalkableRamp(
            parent,
            name + "_RampA",
            flatCenter - forward * rampLength,
            new Vector3(rampWidth, 0.42f, rampLength),
            -15f,
            yawDegrees);

        CreateWalkableRamp(
            parent,
            name + "_RampB",
            flatCenter + forward * rampLength,
            new Vector3(rampWidth, 0.42f, rampLength),
            15f,
            yawDegrees + 180f);

        PlaceRidgeSilhouetteTrees(parent, name, flatCenter, forward, length, ridgeHeight);

        RegisterBlockedArea(flatCenter, width * 0.55f);
    }

    private void PlaceRidgeSilhouetteTrees(
        Transform parent,
        string name,
        Vector3 flatCenter,
        Vector3 forward,
        float length,
        float ridgeHeight)
    {
        int treeCount = 4;
        float moundTopY = groundHeight + ridgeHeight;

        for (int i = 0; i < treeCount; i++)
        {
            float t = (i + 1f) / (treeCount + 1f);
            Vector3 offset = forward * Mathf.Lerp(-length * 0.35f, length * 0.35f, t);
            Vector3 position = flatCenter + offset;
            float trunkHeight = 2.8f;
            float trunkRadius = 0.2f;
            Vector3 trunkPosition = new Vector3(position.x, moundTopY + trunkHeight * 0.5f, position.z);

            CreatePrimitivePart(
                parent,
                $"{name}_RidgeTreeTrunk_{i}",
                PrimitiveType.Cylinder,
                trunkPosition,
                new Vector3(trunkRadius * 2f, trunkHeight, trunkRadius * 2f),
                TrunkColor,
                0.2f,
                true,
                false);

            float leafHeight = 1.8f;
            float leafRadius = 1.1f;
            Vector3 leafPosition = trunkPosition + new Vector3(0f, trunkHeight * 0.55f + leafHeight * 0.35f, 0f);

            CreatePrimitivePart(
                parent,
                $"{name}_RidgeTreeCrown_{i}",
                PrimitiveType.Sphere,
                leafPosition,
                new Vector3(leafRadius * 2f, leafHeight, leafRadius * 2f),
                LeafColor,
                0.15f,
                true,
                false);
        }
    }

    private void CreateWalkableRamp(
        Transform parent,
        string name,
        Vector3 flatCenter,
        Vector3 size,
        float pitchDegrees,
        float yawDegrees)
    {
        Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        float lift = size.y * 0.5f * Mathf.Cos(Mathf.Abs(pitchDegrees) * Mathf.Deg2Rad);
        Vector3 position = new Vector3(flatCenter.x, groundHeight + lift, flatCenter.z);

        CreatePrimitivePart(
            parent,
            name,
            PrimitiveType.Cube,
            position,
            size,
            HillMoundColor,
            0.18f,
            true,
            true,
            0f,
            rotation);
    }

    private void PlaceRuinCluster(Transform parent, Vector3 flatCenter, float yawDegrees)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, 5f))
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3[] offsets =
        {
            new Vector3(-2.4f, 0f, -1.2f),
            new Vector3(0.8f, 0f, 1.6f),
            new Vector3(2.6f, 0f, -0.6f),
            new Vector3(-0.6f, 0f, 2.4f)
        };

        float[] heights = { 5.4f, 4.2f, 6.8f, 3.8f };
        float[] widths = { 1.6f, 2f, 1.4f, 1.8f };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 localOffset = rotation * offsets[i];
            Vector3 position = flatCenter + localOffset;
            float height = heights[i];
            float width = widths[i];
            Vector3 scale = new Vector3(width, height, width * 0.9f);
            Vector3 partPosition = new Vector3(position.x, groundHeight + height * 0.5f, position.z);

            CreatePrimitivePart(
                parent,
                $"RuinPillar_{i}",
                PrimitiveType.Cube,
                partPosition,
                scale,
                LandmarkColor,
                0.28f,
                true,
                true,
                0f,
                rotation);
        }

        RegisterBlockedArea(flatCenter, 4.5f);
    }

    private void PlaceStoneArch(Transform parent, Vector3 flatCenter, float yawDegrees)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, 4.5f))
        {
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        Vector3 leftOffset = rotation * new Vector3(-2.2f, 0f, 0f);
        Vector3 rightOffset = rotation * new Vector3(2.2f, 0f, 0f);
        float pillarHeight = 4.8f;
        float pillarWidth = 1.1f;
        Vector3 pillarScale = new Vector3(pillarWidth, pillarHeight, pillarWidth);

        CreatePrimitivePart(
            parent,
            "StoneArch_Left",
            PrimitiveType.Cube,
            new Vector3(flatCenter.x + leftOffset.x, groundHeight + pillarHeight * 0.5f, flatCenter.z + leftOffset.z),
            pillarScale,
            LandmarkColor,
            0.3f,
            true,
            true,
            0f,
            rotation);

        CreatePrimitivePart(
            parent,
            "StoneArch_Right",
            PrimitiveType.Cube,
            new Vector3(flatCenter.x + rightOffset.x, groundHeight + pillarHeight * 0.5f, flatCenter.z + rightOffset.z),
            pillarScale,
            LandmarkColor,
            0.3f,
            true,
            true,
            0f,
            rotation);

        Vector3 lintelScale = new Vector3(5.2f, 0.7f, 1.2f);
        Vector3 lintelPosition = new Vector3(flatCenter.x, groundHeight + pillarHeight + lintelScale.y * 0.5f, flatCenter.z);

        CreatePrimitivePart(
            parent,
            "StoneArch_Lintel",
            PrimitiveType.Cube,
            lintelPosition,
            lintelScale,
            LandmarkAccentColor,
            0.35f,
            true,
            true,
            0f,
            rotation);

        RegisterBlockedArea(flatCenter, 3.8f);
    }

    private void PlaceTreeCluster(Transform parent, Vector3 flatCenter, float radius, int treeCountInCluster = 5, float maxTrunkHeight = 2.1f)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, radius))
        {
            return;
        }

        System.Random clusterRandom = new System.Random(unchecked((int)(flatCenter.x * 17f) ^ (int)(flatCenter.z * 31f)));

        for (int i = 0; i < treeCountInCluster; i++)
        {
            float angle = RandomRange(clusterRandom, 0f, Mathf.PI * 2f);
            float distance = RandomRange(clusterRandom, radius * 0.15f, radius);
            Vector3 position = flatCenter + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            float trunkHeight = RandomRange(clusterRandom, maxTrunkHeight * 0.72f, maxTrunkHeight);
            float trunkRadius = RandomRange(clusterRandom, 0.16f, 0.28f);
            Vector3 trunkScale = new Vector3(trunkRadius * 2f, trunkHeight, trunkRadius * 2f);
            Vector3 trunkPosition = new Vector3(position.x, groundHeight + trunkHeight * 0.5f, position.z);
            float yaw = RandomRange(clusterRandom, 0f, 360f);

            CreatePrimitivePart(
                parent,
                $"ClusterTrunk_{i}",
                PrimitiveType.Cylinder,
                trunkPosition,
                trunkScale,
                TrunkColor,
                0.2f,
                true,
                false,
                0f,
                Quaternion.Euler(0f, yaw, 0f));

            float leafHeight = RandomRange(clusterRandom, 0.85f, 1.35f);
            float leafRadius = RandomRange(clusterRandom, 0.55f, 0.95f);
            Vector3 leafScale = new Vector3(leafRadius * 2f, leafHeight, leafRadius * 2f);
            Vector3 leafPosition = trunkPosition + new Vector3(0f, trunkHeight * 0.55f + leafHeight * 0.4f, 0f);

            CreatePrimitivePart(
                parent,
                $"ClusterLeaf_{i}",
                PrimitiveType.Sphere,
                leafPosition,
                leafScale,
                LeafColor,
                0.16f,
                true,
                false,
                0f,
                Quaternion.Euler(0f, yaw * 0.5f, 0f));
        }

        RegisterBlockedArea(flatCenter, radius * 0.75f);
    }

    private void CreateFlatGroundPatch(Transform parent, string name, Vector3 flatCenter, Vector3 size, Color color)
    {
        Vector3 position = new Vector3(flatCenter.x, groundHeight + size.y * 0.5f, flatCenter.z);

        CreatePrimitivePart(
            parent,
            name,
            PrimitiveType.Cube,
            position,
            size,
            color,
            0.11f,
            false,
            false);
    }

    private void PlaceDistantRidgeSilhouette(Transform parent, string name, Vector3 flatCenter, float yawDegrees)
    {
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        float ridgeHeight = 7.5f;
        float ridgeLength = 22f;
        float ridgeWidth = 5.5f;
        Vector3 ridgeScale = new Vector3(ridgeWidth, ridgeHeight, ridgeLength);
        Vector3 ridgePosition = new Vector3(flatCenter.x, groundHeight + ridgeHeight * 0.5f, flatCenter.z);

        CreateTerrainMound(
            parent,
            name,
            ridgePosition,
            ridgeScale,
            MountainBaseColor,
            0.18f,
            true,
            false,
            rotation);

        Vector3 capScale = new Vector3(ridgeWidth * 0.7f, ridgeHeight * 0.22f, ridgeLength * 0.55f);
        Vector3 capPosition = ridgePosition + new Vector3(0f, ridgeHeight * 0.38f, 0f);

        CreateTerrainMound(
            parent,
            name + "_Cap",
            capPosition,
            capScale,
            MountainTopColor,
            0.14f,
            true,
            false,
            rotation);

        Vector3 forward = rotation * Vector3.forward;

        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * 0.35f;
            Vector3 treeBase = flatCenter + forward * (ridgeLength * t * 0.25f);
            float trunkHeight = 3.6f;
            Vector3 trunkPosition = new Vector3(treeBase.x, groundHeight + ridgeHeight + trunkHeight * 0.5f, treeBase.z);

            CreatePrimitivePart(
                parent,
                $"{name}_Tree_{i}",
                PrimitiveType.Cylinder,
                trunkPosition,
                new Vector3(0.5f, trunkHeight, 0.5f),
                TrunkColor,
                0.18f,
                true,
                false,
                0f,
                rotation);

            CreatePrimitivePart(
                parent,
                $"{name}_Crown_{i}",
                PrimitiveType.Sphere,
                trunkPosition + new Vector3(0f, trunkHeight * 0.65f, 0f),
                new Vector3(2.4f, 2f, 2.4f),
                LeafColor,
                0.14f,
                true,
                false,
                0f,
                rotation);
        }
    }

    private void PlaceDistantRockSilhouette(Transform parent, string name, Vector3 flatCenter, float yawDegrees)
    {
        Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        float height = 8.5f;
        float width = 5f;
        Vector3 scale = new Vector3(width, height, width * 0.85f);
        Vector3 position = new Vector3(flatCenter.x, groundHeight + height * 0.5f, flatCenter.z);

        CreatePrimitivePart(
            parent,
            name,
            PrimitiveType.Cube,
            position,
            scale,
            LandmarkColor,
            0.26f,
            true,
            false,
            0f,
            rotation);

        Vector3 accentScale = new Vector3(width * 0.82f, height * 0.12f, width * 0.82f);
        Vector3 accentPosition = position + new Vector3(0f, height * 0.5f - accentScale.y * 0.5f, 0f);

        CreatePrimitivePart(
            parent,
            name + "_Accent",
            PrimitiveType.Cube,
            accentPosition,
            accentScale,
            LandmarkAccentColor,
            0.32f,
            true,
            false,
            0f,
            rotation);
    }

    private void PlaceSmallBushCluster(Transform parent, Vector3 flatCenter, string name, System.Random random)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3(RandomRange(random, -1.2f, 1.2f), 0f, RandomRange(random, -1.2f, 1.2f));
            Vector3 position = flatCenter + offset;
            float bushHeight = RandomRange(random, 0.55f, 1f);
            float bushRadius = RandomRange(random, 0.5f, 0.9f);

            CreatePrimitivePart(
                parent,
                $"{name}_{i}",
                PrimitiveType.Sphere,
                new Vector3(position.x, groundHeight + bushHeight * 0.45f, position.z),
                new Vector3(bushRadius * 2f, bushHeight, bushRadius * 2f),
                BushColor,
                0.14f,
                true,
                false);
        }
    }

    private void PlaceSmallRockGroup(Transform parent, Vector3 flatCenter, string name, System.Random random)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = new Vector3(RandomRange(random, -1f, 1f), 0f, RandomRange(random, -1f, 1f));
            Vector3 position = flatCenter + offset;
            Vector3 scale = new Vector3(
                RandomRange(random, 0.5f, 0.9f),
                RandomRange(random, 0.35f, 0.65f),
                RandomRange(random, 0.5f, 0.9f));
            Vector3 rockPosition = new Vector3(position.x, groundHeight + scale.y * 0.5f, position.z);

            CreatePrimitivePart(
                parent,
                $"{name}_{i}",
                PrimitiveType.Cube,
                rockPosition,
                scale,
                RockColor,
                0.2f,
                true,
                false,
                0f,
                Quaternion.Euler(RandomRange(random, -10f, 10f), RandomRange(random, 0f, 360f), 0f));
        }
    }

    private void PlaceSmallRuinMarker(Transform parent, Vector3 flatCenter, string name, System.Random random)
    {
        float height = RandomRange(random, 2.4f, 3.6f);
        float width = RandomRange(random, 0.8f, 1.2f);
        Vector3 scale = new Vector3(width, height, width);
        Vector3 position = new Vector3(flatCenter.x, groundHeight + height * 0.5f, flatCenter.z);
        float yaw = RandomRange(random, 0f, 360f);

        CreatePrimitivePart(
            parent,
            name,
            PrimitiveType.Cube,
            position,
            scale,
            LandmarkColor,
            0.28f,
            true,
            false,
            0f,
            Quaternion.Euler(0f, yaw, 0f));
    }

    private void PlaceFallenLogDecor(Transform parent, Vector3 flatCenter, string name, System.Random random)
    {
        float length = RandomRange(random, 1.8f, 2.8f);
        float radius = RandomRange(random, 0.14f, 0.22f);
        float yaw = RandomRange(random, 0f, 360f);
        Vector3 scale = new Vector3(radius * 2f, radius * 2f, length);
        Vector3 position = new Vector3(flatCenter.x, groundHeight + radius, flatCenter.z);

        CreatePrimitivePart(
            parent,
            name,
            PrimitiveType.Cylinder,
            position,
            scale,
            LogColor,
            0.18f,
            true,
            false,
            0f,
            Quaternion.Euler(0f, yaw, 90f));
    }

    private void PlaceGrassTuftCluster(Transform parent, Vector3 flatCenter, string name, System.Random random)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3(RandomRange(random, -0.9f, 0.9f), 0f, RandomRange(random, -0.9f, 0.9f));
            Vector3 position = flatCenter + offset;
            float width = RandomRange(random, 0.6f, 1.1f);
            float depth = RandomRange(random, 0.6f, 1.1f);
            Color patchColor = random.NextDouble() < 0.5f ? GroundPatchLight : GroundPatchDry;

            CreatePrimitivePart(
                parent,
                $"{name}_{i}",
                PrimitiveType.Cube,
                new Vector3(position.x, groundHeight + 0.02f, position.z),
                new Vector3(width, 0.03f, depth),
                patchColor,
                0.1f,
                false,
                false,
                0f,
                Quaternion.Euler(0f, RandomRange(random, 0f, 360f), 0f));
        }
    }

    private void PlaceHilltopShrine(Transform parent, Vector3 flatCenter, float radius, float peakHeight)
    {
        if (IsPlayabilityPlacementBlocked(flatCenter, radius))
        {
            return;
        }

        PlaceWalkableMound(parent, "HilltopShrine_Mound", flatCenter, radius, peakHeight, 0f, addWideRamps: true, addVisualCrown: true);

        float pillarHeight = 4.6f;
        float pillarWidth = 1.05f;
        Vector3 pillarScale = new Vector3(pillarWidth, pillarHeight, pillarWidth);
        float moundTopY = groundHeight + peakHeight;
        Vector3[] pillarOffsets =
        {
            new Vector3(-1.6f, 0f, 0f),
            new Vector3(1.6f, 0f, 0f),
            new Vector3(0f, 0f, -1.4f)
        };

        for (int i = 0; i < pillarOffsets.Length; i++)
        {
            Vector3 offset = pillarOffsets[i];
            Vector3 position = new Vector3(
                flatCenter.x + offset.x,
                moundTopY + pillarHeight * 0.5f,
                flatCenter.z + offset.z);

            CreatePrimitivePart(
                parent,
                $"HilltopPillar_{i}",
                PrimitiveType.Cube,
                position,
                pillarScale,
                LandmarkAccentColor,
                0.4f,
                true,
                false);
        }
    }

    private void EnsureValidPlayerSpawn(System.Random random)
    {
        if (!spawnSelected || !IsPositionBlocked(selectedPlayerSpawn, 1.2f))
        {
            return;
        }

        float spawnY = groundHeight + PlayerSpawnHeightOffset;
        float borderMargin = BorderInset + playerSpawnSafeRadius;
        float maxX = HalfSizeX - borderMargin;
        float maxZ = HalfSizeZ - borderMargin;
        float minDistance = Mathf.Max(0f, playerSpawnMinDistanceFromCenter);
        float maxDistance = Mathf.Max(minDistance, playerSpawnMaxDistanceFromCenter);

        for (int attempt = 0; attempt < MaxPlayerSpawnAttempts; attempt++)
        {
            float distance = RandomRange(random, minDistance, maxDistance);
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float x = Mathf.Cos(angle) * distance;
            float z = Mathf.Sin(angle) * distance;

            if (Mathf.Abs(x) > maxX || Mathf.Abs(z) > maxZ)
            {
                continue;
            }

            Vector3 candidate = new Vector3(x, spawnY, z);

            if (IsPositionBlocked(candidate, 1.2f))
            {
                continue;
            }

            selectedPlayerSpawn = candidate;
            return;
        }

        selectedPlayerSpawn = new Vector3(0f, spawnY, 0f);

        if (IsPositionBlocked(selectedPlayerSpawn, 1.2f))
        {
            selectedPlayerSpawn = GetSafePointInsideArena(0f, 1.2f);
        }
    }

    private bool TryGetMountainPlacement(System.Random random, out Vector3 center)
    {
        center = Vector3.zero;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GetInteriorTerrainPosition(random, spreadAcrossMap: true);

            if (!IsInsideMapWithBorderMargin(candidate))
            {
                continue;
            }

            if (IsTooCloseToMountainCenters(candidate, minDistanceBetweenMountains))
            {
                continue;
            }

            if (IsTooCloseToPlayerSpawnForMountain(candidate))
            {
                continue;
            }

            if (IsInsidePlayerViewSafeZone(candidate, 6f))
            {
                continue;
            }

            if (IsInsidePlayerForwardClearZone(candidate, 6f))
            {
                continue;
            }

            if (candidate.sqrMagnitude < mountainSafeRadiusFromSpawnCenter * mountainSafeRadiusFromSpawnCenter)
            {
                continue;
            }

            if (IsTooCloseToLargeObjects(candidate, minDistanceBetweenMountains * 0.45f))
            {
                continue;
            }

            center = candidate;
            return true;
        }

        return false;
    }

    private Vector3 GetInteriorTerrainPosition(System.Random random, bool spreadAcrossMap)
    {
        if (spreadAcrossMap && random.NextDouble() < 0.85f)
        {
            float normalizedDistance = RandomRange(random, 0.28f, 0.82f);
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float x = Mathf.Cos(angle) * HalfSizeX * normalizedDistance;
            float z = Mathf.Sin(angle) * HalfSizeZ * normalizedDistance;
            return new Vector3(x, groundHeight, z);
        }

        float randomX = RandomRange(random, -HalfSizeX + minDistanceFromMapBorder, HalfSizeX - minDistanceFromMapBorder);
        float randomZ = RandomRange(random, -HalfSizeZ + minDistanceFromMapBorder, HalfSizeZ - minDistanceFromMapBorder);
        return new Vector3(randomX, groundHeight, randomZ);
    }

    private bool IsInsideMapWithBorderMargin(Vector3 candidate)
    {
        return Mathf.Abs(candidate.x) <= HalfSizeX - minDistanceFromMapBorder
            && Mathf.Abs(candidate.z) <= HalfSizeZ - minDistanceFromMapBorder;
    }

    private bool IsTooCloseToPlayerSpawnForMountain(Vector3 candidate)
    {
        if (!spawnSelected) return false;

        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        Vector3 flatCandidate = candidate;
        flatCandidate.y = 0f;
        float safeRadius = Mathf.Max(mountainSafeRadiusFromPlayer, playerForwardClearRadius) + 6f;

        return (flatCandidate - flatSpawn).sqrMagnitude < safeRadius * safeRadius;
    }

    private void ApplyMountainScaleLimits(ref float horizontalRadius, ref float height)
    {
        float maxHoriz = maxMountainScale * 4.5f;
        float maxHeight = maxMountainScale * 3.5f;
        horizontalRadius = Mathf.Min(horizontalRadius, maxHoriz);
        height = Mathf.Min(height, maxHeight);
    }

    private Vector3 GetPlayerSpawnForward()
    {
        Vector3 forward = selectedPlayerSpawn;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.25f)
        {
            return Vector3.forward;
        }

        return forward.normalized;
    }

    private bool IsInsidePlayerViewSafeZone(Vector3 flatCandidate, float objectRadius)
    {
        if (!spawnSelected) return false;

        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        flatCandidate.y = 0f;
        float requiredDistance = playerViewSafeRadius + objectRadius;

        return (flatCandidate - flatSpawn).sqrMagnitude < requiredDistance * requiredDistance;
    }

    private bool IsInsidePlayerForwardClearZone(Vector3 flatCandidate, float objectRadius)
    {
        if (!spawnSelected) return false;

        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        flatCandidate.y = 0f;
        Vector3 toCandidate = flatCandidate - flatSpawn;
        float horizontalDistance = toCandidate.magnitude;

        if (horizontalDistance < 0.01f)
        {
            return true;
        }

        Vector3 forward = GetPlayerSpawnForward();
        float forwardAlignment = Vector3.Dot(toCandidate / horizontalDistance, forward);

        if (forwardAlignment < 0.35f)
        {
            return false;
        }

        float requiredDistance = playerForwardClearRadius + objectRadius;
        return horizontalDistance < requiredDistance;
    }

    private bool IsLargeObjectPlacementBlocked(Vector3 flatCandidate, float objectRadius, float minY, float maxY)
    {
        if (IsInsidePlayerViewSafeZone(flatCandidate, objectRadius))
        {
            return true;
        }

        if (IsInsidePlayerForwardClearZone(flatCandidate, objectRadius))
        {
            return true;
        }

        return WouldBlockPlayerCameraView(flatCandidate, objectRadius, minY, maxY);
    }

    private bool WouldBlockPlayerCameraView(Vector3 flatCandidate, float objectRadius, float minY, float maxY)
    {
        if (!spawnSelected) return false;

        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        flatCandidate.y = 0f;
        float horizontalDistance = Vector3.Distance(flatCandidate, flatSpawn);
        float nearbyRadius = playerViewSafeRadius + objectRadius + 10f;

        if (horizontalDistance > nearbyRadius)
        {
            return false;
        }

        float eyeY = groundHeight + cameraEyeHeight;
        const float eyeBandHalfHeight = 0.45f;
        float eyeBandMin = eyeY - eyeBandHalfHeight;
        float eyeBandMax = eyeY + eyeBandHalfHeight;

        if (maxY >= eyeBandMin && minY <= eyeBandMax)
        {
            return true;
        }

        if (minY < eyeY + 0.25f && maxY > eyeY + 1.5f && horizontalDistance < playerForwardClearRadius + objectRadius)
        {
            return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void LogLargeObjectSpawn(string category, string objectName, Vector3 worldPosition, float objectRadius)
    {
        if (!spawnSelected || !debugLargeSpawnLogs)
        {
            return;
        }

        Vector3 flatPosition = worldPosition;
        flatPosition.y = 0f;
        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        float distance = Vector3.Distance(flatPosition, flatSpawn);
        Debug.Log(
            "[ProceduralGrassArena] Large spawn: category="
            + category
            + ", name="
            + objectName
            + ", distance="
            + distance.ToString("F1")
            + "m, radius="
            + objectRadius.ToString("F1")
            + "m");
    }
#endif

    private bool IsTooCloseToMountainCenters(Vector3 candidate, float minDistance)
    {
        for (int i = 0; i < placedMountainCenters.Count; i++)
        {
            if ((placedMountainCenters[i] - candidate).sqrMagnitude < minDistance * minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void RegisterBlockedArea(Vector3 flatCenter, float radius)
    {
        blockedAreas.Add(new BlockedArea
        {
            Center = new Vector3(flatCenter.x, groundHeight, flatCenter.z),
            Radius = radius
        });
    }

    private GameObject CreateTerrainMound(
        Transform parent,
        string partName,
        Vector3 position,
        Vector3 scale,
        Color color,
        float smoothness,
        bool castShadow,
        bool useCollider,
        Quaternion rotation,
        float colliderRadiusOverride = -1f)
    {
        GameObject moundObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moundObject.name = partName;
        moundObject.transform.SetParent(parent, false);
        moundObject.transform.position = position;
        moundObject.transform.localScale = scale;
        moundObject.transform.rotation = rotation;
        moundObject.isStatic = true;

        Collider defaultCollider = moundObject.GetComponent<Collider>();

        if (defaultCollider != null)
        {
            Destroy(defaultCollider);
        }

        if (useCollider && useMountainColliders)
        {
            SphereCollider sphereCollider = moundObject.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.5f * Mathf.Max(0.35f, colliderRadiusOverride > 0f ? colliderRadiusOverride : mountainColliderRadiusMultiplier);
            sphereCollider.center = Vector3.zero;
        }

        Renderer renderer = moundObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.shadowCastingMode = castShadow
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            GameVisualStyle.ApplyColor(renderer, color, smoothness, false, 0f);
        }

        return moundObject;
    }

    private bool TryGetLargePlacement(
        System.Random random,
        float minScale,
        float maxScale,
        bool preferEdges,
        out Vector3 position,
        out Vector3 scale,
        out float yaw)
    {
        position = Vector3.zero;
        scale = Vector3.one;
        yaw = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GetRandomFlatPosition(random, preferEdges);

            if (IsBlockedPlacement(candidate))
            {
                continue;
            }

            float sizeX = RandomRange(random, minScale, maxScale);
            float sizeY = RandomRange(random, minScale * 0.7f, maxScale);
            float sizeZ = RandomRange(random, minScale, maxScale);
            sizeX = Mathf.Min(sizeX, maxRockScale);
            sizeY = Mathf.Min(sizeY, maxRockScale);
            sizeZ = Mathf.Min(sizeZ, maxRockScale);
            scale = new Vector3(sizeX, sizeY, sizeZ);
            position = candidate + new Vector3(0f, scale.y * 0.5f, 0f);
            yaw = RandomRange(random, 0f, 360f);

            if (IsTooCloseForLargeObstacle(candidate, Mathf.Max(scale.x, scale.z) * 0.5f))
            {
                continue;
            }

            float objectRadius = Mathf.Max(scale.x, scale.z) * 0.5f;
            float minY = groundHeight;
            float maxY = groundHeight + scale.y;
            if (IsLargeObjectPlacementBlocked(candidate, objectRadius, minY, maxY))
            {
                continue;
            }

            if (IsTooCloseToLargeObjects(candidate, objectRadius))
            {
                continue;
            }

            placedLargeObjectPositions.Add(new Vector3(candidate.x, 0f, candidate.z));
            return true;
        }

        return false;
    }

    private bool TryGetDecorPlacement(System.Random random, bool preferEdges, out Vector3 position, out float yaw)
    {
        position = Vector3.zero;
        yaw = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GetRandomFlatPosition(random, preferEdges);

            if (IsBlockedPlacement(candidate))
            {
                continue;
            }

            position = candidate;
            yaw = RandomRange(random, 0f, 360f);
            return true;
        }

        return false;
    }

    private Vector3 GetRandomFlatPosition(System.Random random, bool preferEdges)
    {
        if (preferEdges && random.NextDouble() < 0.78f)
        {
            float normalizedDistance = RandomRange(random, 0.58f, 0.92f);
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float x = Mathf.Cos(angle) * HalfSizeX * normalizedDistance;
            float z = Mathf.Sin(angle) * HalfSizeZ * normalizedDistance;
            return new Vector3(x, groundHeight, z);
        }

        float randomX = RandomRange(random, -HalfSizeX + BorderInset, HalfSizeX - BorderInset);
        float randomZ = RandomRange(random, -HalfSizeZ + BorderInset, HalfSizeZ - BorderInset);
        return new Vector3(randomX, groundHeight, randomZ);
    }

    private bool IsBlockedPlacement(Vector3 candidate)
    {
        if (IsInsidePlayerViewSafeZone(candidate, 0f))
        {
            return true;
        }

        if (candidate.sqrMagnitude < CenterClearRadius * CenterClearRadius)
        {
            return true;
        }

        return IsPositionBlocked(candidate, 0.85f);
    }

    private bool IsTooCloseForLargeObstacle(Vector3 candidate, float objectRadius)
    {
        if (!spawnSelected) return false;

        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        Vector3 flatCandidate = candidate;
        flatCandidate.y = 0f;

        float minDistance = Mathf.Max(largeObstacleMinDistanceFromPlayer, playerViewSafeRadius);
        float requiredDistance = minDistance + objectRadius;

        return (flatCandidate - flatSpawn).sqrMagnitude < requiredDistance * requiredDistance;
    }

    private bool IsInsidePlayerSpawnSafeZone(Vector3 worldPosition)
    {
        Vector3 flatPosition = worldPosition;
        flatPosition.y = 0f;
        Vector3 flatSpawn = selectedPlayerSpawn;
        flatSpawn.y = 0f;
        return (flatPosition - flatSpawn).sqrMagnitude < playerSpawnSafeRadius * playerSpawnSafeRadius;
    }

    private bool IsTooCloseToLargeObjects(Vector3 flatCandidate, float objectRadius)
    {
        float minDistance = minDistanceBetweenLargeObjects + objectRadius;

        for (int i = 0; i < placedLargeObjectPositions.Count; i++)
        {
            Vector3 existing = placedLargeObjectPositions[i];

            if ((existing - flatCandidate).sqrMagnitude < minDistance * minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private static float RandomRange(System.Random random, float min, float max)
    {
        return (float)(min + random.NextDouble() * (max - min));
    }

    private static GameObject CreatePrimitivePart(
        Transform parent,
        string partName,
        PrimitiveType primitive,
        Vector3 position,
        Vector3 size,
        Color color,
        float smoothness,
        bool castShadow,
        bool collider,
        float emissionIntensity = 0f,
        Quaternion rotation = default)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = partName;
        partObject.transform.SetParent(parent, false);
        partObject.transform.position = position;
        partObject.transform.localScale = size;
        partObject.transform.rotation = rotation == default ? Quaternion.identity : rotation;
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
                Destroy(partCollider);
            }
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.shadowCastingMode = castShadow
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.Off;
            GameVisualStyle.ApplyColor(renderer, color, smoothness, emissionIntensity > 0f, emissionIntensity);
        }

        return partObject;
    }
}
