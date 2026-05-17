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
    [SerializeField] private bool forceRuntimeUrpWaterMaterial = true;
    [SerializeField] private bool reduceWaterBrightness = true;
    [SerializeField] private Color fallbackWaterTint = new Color(0.16f, 0.46f, 0.62f, 0.64f);
    [SerializeField] private float fallbackSmoothness = 0.28f;
    [SerializeField] private float fallbackMetallic = 0f;

    [Header("Runtime URP Water")]
    [Tooltip("Used only when Force Runtime Urp Water Material is enabled. Keeps the water mesh/prefab, but replaces problematic shaders with a stable URP transparent material.")]
    [SerializeField] private Color runtimeWaterColor = new Color(0.10f, 0.42f, 0.58f, 0.58f);
    [SerializeField] private float runtimeSmoothness = 0.18f;
    [SerializeField] private float runtimeMetallic = 0f;
    [SerializeField] private bool preserveMainTextureFromSource = true;
    [SerializeField] private bool preserveNormalMapFromSource = true;

    [Header("Audio")]
    [SerializeField] private AudioClip waterLoop;
    [SerializeField] private float waterVolume = 0.35f;
    [SerializeField] private float maxAudioDistance = 25f;

    private GameObject currentWater;
    private AudioSource currentWaterAudio;
    private Material runtimeWaterMaterial;

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
        {
            Debug.LogWarning("[WaterController] Water prefab has no renderers.");
            return;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material sourceMaterial = renderer.sharedMaterial;

            if (sourceMaterial != null)
                Debug.Log($"[WaterController] Source water shader: {sourceMaterial.shader.name}");

            if (forceRuntimeUrpWaterMaterial)
            {
                renderer.sharedMaterial = BuildRuntimeUrpWaterMaterial(sourceMaterial);
            }
            else if (forceMaterialOverride && waterMaterialOverride != null)
            {
                renderer.sharedMaterial = waterMaterialOverride;
            }

            if (reduceWaterBrightness)
                ClampWaterMaterial(renderer);

            if (renderer.sharedMaterial != null)
                Debug.Log($"[WaterController] Final water shader: {renderer.sharedMaterial.shader.name}");
        }
    }

    private Material BuildRuntimeUrpWaterMaterial(Material sourceMaterial)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

        if (urpLit == null)
        {
            Debug.LogWarning("[WaterController] URP Lit shader not found. Falling back to material override/source material.");
            return waterMaterialOverride != null ? waterMaterialOverride : sourceMaterial;
        }

        if (runtimeWaterMaterial != null)
            Destroy(runtimeWaterMaterial);

        runtimeWaterMaterial = new Material(urpLit)
        {
            name = "Runtime_Stable_URP_Water"
        };

        SetupTransparentUrpMaterial(runtimeWaterMaterial);

        runtimeWaterMaterial.SetColor("_BaseColor", runtimeWaterColor);
        runtimeWaterMaterial.SetFloat("_Metallic", runtimeMetallic);
        runtimeWaterMaterial.SetFloat("_Smoothness", runtimeSmoothness);

        Texture mainTexture = TryGetTexture(sourceMaterial, "_BaseMap", "_MainTex", "_BaseColorMap");
        Texture normalTexture = TryGetTexture(sourceMaterial, "_BumpMap", "_NormalMap", "_NormalTex");

        if (preserveMainTextureFromSource && mainTexture != null)
            runtimeWaterMaterial.SetTexture("_BaseMap", mainTexture);

        if (preserveNormalMapFromSource && normalTexture != null)
        {
            runtimeWaterMaterial.SetTexture("_BumpMap", normalTexture);
            runtimeWaterMaterial.EnableKeyword("_NORMALMAP");
        }

        return runtimeWaterMaterial;
    }

    private Texture TryGetTexture(Material material, params string[] propertyNames)
    {
        if (material == null || propertyNames == null)
            return null;

        foreach (string propertyName in propertyNames)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                continue;

            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);

                if (texture != null)
                    return texture;
            }
        }

        return null;
    }

    private void SetupTransparentUrpMaterial(Material material)
    {
        if (material == null)
            return;

        // URP Lit transparent setup.
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
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

        if (material.HasProperty("_Alpha"))
            material.SetFloat("_Alpha", fallbackWaterTint.a);
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

        if (runtimeWaterMaterial != null)
        {
            Destroy(runtimeWaterMaterial);
            runtimeWaterMaterial = null;
        }

        currentWater = null;
        currentWaterAudio = null;
    }
}