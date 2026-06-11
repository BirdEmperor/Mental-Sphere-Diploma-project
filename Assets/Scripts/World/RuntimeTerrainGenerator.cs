using UnityEngine;

public class RuntimeTerrainGenerator : MonoBehaviour
{
    [Header("Terrain")]
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform locationCenter;
    [SerializeField] private int heightmapResolution = 129;

    [Header("Terrain Layers")]
    [Tooltip("0 = Grass, 1 = forest/soft ground, 2 = rock. Profiles keep grass dominant for calmer VR landscapes.")]
    [SerializeField] private TerrainLayer[] terrainLayers;

    [Header("Land Shape")]
    [Tooltip("Default land height in world units. The terrain is intentionally almost flat for comfort in VR.")]
    [SerializeField] private float defaultLandHeight = 0.72f;

    [Tooltip("Maximum amplitude of long, soft hills. Keep this low for comfort.")]
    [SerializeField] private float broadHillAmplitude = 0.28f;

    [Tooltip("Small surface variation amplitude. Keep this very low to avoid noisy terrain.")]
    [SerializeField] private float microVariationAmplitude = 0.045f;

    [Header("Noise")]
    [SerializeField] private int octaves = 2;
    [SerializeField] private float persistence = 0.35f;
    [SerializeField] private float lacunarity = 1.85f;

    [Header("Smoothing")]
    [SerializeField] private int smoothPasses = 2;

    private GenerationResponse lastResponse;

    public Terrain ActiveTerrain => targetTerrain;
    public Transform LocationCenter => locationCenter;

    public void GenerateTerrain(GenerationResponse response)
    {
        if (targetTerrain == null)
        {
            Debug.LogError("[RuntimeTerrainGenerator] Target terrain is not assigned.");
            return;
        }

        lastResponse = response;

        TerrainConfig config = response.terrain ?? new TerrainConfig();

        TerrainData data = targetTerrain.terrainData;
        data.heightmapResolution = heightmapResolution;
        data.size = new Vector3(config.size, config.terrainHeight, config.size);

        Vector3 center = locationCenter != null ? locationCenter.position : Vector3.zero;

        targetTerrain.transform.position = new Vector3(
            center.x - config.size * 0.5f,
            center.y,
            center.z - config.size * 0.5f
        );

        float[,] heights = new float[heightmapResolution, heightmapResolution];

        float seedOffsetX = (response.seed % 100000) * 0.013f;
        float seedOffsetZ = (response.seed % 70000) * 0.017f;

        bool hasWater = response.audio != null && response.audio.waterEnabled && config.waterRadius > 0.1f;
        float landBase = CalculateLandBaseHeight(config, hasWater);

        for (int z = 0; z < heightmapResolution; z++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                float nx = (float)x / (heightmapResolution - 1);
                float nz = (float)z / (heightmapResolution - 1);

                float heightWorld = BuildCalmLandHeight(nx, nz, config, landBase, seedOffsetX, seedOffsetZ);

                heightWorld = ApplyLakeDepression(heightWorld, nx, nz, config, response);
                heightWorld = ApplySafeSpawnFlattening(heightWorld, nx, nz, config, landBase);

                heights[z, x] = Mathf.Clamp01(heightWorld / Mathf.Max(0.01f, config.terrainHeight));
            }
        }

        for (int i = 0; i < smoothPasses; i++)
            heights = SmoothHeights(heights);

        data.SetHeights(0, 0, heights);

        if (terrainLayers != null && terrainLayers.Length > 0)
        {
            data.terrainLayers = terrainLayers;
            ApplyTerrainTextures(data, config, response);
        }

        TerrainCollider terrainCollider = targetTerrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.enabled = false;
            terrainCollider.terrainData = data;
            terrainCollider.enabled = true;
        }

        RefreshTeleportComponents();

        Debug.Log($"[RuntimeTerrainGenerator] Terrain generated. Seed: {response.seed}, biome={response.biome}, water={hasWater}");
    }

    private float CalculateLandBaseHeight(TerrainConfig config, bool hasWater)
    {
        if (!hasWater)
            return Mathf.Clamp(defaultLandHeight, 0.18f, config.terrainHeight - 0.25f);

        float minimumLandAboveWater = config.waterLevel + 0.38f;
        float desired = Mathf.Max(defaultLandHeight, minimumLandAboveWater);

        return Mathf.Clamp(desired, 0.18f, config.terrainHeight - 0.25f);
    }

    private float BuildCalmLandHeight(float nx, float nz, TerrainConfig config, float landBase, float seedOffsetX, float seedOffsetZ)
    {
        float broad = FractalNoise(
            nx,
            nz,
            Mathf.Max(0.35f, config.noiseScale * 0.22f),
            seedOffsetX,
            seedOffsetZ
        );

        broad = (broad - 0.5f) * 2f;

        float micro = FractalNoise(
            nx,
            nz,
            Mathf.Max(1.2f, config.noiseScale),
            seedOffsetX + 177.3f,
            seedOffsetZ + 59.7f
        );

        micro = (micro - 0.5f) * 2f;

        float requestedHill = Mathf.Clamp(config.heightMultiplier, 0f, 1.2f);
        float broadHeight = broad * broadHillAmplitude * Mathf.Lerp(0.35f, 1f, requestedHill);
        float microHeight = micro * microVariationAmplitude;

        return landBase + broadHeight + microHeight;
    }

    private float FractalNoise(float nx, float nz, float scale, float seedOffsetX, float seedOffsetZ)
    {
        float amplitude = 1f;
        float frequency = 1f;
        float value = 0f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = nx * scale * frequency + seedOffsetX;
            float sampleZ = nz * scale * frequency + seedOffsetZ;

            float noise = Mathf.PerlinNoise(sampleX, sampleZ);
            value += noise * amplitude;

            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        float normalized = value / maxValue;
        normalized = Mathf.SmoothStep(0f, 1f, normalized);

        return normalized;
    }

    private float ApplyLakeDepression(float heightWorld, float nx, float nz, TerrainConfig config, GenerationResponse response)
    {
        bool waterEnabled = response.audio != null && response.audio.waterEnabled;

        if (!waterEnabled || config.waterRadius <= 0.1f)
            return heightWorld;

        Vector2 point = new Vector2(nx, nz);
        Vector2 lakeCenter = new Vector2(config.waterCenterX, config.waterCenterZ);

        float lakeRadiusNormalized = config.waterRadius / Mathf.Max(1f, config.size);
        float shoreRadius = lakeRadiusNormalized * 1.32f;
        float distance = Vector2.Distance(point, lakeCenter);

        if (distance > shoreRadius)
            return heightWorld;

        float lakeBottom = Mathf.Max(0.02f, config.waterLevel - Mathf.Max(0.25f, config.lakeDepth));
        float shallowBottom = Mathf.Max(0.03f, config.waterLevel - 0.24f);

        if (distance < lakeRadiusNormalized)
        {
            float centerT = Mathf.InverseLerp(lakeRadiusNormalized, 0f, distance);
            centerT = Mathf.SmoothStep(0f, 1f, centerT);

            float bottom = Mathf.Lerp(shallowBottom, lakeBottom, centerT);
            return Mathf.Min(heightWorld, bottom);
        }

        float shoreT = Mathf.InverseLerp(shoreRadius, lakeRadiusNormalized, distance);
        shoreT = Mathf.SmoothStep(0f, 1f, shoreT);

        float shoreHeight = Mathf.Lerp(heightWorld, config.waterLevel - 0.08f, shoreT);

        return Mathf.Min(heightWorld, shoreHeight);
    }

    private float ApplySafeSpawnFlattening(float heightWorld, float nx, float nz, TerrainConfig config, float landBase)
    {
        Vector2 point = new Vector2(nx, nz);
        Vector2 center = new Vector2(0.5f, 0.5f);

        float radiusNormalized = config.flattenSpawnRadius / Mathf.Max(1f, config.size);
        float distance = Vector2.Distance(point, center);

        if (distance > radiusNormalized)
            return heightWorld;

        float t = Mathf.Clamp01(distance / radiusNormalized);
        t = Mathf.SmoothStep(0f, 1f, t);

        return Mathf.Lerp(landBase, heightWorld, t);
    }

    private float[,] SmoothHeights(float[,] source)
    {
        int width = source.GetLength(1);
        int height = source.GetLength(0);

        float[,] result = new float[height, width];

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0f;
                int count = 0;

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int px = x + dx;
                        int pz = z + dz;

                        if (px < 0 || px >= width || pz < 0 || pz >= height)
                            continue;

                        sum += source[pz, px];
                        count++;
                    }
                }

                result[z, x] = sum / count;
            }
        }

        return result;
    }

    private void ApplyTerrainTextures(TerrainData data, TerrainConfig config, GenerationResponse response)
    {
        int layerCount = data.terrainLayers.Length;
        if (layerCount <= 0)
            return;

        TextureConfig textures = response.textures ?? new TextureConfig();

        int grassIndex = Mathf.Clamp(textures.grassLayerIndex, 0, layerCount - 1);
        int sandIndex = Mathf.Clamp(textures.sandLayerIndex, 0, layerCount - 1);
        int rockIndex = Mathf.Clamp(textures.rockLayerIndex, 0, layerCount - 1);
        int pathIndex = Mathf.Clamp(textures.pathLayerIndex, 0, layerCount - 1);

        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;

        float[,,] alphamaps = new float[alphaHeight, alphaWidth, layerCount];

        bool hasWater = response.audio != null && response.audio.waterEnabled && config.waterRadius > 0.1f;

        for (int z = 0; z < alphaHeight; z++)
        {
            for (int x = 0; x < alphaWidth; x++)
            {
                float nx = (float)x / (alphaWidth - 1);
                float nz = (float)z / (alphaHeight - 1);

                float steepness = data.GetSteepness(nx, nz);
                float height = data.GetInterpolatedHeight(nx, nz);

                float[] weights = new float[layerCount];
                weights[grassIndex] = 1f;

                if (steepness >= textures.steepRockStart)
                {
                    float rockT = Mathf.InverseLerp(textures.steepRockStart, textures.steepRockFull, steepness);
                    weights[grassIndex] = Mathf.Lerp(1f, 0.08f, rockT);
                    weights[rockIndex] = Mathf.Lerp(0f, 0.92f, rockT);
                }

                if (hasWater)
                {
                    Vector2 point = new Vector2(nx, nz);
                    Vector2 lakeCenter = new Vector2(config.waterCenterX, config.waterCenterZ);
                    float lakeRadiusNormalized = config.waterRadius / Mathf.Max(1f, config.size);
                    float distance = Vector2.Distance(point, lakeCenter);

                    if (distance < lakeRadiusNormalized * textures.sandShoreWidth)
                    {
                        weights[grassIndex] = 0.46f;
                        weights[sandIndex] = 0.46f;
                        weights[rockIndex] = 0.08f;
                    }
                    else if (distance < lakeRadiusNormalized * textures.rockShoreWidth)
                    {
                        weights[grassIndex] = 0.78f;
                        weights[sandIndex] = 0.14f;
                        weights[rockIndex] = 0.08f;
                    }

                    if (height < config.waterLevel + textures.lowHeightSandOffset)
                    {
                        weights[grassIndex] = Mathf.Max(weights[grassIndex], 0.38f);
                        weights[sandIndex] = Mathf.Max(weights[sandIndex], 0.50f);
                    }
                }

                if (textures.pathEnabled && IsOnForestPath(nx, nz, textures, config))
                {
                    weights[grassIndex] = 0.15f;
                    weights[pathIndex] = 0.80f;
                    weights[rockIndex] = Mathf.Max(weights[rockIndex], 0.05f);
                }

                NormalizeWeights(weights);

                for (int layer = 0; layer < layerCount; layer++)
                    alphamaps[z, x, layer] = weights[layer];
            }
        }

        data.SetAlphamaps(0, 0, alphamaps);
    }

    private bool IsOnForestPath(float nx, float nz, TextureConfig textures, TerrainConfig config)
    {
        float curve = 0.5f + Mathf.Sin(nz * Mathf.PI * 2.0f) * textures.pathCurveStrength;
        float width = textures.pathWidth / Mathf.Max(1f, config.size);

        return Mathf.Abs(nx - curve) < width * 0.5f;
    }

    private void NormalizeWeights(float[] weights)
    {
        float total = 0f;

        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        if (total <= 0.0001f)
        {
            weights[0] = 1f;
            return;
        }

        for (int i = 0; i < weights.Length; i++)
            weights[i] /= total;
    }

    private void RefreshTeleportComponents()
    {
        if (targetTerrain == null)
            return;

        Behaviour[] behaviours = targetTerrain.GetComponents<Behaviour>();

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;

            if (typeName.Contains("TeleportationArea") ||
                typeName.Contains("TeleportationAnchor"))
            {
                behaviour.enabled = false;
                behaviour.enabled = true;

                Debug.Log($"[RuntimeTerrainGenerator] Refreshed teleport component: {typeName}");
            }
        }
    }

    public Vector3 GetSafeSpawnPosition(float additionalYOffset = 0.15f)
    {
        if (targetTerrain == null)
            return Vector3.zero;

        Vector3 center = locationCenter != null ? locationCenter.position : Vector3.zero;
        Vector3 spawn = center;

        if (lastResponse != null &&
            lastResponse.audio != null &&
            lastResponse.audio.waterEnabled &&
            lastResponse.terrain != null &&
            lastResponse.terrain.waterRadius > 0.1f)
        {
            TerrainConfig config = lastResponse.terrain;
            TerrainData data = targetTerrain.terrainData;
            Vector3 terrainPos = targetTerrain.transform.position;

            Vector3 lakeCenterWorld = new Vector3(
                terrainPos.x + data.size.x * config.waterCenterX,
                0f,
                terrainPos.z + data.size.z * config.waterCenterZ
            );

            float blockedRadius = config.waterRadius + 5.0f;

            Vector2 center2 = new Vector2(center.x, center.z);
            Vector2 lake2 = new Vector2(lakeCenterWorld.x, lakeCenterWorld.z);

            if (Vector2.Distance(center2, lake2) < blockedRadius)
            {
                Vector2 direction = (center2 - lake2).normalized;

                if (direction.sqrMagnitude < 0.001f)
                    direction = Vector2.right;

                Vector2 safe2 = lake2 + direction * blockedRadius;

                spawn = new Vector3(safe2.x, center.y, safe2.y);
            }
        }

        float y = targetTerrain.SampleHeight(spawn) + targetTerrain.transform.position.y;

        return new Vector3(spawn.x, y + additionalYOffset, spawn.z);
    }

    public bool TryGetTerrainPoint(
        Vector3 worldXZ,
        float maxSlope,
        out Vector3 point,
        out Vector3 normal,
        out float slope)
    {
        point = Vector3.zero;
        normal = Vector3.up;
        slope = 0f;

        if (targetTerrain == null)
            return false;

        TerrainData data = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;

        float nx = (worldXZ.x - terrainPos.x) / data.size.x;
        float nz = (worldXZ.z - terrainPos.z) / data.size.z;

        if (nx < 0f || nx > 1f || nz < 0f || nz > 1f)
            return false;

        float y = targetTerrain.SampleHeight(worldXZ) + terrainPos.y;
        normal = data.GetInterpolatedNormal(nx, nz);
        slope = Vector3.Angle(normal, Vector3.up);

        if (slope > maxSlope)
            return false;

        point = new Vector3(worldXZ.x, y, worldXZ.z);
        return true;
    }
}
