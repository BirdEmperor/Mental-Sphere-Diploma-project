using System;

[Serializable]
public class GenerationResponse
{
    public string profileId;
    public string sphereColor;
    public int seed;
    public string biome;

    public TerrainConfig terrain;
    public ObjectCounts objects;
    public GenerationRules rules;
    public AtmosphereConfig atmosphere;
    public AudioConfig audio;

    public TextureConfig textures;
    public LightingConfig lighting;
    public SkyboxConfig skybox;
    public EffectCounts effects;
    public InteractiveCounts interactives;
}

[Serializable]
public class TerrainConfig
{
    public float size = 48f;
    public float terrainHeight = 4.5f;
    public float heightMultiplier = 1f;
    public float noiseScale = 5f;
    public float flattenSpawnRadius = 5f;

    public float waterLevel = 0.42f;
    public float waterRadius = 0f;
    public float waterCenterX = 0.30f;
    public float waterCenterZ = 0.36f;
    public float lakeDepth = 0.25f;
}

[Serializable]
public class ObjectCounts
{
    public int treeCount = 0;
    public int rockCount = 0;
    public int cliffCount = 0;
    public int bushCount = 0;
    public int grassPatchCount = 0;
    public int flowerPatchCount = 0;
    public int reedCount = 0;
    public int branchCount = 0;
}

[Serializable]
public class GenerationRules
{
    public float safeRadius = 4f;
    public float treeMaxSlope = 15f;
    public float rockMaxSlope = 25f;
    public float cliffMaxSlope = 30f;
    public float bushMaxSlope = 18f;
    public float grassMaxSlope = 25f;
    public float flowerMaxSlope = 22f;
    public float minObjectDistance = 0.35f;
    public float waterAvoidanceMargin = 0.65f;
}

[Serializable]
public class AtmosphereConfig
{
    public string timeOfDay = "morning";
    public string fogColor = "#A8C8E0";
    public float fogStart = 12f;
    public float fogEnd = 58f;
    public float lightIntensity = 0.85f;
}

[Serializable]
public class AudioConfig
{
    public string ambientSound = "forest";
    public string waterSound = "lake";
    public bool waterEnabled = false;
    public float windIntensity = 0.20f;
}

[Serializable]
public class TextureConfig
{
    public int grassLayerIndex = 0;
    public int sandLayerIndex = 1;
    public int rockLayerIndex = 2;

    public float sandShoreWidth = 1.12f;
    public float rockShoreWidth = 1.32f;

    public float steepRockStart = 22f;
    public float steepRockFull = 34f;

    public float lowHeightSandOffset = 0.06f;

    public bool pathEnabled = false;
    public int pathLayerIndex = 1;
    public float pathWidth = 2.8f;
    public float pathCurveStrength = 0.22f;
}

[Serializable]
public class LightingConfig
{
    public string timeOfDay = "morning";

    public float lightIntensity = 0.85f;
    public float lightRotationX = 35f;
    public float lightRotationY = -30f;

    public string lightColor = "#FFEAC7";
    public string ambientColor = "#8FAFC8";
}

[Serializable]
public class SkyboxConfig
{
    public string skyboxId = "default_morning";
    public string fogColor = "#A8C8E0";
    public float fogStart = 12f;
    public float fogEnd = 58f;
}

[Serializable]
public class EffectCounts
{
    public int floatingLeavesCount = 0;
    public int floatingPetalsCount = 0;
    public int butterflySpawnAreas = 0;
    public int godRayCount = 0;
}

[Serializable]
public class InteractiveCounts
{
    public int pickupStoneCount = 0;
}
