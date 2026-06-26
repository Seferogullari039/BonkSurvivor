using System.Collections.Generic;
using UnityEngine;

public static class MapChestSpawner
{
    public const int DefaultMapChestCount = 8;

    private const float MinDistanceFromSpawn = 20f;
    private const float MapEdgeMargin = 10f;
    private const float MinDistanceBetweenChests = 14f;
    private const float ChestObjectRadius = 1.2f;
    private const float ChestHeightOffset = 0.5f;
    private const float MaxChestRiseAboveFlatGround = 1.35f;
    private const float SlopeExclusionRadius = 9f;
    private const int MaxPlacementAttempts = 72;
    private const int MapChestSeedSalt = 0x4D415043;

    private static readonly Vector3[] HardcodedExclusionZones =
    {
        new Vector3(-48f, 0f, -42f),
        new Vector3(48f, 0f, -42f),
        new Vector3(0f, 0f, -62f),
        new Vector3(0f, 0f, 58f),
    };

    private const float HardcodedExclusionRadius = 12f;

    public static void SpawnSeededMapChestsForRun(ChestSpawner spawner, int runSeed)
    {
        if (spawner == null)
        {
            return;
        }

        ProceduralGrassArena arena = ProceduralGrassArena.Instance;
        Vector3 spawnCenter = arena != null ? arena.SelectedPlayerSpawn : Vector3.zero;
        float halfX = arena != null ? arena.HalfSizeX : 80f;
        float halfZ = arena != null ? arena.HalfSizeZ : 80f;

        System.Random random = new System.Random(unchecked(runSeed ^ MapChestSeedSalt));
        List<Vector3> placedPositions = new List<Vector3>(DefaultMapChestCount);

        for (int i = 0; i < DefaultMapChestCount; i++)
        {
            if (!TryPickSeededPosition(random, spawnCenter, halfX, halfZ, placedPositions, out Vector3 position))
            {
                continue;
            }

            ChestRarity rarity = ChestRarityUtility.RollRandomChestRarity(random);
            spawner.SpawnMapChestAt(position, rarity, currentWave: 1);
            placedPositions.Add(position);
        }
    }

    private static bool TryPickSeededPosition(
        System.Random random,
        Vector3 spawnCenter,
        float halfX,
        float halfZ,
        List<Vector3> placedPositions,
        out Vector3 position)
    {
        position = Vector3.zero;
        float maxX = Mathf.Max(4f, halfX - MapEdgeMargin);
        float maxZ = Mathf.Max(4f, halfZ - MapEdgeMargin);
        Vector3 flatSpawn = spawnCenter;
        flatSpawn.y = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            float x = ((float)random.NextDouble() * 2f - 1f) * maxX;
            float z = ((float)random.NextDouble() * 2f - 1f) * maxZ;
            Vector3 candidate = new Vector3(x, 0f, z);
            Vector3 flatCandidate = candidate;

            if ((flatCandidate - flatSpawn).sqrMagnitude < MinDistanceFromSpawn * MinDistanceFromSpawn)
            {
                continue;
            }

            if (IsTooCloseToPlacedChests(flatCandidate, placedPositions))
            {
                continue;
            }

            if (ProceduralGrassArena.Instance != null
                && ProceduralGrassArena.Instance.IsPositionBlocked(candidate, ChestObjectRadius))
            {
                continue;
            }

            if (IsNearHardcodedExclusionZone(flatCandidate))
            {
                continue;
            }

            if (IsNearSlopeTestArea(flatCandidate))
            {
                continue;
            }

            ResolveMapChestGroundPosition(candidate, out position);
            return true;
        }

        return false;
    }

    public static void ApplyMapChestWorldTransform(Transform chestTransform, Vector3 position)
    {
        if (chestTransform == null)
        {
            return;
        }

        chestTransform.SetPositionAndRotation(position, Quaternion.identity);
        chestTransform.localScale = Vector3.one;
    }

    private static void ResolveMapChestGroundPosition(Vector3 candidate, out Vector3 position)
    {
        float flatY = ProceduralGrassArena.GetLootSpawnY(ChestHeightOffset);

        if (GroundSnapUtility.TryGetLootSpawnPosition(
                candidate,
                ChestHeightOffset,
                null,
                out Vector3 groundedPosition)
            && groundedPosition.y <= flatY + MaxChestRiseAboveFlatGround)
        {
            position = groundedPosition;
        }
        else
        {
            position = candidate;
            position.y = flatY;
        }

        ProceduralGrassArena.TryClampHorizontal(ref position);
    }

    private static bool IsTooCloseToPlacedChests(Vector3 flatCandidate, List<Vector3> placedPositions)
    {
        for (int i = 0; i < placedPositions.Count; i++)
        {
            Vector3 flatPlaced = placedPositions[i];
            flatPlaced.y = 0f;

            if ((flatCandidate - flatPlaced).sqrMagnitude < MinDistanceBetweenChests * MinDistanceBetweenChests)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearHardcodedExclusionZone(Vector3 flatCandidate)
    {
        for (int i = 0; i < HardcodedExclusionZones.Length; i++)
        {
            Vector3 zone = HardcodedExclusionZones[i];
            zone.y = 0f;

            if ((flatCandidate - zone).sqrMagnitude < HardcodedExclusionRadius * HardcodedExclusionRadius)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearSlopeTestArea(Vector3 flatCandidate)
    {
        GameObject slopeRoot = GameObject.Find("SlopeTestAreas");

        if (slopeRoot == null)
        {
            return false;
        }

        Transform rootTransform = slopeRoot.transform;

        for (int i = 0; i < rootTransform.childCount; i++)
        {
            Transform child = rootTransform.GetChild(i);

            if (child == null)
            {
                continue;
            }

            Vector3 center = child.position;
            center.y = 0f;

            if ((flatCandidate - center).sqrMagnitude < SlopeExclusionRadius * SlopeExclusionRadius)
            {
                return true;
            }
        }

        return false;
    }
}
