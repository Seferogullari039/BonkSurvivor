using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatedSkeletonBindingProof
{
    private const string SkeletonPath = "Assets/Art/Characters/Enemies/AnimatedSkeleton/skeleton.fbx";
    private const string ControllerPath = "Assets/Animations/Enemies/AnimatedSkeleton_Tank.controller";
    private const string TankViewPrefabPath = "Assets/Prefabs/Enemies/TankEnemy_View.prefab";

    private const float PositionMoveThreshold = 0.0001f;
    private const float RotationMoveThreshold = 0.05f;

    public static void RunProof()
    {
        AnimatedSkeletonClipAudit.EnsureSkeletonImportForPlayback();

        StringBuilder summary = new StringBuilder(512);
        summary.AppendLine("[AnimatedSkeletonBindingProof] === Stage 1: SampleAnimation bone proof ===");

        int idleMoved = ProveClipSample("IDLE", MatchIdleClip, 0.5f, summary);
        int runMoved = ProveClipSample("RUN", MatchRunClip, 0.35f, summary);
        int attackMoved = ProveClipSample("ATTACK", MatchAttackClip, 0.45f, summary);

        summary.AppendLine("[AnimatedSkeletonBindingProof] Stage1 verdict:");
        summary.AppendLine("  idle movedTransforms=" + idleMoved);
        summary.AppendLine("  run movedTransforms=" + runMoved);
        summary.AppendLine("  attack movedTransforms=" + attackMoved);

        if (runMoved == 0 && attackMoved == 0)
        {
            summary.AppendLine("[AnimatedSkeletonBindingProof] VERDICT: Clip kemikleri hareket ettirmiyor. Sorun Tank prefab degil; import/clip binding.");
            summary.AppendLine("[AnimatedSkeletonBindingProof] Run Tools/BonkSurvivor/Fix Tank Skeleton Playback, then prove again.");
            Debug.Log(summary.ToString());
            return;
        }

        summary.AppendLine("[AnimatedSkeletonBindingProof] VERDICT: Raw clip kemikleri hareket ettiriyor. Stage 2 raw Animator proof calistiriliyor...");
        Debug.Log(summary.ToString());

        ProveRawAnimatorPlayback();
    }

    public static void RepairTankSkeletonPrefabBinding()
    {
        Avatar avatar = FindAvatar();
        if (avatar == null)
        {
            Debug.LogError("[AnimatedSkeletonBindingProof] Avatar missing. Run Fix Tank Skeleton Playback first.");
            return;
        }

        AnimatedSkeletonClipAudit.RebindControllerMotionsFromFbx();
        RepairTankViewPrefabHierarchy(avatar);
        AssetDatabase.SaveAssets();
        Debug.Log("[AnimatedSkeletonBindingProof] Tank prefab binding repair complete.");
        Debug.Log(AnimatedSkeletonClipAudit.BuildAuditReport());
    }

    private static int ProveClipSample(string label, Func<AnimationClip, bool> matcher, float sampleRatio, StringBuilder summary)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        AnimationClip clip = FindClip(matcher);

        if (source == null || clip == null)
        {
            summary.AppendLine("[AnimatedSkeletonBindingProof] clip=" + label + " MISSING source or clip.");
            Debug.LogWarning("[AnimatedSkeletonBindingProof] clip=" + label + " missing.");
            return 0;
        }

        GameObject instance = UnityEngine.Object.Instantiate(source);
        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.name = "AnimatedSkeletonBindingProof_" + label;

        try
        {
            Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
            Dictionary<Transform, AnimatedSkeletonPoseSnapshot> before = CapturePoses(transforms);

            float sampleTime = clip.length > 0.01f ? clip.length * sampleRatio : sampleRatio;
            clip.SampleAnimation(instance, sampleTime);

            List<AnimatedSkeletonMoveRecord> movers = MeasureMovement(transforms, before);
            string topMoved = BuildTopMovedString(movers, 10);

            Debug.Log("[AnimatedSkeletonBindingProof] clip=" + label
                + " name='" + clip.name
                + "' length=" + clip.length.ToString("F3")
                + " movedTransforms=" + movers.Count
                + " topMoved=" + topMoved);

            summary.AppendLine("[AnimatedSkeletonBindingProof] clip=" + label
                + " length=" + clip.length.ToString("F3")
                + " movedTransforms=" + movers.Count);

            return movers.Count;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ProveRawAnimatorPlayback()
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPath);
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        Avatar avatar = FindAvatar();

        if (source == null || controller == null)
        {
            Debug.LogError("[AnimatedSkeletonBindingProof] Raw animator proof missing source or controller.");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (instance == null)
        {
            instance = UnityEngine.Object.Instantiate(source);
        }

        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.name = "AnimatedSkeletonBindingProof_Animator";

        try
        {
            Animator animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
            animator.enabled = true;

            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Transform armature = FindChildByName(instance.transform, "Armature")
                ?? FindChildByName(instance.transform, "armature")
                ?? FindChildByName(instance.transform, "RootNode");

            ProveAnimatorState(animator, "Run", renderers, armature);
            ProveAnimatorState(animator, "Attack", renderers, armature);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ProveAnimatorState(
        Animator animator,
        string stateName,
        SkinnedMeshRenderer[] renderers,
        Transform armature)
    {
        animator.Rebind();
        animator.Update(0f);
        animator.Play(stateName, 0, 0f);
        animator.Update(0.1f);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        string clipInfo = clipInfos.Length > 0 && clipInfos[0].clip != null
            ? clipInfos[0].clip.name + " weight=" + clipInfos[0].weight.ToString("F2")
            : "none";

        int boneCount = animator.transform.GetComponentsInChildren<Transform>(true).Length;

        Debug.Log("[AnimatedSkeletonBindingProof] rawAnimator requested=" + stateName
            + " animatorRoot=" + animator.gameObject.name
            + " avatarNull=" + (animator.avatar == null)
            + " avatarValid=" + (animator.avatar != null && animator.avatar.isValid)
            + " controller=" + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null")
            + " currentState=" + ResolveStateLabel(stateInfo, stateName)
            + " clipInfo=" + clipInfo
            + " normalized=" + stateInfo.normalizedTime.ToString("F2")
            + " skinnedMeshCount=" + renderers.Length
            + " boneCount=" + boneCount
            + " armature=" + (armature != null ? armature.name : "null"));
    }

    private static void RepairTankViewPrefabHierarchy(Avatar avatar)
    {
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(TankViewPrefabPath);

        if (prefabRoot == null)
        {
            Debug.LogError("[AnimatedSkeletonBindingProof] Failed to load TankEnemy_View prefab contents.");
            return;
        }

        try
        {
            Transform visualRoot = prefabRoot.transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                Debug.LogError("[AnimatedSkeletonBindingProof] VisualRoot missing.");
                return;
            }

            Transform existingModel = visualRoot.Find("Model");
            Transform skeletonRoot = existingModel;

            if (existingModel != null && existingModel.Find("skeleton") != null)
            {
                skeletonRoot = existingModel.Find("skeleton");
            }

            if (existingModel == null)
            {
                Debug.LogError("[AnimatedSkeletonBindingProof] Model missing under VisualRoot.");
                return;
            }

            Vector3 wrapperScale = existingModel.localScale;
            Vector3 wrapperPosition = existingModel.localPosition;
            Quaternion wrapperRotation = existingModel.localRotation;

            bool needsWrapperSplit = skeletonRoot == existingModel && existingModel.GetComponent<SkinnedMeshRenderer>() == null;
            if (needsWrapperSplit)
            {
                existingModel.name = "skeleton";
                GameObject wrapper = new GameObject("Model");
                wrapper.transform.SetParent(visualRoot, false);
                wrapper.transform.localPosition = wrapperPosition;
                wrapper.transform.localRotation = wrapperRotation;
                wrapper.transform.localScale = wrapperScale;
                existingModel.SetParent(wrapper.transform, false);
                existingModel.localPosition = Vector3.zero;
                existingModel.localRotation = Quaternion.identity;
                existingModel.localScale = Vector3.one;
                skeletonRoot = existingModel;
            }

            Animator animator = skeletonRoot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = skeletonRoot.gameObject.AddComponent<Animator>();
            }

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;

            TankAnimatedVisualController visualController = skeletonRoot.GetComponent<TankAnimatedVisualController>();
            if (visualController == null)
            {
                visualController = skeletonRoot.gameObject.AddComponent<TankAnimatedVisualController>();
            }

            SerializedObject visualControllerObject = new SerializedObject(visualController);
            SerializedProperty avatarProperty = visualControllerObject.FindProperty("skeletonAvatar");
            SerializedProperty controllerProperty = visualControllerObject.FindProperty("animatorController");
            if (avatarProperty != null)
            {
                avatarProperty.objectReferenceValue = avatar;
            }

            if (controllerProperty != null && controller != null)
            {
                controllerProperty.objectReferenceValue = controller;
            }

            visualControllerObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, TankViewPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static Avatar FindAvatar()
    {
        UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(SkeletonPath);
        for (int i = 0; i < subAssets.Length; i++)
        {
            if (subAssets[i] is Avatar avatar)
            {
                return avatar;
            }
        }

        return null;
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

    private static Dictionary<Transform, AnimatedSkeletonPoseSnapshot> CapturePoses(Transform[] transforms)
    {
        Dictionary<Transform, AnimatedSkeletonPoseSnapshot> poses = new Dictionary<Transform, AnimatedSkeletonPoseSnapshot>(transforms.Length);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform == null)
            {
                continue;
            }

            poses[transform] = new AnimatedSkeletonPoseSnapshot(transform.localPosition, transform.localRotation);
        }

        return poses;
    }

    private static List<AnimatedSkeletonMoveRecord> MeasureMovement(
        Transform[] transforms,
        Dictionary<Transform, AnimatedSkeletonPoseSnapshot> before)
    {
        List<AnimatedSkeletonMoveRecord> movers = new List<AnimatedSkeletonMoveRecord>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform == null || !before.TryGetValue(transform, out AnimatedSkeletonPoseSnapshot start))
            {
                continue;
            }

            float positionDelta = Vector3.Distance(transform.localPosition, start.LocalPosition);
            float rotationDelta = Quaternion.Angle(transform.localRotation, start.LocalRotation);

            if (positionDelta <= PositionMoveThreshold && rotationDelta <= RotationMoveThreshold)
            {
                continue;
            }

            movers.Add(new AnimatedSkeletonMoveRecord(transform.name, positionDelta, rotationDelta));
        }

        movers.Sort((a, b) => b.Score.CompareTo(a.Score));

        return movers;
    }

    private static string BuildTopMovedString(List<AnimatedSkeletonMoveRecord> movers, int maxCount)
    {
        if (movers.Count == 0)
        {
            return "none";
        }

        int count = Mathf.Min(maxCount, movers.Count);
        StringBuilder builder = new StringBuilder(256);

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                builder.Append(" | ");
            }

            AnimatedSkeletonMoveRecord record = movers[i];
            builder.Append(record.Path)
                .Append("(p=")
                .Append(record.PositionDelta.ToString("F4"))
                .Append(",r=")
                .Append(record.RotationDelta.ToString("F2"))
                .Append(")");
        }

        return builder.ToString();
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && transforms[i].name == targetName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static string ResolveStateLabel(AnimatorStateInfo stateInfo, string requested)
    {
        if (stateInfo.IsName(requested))
        {
            return requested;
        }

        return stateInfo.shortNameHash.ToString();
    }

    private readonly struct AnimatedSkeletonPoseSnapshot
    {
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;

        public AnimatedSkeletonPoseSnapshot(Vector3 localPosition, Quaternion localRotation)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
        }
    }

    private readonly struct AnimatedSkeletonMoveRecord
    {
        public readonly string Path;
        public readonly float PositionDelta;
        public readonly float RotationDelta;

        public float Score => PositionDelta + RotationDelta * 0.01f;

        public AnimatedSkeletonMoveRecord(string path, float positionDelta, float rotationDelta)
        {
            Path = path;
            PositionDelta = positionDelta;
            RotationDelta = rotationDelta;
        }
    }
}
