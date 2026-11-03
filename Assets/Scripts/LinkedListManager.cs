using UnityEngine;

public class LinkedListManager : MonoBehaviour
{
    public GameObject[] nodePrefabs; // Prefabs for different shapes and colors
    public Transform startPosition;
    public float nodeSpacing = 2.0f;

    private Node head;
    public GameObject nodeListParent; // Parent GameObject to encapsulate nodes
    private int nodeCount = 0; // Counter to track the number of nodes
    private const int maxNodes = 5; // Maximum number of nodes allowed

    public GameObject nullNodePrefab; // The visual representation of a null node
    private GameObject currentNullNode; // Keeps track of the current null node

    public GameObject headMarkerPrefab; // Visual indicator for head node
    private GameObject currentHeadMarker; // Tracks the spawned head marker

    [Header("Visual Offsets")]
    public Vector3 headOffset = new Vector3(0, 1f, 0);     // Offset for head marker
    public Vector3 nullOffset = Vector3.zero;             // Offset for null node


    void Start()
    {
        // Ensure startPosition is assigned
        if (startPosition == null)
        {
            Debug.LogError("Start position is not assigned.");
            return;
        }


    }

    public void AddNode(int prefabIndex)
    {
        // Check if the maximum number of nodes has been reached
        if (nodeCount >= maxNodes)
        {
            Debug.LogWarning("Maximum number of nodes reached. Cannot add more nodes.");
            return;
        }

        // Check if the prefab index is within bounds
        if (prefabIndex < 0 || prefabIndex >= nodePrefabs.Length)
        {
            Debug.LogError("Prefab index out of bounds: " + prefabIndex);
            return;
        }

        // Check if the node prefab is assigned
        if (nodePrefabs[prefabIndex] == null)
        {
            Debug.LogError("Node prefab is not assigned for index: " + prefabIndex);
            return;
        }

        // Instantiate the new node object
        GameObject newNodeObj = Instantiate(nodePrefabs[prefabIndex], GetNextNodePosition(), startPosition.rotation, nodeListParent.transform);

        // Get the Node component from the new object
        Node newNode = newNodeObj.GetComponent<Node>();
        if (newNode == null)
        {
            Debug.LogError("Node component is missing on the prefab: " + nodePrefabs[prefabIndex].name);
            return;
        }

        // Add the new node to the linked list
        if (head == null)
        {
            head = newNode;
        }
        else
        {
            Node current = head;
            while (current.next != null)
            {
                current = current.next;
            }
            current.SetNext(newNode);
        }

        // Increment node count
        nodeCount++;

        // Update visuals
        UpdateNullNodePosition();
        UpdateHeadMarker();


    }

    public void RemoveLastNode()
    {
        if (head == null)
        {
            Debug.LogWarning("No nodes to remove.");
            return;
        }

        if (head.next == null)
        {
            Destroy(head.gameObject);
            head = null;
        }
        else
        {
            Node current = head;
            while (current.next.next != null)
            {
                current = current.next;
            }

            Destroy(current.next.gameObject);
            current.next = null;
        }

        nodeCount--;
        UpdateNullNodePosition();
        UpdateHeadMarker();


    }


    private void UpdateNullNodePosition()
    {
        if (currentNullNode != null)
        {
            Destroy(currentNullNode);
        }

        // Null sits one step past the last node, or at the start when the list is empty
        Node last = LastNode();
        Vector3 position = GetNextNodePosition() + startPosition.rotation * nullOffset;
        currentNullNode = Instantiate(nullNodePrefab, position, startPosition.rotation, nodeListParent.transform);
        AttachLabel(currentNullNode, last != null ? last.transform : startPosition,
            (last != null ? nodeSpacing : 0f) + nullOffset.x, nullOffset.y);
    }



    private void UpdateHeadMarker()
    {
        if (head == null)
        {
            if (currentHeadMarker != null)
            {
                Destroy(currentHeadMarker);
                currentHeadMarker = null;
            }
            return;
        }

        if (currentHeadMarker == null)
        {
            currentHeadMarker = Instantiate(headMarkerPrefab, head.transform.position + startPosition.rotation * headOffset, startPosition.rotation, nodeListParent.transform);
            AttachLabel(currentHeadMarker, head.transform, headOffset.x, headOffset.y);
        }
    }

    // Labels follow their node, sit above it whichever way the card faces, and turn toward the camera
    private void AttachLabel(GameObject label, Transform follow, float alongRow, float above)
    {
        ARLabel arLabel = label.AddComponent<ARLabel>();
        arLabel.follow = follow;
        arLabel.marker = startPosition;
        arLabel.alongRow = alongRow;
        arLabel.above = above;
    }

    private Node LastNode()
    {
        Node current = head;
        while (current != null && current.next != null)
            current = current.next;
        return current;
    }



    private Vector3 GetNextNodePosition()
    {
        if (head == null)
        {
            return startPosition.position;
        }
        else
        {
            Node current = head;
            while (current.next != null)
            {
                current = current.next;
            }

            // Use the position of the last node and offset by nodeSpacing along the marker's X-axis
            return current.transform.position + startPosition.right * nodeSpacing;
        }
    }
}
