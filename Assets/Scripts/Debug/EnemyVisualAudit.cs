using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Play Mode audit for enemy visual visibility. Editor and Development builds only.
/// </summary>
public sealed class EnemyVisualAudit : MonoBehaviour
{
    private const float AuditIntervalSeconds = 2.5f;
    private const float TinyBoundsVolume = 0.0005f;
    private const float HugeBoundsVolume = 250f;
    private const float BoundsCenterMaxDistance = 12f;
    private const float RadarRange = 25f;

    private float nextAuditTime;

    // Default off: the interval audit log is opt-in only (set true to diagnose enemy visuals).
    public static bool LoggingEnabled = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!ShouldRun())
        {
            return;
        }

        if (FindFirstObjectByType<EnemyVisualAudit>() != null)
        {
            return;
        }

        GameObject host = new GameObject("EnemyVisualAudit");
        host.AddComponent<EnemyVisualAudit>();
    }

    private static bool ShouldRun()
    {
#if UNITY_EDITOR
        return Application.isPlaying;
#else
        return Debug.isDebugBuild && Application.isPlaying;
#endif
    }

    private void Awake()
    {
        if (!ShouldRun())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        nextAuditTime = Time.unscaledTime + 1f;
    }

    private void Update()
    {
        if (!ShouldRun() || !LoggingEnabled || Time.unscaledTime < nextAuditTime)
        {
            return;
        }

        nextAuditTime = Time.unscaledTime + AuditIntervalSeconds;
        RunAudit();
    }

    private static void RunAudit()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int total = enemies.Length;
        int basic = 0;
        int fast = 0;
        int tank = 0;
        int elite = 0;
        int other = 0;
        int withVisualRoot = 0;
        int withVisibleRenderer = 0;
        int missingVisual = 0;
        int tinyBounds = 0;
        int hugeBounds = 0;
        int missingMaterial = 0;
        int badScale = 0;
        int inactiveVisualChild = 0;
        int radarEligible = 0;
        int offscreenRadarOnly = 0;

        Transform player = ResolvePlayerTransform();
        Camera mainCamera = Camera.main;
        List<int> missingIds = new List<int>(8);

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];

            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            switch (enemy.Type)
            {
                case Enemy.EnemyType.Normal:
                    basic++;
                    break;
                case Enemy.EnemyType.Fast:
                    fast++;
                    break;
                case Enemy.EnemyType.Tank:
                    tank++;
                    break;
                case Enemy.EnemyType.Elite:
                    elite++;
                    break;
                default:
                    other++;
                    break;
            }

            AuditEnemyVisual(
                enemy,
                player,
                mainCamera,
                ref withVisualRoot,
                ref withVisibleRenderer,
                ref missingVisual,
                ref tinyBounds,
                ref hugeBounds,
                ref missingMaterial,
                ref badScale,
                ref inactiveVisualChild,
                ref radarEligible,
                ref offscreenRadarOnly,
                missingIds);
        }

        StringBuilder builder = new StringBuilder(256);
        builder.Append("[EnemyVisualAudit] total=").Append(total);
        builder.Append(" visibleRenderers=").Append(withVisibleRenderer);
        builder.Append(" missingVisual=").Append(missingVisual);
        builder.Append(" basic=").Append(basic);
        builder.Append(" fast=").Append(fast);
        builder.Append(" tank=").Append(tank);
        builder.Append(" elite=").Append(elite);
        builder.Append(" other=").Append(other);
        builder.Append(" visualRoot=").Append(withVisualRoot);
        builder.Append(" tinyBounds=").Append(tinyBounds);
        builder.Append(" hugeBounds=").Append(hugeBounds);
        builder.Append(" missingMaterial=").Append(missingMaterial);
        builder.Append(" badScale=").Append(badScale);
        builder.Append(" inactiveVisualChild=").Append(inactiveVisualChild);
        builder.Append(" radar25m=").Append(radarEligible);
        builder.Append(" radarOffscreen=").Append(offscreenRadarOnly);

        if (missingIds.Count > 0)
        {
            builder.Append(" missingIds=");
            for (int i = 0; i < missingIds.Count && i < 8; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(missingIds[i]);
            }
        }

        if (radarEligible > withVisibleRenderer)
        {
            builder.Append(" note=Minimap/radar shows tagged enemies within 25m including offscreen; count can exceed on-screen visible meshes.");
        }

        Debug.Log(builder.ToString());
    }

    private static void AuditEnemyVisual(
        Enemy enemy,
        Transform player,
        Camera mainCamera,
        ref int withVisualRoot,
        ref int withVisibleRenderer,
        ref int missingVisual,
        ref int tinyBounds,
        ref int hugeBounds,
        ref int missingMaterial,
        ref int badScale,
        ref int inactiveVisualChild,
        ref int radarEligible,
        ref int offscreenRadarOnly,
        List<int> missingIds)
    {
        Transform enemyTransform = enemy.transform;
        Transform visualRoot = enemyTransform.Find(EnemyVisualController.VisualRootName);
        bool hasVisibleRenderer = false;
        bool hasInactiveVisualIssue = false;
        bool primaryModelInactive = false;
        bool backupModelActive = false;

        if (visualRoot != null)
        {
            withVisualRoot++;

            if (HasBadScale(visualRoot))
            {
                badScale++;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];

                if (renderer == null || IsIntentionallyInactiveBackup(renderer.transform))
                {
                    continue;
                }

                if (!renderer.gameObject.activeInHierarchy)
                {
                    hasInactiveVisualIssue = true;
                    continue;
                }

                if (!renderer.enabled)
                {
                    hasInactiveVisualIssue = true;
                    continue;
                }

                if (renderer.sharedMaterial == null)
                {
                    missingMaterial++;
                }

                Bounds bounds = renderer.bounds;
                float volume = bounds.size.x * bounds.size.y * bounds.size.z;

                if (volume < TinyBoundsVolume)
                {
                    tinyBounds++;
                }
                else if (volume > HugeBoundsVolume)
                {
                    hugeBounds++;
                }

                if (Vector3.Distance(bounds.center, enemyTransform.position) > BoundsCenterMaxDistance)
                {
                    hugeBounds++;
                }

                hasVisibleRenderer = true;
            }

            TrackModelBackupState(visualRoot, ref primaryModelInactive, ref backupModelActive);
        }
        else
        {
            Renderer rootRenderer = enemyTransform.GetComponent<Renderer>();

            if (rootRenderer != null && rootRenderer.enabled && rootRenderer.gameObject.activeInHierarchy)
            {
                hasVisibleRenderer = true;

                if (rootRenderer.sharedMaterial == null)
                {
                    missingMaterial++;
                }
            }
        }

        if (hasVisibleRenderer)
        {
            withVisibleRenderer++;
        }
        else
        {
            missingVisual++;
            missingIds.Add(enemyTransform.GetInstanceID());
        }

        if (hasInactiveVisualIssue || (backupModelActive && primaryModelInactive))
        {
            inactiveVisualChild++;
        }

        if (player != null)
        {
            Vector3 offset = enemyTransform.position - player.position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= RadarRange * RadarRange)
            {
                radarEligible++;

                if (mainCamera != null && !IsWorldPointInCameraView(mainCamera, enemyTransform.position))
                {
                    offscreenRadarOnly++;
                }
            }
        }
    }

    private static void TrackModelBackupState(
        Transform visualRoot,
        ref bool primaryModelInactive,
        ref bool backupModelActive)
    {
        Transform backup = FindDeepChild(visualRoot, "Model_Old_Backup");

        if (backup != null)
        {
            backupModelActive = backup.gameObject.activeSelf;
        }

        Transform motionRoot = FindDeepChild(visualRoot, "SlimeMotionRoot");
        Transform modelRoot = FindDeepChild(visualRoot, "Model");
        Transform primary = motionRoot != null ? motionRoot : modelRoot;

        if (primary != null && !primary.gameObject.activeInHierarchy)
        {
            primaryModelInactive = true;
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool HasBadScale(Transform root)
    {
        Vector3 scale = root.lossyScale;

        return float.IsNaN(scale.x) || float.IsNaN(scale.y) || float.IsNaN(scale.z)
            || scale.x <= 0.001f || scale.y <= 0.001f || scale.z <= 0.001f;
    }

    private static bool IsIntentionallyInactiveBackup(Transform transform)
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.name == "Model_Old_Backup")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Transform ResolvePlayerTransform()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            return playerObject.transform;
        }

        FPSPlayerController fpsController = FindFirstObjectByType<FPSPlayerController>();

        return fpsController != null ? fpsController.transform : null;
    }

    private static bool IsWorldPointInCameraView(Camera camera, Vector3 worldPoint)
    {
        Vector3 viewport = camera.WorldToViewportPoint(worldPoint);

        return viewport.z > 0f
            && viewport.x >= 0f && viewport.x <= 1f
            && viewport.y >= 0f && viewport.y <= 1f;
    }
}
