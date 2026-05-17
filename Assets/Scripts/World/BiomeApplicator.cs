using UnityEngine;
using UnityEngine.Rendering;

public class BiomeApplicator : MonoBehaviour
{
    [Header("Lighting")]
    [SerializeField] private Light directionalLight;

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
        if (directionalLight == null)
            return;

        LightingConfig lighting = response.lighting ?? new LightingConfig();

        directionalLight.intensity = lighting.lightIntensity;
        directionalLight.transform.rotation = Quaternion.Euler(
            lighting.lightRotationX,
            lighting.lightRotationY,
            0f
        );

        if (ColorUtility.TryParseHtmlString(lighting.lightColor, out Color lightColor))
            directionalLight.color = lightColor;

        if (ColorUtility.TryParseHtmlString(lighting.ambientColor, out Color ambientColor))
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
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
}