using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnimatedSkeletonClipFrameRepair
{
    private const string SkeletonPath = "Assets/Art/Characters/Enemies/AnimatedSkeleton/skeleton.fbx";

    private static readonly ClipTarget[] ClipTargets =
    {
        new ClipTarget("Idle", "skeleton-skeleton|idle", true),
        new ClipTarget("Run", "skeleton-skeleton|run", true),
        new ClipTarget("Attack", "skeleton-skeleton|attack", false),
    };

    public static void RepairClipFrameRanges()
    {
        bool repaired = TryRepairClipFrameRangesFromDefaults(
            "[AnimatedSkeletonClipFrameRepair]",
            runFollowUpAudits: true,
            onlyIfNeeded: false);

        if (!repaired)
        {
            Debug.LogError("[AnimatedSkeletonClipFrameRepair] Repair did not complete. See logs above.");
        }
    }

    public static bool TryRepairClipFrameRangesFromDefaults(
        string logPrefix,
        bool runFollowUpAudits,
        bool onlyIfNeeded)
    {
        ModelImporter importer = AssetImporter.GetAtPath(SkeletonPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError(logPrefix + " ModelImporter missing at " + SkeletonPath);
            return false;
        }

        if (onlyIfNeeded && !NeedsFrameRangeRepair(importer))
        {
            return false;
        }

        ModelImporterClipAnimation[] currentClips = importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
        ModelImporterClipAnimation[] defaultClips = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();

        LogClipSet(logPrefix + " before clipAnimations:", currentClips, ClipTargets);

        List<ModelImporterClipAnimation> rebuiltClips = new List<ModelImporterClipAnimation>(ClipTargets.Length);
        for (int i = 0; i < ClipTargets.Length; i++)
        {
            ClipTarget target = ClipTargets[i];
            if (!TryFindDefaultClip(defaultClips, target.TakeName, out ModelImporterClipAnimation defaultClip))
            {
                Debug.LogError(logPrefix + " defaultClipAnimations missing takeName='" + target.TakeName + "'.");
                return false;
            }

            Debug.Log(logPrefix + " default: name='" + defaultClip.name
                + "' takeName='" + defaultClip.takeName
                + "' first=" + defaultClip.firstFrame
                + " last=" + defaultClip.lastFrame);

            if (defaultClip.lastFrame <= defaultClip.firstFrame)
            {
                Debug.LogError(logPrefix + " Invalid default frame range for takeName='"
                    + target.TakeName
                    + "' first=" + defaultClip.firstFrame
                    + " last=" + defaultClip.lastFrame);
                return false;
            }

            rebuiltClips.Add(BuildClipFromDefault(target.OutputName, target.TakeName, target.LoopTime, defaultClip));
        }

        importer.clipAnimations = rebuiltClips.ToArray();
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.optimizeGameObjects = false;

        AssetDatabase.WriteImportSettingsIfDirty(SkeletonPath);
        AssetDatabase.ImportAsset(SkeletonPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        LogClipSet(logPrefix + " after:", importer.clipAnimations, ClipTargets);

        LogPostRepairSummary(logPrefix);

        if (runFollowUpAudits)
        {
            Debug.Log(logPrefix + " Running post-repair curve audit...");
            AnimatedSkeletonCurveBindingAudit.RunCurveBindingAudit();
            Debug.Log(logPrefix + " Running post-repair binding proof...");
            AnimatedSkeletonBindingProof.RunProof();
        }

        return true;
    }

    public static bool NeedsFrameRangeRepair(ModelImporter importer)
    {
        if (importer == null)
        {
            return false;
        }

        ModelImporterClipAnimation[] currentClips = importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
        if (currentClips.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < ClipTargets.Length; i++)
        {
            ClipTarget target = ClipTargets[i];
            if (!TryFindConfiguredClip(currentClips, target.OutputName, target.TakeName, out ModelImporterClipAnimation currentClip))
            {
                return true;
            }

            if (currentClip.lastFrame <= currentClip.firstFrame)
            {
                return true;
            }
        }

        return false;
    }

    private static void LogPostRepairSummary(string logPrefix)
    {
        AnimationClip runClip = FindImportedClip(clip => clip.name.ToLowerInvariant().Contains("run"));
        AnimationClip attackClip = FindImportedClip(clip => clip.name.ToLowerInvariant().Contains("attack"));

        int runCurveBindings = runClip != null ? AnimationUtility.GetCurveBindings(runClip).Length : 0;
        int attackCurveBindings = attackClip != null ? AnimationUtility.GetCurveBindings(attackClip).Length : 0;
        int runRotCurves = CountRotationCurves(runClip);
        int attackRotCurves = CountRotationCurves(attackClip);
        int runMoved = SampleMovedTransforms(runClip, 0.35f);
        int attackMoved = SampleMovedTransforms(attackClip, 0.45f);

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        int skinnedMeshRendererCount = 0;
        int meshRendererCount = 0;

        if (source != null)
        {
            GameObject instance = UnityEngine.Object.Instantiate(source);
            instance.hideFlags = HideFlags.HideAndDontSave;

            try
            {
                skinnedMeshRendererCount = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                meshRendererCount = instance.GetComponentsInChildren<MeshRenderer>(true).Length;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        StringBuilder summary = new StringBuilder(512);
        summary.AppendLine(logPrefix + " post-repair summary:");
        summary.AppendLine("  Run curveBindings=" + runCurveBindings + " rotCurves=" + runRotCurves + " movedTransforms=" + runMoved);
        summary.AppendLine("  Attack curveBindings=" + attackCurveBindings + " rotCurves=" + attackRotCurves + " movedTransforms=" + attackMoved);
        summary.AppendLine("  skinnedMeshRendererCount=" + skinnedMeshRendererCount + " meshRendererCount=" + meshRendererCount);

        if (runCurveBindings == 0 && attackCurveBindings == 0)
        {
            summary.AppendLine(logPrefix + " RESULT: defaultClipAnimations frame range dogru gorunse bile Unity curve import etmiyor. Bu FBX animasyonlari kullanilabilir curve uretmiyor. Blender'dan bake/re-export veya yeni animated skeleton asset gerekli.");
        }
        else if (runCurveBindings > 0 && runMoved > 0)
        {
            summary.AppendLine(logPrefix + " RESULT: Clip import duzeldi. Sonraki gorev Tank prefab binding/playback.");
        }

        if (skinnedMeshRendererCount == 0)
        {
            summary.AppendLine(logPrefix + " WARNING: Bones hareket etse bile mesh skinned degil, gorunur deformasyon olmayabilir. Bu durumda Blender'dan dogru skinned mesh export gerekir.");
        }

        Debug.Log(summary.ToString());
    }

    private static int CountRotationCurves(AnimationClip clip)
    {
        if (clip == null)
        {
            return 0;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        int count = 0;

        for (int i = 0; i < bindings.Length; i++)
        {
            if (bindings[i].propertyName.StartsWith("m_LocalRotation"))
            {
                count++;
            }
        }

        return count;
    }

    private static int SampleMovedTransforms(AnimationClip clip, float sampleRatio)
    {
        if (clip == null)
        {
            return 0;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        if (source == null)
        {
            return 0;
        }

        GameObject instance = UnityEngine.Object.Instantiate(source);
        instance.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
            Vector3[] startPositions = new Vector3[transforms.Length];
            Quaternion[] startRotations = new Quaternion[transforms.Length];

            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == null)
                {
                    continue;
                }

                startPositions[i] = transforms[i].localPosition;
                startRotations[i] = transforms[i].localRotation;
            }

            float sampleTime = clip.length > 0.01f ? clip.length * sampleRatio : sampleRatio;
            clip.SampleAnimation(instance, sampleTime);

            int moved = 0;
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                float positionDelta = Vector3.Distance(transform.localPosition, startPositions[i]);
                float rotationDelta = Quaternion.Angle(transform.localRotation, startRotations[i]);

                if (positionDelta > 0.0001f || rotationDelta > 0.05f)
                {
                    moved++;
                }
            }

            return moved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static AnimationClip FindImportedClip(Func<AnimationClip, bool> matcher)
    {
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SkeletonPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is AnimationClip clip && !clip.name.StartsWith("__preview") && matcher(clip))
            {
                return clip;
            }
        }

        return null;
    }

    private static ModelImporterClipAnimation BuildClipFromDefault(
        string outputName,
        string takeName,
        bool loopTime,
        ModelImporterClipAnimation source)
    {
        return new ModelImporterClipAnimation
        {
            name = outputName,
            takeName = takeName,
            firstFrame = source.firstFrame,
            lastFrame = source.lastFrame,
            loopTime = loopTime,
        };
    }

    private static bool TryFindDefaultClip(
        ModelImporterClipAnimation[] defaultClips,
        string takeName,
        out ModelImporterClipAnimation defaultClip)
    {
        for (int i = 0; i < defaultClips.Length; i++)
        {
            ModelImporterClipAnimation clip = defaultClips[i];
            if (clip.takeName == takeName || clip.name == takeName)
            {
                defaultClip = clip;
                return true;
            }
        }

        defaultClip = default;
        return false;
    }

    private static bool TryFindConfiguredClip(
        ModelImporterClipAnimation[] clips,
        string outputName,
        string takeName,
        out ModelImporterClipAnimation configuredClip)
    {
        for (int i = 0; i < clips.Length; i++)
        {
            ModelImporterClipAnimation clip = clips[i];
            if (clip.name == outputName || clip.takeName == takeName)
            {
                configuredClip = clip;
                return true;
            }
        }

        configuredClip = default;
        return false;
    }

    private static void LogClipSet(string header, ModelImporterClipAnimation[] clips, ClipTarget[] targets)
    {
        Debug.Log(header);

        for (int i = 0; i < targets.Length; i++)
        {
            ClipTarget target = targets[i];
            if (TryFindConfiguredClip(clips, target.OutputName, target.TakeName, out ModelImporterClipAnimation clip))
            {
                string line = "[AnimatedSkeletonClipFrameRepair] name=" + clip.name
                    + " takeName=" + clip.takeName
                    + " first=" + clip.firstFrame
                    + " last=" + clip.lastFrame;

                if (header.IndexOf("after", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    line += " loop=" + clip.loopTime;
                }

                Debug.Log(line);
            }
            else
            {
                Debug.Log("[AnimatedSkeletonClipFrameRepair] name=" + target.OutputName
                    + " takeName=" + target.TakeName
                    + " MISSING");
            }
        }
    }

    private readonly struct ClipTarget
    {
        public readonly string OutputName;
        public readonly string TakeName;
        public readonly bool LoopTime;

        public ClipTarget(string outputName, string takeName, bool loopTime)
        {
            OutputName = outputName;
            TakeName = takeName;
            LoopTime = loopTime;
        }
    }
}
