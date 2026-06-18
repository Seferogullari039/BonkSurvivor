using System.Collections;
using UnityEngine;

public static class ChestOpeningPresentation
{
    private const float OpeningDuration = 0.30f;
    private const float LidOpenAngle = -42f;

    public static Vector3 GetMouthWorldPosition(Transform chestTransform, Vector3 fallbackPosition)
    {
        if (chestTransform == null)
        {
            return fallbackPosition;
        }

        Transform visualRoot = chestTransform.Find("ChestVisualRoot");
        Transform root = visualRoot != null ? visualRoot : chestTransform;

        Transform lid = root.Find("ChestLid");

        if (lid == null)
        {
            lid = root.Find("Lid");
        }

        if (lid != null)
        {
            return lid.position + Vector3.up * 0.05f;
        }

        return chestTransform.position + Vector3.up * 0.68f;
    }

    public static bool UsesMouthAnchor(Transform chestTransform)
    {
        return chestTransform != null;
    }

    public static IEnumerator PlayPhysicalOpening(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            yield break;
        }

        Transform animatedRoot = ResolveAnimatedRoot(chestTransform);
        Transform lidTransform = FindLid(animatedRoot, chestTransform);

        if (animatedRoot == null)
        {
            yield break;
        }

        ChestVisualAnimator visualAnimator = chestTransform.GetComponent<ChestVisualAnimator>();

        if (visualAnimator != null)
        {
            visualAnimator.SetIdleEnabled(false);
        }

        Vector3 baseLocalPosition = animatedRoot.localPosition;
        Quaternion baseLocalRotation = animatedRoot.localRotation;
        Vector3 baseLocalScale = animatedRoot.localScale;
        Quaternion lidBaseRotation = lidTransform != null ? lidTransform.localRotation : Quaternion.identity;

        float elapsed = 0f;

        while (elapsed < OpeningDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / OpeningDuration);
            float decay = 1f - progress;

            float wobbleX = Mathf.Sin(progress * 34f) * decay * 2.8f;
            float wobbleZ = Mathf.Sin(progress * 27f + 0.6f) * decay * 3.2f;
            animatedRoot.localRotation = baseLocalRotation * Quaternion.Euler(wobbleX, 0f, wobbleZ);
            animatedRoot.localPosition = baseLocalPosition + new Vector3(Mathf.Sin(progress * 41f) * 0.028f * decay, 0f, 0f);

            float scaleY;

            if (progress < 0.42f)
            {
                scaleY = Mathf.Lerp(1f, 0.88f, progress / 0.42f);
            }
            else if (progress < 0.72f)
            {
                scaleY = Mathf.Lerp(0.88f, 1.06f, (progress - 0.42f) / 0.3f);
            }
            else
            {
                scaleY = Mathf.Lerp(1.06f, 1f, (progress - 0.72f) / 0.28f);
            }

            float scaleXZ = Mathf.Lerp(1f, 1.04f, 1f - scaleY);
            animatedRoot.localScale = new Vector3(
                baseLocalScale.x * scaleXZ,
                baseLocalScale.y * scaleY,
                baseLocalScale.z * scaleXZ);

            if (lidTransform != null)
            {
                float lidProgress = Mathf.Clamp01((progress - 0.06f) / 0.78f);
                float lidEase = lidProgress * lidProgress * (3f - 2f * lidProgress);
                lidTransform.localRotation = lidBaseRotation * Quaternion.Euler(LidOpenAngle * lidEase, 0f, 0f);
            }

            yield return null;
        }

        animatedRoot.localPosition = baseLocalPosition;
        animatedRoot.localRotation = baseLocalRotation;
        animatedRoot.localScale = baseLocalScale;
    }

    private static Transform ResolveAnimatedRoot(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return null;
        }

        Transform visualRoot = chestTransform.Find("ChestVisualRoot");

        return visualRoot != null ? visualRoot : chestTransform;
    }

    private static Transform FindLid(Transform animatedRoot, Transform chestTransform)
    {
        if (animatedRoot != null)
        {
            Transform lid = animatedRoot.Find("ChestLid");

            if (lid != null)
            {
                return lid;
            }

            lid = animatedRoot.Find("Lid");

            if (lid != null)
            {
                return lid;
            }
        }

        if (chestTransform == null)
        {
            return null;
        }

        Transform nestedLid = chestTransform.Find("ChestVisualRoot/ChestLid");

        return nestedLid != null ? nestedLid : chestTransform.Find("ChestVisualRoot/Lid");
    }
}
