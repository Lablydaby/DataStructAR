using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// One-time layout fixes applied to the scenes. Safe to run again.
// Unity.exe -batchmode -quit -projectPath <project> -executeMethod UIUpgrade.Apply
public static class UIUpgrade
{
    private const string MainMenu = "Assets/Scenes/MainMenu.unity";
    private static readonly Color BrandColor = new Color32(0x45, 0x37, 0x74, 0xFF);

    [MenuItem("Build/Apply UI Upgrade")]
    public static void Apply()
    {
        UpgradeMainMenu();
        AddARGuide("Assets/Scenes/AR/Array.unity", "Array",
            "Pick a shape, then tap Add to fill the next slot or Remove to empty the last one. Tap Next when you're ready for the quiz.");
        AddARGuide("Assets/Scenes/AR/LinkedList.unity", "Linked List",
            "Pick a shape, then tap Add to attach a node or Remove to delete the last one. Tap Next when you're ready for the quiz.");
        AddARGuide("Assets/Scenes/AR/Stack.unity", "Stack",
            "Pick a shape, then tap Push to add it on top or Pop to remove the top block. Tap Next when you're ready for the quiz.");
        AddARGuide("Assets/Scenes/AR/queue.unity", "Queue",
            "Pick a shape, then tap Enqueue to add it at the rear or Dequeue to remove the front. Tap Next when you're ready for the quiz.");
        Debug.Log("[UIUpgrade] Done.");
    }

    private static void UpgradeMainMenu()
    {
        EditorSceneManager.OpenScene(MainMenu);

        // Envelope Parent crops the top and bottom of every screen on phones wider than the
        // iPhone-shaped design. Fit In Parent keeps everything visible on any aspect ratio.
        foreach (AspectRatioFitter fitter in Object.FindObjectsOfType<AspectRatioFitter>(true))
        {
            if (fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent)
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        }

        // The practice panels sit over the menu, so their edges must be opaque once letterboxed
        foreach (string panel in new[] { "QuizArray", "QuizQueue", "QuizLinkedList", "QuizStack" })
        {
            Image background = Find("CanvasHorizontal/" + panel).GetComponent<Image>();
            background.color = Color.white;
        }

        LayOutStackBelowBanner();
        AnchorArrowsBetweenBoxes("CanvasHorizontal/QuizQueue/BG/NumbersData");
        AnchorArrowsBetweenBoxes("CanvasHorizontal/QuizLinkedList/BG/NumbersData");
        AddEndLabels("CanvasHorizontal/QuizQueue/BG/NumbersData", "REAR", "FRONT");
        AddEndLabels("CanvasHorizontal/QuizLinkedList/BG/NumbersData", "HEAD", null);

        AddLabelBelow("CanvasVertical/Result/BG/Reload", "Retry");
        AddLabelBelow("CanvasVertical/Result/BG/Next", "Practice");

        HeaderManager stackBanner = Find("CanvasHorizontal/QuizStack/BG/Heading").GetComponent<HeaderManager>();
        stackBanner.openHeading = StackQuiz.StackInstructions;
        EditorUtility.SetDirty(stackBanner);

        FixQueueQuizAnswer();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    // The question banner covered the top stack slot, so the four slots now fit between the
    // bottom button bar (21% of the height) and the open banner (about 69%)
    private static void LayOutStackBelowBanner()
    {
        RectTransform numbers = Find("CanvasHorizontal/QuizStack/BG/NumbersData");
        SetAnchors(numbers, new Vector2(0.14f, 0.235f), new Vector2(0.84f, 0.665f));

        string[] topToBottom = { "W", "X", "Y", "Z" };
        float slot = 1f / (4f + 3f * 0.25f);
        float gap = slot * 0.25f;
        for (int i = 0; i < topToBottom.Length; i++)
        {
            float top = 1f - i * (slot + gap);
            SetAnchors((RectTransform)numbers.Find(topToBottom[i]), new Vector2(0.33f, top - slot), new Vector2(0.67f, top));
        }
    }

    // The arrows were placed inside a fixed 100x100 box, so they drifted onto the stretched boxes
    // on other screen sizes. Anchoring each arrow to the gap between two boxes keeps it centered.
    private static void AnchorArrowsBetweenBoxes(string numbersPath)
    {
        RectTransform numbers = Find(numbersPath);
        Transform arrowsBox = numbers.Find("Arrows");
        if (arrowsBox == null)
            return;

        List<RectTransform> boxes = Boxes(numbers);
        List<RectTransform> arrows = arrowsBox.Cast<Transform>().Select(t => (RectTransform)t).OrderBy(t => t.name).ToList();

        for (int i = 0; i < arrows.Count && i + 1 < boxes.Count; i++)
        {
            RectTransform left = boxes[i];
            RectTransform right = boxes[i + 1];
            float gap = right.anchorMin.x - left.anchorMax.x;
            float centerY = (left.anchorMin.y + left.anchorMax.y) / 2f;
            float halfHeight = (left.anchorMax.y - left.anchorMin.y) * 0.22f;

            arrows[i].SetParent(numbers, false);
            SetAnchors(arrows[i], new Vector2(left.anchorMax.x + gap * 0.15f, centerY - halfHeight),
                new Vector2(right.anchorMin.x - gap * 0.15f, centerY + halfHeight));
            arrows[i].GetComponent<Image>().preserveAspect = true;
        }

        if (arrowsBox.childCount == 0)
            Object.DestroyImmediate(arrowsBox.gameObject);
    }

    // Small captions under the first and last box, e.g. REAR and FRONT for the queue
    private static void AddEndLabels(string numbersPath, string firstLabel, string lastLabel)
    {
        RectTransform numbers = Find(numbersPath);
        List<RectTransform> boxes = Boxes(numbers);
        if (firstLabel != null)
            AddCaption(numbers, boxes[0], firstLabel);
        if (lastLabel != null)
            AddCaption(numbers, boxes[boxes.Count - 1], lastLabel);
    }

    private static void AddCaption(RectTransform parent, RectTransform box, string text)
    {
        string name = "Label " + text;
        Transform existing = parent.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        TextMeshProUGUI label = CreateText(parent, name, text, 44f);
        float height = box.anchorMax.y - box.anchorMin.y;
        SetAnchors(label.rectTransform, new Vector2(box.anchorMin.x - 0.03f, box.anchorMin.y - height * 0.4f),
            new Vector2(box.anchorMax.x + 0.03f, box.anchorMin.y - height * 0.05f));
    }

    private static void AddLabelBelow(string buttonPath, string text)
    {
        RectTransform button = Find(buttonPath);
        RectTransform parent = (RectTransform)button.parent;
        string name = button.name + " Label";
        Transform existing = parent.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        TextMeshProUGUI label = CreateText(parent, name, text, 40f);
        float height = button.anchorMax.y - button.anchorMin.y;
        SetAnchors(label.rectTransform, new Vector2(button.anchorMin.x - 0.02f, button.anchorMin.y - height * 0.7f),
            new Vector2(button.anchorMax.x + 0.02f, button.anchorMin.y - height * 0.05f));
    }

    // Matches the lesson: dequeuing an empty queue is an underflow error
    private static void FixQueueQuizAnswer()
    {
        QuizManager quiz = Object.FindObjectOfType<QuizManager>(true);
        foreach (QuizManager.QuizQuestion question in quiz.quizCategories.Find(c => c.categoryName == "Queue").questions)
        {
            if (question.question.Contains("dequeue an element from an empty queue"))
                question.correctAnswerIndex = System.Array.FindIndex(question.options, o => o.Contains("underflow"));
        }
        EditorUtility.SetDirty(quiz);
    }

    private static void AddARGuide(string scenePath, string topic, string hint)
    {
        EditorSceneManager.OpenScene(scenePath);
        ARGuide guide = Object.FindObjectOfType<ARGuide>(true);
        if (guide == null)
            guide = new GameObject("ARGuide").AddComponent<ARGuide>();
        guide.topicName = topic;
        guide.controlsHint = hint;
        EditorUtility.SetDirty(guide);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }

    private static List<RectTransform> Boxes(RectTransform numbers)
    {
        return numbers.Cast<Transform>()
            .Where(t => t.name.StartsWith("NumberBG") || t.name.StartsWith("GrayOut"))
            .Select(t => (RectTransform)t)
            .OrderBy(t => t.anchorMin.x)
            .ToList();
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Poppins-Bold SDF t:TMP_FontAsset")[0]));
        label.fontSize = size;
        label.color = BrandColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private static RectTransform Find(string path)
    {
        string[] parts = path.Split(new[] { '/' }, 2);
        GameObject root = EditorSceneManager.GetActiveScene().GetRootGameObjects().First(g => g.name == parts[0]);
        Transform found = parts.Length > 1 ? root.transform.Find(parts[1]) : root.transform;
        if (found == null)
            throw new System.Exception("[UIUpgrade] Not found: " + path);
        return (RectTransform)found;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
