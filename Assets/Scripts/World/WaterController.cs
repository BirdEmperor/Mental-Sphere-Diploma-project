using UnityEngine;

public class WaterController : MonoBehaviour
{
    [System.Serializable]
    public struct LakeData
    {
        public bool enabled;
        public Vector3 center;
        public float radius;
        public float waterY;
    }

    [Header("Water")]
    [SerializeField] private GameObject waterPrefab;
    [SerializeField] private Transform waterRoot;
    [SerializeField] private Material waterMaterialOverride;

    [Tooltip("Negative value lowers water into the terrain depression.")]
    [SerializeField] private float waterYOffset = -0.08f;

    [Tooltip("Visible water diameter multiplier relative to waterRadius.")]
    [SerializeField] private float visibleDiameterMultiplier = 2.25f;

    [Header("Water Visual Safety")]
    [SerializeField] private bool forceMaterialOverride = true;
    [SerializeField] private bool reduceWaterBrightness = true;
    [SerializeField] private Color fallbackWaterTint = new Color(0.20f, 0.55f, 0.72f, 0.72f);
    [SerializeField] private float fallbackSmoothness = 0.45f;
    [SerializeField] private float fallbackMetallic = 0f;

    [Header("Audio")]
    [SerializeField] private AudioClip waterLoop;
    [SerializeField] private float waterVolume = 0.35f;
    [SerializeField] private float maxAudioDistance = 25f;

    private GameObject currentWater;
    private AudioSource currentWaterAudio;

    public LakeData CurrentLake { get; private set; }

    public void ApplyWater(GenerationResponse response, Terrain terrain)
    {
        ClearWater();

        CurrentLake = new LakeData
        {
            enabled = false,
            center = Vector3.zero,
            radius = 0f,
            waterY = 0f
        };

        if (response == null || response.terrain == null || response.audio == null)
            return;

        bool waterEnabled = response.audio.waterEnabled && response.terrain.waterRadius > 0.1f;

        if (!waterEnabled || waterPrefab == null || terrain == null)
            return;

        TerrainConfig config = response.terrain;
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        float centerX = terrainPos.x + data.size.x * config.waterCenterX;
        float centerZ = terrainPos.z + data.size.z * config.waterCenterZ;
        float waterY = terrainPos.y + config.waterLevel + waterYOffset;

        Vector3 waterCenter = new Vector3(centerX, waterY, centerZ);
        Transform parent = waterRoot != null ? waterRoot : transform;

        currentWater = Instantiate(waterPrefab, waterCenter, Quaternion.identity, parent);
        currentWater.name = "Generated_Water";

        ApplyWaterMaterials(currentWater);

        float desiredDiameter = config.waterRadius * visibleDiameterMultiplier;
        ResizeWaterByRendererBounds(currentWater, desiredDiameter);

        SetupWaterAudio(currentWater);

        float visibleRadius = desiredDiameter * 0.5f;

        CurrentLake = new LakeData
        {
            enabled = true,
            center = waterCenter,
            radius = visibleRadius,
            waterY = waterY
        };

        Debug.Log($"[WaterController] Water created. Radius={config.waterRadius}, VisibleRadius={visibleRadius}, DesiredDiameter={desiredDiameter}");
    }

    private void ApplyWaterMaterials(GameObject waterObject)
    {
        if (waterObject == null)
            return;

        Renderer[] renderers = waterObject.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
            return;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            if (forceMaterialOverride && waterMaterialOverride != null)
            {
                renderer.sharedMaterial = waterMaterialOverride;
            }

            if (reduceWaterBrightness)
                ClampWaterMaterial(renderer);
        }
    }

    private void ClampWaterMaterial(Renderer renderer)
    {
        if (renderer == null || renderer.sharedMaterial == null)
            return;

        Material material = renderer.material;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", fallbackWaterTint);
        else if (material.HasProperty("_Color"))
            material.SetColor("_Color", fallbackWaterTint);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", fallbackSmoothness);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", fallbackMetallic);

        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", Color.black);
    }

    private void ResizeWaterByRendererBounds(GameObject waterObject, float desiredDiameter)
    {
        if (waterObject == null)
            return;

        Renderer[] renderers = waterObject.GetComponentsInChildren<Renderer>();

        if (renderers == null || renderers.Length == 0)
        {
            waterObject.transform.localScale = Vector3.one * desiredDiameter;
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float currentDiameter = Mathf.Max(bounds.size.x, bounds.size.z);

        if (currentDiameter <= 0.001f)
        {
            waterObject.transform.localScale = Vector3.one * desiredDiameter;
            return;
        }

        float scaleFactor = desiredDiameter / currentDiameter;
        waterObject.transform.localScale *= scaleFactor;

        Debug.Log($"[WaterController] Water resized. CurrentDiameter={currentDiameter}, ScaleFactor={scaleFactor}");
    }

    private void SetupWaterAudio(GameObject waterObject)
    {
        if (waterLoop == null)
            return;

        currentWaterAudio = waterObject.AddComponent<AudioSource>();
        currentWaterAudio.clip = waterLoop;
        currentWaterAudio.loop = true;
        currentWaterAudio.playOnAwake = true;
        currentWaterAudio.spatialBlend = 1f;
        currentWaterAudio.volume = waterVolume;
        currentWaterAudio.rolloffMode = AudioRolloffMode.Linear;
        currentWaterAudio.minDistance = 2f;
        currentWaterAudio.maxDistance = maxAudioDistance;
        currentWaterAudio.Play();
    }

    public bool IsInsideLake(Vector3 position, float margin = 0f)
    {
        if (!CurrentLake.enabled)
            return false;

        Vector2 a = new Vector2(position.x, position.z);
        Vector2 b = new Vector2(CurrentLake.center.x, CurrentLake.center.z);

        return Vector2.Distance(a, b) <= CurrentLake.radius + margin;
    }

    public bool IsNearLakeShore(Vector3 position, float innerMargin, float outerMargin)
    {
        if (!CurrentLake.enabled)
            return false;

        Vector2 a = new Vector2(position.x, position.z);
        Vector2 b = new Vector2(CurrentLake.center.x, CurrentLake.center.z);

        float distance = Vector2.Distance(a, b);

        return distance >= CurrentLake.radius + innerMargin &&
               distance <= CurrentLake.radius + outerMargin;
    }

    private void ClearWater()
    {
        if (currentWater != null)
            Destroy(currentWater);

        currentWater = null;
        currentWaterAudio = null;
    }
}