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

        string resourcePath = ResourceFolder + iconKey;
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);

        if (loadedSprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);

            if (sprites != null && sprites.Length > 0)
            {
                loadedSprite = sprites[0];
            }
        }

        if (loadedSprite != null)
        {
            SpriteCache[iconKey] = loadedSprite;
        }

        return loadedSprite;
    }
}
