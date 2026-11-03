using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Array practice: insert 45 at index 3. Like a real array insert, the elements from that index onward
// shift one slot right into the free slot at the end.
public class ArrayPuzzle : PuzzleScreen
{
    private static readonly string[] Start = { "3", "67", "5", "25", "" };
    private const int RightIndex = 3;
    private const string RightValue = "45";
    private const float ShiftStepSeconds = 0.3f;

    protected override string Topic => "Array";
    protected override string Instructions => "Insert the value 45 into the array at the specific position of index 3";
    protected override float SuccessSeconds => 2.5f;

    private readonly List<Slot> slots = new List<Slot>();
    private string[] values;
    private int count;
    private bool busy;
    private TMP_InputField indexInput, valueInput;
    private Button insert;

    protected override void Build()
    {
        RectTransform row = UiKit.Create("Cells", stage);
        DK.Anchor(row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500, 100));
        DK.Line(row.gameObject, 8, TextAnchor.MiddleCenter);
        for (int i = 0; i < Start.Length; i++)
        {
            RectTransform cell = UiKit.Create("Cell", row);
            DK.Column(cell.gameObject, 10, null, TextAnchor.UpperCenter).childForceExpandWidth = false;
            slots.Add(MakeSlot(cell, new Vector2(84, 64), 12));
            TextMeshProUGUI index = DK.Text(cell, i.ToString(), 15, DK.Red, true, TextAlignmentOptions.Center);
            DK.Size(index, 84, 16);
        }

        RectTransform bar = ControlRow();
        indexInput = Field(bar, "INDEX", "0-4", 96, 2);
        valueInput = Field(bar, "VALUE", "Whole number", 150, 4);
        RectTransform insertCell = UiKit.Create("InsertCell", bar);
        DK.Column(insertCell.gameObject, 5, new RectOffset(0, 0, 15, 0)).childForceExpandWidth = false;
        insert = LightButton(insertCell, "Insert", 110, 44, OnInsert);
        indexInput.onValueChanged.AddListener(_ => UpdateInsert());
        valueInput.onValueChanged.AddListener(_ => UpdateInsert());
    }

    private TMP_InputField Field(Transform parent, string label, string placeholder, float width, int limit)
    {
        RectTransform cell = UiKit.Create(label, parent);
        DK.Column(cell.gameObject, 5).childForceExpandWidth = false;
        TextMeshProUGUI caps = DK.Caps(cell, label, 10, DK.Lavender300);
        DK.Size(caps, width, 10);
        return Input(cell, placeholder, width, 44, DK.Lavender50, limit);
    }

    protected override void ResetPuzzle()
    {
        busy = false;
        indexInput.text = valueInput.text = "";
        ResetArray();
        UpdateInsert();
    }

    private void ResetArray()
    {
        values = (string[])Start.Clone();
        count = Start.Length - 1;
        Refresh(-1, null);
    }

    private void Refresh(int highlight, Color? color)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].Set(values[i]);
            slots[i].Highlight(i == highlight ? color : null);
        }
        // A correctly placed value turns green
        if (highlight >= 0 && color == DK.Green)
        {
            slots[highlight].outer.color = slots[highlight].fill.color = DK.Green;
        }
    }

    private void UpdateInsert()
    {
        bool ready = !busy && !Solved && indexInput.text.Length > 0 && valueInput.text.Length > 0;
        insert.interactable = ready;
        CanvasGroup group = DK.Get<CanvasGroup>(insert);
        group.alpha = ready ? 1f : 0.45f;
    }

    private void OnInsert()
    {
        if (busy || Solved)
            return;
        if (!int.TryParse(indexInput.text.Trim(), out int index))
        {
            Error("Type a whole number for the index.");
            return;
        }
        if (index < 0 || index >= values.Length)
        {
            Error($"Index out of bounds. This array's indexes go from 0 to {values.Length - 1}.");
            return;
        }
        // Arrays are contiguous: the new element can go at most right after the last one
        if (index > count)
        {
            Error($"Arrays can't have gaps. The next free index is {count}.");
            return;
        }
        StartCoroutine(InsertWithShift(index, valueInput.text.Trim()));
    }

    private IEnumerator InsertWithShift(int index, string value)
    {
        busy = true;
        UpdateInsert();
        Message("", DK.Indigo500);

        // Move elements right one at a time, starting from the end, so students see each shift
        for (int i = count - 1; i >= index; i--)
        {
            values[i + 1] = values[i];
            values[i] = "";
            Refresh(i + 1, DK.Lavender400);
            yield return new WaitForSeconds(ShiftStepSeconds);
        }

        values[index] = value;
        count++;

        int shifted = count - 1 - index;
        string note = shifted == 0 ? "Nothing had to shift."
            : shifted == 1 ? $"{values[index + 1]} shifted right to index {index + 1} to make room."
            : $"{shifted} elements shifted right to make room.";

        if (value == RightValue && index == RightIndex)
        {
            Refresh(index, DK.Green);
            Success($"Correct! {note}");
            yield break;
        }

        Refresh(index, DK.Red);
        string goal = value == RightValue ? $"{RightValue} belongs at index {RightIndex}." : $"the task is to insert {RightValue} at index {RightIndex}.";
        Error($"{note} But {goal} Try again.");
        yield return new WaitForSeconds(3f);

        ResetArray();
        Message("", DK.Indigo500);
        busy = false;
        UpdateInsert();
    }

    // For the screenshot tool
    public void TypeAndInsert(string index, string value)
    {
        indexInput.text = index;
        valueInput.text = value;
        OnInsert();
    }
}
