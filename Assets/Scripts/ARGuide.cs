using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

// The AR scene's on-screen layer, built to the v4 design: a scan prompt until the marker is found,
// a "How to use" card the first time, then the controls (back, add/remove with a count, shape picker,
// Go to Quiz) and short messages. The new buttons trigger the scene's original buttons, which are hidden.
public class ARGuide : MonoBehaviour
{
    private static readonly Vector2 Landscape = new Vector2(844, 390);

    public string topicName = "Array";
    [TextArea] public string controlsHint = "Pick a shape, then tap Add or Remove.";

    private ImageTargetBehaviour[] targets;
    private RectTransform root;
    private GameObject scan, controls, howTo, toast;
    private TextMeshProUGUI scanTitle, scanBody, toastText, countText;
    private CanvasGroup addGroup, removeGroup;
    private bool hasFoundMarker;
    private System.Func<int> count;
    private int capacity = 5;

    private Button originalAdd, originalRemove, originalQuiz, originalBack;
    private TMP_Dropdown originalShapes;
    private GameObject shapeList;
    private TextMeshProUGUI shapeLabel;

    private Image captionIcon;
    private TextMeshProUGUI captionMark, stepsText;
    private GameObject stepsChip;
    private TextMeshProUGUI positionText;
    private int position;

    // Managers report problems (full, empty, gaps) through this; shown in red
    public static void Say(string message)
    {
        ARGuide guide = FindObjectOfType<ARGuide>();
        if (guide != null)
            guide.Narrate(message, -1, true);
    }

    // Managers describe what an operation just did, and how many steps it took
    public static void Explain(string message, int steps)
    {
        ARGuide guide = FindObjectOfType<ARGuide>();
        if (guide != null)
            guide.Narrate(message, steps, false);
    }

    private void Start()
    {
        FindOriginalControls();
        BuildUI();
        ShowSearching();

        targets = FindObjectsOfType<ImageTargetBehaviour>();
        foreach (ImageTargetBehaviour target in targets)
            target.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    private void OnDestroy()
    {
        if (targets == null)
            return;
        foreach (ImageTargetBehaviour target in targets)
        {
            if (target != null)
                target.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void Update()
    {
        if (count == null || countText == null)
            return;
        int n = count();
        countText.text = $"{n}/{capacity}";
        addGroup.alpha = n >= capacity ? 0.45f : 1f;
        removeGroup.alpha = n <= 0 ? 0.45f : 1f;
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Same rule as DefaultObserverEventHandler: tracked or extended-tracked counts as visible
        bool found = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
        if (!found)
        {
            ShowSearching();
            return;
        }

        scan.SetActive(false);
        controls.SetActive(true);
        if (!hasFoundMarker)
        {
            hasFoundMarker = true;
            howTo.SetActive(true);
        }
    }

    public void DismissHowTo() => howTo.SetActive(false);

    public void ShowMessage(string message) => Narrate(message, -1, true);

    // The caption stays up until the next action, so students can read it at their own pace
    public void Narrate(string message, int steps, bool error)
    {
        if (toast == null)
            return;
        toastText.text = message;
        captionIcon.color = error ? DK.Red : DK.Indigo700;
        captionMark.text = error ? "!" : "i";
        stepsChip.SetActive(steps >= 0);
        stepsText.text = steps == 1 ? "1 step" : $"{steps} steps";
        toast.SetActive(true);
    }

    private void ShowSearching()
    {
        scan.SetActive(true);
        controls.SetActive(false);
        howTo.SetActive(false);
        scanTitle.text = hasFoundMarker ? "Marker lost" : $"Point your camera at the {topicName} marker";
        scanBody.text = hasFoundMarker
            ? $"Aim back at the {topicName} card to keep going."
            : "Hold your phone about 30 cm from the printed card, in good light.";
    }

    // The scene's own buttons, recognised by the method they call
    private void FindOriginalControls()
    {
        foreach (Button button in FindObjectsOfType<Button>(true))
        {
            if (button.onClick.GetPersistentEventCount() == 0)
                continue;
            string method = button.onClick.GetPersistentMethodName(0);
            if (method.StartsWith("OnAdd") || method == "OnPushObjectButtonClicked" || method == "EnQueue") originalAdd = button;
            else if (method.StartsWith("OnRemove") || method == "OnPopObjectButtonClicked" || method == "DeQueue") originalRemove = button;
            else if (method == "GoBackToQuiz") originalQuiz = button;
            else if (method == "Exit") originalBack = button;
            else continue;
            button.gameObject.SetActive(false);
        }
        originalShapes = FindObjectOfType<TMP_Dropdown>(true);
        if (originalShapes != null)
            originalShapes.gameObject.SetActive(false);

        if (FindObjectOfType<ArrayManager>() is ArrayManager array) { count = () => array.Count; capacity = array.Capacity; }
        else if (FindObjectOfType<LinkedListManager>() is LinkedListManager list) { count = () => list.Count; capacity = list.Capacity; }
        else if (FindObjectOfType<StackManager>(true) is StackManager stack) { count = () => stack.Count; capacity = stack.Capacity; }
        else if (FindObjectOfType<QueueManager>() is QueueManager queue) { count = () => queue.Count; capacity = queue.Capacity; }
    }

    private void BuildUI()
    {
        Canvas canvas = UiKit.Create("ARGuideCanvas", transform).gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        RectTransform frame = UiKit.Create("Frame", canvas.transform);
        UiKit.Stretch(frame, Vector2.zero, Vector2.one);
        root = UiKit.Create("Screen", frame);
        DesignRoot design = root.gameObject.AddComponent<DesignRoot>();
        design.designSize = Landscape;
        design.fitHeight = true;
        design.Apply();

        BuildScan();
        BuildControls();
        BuildToast();
        BuildHowTo();

        // Back is always there, as the scene's exit button was
        Image back = DK.Box(root, DK.White, 14, "Back");
        DK.At(back, 20, 20, 52, 52);
        DK.MakeButton(back, () => originalBack?.onClick.Invoke());
        RectTransform arrow = DK.Icon(back.transform, "arrow-left", 22, DK.Indigo900);
        DK.Anchor(arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22, 22));
    }

    private void BuildScan()
    {
        RectTransform group = UiKit.Create("Scan", root);
        UiKit.Stretch(group, Vector2.zero, Vector2.one);
        scan = group.gameObject;

        // Corner brackets where the card should go
        RectTransform brackets = UiKit.Create("Brackets", group);
        DK.Anchor(brackets, new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(400, 190));
        foreach (Vector2 corner in new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0) })
        {
            Image h = DK.Box(brackets, DK.White, 2, "H");
            DK.Anchor(h, corner, Vector2.zero, new Vector2(36, 4));
            Image v = DK.Box(brackets, DK.White, 2, "V");
            DK.Anchor(v, corner, Vector2.zero, new Vector2(4, 36));
        }

        Image pill = DK.Box(group, DK.White, 14, "Prompt");
        DK.Anchor(pill, new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(0, 64));
        HorizontalLayoutGroup row = DK.Line(pill.gameObject, 12);
        row.padding = new RectOffset(16, 22, 10, 10);
        pill.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image iconTile = DK.Box(pill.transform, DK.Lavender100, 8, "IconTile");
        DK.Size(iconTile, 28, 28);
        RectTransform camera = DK.Icon(iconTile.transform, "camera", 16, DK.Indigo700);
        DK.Anchor(camera, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));

        RectTransform texts = UiKit.Create("Texts", pill.transform);
        DK.Column(texts.gameObject, 4).childForceExpandWidth = false;
        scanTitle = DK.Text(texts, "", 16, DK.Indigo900);
        scanTitle.enableWordWrapping = false;
        scanBody = DK.Text(texts, "", 13, DK.Indigo500);
        scanBody.enableWordWrapping = false;
    }

    private void BuildControls()
    {
        RectTransform group = UiKit.Create("Controls", root);
        UiKit.Stretch(group, Vector2.zero, Vector2.one);
        controls = group.gameObject;

        string[] labels = topicName == "Stack" ? new[] { "Push", "Pop" }
            : topicName == "Queue" ? new[] { "Enqueue", "Dequeue" } : new[] { "Add", "Remove" };
        string countLabel = topicName == "Array" ? "FILLED" : topicName == "Linked List" ? "NODES" : "SIZE";

        Image bar = DK.Box(group, DK.White, 14, "Actions");
        DK.At(bar, 82, 20, 0, 52);
        HorizontalLayoutGroup row = DK.Line(bar.gameObject, 4);
        row.padding = new RectOffset(4, 4, 4, 4);
        bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        Button add = DK.Pill(bar.transform, labels[0], DK.Indigo700, DK.White, 10, 16, () => originalAdd?.onClick.Invoke(), "plus");
        DK.Size(add, labels[0].Length * 10 + 64, 44);
        addGroup = DK.Get<CanvasGroup>(add);
        Button remove = DK.Pill(bar.transform, labels[1], DK.Clear, DK.Indigo900, 10, 16, () => originalRemove?.onClick.Invoke(), "minus");
        DK.Size(remove, labels[1].Length * 10 + 64, 44);
        removeGroup = DK.Get<CanvasGroup>(remove);

        Image divider = DK.Box(bar.transform, DK.Lavender300, 0, "Divider");
        DK.Size(divider, 1, 28);
        RectTransform counter = UiKit.Create("Count", bar.transform);
        DK.Column(counter.gameObject, 3, new RectOffset(4, 12, 6, 0)).childForceExpandWidth = false;
        DK.Caps(counter, countLabel, 10, DK.Lavender500);
        countText = DK.Text(counter, "0/5", 17, DK.Indigo900, true);
        countText.enableWordWrapping = false;

        if (topicName == "Array" || topicName == "Linked List")
            BuildPositionRow(group);

        if (originalShapes != null)
            BuildShapePicker(group);

        Button quiz = DK.Pill(group, "Go to Quiz", DK.Indigo700, DK.White, 14, 17, () => originalQuiz?.onClick.Invoke(), null, "arrow-right");
        DK.Anchor(quiz, new Vector2(1, 0), new Vector2(-20, 20), new Vector2(190, 56));
        controls.SetActive(false);
    }

    private int Shape => originalShapes != null ? originalShapes.value : 0;

    // Array: pick an index, then insert or delete there. Linked list: insert at the head, or delete node N.
    private void BuildPositionRow(RectTransform group)
    {
        bool array = topicName == "Array";
        Image bar = DK.Box(group, DK.White, 14, "Position");
        DK.At(bar, 82, 80, 0, 52);
        HorizontalLayoutGroup row = DK.Line(bar.gameObject, 4);
        row.padding = new RectOffset(4, 4, 4, 4);
        bar.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (!array)
        {
            Button head = DK.Pill(bar.transform, "Insert at head", DK.Indigo700, DK.White, 10, 15,
                () => FindObjectOfType<LinkedListManager>()?.InsertAtHead(Shape), "plus");
            DK.Size(head, 168, 44);
            Image divider = DK.Box(bar.transform, DK.Lavender300, 0, "Divider");
            DK.Size(divider, 1, 28);
        }

        position = array ? 0 : 1;
        Button less = DK.Pill(bar.transform, "", DK.Lavender100, DK.Indigo900, 10, 15, () => Step(-1), "minus");
        DK.Size(less, 40, 44);
        RectTransform labelCell = UiKit.Create("Position", bar.transform);
        DK.Column(labelCell.gameObject, 2, new RectOffset(4, 4, 6, 0), TextAnchor.UpperCenter).childForceExpandWidth = false;
        DK.Caps(labelCell, array ? "INDEX" : "NODE", 10, DK.Lavender500, TextAlignmentOptions.Center);
        positionText = DK.Text(labelCell, "", 17, DK.Indigo900, true, TextAlignmentOptions.Center);
        DK.Size(positionText, 44, -1);
        Button more = DK.Pill(bar.transform, "", DK.Lavender100, DK.Indigo900, 10, 15, () => Step(1), "plus");
        DK.Size(more, 40, 44);
        Step(0);

        if (array)
        {
            Button insert = DK.Pill(bar.transform, "Insert at", DK.Indigo700, DK.White, 10, 15,
                () => FindObjectOfType<ArrayManager>()?.InsertAt(position, Shape));
            DK.Size(insert, 104, 44);
        }
        Button delete = DK.Pill(bar.transform, array ? "Delete at" : "Delete", DK.Clear, DK.Indigo900, 10, 15,
            () =>
            {
                if (array) FindObjectOfType<ArrayManager>()?.DeleteAt(position);
                else FindObjectOfType<LinkedListManager>()?.DeleteAt(position - 1);
            }, "minus");
        DK.Size(delete, array ? 116 : 100, 44);
    }

    private void Step(int delta)
    {
        int min = topicName == "Array" ? 0 : 1;
        int max = min + capacity - 1;
        position = Mathf.Clamp(position + delta, min, max);
        positionText.text = position.ToString();
    }

    // For the screenshot tool
    public void SetPosition(int value)
    {
        position = value;
        Step(0);
    }

    private void BuildShapePicker(RectTransform group)
    {
        Image button = DK.Box(group, DK.White, 14, "Shape");
        DK.Anchor(button, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(184, 52));
        DK.MakeButton(button, () => shapeList.SetActive(!shapeList.activeSelf));
        TextMeshProUGUI caps = DK.Caps(button.transform, "SHAPE", 10, DK.Lavender500);
        DK.Anchor(caps, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(50, 12));
        shapeLabel = DK.Text(button.transform, "", 16, DK.Indigo900, false, TextAlignmentOptions.MidlineLeft);
        DK.Anchor(shapeLabel, new Vector2(0, 0.5f), new Vector2(72, 0), new Vector2(80, 30));
        RectTransform chevron = DK.Icon(button.transform, "chevron-down", 18, DK.Indigo900);
        DK.Anchor(chevron, new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(18, 18));

        Image list = DK.Box(group, DK.White, 14, "ShapeList");
        DK.Anchor(list, new Vector2(1, 1), new Vector2(-20, -78), new Vector2(184, 0));
        DK.Column(list.gameObject, 2, new RectOffset(6, 6, 6, 6));
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        shapeList = list.gameObject;
        List<TMP_Dropdown.OptionData> options = originalShapes.options;
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            Image option = DK.Box(list.transform, DK.Clear, 10, "Option");
            DK.Size(option, -1, 42);
            TextMeshProUGUI text = DK.Text(option.transform, options[i].text, 15, DK.Indigo900, false, TextAlignmentOptions.MidlineLeft);
            DK.Stretch(text.rectTransform, 12, 0, 12, 0);
            DK.MakeButton(option, () => PickShape(index));
        }
        shapeList.SetActive(false);
        PickShape(originalShapes.value);
    }

    private void PickShape(int index)
    {
        originalShapes.value = index; // Its listeners pick the prefab, as before
        shapeLabel.text = originalShapes.options[index].text;
        for (int i = 0; i < shapeList.transform.childCount; i++)
            shapeList.transform.GetChild(i).GetComponent<Image>().color = i == index ? DK.Lavender100 : DK.Clear;
        shapeList.SetActive(false);
    }

    private void BuildToast()
    {
        Image card = DK.Box(root, DK.White, 14, "Caption");
        DK.Anchor(card, new Vector2(0.5f, 0), new Vector2(0, 20), new Vector2(400, 0));
        HorizontalLayoutGroup row = DK.Line(card.gameObject, 10);
        row.padding = new RectOffset(12, 12, 10, 10);
        row.childAlignment = TextAnchor.MiddleLeft;
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        DK.Size(card, -1, -1).minHeight = 56;

        captionIcon = DK.Box(card.transform, DK.Indigo700, 13, "Icon");
        DK.Size(captionIcon, 26, 26);
        captionMark = DK.Text(captionIcon.transform, "i", 15, DK.White, true, TextAlignmentOptions.Center);
        UiKit.Stretch(captionMark.rectTransform, Vector2.zero, Vector2.one);

        toastText = DK.Text(card.transform, "", 14, DK.Indigo900, true);
        DK.Size(toastText, -1, -1, 1);

        Image chip = DK.Box(card.transform, DK.Lavender100, 8, "Steps");
        DK.Size(chip, -1, 26);
        HorizontalLayoutGroup pad = DK.Line(chip.gameObject, 0, TextAnchor.MiddleCenter);
        pad.padding = new RectOffset(8, 8, 5, 5);
        stepsText = DK.Text(chip.transform, "", 12, DK.Indigo700, true, TextAlignmentOptions.Center);
        stepsText.enableWordWrapping = false;
        stepsChip = chip.gameObject;

        toast = card.gameObject;
        toast.SetActive(false);
    }

    private void BuildHowTo()
    {
        Image dim = DK.Box(root, new Color(DK.Ink.r, DK.Ink.g, DK.Ink.b, 0.35f), 0, "HowTo");
        UiKit.Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.raycastTarget = true;
        howTo = dim.gameObject;

        Image card = DK.Box(dim.transform, DK.White, 20, "Card");
        DK.Anchor(card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 0));
        DK.Column(card.gameObject, 12, new RectOffset(22, 22, 22, 18));
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        DK.Caps(card.transform, "HOW TO USE", 11, DK.Lavender500);
        DK.Text(card.transform, controlsHint, 17, DK.Indigo900);
        RectTransform buttonRow = UiKit.Create("Buttons", card.transform);
        DK.Line(buttonRow.gameObject, 0, TextAnchor.MiddleRight);
        Button gotIt = DK.Pill(buttonRow, "Got it", DK.Indigo700, DK.White, 12, 15, () => howTo.SetActive(false));
        DK.Size(gotIt, 110, 44);
        howTo.SetActive(false);
    }
}
