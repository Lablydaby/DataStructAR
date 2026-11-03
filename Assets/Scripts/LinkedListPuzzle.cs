using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Linked list practice: nodes A-D start shuffled; drag one onto another slot to swap them until the
// chain reads A -> B -> C -> D from the head.
public class LinkedListPuzzle : PuzzleScreen
{
    private static readonly string[] Correct = { "A", "B", "C", "D" };

    protected override string Topic => "Linked List";
    protected override string Instructions => "The nodes are all mixed up — swap them to fix the linked list! {A → B → C → D}";

    private readonly List<RectTransform> slotAreas = new List<RectTransform>();
    private readonly List<NodeView> nodes = new List<NodeView>();
    private string[] order;

    private class NodeView
    {
        public RectTransform rect;
        public TextMeshProUGUI label;
        public GameObject ok;
    }

    protected override void Build()
    {
        RectTransform row = UiKit.Create("Chain", stage);
        DK.Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(620, 110));
        DK.Line(row.gameObject, 6, TextAnchor.LowerCenter);

        for (int i = 0; i < Correct.Length; i++)
        {
            RectTransform cell = UiKit.Create("Cell", row);
            DK.Column(cell.gameObject, 8, null, TextAnchor.LowerCenter).childForceExpandWidth = false;
            TextMeshProUGUI head = SmallCaps(cell, i == 0 ? "HEAD" : "", DK.Indigo900);
            DK.Size(head, 96, 22);

            Image slot = DK.Outlined(cell, DK.White, DK.Dash, 14, 2, "Slot");
            DK.Size(slot, 96, 76);
            slot.raycastTarget = true;
            slotAreas.Add(slot.rectTransform);

            nodes.Add(MakeNode(slot.transform, i));

            RectTransform arrowCell = UiKit.Create("ArrowCell", row);
            DK.Size(arrowCell, 22, 76);
            RectTransform arrow = DK.Icon(arrowCell, "arrow-right", 22, DK.Lavender400);
            DK.Anchor(arrow, new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(22, 22));
        }
        TextMeshProUGUI nul = DK.Caps(row, "NULL", 13, DK.Lavender500, TextAlignmentOptions.Center);
        DK.Size(nul, 50, 76);
        nul.alignment = TextAlignmentOptions.Center;

        RectTransform bar = ControlRow(10);
        RectTransform iconCell = UiKit.Create("IconCell", bar);
        DK.Size(iconCell, 22, 22);
        RectTransform drag = DK.Icon(iconCell, "arrow-right", 20, DK.Lavender50);
        DK.Anchor(drag, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
        TextMeshProUGUI hint = DK.Text(bar, "Drag a node onto another slot to swap them.", 16, DK.Lavender50);
        hint.enableWordWrapping = false;
    }

    private NodeView MakeNode(Transform slot, int index)
    {
        NodeView node = new NodeView();
        Image body = DK.Box(slot, DK.Indigo700, 12, "Node");
        DK.Anchor(body, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78, 60));
        body.raycastTarget = true;
        node.rect = body.rectTransform;

        Image pointer = DK.Box(body.transform, DK.Indigo500, 12, "Pointer");
        DK.Anchor(pointer, new Vector2(1, 0.5f), Vector2.zero, new Vector2(30, 60));
        Image cover = DK.Box(body.transform, DK.Indigo700, 0, "Cover");
        DK.Anchor(cover, new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(10, 60));
        Image dot = DK.Box(body.transform, DK.Lavender50, 3.5f, "Dot");
        DK.Anchor(dot, new Vector2(1, 0.5f), new Vector2(-7.5f, 0), new Vector2(7, 7));

        node.label = DK.Text(body.transform, "", 26, DK.White, true, TextAlignmentOptions.Center);
        DK.Anchor(node.label, new Vector2(0, 0.5f), Vector2.zero, new Vector2(56, 60));

        Image ok = DK.Outlined(body.transform, DK.Green, DK.White, 11, 2, "Ok");
        DK.Anchor(ok, new Vector2(1, 1), new Vector2(8, 8), new Vector2(22, 22));
        RectTransform check = DK.Icon(ok.transform, "check", 12, DK.White);
        DK.Anchor(check, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12));
        node.ok = ok.gameObject;

        NodeDrag drag = body.gameObject.AddComponent<NodeDrag>();
        drag.puzzle = this;
        drag.index = index;
        return node;
    }

    protected override void ResetPuzzle()
    {
        // Shuffle, but never start already solved
        do
        {
            order = (string[])Correct.Clone();
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        } while (string.Join("", order) == string.Join("", Correct));
        Refresh();
    }

    protected override string DefaultMessage => PlacedMessage();

    private string PlacedMessage()
    {
        if (order == null)
            return "";
        int right = 0;
        for (int i = 0; i < order.Length; i++)
            if (order[i] == Correct[i]) right++;
        return $"{right} of {Correct.Length} nodes are in the right place.";
    }

    private void Refresh()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].label.text = order[i];
            nodes[i].ok.SetActive(order[i] == Correct[i]);
            nodes[i].rect.anchoredPosition = Vector2.zero;
        }
        if (!Solved)
            Message(PlacedMessage(), DK.Indigo500);
    }

    public void Drop(int from, Vector2 screenPoint, Camera eventCamera)
    {
        int to = -1;
        for (int i = 0; i < slotAreas.Count; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(slotAreas[i], screenPoint, eventCamera))
                to = i;
        if (to >= 0 && to != from)
            (order[from], order[to]) = (order[to], order[from]);
        Refresh();
        if (string.Join("", order) == string.Join("", Correct))
            Success("Great job! The Linked List is fixed!");
    }

    // For the screenshot tool
    public void Swap(int a, int b)
    {
        (order[a], order[b]) = (order[b], order[a]);
        Refresh();
        if (string.Join("", order) == string.Join("", Correct))
            Success("Great job! The Linked List is fixed!");
    }

    public string[] Order => order;

    public bool Locked => Solved;
}
