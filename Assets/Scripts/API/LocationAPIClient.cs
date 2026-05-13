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

    [Header("Fallback")]
    [SerializeField] private bool useFallbackOnFailure = true;

    public string ServerBaseUrl => serverBaseUrl;
    public string UserId => userId;

    public IEnumerator FetchGenerationProfile(string sphereColor, Action<GenerationResponse> onSuccess)
    {
        string color = string.IsNullOrWhiteSpace(sphereColor)
            ? "blue"
            : sphereColor.Trim().ToLowerInvariant();

        string url = $"{serverBaseUrl.TrimEnd('/')}/generate/{color}?user_id={userId}";

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.timeout = Mathf.RoundToInt(timeoutSeconds);

        Debug.Log($"[LocationApiClient] GET {url}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[LocationApiClient] Server request failed for color '{color}'. Error: {request.error}");

            if (useFallbackOnFailure)
                onSuccess?.Invoke(CreateFallbackResponse(color));
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

                if (useFallbackOnFailure)
                    onSuccess?.Invoke(CreateFallbackResponse(color));
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

            if (useFallbackOnFailure)
                onSuccess?.Invoke(CreateFallbackResponse(color));
            else
                onSuccess?.Invoke(null);
        }
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

    private GenerationResponse CreateFallbackResponse(string sphereColor)
    {
        Debug.LogWarning($"[LocationApiClient] Fallback profile used for '{sphereColor}'.");

        sphereColor = string.IsNullOrWhiteSpace(sphereColor)
            ? "blue"
            : sphereColor.Trim().ToLowerInvariant();

        GenerationResponse response = new GenerationResponse
        {
            sphereColor = sphereColor,
            seed = UnityEngine.Random.Range(1000, 999999999),
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

        if (sphereColor == "blue")
        {
            response.profileId = "fallback_lake_blue";
            response.biome = "lake";

            response.terrain.size = 60f;
            response.terrain.terrainHeight = 5f;
            response.terrain.heightMultiplier = 0.32f;
            response.terrain.noiseScale = 3.2f;
            response.terrain.flattenSpawnRadius = 5.5f;
            response.terrain.waterLevel = 0.46f;
            response.terrain.waterRadius = 17.5f;
            response.terrain.waterCenterX = 0.24f;
            response.terrain.waterCenterZ = 0.28f;
            response.terrain.lakeDepth = 1.15f;

            response.objects.treeCount = 10;
            response.objects.rockCount = 34;
            response.objects.cliffCount = 4;
            response.objects.bushCount = 22;
            response.objects.grassPatchCount = 520;
            response.objects.flowerPatchCount = 24;
            response.objects.reedCount = 64;
            response.objects.branchCount = 4;

            response.interactives.pickupStoneCount = 18;

            response.audio.ambientSound = "lake";
            response.audio.waterSound = "water_soft";
            response.audio.waterEnabled = true;
            response.audio.windIntensity = 0.20f;

            response.effects.floatingLeavesCount = 0;
            response.effects.floatingPetalsCount = 0;
            response.effects.butterflySpawnAreas = 0;

            response.skybox.skyboxId = "lake_morning";
        }
        else if (sphereColor == "green")
        {
            response.profileId = "fallback_forest_green";
            response.biome = "forest";

            response.terrain.size = 56f;
            response.terrain.terrainHeight = 4.6f;
            response.terrain.heightMultiplier = 0.42f;
            response.terrain.noiseScale = 3.8f;
            response.terrain.flattenSpawnRadius = 5f;
            response.terrain.waterRadius = 0f;

            response.objects.treeCount = 70;
            response.objects.rockCount = 14;
            response.objects.cliffCount = 0;
            response.objects.bushCount = 56;
            response.objects.grassPatchCount = 900;
            response.objects.flowerPatchCount = 40;
            response.objects.reedCount = 0;
            response.objects.branchCount = 22;

            response.interactives.pickupStoneCount = 6;

            response.audio.ambientSound = "forest";
            response.audio.waterSound = "none";
            response.audio.waterEnabled = false;
            response.audio.windIntensity = 0.16f;

            response.effects.floatingLeavesCount = 5;
            response.effects.floatingPetalsCount = 0;
            response.effects.butterflySpawnAreas = 0;

            response.skybox.skyboxId = "forest_day";
        }
        else if (sphereColor == "yellow")
        {
            response.profileId = "fallback_field_yellow";
            response.biome = "flower_field";

            response.terrain.size = 56f;
            response.terrain.terrainHeight = 3.8f;
            response.terrain.heightMultiplier = 0.22f;
            response.terrain.noiseScale = 3f;
            response.terrain.flattenSpawnRadius = 5f;
            response.terrain.waterRadius = 0f;

            response.objects.treeCount = 0;
            response.objects.rockCount = 2;
            response.objects.cliffCount = 0;
            response.objects.bushCount = 4;
            response.objects.grassPatchCount = 900;
            response.objects.flowerPatchCount = 1200;
            response.objects.reedCount = 0;
            response.objects.branchCount = 0;

            response.interactives.pickupStoneCount = 0;

            response.audio.ambientSound = "field";
            response.audio.waterSound = "none";
            response.audio.waterEnabled = false;
            response.audio.windIntensity = 0.18f;

            response.effects.floatingLeavesCount = 0;
            response.effects.floatingPetalsCount = 3;
            response.effects.butterflySpawnAreas = 1;

            response.skybox.skyboxId = "field_warm";
        }
        else
        {
            response.profileId = "fallback_unknown_blue";
            response.biome = "lake";
            response.sphereColor = "blue";
            return CreateFallbackResponse("blue");
        }

        return response;
    }
}