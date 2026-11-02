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

    // Final resting positions of each stacked object, local to the marker anchor
    private readonly List<Vector3> targetLocalPositions = new List<Vector3>();

    // Objects live under the image target so they follow it and hide when tracking is lost
    private Transform Anchor => startPos.parent != null ? startPos.parent : parent;


    void Start()
    {
        // Spawn the initial stack
        StartCoroutine(SpawnInitialStack());
    }

    IEnumerator SpawnInitialStack()
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 3; i++)
        {
            // Wait out the push cooldown so each initial push goes through
            yield return new WaitWhile(() => isPushOnCooldown);
            Push(i);
        }
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

        GameObject newObject = Instantiate(stackPrefabs[selectedIndex], startPos.position, startPos.rotation, parent);
        newObject.transform.SetParent(Anchor, true); // Keep world size, but follow the marker

        Vector3 spawnPosition = GetNextSpawnPosition(newObject.transform);
        newObject.transform.position = spawnPosition + startPos.up * spawnHeightOffset; // Spawn above the target position

        tempObject = newObject.transform;
        stackObjects.Add(newObject);

        Vector3 targetLocal = Anchor.InverseTransformPoint(spawnPosition);
        targetLocalPositions.Add(targetLocal);
        StartCoroutine(AnimateMove(newObject, targetLocal));

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
        targetLocalPositions.RemoveAt(targetLocalPositions.Count - 1);

        Vector3 targetPosition = topObject.transform.position + startPos.up * spawnHeightOffset; // Move up before destroying
        StartCoroutine(AnimateMoveAndDestroy(topObject, Anchor.InverseTransformPoint(targetPosition)));

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


    private Vector3 GetNextSpawnPosition(Transform newObject)
    {
        // World-space heights, so scaled parents don't skew the spacing
        float newHalfHeight = newObject.lossyScale.y / 2;

        if (stackObjects.Count == 0)
        {
            // Return the initial position for the first object
            return startPos.position + startPos.up * newHalfHeight;
        }
        else
        {
            // Stack on the top object's resting spot, even if it is still animating
            Vector3 topPosition = Anchor.TransformPoint(targetLocalPositions[targetLocalPositions.Count - 1]);
            float topHalfHeight = tempObject.lossyScale.y / 2;
            return topPosition + startPos.up * (topHalfHeight + stackSpacing + newHalfHeight);
        }
    }

    // Animates in the anchor's local space so the motion stays attached to the marker
    private IEnumerator AnimateMove(GameObject obj, Vector3 targetLocalPosition)
    {
        Vector3 startPosition = obj.transform.localPosition;
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            obj.transform.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.transform.localPosition = targetLocalPosition;
    }

    private IEnumerator AnimateMoveAndDestroy(GameObject obj, Vector3 targetPosition)
    {
        yield return AnimateMove(obj, targetPosition);
        Destroy(obj);
    }
}
