using UnityEngine;

public class FPSViewModel : MonoBehaviour
{
    public static FPSViewModel Instance { get; private set; }

    private static readonly Vector3 RootLocalPosition = new Vector3(0f, -0.35f, 0.65f);
    private static readonly Vector3 WeaponMountLocalPosition = Vector3.zero;

    private static readonly Color UpperArmColor = new Color(0.76f, 0.62f, 0.52f);
    private static readonly Color ForearmColor = new Color(0.72f, 0.58f, 0.48f);
    private static readonly Color HandColor = new Color(0.68f, 0.55f, 0.45f);
    private static readonly Color WeaponBodyColor = new Color(0.22f, 0.24f, 0.28f);
    private static readonly Color WeaponMetalColor = new Color(0.18f, 0.2f, 0.24f);
    private static readonly Color WeaponCoreColor = new Color(0.15f, 0.72f, 0.86f);
    private static readonly Color WeaponGlowColor = new Color(0.55f, 0.92f, 1f);

    private Transform viewModelRoot;
    private Transform weaponMountTransform;
    private Renderer weaponCoreRenderer;
    private Material weaponCoreMaterial;
    private Color weaponCoreBaseColor;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private float swayPitch;
    private float swayYaw;
    private float bobTimer;
    private float recoilTimer;
    private float dashTimer;
    private float glowTimer;

    private Vector3 recoilOffset;
    private Vector3 dashOffset;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ApplyCameraNearClip();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (weaponCoreMaterial != null)
        {
            Destroy(weaponCoreMaterial);
        }
    }

    private void Update()
    {
        if (!TryGetViewModelCamera(out Camera camera))
        {
            return;
        }

        if (!MainMenuManager.IsRunActive)
        {
            if (viewModelRoot != null && viewModelRoot.gameObject.activeSelf)
            {
                viewModelRoot.gameObject.SetActive(false);
            }

            return;
        }

        ResolveViewModelReferences(camera);

        if (!IsViewModelParentChainValid(camera))
        {
            if (viewModelRoot == null)
            {
                BuildViewModel(camera);
            }
            else
            {
                RepairViewModelTransformChain(camera);
            }
        }

        if (viewModelRoot == null)
        {
            return;
        }

        bool showViewModel = MainMenuManager.IsRunActive;

        if (viewModelRoot.gameObject.activeSelf != showViewModel)
        {
            viewModelRoot.gameObject.SetActive(showViewModel);
        }

        if (!showViewModel)
        {
            return;
        }

        UpdateSway();
        UpdateBob();
        UpdateRecoil();
        UpdateDash();
        UpdateWeaponGlow();
        ApplyViewModelTransform();
    }

    public void PlayRecoil()
    {
        recoilTimer = 0.08f;
        recoilOffset = new Vector3(0f, -0.03f, -0.08f);
        PlayFireGlow();
    }

    public void PlayFireGlow()
    {
        glowTimer = 0.1f;
    }

    public void PlayDash()
    {
        dashTimer = 0.12f;
        dashOffset = new Vector3(0.05f, -0.07f, 0.02f);
    }

    public void EnsureViewModelVisible()
    {
        if (!TryGetViewModelCamera(out Camera camera))
        {
            return;
        }

        ResolveViewModelReferences(camera);

        if (viewModelRoot == null)
        {
            BuildViewModel(camera);
        }
        else
        {
            RepairViewModelTransformChain(camera);
        }

        if (viewModelRoot == null)
        {
            return;
        }

        viewModelRoot.gameObject.SetActive(true);

        if (weaponMountTransform != null)
        {
            weaponMountTransform.gameObject.SetActive(true);
        }

        ApplyCameraNearClip();
        Debug.Log("[Recovery] ViewModel visible");
        StarterWeaponViewModel.NotifyViewModelRebuilt();
    }

    public void HideViewModelForMenu()
    {
        if (viewModelRoot != null)
        {
            viewModelRoot.gameObject.SetActive(false);
        }
    }

    public void RefreshForGameplayStart()
    {
        EnsureViewModelVisible();
    }

    private static bool ShouldShowViewModel()
    {
        return MainMenuManager.IsRunActive && Time.timeScale > 0f;
    }

    private static bool TryGetViewModelCamera(out Camera camera)
    {
        camera = Camera.main;

        return camera != null && camera.enabled;
    }

    private void ResolveViewModelReferences(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        if (viewModelRoot == null)
        {
            Transform existingRoot = camera.transform.Find("ViewModelRoot");

            if (existingRoot == null)
            {
                existingRoot = FindTransformByName("ViewModelRoot");
            }

            viewModelRoot = existingRoot;
        }

        if (weaponMountTransform == null && viewModelRoot != null)
        {
            weaponMountTransform = viewModelRoot.Find("WeaponMount");
        }

        if (weaponMountTransform == null)
        {
            weaponMountTransform = FindTransformByName("WeaponMount");
        }
    }

    private bool IsViewModelParentChainValid(Camera camera)
    {
        if (camera == null || viewModelRoot == null || weaponMountTransform == null)
        {
            return false;
        }

        if (viewModelRoot.parent != camera.transform)
        {
            return false;
        }

        return weaponMountTransform.parent == viewModelRoot;
    }

    private bool IsViewModelTransformChainValid(Camera camera)
    {
        if (!IsViewModelParentChainValid(camera))
        {
            return false;
        }

        if ((viewModelRoot.localPosition - RootLocalPosition).sqrMagnitude > 0.0001f)
        {
            return false;
        }

        return weaponMountTransform.localPosition.sqrMagnitude <= 0.0001f;
    }

    private void RepairViewModelTransformChain(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        ResolveViewModelReferences(camera);

        if (viewModelRoot == null)
        {
            BuildViewModel(camera);
            return;
        }

        viewModelRoot.SetParent(camera.transform, false);
        viewModelRoot.localPosition = RootLocalPosition;
        viewModelRoot.localRotation = Quaternion.identity;
        viewModelRoot.localScale = Vector3.one;

        baseLocalPosition = RootLocalPosition;
        baseLocalRotation = Quaternion.identity;

        if (weaponMountTransform == null)
        {
            weaponMountTransform = viewModelRoot.Find("WeaponMount");
        }

        if (weaponMountTransform == null)
        {
            weaponMountTransform = FindTransformByName("WeaponMount");
        }

        if (weaponMountTransform == null)
        {
            weaponMountTransform = CreateMount(
                viewModelRoot,
                "WeaponMount",
                WeaponMountLocalPosition,
                Quaternion.identity);
        }
        else
        {
            weaponMountTransform.SetParent(viewModelRoot, false);
            weaponMountTransform.localPosition = WeaponMountLocalPosition;
            weaponMountTransform.localRotation = Quaternion.identity;
            weaponMountTransform.localScale = Vector3.one;
        }

        Transform weaponVisual = weaponMountTransform.Find("StarterWeaponVisual");

        if (weaponVisual != null && weaponVisual.parent != weaponMountTransform)
        {
            weaponVisual.SetParent(weaponMountTransform, false);
        }

        viewModelRoot.gameObject.SetActive(true);
        weaponMountTransform.gameObject.SetActive(true);
    }

    private static Transform FindTransformByName(string objectName)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate != null && candidate.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }

    private void BuildViewModel(Camera camera)
    {
        if (camera == null)
        {
            return;
        }

        Transform existingRoot = camera.transform.Find("ViewModelRoot");

        if (existingRoot != null)
        {
            Object.Destroy(existingRoot.gameObject);
        }

        viewModelRoot = null;
        weaponMountTransform = null;
        weaponCoreRenderer = null;

        GameObject rootObject = new GameObject("ViewModelRoot");
        viewModelRoot = rootObject.transform;
        viewModelRoot.SetParent(camera.transform, false);
        viewModelRoot.localPosition = RootLocalPosition;
        viewModelRoot.localRotation = Quaternion.identity;
        viewModelRoot.localScale = Vector3.one;

        baseLocalPosition = RootLocalPosition;
        baseLocalRotation = Quaternion.identity;

        CreatePart(
            "UpperArm",
            PrimitiveType.Cylinder,
            new Vector3(-0.02f, -0.04f, -0.08f),
            Quaternion.Euler(70f, 14f, 6f),
            new Vector3(0.048f, 0.11f, 0.048f),
            UpperArmColor,
            false,
            0.38f);

        CreatePart(
            "Forearm",
            PrimitiveType.Cylinder,
            new Vector3(0.015f, -0.028f, 0.015f),
            Quaternion.Euler(76f, -6f, 12f),
            new Vector3(0.042f, 0.095f, 0.042f),
            ForearmColor,
            false,
            0.38f);

        CreatePart(
            "Wrist",
            PrimitiveType.Cylinder,
            new Vector3(0.03f, -0.015f, 0.038f),
            Quaternion.Euler(82f, -8f, 4f),
            new Vector3(0.034f, 0.022f, 0.034f),
            ForearmColor,
            false,
            0.35f);

        CreatePart(
            "Hand",
            PrimitiveType.Capsule,
            new Vector3(0.042f, -0.008f, 0.058f),
            Quaternion.Euler(84f, -10f, 0f),
            new Vector3(0.046f, 0.038f, 0.046f),
            HandColor,
            false,
            0.36f);

        CreatePart(
            "Thumb",
            PrimitiveType.Cylinder,
            new Vector3(0.058f, -0.004f, 0.05f),
            Quaternion.Euler(78f, -28f, 18f),
            new Vector3(0.012f, 0.028f, 0.012f),
            HandColor,
            false,
            0.34f);

        weaponMountTransform = CreateMount(
            viewModelRoot,
            "WeaponMount",
            WeaponMountLocalPosition,
            Quaternion.identity);

        CreatePart(
            "WeaponBody",
            PrimitiveType.Cube,
            weaponMountTransform,
            new Vector3(0f, 0f, 0.035f),
            Quaternion.identity,
            new Vector3(0.065f, 0.038f, 0.1f),
            WeaponBodyColor,
            false,
            0.48f);

        CreatePart(
            "WeaponGrip",
            PrimitiveType.Cube,
            weaponMountTransform,
            new Vector3(0f, -0.028f, 0.015f),
            Quaternion.Euler(12f, 0f, 0f),
            new Vector3(0.032f, 0.042f, 0.04f),
            WeaponMetalColor,
            false,
            0.4f);

        GameObject weaponCore = CreatePart(
            "EnergyCore",
            PrimitiveType.Capsule,
            weaponMountTransform,
            new Vector3(0f, 0.004f, 0.04f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.028f, 0.018f, 0.028f),
            WeaponCoreColor,
            true,
            0.62f,
            0.28f);

        weaponCoreRenderer = weaponCore.GetComponent<Renderer>();

        if (weaponCoreRenderer != null)
        {
            weaponCoreMaterial = weaponCoreRenderer.material;
            weaponCoreBaseColor = WeaponCoreColor;
        }

        CreatePart(
            "WeaponBarrel",
            PrimitiveType.Cylinder,
            weaponMountTransform,
            new Vector3(0f, 0.008f, 0.105f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.02f, 0.038f, 0.02f),
            WeaponMetalColor,
            false,
            0.52f);

        CreatePart(
            "WeaponMuzzle",
            PrimitiveType.Cylinder,
            weaponMountTransform,
            new Vector3(0f, 0.008f, 0.145f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.026f, 0.01f, 0.026f),
            WeaponBodyColor,
            false,
            0.45f);

        viewModelRoot.gameObject.SetActive(true);
        weaponMountTransform.gameObject.SetActive(true);
        StarterWeaponViewModel.NotifyViewModelRebuilt();
    }

    private static Transform CreateMount(Transform parent, string mountName, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject mountObject = new GameObject(mountName);
        Transform mountTransform = mountObject.transform;
        mountTransform.SetParent(parent, false);
        mountTransform.localPosition = localPosition;
        mountTransform.localRotation = localRotation;
        mountTransform.localScale = Vector3.one;
        return mountTransform;
    }

    private static void ApplyCameraNearClip()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        camera.nearClipPlane = 0.03f;
    }

    private GameObject CreatePart(
        string partName,
        PrimitiveType primitive,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color,
        bool glow,
        float smoothness = 0.5f,
        float emissionIntensity = 0.35f)
    {
        return CreatePart(partName, primitive, viewModelRoot, localPosition, localRotation, localScale, color, glow, smoothness, emissionIntensity);
    }

    private GameObject CreatePart(
        string partName,
        PrimitiveType primitive,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color,
        bool glow,
        float smoothness = 0.5f,
        float emissionIntensity = 0.35f)
    {
        GameObject partObject = GameObject.CreatePrimitive(primitive);
        partObject.name = partName;
        partObject.transform.SetParent(parent, false);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localRotation = localRotation;
        partObject.transform.localScale = localScale;

        Collider collider = partObject.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }

        Renderer renderer = partObject.GetComponent<Renderer>();

        if (renderer != null)
        {
            GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emissionIntensity);
        }

        return partObject;
    }

    private void UpdateSway()
    {
        float targetYaw = -Input.GetAxis("Mouse X") * 2.5f;
        float targetPitch = -Input.GetAxis("Mouse Y") * 1.8f;

        swayYaw = Mathf.Lerp(swayYaw, targetYaw, Time.deltaTime * 8f);
        swayPitch = Mathf.Lerp(swayPitch, targetPitch, Time.deltaTime * 8f);
    }

    private void UpdateBob()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;

        float moveAmount = new Vector2(horizontal, vertical).magnitude;

        if (moveAmount > 0.01f)
        {
            bobTimer += Time.deltaTime * 9f;
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 6f);
        }
    }

    private void UpdateRecoil()
    {
        if (recoilTimer <= 0f)
        {
            recoilOffset = Vector3.zero;
            return;
        }

        recoilTimer -= Time.deltaTime;
        recoilOffset = Vector3.Lerp(new Vector3(0f, -0.03f, -0.08f), Vector3.zero, 1f - Mathf.Clamp01(recoilTimer / 0.08f));
    }

    private void UpdateDash()
    {
        if (dashTimer <= 0f)
        {
            dashOffset = Vector3.zero;
            return;
        }

        dashTimer -= Time.deltaTime;
        dashOffset = Vector3.Lerp(new Vector3(0.05f, -0.07f, 0.02f), Vector3.zero, 1f - Mathf.Clamp01(dashTimer / 0.12f));
    }

    private void UpdateWeaponGlow()
    {
        if (weaponCoreMaterial == null)
        {
            return;
        }

        if (glowTimer <= 0f)
        {
            weaponCoreMaterial.color = weaponCoreBaseColor;

            if (weaponCoreMaterial.HasProperty("_BaseColor"))
            {
                weaponCoreMaterial.SetColor("_BaseColor", weaponCoreBaseColor);
            }

            if (weaponCoreMaterial.HasProperty("_EmissionColor"))
            {
                weaponCoreMaterial.SetColor("_EmissionColor", WeaponCoreColor * 0.28f);
            }

            return;
        }

        glowTimer -= Time.deltaTime;
        float flash = glowTimer / 0.1f;
        Color glowColor = Color.Lerp(weaponCoreBaseColor, WeaponGlowColor, flash);
        weaponCoreMaterial.color = glowColor;

        if (weaponCoreMaterial.HasProperty("_BaseColor"))
        {
            weaponCoreMaterial.SetColor("_BaseColor", glowColor);
        }

        if (weaponCoreMaterial.HasProperty("_EmissionColor"))
        {
            weaponCoreMaterial.SetColor("_EmissionColor", WeaponGlowColor * (0.45f + flash * 0.55f));
        }
    }

    private void ApplyViewModelTransform()
    {
        float bobY = Mathf.Sin(bobTimer) * 0.015f;
        float bobX = Mathf.Cos(bobTimer * 0.5f) * 0.008f;

        viewModelRoot.localPosition = baseLocalPosition
            + new Vector3(bobX, bobY, 0f)
            + recoilOffset
            + dashOffset;

        viewModelRoot.localRotation = baseLocalRotation * Quaternion.Euler(swayPitch, swayYaw, 0f);
    }
}
