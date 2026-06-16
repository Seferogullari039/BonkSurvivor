using UnityEngine;

public static class GameVisualStyle
{
    private static MaterialPropertyBlock sharedPropertyBlock;

    public static void ApplyColor(
        Renderer renderer,
        Color color,
        float smoothness = 0.5f,
        bool emissionGlow = false,
        float emissionIntensity = 0.45f)
    {
        if (renderer == null) return;

        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }

        renderer.GetPropertyBlock(sharedPropertyBlock);

        sharedPropertyBlock.SetColor("_Color", color);

        Material sharedMaterial = renderer.sharedMaterial;
        if (sharedMaterial != null)
        {
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                sharedPropertyBlock.SetColor("_BaseColor", color);
            }

            if (sharedMaterial.HasProperty("_Smoothness"))
            {
                sharedPropertyBlock.SetFloat("_Smoothness", smoothness);
            }

            if (sharedMaterial.HasProperty("_EmissionColor"))
            {
                Color emissionColor = emissionGlow ? color * emissionIntensity : Color.black;
                sharedPropertyBlock.SetColor("_EmissionColor", emissionColor);

                if (emissionGlow && !sharedMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    sharedMaterial.EnableKeyword("_EMISSION");
                }
            }
        }

        renderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public static GameObject AttachWorldVisual(
        Transform parent,
        PrimitiveType primitive,
        Color color,
        float diameter,
        float smoothness = 0.55f,
        bool emissionGlow = false)
    {
        GameObject visualObject = GameObject.CreatePrimitive(primitive);
        visualObject.name = "WorldVisual";
        visualObject.transform.SetParent(parent, false);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localScale = Vector3.one * diameter;

        Collider visualCollider = visualObject.GetComponent<Collider>();

        if (visualCollider != null)
        {
            Object.Destroy(visualCollider);
        }

        ApplyColor(visualObject.GetComponent<Renderer>(), color, smoothness, emissionGlow);

        Renderer parentRenderer = parent.GetComponent<Renderer>();

        if (parentRenderer != null)
        {
            parentRenderer.enabled = false;
        }

        return visualObject;
    }
}
