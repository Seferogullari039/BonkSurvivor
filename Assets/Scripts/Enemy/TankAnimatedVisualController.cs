using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(250)]
public class TankAnimatedVisualController : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Material bodyMaterial;

    [Header("Movement")]
    [SerializeField] private float moveSpeedThreshold = 0.05f;

    [Header("Attack Visual")]
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Sword Visual")]
    [SerializeField] private bool createVisualSword = true;
    [SerializeField] private string preferredHandName = "hand.R";

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private Animator animator;
    private Transform enemyRoot;
    private Vector3 lastRootPosition;
    private float nextAttackTime;
    private bool warnedMissingAnimator;
    private GameObject swordVisualRoot;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyRoot = FindEnemyRoot();
        lastRootPosition = enemyRoot != null ? enemyRoot.position : transform.position;

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        SanitizeVisualComponents();
        ConfigureAnimator();
        ApplyBodyMaterial();
        CreateVisualSwordIfNeeded();
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        UpdateMovementParameter();
        TryTriggerAttackVisual();
    }

    private void ConfigureAnimator()
    {
        if (animator == null)
        {
            if (!warnedMissingAnimator)
            {
                warnedMissingAnimator = true;
                Debug.LogWarning("[TankAnimatedVisualController] Animator missing on " + name, this);
            }

            return;
        }

        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.updateMode = AnimatorUpdateMode.Normal;

        if (animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }
    }

    private void ApplyBodyMaterial()
    {
        if (bodyMaterial == null)
        {
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer != null)
            {
                renderer.sharedMaterial = bodyMaterial;
            }
        }
    }

    private void UpdateMovementParameter()
    {
        if (enemyRoot == null)
        {
            enemyRoot = FindEnemyRoot();
        }

        if (enemyRoot == null)
        {
            animator.SetBool(IsMovingHash, false);
            return;
        }

        Vector3 delta = enemyRoot.position - lastRootPosition;
        lastRootPosition = enemyRoot.position;
        delta.y = 0f;

        bool isMoving = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f) > moveSpeedThreshold;
        animator.SetBool(IsMovingHash, isMoving);
    }

    private void TryTriggerAttackVisual()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        Transform target = ResolveTargetTransform();

        if (target == null || enemyRoot == null)
        {
            return;
        }

        Vector3 toTarget = target.position - enemyRoot.position;
        toTarget.y = 0f;

        if (toTarget.magnitude > attackRange)
        {
            return;
        }

        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
        nextAttackTime = Time.time + attackCooldown;
    }

    private void SanitizeVisualComponents()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);

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

    private void CreateVisualSwordIfNeeded()
    {
        if (!createVisualSword || swordVisualRoot != null)
        {
            return;
        }

        Transform hand = FindTransformByName(preferredHandName);

        if (hand == null)
        {
            hand = FindTransformByName("hand.L");
        }

        if (hand == null)
        {
            hand = transform;
        }

        swordVisualRoot = new GameObject("VisualSword");
        swordVisualRoot.transform.SetParent(hand, false);
        swordVisualRoot.transform.localPosition = new Vector3(0.04f, 0.02f, 0.12f);
        swordVisualRoot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        swordVisualRoot.transform.localScale = Vector3.one;

        GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name = "Blade";
        blade.transform.SetParent(swordVisualRoot.transform, false);
        blade.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        blade.transform.localScale = new Vector3(0.05f, 0.32f, 0.02f);
        DisableCollider(blade);
        ApplyMaterial(blade, new Color(0.72f, 0.74f, 0.78f), 0.72f);

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(swordVisualRoot.transform, false);
        handle.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        handle.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
        DisableCollider(handle);
        ApplyMaterial(handle, new Color(0.18f, 0.14f, 0.11f), 0.25f);
    }

    private static void DisableCollider(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();

        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private static void ApplyMaterial(GameObject target, Color color, float smoothness)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.35f);
        }

        renderer.sharedMaterial = material;
    }

    private Transform FindTransformByName(string targetName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];

            if (candidate != null && string.Equals(candidate.name, targetName, System.StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
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

        FPSPlayerController fpsController = Object.FindFirstObjectByType<FPSPlayerController>();

        if (fpsController != null)
        {
            return fpsController.transform;
        }

        PlayerController playerController = Object.FindFirstObjectByType<PlayerController>();

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
