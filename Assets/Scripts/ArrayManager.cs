using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// AR array: a fixed row of slots with their indexes. Add/Remove work at the end in one step; Insert at
// and Delete at shift the elements after that index one slot at a time, so the O(n) cost is visible.
public class ArrayManager : MonoBehaviour
{
    [Header("Prefabs & Settings")]
    public GameObject[] shapePrefabs;
    public GameObject spacePrefab;
    public Transform startPosition;
    public float elementSpacing = 2.0f;
    public float spaceSpacing = 2.0f;
    public float animationDuration = 0.5f;
    public float spawnYOffset = 2f;
    public int maxNodes = 5;
    public Vector3 elementScale = Vector3.one; // Manual scale adjustment
    public float shiftDuration = 0.3f; // Per element, when inserting or deleting in the middle

    private List<GameObject> elements = new List<GameObject>();
    private List<int> values = new List<int>();
    private List<TextMeshPro> valueLabels = new List<TextMeshPro>();
    private List<GameObject> spaces = new List<GameObject>();
    private readonly List<TextMeshPro> indexLabels = new List<TextMeshPro>();
    private int selectedSlot = -1;

    // The slot the position stepper points at: its index turns indigo
    public void SelectSlot(int slot)
    {
        if (slot == selectedSlot || indexLabels.Count == 0)
            return;
        selectedSlot = slot;
        for (int i = 0; i < indexLabels.Count; i++)
        {
            // The selected index is white on indigo; the rest are red on white
            indexLabels[i].color = i == slot ? Color.white : ARText.IndexRed;
            ARText.Chip(indexLabels[i], i == slot ? ARText.Ink : Color.white);
        }
    }
    private bool isBusy = false;

    private const float ValueSize = 0.9f;
    private const float ValueAbove = 0.085f;

    public int Count => elements.Count;
    public int Capacity => maxNodes;

    void Start()
    {
        SpawnAllSpaces();
    }

    private void SpawnAllSpaces()
    {
        for (int i = 0; i < maxNodes; i++)
        {
            Vector3 pos = GetSpacePositionAtIndex(i);
            GameObject space = Instantiate(spacePrefab, pos, startPosition.rotation, transform);
            spaces.Add(space);
            // Red index numbers under the slots, like the lesson's diagrams
            indexLabels.Add(ARText.Label(transform, i.ToString(), 0.65f, ARText.IndexRed, space.transform, startPosition, 0, -0.085f));
            ARText.Chip(indexLabels[indexLabels.Count - 1], Color.white);
        }
    }

    public void AddElement(int prefabIndex)
    {
        if (!isBusy && Count >= maxNodes)
            ARGuide.Say("The array is full.");
        if (isBusy || Count >= maxNodes || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;

        StartCoroutine(InsertRoutine(Count, prefabIndex, false));
    }

    public void RemoveLastElement()
    {
        if (!isBusy && Count == 0)
            ARGuide.Say("The array is empty.");
        if (isBusy || Count == 0)
            return;

        StartCoroutine(DeleteRoutine(Count - 1, false));
    }

    public void InsertAt(int index, int prefabIndex)
    {
        if (isBusy || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;
        if (Count >= maxNodes)
        {
            ARGuide.Say("The array is full, so nothing can shift to make room.");
            return;
        }
        // Arrays are contiguous: the new element can go at most right after the last one
        if (index > Count)
        {
            ARGuide.Say($"Arrays can't have gaps. The next free index is {Count}.");
            return;
        }
        StartCoroutine(InsertRoutine(index, prefabIndex, true));
    }

    public void DeleteAt(int index)
    {
        if (isBusy)
            return;
        if (index >= Count)
        {
            ARGuide.Say(Count == 0 ? "The array is empty." : $"Index {index} is empty. The last element is at index {Count - 1}.");
            return;
        }
        StartCoroutine(DeleteRoutine(index, true));
    }

    private IEnumerator InsertRoutine(int index, int prefabIndex, bool positional)
    {
        isBusy = true;

        // Make room: move elements right one at a time, starting from the end
        int shifted = 0;
        for (int k = Count - 1; k >= index; k--)
        {
            yield return MoveElement(k, k + 1);
            shifted++;
        }

        GameObject targetSpace = spaces[index];
        Vector3 targetPos = targetSpace.transform.position;
        GameObject element = Instantiate(shapePrefabs[prefabIndex], targetPos + targetSpace.transform.up * spawnYOffset, targetSpace.transform.rotation);
        element.transform.SetParent(targetSpace.transform, true);
        element.transform.localScale = elementScale;

        int value = ARText.NewValue(values);
        elements.Insert(index, element);
        values.Insert(index, value);
        valueLabels.Insert(index, ARText.Value(transform, value.ToString(), ValueSize, element.transform, startPosition, 0, ValueAbove));

        yield return StartCoroutine(MoveToPosition(element.transform, targetPos, animationDuration));

        ARGuide.Record(positional && shifted > 0 ? "insert" : "add", index);
        if (!positional || shifted == 0)
            ARGuide.Explain($"Added {value} at index {index}, the next free slot. Nothing had to shift.", 1);
        else
            ARGuide.Explain($"Inserted {value} at index {index}. {Elements(shifted)} shifted right to make room first.", shifted + 1);
        isBusy = false;
    }

    private IEnumerator DeleteRoutine(int index, bool positional)
    {
        isBusy = true;

        int value = values[index];
        Destroy(elements[index]);
        Destroy(valueLabels[index].gameObject);
        elements.RemoveAt(index);
        values.RemoveAt(index);
        valueLabels.RemoveAt(index);

        // Close the gap: move the later elements left one at a time
        int shifted = 0;
        for (int k = index; k < Count; k++)
        {
            yield return MoveElement(k, k);
            shifted++;
        }

        ARGuide.Record(positional && shifted > 0 ? "delete" : "remove", index);
        if (!positional || shifted == 0)
            ARGuide.Explain($"Removed {value} from index {index}, the last element. Nothing had to shift.", 1);
        else
            ARGuide.Explain($"Deleted {value} from index {index}. {Elements(shifted)} shifted left to close the gap.", shifted + 1);
        isBusy = false;
    }

    // Slides the element now at list position `from` into slot `toSlot`
    private IEnumerator MoveElement(int from, int toSlot)
    {
        Transform element = elements[from].transform;
        GameObject space = spaces[toSlot];
        yield return StartCoroutine(MoveToPosition(element, space.transform.position, shiftDuration));
        element.SetParent(space.transform, true);
    }

    private static string Elements(int n) => n == 1 ? "1 element" : $"{n} elements";

    Vector3 GetSpacePositionAtIndex(int index)
    {
        return startPosition.position + startPosition.right * (index * spaceSpacing);
    }

    IEnumerator MoveToPosition(Transform target, Vector3 end, float timeToMove)
    {
        Vector3 start = target.position;
        float elapsedTime = 0f;

        while (elapsedTime < timeToMove)
        {
            target.position = Vector3.Lerp(start, end, elapsedTime / timeToMove);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        target.position = end;
    }
}
