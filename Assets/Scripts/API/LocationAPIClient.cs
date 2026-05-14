using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LocationApiClient : MonoBehaviour
{
    [Header("Server")]
    [SerializeField] private string serverBaseUrl = "http://127.0.0.1:8000";
    [SerializeField] private string userId = "default";
    [SerializeField] private float timeoutSeconds = 5f;

    [Header("Failure Mode")]
    [Tooltip("If OFF, generation is canceled when server is unavailable.")]
    [SerializeField] private bool useFallbackOnFailure = false;

    [Tooltip("Use only a minimal emergency profile, not full local biomes.")]
    [SerializeField] private bool useMinimalEmergencyFallback = true;

    public string ServerBaseUrl => serverBaseUrl;
    public string UserId => userId;

    public IEnumerator FetchGenerationProfile(string sphereColor, Action<GenerationResponse> onSuccess)
    {
        string color = NormalizeColor(sphereColor);
        string url = $"{serverBaseUrl.TrimEnd('/')}/generate/{color}?user_id={userId}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = Mathf.Max(1, Mathf.RoundToInt(timeoutSeconds));

        Debug.Log($"[LocationApiClient] GET {url}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[LocationApiClient] Server request failed for color '{color}'. Error: {request.error}");

            if (useFallbackOnFailure && useMinimalEmergencyFallback)
                onSuccess?.Invoke(CreateMinimalEmergencyFallback(color));
            else
                onSuccess?.Invoke(null);

            yield break;
        }

        string json = request.downloadHandler.text;
        Debug.Log($"[LocationApiClient] Raw server response for '{color}': {json}");

        try
        {
            GenerationResponse response = JsonUtility.FromJson<GenerationResponse>(json);

            if (response == null || response.terrain == null || response.objects == null)
            {
                Debug.LogError($"[LocationApiClient] Invalid JSON response for color '{color}'.");

                if (useFallbackOnFailure && useMinimalEmergencyFallback)
                    onSuccess?.Invoke(CreateMinimalEmergencyFallback(color));
                else
                    onSuccess?.Invoke(null);

                yield break;
            }

            EnsureDefaults(response);

            Debug.Log(
                $"[LocationApiClient] Parsed profile: color={response.sphereColor}, profile={response.profileId}, biome={response.biome}, seed={response.seed}"
            );

            onSuccess?.Invoke(response);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[LocationApiClient] JSON parse error for color '{color}': {exception.Message}");

            if (useFallbackOnFailure && useMinimalEmergencyFallback)
                onSuccess?.Invoke(CreateMinimalEmergencyFallback(color));
            else
                onSuccess?.Invoke(null);
        }
    }

    private string NormalizeColor(string color)
    {
        return string.IsNullOrWhiteSpace(color)
            ? "blue"
            : color.Trim().ToLowerInvariant();
    }

    private void EnsureDefaults(GenerationResponse response)
    {
        response.terrain ??= new TerrainConfig();
        response.objects ??= new ObjectCounts();
        response.rules ??= new GenerationRules();
        response.atmosphere ??= new AtmosphereConfig();
        response.audio ??= new AudioConfig();
        response.textures ??= new TextureConfig();
        response.lighting ??= new LightingConfig();
        response.skybox ??= new SkyboxConfig();
        response.effects ??= new EffectCounts();
        response.interactives ??= new InteractiveCounts();
    }

    private GenerationResponse CreateMinimalEmergencyFallback(string requestedColor)
    {
        Debug.LogWarning(
            $"[LocationApiClient] Server unavailable. Minimal emergency fallback used. Requested color was '{requestedColor}'."
        );

        GenerationResponse response = new GenerationResponse
        {
            profileId = "offline_emergency_profile",
            sphereColor = requestedColor,
            seed = UnityEngine.Random.Range(1000, 999999999),
            biome = "offline_plain",

            terrain = new TerrainConfig(),
            objects = new ObjectCounts(),
            rules = new GenerationRules(),
            atmosphere = new AtmosphereConfig(),
            audio = new AudioConfig(),
            textures = new TextureConfig(),
            lighting = new LightingConfig(),
            skybox = new SkyboxConfig(),
            effects = new EffectCounts(),
            interactives = new InteractiveCounts()
        };

        response.terrain.size = 32f;
        response.terrain.terrainHeight = 3.5f;
        response.terrain.heightMultiplier = 0.04f;
        response.terrain.noiseScale = 1.5f;
        response.terrain.flattenSpawnRadius = 8f;
        response.terrain.waterLevel = 0f;
        response.terrain.waterRadius = 0f;
        response.terrain.lakeDepth = 0f;

        response.objects.treeCount = 0;
        response.objects.rockCount = 0;
        response.objects.cliffCount = 0;
        response.objects.bushCount = 0;
        response.objects.grassPatchCount = 0;
        response.objects.flowerPatchCount = 0;
        response.objects.reedCount = 0;
        response.objects.branchCount = 0;

        response.interactives.pickupStoneCount = 0;

        response.rules.safeRadius = 5f;
        response.rules.minObjectDistance = 0.5f;
        response.rules.waterAvoidanceMargin = 0f;

        response.audio.ambientSound = "offline";
        response.audio.waterSound = "none";
        response.audio.waterEnabled = false;
        response.audio.windIntensity = 0.05f;

        response.atmosphere.timeOfDay = "offline";
        response.atmosphere.fogColor = "#B8C0C8";
        response.atmosphere.fogStart = 8f;
        response.atmosphere.fogEnd = 36f;
        response.atmosphere.lightIntensity = 0.55f;

        response.lighting.timeOfDay = "offline";
        response.lighting.lightIntensity = 0.55f;
        response.lighting.lightRotationX = 35f;
        response.lighting.lightRotationY = -25f;
        response.lighting.lightColor = "#DDE6EE";
        response.lighting.ambientColor = "#8C98A2";

        response.skybox.skyboxId = "offline_default";
        response.skybox.fogColor = "#B8C0C8";
        response.skybox.fogStart = 8f;
        response.skybox.fogEnd = 36f;

        return response;
    }
}