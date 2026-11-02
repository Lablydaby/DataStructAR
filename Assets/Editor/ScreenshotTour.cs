using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Dev tool: lists the MainMenu UI so screens can be walked through and captured.
// Unity.exe -batchmode -quit -projectPath <project> -executeMethod ScreenshotTour.DumpMainMenu
public static class ScreenshotTour
{
    private const string OutputFolder = "Builds/Screenshots";

    public static void DumpMainMenu()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        StringBuilder sb = new StringBuilder();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Dump(root.transform, 0, sb);

        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(Path.Combine(OutputFolder, "mainmenu-hierarchy.txt"), sb.ToString());
    }

    private static void Dump(Transform t, int depth, StringBuilder sb)
    {
        sb.Append(' ', depth * 2).Append(t.gameObject.activeSelf ? "+ " : "- ").Append(t.name);

        Button button = t.GetComponent<Button>();
        if (button != null)
        {
            sb.Append("  [Button");
            SerializedProperty calls = new SerializedObject(button).FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                Object target = button.onClick.GetPersistentTarget(i);
                SerializedProperty args = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_Arguments");
                sb.Append(" -> ").Append(target != null ? target.name : "null")
                  .Append('.').Append(button.onClick.GetPersistentMethodName(i))
                  .Append('(').Append(args.FindPropertyRelative("m_BoolArgument").boolValue)
                  .Append(',').Append(args.FindPropertyRelative("m_StringArgument").stringValue).Append(')');
                if (button.onClick.GetPersistentListenerState(i) == UnityEventCallState.Off)
                    sb.Append("(off)");
            }
            sb.Append(']');
        }

        TMPro.TMP_Text label = t.GetComponent<TMPro.TMP_Text>();
        if (label != null && !string.IsNullOrWhiteSpace(label.text))
            sb.Append("  \"").Append(label.text.Replace("\n", " ").Trim()).Append('"');

        sb.AppendLine();
        foreach (Transform child in t)
            Dump(child, depth + 1, sb);
    }
}
