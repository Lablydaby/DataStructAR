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
            Vector3 pos = startPosition.position + new Vector3(i * elementSpacing, 0, 0);
            spaces[i] = Instantiate(spacePrefab, pos, Quaternion.identity, transform);
        }
    }

    public void Enqueue(int prefabIndex)
    {
        if (isBusy || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;

        if (rear >= queueCapacity)
        {
            StartCoroutine(ForceEnqueueWithDequeue(prefabIndex));
        }
        else
        {
            StartCoroutine(EnqueueWithCooldown(prefabIndex));
        }
    }

    IEnumerator ForceEnqueueWithDequeue(int prefabIndex)
    {
        isBusy = true;

        yield return StartCoroutine(DequeueWithShift()); // Wait for Dequeue to complete

        yield return StartCoroutine(EnqueueWithCooldown(prefabIndex)); // Now enqueue

        isBusy = false;
    }


    IEnumerator EnqueueWithCooldown(int prefabIndex)
    {
        isBusy = true;

        Vector3 targetPos = spaces[rear].transform.position;
        Quaternion targetRot = spaces[rear].transform.rotation;
        Vector3 spawnPos = targetPos + new Vector3(0, spawnYOffset, 0);

        GameObject newElement = Instantiate(shapePrefabs[prefabIndex], spawnPos, targetRot, transform);
        elements[rear] = newElement;

        yield return MoveToPosition(newElement.transform, targetPos, animationDuration);

        rear++;
        isBusy = false;
    }

    public void Dequeue()
    {
        if (rear == 0 || isBusy)
        {
            Debug.LogWarning("Queue is empty or busy.");
            return;
        }

        StartCoroutine(DequeueWithShift());
    }

    IEnumerator DequeueWithShift()
    {
        isBusy = true;

        Destroy(elements[0]);

        for (int i = 1; i < rear; i++)
        {
            elements[i - 1] = elements[i];
            Vector3 newPos = spaces[i - 1].transform.position;
            yield return MoveToPosition(elements[i - 1].transform, newPos, animationDuration);
        }

        rear--;
        elements[rear] = null;
        isBusy = false;
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
