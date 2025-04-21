using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArrayListManager : MonoBehaviour
{
    public GameObject[] shapePrefabs; // Array of different shapes
    public GameObject spacePrefab; // Prefab for the transparent green cubes representing spaces
    public Transform startPosition;
    public float elementSpacing = 2.0f;
    public float animationDuration = 1.0f;
    public float newArraySpawnDistance = 2.0f; // Distance at which the new array spawns above the current array

    private List<GameObject> elements = new List<GameObject>();
    private List<GameObject> spaces = new List<GameObject>();
    private int arrayCapacity = 4; // Initial capacity of the array
    private bool isResizing = false; // Flag to prevent multiple resize coroutines

    void Start()
    {
        InitializeSpaces();
        InitializeShapes();
    }

    void InitializeSpaces()
    {
        for (int i = 0; i < arrayCapacity; i++)
        {
            Vector3 position = startPosition.position + new Vector3(i * elementSpacing, 0, 0);
            GameObject space = Instantiate(spacePrefab, position, Quaternion.identity, transform);
            spaces.Add(space);
        }
    }

    void InitializeShapes()
    {
        for (int i = 0; i < shapePrefabs.Length; i++)
        {
            AddElement(i);
        }
    }

    public void AddElement(int prefabIndex)
    {
        if (isResizing) return;

        // Check if the prefab index is within bounds
        if (prefabIndex < 0 || prefabIndex >= shapePrefabs.Length)
        {
            Debug.LogError("Prefab index out of bounds: " + prefabIndex);
            return;
        }

        // Instantiate the new element object
        Vector3 position = startPosition.position + new Vector3(elements.Count * elementSpacing, 0, 0);
        GameObject newElement = Instantiate(shapePrefabs[prefabIndex], position, Quaternion.identity, transform);
        elements.Add(newElement);

        if (elements.Count > arrayCapacity)
        {
            StartCoroutine(ResizeArray());
        }
    }

    public void RemoveElement()
    {
        if (elements.Count == 0 || isResizing) return;

        GameObject elementToRemove = elements[elements.Count - 1];
        elements.RemoveAt(elements.Count - 1);
        Destroy(elementToRemove);

        UpdateElementPositions();
    }

    IEnumerator ResizeArray()
    {
        isResizing = true;

        int newCapacity = arrayCapacity * 2;
        List<GameObject> newSpaces = new List<GameObject>();
        List<GameObject> newElements = new List<GameObject>();

        Vector3 newArrayStartPosition = startPosition.position + new Vector3(0, newArraySpawnDistance, 0); // Position the new array above the current one

        // Create new spaces
        for (int i = 0; i < newCapacity; i++)
        {
            Vector3 position = newArrayStartPosition + new Vector3(i * elementSpacing, 0, 0);
            GameObject space = Instantiate(spacePrefab, position, Quaternion.identity, transform);
            newSpaces.Add(space);
        }

        // Animate moving elements to new spaces
        for (int i = 0; i < elements.Count; i++)
        {
            GameObject element = elements[i];
            Vector3 newPosition = newArrayStartPosition + new Vector3(i * elementSpacing, 0, 0);
            StartCoroutine(MoveToPosition(element.transform, newPosition, animationDuration));
            newElements.Add(element);
        }

        yield return new WaitForSeconds(animationDuration);

        // Destroy old spaces
        foreach (GameObject space in spaces)
        {
            Destroy(space);
        }

        // Move new array down to original position
        for (int i = 0; i < newElements.Count; i++)
        {
            GameObject element = newElements[i];
            Vector3 finalPosition = startPosition.position + new Vector3(i * elementSpacing, 0, 0);
            StartCoroutine(MoveToPosition(element.transform, finalPosition, animationDuration));
        }

        foreach (GameObject space in newSpaces)
        {
            Vector3 finalPosition = startPosition.position + new Vector3(newSpaces.IndexOf(space) * elementSpacing, 0, 0);
            StartCoroutine(MoveToPosition(space.transform, finalPosition, animationDuration));
        }

        yield return new WaitForSeconds(animationDuration);

        // Update references
        elements = newElements;
        spaces = newSpaces;
        arrayCapacity = newCapacity;

        isResizing = false;
    }

    IEnumerator MoveToPosition(Transform transform, Vector3 position, float timeToMove)
    {
        Vector3 currentPos = transform.position;
        float elapsedTime = 0;

        while (elapsedTime < timeToMove)
        {
            transform.position = Vector3.Lerp(currentPos, position, (elapsedTime / timeToMove));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = position;
    }

    void UpdateElementPositions()
    {
        for (int i = 0; i < elements.Count; i++)
        {
            Vector3 position = startPosition.position + new Vector3(i * elementSpacing, 0, 0);
            elements[i].transform.position = position;
        }
    }
}
