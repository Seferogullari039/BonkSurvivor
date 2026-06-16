using UnityEngine;

[DisallowMultipleComponent]
public class PlayerVisual : MonoBehaviour
{
    private Renderer[] bodyRenderers;

    private void Awake()
    {
        GameVisualStyle.AttachWorldVisual(
            transform,
            PrimitiveType.Sphere,
            GameVisualPalette.Player,
            0.9f,
            0.72f);

        bodyRenderers = GetComponentsInChildren<Renderer>(true);
        UpdateFpsBodyVisibility();
    }

    private void Update()
    {
        UpdateFpsBodyVisibility();
    }

    private void UpdateFpsBodyVisibility()
    {
        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (bodyRenderers == null) return;

        bool hideBody = FPSPlayerController.IsFpsModeActive && MainMenuManager.IsRunActive;

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            Renderer renderer = bodyRenderers[i];

            if (renderer == null) continue;

            renderer.enabled = !hideBody;
        }
    }
}
