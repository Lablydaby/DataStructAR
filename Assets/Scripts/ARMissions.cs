using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Three short missions per AR scene, in a card on the right. Each mission says what to do and how
// (and highlights the controls to use), shows progress for count goals, and on completion explains
// why it matters before the student moves on with Next.
public class ARMissions : MonoBehaviour
{
    private class Mission
    {
        public string goal, how, why;
        public string[] targets;                // Controls to highlight ("add", "insert", "index"...)
        public Func<string, int, bool> done;    // (operation, index) -> done?
        public Func<(int current, int total)> progress; // Optional, for count goals
    }

    private readonly List<Mission> missions = new List<Mission>();
    private int index;
    private bool completed; // Current mission done, waiting for Next
    private bool collapsed;

    private string topic;
    private Func<int> count;
    private Dictionary<string, RectTransform> controls;
    private Action goToQuiz;

    private Image card, badge;
    private RectTransform check, body, progressRow, progressFill, chevronUp, chevronDown;
    private TextMeshProUGUI badgeNumber, title, goal, how, progressText;
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
        DK.Anchor(card, new Vector2(1, 1), new Vector2(-20, -84), new Vector2(270, 0));
        DK.Column(card.gameObject, 10, new RectOffset(14, 14, 12, 14));
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Header: badge, "MISSION 2 OF 3", progress bars, collapse
        RectTransform header = UiKit.Create("Header", card.transform);
        DK.Line(header.gameObject, 8);
        badge = DK.Box(header, DK.Indigo700, 12, "Badge");
        DK.Size(badge, 24, 24);
        badgeNumber = DK.Text(badge.transform, "", 12, DK.White, true, TextAlignmentOptions.Center);
        UiKit.Stretch(badgeNumber.rectTransform, Vector2.zero, Vector2.one);
        check = DK.Icon(badge.transform, "check", 13, DK.White);
        DK.Anchor(check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(13, 13));
        title = DK.Caps(header, "", 10, DK.Lavender500);
        DK.Size(title, -1, -1, 1);

        RectTransform barRow = UiKit.Create("Bars", header);
        DK.Line(barRow.gameObject, 4);
        for (int i = 0; i < missions.Count; i++)
        {
            Image bar = DK.Box(barRow, DK.Lavender300, 2.5f, "Bar");
            DK.Size(bar, 16, 5);
            bars.Add(bar);
        }

        Image toggle = DK.Box(header, DK.Clear, 8, "Collapse");
        DK.Size(toggle, 26, 26);
        DK.MakeButton(toggle, () => { collapsed = !collapsed; Refresh(); });
        chevronUp = DK.Icon(toggle.transform, "chevron-up", 16, DK.Indigo900);
        chevronDown = DK.Icon(toggle.transform, "chevron-down", 16, DK.Indigo900);
        DK.Anchor(chevronUp, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));
        DK.Anchor(chevronDown, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16, 16));

        // Body: goal, how-to, progress, Next
        body = UiKit.Create("Body", card.transform);
        DK.Column(body.gameObject, 8);
        goal = DK.Text(body, "", 15, DK.Indigo900, true);
        how = DK.Text(body, "", 13, DK.Indigo500);

        progressRow = UiKit.Create("Progress", body);
        DK.Line(progressRow.gameObject, 8);
        Image track = DK.Box(progressRow, DK.Lavender100, 3, "Track");
        DK.Size(track, -1, 6, 1);
        Image fill = DK.Box(track.transform, DK.Indigo700, 3, "Fill");
        progressFill = fill.rectTransform;
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = new Vector2(0, 1);
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        progressText = DK.Text(progressRow, "", 12, DK.Indigo700, true);
        progressText.enableWordWrapping = false;

        next = DK.Pill(body, "Next mission", DK.Indigo700, DK.White, 10, 14, OnNext, null, "arrow-right");
        DK.Size(next, -1, 40);

        Refresh();
    }

    private void Define()
    {
        int Items() => count != null ? count() : 0;
        void Add(string goalText, string howText, string whyText, string[] targets, Func<string, int, bool> done,
            Func<(int, int)> progress = null)
        {
            missions.Add(new Mission { goal = goalText, how = howText, why = whyText, targets = targets, done = done, progress = progress });
        }

        switch (topic)
        {
            case "Array":
                Add("Fill the array up to index 2.",
                    "Tap <b>Add</b> until indexes 0, 1 and 2 are filled.",
                    "Adding at the end never moves anything else, so each Add takes 1 step.",
                    new[] { "add" }, (op, i) => Items() >= 3, () => (Mathf.Min(Items(), 3), 3));
                Add("Insert a value at index 0.",
                    "Set <b>INDEX</b> to 0, then tap <b>Insert at</b>. Watch the step count.",
                    "Every element moved one slot right to make room, so inserting at the front takes n steps: O(n).",
                    new[] { "index", "insert" }, (op, i) => op == "insert" && i == 0);
                Add("Delete the element at index 1.",
                    "Set <b>INDEX</b> to 1, then tap <b>Delete at</b>.",
                    "The elements after index 1 shifted left to close the gap, so deleting from the middle is O(n) too.",
                    new[] { "index", "delete" }, (op, i) => op == "delete" && i == 1);
                break;
            case "Linked List":
                Add("Build a list of 3 nodes.",
                    "Tap <b>Add</b> until there are 3 nodes. Watch the step count grow with each one.",
                    "Each Add walks every link from the head to reach the end, so it gets slower as the list grows: O(n).",
                    new[] { "add" }, (op, i) => op == "add" && Items() >= 3, () => (Mathf.Min(Items(), 3), 3));
                Add("Insert a node at the head.",
                    "Tap <b>Insert at head</b>.",
                    "Only one pointer changed: the new node points to the old head. That's why inserting at the head is O(1).",
                    new[] { "head" }, (op, i) => op == "insert-head");
                Add("Delete node 2.",
                    "Set <b>NODE</b> to 2, then tap <b>Delete</b>. Count the links walked.",
                    "The list walked to the node before it and pointed that node past node 2. Nothing else moved.",
                    new[] { "index", "delete" }, (op, i) => op == "delete" && i == 1);
                break;
            case "Stack":
                Add("Push until the stack is full.",
                    "Tap <b>Push</b> until SIZE reads 5/5.",
                    "Each push lands on top in 1 step; nothing below it moves.",
                    new[] { "add" }, (op, i) => op == "push" && Items() >= 5, () => (Items(), 5));
                Add("Push once more.",
                    "Tap <b>Push</b> again while the stack is full.",
                    "That's a stack overflow: there's no room left on top, so the push is refused.",
                    new[] { "add" }, (op, i) => op == "overflow");
                Add("Pop down to one block.",
                    "Tap <b>Pop</b> until one block is left. Watch which values come off first.",
                    "The last block pushed came off first, and the first one in is still at the bottom: Last In, First Out.",
                    new[] { "remove" }, (op, i) => op == "pop" && Items() == 1, () => (Mathf.Clamp(5 - Items(), 0, 4), 4));
                break;
            case "Queue":
                Add("Enqueue 3 shapes.",
                    "Tap <b>Enqueue</b> three times. Watch FRONT stay on the first one.",
                    "New items join at the rear; the front doesn't change.",
                    new[] { "add" }, (op, i) => op.StartsWith("enqueue") && Items() >= 3, () => (Mathf.Min(Items(), 3), 3));
                Add("Dequeue once.",
                    "Tap <b>Dequeue</b>.",
                    "The first shape in was the first one out: First In, First Out. Only FRONT moved.",
                    new[] { "remove" }, (op, i) => op == "dequeue");
                Add("Make the rear wrap around.",
                    "Tap <b>Enqueue</b> until REAR jumps back to slot 0.",
                    "The slot freed at the front is reused. That's a circular queue, and every step is still O(1).",
                    new[] { "add" }, (op, i) => op == "enqueue-wrap");
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
        if (index >= missions.Count)
        {
            goToQuiz?.Invoke();
            return;
        }
        completed = false;
        index++;
        // The new mission may already be met (e.g. the list is already long enough)
        if (index < missions.Count && missions[index].done("", -1))
            completed = true;
        Refresh();
    }

    private void Update()
    {
        if (card == null)
            return;
        // Keep count-goal progress live
        if (!completed && index < missions.Count && missions[index].progress != null)
            ShowProgress(missions[index].progress());

        // Gently pulse the highlighted controls
        float a = 0.35f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
        foreach (Image ring in highlights)
            ring.color = new Color(DK.Ochre.r, DK.Ochre.g, DK.Ochre.b, a);
    }

    private void ShowProgress((int current, int total) p)
    {
        progressFill.anchorMax = new Vector2(p.total > 0 ? (float)p.current / p.total : 0, 1);
        progressText.text = $"{p.current}/{p.total}";
    }

    private void Refresh()
    {
        bool allDone = index >= missions.Count;
        bool green = allDone || completed;

        card.color = green ? DK.GreenTint : DK.White;
        badge.color = green ? DK.Green : DK.Indigo700;
        check.gameObject.SetActive(green);
        badgeNumber.text = green ? "" : (index + 1).ToString();
        title.color = green ? DK.Green : DK.Lavender500;
        // Kept short so the header fits beside the progress bars
        title.text = allDone ? "ALL DONE"
            : completed ? $"MISSION {index + 1} DONE"
            : $"MISSION {index + 1} OF {missions.Count}";
        for (int i = 0; i < bars.Count; i++)
            bars[i].color = i < index || (i == index && completed) || allDone ? DK.Green
                : i == index ? DK.Indigo700 : DK.Lavender300;

        body.gameObject.SetActive(!collapsed);
        chevronUp.gameObject.SetActive(!collapsed);
        chevronDown.gameObject.SetActive(collapsed);

        if (allDone)
        {
            goal.text = "Nice work!";
            how.text = $"You've tried the key ideas of the {topic.ToLowerInvariant()}. See how they come up in the quiz.";
            how.color = DK.Indigo900;
            progressRow.gameObject.SetActive(false);
            next.gameObject.SetActive(true);
            DK.PillLabel(next).text = "Go to Quiz";
        }
        else if (completed)
        {
            Mission m = missions[index];
            goal.text = m.goal;
            how.text = $"<b>Why it matters:</b> {m.why}";
            how.color = DK.Indigo900;
            progressRow.gameObject.SetActive(false);
            next.gameObject.SetActive(true);
            DK.PillLabel(next).text = index == missions.Count - 1 ? "Finish" : "Next mission";
        }
        else
        {
            Mission m = missions[index];
            goal.text = m.goal;
            how.text = m.how;
            how.color = DK.Indigo500;
            progressRow.gameObject.SetActive(m.progress != null);
            if (m.progress != null)
                ShowProgress(m.progress());
            next.gameObject.SetActive(false);
        }

        Highlight(!allDone && !completed ? missions[index].targets : null);
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
            foreach (KeyValuePair<string, RectTransform> control in controls)
            {
                // "index" covers both stepper buttons
                if (control.Key != target && !(target == "index" && control.Key.StartsWith("index")))
                    continue;
                Image ring = DK.Ring(control.Value, DK.Ochre, 14, "Highlight");
                DK.Stretch(ring.rectTransform, -5, -5, -5, -5);
                ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                highlights.Add(ring);
            }
        }
    }
}
