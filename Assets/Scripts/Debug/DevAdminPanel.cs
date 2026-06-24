using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DevAdminPanel : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private static DevAdminPanel instance;

    private const float PanelWidth = 380f;
    private const float PanelHeight = 600f;
    private const float ButtonHeight = 50f;
    private const float ButtonSpacing = 10f;
    private const int TitleFontSize = 26;
    private const int SubtitleFontSize = 18;
    private const int ButtonFontSize = 22;

    private static readonly Color PanelBackgroundColor = new Color(0.06f, 0.06f, 0.08f, 0.94f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    private static readonly Color GodModeActiveColor = new Color(0.18f, 0.58f, 0.28f, 1f);

    private GameObject panelRoot;
    private TextMeshProUGUI godModeButtonLabel;
    private Image godModeButtonImage;
    private bool panelVisible;
    private bool godModeEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!IsDevPanelEnabled()) return;
        if (FindFirstObjectByType<DevAdminPanel>() != null) return;

        GameObject host = new GameObject("DevAdminPanel");
        host.AddComponent<DevAdminPanel>();
    }

    private static bool IsDevPanelEnabled()
    {
#if UNITY_EDITOR
        return true;
#else
        return Debug.isDebugBuild;
#endif
    }

    private void Awake()
    {
        if (!IsDevPanelEnabled())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        instance = this;
        BuildPanel();
        SetPanelVisible(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            IsOpen = false;
        }
    }

    private void Update()
    {
        if (!IsDevPanelEnabled()) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            SetPanelVisible(!panelVisible);
        }
    }

    private void SetPanelVisible(bool visible)
    {
        panelVisible = visible;
        IsOpen = visible;

        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }

        if (visible)
        {
            EnsureEventSystem();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool lockCursor = FPSPlayerController.IsFpsModeActive
            && MainMenuManager.IsRunActive
            && Time.timeScale > 0f;

        Cursor.visible = false;
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void BuildPanel()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("DevAdminCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("DevPanelRoot");
        panelRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = new Vector2(24f, -72f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        Image panelBackground = panelRoot.AddComponent<Image>();
        panelBackground.color = PanelBackgroundColor;
        panelBackground.raycastTarget = true;

        VerticalLayoutGroup panelLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(16, 16, 16, 16);
        panelLayout.spacing = 12f;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        CreateHeaderLabel(panelRoot.transform, "DEV PANEL", TitleFontSize, FontStyles.Bold);
        CreateHeaderLabel(panelRoot.transform, "FPS Survivor Debug Tools", SubtitleFontSize, FontStyles.Italic);

        Transform scrollContent = CreateScrollArea(panelRoot.transform);

        CreateButton(scrollContent, "God Mode", ToggleGodMode, out godModeButtonLabel, out godModeButtonImage);
        UpdateGodModeVisuals();
        CreateButton(scrollContent, "Add 1000 Coins", AddCoins);
        CreateButton(scrollContent, "Level Up", ForceLevelUp);
        CreateButton(scrollContent, "Next Wave", AdvanceWave);
        CreateButton(scrollContent, "Spawn Boss", SpawnBoss);
        CreateHeaderLabel(scrollContent, "Event Tests", SubtitleFontSize, FontStyles.Bold);
        CreateButton(scrollContent, "Spawn Elite Enemy", SpawnEliteEnemy);
        CreateButton(scrollContent, "Spawn Mimic Chest", SpawnMimicChest);
        CreateButton(scrollContent, "Spawn Golden Dragon", SpawnGoldenDragon);
        CreateButton(scrollContent, "Trigger Blood Moon", TriggerBloodMoon);
        CreateButton(scrollContent, "Trigger Void Portal", TriggerVoidPortal);
        CreateButton(scrollContent, "Spawn Shrine Event", SpawnShrine);
        CreateButton(scrollContent, "Scan Large Visuals (F8)", ScanLargeVisuals);
        CreateHeaderLabel(scrollContent, "Relics", SubtitleFontSize, FontStyles.Bold);
        CreateButton(scrollContent, "Add Relic: Sharp Fang", AddRelicSharpFang);
        CreateButton(scrollContent, "Add Relic: Swift Boots", AddRelicSwiftBoots);
        CreateButton(scrollContent, "Add Relic: Golden Charm", AddRelicGoldenCharm);
        CreateButton(scrollContent, "Add Relic: Magnet Stone", AddRelicMagnetStone);
        CreateButton(scrollContent, "Add Relic: Vital Core", AddRelicVitalCore);
        CreateButton(scrollContent, "Add Relic: Hunter Mark", AddRelicHunterMark);
        CreateButton(scrollContent, "Add Relic: Quick Hands", AddRelicQuickHands);
        CreateButton(scrollContent, "Clear Relics", ClearRelics);
        CreateHeaderLabel(scrollContent, "Weapons", SubtitleFontSize, FontStyles.Bold);
        CreateButton(scrollContent, "Give Rocket Launcher (Legacy)", GiveRocketLauncher);
        CreateButton(scrollContent, "Give Laser Beam (Legacy)", GiveLaserBeam);
        CreateButton(scrollContent, "Give Chain Lightning", GiveChainLightning);
        CreateButton(scrollContent, "Give Frost Sigil", GiveFrostSigil);
        CreateButton(scrollContent, "Give Cryo Core", GiveCryoCore);
        CreateButton(scrollContent, "Give Shadow Rift", GiveShadowRift);
        CreateButton(scrollContent, "Give Void Catalyst", GiveVoidCatalyst);
        CreateButton(scrollContent, "Unlock All Weapons", UnlockAllWeapons);
        CreateButton(scrollContent, "Heal Full", HealFull);
        CreateButton(scrollContent, "Kill All Enemies", KillAllEnemies);
    }

    private static Transform CreateScrollArea(Transform parent)
    {
        GameObject scrollObject = new GameObject("ScrollView");
        scrollObject.transform.SetParent(parent, false);

        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 420f;

        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);

        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportMask = viewportObject.AddComponent<Image>();
        viewportMask.color = new Color(0f, 0f, 0f, 0.01f);
        viewportMask.raycastTarget = true;
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);

        RectTransform contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 4, 8);
        contentLayout.spacing = ButtonSpacing;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        return contentObject.transform;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("DevAdminEventSystem");
            eventSystemObject.transform.SetParent(instance != null ? instance.transform : null);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        if (eventSystem.GetComponent<StandaloneInputModule>() == null
            && eventSystem.GetComponent<BaseInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    private static void CreateHeaderLabel(Transform parent, string text, int fontSize, FontStyles fontStyle)
    {
        GameObject labelObject = new GameObject("HeaderLabel");
        labelObject.transform.SetParent(parent, false);

        LayoutElement layout = labelObject.AddComponent<LayoutElement>();
        layout.minHeight = fontSize + 12f;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        ApplyTmpDefaults(label);
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void CreateButton(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction action,
        out TextMeshProUGUI labelText,
        out Image buttonImage)
    {
        labelText = null;
        buttonImage = null;
        CreateButtonInternal(parent, label, action, out labelText, out buttonImage);
    }

    private static void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        CreateButtonInternal(parent, label, action, out _, out _);
    }

    private static void CreateButtonInternal(
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction action,
        out TextMeshProUGUI labelText,
        out Image buttonImage)
    {
        GameObject buttonObject = new GameObject(label.Replace(" ", string.Empty) + "Button");
        buttonObject.transform.SetParent(parent, false);

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = ButtonHeight;
        layout.preferredHeight = ButtonHeight;

        buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = ButtonColor;
        buttonImage.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 0f);
        textRect.offsetMax = new Vector2(-8f, 0f);

        labelText = textObject.AddComponent<TextMeshProUGUI>();
        ApplyTmpDefaults(labelText);
        labelText.text = label;
        labelText.fontSize = ButtonFontSize;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
    }

    private static void ApplyTmpDefaults(TextMeshProUGUI text)
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        if (font != null)
        {
            text.font = font;
        }

        text.raycastTarget = false;
    }

    private PlayerStats GetPlayerStats()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return null;

        return player.GetComponent<PlayerStats>();
    }

    private EnemySpawner GetEnemySpawner()
    {
        return FindFirstObjectByType<EnemySpawner>();
    }

    private void ToggleGodMode()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        godModeEnabled = !godModeEnabled;
        playerStats.SetGodMode(godModeEnabled);
        UpdateGodModeVisuals();
    }

    private void UpdateGodModeVisuals()
    {
        if (godModeButtonLabel != null)
        {
            godModeButtonLabel.text = godModeEnabled ? "God Mode: ON" : "God Mode: OFF";
        }

        if (godModeButtonImage != null)
        {
            godModeButtonImage.color = godModeEnabled ? GodModeActiveColor : ButtonColor;
        }
    }

    private void AddCoins()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.AddCoins(1000);
    }

    private void ForceLevelUp()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.DevForceLevelUp();
    }

    private void AdvanceWave()
    {
        EnemySpawner spawner = GetEnemySpawner();

        if (spawner == null) return;

        spawner.DevAdvanceWave();
    }

    private void SpawnBoss()
    {
        EnemySpawner spawner = GetEnemySpawner();

        if (spawner == null) return;

        spawner.DevSpawnBoss();
    }

    private void SpawnGoldenDragon()
    {
        if (!panelVisible || !IsDevPanelEnabled()) return;

        GoldenDragonEventManager manager = GoldenDragonEventManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<GoldenDragonEventManager>();
        }

        manager?.DevSpawnGoldenDragon();
    }

    private void SpawnEliteEnemy()
    {
        if (!panelVisible || !IsDevPanelEnabled()) return;

        EnemySpawner spawner = GetEnemySpawner();

        if (spawner == null) return;

        spawner.DevSpawnElite();
    }

    private void TriggerBloodMoon()
    {
        if (!panelVisible || !IsDevPanelEnabled()) return;

        BloodMoonEventManager manager = BloodMoonEventManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<BloodMoonEventManager>();
        }

        manager?.DevTriggerBloodMoon();
    }

    private void TriggerVoidPortal()
    {
        if (!panelVisible || !IsDevPanelEnabled()) return;

        VoidPortalEventManager manager = VoidPortalEventManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<VoidPortalEventManager>();
        }

        manager?.DevTriggerVoidPortal();
    }

    private void AddRelicSharpFang()
    {
        AddRelic(RelicType.SharpFang);
    }

    private void AddRelicSwiftBoots()
    {
        AddRelic(RelicType.SwiftBoots);
    }

    private void AddRelicGoldenCharm()
    {
        AddRelic(RelicType.GoldenCharm);
    }

    private void AddRelicMagnetStone()
    {
        AddRelic(RelicType.MagnetStone);
    }

    private void AddRelicVitalCore()
    {
        AddRelic(RelicType.VitalCore);
    }

    private void AddRelicHunterMark()
    {
        AddRelic(RelicType.HunterMark);
    }

    private void AddRelicQuickHands()
    {
        AddRelic(RelicType.QuickHands);
    }

    private void AddRelic(RelicType relic)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DevAdminPanel] Add Relic requires Play Mode.");
            return;
        }

        RelicManager manager = RelicManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<RelicManager>();
        }

        if (manager == null)
        {
            Debug.LogWarning("[DevAdminPanel] RelicManager not found.");
            return;
        }

        manager.AddRelic(relic);
        LogRelicState(manager);
    }

    private void ClearRelics()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DevAdminPanel] Clear Relics requires Play Mode.");
            return;
        }

        RelicManager manager = RelicManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<RelicManager>();
        }

        if (manager == null)
        {
            Debug.LogWarning("[DevAdminPanel] RelicManager not found.");
            return;
        }

        manager.ClearRelics();
        LogRelicState(manager);
    }

    private static void LogRelicState(RelicManager manager)
    {
        if (manager == null)
        {
            return;
        }

        Debug.Log("[DevAdminPanel] Relics: SharpFang=" + manager.HasRelic(RelicType.SharpFang)
            + " SwiftBoots=" + manager.HasRelic(RelicType.SwiftBoots)
            + " GoldenCharm=" + manager.HasRelic(RelicType.GoldenCharm)
            + " MagnetStone=" + manager.HasRelic(RelicType.MagnetStone)
            + " VitalCore=" + manager.HasRelic(RelicType.VitalCore)
            + " HunterMark=" + manager.HasRelic(RelicType.HunterMark)
            + " QuickHands=" + manager.HasRelic(RelicType.QuickHands)
            + " | " + manager.BuildMultiplierSummary());
    }

    private void ScanLargeVisuals()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DevAdminPanel] Scan Large Visuals requires Play Mode.");
            return;
        }

        VisualBugInspector.ReportLargeRenderersInScene("[DevAdminPanel] Manual large visual scan");
    }

    private void SpawnShrine()
    {
        if (!panelVisible || !IsDevPanelEnabled()) return;

        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DevAdminPanel] Spawn Shrine requires Play Mode.");
            return;
        }

        Debug.Log("[DevAdminPanel] Spawn Shrine requested.");

        ShrineEventManager manager = ShrineEventManager.Instance;

        if (manager == null)
        {
            manager = FindFirstObjectByType<ShrineEventManager>();
        }

        if (manager == null)
        {
            Debug.LogWarning("[DevAdminPanel] ShrineEventManager not found.");
            return;
        }

        if (!manager.DevSpawnShrine())
        {
            Debug.Log("[DevAdminPanel] Shrine spawn skipped (active shrine or invalid state).");
        }
    }

    private void SpawnMimicChest()
    {
        if (!panelVisible || !IsDevPanelEnabled()) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null) return;

        Vector3 forward = player.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward.Normalize();
        }

        Vector3 spawnPosition = player.transform.position + forward * 4f;
        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, 4f, 1.2f);

        GameObject prefab = ResolveDevChestPrefab();

        if (prefab == null) return;

        GameObject chestObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Chest chest = chestObject.GetComponent<Chest>();

        if (chest == null) return;

        chest.ConfigureMapChest(ChestRarity.Normal, true);
    }

    private static GameObject ResolveDevChestPrefab()
    {
        ChestSpawner chestSpawner = FindFirstObjectByType<ChestSpawner>();

        if (chestSpawner != null)
        {
            GameObject spawnerPrefab = chestSpawner.GetChestPrefabForRarity(ChestRarity.Normal);

            if (spawnerPrefab != null)
            {
                return spawnerPrefab;
            }
        }

        return ChestPrefabUtility.ResolveChestPrefab(ChestRarity.Normal);
    }

    private void GiveRocketLauncher()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeRocketLauncher();
    }

    private void GiveLaserBeam()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeLaserBeam();
    }

    private void GiveChainLightning()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.UpgradeChainLightning();
    }

    private void GiveFrostSigil()
    {
        RunBuildTracker.GetOrCreate().RecordUpgrade(UpgradeOptionCatalog.FrostSigilIndex);
    }

    private void GiveCryoCore()
    {
        RunBuildTracker.GetOrCreate().RecordUpgrade(UpgradeOptionCatalog.CryoCoreIndex);
    }

    private void GiveShadowRift()
    {
        RunBuildTracker.GetOrCreate().RecordUpgrade(UpgradeOptionCatalog.ShadowRiftIndex);
    }

    private void GiveVoidCatalyst()
    {
        RunBuildTracker.GetOrCreate().RecordUpgrade(UpgradeOptionCatalog.VoidCatalystIndex);
    }

    private void UnlockAllWeapons()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.DevUnlockAllWeapons();

        WeaponManager weaponManager = playerStats.GetComponent<WeaponManager>();

        if (weaponManager != null)
        {
            weaponManager.RefreshOrbitWeapon();
        }
    }

    private void HealFull()
    {
        PlayerStats playerStats = GetPlayerStats();

        if (playerStats == null) return;

        playerStats.DevHealFull();
    }

    private void KillAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null) continue;

            Enemy enemy = enemyObject.GetComponent<Enemy>();

            if (enemy == null) continue;

            enemy.TakeDamage(99999);
        }
    }
}
