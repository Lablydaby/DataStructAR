using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// AR singly linked list. Adding or removing at the end has to walk the links from the head; inserting at
// the head or unlinking a node only changes one pointer. Captions count those steps.
public class LinkedListManager : MonoBehaviour
{
    public GameObject[] nodePrefabs; // Prefabs for different shapes and colors
    public Transform startPosition;
    public float nodeSpacing = 2.0f;
    public float slideDuration = 0.3f; // Nodes slide over on screen to keep the row tidy

    public GameObject nodeListParent; // Parent GameObject to encapsulate nodes
    private const int maxNodes = 5; // Maximum number of nodes allowed
    public int Count => nodes.Count;
    public int Capacity => maxNodes;

    public GameObject nullNodePrefab; // The visual representation of a null node
    private GameObject currentNullNode; // Keeps track of the current null node

    public GameObject headMarkerPrefab; // Visual indicator for head node
    private GameObject currentHeadMarker; // Tracks the spawned head marker

    [Header("Visual Offsets")]
    public Vector3 headOffset = new Vector3(0, 1f, 0);     // Offset for head marker
    public Vector3 nullOffset = Vector3.zero;             // Offset for null node

    // The list in order from the head; each node's next pointer matches it
    private readonly List<Node> nodes = new List<Node>();
    private readonly List<int> values = new List<int>();
    private readonly List<TextMeshPro> valueLabels = new List<TextMeshPro>();
    private bool isBusy;

    private const float ValueSize = 0.9f;
    private const float ValueAbove = 0.065f;

    void Start()
    {
        // Ensure startPosition is assigned
        if (startPosition == null)
            Debug.LogError("Start position is not assigned.");
        UpdateNullNodePosition();
    }

    public void AddNode(int prefabIndex)
    {
        if (!CanAdd(prefabIndex))
            return;
        int walked = Mathf.Max(nodes.Count - 1, 0);
        int last = nodes.Count > 0 ? values[nodes.Count - 1] : -1;
        int value = Spawn(nodes.Count, prefabIndex);
        Relink();
        ARGuide.Record("add", nodes.Count - 1);
        StartCoroutine(Layout(() =>
        {
            if (nodes.Count == 1)
                ARGuide.Explain($"Added {value}. It's the head and the only node.", 1);
            else
                ARGuide.Explain(walked == 0
                    ? $"Added {value} at the end: {last} is the head and the last node, so {last} now points to {value}."
                    : $"Added {value} at the end: walked {Links(walked)} from the head to the last node, {last}, then pointed {last} to {value}.", walked + 1);
        }));
    }

    public void InsertAtHead(int prefabIndex)
    {
        if (!CanAdd(prefabIndex))
            return;
        int oldHead = nodes.Count > 0 ? values[0] : -1;
        int value = Spawn(0, prefabIndex);
        Relink();
        ARGuide.Record("insert-head", 0);
        StartCoroutine(Layout(() =>
            ARGuide.Explain(oldHead < 0
                ? $"Inserted {value} at the head. It's the only node, pointing to null."
                : $"Inserted {value} at the head: it points to the old head ({oldHead}), and Head moves to it. The other nodes only slid over on screen.", 1)));
    }

    // Inserts so the new node becomes the node at `index` (0-based): 0 is the head, Count is the end
    public void InsertAt(int index, int prefabIndex)
    {
        if (index <= 0)
        {
            InsertAtHead(prefabIndex);
            return;
        }
        if (index >= nodes.Count)
        {
            AddNode(prefabIndex);
            return;
        }
        if (!CanAdd(prefabIndex))
            return;
        int before = values[index - 1], after = values[index];
        int value = Spawn(index, prefabIndex);
        Relink();
        ARGuide.Record("insert", index);
        StartCoroutine(Layout(() =>
            ARGuide.Explain(index == 1
                ? $"Inserted {value} at position 1: the node before it is the head, {before}, so {before} now points to {value} and {value} points to {after}."
                : $"Inserted {value} at position {index}: walked {Links(index - 1)} from the head to {before}, then pointed {before} to {value} and {value} to {after}.", index)));
    }

    public void RemoveLastNode()
    {
        if (isBusy)
            return;
        if (nodes.Count == 0)
        {
            ARGuide.Say("The list is empty.");
            return;
        }
        int walked = Mathf.Max(nodes.Count - 2, 0);
        bool only = nodes.Count == 1;
        int before = nodes.Count > 1 ? values[nodes.Count - 2] : -1;
        int value = Remove(nodes.Count - 1);
        ARGuide.Record("remove", nodes.Count);
        StartCoroutine(Layout(() =>
            ARGuide.Explain(only
                ? $"Removed {value}. The list is empty now: Head points to null."
                : walked == 0
                    ? $"Removed {value} from the end: the node before it is the head, {before}, so {before} now points to null."
                    : $"Removed {value} from the end: walked {Links(walked)} from the head to {before}, the node before it, and pointed {before} to null.", walked + 1)));
    }

    // index is 0-based from the head
    public void DeleteAt(int index)
    {
        if (isBusy)
            return;
        if (nodes.Count == 0)
        {
            ARGuide.Say("The list is empty.");
            return;
        }
        if (index >= nodes.Count)
        {
            ARGuide.Say($"Position {index} is empty. The last node is at position {nodes.Count - 1}.");
            return;
        }
        int before = index > 0 ? values[index - 1] : -1;
        int value = Remove(index);
        string next = index < nodes.Count ? values[index].ToString() : "null";
        ARGuide.Record("delete", index);
        StartCoroutine(Layout(() =>
            ARGuide.Explain(index == 0
                ? $"Deleted {value}, the head. Head now points to {next}. Nothing else changed."
                : index == 1
                    ? $"Deleted {value}: the node before it is the head, {before}, so {before} now points to {next}."
                    : $"Deleted {value}: walked {Links(index - 1)} from the head to {before}, the node before it, then pointed {before} to {next}.", Mathf.Max(index, 1))
        ));
    }

    private bool CanAdd(int prefabIndex)
    {
        if (isBusy)
            return false;
        if (nodes.Count >= maxNodes)
        {
            ARGuide.Say("The list is full.");
            return false;
        }
        if (prefabIndex < 0 || prefabIndex >= nodePrefabs.Length || nodePrefabs[prefabIndex] == null)
        {
            Debug.LogError("Prefab index out of bounds or not assigned: " + prefabIndex);
            return false;
        }
        return true;
    }

    // Creates a node at a list position; it appears at its final spot once the others have slid over
    private int Spawn(int index, int prefabIndex)
    {
        GameObject newNodeObj = Instantiate(nodePrefabs[prefabIndex], PositionAt(index), startPosition.rotation, nodeListParent.transform);
        Node node = newNodeObj.GetComponent<Node>();
        if (node == null)
            node = newNodeObj.AddComponent<Node>();
        int value = ARText.NewValue(values);
        node.SetData(value);
        nodes.Insert(index, node);
        values.Insert(index, value);
        valueLabels.Insert(index, ARText.Value(nodeListParent.transform, value.ToString(), ValueSize, node.transform, startPosition, 0, ValueAbove));
        return value;
    }

    private int Remove(int index)
    {
        int value = values[index];
        Destroy(nodes[index].gameObject);
        Destroy(valueLabels[index].gameObject);
        nodes.RemoveAt(index);
        values.RemoveAt(index);
        valueLabels.RemoveAt(index);
        Relink();
        return value;
    }

    private void Relink()
    {
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].SetNext(i + 1 < nodes.Count ? nodes[i + 1] : null);
    }

    private Vector3 PositionAt(int index) => startPosition.position + startPosition.right * (nodeSpacing * index);

    // Slides every node to its spot in the row, then moves the Head and Null labels and explains
    private IEnumerator Layout(System.Action explain)
    {
        isBusy = true;
        Vector3[] from = new Vector3[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
            from[i] = nodes[i].transform.position;
        for (float t = 0; t < slideDuration; t += Time.deltaTime)
        {
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].transform.position = Vector3.Lerp(from[i], PositionAt(i), t / slideDuration);
            yield return null;
        }
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].transform.position = PositionAt(i);

        UpdateNullNodePosition();
        UpdateHeadMarker();
        explain();
        isBusy = false;
    }

    private void UpdateNullNodePosition()
    {
        // Null sits one step past the last node, or at the start when the list is empty
        Transform follow = nodes.Count > 0 ? nodes[nodes.Count - 1].transform : startPosition;
        float along = (nodes.Count > 0 ? nodeSpacing : 0f) + nullOffset.x;
        if (currentNullNode == null)
        {
            currentNullNode = Instantiate(nullNodePrefab, PositionAt(nodes.Count) + startPosition.rotation * nullOffset, startPosition.rotation, nodeListParent.transform);
            AttachLabel(currentNullNode, follow, along, nullOffset.y);
        }
        else
        {
            ARLabel label = currentNullNode.GetComponent<ARLabel>();
            label.follow = follow;
            label.alongRow = along;
        }
    }

    private void UpdateHeadMarker()
    {
        if (nodes.Count == 0)
        {
            if (currentHeadMarker != null)
                currentHeadMarker.SetActive(false);
            return;
        }

        if (currentHeadMarker == null)
        {
            currentHeadMarker = Instantiate(headMarkerPrefab, nodes[0].transform.position + startPosition.rotation * headOffset, startPosition.rotation, nodeListParent.transform);
            AttachLabel(currentHeadMarker, nodes[0].transform, headOffset.x, headOffset.y);
        }
        currentHeadMarker.SetActive(true);
        currentHeadMarker.GetComponent<ARLabel>().follow = nodes[0].transform;
    }

    // Labels follow their node, sit above it whichever way the card faces, and turn toward the camera
    private void AttachLabel(GameObject label, Transform follow, float alongRow, float above)
    {
        // Same look as the other AR tags: white capitals on a dark indigo chip
        TextMeshPro text = label.GetComponentInChildren<TextMeshPro>();
        if (text != null)
        {
            text.font = UiKit.Font("Poppins-Bold SDF");
            text.outlineWidth = 0f;
            ARText.StyleTag(text);
        }

        ARLabel arLabel = label.AddComponent<ARLabel>();
        arLabel.follow = follow;
        arLabel.marker = startPosition;
        arLabel.alongRow = alongRow;
        arLabel.above = above;
    }

    private static string Links(int n) => n == 1 ? "1 link" : $"{n} links";
}
