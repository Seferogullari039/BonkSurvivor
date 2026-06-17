using System.Collections.Generic;
using UnityEngine;

public class StarterWeaponViewModel : MonoBehaviour
{
    private static readonly Color BowWoodColor = new Color(0.42f, 0.26f, 0.13f);
    private static readonly Color BowWoodHighlight = new Color(0.52f, 0.34f, 0.18f);
    private static readonly Color BowGripColor = new Color(0.22f, 0.12f, 0.07f);
    private static readonly Color BowStringColor = new Color(0.86f, 0.84f, 0.78f);
    private static readonly Color BowArrowShaftColor = new Color(0.5f, 0.38f, 0.24f);
    private static readonly Color BowArrowHeadColor = new Color(0.7f, 0.72f, 0.76f);
    private static readonly Color BowArrowFletchColor = new Color(0.36f, 0.18f, 0.12f);
    private static readonly Color StaffWoodColor = new Color(0.32f, 0.18f, 0.1f);
    private static readonly Color StaffCoreColor = new Color(1f, 0.45f, 0.08f);
    private static readonly Color SwordBladeColor = new Color(0.78f, 0.82f, 0.88f);
    private static readonly Color SwordGuardColor = new Color(0.55f, 0.45f, 0.2f);
    private static readonly Color SwordGripColor = new Color(0.22f, 0.12f, 0.08f);

    private static readonly Vector3 VisibleWeaponLocalPosition = new Vector3(0.34f, -0.16f, 0.58f);
    private static readonly Vector3 VisibleWeaponLocalRotation = new Vector3(8f, -24f, 6f);
    private static readonly Vector3 VisibleWeaponLocalScale = new Vector3(2.8f, 2.8f, 2.8f);
    private static readonly Vector3 BowWeaponLocalPosition = new Vector3(0.28f, -0.16f, 0.48f);
    private static readonly Vector3 BowWeaponLocalRotation = new Vector3(5f, -20f, 3f);
    private static readonly Vector3 BowWeaponLocalScale = new Vector3(2.2f, 2.2f, 2.2f);
    private static readonly Vector3 BowPrefabInstanceLocalPosition = new Vector3(0.045f, 0.015f, 0.05f);
    private static readonly Vector3 BowPrefabInstanceLocalRotation = new Vector3(2f, -6f, 2f);
    private const float BowStringRestX = -0.018f;
    private const float BowFireAnimDuration = 0.21f;
    private const float BowFireDrawDuration = 0.055f;
    private const float BowFireReleaseDuration = 0.045f;
    private const float BowFireSettleDuration = 0.11f;
    private const float BowStringDrawBackOffset = 0.035f;
    private const float BowArrowDrawBackOffset = 0.032f;
    private const float BowStringReleaseSnapOffset = 0.058f;
    private const float BowArrowReleaseSnapOffset = 0.052f;
    private const float BowBodyDrawAnticipation = 0.012f;
    private const float BowBodyRecoilDistance = 0.04f;
    private const float BowBodyKickDown = 0.008f;
    private const float BowBodyRecoilPitch = 3.5f;
    private const float BowBodyRecoilRoll = 1.75f;
    private const float MinVisualBoundsExtent = 0.06f;
    private const float MaxVisualScaleMultiplier = 3f;
    private static bool weaponVisualDiagnosticsLogged;
    private static bool staffPrefabWarningLogged;
    private static bool bowPrefabWarningLogged;
    private static MaterialPropertyBlock staffGlowPropertyBlock;

    private static readonly Vector3 StaffPrefabLocalPosition = new Vector3(0.38f, -0.30f, 0.40f);
    private static readonly Vector3 StaffPrefabLocalRotation = new Vector3(18f, -42f, 14f);
    private static readonly Vector3 StaffPrefabLocalScale = new Vector3(0.78f, 0.78f, 0.78f);
    private static readonly Vector3 SwordWeaponLocalPosition = new Vector3(0.36f, -0.22f, 0.46f);
    private static readonly Vector3 SwordWeaponLocalRotation = new Vector3(10f, -26f, 7f);
    private static readonly Vector3 SwordWeaponLocalScale = new Vector3(1.75f, 1.75f, 1.75f);
    private const string StaffPrefabAssetPath = "Assets/Prefabs/Weapons/Staff_ViewModel.prefab";
    private const string BowPrefabAssetPath = "Assets/Prefabs/Weapons/Bow_ViewModel.prefab";
    private const string FpsArmsPrefabAssetPath = "Assets/Prefabs/Characters/FPS_Arms_ViewModel.prefab";
    private static readonly Vector3 BowFpsArmsLocalPosition = new Vector3(0.015f, -0.165f, 0.34f);
    private static readonly Vector3 StaffFpsArmsLocalPosition = new Vector3(0.03f, -0.18f, 0.33f);
    private static readonly Vector3 SwordFpsArmsLocalPosition = new Vector3(0.035f, -0.19f, 0.32f);

    private static readonly string[] OffhandRendererNameKeywords =
    {
        "left", "leftarm", "left_arm", "l_arm", "lhand", "left_hand", "l_forearm", "l_upperarm",
        "offhand", "support_hand", "weak_hand", "secondary_hand"
    };

    private static readonly string[] WeaponRendererNameKeywords =
    {
        "smg", "gun", "weapon", "rifle", "pistol", "firearm", "magazine", "barrel", "trigger", "stock"
    };

    private enum FpsArmsVisibilityMode
    {
        TwoHandBow,
        MainHandOnly
    }

    private static readonly string[] PrimitiveArmPlaceholderNames =
    {
        "UpperArm", "Forearm", "Wrist", "Hand", "Thumb"
    };

    private readonly HashSet<GameObject> defaultWeaponParts = new HashSet<GameObject>();
    private Transform weaponMount;
    private GameObject activeVisualRoot;
    private Transform bowStringTransform;
    private Transform bowArrowShaftTransform;
    private Renderer bowStringRenderer;
    private Renderer bowArrowTipRenderer;
    private Renderer bowArrowShaftRenderer;
    private Vector3 bowStringRestLocalPosition = new Vector3(BowStringRestX, 0f, 0f);
    private Vector3 bowArrowTipRestLocalPosition;
    private Vector3 bowArrowShaftRestLocalPosition;
    private Vector3 bowRestLocalPosition = BowWeaponLocalPosition;
    private Quaternion bowRestLocalRotation = Quaternion.Euler(BowWeaponLocalRotation);
    private Renderer staffOrbRenderer;
    private Renderer[] staffGlowRenderers = System.Array.Empty<Renderer>();
    private Transform fireballSpawnPoint;
    private Transform meteorCastPoint;
    private GameObject staffViewModelPrefab;
    private bool staffPrefabLoadAttempted;
    private bool usingStaffPrefabVisual;
    private GameObject bowViewModelPrefab;
    private bool bowPrefabLoadAttempted;
    private bool usingBowPrefabVisual;
    private GameObject fpsArmsViewModelPrefab;
    private bool fpsArmsPrefabLoadAttempted;
    private GameObject fpsArmsContainer;
    private Renderer[] fpsArmsDisabledRenderers = System.Array.Empty<Renderer>();
    private bool usingFpsArmsVisual;
    private bool fpsArmsUsesCombinedMesh;
    private readonly List<GameObject> primitiveArmPlaceholders = new List<GameObject>();
    private StarterWeaponType currentWeapon = StarterWeaponType.HunterBow;
    private StarterWeaponType pendingWeapon = StarterWeaponType.HunterBow;
    private bool needsApply = true;
    private float bowFireAnimTimer;
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
        bowArrowShaftTransform = null;
        bowStringRenderer = null;
        bowArrowTipRenderer = null;
        bowArrowShaftRenderer = null;
        bowFireAnimTimer = 0f;
        staffOrbRenderer = null;
        staffGlowRenderers = System.Array.Empty<Renderer>();
        fireballSpawnPoint = null;
        meteorCastPoint = null;
        usingStaffPrefabVisual = false;
        usingBowPrefabVisual = false;
        ClearFpsArmsVisual();
        SetPrimitiveArmPlaceholdersVisible(true);
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
        bowArrowShaftTransform = null;
        bowStringRenderer = null;
        bowArrowTipRenderer = null;
        bowArrowShaftRenderer = null;
        bowFireAnimTimer = 0f;
        staffOrbRenderer = null;
        staffGlowRenderers = System.Array.Empty<Renderer>();
        fireballSpawnPoint = null;
        meteorCastPoint = null;
        usingStaffPrefabVisual = false;
        usingBowPrefabVisual = false;
        ClearFpsArmsVisual();
        SetPrimitiveArmPlaceholdersVisible(true);
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
        bowFireAnimTimer = BowFireAnimDuration;
    }

    public void PlayStaffGlowPulse()
    {
        staffGlowPulseTimer = 0.14f;

        if (staffOrbRenderer == null)
        {
            return;
        }

        if (usingStaffPrefabVisual)
        {
            ApplyStaffRendererGlow(staffOrbRenderer, new Color(1f, 0.65f, 0.15f), 0.75f);
        }
        else
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

        UpdateBowVisualFeedback();
        UpdateStaffGlowEffects();
    }

    private void UpdateBowVisualFeedback()
    {
        if (currentWeapon != StarterWeaponType.HunterBow)
        {
            return;
        }

        UpdateBowDrawFireVisuals();
    }

    private void UpdateBowDrawFireVisuals()
    {
        if (bowFireAnimTimer <= 0f)
        {
            ResetBowDrawFireRestPose();
            return;
        }

        bowFireAnimTimer -= Time.deltaTime;
        float elapsed = BowFireAnimDuration - bowFireAnimTimer;
        elapsed = Mathf.Clamp(elapsed, 0f, BowFireAnimDuration);

        float stringPullX;
        float arrowPullZ;
        float tipGlow;
        float arrowVisibility;
        float bodyRecoilZ;
        float bodyKickY;
        float bodyPitch;
        float bodyRoll;

        if (elapsed <= BowFireDrawDuration)
        {
            float drawT = elapsed / BowFireDrawDuration;
            float drawCurve = 1f - Mathf.Pow(1f - drawT, 2f);
            stringPullX = -BowStringDrawBackOffset * drawCurve;
            arrowPullZ = -BowArrowDrawBackOffset * drawCurve;
            tipGlow = drawCurve * 0.28f;
            arrowVisibility = 1f;
            bodyRecoilZ = BowBodyDrawAnticipation * drawCurve;
            bodyKickY = -BowBodyKickDown * 0.35f * drawCurve;
            bodyPitch = -1.5f * drawCurve;
            bodyRoll = 0.6f * drawCurve;
        }
        else if (elapsed <= BowFireDrawDuration + BowFireReleaseDuration)
        {
            float releaseT = (elapsed - BowFireDrawDuration) / BowFireReleaseDuration;
            float snapCurve = 1f - Mathf.Pow(1f - releaseT, 3f);
            stringPullX = Mathf.Lerp(-BowStringDrawBackOffset, BowStringReleaseSnapOffset, snapCurve);
            arrowPullZ = Mathf.Lerp(-BowArrowDrawBackOffset, BowArrowReleaseSnapOffset, snapCurve);
            tipGlow = Mathf.Sin(releaseT * Mathf.PI);
            arrowVisibility = releaseT < 0.38f
                ? 1f
                : 1f - Mathf.SmoothStep(0.38f, 0.78f, releaseT);
            float releasePulse = Mathf.Sin(releaseT * Mathf.PI);
            bodyRecoilZ = Mathf.Lerp(BowBodyDrawAnticipation, BowBodyRecoilDistance, releasePulse);
            bodyKickY = -BowBodyKickDown * releasePulse;
            bodyPitch = -BowBodyRecoilPitch * releasePulse;
            bodyRoll = BowBodyRecoilRoll * releasePulse;
        }
        else
        {
            float settleT = (elapsed - BowFireDrawDuration - BowFireReleaseDuration) / BowFireSettleDuration;
            float settleEase = 1f - Mathf.Pow(1f - settleT, 2f);
            stringPullX = Mathf.Lerp(BowStringReleaseSnapOffset, 0f, settleEase);
            arrowPullZ = Mathf.Lerp(BowArrowReleaseSnapOffset, 0f, settleEase);
            tipGlow = (1f - settleEase) * 0.42f;
            arrowVisibility = Mathf.Lerp(0.08f, 1f, settleEase);
            float settleWave = Mathf.Sin((1f - settleEase) * Mathf.PI);
            bodyRecoilZ = settleWave * BowBodyRecoilDistance * 0.45f;
            bodyKickY = -BowBodyKickDown * 0.25f * settleWave;
            bodyPitch = -BowBodyRecoilPitch * 0.35f * settleWave;
            bodyRoll = BowBodyRecoilRoll * 0.35f * settleWave;
        }

        ApplyBowStringAndArrowPose(stringPullX, arrowPullZ, arrowVisibility, tipGlow);
        ApplyBowViewModelKick(bodyRecoilZ, bodyKickY, bodyPitch, bodyRoll);
    }

    private void ApplyBowViewModelKick(float bodyRecoilZ, float bodyKickY, float bodyPitch, float bodyRoll)
    {
        if (activeVisualRoot == null)
        {
            return;
        }

        Transform visualTransform = activeVisualRoot.transform;
        visualTransform.localPosition = bowRestLocalPosition + new Vector3(0f, bodyKickY, -bodyRecoilZ);
        visualTransform.localRotation = bowRestLocalRotation * Quaternion.Euler(bodyPitch, 0f, bodyRoll);
    }

    private void ResetBowDrawFireRestPose()
    {
        ApplyBowStringAndArrowPose(0f, 0f, 1f, 0f);

        if (activeVisualRoot == null)
        {
            return;
        }

        Transform visualTransform = activeVisualRoot.transform;

        if (visualTransform.localPosition != bowRestLocalPosition)
        {
            visualTransform.localPosition = bowRestLocalPosition;
        }

        if (visualTransform.localRotation != bowRestLocalRotation)
        {
            visualTransform.localRotation = bowRestLocalRotation;
        }
    }

    private void ApplyBowStringAndArrowPose(float stringPullX, float arrowPullZ, float arrowVisibility, float tipGlow)
    {
        if (bowStringTransform != null)
        {
            Vector3 stringRest = bowStringRestLocalPosition;
            bowStringTransform.localPosition = stringRest + new Vector3(stringPullX, 0f, 0f);
        }

        if (bowArrowTipRenderer != null)
        {
            bowArrowTipRenderer.transform.localPosition = bowArrowTipRestLocalPosition + new Vector3(0f, 0f, arrowPullZ);
            SetBowRendererVisible(bowArrowTipRenderer, arrowVisibility > 0.08f);
            GameVisualStyle.ApplyColor(
                bowArrowTipRenderer,
                Color.Lerp(BowArrowHeadColor, new Color(0.96f, 0.98f, 1f), tipGlow * 0.72f),
                0.62f,
                tipGlow > 0.22f,
                tipGlow * 0.38f);
        }

        if (bowArrowShaftTransform != null)
        {
            bowArrowShaftTransform.localPosition = bowArrowShaftRestLocalPosition + new Vector3(0f, 0f, arrowPullZ);
        }

        if (bowArrowShaftRenderer != null)
        {
            SetBowRendererVisible(bowArrowShaftRenderer, arrowVisibility > 0.08f);
        }

        if (bowStringRenderer != null)
        {
            Color stringColor = Color.Lerp(BowStringColor, new Color(1f, 0.94f, 0.68f), tipGlow * 0.55f);
            GameVisualStyle.ApplyColor(bowStringRenderer, stringColor, 0.16f, tipGlow > 0.3f, tipGlow * 0.28f);
        }
    }

    private static void SetBowRendererVisible(Renderer renderer, bool visible)
    {
        if (renderer != null)
        {
            renderer.enabled = visible;
        }
    }

    private void CacheBowFeedbackRestPose()
    {
        if (bowStringTransform != null)
        {
            bowStringRestLocalPosition = bowStringTransform.localPosition;
        }

        if (bowArrowTipRenderer != null)
        {
            bowArrowTipRestLocalPosition = bowArrowTipRenderer.transform.localPosition;
        }

        if (bowArrowShaftTransform != null)
        {
            bowArrowShaftRestLocalPosition = bowArrowShaftTransform.localPosition;
        }
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
        CachePrimitiveArmPlaceholders();
        needsApply = true;
    }

    private void CachePrimitiveArmPlaceholders()
    {
        primitiveArmPlaceholders.Clear();

        if (weaponMount == null || weaponMount.parent == null)
        {
            return;
        }

        Transform viewModelRoot = weaponMount.parent;

        for (int i = 0; i < PrimitiveArmPlaceholderNames.Length; i++)
        {
            Transform placeholder = viewModelRoot.Find(PrimitiveArmPlaceholderNames[i]);

            if (placeholder != null)
            {
                primitiveArmPlaceholders.Add(placeholder.gameObject);
            }
        }
    }

    private void SetPrimitiveArmPlaceholdersVisible(bool visible)
    {
        if (primitiveArmPlaceholders.Count == 0)
        {
            CachePrimitiveArmPlaceholders();
        }

        for (int i = 0; i < primitiveArmPlaceholders.Count; i++)
        {
            GameObject placeholder = primitiveArmPlaceholders[i];

            if (placeholder != null)
            {
                placeholder.SetActive(visible);
            }
        }
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
        bowArrowShaftTransform = null;
        bowStringRenderer = null;
        bowArrowTipRenderer = null;
        bowArrowShaftRenderer = null;
        bowFireAnimTimer = 0f;
        staffOrbRenderer = null;
        staffGlowRenderers = System.Array.Empty<Renderer>();
        fireballSpawnPoint = null;
        meteorCastPoint = null;
        usingStaffPrefabVisual = false;
        usingBowPrefabVisual = false;
        ClearFpsArmsVisual();
        SetPrimitiveArmPlaceholdersVisible(true);
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

        if (currentWeapon == StarterWeaponType.HunterBow)
        {
            ApplyBowContainerPose(visualRoot);
            bowRestLocalPosition = BowWeaponLocalPosition;
            bowRestLocalRotation = Quaternion.Euler(BowWeaponLocalRotation);
        }
        else if (currentWeapon == StarterWeaponType.KnightSword)
        {
            ApplySwordContainerPose(visualRoot);
        }
        else
        {
            visualRoot.localPosition = VisibleWeaponLocalPosition;
            visualRoot.localRotation = Quaternion.Euler(VisibleWeaponLocalRotation);
            visualRoot.localScale = VisibleWeaponLocalScale;
        }

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
        else if (usingBowPrefabVisual)
        {
            ApplyBowPrefabContainerPose(visualRoot);
        }

        TryCreateFpsArmsForCurrentWeapon(visualRoot);

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

    private static void ApplyBowPrefabContainerPose(Transform visualRoot)
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
        else if (usingBowPrefabVisual)
        {
            ApplyBowPrefabContainerPose(visualTransform);
            bowRestLocalPosition = Vector3.zero;
            bowRestLocalRotation = Quaternion.identity;
        }
        else if (currentWeapon == StarterWeaponType.HunterBow)
        {
            ApplyBowContainerPose(visualTransform);
            bowRestLocalPosition = BowWeaponLocalPosition;
            bowRestLocalRotation = Quaternion.Euler(BowWeaponLocalRotation);
        }
        else if (currentWeapon == StarterWeaponType.KnightSword)
        {
            ApplySwordContainerPose(visualTransform);
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

                if (!usingStaffPrefabVisual && !usingBowPrefabVisual)
                {
                    EnsureOpaqueMaterial(renderers[i]);
                }
            }
        }

        Camera camera = Camera.main;

        if (camera != null)
        {
            Bounds bounds = ComputeCombinedBounds(renderers);

            if (IsVisualClipping(camera, bounds) && !usingStaffPrefabVisual && !usingBowPrefabVisual)
            {
                if (currentWeapon == StarterWeaponType.HunterBow)
                {
                    ApplyBowContainerPose(visualTransform);
                    bowRestLocalPosition = BowWeaponLocalPosition;
                    bowRestLocalRotation = Quaternion.Euler(BowWeaponLocalRotation);
                }
                else if (currentWeapon == StarterWeaponType.KnightSword)
                {
                    ApplySwordContainerPose(visualTransform);
                }
                else
                {
                    visualTransform.localPosition = VisibleWeaponLocalPosition;
                    visualTransform.localRotation = Quaternion.Euler(VisibleWeaponLocalRotation);
                    visualTransform.localScale = VisibleWeaponLocalScale;
                }
            }
        }

        PreserveFpsArmsDisabledRenderers();
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

    private static void ApplyBowContainerPose(Transform visualTransform)
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.localPosition = BowWeaponLocalPosition;
        visualTransform.localRotation = Quaternion.Euler(BowWeaponLocalRotation);
        visualTransform.localScale = BowWeaponLocalScale;
    }

    private static void ApplyBowPrefabInstancePose(Transform instanceTransform)
    {
        if (instanceTransform == null)
        {
            return;
        }

        instanceTransform.localPosition = BowPrefabInstanceLocalPosition;
        instanceTransform.localRotation = Quaternion.Euler(BowPrefabInstanceLocalRotation);
        instanceTransform.localScale = Vector3.one;
    }

    private static void ApplySwordContainerPose(Transform visualTransform)
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.localPosition = SwordWeaponLocalPosition;
        visualTransform.localRotation = Quaternion.Euler(SwordWeaponLocalRotation);
        visualTransform.localScale = SwordWeaponLocalScale;
    }

    private void BuildBowVisual(Transform root)
    {
        GameObject prefab = GetBowViewModelPrefab();

        if (prefab != null && TryBuildBowPrefabVisual(root, prefab))
        {
            usingBowPrefabVisual = true;
            bowRestLocalPosition = Vector3.zero;
            bowRestLocalRotation = Quaternion.identity;
            return;
        }

        BuildBowPrimitiveVisual(root);
        usingBowPrefabVisual = false;
        bowRestLocalPosition = BowWeaponLocalPosition;
        bowRestLocalRotation = Quaternion.Euler(BowWeaponLocalRotation);
    }

    private bool TryBuildBowPrefabVisual(Transform root, GameObject prefab)
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
            Debug.LogWarning("[ViewModel] Bow prefab instantiate failed, using primitive fallback. " + exception.Message);
            return false;
        }

        if (instance == null)
        {
            return false;
        }

        instance.name = "BowViewModelPrefab";

        Transform instanceTransform = instance.transform;
        ApplyBowPrefabInstancePose(instanceTransform);

        DisableBowPrefabPhysics(instance);
        SetLayerRecursively(instance, 0);
        RepairBowPrefabMissingMaterials(instanceTransform);
        BuildBowPrefabFeedbackOverlays(instanceTransform);

        return true;
    }

    private void BuildBowPrefabFeedbackOverlays(Transform bowInstanceRoot)
    {
        Transform feedbackParent = FindDeepChild(bowInstanceRoot, "BowRoot") ?? bowInstanceRoot;

        GameObject bowString = CreatePart(
            "BowStringFeedback",
            PrimitiveType.Cube,
            feedbackParent,
            new Vector3(BowStringRestX, 0f, 0.02f),
            Quaternion.identity,
            new Vector3(0.008f, 0.16f, 0.008f),
            BowStringColor,
            false,
            0.14f);
        bowStringTransform = bowString != null ? bowString.transform : null;
        bowStringRenderer = bowString != null ? bowString.GetComponent<Renderer>() : null;

        GameObject arrowHead = CreatePart(
            "BowArrowTipFeedback",
            PrimitiveType.Cylinder,
            feedbackParent,
            new Vector3(0.014f, 0.007f, 0.12f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.012f, 0.022f, 0.012f),
            BowArrowHeadColor,
            false,
            0.6f);
        bowArrowTipRenderer = arrowHead != null ? arrowHead.GetComponent<Renderer>() : null;

        GameObject arrowShaft = CreatePart(
            "BowArrowShaftFeedback",
            PrimitiveType.Cylinder,
            feedbackParent,
            new Vector3(0.014f, 0.007f, 0.09f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.009f, 0.05f, 0.009f),
            BowArrowShaftColor,
            false,
            0.26f);
        bowArrowShaftTransform = arrowShaft != null ? arrowShaft.transform : null;
        bowArrowShaftRenderer = arrowShaft != null ? arrowShaft.GetComponent<Renderer>() : null;

        CacheBowFeedbackRestPose();
    }

    private static void DisableBowPrefabPhysics(GameObject bowInstance)
    {
        if (bowInstance == null)
        {
            return;
        }

        Collider[] colliders = bowInstance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        Rigidbody[] rigidbodies = bowInstance.GetComponentsInChildren<Rigidbody>(true);

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

        Animator[] animators = bowInstance.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].enabled = false;
            }
        }
    }

    private void RepairBowPrefabMissingMaterials(Transform bowRoot)
    {
        if (bowRoot == null)
        {
            return;
        }

        Renderer[] renderers = bowRoot.GetComponentsInChildren<Renderer>(true);

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
            Material fallback = ResolveBowFallbackMaterial(label);

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

    private static Material ResolveBowFallbackMaterial(string label)
    {
        Material wood = LoadStaffFallbackMaterial("FireStaff_Wood")
            ?? LoadStaffFallbackMaterial("M_Staff_Wood");
        Material metal = LoadStaffFallbackMaterial("FireStaff_Metal")
            ?? LoadStaffFallbackMaterial("M_Staff_Metal");

        if (label.Contains("string")
            || label.Contains("wire")
            || label.Contains("metal")
            || label.Contains("steel")
            || label.Contains("sight"))
        {
            return metal ?? wood;
        }

        return wood ?? metal;
    }

    private GameObject GetBowViewModelPrefab()
    {
        if (bowViewModelPrefab != null)
        {
            return bowViewModelPrefab;
        }

        if (bowPrefabLoadAttempted)
        {
            return null;
        }

        bowPrefabLoadAttempted = true;

#if UNITY_EDITOR
        bowViewModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BowPrefabAssetPath);
#endif

        if (bowViewModelPrefab == null)
        {
            bowViewModelPrefab = Resources.Load<GameObject>("Bow_ViewModel");
        }

        if (bowViewModelPrefab == null && !bowPrefabWarningLogged)
        {
            bowPrefabWarningLogged = true;
            Debug.LogWarning("[ViewModel] Bow prefab not found, using primitive fallback.");
        }

        return bowViewModelPrefab;
    }

    private GameObject GetFpsArmsViewModelPrefab()
    {
        if (fpsArmsViewModelPrefab != null)
        {
            return fpsArmsViewModelPrefab;
        }

        if (fpsArmsPrefabLoadAttempted)
        {
            return null;
        }

        fpsArmsPrefabLoadAttempted = true;

#if UNITY_EDITOR
        fpsArmsViewModelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(FpsArmsPrefabAssetPath);
#endif

        if (fpsArmsViewModelPrefab == null)
        {
            fpsArmsViewModelPrefab = Resources.Load<GameObject>("FPS_Arms_ViewModel");
        }

        return fpsArmsViewModelPrefab;
    }

    private static Vector3 GetFpsArmsLocalPosition(StarterWeaponType weaponType)
    {
        return weaponType switch
        {
            StarterWeaponType.FireStaff => StaffFpsArmsLocalPosition,
            StarterWeaponType.KnightSword => SwordFpsArmsLocalPosition,
            _ => BowFpsArmsLocalPosition
        };
    }

    private bool SupportsFpsArmsVisual(StarterWeaponType weaponType)
    {
        return weaponType == StarterWeaponType.HunterBow
            || weaponType == StarterWeaponType.FireStaff
            || weaponType == StarterWeaponType.KnightSword;
    }

    private void ClearFpsArmsVisual()
    {
        if (fpsArmsContainer != null)
        {
            Destroy(fpsArmsContainer);
            fpsArmsContainer = null;
        }

        fpsArmsDisabledRenderers = System.Array.Empty<Renderer>();
        usingFpsArmsVisual = false;
        fpsArmsUsesCombinedMesh = false;
    }

    private static FpsArmsVisibilityMode GetFpsArmsVisibilityMode(StarterWeaponType weaponType)
    {
        return weaponType switch
        {
            StarterWeaponType.FireStaff => FpsArmsVisibilityMode.MainHandOnly,
            StarterWeaponType.KnightSword => FpsArmsVisibilityMode.MainHandOnly,
            _ => FpsArmsVisibilityMode.TwoHandBow
        };
    }

    private bool TryCreateFpsArmsForCurrentWeapon(Transform root)
    {
        usingFpsArmsVisual = false;

        if (root == null || !SupportsFpsArmsVisual(currentWeapon))
        {
            SetPrimitiveArmPlaceholdersVisible(true);
            return false;
        }

        GameObject prefab = GetFpsArmsViewModelPrefab();

        if (prefab == null)
        {
            SetPrimitiveArmPlaceholdersVisible(true);
            return false;
        }

        ClearFpsArmsVisual();

        GameObject container = new GameObject("FpsArmsContainer");
        Transform containerTransform = container.transform;
        containerTransform.SetParent(root, false);
        containerTransform.SetAsFirstSibling();
        containerTransform.localPosition = GetFpsArmsLocalPosition(currentWeapon);
        containerTransform.localRotation = Quaternion.identity;
        containerTransform.localScale = Vector3.one;

        GameObject armsInstance = null;

        try
        {
            armsInstance = Instantiate(prefab, containerTransform, false);
        }
        catch (System.Exception)
        {
            Destroy(container);
            SetPrimitiveArmPlaceholdersVisible(true);
            return false;
        }

        if (armsInstance == null)
        {
            Destroy(container);
            SetPrimitiveArmPlaceholdersVisible(true);
            return false;
        }

        fpsArmsContainer = container;
        armsInstance.name = "FpsArmsViewModel";

        Transform armsTransform = armsInstance.transform;
        armsTransform.localPosition = Vector3.zero;
        armsTransform.localRotation = Quaternion.identity;
        armsTransform.localScale = Vector3.one;

        DisableBowPrefabPhysics(armsInstance);
        SetLayerRecursively(armsInstance, 0);
        RepairFpsArmsMissingMaterials(armsInstance.transform);

        FpsArmsVisibilityMode visibilityMode = GetFpsArmsVisibilityMode(currentWeapon);

        if (!ApplyFpsArmsVisibilityMode(armsInstance, visibilityMode))
        {
            Destroy(container);
            fpsArmsContainer = null;
            fpsArmsUsesCombinedMesh = visibilityMode == FpsArmsVisibilityMode.MainHandOnly;
            SetPrimitiveArmPlaceholdersVisible(true);
            return false;
        }

        CacheFpsArmsDisabledRenderers(armsInstance);

        usingFpsArmsVisual = true;
        fpsArmsUsesCombinedMesh = false;
        SetPrimitiveArmPlaceholdersVisible(false);
        return true;
    }

    private bool ApplyFpsArmsVisibilityMode(GameObject armsInstance, FpsArmsVisibilityMode mode)
    {
        if (armsInstance == null)
        {
            return false;
        }

        if (mode == FpsArmsVisibilityMode.TwoHandBow)
        {
            return HasVisibleFpsArmRenderers(armsInstance);
        }

        return TryHideOffhandFpsArms(armsInstance);
    }

    private bool TryHideOffhandFpsArms(GameObject armsInstance)
    {
        List<Renderer> armRenderers = CollectEnabledArmRenderers(armsInstance);

        if (armRenderers.Count == 0)
        {
            return false;
        }

        if (armRenderers.Count == 1)
        {
            return false;
        }

        int disabledCount = 0;

        for (int i = 0; i < armRenderers.Count; i++)
        {
            Renderer renderer = armRenderers[i];

            if (renderer == null || !RendererNameMatchesOffhand(renderer.gameObject.name))
            {
                continue;
            }

            renderer.enabled = false;
            disabledCount++;
        }

        if (disabledCount == 0 && armRenderers.Count == 2)
        {
            Renderer offhandRenderer = SelectLeftmostArmRenderer(armRenderers);

            if (offhandRenderer != null)
            {
                offhandRenderer.enabled = false;
                disabledCount = 1;
            }
        }

        return disabledCount > 0 && HasVisibleFpsArmRenderers(armsInstance);
    }

    private static List<Renderer> CollectEnabledArmRenderers(GameObject armsInstance)
    {
        List<Renderer> armRenderers = new List<Renderer>();
        Renderer[] renderers = armsInstance.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled || RendererNameMatchesWeapon(renderer.gameObject.name))
            {
                continue;
            }

            armRenderers.Add(renderer);
        }

        return armRenderers;
    }

    private static bool HasVisibleFpsArmRenderers(GameObject armsInstance)
    {
        return CollectEnabledArmRenderers(armsInstance).Count > 0;
    }

    private static bool RendererNameMatchesOffhand(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string label = objectName.ToLowerInvariant();

        for (int i = 0; i < OffhandRendererNameKeywords.Length; i++)
        {
            if (label.Contains(OffhandRendererNameKeywords[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RendererNameMatchesWeapon(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string label = objectName.ToLowerInvariant();

        for (int i = 0; i < WeaponRendererNameKeywords.Length; i++)
        {
            if (label.Contains(WeaponRendererNameKeywords[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static Renderer SelectLeftmostArmRenderer(List<Renderer> armRenderers)
    {
        if (armRenderers == null || armRenderers.Count == 0)
        {
            return null;
        }

        Renderer leftmost = armRenderers[0];
        float leftmostX = leftmost != null ? leftmost.bounds.center.x : 0f;

        for (int i = 1; i < armRenderers.Count; i++)
        {
            Renderer candidate = armRenderers[i];

            if (candidate == null)
            {
                continue;
            }

            if (candidate.bounds.center.x < leftmostX)
            {
                leftmost = candidate;
                leftmostX = candidate.bounds.center.x;
            }
        }

        return leftmost;
    }

    private void RepairFpsArmsMissingMaterials(Transform armsRoot)
    {
        if (armsRoot == null)
        {
            return;
        }

        Material fallback = LoadStaffFallbackMaterial("FireStaff_Wood")
            ?? LoadStaffFallbackMaterial("M_Staff_Wood");

        if (fallback == null)
        {
            return;
        }

        Renderer[] renderers = armsRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changed = false;

            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (sharedMaterials[materialIndex] != null)
                {
                    continue;
                }

                sharedMaterials[materialIndex] = fallback;
                changed = true;
            }

            if (changed)
            {
                renderer.sharedMaterials = sharedMaterials;
            }
        }
    }

    private void CacheFpsArmsDisabledRenderers(GameObject armsInstance)
    {
        if (armsInstance == null)
        {
            fpsArmsDisabledRenderers = System.Array.Empty<Renderer>();
            return;
        }

        List<Renderer> disabledRenderers = new List<Renderer>();
        Renderer[] renderers = armsInstance.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer != null && !renderer.enabled)
            {
                disabledRenderers.Add(renderer);
            }
        }

        fpsArmsDisabledRenderers = disabledRenderers.ToArray();
    }

    private void PreserveFpsArmsDisabledRenderers()
    {
        if (!usingFpsArmsVisual || fpsArmsDisabledRenderers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < fpsArmsDisabledRenderers.Length; i++)
        {
            Renderer renderer = fpsArmsDisabledRenderers[i];

            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
    }

    private void BuildBowPrimitiveVisual(Transform root)
    {
        CreatePart(
            "BowRiser",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0.01f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.048f, 0.08f, 0.048f),
            BowWoodColor,
            false,
            0.34f);
        CreatePart(
            "BowGrip",
            PrimitiveType.Cylinder,
            root,
            new Vector3(-0.008f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.034f, 0.058f, 0.034f),
            BowGripColor,
            false,
            0.28f);
        CreatePart(
            "BowUpperLimb",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0.024f, 0.082f, 0f),
            Quaternion.Euler(0f, 0f, -32f),
            new Vector3(0.026f, 0.12f, 0.026f),
            BowWoodColor,
            false,
            0.32f);
        CreatePart(
            "BowUpperLimbTip",
            PrimitiveType.Sphere,
            root,
            new Vector3(0.05f, 0.145f, 0f),
            Quaternion.identity,
            new Vector3(0.028f, 0.028f, 0.028f),
            BowWoodHighlight,
            false,
            0.36f);
        CreatePart(
            "BowLowerLimb",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0.024f, -0.082f, 0f),
            Quaternion.Euler(0f, 0f, 32f),
            new Vector3(0.026f, 0.12f, 0.026f),
            BowWoodColor,
            false,
            0.32f);
        CreatePart(
            "BowLowerLimbTip",
            PrimitiveType.Sphere,
            root,
            new Vector3(0.05f, -0.145f, 0f),
            Quaternion.identity,
            new Vector3(0.028f, 0.028f, 0.028f),
            BowWoodHighlight,
            false,
            0.36f);

        GameObject bowString = CreatePart(
            "BowString",
            PrimitiveType.Cube,
            root,
            new Vector3(BowStringRestX, 0f, 0f),
            Quaternion.identity,
            new Vector3(0.008f, 0.16f, 0.008f),
            BowStringColor,
            false,
            0.14f);
        bowStringTransform = bowString != null ? bowString.transform : null;
        bowStringRenderer = bowString != null ? bowString.GetComponent<Renderer>() : null;

        CreatePart(
            "BowStringGripPinch",
            PrimitiveType.Cube,
            root,
            new Vector3(-0.011f, 0f, 0f),
            Quaternion.identity,
            new Vector3(0.006f, 0.028f, 0.006f),
            new Color(0.72f, 0.7f, 0.66f),
            false,
            0.12f);

        GameObject arrowShaft = CreatePart(
            "NockedArrowShaft",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0.014f, 0.007f, 0.105f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.009f, 0.062f, 0.009f),
            BowArrowShaftColor,
            false,
            0.26f);
        bowArrowShaftTransform = arrowShaft != null ? arrowShaft.transform : null;
        bowArrowShaftRenderer = arrowShaft != null ? arrowShaft.GetComponent<Renderer>() : null;
        GameObject arrowHead = CreatePart(
            "NockedArrowHead",
            PrimitiveType.Cylinder,
            root,
            new Vector3(0.014f, 0.007f, 0.148f),
            Quaternion.Euler(90f, 0f, 0f),
            new Vector3(0.012f, 0.022f, 0.012f),
            BowArrowHeadColor,
            false,
            0.6f);
        bowArrowTipRenderer = arrowHead != null ? arrowHead.GetComponent<Renderer>() : null;
        CreatePart(
            "NockedArrowFletch",
            PrimitiveType.Cube,
            root,
            new Vector3(0.014f, 0.009f, 0.058f),
            Quaternion.Euler(90f, 0f, 18f),
            new Vector3(0.014f, 0.004f, 0.018f),
            BowArrowFletchColor,
            false,
            0.22f);

        CacheBowFeedbackRestPose();
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

            if (usingStaffPrefabVisual)
            {
                ApplyStaffRendererGlow(renderer, glowColor, 0.35f + pulseStrength * 0.55f);
            }
            else
            {
                GameVisualStyle.ApplyColor(renderer, glowColor, 0.45f, true, 0.35f + pulseStrength * 0.55f);
            }
        }
    }

    private static void ApplyStaffRendererGlow(Renderer renderer, Color glowColor, float emissionIntensity)
    {
        if (renderer == null)
        {
            return;
        }

        if (staffGlowPropertyBlock == null)
        {
            staffGlowPropertyBlock = new MaterialPropertyBlock();
        }

        renderer.GetPropertyBlock(staffGlowPropertyBlock);

        Material sharedMaterial = renderer.sharedMaterial;

        if (sharedMaterial != null)
        {
            if (sharedMaterial.HasProperty("_BaseColor"))
            {
                staffGlowPropertyBlock.SetColor("_BaseColor", glowColor);
            }

            if (sharedMaterial.HasProperty("_Color"))
            {
                staffGlowPropertyBlock.SetColor("_Color", glowColor);
            }

            if (sharedMaterial.HasProperty("_EmissionColor"))
            {
                staffGlowPropertyBlock.SetColor("_EmissionColor", glowColor * emissionIntensity);
            }
        }

        renderer.SetPropertyBlock(staffGlowPropertyBlock);
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
