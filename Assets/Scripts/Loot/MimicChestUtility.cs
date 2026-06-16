using UnityEngine;

public static class MimicChestUtility
{
    private const float BaseChance = 0.05f;
    private const float ChancePerWave = 0.005f;
    private const float MaxChance = 0.10f;
    private const int MinWave = 4;

    public static bool RollMimicForMapChest(int currentWave)
    {
        if (currentWave < MinWave)
        {
            return false;
        }

        float chance = BaseChance + (currentWave - MinWave) * ChancePerWave;
        chance = Mathf.Min(chance, MaxChance);

        return Random.value < chance;
    }
}
