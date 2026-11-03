using UnityEngine;
using UnityEngine.EventSystems;

// Drags a linked-list node; dropping it on another slot swaps the two
public class NodeDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public LinkedListPuzzle puzzle;
    public int index;
    private Canvas canvas;
    private CanvasGroup group;

    private void Awake()
    {
        group = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (puzzle.Locked)
            return;
        canvas = GetComponentInParent<Canvas>();
        group.alpha = 0.55f;
        group.blocksRaycasts = false;
        transform.SetAsLastSibling();
        transform.localScale = Vector3.one * 1.06f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (puzzle.Locked)
            return;
        // Move in the node's own (scaled) space
        RectTransform rect = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent, eventData.position, eventData.pressEventCamera, out Vector2 local);
        rect.anchoredPosition = local;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        group.alpha = 1f;
        group.blocksRaycasts = true;
        transform.localScale = Vector3.one;
        puzzle.Drop(index, eventData.position, eventData.pressEventCamera);
    }
}
