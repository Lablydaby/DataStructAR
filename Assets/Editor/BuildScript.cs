using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the app from the Build menu, or from the command line:
// Unity.exe -batchmode -quit -projectPath <project> -buildTarget iOS -executeMethod BuildScript.BuildIOS
public static class BuildScript
{
    private const string BundleId = "com.lablydaby.datastructar";
    private const string CameraUsage = "The camera is used to detect the AR markers.";

    [MenuItem("Build/iOS Xcode Project")]
    public static void BuildIOS()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
        PlayerSettings.iOS.cameraUsageDescription = CameraUsage; // iOS closes the app on camera access without this
        PlayerSettings.iOS.targetOSVersionString = "15.0";

        Build(BuildTarget.iOS, "Builds/iOS");
    }

    [MenuItem("Build/Android APK")]
    public static void BuildAndroid()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleId);
        EditorUserBuildSettings.buildAppBundle = false; // .apk, not .aab

        Build(BuildTarget.Android, "Builds/Android/DataStructAR.apk");
    }

    private static void Build(BuildTarget target, string outputPath)
    {
        AssetDatabase.SaveAssets();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[BuildScript] {target} build {report.summary.result} with {report.summary.totalErrors} errors -> {outputPath}");

        if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
