using UnityEngine;
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;

public static class BuildScript
{
    public static void PerformBuild()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        Debug.Log("[BuildScript] Starting Addressables Content Build...");
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressablesResult);

        if(!string.IsNullOrEmpty(addressablesResult.Error))
        {
            throw new Exception($"[BuildScript] Addressables Build Failed: {addressablesResult.Error}");
        }

        Debug.Log("[BuildScript] Addressables Content Build Successfully");
        BuildPlayerOptions buildPlayerOptions = new()
        {
            scenes = new[]{"Assets/Scenes/MainScene.unity"},
            locationPathName = "Builds/Android/ElementalBlacksmithStory.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };
        Debug.Log("[BuildScript] Starting Android Player Build");

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        if(report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new Exception("빌드 실패");
        }
        Debug.Log("[BuildScript] Android Build Completed Successfully");
    }
}
