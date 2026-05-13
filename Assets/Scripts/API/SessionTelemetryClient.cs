using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SessionTelemetryClient : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private LocationApiClient apiClient;
    [SerializeField] private float timeoutSeconds = 5f;

    public string CurrentSessionId { get; private set; }

    public IEnumerator StartSession(
        GenerationResponse response,
        Action<string> onStarted)
    {
        if (apiClient == null || response == null)
        {
            onStarted?.Invoke(string.Empty);
            yield break;
        }

        StartSessionRequest body = new StartSessionRequest
        {
            userId = apiClient.UserId,
            sphereColor = response.sphereColor,
            profileId = response.profileId,
            seed = response.seed
        };

        string json = JsonUtility.ToJson(body);
        string url = $"{apiClient.ServerBaseUrl}/sessions/start";

        using UnityWebRequest request = CreatePostRequest(url, json);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[SessionTelemetryClient] Start session failed: {request.error}");
            CurrentSessionId = string.Empty;
            onStarted?.Invoke(string.Empty);
            yield break;
        }

        StartSessionResponse result = JsonUtility.FromJson<StartSessionResponse>(request.downloadHandler.text);
        CurrentSessionId = result != null ? result.sessionId : string.Empty;

        Debug.Log($"[SessionTelemetryClient] Session started: {CurrentSessionId}");
        onStarted?.Invoke(CurrentSessionId);
    }

    public IEnumerator SendEvent(string eventType, string payloadJson = "")
    {
        if (string.IsNullOrEmpty(CurrentSessionId) || apiClient == null)
            yield break;

        SessionEventRequest body = new SessionEventRequest
        {
            eventType = eventType,
            payloadJson = payloadJson ?? ""
        };

        string json = JsonUtility.ToJson(body);
        string url = $"{apiClient.ServerBaseUrl}/sessions/{CurrentSessionId}/event";

        using UnityWebRequest request = CreatePostRequest(url, json);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[SessionTelemetryClient] Event send failed: {request.error}");
        }
    }

    public IEnumerator FinishSession(FinishSessionRequest finishRequest)
    {
        if (string.IsNullOrEmpty(CurrentSessionId) || apiClient == null)
            yield break;

        string json = JsonUtility.ToJson(finishRequest);
        string url = $"{apiClient.ServerBaseUrl}/sessions/{CurrentSessionId}/finish";

        using UnityWebRequest request = CreatePostRequest(url, json);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[SessionTelemetryClient] Finish session failed: {request.error}");
            yield break;
        }

        Debug.Log($"[SessionTelemetryClient] Session finished: {CurrentSessionId}");
        CurrentSessionId = string.Empty;
    }

    private UnityWebRequest CreatePostRequest(string url, string json)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = Mathf.RoundToInt(timeoutSeconds);

        return request;
    }
}