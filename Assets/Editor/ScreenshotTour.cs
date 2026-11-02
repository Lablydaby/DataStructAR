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

    // A common modern Android phone shape (19:9)
    private const int PortraitWidth = 720;
    private const int PortraitHeight = 1520;

    private static readonly string[] CategoryButtons = { "ArrayBtn", "LinkedListBtn", "StackBtn", "QueueBtn" };
    private static readonly string[] CategorySlugs = { "array", "linkedlist", "stack", "queue" };

    private const string ModeKey = "ScreenshotTour.Mode";
    private const string FolderKey = "ScreenshotTour.Folder";
    private const string QueueOverflowMode = "QueueOverflow";

    private static List<(float wait, Action action)> steps;
    private static List<(float wait, Action action)> Steps =>
        steps ??= SessionState.GetString(ModeKey, "") == QueueOverflowMode ? BuildQueueOverflowSteps() : BuildSteps();

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
        PlayerPrefs.SetInt("ReturnedFromAR", 0);
        PlayerPrefs.Save();
        Start("", OutputFolder, "Assets/Scenes/MainMenu.unity", PortraitWidth, PortraitHeight);
    }

    public static void RunAndQuit()
    {
        SessionState.SetBool(QuitKey, true);
        Run();
    }

    // Shows what happens when a sixth shape is enqueued into the full AR queue
    [MenuItem("Build/Capture Queue Overflow")]
    public static void RunQueueOverflow()
    {
        Start(QueueOverflowMode, OutputFolder + "/QueueOverflow", "Assets/Scenes/AR/queue.unity", PortraitHeight, PortraitWidth);
    }

    public static void RunQueueOverflowAndQuit()
    {
        SessionState.SetBool(QuitKey, true);
        RunQueueOverflow();
    }

    private static void Start(string mode, string folder, string scenePath, int width, int height)
    {
        Directory.CreateDirectory(folder);
        SessionState.SetString(ModeKey, mode);
        SessionState.SetString(FolderKey, folder);
        SessionState.SetInt(StepKey, 0);
        SessionState.SetFloat(NextTimeKey, 0);
        SessionState.SetInt(ShotKey, 1);
        steps = null;

        SetGameViewSize(width, height);
        EditorSceneManager.OpenScene(scenePath);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }

    private static List<(float, Action)> BuildQueueOverflowSteps()
    {
        List<(float, Action)> list = new List<(float, Action)>();
        void Do(float wait, Action action) => list.Add((wait, action));
        void Shot(float wait, string name) => Do(wait, () => Capture(name));

        Do(0.1f, () => SetGameViewSize(PortraitHeight, PortraitWidth));
        Do(3f, ARDemo.PretendMarkerFound);
        Shot(0.8f, "marker-found");
        for (int i = 0; i < 5; i++)
        {
            int shape = i;
            Do(i == 0 ? 0.5f : 1.3f, () => ARDemo.Queue.Enqueue(shape % ARDemo.Queue.shapePrefabs.Length));
        }
        Shot(1.5f, "queue-full");
        Do(0.2f, () => ARDemo.Queue.Enqueue(0));
        Shot(0.5f, "overflow-message");
        Do(0.5f, () => ARDemo.Queue.Dequeue());
        Shot(0.5f, "dequeue-shifting");
        Shot(1f, "after-dequeue");
        return list;
    }

    private static List<(float, Action)> BuildSteps()
    {
        List<(float, Action)> steps = new List<(float, Action)>();
        void Do(float wait, Action action) => steps.Add((wait, action));
        void Shot(float wait, string name) => Do(wait, () => Capture(name));
        void Click(float wait, string path) => Do(wait, () => ClickButton(path));

        Do(0.1f, () => SetGameViewSize(PortraitWidth, PortraitHeight));
        Shot(0.6f, "splash");
        Shot(2.5f, "intro");
        Click(0.5f, "Splash2/BG/NextButton");
        Shot(1f, "menu");

        for (int i = 0; i < CategoryButtons.Length; i++)
        {
            string slug = CategorySlugs[i];

            Click(i == 0 ? 0.5f : 3f, "Options/BG/" + CategoryButtons[i]);
            if (i == 0)
                Shot(0.5f, "menu-selected");
            Click(0.3f, "Options/BG/ShowInfo");
            Shot(1f, slug + "-info");
            Do(0.2f, () => ScrollLesson(0.6f));
            Shot(0.5f, slug + "-info-middle");
            Do(0.2f, () => ScrollLesson(0f));
            Shot(0.5f, slug + "-info-end");

            Do(0.3f, () => SetGameViewSize(PortraitHeight, PortraitWidth));
            Click(0.5f, "Info/BG/StartARButton");
            Shot(5f, slug + "-ar");

            // Same as the AR scene's back-to-quiz button
            Do(0.5f, () => ReturnToMenu(1));
            Shot(3f, slug + "-quiz");
            Click(0.2f, "Quiz/BG/Question1");
            Shot(0.4f, slug + "-quiz-feedback");
            for (int q = 1; q < 5; q++)
                Click(1.2f, "Quiz/BG/Question1");
            Shot(1.5f, slug + "-quiz-finished");
            Click(0.3f, "Quiz/BG/Done");
            Shot(1f, slug + "-result");

            Do(0.3f, () => SetGameViewSize(PortraitHeight, PortraitWidth));
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
        SetGameViewSize(PortraitWidth, PortraitHeight);
        SceneManager.LoadScene("MainMenu");
    }

    private static void ScrollLesson(float normalizedPosition)
    {
        foreach (ScrollRect scroll in Object.FindObjectsOfType<ScrollRect>())
        {
            if (scroll.name == "Scroll View")
                scroll.verticalNormalizedPosition = normalizedPosition;
        }
    }

    private static void Capture(string name)
    {
        int shot = SessionState.GetInt(ShotKey, 1);
        SessionState.SetInt(ShotKey, shot + 1);
        string path = Path.Combine(SessionState.GetString(FolderKey, OutputFolder), $"{shot:00}-{name}.png");
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
        DumpScene("Assets/Scenes/MainMenu.unity", "mainmenu-hierarchy.txt");
        DumpScene("Assets/Scenes/AR/Array.unity", "array-ar-hierarchy.txt");
    }

    private static void DumpScene(string scenePath, string fileName)
    {
        EditorSceneManager.OpenScene(scenePath);
        StringBuilder sb = new StringBuilder();
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            Dump(root.transform, 0, sb);

        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(Path.Combine(OutputFolder, fileName), sb.ToString());
    }

    private static void Dump(Transform t, int depth, StringBuilder sb)
    {
        sb.Append(' ', depth * 2).Append(t.gameObject.activeSelf ? "+ " : "- ").Append(t.name);

        // Layout and visuals, so UI can be adjusted from code without guessing
        if (t is RectTransform rt)
            sb.Append($"  rect(anchor {rt.anchorMin}-{rt.anchorMax} pos {rt.anchoredPosition} size {rt.sizeDelta} pivot {rt.pivot})");
        foreach (Component c in t.GetComponents<Component>())
        {
            if (c == null) { sb.Append(" {Missing}"); continue; }
            if (c is Transform || c is CanvasRenderer) continue;
            sb.Append(" {").Append(c.GetType().Name);
            if (c is Image img)
                sb.Append($" sprite={(img.sprite != null ? img.sprite.name : "none")} color=#{ColorUtility.ToHtmlStringRGBA(img.color)} type={img.type}");
            if (c is TMPro.TMP_Text tmp)
                sb.Append($" font={(tmp.font != null ? tmp.font.name : "none")} size={tmp.fontSize} color=#{ColorUtility.ToHtmlStringRGBA(tmp.color)} align={tmp.alignment} style={tmp.fontStyle}");
            if (c is LayoutGroup lg)
                sb.Append($" padding={lg.padding} align={lg.childAlignment}");
            if (c is HorizontalOrVerticalLayoutGroup hv)
                sb.Append($" spacing={hv.spacing} ctrlW={hv.childControlWidth} ctrlH={hv.childControlHeight} expandW={hv.childForceExpandWidth} expandH={hv.childForceExpandHeight}");
            if (c is ContentSizeFitter csf)
                sb.Append($" h={csf.verticalFit} w={csf.horizontalFit}");
            sb.Append('}');
        }

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
