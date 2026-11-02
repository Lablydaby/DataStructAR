using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    private List<GameObject> elements = new List<GameObject>();
    private List<GameObject> spaces = new List<GameObject>();
    private int currentIndex = 0;
    private bool isBusy = false;

    void Start()
    {
        SpawnAllSpaces();
    }

    private void SpawnAllSpaces()
    {
        for (int i = 0; i < maxNodes; i++)
        {
            Vector3 pos = GetSpacePositionAtIndex(i);
            GameObject space = Instantiate(spacePrefab, pos, Quaternion.identity, transform);
            spaces.Add(space);
        }
    }

    public void AddElement(int prefabIndex)
    {
        if (isBusy || currentIndex >= maxNodes || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;

        StartCoroutine(SpawnElementWithAnimation(prefabIndex));
    }

    IEnumerator SpawnElementWithAnimation(int prefabIndex)
    {
        isBusy = true;

        GameObject targetSpace = spaces[currentIndex];
        Vector3 targetPos = targetSpace.transform.position;

        GameObject element = Instantiate(shapePrefabs[prefabIndex], targetPos + Vector3.up * spawnYOffset, Quaternion.identity);
        element.transform.SetParent(targetSpace.transform, true);

        // Apply custom scale
        element.transform.localScale = elementScale;

        elements.Add(element);

        // Move to center of space
        yield return StartCoroutine(MoveToPosition(element.transform, targetPos, animationDuration));

        currentIndex++;
        isBusy = false;
    }

    public void RemoveLastElement()
    {
        if (isBusy || currentIndex == 0)
            return;

        int lastIndex = currentIndex - 1;

        if (lastIndex < elements.Count && elements[lastIndex] != null)
        {
            Destroy(elements[lastIndex]);
            elements.RemoveAt(lastIndex);
            currentIndex--;
        }
    }

    Vector3 GetSpacePositionAtIndex(int index)
    {
        return startPosition.position + new Vector3(index * spaceSpacing, 0f, 0f);
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
