using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public class BatWingFlapController : MonoBehaviour
{
    [SerializeField] private Transform[] leftWingBones = Array.Empty<Transform>();
    [SerializeField] private Transform[] rightWingBones = Array.Empty<Transform>();
    [SerializeField] private bool autoFindWingBones = true;
    [SerializeField] private float flapSpeed = 9f;
    [SerializeField] private float flapAngle = 32f;
    [SerializeField] private float outerWingMultiplier = 1.15f;
    [SerializeField] private float innerWingMultiplier = 0.65f;
    [SerializeField] private float phaseOffset = 0.12f;
    [SerializeField] private Vector3 leftFlapAxis = new Vector3(0f, 0f, 1f);
    [SerializeField] private Vector3 rightFlapAxis = new Vector3(0f, 0f, -1f);
    [SerializeField] private bool useLateUpdate = true;

    private Quaternion[] baseLeftRotations = Array.Empty<Quaternion>();
    private Quaternion[] baseRightRotations = Array.Empty<Quaternion>();
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!useLateUpdate)
        {
            ApplyFlap();
        }
    }

    private void LateUpdate()
    {
        if (useLateUpdate)
        {
            ApplyFlap();
        }
    }

    private void OnDisable()
    {
        RestoreBaseRotations();
    }

    private void OnDestroy()
    {
        RestoreBaseRotations();
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return leftWingBones.Length > 0 || rightWingBones.Length > 0;
        }

        if (autoFindWingBones)
        {
            AutoFindWingBones();
        }

        if (leftWingBones.Length == 0 && rightWingBones.Length == 0)
        {
            Debug.LogWarning("[BatWingFlapController] No wing bones found. Disabling component.", this);
            enabled = false;
            return false;
        }

        CacheBaseRotations();
        initialized = true;
        return true;
    }

    private void ApplyFlap()
    {
        if (!initialized && !TryInitialize())
        {
            return;
        }

        AnimateWingChain(leftWingBones, baseLeftRotations, leftFlapAxis, false);
        AnimateWingChain(rightWingBones, baseRightRotations, rightFlapAxis, true);
    }

    private void AnimateWingChain(
        Transform[] bones,
        Quaternion[] baseRotations,
        Vector3 flapAxis,
        bool mirrorPhase)
    {
        if (bones == null || baseRotations == null || bones.Length == 0)
        {
            return;
        }

        int wingCount = bones.Length;
        float maxIndex = Mathf.Max(1, wingCount - 1);

        for (int i = 0; i < wingCount; i++)
        {
            Transform bone = bones[i];

            if (bone == null)
            {
                continue;
            }

            float chainT = i / maxIndex;
            float multiplier = Mathf.Lerp(innerWingMultiplier, outerWingMultiplier, chainT);
            float phase = mirrorPhase ? -i * phaseOffset : i * phaseOffset;
            float flap = Mathf.Sin((Time.time * flapSpeed) + phase);
            float angle = flap * flapAngle * multiplier;
            bone.localRotation = baseRotations[i] * Quaternion.AngleAxis(angle, flapAxis.normalized);
        }
    }

    private void AutoFindWingBones()
    {
        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        List<Transform> left = new List<Transform>();
        List<Transform> right = new List<Transform>();

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];

            if (candidate == null || candidate == transform)
            {
                continue;
            }

            string name = candidate.name.ToLowerInvariant();

            if (!name.Contains("wing") || name.Contains("_end"))
            {
                continue;
            }

            if (IsLeftWingName(name))
            {
                left.Add(candidate);
                continue;
            }

            if (IsRightWingName(name))
            {
                right.Add(candidate);
            }
        }

        leftWingBones = SortWingBones(left);
        rightWingBones = SortWingBones(right);
    }

    private static bool IsLeftWingName(string lowerName)
    {
        return lowerName.Contains("left")
            || lowerName.Contains(" l ")
            || lowerName.StartsWith("l_")
            || lowerName.EndsWith("_l")
            || lowerName.EndsWith(".l")
            || lowerName.Contains(".l")
            || lowerName.Contains("_l_")
            || lowerName.Contains("wing_l");
    }

    private static bool IsRightWingName(string lowerName)
    {
        return lowerName.Contains("right")
            || lowerName.Contains(" r ")
            || lowerName.StartsWith("r_")
            || lowerName.EndsWith("_r")
            || lowerName.EndsWith(".r")
            || lowerName.Contains(".r")
            || lowerName.Contains("_r_")
            || lowerName.Contains("wing_r");
    }

    private static Transform[] SortWingBones(List<Transform> bones)
    {
        if (bones == null || bones.Count == 0)
        {
            return Array.Empty<Transform>();
        }

        bones.Sort(CompareWingBoneOrder);
        return bones.ToArray();
    }

    private static int CompareWingBoneOrder(Transform a, Transform b)
    {
        int orderA = ExtractWingIndex(a != null ? a.name : string.Empty);
        int orderB = ExtractWingIndex(b != null ? b.name : string.Empty);

        if (orderA != orderB)
        {
            return orderA.CompareTo(orderB);
        }

        return string.Compare(a.name, b.name, StringComparison.Ordinal);
    }

    private static int ExtractWingIndex(string boneName)
    {
        if (string.IsNullOrEmpty(boneName))
        {
            return int.MaxValue;
        }

        int bestIndex = int.MaxValue;
        string lower = boneName.ToLowerInvariant();

        for (int i = 0; i < lower.Length; i++)
        {
            if (!char.IsDigit(lower[i]))
            {
                continue;
            }

            int value = 0;
            int j = i;

            while (j < lower.Length && char.IsDigit(lower[j]))
            {
                value = (value * 10) + (lower[j] - '0');
                j++;
            }

            if (value < bestIndex)
            {
                bestIndex = value;
            }
        }

        return bestIndex;
    }

    private void CacheBaseRotations()
    {
        baseLeftRotations = new Quaternion[leftWingBones.Length];
        baseRightRotations = new Quaternion[rightWingBones.Length];

        for (int i = 0; i < leftWingBones.Length; i++)
        {
            baseLeftRotations[i] = leftWingBones[i] != null
                ? leftWingBones[i].localRotation
                : Quaternion.identity;
        }

        for (int i = 0; i < rightWingBones.Length; i++)
        {
            baseRightRotations[i] = rightWingBones[i] != null
                ? rightWingBones[i].localRotation
                : Quaternion.identity;
        }
    }

    private void RestoreBaseRotations()
    {
        RestoreChain(leftWingBones, baseLeftRotations);
        RestoreChain(rightWingBones, baseRightRotations);
    }

    private static void RestoreChain(Transform[] bones, Quaternion[] baseRotations)
    {
        if (bones == null || baseRotations == null)
        {
            return;
        }

        int count = Mathf.Min(bones.Length, baseRotations.Length);

        for (int i = 0; i < count; i++)
        {
            if (bones[i] != null)
            {
                bones[i].localRotation = baseRotations[i];
            }
        }
    }
}
