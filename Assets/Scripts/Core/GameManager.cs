using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private LocationApiClient apiClient;
    [SerializeField] private SessionTelemetryClient telemetryClient;
    [SerializeField] private RuntimeTerrainGenerator terrainGenerator;
    [SerializeField] private TerrainBoundaryBuilder terrainBoundaryBuilder;
    [SerializeField] private WaterController waterController;
    [SerializeField] private WorldSpawner worldSpawner;
    [SerializeField] private BiomeApplicator biomeApplicator;
    [SerializeField] private TransitionManager transitionManager;
    [SerializeField] private PerformanceSampler performanceSampler;

    [Header("Player")]
    [SerializeField] private Transform xrOrigin;

    [Header("Scene Roots")]
    [SerializeField] private GameObject hubRoot;
    [SerializeField] private GameObject locationRoot;

    [Header("Spawn Points")]
    [SerializeField] private Transform hubSpawnPoint;
    [SerializeField] private Transform locationCenter;

    [Header("Generated Location Offset")]
    [SerializeField] private Vector3 baseLocationCenter = new Vector3(0f, 0f, 120f);
    [SerializeField] private float randomLocationOffsetRadius = 20f;

    [Header("Testing")]
    [SerializeField] private bool enableKeyboardTest = true;
    [SerializeField] private string testSphereColor = "blue";
    [SerializeField] private bool allowKeyboardGenerationDuringSession = false;

    [Header("Player Safety")]
    [SerializeField] private float playerFallResetY = -10f;

    [Header("Session Metrics")]
    [SerializeField] private int comfortBefore = -1;
    [SerializeField] private int stressBefore = -1;
    [SerializeField] private int comfortAfter = -1;
    [SerializeField] private int stressAfter = -1;

    private bool isTransitionRunning;
    private bool sessionActive;
    private float sessionStartTime;

    private int interactionCount;
    private int stonesThrown;
    private int objectsCollected;

    private void Start()
    {
        if (locationRoot != null)
            locationRoot.SetActive(false);

        if (hubRoot != null)
            hubRoot.SetActive(true);
    }

    private void Update()
    {
        if (sessionActive && performanceSampler != null)
            performanceSampler.Sample(xrOrigin);

        if (sessionActive && xrOrigin != null && xrOrigin.position.y < playerFallResetY)
        {
            Debug.LogWarning("[GameManager] Player fell below safety limit. Returning to hub.");
            FinishCurrentSession();
        }

        if (!enableKeyboardTest)
            return;

        bool generationKeysAllowed = !sessionActive || allowKeyboardGenerationDuringSession;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame && generationKeysAllowed)
            OnSphereActivated(testSphereColor);

        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame && generationKeysAllowed)
            OnSphereActivated("blue");

        if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame && generationKeysAllowed)
            OnSphereActivated("green");

        if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame && generationKeysAllowed)
            OnSphereActivated("yellow");

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            FinishCurrentSession();
#else
        if (Input.GetKeyDown(KeyCode.B) && generationKeysAllowed)
            OnSphereActivated(testSphereColor);

        if (Input.GetKeyDown(KeyCode.Alpha1) && generationKeysAllowed)
            OnSphereActivated("blue");

        if (Input.GetKeyDown(KeyCode.Alpha2) && generationKeysAllowed)
            OnSphereActivated("green");

        if (Input.GetKeyDown(KeyCode.Alpha3) && generationKeysAllowed)
            OnSphereActivated("yellow");

        if (Input.GetKeyDown(KeyCode.F))
            FinishCurrentSession();
#endif
    }

    public void OnSphereActivated(string sphereColor)
    {
        string color = string.IsNullOrWhiteSpace(sphereColor)
            ? "blue"
            : sphereColor.Trim().ToLowerInvariant();

        Debug.Log($"[GameManager] OnSphereActivated called with color: {color}");

        if (isTransitionRunning)
        {
            Debug.LogWarning("[GameManager] Transition already running.");
            return;
        }

        if (sessionActive)
        {
            Debug.LogWarning($"[GameManager] Location session is already active. Sphere activation ignored: {color}");
            return;
        }

        StartCoroutine(GenerateLocationFlow(color));
    }

    private IEnumerator GenerateLocationFlow(string sphereColor)
    {
        isTransitionRunning = true;

        Debug.Log($"[GameManager] Generation started: {sphereColor}");

        GenerationResponse response = null;

        if (apiClient != null)
        {
            yield return apiClient.FetchGenerationProfile(
                sphereColor,
                result => response = result
            );
        }

        if (response == null)
        {
            Debug.LogError("[GameManager] Generation response is null. Location generation canceled.");
            isTransitionRunning = false;
            yield break;
        }

        Debug.Log(
            $"[GameManager] Using profile: color={response.sphereColor}, profile={response.profileId}, biome={response.biome}, seed={response.seed}"
        );

        if (telemetryClient != null)
        {
            yield return telemetryClient.StartSession(
                response,
                sessionId =>
                {
                    Debug.Log($"[GameManager] Telemetry session id: {sessionId}");
                }
            );
        }

        interactionCount = 0;
        stonesThrown = 0;
        objectsCollected = 0;
        ThrowableStoneCounter.ResetCounter();

        if (transitionManager != null)
        {
            yield return StartCoroutine(transitionManager.PlayTransition(() =>
            {
                GenerateLocationInternal(response);
            }));
        }
        else
        {
            GenerateLocationInternal(response);
        }

        sessionStartTime = Time.time;
        sessionActive = true;

        if (performanceSampler != null && xrOrigin != null)
            performanceSampler.ResetSampler(xrOrigin.position);

        if (telemetryClient != null)
            yield return telemetryClient.SendEvent("location_generated", JsonUtility.ToJson(response));

        Debug.Log("[GameManager] Generation completed.");

        isTransitionRunning = false;
    }

    private void GenerateLocationInternal(GenerationResponse response)
    {
        if (locationRoot != null)
            locationRoot.SetActive(true);

        RandomizeLocationCenter(response.seed);

        if (terrainGenerator != null)
            terrainGenerator.GenerateTerrain(response);

        if (waterController != null && terrainGenerator != null)
            waterController.ApplyWater(response, terrainGenerator.ActiveTerrain);

        if (terrainBoundaryBuilder != null && terrainGenerator != null)
            terrainBoundaryBuilder.RebuildBoundaries(terrainGenerator.ActiveTerrain);

        if (worldSpawner != null && terrainGenerator != null)
            worldSpawner.GenerateWorld(response, terrainGenerator, waterController);

        if (biomeApplicator != null)
            biomeApplicator.ApplyBiome(response);

        MovePlayerToGeneratedLocation();

        if (hubRoot != null)
            hubRoot.SetActive(false);
    }

    private void RandomizeLocationCenter(int seed)
    {
        if (locationCenter == null)
            return;

        System.Random rng = new System.Random(seed);

        float offsetX = Mathf.Lerp(
            -randomLocationOffsetRadius,
            randomLocationOffsetRadius,
            (float)rng.NextDouble()
        );

        float offsetZ = Mathf.Lerp(
            -randomLocationOffsetRadius,
            randomLocationOffsetRadius,
            (float)rng.NextDouble()
        );

        locationCenter.position = baseLocationCenter + new Vector3(offsetX, 0f, offsetZ);
    }

    private void MovePlayerToGeneratedLocation()
    {
        if (xrOrigin == null || terrainGenerator == null)
            return;

        Vector3 spawnPosition = terrainGenerator.GetSafeSpawnPosition(0.15f);

        xrOrigin.position = spawnPosition;
        xrOrigin.rotation = Quaternion.identity;
    }

    public void RegisterInteraction(string eventName)
    {
        interactionCount++;

        if (telemetryClient != null)
            StartCoroutine(telemetryClient.SendEvent(eventName, ""));
    }

    public void RegisterStoneThrown()
    {
        stonesThrown++;
        RegisterInteraction("stone_thrown");
    }

    public void RegisterObjectCollected()
    {
        objectsCollected++;
        RegisterInteraction("object_collected");
    }

    public void FinishCurrentSession()
    {
        Debug.Log("[GameManager] FinishCurrentSession called.");

        if (isTransitionRunning)
        {
            Debug.LogWarning("[GameManager] Transition is running. Finish ignored.");
            return;
        }

        if (!sessionActive)
        {
            Debug.LogWarning("[GameManager] No active session. Forced return to hub.");
            ReturnPlayerToHub();
            return;
        }

        StartCoroutine(FinishSessionFlow("completed"));
    }

    private IEnumerator FinishSessionFlow(string status)
    {
        sessionActive = false;
        isTransitionRunning = true;

        stonesThrown = ThrowableStoneCounter.TotalReleasedStones;

        float duration = Time.time - sessionStartTime;

        FinishSessionRequest finishRequest = new FinishSessionRequest
        {
            durationSeconds = duration,
            averageFps = performanceSampler != null ? performanceSampler.AverageFps : 0f,
            interactionCount = interactionCount,
            stonesThrown = stonesThrown,
            objectsCollected = objectsCollected,
            distanceMoved = performanceSampler != null ? performanceSampler.DistanceMoved : 0f,
            comfortBefore = comfortBefore,
            comfortAfter = comfortAfter,
            stressBefore = stressBefore,
            stressAfter = stressAfter,
            status = status
        };

        if (transitionManager != null)
        {
            yield return StartCoroutine(transitionManager.PlayTransition(() =>
            {
                ReturnPlayerToHub();
            }));
        }
        else
        {
            ReturnPlayerToHub();
        }

        if (telemetryClient != null)
            yield return telemetryClient.FinishSession(finishRequest);

        Debug.Log("[GameManager] Session finished and player returned to Hub.");

        isTransitionRunning = false;
    }

    private void ReturnPlayerToHub()
    {
        if (terrainBoundaryBuilder != null)
            terrainBoundaryBuilder.ClearBoundaries();

        if (locationRoot != null)
            locationRoot.SetActive(false);

        if (hubRoot != null)
            hubRoot.SetActive(true);

        if (xrOrigin != null && hubSpawnPoint != null)
        {
            xrOrigin.position = hubSpawnPoint.position;
            xrOrigin.rotation = hubSpawnPoint.rotation;
        }
    }
}