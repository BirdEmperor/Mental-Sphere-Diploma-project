using UnityEngine;

public class PerformanceSampler : MonoBehaviour
{
    private float accumulatedFps;
    private int frameCount;
    private Vector3 lastPosition;
    private float distanceMoved;

    public float AverageFps
    {
        get
        {
            if (frameCount <= 0)
                return 0f;

            return accumulatedFps / frameCount;
        }
    }

    public float DistanceMoved => distanceMoved;

    public void ResetSampler(Vector3 startPosition)
    {
        accumulatedFps = 0f;
        frameCount = 0;
        distanceMoved = 0f;
        lastPosition = startPosition;
    }

    public void Sample(Transform trackedTransform)
    {
        if (Time.unscaledDeltaTime > 0f)
        {
            accumulatedFps += 1f / Time.unscaledDeltaTime;
            frameCount++;
        }

        if (trackedTransform != null)
        {
            Vector3 current = trackedTransform.position;
            distanceMoved += Vector3.Distance(lastPosition, current);
            lastPosition = current;
        }
    }
}