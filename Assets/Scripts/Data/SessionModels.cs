using System;

[Serializable]
public class StartSessionRequest
{
    public string userId;
    public string sphereColor;
    public string profileId;
    public int seed;
}

[Serializable]
public class StartSessionResponse
{
    public string sessionId;
    public string status;
}

[Serializable]
public class SessionEventRequest
{
    public string eventType;
    public string payloadJson;
}

[Serializable]
public class FinishSessionRequest
{
    public float durationSeconds;
    public float averageFps;
    public int interactionCount;
    public int stonesThrown;
    public int objectsCollected;
    public float distanceMoved;
    public int comfortBefore;
    public int comfortAfter;
    public int stressBefore;
    public int stressAfter;
    public string status;
}