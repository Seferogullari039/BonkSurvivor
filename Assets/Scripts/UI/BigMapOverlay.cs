using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigMapOverlay : MonoBehaviour
{
    private const float MapPanelWidth = 1248f;
    private const float MapPanelHeight = 702f;
    private const float MarkerPadding = 14f;
    private const float MarkerUpdateInterval = 0.1f;
    private const int MaxEnemyMarkers = 80;
    private const float DefaultWorldHalfSize = ProceduralGrassArena.DefaultMapHalfSize;

    private static readonly Color DimOverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.32f);
    private static readonly Color MapPanelColor = new Color(0.09f, 0.22f, 0.11f, 0.44f);
    private static readonly Color MapGrassPatchColor = new Color(0.14f, 0.32f, 0.16f, 0.13f);
    private static readonly Color MapGrassStripeColor = new Color(0.18f, 0.38f, 0.2f, 0.07f);
    private static readonly Color MapGrassDotColor = new Color(0.22f, 0.44f, 0.24f, 0.04f);
    private static readonly Color MapBorderColor = new Color(0.42f, 0.58f, 0.38f, 0.72f);
    private static readonly Color PlayerMarkerColor = new Color(0.35f, 0.88f, 1f, 1f);
    private static readonly Color EnemyMarkerColor = new Color(1f, 0.28f, 0.28f, 0.95f);
    private static readonly Color ChestBodyColor = new Color(0.72f, 0.52f, 0.2f, 1f);
    private static readonly Color ChestLidColor = new Color(0.48f, 0.34f, 0.12f, 1f);
    private static readonly Color ChestBorderColor = new Color(0.22f, 0.15f, 0.06f, 0.95f);
    private static readonly Color ChestLockColor = new Color(0.95f, 0.82f, 0.22f, 1f);
    private static readonly Color SlopeMarkerColor = new Color(0.45f, 0.78f, 0.42f, 0.7f);

    private static BigMapOverlay instance;

    private GameObject overlayRoot;
    private RectTransform markerContainer;
    private RectTransform playerMarkerRect;
    private Transform playerTransform;
    private readonly List<Image> enemyMarkerPool = new List<Image>();
    private readonly List<RectTransform> chestMarkerPool = new List<RectTransform>();
    private readonly List<Image> slopeMarkerPool = new List<Image>();
    private readonly List<Transform> slopeMarkerTargets = new List<Transform>();
    private float markerUpdateTimer;
    private bool isBuilt;
    private bool slopeMarkersCached;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureReadyForRun();
    }

    public static void EnsureReadyForRun()
    {
        BigMapOverlay overlay = ResolveInstance();

        if (overlay == null)
        {
            return;
        }

        if (!overlay.gameObject.activeSelf)
        {
            overlay.gameObject.SetActive(true);
        }

        overlay.EnsureReadyInternal();
    }

    private static BigMapOverlay ResolveInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<BigMapOverlay>(FindObjectsInactive.Include);

        if (instance != null)
        {
            return instance;
        }

        GameObject host = new GameObject("BigMapOverlay");
        instance = host.AddComponent<BigMapOverlay>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        EnsureReadyInternal();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (MainMenuManager.IsRunActive && !IsUiHealthy())
        {
            EnsureReadyInternal();
        }

        if (!IsUiHealthy())
        {
            return;
        }

        bool shouldShow = Input.GetKey(KeyCode.Tab) && CanShowBigMap();

        if (overlayRoot.activeSelf != shouldShow)
        {
            if (shouldShow)
            {
                ShowOverlay();
            }
            else
            {
                HideOverlay();
            }
        }

        if (!shouldShow)
        {
            return;
        }

        markerUpdateTimer -= Time.unscaledDeltaTime;

        if (markerUpdateTimer <= 0f)
        {
            markerUpdateTimer = MarkerUpdateInterval;
            RefreshMarkers();
        }
    }

    private static bool CanShowBigMap()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return false;
        }

        if (Time.timeScale <= 0f)
        {
            return false;
        }

        if (GameOverManager.Instance != null && GameOverManager.Instance.IsGameOverActive)
        {
            return false;
        }

        if (PauseMenuManager.IsGameplayPaused)
        {
            return false;
        }

        if (MerchantShrineUI.IsOpen)
        {
            return false;
        }

        if (ItemOfferHudVisibility.IsGameplaySuppressed)
        {
            return false;
        }

        if (SettingsMenuUI.IsOpen)
        {
            return false;
        }

        if (DevAdminPanel.IsOpen)
        {
            return false;
        }

        LevelUpManager levelUpManager = LevelUpManager.Instance;

        if (levelUpManager != null && levelUpManager.BlocksGameplayPause)
        {
            return false;
        }

        return true;
    }

    private void ShowOverlay()
    {
        ResolvePlayerTransform();
        CacheSlopeMarkersIfNeeded();
        FPSRadar.SetSuppressedByBigMap(true);
        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        markerUpdateTimer = 0f;
        RefreshMarkers();
    }

    private void HideOverlay()
    {
        FPSRadar.SetSuppressedByBigMap(false);

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
    }

    private bool IsUiHealthy()
    {
        return isBuilt && overlayRoot != null && markerContainer != null;
    }

    private void EnsureReadyInternal()
    {
        playerTransform = null;

        if (overlayRoot != null && overlayRoot.transform.parent == null)
        {
            TeardownUi();
        }

        if (!IsUiHealthy())
        {
            TeardownUi();
            BuildOverlayUi();
        }

        HideOverlay();
    }

    private void TeardownUi()
    {
        if (overlayRoot != null)
        {
            Destroy(overlayRoot);
        }

        overlayRoot = null;
        markerContainer = null;
        playerMarkerRect = null;
        enemyMarkerPool.Clear();
        chestMarkerPool.Clear();
        slopeMarkerPool.Clear();
        slopeMarkersCached = false;
        isBuilt = false;
    }

    private void BuildOverlayUi()
    {
        if (isBuilt)
        {
            return;
        }

        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null)
        {
            return;
        }

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        overlayRoot = new GameObject("BigMapOverlayRoot");
        overlayRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = overlayRoot.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(rootRect);

        Image dimImage = overlayRoot.AddComponent<Image>();
        dimImage.color = DimOverlayColor;
        dimImage.raycastTarget = false;

        GameObject mapPanelObject = new GameObject("BigMapPanel");
        mapPanelObject.transform.SetParent(overlayRoot.transform, false);

        RectTransform mapPanelRect = mapPanelObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorCenter(mapPanelRect, 0f, 0f, MapPanelWidth, MapPanelHeight);

        Image mapPanelImage = mapPanelObject.AddComponent<Image>();
        mapPanelImage.color = MapPanelColor;
        mapPanelImage.raycastTarget = false;

        BuildGrassBackground(mapPanelObject.transform);

        Outline mapOutline = mapPanelObject.AddComponent<Outline>();
        mapOutline.effectColor = MapBorderColor;
        mapOutline.effectDistance = new Vector2(2f, -2f);

        CreateLabel(
            mapPanelObject.transform,
            "MapTitle",
            "MAP",
            34f,
            new Vector2(0f, MapPanelHeight * 0.5f - 34f),
            new Vector2(220f, 44f),
            FontStyles.Bold);

        CreateLabel(
            mapPanelObject.transform,
            "MapHint",
            "Hold TAB to view",
            18f,
            new Vector2(0f, -MapPanelHeight * 0.5f + 28f),
            new Vector2(260f, 30f),
            FontStyles.Italic,
            new Color(0.72f, 0.76f, 0.82f, 0.9f));

        GameObject markerContainerObject = new GameObject("MarkerContainer");
        markerContainerObject.transform.SetParent(mapPanelObject.transform, false);
        markerContainer = markerContainerObject.AddComponent<RectTransform>();
        UiLayoutUtility.SetStretch(markerContainer, MarkerPadding, MarkerPadding, 56f, 44f);

        Image playerMarkerImage = CreateMarkerImage(markerContainer, "PlayerMarker", 16f, PlayerMarkerColor, true);
        playerMarkerRect = playerMarkerImage.rectTransform;
        playerMarkerRect.SetAsLastSibling();

        isBuilt = true;
        overlayRoot.SetActive(false);
    }

    private void RefreshMarkers()
    {
        if (markerContainer == null)
        {
            return;
        }

        ResolvePlayerTransform();

        if (playerTransform != null && playerMarkerRect != null)
        {
            PlaceWorldMarker(playerTransform.position, playerMarkerRect);
            playerMarkerRect.localRotation = Quaternion.Euler(0f, 0f, -playerTransform.eulerAngles.y);
            playerMarkerRect.gameObject.SetActive(true);
        }
        else if (playerMarkerRect != null)
        {
            playerMarkerRect.gameObject.SetActive(false);
        }

        RefreshEnemyMarkers();
        RefreshChestMarkers();
        RefreshSlopeMarkers();
    }

    private void RefreshEnemyMarkers()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Vector3 playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;

        if (enemies.Length > 1)
        {
            System.Array.Sort(enemies, (a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;

                float distA = (a.transform.position - playerPosition).sqrMagnitude;
                float distB = (b.transform.position - playerPosition).sqrMagnitude;
                return distA.CompareTo(distB);
            });
        }

        int enemyCount = Mathf.Min(enemies.Length, MaxEnemyMarkers);
        int markerIndex = 0;

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null || !enemyObject.activeInHierarchy)
            {
                continue;
            }

            Image marker = GetOrCreateMarker(enemyMarkerPool, markerIndex, EnemyMarkerColor, 7f, false);
            PlaceWorldMarker(enemyObject.transform.position, marker.rectTransform);
            marker.gameObject.SetActive(true);
            markerIndex++;
        }

        HideUnusedMarkers(enemyMarkerPool, markerIndex);
    }

    private void RefreshChestMarkers()
    {
        Chest[] chests = FindObjectsByType<Chest>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int markerIndex = 0;

        for (int i = 0; i < chests.Length; i++)
        {
            Chest chest = chests[i];

            if (chest == null || !chest.ShouldShowOnBigMap)
            {
                continue;
            }

            RectTransform marker = GetOrCreateChestMarker(markerIndex);
            PlaceWorldMarker(chest.transform.position, marker);
            marker.gameObject.SetActive(true);
            markerIndex++;
        }

        HideUnusedChestMarkers(chestMarkerPool, markerIndex);
    }

    private void RefreshSlopeMarkers()
    {
        CacheSlopeMarkersIfNeeded();
        int markerIndex = 0;

        for (int i = 0; i < slopeMarkerTargets.Count; i++)
        {
            Transform slopeTransform = slopeMarkerTargets[i];

            if (slopeTransform == null)
            {
                continue;
            }

            Image marker = GetOrCreateMarker(slopeMarkerPool, markerIndex, SlopeMarkerColor, 6f, false);
            PlaceWorldMarker(slopeTransform.position, marker.rectTransform);
            marker.gameObject.SetActive(true);
            markerIndex++;
        }

        HideUnusedMarkers(slopeMarkerPool, markerIndex);
    }

    private void CacheSlopeMarkersIfNeeded()
    {
        if (slopeMarkersCached)
        {
            return;
        }

        slopeMarkersCached = true;
        slopeMarkerTargets.Clear();

        GameObject slopeRoot = GameObject.Find("SlopeTestAreas");

        if (slopeRoot == null)
        {
            return;
        }

        for (int i = 0; i < slopeRoot.transform.childCount; i++)
        {
            Transform child = slopeRoot.transform.GetChild(i);

            if (child != null)
            {
                slopeMarkerTargets.Add(child);
            }
        }
    }

    private void PlaceWorldMarker(Vector3 worldPosition, RectTransform markerRect)
    {
        if (markerContainer == null || markerRect == null)
        {
            return;
        }

        ResolveWorldHalfSizes(out float halfX, out float halfZ);

        float normalizedX = Mathf.Clamp01(Mathf.InverseLerp(-halfX, halfX, worldPosition.x));
        float normalizedZ = Mathf.Clamp01(Mathf.InverseLerp(-halfZ, halfZ, worldPosition.z));

        Vector2 containerSize = markerContainer.rect.size;
        float x = (-containerSize.x * 0.5f) + normalizedX * containerSize.x;
        float y = (-containerSize.y * 0.5f) + normalizedZ * containerSize.y;
        markerRect.anchoredPosition = new Vector2(x, y);
    }

    private static void ResolveWorldHalfSizes(out float halfX, out float halfZ)
    {
        if (ProceduralGrassArena.Instance != null)
        {
            halfX = ProceduralGrassArena.Instance.HalfSizeX;
            halfZ = ProceduralGrassArena.Instance.HalfSizeZ;
            return;
        }

        halfX = DefaultWorldHalfSize;
        halfZ = DefaultWorldHalfSize;
    }

    private void ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            return;
        }

        FPSPlayerController fpsController = FindFirstObjectByType<FPSPlayerController>();

        if (fpsController != null)
        {
            playerTransform = fpsController.transform;
        }
    }

    private RectTransform GetOrCreateChestMarker(int index)
    {
        while (chestMarkerPool.Count <= index)
        {
            chestMarkerPool.Add(null);
        }

        if (chestMarkerPool[index] == null)
        {
            chestMarkerPool[index] = CreateChestMapMarker(markerContainer, "ChestMapMarker");
        }

        return chestMarkerPool[index];
    }

    private static void HideUnusedChestMarkers(List<RectTransform> pool, int activeCount)
    {
        for (int i = activeCount; i < pool.Count; i++)
        {
            if (pool[i] != null)
            {
                pool[i].gameObject.SetActive(false);
            }
        }
    }

    private static void BuildGrassBackground(Transform mapPanel)
    {
        GameObject grassRoot = new GameObject("GrassBackground");
        grassRoot.transform.SetParent(mapPanel, false);
        grassRoot.transform.SetAsFirstSibling();

        RectTransform grassRect = grassRoot.AddComponent<RectTransform>();
        UiLayoutUtility.StretchToParent(grassRect);

        CreateGrassPatch(grassRoot.transform, "GrassPatch_A", new Vector2(-280f, 120f), new Vector2(420f, 180f), 18f);
        CreateGrassPatch(grassRoot.transform, "GrassPatch_B", new Vector2(240f, -90f), new Vector2(360f, 160f), -12f);
        CreateGrassPatch(grassRoot.transform, "GrassPatch_C", new Vector2(40f, 40f), new Vector2(520f, 220f), 8f);

        for (int i = 0; i < 5; i++)
        {
            float y = -MapPanelHeight * 0.42f + i * (MapPanelHeight * 0.2f);
            CreateGrassStripe(grassRoot.transform, "GrassStripe_" + i, y);
        }

        CreateGrassDots(grassRoot.transform);
    }

    private static void CreateGrassDots(Transform parent)
    {
        const int columns = 9;
        const int rows = 6;
        float startX = -(columns - 1) * 0.5f * 118f;
        float startY = -(rows - 1) * 0.5f * 96f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if ((row + col) % 2 != 0)
                {
                    continue;
                }

                GameObject dotObject = new GameObject("GrassDot_" + row + "_" + col);
                dotObject.transform.SetParent(parent, false);

                RectTransform rect = dotObject.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(startX + col * 118f, startY + row * 96f);
                rect.sizeDelta = new Vector2(6f, 6f);

                Image image = dotObject.AddComponent<Image>();
                image.color = MapGrassDotColor;
                image.raycastTarget = false;
            }
        }
    }

    private static void CreateGrassPatch(
        Transform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        float rotationZ)
    {
        GameObject patchObject = new GameObject(name);
        patchObject.transform.SetParent(parent, false);

        RectTransform rect = patchObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotationZ);

        Image image = patchObject.AddComponent<Image>();
        image.color = MapGrassPatchColor;
        image.raycastTarget = false;
    }

    private static void CreateGrassStripe(Transform parent, string name, float y)
    {
        GameObject stripeObject = new GameObject(name);
        stripeObject.transform.SetParent(parent, false);

        RectTransform rect = stripeObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(MapPanelWidth - 80f, 10f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -8f);

        Image image = stripeObject.AddComponent<Image>();
        image.color = MapGrassStripeColor;
        image.raycastTarget = false;
    }

    private static RectTransform CreateChestMapMarker(Transform parent, string name)
    {
        GameObject markerObject = new GameObject(name);
        markerObject.transform.SetParent(parent, false);

        RectTransform markerRect = markerObject.AddComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(14f, 12f);

        GameObject borderObject = new GameObject("Border");
        borderObject.transform.SetParent(markerObject.transform, false);
        RectTransform borderRect = borderObject.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-1.5f, -1.5f);
        borderRect.offsetMax = new Vector2(1.5f, 1.5f);
        Image borderImage = borderObject.AddComponent<Image>();
        borderImage.color = ChestBorderColor;
        borderImage.raycastTarget = false;

        GameObject bodyObject = new GameObject("Body");
        bodyObject.transform.SetParent(markerObject.transform, false);
        RectTransform bodyRect = bodyObject.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, -1.5f);
        bodyRect.sizeDelta = new Vector2(11f, 6.5f);
        Image bodyImage = bodyObject.AddComponent<Image>();
        bodyImage.color = ChestBodyColor;
        bodyImage.raycastTarget = false;

        GameObject lidObject = new GameObject("Lid");
        lidObject.transform.SetParent(markerObject.transform, false);
        RectTransform lidRect = lidObject.AddComponent<RectTransform>();
        lidRect.anchorMin = new Vector2(0.5f, 0.5f);
        lidRect.anchorMax = new Vector2(0.5f, 0.5f);
        lidRect.pivot = new Vector2(0.5f, 0.5f);
        lidRect.anchoredPosition = new Vector2(0f, 3.5f);
        lidRect.sizeDelta = new Vector2(11f, 3f);
        Image lidImage = lidObject.AddComponent<Image>();
        lidImage.color = ChestLidColor;
        lidImage.raycastTarget = false;

        GameObject lockObject = new GameObject("Lock");
        lockObject.transform.SetParent(markerObject.transform, false);
        RectTransform lockRect = lockObject.AddComponent<RectTransform>();
        lockRect.anchorMin = new Vector2(0.5f, 0.5f);
        lockRect.anchorMax = new Vector2(0.5f, 0.5f);
        lockRect.pivot = new Vector2(0.5f, 0.5f);
        lockRect.anchoredPosition = new Vector2(0f, -1f);
        lockRect.sizeDelta = new Vector2(2.5f, 2.5f);
        Image lockImage = lockObject.AddComponent<Image>();
        lockImage.color = ChestLockColor;
        lockImage.raycastTarget = false;

        return markerRect;
    }

    private Image GetOrCreateMarker(
        List<Image> pool,
        int index,
        Color color,
        float size,
        bool isPlayer)
    {
        while (pool.Count <= index)
        {
            pool.Add(null);
        }

        if (pool[index] == null)
        {
            pool[index] = CreateMarkerImage(
                markerContainer,
                isPlayer ? "PlayerMarker" : "MapMarker",
                size,
                color,
                isPlayer);
        }

        pool[index].color = color;

        RectTransform rect = pool[index].rectTransform;
        rect.sizeDelta = new Vector2(size, size);

        return pool[index];
    }

    private static void HideUnusedMarkers(List<Image> pool, int activeCount)
    {
        for (int i = activeCount; i < pool.Count; i++)
        {
            if (pool[i] != null)
            {
                pool[i].gameObject.SetActive(false);
            }
        }
    }

    private static Image CreateMarkerImage(
        Transform parent,
        string name,
        float size,
        Color color,
        bool isPlayer)
    {
        GameObject markerObject = new GameObject(name);
        markerObject.transform.SetParent(parent, false);

        RectTransform rect = markerObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        Image image = markerObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (isPlayer)
        {
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        return image;
    }

    private static TMP_Text CreateLabel(
        Transform parent,
        string name,
        string text,
        float fontSize,
        Vector2 anchoredPosition,
        Vector2 size,
        FontStyles fontStyle,
        Color? color = null)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.fontStyle = fontStyle;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color ?? Color.white;
        textMesh.raycastTarget = false;

        return textMesh;
    }
}
