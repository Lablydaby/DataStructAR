using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableNode : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    //private Transform originalParent;

    public Transform originalParent { get; set; } // make it accessible


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform); // Keep on top
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // If not dropped on any zone
        if (transform.parent == canvas.transform)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }

        // Call order check from manager
        FindObjectOfType<LinkedListQuiz>().CheckLinkedListOrder();
    }
}
