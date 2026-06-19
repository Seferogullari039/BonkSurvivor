using UnityEngine;

public enum SlimeEyeLookTargetMode
{
    CameraThenPlayer
}

[DisallowMultipleComponent]
public class SlimeEyeLookController : MonoBehaviour
{
    [SerializeField] private SlimeEyeLookTargetMode lookTargetMode = SlimeEyeLookTargetMode.CameraThenPlayer;
    [SerializeField] private float eyeFollowAmount = 0.035f;
    [SerializeField] private float eyeFollowSpeed = 10f;
    [SerializeField] private float maxEyeOffset = 0.045f;
    [SerializeField] private bool enableProceduralPupilOverlay = false;
    [SerializeField] private Vector3 leftPupilLocalPosition = new Vector3(-0.014f, 0.016f, 0.024f);
    [SerializeField] private Vector3 rightPupilLocalPosition = new Vector3(0.014f, 0.016f, 0.024f);
    [SerializeField] private float pupilScale = 0.05f;
    [SerializeField] private Material pupilMaterial;

    private Transform motionRoot;
    private Transform leftPupilTransform;
    private Transform rightPupilTransform;
    private Vector3 leftBaseLocalPosition;
    private Vector3 rightBaseLocalPosition;
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

        if (!enableProceduralPupilOverlay
            || leftPupilTransform == null
            || rightPupilTransform == null)
        {
            return;
        }

        MaintainOverlayWorldScale(leftPupilTransform);
        MaintainOverlayWorldScale(rightPupilTransform);

        if (!TryResolveLookTarget(out Vector3 lookTargetWorldPosition))
        {
            return;
        }

        UpdateProceduralPupilLook(leftPupilTransform, leftBaseLocalPosition, lookTargetWorldPosition);
        UpdateProceduralPupilLook(rightPupilTransform, rightBaseLocalPosition, lookTargetWorldPosition);
    }

    private bool TryInitialize()
    {
        if (initialized)
        {
            return motionRoot != null;
        }

        Transform visualRoot = ResolveVisualRoot();

        if (visualRoot == null)
        {
            enabled = false;
            return false;
        }

        motionRoot = ResolveMotionRoot(visualRoot);

        if (motionRoot == null || !motionRoot.gameObject.activeInHierarchy)
        {
            enabled = false;
            return false;
        }

        if (enableProceduralPupilOverlay)
        {
            leftPupilTransform = motionRoot.Find("LeftPupilOverlay");
            rightPupilTransform = motionRoot.Find("RightPupilOverlay");

            if (leftPupilTransform == null || rightPupilTransform == null)
            {
                CreateProceduralPupilOverlays();
            }

            leftBaseLocalPosition = leftPupilLocalPosition;
            rightBaseLocalPosition = rightPupilLocalPosition;
        }

        initialized = true;
        return true;
    }

    private static Transform ResolveMotionRoot(Transform visualRoot)
    {
        return visualRoot.Find("SlimeMotionRoot");
    }

    private void CreateProceduralPupilOverlays()
    {
        leftPupilTransform = CreatePupilOverlay("LeftPupilOverlay", leftPupilLocalPosition);
        rightPupilTransform = CreatePupilOverlay("RightPupilOverlay", rightPupilLocalPosition);
    }

    private Transform CreatePupilOverlay(string overlayName, Vector3 localPosition)
    {
        GameObject pupilObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupilObject.name = overlayName;
        pupilObject.transform.SetParent(motionRoot, false);
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
        if (pupilTransform == null || motionRoot == null)
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

    private Vector3 ComputeLocalLookOffset(Transform pupilTransform, Vector3 lookTargetWorldPosition)
    {
        Vector3 worldOffset = lookTargetWorldPosition - pupilTransform.position;
        worldOffset.y *= 0.75f;

        if (worldOffset.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 localDirection = motionRoot.InverseTransformDirection(worldOffset.normalized);
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
