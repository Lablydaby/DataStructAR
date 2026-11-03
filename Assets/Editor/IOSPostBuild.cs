#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// Fixes up the exported Xcode project so it builds on recent Xcode versions without manual changes
public static class IOSPostBuild
{
    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS)
            return;

        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

        // Xcode 15+ sandboxes build scripts by default, which blocks Unity's script phases
        // (e.g. MapFileParser.sh) with "Sandbox: deny file-read-data" errors
        foreach (string guid in new[] { project.ProjectGuid(), project.GetUnityMainTargetGuid(), project.GetUnityFrameworkTargetGuid() })
            project.SetBuildProperty(guid, "ENABLE_USER_SCRIPT_SANDBOXING", "NO");

        File.WriteAllText(projectPath, project.WriteToString());
    }
}
#endif
