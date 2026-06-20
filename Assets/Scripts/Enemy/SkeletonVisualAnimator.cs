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
    [SerializeField] private float armSwingAngle = 12f;
    [SerializeField] private float bodyBobAmount = 0.035f;
    [SerializeField] private float bodyBobSpeed = 7f;
    [SerializeField] private float moveThreshold = 0.015f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackDuration = 0.55f;
    [SerializeField] private float attackRaiseAngle = -55f;
    [SerializeField] private float attackSlashAngle = 75f;
    [SerializeField] private float attackSideAngle = 15f;

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

    private readonly Dictionary<Transform, Quaternion> baseRotations = new Dictionary<Transform, Quaternion>();
    private Transform enemyRoot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 lastEnemyPosition;
    private float smoothedMoveSpeed;
    private float walkPhase;
    private float nextAttackTime;
    private float attackStartTime;
    private bool attackActive;
    private bool attackVisualEnabled = true;
    private bool useModelRootFallback;
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void LateUpdate()
    {
        if (!initialized && !TryInitialize())
        {
            return;
        }

        UpdateMovementSample();
        float attackBlend = UpdateAttackState();

        if (useModelRootFallback)
        {
            ApplyModelRootAnimation(attackBlend);
            return;
        }

        ApplyBoneIdleOrWalk(attackBlend);
        ApplyBoneAttack(attackBlend);
    }

    private void OnDisable()
    {
        RestoreBasePose();
    }

    private void OnDestroy()
    {
        RestoreBasePose();
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
        }

        CacheBaseRotations();
        useModelRootFallback = !HasWalkBones();

        if (!HasAttackBones())
        {
            attackVisualEnabled = false;
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Right arm bones not found, sword attack visual disabled.",
                this);
        }

        if (useModelRootFallback)
        {
            Debug.LogWarning(
                "[SkeletonVisualAnimator] Rig bones not found, using model-root procedural fallback.",
                this);
        }

        initialized = true;
        return true;
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
            walkPhase += Time.deltaTime * walkSpeed * Mathf.Clamp(smoothedMoveSpeed * 0.35f, 0.6f, 1.6f);
        }
    }

    private float UpdateAttackState()
    {
        if (!attackVisualEnabled)
        {
            return 0f;
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

        if (!attackActive && distance <= attackRange && Time.time >= nextAttackTime)
        {
            attackActive = true;
            attackStartTime = Time.time;
            nextAttackTime = Time.time + attackCooldown;
        }

        if (!attackActive)
        {
            return 0f;
        }

        float normalized = (Time.time - attackStartTime) / Mathf.Max(0.001f, attackDuration);

        if (normalized >= 1f)
        {
            attackActive = false;
            return 0f;
        }

        return EvaluateAttackCurve(normalized);
    }

    private static float EvaluateAttackCurve(float normalized)
    {
        if (normalized < 0.25f)
        {
            return Mathf.SmoothStep(0f, 1f, normalized / 0.25f);
        }

        if (normalized < 0.55f)
        {
            return 1f;
        }

        return 1f - Mathf.SmoothStep(0f, 1f, (normalized - 0.55f) / 0.45f);
    }

    private void ApplyModelRootAnimation(float attackBlend)
    {
        bool isMoving = smoothedMoveSpeed > moveThreshold;
        float bob = isMoving
            ? Mathf.Sin(walkPhase) * bodyBobAmount
            : Mathf.Sin(Time.time * bodyBobSpeed) * (bodyBobAmount * 0.35f);
        float sway = isMoving ? Mathf.Sin(walkPhase) * 4f : 0f;

        float attackRaise = attackBlend * attackRaiseAngle;
        float attackSlash = attackBlend * attackSlashAngle;
        float attackSide = attackBlend * attackSideAngle;

        transform.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        transform.localRotation =
            baseLocalRotation
            * Quaternion.Euler(attackRaise + attackSlash * 0.35f, attackSide, sway);
    }

    private void ApplyBoneIdleOrWalk(float attackBlend)
    {
        bool isMoving = smoothedMoveSpeed > moveThreshold;
        float bob = isMoving
            ? Mathf.Sin(walkPhase) * bodyBobAmount
            : Mathf.Sin(Time.time * bodyBobSpeed) * (bodyBobAmount * 0.35f);

        ApplyBoneRotation(hips, Vector3.zero, bob * 20f);
        ApplyBoneRotation(spine, Vector3.zero, bob * 12f);
        ApplyBoneRotation(chest, Vector3.zero, bob * 8f);

        if (!isMoving || attackBlend > 0.05f)
        {
            return;
        }

        float legSwing = Mathf.Sin(walkPhase) * legSwingAngle;
        float armSwing = Mathf.Sin(walkPhase) * armSwingAngle;

        ApplyBoneRotation(leftUpperLeg, new Vector3(legSwing, 0f, 0f));
        ApplyBoneRotation(leftLowerLeg, new Vector3(-legSwing * 0.55f, 0f, 0f));
        ApplyBoneRotation(leftFoot, new Vector3(legSwing * 0.25f, 0f, 0f));

        ApplyBoneRotation(rightUpperLeg, new Vector3(-legSwing, 0f, 0f));
        ApplyBoneRotation(rightLowerLeg, new Vector3(legSwing * 0.55f, 0f, 0f));
        ApplyBoneRotation(rightFoot, new Vector3(-legSwing * 0.25f, 0f, 0f));

        ApplyBoneRotation(leftUpperArm, new Vector3(-armSwing * 0.7f, 0f, 0f));
        ApplyBoneRotation(leftForeArm, new Vector3(-armSwing * 0.35f, 0f, 0f));
        ApplyBoneRotation(rightUpperArm, new Vector3(armSwing * 0.35f, 0f, 0f));
        ApplyBoneRotation(rightForeArm, new Vector3(armSwing * 0.2f, 0f, 0f));
    }

    private void ApplyBoneAttack(float attackBlend)
    {
        if (attackBlend <= 0.001f || !attackVisualEnabled)
        {
            return;
        }

        float raise = attackRaiseAngle * attackBlend;
        float slash = attackSlashAngle * attackBlend;
        float side = attackSideAngle * attackBlend;

        ApplyBoneRotation(rightUpperArm, new Vector3(raise + slash * 0.45f, side, 0f));
        ApplyBoneRotation(rightForeArm, new Vector3(raise * 0.35f + slash * 0.75f, side * 0.5f, 0f));
        ApplyBoneRotation(rightHand, new Vector3(slash * 0.35f, side * 0.25f, 0f));
        ApplyBoneRotation(chest, new Vector3(-slash * 0.08f, side * 0.2f, 0f));
        ApplyBoneRotation(spine, new Vector3(-slash * 0.05f, 0f, 0f));
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

    private void AutoFindBones()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        List<Transform> candidates = new List<Transform>();

        for (int i = 0; i < all.Length; i++)
        {
            Transform candidate = all[i];

            if (candidate == null || candidate == transform)
            {
                continue;
            }

            candidates.Add(candidate);
        }

        hips ??= FindBestBone(candidates, Side.Any, "hips", "pelvis");
        spine ??= FindBestBone(candidates, Side.Any, "spine");
        chest ??= FindBestBone(candidates, Side.Any, "chest", "spine1", "spine2");
        leftUpperArm ??= FindBestBone(candidates, Side.Left, "upperarm", "upper_arm", "arm");
        leftForeArm ??= FindBestBone(candidates, Side.Left, "forearm", "lowerarm", "lower_arm");
        rightUpperArm ??= FindBestBone(candidates, Side.Right, "upperarm", "upper_arm", "arm");
        rightForeArm ??= FindBestBone(candidates, Side.Right, "forearm", "lowerarm", "lower_arm");
        rightHand ??= FindBestBone(candidates, Side.Right, "hand");
        leftUpperLeg ??= FindBestBone(candidates, Side.Left, "upleg", "upperleg", "thigh");
        leftLowerLeg ??= FindBestBone(candidates, Side.Left, "leg", "lowerleg", "calf", "shin");
        leftFoot ??= FindBestBone(candidates, Side.Left, "foot", "toe");
        rightUpperLeg ??= FindBestBone(candidates, Side.Right, "upleg", "upperleg", "thigh");
        rightLowerLeg ??= FindBestBone(candidates, Side.Right, "leg", "lowerleg", "calf", "shin");
        rightFoot ??= FindBestBone(candidates, Side.Right, "foot", "toe");
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
                score += 10;
            }
        }

        if (side == Side.Left && lowerName.Contains("arm") && lowerName.Contains("fore"))
        {
            score -= 5;
        }

        if (side == Side.Right && lowerName.Contains("arm") && lowerName.Contains("upper"))
        {
            score += 2;
        }

        return score;
    }

    private static bool IsLeftName(string lowerName)
    {
        return lowerName.Contains("left")
            || lowerName.EndsWith(".l")
            || lowerName.Contains("_l")
            || lowerName.Contains(".l_");
    }

    private static bool IsRightName(string lowerName)
    {
        return lowerName.Contains("right")
            || lowerName.EndsWith(".r")
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
