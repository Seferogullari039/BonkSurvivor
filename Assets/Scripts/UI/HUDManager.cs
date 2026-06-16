using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image xpBarFill;

    private GameObject hpBarBackground;
    private GameObject xpBarBackground;
    private static bool missingReferenceWarningShown;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
        SetupHudLayout();
    }

    private void Start()
    {
        UpdateHP(10, 10);
        UpdateXP(0, 5);
        UpdateLevel(1);
        UpdateWave(1);
        UpdateXPBar(0, 5);
        UpdateCoins(0);
    }

    private void ResolveReferences()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return;
        }

        Transform canvasTransform = canvas.transform;

        hpText ??= FindText(canvasTransform, "HPText");
        xpText ??= FindText(canvasTransform, "XPText");
        levelText ??= FindText(canvasTransform, "LevelText");
        waveText ??= FindText(canvasTransform, "WaveText");
        coinText ??= FindText(canvasTransform, "CoinText");

        hpBarBackground ??= canvasTransform.Find("HPBarBackground")?.gameObject;
        xpBarBackground ??= canvasTransform.Find("XPBarBackground")?.gameObject;

        hpBarFill ??= FindBarFill(canvasTransform, "HPBarBackground", "HPBarFill");
        xpBarFill ??= FindBarFill(canvasTransform, "XPBarBackground", "XPBarFill");

        if (missingReferenceWarningShown)
        {
            return;
        }

        if (hpBarFill == null || xpBarFill == null)
        {
            Debug.LogWarning("HUDManager: HPBarFill veya XPBarFill bulunamadi. Canvas referanslarini kontrol edin.");
            missingReferenceWarningShown = true;
        }
    }

    private void SetupHudLayout()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            return;
        }

        canvas.gameObject.SetActive(true);
        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        Transform canvasTransform = canvas.transform;

        SetupHudText(hpText, canvasTransform, new Vector2(20f, -20f));
        SetupHudText(xpText, canvasTransform, new Vector2(20f, -60f));
        SetupHudText(levelText, canvasTransform, new Vector2(20f, -100f));
        SetupHudText(waveText, canvasTransform, new Vector2(20f, -140f));
        SetupHudText(coinText, canvasTransform, new Vector2(20f, -180f));

        Transform hudPanel = canvasTransform.Find("HUDPanel");

        if (hudPanel != null)
        {
            hudPanel.gameObject.SetActive(false);
        }
    }

    private static void SetupHudText(TMP_Text text, Transform canvasTransform, Vector2 anchoredPosition)
    {
        if (text == null)
        {
            return;
        }

        text.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(400f, 40f);
        rectTransform.anchoredPosition = anchoredPosition;
        text.fontSize = 24f;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.color = Color.white;
        text.raycastTarget = false;
        text.gameObject.SetActive(true);
    }

    private TMP_Text FindText(Transform canvasTransform, string objectName)
    {
        Transform target = canvasTransform.Find(objectName);

        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private Image FindBarFill(Transform canvasTransform, string backgroundName, string fillName)
    {
        Transform background = canvasTransform.Find(backgroundName);

        if (background == null)
        {
            return null;
        }

        Transform fill = background.Find(fillName);

        return fill != null ? fill.GetComponent<Image>() : null;
    }

    public void UpdateHP(int currentHP, int maxHP)
    {
        if (hpText != null)
        {
            hpText.text = "HP " + currentHP + " / " + maxHP;
        }

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        }
    }

    public void UpdateXPBar(int currentXP, int xpToNextLevel)
    {
        if (xpBarFill == null)
        {
            return;
        }

        if (xpToNextLevel <= 0)
        {
            xpBarFill.fillAmount = 0f;
            return;
        }

        xpBarFill.fillAmount = (float)currentXP / xpToNextLevel;
    }

    public void UpdateXP(int currentXP, int xpToNextLevel)
    {
        if (xpText == null)
        {
            return;
        }

        xpText.text = "XP " + currentXP + " / " + xpToNextLevel;
    }

    public void UpdateLevel(int currentLevel)
    {
        if (levelText == null)
        {
            return;
        }

        levelText.text = "LEVEL " + currentLevel;
    }

    public void UpdateWave(int wave)
    {
        if (waveText == null)
        {
            return;
        }

        waveText.text = "WAVE " + wave;
    }

    public void UpdateCoins(int coins)
    {
        if (coinText == null)
        {
            return;
        }

        coinText.text = "COINS " + coins;
    }

    public void OnGameplayStarted()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            UiLayoutUtility.ConfigureGameplayCanvas(canvas);
        }

        gameObject.SetActive(true);
        SetGameplayHUDVisible(true);
    }

    public void SetGameplayHUDVisible(bool visible)
    {
        if (visible)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null && !canvas.gameObject.activeSelf)
            {
                canvas.gameObject.SetActive(true);
            }
        }

        SetElementVisible(hpText, visible);
        SetElementVisible(xpText, visible);
        SetElementVisible(levelText, visible);
        SetElementVisible(waveText, visible);
        SetElementVisible(coinText, visible);
        SetElementVisible(hpBarBackground, visible);
        SetElementVisible(xpBarBackground, visible);
    }

    private static void SetElementVisible(Component component, bool visible)
    {
        if (component != null)
        {
            component.gameObject.SetActive(visible);
        }
    }

    private static void SetElementVisible(GameObject target, bool visible)
    {
        if (target != null)
        {
            target.SetActive(visible);
        }
    }
}
