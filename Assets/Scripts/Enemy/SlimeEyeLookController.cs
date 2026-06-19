using UnityEngine;

public enum SlimeEyeLookTargetMode
{
    CameraThenPlayer
}

[DisallowMultipleComponent]
public class SlimeEyeLookController : MonoBehaviour
{
    [SerializeField] private SlimeEyeLookTargetMode lookTargetMode = SlimeEyeLookTargetMode.CameraThenPlayer;
    [SerializeField] private float eyeFollowAmount = 0.045f;
    [SerializeField] private float eyeFollowSpeed = 12f;
    [SerializeField] private float maxEyeOffset = 0.055f;
    [SerializeField] private bool enableProceduralPupilOverlay = true;
    [SerializeField] private Vector3 leftPupilLocalPosition = new Vector3(-0.014f, 0.016f, 0.024f);
    [SerializeField] private Vector3 rightPupilLocalPosition = new Vector3(0.014f, 0.016f, 0.024f);
    [SerializeField] private float pupilScale = 0.05f;
    [SerializeField] private Material pupilMaterial;

    private Transform modelTransform;
    private Transform leftPupilTransform;
    private Transform rightPupilTransform;
    private Transform leftEyeTransform;
    private Transform rightEyeTransform;
    private Vector3 leftBaseLocalPosition;
    private Vector3 rightBaseLocalPosition;
    private bool usesProceduralOverlay;
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

        MaintainOverlayWorldScale(leftPupilTransform);
        MaintainOverlayWorldScale(rightPupilTransform);

        if (!TryResolveLookTarget(out Vector3 lookTargetWorldPosition))
        {
            return;
        }

        if (usesProceduralOverlay)
        {
            UpdateProceduralPupilLook(leftPupilTransform, leftBaseLocalPosition, lookTargetWorldPosition);
            UpdateProceduralPupilLook(rightPupilTransform, rightBaseLocalPosition, lookTargetWorldPosition);
            return;
        }

        UpdateTransformLook(leftEyeTransform, lookTargetWorldPosition);
        UpdateTransformLook(rightEyeTransform, lookTargetWorldPosition);
        UpdateTransformLook(leftPupilTransform, lookTargetWorldPosition);
        UpdateTransformLook(rightPupilTransform, lookTargetWorldPosition);
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return modelTransform != null;
        }

        Transform visualRoot = ResolveVisualRoot();

        if (visualRoot == null)
        {
            enabled = false;
            return false;
        }

        modelTransform = visualRoot.Find("Model");

        if (modelTransform == null || !modelTransform.gameObject.activeInHierarchy)
        {
            enabled = false;
            return false;
        }

        CacheNamedEyeTransforms(modelTransform);

        if (leftPupilTransform == null && rightPupilTransform == null && enableProceduralPupilOverlay)
        {
            CreateProceduralPupilOverlays();
        }

        usesProceduralOverlay = leftPupilTransform != null
            && rightPupilTransform != null
            && leftPupilTransform.name.Contains("Overlay");

        if (leftPupilTransform == null && rightPupilTransform == null
            && leftEyeTransform == null && rightEyeTransform == null)
        {
            enabled = false;
            return false;
        }

        initialized = true;
        return true;
    }

    private void CacheNamedEyeTransforms(Transform root)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null || child == root)
            {
                continue;
            }

            string lowerName = child.name.ToLowerInvariant();

            if (lowerName.Contains("pupil"))
            {
                if (lowerName.Contains("left") || lowerName.Contains("_l") || lowerName.EndsWith("l"))
                {
                    leftPupilTransform = child;
                    leftBaseLocalPosition = child.localPosition;
                }
                else if (lowerName.Contains("right") || lowerName.Contains("_r") || lowerName.EndsWith("r"))
                {
                    rightPupilTransform = child;
                    rightBaseLocalPosition = child.localPosition;
                }
            }
            else if (lowerName.Contains("eye"))
            {
                if (lowerName.Contains("left") || lowerName.Contains("_l") || lowerName.EndsWith("l"))
                {
                    leftEyeTransform = child;
                }
                else if (lowerName.Contains("right") || lowerName.Contains("_r") || lowerName.EndsWith("r"))
                {
                    rightEyeTransform = child;
                }
            }
        }
    }

    private void CreateProceduralPupilOverlays()
    {
        leftPupilTransform = CreatePupilOverlay("LeftPupilOverlay", leftPupilLocalPosition);
        rightPupilTransform = CreatePupilOverlay("RightPupilOverlay", rightPupilLocalPosition);
        leftBaseLocalPosition = leftPupilLocalPosition;
        rightBaseLocalPosition = rightPupilLocalPosition;
    }

    private Transform CreatePupilOverlay(string overlayName, Vector3 localPosition)
    {
        GameObject pupilObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupilObject.name = overlayName;
        pupilObject.transform.SetParent(modelTransform, false);
        pupilObject.transform.localPosition = localPosition;
        pupilObject.transform.localRotation = Quaternion.identity;

        Collider collider = pupilObject.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = pupilObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            renderer.enabled = true;

            if (pupilMaterial != null)
            {
                renderer.sharedMaterial = pupilMaterial;
            }
        }

        MaintainOverlayWorldScale(pupilObject.transform);
        return pupilObject.transform;
    }

    private void MaintainOverlayWorldScale(Transform pupilTransform)
    {
        if (pupilTransform == null || pupilTransform.parent == null)
        {
            return;
        }

        float parentScale = Mathf.Max(pupilTransform.parent.lossyScale.x, 0.001f);
        float localScale = pupilScale / parentScale;
        pupilTransform.localScale = Vector3.one * localScale;
    }

    private void UpdateProceduralPupilLook(Transform pupilTransform, Vector3 baseLocalPosition, Vector3 lookTargetWorldPosition)
    {
        if (pupilTransform == null || modelTransform == null)
        {
            return;
        }

        Vector3 offset = ComputeLocalLookOffset(pupilTransform, lookTargetWorldPosition);
        Vector3 targetLocalPosition = baseLocalPosition + offset;
        pupilTransform.localPosition = Vector3.Lerp(
            pupilTransform.localPosition,
            targetLocalPosition,
            Time.deltaTime * eyeFollowSpeed);
    }

    private void UpdateTransformLook(Transform eyeTransform, Vector3 lookTargetWorldPosition)
    {
        if (eyeTransform == null)
        {
            return;
        }

        Vector3 lookDirection = lookTargetWorldPosition - eyeTransform.position;
        lookDirection.y *= 0.75f;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, eyeTransform.parent != null
            ? eyeTransform.parent.up
            : Vector3.up);

        eyeTransform.rotation = Quaternion.Slerp(
            eyeTransform.rotation,
            targetRotation,
            Time.deltaTime * eyeFollowSpeed);
    }

    private Vector3 ComputeLocalLookOffset(Transform pupilTransform, Vector3 lookTargetWorldPosition)
    {
        Vector3 worldOffset = lookTargetWorldPosition - pupilTransform.position;
        worldOffset.y *= 0.75f;

        if (worldOffset.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 localDirection = modelTransform.InverseTransformDirection(worldOffset.normalized);
        localDirection.z = 0f;

        if (localDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        localDirection.Normalize();
        Vector3 offset = localDirection * eyeFollowAmount;
        return Vector3.ClampMagnitude(offset, maxEyeOffset);
    }

    private bool TryResolveLookTarget(out Vector3 lookTargetWorldPosition)
    {
        lookTargetWorldPosition = Vector3.zero;

        if (lookTargetMode != SlimeEyeLookTargetMode.CameraThenPlayer)
        {
            return false;
        }

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            lookTargetWorldPosition = mainCamera.transform.position;
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            lookTargetWorldPosition = playerObject.transform.position + Vector3.up * 1.2f;
            return true;
        }

        return false;
    }

    private Transform ResolveVisualRoot()
    {
        if (transform.name == "VisualRoot")
        {
            return transform;
        }

        Transform found = transform.Find("VisualRoot");

        if (found != null)
        {
            return found;
        }

        Transform parent = transform.parent;

        while (parent != null)
        {
            if (parent.name == "VisualRoot")
            {
                return parent;
            }

            parent = parent.parent;
        }

        return null;
    }
}
