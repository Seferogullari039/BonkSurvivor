using UnityEngine;
using UnityEngine.UI;

public class BloodMoonEventManager : MonoBehaviour
{
    public static BloodMoonEventManager Instance { get; private set; }

    private const int MinWave = 5;
    private const float TriggerChance = 0.08f;
    private const float SpawnTempoMultiplier = 0.84f;
    private const float MinDuration = 30f;
    private const float MaxDuration = 45f;
    private const float ActiveTintAlpha = 0.14f;

    private static readonly Color TintColor = new Color(0.55f, 0.04f, 0.06f, 0f);

    private bool isActive;
    private float eventTimer;
    private float eventDuration;
    private int lastRollWave;
    private EnemySpawner cachedSpawner;
    private Canvas overlayCanvas;
    private Image tintImage;

    public bool IsActive => isActive;
    public float SpawnIntervalMultiplier => isActive ? SpawnTempoMultiplier : 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<BloodMoonEventManager>() != null) return;

        GameObject host = new GameObject("BloodMoonEventManager");
        host.AddComponent<BloodMoonEventManager>();
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
        HideOverlayImmediate();
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
            if (isActive || lastRollWave != 0)
            {
                ResetRunState();
            }

            return;
        }

        if (isActive)
        {
            eventTimer += Time.deltaTime;
            UpdateActiveTint();

            if (eventTimer >= eventDuration)
            {
                EndEvent();
            }

            return;
        }

        TryRollEvent();
    }

    public void ResetRunState()
    {
        isActive = false;
        eventTimer = 0f;
        eventDuration = 0f;
        lastRollWave = 0;
        cachedSpawner = null;
        HideOverlayImmediate();
    }

    public bool DevTriggerBloodMoon()
    {
        if (isActive || !MainMenuManager.IsRunActive) return false;

        StartEvent();
        return true;
    }

    private void TryRollEvent()
    {
        if (cachedSpawner == null)
        {
            cachedSpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (cachedSpawner == null) return;

        int currentWave = cachedSpawner.CurrentWave;

        if (currentWave < MinWave) return;
        if (lastRollWave == currentWave) return;

        lastRollWave = currentWave;

        if (Random.value > TriggerChance) return;

        StartEvent();
    }

    private void StartEvent()
    {
        isActive = true;
        eventTimer = 0f;
        eventDuration = Random.Range(MinDuration, MaxDuration);
        SetTintAlpha(ActiveTintAlpha);
        RunEventMessageDisplay.ShowBloodMoon();
    }

    private void EndEvent()
    {
        isActive = false;
        eventTimer = 0f;
        eventDuration = 0f;
        HideOverlayImmediate();
    }

    private void UpdateActiveTint()
    {
        if (tintImage == null) return;

        float pulse = 0.9f + Mathf.Sin(Time.time * 2.4f) * 0.1f;
        SetTintAlpha(ActiveTintAlpha * pulse);
    }

    private void SetTintAlpha(float alpha)
    {
        if (tintImage == null) return;

        Color color = TintColor;
        color.a = alpha;
        tintImage.color = color;
        tintImage.enabled = alpha > 0.001f;
    }

    private void HideOverlayImmediate()
    {
        SetTintAlpha(0f);
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("BloodMoonOverlayCanvas");
        canvasObject.transform.SetParent(transform, false);

        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 35;
        overlayCanvas.pixelPerfect = false;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject tintObject = new GameObject("BloodMoonTint");
        tintObject.transform.SetParent(canvasObject.transform, false);
        tintImage = tintObject.AddComponent<Image>();
        tintImage.color = TintColor;
        tintImage.raycastTarget = false;
        UiLayoutUtility.StretchToParent(tintImage.rectTransform);
    }
}
