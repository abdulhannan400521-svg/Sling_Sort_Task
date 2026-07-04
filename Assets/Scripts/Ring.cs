using UnityEngine;

public class Ring : MonoBehaviour
{
    public int StackIndex { get; private set; }

    public void SetStackIndex(int index)
    {
        StackIndex = index;
    }
}
