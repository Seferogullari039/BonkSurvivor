using System.Collections.Generic;
using UnityEngine;

public enum RelicType
{
    SharpFang,
    SwiftBoots,
    GoldenCharm
}

/// <summary>
/// Foundation for run-scoped passive relics. First pass: no save/load, no inventory UI,
/// no automatic chest/shrine reward integration. Relics are granted via the dev panel.
/// Bonuses are exposed as safe multiplier getters so callers can opt in without rewrites.
/// </summary>
[DisallowMultipleComponent]
public class RelicManager : MonoBehaviour
{
    private const float SharpFangDamageBonus = 0.10f;
    private const float SwiftBootsMoveSpeedBonus = 0.08f;
    private const float GoldenCharmCoinBonus = 0.15f;

    public static RelicManager Instance { get; private set; }

    private readonly HashSet<RelicType> ownedRelics = new HashSet<RelicType>();

    public static float DamageMultiplier => Instance != null ? Instance.GetDamageMultiplier() : 1f;
    public static float MoveSpeedMultiplier => Instance != null ? Instance.GetMoveSpeedMultiplier() : 1f;
    public static float CoinGainMultiplier => Instance != null ? Instance.GetCoinGainMultiplier() : 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        if (FindFirstObjectByType<RelicManager>() != null) return;

        GameObject host = new GameObject("RelicManager");
        host.AddComponent<RelicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool HasRelic(RelicType relic)
    {
        return ownedRelics.Contains(relic);
    }

    public bool AddRelic(RelicType relic)
    {
        if (ownedRelics.Contains(relic))
        {
            Debug.Log("[RelicManager] Relic already owned: " + GetRelicName(relic));
            return false;
        }

        ownedRelics.Add(relic);
        Debug.Log("[RelicManager] Added relic: " + GetRelicName(relic) + " | " + BuildMultiplierSummary());
        return true;
    }

    public void ClearRelics()
    {
        if (ownedRelics.Count == 0)
        {
            Debug.Log("[RelicManager] No relics to clear.");
            return;
        }

        ownedRelics.Clear();
        Debug.Log("[RelicManager] Cleared all relics | " + BuildMultiplierSummary());
    }

    public string BuildMultiplierSummary()
    {
        return "Damage x" + GetDamageMultiplier().ToString("0.00")
            + " | MoveSpeed x" + GetMoveSpeedMultiplier().ToString("0.00")
            + " | Coin x" + GetCoinGainMultiplier().ToString("0.00");
    }

    public float GetDamageMultiplier()
    {
        return ownedRelics.Contains(RelicType.SharpFang) ? 1f + SharpFangDamageBonus : 1f;
    }

    public float GetMoveSpeedMultiplier()
    {
        return ownedRelics.Contains(RelicType.SwiftBoots) ? 1f + SwiftBootsMoveSpeedBonus : 1f;
    }

    public float GetCoinGainMultiplier()
    {
        return ownedRelics.Contains(RelicType.GoldenCharm) ? 1f + GoldenCharmCoinBonus : 1f;
    }

    public static string GetRelicName(RelicType relic)
    {
        switch (relic)
        {
            case RelicType.SharpFang:
                return "Sharp Fang";
            case RelicType.SwiftBoots:
                return "Swift Boots";
            case RelicType.GoldenCharm:
                return "Golden Charm";
            default:
                return relic.ToString();
        }
    }
}
