using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VoidPortalEventManager : MonoBehaviour
{
    public static VoidPortalEventManager Instance { get; private set; }

    private const int MinWave = 8;
    private const float TriggerChance = 0.04f;
    private const float WarningDuration = 2.4f;

    private static readonly Color WarningTextColor = new Color(0.78f, 0.38f, 1f, 1f);

    private int lastRollWave;
    private VoidPortalController activePortal;
    private EnemySpawner cachedSpawner;
    private Transform cachedPlayer;
    private Canvas overlayCanvas;
    private TextMeshProUGUI warningText;
    private Coroutine warningRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<VoidPortalEventManager>() != null) return;

        GameObject host = new GameObject("VoidPortalEventManager");
        host.AddComponent<VoidPortalEventManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildOverlay();
        HideWarningImmediate();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!MainMenuManager.IsRunActive)
        {
            if (lastRollWave != 0 || activePortal != null)
            {
                ResetRunState();
            }

            return;
        }

        if (activePortal != null) return;

        TryRollSpawn();
    }

    public void ResetRunState()
    {
        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
            warningRoutine = null;
        }

        lastRollWave = 0;
        activePortal = null;
        cachedSpawner = null;
        cachedPlayer = null;
        VoidPortalSpawnTracker.ResetRun();
        HideWarningImmediate();
    }

    public void NotifyPortalClosed(VoidPortalController portal)
    {
        if (activePortal == portal)
        {
            activePortal = null;
        }
    }

    public bool DevTriggerVoidPortal()
    {
        if (activePortal != null || !VoidPortalSpawnTracker.CanSpawn) return false;
        if (!MainMenuManager.IsRunActive) return false;

        return SpawnPortalAtPlayer();
    }

    private void TryRollSpawn()
    {
        if (!CanRollPortalEvent()) return;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return;

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave) return;
        if (lastRollWave == currentWave) return;
        if (IsBlockedWave(currentWave)) return;

        lastRollWave = currentWave;

        if (Random.value > TriggerChance) return;

        TrySpawnPortal(currentWave);
    }

    private static bool CanRollPortalEvent()
    {
        if (!VoidPortalSpawnTracker.CanSpawn) return false;
        if (!GoldenDragonSpawnTracker.CanSpawn) return false;

        if (BloodMoonEventManager.Instance != null && BloodMoonEventManager.Instance.IsActive)
        {
            return false;
        }

        return true;
    }

    private static bool IsBlockedWave(int wave)
    {
        if (wave % 5 == 0) return true;

        return DragonBossSpawnTracker.IsDragonWave(wave);
    }

    private bool TrySpawnPortal(int wave)
    {
        if (!CanRollPortalEvent()) return false;

        return SpawnPortalAtPlayer();
    }

    private bool SpawnPortalAtPlayer()
    {
        if (activePortal != null || !VoidPortalSpawnTracker.CanSpawn) return false;

        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                cachedPlayer = playerObject.transform;
            }
        }

        if (cachedPlayer == null) return false;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return false;

        Vector3 spawnPosition = GetSpawnPosition(cachedPlayer.position);

        GameObject portalObject = new GameObject("VoidPortal");
        portalObject.transform.position = spawnPosition;

        VoidPortalController controller = portalObject.AddComponent<VoidPortalController>();
        controller.Initialize(this, cachedSpawner);

        VoidPortalSpawnTracker.RegisterActive();
        activePortal = controller;
        ShowWarning();
        return true;
    }

    private static Vector3 GetSpawnPosition(Vector3 playerPosition)
    {
        const float minDistance = 20f;
        const float maxDistance = 30f;

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
        Vector3 spawnPosition = new Vector3(
            playerPosition.x + randomCircle.x,
            ProceduralGrassArena.GetLootSpawnY(0.5f),
            playerPosition.z + randomCircle.y);

        spawnPosition.y = ProceduralGrassArena.GetLootSpawnY(0.5f);
        ProceduralGrassArena.TryClampHorizontal(ref spawnPosition, 4f);
        ProceduralGrassArena.TryResolveBlockedSpawn(ref spawnPosition, minDistance, 2f);
        return spawnPosition;
    }

    private void ShowWarning()
    {
        if (warningText == null) return;

        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
        }

        warningRoutine = StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        if (warningText == null) yield break;

        warningText.gameObject.SetActive(true);
        warningText.text = "VOID PORTAL OPENS";
        Color color = WarningTextColor;
        color.a = 0f;
        warningText.color = color;

        const float fadeInDuration = 0.25f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeInDuration);
            warningText.color = color;
            yield return null;
        }

        color.a = 1f;
        warningText.color = color;

        float holdElapsed = 0f;
        const float fadeOutDuration = 0.45f;
        float holdDuration = WarningDuration - fadeInDuration - fadeOutDuration;

        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            color.a = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
            warningText.color = color;
            yield return null;
        }

        warningText.gameObject.SetActive(false);
        warningRoutine = null;
    }

    private void HideWarningImmediate()
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("VoidPortalOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 34;
        overlayCanvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject warningObject = new GameObject("VoidPortalWarning");
        warningObject.transform.SetParent(canvasObject.transform, false);
        warningText = warningObject.AddComponent<TextMeshProUGUI>();
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = 50f;
        warningText.fontStyle = FontStyles.Bold;
        warningText.color = WarningTextColor;
        warningText.raycastTarget = false;
        UiLayoutUtility.SetAnchorCenter(warningText.rectTransform, new Vector2(0f, 220f), new Vector2(920f, 72f));
    }
}
