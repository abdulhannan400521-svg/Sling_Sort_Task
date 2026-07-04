using System.Collections;
using UnityEngine;

public static class RingMover
{
    public static IEnumerator MoveWithSpring(
        Transform ring,
        Vector3 targetPosition,
        float duration,
        float arcHeight,
        float springFrequency,
        float springDamping,
        float startDelay)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        Vector3 startPosition = ring.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            float arcOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;

            float springOffset = Mathf.Sin(t * springFrequency * Mathf.PI * 2f) *
                                  Mathf.Exp(-t * springDamping) *
                                  arcHeight * 0.35f;

            basePosition.y += arcOffset + springOffset;
            ring.position = basePosition;

            yield return null;
        }

        ring.position = targetPosition;
    }
}
