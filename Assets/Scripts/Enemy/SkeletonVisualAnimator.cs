using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(260)]
public class SkeletonVisualAnimator : MonoBehaviour
{
    [Header("Walk")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float legSwingAngle = 35f;
    [SerializeField] private float armSwingAngle = 25f;
    [SerializeField] private float bodyBobAmount = 0.07f;
    [SerializeField] private float bodyBobSpeed = 8f;
    [SerializeField] private float bodySwayAngle = 5f;
    [SerializeField] private float bodyLeanAngle = 5f;
    [SerializeField] private float moveThreshold = 0.02f;
    [SerializeField] private float walkFrequency = 8f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private float attackCooldown = 0.8f;
    [SerializeField] private float attackDuration = 0.9f;
    [SerializeField] private float attackRaiseAngle = -130f;
    [SerializeField] private float attackForeArmBendAngle = -35f;
    [SerializeField] private float attackSlashAngle = 180f;
    [SerializeField] private float attackSideAngle = 55f;

    [Header("Attack Axes")]
    [SerializeField] private AttackAxisMode attackAxisMode = AttackAxisMode.XZCombined;
    [SerializeField] private Vector3 rightUpperArmRaiseAxis = Vector3.right;
    [SerializeField] private Vector3 rightUpperArmSlashAxis = Vector3.forward;
    [SerializeField] private Vector3 rightForeArmAxis = Vector3.right;
    [SerializeField] private Vector3 rightHandAxis = Vector3.forward;

    [Header("Fallback Walk")]
    [SerializeField] private float walkBobAmount = 0.045f;
    [SerializeField] private float walkSwayAngle = 3.5f;
    [SerializeField] private float walkLeanAngle = 3f;

    [Header("Bones")]
    [SerializeField] private bool autoFindBones = true;
    [SerializeField] private Transform hips;
    [SerializeField] private Transform spine;
    [SerializeField] private Transform chest;
    [SerializeField] private Transform leftUpperArm;
    [SerializeField] private Transform leftForeArm;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightUpperArm;
    [SerializeField] private Transform rightForeArm;
    [SerializeField] private Transform rightHand;
    [SerializeField] private Transform leftUpperLeg;
    [SerializeField] private Transform leftLowerLeg;
    [SerializeField] private Transform leftFoot;
    [SerializeField] private Transform rightUpperLeg;
    [SerializeField] private Transform rightLowerLeg;
    [SerializeField] private Transform rightFoot;
    [SerializeField] private Transform swordTransform;

    [Header("Tank Visual")]
    [SerializeField] private float visualGroundYOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool debugForceVisibleMotion = true;
    [SerializeField] private bool debugForceWalk = true;
    [SerializeField] private bool debugForceAttackLoop = true;
    [SerializeField] private bool debugForceBonePose = false;

    [Header("Safety")]
    [SerializeField] private bool enableFallbackVisuals = true;

    private enum AttackAxisMode
    {
        LocalX,
        LocalZ,
        LocalY,
        XOnly,
        ZOnly,
        YOnly,
        XYCombined,
        XZCombined,
        YZCombined
    }

    private enum AttackSide
    {
        Left,
        Right
    }

    private enum AttackMode
    {
        None,
        Bones,
        SwordTransform,
        ModelFallback
    }

    private readonly Dictionary<Transform, Quaternion> baseRotations = new Dictionary<Transform, Quaternion>();
    private Transform enemyRoot;
    private Transform attackUpperArm;
    private Transform attackForeArm;
    private Transform attackHand;
    private Renderer swordRenderer;
    private AttackSide attackSide = AttackSide.Right;
    private float swordDistanceLeft;
    private float swordDistanceRight;
    private readonly HashSet<Transform> skinnedMeshBones = new HashSet<Transform>();
    private bool bonesSummaryLogged;
    private bool activeInstanceLogged;
    private int disabledAnimatorCount;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseSwordLocalPosition;
    private Quaternion baseSwordLocalRotation;
    private Vector3 lastEnemyPosition;
    private float smoothedMoveSpeed;
    private float walkPhase;
    private float nextAttackTime;
    private float attackStartTime;
    private bool attackActive;
    private bool useModelRootFallback;
    private bool rigReported;
    private bool usingBoneWalk;
    private bool usingBoneAttack;
    private AttackMode attackMode = AttackMode.None;
    private GameObject slashArcVisual;
    private LineRenderer slashLineRenderer;
    private float slashArcHideTime;
    private float nextStateLogTime;
    private float lastAttackDistance;
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void Start()
    {
        if (!initialized)
        {
            TryInitialize();
        }

        LogActiveInstance();
    }

    private void LateUpdate()
    {
        if (!enableFallbackVisuals)
        {
            return;
        }

        if (!initialized && !TryInitialize())
        {
            return;
        }

        UpdateMovementSample();
        float attackBlend = UpdateAttackState(out float attackNormalized);
        bool effectivelyMoving = IsEffectivelyMoving();

        switch (attackMode)
        {
            case AttackMode.Bones:
                ApplyBoneIdleOrWalk(attackBlend, effectivelyMoving);
                ApplyBoneAttack(attackBlend, attackNormalized);
                ApplyModelAttackLeanSupplement(attackBlend, attackNormalized);
                UpdateBoneAttackSlashArc(attackBlend, attackNormalized);
                break;

            case AttackMode.SwordTransform:
                ApplyBoneIdleOrWalk(attackBlend, effectivelyMoving);
                ApplyBoneAttack(attackBlend, attackNormalized);
                ApplySwordTransformAttack(attackBlend, attackNormalized);
                ApplyModelAttackLeanSupplement(attackBlend, attackNormalized);
                UpdateBoneAttackSlashArc(attackBlend, attackNormalized);
                break;

            case AttackMode.ModelFallback:
                ApplyModelRootAnimation(attackBlend, attackNormalized, effectivelyMoving);
                break;

            default:
                ApplyModelRootAnimation(attackBlend, attackNormalized, effectivelyMoving);
                break;
        }

        if (debugForceVisibleMotion)
        {
            ApplyDebugForceVisibleMotion(attackBlend, attackNormalized);
        }
        else if (attackBlend <= 0.001f && !effectivelyMoving)
        {
            RestoreTransformBase();
        }

        if (debugForceBonePose)
        {
            ApplyDebugForceBonePose();
        }
    }

    private void OnDisable()
    {
        RestoreBasePose();
        HideSlashArc();
    }

    private void OnDestroy()
    {
        RestoreBasePose();

        if (slashArcVisual != null)
        {
            Destroy(slashArcVisual);
        }
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return true;
        }

        enemyRoot = FindEnemyRoot();

        if (enemyRoot == null)
        {
            Debug.LogWarning("[SkeletonVisualAnimator] Enemy root not found. Disabling component.", this);
            enabled = false;
            return false;
        }

        if (Mathf.Abs(visualGroundYOffset) > 0.0001f)
        {
            Vector3 groundedPosition = transform.localPosition;
            groundedPosition.y += visualGroundYOffset;
            transform.localPosition = groundedPosition;
        }

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        lastEnemyPosition = enemyRoot.position;

        if (autoFindBones)
        {
            CollectSkinnedMeshBoneSet();
            AutoFindBones();
            FindSwordTransform();
            ResolveAttackArm();
            ValidateSkinnedBoneBindings();
        }

        disabledAnimatorCount = DisableBlockingAnimators();

        CacheBaseRotations();
        useModelRootFallback = !HasWalkBones() || !HasSkinnedWalkBones();
        usingBoneWalk = HasWalkBones();
        attackMode = ResolveAttackMode();
        usingBoneAttack = attackMode == AttackMode.Bones || attackMode == AttackMode.SwordTransform;

        if (useModelRootFallback)
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Walk bones missing or not in skinned mesh; model fallback may be used.",
                this);
        }

        if (!usingBoneAttack && attackMode == AttackMode.ModelFallback)
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Attack arm bones not found; attack uses slash arc fallback.",
                this);
        }

        LogBonesSummary();
        LogActiveInstance();

        initialized = true;
        return true;
    }

    private void LogActiveInstance()
    {
        if (activeInstanceLogged)
        {
            return;
        }

        activeInstanceLogged = true;

        SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int skinnedBoneCount = 0;

        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i]?.bones != null)
            {
                skinnedBoneCount += skinned[i].bones.Length;
            }
        }

        int rendererCount = GetComponentsInChildren<Renderer>(true).Length;

        Debug.Log(
            "[SkeletonVisualAnimator] ACTIVE instance="
            + gameObject.name
            + " enabled="
            + enabled
            + " model="
            + transform.name
            + " skinnedMeshes="
            + skinned.Length
            + " bones="
            + skinnedBoneCount
            + " renderers="
            + rendererCount
            + " disabledAnimators="
            + disabledAnimatorCount
            + " debugVisibleMotion="
            + debugForceVisibleMotion
            + " debugWalk="
            + debugForceWalk
            + " debugAttackLoop="
            + debugForceAttackLoop,
            this);
    }

    private void CollectSkinnedMeshBoneSet()
    {
        skinnedMeshBones.Clear();
        SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        for (int i = 0; i < skinned.Length; i++)
        {
            Transform[] bones = skinned[i]?.bones;

            if (bones == null)
            {
                continue;
            }

            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                if (bones[boneIndex] != null)
                {
                    skinnedMeshBones.Add(bones[boneIndex]);
                }
            }
        }
    }

    private void ValidateSkinnedBoneBindings()
    {
        ValidateBoneInSkinnedMesh(attackUpperArm, "attackUpperArm");
        ValidateBoneInSkinnedMesh(attackForeArm, "attackForeArm");
        ValidateBoneInSkinnedMesh(attackHand, "attackHand");
        ValidateBoneInSkinnedMesh(leftUpperLeg, "leftUpperLeg");
        ValidateBoneInSkinnedMesh(rightUpperLeg, "rightUpperLeg");
    }

    private void ValidateBoneInSkinnedMesh(Transform bone, string label)
    {
        if (bone == null || skinnedMeshBones.Count == 0)
        {
            return;
        }

        if (!skinnedMeshBones.Contains(bone))
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Selected "
                + label
                + " not in skinned mesh bones, may not affect mesh. bone="
                + bone.name,
                this);
        }
    }

    private bool HasSkinnedWalkBones()
    {
        return IsBoneInSkinnedMesh(leftUpperLeg) && IsBoneInSkinnedMesh(rightUpperLeg);
    }

    private bool IsBoneInSkinnedMesh(Transform bone)
    {
        return bone != null && (skinnedMeshBones.Count == 0 || skinnedMeshBones.Contains(bone));
    }

    private bool IsEffectivelyMoving()
    {
        if (debugForceWalk)
        {
            return true;
        }

        return smoothedMoveSpeed > moveThreshold;
    }

    private void ApplyDebugForceVisibleMotion(float attackBlend, float attackNormalized)
    {
        float time = Time.time;
        float bob = Mathf.Sin(time * 4f) * 0.08f;
        float swayX = Mathf.Sin(time * 3f) * 4f;
        float swayZ = Mathf.Cos(time * 2.5f) * 4f;

        if (attackBlend > 0.01f)
        {
            swayX += attackBlend * attackSideAngle * 0.12f * Mathf.Sin(attackNormalized * Mathf.PI);
            swayZ += attackBlend * 6f * Mathf.Sin(attackNormalized * Mathf.PI);
        }

        transform.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(swayX, 0f, swayZ);
    }

    private void ApplyDebugForceBonePose()
    {
        Transform upperArm = attackUpperArm ?? rightUpperArm ?? leftUpperArm;
        Transform upperLeg = leftUpperLeg ?? rightUpperLeg;

        if (upperArm != null && baseRotations.TryGetValue(upperArm, out Quaternion upperBase))
        {
            upperArm.localRotation = upperBase * Quaternion.Euler(-80f, 30f, 45f);
        }

        if (upperLeg != null && baseRotations.TryGetValue(upperLeg, out Quaternion legBase))
        {
            upperLeg.localRotation = legBase * Quaternion.Euler(35f, 0f, 0f);
        }
    }

    private void ApplyModelAttackLeanSupplement(float attackBlend, float attackNormalized)
    {
        if (attackBlend <= 0.001f || debugForceVisibleMotion)
        {
            return;
        }

        float lean = attackBlend * 10f * Mathf.Sin(attackNormalized * Mathf.PI);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(lean, 0f, 0f);
    }

    private void RestoreTransformBase()
    {
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogBonesSummary()
    {
        if (bonesSummaryLogged)
        {
            return;
        }

        bonesSummaryLogged = true;

        SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int skinnedBoneCount = 0;

        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i]?.bones != null)
            {
                skinnedBoneCount += skinned[i].bones.Length;
            }
        }

        string swordLabel = swordTransform != null ? BoneName(swordTransform) : "null";
        string fallbackNote = swordTransform == null ? " fallback=Right" : string.Empty;

        Debug.Log(
            "[SkeletonVisualAnimator] Bones summary skinned="
            + skinned.Length
            + " bones="
            + skinnedBoneCount
            + " LHand="
            + BoneName(leftHand)
            + " RHand="
            + BoneName(rightHand)
            + " LUpper="
            + BoneName(leftUpperArm)
            + " RUpper="
            + BoneName(rightUpperArm)
            + " LFore="
            + BoneName(leftForeArm)
            + " RFore="
            + BoneName(rightForeArm)
            + " sword="
            + swordLabel
            + " attackSide="
            + attackSide
            + " attackUpperInSkinned="
            + IsBoneInSkinnedMesh(attackUpperArm)
            + " attackForeInSkinned="
            + IsBoneInSkinnedMesh(attackForeArm)
            + " attackHandInSkinned="
            + IsBoneInSkinnedMesh(attackHand)
            + " swordDistL="
            + swordDistanceLeft.ToString("F3")
            + " swordDistR="
            + swordDistanceRight.ToString("F3")
            + fallbackNote,
            this);
    }

    private static string BoneName(Transform bone)
    {
        return bone != null ? bone.name : "null";
    }

    private AttackMode ResolveAttackMode()
    {
        if (HasAttackBones())
        {
            return AttackMode.Bones;
        }

        if (swordTransform != null)
        {
            baseSwordLocalPosition = swordTransform.localPosition;
            baseSwordLocalRotation = swordTransform.localRotation;
            return AttackMode.SwordTransform;
        }

        return AttackMode.ModelFallback;
    }

    private void UpdateMovementSample()
    {
        if (debugForceWalk)
        {
            walkPhase += Time.deltaTime * walkFrequency;
            smoothedMoveSpeed = Mathf.Max(smoothedMoveSpeed, walkSpeed);
            return;
        }

        Vector3 delta = enemyRoot.position - lastEnemyPosition;
        lastEnemyPosition = enemyRoot.position;
        delta.y = 0f;
        float instantSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        smoothedMoveSpeed = Mathf.Lerp(smoothedMoveSpeed, instantSpeed, Time.deltaTime * 8f);

        if (IsEffectivelyMoving())
        {
            walkPhase += Time.deltaTime * walkFrequency * Mathf.Clamp(smoothedMoveSpeed * 0.22f, 0.45f, 1.4f);
        }
    }

    private float UpdateAttackState(out float attackNormalized)
    {
        attackNormalized = 0f;

        if (debugForceAttackLoop)
        {
            attackActive = true;
            attackNormalized = Mathf.PingPong(Time.time / Mathf.Max(0.001f, attackDuration), 1f);

            if (Mathf.Repeat(Time.time, attackDuration * 2f) < 0.05f)
            {
                ShowSlashArc(0.35f);
            }

            return 1f;
        }

        Transform target = ResolveTargetTransform();

        if (target == null)
        {
            attackActive = false;
            return 0f;
        }

        Vector3 toTarget = target.position - enemyRoot.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        lastAttackDistance = distance;

        if (!attackActive && distance <= attackRange && Time.time >= nextAttackTime)
        {
            attackActive = true;
            attackStartTime = Time.time;
            nextAttackTime = Time.time + attackCooldown;
            LogAttackTrigger(distance);
        }

        if (!attackActive)
        {
            return 0f;
        }

        attackNormalized = (Time.time - attackStartTime) / Mathf.Max(0.001f, attackDuration);

        if (attackNormalized >= 1f)
        {
            attackActive = false;
            attackNormalized = 0f;
            return 0f;
        }

        return EvaluateAttackCurve(attackNormalized);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogAttackTrigger(float distance)
    {
        Debug.Log(
            "[SkeletonVisualAnimator] ATTACK START distance="
            + distance.ToString("F2")
            + " side="
            + attackSide
            + " upper="
            + BoneName(attackUpperArm)
            + " fore="
            + BoneName(attackForeArm)
            + " hand="
            + BoneName(attackHand)
            + " sword="
            + BoneName(swordTransform)
            + " axis="
            + attackAxisMode,
            this);

        ShowSlashArc(0.35f);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogAttackArmResolution()
    {
        // Kept for compatibility; summary logged once at startup.
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogPeriodicState(float attackNormalized)
    {
        // Disabled to avoid log spam during playtests.
    }

    private static float EvaluateAttackCurve(float normalized)
    {
        if (normalized < 0.35f)
        {
            return Mathf.SmoothStep(0f, 1f, normalized / 0.35f);
        }

        if (normalized < 0.65f)
        {
            return 1f;
        }

        return 1f - Mathf.SmoothStep(0f, 1f, (normalized - 0.65f) / 0.35f);
    }

    private void GetAttackPhaseAngles(float normalized, out float raiseAmount, out float slashAmount, out float sideAmount)
    {
        float windUp = normalized < 0.35f
            ? Mathf.SmoothStep(0f, 1f, normalized / 0.35f)
            : normalized < 0.70f
                ? 1f
                : Mathf.Lerp(1f, 0f, (normalized - 0.70f) / 0.30f);

        float strike = 0f;

        if (normalized >= 0.35f && normalized < 0.70f)
        {
            strike = Mathf.SmoothStep(0f, 1f, (normalized - 0.35f) / 0.35f);
        }
        else if (normalized >= 0.70f)
        {
            strike = Mathf.Lerp(1f, 0f, (normalized - 0.70f) / 0.30f);
        }

        raiseAmount = attackRaiseAngle * windUp;
        slashAmount = attackSlashAngle * strike;
        sideAmount = attackSideAngle * Mathf.Max(windUp, strike);
    }

    private void ApplyModelRootAnimation(float attackBlend, float attackNormalized, bool isMoving)
    {
        float bob = isMoving
            ? Mathf.Sin(walkPhase) * walkBobAmount
            : Mathf.Sin(Time.time * bodyBobSpeed) * (walkBobAmount * 0.35f);
        float sway = isMoving ? Mathf.Sin(walkPhase) * walkSwayAngle : 0f;
        float lean = isMoving ? Mathf.Cos(walkPhase * 0.5f) * bodyLeanAngle : 0f;

        float attackRaise = attackBlend * attackRaiseAngle;
        float attackSlash = attackBlend * attackSlashAngle;
        float attackSide = attackBlend * attackSideAngle;
        float strikeLean = attackBlend * 12f;

        transform.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        transform.localRotation =
            baseLocalRotation
            * Quaternion.Euler(
                attackRaise + attackSlash * 0.35f + lean + strikeLean,
                attackSide,
                sway);

        UpdateFallbackSlashArc(attackBlend, attackNormalized);
    }

    private void ApplySwordTransformAttack(float attackBlend, float attackNormalized)
    {
        if (swordTransform == null || attackBlend <= 0.001f)
        {
            return;
        }

        GetAttackPhaseAngles(attackNormalized, out float raise, out float slash, out float side);

        swordTransform.localRotation =
            baseSwordLocalRotation * Quaternion.Euler(raise + slash * 0.85f, side * 0.65f, slash * 0.2f);
        swordTransform.localPosition =
            baseSwordLocalPosition + new Vector3(0f, attackBlend * 0.07f, attackBlend * 0.045f);
    }

    private void ApplyBoneIdleOrWalk(float attackBlend, bool isMoving)
    {
        float bob = isMoving
            ? Mathf.Sin(walkPhase) * bodyBobAmount
            : Mathf.Sin(Time.time * bodyBobSpeed) * (bodyBobAmount * 0.25f);
        float sway = isMoving ? Mathf.Sin(walkPhase * 0.5f) * bodySwayAngle : 0f;
        float lean = isMoving ? Mathf.Cos(walkPhase * 0.5f) * bodyLeanAngle : 0f;

        ApplyBoneRotation(hips, new Vector3(lean * 0.35f, 0f, sway * 0.6f), bob * 12f);
        ApplyBoneRotation(spine, new Vector3(0f, 0f, sway * 0.35f), bob * 8f);
        ApplyBoneRotation(chest, Vector3.zero, bob * 5f);

        if (!isMoving)
        {
            return;
        }

        float legSwing = Mathf.Sin(walkPhase) * legSwingAngle;
        float armSwing = Mathf.Sin(walkPhase + Mathf.PI) * armSwingAngle;

        ApplyBoneRotation(leftUpperLeg, new Vector3(legSwing, 0f, 0f));
        ApplyBoneRotation(leftLowerLeg, new Vector3(-legSwing * 0.55f, 0f, 0f));
        ApplyBoneRotation(leftFoot, new Vector3(legSwing * 0.25f, 0f, 0f));

        ApplyBoneRotation(rightUpperLeg, new Vector3(-legSwing, 0f, 0f));
        ApplyBoneRotation(rightLowerLeg, new Vector3(legSwing * 0.55f, 0f, 0f));
        ApplyBoneRotation(rightFoot, new Vector3(-legSwing * 0.25f, 0f, 0f));

        if (leftUpperArm != attackUpperArm && leftForeArm != attackForeArm)
        {
            ApplyBoneRotation(leftUpperArm, new Vector3(-armSwing, 0f, 0f));
            ApplyBoneRotation(leftForeArm, new Vector3(-armSwing * 0.4f, 0f, 0f));
        }

        if (rightUpperArm != attackUpperArm && rightForeArm != attackForeArm && attackBlend <= 0.02f)
        {
            float rightArmSwing = armSwing * 0.12f;
            ApplyBoneRotation(rightUpperArm, new Vector3(rightArmSwing, 0f, 0f));
            ApplyBoneRotation(rightForeArm, new Vector3(rightArmSwing * 0.25f, 0f, 0f));
        }
    }

    private void ApplyBoneAttack(float attackBlend, float attackNormalized)
    {
        if (!debugForceAttackLoop && attackBlend <= 0.001f)
        {
            return;
        }

        if (!HasAttackBones())
        {
            return;
        }

        float sideSign = attackSide == AttackSide.Left ? -1f : 1f;
        GetForcedAttackEulers(attackNormalized, sideSign, out Vector3 upperEuler, out Vector3 foreEuler, out Vector3 handEuler);

        ApplyBoneRotationAbsolute(attackUpperArm, upperEuler);
        ApplyBoneRotationAbsolute(attackForeArm, foreEuler);
        ApplyBoneRotationAbsolute(attackHand, handEuler);

        if (swordTransform != null && baseRotations.ContainsKey(swordTransform))
        {
            Vector3 swordEuler = new Vector3(
                handEuler.x * 0.6f,
                handEuler.y * 0.5f,
                handEuler.z * 0.75f);
            ApplyBoneRotationAbsolute(swordTransform, swordEuler);
        }
    }

    private void GetForcedAttackEulers(
        float normalized,
        float sideSign,
        out Vector3 upperEuler,
        out Vector3 foreEuler,
        out Vector3 handEuler)
    {
        Vector3 windUpUpper = new Vector3(attackRaiseAngle * 0.7f, 20f * sideSign, attackSideAngle * sideSign);
        Vector3 windUpFore = new Vector3(attackForeArmBendAngle, 0f, 25f * sideSign);
        Vector3 windUpHand = new Vector3(0f, 0f, attackSideAngle * 0.65f * sideSign);

        Vector3 slashUpper = new Vector3(attackSlashAngle * 0.42f, -20f * sideSign, -attackSideAngle * sideSign);
        Vector3 slashFore = new Vector3(attackForeArmBendAngle * -0.85f, 0f, -35f * sideSign);
        Vector3 slashHand = new Vector3(0f, 0f, -attackSideAngle * sideSign);

        if (normalized < 0.30f)
        {
            float t = Mathf.SmoothStep(0f, 1f, normalized / 0.30f);
            upperEuler = Vector3.Lerp(Vector3.zero, windUpUpper, t);
            foreEuler = Vector3.Lerp(Vector3.zero, windUpFore, t);
            handEuler = Vector3.Lerp(Vector3.zero, windUpHand, t);
            return;
        }

        if (normalized < 0.65f)
        {
            float t = Mathf.SmoothStep(0f, 1f, (normalized - 0.30f) / 0.35f);
            upperEuler = Vector3.Lerp(windUpUpper, slashUpper, t);
            foreEuler = Vector3.Lerp(windUpFore, slashFore, t);
            handEuler = Vector3.Lerp(windUpHand, slashHand, t);
            return;
        }

        float recoverT = Mathf.SmoothStep(0f, 1f, (normalized - 0.65f) / 0.35f);
        upperEuler = Vector3.Lerp(slashUpper, Vector3.zero, recoverT);
        foreEuler = Vector3.Lerp(slashFore, Vector3.zero, recoverT);
        handEuler = Vector3.Lerp(slashHand, Vector3.zero, recoverT);
    }

    private Vector3 BuildAttackEuler(
        float raise,
        float slash,
        float side,
        Vector3 raiseAxis,
        Vector3 slashAxis)
    {
        switch (attackAxisMode)
        {
            case AttackAxisMode.LocalX:
            case AttackAxisMode.XOnly:
                return new Vector3(raise + slash, side, 0f);

            case AttackAxisMode.LocalZ:
            case AttackAxisMode.ZOnly:
                return new Vector3(0f, side, raise + slash);

            case AttackAxisMode.LocalY:
            case AttackAxisMode.YOnly:
                return new Vector3(0f, raise + slash + side, 0f);

            case AttackAxisMode.XYCombined:
                Vector3 xyCombined = raiseAxis.normalized * raise + Vector3.up * slash;
                xyCombined.z += side * 0.35f;
                return xyCombined;

            case AttackAxisMode.YZCombined:
                return new Vector3(side * 0.35f, raise + slash * 0.5f, raise * 0.25f + slash);

            default:
                Vector3 combined =
                    raiseAxis.normalized * raise
                    + slashAxis.normalized * slash;
                combined.y += side;
                return combined;
        }
    }

    private void ApplyBoneRotationAbsolute(Transform bone, Vector3 eulerOffset)
    {
        if (bone == null || !baseRotations.TryGetValue(bone, out Quaternion baseRotation))
        {
            return;
        }

        bone.localRotation = baseRotation * Quaternion.Euler(eulerOffset);
    }

    private void ApplyBoneRotation(Transform bone, Vector3 eulerOffset, float extraX = 0f)
    {
        if (bone == null || !baseRotations.TryGetValue(bone, out Quaternion baseRotation))
        {
            return;
        }

        eulerOffset.x += extraX;
        bone.localRotation = baseRotation * Quaternion.Euler(eulerOffset);
    }

    private void UpdateBoneAttackSlashArc(float attackBlend, float attackNormalized)
    {
        if (!debugForceAttackLoop && attackBlend <= 0.05f)
        {
            if (slashArcVisual != null && Time.time >= slashArcHideTime)
            {
                HideSlashArc();
            }

            return;
        }

        ShowSlashArc(attackNormalized);
    }

    private void UpdateFallbackSlashArc(float attackBlend, float attackNormalized)
    {
        if (attackBlend <= 0.05f || attackNormalized < 0.3f || attackNormalized > 0.7f)
        {
            if (slashArcVisual != null && Time.time >= slashArcHideTime)
            {
                HideSlashArc();
            }

            return;
        }

        ShowSlashArc(attackNormalized);
    }

    private void ShowSlashArc(float attackNormalized)
    {
        EnsureSlashArcVisual();
        slashArcVisual.SetActive(true);
        slashArcHideTime = Time.time + 0.35f;

        float strikeT = attackNormalized < 0.30f
            ? 0f
            : Mathf.InverseLerp(0.30f, 0.65f, attackNormalized);
        float sideSign = attackSide == AttackSide.Left ? -1f : 1f;
        Transform arcTransform = slashArcVisual.transform;
        arcTransform.localPosition = new Vector3(0.28f * sideSign, 1.08f, 0.62f + strikeT * 0.32f);
        arcTransform.localRotation = Quaternion.Euler(-60f - strikeT * 45f, 35f * sideSign, 42f * sideSign);
        arcTransform.localScale = new Vector3(1.15f, 0.08f, 0.38f + strikeT * 0.62f);

        if (slashLineRenderer != null)
        {
            float alpha = Mathf.Lerp(1f, 0.35f, Mathf.Abs(strikeT - 0.5f) * 2f);
            Color color = new Color(1f, 0.98f, 0.55f, alpha);
            slashLineRenderer.startColor = color;
            slashLineRenderer.endColor = color;
        }
    }

    private void EnsureSlashArcVisual()
    {
        if (slashArcVisual != null)
        {
            return;
        }

        slashArcVisual = new GameObject("SlashArcVisual");
        slashArcVisual.transform.SetParent(transform, false);

        slashLineRenderer = slashArcVisual.AddComponent<LineRenderer>();
        slashLineRenderer.useWorldSpace = false;
        slashLineRenderer.positionCount = 8;
        slashLineRenderer.startWidth = 0.11f;
        slashLineRenderer.endWidth = 0.03f;
        slashLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        slashLineRenderer.receiveShadows = false;
        slashLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        slashLineRenderer.material.color = new Color(1f, 0.92f, 0.45f, 0.85f);

        Vector3[] points = new Vector3[8];

        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            float angle = Mathf.Lerp(-70f, 40f, t) * Mathf.Deg2Rad;
            points[i] = new Vector3(Mathf.Sin(angle) * 0.72f, Mathf.Cos(angle) * 0.72f, 0f);
        }

        slashLineRenderer.SetPositions(points);

        GameObject meshFallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        meshFallback.name = "SlashArcMesh";
        meshFallback.transform.SetParent(slashArcVisual.transform, false);
        meshFallback.transform.localPosition = Vector3.zero;
        meshFallback.transform.localScale = new Vector3(0.72f, 0.05f, 0.24f);

        Collider collider = meshFallback.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = meshFallback.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            renderer.material.color = new Color(1f, 0.92f, 0.45f, 0.35f);
        }

        slashArcVisual.SetActive(false);
    }

    private void HideSlashArc()
    {
        if (slashArcVisual != null)
        {
            slashArcVisual.SetActive(false);
        }
    }

    private void FindSwordTransform()
    {
        swordTransform = null;
        swordRenderer = null;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        Transform bestTransform = null;
        int bestDepth = int.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];

            if (candidate == null || candidate == transform)
            {
                continue;
            }

            if (!IsSwordName(candidate.name))
            {
                continue;
            }

            int depth = GetTransformDepth(candidate);

            if (depth < bestDepth)
            {
                bestDepth = depth;
                bestTransform = candidate;
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        Renderer bestRenderer = null;
        int bestRendererDepth = int.MaxValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !IsSwordName(renderer.name))
            {
                continue;
            }

            int depth = GetTransformDepth(renderer.transform);

            if (depth < bestRendererDepth)
            {
                bestRendererDepth = depth;
                bestRenderer = renderer;
            }
        }

        if (bestRenderer != null && (bestTransform == null || bestRendererDepth <= bestDepth))
        {
            swordRenderer = bestRenderer;
            swordTransform = bestRenderer.transform;
            return;
        }

        swordTransform = bestTransform;

        if (swordTransform != null)
        {
            swordRenderer = swordTransform.GetComponent<Renderer>();
        }
    }

    private Vector3 GetSwordWorldPosition()
    {
        if (swordRenderer != null)
        {
            return swordRenderer.bounds.center;
        }

        if (swordTransform != null)
        {
            return swordTransform.position;
        }

        return transform.position;
    }

    private static Transform FindSwordTransformUnder(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];

            if (candidate != null && IsSwordName(candidate.name))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool IsSwordName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string lower = name.ToLowerInvariant();
        return lower.Contains("sword")
            || lower.Contains("blade")
            || lower.Contains("weapon")
            || lower.Contains("katana")
            || lower.Contains("knife");
    }

    private static int GetTransformDepth(Transform node)
    {
        int depth = 0;
        Transform current = node;

        while (current != null)
        {
            depth++;
            current = current.parent;
        }

        return depth;
    }

    private int DisableBlockingAnimators()
    {
        int disabledCount = 0;
        Animator[] animators = GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
            {
                continue;
            }

            animator.applyRootMotion = false;

            if (animator.enabled)
            {
                disabledCount++;
            }

            animator.enabled = false;
        }

        if (disabledCount > 0)
        {
            Debug.Log(
                "[SkeletonVisualAnimator] disabled animator count="
                + disabledCount,
                this);
        }

        return disabledCount;
    }

    private void ResolveAttackArm()
    {
        EnsureHandBonesFound();

        swordDistanceLeft = float.PositiveInfinity;
        swordDistanceRight = float.PositiveInfinity;

        if (swordTransform != null || swordRenderer != null)
        {
            Vector3 swordPos = GetSwordWorldPosition();

            if (leftHand != null)
            {
                swordDistanceLeft = Vector3.Distance(swordPos, leftHand.position);
            }

            if (rightHand != null)
            {
                swordDistanceRight = Vector3.Distance(swordPos, rightHand.position);
            }

            attackSide = swordDistanceLeft <= swordDistanceRight ? AttackSide.Left : AttackSide.Right;
        }
        else
        {
            attackSide = AttackSide.Right;

            if (leftHand == null && rightHand == null)
            {
                Debug.LogWarning(
                    "[SkeletonVisualAnimator] sword=null and both hands null; fallback=Right",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    "[SkeletonVisualAnimator] sword=null LHand="
                    + BoneName(leftHand)
                    + " RHand="
                    + BoneName(rightHand)
                    + " fallback=Right",
                    this);
            }
        }

        AssignAttackArmBones();
        EnsureAttackArmChain();

        if (swordTransform == null && attackHand != null)
        {
            swordTransform = FindSwordTransformUnder(attackHand);
            swordRenderer = swordTransform != null ? swordTransform.GetComponent<Renderer>() : null;
        }
    }

    private void EnsureAttackArmChain()
    {
        if (attackForeArm == null && attackHand != null && attackHand.parent != null)
        {
            attackForeArm = attackHand.parent;
        }

        if (attackUpperArm == null && attackForeArm != null && attackForeArm.parent != null)
        {
            string parentName = attackForeArm.parent.name.ToLowerInvariant();

            if (parentName.Contains("arm") || parentName.Contains("shoulder") || IsLeftName(parentName) || IsRightName(parentName))
            {
                attackUpperArm = attackForeArm.parent;
            }
        }

        if (attackHand == null && attackForeArm != null)
        {
            attackHand = FindChildBone(attackForeArm, "hand", "wrist");
        }
    }

    private void EnsureHandBonesFound()
    {
        List<Transform> candidates = CollectBoneCandidates();

        leftHand ??= FindBoneByPreferredNames(
            candidates,
            Side.Left,
            "L.Hand",
            "LeftHand",
            "Hand.L",
            "L_Hand",
            "hand.l");
        leftHand ??= FindBestBone(candidates, Side.Left, "hand", "wrist");

        rightHand ??= FindBoneByPreferredNames(
            candidates,
            Side.Right,
            "R.Hand",
            "RightHand",
            "Hand.R",
            "R_Hand",
            "hand.r");
        rightHand ??= FindBestBone(candidates, Side.Right, "hand", "wrist");
    }

    private void AssignAttackArmBones()
    {
        List<Transform> candidates = CollectBoneCandidates();

        if (attackSide == AttackSide.Left)
        {
            attackHand = leftHand;
            attackUpperArm = leftUpperArm ?? FindBoneByPreferredNames(
                candidates,
                Side.Left,
                "L.UpperArm",
                "LeftUpperArm",
                "LeftArm",
                "Arm.L",
                "UpperArm.L");
            attackUpperArm ??= FindBestBone(candidates, Side.Left, "upperarm", "upper_arm", "arm");
            attackForeArm = leftForeArm ?? FindBoneByPreferredNames(
                candidates,
                Side.Left,
                "L.ForeArm",
                "L.LowerArm",
                "L.DownArm",
                "LeftForeArm",
                "LeftLowerArm",
                "ForeArm.L",
                "LowerArm.L");
            attackForeArm ??= FindBestBone(
                candidates,
                Side.Left,
                "forearm",
                "lowerarm",
                "lower_arm",
                "fore_arm",
                "downarm");
        }
        else
        {
            attackHand = rightHand;
            attackUpperArm = rightUpperArm ?? FindBoneByPreferredNames(
                candidates,
                Side.Right,
                "R.UpperArm",
                "RightUpperArm",
                "RightArm",
                "Arm.R",
                "UpperArm.R");
            attackUpperArm ??= FindBestBone(candidates, Side.Right, "upperarm", "upper_arm", "arm");
            attackForeArm = rightForeArm ?? FindBoneByPreferredNames(
                candidates,
                Side.Right,
                "R.ForeArm",
                "R.LowerArm",
                "R.DownArm",
                "RightForeArm",
                "RightLowerArm",
                "ForeArm.R",
                "LowerArm.R");
            attackForeArm ??= FindBestBone(
                candidates,
                Side.Right,
                "forearm",
                "lowerarm",
                "lower_arm",
                "fore_arm",
                "downarm");
        }

        if (attackHand == null && attackUpperArm != null)
        {
            attackHand = FindChildBone(attackUpperArm, "hand", "wrist");
        }
    }

    private static Transform FindChildBone(Transform root, params string[] tokens)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            string lowerName = child.name.ToLowerInvariant();

            for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                if (lowerName.Contains(tokens[tokenIndex]))
                {
                    return child;
                }
            }
        }

        return null;
    }

    private void AutoFindBones()
    {
        List<Transform> skinnedCandidates = CollectSkinnedBoneCandidates();
        List<Transform> allCandidates = CollectBoneCandidates();

        hips ??= FindBestBone(skinnedCandidates, Side.Any, "hips", "pelvis", "root");
        hips ??= FindBestBone(allCandidates, Side.Any, "hips", "pelvis", "root");
        spine ??= FindBestBone(skinnedCandidates, Side.Any, "spine");
        spine ??= FindBestBone(allCandidates, Side.Any, "spine");
        chest ??= FindBestBone(skinnedCandidates, Side.Any, "chest", "spine1", "spine2", "torso");
        chest ??= FindBestBone(allCandidates, Side.Any, "chest", "spine1", "spine2", "torso");

        leftUpperArm ??= FindBoneByPreferredNames(skinnedCandidates, Side.Left, "L.UpperArm", "LeftUpperArm", "LeftArm", "Arm.L", "UpperArm.L");
        leftUpperArm ??= FindBestBone(skinnedCandidates, Side.Left, "upperarm", "upper_arm", "arm");
        leftUpperArm ??= FindBoneByPreferredNames(allCandidates, Side.Left, "L.UpperArm", "LeftUpperArm", "LeftArm", "Arm.L", "UpperArm.L");
        leftUpperArm ??= FindBestBone(allCandidates, Side.Left, "upperarm", "upper_arm", "arm");

        leftForeArm ??= FindBoneByPreferredNames(skinnedCandidates, Side.Left, "L.ForeArm", "L.LowerArm", "L.DownArm", "LeftForeArm", "LeftLowerArm", "ForeArm.L", "LowerArm.L");
        leftForeArm ??= FindBestBone(skinnedCandidates, Side.Left, "forearm", "lowerarm", "lower_arm", "fore_arm", "downarm");
        leftForeArm ??= FindBoneByPreferredNames(allCandidates, Side.Left, "L.ForeArm", "L.LowerArm", "L.DownArm", "LeftForeArm", "LeftLowerArm", "ForeArm.L", "LowerArm.L");
        leftForeArm ??= FindBestBone(allCandidates, Side.Left, "forearm", "lowerarm", "lower_arm", "fore_arm", "downarm");

        leftHand ??= FindBoneByPreferredNames(skinnedCandidates, Side.Left, "L.Hand", "LeftHand", "Hand.L", "L_Hand", "hand.l");
        leftHand ??= FindBestBone(skinnedCandidates, Side.Left, "hand", "wrist");
        leftHand ??= FindBoneByPreferredNames(allCandidates, Side.Left, "L.Hand", "LeftHand", "Hand.L", "L_Hand", "hand.l");
        leftHand ??= FindBestBone(allCandidates, Side.Left, "hand", "wrist");

        leftUpperLeg ??= FindBestBone(skinnedCandidates, Side.Left, "upperleg", "upleg", "thigh", "upper_leg");
        leftUpperLeg ??= FindBestBone(allCandidates, Side.Left, "upperleg", "upleg", "thigh", "upper_leg");
        leftLowerLeg ??= FindBestBone(skinnedCandidates, Side.Left, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        leftLowerLeg ??= FindBestBone(allCandidates, Side.Left, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        leftFoot ??= FindBestBone(skinnedCandidates, Side.Left, "foot", "toe", "ankle");
        leftFoot ??= FindBestBone(allCandidates, Side.Left, "foot", "toe", "ankle");

        rightUpperLeg ??= FindBestBone(skinnedCandidates, Side.Right, "upperleg", "upleg", "thigh", "upper_leg");
        rightUpperLeg ??= FindBestBone(allCandidates, Side.Right, "upperleg", "upleg", "thigh", "upper_leg");
        rightLowerLeg ??= FindBestBone(skinnedCandidates, Side.Right, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        rightLowerLeg ??= FindBestBone(allCandidates, Side.Right, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        rightFoot ??= FindBestBone(skinnedCandidates, Side.Right, "foot", "toe", "ankle");
        rightFoot ??= FindBestBone(allCandidates, Side.Right, "foot", "toe", "ankle");

        AutoFindRightArmBones(skinnedCandidates, allCandidates);
    }

    private void AutoFindRightArmBones(List<Transform> skinnedCandidates, List<Transform> allCandidates)
    {
        rightUpperArm ??= FindBoneByPreferredNames(skinnedCandidates, Side.Right, "R.UpperArm", "RightUpperArm", "RightArm", "Arm.R", "UpperArm.R");
        rightUpperArm ??= FindBestBone(skinnedCandidates, Side.Right, "upperarm", "upper_arm", "arm");
        rightUpperArm ??= FindBoneByPreferredNames(allCandidates, Side.Right, "R.UpperArm", "RightUpperArm", "RightArm", "Arm.R", "UpperArm.R");
        rightUpperArm ??= FindBestBone(allCandidates, Side.Right, "upperarm", "upper_arm", "arm");

        rightForeArm ??= FindBoneByPreferredNames(skinnedCandidates, Side.Right, "R.ForeArm", "R.LowerArm", "R.DownArm", "RightForeArm", "RightLowerArm", "ForeArm.R", "LowerArm.R");
        rightForeArm ??= FindBestBone(skinnedCandidates, Side.Right, "forearm", "lowerarm", "lower_arm", "fore_arm", "downarm");
        rightForeArm ??= FindBoneByPreferredNames(allCandidates, Side.Right, "R.ForeArm", "R.LowerArm", "R.DownArm", "RightForeArm", "RightLowerArm", "ForeArm.R", "LowerArm.R");
        rightForeArm ??= FindBestBone(allCandidates, Side.Right, "forearm", "lowerarm", "lower_arm", "fore_arm", "downarm");

        rightHand ??= FindBoneByPreferredNames(skinnedCandidates, Side.Right, "R.Hand", "RightHand", "Hand.R", "R_Hand", "hand.r");
        rightHand ??= FindBestBone(skinnedCandidates, Side.Right, "hand", "wrist");
        rightHand ??= FindBoneByPreferredNames(allCandidates, Side.Right, "R.Hand", "RightHand", "Hand.R", "R_Hand", "hand.r");
        rightHand ??= FindBestBone(allCandidates, Side.Right, "hand", "wrist");
    }

    private List<Transform> CollectSkinnedBoneCandidates()
    {
        List<Transform> candidates = new List<Transform>();
        SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        for (int i = 0; i < skinned.Length; i++)
        {
            Transform[] bones = skinned[i]?.bones;

            if (bones == null)
            {
                continue;
            }

            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                Transform bone = bones[boneIndex];

                if (bone != null && !candidates.Contains(bone))
                {
                    candidates.Add(bone);
                }
            }
        }

        return candidates;
    }

    private static Transform FindBoneByPreferredNames(
        List<Transform> candidates,
        Side side,
        params string[] preferredNames)
    {
        for (int nameIndex = 0; nameIndex < preferredNames.Length; nameIndex++)
        {
            string preferred = preferredNames[nameIndex];

            for (int i = 0; i < candidates.Count; i++)
            {
                Transform candidate = candidates[i];

                if (candidate == null)
                {
                    continue;
                }

                string lowerName = candidate.name.ToLowerInvariant();

                if (!string.Equals(candidate.name, preferred, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(lowerName, preferred.ToLowerInvariant()))
                {
                    continue;
                }

                if (side == Side.Left && !IsLeftName(lowerName))
                {
                    continue;
                }

                if (side == Side.Right && !IsRightName(lowerName))
                {
                    continue;
                }

                return candidate;
            }
        }

        return null;
    }

    private List<Transform> CollectBoneCandidates()
    {
        List<Transform> candidates = new List<Transform>();
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];

            if (candidate == null || candidate == transform)
            {
                continue;
            }

            candidates.Add(candidate);
        }

        SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);

        for (int i = 0; i < skinned.Length; i++)
        {
            Transform[] bones = skinned[i]?.bones;

            if (bones == null)
            {
                continue;
            }

            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                Transform bone = bones[boneIndex];

                if (bone != null && !candidates.Contains(bone))
                {
                    candidates.Add(bone);
                }
            }
        }

        return candidates;
    }

    private enum Side
    {
        Any,
        Left,
        Right
    }

    private static Transform FindBestBone(List<Transform> candidates, Side side, params string[] tokens)
    {
        Transform best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Transform candidate = candidates[i];
            string name = candidate.name.ToLowerInvariant();
            int score = ScoreBoneName(name, side, tokens);

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return bestScore > 0 ? best : null;
    }

    private static int ScoreBoneName(string lowerName, Side side, string[] tokens)
    {
        if (lowerName.Contains("_end"))
        {
            return int.MinValue;
        }

        bool matchesSide = side switch
        {
            Side.Left => IsLeftName(lowerName),
            Side.Right => IsRightName(lowerName),
            _ => !IsLeftName(lowerName) && !IsRightName(lowerName)
        };

        if (!matchesSide)
        {
            return int.MinValue;
        }

        int score = 0;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (lowerName.Contains(tokens[i]))
            {
                score += 10 + tokens[i].Length;
            }
        }

        if (lowerName.Contains("end") || lowerName.Contains("tip"))
        {
            score -= 5;
        }

        return score;
    }

    private static bool IsLeftName(string lowerName)
    {
        return lowerName.Contains("left")
            || lowerName.StartsWith("l.")
            || lowerName.Contains(".l")
            || lowerName.EndsWith(".l")
            || lowerName.Contains("_l")
            || lowerName.Contains(".l_");
    }

    private static bool IsRightName(string lowerName)
    {
        return lowerName.Contains("right")
            || lowerName.StartsWith("r.")
            || lowerName.Contains(".r")
            || lowerName.EndsWith(".r")
            || lowerName.EndsWith("arm.r")
            || lowerName.Contains("_r")
            || lowerName.Contains(".r_");
    }

    private bool HasWalkBones()
    {
        return leftUpperLeg != null
            && rightUpperLeg != null
            && (hips != null || spine != null || chest != null);
    }

    private bool HasAttackBones()
    {
        return attackUpperArm != null || attackForeArm != null || attackHand != null;
    }

    private void CacheBaseRotations()
    {
        baseRotations.Clear();
        CacheBone(hips);
        CacheBone(spine);
        CacheBone(chest);
        CacheBone(leftUpperArm);
        CacheBone(leftForeArm);
        CacheBone(leftHand);
        CacheBone(rightUpperArm);
        CacheBone(rightForeArm);
        CacheBone(rightHand);
        CacheBone(attackUpperArm);
        CacheBone(attackForeArm);
        CacheBone(attackHand);
        CacheBone(leftUpperLeg);
        CacheBone(leftLowerLeg);
        CacheBone(leftFoot);
        CacheBone(rightUpperLeg);
        CacheBone(rightLowerLeg);
        CacheBone(rightFoot);

        if (swordTransform != null)
        {
            CacheBone(swordTransform);
            baseSwordLocalPosition = swordTransform.localPosition;
            baseSwordLocalRotation = swordTransform.localRotation;
        }
    }

    private void CacheBone(Transform bone)
    {
        if (bone != null)
        {
            baseRotations[bone] = bone.localRotation;
        }
    }

    private void RestoreBasePose()
    {
        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;

        foreach (KeyValuePair<Transform, Quaternion> entry in baseRotations)
        {
            if (entry.Key != null)
            {
                entry.Key.localRotation = entry.Value;
            }
        }

        if (swordTransform != null)
        {
            swordTransform.localPosition = baseSwordLocalPosition;
            swordTransform.localRotation = baseSwordLocalRotation;
        }
    }

    private static Bounds CalculateRendererBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(root.position, Vector3.one * 0.1f);
        }

        return bounds;
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

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            return mainCamera.transform;
        }

        return null;
    }
}
