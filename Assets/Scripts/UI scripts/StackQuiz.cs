using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StackQuiz : MonoBehaviour
{
    [Header("Stack UI Setup")]
    public List<RectTransform> stackSlots;              // Top = 0, Bottom = last
    public GameObject stackElementPrefab;
    public Button[] pushButtons;                        // Buttons: W, X, Y, Z
    public Button popButton;
    public TextMeshProUGUI feedbackText;
    public RectTransform offScreenTop;                  // UI-based position above for push entry
    public RectTransform offScreenPop;                  // UI-based position above for pop exit
    public RectTransform stackParent;                   // Parent container for instantiated elements
    public GameObject StackPanel;

    // Also the banner's open heading (set on the Heading's HeaderManager in the scene)
    public const string StackInstructions = "Pop every block, then push them back so the stack reads W, X, Y, Z from top to bottom.";

    private Stack<string> stack = new Stack<string>();
    private readonly string[] correctOrder = { "W", "X", "Y", "Z" };

    private void Start()
    {
        feedbackText.text = StackInstructions;

        // Prefill with broken stack, filling slots bottom-up so the visuals match push order
        string[] wrongInit = { "X", "Y", "W", "Z" };
        for (int i = 0; i < wrongInit.Length; i++)
        {
            CreateBlockAtSlot(wrongInit[i], stackSlots.Count - 1 - i);
            stack.Push(wrongInit[i]);
        }

        foreach (Button btn in pushButtons)
        {
            string val = btn.GetComponentInChildren<TextMeshProUGUI>().text;
            btn.onClick.AddListener(() => PushElement(val));
        }

        popButton.onClick.AddListener(PopElement);
    }

    private void CreateBlockAtSlot(string value, int index)
    {
        GameObject block = Instantiate(stackElementPrefab, stackSlots[index]);

        RectTransform rect = block.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        block.GetComponentInChildren<TextMeshProUGUI>().text = value;
    }

    public void PushElement(string value)
    {
        int targetIndex = GetNextAvailableIndex();
        if (targetIndex == -1)
        {
            feedbackText.text = "Stack Overflow!";
            return;
        }

        GameObject newBlock = Instantiate(stackElementPrefab, stackSlots[targetIndex]);

        RectTransform rect = newBlock.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        newBlock.GetComponentInChildren<TextMeshProUGUI>().text = value;

        stack.Push(value);
        CheckStackOrder(); // Immediate check after push
    }


    public void PopElement()
    {
        if (stack.Count == 0)
        {
            //feedbackText.text = "Stack is empty. Start building the correct order.";
            return;
        }

        int topFilledIndex = GetTopFilledIndex();
        if (topFilledIndex == -1)
        {
            feedbackText.text = "Stack visually empty.";
            return;
        }

        RectTransform topSlot = stackSlots[topFilledIndex];

        if (topSlot.childCount > 0)
        {
            RectTransform block = topSlot.GetChild(0).GetComponent<RectTransform>();
            block.SetParent(stackParent); // detach for animation
            StartCoroutine(MoveAndDestroy(block, offScreenPop.anchoredPosition, 0.4f));
        }

        stack.Pop();
    }

    private int GetTopIndex()
    {
        for (int i = stackSlots.Count - 1; i >= 0; i--)
        {
            if (stackSlots[i].childCount == 0)
                return i;
        }
        return -1;
    }

    private int GetNextAvailableIndex()
    {
        for (int i = stackSlots.Count - 1; i >= 0; i--)
        {
            if (stackSlots[i].childCount == 0)
                return i;
        }
        return -1;
    }

    private int GetTopFilledIndex()
    {
        for (int i = 0; i < stackSlots.Count; i++)
        {
            if (stackSlots[i].childCount > 0)
                return i;
        }
        return -1;
    }

    private void CheckStackOrder()
    {
        if (stack.Count != correctOrder.Length)
        {
            //feedbackText.text = "Stack incomplete or incorrect. Build: {W → X → Y → Z}";
            return;
        }

        string[] current = stack.ToArray(); // Top to bottom

        for (int i = 0; i < correctOrder.Length; i++)
        {
            if (current[i] != correctOrder[i])
            {
                feedbackText.text = "Stack order incorrect!";
                return;
            }
        }

        feedbackText.text = "Great job! Stack is in correct LIFO order!";
        Invoke("Complete", 2f);
    }

    private IEnumerator MoveToSlot(RectTransform obj, RectTransform targetSlot, float duration)
    {
        Vector2 start = obj.anchoredPosition;
        Vector2 end = targetSlot.anchoredPosition;
        float time = 0;

        while (time < duration)
        {
            obj.anchoredPosition = Vector2.Lerp(start, end, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        obj.anchoredPosition = end;

        obj.SetParent(targetSlot);

        // Stretch to fit slot fully
        obj.anchorMin = Vector2.zero;
        obj.anchorMax = Vector2.one;
        obj.offsetMin = Vector2.zero;
        obj.offsetMax = Vector2.zero;

        obj.anchoredPosition = Vector2.zero;

        CheckStackOrder();
    }

    private IEnumerator MoveAndDestroy(RectTransform obj, Vector2 target, float duration)
    {
        Vector2 start = obj.anchoredPosition;
        float time = 0;

        while (time < duration)
        {
            obj.anchoredPosition = Vector2.Lerp(start, target, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        Destroy(obj.gameObject);
    }

    public void Complete()
    {
        OrientationController.Instance.SetPortrait();
        StackPanel.SetActive(false);
    }
}
