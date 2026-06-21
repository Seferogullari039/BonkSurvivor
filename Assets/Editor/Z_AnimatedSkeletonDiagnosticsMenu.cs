using UnityEditor;
using UnityEngine;

public static class Z_AnimatedSkeletonDiagnosticsMenu
{
    [MenuItem("Tools/BonkSurvivor/Repair Animated Skeleton Clip Frame Ranges", false, 9988)]
    public static void RepairAnimatedSkeletonClipFrameRanges()
    {
        Debug.Log("[Z_AnimatedSkeletonDiagnosticsMenu] Clip frame repair menu clicked.");
        AnimatedSkeletonClipFrameRepair.RepairClipFrameRanges();
    }

    [MenuItem("Tools/BonkSurvivor/ZZ Diagnostic Menu Ping", false, 9989)]
    public static void Ping()
    {
        Debug.Log("[Z_AnimatedSkeletonDiagnosticsMenu] Ping OK - menu registered.");
    }

    [MenuItem("Tools/BonkSurvivor/Audit Animated Skeleton Curve Bindings", false, 9990)]
    public static void AuditAnimatedSkeletonCurveBindings()
    {
        Debug.Log("[Z_AnimatedSkeletonDiagnosticsMenu] Curve binding audit menu clicked.");
        AnimatedSkeletonCurveBindingAudit.RunCurveBindingAudit();
    }

    [MenuItem("Tools/BonkSurvivor/Prove Animated Skeleton Clip Binding", false, 9991)]
    public static void ProveAnimatedSkeletonClipBinding()
    {
        Debug.Log("[Z_AnimatedSkeletonDiagnosticsMenu] Prove menu clicked.");
        AnimatedSkeletonBindingProof.RunProof();
    }

    [MenuItem("Tools/BonkSurvivor/Fix Tank Skeleton Playback", false, 9992)]
    public static void FixTankSkeletonPlayback()
    {
        Debug.Log("[Z_AnimatedSkeletonDiagnosticsMenu] Fix playback menu clicked.");
        AnimatedSkeletonClipAudit.FixTankSkeletonPlayback();
    }

    [MenuItem("Tools/BonkSurvivor/Audit Tank Skeleton Prefab Binding", false, 9994)]
    public static void AuditTankSkeletonPrefabBinding()
    {
        Debug.Log(AnimatedSkeletonBindingProof.BuildTankViewPrefabBindingAuditReport());
    }

    [MenuItem("Tools/BonkSurvivor/Repair Tank Skeleton Prefab Binding", false, 9993)]
    public static void RepairTankSkeletonPrefabBinding()
    {
        Debug.Log("[Z_AnimatedSkeletonDiagnosticsMenu] Repair prefab binding menu clicked.");
        AnimatedSkeletonBindingProof.RepairTankSkeletonPrefabBinding();
    }
}
