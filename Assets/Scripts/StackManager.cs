using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackManager : MonoBehaviour
{
    public GameObject[] stackPrefabs;
    public float stackSpacing = 0.1f;
    public float animationDuration = 0.5f;
    public float spawnHeightOffset = 5.0f; // Height above the stack to spawn objects from
    public List<GameObject> stackObjects = new List<GameObject>();
    public Transform startPos;
    public Transform tempObject;
    public Transform parent;

    public float pushCooldown = 0.5f; // Cooldown duration in seconds
    private bool isPushOnCooldown = false;
    public float popCooldown = 0.5f; // Cooldown duration for Pop
    private bool isPopOnCooldown = false;


    void Start()
    {
        // Spawn the initial stack
        StartCoroutine(SpawnStack(0));
        StartCoroutine(SpawnStack(1));
        StartCoroutine(SpawnStack(2));
    }

    IEnumerator SpawnStack(int index)
    {
        yield return new WaitForSeconds(0.5f);

        Push(index);
    }

    public void Push(int selectedIndex)
    {
        if (isPushOnCooldown)
        {
            Debug.LogWarning("Push is on cooldown.");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= stackPrefabs.Length)
        {
            Debug.LogError("Invalid index selected.");
            return;
        }

        if (stackObjects.Count >= 5)
        {
            return; // Limit the stack to 5 objects
        }

        StartCoroutine(PushWithCooldown(selectedIndex));
    }

    private IEnumerator PushWithCooldown(int selectedIndex)
    {
        isPushOnCooldown = true;

        Vector3 spawnPosition = GetNextSpawnPosition();
        Vector3 initialPosition = spawnPosition + new Vector3(0, spawnHeightOffset, 0); // Spawn above the target position
        GameObject newObject = Instantiate(stackPrefabs[selectedIndex], initialPosition, Quaternion.identity,parent);
        if (stackObjects.Count <= 5)
        {
            tempObject = newObject.transform;
        }
        stackObjects.Add(newObject);
        StartCoroutine(AnimateMove(newObject, spawnPosition));

        yield return new WaitForSeconds(pushCooldown);
        isPushOnCooldown = false;

    }

    public void Pop()
    {
        if (isPopOnCooldown)
        {
            Debug.LogWarning("Pop is on cooldown.");
            return;
        }

        if (stackObjects.Count > 0)
        {
            StartCoroutine(PopWithCooldown());
        }
    }

    private IEnumerator PopWithCooldown()
    {
        isPopOnCooldown = true;

        GameObject topObject = stackObjects[stackObjects.Count - 1];
        stackObjects.RemoveAt(stackObjects.Count - 1);

        Vector3 targetPosition = topObject.transform.position + new Vector3(0, spawnHeightOffset, 0); // Move up before destroying
        StartCoroutine(AnimateMoveAndDestroy(topObject, targetPosition));

        if (stackObjects.Count <= 0)
        {
            tempObject = null;

        }
        else
        {
            tempObject = stackObjects[stackObjects.Count - 1].transform;
        }

        yield return new WaitForSeconds(popCooldown);
        isPopOnCooldown = false;
    }


    private Vector3 GetNextSpawnPosition()
    {

        if (stackObjects.Count == 0)
        {

            // Return the initial position for the first object
            return startPos.position + new Vector3(0, stackPrefabs[0].transform.localScale.y / 2, 0);

        }
        else
        {
            //GameObject topObject = stackObjects[stackObjects.Count - 1];
            //float objectHeight = topObject.transform.localScale.y;
            //return topObject.transform.position + new Vector3(0, objectHeight + stackSpacing, 0);


            GameObject topObject = tempObject.gameObject;
            float objectHeight = topObject.transform.localScale.y;
            //tempObject = topObject.transform;
            return topObject.transform.position + new Vector3(0, objectHeight + stackSpacing, 0);
        }
    }

    private IEnumerator AnimateMove(GameObject obj, Vector3 targetPosition)
    {
        Vector3 startPosition = obj.transform.position;
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            obj.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.transform.position = targetPosition;
    }

    private IEnumerator AnimateMoveAndDestroy(GameObject obj, Vector3 targetPosition)
    {
        yield return AnimateMove(obj, targetPosition);
        Destroy(obj);
    }
}
