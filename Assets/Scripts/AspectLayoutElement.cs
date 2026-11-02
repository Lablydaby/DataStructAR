using UnityEngine;
using UnityEngine.UI;

// Asks the layout for a height that keeps the image's aspect ratio at whatever width it is given
public class AspectLayoutElement : LayoutElement
{
    public float aspect = 1f;

    public override float preferredHeight
    {
        get { return ((RectTransform)transform).rect.width / Mathf.Max(aspect, 0.01f); }
        set { }
    }
}
