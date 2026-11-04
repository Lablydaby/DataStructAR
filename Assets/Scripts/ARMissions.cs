using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Three short missions per AR scene in a compact card at the top left (the "AR scenes" design). The header
// shows which mission and the progress, and collapses the card; the body shows the goal, and once it's done,
// why it matters and a button to move on. The controls a mission needs get a pulsing outline.
public class ARMissions : MonoBehaviour
{
    private class Mission
    {
        public string goal, why;
        public string[] targets;               // Controls to highlight ("insert", "index-minus", "add"...)
        public Func<string, int, bool> done;   // (operation, index) -> done?
    }

    private readonly List<Mission> missions = new List<Mission>();
    private int index;
    private bool completed; // Current mission done, waiting for Next
    private bool collapsed;
    private int dequeues;   // Counted per mission, for "Dequeue 2 shapes"

    private string topic;
    private Func<int> count;
    private Dictionary<string, RectTransform> controls;
    private Action goToQuiz;

    private Image card, badge;
    private RectTransform check, body, chevronUp, chevronDown;
    private TextMeshProUGUI badgeNumber, title, goal, why;
    private GameObject whyBox;
    private Button next;
    private readonly List<Image> bars = new List<Image>();
    private readonly List<Image> highlights = new List<Image>();

    public string Status => index >= missions.Count ? "all done"
        : $"{index + 1}/{missions.Count}{(completed ? " complete" : "")}: {missions[index].goal}";

    public void Build(RectTransform parent, string topicName, Func<int> itemCount,
        Dictionary<string, RectTransform> controlsByName, Action quiz)
    {
        topic = topicName;
        count = itemCount;
        controls = controlsByName;
        goToQuiz = quiz;
        Define();
        if (missions.Count == 0)
            return;

        card = DK.Box(parent, DK.White, 16, "Missions");
        DK.Anchor(card, new Vector2(0, 1), new Vector2(74, -16), new Vector2(290, 0));
        DK.Column(card.gameObject, 0);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header: badge, "MISSION 1 OF 3", progress bars, chevron; tapping it collapses the card
        Image header = DK.Box(card.transform, DK.Clear, 16, "Header");
        DK.Size(header, -1, 48);
        HorizontalLayoutGroup row = DK.Line(header.gameObject, 10);
        row.padding = new RectOffset(14, 10, 0, 0);
        DK.MakeButton(header, () => { collapsed = !collapsed; Refresh(); });

        badge = DK.Box(header.transform, DK.Indigo700, 12, "Badge");
        DK.Size(badge, 24, 24);
        badgeNumber = DK.Text(badge.transform, "", 12, DK.White, true, TextAlignmentOptions.Center);
        UiKit.Stretch(badgeNumber.rectTransform, Vector2.zero, Vector2.one);
        check = DK.Icon(badge.transform, "check", 13, DK.White);
        DK.Anchor(check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13, 13));

        title = DK.Caps(header.transform, "", 11, DK.Lavender500);
        DK.Size(title, -1, -1, 1);

        RectTransform barRow = UiKit.Create("Bars", header.transform);
        DK.Line(barRow.gameObject, 3);
        for (int i = 0; i < missions.Count; i++)
        {
            Image bar = DK.Box(barRow, DK.Lavender300, 2, "Bar");
            DK.Size(bar, 16, 4);
            bars.Add(bar);
        }

        RectTransform chevronCell = UiKit.Create("Chevron", header.transform);
        DK.Size(chevronCell, 18, 18);
        chevronUp = DK.Icon(chevronCell, "chevron-up", 18, DK.Indigo900);
        chevronDown = DK.Icon(chevronCell, "chevron-down", 18, DK.Indigo900);
        DK.Anchor(chevronUp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
        DK.Anchor(chevronDown, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));

        // Body: the goal; once done, why it matters and the button to move on
        body = UiKit.Create("Body", card.transform);
        DK.Column(body.gameObject, 8, new RectOffset(14, 14, 0, 14));
        goal = DK.Text(body, "", 15, DK.Indigo900, true);

        Image box = DK.Box(body, DK.Lavender100, 10, "Why");
        DK.Column(box.gameObject, 0, new RectOffset(12, 12, 10, 10));
        why = DK.Text(box.transform, "", 13, DK.Ink);
        whyBox = box.gameObject;

        next = DK.Pill(body, "Next mission", DK.Indigo700, DK.White, 10, 14, OnNext, null, "arrow-right");
        DK.Size(next, -1, 40);

        Refresh();
    }

    private void Define()
    {
        int Items() => count != null ? count() : 0;
        void Add(string goalText, string whyText, string[] targets, Func<string, int, bool> done) =>
            missions.Add(new Mission { goal = goalText, why = whyText, targets = targets, done = done });

        switch (topic)
        {
            case "Array":
                Add("Fill the array up to index 2.",
                    "Inserting at the end never moves anything else, so each one takes 1 step.",
                    new[] { "insert" }, (op, i) => Items() >= 3);
                Add("Insert a value at index 1.",
                    "Every element from index 1 onward shifted one place right to make room, so inserting in the middle is O(n).",
                    new[] { "index-minus", "insert" }, (op, i) => op == "insert" && i == 1);
                Add("Delete the element at index 0.",
                    "The elements after it shifted left to close the gap, so deleting from the front is O(n).",
                    new[] { "index-minus", "delete" }, (op, i) => op == "delete" && i == 0);
                break;
            case "Linked List":
                Add("Build a list of 3 nodes.",
                    "Each new node joins by changing one pointer. Nothing else moves.",
                    new[] { "insert" }, (op, i) => (op == "add" || op == "insert" || op == "insert-head") && Items() >= 3);
                Add("Insert a node at position 1.",
                    "Only the pointers around the new node changed. No other node moved, unlike an array.",
                    new[] { "index-minus", "insert" }, (op, i) => op == "insert" && i == 1);
                Add("Delete the head node.",
                    "Head now points to the second node. Deleting the head is O(1).",
                    new[] { "index-minus", "delete" }, (op, i) => op == "delete" && i == 0 && Items() >= 1);
                break;
            case "Stack":
                Add("Push 3 shapes.",
                    "Each push lands on the top, so it takes 1 step: O(1).",
                    new[] { "add" }, (op, i) => op == "push" && Items() >= 3);
                Add("Pop until 1 shape is left.",
                    "Pop always takes the top item: last in, first out.",
                    new[] { "remove" }, (op, i) => op == "pop" && Items() == 1);
                Add("Pop the empty stack.",
                    "Popping an empty stack is a stack underflow.",
                    new[] { "remove" }, (op, i) => op == "underflow");
                break;
            case "Queue":
                Add("Enqueue 3 shapes.",
                    "New items join at the rear; the front doesn't change.",
                    new[] { "add" }, (op, i) => op.StartsWith("enqueue") && Items() >= 3);
                Add("Dequeue 2 shapes.",
                    "Items leave from the front: first in, first out.",
                    new[] { "remove" }, (op, i) => op == "dequeue" && ++dequeues >= 2);
                Add("Enqueue into a full queue.",
                    "Adding to a full queue is an overflow.",
                    new[] { "add" }, (op, i) => op == "overflow");
                break;
        }
    }

    public void OnOperation(string op, int at)
    {
        if (card == null || completed || index >= missions.Count)
            return;
        if (!missions[index].done(op, at))
            return;
        completed = true;
        collapsed = false; // Make sure the takeaway is seen
        Refresh();
    }

    public void OnNext()
    {
        if (!completed)
            return;
        if (index >= missions.Count - 1)
        {
            goToQuiz?.Invoke();
            return;
        }
        completed = false;
        dequeues = 0;
        index++;
        // The new mission may already be met (e.g. the structure is already big enough)
        if (missions[index].done("", -1))
            completed = true;
        Refresh();
    }

    private void Update()
    {
        // Gently pulse the highlighted controls
        float a = 0.35f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
        foreach (Image ring in highlights)
            ring.color = new Color(DK.Ochre.r, DK.Ochre.g, DK.Ochre.b, a);
    }

    private void Refresh()
    {
        Mission m = missions[Mathf.Min(index, missions.Count - 1)];

        badge.color = completed ? DK.Green : DK.Indigo700;
        check.gameObject.SetActive(completed);
        badgeNumber.text = completed ? "" : (index + 1).ToString();
        title.color = completed ? DK.Green : DK.Lavender500;
        title.text = completed ? $"MISSION {index + 1} DONE" : $"MISSION {index + 1} OF {missions.Count}";
        for (int i = 0; i < bars.Count; i++)
            bars[i].color = i < index || (i == index && completed) ? DK.Green
                : i == index ? DK.Indigo700 : DK.Lavender300;

        body.gameObject.SetActive(!collapsed);
        chevronUp.gameObject.SetActive(!collapsed);
        chevronDown.gameObject.SetActive(collapsed);

        goal.text = m.goal;
        why.text = m.why;
        whyBox.SetActive(completed);
        next.gameObject.SetActive(completed);
        DK.PillLabel(next).text = index >= missions.Count - 1 ? "Go to Quiz" : "Next mission";

        Highlight(completed ? null : m.targets);
    }

    // Outlines the controls the current mission needs
    private void Highlight(string[] targets)
    {
        foreach (Image ring in highlights)
            Destroy(ring.gameObject);
        highlights.Clear();
        if (targets == null)
            return;
        foreach (string target in targets)
        {
            if (!controls.TryGetValue(target, out RectTransform control))
                continue;
            Image ring = DK.Ring(control, DK.Ochre, 14, "Highlight");
            DK.Stretch(ring.rectTransform, -5, -5, -5, -5);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            highlights.Add(ring);
        }
    }
}
