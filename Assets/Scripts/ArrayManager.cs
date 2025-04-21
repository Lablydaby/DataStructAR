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
    public float animationDuration = 0.5f;
    public float spawnYOffset = 2f;
    public int maxNodes = 5;

    private List<GameObject> elements = new List<GameObject>();
    private List<GameObject> spaces = new List<GameObject>();
    private int currentIndex = 0;
    private bool isBusy = false;

    void Start()
    {
        AddSpaceAt(currentIndex); // Always show the initial space
    }

    public void AddElement(int prefabIndex)
    {
        if (isBusy || currentIndex >= maxNodes || prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
            return;

        StartCoroutine(SpawnElementWithSpaceCheck(prefabIndex));
    }

    IEnumerator SpawnElementWithSpaceCheck(int prefabIndex)
    {
        isBusy = true;

        Vector3 elementPos = GetPositionAtIndex(currentIndex);
        GameObject element = Instantiate(shapePrefabs[prefabIndex], elementPos + Vector3.up * spawnYOffset, Quaternion.identity, transform);
        elements.Add(element);
        yield return StartCoroutine(MoveToPosition(element.transform, elementPos, animationDuration));

        currentIndex++;

        // If there's room and the next space doesn't exist, create it
        if (currentIndex < maxNodes)
        {
            if (spaces.Count <= currentIndex || spaces[currentIndex] == null)
            {
                Vector3 spacePos = GetPositionAtIndex(currentIndex);
                GameObject space = Instantiate(spacePrefab, spacePos + Vector3.up * spawnYOffset, Quaternion.identity, transform);

                if (spaces.Count <= currentIndex)
                    spaces.Add(space);
                else
                    spaces[currentIndex] = space;

                yield return StartCoroutine(MoveToPosition(space.transform, spacePos, animationDuration));
            }
        }

        isBusy = false;
    }

    public void RemoveLastElement()
    {
        if (isBusy || currentIndex == 0)
            return;

        int lastIndex = currentIndex - 1;

        // If there’s an element at the last index, remove it
        if (lastIndex < elements.Count && elements[lastIndex] != null)
        {
            Destroy(elements[lastIndex]);
            elements.RemoveAt(lastIndex);
            currentIndex--;
            return;
        }

        // No element found, try to remove the corresponding space (but not index 0)
        if (currentIndex < spaces.Count && currentIndex > 0 && spaces[currentIndex] != null)
        {
            Destroy(spaces[currentIndex]);
            spaces[currentIndex] = null;
            currentIndex--;
        }
    }

    void AddSpaceAt(int index)
    {
        Vector3 pos = GetPositionAtIndex(index);
        GameObject space = Instantiate(spacePrefab, pos, Quaternion.identity, transform);

        if (spaces.Count <= index)
            spaces.Add(space);
        else
            spaces[index] = space;
    }

    Vector3 GetPositionAtIndex(int index)
    {
        return startPosition.position + new Vector3(index * elementSpacing, 0f, 0f);
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
