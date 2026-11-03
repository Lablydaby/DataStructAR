using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

// AR queue as a circular buffer: items stay in their slots while FRONT and REAR move, and the rear
// wraps around to the first slot. So enqueue and dequeue each take one step, as the lesson says.
public class QueueManager : MonoBehaviour
{
    [Header("Prefabs and Settings")]
    public GameObject[] shapePrefabs; // Prefabs to enqueue
    public GameObject spacePrefab;    // Visual empty slot
    public Transform startPosition;
    public float elementSpacing = 2.0f;
    public float animationDuration = 1.0f;
    public float spawnYOffset = 2f; // Vertical spawn offset

    private GameObject[] elements;
    private int[] values;
    private TextMeshPro[] valueLabels;
    private GameObject[] spaces;
    private int queueCapacity = 5;
    private int front = 0;
    private int count = 0;
    private bool isBusy = false;
    private TextMeshPro frontLabel, rearLabel;

    public int Count => count;
    public int Capacity => queueCapacity;

    private const float LabelSize = 0.9f;

    void Start()
    {
        elements = new GameObject[queueCapacity];
        values = new int[queueCapacity];
        valueLabels = new TextMeshPro[queueCapacity];
        spaces = new GameObject[queueCapacity];
        InitializeSpaces();

        frontLabel = ARText.Label(transform, "FRONT", 0.55f, ARText.Ink, spaces[0].transform, startPosition, 0, -0.075f);
        rearLabel = ARText.Label(transform, "REAR", 0.55f, ARText.Ink, spaces[0].transform, startPosition, 0, -0.115f);
        UpdatePointers();
    }

    void InitializeSpaces()
    {
        for (int i = 0; i < queueCapacity; i++)
        {
            Vector3 pos = startPosition.position + startPosition.right * (i * elementSpacing);
            spaces[i] = Instantiate(spacePrefab, pos, startPosition.rotation, transform);
        }
    }

    private int Rear => (front + count - 1) % queueCapacity;

    public void Enqueue(int prefabIndex)
    {
        if (isBusy || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;

        // A full queue refuses new items (overflow); items only leave through Dequeue
        if (count >= queueCapacity)
        {
            ShowMessage("The queue is full. That's an overflow! Dequeue first to make room.");
            return;
        }

        StartCoroutine(EnqueueWithCooldown(prefabIndex));
    }


    IEnumerator EnqueueWithCooldown(int prefabIndex)
    {
        isBusy = true;

        int slot = (front + count) % queueCapacity;
        bool wrapped = count > 0 && slot < front;
        Vector3 targetPos = spaces[slot].transform.position;
        Quaternion targetRot = spaces[slot].transform.rotation;
        Vector3 spawnPos = targetPos + spaces[slot].transform.up * spawnYOffset;

        GameObject newElement = Instantiate(shapePrefabs[prefabIndex], spawnPos, targetRot, transform);
        elements[slot] = newElement;
        values[slot] = ARText.NewValue(CurrentValues());
        valueLabels[slot] = ARText.Label(transform, values[slot].ToString(), LabelSize, ARText.Ink, newElement.transform, startPosition, 0, 0.08f);

        yield return MoveToPosition(newElement.transform, targetPos, animationDuration);

        count++;
        UpdatePointers();
        if (count == 1)
            ARGuide.Explain($"Enqueued {values[slot]}. It's both the front and the rear.", 1);
        else
            ARGuide.Explain($"Enqueued {values[slot]} at the rear" + (wrapped ? $", wrapping around to slot {slot}" : "") +
                            $". Front is still {values[front]}.", 1);
        isBusy = false;
    }

    public void Dequeue()
    {
        if (isBusy)
            return;

        if (count == 0)
        {
            ShowMessage("The queue is empty. That's an underflow! Enqueue a shape first.");
            return;
        }

        StartCoroutine(DequeueFront());
    }

    // Only the front item leaves; nothing else moves, the FRONT pointer just advances
    IEnumerator DequeueFront()
    {
        isBusy = true;

        int slot = front;
        int value = values[slot];
        GameObject leaving = elements[slot];
        elements[slot] = null;
        Destroy(valueLabels[slot].gameObject);
        valueLabels[slot] = null;

        front = (front + 1) % queueCapacity;
        count--;
        UpdatePointers();
        ARGuide.Explain($"Dequeued {value} from the front. " +
                        (count > 0 ? $"{values[front]} is the new front; nothing else moved." : "The queue is empty now."), 1);

        Vector3 up = leaving.transform.position + spaces[slot].transform.up * spawnYOffset;
        yield return MoveToPosition(leaving.transform, up, animationDuration * 0.6f);
        Destroy(leaving);

        isBusy = false;
    }

    private List<int> CurrentValues()
    {
        List<int> taken = new List<int>();
        for (int i = 0; i < count; i++)
            taken.Add(values[(front + i) % queueCapacity]);
        return taken;
    }

    // FRONT and REAR sit under their slots; with one item both point at it
    private void UpdatePointers()
    {
        if (frontLabel == null)
            return;
        frontLabel.gameObject.SetActive(count > 0);
        rearLabel.gameObject.SetActive(count > 0);
        if (count == 0)
            return;
        ARText.Move(frontLabel, spaces[front].transform, 0, -0.075f);
        ARText.Move(rearLabel, spaces[Rear].transform, 0, count == 1 ? -0.115f : -0.075f);
    }

    private void ShowMessage(string message)
    {
        ARGuide guide = FindObjectOfType<ARGuide>();
        if (guide != null)
            guide.ShowMessage(message);
        else
            Debug.LogWarning(message);
    }

    IEnumerator MoveToPosition(Transform trans, Vector3 pos, float duration)
    {
        if (trans == null) yield break;

        Vector3 start = trans.position;
        float time = 0f;

        while (time < duration)
        {
            if (trans == null) yield break;

            trans.position = Vector3.Lerp(start, pos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        if (trans != null)
        {
            trans.position = pos;
        }
    }
}
