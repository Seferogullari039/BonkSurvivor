using System.Collections.Generic;
using UnityEngine;

public static class UpgradeCardIconUtility
{
    private const string ResourceFolder = "UpgradeIcons/";

    private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

    public static Sprite TryLoadSprite(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey))
        {
            return null;
        }

        if (SpriteCache.TryGetValue(iconKey, out Sprite cachedSprite))
        {
            return cachedSprite;
        }

        Sprite loadedSprite = Resources.Load<Sprite>(ResourceFolder + iconKey);
        SpriteCache[iconKey] = loadedSprite;
        return loadedSprite;
    }

    public static string GetFallbackLabel(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey))
        {
            return string.Empty;
        }

        return iconKey switch
        {
            "rapid_mechanism" => "⚡",
            "swift_projectiles" => "➤",
            "magnet_sense" => "◎",
            "sharp_instinct" => "✦",
            "spread_shot" => "⋔",
            "piercing_shot" => "➹",
            "orbiting_orb" => "◉",
            "rocket_launcher" => "🚀",
            "chain_lightning" => "⚡",
            "laser_beam" => "═",
            "meteor_focus" => "☄",
            "whirlwind_training" => "🌀",
            "arrow_storm" => "🏹",
            "inferno_ritual" => "🔥",
            "blade_tempest" => "⚔",
            "rain_caller" => "🌧",
            "ember_core" => "🔥",
            "sharpened_arrows" => "🏹",
            "honed_blade" => "⚔",
            _ => "✦"
        };
    }
}
