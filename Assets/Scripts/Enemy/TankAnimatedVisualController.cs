using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public class TankAnimatedVisualController : MonoBehaviour
{
    private const string AttackStateName = "Attack";
    private const string IdleStateName = "Idle";
    private const string RunStateName = "Run";
    private const string TankSwordAnchorName = "TankSwordAnchor";
    private const string HandBoneName = "hand.R";
    private const float AttackCrossFadeDuration = 0.05f;
    private const float RunCrossFadeDuration = 0.08f;
    private const float IdleCrossFadeDuration = 0.12f;
    private const float DefaultAttackVisualDuration = 0.9f;
    private const float DefaultSlashVisualDuration = 0.52f;
    private const float DebugForceAttackInterval = 1.2f;
    private static readonly string[] HandBoneRelativePaths =
    {
        "skeleton-skeleton/belly/chest/upperarm.R/lowerarm.R/hand.R",
        "Model/skeleton-skeleton/belly/chest/upperarm.R/lowerarm.R/hand.R",
        "upperarm.R/lowerarm.R/hand.R",
    };
    private static readonly Vector3 SwordRestLocalEuler = Vector3.zero;
    private static readonly Vector3 SwordRaiseLocalEuler = new Vector3(-52f, -8f, 18f);
    private static readonly Vector3 SwordSlashLocalEuler = new Vector3(54f, 16f, 34f);

    [Header("Animator")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Avatar skeletonAvatar;

    [Header("Movement")]
    [SerializeField] private float moveSpeedThreshold = 0.03f;

    [Header("Attack Visual")]
    [SerializeField] private float attackRange = 4f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float slashVisualDuration = DefaultSlashVisualDuration;

    [Header("Procedural Fallback")]
    [SerializeField] private bool fallbackProceduralAnimation = true;
    [SerializeField] private float walkBobAmount = 0.025f;
    [SerializeField] private float walkSwayAngle = 2.5f;
    [SerializeField] private float attackBodyLeanAngle = 10f;

    [Header("Debug")]
    [SerializeField] private bool debugForceAttackLoop = false;
    [SerializeField] private bool debugForceRun = false;
    [SerializeField] private bool debugAnimatorLogs = false;
    [SerializeField] private bool debugAttackLogs = false;
    [SerializeField] private bool debugMeshDiagnosticLogs = false;

    [Header("Sword Visual")]
    [SerializeField] private bool createVisualSword = true;
    [SerializeField] private string preferredHandName = "hand.R";
    [SerializeField] private Material swordMaterial;
    [SerializeField] private Material swordHandleMaterial;
    [SerializeField] private Vector3 swordHandLocalPosition = new Vector3(0.02f, 0f, 0f);
    [SerializeField] private Vector3 swordHandLocalEuler = new Vector3(0f, 90f, -90f);
    [SerializeField] private Vector3 swordFallbackLocalPosition = new Vector3(0.42f, 0.24f, 0.04f);
    [SerializeField] private Vector3 swordFallbackLocalEuler = new Vector3(35f, 25f, -82f);
    [SerializeField] private Vector3 swordFallbackLocalScale = new Vector3(0.88f, 0.88f, 0.88f);

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Animator animator;
    private Transform enemyRoot;
    private Transform bodyMotionTransform;
    private Transform swordMountTransform;
    private Vector3 lastRootPosition;
    private Vector3 bodyRestLocalPosition;
    private Quaternion bodyRestLocalRotation;
    private Vector3 bodyRestLocalScale;
    private float nextAttackTime;
    private float nextDebugForceAttackTime;
    private float nextAnimatorDebugLogTime;
    private float attackEndTime;
    private float attackSlashStartTime;
    private float attackVisualDuration = DefaultAttackVisualDuration;
    private bool isAttacking;
    private bool lastIsMoving;
    private float lastMovementSpeed;
    private string currentVisualState = IdleStateName;
    private string runClipName = RunStateName;
    private bool warnedMissingAnimator;
    private bool warnedMissingTarget;
    private string attackClipName = AttackStateName;
    private bool warnedMissingAvatar;
    private bool loggedSwordFallback;
    private bool loggedSwordParent;
    private bool swordAnchoredToHandBone;
    private GameObject swordVisualRoot;
    private int skinnedMeshRendererCount;
    private int meshRendererCount;
    private Transform boneProbeTransform;
    private Quaternion boneProbeLastRotation;
    private bool bonesMovedLastFrame;
    private bool useProceduralFallback;

    public void ConfigurePlaybackReferences(Avatar avatar, RuntimeAnimatorController controller = null)
    {
        if (avatar != null)
        {
            skeletonAvatar = avatar;
        }

        if (controller != null)
        {
            animatorController = controller;
        }
    }

    private void Awake()
    {
        ResolveAnimatedAnimator();
        enemyRoot = FindEnemyRoot();
        lastRootPosition = enemyRoot != null ? enemyRoot.position : transform.position;
        bodyMotionTransform = transform;

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        SanitizeVisualComponents();
        ConfigureAnimator();
        ApplyBodyMaterial();
        CacheMeshDiagnostics();
        InitBoneProbe();
        ResolveProceduralFallbackMode();
        ResolveAttackClipInfo();
        ResolveRunClipInfo();
        LogMeshDiagnosticOnce();
        CreateVisualSwordIfNeeded();
        CacheBodyRestTransform();
        PlayMovementVisualState(force: true);
    }

    private void ResolveAnimatedAnimator()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);
        Animator best = null;

        for (int i = 0; i < animators.Length; i++)
        {
            Animator candidate = animators[i];

            if (candidate == null)
            {
                continue;
            }

            if (candidate.avatar != null && candidate.avatar.isValid)
            {
                best = candidate;
                break;
            }

            if (best == null)
            {
                best = candidate;
            }
        }

        animator = best ?? GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        UpdateMovementParameter();
        UpdateProceduralVisuals();

        if (debugForceAttackLoop && !isAttacking && Time.time >= nextDebugForceAttackTime)
        {
            nextDebugForceAttackTime = Time.time + DebugForceAttackInterval;
            PlayAttackDirect(null, 0f, "debugForceAttackLoop");
        }
        else if (!isAttacking)
        {
            TryTriggerAttackVisual();
        }

        if (isAttacking && Time.time >= attackEndTime)
        {
            EndAttackVisual();
        }

        UpdateBoneMovementProbe();
        MaybeLogAnimatorDebug();
    }

    private void ResolveProceduralFallbackMode()
    {
        useProceduralFallback = fallbackProceduralAnimation && skinnedMeshRendererCount == 0;

        if (useProceduralFallback)
        {
            attackVisualDuration = slashVisualDuration;
        }
    }

    private void ConfigureAnimator()
    {
        if (animator == null)
        {
            if (!warnedMissingAnimator)
            {
                warnedMissingAnimator = true;
                Debug.LogWarning("[TankAnimatedVisualController] Animator missing on " + name, this);
            }

            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        if (animator.avatar == null && skeletonAvatar != null)
        {
            animator.avatar = skeletonAvatar;
        }

        animator.speed = 1f;
        animator.Rebind();

        if ((animator.avatar == null || !animator.avatar.isValid) && !warnedMissingAvatar)
        {
            warnedMissingAvatar = true;
            Debug.LogWarning("[TankAnimatedVisualController] Animator avatar missing/invalid on "
                + name + ". Run Tools/BonkSurvivor/Fix Tank Skeleton Playback.", this);
        }
    }

    private void ResolveAttackClipInfo()
    {
        attackVisualDuration = useProceduralFallback ? slashVisualDuration : DefaultAttackVisualDuration;
        attackClipName = AttackStateName;

        if (animator == null || animator.runtimeAnimatorController == null || useProceduralFallback)
        {
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip == null)
            {
                continue;
            }

            if (clip.name.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                attackClipName = clip.name;
                attackVisualDuration = clip.length > 0.01f ? clip.length : DefaultAttackVisualDuration;
                return;
            }
        }
    }

    private void ResolveRunClipInfo()
    {
        runClipName = RunStateName;

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];

            if (clip == null)
            {
                continue;
            }

            if (clip.name.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                runClipName = clip.name;
                return;
            }
        }
    }

    private void ApplyBodyMaterial()
    {
        if (bodyMaterial == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer != null)
            {
                renderer.sharedMaterial = bodyMaterial;
            }
        }
    }

    private void UpdateMovementParameter()
    {
        if (isAttacking)
        {
            return;
        }

        if (enemyRoot == null)
        {
            enemyRoot = FindEnemyRoot();
        }

        if (enemyRoot == null)
        {
            lastIsMoving = false;
            lastMovementSpeed = 0f;
            animator.SetBool(IsMovingHash, false);
            PlayMovementVisualState();
            return;
        }

        Vector3 delta = enemyRoot.position - lastRootPosition;
        lastRootPosition = enemyRoot.position;
        delta.y = 0f;

        lastMovementSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastIsMoving = lastMovementSpeed > moveSpeedThreshold;
        animator.SetBool(IsMovingHash, lastIsMoving);
        PlayMovementVisualState();
    }

    private void PlayMovementVisualState(bool force = false)
    {
        if (isAttacking)
        {
            return;
        }

        bool shouldRun = debugForceRun || lastIsMoving;
        string desiredState = shouldRun ? RunStateName : IdleStateName;

        if (!force && currentVisualState == desiredState)
        {
            return;
        }

        float crossFadeDuration = shouldRun ? RunCrossFadeDuration : IdleCrossFadeDuration;
        CrossFadeVisualState(desiredState, crossFadeDuration);
    }

    private void CrossFadeVisualState(string stateName, float duration)
    {
        if (!useProceduralFallback)
        {
            animator.CrossFadeInFixedTime(stateName, duration, 0, 0f);
            animator.Update(0f);
        }

        currentVisualState = stateName;
    }

    private void TryTriggerAttackVisual()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        Transform target = ResolveTargetTransform();

        if (target == null || enemyRoot == null)
        {
            if (!warnedMissingTarget)
            {
                warnedMissingTarget = true;
                Debug.LogWarning("[TankAnimatedVisualController] target missing, attack disabled", this);
            }

            return;
        }

        Vector3 toTarget = target.position - enemyRoot.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance > attackRange)
        {
            return;
        }

        PlayAttackDirect(target, distance);
    }

    private void PlayAttackDirect(Transform target, float distance, string debugTargetName = null)
    {
        if (animator == null)
        {
            return;
        }

        string stateBefore = GetCurrentStateLabel();
        string targetName = debugTargetName ?? (target != null ? target.name : "unknown");

        CrossFadeVisualState(AttackStateName, AttackCrossFadeDuration);
        isAttacking = true;
        attackSlashStartTime = Time.time;
        attackEndTime = Time.time + attackVisualDuration;
        nextAttackTime = Time.time + attackCooldown;

        if (debugAttackLogs)
        {
            Debug.Log("[TankAnimatedVisualController] ATTACK distance=" + distance.ToString("F2")
                + " target=" + targetName
                + " stateBefore=" + stateBefore
                + " clip=" + attackClipName
                + " proceduralSlash=" + useProceduralFallback);
        }
    }

    private void EndAttackVisual()
    {
        isAttacking = false;
        ResetProceduralAttackPose();

        string returnState = (debugForceRun || lastIsMoving) ? RunStateName : IdleStateName;
        float crossFadeDuration = returnState == RunStateName ? RunCrossFadeDuration : IdleCrossFadeDuration;
        CrossFadeVisualState(returnState, crossFadeDuration);
        animator.SetBool(IsMovingHash, lastIsMoving);
    }

    private void UpdateProceduralVisuals()
    {
        if (isAttacking)
        {
            UpdateSwordSlashVisual();
            return;
        }

        ResetSwordToRest();

        if (useProceduralFallback && bodyMotionTransform != null)
        {
            UpdateWalkBobVisual();
        }
    }

    private void UpdateWalkBobVisual()
    {
        bool shouldMove = debugForceRun || lastIsMoving;

        if (!shouldMove)
        {
            bodyMotionTransform.localPosition = bodyRestLocalPosition;
            bodyMotionTransform.localRotation = bodyRestLocalRotation;
            bodyMotionTransform.localScale = bodyRestLocalScale;
            return;
        }

        float bob = Mathf.Sin(Time.time * 9f) * walkBobAmount;
        float sway = Mathf.Sin(Time.time * 4.5f) * walkSwayAngle;
        bodyMotionTransform.localPosition = bodyRestLocalPosition + new Vector3(0f, bob, 0f);
        bodyMotionTransform.localRotation = bodyRestLocalRotation * Quaternion.Euler(0f, 0f, sway);
        bodyMotionTransform.localScale = bodyRestLocalScale;
    }

    private void UpdateSwordSlashVisual()
    {
        if (swordMountTransform == null)
        {
            return;
        }

        float duration = Mathf.Max(slashVisualDuration, 0.01f);
        float normalized = Mathf.Clamp01((Time.time - attackSlashStartTime) / duration);
        const float raiseEnd = 0.22f;
        const float slashEnd = 0.62f;

        Vector3 swordEuler;
        float bodyPitch;
        float scalePulse = 1f;

        if (normalized < raiseEnd)
        {
            float blend = normalized / raiseEnd;
            swordEuler = Vector3.Lerp(SwordRestLocalEuler, SwordRaiseLocalEuler, blend);
            bodyPitch = Mathf.Lerp(0f, -attackBodyLeanAngle * 0.45f, blend);
        }
        else if (normalized < slashEnd)
        {
            float blend = (normalized - raiseEnd) / (slashEnd - raiseEnd);
            swordEuler = Vector3.Lerp(SwordRaiseLocalEuler, SwordSlashLocalEuler, blend);
            bodyPitch = Mathf.Lerp(-attackBodyLeanAngle * 0.45f, attackBodyLeanAngle, blend);
            scalePulse = 1f + 0.025f * Mathf.Sin(blend * Mathf.PI);
        }
        else
        {
            float blend = (normalized - slashEnd) / (1f - slashEnd);
            swordEuler = Vector3.Lerp(SwordSlashLocalEuler, SwordRestLocalEuler, blend);
            bodyPitch = Mathf.Lerp(attackBodyLeanAngle, 0f, blend);
        }

        swordMountTransform.localRotation = Quaternion.Euler(swordEuler);

        if (useProceduralFallback && bodyMotionTransform != null)
        {
            bodyMotionTransform.localRotation = bodyRestLocalRotation * Quaternion.Euler(bodyPitch, 0f, 0f);
            bodyMotionTransform.localScale = bodyRestLocalScale * scalePulse;
        }
    }

    private void ResetSwordToRest()
    {
        if (swordMountTransform == null)
        {
            return;
        }

        swordMountTransform.localRotation = Quaternion.Euler(SwordRestLocalEuler);
    }

    private void ResetProceduralAttackPose()
    {
        if (bodyMotionTransform == null)
        {
            return;
        }

        bodyMotionTransform.localPosition = bodyRestLocalPosition;
        bodyMotionTransform.localRotation = bodyRestLocalRotation;
        bodyMotionTransform.localScale = bodyRestLocalScale;
        ResetSwordToRest();
    }

    private void CacheBodyRestTransform()
    {
        if (bodyMotionTransform == null)
        {
            return;
        }

        bodyRestLocalPosition = bodyMotionTransform.localPosition;
        bodyRestLocalRotation = bodyMotionTransform.localRotation;
        bodyRestLocalScale = bodyMotionTransform.localScale;
    }

    private void MaybeLogAnimatorDebug()
    {
        if (!debugAnimatorLogs || Time.time < nextAnimatorDebugLogTime)
        {
            return;
        }

        nextAnimatorDebugLogTime = Time.time + 1f;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
        string clipInfo = clipInfos.Length > 0 && clipInfos[0].clip != null
            ? clipInfos[0].clip.name
            : "none";

        Debug.Log("[TankAnimatedVisualController] state=" + currentVisualState
            + " clipInfo=" + clipInfo
            + " avatarValid=" + (animator.avatar != null && animator.avatar.isValid)
            + " animatorRoot=" + animator.gameObject.name
            + " moving=" + lastIsMoving
            + " attacking=" + isAttacking
            + " speed=" + lastMovementSpeed.ToString("F2")
            + " forceRun=" + debugForceRun
            + " normalized=" + stateInfo.normalizedTime.ToString("F2")
            + " skinnedMeshRendererCount=" + skinnedMeshRendererCount
            + " meshRendererCount=" + meshRendererCount
            + " bonesMoved=" + bonesMovedLastFrame
            + " visibleMeshLikelySkinned=" + (skinnedMeshRendererCount > 0)
            + " proceduralFallback=" + useProceduralFallback);
    }

    private void CacheMeshDiagnostics()
    {
        skinnedMeshRendererCount = GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
        meshRendererCount = GetComponentsInChildren<MeshRenderer>(true).Length;
    }

    private void InitBoneProbe()
    {
        boneProbeTransform = FindTransformByName(preferredHandName)
            ?? FindTransformByName("spine")
            ?? FindTransformByName("skeleton-skeleton");

        if (boneProbeTransform != null)
        {
            boneProbeLastRotation = boneProbeTransform.localRotation;
        }
    }

    private void UpdateBoneMovementProbe()
    {
        if (boneProbeTransform == null)
        {
            bonesMovedLastFrame = false;
            return;
        }

        float rotationDelta = Quaternion.Angle(boneProbeTransform.localRotation, boneProbeLastRotation);
        bonesMovedLastFrame = rotationDelta > 0.05f;
        boneProbeLastRotation = boneProbeTransform.localRotation;
    }

    private void LogMeshDiagnosticOnce()
    {
        if (debugMeshDiagnosticLogs)
        {
            bool visibleMeshLikelySkinned = skinnedMeshRendererCount > 0;
            Debug.Log("[TankAnimatedVisualController] meshDiagnostic skinnedMeshRendererCount="
                + skinnedMeshRendererCount
                + " meshRendererCount=" + meshRendererCount
                + " visibleMeshLikelySkinned=" + visibleMeshLikelySkinned
                + " proceduralFallback=" + useProceduralFallback
                + " animatorRoot=" + (animator != null ? animator.gameObject.name : "null"));
        }

        if (useProceduralFallback && !loggedSwordFallback)
        {
            loggedSwordFallback = true;
            Debug.Log("[TankAnimatedVisualController] using visual sword slash fallback because SkinnedMeshRenderer=0");
        }
    }

    private string GetCurrentStateLabel()
    {
        return GetStateLabel(animator.GetCurrentAnimatorStateInfo(0));
    }

    private static string GetStateLabel(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName(AttackStateName))
        {
            return AttackStateName;
        }

        if (stateInfo.IsName(RunStateName))
        {
            return RunStateName;
        }

        if (stateInfo.IsName(IdleStateName))
        {
            return IdleStateName;
        }

        return stateInfo.shortNameHash.ToString();
    }

    private void SanitizeVisualComponents()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rigidbody = rigidbodies[i];

            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }
    }

    private void CreateVisualSwordIfNeeded()
    {
        if (!createVisualSword || swordVisualRoot != null)
        {
            return;
        }

        Transform swordAnchor = ResolveSwordAnchorTransform();
        swordMountTransform = swordAnchor;
        swordVisualRoot = swordAnchor.gameObject;

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "SwordHandle";
        handle.transform.SetParent(swordAnchor, false);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        handle.transform.localScale = new Vector3(0.022f, 0.038f, 0.022f);
        DisableCollider(handle);
        ApplySwordMaterial(handle, swordHandleMaterial, new Color(0.16f, 0.13f, 0.11f), 0.22f);

        GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "SwordGuard";
        guard.transform.SetParent(swordAnchor, false);
        guard.transform.localPosition = Vector3.zero;
        guard.transform.localScale = new Vector3(0.072f, 0.016f, 0.026f);
        DisableCollider(guard);
        ApplySwordMaterial(guard, swordMaterial, new Color(0.48f, 0.5f, 0.54f), 0.62f);

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "SwordBlade";
        blade.transform.SetParent(swordAnchor, false);
        blade.transform.localPosition = new Vector3(0f, 0.105f, 0.012f);
        blade.transform.localScale = new Vector3(0.029f, 0.21f, 0.011f);
        DisableCollider(blade);
        ApplySwordMaterial(blade, swordMaterial, new Color(0.62f, 0.64f, 0.68f), 0.72f);
    }

    private Transform ResolveSwordAnchorTransform()
    {
        Transform handBone = FindHandBoneTransform(out string handPath);
        Transform anchorParent = handBone != null ? handBone : transform;
        swordAnchoredToHandBone = handBone != null;

        Transform existingAnchor = anchorParent.Find(TankSwordAnchorName);
        if (existingAnchor != null)
        {
            ApplySwordAnchorTransform(existingAnchor, swordAnchoredToHandBone);
            LogSwordParentOnce(existingAnchor, handPath);
            return existingAnchor;
        }

        GameObject anchor = new GameObject(TankSwordAnchorName);
        anchor.transform.SetParent(anchorParent, false);
        ApplySwordAnchorTransform(anchor.transform, swordAnchoredToHandBone);
        LogSwordParentOnce(anchor.transform, handPath);
        return anchor.transform;
    }

    private void LogSwordParentOnce(Transform anchor, string handPath)
    {
        if (loggedSwordParent)
        {
            return;
        }

        loggedSwordParent = true;

        if (swordAnchoredToHandBone)
        {
            Debug.Log("[TankAnimatedVisualController] sword parent=hand.R path="
                + handPath
                + " anchorPath="
                + GetTransformPath(anchor));
            return;
        }

        Debug.LogWarning("[TankAnimatedVisualController] WARNING: hand.R not found, using fallback anchor. path="
            + GetTransformPath(anchor));
    }

    private Transform FindHandBoneTransform(out string resolvedPath)
    {
        resolvedPath = string.Empty;
        Transform searchRoot = transform;

        for (int i = 0; i < HandBoneRelativePaths.Length; i++)
        {
            string relativePath = HandBoneRelativePaths[i];
            Transform found = searchRoot.Find(relativePath);
            if (found != null)
            {
                resolvedPath = relativePath;
                return found;
            }
        }

        Transform recursiveHand = FindTransformByName(HandBoneName);
        if (recursiveHand != null)
        {
            resolvedPath = GetTransformPath(recursiveHand);
            return recursiveHand;
        }

        if (!string.Equals(preferredHandName, HandBoneName, StringComparison.OrdinalIgnoreCase))
        {
            recursiveHand = FindTransformByName(preferredHandName);
            if (recursiveHand != null)
            {
                resolvedPath = GetTransformPath(recursiveHand);
                return recursiveHand;
            }
        }

        return null;
    }

    private void ApplySwordAnchorTransform(Transform anchor, bool useHandBonePlacement)
    {
        if (useHandBonePlacement)
        {
            anchor.localPosition = swordHandLocalPosition;
            anchor.localRotation = Quaternion.Euler(swordHandLocalEuler);
            anchor.localScale = Vector3.one;
            return;
        }

        anchor.localPosition = swordFallbackLocalPosition;
        anchor.localRotation = Quaternion.Euler(swordFallbackLocalEuler);
        anchor.localScale = swordFallbackLocalScale;
    }

    private static string GetTransformPath(Transform target)
    {
        if (target == null)
        {
            return "null";
        }

        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>(8);
        Transform current = target;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private void ApplySwordMaterial(GameObject target, Material sharedMaterial, Color fallbackColor, float smoothness)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        if (sharedMaterial != null)
        {
            renderer.sharedMaterial = sharedMaterial;
            return;
        }

        ApplyMaterial(target, fallbackColor, smoothness);
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static void ApplyMaterial(GameObject target, Color color, float smoothness)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.35f);
        }

        renderer.sharedMaterial = material;
    }

    private Transform FindTransformByName(string targetName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate != null && string.Equals(candidate.name, targetName, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private Transform FindEnemyRoot()
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.GetComponent<Enemy>() != null)
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private static Transform ResolveTargetTransform()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");

        if (taggedPlayer != null)
        {
            return taggedPlayer.transform;
        }

        FPSPlayerController fpsController = UnityEngine.Object.FindFirstObjectByType<FPSPlayerController>();

        if (fpsController != null)
        {
            return fpsController.transform;
        }

        PlayerController playerController = UnityEngine.Object.FindFirstObjectByType<PlayerController>();

        if (playerController != null)
        {
            return playerController.transform;
        }

        CharacterController[] characterControllers = UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None);

        for (int i = 0; i < characterControllers.Length; i++)
        {
            CharacterController characterController = characterControllers[i];

            if (characterController == null || characterController.GetComponentInParent<Enemy>() != null)
            {
                continue;
            }

            return characterController.transform;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return null;
    }
}
