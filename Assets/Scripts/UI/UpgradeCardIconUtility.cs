using System.Collections.Generic;
using UnityEngine;

public static class UpgradeCardIconUtility
{
    private const string ResourceFolder = "UpgradeIcons/";
    private const int ProceduralIconSize = 64;

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

        if (loadedSprite == null)
        {
            loadedSprite = TryCreateProceduralIcon(iconKey);
        }

        if (loadedSprite != null)
        {
            SpriteCache[iconKey] = loadedSprite;
        }

        return loadedSprite;
    }

    public static bool TryGetIconFrameColor(string iconKey, out Color frameColor)
    {
        switch (iconKey)
        {
            case "frost_sigil":
                frameColor = new Color(0.36f, 0.82f, 0.98f, 1f);
                return true;
            case "cryo_core":
                frameColor = new Color(0.28f, 0.62f, 0.95f, 1f);
                return true;
            case "shadow_rift":
                frameColor = new Color(0.58f, 0.28f, 0.82f, 1f);
                return true;
            case "void_catalyst":
                frameColor = new Color(0.72f, 0.34f, 0.92f, 1f);
                return true;
            default:
                frameColor = Color.white;
                return false;
        }
    }

    private static Sprite TryCreateProceduralIcon(string iconKey)
    {
        switch (iconKey)
        {
            case "frost_sigil":
                return CreateSpriteFromPixels(BuildFrostSigilIcon());
            case "cryo_core":
                return CreateSpriteFromPixels(BuildCryoCoreIcon());
            case "shadow_rift":
                return CreateSpriteFromPixels(BuildShadowRiftIcon());
            case "void_catalyst":
                return CreateSpriteFromPixels(BuildVoidCatalystIcon());
            default:
                return null;
        }
    }

    private static Sprite CreateSpriteFromPixels(Color[] pixels)
    {
        Texture2D texture = new Texture2D(ProceduralIconSize, ProceduralIconSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, ProceduralIconSize, ProceduralIconSize),
            new Vector2(0.5f, 0.5f),
            ProceduralIconSize);
    }

    private static Color[] CreateBlankPixels()
    {
        Color[] pixels = new Color[ProceduralIconSize * ProceduralIconSize];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        return pixels;
    }

    private static Color[] BuildFrostSigilIcon()
    {
        Color[] pixels = CreateBlankPixels();
        Vector2 center = new Vector2(32f, 32f);

        FillDisk(pixels, center, 30f, new Color(0.05f, 0.14f, 0.24f, 1f));
        FillRing(pixels, center, 24f, 29f, new Color(0.28f, 0.72f, 0.92f, 0.55f));
        FillRing(pixels, center, 17f, 19f, new Color(0.55f, 0.92f, 1f, 0.85f));

        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3f;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            DrawLine(pixels, center, center + dir * 20f, 2.2f, new Color(0.78f, 0.96f, 1f, 1f));
            DrawLine(pixels, center + dir * 10f, center + dir * 14f + Rotate(dir, 0.55f) * 6f, 1.6f, new Color(0.62f, 0.9f, 1f, 0.95f));
        }

        FillDisk(pixels, center, 4.5f, new Color(0.92f, 1f, 1f, 1f));
        return pixels;
    }

    private static Color[] BuildCryoCoreIcon()
    {
        Color[] pixels = CreateBlankPixels();
        Vector2 center = new Vector2(32f, 32f);

        FillDisk(pixels, center, 30f, new Color(0.04f, 0.12f, 0.22f, 1f));
        FillRing(pixels, center, 22f, 28f, new Color(0.18f, 0.42f, 0.78f, 0.45f));

        Vector2 top = center + Vector2.up * 16f;
        Vector2 right = center + Vector2.right * 12f;
        Vector2 bottom = center + Vector2.down * 16f;
        Vector2 left = center + Vector2.left * 12f;
        FillTriangle(pixels, top, right, center, new Color(0.45f, 0.82f, 1f, 0.95f));
        FillTriangle(pixels, right, bottom, center, new Color(0.32f, 0.68f, 0.96f, 0.95f));
        FillTriangle(pixels, bottom, left, center, new Color(0.24f, 0.58f, 0.9f, 0.95f));
        FillTriangle(pixels, left, top, center, new Color(0.38f, 0.76f, 1f, 0.95f));
        FillDisk(pixels, center, 6f, new Color(0.86f, 0.98f, 1f, 1f));

        return pixels;
    }

    private static Color[] BuildShadowRiftIcon()
    {
        Color[] pixels = CreateBlankPixels();
        Vector2 center = new Vector2(32f, 32f);

        FillDisk(pixels, center, 30f, new Color(0.07f, 0.03f, 0.12f, 1f));
        FillRing(pixels, center, 21f, 28f, new Color(0.42f, 0.14f, 0.62f, 0.9f));
        FillRing(pixels, center, 14f, 18f, new Color(0.18f, 0.05f, 0.24f, 0.95f));
        FillDisk(pixels, center, 11f, new Color(0.03f, 0.01f, 0.06f, 1f));

        for (int y = 0; y < ProceduralIconSize; y++)
        {
            for (int x = 0; x < ProceduralIconSize; x++)
            {
                float dx = (x - center.x) / 4.5f;
                float dy = (y - center.y) / 11f;
                float ellipse = dx * dx + dy * dy;

                if (ellipse <= 1f)
                {
                    BlendPixel(pixels, x, y, new Color(0.62f, 0.22f, 0.82f, 0.75f * (1f - ellipse)));
                }
            }
        }

        return pixels;
    }

    private static Color[] BuildVoidCatalystIcon()
    {
        Color[] pixels = CreateBlankPixels();
        Vector2 center = new Vector2(32f, 32f);

        FillDisk(pixels, center, 30f, new Color(0.08f, 0.03f, 0.14f, 1f));
        FillDisk(pixels, center, 15f, new Color(0.34f, 0.1f, 0.52f, 0.85f));
        FillRing(pixels, center, 15f, 18f, new Color(0.58f, 0.22f, 0.82f, 0.7f));
        FillDisk(pixels, center, 7f, new Color(0.82f, 0.48f, 1f, 1f));
        FillDisk(pixels, center, 3f, new Color(0.95f, 0.82f, 1f, 1f));

        Vector2[] sparkDirs =
        {
            new Vector2(1f, 1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        for (int i = 0; i < sparkDirs.Length; i++)
        {
            Vector2 sparkCenter = center + sparkDirs[i] * 20f;
            FillDisk(pixels, sparkCenter, 2.4f, new Color(0.74f, 0.34f, 0.95f, 0.85f));
        }

        return pixels;
    }

    private static Vector2 Rotate(Vector2 vector, float radians)
    {
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    private static void FillDisk(Color[] pixels, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        int maxX = Mathf.Min(ProceduralIconSize - 1, Mathf.CeilToInt(center.x + radius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        int maxY = Mathf.Min(ProceduralIconSize - 1, Mathf.CeilToInt(center.y + radius));
        float radiusSqr = radius * radius;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;

                if (dx * dx + dy * dy <= radiusSqr)
                {
                    BlendPixel(pixels, x, y, color);
                }
            }
        }
    }

    private static void FillRing(Color[] pixels, Vector2 center, float innerRadius, float outerRadius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - outerRadius));
        int maxX = Mathf.Min(ProceduralIconSize - 1, Mathf.CeilToInt(center.x + outerRadius));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - outerRadius));
        int maxY = Mathf.Min(ProceduralIconSize - 1, Mathf.CeilToInt(center.y + outerRadius));
        float innerSqr = innerRadius * innerRadius;
        float outerSqr = outerRadius * outerRadius;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float distSqr = dx * dx + dy * dy;

                if (distSqr >= innerSqr && distSqr <= outerSqr)
                {
                    BlendPixel(pixels, x, y, color);
                }
            }
        }
    }

    private static void FillTriangle(Color[] pixels, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, ProceduralIconSize - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, ProceduralIconSize - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, ProceduralIconSize - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, ProceduralIconSize - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);

                if (PointInTriangle(point, a, b, c))
                {
                    BlendPixel(pixels, x, y, color);
                }
            }
        }
    }

    private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(point, a, b);
        float d2 = Sign(point, b, c);
        float d3 = Sign(point, c, a);
        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private static void DrawLine(Color[] pixels, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;

        if (length <= 0.01f)
        {
            return;
        }

        Vector2 direction = delta / length;
        int steps = Mathf.CeilToInt(length);

        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = start + direction * i;
            FillDisk(pixels, point, thickness * 0.5f, color);
        }
    }

    private static void BlendPixel(Color[] pixels, int x, int y, Color color)
    {
        int index = y * ProceduralIconSize + x;
        Color existing = pixels[index];
        float alpha = color.a;
        pixels[index] = new Color(
            Mathf.Lerp(existing.r, color.r, alpha),
            Mathf.Lerp(existing.g, color.g, alpha),
            Mathf.Lerp(existing.b, color.b, alpha),
            Mathf.Clamp01(existing.a + alpha * (1f - existing.a)));
    }
}
