using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StackManager : MonoBehaviour
{
    public GameObject[] stackPrefabs;
    public float stackSpacing = 0.1f;
    public float animationDuration = 0.5f;
    public float spawnHeightOffset = 5.0f; // Height above the stack to spawn objects from
    public List<GameObject> stackObjects = new List<GameObject>();
    public int Count => stackObjects.Count;
    public int Capacity => 5;
    public Transform startPos;
    public Transform tempObject;
    public Transform parent;

    public float pushCooldown = 0.5f; // Cooldown duration in seconds
    private bool isPushOnCooldown = false;
    public float popCooldown = 0.5f; // Cooldown duration for Pop
    private bool isPopOnCooldown = false;

    // Each block's value and the TOP marker, shown beside the blocks
    private readonly List<int> values = new List<int>();
    private readonly List<TextMeshPro> valueLabels = new List<TextMeshPro>();
    private TextMeshPro topLabel;
    private const float LabelSide = 0.09f;

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


    private bool initialStackDone;
    private const int StartingBlocks = 0; // The stack starts empty; students push the first block

    void Awake()
    {
        stackAxisLocal = Anchor.InverseTransformDirection(startPos.up);
    }

    // The Stack scene switches this object off whenever the marker is lost, which stops every running
    // animation and cooldown. Finish them instantly when it comes back, so Push and Pop keep working.
    void OnEnable()
    {
        isPushOnCooldown = false;
        isPopOnCooldown = false;

        foreach (GameObject obj in moving)
        {
            if (obj != null && !stackObjects.Contains(obj))
                Destroy(obj); // Was being popped
        }
        moving.Clear();
        for (int i = 0; i < stackObjects.Count; i++)
            stackObjects[i].transform.localPosition = RestLocalPosition(i);

        // Spawn the initial stack, or finish it if tracking was lost partway
        if (!initialStackDone)
            StartCoroutine(SpawnInitialStack());
    }

    // While the marker is lost the stack is hidden, so it simply re-picks its axis once tracking resumes
    void Update()
    {
        Vector3 best = ARUp.ClosestMarkerAxis(startPos, StackUp);
        if (best != StackUp)
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

        while (stackObjects.Count < StartingBlocks)
        {
            // Wait out the push cooldown so each initial push goes through
            yield return new WaitWhile(() => isPushOnCooldown);
            Push(stackObjects.Count);
        }
        initialStackDone = true;
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
            ARGuide.Say("Stack Overflow!");
            ARGuide.Record("overflow");
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
        int value = ARText.NewValue(values);
        values.Add(value);
        valueLabels.Add(ARText.Value(Anchor, value.ToString(), 0.9f, newObject.transform, startPos, LabelSide, 0));
        UpdateTopLabel();
        // The three starting blocks arrive on their own; only the student's pushes are explained
        if (initialStackDone)
            ARGuide.Record("push");
        if (initialStackDone)
            ARGuide.Explain($"Pushed {value}. It's the new top; nothing below it moved.", 1);

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
        else
        {
            ARGuide.Say("Stack Underflow!");
            ARGuide.Record("underflow");
        }
    }

    private IEnumerator PopWithCooldown()
    {
        isPopOnCooldown = true;

        GameObject topObject = stackObjects[stackObjects.Count - 1];
        stackObjects.RemoveAt(stackObjects.Count - 1);
        moving.Add(topObject);
        int value = values[values.Count - 1];
        values.RemoveAt(values.Count - 1);
        Destroy(valueLabels[valueLabels.Count - 1].gameObject);
        valueLabels.RemoveAt(valueLabels.Count - 1);
        UpdateTopLabel();
        ARGuide.Record("pop");
        ARGuide.Explain($"Popped {value} off the top. " + (values.Count > 0 ? $"{values[values.Count - 1]} is the new top." : "The stack is empty now."), 1);

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


    private void UpdateTopLabel()
    {
        if (topLabel == null)
            topLabel = ARText.Tag(Anchor, "TOP", 0.55f, startPos, startPos, LabelSide * 3.1f, 0);
        topLabel.gameObject.SetActive(stackObjects.Count > 0);
        if (stackObjects.Count > 0)
            ARText.Move(topLabel, stackObjects[stackObjects.Count - 1].transform, LabelSide * 3.1f, 0);
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
