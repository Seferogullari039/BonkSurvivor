using System.Collections.Generic;
using UnityEngine;

public class StarterWeaponViewModel : MonoBehaviour
{
    private static readonly Color BowWoodColor = new Color(0.38f, 0.22f, 0.11f);
    private static readonly Color BowGripColor = new Color(0.2f, 0.11f, 0.06f);
    private static readonly Color BowStringColor = new Color(0.9f, 0.88f, 0.82f);
    private static readonly Color BowArrowShaftColor = new Color(0.55f, 0.42f, 0.28f);
    private static readonly Color BowArrowHeadColor = new Color(0.74f, 0.76f, 0.8f);
    private static readonly Color StaffWoodColor = new Color(0.32f, 0.18f, 0.1f);
    private static readonly Color StaffCoreColor = new Color(1f, 0.45f, 0.08f);
    private static readonly Color SwordBladeColor = new Color(0.78f, 0.82f, 0.88f);
    private static readonly Color SwordGuardColor = new Color(0.55f, 0.45f, 0.2f);
    private static readonly Color SwordGripColor = new Color(0.22f, 0.12f, 0.08f);

    private static readonly Vector3 VisibleWeaponLocalPosition = new Vector3(0.34f, -0.16f, 0.58f);
    private static readonly Vector3 VisibleWeaponLocalRotation = new Vector3(8f, -24f, 6f);
    private static readonly Vector3 VisibleWeaponLocalScale = new Vector3(2.8f, 2.8f, 2.8f);
    private const float MinVisualBoundsExtent = 0.06f;
    private const float MaxVisualScaleMultiplier = 3f;
    private static bool weaponVisualDiagnosticsLogged;
    private static bool staffPrefabWarningLogged;

    private static readonly Vector3 StaffPrefabLocalPosition = new Vector3(0.32f, -0.34f, 0.24f);
    private static readonly Vector3 StaffPrefabLocalRotation = new Vector3(20f, -38f, 18f);
    private static readonly Vector3 StaffPrefabLocalScale = new Vector3(0.82f, 0.82f, 0.82f);
    private const string StaffPrefabAssetPath = "Assets/Prefabs/Weapons/Staff_ViewModel.prefab";

    private readonly HashSet<GameObject> defaultWeaponParts = new HashSet<GameObject>();
    private Transform weaponMount;
    private GameObject activeVisualRoot;
    private Transform bowStringTransform;
    private Renderer staffOrbRenderer;
    private Renderer[] staffGlowRenderers = System.Array.Empty<Renderer>();
    private Transform fireballSpawnPoint;
    private Transform meteorCastPoint;
    private GameObject staffViewModelPrefab;
    private bool staffPrefabLoadAttempted;
    private bool usingStaffPrefabVisual;
    private StarterWeaponType currentWeapon = StarterWeaponType.HunterBow;
    private StarterWeaponType pendingWeapon = StarterWeaponType.HunterBow;
    private bool needsApply = true;
    private float bowStringKickTimer;
    private float staffGlowPulseTimer;
    private float staffChargeGlowTimer;

    public Transform FireballSpawnPoint => fireballSpawnPoint;
    public Transform MeteorCastPoint => meteorCastPoint;
    public bool IsUsingStaffPrefabVisual => usingStaffPrefabVisual;

    public static void NotifyViewModelRebuilt()
    {
        StarterWeaponViewModel[] viewModels = FindObjectsByType<StarterWeaponViewModel>(FindObjectsSortMode.None);

        for (int i = 0; i < viewModels.Length; i++)
        {
            if (viewModels[i] != null)
            {
                viewModels[i].HandleViewModelRebuilt();
            }
        }
    }

    private void HandleViewModelRebuilt()
    {
        weaponMount = null;
        defaultWeaponParts.Clear();
        activeVisualRoot = null;
        bowStringTransform = null;
        staffOrbRenderer = null;
        staffGlowRenderers = System.Array.Empty<Renderer>();
        fireballSpawnPoint = null;
        meteorCastPoint = null;
        usingStaffPrefabVisual = false;
        needsApply = true;
    }

    public void ApplyWeapon(StarterWeaponType weaponType)
    {
        pendingWeapon = weaponType;
        needsApply = true;

        if (MainMenuManager.IsRunActive)
        {
            TryApplyPending();
        }
    }

    public void PrepareSelectedWeapon(StarterWeaponType weaponType)
    {
        pendingWeapon = weaponType;
        needsApply = true;
    }

    public void HideWeaponVisualForMenu()
    {
        if (activeVisualRoot != null)
        {
            Destroy(activeVisualRoot);
            activeVisualRoot = null;
        }

        bowStringTransform = null;
        staffOrbRenderer = null;
        staffGlowRenderers = System.Array.Empty<Renderer>();
        fireballSpawnPoint = null;
        meteorCastPoint = null;
        usingStaffPrefabVisual = false;
        weaponVisualDiagnosticsLogged = false;
        needsApply = true;
    }

    public void RefreshCurrentWeapon()
    {
        pendingWeapon = currentWeapon;
        needsApply = true;
        TryApplyPending();
        LogRecoveryWeaponVisual();
    }

    public void PlayBowStringKick()
    {
        bowStringKickTimer = 0.12f;
    }

    public void PlayStaffGlowPulse()
    {
        staffGlowPulseTimer = 0.14f;

        if (staffOrbRenderer != null)
        {
            GameVisualStyle.ApplyColor(staffOrbRenderer, new Color(1f, 0.65f, 0.15f), 0.55f, true, 0.75f);
        }
    }

    public void PlayStaffChargeGlow(float duration = 0.75f)
    {
        staffChargeGlowTimer = Mathf.Max(staffChargeGlowTimer, duration);
    }

    public bool TryGetFireballSpawnPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (fireballSpawnPoint == null)
        {
            return false;
        }

        worldPosition = fireballSpawnPoint.position;
        return true;
    }

    private void LateUpdate()
    {
        EnsureWeaponMount();

        if (needsApply)
        {
            TryApplyPending();
        }

        UpdateBowStringKick();
        UpdateStaffGlowEffects();
    }

    private void UpdateBowStringKick()
    {
        if (bowStringTransform == null || bowStringKickTimer <= 0f) return;

        bowStringKickTimer -= Time.deltaTime;
        float kick = Mathf.Sin(Mathf.Clamp01(bowStringKickTimer / 0.12f) * Mathf.PI) * 0.035f;
        bowStringTransform.localPosition = new Vector3(-0.015f + kick, 0f, 0f);
    }

    private void TryApplyPending()
    {
        if (!MainMenuManager.IsRunActive)
        {
            return;
        }

        EnsureWeaponMount();

        if (weaponMount == null)
        {
            return;
        }

        needsApply = false;
        currentWeapon = pendingWeapon;
        HideDefaultWeaponParts();
        RebuildActiveVisual();
    }

    private void EnsureWeaponMount()
    {
        Camera camera = Camera.main;

        if (camera == null || !camera.enabled)
        {
            if (weaponMount != null)
            {
                weaponMount = null;
                defaultWeaponParts.Clear();
                needsApply = true;
            }

            return;
        }

        Transform root = camera.transform.Find("ViewModelRoot");

        if (root == null)
        {
            if (weaponMount != null)
            {
                weaponMount = null;
                defaultWeaponParts.Clear();
                needsApply = true;
            }

            return;
        }

        Transform mount = root.Find("WeaponMount");

        if (mount == null)
        {
            if (weaponMount != null)
            {
                weaponMount = null;
                defaultWeaponParts.Clear();
                needsApply = true;
            }

            return;
        }

        if (weaponMount == mount)
        {
            return;
        }

        weaponMount = mount;
        CacheDefaultWeaponParts();
        needsApply = true;
    }

    private void CacheDefaultWeaponParts()
    {
        defaultWeaponParts.Clear();

        if (weaponMount == null) return;

        for (int i = 0; i < weaponMount.childCount; i++)
        {
            Transform child = weaponMount.GetChild(i);

            if (child == null) continue;

            if (child.name == "StarterWeaponVisual")
            {
                continue;
            }

            defaultWeaponParts.Add(child.gameObject);
        }
    }

    private void HideDefaultWeaponParts()
    {
        foreach (GameObject part in defaultWeaponParts)
        {
            if (part != null && part.name != "StarterWeaponVisual")
            {
                part.SetActive(false);
            }
        }
    }

    private void ClearStarterWeaponVisualChildren()
    {
        if (weaponMount == null)
        {
            return;
        }

        for (int i = weaponMount.childCount - 1; i >= 0; i--)
        {
            Transform child = weaponMount.GetChild(i);

            if (child != null && child.name == "StarterWeaponVisual")
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void RebuildActiveVisual()
    {
        if (activeVisualRoot != null)
        {
            Destroy(activeVisualRoot);
            activeVisualRoot = null;
        }

        bowStringTransform = null;
        staffOrbRenderer = null;
        staffGlowRenderers = System.Array.Empty<Renderer>();
        fireballSpawnPoint = null;
        meteorCastPoint = null;
        usingStaffPrefabVisual = false;
        staffGlowPulseTimer = 0f;
        staffChargeGlowTimer = 0f;

        if (weaponMount == null)
        {
            return;
        }

        ClearStarterWeaponVisualChildren();

        activeVisualRoot = new GameObject("StarterWeaponVisual");
        activeVisualRoot.layer = 0;

        Transform visualRoot = activeVisualRoot.transform;
        visualRoot.SetParent(weaponMount, false);
        visualRoot.localPosition = VisibleWeaponLocalPosition;
        visualRoot.localRotation = Quaternion.Euler(VisibleWeaponLocalRotation);
        visualRoot.localScale = VisibleWeaponLocalScale;

        switch (currentWeapon)
        {
            case StarterWeaponType.HunterBow:
                BuildBowVisual(visualRoot);
                break;
            case StarterWeaponType.FireStaff:
                BuildStaffVisual(visualRoot);
                break;
            case StarterWeaponType.KnightSword:
                BuildSwordVisual(visualRoot);
                break;
        }

        if (usingStaffPrefabVisual)
        {
            ApplyStaffPrefabContainerPose(visualRoot);
        }

        EnsureWeaponVisualVisible();
    }

    private static void ApplyStaffPrefabContainerPose(Transform visualRoot)
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
    }

    private static void ApplyStaffPrefabInstancePose(Transform instanceTransform)
    {
        if (instanceTransform == null)
        {
            return;
        }

        instanceTransform.localPosition = StaffPrefabLocalPosition;
        instanceTransform.localRotation = Quaternion.Euler(StaffPrefabLocalRotation);
        instanceTransform.localScale = StaffPrefabLocalScale;
    }

    private void EnsureWeaponVisualVisible()
    {
        if (activeVisualRoot == null || weaponMount == null)
        {
            return;
        }

        activeVisualRoot.SetActive(true);
        SetLayerRecursively(activeVisualRoot, 0);

        Transform visualTransform = activeVisualRoot.transform;

        if (usingStaffPrefabVisual)
        {
            ApplyStaffPrefabContainerPose(visualTransform);
        }
        else
        {
            visualTransform.localPosition = VisibleWeaponLocalPosition;
            visualTransform.localRotation = Quaternion.Euler(VisibleWeaponLocalRotation);
            visualTransform.localScale = VisibleWeaponLocalScale;
        }

        Renderer[] renderers = activeVisualRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = true;

                if (!usingStaffPrefabVisual)
                {
                    EnsureOpaqueMaterial(renderers[i]);
                }
            }
        }

        Camera camera = Camera.main;

        if (camera != null)
        {
            Bounds bounds = ComputeCombinedBounds(renderers);

            if (IsVisualClipping(camera, bounds) && !usingStaffPrefabVisual)
            {
                visualTransform.localPosition = VisibleWeaponLocalPosition;
                visualTransform.localRotation = Quaternion.Euler(VisibleWeaponLocalRotation);
                visualTransform.localScale = VisibleWeaponLocalScale;
            }
        }

        LogWeaponVisualDiagnostics(visualTransform, renderers);
    }

    private static void EnsureOpaqueMaterial(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material[] materials = renderer.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];

            if (material == null)
            {
                continue;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 0f);
            }

            Color baseColor = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
            baseColor.a = 1f;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            material.color = baseColor;
        }
    }

    private static Bounds ComputeCombinedBounds(Renderer[] renderers)
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

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

        return bounds;
    }

    private static float GetMaxExtent(Bounds bounds)
    {
        return Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
    }

    private static bool IsVisualClipping(Camera camera, Bounds bounds)
    {
        if (camera == null || bounds.size.sqrMagnitude <= 0.000001f)
        {
            return true;
        }

        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        float nearLimit = -camera.nearClipPlane + 0.015f;
        bool anyInFront = false;

        for (int i = 0; i < corners.Length; i++)
        {
            float viewZ = camera.worldToCameraMatrix.MultiplyPoint(corners[i]).z;

            if (viewZ < nearLimit)
            {
                anyInFront = true;
            }

            if (viewZ > nearLimit)
            {
                return true;
            }
        }

        return !anyInFront;
    }

    private void LogWeaponVisualDiagnostics(Transform visualTransform, Renderer[] renderers)
    {
        if (visualTransform == null || weaponVisualDiagnosticsLogged)
        {
            return;
        }

        weaponVisualDiagnosticsLogged = true;
        int enabledCount = 0;

        Debug.Log(
            "[WeaponVisual] StarterWeaponVisual"
            + " localPosition="
            + visualTransform.localPosition
            + " localRotation="
            + visualTransform.localRotation.eulerAngles
            + " localScale="
            + visualTransform.localScale);

        for (int i = 0; i < visualTransform.childCount; i++)
        {
            Transform child = visualTransform.GetChild(i);

            if (child == null)
            {
                continue;
            }

            Renderer childRenderer = child.GetComponent<Renderer>();
            string boundsText = " boundsCenter=none";

            if (childRenderer != null)
            {
                Bounds bounds = childRenderer.bounds;
                boundsText = " boundsCenter=" + bounds.center + " boundsSize=" + bounds.size;

                if (childRenderer.enabled)
                {
                    enabledCount++;
                }
            }

            Debug.Log(
                "[WeaponVisual] child "
                + child.name
                + " localPosition="
                + child.localPosition
                + " localRotation="
                + child.localRotation.eulerAngles
                + " localScale="
                + child.localScale
                + boundsText);
        }

        Debug.Log(
            "[WeaponVisual] Summary enabledRenderers="
            + enabledCount
            + " totalRenderers="
            + renderers.Length);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null)
        {
            return;
        }

        target.layer = layer;

        Transform transform = target.transform;

        for (int i = 0; i < transform.childCount; i++)
        {
            SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }
    }

    private void LogRecoveryWeaponVisual()
    {
        if (activeVisualRoot == null)
        {
            return;
        }

        Debug.Log("[Recovery] Weapon visual visible: " + GetRecoveryWeaponLabel(currentWeapon));
    }

    private static string GetRecoveryWeaponLabel(StarterWeaponType weaponType)
    {
        return weaponType switch
        {
            StarterWeaponType.FireStaff => "Staff",
            StarterWeaponType.KnightSword => "Sword",
            _ => "Bow"
        };
    }

    private void BuildBowVisual(Transform root)
    {
        CreatePart("BowGrip", PrimitiveType.Cylinder, root, new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(0.04f, 0.07f, 0.04f), BowGripColor, false, 0.3f);
        CreatePart("BowUpperLimb", PrimitiveType.Cylinder, root, new Vector3(0.03f, 0.09f, 0f), Quaternion.Euler(0f, 0f, -28f), new Vector3(0.03f, 0.14f, 0.03f), BowWoodColor, false, 0.32f);
        CreatePart("BowLowerLimb", PrimitiveType.Cylinder, root, new Vector3(0.03f, -0.09f, 0f), Quaternion.Euler(0f, 0f, 28f), new Vector3(0.03f, 0.14f, 0.03f), BowWoodColor, false, 0.32f);

        GameObject bowString = CreatePart("BowString", PrimitiveType.Cube, root, new Vector3(-0.015f, 0f, 0f), Quaternion.identity, new Vector3(0.012f, 0.18f, 0.012f), BowStringColor, false, 0.15f);
        bowStringTransform = bowString != null ? bowString.transform : null;

        CreatePart("NockedArrowShaft", PrimitiveType.Cylinder, root, new Vector3(0.02f, 0.01f, 0.12f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.012f, 0.08f, 0.012f), BowArrowShaftColor, false, 0.28f);
        CreatePart("NockedArrowHead", PrimitiveType.Cylinder, root, new Vector3(0.02f, 0.01f, 0.2f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.018f, 0.03f, 0.018f), BowArrowHeadColor, false, 0.62f);
    }

    private void BuildStaffVisual(Transform root)
    {
        GameObject prefab = GetStaffViewModelPrefab();

        if (prefab != null && TryBuildStaffPrefabVisual(root, prefab))
        {
            usingStaffPrefabVisual = true;
            return;
        }

        BuildStaffPrimitiveVisual(root);
        usingStaffPrefabVisual = false;
    }

    private bool TryBuildStaffPrefabVisual(Transform root, GameObject prefab)
    {
        if (root == null || prefab == null)
        {
            return false;
        }

        GameObject instance = null;

        try
        {
            instance = Instantiate(prefab, root, false);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("[ViewModel] Fire Staff prefab instantiate failed, using primitive fallback. " + exception.Message);
            return false;
        }

        if (instance == null)
        {
            return false;
        }

        instance.name = "StaffViewModelPrefab";

        Transform instanceTransform = instance.transform;
        ApplyStaffPrefabInstancePose(instanceTransform);

        DisableStaffPrefabPhysics(instance);
        SetLayerRecursively(instance, 0);
        RepairStaffPrefabMissingMaterials(instanceTransform);

        CacheStaffSpawnPoints(instanceTransform);
        CacheStaffGlowRenderers(instanceTransform);

        if (fireballSpawnPoint == null)
        {
            fireballSpawnPoint = CreateSpawnPoint(root, "FireballSpawnPoint", new Vector3(0.04f, 0.06f, 0.18f));
        }

        if (meteorCastPoint == null)
        {
            meteorCastPoint = CreateSpawnPoint(root, "MeteorCastPoint", new Vector3(0.04f, 0.06f, 0.18f));
        }

        if (staffOrbRenderer == null && staffGlowRenderers.Length > 0)
        {
            staffOrbRenderer = staffGlowRenderers[0];
        }

        return true;
    }

    private static void DisableStaffPrefabPhysics(GameObject staffInstance)
    {
        if (staffInstance == null)
        {
            return;
        }

        Collider[] colliders = staffInstance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = staffInstance.GetComponentsInChildren<Rigidbody>(true);

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

    private void BuildStaffPrimitiveVisual(Transform root)
    {
        CreatePart("StaffShaft", PrimitiveType.Cylinder, root, new Vector3(0.02f, -0.04f, 0.08f), Quaternion.Euler(68f, -12f, 0f), new Vector3(0.04f, 0.24f, 0.04f), StaffWoodColor, false, 0.28f);
        CreatePart("StaffCap", PrimitiveType.Cylinder, root, new Vector3(0f, -0.1f, 0.02f), Quaternion.Euler(68f, -12f, 0f), new Vector3(0.045f, 0.03f, 0.045f), StaffWoodColor, false, 0.25f);

        GameObject staffOrb = CreatePart("StaffOrb", PrimitiveType.Sphere, root, new Vector3(0.04f, 0.06f, 0.18f), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.08f), StaffCoreColor, true, 0.55f, 0.75f);
        staffOrbRenderer = staffOrb != null ? staffOrb.GetComponent<Renderer>() : null;

        fireballSpawnPoint = CreateSpawnPoint(root, "FireballSpawnPoint", new Vector3(0.04f, 0.06f, 0.18f));
        meteorCastPoint = CreateSpawnPoint(root, "MeteorCastPoint", new Vector3(0.04f, 0.06f, 0.18f));
    }

    private static Transform CreateSpawnPoint(Transform parent, string pointName, Vector3 localPosition)
    {
        GameObject pointObject = new GameObject(pointName);
        Transform pointTransform = pointObject.transform;
        pointTransform.SetParent(parent, false);
        pointTransform.localPosition = localPosition;
        pointTransform.localRotation = Quaternion.identity;
        pointTransform.localScale = Vector3.one;
        return pointTransform;
    }

    private GameObject GetStaffViewModelPrefab()
    {
        if (staffViewModelPrefab != null)
        {
            return staffViewModelPrefab;
        }

        if (staffPrefabLoadAttempted)
        {
            return null;
        }

        staffPrefabLoadAttempted = true;

#if UNITY_EDITOR
        staffViewModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(StaffPrefabAssetPath);
#endif

        if (staffViewModelPrefab == null)
        {
            staffViewModelPrefab = Resources.Load<GameObject>("Staff_ViewModel");
        }

        if (staffViewModelPrefab == null && !staffPrefabWarningLogged)
        {
            staffPrefabWarningLogged = true;
            Debug.LogWarning("[ViewModel] Fire Staff prefab not found, using primitive fallback.");
        }

        return staffViewModelPrefab;
    }

    private void CacheStaffSpawnPoints(Transform staffRoot)
    {
        fireballSpawnPoint = FindDeepChild(staffRoot, "FireballSpawnPoint");
        meteorCastPoint = FindDeepChild(staffRoot, "MeteorCastPoint");
    }

    private void CacheStaffGlowRenderers(Transform staffRoot)
    {
        List<Renderer> glowRenderers = new List<Renderer>();
        Renderer[] renderers = staffRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            string label = renderer.gameObject.name.ToLowerInvariant();

            if (label.Contains("amber")
                || label.Contains("flame")
                || label.Contains("crystal")
                || label.Contains("lightbeam")
                || label.Contains("ignitor")
                || label.Contains("beamreciever")
                || label.Contains("beamreceiver")
                || label.Contains("electricbeam")
                || label.Contains("energysphere")
                || label.Contains("scanring"))
            {
                glowRenderers.Add(renderer);
            }
        }

        staffGlowRenderers = glowRenderers.ToArray();
    }

    private void RepairStaffPrefabMissingMaterials(Transform staffRoot)
    {
        if (staffRoot == null)
        {
            return;
        }

        Renderer[] renderers = staffRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool hasMissingMaterial = false;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (sharedMaterials[materialIndex] == null)
                {
                    hasMissingMaterial = true;
                    break;
                }
            }

            if (!hasMissingMaterial)
            {
                continue;
            }

            string label = renderer.gameObject.name.ToLowerInvariant();
            Material fallback = ResolveStaffFallbackMaterial(label);

            if (fallback == null)
            {
                continue;
            }

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (sharedMaterials[materialIndex] == null)
                {
                    sharedMaterials[materialIndex] = fallback;
                }
            }

            renderer.sharedMaterials = sharedMaterials;
        }
    }

    private static Material ResolveStaffFallbackMaterial(string label)
    {
        Material loaded = LoadStaffFallbackMaterial("FireStaff_Crystal_Emissive")
            ?? LoadStaffFallbackMaterial("FireStaff_Wood")
            ?? LoadStaffFallbackMaterial("FireStaff_Metal")
            ?? LoadStaffFallbackMaterial("FireStaff_DarkTrim")
            ?? LoadStaffFallbackMaterial("M_Staff_Crystal")
            ?? LoadStaffFallbackMaterial("M_Staff_Wood")
            ?? LoadStaffFallbackMaterial("M_Staff_Metal");

        if (loaded == null)
        {
            return null;
        }

        if (label.Contains("amber")
            || label.Contains("flame")
            || label.Contains("crystal")
            || label.Contains("lightbeam")
            || label.Contains("electricbeam")
            || label.Contains("energysphere")
            || label.Contains("scanring")
            || label.Contains("ignitorflame"))
        {
            return LoadStaffFallbackMaterial("FireStaff_Crystal_Emissive")
                ?? LoadStaffFallbackMaterial("M_Staff_Crystal")
                ?? loaded;
        }

        if (label.Contains("wire") || label.Contains("panel") || label.Contains("phong"))
        {
            return LoadStaffFallbackMaterial("FireStaff_DarkTrim") ?? loaded;
        }

        if (label.Contains("grip")
            || label.Contains("vertical")
            || label.Contains("horizontal")
            || label.Contains("strongwood")
            || label.Contains("lambert"))
        {
            return LoadStaffFallbackMaterial("FireStaff_Wood")
                ?? LoadStaffFallbackMaterial("M_Staff_Wood")
                ?? loaded;
        }

        return LoadStaffFallbackMaterial("FireStaff_Metal")
            ?? LoadStaffFallbackMaterial("M_Staff_Metal")
            ?? loaded;
    }

    private static Material LoadStaffFallbackMaterial(string materialName)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Weapons/Firestaff/Materials/" + materialName + ".mat");
#else
        return null;
#endif
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
            {
                continue;
            }

            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);

            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void DisableColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    private void UpdateStaffGlowEffects()
    {
        if (currentWeapon != StarterWeaponType.FireStaff)
        {
            return;
        }

        if (staffGlowPulseTimer > 0f)
        {
            staffGlowPulseTimer -= Time.deltaTime;
        }

        if (staffChargeGlowTimer > 0f)
        {
            staffChargeGlowTimer -= Time.deltaTime;
        }

        if (staffGlowRenderers.Length == 0)
        {
            return;
        }

        float pulseStrength = 0f;

        if (staffGlowPulseTimer > 0f)
        {
            float pulseProgress = Mathf.Clamp01(staffGlowPulseTimer / 0.14f);
            pulseStrength = Mathf.Max(pulseStrength, Mathf.Sin(pulseProgress * Mathf.PI) * 0.85f);
        }

        if (staffChargeGlowTimer > 0f)
        {
            float chargeProgress = 1f - Mathf.Clamp01(staffChargeGlowTimer / 0.75f);
            pulseStrength = Mathf.Max(pulseStrength, 0.35f + chargeProgress * 0.55f);
        }

        if (pulseStrength <= 0.001f)
        {
            return;
        }

        Color glowColor = Color.Lerp(new Color(1f, 0.45f, 0.08f), new Color(1f, 0.72f, 0.18f), pulseStrength);

        for (int i = 0; i < staffGlowRenderers.Length; i++)
        {
            Renderer renderer = staffGlowRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.materials;

            if (materials == null || materials.Length == 0)
            {
                continue;
            }

            GameVisualStyle.ApplyColor(renderer, glowColor, 0.45f, true, 0.35f + pulseStrength * 0.55f);
        }
    }

    private void BuildSwordVisual(Transform root)
    {
        CreatePart("SwordGrip", PrimitiveType.Cylinder, root, new Vector3(0f, -0.03f, 0.04f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.035f, 0.07f, 0.035f), SwordGripColor, false, 0.28f);
        CreatePart("SwordGuard", PrimitiveType.Cylinder, root, new Vector3(0f, 0f, 0.1f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.12f, 0.018f, 0.12f), SwordGuardColor, false, 0.5f);
        CreatePart("SwordBlade", PrimitiveType.Cylinder, root, new Vector3(0f, 0.01f, 0.22f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.035f, 0.2f, 0.012f), SwordBladeColor, false, 0.68f);
    }

    private static GameObject CreatePart(
        string partName,
        PrimitiveType primitive,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Color color,
        bool glow,
        float smoothness = 0.45f,
        float emissionIntensity = 0.2f)
    {
        GameObject partObject = new GameObject(partName);
        partObject.layer = 0;

        Transform partTransform = partObject.transform;
        partTransform.SetParent(parent, false);
        partTransform.localPosition = localPosition;
        partTransform.localRotation = localRotation;
        partTransform.localScale = localScale;

        MeshFilter meshFilter = partObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = PrimitiveMeshCache.GetMesh(primitive);

        MeshRenderer renderer = partObject.AddComponent<MeshRenderer>();
        Material defaultMaterial = PrimitiveMeshCache.GetDefaultMaterial();

        if (defaultMaterial != null)
        {
            renderer.sharedMaterial = defaultMaterial;
        }

        renderer.enabled = true;
        GameVisualStyle.ApplyColor(renderer, color, smoothness, glow, emissionIntensity);
        return partObject;
    }

    private static class PrimitiveMeshCache
    {
        private static readonly Dictionary<PrimitiveType, Mesh> Meshes = new Dictionary<PrimitiveType, Mesh>();
        private static Material defaultMaterial;

        public static Mesh GetMesh(PrimitiveType primitive)
        {
            if (Meshes.TryGetValue(primitive, out Mesh cachedMesh) && cachedMesh != null)
            {
                return cachedMesh;
            }

            GameObject tempPrimitive = GameObject.CreatePrimitive(primitive);

            try
            {
                MeshFilter tempFilter = tempPrimitive.GetComponent<MeshFilter>();
                Mesh mesh = tempFilter != null ? tempFilter.sharedMesh : null;

                if (defaultMaterial == null)
                {
                    Renderer tempRenderer = tempPrimitive.GetComponent<Renderer>();

                    if (tempRenderer != null)
                    {
                        defaultMaterial = tempRenderer.sharedMaterial;
                    }
                }

                Meshes[primitive] = mesh;
                return mesh;
            }
            finally
            {
                Object.Destroy(tempPrimitive);
            }
        }

        public static Material GetDefaultMaterial()
        {
            if (defaultMaterial == null)
            {
                GetMesh(PrimitiveType.Cube);
            }

            return defaultMaterial;
        }
    }
}
