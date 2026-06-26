using System.Collections;
using UnityEngine;

public static class ChestOpeningPresentation
{
    private const float AnticipationDuration = 0.14f;
    private const float OpeningDuration = 0.32f;
    private const float LidOpenAngle = -34f;
    private const float MouthLocalY = 0.56f;
    private const float FallbackMouthHeight = 0.75f;
    private const float CloseDistance = 2.4f;
    private const float FarDistance = 4.5f;
    private const float MinVisualIntensity = 0.42f;
    private const float PopScalePeak = 1.045f;
    private const float PopScaleXZPeak = 1.018f;
    private const float RevealSoftScale = 0.9f;
    private const float RevealSoftDropLocalY = -0.14f;

    public static Transform GetMouthAnchorTransform(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return null;
        }

        Transform visualRoot = ResolveAnimatedRoot(chestTransform);

        if (visualRoot != null)
        {
            return visualRoot;
        }

        Transform glow = chestTransform.Find("ChestGlow");

        if (glow != null)
        {
            return glow;
        }

        return chestTransform;
    }

    public static Vector3 GetMouthLocalOffset(Transform anchorTransform)
    {
        if (anchorTransform == null)
        {
            return Vector3.zero;
        }

        if (anchorTransform.name == "ChestVisualRoot" || anchorTransform.Find("ChestBase") != null)
        {
            return new Vector3(0f, MouthLocalY, 0f);
        }

        if (anchorTransform.name == "ChestLid" || anchorTransform.name == "Lid")
        {
            return new Vector3(0f, -0.06f, 0.02f);
        }

        return Vector3.zero;
    }

    public static Vector3 GetMouthWorldPosition(Transform chestTransform, Vector3 fallbackPosition)
    {
        Transform anchorTransform = GetMouthAnchorTransform(chestTransform);

        if (anchorTransform != null)
        {
            return anchorTransform.TransformPoint(GetMouthLocalOffset(anchorTransform));
        }

        if (chestTransform != null)
        {
            return chestTransform.position + Vector3.up * FallbackMouthHeight;
        }

        return fallbackPosition + Vector3.up * FallbackMouthHeight;
    }

    public static bool UsesMouthAnchor(Transform chestTransform)
    {
        return chestTransform != null;
    }

    public static void ApplyIdleOpenLid(Transform chestTransform, float openAngle = -28f)
    {
        if (chestTransform == null)
        {
            return;
        }

        Transform animatedRoot = ResolveAnimatedRoot(chestTransform);
        Transform lidTransform = FindLid(animatedRoot, chestTransform);

        if (lidTransform == null)
        {
            return;
        }

        lidTransform.localRotation = Quaternion.Euler(openAngle, 0f, 0f);
    }

    public static IEnumerator PlayAnticipationShake(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            yield break;
        }

        Transform animatedRoot = ResolveAnimatedRoot(chestTransform);

        if (animatedRoot == null)
        {
            yield break;
        }

        Vector3 baseLocalPosition = animatedRoot.localPosition;
        Quaternion baseLocalRotation = animatedRoot.localRotation;
        float visualIntensity = GetOpenVisualIntensity(chestTransform);
        float elapsed = 0f;

        while (elapsed < AnticipationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / AnticipationDuration);
            float decay = 1f - progress;
            float shake = Mathf.Sin(progress * 52f) * decay * 2.4f * visualIntensity;

            animatedRoot.localRotation = baseLocalRotation * Quaternion.Euler(0f, shake * 0.22f, shake * 0.65f);
            animatedRoot.localPosition = baseLocalPosition + new Vector3(shake * 0.008f, 0f, shake * 0.006f);

            yield return null;
        }

        animatedRoot.localPosition = baseLocalPosition;
        animatedRoot.localRotation = baseLocalRotation;
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

        FPSScreenShake.Shake(0.018f, 0.1f);

        Vector3 baseLocalPosition = animatedRoot.localPosition;
        Quaternion baseLocalRotation = animatedRoot.localRotation;
        Vector3 baseLocalScale = animatedRoot.localScale;
        Quaternion lidBaseRotation = lidTransform != null ? lidTransform.localRotation : Quaternion.identity;
        float visualIntensity = GetOpenVisualIntensity(chestTransform);
        float lidOpenAngle = LidOpenAngle * visualIntensity;
        float popScalePeak = Mathf.Lerp(1f, PopScalePeak, visualIntensity);
        float popScaleXZPeak = Mathf.Lerp(1f, PopScaleXZPeak, visualIntensity);
        float squashDepth = Mathf.Lerp(1f, 0.94f, visualIntensity);
        Vector3 cameraAwayOffset = GetCameraAwayLocalOffset(chestTransform, animatedRoot) * visualIntensity;

        float elapsed = 0f;

        while (elapsed < OpeningDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / OpeningDuration);
            float decay = 1f - progress;

            float wobbleX = Mathf.Sin(progress * 34f) * decay * 1.5f * visualIntensity;
            float wobbleZ = Mathf.Sin(progress * 27f + 0.6f) * decay * 1.7f * visualIntensity;
            animatedRoot.localRotation = baseLocalRotation * Quaternion.Euler(wobbleX, 0f, wobbleZ);

            float awayStrength = progress < 0.72f
                ? Mathf.Sin(progress / 0.72f * Mathf.PI * 0.5f)
                : 1f - ((progress - 0.72f) / 0.28f);
            animatedRoot.localPosition = baseLocalPosition
                + new Vector3(Mathf.Sin(progress * 41f) * 0.014f * decay, -0.05f * awayStrength * visualIntensity, 0f)
                + cameraAwayOffset * awayStrength;

            float scaleY;

            if (progress < 0.42f)
            {
                scaleY = Mathf.Lerp(1f, squashDepth, progress / 0.42f);
            }
            else if (progress < 0.72f)
            {
                scaleY = Mathf.Lerp(squashDepth, popScalePeak, (progress - 0.42f) / 0.3f);
            }
            else
            {
                scaleY = Mathf.Lerp(popScalePeak * 0.98f, 1f, (progress - 0.72f) / 0.28f);
            }

            float scaleXZ = Mathf.Lerp(1f, popScaleXZPeak, (1f - scaleY) * visualIntensity);
            animatedRoot.localScale = new Vector3(
                baseLocalScale.x * scaleXZ,
                baseLocalScale.y * scaleY,
                baseLocalScale.z * scaleXZ);

            if (lidTransform != null)
            {
                float lidProgress = Mathf.Clamp01((progress - 0.08f) / 0.74f);
                float lidEase = lidProgress * lidProgress * (3f - 2f * lidProgress);
                lidTransform.localRotation = lidBaseRotation * Quaternion.Euler(lidOpenAngle * lidEase, 0f, 0f);
            }

            yield return null;
        }

        animatedRoot.localPosition = baseLocalPosition;
        animatedRoot.localRotation = baseLocalRotation;
        animatedRoot.localScale = baseLocalScale;
    }

    public static void ApplyRevealOcclusionSoftening(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return;
        }

        Transform animatedRoot = ResolveAnimatedRoot(chestTransform);

        if (animatedRoot == null)
        {
            return;
        }

        float visualIntensity = GetOpenVisualIntensity(chestTransform);
        float softenScale = Mathf.Lerp(1f, RevealSoftScale, visualIntensity);
        animatedRoot.localScale = Vector3.one * softenScale;
        animatedRoot.localPosition += new Vector3(0f, RevealSoftDropLocalY * visualIntensity, 0f);
    }

    private static float GetOpenVisualIntensity(Transform chestTransform)
    {
        Camera camera = Camera.main;

        if (camera == null || chestTransform == null)
        {
            return 1f;
        }

        float distance = Vector3.Distance(camera.transform.position, chestTransform.position);

        if (distance >= FarDistance)
        {
            return 1f;
        }

        if (distance <= CloseDistance)
        {
            return MinVisualIntensity;
        }

        return Mathf.Lerp(MinVisualIntensity, 1f, (distance - CloseDistance) / (FarDistance - CloseDistance));
    }

    private static Vector3 GetCameraAwayLocalOffset(Transform chestTransform, Transform animatedRoot)
    {
        Camera camera = Camera.main;

        if (camera == null || chestTransform == null || animatedRoot == null)
        {
            return Vector3.zero;
        }

        Vector3 awayDirection = chestTransform.position - camera.transform.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.0001f)
        {
            awayDirection = -camera.transform.forward;
            awayDirection.y = 0f;
        }

        awayDirection.Normalize();
        Vector3 worldOffset = awayDirection * 0.12f + Vector3.down * 0.04f;
        return animatedRoot.parent != null
            ? animatedRoot.parent.InverseTransformVector(worldOffset)
            : worldOffset;
    }

    private static Transform ResolveAnimatedRoot(Transform chestTransform)
    {
        if (chestTransform == null)
        {
            return null;
        }

        Transform visualRoot = chestTransform.Find("ChestVisualRoot");

        return visualRoot != null ? visualRoot : null;
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
