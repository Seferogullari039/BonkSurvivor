using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AnimatedSkeletonCurveBindingAudit
{
    private const string SkeletonPath = "Assets/Art/Characters/Enemies/AnimatedSkeleton/skeleton.fbx";
    private const int MaxBindingLogCount = 50;
    private const int MaxPathLogCount = 100;
    private const int MaxMissingPathLogCount = 30;
    private const int MaxBonePathLogCount = 30;
    private const float ConstantCurveTolerance = 0.0001f;

    public static void RunCurveBindingAudit()
    {
        AnimatedSkeletonClipAudit.EnsureSkeletonImportForPlayback();

        AnimatedSkeletonCurveBindingAuditContextHolder.LastSampleRootMoved = 0;

        AuditContext context = new AuditContext();
        StringBuilder report = new StringBuilder(4096);

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 1: Curve binding audit ===");
        AuditClipBindings("Idle", MatchIdleClip, context, report);
        AuditClipBindings("Run", MatchRunClip, context, report);
        AuditClipBindings("Attack", MatchAttackClip, context, report);

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 2: Raw FBX hierarchy audit ===");
        GameObject instance = AuditRawHierarchy(context, report);

        if (instance != null)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 3: Binding path vs hierarchy ===");
            AuditPathMatching(instance, context, report);

            UnityEngine.Object.DestroyImmediate(instance);
            instance = null;
        }

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 4: Sample root variation ===");
        AnimatedSkeletonBindingProof.RunSampleRootVariationProof();

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 5: Direct curve value test ===");
        AuditRunCurveValues(context, report);

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 6: Import audit ===");
        AuditImportSettings(report);

        context.SampleRootMovedTransforms = AnimatedSkeletonCurveBindingAuditContextHolder.LastSampleRootMoved;

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] === Stage 7: Decision report ===");
        AppendDecisionReport(context, report);

        Debug.Log(report.ToString());
    }

    private static void AuditClipBindings(string label, Func<AnimationClip, bool> matcher, AuditContext context, StringBuilder report)
    {
        AnimationClip clip = FindClip(matcher);
        if (clip == null)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] clip=" + label + " MISSING.");
            Debug.LogWarning("[AnimatedSkeletonCurveBindingAudit] clip=" + label + " missing.");
            return;
        }

        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

        ClipAuditStats stats = CountCurveTypes(curveBindings);
        bool curvesVary = ClipHasVaryingCurves(clip);
        if (label == "Run")
        {
            context.RunCurveBindings = curveBindings.Length;
            context.RunPosCurves = stats.PositionCurves;
            context.RunRotCurves = stats.RotationCurves;
            context.RunCurvesVary = curvesVary;
        }
        else if (label == "Attack")
        {
            context.AttackCurveBindings = curveBindings.Length;
            context.AttackPosCurves = stats.PositionCurves;
            context.AttackRotCurves = stats.RotationCurves;
            context.AttackCurvesVary = curvesVary;
        }

        string summary = "[AnimatedSkeletonCurveBindingAudit] clip=" + label
            + " length=" + clip.length.ToString("F3")
            + " frameRate=" + clip.frameRate.ToString("F1")
            + " curveBindings=" + curveBindings.Length
            + " objectBindings=" + objectBindings.Length;

        report.AppendLine(summary);
        Debug.Log(summary);

        string typeSummary = "[AnimatedSkeletonCurveBindingAudit] clip=" + label
            + " posCurves=" + stats.PositionCurves
            + " rotCurves=" + stats.RotationCurves
            + " eulerCurves=" + stats.EulerCurves
            + " scaleCurves=" + stats.ScaleCurves
            + " blendshapeCurves=" + stats.BlendShapeCurves;

        report.AppendLine(typeSummary);
        Debug.Log(typeSummary);

        int bindingLogCount = Mathf.Min(MaxBindingLogCount, curveBindings.Length);
        for (int i = 0; i < bindingLogCount; i++)
        {
            EditorCurveBinding binding = curveBindings[i];
            string bindingLine = "[AnimatedSkeletonCurveBindingAudit] binding path='"
                + binding.path
                + "' property='" + binding.propertyName
                + "' type='" + binding.type + "'";

            report.AppendLine(bindingLine);
            Debug.Log(bindingLine);
        }
    }

    private static GameObject AuditRawHierarchy(AuditContext context, StringBuilder report)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        if (source == null)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] skeleton.fbx source missing.");
            Debug.LogError("[AnimatedSkeletonCurveBindingAudit] skeleton.fbx source missing.");
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(source);
        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.name = "AnimatedSkeletonCurveBindingAudit_Instance";

        Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
        SkinnedMeshRenderer[] skinnedRenderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        MeshRenderer[] meshRenderers = instance.GetComponentsInChildren<MeshRenderer>(true);
        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);

        context.TransformCount = transforms.Length;
        context.SkinnedMeshRendererCount = skinnedRenderers.Length;
        context.AnimatorCount = animators.Length;

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] transformCount=" + transforms.Length);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] skinnedMeshRendererCount=" + skinnedRenderers.Length);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] meshRendererCount=" + meshRenderers.Length);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] animatorCount=" + animators.Length);

        Debug.Log("[AnimatedSkeletonCurveBindingAudit] transformCount=" + transforms.Length
            + " skinnedMeshRendererCount=" + skinnedRenderers.Length
            + " meshRendererCount=" + meshRenderers.Length
            + " animatorCount=" + animators.Length);

        if (skinnedRenderers.Length == 0)
        {
            string warning = "[AnimatedSkeletonCurveBindingAudit] WARNING: SkinnedMeshRenderer count is 0. Mesh may not be bound to bones.";
            report.AppendLine(warning);
            Debug.LogWarning(warning);
        }
        else
        {
            SkinnedMeshRenderer renderer = skinnedRenderers[0];
            string rootBonePath = renderer.rootBone != null
                ? GetRelativePath(instance.transform, renderer.rootBone)
                : "null";

            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] rootBone path='" + rootBonePath + "'");
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] bones count=" + renderer.bones.Length);
            Debug.Log("[AnimatedSkeletonCurveBindingAudit] rootBone path='" + rootBonePath
                + "' bones count=" + renderer.bones.Length);

            int boneLogCount = Mathf.Min(MaxBonePathLogCount, renderer.bones.Length);
            for (int i = 0; i < boneLogCount; i++)
            {
                Transform bone = renderer.bones[i];
                if (bone == null)
                {
                    continue;
                }

                string bonePath = GetRelativePath(instance.transform, bone);
                report.AppendLine("[AnimatedSkeletonCurveBindingAudit] bone path='" + bonePath + "'");
                Debug.Log("[AnimatedSkeletonCurveBindingAudit] bone path='" + bonePath + "'");
            }
        }

        int pathLogCount = Mathf.Min(MaxPathLogCount, transforms.Length);
        for (int i = 0; i < pathLogCount; i++)
        {
            if (transforms[i] == null)
            {
                continue;
            }

            string path = GetRelativePath(instance.transform, transforms[i]);
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] hierarchy path='" + path + "'");
            Debug.Log("[AnimatedSkeletonCurveBindingAudit] hierarchy path='" + path + "'");
        }

        return instance;
    }

    private static void AuditPathMatching(GameObject instance, AuditContext context, StringBuilder report)
    {
        HashSet<string> hierarchyPaths = BuildHierarchyPathSet(instance.transform);

        AuditClipPathMatch("Run", MatchRunClip, hierarchyPaths, context, report);
        AuditClipPathMatch("Attack", MatchAttackClip, hierarchyPaths, context, report);
    }

    private static void AuditClipPathMatch(
        string label,
        Func<AnimationClip, bool> matcher,
        HashSet<string> hierarchyPaths,
        AuditContext context,
        StringBuilder report)
    {
        AnimationClip clip = FindClip(matcher);
        if (clip == null)
        {
            return;
        }

        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
        HashSet<string> bindingPaths = new HashSet<string>();

        for (int i = 0; i < curveBindings.Length; i++)
        {
            bindingPaths.Add(curveBindings[i].path);
        }

        int matched = 0;
        List<string> missingPaths = new List<string>();

        foreach (string path in bindingPaths)
        {
            if (HierarchyContainsPath(hierarchyPaths, path))
            {
                matched++;
            }
            else
            {
                missingPaths.Add(path);
            }
        }

        if (label == "Run")
        {
            context.RunPathTotal = bindingPaths.Count;
            context.RunPathMatched = matched;
            context.RunPathMissing = missingPaths.Count;
        }

        string summary = "[AnimatedSkeletonCurveBindingAudit] pathMatch clip=" + label
            + " totalPaths=" + bindingPaths.Count
            + " matched=" + matched
            + " missing=" + missingPaths.Count;

        report.AppendLine(summary);
        Debug.Log(summary);

        int missingLogCount = Mathf.Min(MaxMissingPathLogCount, missingPaths.Count);
        for (int i = 0; i < missingLogCount; i++)
        {
            string missingLine = "[AnimatedSkeletonCurveBindingAudit] missing path='" + missingPaths[i] + "'";
            report.AppendLine(missingLine);
            Debug.Log(missingLine);
        }
    }

    private static void AuditRunCurveValues(AuditContext context, StringBuilder report)
    {
        AnimationClip runClip = FindClip(MatchRunClip);
        if (runClip == null)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] Run clip missing for curve value test.");
            return;
        }

        EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(runClip);
        if (curveBindings.Length == 0)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] Run clip has curveBindings=0. No curve value test possible.");
            context.RunCurvesVary = false;
            return;
        }

        EditorCurveBinding? selectedBinding = null;
        for (int i = 0; i < curveBindings.Length; i++)
        {
            string propertyName = curveBindings[i].propertyName;
            if (propertyName.StartsWith("m_LocalRotation") || propertyName.StartsWith("m_LocalPosition"))
            {
                selectedBinding = curveBindings[i];
                break;
            }
        }

        if (selectedBinding == null)
        {
            selectedBinding = curveBindings[0];
        }

        EditorCurveBinding binding = selectedBinding.Value;
        AnimationCurve curve = AnimationUtility.GetEditorCurve(runClip, binding);
        if (curve == null || curve.length == 0)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] Selected Run curve has no keys.");
            context.RunCurvesVary = false;
            return;
        }

        int midIndex = curve.length / 2;
        int lastIndex = curve.length - 1;

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] curveTest clip=Run path='" + binding.path
            + "' property='" + binding.propertyName
            + "' keys=" + curve.length);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] curveTest key0 time="
            + curve.keys[0].time.ToString("F4")
            + " value=" + curve.keys[0].value.ToString("F6"));
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] curveTest mid time="
            + curve.keys[midIndex].time.ToString("F4")
            + " value=" + curve.keys[midIndex].value.ToString("F6"));
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] curveTest last time="
            + curve.keys[lastIndex].time.ToString("F4")
            + " value=" + curve.keys[lastIndex].value.ToString("F6"));

        Debug.Log("[AnimatedSkeletonCurveBindingAudit] curveTest clip=Run path='" + binding.path
            + "' property='" + binding.propertyName
            + "' keys=" + curve.length
            + " key0=" + curve.keys[0].time.ToString("F4") + "/" + curve.keys[0].value.ToString("F6")
            + " mid=" + curve.keys[midIndex].time.ToString("F4") + "/" + curve.keys[midIndex].value.ToString("F6")
            + " last=" + curve.keys[lastIndex].time.ToString("F4") + "/" + curve.keys[lastIndex].value.ToString("F6"));

        context.RunCurvesVary = !IsCurveConstant(curve);
    }

    private static void AuditImportSettings(StringBuilder report)
    {
        ModelImporter importer = AssetImporter.GetAtPath(SkeletonPath) as ModelImporter;
        if (importer == null)
        {
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] ModelImporter missing.");
            return;
        }

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] animationType=" + importer.animationType);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] importAnimation=" + importer.importAnimation);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] avatarSetup=" + importer.avatarSetup);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] optimizeGameObjects=" + importer.optimizeGameObjects);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] importCameras=" + importer.importCameras);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] importLights=" + importer.importLights);

        ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations ?? Array.Empty<ModelImporterClipAnimation>();
        ModelImporterClipAnimation[] defaultClipAnimations = importer.defaultClipAnimations ?? Array.Empty<ModelImporterClipAnimation>();

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] clipAnimations length=" + clipAnimations.Length);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] defaultClipAnimations length=" + defaultClipAnimations.Length);

        for (int i = 0; i < clipAnimations.Length; i++)
        {
            ModelImporterClipAnimation clip = clipAnimations[i];
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] clipAnimations[" + i + "] name='"
                + clip.name + "' takeName='" + clip.takeName
                + "' firstFrame=" + clip.firstFrame
                + " lastFrame=" + clip.lastFrame
                + " loopTime=" + clip.loopTime + "'");

            ModelImporterClipAnimation defaultClip = FindDefaultClip(defaultClipAnimations, clip.name, clip.takeName);
            if (defaultClip.name != null
                && clip.firstFrame == 0
                && clip.lastFrame == 0
                && (defaultClip.lastFrame > defaultClip.firstFrame))
            {
                string mismatch = "[AnimatedSkeletonCurveBindingAudit] WARNING: clipAnimations '"
                    + clip.name
                    + "' has zero frame range but defaultClipAnimations shows firstFrame="
                    + defaultClip.firstFrame
                    + " lastFrame="
                    + defaultClip.lastFrame
                    + ". Fix tool may need to rewrite clipAnimations from defaults.";
                report.AppendLine(mismatch);
                Debug.LogWarning(mismatch);
            }
        }

        for (int i = 0; i < defaultClipAnimations.Length; i++)
        {
            ModelImporterClipAnimation clip = defaultClipAnimations[i];
            report.AppendLine("[AnimatedSkeletonCurveBindingAudit] defaultClipAnimations[" + i + "] name='"
                + clip.name + "' takeName='" + clip.takeName
                + "' firstFrame=" + clip.firstFrame
                + " lastFrame=" + clip.lastFrame
                + " loopTime=" + clip.loopTime + "'");
        }

        TakeInfo[] takeInfos = importer.importedTakeInfos;
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] importedTakeInfos length=" + (takeInfos != null ? takeInfos.Length : 0));
        if (takeInfos != null)
        {
            for (int i = 0; i < takeInfos.Length; i++)
            {
                report.AppendLine("[AnimatedSkeletonCurveBindingAudit] takeInfo[" + i + "] name='"
                    + takeInfos[i].name + "'");
            }
        }
    }

    private static ModelImporterClipAnimation FindDefaultClip(
        ModelImporterClipAnimation[] defaultClipAnimations,
        string name,
        string takeName)
    {
        for (int i = 0; i < defaultClipAnimations.Length; i++)
        {
            ModelImporterClipAnimation clip = defaultClipAnimations[i];
            if (clip.name == name || clip.takeName == takeName)
            {
                return clip;
            }
        }

        return default;
    }

    private static void AppendDecisionReport(AuditContext context, StringBuilder report)
    {
        string verdict;
        if (context.RunCurveBindings == 0 && context.AttackCurveBindings == 0)
        {
            verdict = "Case A: curveBindings=0 -> FBX clipleri Unity'de bos. Blender'dan animasyon bake/re-export veya yeni animated skeleton asset gerekli.";
        }
        else if (!context.RunCurvesVary && !context.AttackCurvesVary)
        {
            verdict = "Case B: curveBindings>0 ama curve degerleri sabit -> clipler var ama hareket verisi yok/bake edilmemis.";
        }
        else if (context.RunPathMatched == 0 && context.RunPathTotal > 0)
        {
            verdict = "Case C: curveBindings>0, curves degisiyor, path matched=0 -> root/path mismatch. Dogru sample root veya prefab hiyerarsisi bulunmali.";
        }
        else if (context.SampleRootMovedTransforms > 0)
        {
            verdict = "Case E: sampleRoot movedTransforms>0 -> asset calisiyor. Sonraki gorevde Tank prefab binding dogru root'a gore duzeltilecek.";
        }
        else if (context.RunPathMatched > 0)
        {
            verdict = "Case D: curveBindings>0, curves degisiyor, path matched>0 ama movedTransforms=0 -> SampleAnimation root/tool bug olabilir; sample root varyasyon sonuclarina bak.";
        }
        else
        {
            verdict = "Case unknown: ek inceleme gerekli.";
        }

        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] VERDICT: " + verdict);
        report.AppendLine("[AnimatedSkeletonCurveBindingAudit] summary runCurveBindings=" + context.RunCurveBindings
            + " attackCurveBindings=" + context.AttackCurveBindings
            + " runPosCurves=" + context.RunPosCurves
            + " runRotCurves=" + context.RunRotCurves
            + " attackPosCurves=" + context.AttackPosCurves
            + " attackRotCurves=" + context.AttackRotCurves
            + " skinnedMeshRendererCount=" + context.SkinnedMeshRendererCount
            + " animatorCount=" + context.AnimatorCount
            + " runPathMatched=" + context.RunPathMatched + "/" + context.RunPathTotal
            + " runPathMissing=" + context.RunPathMissing
            + " sampleRootMovedTransforms=" + context.SampleRootMovedTransforms);

        Debug.Log("[AnimatedSkeletonCurveBindingAudit] VERDICT: " + verdict);
    }

    private static ClipAuditStats CountCurveTypes(EditorCurveBinding[] curveBindings)
    {
        ClipAuditStats stats = new ClipAuditStats();

        for (int i = 0; i < curveBindings.Length; i++)
        {
            EditorCurveBinding binding = curveBindings[i];
            string propertyName = binding.propertyName;

            if (propertyName.StartsWith("m_LocalPosition"))
            {
                stats.PositionCurves++;
            }
            else if (propertyName.StartsWith("m_LocalRotation"))
            {
                stats.RotationCurves++;
            }
            else if (propertyName.Contains("localEulerAngles"))
            {
                stats.EulerCurves++;
            }
            else if (propertyName.StartsWith("m_LocalScale"))
            {
                stats.ScaleCurves++;
            }
            else if (propertyName.Contains("blendShape"))
            {
                stats.BlendShapeCurves++;
            }
        }

        return stats;
    }

    private static bool ClipHasVaryingCurves(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, bindings[i]);
            if (!IsCurveConstant(curve))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCurveConstant(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
        {
            return true;
        }

        float firstValue = curve.keys[0].value;
        for (int i = 1; i < curve.length; i++)
        {
            if (Mathf.Abs(curve.keys[i].value - firstValue) > ConstantCurveTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static HashSet<string> BuildHierarchyPathSet(Transform root)
    {
        HashSet<string> paths = new HashSet<string>();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] == null)
            {
                continue;
            }

            paths.Add(GetRelativePath(root, transforms[i]));
        }

        return paths;
    }

    private static bool HierarchyContainsPath(HashSet<string> hierarchyPaths, string bindingPath)
    {
        if (string.IsNullOrEmpty(bindingPath))
        {
            return hierarchyPaths.Contains(string.Empty) || hierarchyPaths.Contains("");
        }

        return hierarchyPaths.Contains(bindingPath);
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (target == null)
        {
            return "null";
        }

        if (target == root)
        {
            return string.Empty;
        }

        List<string> parts = new List<string>(8);
        Transform current = target;
        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private static AnimationClip FindClip(Func<AnimationClip, bool> matcher)
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

    private static bool MatchIdleClip(AnimationClip clip)
    {
        return clip.name.ToLowerInvariant().Contains("idle");
    }

    private static bool MatchRunClip(AnimationClip clip)
    {
        return clip.name.ToLowerInvariant().Contains("run");
    }

    private static bool MatchAttackClip(AnimationClip clip)
    {
        return clip.name.ToLowerInvariant().Contains("attack");
    }

    private sealed class AuditContext
    {
        public int RunCurveBindings;
        public int AttackCurveBindings;
        public int RunPosCurves;
        public int RunRotCurves;
        public int AttackPosCurves;
        public int AttackRotCurves;
        public bool RunCurvesVary;
        public bool AttackCurvesVary;
        public int TransformCount;
        public int SkinnedMeshRendererCount;
        public int AnimatorCount;
        public int RunPathTotal;
        public int RunPathMatched;
        public int RunPathMissing;
        public int SampleRootMovedTransforms;
    }

    private struct ClipAuditStats
    {
        public int PositionCurves;
        public int RotationCurves;
        public int EulerCurves;
        public int ScaleCurves;
        public int BlendShapeCurves;
    }
}

internal static class AnimatedSkeletonCurveBindingAuditContextHolder
{
    public static int LastSampleRootMoved;
}
