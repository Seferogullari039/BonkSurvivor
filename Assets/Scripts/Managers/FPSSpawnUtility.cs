using UnityEngine;

public enum FPSSpawnZone
{
    Front,
    Side,
    Back
}

public static class FPSSpawnUtility
{
    private const float MinSpawnDistance = 14f;
    private const float MaxSpawnDistance = 28f;
    private const float BackMinSpawnDistance = 22f;

    public static FPSSpawnZone RollSpawnZone(int currentWave)
    {
        float roll = Random.value;

        if (currentWave <= 3)
        {
            return roll < 0.78f ? FPSSpawnZone.Front : FPSSpawnZone.Side;
        }

        if (roll < 0.7f) return FPSSpawnZone.Front;
        if (roll < 0.9f) return FPSSpawnZone.Side;
        return FPSSpawnZone.Back;
    }

    public static Vector3 GetSpawnOffset(Transform player, FPSSpawnZone zone)
    {
        if (player == null) return Vector3.zero;

        float minDistance = zone == FPSSpawnZone.Back ? BackMinSpawnDistance : MinSpawnDistance;
        float maxDistance = MaxSpawnDistance;
        float distance = Random.Range(minDistance, maxDistance);
        float angle = GetRandomAngleForZone(zone);

        Vector3 forward = player.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        return Quaternion.Euler(0f, angle, 0f) * forward * distance;
    }

    public static Vector3 GetBossSpawnOffset(Transform player)
    {
        return GetSpawnOffset(player, FPSSpawnZone.Front);
    }

    public static bool IsBackSpawnAllowed(int currentWave)
    {
        return currentWave > 3;
    }

    private static float GetRandomAngleForZone(FPSSpawnZone zone)
    {
        switch (zone)
        {
            case FPSSpawnZone.Front:
                return Random.Range(-60f, 60f);
            case FPSSpawnZone.Side:
                return Random.value < 0.5f
                    ? Random.Range(-120f, -60f)
                    : Random.Range(60f, 120f);
            default:
                return Random.value < 0.5f
                    ? Random.Range(-180f, -120f)
                    : Random.Range(120f, 180f);
        }
    }
}
