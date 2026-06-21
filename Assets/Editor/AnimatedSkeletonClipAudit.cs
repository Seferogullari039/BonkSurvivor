using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatedSkeletonClipAudit
{
    private const string SkeletonPath = "Assets/Art/Characters/Enemies/AnimatedSkeleton/skeleton.fbx";
    private const string ControllerPath = "Assets/Animations/Enemies/AnimatedSkeleton_Tank.controller";
    private const string TankViewPrefabPath = "Assets/Prefabs/Enemies/TankEnemy_View.prefab";

    [MenuItem("Tools/BonkSurvivor/Audit Animated Skeleton Clips", false, 30)]
    public static void AuditAnimatedSkeletonClips()
    {
        Debug.Log(BuildAuditReport());
    }

    [MenuItem("Tools/BonkSurvivor/Fix Tank Skeleton Playback", false, 31)]
    public static void FixTankSkeletonPlayback()
    {
        EnsureSkeletonImportSettings();
        AnimationClip idleClip = null;
        AnimationClip runClip = null;
        AnimationClip attackClip = null;
        Avatar avatar = null;
        CollectSkeletonAssets(out idleClip, out runClip, out attackClip, out avatar, out _);

        if (avatar == null)
        {
            Debug.LogError("[AnimatedSkeletonClipAudit] Avatar missing on skeleton.fbx import. Fix import settings and reimport.");
            return;
        }

        RebindControllerMotions(idleClip, runClip, attackClip);
        FixTankViewPrefabAvatar(avatar);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[AnimatedSkeletonClipAudit] Tank skeleton playback fix applied. Re-run audit to verify.");
        AuditAnimatedSkeletonClips();
    }

    public static string BuildAuditReport()
    {
        StringBuilder report = new StringBuilder(2048);
        report.AppendLine("[AnimatedSkeletonClipAudit] === skeleton.fbx audit ===");

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SkeletonPath);
        report.AppendLine("LoadAllAssetsAtPath count=" + subAssets.Length);

        ModelImporter importer = AssetImporter.GetAtPath(SkeletonPath) as ModelImporter;
        if (importer != null)
        {
            report.AppendLine("animationType=" + importer.animationType);
            report.AppendLine("avatarSetup=" + importer.avatarSetup);
            report.AppendLine("optimizeGameObjects=" + importer.optimizeGameObjects);
            report.AppendLine("importAnimation=" + importer.importAnimation);
        }
        else
        {
            report.AppendLine("ModelImporter missing.");
        }

        AnimationClip idleClip = null;
        AnimationClip runClip = null;
        AnimationClip attackClip = null;
        Avatar avatar = null;
        CollectSkeletonAssets(out idleClip, out runClip, out attackClip, out avatar, out int clipCount);

        report.AppendLine("AnimationClip count=" + clipCount);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is AnimationClip clip && !clip.name.StartsWith("__preview"))
            {
                report.AppendLine("  clip name='" + clip.name + "' length=" + clip.length.ToString("F3")
                    + " legacy=" + clip.legacy + " loop=" + clip.isLooping);
            }
        }

        report.AppendLine("idleClip=" + FormatClip(idleClip));
        report.AppendLine("runClip=" + FormatClip(runClip));
        report.AppendLine("attackClip=" + FormatClip(attackClip));
        report.AppendLine("avatar=" + (avatar != null ? avatar.name : "null")
            + " valid=" + (avatar != null && avatar.isValid));

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        if (source != null)
        {
            Animator sourceAnimator = source.GetComponent<Animator>();
            SkinnedMeshRenderer[] renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            report.AppendLine("sourceRoot=" + source.name);
            report.AppendLine("sourceAnimator=" + (sourceAnimator != null));
            report.AppendLine("sourceAnimator.avatar valid="
                + (sourceAnimator != null && sourceAnimator.avatar != null && sourceAnimator.avatar.isValid));
            report.AppendLine("skinnedMeshRendererCount=" + renderers.Length);
            if (renderers.Length > 0 && renderers[0] != null)
            {
                report.AppendLine("skinnedRootBone=" + (renderers[0].rootBone != null ? renderers[0].rootBone.name : "null"));
            }
        }

        report.AppendLine("=== AnimatedSkeleton_Tank.controller ===");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null)
        {
            ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                Motion motion = states[i].state.motion;
                string motionName = motion != null ? motion.name : "NULL";
                report.AppendLine("  state=" + states[i].state.name + " motion=" + motionName);
            }
        }
        else
        {
            report.AppendLine("Controller missing.");
        }

        report.AppendLine("=== TankEnemy_View.prefab ===");
        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(TankViewPrefabPath);
        if (prefabRoot != null)
        {
            TankAnimatedVisualController[] controllers = prefabRoot.GetComponentsInChildren<TankAnimatedVisualController>(true);
            Animator[] animators = prefabRoot.GetComponentsInChildren<Animator>(true);
            report.AppendLine("TankAnimatedVisualController count=" + controllers.Length);
            report.AppendLine("Animator count=" + animators.Length);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator == null)
                {
                    continue;
                }

                report.AppendLine("  animatorGO=" + animator.gameObject.name
                    + " avatarValid=" + (animator.avatar != null && animator.avatar.isValid)
                    + " controller=" + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null"));
            }
        }

        return report.ToString();
    }

    private static void EnsureSkeletonImportSettings()
    {
        ModelImporter importer = AssetImporter.GetAtPath(SkeletonPath) as ModelImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;

        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (importer.optimizeGameObjects)
        {
            importer.optimizeGameObjects = false;
            changed = true;
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
        }
    }

    private static void CollectSkeletonAssets(
        out AnimationClip idleClip,
        out AnimationClip runClip,
        out AnimationClip attackClip,
        out Avatar avatar,
        out int clipCount)
    {
        idleClip = null;
        runClip = null;
        attackClip = null;
        avatar = null;
        clipCount = 0;

        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SkeletonPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is Avatar foundAvatar)
            {
                avatar = foundAvatar;
            }

            if (subAssets[i] is AnimationClip clip && !clip.name.StartsWith("__preview"))
            {
                clipCount++;
                string lower = clip.name.ToLowerInvariant();

                if (lower.Contains("idle"))
                {
                    idleClip = clip;
                }
                else if (lower.Contains("run"))
                {
                    runClip = clip;
                }
                else if (lower.Contains("attack"))
                {
                    attackClip = clip;
                }
            }
        }
    }

    private static void RebindControllerMotions(AnimationClip idleClip, AnimationClip runClip, AnimationClip attackClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("[AnimatedSkeletonClipAudit] Controller missing at " + ControllerPath);
            return;
        }

        ChildAnimatorState[] states = controller.layers[0].stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            switch (state.name)
            {
                case "Idle":
                    if (idleClip != null)
                    {
                        state.motion = idleClip;
                    }

                    break;
                case "Run":
                    if (runClip != null)
                    {
                        state.motion = runClip;
                    }

                    break;
                case "Attack":
                    if (attackClip != null)
                    {
                        state.motion = attackClip;
                    }

                    break;
            }
        }

        EditorUtility.SetDirty(controller);
    }

    private static void FixTankViewPrefabAvatar(Avatar avatar)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(TankViewPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError("[AnimatedSkeletonClipAudit] Failed to load prefab contents.");
            return;
        }

        try
        {
            TankAnimatedVisualController visualController = prefabRoot.GetComponentInChildren<TankAnimatedVisualController>(true);
            Animator animator = visualController != null
                ? visualController.GetComponent<Animator>() ?? visualController.gameObject.GetComponent<Animator>()
                : prefabRoot.GetComponentInChildren<Animator>(true);

            if (animator == null && visualController != null)
            {
                animator = visualController.gameObject.AddComponent<Animator>();
            }

            if (animator != null)
            {
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            if (visualController != null)
            {
                SerializedObject visualControllerObject = new SerializedObject(visualController);
                SerializedProperty avatarProperty = visualControllerObject.FindProperty("skeletonAvatar");
                if (avatarProperty != null)
                {
                    avatarProperty.objectReferenceValue = avatar;
                }

                visualControllerObject.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, TankViewPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static string FormatClip(AnimationClip clip)
    {
        if (clip == null)
        {
            return "null";
        }

        return clip.name + " length=" + clip.length.ToString("F3") + " loop=" + clip.isLooping;
    }
}
