using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared colors, fonts and helpers for UI that is built from code
public static class UiKit
{
    public static readonly Color Ink = new Color32(0x1E, 0x1A, 0x3B, 0xFF);
    public static readonly Color Body = new Color32(0x3A, 0x35, 0x55, 0xFF);
    public static readonly Color Brand = new Color32(0x45, 0x37, 0x74, 0xFF);
    public static readonly Color CardTint = new Color32(0xEE, 0xEA, 0xF7, 0xFF);
    public static readonly Color Correct = new Color32(0x3D, 0xBE, 0x7A, 0xFF);
    public static readonly Color Wrong = new Color32(0xE0, 0x52, 0x52, 0xFF);
    public static readonly Color Selected = new Color32(0xDC, 0xD4, 0xF0, 0xFF);

    private static readonly Dictionary<string, Object> Cache = new Dictionary<string, Object>();

    // Fonts and sprites referenced by the open scene are already loaded, so they can be found by name
    public static TMP_FontAsset Font(string name)
    {
        TMP_FontAsset font = Find<TMP_FontAsset>(name);
        return font != null ? font : TMP_Settings.defaultFontAsset;
    }

    public static Sprite Sprite(string name)
    {
        return Find<Sprite>(name);
    }

    private static T Find<T>(string name) where T : Object
    {
        if (Cache.TryGetValue(name, out Object cached) && cached != null)
            return cached as T;

        foreach (T asset in Resources.FindObjectsOfTypeAll<T>())
        {
            if (asset.name != name)
                continue;
            Cache[name] = asset;
            return asset;
        }
        return null;
    }

    public static RectTransform Create(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    public static TextMeshProUGUI Text(Transform parent, string text, string font, float size, Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
    {
        TextMeshProUGUI tmp = Create("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = Font(font);
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.text = text;
        return tmp;
    }

    // Rounded panel using the default UI sprite (9-sliced so corners stay round)
    public static Image Panel(Transform parent, Color color)
    {
        Image image = Create("Panel", parent).gameObject.AddComponent<Image>();
        image.sprite = Sprite("UISprite");
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
