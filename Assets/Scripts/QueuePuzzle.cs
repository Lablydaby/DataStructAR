using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Queue practice: the queue starts as 5, 6, 3, 2, 1 from front to rear. Dequeue from the front and
// enqueue at the rear until it reads 1, 2, 3, 4, 5.
public class QueuePuzzle : PuzzleScreen
{
    private static readonly string[] StartFrontToRear = { "5", "6", "3", "2", "1" };
    private static readonly string[] Solved1To5 = { "1", "2", "3", "4", "5" };
    private const int Capacity = 5;
    private const string Guide = "Oh no! The queue is not arranged! Can you fix it by following the First-In, First-Out (FIFO) rule?";

    protected override string Topic => "Queue";
    protected override string Instructions => Guide;

    private readonly List<Slot> slots = new List<Slot>();
    private readonly List<string> items = new List<string>(); // Front first
    private TMP_InputField input;
    private Button enqueue;

    protected override void Build()
    {
        RectTransform row = UiKit.Create("Queue", stage);
        DK.Anchor(row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 100));
        DK.Line(row.gameObject, 8, TextAnchor.MiddleCenter);
        for (int i = 0; i < Capacity; i++)
        {
            RectTransform cell = UiKit.Create("Cell", row);
            DK.Column(cell.gameObject, 10, null, TextAnchor.UpperCenter).childForceExpandWidth = false;
            slots.Add(MakeSlot(cell, new Vector2(76, 64), 12));
            TextMeshProUGUI tag = SmallCaps(cell, i == 0 ? "FRONT" : i == Capacity - 1 ? "REAR" : "", DK.Lavender500);
            DK.Size(tag, 76, 12);
        }

        RectTransform bar = ControlRow(12);
        Image group = DK.Box(bar, DK.Lavender50, 12, "EnqueueGroup");
        DK.Size(group, 230, 52);
        HorizontalLayoutGroup inner = DK.Line(group.gameObject, 4);
        inner.padding = new RectOffset(4, 4, 4, 4);
        input = Input(group.transform, "Value", 96, 44, DK.Clear, 2);
        enqueue = DK.Pill(group.transform, "Enqueue", DK.Indigo700, DK.White, 9, 15, Enqueue, "plus", null, null, true);
        DK.Size(enqueue, 122, 44);
        input.onValueChanged.AddListener(_ => UpdateEnqueue());

        OutlineButton(bar, "Dequeue", 130, 52, Dequeue, "minus", 15);
        OutlineButton(bar, "", 52, 52, () => { ResetPuzzle(); Message(DefaultMessage, DK.Indigo500); }, "reset");
    }

    protected override void ResetPuzzle()
    {
        items.Clear();
        items.AddRange(StartFrontToRear);
        input.text = "";
        Refresh();
        UpdateEnqueue();
    }

    private void Refresh()
    {
        for (int i = 0; i < Capacity; i++)
            slots[i].Set(i < items.Count ? items[i] : "");
    }

    private void UpdateEnqueue()
    {
        bool ready = !Solved && input.text.Trim().Length > 0;
        CanvasGroup group = DK.Get<CanvasGroup>(enqueue);
        group.alpha = ready ? 1f : 0.45f;
        enqueue.interactable = ready;
    }

    private void Enqueue()
    {
        string value = input.text.Trim();
        if (Solved || value.Length == 0)
            return;
        input.text = "";
        if (items.Count >= Capacity)
        {
            Error("Queue Overflow! The queue holds 5 values. Dequeue first.");
            return;
        }
        items.Add(value);
        Refresh();
        Message("", DK.Indigo500);
        if (items.Count < Capacity)
            return;
        if (string.Join(",", items) == string.Join(",", Solved1To5))
            Success("Great job! The queue is perfectly arranged from 1 to 5 in FIFO order!");
        else
            Error("Queue is full but the order is wrong! Try using Dequeue to fix it using FIFO.");
    }

    private void Dequeue()
    {
        if (Solved)
            return;
        if (items.Count == 0)
        {
            Error("Queue is empty! " + Guide);
            return;
        }
        items.RemoveAt(0);
        Refresh();
        Message("", DK.Indigo500);
    }

    // For the screenshot tool
    public void Press(string key)
    {
        if (key == "dequeue") Dequeue();
        else
        {
            input.text = key;
            Enqueue();
        }
    }
}
