using UnityEngine;
using System.Collections;

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
    private GameObject[] spaces;
    private int queueCapacity = 5;
    private int front = 0;
    private int rear = 0;
    private bool isBusy = false;

    void Start()
    {
        elements = new GameObject[queueCapacity];
        spaces = new GameObject[queueCapacity];
        InitializeSpaces();
    }

    void InitializeSpaces()
    {
        for (int i = 0; i < queueCapacity; i++)
        {
            Vector3 pos = startPosition.position + startPosition.right * (i * elementSpacing);
            spaces[i] = Instantiate(spacePrefab, pos, startPosition.rotation, transform);
        }
    }

    public void Enqueue(int prefabIndex)
    {
        if (isBusy || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;

        // A full queue refuses new items (overflow); items only leave through Dequeue
        if (rear >= queueCapacity)
        {
            ShowMessage("The queue is full. That's an overflow! Dequeue first to make room.");
            return;
        }

        StartCoroutine(EnqueueWithCooldown(prefabIndex));
    }


    IEnumerator EnqueueWithCooldown(int prefabIndex)
    {
        isBusy = true;

        Vector3 targetPos = spaces[rear].transform.position;
        Quaternion targetRot = spaces[rear].transform.rotation;
        Vector3 spawnPos = targetPos + spaces[rear].transform.up * spawnYOffset;

        GameObject newElement = Instantiate(shapePrefabs[prefabIndex], spawnPos, targetRot, transform);
        elements[rear] = newElement;

        yield return MoveToPosition(newElement.transform, targetPos, animationDuration);

        rear++;
        isBusy = false;
    }

    public void Dequeue()
    {
        if (isBusy)
            return;

        if (rear == 0)
        {
            ShowMessage("The queue is empty. That's an underflow! Enqueue a shape first.");
            return;
        }

        StartCoroutine(DequeueWithShift());
    }

    IEnumerator DequeueWithShift()
    {
        isBusy = true;

        Destroy(elements[0]);

        // Every remaining item steps forward one slot at the same time
        for (int i = 1; i < rear; i++)
        {
            elements[i - 1] = elements[i];
            Vector3 newPos = spaces[i - 1].transform.position;
            StartCoroutine(MoveToPosition(elements[i - 1].transform, newPos, animationDuration));
        }
        yield return new WaitForSeconds(animationDuration);

        rear--;
        elements[rear] = null;
        isBusy = false;
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
