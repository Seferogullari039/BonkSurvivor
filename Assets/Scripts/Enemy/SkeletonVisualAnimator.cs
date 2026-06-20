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
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackCooldown = 1.15f;
    [SerializeField] private float attackDuration = 0.55f;
    [SerializeField] private float attackRaiseAngle = -50f;
    [SerializeField] private float attackSlashAngle = 75f;
    [SerializeField] private float attackSideAngle = 10f;

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
        float attackBlend = UpdateAttackState();
        float attackNormalized = attackActive
            ? (Time.time - attackStartTime) / Mathf.Max(0.001f, attackDuration)
            : 0f;

        switch (attackMode)
        {
            case AttackMode.Bones:
                ApplyBoneIdleOrWalk(attackBlend);
                ApplyBoneAttack(attackBlend);
                HideSlashArc();
                break;

            case AttackMode.SwordTransform:
                ApplyBoneIdleOrWalk(attackBlend);
                ApplyBoneAttack(attackBlend);
                ApplySwordTransformAttack(attackBlend, attackNormalized);
                HideSlashArc();
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

    private float UpdateAttackState()
    {
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

        if (!isMoving || attackBlend > 0.05f)
        {
            return;
        }

        float legSwing = Mathf.Sin(walkPhase) * legSwingAngle;
        float armSwing = Mathf.Sin(walkPhase + Mathf.PI) * armSwingAngle;
        float rightArmSwing = armSwing * 0.3f;

        ApplyBoneRotation(leftUpperLeg, new Vector3(legSwing, 0f, 0f));
        ApplyBoneRotation(leftLowerLeg, new Vector3(-legSwing * 0.55f, 0f, 0f));
        ApplyBoneRotation(leftFoot, new Vector3(legSwing * 0.25f, 0f, 0f));

        ApplyBoneRotation(rightUpperLeg, new Vector3(-legSwing, 0f, 0f));
        ApplyBoneRotation(rightLowerLeg, new Vector3(legSwing * 0.55f, 0f, 0f));
        ApplyBoneRotation(rightFoot, new Vector3(-legSwing * 0.25f, 0f, 0f));

        ApplyBoneRotation(leftUpperArm, new Vector3(-armSwing * 0.7f, 0f, 0f));
        ApplyBoneRotation(leftForeArm, new Vector3(-armSwing * 0.35f, 0f, 0f));
        ApplyBoneRotation(rightUpperArm, new Vector3(rightArmSwing, 0f, 0f));
        ApplyBoneRotation(rightForeArm, new Vector3(rightArmSwing * 0.5f, 0f, 0f));
    }

    private void ApplyBoneAttack(float attackBlend)
    {
        if (attackBlend <= 0.001f)
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

        if (swordTransform != null && baseRotations.ContainsKey(swordTransform))
        {
            ApplyBoneRotation(swordTransform, new Vector3(slash * 0.25f, side * 0.15f, 0f));
        }
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

    private void UpdateFallbackSlashArc(float attackBlend, float attackNormalized)
    {
        if (attackBlend <= 0.05f || attackNormalized < 0.2f || attackNormalized > 0.65f)
        {
            HideSlashArc();
            return;
        }

        EnsureSlashArcVisual();
        slashArcVisual.SetActive(true);

        float strikeT = Mathf.InverseLerp(0.2f, 0.55f, attackNormalized);
        Transform arcTransform = slashArcVisual.transform;
        arcTransform.localPosition = new Vector3(0.08f, 0.55f, 0.35f + strikeT * 0.15f);
        arcTransform.localRotation = Quaternion.Euler(-20f - attackSlashAngle * strikeT, 15f, 0f);
        arcTransform.localScale = new Vector3(0.55f, 0.04f, 0.18f + strikeT * 0.25f);
    }

    private void EnsureSlashArcVisual()
    {
        if (slashArcVisual != null)
        {
            return;
        }

        slashArcVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slashArcVisual.name = "SlashArcVisual";
        slashArcVisual.transform.SetParent(transform, false);

        Collider collider = slashArcVisual.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = slashArcVisual.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material material = renderer.material;
            material.color = new Color(0.95f, 0.85f, 0.35f, 0.55f);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
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

            if (name.Contains("sword") || name.Contains("blade") || name.Contains("weapon"))
            {
                return candidate;
            }
        }

        return null;
    }

    private void AutoFindBones()
    {
        List<Transform> candidates = CollectBoneCandidates();

        hips ??= FindBestBone(candidates, Side.Any, "hips", "pelvis", "root");
        spine ??= FindBestBone(candidates, Side.Any, "spine");
        chest ??= FindBestBone(candidates, Side.Any, "chest", "spine1", "spine2", "torso");
        leftUpperArm ??= FindBestBone(candidates, Side.Left, "upperarm", "upper_arm", "arm");
        leftForeArm ??= FindBestBone(candidates, Side.Left, "forearm", "lowerarm", "lower_arm", "fore_arm");
        rightUpperArm ??= FindBestBone(candidates, Side.Right, "upperarm", "upper_arm", "arm");
        rightForeArm ??= FindBestBone(candidates, Side.Right, "forearm", "lowerarm", "lower_arm", "fore_arm");
        rightHand ??= FindBestBone(candidates, Side.Right, "hand", "wrist");
        leftUpperLeg ??= FindBestBone(candidates, Side.Left, "upperleg", "upleg", "thigh", "upper_leg");
        leftLowerLeg ??= FindBestBone(candidates, Side.Left, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        leftFoot ??= FindBestBone(candidates, Side.Left, "foot", "toe", "ankle");
        rightUpperLeg ??= FindBestBone(candidates, Side.Right, "upperleg", "upleg", "thigh", "upper_leg");
        rightLowerLeg ??= FindBestBone(candidates, Side.Right, "downleg", "lowerleg", "lower_leg", "leg", "calf", "shin");
        rightFoot ??= FindBestBone(candidates, Side.Right, "foot", "toe", "ankle");
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
