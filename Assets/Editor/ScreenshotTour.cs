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

    private static readonly string[] CategorySlugs = { "array", "linkedlist", "stack", "queue" };

    private const string ModeKey = "ScreenshotTour.Mode";
    private const string FolderKey = "ScreenshotTour.Folder";
    private const string QueueOverflowMode = "QueueOverflow";

    private const string UprightMarkersMode = "UprightMarkers";
    private const string ARChecksMode = "ARChecks";

    private static List<(float wait, Action action)> steps;
    private static List<(float wait, Action action)> Steps =>
        steps ??= SessionState.GetString(ModeKey, "") switch
        {
            QueueOverflowMode => BuildQueueOverflowSteps(),
            UprightMarkersMode => BuildUprightMarkerSteps(),
            ARChecksMode => BuildARCheckSteps(),
            _ => BuildSteps(),
        };

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
        ClearProgress();
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

    // Linked list and stack with the marker upright (on a monitor or wall), viewed head-on
    [MenuItem("Build/Capture Upright Markers")]
    public static void RunUprightMarkers()
    {
        Start(UprightMarkersMode, OutputFolder + "/UprightMarkers", "Assets/Scenes/MainMenu.unity", PortraitHeight, PortraitWidth);
    }

    public static void RunUprightMarkersAndQuit()
    {
        SessionState.SetBool(QuitKey, true);
        RunUprightMarkers();
    }

    private static List<(float, Action)> BuildUprightMarkerSteps()
    {
        List<(float, Action)> list = new List<(float, Action)>();
        void Do(float wait, Action action) => list.Add((wait, action));
        void Shot(float wait, string name) => Do(wait, () => Capture(name));

        Do(0.1f, () => SetGameViewSize(PortraitHeight, PortraitWidth));

        // Each scene viewed head-on with its stage card drawn underneath, walking through its operations
        ArrayManager Arr() => Object.FindObjectOfType<ArrayManager>();
        LinkedListManager List() => Object.FindObjectOfType<LinkedListManager>();
        StackManager Stk() => Object.FindObjectOfType<StackManager>();
        QueueManager Que() => Object.FindObjectOfType<QueueManager>();

        void Scene(string scene, string slug)
        {
            Do(0.5f, () => SceneManager.LoadScene(scene));
            Do(2.5f, ARDemo.PretendUprightMarkerFound);
            if (slug == "array")
                Shot(0.5f, "array-howto");
            Do(0.2f, () => Object.FindObjectOfType<ARGuide>().DismissHowTo());
        }
        void Frame() => Do(0.1f, ARDemo.FrameUpright);

        Scene("Array", "array");
        for (int i = 0; i < 3; i++) { int s = i; Do(1.0f, () => Arr().AddElement(s % 3)); }
        Frame();
        Shot(1.0f, "array-added");
        Do(0.2f, () => { Object.FindObjectOfType<ARGuide>().SetPosition(1); Arr().InsertAt(1, 1); });
        Shot(1.6f, "array-insert-at-1");
        Do(0.3f, () => Arr().DeleteAt(0));
        Shot(1.6f, "array-delete-at-0");

        Scene("LinkedList", "linkedlist");
        for (int i = 0; i < 3; i++) { int s = i; Do(0.6f, () => List().AddNode(s % 3)); }
        Frame();
        Shot(0.8f, "linkedlist-added");
        Do(0.2f, () => List().InsertAtHead(1));
        Frame();
        Shot(0.8f, "linkedlist-insert-head");
        Do(0.2f, () => List().DeleteAt(2));
        Shot(0.8f, "linkedlist-delete-node-3");

        Scene("Stack", "stack");
        Do(2.5f, () => Stk().Push(0));
        Frame();
        Shot(1.2f, "stack-pushed");
        Do(0.2f, () => Stk().Pop());
        Shot(1.0f, "stack-popped");

        Scene("queue", "queue");
        for (int i = 0; i < 5; i++) { int s = i; Do(1.3f, () => Que().Enqueue(s % 3)); }
        Frame();
        Shot(1.3f, "queue-full");
        Do(0.2f, () => Que().Dequeue());
        Do(0.8f, () => Que().Dequeue());
        Shot(0.8f, "queue-dequeued-twice");
        Do(0.2f, () => Que().Enqueue(1));
        Shot(1.4f, "queue-wrapped");
        return list;
    }

    // Runs every AR control through its edge cases; the captions land in the log as [Caption] lines
    public static void RunARChecksAndQuit()
    {
        SessionState.SetBool(QuitKey, true);
        Start(ARChecksMode, OutputFolder + "/ARChecks", "Assets/Scenes/MainMenu.unity", PortraitHeight, PortraitWidth);
    }

    private static List<(float, Action)> BuildARCheckSteps()
    {
        List<(float, Action)> list = new List<(float, Action)>();
        void Do(float wait, Action action) => list.Add((wait, action));
        void Note(string text) => Do(0.05f, () => Debug.Log("[Check] " + text));
        ArrayManager Arr() => Object.FindObjectOfType<ArrayManager>();
        LinkedListManager Lst() => Object.FindObjectOfType<LinkedListManager>();
        StackManager Stk() => Object.FindObjectOfType<StackManager>();
        QueueManager Que() => Object.FindObjectOfType<QueueManager>();
        void Scene(string scene)
        {
            Do(0.5f, () => SceneManager.LoadScene(scene));
            Do(2.5f, ARDemo.PretendUprightMarkerFound);
            Note("scene " + scene);
        }
        Do(0.1f, () => SetGameViewSize(PortraitHeight, PortraitWidth));

        Scene("Array");
        Note("remove from empty"); Do(0.2f, () => Arr().RemoveLastElement());
        Note("delete at 0 on empty"); Do(0.2f, () => Arr().DeleteAt(0));
        for (int i = 0; i < 3; i++) { Note("add"); Do(0.9f, () => Arr().AddElement(0)); }
        Note("insert at 4 with 3 elements (gap)"); Do(0.9f, () => Arr().InsertAt(4, 0));
        Note("insert at 3 (append)"); Do(0.9f, () => Arr().InsertAt(3, 0));
        Note("insert at 0 (shift 4)"); Do(0.9f, () => Arr().InsertAt(0, 1));
        Note("add when full"); Do(2.4f, () => Arr().AddElement(0));
        Note("insert at 1 when full"); Do(0.2f, () => Arr().InsertAt(1, 0));
        Note("delete at 4 (last)"); Do(0.2f, () => Arr().DeleteAt(4));
        Note("delete at 0 (shift 3)"); Do(0.9f, () => Arr().DeleteAt(0));
        Note("delete at 3 when 3 elements"); Do(1.5f, () => Arr().DeleteAt(3));
        Note("remove last"); Do(0.2f, () => Arr().RemoveLastElement());

        Scene("LinkedList");
        Note("remove from empty"); Do(0.2f, () => Lst().RemoveLastNode());
        Note("delete node 1 on empty"); Do(0.2f, () => Lst().DeleteAt(0));
        Note("insert at head on empty"); Do(0.2f, () => Lst().InsertAtHead(0));
        Note("add (1 node before)"); Do(0.6f, () => Lst().AddNode(0));
        Note("add (2 nodes before)"); Do(0.6f, () => Lst().AddNode(0));
        Note("insert at head (3 before)"); Do(0.6f, () => Lst().InsertAtHead(1));
        Note("delete node 5 with 4 nodes"); Do(0.6f, () => Lst().DeleteAt(4));
        Note("delete node 3"); Do(0.2f, () => Lst().DeleteAt(2));
        Note("delete node 2"); Do(0.6f, () => Lst().DeleteAt(1));
        Note("delete node 1 (head)"); Do(0.6f, () => Lst().DeleteAt(0));
        Note("add (1 before)"); Do(0.6f, () => Lst().AddNode(0));
        Note("remove last (2 nodes)"); Do(0.6f, () => Lst().RemoveLastNode());
        Note("remove last (1 node)"); Do(0.6f, () => Lst().RemoveLastNode());
        for (int i = 0; i < 5; i++) { Note("add to fill"); Do(0.6f, () => Lst().AddNode(0)); }
        Note("add when full"); Do(0.6f, () => Lst().AddNode(0));
        Note("insert at head when full"); Do(0.2f, () => Lst().InsertAtHead(0));
        Note("remove last (5 nodes)"); Do(0.2f, () => Lst().RemoveLastNode());

        Scene("Stack");
        Note("push x2 onto starting 3"); Do(2.5f, () => Stk().Push(0)); Do(0.8f, () => Stk().Push(0));
        Note("push when full"); Do(0.8f, () => Stk().Push(0));
        for (int i = 0; i < 5; i++) { Note("pop"); Do(0.7f, () => Stk().Pop()); }
        Note("pop when empty"); Do(0.7f, () => Stk().Pop());

        Scene("queue");
        Note("dequeue empty"); Do(0.2f, () => Que().Dequeue());
        for (int i = 0; i < 5; i++) { Note("enqueue"); Do(1.3f, () => Que().Enqueue(0)); }
        Note("enqueue when full"); Do(1.3f, () => Que().Enqueue(0));
        Note("dequeue x2"); Do(0.2f, () => Que().Dequeue()); Do(0.8f, () => Que().Dequeue());
        Note("enqueue x2 (wrap)"); Do(0.8f, () => Que().Enqueue(0)); Do(1.3f, () => Que().Enqueue(0));
        for (int i = 0; i < 5; i++) { Note("dequeue"); Do(1.3f, () => Que().Dequeue()); }
        Note("dequeue empty"); Do(0.8f, () => Que().Dequeue());
        // The missions, played straight through in each scene; Next moves on after each one
        void Mission() => Do(0.05f, () => Debug.Log("[Mission] " + Object.FindObjectOfType<ARGuide>().MissionStatus));
        void Next() => Do(0.4f, () => Object.FindObjectOfType<ARGuide>().NextMission());
        void Shot(float wait, string name) => Do(wait, () => Capture(name));
        void Start(string scene)
        {
            Scene(scene);
            Do(0.2f, () => Object.FindObjectOfType<ARGuide>().DismissHowTo());
            Mission();
        }

        Start("Array");
        Do(0.9f, () => Arr().AddElement(0));
        Do(0.2f, ARDemo.FrameUpright);
        Shot(1.0f, "array-mission-1-progress");
        Do(0.2f, () => Arr().AddElement(0)); Do(0.9f, () => Arr().AddElement(0));
        Shot(1.0f, "array-mission-1-complete"); Mission(); Next(); Mission();
        Shot(0.3f, "array-mission-2-highlight");
        Do(0.2f, () => Arr().InsertAt(0, 1));
        Do(2.0f, () => { }); Mission(); Next();
        Do(0.2f, () => Arr().DeleteAt(1));
        Do(1.6f, () => { }); Mission(); Next(); Mission();
        Shot(0.3f, "array-missions-done");

        Start("LinkedList");
        for (int i = 0; i < 3; i++) Do(0.6f, () => Lst().AddNode(0));
        Do(0.5f, () => { }); Mission(); Next(); Mission();
        Do(0.2f, ARDemo.FrameUpright);
        Shot(0.3f, "linkedlist-mission-2-highlight");
        Do(0.2f, () => Lst().InsertAtHead(1));
        Do(0.8f, () => { }); Mission(); Next();
        Do(0.2f, () => Lst().DeleteAt(1));
        Do(0.8f, () => { }); Mission();
        // A narrower 16:9 phone: everything should still fit
        Do(0.2f, () => SetGameViewSize(1280, 720));
        Do(0.4f, ARDemo.FrameUpright);
        Shot(0.3f, "linkedlist-16x9-mission-3-complete");
        Do(0.2f, () => SetGameViewSize(PortraitHeight, PortraitWidth));
        Next(); Mission();

        Start("Stack");
        Do(2.5f, () => Stk().Push(0)); Do(0.8f, () => Stk().Push(0));
        Do(0.5f, () => { }); Mission(); Next();
        Do(0.3f, () => Stk().Push(0)); Mission(); Next();
        for (int i = 0; i < 2; i++) Do(0.7f, () => Stk().Pop());
        Do(0.2f, ARDemo.FrameUpright);
        Shot(0.3f, "stack-mission-3-progress");
        for (int i = 0; i < 2; i++) Do(0.7f, () => Stk().Pop());
        Do(0.5f, () => { }); Mission();
        Shot(0.3f, "stack-mission-3-complete");
        Next(); Mission();

        Start("queue");
        for (int i = 0; i < 3; i++) Do(1.3f, () => Que().Enqueue(0));
        Do(1.3f, () => { }); Mission(); Next();
        Do(0.3f, () => Que().Dequeue());
        Do(0.8f, () => { }); Mission(); Next();
        Do(0.3f, () => Que().Enqueue(0)); Do(1.3f, () => Que().Enqueue(0)); Do(1.3f, () => Que().Enqueue(0));
        Do(1.4f, () => { }); Mission(); Next(); Mission();
        Do(0.2f, ARDemo.FrameUpright);
        Shot(0.3f, "queue-missions-done");

        Do(0.5f, () => Debug.Log("[Check] done"));
        return list;
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

        AppScreens Screens() => Object.FindObjectOfType<AppScreens>();
        QuizManager Quiz() => Object.FindObjectOfType<QuizManager>();
        for (int i = 0; i < QuizManager.Topics.Length; i++)
        {
            string slug = CategorySlugs[i], topic = QuizManager.Topics[i];

            Do(i == 0 ? 0.5f : 3f, () => Screens().SelectTopic(topic));
            if (i == 0)
                Shot(0.5f, "menu-selected");
            Do(0.3f, () => Screens().ShowLesson());
            Shot(1f, slug + "-info");
            Do(0.2f, () => ScrollLesson(0.6f));
            Shot(0.5f, slug + "-info-middle");
            Do(0.2f, () => ScrollLesson(0f));
            Shot(0.5f, slug + "-info-end");

            Do(0.3f, () => SetGameViewSize(PortraitHeight, PortraitWidth));
            Do(0.5f, () => Quiz().LoadARScene());
            Shot(5f, slug + "-ar");

            // Same as the AR scene's back-to-quiz button
            Do(0.5f, () => ReturnToMenu(1));
            Shot(3f, slug + "-quiz");
            Do(0.2f, () => Screens().Pick(0));
            Shot(0.4f, slug + "-quiz-feedback");
            Do(0.3f, () => Screens().NextQuestion());
            for (int q = 1; q < 5; q++)
            {
                Do(0.3f, () => Screens().Pick(0));
                Do(0.4f, () => Screens().NextQuestion());
            }
            Shot(1f, slug + "-quiz-finished");
            Do(0.3f, () => Screens().ShowResult());
            Shot(1f, slug + "-result");

            Do(0.3f, () => SetGameViewSize(PortraitHeight, PortraitWidth));
            Do(0.3f, () => Quiz().OpenPractice());
            Shot(1.5f, slug + "-practice");
            if (i == 0)
            {
                Do(0.2f, () => Object.FindObjectOfType<ArrayPuzzle>().TypeAndInsert("1", "45"));
                Shot(0.45f, "array-practice-shifting");
                Shot(1.2f, "array-practice-wrong");
                Do(3f, () => Object.FindObjectOfType<ArrayPuzzle>().TypeAndInsert("3", "45"));
                Shot(1.2f, "array-practice-correct");
            }
            else if (i == 1)
            {
                Do(0.2f, () => SolveLinkedList());
                Shot(1.2f, "linkedlist-practice-solved");
            }
            else if (i == 2)
            {
                Do(0.2f, () => Object.FindObjectOfType<StackPuzzle>().Press("W"));
                Shot(0.4f, "stack-practice-overflow");
            }
            else
            {
                Do(0.2f, () => Object.FindObjectOfType<QueuePuzzle>().Press("7"));
                Shot(0.4f, "queue-practice-overflow");
            }

            // Same as the AR scene's exit button, which returns to the topic menu
            Do(3.5f, () => ReturnToMenu(2));
        }

        Shot(2f, "menu-progress");
        Do(1f, () => { PlayerPrefs.SetInt("ReturnedFromAR", 0); PlayerPrefs.Save(); });
        return steps;
    }

    // Swaps nodes into A, B, C, D order one pair at a time
    private static void SolveLinkedList()
    {
        LinkedListPuzzle puzzle = Object.FindObjectOfType<LinkedListPuzzle>();
        string[] order = puzzle.Order;
        for (int i = 0; i < order.Length; i++)
        {
            int at = System.Array.IndexOf(order, ((char)('A' + i)).ToString());
            if (at != i)
                puzzle.Swap(i, at);
            order = puzzle.Order;
        }
    }

    // So the menu's saved-progress labels start from scratch on every tour
    private static void ClearProgress()
    {
        foreach (string topic in new[] { "Array", "Linked List", "Stack", "Queue" })
        {
            PlayerPrefs.DeleteKey("BestScore_" + topic);
            PlayerPrefs.DeleteKey("QuestionCount_" + topic);
            PlayerPrefs.DeleteKey("PracticeDone_" + topic);
        }
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
