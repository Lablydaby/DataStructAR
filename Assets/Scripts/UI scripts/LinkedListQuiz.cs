using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class LinkedListQuiz : MonoBehaviour
{
    [Header("Linked List Setup")]
    public Canvas canvas;
    public List<Transform> dropSlots;        // Slots where nodes are placed (4 slots)
    public List<DraggableNode> nodes;        // Nodes: A, B, C, D
    public TextMeshProUGUI feedbackText;
    public GameObject LinkedListPanel; // Panel to show/hide the quiz
    private readonly string[] correctOrder = { "A", "B", "C", "D" };
    private const string successText = "Great job! The Linked List is fixed!";
    //private const string guidanceText = "Can you fix the broken links? Drag and adjust the pointers to restore the correct order!";

    private void Start()
    {
        //feedbackText.text = guidanceText;
        ShuffleAndPlaceNodes();
    }

    private void ShuffleAndPlaceNodes()
    {
        List<DraggableNode> shuffled = new List<DraggableNode>(nodes);
        bool isCorrectOrder;

        // Keep shuffling until the order is NOT A-B-C-D
        do
        {
            for (int i = 0; i < shuffled.Count; i++)
            {
                int rand = Random.Range(i, shuffled.Count);
                var temp = shuffled[i];
                shuffled[i] = shuffled[rand];
                shuffled[rand] = temp;
            }

            isCorrectOrder = true;
            for (int i = 0; i < shuffled.Count; i++)
            {
                string label = shuffled[i].GetComponentInChildren<TextMeshProUGUI>().text;
                if (label != correctOrder[i])
                {
                    isCorrectOrder = false;
                    break;
                }
            }

        } while (isCorrectOrder); // Keep retrying if it's already correct

        // Place shuffled nodes in slots
        for (int i = 0; i < dropSlots.Count; i++)
        {
            shuffled[i].transform.SetParent(dropSlots[i]);
            shuffled[i].transform.localPosition = Vector3.zero;
            shuffled[i].originalParent = dropSlots[i];
        }
    }


    public void CheckLinkedListOrder()
    {
        for (int i = 0; i < dropSlots.Count; i++)
        {
            if (dropSlots[i].childCount == 0)
            {
                //feedbackText.text = guidanceText;
                return;
            }

            string label = dropSlots[i].GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text;
            if (label != correctOrder[i])
            {
                //feedbackText.text = guidanceText;
                return;
            }
        }

        // All matched (the check runs on both drop and end-drag, so only schedule once)
        if (IsInvoking("Complete"))
            return;

        feedbackText.text = successText;

        Invoke("Complete", 2f); // Call Complete after 2 seconds    


    }

    public void Complete()
    {
        Progress.MarkPracticeDone("Linked List");
        OrientationController.Instance.SetPortrait();
        LinkedListPanel.SetActive(false); // Hide the quiz panel
    }



    public void DeleteAll()
    {
        foreach (var slot in dropSlots)
        {
            if (slot.childCount > 0)
                Destroy(slot.GetChild(0).gameObject);
        }

        feedbackText.text = "List cleared. Try again!";
    }
}
