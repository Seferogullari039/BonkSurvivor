using System;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public class TankAnimatedVisualController : MonoBehaviour
{
    private const string AttackStateName = "Attack";
    private const string IdleStateName = "Idle";
    private const string RunStateName = "Run";
    private const float AttackCrossFadeDuration = 0.05f;
    private const float RunCrossFadeDuration = 0.08f;
    private const float IdleCrossFadeDuration = 0.12f;
    private const float DefaultAttackVisualDuration = 0.9f;
    private const float DebugForceAttackInterval = 1.2f;

    [Header("Animator")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Avatar skeletonAvatar;

    [Header("Movement")]
    [SerializeField] private float moveSpeedThreshold = 0.03f;

    [Header("Attack Visual")]
    [SerializeField] private float attackRange = 4f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugForceAttackLoop = true;
    [SerializeField] private bool debugForceRun = true;
    [SerializeField] private bool debugAnimatorLogs = true;

    [Header("Sword Visual")]
    [SerializeField] private bool createVisualSword = true;
    [SerializeField] private string preferredHandName = "hand.R";

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private Animator animator;
    private Transform enemyRoot;
    private Vector3 lastRootPosition;
    private float nextAttackTime;
    private float nextDebugForceAttackTime;
    private float nextAnimatorDebugLogTime;
    private float attackEndTime;
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
    private GameObject swordVisualRoot;

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

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        SanitizeVisualComponents();
        ConfigureAnimator();
        ApplyBodyMaterial();
        ResolveAttackClipInfo();
        ResolveRunClipInfo();
        CreateVisualSwordIfNeeded();
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

        MaybeLogAnimatorDebug();
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
        attackVisualDuration = DefaultAttackVisualDuration;
        attackClipName = AttackStateName;

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.Log("[TankAnimatedVisualController] Attack clip=" + attackClipName + " length=" + attackVisualDuration + " directPlay=True");
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
                Debug.Log("[TankAnimatedVisualController] Attack clip=" + attackClipName + " length=" + attackVisualDuration + " directPlay=True");
                return;
            }
        }

        Debug.Log("[TankAnimatedVisualController] Attack clip=" + attackClipName + " length=" + attackVisualDuration + " directPlay=True");
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
        animator.CrossFadeInFixedTime(stateName, duration, 0, 0f);
        animator.Update(0f);
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
        attackEndTime = Time.time + attackVisualDuration;
        nextAttackTime = Time.time + attackCooldown;

        Debug.Log("[TankAnimatedVisualController] ATTACK distance=" + distance.ToString("F2")
            + " target=" + targetName
            + " stateBefore=" + stateBefore
            + " clip=" + attackClipName);
    }

    private void EndAttackVisual()
    {
        isAttacking = false;

        string returnState = (debugForceRun || lastIsMoving) ? RunStateName : IdleStateName;
        float crossFadeDuration = returnState == RunStateName ? RunCrossFadeDuration : IdleCrossFadeDuration;
        CrossFadeVisualState(returnState, crossFadeDuration);
        animator.SetBool(IsMovingHash, lastIsMoving);
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
            + " normalized=" + stateInfo.normalizedTime.ToString("F2"));
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

        Transform hand = FindTransformByName(preferredHandName);

        if (hand == null)
        {
            hand = FindTransformByName("hand.L");
        }

        if (hand == null)
        {
            hand = transform;
        }

        swordVisualRoot = new GameObject("VisualSword");
        swordVisualRoot.transform.SetParent(hand, false);
        swordVisualRoot.transform.localPosition = new Vector3(0.04f, 0.02f, 0.12f);
        swordVisualRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        swordVisualRoot.transform.localScale = Vector3.one;

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        blade.transform.SetParent(swordVisualRoot.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        blade.transform.localScale = new Vector3(0.05f, 0.32f, 0.02f);
        DisableCollider(blade);
        ApplyMaterial(blade, new Color(0.72f, 0.74f, 0.78f), 0.72f);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(swordVisualRoot.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        handle.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
        DisableCollider(handle);
        ApplyMaterial(handle, new Color(0.18f, 0.14f, 0.11f), 0.25f);
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
