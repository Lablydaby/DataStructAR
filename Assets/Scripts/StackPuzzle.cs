using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Stack practice: the stack starts as Z, W, Y, X from top to bottom. Pop everything, then push the
// blocks back so it reads W, X, Y, Z from top to bottom.
public class StackPuzzle : PuzzleScreen
{
    private static readonly string[] StartBottomUp = { "X", "Y", "W", "Z" };
    private static readonly string[] SolvedBottomUp = { "Z", "Y", "X", "W" };
    private const int Capacity = 4;

    protected override string Topic => "Stack";
    protected override string Instructions => StackQuiz.StackInstructions;

    private readonly List<Slot> slots = new List<Slot>(); // Top slot first
    private readonly List<GameObject> topTags = new List<GameObject>();
    private readonly List<string> items = new List<string>(); // Bottom first

    protected override void Build()
    {
        RectTransform column = UiKit.Create("Stack", stage);
        DK.Anchor(column, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(232, 180));
        DK.Column(column.gameObject, 6, null, TextAnchor.MiddleCenter).childForceExpandWidth = false;
        for (int i = 0; i < Capacity; i++)
        {
            Slot slot = MakeSlot(column, new Vector2(200, 36), 10);
            slots.Add(slot);
            TextMeshProUGUI tag = DK.Caps(slot.outer.transform.parent, "← TOP", 11, DK.Lavender500);
            DK.Anchor(tag, new Vector2(1, 0.5f), new Vector2(80, 0), new Vector2(70, 14));
            topTags.Add(tag.gameObject);
        }
        Image floor = DK.Box(column, DK.Lavender300, 2, "Floor");
        DK.Size(floor, 232, 4);

        RectTransform bar = ControlRow(10);
        TextMeshProUGUI push = DK.Caps(bar, "PUSH", 11, DK.Lavender300);
        DK.Size(push, 44, 14);
        foreach (string value in new[] { "W", "X", "Y", "Z" })
        {
            string v = value;
            OutlineButton(bar, v, 52, 48, () => Push(v), null, 20);
        }
        Image divider = DK.Box(bar, new Color(0.97f, 0.98f, 1f, 0.3f), 0, "Divider");
        DK.Size(divider, 1, 32);
        LightButton(bar, "Pop", 96, 48, Pop, "minus");
        OutlineButton(bar, "", 48, 48, () => { ResetPuzzle(); Message("", DK.Indigo500); }, "reset");
    }

    protected override void ResetPuzzle()
    {
        items.Clear();
        items.AddRange(StartBottomUp);
        Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < Capacity; i++)
        {
            int level = Capacity - 1 - i; // slots[0] is the top position
            string value = level < items.Count ? items[level] : "";
            slots[i].Set(value, 20);
            topTags[i].SetActive(items.Count > 0 && level == items.Count - 1);
        }
    }

    private void Push(string value)
    {
        if (Solved)
            return;
        if (items.Count >= Capacity)
        {
            Error("Stack Overflow!");
            return;
        }
        items.Add(value);
        Refresh();
        Message("", DK.Indigo500);
        if (items.Count < Capacity)
            return;
        if (string.Join("", items) == string.Join("", SolvedBottomUp))
            Success("Great job! Stack is in correct LIFO order!");
        else
            Error("Stack order incorrect!");
    }

    private void Pop()
    {
        if (Solved)
            return;
        if (items.Count == 0)
        {
            Error("Stack Underflow! There's nothing to pop.");
            return;
        }
        items.RemoveAt(items.Count - 1);
        Refresh();
        Message("", DK.Indigo500);
    }

    // For the screenshot tool
    public void Press(string key)
    {
        if (key == "pop") Pop();
        else Push(key);
    }
}
