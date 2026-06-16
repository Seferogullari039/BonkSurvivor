using System.Collections;
using TMPro;
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
    private const float WarningDuration = 2.6f;

    private static readonly Color TintColor = new Color(0.55f, 0.04f, 0.06f, 0f);
    private static readonly Color WarningTextColor = new Color(0.95f, 0.18f, 0.16f, 1f);

    private bool isActive;
    private float eventTimer;
    private float eventDuration;
    private int lastRollWave;
    private EnemySpawner cachedSpawner;
    private Canvas overlayCanvas;
    private Image tintImage;
    private TextMeshProUGUI warningText;
    private Coroutine warningRoutine;

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
        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
            warningRoutine = null;
        }

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
        ShowWarning();
    }

    private void EndEvent()
    {
        isActive = false;
        eventTimer = 0f;
        eventDuration = 0f;
        HideOverlayImmediate();
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
        warningText.text = "BLOOD MOON RISES";
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

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
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

        GameObject warningObject = new GameObject("BloodMoonWarning");
        warningObject.transform.SetParent(canvasObject.transform, false);
        warningText = warningObject.AddComponent<TextMeshProUGUI>();
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = 54f;
        warningText.fontStyle = FontStyles.Bold;
        warningText.color = WarningTextColor;
        warningText.raycastTarget = false;
        UiLayoutUtility.SetAnchorCenter(warningText.rectTransform, new Vector2(0f, 280f), new Vector2(920f, 72f));
    }
}
