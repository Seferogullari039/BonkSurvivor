using System.Collections.Generic;
using UnityEngine;

public static class WeaponFxUtility
{
    public static void SpawnLaserBeam(Vector3 start, Vector3 end)
    {
        SpawnLine(start, end, Color.cyan, 0.045f, 0.08f);
    }

    public static void SpawnLaserBeam(Vector3 start, Vector3 end, float life)
    {
        SpawnLine(start, end, Color.cyan, 0.045f, life);
    }

    public static void SpawnChainLightning(Vector3 start, Vector3 end)
    {
        SpawnLine(start, end, Color.blue, 0.035f, 0.12f);
    }

    public static void SpawnChainLightning(IReadOnlyList<Vector3> points, float life)
    {
        if (points == null || points.Count < 2) return;

        for (int i = 1; i < points.Count; i++)
        {
            SpawnLine(points[i - 1], points[i], Color.blue, 0.035f, life);
        }
    }

    public static void SpawnLightningSegment(Vector3 start, Vector3 end)
    {
        SpawnChainLightning(start, end);
    }

    public static void SpawnRocketExplosion(Vector3 position)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "RocketExplosionFX";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 0.25f;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null)
        {
            Object.Destroy(col);
        }

        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = new Color(1f, 0.45f, 0.05f, 0.85f);
            renderer.material = material;
        }

        RocketExplosionFx fx = sphere.AddComponent<RocketExplosionFx>();
        fx.life = 0.45f;
        fx.maxScale = 3.2f;

        FPSScreenShake.ShakeBig();
    }

    private static void SpawnLine(Vector3 start, Vector3 end, Color color, float width, float life)
    {
        GameObject go = new GameObject("WeaponLineFX");

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = width;
        line.endWidth = width * 0.45f;
        line.useWorldSpace = true;

        Material material = new Material(Shader.Find("Sprites/Default"));
        line.material = material;
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0f);

        Object.Destroy(go, life);
    }

    private class RocketExplosionFx : MonoBehaviour
    {
        public float life = 0.45f;
        public float maxScale = 3.2f;

        private float timer;
        private Renderer cachedRenderer;

        private void Awake()
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            float t = life > 0f ? Mathf.Clamp01(timer / life) : 1f;
            transform.localScale = Vector3.one * Mathf.Lerp(0.25f, maxScale, t);

            if (cachedRenderer != null && cachedRenderer.material != null)
            {
                Color color = cachedRenderer.material.color;
                color.a = 1f - t;
                cachedRenderer.material.color = color;
            }

            if (timer >= life)
            {
                Destroy(gameObject);
            }
        }
    }
}
