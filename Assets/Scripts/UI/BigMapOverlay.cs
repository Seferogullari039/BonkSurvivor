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
    private const float DefaultWorldHalfSize = 80f;

    private static readonly Color DimOverlayColor = new Color(0.02f, 0.03f, 0.05f, 0.55f);
    private static readonly Color MapPanelColor = new Color(0.05f, 0.07f, 0.1f, 0.82f);
    private static readonly Color MapBorderColor = new Color(0.38f, 0.48f, 0.58f, 0.75f);
    private static readonly Color PlayerMarkerColor = new Color(0.35f, 0.88f, 1f, 1f);
    private static readonly Color EnemyMarkerColor = new Color(1f, 0.28f, 0.28f, 0.95f);
    private static readonly Color ChestMarkerColor = new Color(0.92f, 0.72f, 0.28f, 0.95f);
    private static readonly Color SlopeMarkerColor = new Color(0.45f, 0.78f, 0.42f, 0.7f);

    private static BigMapOverlay instance;

    private GameObject overlayRoot;
    private RectTransform markerContainer;
    private RectTransform playerMarkerRect;
    private Transform playerTransform;
    private readonly List<Image> enemyMarkerPool = new List<Image>();
    private readonly List<Image> chestMarkerPool = new List<Image>();
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
        if (FindFirstObjectByType<BigMapOverlay>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject host = new GameObject("BigMapOverlay");
        host.AddComponent<BigMapOverlay>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildOverlayUi();
        HideOverlay();
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
        if (!isBuilt || overlayRoot == null)
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
        overlayRoot.SetActive(true);
        overlayRoot.transform.SetAsLastSibling();
        markerUpdateTimer = 0f;
        RefreshMarkers();
    }

    private void HideOverlay()
    {
        if (overlayRoot != null)
        {
            overlayRoot.SetActive(false);
        }
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

            if (chest == null || !chest.gameObject.activeInHierarchy)
            {
                continue;
            }

            Image marker = GetOrCreateMarker(chestMarkerPool, markerIndex, ChestMarkerColor, 8f, false);
            PlaceWorldMarker(chest.transform.position, marker.rectTransform);
            marker.gameObject.SetActive(true);
            markerIndex++;
        }

        HideUnusedMarkers(chestMarkerPool, markerIndex);
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
