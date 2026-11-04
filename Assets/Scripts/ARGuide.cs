using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

// The AR scene's on-screen layer, built to the "AR scenes" design: a scan prompt until the marker is found,
// a "How to use" card the first time, then the same layout for every topic: back and a compact mission
// card at the top left, the shape picker at the top right, one control dock at the bottom left with a
// short message above it, and Go to Quiz at the bottom right. The scene's original buttons are hidden.
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
    private LayoutElement toastWidth;
    private Coroutine hideToast;
    private ArrayManager arrayManager;

    // Managers report problems (full, empty, gaps) through this; shown in red
    public static void Say(string message)
    {
        ARGuide guide = FindObjectOfType<ARGuide>();
        if (guide != null)
            guide.Narrate(message, -1, true);
    }

    // Managers report each finished operation ("add", "insert", "pop", "enqueue-wrap"...) for the missions
    public static void Record(string op, int index = -1)
    {
        ARGuide guide = FindObjectOfType<ARGuide>();
        if (guide != null)
            guide.OnOperation(op, index);
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
        FollowEnd();
        // The array marks the slot Insert and Delete will use
        if (arrayManager != null)
            arrayManager.SelectSlot(atEnd ? Mathf.Min(n, capacity - 1) : position);
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
#if UNITY_EDITOR
        Debug.Log($"[Caption] {(error ? "ERROR" : steps + " step(s)")} | {message}");
#endif
        if (toast == null)
            return;
        toastText.text = message;
        toastWidth.preferredWidth = Mathf.Min(toastText.GetPreferredValues(message).x + 2, 440);
        captionIcon.color = error ? DK.Red : DK.Indigo700;
        captionMark.text = error ? "!" : "i";
        stepsChip.SetActive(steps >= 0);
        stepsText.text = steps == 1 ? "1 step" : $"{steps} steps";
        toast.SetActive(true);
        if (hideToast != null)
            StopCoroutine(hideToast);
        hideToast = StartCoroutine(HideToast(steps >= 0 ? 6f : 4f));
    }

    private IEnumerator HideToast(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        toast.SetActive(false);
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

        if (FindObjectOfType<ArrayManager>() is ArrayManager array) { count = () => array.Count; capacity = array.Capacity; arrayManager = array; }
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
        DK.At(back, 16, 16, 48, 48);
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
            : topicName == "Queue" ? new[] { "Enqueue", "Dequeue" } : new[] { "Insert", "Delete" };
        string countLabel = topicName == "Array" ? "FILLED" : topicName == "Linked List" ? "NODES" : "SIZE";
        bool positional = topicName == "Array" || topicName == "Linked List";

        // One dock at the bottom left: [position stepper] [main action] [second action] | count
        Image dock = DK.Box(group, DK.White, 16, "Dock");
        DK.Anchor(dock, new Vector2(0, 0), new Vector2(16, 16), new Vector2(0, 56));
        HorizontalLayoutGroup row = DK.Line(dock.gameObject, 4);
        row.padding = new RectOffset(4, 4, 4, 4);
        dock.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (positional)
            BuildStepper(dock.transform);

        System.Action main = positional ? Insert : () => originalAdd?.onClick.Invoke();
        System.Action second = positional ? Delete : () => originalRemove?.onClick.Invoke();
        Button add = DK.Pill(dock.transform, labels[0], DK.Indigo700, DK.White, 12, 15, main, "plus");
        DK.Size(add, labels[0].Length * 9 + 60, 48);
        addGroup = DK.Get<CanvasGroup>(add);
        controlRefs[positional ? "insert" : "add"] = (RectTransform)add.transform;
        Button remove = DK.Pill(dock.transform, labels[1], DK.Clear, DK.Indigo900, 12, 15, second, "minus");
        DK.Size(remove, labels[1].Length * 9 + 56, 48);
        removeGroup = DK.Get<CanvasGroup>(remove);
        controlRefs[positional ? "delete" : "remove"] = (RectTransform)remove.transform;

        Image divider = DK.Box(dock.transform, DK.Lavender300, 0, "Divider");
        DK.Size(divider, 1, 28);
        RectTransform counter = UiKit.Create("Count", dock.transform);
        DK.Column(counter.gameObject, 3, new RectOffset(4, 12, 9, 0)).childForceExpandWidth = false;
        DK.Caps(counter, countLabel, 9, DK.Lavender500);
        countText = DK.Text(counter, "0/5", 16, DK.Indigo900, true);
        countText.enableWordWrapping = false;

        // Compact mission card at the top left, beside Back; before the shape picker so its list draws on top
        missions = gameObject.AddComponent<ARMissions>();
        missions.Build(group, topicName, count, controlRefs, () => originalQuiz?.onClick.Invoke());

        if (originalShapes != null)
            BuildShapePicker(group);

        Button quiz = DK.Pill(group, "Go to Quiz", DK.Indigo700, DK.White, 16, 16, () => originalQuiz?.onClick.Invoke(), null, "arrow-right");
        DK.Anchor(quiz, new Vector2(1, 0), new Vector2(-16, 16), new Vector2(160, 56));
        controls.SetActive(false);
    }

    private int Shape => originalShapes != null ? originalShapes.value : 0;

    // ---- Missions (ARMissions) ----

    private ARMissions missions;
    private readonly Dictionary<string, RectTransform> controlRefs = new Dictionary<string, RectTransform>();

    private void OnOperation(string op, int index) => missions?.OnOperation(op, index);

    // For the screenshot tool
    public string MissionStatus => missions != null ? missions.Status : "";
    public void NextMission() => missions?.OnNext();

    // ---- Position stepper (array index / linked-list node) ----
    // The position starts on "end" and follows it as items come and go, so Insert and Delete work at the
    // end until the student moves it: Insert then fills the next free slot and Delete empties the last one.

    private bool atEnd = true;
    private bool IsArray => topicName == "Array";
    private int MinPosition => 0;
    private int EndPosition => count != null ? count() : 0;

    // ‹ INDEX › (or NODE) on a light block
    private void BuildStepper(Transform dock)
    {
        Image block = DK.Box(dock, DK.Lavender100, 12, "Stepper");
        DK.Size(block, -1, 48);
        DK.Line(block.gameObject, 0);

        Image less = DK.Box(block.transform, DK.Clear, 12, "Less");
        DK.Size(less, 40, 48);
        DK.MakeButton(less, () => Step(-1));
        RectTransform left = DK.Icon(less.transform, "chevron-left", 16, DK.Indigo900);
        DK.Anchor(left, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
        controlRefs["index-minus"] = less.rectTransform;

        RectTransform labelCell = UiKit.Create("Position", block.transform);
        DK.Size(labelCell, 58, 48);
        DK.Column(labelCell.gameObject, 3, new RectOffset(0, 0, 9, 0), TextAnchor.UpperCenter).childForceExpandWidth = false;
        DK.Caps(labelCell, IsArray ? "INDEX" : "NODE", 9, DK.Lavender500, TextAlignmentOptions.Center);
        positionText = DK.Text(labelCell, "", 17, DK.Indigo900, true, TextAlignmentOptions.Center);
        DK.Size(positionText, 58, -1);

        Image more = DK.Box(block.transform, DK.Clear, 12, "More");
        DK.Size(more, 40, 48);
        DK.MakeButton(more, () => Step(1));
        RectTransform right = DK.Icon(more.transform, "chevron-right", 16, DK.Indigo900);
        DK.Anchor(right, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
        controlRefs["index-plus"] = more.rectTransform;

        position = EndPosition;
        ShowPosition();
    }

    private void Step(int delta)
    {
        position = Mathf.Clamp(position + delta, MinPosition, EndPosition);
        atEnd = position == EndPosition;
        ShowPosition();
    }

    // Keeps the position on "end" (or within range) as the structure grows and shrinks
    private void FollowEnd()
    {
        if (positionText == null)
            return;
        int end = EndPosition;
        int wanted = atEnd ? end : Mathf.Min(position, end);
        if (wanted != position)
        {
            position = wanted;
            atEnd = position == end;
            ShowPosition();
        }
    }

    private void ShowPosition()
    {
        positionText.text = position == EndPosition ? "end" : position.ToString();
    }

    private void Insert()
    {
        int shape = Shape;
        if (IsArray)
        {
            ArrayManager array = FindObjectOfType<ArrayManager>();
            if (atEnd) array.AddElement(shape);
            else array.InsertAt(position, shape);
        }
        else
        {
            LinkedListManager list = FindObjectOfType<LinkedListManager>();
            if (atEnd) list.AddNode(shape);
            else list.InsertAt(position, shape);
        }
    }

    private void Delete()
    {
        if (IsArray)
        {
            ArrayManager array = FindObjectOfType<ArrayManager>();
            if (atEnd) array.RemoveLastElement();
            else array.DeleteAt(position);
        }
        else
        {
            LinkedListManager list = FindObjectOfType<LinkedListManager>();
            if (atEnd) list.RemoveLastNode();
            else list.DeleteAt(position);
        }
    }

    // For the screenshot tool
    public void PressInsert() => Insert();
    public void PressDelete() => Delete();

    public void SetPosition(int value)
    {
        position = value;
        atEnd = position == EndPosition;
        ShowPosition();
    }

    private void BuildShapePicker(RectTransform group)
    {
        Image button = DK.Box(group, DK.White, 14, "Shape");
        DK.Anchor(button, new Vector2(1, 1), new Vector2(-16, -16), new Vector2(168, 48));
        DK.MakeButton(button, () => shapeList.SetActive(!shapeList.activeSelf));
        TextMeshProUGUI caps = DK.Caps(button.transform, "SHAPE", 10, DK.Lavender500);
        DK.Anchor(caps, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(50, 12));
        shapeLabel = DK.Text(button.transform, "", 16, DK.Indigo900, false, TextAlignmentOptions.MidlineLeft);
        DK.Anchor(shapeLabel, new Vector2(0, 0.5f), new Vector2(66, 0), new Vector2(76, 30));
        RectTransform chevron = DK.Icon(button.transform, "chevron-down", 18, DK.Indigo900);
        DK.Anchor(chevron, new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(16, 16));

        Image list = DK.Box(group, DK.White, 14, "ShapeList");
        DK.Anchor(list, new Vector2(1, 1), new Vector2(-16, -70), new Vector2(168, 0));
        DK.Column(list.gameObject, 2, new RectOffset(6, 6, 6, 6));
        list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        shapeList = list.gameObject;
        List<TMP_Dropdown.OptionData> options = originalShapes.options;
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            Image option = DK.Box(list.transform, DK.Clear, 10, "Option");
            DK.Size(option, -1, 40);
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
        Image card = DK.Box(root, DK.White, 12, "Toast");
        DK.Anchor(card, new Vector2(0, 0), new Vector2(16, 84), new Vector2(0, 0));
        HorizontalLayoutGroup row = DK.Line(card.gameObject, 10);
        row.padding = new RectOffset(10, 12, 9, 9);
        row.childAlignment = TextAnchor.MiddleLeft;
        ContentSizeFitter fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        captionIcon = DK.Box(card.transform, DK.Indigo700, 11, "Icon");
        DK.Size(captionIcon, 22, 22);
        captionMark = DK.Text(captionIcon.transform, "i", 13, DK.White, true, TextAlignmentOptions.Center);
        UiKit.Stretch(captionMark.rectTransform, Vector2.zero, Vector2.one);

        toastText = DK.Text(card.transform, "", 14, DK.Indigo900, true);
        toastWidth = DK.Size(toastText, 100, -1);

        Image chip = DK.Box(card.transform, DK.Lavender100, 8, "Steps");
        DK.Size(chip, -1, 24);
        HorizontalLayoutGroup pad = DK.Line(chip.gameObject, 0, TextAnchor.MiddleCenter);
        pad.padding = new RectOffset(8, 8, 4, 4);
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
        DK.Text(card.transform, "After each action, a short message above the controls explains what happened and counts its steps. The missions are at the top left; the controls to use glow orange.", 14, DK.Indigo500);
        RectTransform buttonRow = UiKit.Create("Buttons", card.transform);
        DK.Line(buttonRow.gameObject, 0, TextAnchor.MiddleRight);
        Button gotIt = DK.Pill(buttonRow, "Got it", DK.Indigo700, DK.White, 12, 15, () => howTo.SetActive(false));
        DK.Size(gotIt, 110, 44);
        howTo.SetActive(false);
    }
}
