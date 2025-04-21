using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LinkedListUI : MonoBehaviour
{
    public LinkedListManager linkedListManager;
    public ArrayManager arrayManager;
    public ArrayListManager arrayListManager;
    public StackManager stackManager; // Reference to the StackManager component
    public QueueManager queueManager; // Reference to the StackManager component
    public TMP_Dropdown shapeDropdown;

    public void OnAddNodeButtonClicked()
    {
        int prefabIndex = shapeDropdown.value;
        linkedListManager.AddNode(prefabIndex);
    }

    public void OnAddNodeButtonClickedArray()
    {
        int prefabIndex = shapeDropdown.value;
        arrayManager.AddElement(prefabIndex);
    }

    public void OnRemoveNodeButtonClickedArray()
    {
        arrayManager.RemoveLastElement();
    }

    public void OnRemoveNodeButtonClicked()
    {
        linkedListManager.RemoveLastNode();
    }

    public void OnAddArrayElementButtonClicked()
    {
        int prefabIndex = shapeDropdown.value;
        arrayListManager.AddElement(prefabIndex);
    }

    public void OnRemoveArrayElementButtonClicked()
    {
        arrayListManager.RemoveElement();
    }

    public void OnPushObjectButtonClicked()
    {
        int prefabIndex = shapeDropdown.value;
        stackManager.Push(prefabIndex); // Call the PushObject method of the StackManager
    }

    public void OnPopObjectButtonClicked()
    {
        stackManager.Pop(); // Call the PopObject method of the StackManager
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }


    public void EnQueue()
    {
        int prefabIndex = shapeDropdown.value;
        queueManager.Enqueue(prefabIndex);
    }

    public void DeQueue()
    {
        queueManager.Dequeue();
    }
}
