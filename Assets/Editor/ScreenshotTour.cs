using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Dev tool: walks through every app screen in Play mode and saves a screenshot of each to Builds/Screenshots.
// Run from the Build menu, or: Unity.exe -projectPath <project> -executeMethod ScreenshotTour.RunAndQuit
[InitializeOnLoad]
public static class ScreenshotTour
{
    private const string OutputFolder = "Builds/Screenshots";
    private const string StepKey = "ScreenshotTour.Step";
    private const string NextTimeKey = "ScreenshotTour.NextTime";
    private const string ShotKey = "ScreenshotTour.Shot";
    private const string QuitKey = "ScreenshotTour.Quit";

    private static readonly string[] CategoryButtons = { "ArrayBtn", "LinkedListBtn", "StackBtn", "QueueBtn" };
    private static readonly string[] CategorySlugs = { "array", "linkedlist", "stack", "queue" };

    private static readonly List<(float wait, Action action)> Steps = BuildSteps();

    // Play mode reloads scripts, so the tour's progress lives in SessionState and resumes here
    static ScreenshotTour()
    {
        if (SessionState.GetInt(StepKey, -1) >= 0)
            EditorApplication.update += Tick;

        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(QuitKey, false))
                EditorApplication.Exit(0);
        };
    }

    [MenuItem("Build/Capture Screenshot Tour")]
    public static void Run()
    {
        Directory.CreateDirectory(OutputFolder);
        PlayerPrefs.SetInt("ReturnedFromAR", 0);
        PlayerPrefs.Save();

        SessionState.SetInt(StepKey, 0);
        SessionState.SetFloat(NextTimeKey, 0);
        SessionState.SetInt(ShotKey, 1);

        SetGameViewSize(720, 1280);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    public static void RunAndQuit()
    {
        SessionState.SetBool(QuitKey, true);
        Run();
    }

    private static List<(float, Action)> BuildSteps()
    {
        List<(float, Action)> steps = new List<(float, Action)>();
        void Do(float wait, Action action) => steps.Add((wait, action));
        void Shot(float wait, string name) => Do(wait, () => Capture(name));
        void Click(float wait, string path) => Do(wait, () => ClickButton(path));

        Shot(1f, "splash");
        Shot(2.5f, "intro");
        Click(0.5f, "Splash2/BG/NextButton");
        Shot(1f, "menu");

        for (int i = 0; i < CategoryButtons.Length; i++)
        {
            string slug = CategorySlugs[i];

            Click(i == 0 ? 0.5f : 3f, "Options/BG/" + CategoryButtons[i]);
            Click(0.3f, "Options/BG/ShowInfo");
            Shot(1f, slug + "-info");

            Do(0.3f, () => SetGameViewSize(1280, 720));
            Click(0.5f, "Info/BG/StartARButton");
            Shot(5f, slug + "-ar");

            // Same as the AR scene's back-to-quiz button
            Do(0.5f, () => ReturnToMenu(1));
            Shot(3f, slug + "-quiz");
            for (int q = 0; q < 10; q++)
                Click(0.2f, "Quiz/BG/Question1");
            Shot(0.5f, slug + "-quiz-finished");
            Click(0.3f, "Quiz/BG/Done");
            Shot(1f, slug + "-result");

            Do(0.3f, () => SetGameViewSize(1280, 720));
            Click(0.3f, "Result/BG/Next");
            Shot(1.5f, slug + "-practice");

            // Same as the AR scene's exit button, which returns to the topic menu
            Do(0.5f, () => ReturnToMenu(2));
        }

        Do(1f, () => { PlayerPrefs.SetInt("ReturnedFromAR", 0); PlayerPrefs.Save(); });
        return steps;
    }

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, -1);
        if (step < 0)
        {
            EditorApplication.update -= Tick;
            return;
        }
        if (!EditorApplication.isPlaying)
            return;

        float now = (float)EditorApplication.timeSinceStartup;
        float nextTime = SessionState.GetFloat(NextTimeKey, 0);
        if (nextTime == 0)
        {
            SessionState.SetFloat(NextTimeKey, now + Steps[step].wait);
            return;
        }
        if (now < nextTime)
            return;

        try
        {
            Steps[step].action();
        }
        catch (Exception e)
        {
            Debug.LogError($"[ScreenshotTour] Step {step} failed: {e}");
        }

        step++;
        SessionState.SetFloat(NextTimeKey, 0);
        if (step < Steps.Count)
        {
            SessionState.SetInt(StepKey, step);
            return;
        }

        SessionState.SetInt(StepKey, -1);
        EditorApplication.update -= Tick;
        Debug.Log("[ScreenshotTour] Finished.");
        EditorApplication.ExitPlaymode();
    }

    private static void ReturnToMenu(int returnedFromAR)
    {
        PlayerPrefs.SetInt("ReturnedFromAR", returnedFromAR);
        PlayerPrefs.Save();
        SetGameViewSize(720, 1280);
        SceneManager.LoadScene("MainMenu");
    }

    private static void Capture(string name)
    {
        int shot = SessionState.GetInt(ShotKey, 1);
        SessionState.SetInt(ShotKey, shot + 1);
        string path = Path.Combine(OutputFolder, $"{shot:00}-{name}.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[ScreenshotTour] Captured " + path);
    }

    private static void ClickButton(string pathSuffix)
    {
        foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (!button.gameObject.scene.IsValid() || !GetPath(button.transform).EndsWith(pathSuffix))
                continue;

            button.onClick.Invoke();
            return;
        }
        Debug.LogWarning("[ScreenshotTour] Button not found: " + pathSuffix);
    }

    private static string GetPath(Transform t)
    {
        return t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;
    }

    // Game view sizes are internal, so they are set through reflection
    private static void SetGameViewSize(int width, int height)
    {
        try
        {
            Assembly editorAssembly = typeof(Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            object group = sizesType.GetProperty("currentGroup").GetValue(sizes);
            Type groupType = group.GetType();

            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < total && index < 0; i++)
            {
                object size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                Type sizeType = size.GetType();
                if ((int)sizeType.GetProperty("width").GetValue(size) == width &&
                    (int)sizeType.GetProperty("height").GetValue(size) == height &&
                    sizeType.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution")
                    index = i;
            }

            if (index < 0)
            {
                Type gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                object fixedResolution = Enum.Parse(editorAssembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution");
                object newSize = Activator.CreateInstance(gameViewSizeType, fixedResolution, width, height, $"Tour {width}x{height}");
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                index = total;
            }

            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(gameView, new object[] { index, null });
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ScreenshotTour] Could not set Game view size: " + e.Message);
        }
    }

    // Lists the MainMenu UI with each button's click actions
    // Unity.exe -batchmode -quit -projectPath <project> -executeMethod ScreenshotTour.DumpMainMenu
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
