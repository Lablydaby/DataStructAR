using UnityEngine;

// Scales a screen built in design pixels to fill its parent: portrait screens fit the design width
// (the height follows the device), landscape screens fit the design height.
[ExecuteAlways]
public class DesignRoot : MonoBehaviour
{
    public Vector2 designSize = new Vector2(390, 844);
    public bool fitHeight;

    private void OnEnable() => Apply();
    private void Update() => Apply();

    public void Apply()
    {
        RectTransform rect = (RectTransform)transform;
        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
            return;
        Vector2 size = parent.rect.size;
        if (size.x <= 0 || size.y <= 0)
            return;
        // Landscape screens fit the design height, but never so much that the design's width overflows
        // (e.g. 16:9 phones or iPads); the extra room then goes to the height
        float scale = fitHeight ? Mathf.Min(size.y / designSize.y, size.x / designSize.x) : size.x / designSize.x;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = new Vector3(scale, scale, 1f);
        rect.sizeDelta = size / scale;
    }
}
