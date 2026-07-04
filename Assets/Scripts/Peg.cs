using System.Collections.Generic;
using UnityEngine;

public class Peg : MonoBehaviour
{
    [SerializeField] private Transform ringAnchor;
    [SerializeField] private float ringSpacing = 0.045f;
    [SerializeField] private float startY = -0.15f;
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 liftOffset = new Vector3(0f, 0.5f, 0f);

    private readonly List<Ring> stackedRings = new List<Ring>();

    public int RingCount => stackedRings.Count;

    public Vector3 GetSlotPosition(int index)
    {
        Vector3 basePosition = ringAnchor != null ? ringAnchor.position : transform.position;
        basePosition.y = startY;
        index = Mathf.Max(0, index);
        return basePosition + startOffset + Vector3.up * (ringSpacing * index);
    }
    
    public void RegisterRing(Ring ring)
    {
        if (ring == null)
            return;

        if (!stackedRings.Contains(ring))
            stackedRings.Add(ring);
    }

    public bool UnregisterRing(Ring ring)
    {
        if (ring == null)
            return false;

        return stackedRings.Remove(ring);
    }

    public Ring PopTopRing()
    {
        if (stackedRings.Count == 0)
            return null;

        int lastIndex = stackedRings.Count - 1;
        Ring topRing = stackedRings[lastIndex];
        stackedRings.RemoveAt(lastIndex);
        return topRing;
    }

    public List<Ring> PopAllRings()
    {
        List<Ring> removed = new List<Ring>(stackedRings);
        stackedRings.Clear();
        return removed;
    }

    public Vector3 LiftOffset => liftOffset;
}
