using UnityEngine;
using UnityEngine.EventSystems;

public class DropZoneHandler : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;

        if (dropped == null)
            return;

        Transform incoming = dropped.transform;
        Transform existing = transform.childCount > 0 ? transform.GetChild(0) : null;

        // If current drop zone already has a node, swap
        if (existing != null && incoming != existing)

        {
            Transform originalParent = incoming.GetComponent<DraggableNode>().originalParent;

            // Swap
            existing.SetParent(originalParent);
            existing.localPosition = Vector3.zero;

            incoming.SetParent(transform);
            incoming.localPosition = Vector3.zero;
        }
        else
        {
            // No existing → just drop
            incoming.SetParent(transform);
            incoming.localPosition = Vector3.zero;
        }

        // Update original parent in DragHandler
        incoming.GetComponent<DraggableNode>().originalParent = transform;

        // Check order again
        FindObjectOfType<LinkedListQuiz>().CheckLinkedListOrder();
    }
}
