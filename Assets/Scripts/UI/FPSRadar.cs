using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSRadar : MonoBehaviour
{
    private const float RadarRange = 25f;
    private const float RadarPixelSize = 150f;
    private const float BlipSize = 7f;

    private static readonly Color PlayerColor = new Color(0.85f, 0.95f, 1f, 1f);
    private static readonly Color EnemyColor = new Color(1f, 0.25f, 0.25f, 1f);
    private static readonly Color EliteColor = new Color(1f, 0.9f, 0.2f, 1f);
    private static readonly Color BossColor = new Color(0.75f, 0.35f, 1f, 1f);

    private GameObject radarPanel;
    private RectTransform blipContainer;
    private Transform playerTransform;
    private readonly List<Image> blipPool = new List<Image>();

    private void Awake()
    {
        playerTransform = transform;
        BuildRadarUi();
    }

    private void Update()
    {
        if (radarPanel == null) return;

        bool showRadar = FPSPlayerController.IsFpsModeActive
            && MainMenuManager.IsRunActive
            && Time.timeScale > 0f;

        if (radarPanel.activeSelf != showRadar)
        {
            radarPanel.SetActive(showRadar);
        }

        if (!showRadar) return;

        UpdateBlips();
    }

    private void BuildRadarUi()
    {
        Canvas canvas = UiLayoutUtility.GetGameplayCanvas();

        if (canvas == null) return;

        UiLayoutUtility.ConfigureGameplayCanvas(canvas);

        radarPanel = new GameObject("FPSRadarPanel");
        radarPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = radarPanel.AddComponent<RectTransform>();
        UiLayoutUtility.SetAnchorTopRight(panelRect, new Vector2(-24f, -24f), new Vector2(RadarPixelSize, RadarPixelSize));

        Image panelBackground = radarPanel.AddComponent<Image>();
        panelBackground.color = new Color(0.04f, 0.05f, 0.07f, 0.68f);
        panelBackground.raycastTarget = false;

        Outline panelOutline = radarPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.35f, 0.45f, 0.55f, 0.55f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        GameObject containerObject = new GameObject("BlipContainer");
        containerObject.transform.SetParent(radarPanel.transform, false);
        blipContainer = containerObject.AddComponent<RectTransform>();
        blipContainer.anchorMin = Vector2.zero;
        blipContainer.anchorMax = Vector2.one;
        blipContainer.offsetMin = Vector2.zero;
        blipContainer.offsetMax = Vector2.zero;

        CreateBlip(PlayerColor, Vector2.zero, true);

        radarPanel.SetActive(false);
    }

    private void UpdateBlips()
    {
        int blipIndex = 1;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (int i = 0; i < enemies.Length; i++)
        {
            GameObject enemyObject = enemies[i];

            if (enemyObject == null) continue;

            Vector3 worldOffset = enemyObject.transform.position - playerTransform.position;
            worldOffset.y = 0f;

            if (worldOffset.sqrMagnitude > RadarRange * RadarRange) continue;

            Vector3 localOffset = Quaternion.Euler(0f, -playerTransform.eulerAngles.y, 0f) * worldOffset;
            Vector2 radarPosition = new Vector2(localOffset.x, localOffset.z) / RadarRange * (RadarPixelSize * 0.42f);

            if (radarPosition.sqrMagnitude > (RadarPixelSize * 0.42f) * (RadarPixelSize * 0.42f))
            {
                radarPosition = radarPosition.normalized * (RadarPixelSize * 0.42f);
            }

            Enemy enemy = enemyObject.GetComponent<Enemy>();
            Color blipColor = EnemyColor;
            float blipSize = BlipSize;

            if (enemy != null)
            {
                blipColor = enemy.Type switch
                {
                    Enemy.EnemyType.Elite => EliteColor,
                    Enemy.EnemyType.MiniBoss => BossColor,
                    Enemy.EnemyType.DragonBoss => BossColor,
                    _ => EnemyColor
                };

                if (enemy.Type == Enemy.EnemyType.DragonBoss)
                {
                    blipSize = BlipSize + 5f;
                }
            }

            Image blip = GetOrCreateBlip(blipIndex);
            blip.gameObject.SetActive(true);
            blip.color = blipColor;

            RectTransform blipRect = blip.rectTransform;
            blipRect.sizeDelta = new Vector2(blipSize, blipSize);
            blipRect.anchoredPosition = radarPosition;
            blipIndex++;
        }

        for (int i = blipIndex; i < blipPool.Count; i++)
        {
            if (blipPool[i] != null)
            {
                blipPool[i].gameObject.SetActive(false);
            }
        }
    }

    private Image GetOrCreateBlip(int index)
    {
        while (blipPool.Count <= index)
        {
            blipPool.Add(CreateBlip(EnemyColor, Vector2.zero, false));
        }

        return blipPool[index];
    }

    private Image CreateBlip(Color color, Vector2 anchoredPosition, bool isPlayerBlip)
    {
        GameObject blipObject = new GameObject(isPlayerBlip ? "PlayerBlip" : "EnemyBlip");
        blipObject.transform.SetParent(blipContainer, false);

        RectTransform blipRect = blipObject.AddComponent<RectTransform>();
        blipRect.anchorMin = new Vector2(0.5f, 0.5f);
        blipRect.anchorMax = new Vector2(0.5f, 0.5f);
        blipRect.pivot = new Vector2(0.5f, 0.5f);
        blipRect.sizeDelta = new Vector2(isPlayerBlip ? BlipSize + 2f : BlipSize, isPlayerBlip ? BlipSize + 2f : BlipSize);
        blipRect.anchoredPosition = anchoredPosition;

        Image blipImage = blipObject.AddComponent<Image>();
        blipImage.color = color;

        if (!isPlayerBlip)
        {
            blipObject.SetActive(false);
        }

        return blipImage;
    }
}
