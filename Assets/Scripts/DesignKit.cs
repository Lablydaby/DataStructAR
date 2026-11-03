using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Colours, shapes, text and icons for the v4 app design, built in code.
// Sizes are in design pixels (the design's 390-wide phone frame); DesignRoot scales them to the screen.
public static class DK
{
    // Palette from the design's tokens
    public static readonly Color Indigo900 = Hex(0x3A316B);
    public static readonly Color Indigo700 = Hex(0x483A75);
    public static readonly Color Indigo500 = Hex(0x5F5787);
    public static readonly Color Lavender500 = Hex(0x8F89A5);
    public static readonly Color Lavender400 = Hex(0xA299C4);
    public static readonly Color Lavender300 = Hex(0xD7D5E3);
    public static readonly Color Lavender200 = Hex(0xDADDF1);
    public static readonly Color Lavender100 = Hex(0xEFF0F6);
    public static readonly Color Lavender50 = Hex(0xF8F9FF);
    public static readonly Color Ink = Hex(0x1E1A3C);
    public static readonly Color ArrowInk = Hex(0x262B4B);
    public static readonly Color AppBg = Hex(0xF9F9FB);
    public static readonly Color Deco = Hex(0xEFEFF1);
    public static readonly Color DecoRing = Hex(0xE4E4E7);
    public static readonly Color Dash = Hex(0xC9C5DA);
    public static readonly Color Green = Hex(0x2F8F5E);
    public static readonly Color GreenTint = Hex(0xE3F4EA);
    public static readonly Color Red = Hex(0xC2453A);
    public static readonly Color RedTint = Hex(0xF8E4E1);
    public static readonly Color WrongRing = Hex(0xB04A3A);
    public static readonly Color WrongTint = Hex(0xF6E3DF);
    public static readonly Color Ochre = Hex(0xD89839);
    public static readonly Color White = Color.white;
    public static readonly Color Clear = new Color(1, 1, 1, 0);

    public const string Regular = "Poppins-Regular SDF";
    public const string Bold = "Poppins-Bold SDF";

    // GetComponent returns Unity's fake null when missing, so ?? can't be used to add one
    public static T Get<T>(GameObject go) where T : Component => go.TryGetComponent(out T c) ? c : go.AddComponent<T>();
    public static T Get<T>(Component c) where T : Component => Get<T>(c.gameObject);

    public static Color Hex(int rgb, float alpha = 1f)
    {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }

    public static string Html(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // ---- Procedural sprites ----

    private const int RoundSize = 64;
    private const float RoundBorder = 32f;
    private static Sprite round, star, starOutline;

    // A circle texture used 9-sliced: its corners become any radius via pixelsPerUnitMultiplier
    public static Sprite Round => round ??= MakeSprite(RoundSize, (x, y) => Circle(x, y, RoundSize), RoundBorder);
    public static Sprite Star => star ??= MakeSprite(128, (x, y) => StarDistance(x, y, 128), 0);
    public static Sprite StarOutline => starOutline ??= MakeSprite(128, (x, y) => 4f - Mathf.Abs(StarDistance(x, y, 128)), 0);

    private static float Circle(float x, float y, int size)
    {
        float r = size / 2f;
        return r - Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
    }

    // Signed distance to a five-point star (negative inside)
    private static float StarDistance(float x, float y, int size)
    {
        Vector2 p = new Vector2(x - size / 2f, y - size / 2f - size * 0.03f) / (size * 0.47f);
        float angle = Mathf.Atan2(p.x, p.y);
        float sector = Mathf.PI * 2f / 5f;
        float a = Mathf.Repeat(angle + sector / 2f, sector) - sector / 2f;
        Vector2 q = new Vector2(Mathf.Abs(Mathf.Sin(a)), Mathf.Cos(a)) * p.magnitude;
        // Edge from the outer tip (0,1) to the inner corner
        float inner = 0.45f;
        Vector2 tip = new Vector2(0, 1), corner = new Vector2(Mathf.Sin(sector / 2f), Mathf.Cos(sector / 2f)) * inner;
        Vector2 edge = corner - tip, rel = q - tip;
        float t = Mathf.Clamp01(Vector2.Dot(rel, edge) / edge.sqrMagnitude);
        float dist = (rel - edge * t).magnitude;
        float side = edge.x * rel.y - edge.y * rel.x;
        return (side > 0 ? -dist : dist) * size * 0.47f;
    }

    private static Sprite MakeSprite(int size, Func<float, float, float> inside, float border)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(inside(x + 0.5f, y + 0.5f) + 0.5f));
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(border, border, border, border));
    }

    // ---- Building blocks ----

    public static RectTransform Rect(Transform parent, string name)
    {
        RectTransform rect = UiKit.Create(name, parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        return rect;
    }

    // Rounded rectangle; radius 0 gives square corners
    public static Image Box(Transform parent, Color color, float radius, string name = "Box")
    {
        Image image = UiKit.Create(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        SetRadius(image, radius);
        return image;
    }

    public static void SetRadius(Image image, float radius)
    {
        if (radius <= 0f)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            return;
        }
        image.sprite = Round;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = RoundBorder / radius;
    }

    // Rounded box with an inner border: the outer box is the border colour, the inner one the fill
    public static Image Outlined(Transform parent, Color fill, Color border, float radius, float width, string name = "Box")
    {
        Image outer = Box(parent, border, radius, name);
        Image inner = Box(outer.transform, fill, Mathf.Max(radius - width, 0.01f), "Fill");
        Stretch(inner.rectTransform, width, width, width, width);
        inner.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        inner.transform.SetAsFirstSibling();
        return outer;
    }

    public static Image Fill(Image outlined) => outlined.transform.Find("Fill").GetComponent<Image>();

    public static TextMeshProUGUI Text(Transform parent, string text, float size, Color color, bool bold = false,
        TextAlignmentOptions align = TextAlignmentOptions.TopLeft, string name = "Text")
    {
        TextMeshProUGUI tmp = UiKit.Text(parent, text, bold ? Bold : Regular, size, color, align);
        tmp.name = name;
        tmp.richText = true;
        return tmp;
    }

    // Small spaced capitals, standing in for the design's Montserrat labels
    public static TextMeshProUGUI Caps(Transform parent, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI tmp = Text(parent, text, size, color, true, align, "Label");
        tmp.characterSpacing = 8f;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    public static Button MakeButton(Graphic target, Action onClick)
    {
        target.raycastTarget = true;
        Button button = DK.Get<Button>(target.gameObject);
        button.targetGraphic = target;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.98f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.9f);
        colors.disabledColor = new Color(1, 1, 1, 0.45f);
        button.colors = colors;
        if (onClick != null)
            button.onClick.AddListener(() => onClick());
        return button;
    }

    // A full button: rounded fill, optional icon and label laid out in a centred row
    public static Button Pill(Transform parent, string label, Color fill, Color textColor, float radius, float fontSize,
        Action onClick, string iconBefore = null, string iconAfter = null, Color? border = null, bool bold = false)
    {
        Image bg = border.HasValue ? Outlined(parent, fill, border.Value, radius, 1.5f, "Button") : Box(parent, fill, radius, "Button");
        RectTransform row = UiKit.Create("Row", bg.transform);
        UiKit.Stretch(row, Vector2.zero, Vector2.one);
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        if (iconBefore != null)
            IconCell(row, iconBefore, fontSize * 1.1f, textColor);
        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI text = Text(row, label, fontSize, textColor, bold, TextAlignmentOptions.Center, "Label");
            text.enableWordWrapping = false;
        }
        if (iconAfter != null)
            IconCell(row, iconAfter, fontSize * 1.1f, textColor);
        return MakeButton(bg, onClick);
    }

    public static TextMeshProUGUI PillLabel(Button button) => button.GetComponentInChildren<TextMeshProUGUI>(true);

    private static void IconCell(Transform row, string kind, float size, Color color)
    {
        RectTransform cell = UiKit.Create("IconCell", row);
        LayoutElement le = cell.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = le.preferredHeight = le.minWidth = le.minHeight = size;
        RectTransform icon = Icon(cell, kind, size, color);
        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
        icon.anchoredPosition = Vector2.zero;
    }

    // ---- Placement (design pixels, y measured down from the top unless stated) ----

    public static T At<T>(T c, float x, float y, float w, float h) where T : Component
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return c;
    }

    // Anchored to a corner/edge point of the parent: anchor (0..1, 0..1), offset from it, pivot = anchor
    public static T Anchor<T>(T c, Vector2 anchor, Vector2 offset, Vector2 size) where T : Component
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = offset;
        rt.sizeDelta = size;
        return c;
    }

    // Full width between left/right margins, at y from the top
    public static T Row<T>(T c, float left, float right, float y, float h) where T : Component
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(left, -y - h);
        rt.offsetMax = new Vector2(-right, -y);
        return c;
    }

    // Full width between left/right margins, at y from the bottom
    public static T BottomRow<T>(T c, float left, float right, float bottom, float h) where T : Component
    {
        RectTransform rt = (RectTransform)c.transform;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, bottom + h);
        return c;
    }

    public static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static VerticalLayoutGroup Column(GameObject go, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft)
    {
        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = padding ?? new RectOffset();
        layout.childAlignment = align;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static HorizontalLayoutGroup Line(GameObject go, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
    {
        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = align;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        return layout;
    }

    public static LayoutElement Size(Component c, float w = -1, float h = -1, float flexW = -1)
    {
        LayoutElement le = DK.Get<LayoutElement>(c.gameObject);
        if (w >= 0) le.preferredWidth = le.minWidth = w;
        if (h >= 0) le.preferredHeight = le.minHeight = h;
        if (flexW >= 0) le.flexibleWidth = flexW;
        return le;
    }

    // The design's soft background circles
    public static void Decorate(Transform root, bool bottomRing = true)
    {
        Image top = Box(root, Deco, 115, "DecoTop");
        Anchor(top, new Vector2(1, 1), new Vector2(90, 90), new Vector2(230, 230));
        if (!bottomRing)
            return;
        Image ring = Outlined(root, AppBg, DecoRing, 90, 10, "DecoRing");
        Anchor(ring, new Vector2(0, 0), new Vector2(-70, -60), new Vector2(180, 180));
    }

    // ---- Icons, drawn from rounded bars in a size x size box ----

    public static RectTransform Icon(Transform parent, string kind, float size, Color color)
    {
        RectTransform box = UiKit.Create("Icon-" + kind, parent);
        box.sizeDelta = new Vector2(size, size);
        float w = Mathf.Max(size * 0.11f, 1.6f);
        void Bar(float x1, float y1, float x2, float y2) => DrawBar(box, new Vector2(x1, y1) * size, new Vector2(x2, y2) * size, w, color);
        switch (kind)
        {
            case "arrow-right":
                Bar(-0.32f, 0, 0.32f, 0); Bar(0.32f, 0, 0.04f, 0.28f); Bar(0.32f, 0, 0.04f, -0.28f); break;
            case "arrow-left":
                Bar(-0.32f, 0, 0.32f, 0); Bar(-0.32f, 0, -0.04f, 0.28f); Bar(-0.32f, 0, -0.04f, -0.28f); break;
            case "plus":
                Bar(-0.3f, 0, 0.3f, 0); Bar(0, -0.3f, 0, 0.3f); break;
            case "minus":
                Bar(-0.3f, 0, 0.3f, 0); break;
            case "check":
                Bar(-0.34f, 0.02f, -0.1f, -0.24f); Bar(-0.1f, -0.24f, 0.36f, 0.26f); break;
            case "cross":
                Bar(-0.27f, -0.27f, 0.27f, 0.27f); Bar(-0.27f, 0.27f, 0.27f, -0.27f); break;
            case "chevron-down":
                Bar(-0.27f, 0.12f, 0, -0.15f); Bar(0, -0.15f, 0.27f, 0.12f); break;
            case "chevron-up":
                Bar(-0.27f, -0.12f, 0, 0.15f); Bar(0, 0.15f, 0.27f, -0.12f); break;
            case "scan":
                foreach (Vector2 c in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
                {
                    Bar(c.x * 0.38f, c.y * 0.38f, c.x * 0.16f, c.y * 0.38f);
                    Bar(c.x * 0.38f, c.y * 0.38f, c.x * 0.38f, c.y * 0.16f);
                }
                Ring(box, 0, 0, size * 0.26f, w, color); break;
            case "camera":
                Bar(-0.4f, 0.22f, 0.4f, 0.22f); Bar(-0.4f, -0.3f, 0.4f, -0.3f); Bar(-0.4f, 0.22f, -0.4f, -0.3f); Bar(0.4f, 0.22f, 0.4f, -0.3f);
                Bar(-0.14f, 0.34f, 0.14f, 0.34f);
                Ring(box, 0, -size * 0.04f, size * 0.32f, w, color); break;
            case "bulb":
                Ring(box, 0, size * 0.1f, size * 0.52f, w, color);
                Bar(-0.12f, -0.26f, 0.12f, -0.26f); Bar(-0.08f, -0.4f, 0.08f, -0.4f); break;
            case "reset":
                Ring(box, 0, 0, size * 0.62f, w, color);
                Bar(0.31f, 0.12f, 0.31f, -0.06f); Bar(0.31f, 0.12f, 0.14f, 0.06f); break;
            case "bang":
                Bar(0, 0.22f, 0, -0.06f); Bar(0, -0.24f, 0, -0.26f); break;
        }
        return box;
    }

    // Hollow circle; the band is about a tenth of the diameter
    private static void Ring(RectTransform parent, float x, float y, float diameter, float width, Color color)
    {
        Image ring = Box(parent, color, 0, "Ring");
        ring.sprite = RingSprite;
        Anchor(ring, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(diameter, diameter));
    }

    private static Sprite ringSprite;
    private static Sprite RingSprite => ringSprite ??= MakeSprite(96, (px, py) => 6f - Mathf.Abs(Circle(px, py, 96) - 6.5f), 0);

    private static void DrawBar(RectTransform parent, Vector2 a, Vector2 b, float width, Color color)
    {
        Vector2 d = b - a;
        float length = d.magnitude + width;
        Image bar = Box(parent, color, width / 2f, "Bar");
        RectTransform rt = bar.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(length, width);
        rt.anchoredPosition = (a + b) / 2f;
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    // Removes every child of a panel and gives it a fresh design root
    public static RectTransform Screen(Transform panel, Vector2 designSize, bool fitHeight, Color background, string name = "Screen")
    {
        foreach (Transform child in panel)
            child.gameObject.SetActive(false);
        Image bg = Box(panel, background, 0, name + "Bg");
        UiKit.Stretch(bg.rectTransform, Vector2.zero, Vector2.one);
        bg.raycastTarget = true;
        RectTransform root = UiKit.Create(name, panel);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
        DesignRoot scaler = root.gameObject.AddComponent<DesignRoot>();
        scaler.designSize = designSize;
        scaler.fitHeight = fitHeight;
        scaler.Apply();
        return root;
    }

    public static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }
}
