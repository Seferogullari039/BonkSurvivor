using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public readonly struct RunStatsSnapshot
{
    public RunStatsSnapshot(
        float timeSurvived,
        int waveReached,
        int levelReached,
        int enemiesKilled,
        int elitesKilled,
        int bossesKilled,
        int chestsOpened,
        int coinsEarned,
        int metaCoinsAdded,
        int xpCollected,
        int relicsCollected,
        int evolutionsUnlocked,
        float damageDealt,
        float damageTaken,
        IReadOnlyList<string> evolutionNames,
        IReadOnlyList<KeyValuePair<string, float>> topDamageSources,
        string buildSummary,
        string chestBuffSummary)
    {
        TimeSurvived = timeSurvived;
        WaveReached = waveReached;
        LevelReached = levelReached;
        EnemiesKilled = enemiesKilled;
        ElitesKilled = elitesKilled;
        BossesKilled = bossesKilled;
        ChestsOpened = chestsOpened;
        CoinsEarned = coinsEarned;
        MetaCoinsAdded = metaCoinsAdded;
        XpCollected = xpCollected;
        RelicsCollected = relicsCollected;
        EvolutionsUnlocked = evolutionsUnlocked;
        DamageDealt = damageDealt;
        DamageTaken = damageTaken;
        EvolutionNames = evolutionNames;
        TopDamageSources = topDamageSources;
        BuildSummary = buildSummary;
        ChestBuffSummary = chestBuffSummary;
    }

    public float TimeSurvived { get; }
    public int WaveReached { get; }
    public int LevelReached { get; }
    public int EnemiesKilled { get; }
    public int ElitesKilled { get; }
    public int BossesKilled { get; }
    public int ChestsOpened { get; }
    public int CoinsEarned { get; }
    public int MetaCoinsAdded { get; }
    public int XpCollected { get; }
    public int RelicsCollected { get; }
    public int EvolutionsUnlocked { get; }
    public float DamageDealt { get; }
    public float DamageTaken { get; }
    public IReadOnlyList<string> EvolutionNames { get; }
    public IReadOnlyList<KeyValuePair<string, float>> TopDamageSources { get; }
    public string BuildSummary { get; }
    public string ChestBuffSummary { get; }

    public string BuildSummaryText()
    {
        return RunStatsSummaryFormatter.Format(this);
    }
}

public static class RunStatsSummaryFormatter
{
    public static string Format(RunStatsSnapshot snapshot)
    {
        return FormatLeftColumn(snapshot) + "\n\n" + FormatRightColumn(snapshot);
    }

    public static string FormatLeftColumn(RunStatsSnapshot snapshot)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("RUN");
        builder.AppendLine("Time: " + FormatTime(snapshot.TimeSurvived));
        builder.AppendLine("Wave: " + snapshot.WaveReached);
        builder.AppendLine("Level: " + snapshot.LevelReached);
        builder.AppendLine();
        builder.AppendLine("KILLS");
        builder.AppendLine("Enemies: " + snapshot.EnemiesKilled);
        builder.AppendLine("Elites: " + snapshot.ElitesKilled);
        builder.AppendLine("Bosses: " + snapshot.BossesKilled);
        builder.AppendLine();
        builder.AppendLine("LOOT");
        builder.AppendLine("Coins: " + FormatNumber(snapshot.CoinsEarned));
        builder.AppendLine("Added to Total Coins: +" + FormatNumber(GetMetaCoinsAddedForSummary(snapshot)));
        builder.AppendLine("XP: " + FormatNumber(snapshot.XpCollected));
        builder.AppendLine("Chests: " + snapshot.ChestsOpened);
        builder.AppendLine("Relics: " + snapshot.RelicsCollected);
        builder.AppendLine("Evolutions: " + FormatEvolutionList(snapshot.EvolutionNames));
        return builder.ToString().TrimEnd();
    }

    public static string FormatRightColumn(RunStatsSnapshot snapshot)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("COMBAT");
        builder.AppendLine("Damage Dealt: " + FormatNumber(Mathf.RoundToInt(snapshot.DamageDealt)));
        builder.AppendLine("Damage Taken: " + FormatNumber(Mathf.RoundToInt(snapshot.DamageTaken)));

        if (snapshot.TopDamageSources != null && snapshot.TopDamageSources.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("TOP DAMAGE");

            for (int i = 0; i < snapshot.TopDamageSources.Count; i++)
            {
                KeyValuePair<string, float> entry = snapshot.TopDamageSources[i];
                builder.AppendLine((i + 1) + ". " + entry.Key + " — " + FormatNumber(Mathf.RoundToInt(entry.Value)));
            }
        }

        if (!string.IsNullOrEmpty(snapshot.BuildSummary))
        {
            builder.AppendLine();
            builder.AppendLine("BUILD");
            builder.AppendLine(snapshot.BuildSummary);
        }

        if (!string.IsNullOrEmpty(snapshot.ChestBuffSummary))
        {
            builder.AppendLine();
            builder.AppendLine("CHEST BUFFS");
            builder.AppendLine(snapshot.ChestBuffSummary);
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatEvolutionList(IReadOnlyList<string> evolutionNames)
    {
        if (evolutionNames == null || evolutionNames.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", evolutionNames);
    }

    public static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
    }

    public static string FormatNumber(int value)
    {
        return value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int GetMetaCoinsAddedForSummary(RunStatsSnapshot snapshot)
    {
        if (snapshot.MetaCoinsAdded >= 0)
        {
            return snapshot.MetaCoinsAdded;
        }

        return snapshot.CoinsEarned;
    }
}

public class RunStatsTracker : MonoBehaviour
{
    public static RunStatsTracker Instance { get; private set; }

    private const int TopDamageSourceCount = 3;

    private float runStartTime;
    private float cachedTimeSurvived;
    private bool runActive;

    private int enemiesKilled;
    private int elitesKilled;
    private int bossesKilled;
    private int chestsOpened;
    private int coinsEarned;
    private int metaCoinsAdded = -1;
    private int xpCollected;
    private int levelReached = 1;
    private int waveReached = 1;
    private int relicsCollected;
    private float damageDealt;
    private float damageTaken;

    private readonly Dictionary<string, float> damageBySource = new Dictionary<string, float>();
    private readonly HashSet<BuildEvolutionId> unlockedEvolutions = new HashSet<BuildEvolutionId>();
    private readonly List<string> evolutionNameCache = new List<string>();
    private readonly List<KeyValuePair<string, float>> topDamageCache = new List<KeyValuePair<string, float>>();

    public static RunStatsTracker GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        RunStatsTracker existing = FindFirstObjectByType<RunStatsTracker>();

        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject trackerObject = new GameObject("RunStatsTracker");
        return trackerObject.AddComponent<RunStatsTracker>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        UnsubscribeEvolutions();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void StartRun()
    {
        ClearRun();
        runActive = true;
        runStartTime = Time.unscaledTime;
        cachedTimeSurvived = 0f;
        levelReached = 1;
        waveReached = 1;
        SubscribeEvolutions();
    }

    public void EndRun()
    {
        if (runActive && runStartTime > 0f)
        {
            cachedTimeSurvived = Mathf.Max(0f, Time.unscaledTime - runStartTime);
        }

        runActive = false;
        UnsubscribeEvolutions();
    }

    public void RecordEnemyKill(bool isElite, bool isBoss)
    {
        if (!runActive)
        {
            return;
        }

        enemiesKilled++;

        if (isBoss)
        {
            bossesKilled++;
        }
        else if (isElite)
        {
            elitesKilled++;
        }
    }

    public void RecordChestOpened()
    {
        if (!runActive)
        {
            return;
        }

        chestsOpened++;
    }

    public void RecordCoinsEarned(int amount)
    {
        if (!runActive || amount <= 0)
        {
            return;
        }

        coinsEarned += amount;
    }

    public void RecordMetaCoinsAdded(int amount)
    {
        metaCoinsAdded = Mathf.Max(0, amount);
    }

    public void RecordXpCollected(int amount)
    {
        if (!runActive || amount <= 0)
        {
            return;
        }

        xpCollected += amount;
    }

    public void RecordLevelReached(int level)
    {
        if (!runActive)
        {
            return;
        }

        levelReached = Mathf.Max(levelReached, level);
    }

    public void RecordWaveReached(int wave)
    {
        if (!runActive)
        {
            return;
        }

        waveReached = Mathf.Max(waveReached, Mathf.Max(1, wave));
    }

    public void RecordRelicCollected(string relicName)
    {
        if (!runActive || string.IsNullOrEmpty(relicName))
        {
            return;
        }

        relicsCollected++;
    }

    public void RecordEvolutionUnlocked(BuildEvolutionId id)
    {
        if (!runActive || id == BuildEvolutionId.None)
        {
            return;
        }

        unlockedEvolutions.Add(id);
    }

    public void RecordDamageDealt(string source, float amount)
    {
        if (!runActive || amount <= 0f || string.IsNullOrEmpty(source))
        {
            return;
        }

        damageDealt += amount;

        if (damageBySource.TryGetValue(source, out float existing))
        {
            damageBySource[source] = existing + amount;
        }
        else
        {
            damageBySource[source] = amount;
        }
    }

    public void RecordDamageTaken(float amount)
    {
        if (!runActive || amount <= 0f)
        {
            return;
        }

        damageTaken += amount;
    }

    public RunStatsSnapshot CreateSnapshot()
    {
        float timeSurvived = cachedTimeSurvived;

        if (timeSurvived <= 0f && runStartTime > 0f)
        {
            timeSurvived = Mathf.Max(0f, Time.unscaledTime - runStartTime);
        }

        BuildEvolutionNameCache();
        BuildTopDamageCache();

        return new RunStatsSnapshot(
            timeSurvived,
            waveReached,
            levelReached,
            enemiesKilled,
            elitesKilled,
            bossesKilled,
            chestsOpened,
            coinsEarned,
            metaCoinsAdded,
            xpCollected,
            relicsCollected,
            unlockedEvolutions.Count,
            damageDealt,
            damageTaken,
            evolutionNameCache,
            topDamageCache,
            BuildBuildSummary(),
            BuildChestBuffSummary());
    }

    public void ClearRun()
    {
        runActive = false;
        runStartTime = 0f;
        cachedTimeSurvived = 0f;
        enemiesKilled = 0;
        elitesKilled = 0;
        bossesKilled = 0;
        chestsOpened = 0;
        coinsEarned = 0;
        metaCoinsAdded = -1;
        xpCollected = 0;
        levelReached = 1;
        waveReached = 1;
        relicsCollected = 0;
        damageDealt = 0f;
        damageTaken = 0f;
        damageBySource.Clear();
        unlockedEvolutions.Clear();
        evolutionNameCache.Clear();
        topDamageCache.Clear();
        UnsubscribeEvolutions();
    }

    private void SubscribeEvolutions()
    {
        RunBuildTracker tracker = RunBuildTracker.GetOrCreate();
        tracker.OnEvolutionUnlocked -= HandleEvolutionUnlocked;
        tracker.OnEvolutionUnlocked += HandleEvolutionUnlocked;
    }

    private void UnsubscribeEvolutions()
    {
        if (RunBuildTracker.Instance != null)
        {
            RunBuildTracker.Instance.OnEvolutionUnlocked -= HandleEvolutionUnlocked;
        }
    }

    private void HandleEvolutionUnlocked(BuildEvolutionId evolutionId)
    {
        RecordEvolutionUnlocked(evolutionId);
    }

    private void BuildEvolutionNameCache()
    {
        evolutionNameCache.Clear();

        foreach (BuildEvolutionId evolutionId in unlockedEvolutions)
        {
            string name = UpgradeOptionCatalog.GetEvolutionDisplayName(evolutionId);

            if (!string.IsNullOrEmpty(name))
            {
                evolutionNameCache.Add(name);
            }
        }

        evolutionNameCache.Sort(StringComparer.Ordinal);
    }

    private void BuildTopDamageCache()
    {
        topDamageCache.Clear();

        foreach (KeyValuePair<string, float> pair in damageBySource)
        {
            topDamageCache.Add(pair);
        }

        topDamageCache.Sort((a, b) => b.Value.CompareTo(a.Value));

        if (topDamageCache.Count > TopDamageSourceCount)
        {
            topDamageCache.RemoveRange(TopDamageSourceCount, topDamageCache.Count - TopDamageSourceCount);
        }
    }

    private static string BuildBuildSummary()
    {
        RunBuildTracker tracker = RunBuildTracker.Instance;

        if (tracker == null)
        {
            tracker = RunBuildTracker.GetOrCreate();
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("Skills: ");
        AppendSlotSummary(builder, tracker, RewardCategory.Skill);
        builder.AppendLine();
        builder.Append("Passives: ");
        AppendSlotSummary(builder, tracker, RewardCategory.Passive);
        return builder.ToString().TrimEnd();
    }

    private static void AppendSlotSummary(StringBuilder builder, RunBuildTracker tracker, RewardCategory category)
    {
        bool any = false;

        for (int i = 0; i < RunBuildTracker.MaxSlotsPerCategory; i++)
        {
            RunBuildSlotEntry entry = category == RewardCategory.Skill
                ? tracker.GetSkillSlot(i)
                : tracker.GetPassiveSlot(i);

            if (entry == null)
            {
                continue;
            }

            if (any)
            {
                builder.Append(" | ");
            }

            builder.Append(FormatBuildSlotLine(entry));
            any = true;
        }

        if (!any)
        {
            builder.Append("empty");
        }
    }

    private static string FormatBuildSlotLine(RunBuildSlotEntry entry)
    {
        int maxLevel = UpgradeOptionCatalog.GetMaxLevel(entry.UpgradeIndex);
        bool flameOrbitEvolved = entry.UpgradeIndex == 6
            && RunBuildTracker.Instance != null
            && RunBuildTracker.Instance.HasEvolution(BuildEvolutionId.FlameOrbit);
        string displayName = flameOrbitEvolved ? "Flame Orbit" : entry.DisplayName;

        if (entry.Level >= maxLevel)
        {
            return displayName + " MAX";
        }

        return displayName + " Lv." + entry.Level + "/" + maxLevel;
    }

    private static string BuildChestBuffSummary()
    {
        ChestStatBuffTracker tracker = ChestStatBuffTracker.Instance;

        if (tracker == null)
        {
            return string.Empty;
        }

        IReadOnlyList<ChestStatBuffEntry> buffs = tracker.GetActiveBuffs();

        if (buffs == null || buffs.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < buffs.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            builder.Append(ChestStatBuffTracker.FormatPauseSummaryLine(buffs[i]));
        }

        return builder.ToString();
    }
}
