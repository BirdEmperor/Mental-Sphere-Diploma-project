using UnityEngine;
using UnityEngine.Rendering;

public class HubEnvironmentRestorer : MonoBehaviour
{
    [Header("Skybox")]
    [SerializeField] private Material hubSkybox;
    [SerializeField] private bool updateDynamicGI = true;

    [Header("Fog")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color fogColor = new Color(0.62f, 0.70f, 0.76f, 1f);
    [SerializeField] private float fogStart = 10f;
    [SerializeField] private float fogEnd = 55f;

    [Header("Lighting")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float lightIntensity = 0.65f;
    [SerializeField] private Color lightColor = new Color(1f, 0.93f, 0.82f, 1f);
    [SerializeField] private Vector3 lightEuler = new Vector3(35f, -30f, 0f);
    [SerializeField] private Color ambientColor = new Color(0.55f, 0.62f, 0.68f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioClip hubAmbientClip;
    [SerializeField] private float ambientVolume = 0.18f;

    private void OnEnable()
    {
        RestoreHubEnvironment();
    }

    public void RestoreHubEnvironment()
    {
        if (hubSkybox != null)
        {
            RenderSettings.skybox = hubSkybox;

            if (updateDynamicGI)
                DynamicGI.UpdateEnvironment();
        }

        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        if (directionalLight != null)
        {
            directionalLight.intensity = lightIntensity;
            directionalLight.color = lightColor;
            directionalLight.transform.rotation = Quaternion.Euler(lightEuler);
        }

        if (ambientAudioSource != null)
        {
            if (hubAmbientClip == null)
            {
                ambientAudioSource.Stop();
                ambientAudioSource.clip = null;
            }
            else
            {
                ambientAudioSource.clip = hubAmbientClip;
                ambientAudioSource.loop = true;
                ambientAudioSource.spatialBlend = 0f;
                ambientAudioSource.volume = ambientVolume;
                ambientAudioSource.Play();
            }
        }

        Debug.Log("[HubEnvironmentRestorer] Hub environment restored.");
    }
}
