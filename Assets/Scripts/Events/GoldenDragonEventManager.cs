using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldenDragonEventManager : MonoBehaviour
{
    public static GoldenDragonEventManager Instance { get; private set; }

    private const int MinWave = 6;
    private const float TriggerChance = 0.03f;
    private const float WarningDuration = 2.8f;

    private static readonly Color WarningTextColor = new Color(1f, 0.82f, 0.18f, 1f);

    private int lastRollWave;
    private GoldenDragonController activeDragon;
    private EnemySpawner cachedSpawner;
    private Transform cachedPlayer;
    private Canvas overlayCanvas;
    private TextMeshProUGUI warningText;
    private Coroutine warningRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<GoldenDragonEventManager>() != null) return;

        GameObject host = new GameObject("GoldenDragonEventManager");
        host.AddComponent<GoldenDragonEventManager>();
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
            if (lastRollWave != 0 || activeDragon != null)
            {
                ResetRunState();
            }

            return;
        }

        if (activeDragon != null) return;

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
        activeDragon = null;
        cachedSpawner = null;
        cachedPlayer = null;
        GoldenDragonSpawnTracker.ResetRun();
        HideWarningImmediate();
    }

    public void NotifyDragonResolved(GoldenDragonController dragon)
    {
        if (activeDragon == dragon)
        {
            activeDragon = null;
        }
    }

    public bool DevSpawnGoldenDragon()
    {
        if (activeDragon != null || !GoldenDragonSpawnTracker.CanSpawn) return false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null) return false;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        int wave = cachedSpawner != null ? Mathf.Max(1, cachedSpawner.CurrentWave) : 6;
        cachedPlayer = playerObject.transform;
        return SpawnGoldenDragonAt(wave, cachedPlayer.position, 25f, 35f);
    }

    private void TryRollSpawn()
    {
        if (!GoldenDragonSpawnTracker.CanSpawn) return;

        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return;

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave) return;
        if (lastRollWave == currentWave) return;
        if (DragonBossSpawnTracker.IsDragonWave(currentWave)) return;
        if (currentWave % 5 == 0) return;

        lastRollWave = currentWave;

        if (Random.value > TriggerChance) return;

        TrySpawnGoldenDragon(currentWave);
    }

    private bool TrySpawnGoldenDragon(int wave)
    {
        if (cachedPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                cachedPlayer = playerObject.transform;
            }
        }

        if (cachedPlayer == null) return false;

        return SpawnGoldenDragonAt(wave, cachedPlayer.position, 28f, 34f);
    }

    private bool SpawnGoldenDragonAt(int wave, Vector3 playerPosition, float minDistance, float maxDistance)
    {
        if (activeDragon != null || !GoldenDragonSpawnTracker.CanSpawn) return false;

        Vector3 spawnPosition = GetSpawnPosition(playerPosition, minDistance, maxDistance);

        GameObject dragonObject = new GameObject("GoldenDragon");
        dragonObject.transform.position = spawnPosition;
        dragonObject.transform.localScale = Vector3.one * 1.65f;

        GoldenDragonController controller = dragonObject.AddComponent<GoldenDragonController>();
        controller.Initialize(this, wave);

        GoldenDragonSpawnTracker.RegisterActive();
        activeDragon = controller;
        ShowWarning();
        return true;
    }

    private static Vector3 GetSpawnPosition(Vector3 playerPosition, float minDistance = 28f, float maxDistance = 34f)
    {
        Vector3 spawnPosition;

        if (ProceduralGrassArena.Instance != null)
        {
            spawnPosition = ProceduralGrassArena.Instance.GetSafePointInsideArena(minDistance, 4f);
        }
        else
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistance, maxDistance);
            spawnPosition = new Vector3(
                playerPosition.x + randomCircle.x,
                ProceduralGrassArena.GetLootSpawnY(0.5f),
                playerPosition.z + randomCircle.y);
        }

        Vector3 awayFromPlayer = spawnPosition - playerPosition;
        awayFromPlayer.y = 0f;

        float minDistanceSqr = minDistance * minDistance;

        if (awayFromPlayer.sqrMagnitude < minDistanceSqr)
        {
            if (awayFromPlayer.sqrMagnitude < 0.001f)
            {
                awayFromPlayer = Random.insideUnitSphere;
                awayFromPlayer.y = 0f;
            }

            awayFromPlayer.Normalize();
            spawnPosition = playerPosition + awayFromPlayer * Random.Range(minDistance, maxDistance);
        }

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
        warningText.text = "GOLDEN DRAGON APPEARS";
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
        const float holdDuration = WarningDuration - fadeInDuration - 0.45f;

        while (holdElapsed < holdDuration)
        {
            holdElapsed += Time.deltaTime;
            yield return null;
        }

        const float fadeOutDuration = 0.45f;
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
        GameObject canvasObject = new GameObject("GoldenDragonOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 36;
        overlayCanvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject warningObject = new GameObject("GoldenDragonWarning");
        warningObject.transform.SetParent(canvasObject.transform, false);
        warningText = warningObject.AddComponent<TextMeshProUGUI>();
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = 52f;
        warningText.fontStyle = FontStyles.Bold;
        warningText.color = WarningTextColor;
        warningText.raycastTarget = false;
        UiLayoutUtility.SetAnchorCenter(warningText.rectTransform, new Vector2(0f, 240f), new Vector2(980f, 72f));
    }
}
