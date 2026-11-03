using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds a lesson page from a plain-text file in Resources/Lessons, styled like the v4 design.
// Format, one block per line:
//   # Heading
//   - bullet          1. numbered item
//   > callout line    (consecutive "> " lines form one card; "> - " lines are bullets inside it)
//   [image:name]      (sprite in Resources/Lessons)
//   anything else is a paragraph; **bold** and *italic* are supported
// "# Key takeaways" and "# Try it in AR" sections become the design's dark summary card and AR card.
public static class LessonBuilder
{
    private const float BodySize = 16f;
    private const float ListSize = 15f;

    public static TextAsset Load(string groupName)
    {
        return Resources.Load<TextAsset>("Lessons/" + groupName.Replace(" ", ""));
    }

    public static void Build(Transform content, string lesson)
    {
        List<string> callout = new List<string>();
        string section = null; // The heading the current lines belong to
        RectTransform list = null;
        int number = 0;

        foreach (string rawLine in lesson.Replace("\r", "").Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.StartsWith(">"))
            {
                callout.Add(line.Substring(1).Trim());
                continue;
            }
            if (callout.Count > 0)
            {
                if (section == "Key takeaways") AddTakeaways(content, callout);
                else AddCallout(content, callout);
                callout.Clear();
            }

            bool isItem = line.StartsWith("- ") || Regex.IsMatch(line, @"^\d+\.\s") || line.StartsWith("[image:");
            if (!isItem)
                list = null;

            if (line.Length == 0)
                continue;
            if (line.StartsWith("# "))
            {
                section = line.Substring(2).Trim();
                if (section != "Key takeaways" && section != "Try it in AR")
                    AddHeading(content, line.Substring(2));
                number = 0;
            }
            else if (line.StartsWith("- ") || Regex.IsMatch(line, @"^\d+\.\s"))
            {
                bool numbered = !line.StartsWith("- ");
                list ??= List(content);
                string text = numbered ? line.Substring(line.IndexOf('.') + 1).Trim() : line.Substring(2);
                AddListItem(list, numbered ? (++number).ToString() : null, text);
            }
            else if (line.StartsWith("[image:") && line.EndsWith("]"))
                AddImage(list != null ? list : (RectTransform)content, line.Substring(7, line.Length - 8));
            else if (section == "Try it in AR")
                AddTryIt(content, line);
            else
                AddParagraph(content, line, BodySize, DK.Ink);
        }
        if (callout.Count > 0)
        {
            if (section == "Key takeaways") AddTakeaways(content, callout);
            else AddCallout(content, callout);
        }
    }

    private static string Markup(string text, Color boldColor)
    {
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", $"<b><color={DK.Html(boldColor)}>$1</color></b>");
        return Regex.Replace(text, @"\*(.+?)\*", "<i>$1</i>");
    }

    private static void AddHeading(Transform parent, string text)
    {
        TextMeshProUGUI heading = DK.Text(parent, Markup(text, DK.Indigo900), 18, DK.Indigo900, true, TextAlignmentOptions.TopLeft, "Heading");
        heading.margin = new Vector4(0, 8, 0, 0);
    }

    private static TextMeshProUGUI AddParagraph(Transform parent, string text, float size, Color color)
    {
        TextMeshProUGUI paragraph = DK.Text(parent, Markup(text, DK.Indigo900), size, color);
        paragraph.lineSpacing = 6;
        return paragraph;
    }

    private static RectTransform List(Transform parent)
    {
        RectTransform list = UiKit.Create("List", parent);
        DK.Column(list.gameObject, 12);
        return list;
    }

    // number is "1", "2"... for numbered items, or null for a bullet dot
    private static void AddListItem(Transform parent, string number, string text)
    {
        RectTransform row = UiKit.Create("ListItem", parent);
        HorizontalLayoutGroup layout = DK.Line(row.gameObject, 12, TextAnchor.UpperLeft);
        layout.childForceExpandHeight = false;

        RectTransform marker = UiKit.Create("Marker", row);
        if (number != null)
        {
            DK.Size(marker, 24, 24);
            Image circle = DK.Box(marker, DK.Indigo700, 12, "Circle");
            UiKit.Stretch(circle.rectTransform, Vector2.zero, Vector2.one);
            TextMeshProUGUI digit = DK.Text(circle.transform, number, 12, DK.White, true, TextAlignmentOptions.Center);
            UiKit.Stretch(digit.rectTransform, Vector2.zero, Vector2.one);
        }
        else
        {
            // Poppins has no bullet glyph, so the bullet is a small dot
            DK.Size(marker, 15, 22);
            Image dot = DK.Box(marker, DK.Lavender400, 3.5f, "Dot");
            DK.Anchor(dot, new Vector2(0.5f, 1), new Vector2(0, -9), new Vector2(7, 7));
        }

        TextMeshProUGUI body = AddParagraph(row, text, ListSize, DK.Ink);
        DK.Size(body, -1, -1, 1);
    }

    private static Image Card(Transform parent, Color fill, float radius, RectOffset padding, float spacing, string name)
    {
        Image card = DK.Box(parent, fill, radius, name);
        DK.Column(card.gameObject, spacing, padding);
        return card;
    }

    // Light card with a bulb, for the "Think of..." analogies
    private static void AddCallout(Transform parent, List<string> lines)
    {
        Image card = DK.Box(parent, DK.Lavender100, 14, "Callout");
        HorizontalLayoutGroup row = DK.Line(card.gameObject, 12, TextAnchor.UpperLeft);
        row.padding = new RectOffset(18, 18, 16, 16);

        RectTransform iconCell = UiKit.Create("IconCell", card.transform);
        DK.Size(iconCell, 20, 22);
        RectTransform bulb = DK.Icon(iconCell, "bulb", 20, DK.Indigo700);
        DK.Anchor(bulb, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(20, 20));

        TextMeshProUGUI text = AddParagraph(card.transform, string.Join(" ", lines), ListSize, DK.Indigo900);
        DK.Size(text, -1, -1, 1);
    }

    private static void AddTakeaways(Transform parent, List<string> lines)
    {
        Image card = Card(parent, DK.Indigo700, 16, new RectOffset(18, 18, 18, 20), 12, "Takeaways");
        card.GetComponent<LayoutElement>();
        DK.Caps(card.transform, "KEY TAKEAWAYS", 12, DK.Lavender300);
        foreach (string line in lines)
        {
            RectTransform row = UiKit.Create("Takeaway", card.transform);
            DK.Line(row.gameObject, 10, TextAnchor.UpperLeft);
            RectTransform cell = UiKit.Create("IconCell", row);
            DK.Size(cell, 16, 20);
            RectTransform check = DK.Icon(cell, "check", 16, DK.Lavender50);
            DK.Anchor(check, new Vector2(0.5f, 1), new Vector2(0, -3), new Vector2(16, 16));
            TextMeshProUGUI text = AddParagraph(row, line.StartsWith("- ") ? line.Substring(2) : line, ListSize, DK.White);
            DK.Size(text, -1, -1, 1);
        }
    }

    private static void AddTryIt(Transform parent, string line)
    {
        Image outer = DK.Outlined(parent, DK.White, DK.Lavender300, 16, 1.5f, "TryIt");
        DK.Column(outer.gameObject, 8, new RectOffset(18, 18, 16, 16));
        RectTransform title = UiKit.Create("Title", outer.transform);
        DK.Line(title.gameObject, 8);
        RectTransform cell = UiKit.Create("IconCell", title);
        DK.Size(cell, 18, 18);
        RectTransform scan = DK.Icon(cell, "scan", 18, DK.Indigo900);
        DK.Anchor(scan, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 18));
        TextMeshProUGUI label = DK.Text(title, "Try it in AR", 15, DK.Indigo900, true);
        label.enableWordWrapping = false;
        AddParagraph(outer.transform, line, ListSize, DK.Ink);
    }

    private static void AddImage(RectTransform parent, string spriteName)
    {
        Sprite sprite = Resources.Load<Sprite>("Lessons/" + spriteName);
        if (sprite == null)
        {
            Debug.LogWarning("Lesson image not found: " + spriteName);
            return;
        }

        // In a list, the diagram sits under its item, lined up with the text
        Image card = DK.Outlined(parent, DK.White, DK.Lavender100, 14, 1, "Diagram");
        DK.Column(card.gameObject, 0, new RectOffset(12, 12, 12, 12));
        Image image = UiKit.Create("Image", card.transform).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.AddComponent<AspectLayoutElement>().aspect = sprite.rect.width / sprite.rect.height;
    }
}
