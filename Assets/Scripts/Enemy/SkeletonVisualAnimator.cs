using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(260)]
public class SkeletonVisualAnimator : MonoBehaviour
{
    [Header("Walk")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float legSwingAngle = 18f;
    [SerializeField] private float armSwingAngle = 10f;
    [SerializeField] private float bodyBobAmount = 0.025f;
    [SerializeField] private float bodyBobSpeed = 6.5f;
    [SerializeField] private float bodySwayAngle = 2f;
    [SerializeField] private float moveThreshold = 0.015f;
    [SerializeField] private float walkFrequency = 6.5f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 3.2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackDuration = 0.7f;
    [SerializeField] private float attackRaiseAngle = -85f;
    [SerializeField] private float attackSlashAngle = 120f;
    [SerializeField] private float attackSideAngle = 25f;

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

    [Header("Safety")]
    [SerializeField] private bool enableFallbackVisuals = true;

    private enum AttackAxisMode
    {
        LocalX,
        LocalZ,
        LocalY,
        XZCombined
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
        LogPeriodicState(attackNormalized);

        switch (attackMode)
        {
            case AttackMode.Bones:
                ApplyBoneIdleOrWalk(attackBlend);
                ApplyBoneAttack(attackBlend, attackNormalized);
                UpdateBoneAttackSlashArc(attackBlend, attackNormalized);
                break;

            case AttackMode.SwordTransform:
                ApplyBoneIdleOrWalk(attackBlend);
                ApplyBoneAttack(attackBlend, attackNormalized);
                ApplySwordTransformAttack(attackBlend, attackNormalized);
                UpdateBoneAttackSlashArc(attackBlend, attackNormalized);
                break;

            case AttackMode.ModelFallback:
                ApplyModelRootAnimation(attackBlend, attackNormalized);
                break;

            default:
                ApplyModelRootAnimation(attackBlend, attackNormalized);
                break;
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

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        lastEnemyPosition = enemyRoot.position;

        if (autoFindBones)
        {
            AutoFindBones();
            swordTransform ??= FindSwordTransform();
        }

        DisableBlockingAnimators();

        CacheBaseRotations();
        useModelRootFallback = !HasWalkBones();
        usingBoneWalk = !useModelRootFallback;
        attackMode = ResolveAttackMode();
        usingBoneAttack = attackMode == AttackMode.Bones || attackMode == AttackMode.SwordTransform;
        ReportRigStatus();

        if (useModelRootFallback)
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] skeleton 1 rig bones not found, fallback used.",
                this);
        }

        if (!usingBoneAttack && attackMode == AttackMode.ModelFallback)
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Right arm bones not found; attack uses slash arc fallback.",
                this);
        }
        else if (rightUpperArm == null && rightForeArm == null && rightHand == null)
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Right arm bones not found (rightUpperArm/rightForeArm/rightHand=null).",
                this);
        }

        initialized = true;
        return true;
    }

    private void ReportRigStatus()
    {
        if (rigReported)
        {
            return;
        }

        rigReported = true;

        SkinnedMeshRenderer[] skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int skinnedBoneCount = 0;

        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i]?.bones != null)
            {
                skinnedBoneCount += skinned[i].bones.Length;
            }
        }

        Debug.Log(
            "[SkeletonVisualAnimator] skinnedMeshes="
            + skinned.Length
            + " skinnedBones="
            + skinnedBoneCount
            + " hips="
            + BoneName(hips)
            + " spine="
            + BoneName(spine)
            + " rightUpperArm="
            + BoneName(rightUpperArm)
            + " rightForeArm="
            + BoneName(rightForeArm)
            + " rightHand="
            + BoneName(rightHand)
            + " leftUpperLeg="
            + BoneName(leftUpperLeg)
            + " rightUpperLeg="
            + BoneName(rightUpperLeg)
            + " sword="
            + BoneName(swordTransform)
            + " walkMode="
            + (usingBoneWalk ? "bones" : "fallback")
            + " attackMode="
            + attackMode,
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
        Vector3 delta = enemyRoot.position - lastEnemyPosition;
        lastEnemyPosition = enemyRoot.position;
        delta.y = 0f;
        float instantSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        smoothedMoveSpeed = Mathf.Lerp(smoothedMoveSpeed, instantSpeed, Time.deltaTime * 8f);
        bool isMoving = smoothedMoveSpeed > moveThreshold;

        if (isMoving)
        {
            walkPhase += Time.deltaTime * walkFrequency * Mathf.Clamp(smoothedMoveSpeed * 0.35f, 0.6f, 1.6f);
        }
    }

    private float UpdateAttackState(out float attackNormalized)
    {
        attackNormalized = 0f;
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
            "[SkeletonVisualAnimator] Attack trigger distance="
            + distance.ToString("F2")
            + " rightUpperArm="
            + BoneName(rightUpperArm)
            + " rightForeArm="
            + BoneName(rightForeArm)
            + " rightHand="
            + BoneName(rightHand)
            + " sword="
            + BoneName(swordTransform)
            + " mode="
            + attackMode,
            this);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogPeriodicState(float attackNormalized)
    {
        if (Time.time < nextStateLogTime)
        {
            return;
        }

        nextStateLogTime = Time.time + 2f;
        bool isMoving = smoothedMoveSpeed > moveThreshold;
        bool canAttack = Time.time >= nextAttackTime;

        Debug.Log(
            "[SkeletonVisualAnimator] state moving="
            + isMoving
            + " distance="
            + lastAttackDistance.ToString("F2")
            + " canAttack="
            + canAttack
            + " attackTimer="
            + (attackActive ? attackNormalized.ToString("F2") : "idle")
            + " attackMode="
            + attackMode,
            this);
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
            : Mathf.Lerp(1f, 0.15f, Mathf.Clamp01((normalized - 0.35f) / 0.3f));

        float strike = 0f;

        if (normalized >= 0.35f && normalized <= 0.65f)
        {
            strike = Mathf.SmoothStep(0f, 1f, (normalized - 0.35f) / 0.3f);
        }
        else if (normalized > 0.65f)
        {
            strike = Mathf.Lerp(1f, 0f, (normalized - 0.65f) / 0.35f);
        }

        raiseAmount = attackRaiseAngle * windUp;
        slashAmount = attackSlashAngle * strike;
        sideAmount = attackSideAngle * Mathf.Max(windUp, strike);
    }

    private void ApplyModelRootAnimation(float attackBlend, float attackNormalized)
    {
        bool isMoving = smoothedMoveSpeed > moveThreshold;
        float bob = isMoving
            ? Mathf.Sin(walkPhase) * walkBobAmount
            : Mathf.Sin(Time.time * bodyBobSpeed) * (walkBobAmount * 0.35f);
        float sway = isMoving ? Mathf.Sin(walkPhase) * walkSwayAngle : 0f;
        float lean = isMoving ? Mathf.Cos(walkPhase * 0.5f) * walkLeanAngle : 0f;

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

        float raise = attackRaiseAngle * attackBlend;
        float slash = attackSlashAngle * attackBlend;
        float side = attackSideAngle * attackBlend;

        swordTransform.localRotation =
            baseSwordLocalRotation * Quaternion.Euler(raise + slash, side, 0f);
        swordTransform.localPosition =
            baseSwordLocalPosition + new Vector3(0f, attackBlend * 0.04f, attackBlend * 0.02f);
    }

    private void ApplyBoneIdleOrWalk(float attackBlend)
    {
        bool isMoving = smoothedMoveSpeed > moveThreshold;
        float bob = isMoving
            ? Mathf.Sin(walkPhase) * bodyBobAmount
            : Mathf.Sin(Time.time * bodyBobSpeed) * (bodyBobAmount * 0.35f);
        float sway = isMoving ? Mathf.Sin(walkPhase * 0.5f) * bodySwayAngle : 0f;

        ApplyBoneRotation(hips, new Vector3(0f, 0f, sway), bob * 20f);
        ApplyBoneRotation(spine, new Vector3(0f, 0f, sway * 0.6f), bob * 12f);
        ApplyBoneRotation(chest, new Vector3(0f, 0f, sway * 0.35f), bob * 8f);

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

        ApplyBoneRotation(leftUpperArm, new Vector3(-armSwing * 0.7f, 0f, 0f));
        ApplyBoneRotation(leftForeArm, new Vector3(-armSwing * 0.35f, 0f, 0f));

        if (attackBlend <= 0.02f)
        {
            float rightArmSwing = armSwing * 0.25f;
            ApplyBoneRotation(rightUpperArm, new Vector3(rightArmSwing, 0f, 0f));
            ApplyBoneRotation(rightForeArm, new Vector3(rightArmSwing * 0.5f, 0f, 0f));
        }
    }

    private void ApplyBoneAttack(float attackBlend, float attackNormalized)
    {
        if (attackBlend <= 0.001f || !HasAttackBones())
        {
            return;
        }

        GetAttackPhaseAngles(attackNormalized, out float raiseAmount, out float slashAmount, out float sideAmount);

        Vector3 upperArmEuler = BuildAttackEuler(
            raiseAmount,
            slashAmount,
            sideAmount,
            rightUpperArmRaiseAxis,
            rightUpperArmSlashAxis);
        Vector3 foreArmEuler = BuildAttackEuler(
            raiseAmount * 0.45f,
            slashAmount * 0.85f,
            sideAmount * 0.5f,
            rightForeArmAxis,
            rightForeArmAxis);
        Vector3 handEuler = BuildAttackEuler(
            raiseAmount * 0.2f,
            slashAmount * 0.55f,
            sideAmount * 0.35f,
            rightHandAxis,
            rightHandAxis);

        ApplyBoneRotationAbsolute(rightUpperArm, upperArmEuler);
        ApplyBoneRotationAbsolute(rightForeArm, foreArmEuler);
        ApplyBoneRotationAbsolute(rightHand, handEuler);
        ApplyBoneRotation(chest, new Vector3(-slashAmount * 0.06f, sideAmount * 0.15f, 0f));
        ApplyBoneRotation(spine, new Vector3(-slashAmount * 0.04f, 0f, 0f));

        if (swordTransform != null && baseRotations.ContainsKey(swordTransform))
        {
            Vector3 swordEuler = BuildAttackEuler(
                raiseAmount * 0.25f,
                slashAmount * 0.65f,
                sideAmount * 0.25f,
                Vector3.right,
                Vector3.forward);
            ApplyBoneRotationAbsolute(swordTransform, swordEuler);
        }
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
                return new Vector3(raise + slash, side, 0f);

            case AttackAxisMode.LocalZ:
                return new Vector3(0f, side, raise + slash);

            case AttackAxisMode.LocalY:
                return new Vector3(0f, raise + slash + side, 0f);

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
        slashArcHideTime = Time.time + 0.25f;

        float strikeT = Mathf.InverseLerp(0.35f, 0.65f, attackNormalized);
        Transform arcTransform = slashArcVisual.transform;
        arcTransform.localPosition = new Vector3(0.15f, 0.9f, 0.45f + strikeT * 0.2f);
        arcTransform.localRotation = Quaternion.Euler(-35f - attackSlashAngle * strikeT * 0.35f, 25f, 35f);
        arcTransform.localScale = new Vector3(0.7f, 0.05f, 0.22f + strikeT * 0.35f);

        if (slashLineRenderer != null)
        {
            float alpha = Mathf.Lerp(0.85f, 0.15f, Mathf.Abs(strikeT - 0.5f) * 2f);
            Color color = new Color(1f, 0.92f, 0.45f, alpha);
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
        slashLineRenderer.startWidth = 0.08f;
        slashLineRenderer.endWidth = 0.02f;
        slashLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        slashLineRenderer.receiveShadows = false;
        slashLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        slashLineRenderer.material.color = new Color(1f, 0.92f, 0.45f, 0.85f);

        Vector3[] points = new Vector3[8];

        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            float angle = Mathf.Lerp(-70f, 40f, t) * Mathf.Deg2Rad;
            points[i] = new Vector3(Mathf.Sin(angle) * 0.55f, Mathf.Cos(angle) * 0.55f, 0f);
        }

        slashLineRenderer.SetPositions(points);

        GameObject meshFallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        meshFallback.name = "SlashArcMesh";
        meshFallback.transform.SetParent(slashArcVisual.transform, false);
        meshFallback.transform.localPosition = Vector3.zero;
        meshFallback.transform.localScale = new Vector3(0.55f, 0.04f, 0.2f);

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

    private Transform FindSwordTransform()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];

            if (candidate == null || candidate == transform)
            {
                continue;
            }

            string name = candidate.name.ToLowerInvariant();

            if (name.Contains("sword")
                || name.Contains("blade")
                || name.Contains("weapon")
                || name.Contains("katana")
                || name.Contains("knife"))
            {
                return candidate;
            }
        }

        return null;
    }

    private void DisableBlockingAnimators()
    {
        Animator[] animators = GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];

            if (animator == null)
            {
                continue;
            }

            animator.applyRootMotion = false;

            if (animator.runtimeAnimatorController == null)
            {
                animator.enabled = false;
            }
        }
    }

    private void AutoFindBones()
    {
        List<Transform> candidates = CollectBoneCandidates();

        hips ??= FindBestBone(candidates, Side.Any, "hips", "pelvis", "root");
        spine ??= FindBestBone(candidates, Side.Any, "spine");
        chest ??= FindBestBone(candidates, Side.Any, "chest", "spine1", "spine2", "torso");
        leftUpperArm ??= FindBestBone(candidates, Side.Left, "upperarm", "upper_arm", "arm");
        leftForeArm ??= FindBestBone(
            candidates,
            Side.Left,
            "forearm",
            "lowerarm",
            "lower_arm",
            "fore_arm",
            "downarm");
        leftHand ??= FindBestBone(candidates, Side.Left, "hand", "wrist");
        leftUpperLeg ??= FindBestBone(candidates, Side.Left, "upperleg", "upleg", "thigh", "upper_leg");
        leftLowerLeg ??= FindBestBone(candidates, Side.Left, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        leftFoot ??= FindBestBone(candidates, Side.Left, "foot", "toe", "ankle");
        rightUpperLeg ??= FindBestBone(candidates, Side.Right, "upperleg", "upleg", "thigh", "upper_leg");
        rightLowerLeg ??= FindBestBone(candidates, Side.Right, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        rightFoot ??= FindBestBone(candidates, Side.Right, "foot", "toe", "ankle");

        AutoFindRightArmBones(candidates);
    }

    private void AutoFindRightArmBones(List<Transform> candidates)
    {
        rightUpperArm ??= FindBoneByPreferredNames(
            candidates,
            Side.Right,
            "R.UpperArm",
            "RightUpperArm",
            "RightArm",
            "Arm.R",
            "UpperArm.R");
        rightForeArm ??= FindBoneByPreferredNames(
            candidates,
            Side.Right,
            "R.ForeArm",
            "R.LowerArm",
            "RightForeArm",
            "RightLowerArm",
            "ForeArm.R",
            "LowerArm.R");
        rightHand ??= FindBoneByPreferredNames(
            candidates,
            Side.Right,
            "R.Hand",
            "RightHand",
            "Hand.R");

        rightUpperArm ??= FindBestBone(candidates, Side.Right, "upperarm", "upper_arm", "arm");
        rightForeArm ??= FindBestBone(
            candidates,
            Side.Right,
            "forearm",
            "lowerarm",
            "lower_arm",
            "fore_arm",
            "downarm");
        rightHand ??= FindBestBone(candidates, Side.Right, "hand", "wrist");
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
        return rightUpperArm != null || rightForeArm != null || rightHand != null;
    }

    private void CacheBaseRotations()
    {
        baseRotations.Clear();
        CacheBone(hips);
        CacheBone(spine);
        CacheBone(chest);
        CacheBone(leftUpperArm);
        CacheBone(leftForeArm);
        CacheBone(rightUpperArm);
        CacheBone(rightForeArm);
        CacheBone(rightHand);
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
