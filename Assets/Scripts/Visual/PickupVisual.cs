using UnityEngine;

public enum PickupVisualType
{
    Coin,
    XPOrb,
    PlayerProjectile
}

[DisallowMultipleComponent]
public class PickupVisual : MonoBehaviour
{
    [SerializeField] private PickupVisualType visualType;

    private void Awake()
    {
        switch (visualType)
        {
            case PickupVisualType.Coin:
                GameVisualStyle.AttachWorldVisual(
                    transform,
                    PrimitiveType.Sphere,
                    GameVisualPalette.Coin,
                    0.55f,
                    0.9f,
                    true
                );
                break;
            case PickupVisualType.XPOrb:
                GameVisualStyle.AttachWorldVisual(
                    transform,
                    PrimitiveType.Sphere,
                    GameVisualPalette.XPOrb,
                    0.5f,
                    0.82f,
                    true
                );
                break;
            case PickupVisualType.PlayerProjectile:
                GameVisualStyle.AttachWorldVisual(
                    transform,
                    PrimitiveType.Sphere,
                    GameVisualPalette.PlayerProjectile,
                    0.32f,
                    0.85f,
                    true
                );
                break;
        }
    }
}
