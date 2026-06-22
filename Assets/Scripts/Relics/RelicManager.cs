using System.Collections.Generic;
using UnityEngine;

public enum RelicType
{
    SharpFang,
    SwiftBoots,
    GoldenCharm,
    MagnetStone,
    VitalCore,
    HunterMark,
    QuickHands
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
    private const float MagnetStonePickupBonus = 0.25f;
    private const int VitalCoreMaxHealthBonus = 20;
    private const float HunterMarkEliteDamageBonus = 0.15f;
    private const float QuickHandsCooldownReduction = 0.08f;

    public static RelicManager Instance { get; private set; }

    private static readonly RelicType[] AllRelics =
    {
        RelicType.SharpFang,
        RelicType.SwiftBoots,
        RelicType.GoldenCharm,
        RelicType.MagnetStone,
        RelicType.VitalCore,
        RelicType.HunterMark,
        RelicType.QuickHands
    };

    private readonly HashSet<RelicType> ownedRelics = new HashSet<RelicType>();

    public static float DamageMultiplier => Instance != null ? Instance.GetDamageMultiplier() : 1f;
    public static float MoveSpeedMultiplier => Instance != null ? Instance.GetMoveSpeedMultiplier() : 1f;
    public static float CoinGainMultiplier => Instance != null ? Instance.GetCoinGainMultiplier() : 1f;
    public static float PickupRangeMultiplier => Instance != null ? Instance.GetPickupRangeMultiplier() : 1f;
    public static int MaxHealthBonus => Instance != null ? Instance.GetMaxHealthBonus() : 0;
    public static float EliteDamageMultiplier => Instance != null ? Instance.GetEliteDamageMultiplier() : 1f;
    public static float CooldownMultiplier => Instance != null ? Instance.GetCooldownMultiplier() : 1f;

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

    public List<RelicType> GetUnownedRelics()
    {
        List<RelicType> unowned = new List<RelicType>();

        for (int i = 0; i < AllRelics.Length; i++)
        {
            if (!ownedRelics.Contains(AllRelics[i]))
            {
                unowned.Add(AllRelics[i]);
            }
        }

        return unowned;
    }

    public bool TryGrantRandomRelic(out RelicType grantedRelic)
    {
        grantedRelic = default;

        List<RelicType> unowned = GetUnownedRelics();

        if (unowned.Count == 0)
        {
            Debug.Log("[RelicManager] No unowned relics available.");
            return false;
        }

        grantedRelic = unowned[Random.Range(0, unowned.Count)];
        ownedRelics.Add(grantedRelic);
        Debug.Log("[RelicManager] Granted random relic: " + GetDisplayName(grantedRelic) + " | " + BuildMultiplierSummary());
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
            + " | Coin x" + GetCoinGainMultiplier().ToString("0.00")
            + " | Pickup x" + GetPickupRangeMultiplier().ToString("0.00")
            + " | HP +" + GetMaxHealthBonus()
            + " | EliteDmg x" + GetEliteDamageMultiplier().ToString("0.00")
            + " | Cooldown x" + GetCooldownMultiplier().ToString("0.00");
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

    public float GetPickupRangeMultiplier()
    {
        return ownedRelics.Contains(RelicType.MagnetStone) ? 1f + MagnetStonePickupBonus : 1f;
    }

    public int GetMaxHealthBonus()
    {
        return ownedRelics.Contains(RelicType.VitalCore) ? VitalCoreMaxHealthBonus : 0;
    }

    public float GetEliteDamageMultiplier()
    {
        return ownedRelics.Contains(RelicType.HunterMark) ? 1f + HunterMarkEliteDamageBonus : 1f;
    }

    public float GetCooldownMultiplier()
    {
        return ownedRelics.Contains(RelicType.QuickHands) ? 1f - QuickHandsCooldownReduction : 1f;
    }

    public static string GetDisplayName(RelicType relic)
    {
        return GetRelicName(relic);
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
            case RelicType.MagnetStone:
                return "Magnet Stone";
            case RelicType.VitalCore:
                return "Vital Core";
            case RelicType.HunterMark:
                return "Hunter Mark";
            case RelicType.QuickHands:
                return "Quick Hands";
            default:
                return relic.ToString();
        }
    }
}
