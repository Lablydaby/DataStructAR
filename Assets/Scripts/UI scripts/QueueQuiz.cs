using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class QueueQuiz : MonoBehaviour
{
    [Header("Queue UI Elements (Reversed)")]
    public List<Button> queueSlots; // Ordered right to left: [Rear → ... → Front]
    public TMP_InputField inputField;
    public Button enqueueButton;
    public Button dequeueButton;
    public TextMeshProUGUI feedbackText;

    public GameObject queuePanel;


    private Queue<string> queue = new Queue<string>();

    private const string guidanceText = "Oh no! The queue is not arranged! Can you fix it by following the First-In, First-Out (FIFO) rule?";
    private const string successText = "Great job! The queue is perfectly arranged from 1 to 5 in FIFO order!";
    private const string fullWrongText = "Queue is full but the order is wrong!\nTry using Dequeue to fix it using FIFO.";

    private void Start()
    {
        feedbackText.text = guidanceText;

        string[] initialWrongValues = { "5", "6", "3", "2", "1" };
        foreach (string val in initialWrongValues)
        {
            queue.Enqueue(val);
        }

        inputField.onValueChanged.AddListener(OnInputChanged);
        enqueueButton.onClick.AddListener(EnqueueValue);
        dequeueButton.onClick.AddListener(DequeueValue);

        enqueueButton.interactable = false;
        UpdateQueueDisplay();
    }

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(OnInputChanged);
        enqueueButton.onClick.RemoveListener(EnqueueValue);
        dequeueButton.onClick.RemoveListener(DequeueValue);
    }

    private void OnInputChanged(string input)
    {
        enqueueButton.interactable = !string.IsNullOrWhiteSpace(input);
        feedbackText.text = guidanceText;
    }

    public void EnqueueValue()
    {
        if (queue.Count >= queueSlots.Count)
        {
            UpdateQueueDisplay(); // So it checks even on a full queue
            return;
        }

        string value = inputField.text.Trim();
        queue.Enqueue(value);
        inputField.text = "";
        enqueueButton.interactable = false;

        UpdateQueueDisplay();
    }

    public void DequeueValue()
    {
        if (queue.Count == 0)
        {
            feedbackText.text = "Queue is empty!\n" + guidanceText;
            return;
        }

        queue.Dequeue();
        UpdateQueueDisplay();
    }

    private void UpdateQueueDisplay()
    {
        for (int i = 0; i < queueSlots.Count; i++)
        {
            var text = queueSlots[i].GetComponentInChildren<TextMeshProUGUI>();

            if (i < queue.Count)
            {
                int reversedIndex = queue.Count - 1 - i;
                text.text = GetQueueValueAt(reversedIndex);
            }
            else
            {
                text.text = "";
            }

            queueSlots[i].gameObject.SetActive(true);
        }

        CheckQueueOrder();
    }

    private string GetQueueValueAt(int index)
    {
        int i = 0;
        foreach (string val in queue)
        {
            if (i == index) return val;
            i++;
        }
        return "";
    }

    private void CheckQueueOrder()
    {
        string[] correctOrder = { "1", "2", "3", "4", "5" };

        if (queue.Count != correctOrder.Length)
        {
            // Queue not full yet → still working
            feedbackText.text = guidanceText;
            return;
        }

        int i = 0;
        foreach (string val in queue)
        {
            if (val != correctOrder[i])
            {
                // ❌ Full but wrong
                feedbackText.text = fullWrongText;
                return;
            }
            i++;
        }

        // ✅ Success!
        feedbackText.text = successText;
        enqueueButton.interactable = false;
        dequeueButton.interactable = false;

        Invoke("CompleteQueue", 2f); // Wait 2 seconds before completing the queue
    }

    public void CompleteQueue()
    {
        OrientationController.Instance.SetPortrait();
        queuePanel.SetActive(false); 

    }
}
