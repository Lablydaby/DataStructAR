using TMPro;
using UnityEngine;

// Small 3D labels for AR items (values, indexes, FRONT/REAR...), kept next to the item they name
// and turned to face the camera by ARLabel
public static class ARText
{
    public static readonly Color Ink = new Color32(0x3A, 0x31, 0x6B, 0xFF);
    public static readonly Color IndexRed = new Color32(0xC2, 0x45, 0x3A, 0xFF);

    public static TextMeshPro Label(Transform parent, string text, float size, Color color,
        Transform follow, Transform marker, float alongRow, float above)
    {
        GameObject go = new GameObject("Label-" + text);
        go.transform.SetParent(parent, false);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.font = UiKit.Font("Poppins-Bold SDF");
        tmp.text = text;
        // Same visible size whatever the parent's scale (the stack's parent is at half scale)
        tmp.fontSize = size / Mathf.Max(parent.lossyScale.x, 0.0001f);
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
        // A white edge keeps the dark text readable on the card and over the camera picture
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = Color.white;

        ARLabel label = go.AddComponent<ARLabel>();
        label.follow = follow;
        label.marker = marker;
        label.alongRow = alongRow;
        label.above = above;
        label.lift = 0.03f;
        return tmp;
    }

    public static void Move(TextMeshPro label, Transform follow, float alongRow, float above)
    {
        ARLabel arLabel = label.GetComponent<ARLabel>();
        arLabel.follow = follow;
        arLabel.alongRow = alongRow;
        arLabel.above = above;
    }

    // Distinct small numbers for the items in a structure
    public static int NewValue(System.Collections.Generic.ICollection<int> taken)
    {
        int value;
        do
            value = Random.Range(1, 100);
        while (taken.Contains(value));
        return value;
    }
}
