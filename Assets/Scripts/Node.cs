using UnityEngine;

public class Node : MonoBehaviour
{
    public int data;
    public Node next;

    public void SetData(int data)
    {
        this.data = data;
    }

    public void SetNext(Node nextNode)
    {
        this.next = nextNode;
    }
}
