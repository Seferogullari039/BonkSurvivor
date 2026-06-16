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

    private static readonly Vector3 BowWeaponLocalPosition = Vector3.zero;
    private static readonly Vector3 BowWeaponLocalRotation = Vector3.zero;
    private static readonly float BowWeaponLocalScale = 1f;

    private static readonly Vector3 StaffWeaponLocalPosition = Vector3.zero;
    private static readonly Vector3 StaffWeaponLocalRotation = Vector3.zero;
    private static readonly float StaffWeaponLocalScale = 1f;

    private static readonly Vector3 SwordWeaponLocalPosition = Vector3.zero;
    private static readonly Vector3 SwordWeaponLocalRotation = Vector3.zero;
    private static readonly float SwordWeaponLocalScale = 1f;

    private static readonly Vector3 StaffPrefabLocalPosition = new Vector3(0.04f, -0.06f, 0.08f);
    private static readonly Vector3 StaffPrefabLocalRotation = new Vector3(18f, -12f, 10f);
    private static readonly float StaffPrefabLocalScale = 1f;
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
        TryApplyPending();
    }

    public void RefreshCurrentWeapon()
    {
        pendingWeapon = currentWeapon;
        needsApply = true;
        TryApplyPending();
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
        bowStringTransform.localPosition = new Vector3(kick, 0f, 0.062f);
    }

    private void TryApplyPending()
    {
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
            if (part != null)
            {
                part.SetActive(false);
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

        activeVisualRoot = new GameObject("StarterWeaponVisual");
        activeVisualRoot.transform.SetParent(weaponMount, false);
        ApplyWeaponTransform(activeVisualRoot.transform, currentWeapon);

        switch (currentWeapon)
        {
            case StarterWeaponType.HunterBow:
                BuildBowVisual(activeVisualRoot.transform);
                break;
            case StarterWeaponType.FireStaff:
                BuildStaffVisual(activeVisualRoot.transform);
                break;
            case StarterWeaponType.KnightSword:
                BuildSwordVisual(activeVisualRoot.transform);
                break;
        }

        activeVisualRoot.SetActive(true);
    }

    private static void ApplyWeaponTransform(Transform root, StarterWeaponType weaponType)
    {
        switch (weaponType)
        {
            case StarterWeaponType.FireStaff:
                root.localPosition = StaffWeaponLocalPosition;
                root.localRotation = Quaternion.Euler(StaffWeaponLocalRotation);
                root.localScale = Vector3.one * StaffWeaponLocalScale;
                break;
            case StarterWeaponType.KnightSword:
                root.localPosition = SwordWeaponLocalPosition;
                root.localRotation = Quaternion.Euler(SwordWeaponLocalRotation);
                root.localScale = Vector3.one * SwordWeaponLocalScale;
                break;
            default:
                root.localPosition = BowWeaponLocalPosition;
                root.localRotation = Quaternion.Euler(BowWeaponLocalRotation);
                root.localScale = Vector3.one * BowWeaponLocalScale;
                break;
        }
    }

    private void BuildBowVisual(Transform root)
    {
        CreatePart("BowGrip", PrimitiveType.Cylinder, root, new Vector3(0f, 0f, 0.015f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.018f, 0.038f, 0.018f), BowGripColor, false, 0.3f);
        CreatePart("BowUpperLimb", PrimitiveType.Cylinder, root, new Vector3(0.008f, 0.048f, -0.018f), Quaternion.Euler(58f, 8f, -16f), new Vector3(0.007f, 0.095f, 0.007f), BowWoodColor, false, 0.32f);
        CreatePart("BowLowerLimb", PrimitiveType.Cylinder, root, new Vector3(0.008f, -0.048f, -0.018f), Quaternion.Euler(-58f, 8f, 16f), new Vector3(0.007f, 0.095f, 0.007f), BowWoodColor, false, 0.32f);

        GameObject bowString = CreatePart("BowString", PrimitiveType.Cylinder, root, new Vector3(0f, 0f, 0.062f), Quaternion.identity, new Vector3(0.001f, 0.108f, 0.001f), BowStringColor, false, 0.15f);
        bowStringTransform = bowString != null ? bowString.transform : null;

        CreatePart("NockedArrowShaft", PrimitiveType.Cylinder, root, new Vector3(0f, 0.003f, 0.098f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.004f, 0.055f, 0.004f), BowArrowShaftColor, false, 0.28f);
        CreatePart("NockedArrowHead", PrimitiveType.Cylinder, root, new Vector3(0f, 0.003f, 0.138f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.007f, 0.016f, 0.007f), BowArrowHeadColor, false, 0.62f);
    }

    private void BuildStaffVisual(Transform root)
    {
        CreatePart("StaffShaft", PrimitiveType.Cylinder, root, new Vector3(0f, 0f, 0.11f), Quaternion.Euler(72f, 0f, 0f), new Vector3(0.024f, 0.19f, 0.024f), StaffWoodColor, false, 0.28f);
        CreatePart("StaffCap", PrimitiveType.Cylinder, root, new Vector3(0f, -0.05f, 0.03f), Quaternion.Euler(72f, 0f, 0f), new Vector3(0.028f, 0.022f, 0.028f), StaffWoodColor, false, 0.25f);

        GameObject staffOrb = CreatePart("StaffOrb", PrimitiveType.Sphere, root, new Vector3(0f, 0.045f, 0.2f), Quaternion.identity, new Vector3(0.055f, 0.055f, 0.055f), StaffCoreColor, true, 0.55f, 0.55f);
        staffOrbRenderer = staffOrb != null ? staffOrb.GetComponent<Renderer>() : null;
    }

    private GameObject GetStaffViewModelPrefab()
    {
        return null;
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

    private void EnsureStaffVisualMaterials(Transform staffRoot)
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
        CreatePart("SwordGrip", PrimitiveType.Cylinder, root, new Vector3(0f, -0.015f, 0.035f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.02f, 0.048f, 0.02f), SwordGripColor, false, 0.28f);
        CreatePart("SwordGuard", PrimitiveType.Cylinder, root, new Vector3(0f, 0.004f, 0.075f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.09f, 0.012f, 0.09f), SwordGuardColor, false, 0.5f);
        CreatePart("SwordBlade", PrimitiveType.Cylinder, root, new Vector3(0f, 0.008f, 0.16f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.022f, 0.14f, 0.006f), SwordBladeColor, false, 0.68f);
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
}
