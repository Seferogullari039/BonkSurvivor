using System.Collections;
using UnityEngine;

public static class EnemyTelegraphUtility
{
    public static void ApplyFlashColor(Renderer renderer, Color flashColor, float emissionIntensity = 0.65f)
    {
        if (renderer == null) return;

        GameVisualStyle.ApplyColor(renderer, flashColor, 0.55f, true, emissionIntensity);
    }

    public static void RestoreColor(Renderer renderer, Color baseColor, float smoothness, bool glow, float emissionIntensity = 0.45f)
    {
        if (renderer == null) return;

        GameVisualStyle.ApplyColor(renderer, baseColor, smoothness, glow, emissionIntensity);
    }

    public static IEnumerator WaitSafely(MonoBehaviour host, float duration)
    {
        if (host == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (host == null) yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public static GameObject SpawnRingFlash(Vector3 position, Color color, float radius, float duration)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TelegraphRing";
        ring.transform.position = position + Vector3.up * 0.05f;
        ring.transform.localScale = new Vector3(radius, 0.04f, radius);

        Collider collider = ring.GetComponent<Collider>();

        if (collider != null)
        {
            Object.Destroy(collider);
        }

        Renderer renderer = ring.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, 0.7f, true, 0.55f);
        }

        Object.Destroy(ring, duration);
        return ring;
    }
}
