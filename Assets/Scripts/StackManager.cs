using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

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

    // Objects still sliding into place; the rest sit at their resting spot
    private readonly HashSet<GameObject> moving = new HashSet<GameObject>();

    // Direction the stack grows, local to the marker anchor. An image target's own up is the card's
    // normal, which only points up when the card lies on a table; on a wall or monitor the card's top
    // edge points up instead, so this follows whichever marker axis is closest to real-world up.
    private Vector3 stackAxisLocal;

    // Objects live under the image target so they follow it and hide when tracking is lost
    private Transform Anchor => startPos.parent != null ? startPos.parent : parent;
    private Vector3 StackUp => Anchor.TransformDirection(stackAxisLocal).normalized;
    private Quaternion StackRotation => Quaternion.FromToRotation(startPos.up, StackUp) * startPos.rotation;


    void Start()
    {
        stackAxisLocal = Anchor.InverseTransformDirection(startPos.up);

        // Spawn the initial stack
        StartCoroutine(SpawnInitialStack());
    }

    void Update()
    {
        if (!IsMarkerTracked())
            return;

        Vector3 up = WorldUp();
        Vector3 best = StackUp;
        foreach (Vector3 axis in new[] { startPos.up, startPos.forward, -startPos.forward, startPos.right, -startPos.right })
        {
            if (Vector3.Dot(axis, up) > Vector3.Dot(best, up))
                best = axis;
        }

        // Some slack so the stack doesn't flip back and forth when the card is tilted halfway
        if (Vector3.Dot(best, up) > Vector3.Dot(StackUp, up) + 0.2f)
        {
            stackAxisLocal = Anchor.InverseTransformDirection(best);
            for (int i = 0; i < stackObjects.Count; i++)
            {
                stackObjects[i].transform.rotation = StackRotation;
                if (!moving.Contains(stackObjects[i]))
                    stackObjects[i].transform.localPosition = RestLocalPosition(i);
            }
        }
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

        GameObject newObject = Instantiate(stackPrefabs[selectedIndex], startPos.position, StackRotation, parent);
        newObject.transform.SetParent(Anchor, true); // Keep world size, but follow the marker

        tempObject = newObject.transform;
        stackObjects.Add(newObject);

        // Spawn above the resting spot
        Vector3 restWorld = Anchor.TransformPoint(RestLocalPosition(stackObjects.Count - 1));
        newObject.transform.position = restWorld + StackUp * spawnHeightOffset;
        StartCoroutine(AnimateIntoStack(newObject));

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
        moving.Add(topObject);

        Vector3 targetPosition = topObject.transform.position + StackUp * spawnHeightOffset; // Move up before destroying
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


    // Resting spot of the object at this index, local to the anchor
    private Vector3 RestLocalPosition(int index)
    {
        // World-space heights, so scaled parents don't skew the spacing
        float height = 0f;
        for (int i = 0; i < index; i++)
            height += stackObjects[i].transform.lossyScale.y + stackSpacing;
        height += stackObjects[index].transform.lossyScale.y / 2;

        return Anchor.InverseTransformPoint(startPos.position + StackUp * height);
    }

    private bool IsMarkerTracked()
    {
        ObserverBehaviour observer = Anchor.GetComponent<ObserverBehaviour>();
        if (observer == null)
            return true;
        Status status = observer.TargetStatus.Status;
        return status == Status.TRACKED || status == Status.EXTENDED_TRACKED;
    }

    // Real-world up from the phone's gravity sensor, in world space.
    // Falls back to the camera's up where there is no accelerometer, e.g. in the editor.
    private static Vector3 WorldUp()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return Vector3.up;

        // Already adjusted to the screen orientation (Input.compensateSensors); +z is out of the screen
        Vector3 gravity = Input.acceleration;
        if (gravity.sqrMagnitude < 0.01f)
            return camera.transform.up;

        return -camera.transform.TransformDirection(new Vector3(gravity.x, gravity.y, -gravity.z)).normalized;
    }

    // Slides a pushed object onto its resting spot, re-read each frame in case the stack turns
    private IEnumerator AnimateIntoStack(GameObject obj)
    {
        moving.Add(obj);
        Vector3 startPosition = obj.transform.localPosition;
        float elapsedTime = 0;

        while (elapsedTime < animationDuration)
        {
            int index = stackObjects.IndexOf(obj);
            if (index < 0)
                yield break; // Popped mid-animation
            obj.transform.localPosition = Vector3.Lerp(startPosition, RestLocalPosition(index), elapsedTime / animationDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        moving.Remove(obj);
        int finalIndex = stackObjects.IndexOf(obj);
        if (finalIndex >= 0)
            obj.transform.localPosition = RestLocalPosition(finalIndex);
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
        moving.Remove(obj);
        Destroy(obj);
    }
}
