using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds a lesson page from a plain-text file in Resources/Lessons.
// Format, one block per line:
//   # Heading
//   - bullet          1. numbered item
//   > callout line    (consecutive "> " lines form one card; "> - " lines are bullets inside it)
//   [image:name]      (sprite in Resources/Lessons)
//   anything else is a paragraph; **bold** and *italic* are supported
public static class LessonBuilder
{
    private const string RegularFont = "Poppins-Regular SDF";
    private const string BoldFont = "Poppins-Bold SDF";
    private const float BodySize = 46f;
    private const float HeadingSize = 56f;
    private const float LineSpacing = 18f;
    private const float MarkerWidth = 56f;

    public static TextAsset Load(string groupName)
    {
        return Resources.Load<TextAsset>("Lessons/" + groupName.Replace(" ", ""));
    }

    public static void Build(Transform content, string lesson)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.padding = new RectOffset(0, 32, 24, 64);
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        List<string> callout = new List<string>();
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
                AddCallout(content, callout);
                callout.Clear();
            }

            if (line.Length == 0)
                continue;
            if (line.StartsWith("# "))
                AddHeading(content, line.Substring(2));
            else if (line.StartsWith("- "))
                AddListItem(content, null, line.Substring(2));
            else if (Regex.IsMatch(line, @"^\d+\.\s"))
                AddListItem(content, line.Substring(0, line.IndexOf('.') + 1), line.Substring(line.IndexOf('.') + 1).Trim());
            else if (line.StartsWith("[image:") && line.EndsWith("]"))
                AddImage(content, line.Substring(7, line.Length - 8));
            else
                AddParagraph(content, line, UiKit.Body);
        }
        if (callout.Count > 0)
            AddCallout(content, callout);
    }

    private static string Markup(string text)
    {
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");
        return Regex.Replace(text, @"\*(.+?)\*", "<i>$1</i>");
    }

    private static void AddHeading(Transform parent, string text)
    {
        // Extra space above each section
        LayoutElement spacer = UiKit.Create("Spacer", parent).gameObject.AddComponent<LayoutElement>();
        spacer.preferredHeight = 12f;

        TextMeshProUGUI heading = UiKit.Text(parent, Markup(text), BoldFont, HeadingSize, UiKit.Ink);
        heading.name = "Heading";
    }

    private static TextMeshProUGUI AddParagraph(Transform parent, string text, Color color)
    {
        TextMeshProUGUI paragraph = UiKit.Text(parent, Markup(text), RegularFont, BodySize, color);
        paragraph.lineSpacing = LineSpacing;
        return paragraph;
    }

    // marker is "1." for numbered items, or null for a bullet dot
    private static void AddListItem(Transform parent, string marker, string text)
    {
        RectTransform row = UiKit.Create("ListItem", parent);
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(8, 0, 0, 0);
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        RectTransform markerBox = UiKit.Create("Marker", row);
        LayoutElement markerLayout = markerBox.gameObject.AddComponent<LayoutElement>();
        markerLayout.minWidth = markerLayout.preferredWidth = MarkerWidth;

        if (marker != null)
        {
            TextMeshProUGUI number = UiKit.Text(markerBox, marker, BoldFont, BodySize, UiKit.Brand);
            UiKit.Stretch(number.rectTransform, Vector2.zero, Vector2.one);
            number.rectTransform.offsetMin = new Vector2(0, -BodySize * 1.5f);
        }
        else
        {
            // Poppins has no bullet glyph, so the bullet is a small rounded dot
            Image dot = UiKit.Panel(markerBox, UiKit.Brand);
            RectTransform dotRect = dot.rectTransform;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(0.35f, 1f);
            dotRect.sizeDelta = new Vector2(14f, 14f);
            dotRect.anchoredPosition = new Vector2(0f, -BodySize * 0.72f);
        }

        TextMeshProUGUI body = AddParagraph(row, text, UiKit.Body);
        body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private static void AddCallout(Transform parent, List<string> lines)
    {
        Image card = UiKit.Panel(parent, UiKit.CardTint);
        card.name = "Callout";
        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(44, 44, 36, 36);
        cardLayout.spacing = 16f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        foreach (string line in lines)
        {
            if (line.StartsWith("- "))
                AddListItem(card.transform, null, line.Substring(2));
            else
                AddParagraph(card.transform, line, UiKit.Brand);
        }
    }

    private static void AddImage(Transform parent, string spriteName)
    {
        Sprite sprite = Resources.Load<Sprite>("Lessons/" + spriteName);
        if (sprite == null)
        {
            Debug.LogWarning("Lesson image not found: " + spriteName);
            return;
        }

        Image image = UiKit.Create("Image", parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.gameObject.AddComponent<AspectLayoutElement>().aspect = sprite.rect.width / sprite.rect.height;
    }
}
