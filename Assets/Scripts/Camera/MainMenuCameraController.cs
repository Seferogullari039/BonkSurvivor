using UnityEngine;

[DefaultExecutionOrder(-90)]
public class MainMenuCameraController : MonoBehaviour
{
    public static MainMenuCameraController Instance { get; private set; }

    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float cameraHeightMultiplier = 0.82f;
    [SerializeField] private float cameraBackMultiplier = 0.62f;
    [SerializeField] private float minimumCameraHeight = 72f;
    [SerializeField] private float menuFieldOfView = 42f;

    private Camera menuCamera;
    private CameraFollow cameraFollow;
    private float savedGameplayFieldOfView;

    private void Awake()
    {
        Instance = this;
        ResolveGameplayCamera();
        EnsureMenuCamera();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowMenuCamera()
    {
        ResolveGameplayCamera();
        EnsureMenuCamera();
        PositionMenuCamera();

        if (menuCamera != null)
        {
            menuCamera.enabled = true;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = false;
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowGameplayCamera()
    {
        if (menuCamera != null)
        {
            menuCamera.enabled = false;
        }

        if (gameplayCamera != null)
        {
            gameplayCamera.enabled = true;

            if (savedGameplayFieldOfView > 0f)
            {
                gameplayCamera.fieldOfView = savedGameplayFieldOfView;
            }
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = false;
        }
    }

    private void ResolveGameplayCamera()
    {
        if (gameplayCamera != null) return;

        FPSPlayerController fpsController = FindFirstObjectByType<FPSPlayerController>();

        if (fpsController != null)
        {
            Camera fpsCamera = fpsController.GetComponentInChildren<Camera>();

            if (fpsCamera != null)
            {
                gameplayCamera = fpsCamera;
            }
        }

        if (gameplayCamera == null && Camera.main != null)
        {
            gameplayCamera = Camera.main;
        }

        if (gameplayCamera == null) return;

        cameraFollow = gameplayCamera.GetComponent<CameraFollow>();
        savedGameplayFieldOfView = gameplayCamera.fieldOfView;
    }

    private void EnsureMenuCamera()
    {
        if (menuCamera != null) return;

        GameObject menuCameraObject = new GameObject("MainMenuCamera");
        menuCameraObject.transform.SetParent(transform, false);
        menuCamera = menuCameraObject.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.Skybox;
        menuCamera.fieldOfView = menuFieldOfView;
        menuCamera.nearClipPlane = 0.3f;
        menuCamera.farClipPlane = 500f;
        menuCamera.depth = 10f;
        menuCamera.enabled = false;
    }

    private void PositionMenuCamera()
    {
        if (menuCamera == null) return;

        float mapHalfSize = 80f;

        if (ProceduralGrassArena.Instance != null)
        {
            mapHalfSize = Mathf.Max(ProceduralGrassArena.Instance.HalfSizeX, ProceduralGrassArena.Instance.HalfSizeZ);
        }

        Vector3 lookAt = lookAtOffset;
        float height = Mathf.Max(minimumCameraHeight, mapHalfSize * cameraHeightMultiplier);
        float backDistance = mapHalfSize * cameraBackMultiplier;
        Vector3 cameraPosition = lookAt + new Vector3(0f, height, -backDistance);

        menuCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.identity);
        menuCamera.transform.LookAt(lookAt);
    }
}
