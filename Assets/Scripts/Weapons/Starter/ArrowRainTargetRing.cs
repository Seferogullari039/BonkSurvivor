using UnityEngine;

public class ArrowRainTargetRing : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float fadeDuration = 1f;
    private float elapsed;
    private Color baseColor = new Color(1f, 0.52f, 0.18f, 0.28f);

    public static void Spawn(Vector3 center, float radius, float duration = 1f)
    {
        if (radius <= 0f) return;

        GameObject ringObject = new GameObject("ArrowRainTargetRing");
        ringObject.transform.position = center + Vector3.up * 0.12f;

        ArrowRainTargetRing ring = ringObject.AddComponent<ArrowRainTargetRing>();
        ring.Initialize(radius, duration);
    }

    private void Initialize(float radius, float duration)
    {
        fadeDuration = Mathf.Max(0.2f, duration);
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.widthMultiplier = 0.07f;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.numCapVertices = 2;

        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");

        if (lineShader == null)
        {
            lineShader = Shader.Find("Sprites/Default");
        }

        if (lineShader != null)
        {
            lineRenderer.material = new Material(lineShader);
        }

        const int segments = 40;
        lineRenderer.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            lineRenderer.SetPosition(
                i,
                new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        ApplyAlpha(1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float remaining = 1f - (elapsed / fadeDuration);

        if (remaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        ApplyAlpha(remaining);
    }

    private void ApplyAlpha(float alphaMultiplier)
    {
        if (lineRenderer == null) return;

        Color color = baseColor;
        color.a = baseColor.a * alphaMultiplier;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
