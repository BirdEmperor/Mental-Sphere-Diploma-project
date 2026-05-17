using UnityEngine;
using UnityEngine.Rendering;

public class BiomeApplicator : MonoBehaviour
{
    [Header("Lighting")]
    [SerializeField] private Light directionalLight;

    [Header("Runtime Light Safety")]
    [SerializeField] private bool clampRuntimeLighting = true;
    [SerializeField] private float maxDirectionalLightIntensity = 0.75f;
    [SerializeField] private float lakeMaxDirectionalLightIntensity = 0.55f;
    [SerializeField] private float maxReflectionIntensity = 0.45f;
    [SerializeField] private float lakeReflectionIntensity = 0.22f;
    [SerializeField] private float ambientColorMultiplier = 0.75f;
    [SerializeField] private float lakeAmbientColorMultiplier = 0.52f;

    [Header("Skyboxes")]
    [SerializeField] private Material defaultMorningSkybox;
    [SerializeField] private Material lakeSkybox;
    [SerializeField] private Material forestSkybox;
    [SerializeField] private Material fieldSkybox;
    [SerializeField] private Material eveningSkybox;

    [Header("Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioClip lakeAmbient;
    [SerializeField] private AudioClip forestAmbient;
    [SerializeField] private AudioClip fieldAmbient;
    [SerializeField] private AudioClip nightAmbient;
    [SerializeField] private float ambientVolume = 0.25f;

    public void ApplyBiome(GenerationResponse response)
    {
        if (response == null)
            return;

        ApplyLight(response);
        ApplySkyboxAndFog(response);
        ApplyAmbientAudio(response);

        Debug.Log($"[BiomeApplicator] Biome applied: {response.biome}");
    }

    private void ApplyLight(GenerationResponse response)
    {
        LightingConfig lighting = response.lighting ?? new LightingConfig();
        string biome = Normalize(response.biome);
        bool isLake = biome == "lake";

        if (directionalLight != null)
        {
            float targetIntensity = lighting.lightIntensity;

            if (clampRuntimeLighting)
            {
                float maxIntensity = isLake ? lakeMaxDirectionalLightIntensity : maxDirectionalLightIntensity;
                targetIntensity = Mathf.Min(targetIntensity, maxIntensity);
            }

            directionalLight.intensity = targetIntensity;
            directionalLight.transform.rotation = Quaternion.Euler(
                lighting.lightRotationX,
                lighting.lightRotationY,
                0f
            );

            if (ColorUtility.TryParseHtmlString(lighting.lightColor, out Color lightColor))
            {
                if (clampRuntimeLighting)
                    lightColor = ClampColorBrightness(lightColor, isLake ? 0.82f : 0.95f);

                directionalLight.color = lightColor;
            }
        }

        if (ColorUtility.TryParseHtmlString(lighting.ambientColor, out Color ambientColor))
        {
            RenderSettings.ambientMode = AmbientMode.Flat;

            if (clampRuntimeLighting)
            {
                float multiplier = isLake ? lakeAmbientColorMultiplier : ambientColorMultiplier;
                ambientColor *= multiplier;
                ambientColor.a = 1f;
            }

            RenderSettings.ambientLight = ambientColor;
        }

        if (clampRuntimeLighting)
        {
            RenderSettings.reflectionIntensity = isLake
                ? lakeReflectionIntensity
                : Mathf.Min(RenderSettings.reflectionIntensity, maxReflectionIntensity);
        }
    }

    private void ApplySkyboxAndFog(GenerationResponse response)
    {
        SkyboxConfig skybox = response.skybox ?? new SkyboxConfig();

        Material selectedSkybox = skybox.skyboxId switch
        {
            "lake_morning" => lakeSkybox,
            "forest_day" => forestSkybox,
            "field_warm" => fieldSkybox,
            "evening" => eveningSkybox,
            _ => defaultMorningSkybox
        };

        if (selectedSkybox != null)
        {
            RenderSettings.skybox = selectedSkybox;
            DynamicGI.UpdateEnvironment();
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = skybox.fogStart;
        RenderSettings.fogEndDistance = skybox.fogEnd;

        if (ColorUtility.TryParseHtmlString(skybox.fogColor, out Color fogColor))
            RenderSettings.fogColor = fogColor;
    }

    private void ApplyAmbientAudio(GenerationResponse response)
    {
        if (ambientAudioSource == null || response.audio == null)
            return;

        AudioClip selectedClip = response.audio.ambientSound switch
        {
            "lake" => lakeAmbient,
            "forest" => forestAmbient,
            "field" => fieldAmbient,
            "night" => nightAmbient,
            _ => null
        };

        if (selectedClip == null)
        {
            ambientAudioSource.Stop();
            ambientAudioSource.clip = null;
            return;
        }

        ambientAudioSource.clip = selectedClip;
        ambientAudioSource.loop = true;
        ambientAudioSource.spatialBlend = 0f;
        ambientAudioSource.volume = ambientVolume;
        ambientAudioSource.Play();
    }

    private string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }

    private Color ClampColorBrightness(Color color, float maxChannelValue)
    {
        color.r = Mathf.Min(color.r, maxChannelValue);
        color.g = Mathf.Min(color.g, maxChannelValue);
        color.b = Mathf.Min(color.b, maxChannelValue);
        color.a = 1f;

        return color;
    }
}