using UnityEditor;
using UnityEngine;

public static class AnimatedSkeletonDiagnosticsMenu
{
    [MenuItem("Tools/BonkSurvivor/Prove Animated Skeleton Clip Binding", false, 32)]
    public static void ProveAnimatedSkeletonClipBindingMenu()
    {
        Debug.Log("[AnimatedSkeletonDiagnosticsMenu] Prove Animated Skeleton Clip Binding");
        AnimatedSkeletonBindingProof.RunProof();
    }

    [MenuItem("Tools/BonkSurvivor/Fix Tank Skeleton Playback", false, 31)]
    public static void FixTankSkeletonPlaybackMenu()
    {
        Debug.Log("[AnimatedSkeletonDiagnosticsMenu] Fix Tank Skeleton Playback");
        AnimatedSkeletonClipAudit.FixTankSkeletonPlayback();
    }

    [MenuItem("Tools/BonkSurvivor/Repair Tank Skeleton Prefab Binding", false, 33)]
    public static void RepairTankSkeletonPrefabBindingMenu()
    {
        Debug.Log("[AnimatedSkeletonDiagnosticsMenu] Repair Tank Skeleton Prefab Binding");
        AnimatedSkeletonBindingProof.RepairTankSkeletonPrefabBinding();
    }
}
