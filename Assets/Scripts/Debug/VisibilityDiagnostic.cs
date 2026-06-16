using UnityEngine;
using UnityEngine.UI;

public static class VisibilityDiagnostic
{
    public static void LogPlayState(string stage)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        Debug.Log("[VisibilityDiagnostic] ===== " + stage + " | canvasCount=" + canvases.Length + " =====");

        for (int i = 0; i < canvases.Length; i++)
        {
            LogCanvas(canvases[i], i);
        }

        LogHudElements();
        LogViewModelChain();
        LogMainCamera();
    }

    private static void LogCanvas(Canvas canvas, int index)
    {
        if (canvas == null)
        {
            Debug.Log("[VisibilityDiagnostic] Canvas[" + index + "] missing");
            return;
        }

        string role = ResolveCanvasRole(canvas);

        Debug.Log(
            "[VisibilityDiagnostic] Canvas[" + index + "] role="
            + role
            + " name="
            + canvas.name
            + " activeSelf="
            + canvas.gameObject.activeSelf
            + " activeInHierarchy="
            + canvas.gameObject.activeInHierarchy
            + " renderMode="
            + canvas.renderMode
            + " sortingOrder="
            + canvas.sortingOrder
            + " scale="
            + canvas.GetComponent<RectTransform>().localScale);
    }

    private static string ResolveCanvasRole(Canvas canvas)
    {
        if (canvas.name == "DevAdminCanvas")
        {
            return "DevAdmin";
        }

        if (canvas.transform.Find("HPBarBackground") != null
            || canvas.transform.Find("MainMenuPanel") != null
            || canvas.transform.Find("LevelUpPanel") != null)
        {
            return "Gameplay(HUD+Menu+LevelUp)";
        }

        return "Unknown";
    }

    private static void LogHudElements()
    {
        Canvas gameplayCanvas = UiLayoutUtility.GetGameplayCanvas();

        LogElement("HUD Canvas", gameplayCanvas != null ? gameplayCanvas.gameObject : null);
        LogElement("MainMenuPanel", gameplayCanvas != null ? gameplayCanvas.transform.Find("MainMenuPanel")?.gameObject : null);
        LogElement("LevelUpPanel", gameplayCanvas != null ? gameplayCanvas.transform.Find("LevelUpPanel")?.gameObject : null);
        LogElement("HP Bar", gameplayCanvas != null ? gameplayCanvas.transform.Find("HPBarBackground")?.gameObject : null);
        LogElement("XP Bar", gameplayCanvas != null ? gameplayCanvas.transform.Find("XPBarBackground")?.gameObject : null);
        LogElement("Wave Text", gameplayCanvas != null ? gameplayCanvas.transform.Find("WaveText")?.gameObject : null);
        LogElement("Level Text", gameplayCanvas != null ? gameplayCanvas.transform.Find("LevelText")?.gameObject : null);
        LogElement("Coin Text", gameplayCanvas != null ? gameplayCanvas.transform.Find("CoinText")?.gameObject : null);

        Transform radar = gameplayCanvas != null ? gameplayCanvas.transform.Find("FPSRadarPanel") : null;

        LogElement("Radar", radar != null ? radar.gameObject : null);
    }

    private static void LogElement(string label, GameObject target)
    {
        if (target == null)
        {
            Debug.Log("[VisibilityDiagnostic] " + label + " | missing");
            return;
        }

        Debug.Log(
            "[VisibilityDiagnostic] "
            + label
            + " | activeSelf="
            + target.activeSelf
            + " activeInHierarchy="
            + target.activeInHierarchy);
    }

    private static void LogViewModelChain()
    {
        Camera camera = Camera.main;
        Transform cameraTransform = camera != null ? camera.transform : null;
        Transform viewModelRoot = cameraTransform != null ? cameraTransform.Find("ViewModelRoot") : null;
        Transform weaponMount = viewModelRoot != null ? viewModelRoot.Find("WeaponMount") : null;
        Transform weaponVisual = weaponMount != null ? weaponMount.Find("StarterWeaponVisual") : null;

        if (cameraTransform != null)
        {
            Debug.Log("[VisibilityDiagnostic] MainCamera worldPosition=" + cameraTransform.position);
        }
        else
        {
            Debug.Log("[VisibilityDiagnostic] MainCamera | missing");
        }

        if (viewModelRoot != null)
        {
            Debug.Log(
                "[VisibilityDiagnostic] ViewModelRoot"
                + " worldPosition="
                + viewModelRoot.position
                + " localPosition="
                + viewModelRoot.localPosition
                + " parent="
                + FormatParent(viewModelRoot));
        }
        else
        {
            Debug.Log("[VisibilityDiagnostic] ViewModelRoot | missing");
        }

        if (weaponMount != null)
        {
            Debug.Log(
                "[VisibilityDiagnostic] WeaponMount"
                + " worldPosition="
                + weaponMount.position
                + " localPosition="
                + weaponMount.localPosition
                + " parent="
                + FormatParent(weaponMount));
        }
        else
        {
            Debug.Log("[VisibilityDiagnostic] WeaponMount | missing");
        }

        if (weaponVisual != null)
        {
            Debug.Log(
                "[VisibilityDiagnostic] StarterWeaponVisual"
                + " worldPosition="
                + weaponVisual.position
                + " localPosition="
                + weaponVisual.localPosition
                + " localRotation="
                + weaponVisual.localRotation.eulerAngles
                + " localScale="
                + weaponVisual.localScale
                + " parent="
                + FormatParent(weaponVisual)
                + " active="
                + weaponVisual.gameObject.activeInHierarchy);

            Renderer[] renderers = weaponVisual.GetComponentsInChildren<Renderer>(true);
            int enabledCount = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null)
                {
                    continue;
                }

                if (renderer.enabled)
                {
                    enabledCount++;
                }

                Bounds bounds = renderer.bounds;
                Color color = ResolveRendererColor(renderer);

                Debug.Log(
                    "[VisibilityDiagnostic] "
                    + renderer.gameObject.name
                    + " boundsCenter="
                    + bounds.center
                    + " boundsSize="
                    + bounds.size
                    + " color="
                    + color
                    + " alpha="
                    + color.a);
            }

            Debug.Log(
                "[VisibilityDiagnostic] StarterWeaponVisual active="
                + weaponVisual.gameObject.activeInHierarchy
                + " RendererCount="
                + renderers.Length
                + " enabledRenderers="
                + enabledCount);
        }
        else
        {
            Debug.Log("[VisibilityDiagnostic] StarterWeaponVisual | missing");
        }

        Debug.Log(
            "[VisibilityDiagnostic] ViewModelRoot localPosition="
            + (viewModelRoot != null ? viewModelRoot.localPosition.ToString() : "missing"));
        Debug.Log(
            "[VisibilityDiagnostic] WeaponMount localPosition="
            + (weaponMount != null ? weaponMount.localPosition.ToString() : "missing"));
    }

    private static Color ResolveRendererColor(Renderer renderer)
    {
        if (renderer == null)
        {
            return Color.magenta;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        if (block.HasColor("_BaseColor"))
        {
            return block.GetColor("_BaseColor");
        }

        if (block.HasColor("_Color"))
        {
            return block.GetColor("_Color");
        }

        Material material = renderer.sharedMaterial;

        if (material == null)
        {
            return Color.magenta;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        return material.color;
    }

    private static string FormatParent(Transform target)
    {
        if (target == null || target.parent == null)
        {
            return "null";
        }

        return target.parent.name;
    }

    private static void LogMainCamera()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            Debug.Log("[VisibilityDiagnostic] Main Camera | missing");
            return;
        }

        Debug.Log(
            "[VisibilityDiagnostic] Main Camera"
            + " | activeSelf="
            + camera.gameObject.activeSelf
            + " activeInHierarchy="
            + camera.gameObject.activeInHierarchy
            + " enabled="
            + camera.enabled
            + " cullingMask="
            + camera.cullingMask
            + " seesDefaultLayer="
            + ((camera.cullingMask & (1 << 0)) != 0));
    }
}
