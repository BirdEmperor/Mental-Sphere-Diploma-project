using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class SphereGlowFeedback : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Emission")]
    [SerializeField] private Color baseEmissionColor = new Color(0.02f, 0.04f, 0.06f);
    [SerializeField] private Color grabbedEmissionColor = new Color(0.25f, 0.75f, 1.0f);
    [SerializeField] private float baseIntensity = 0.15f;
    [SerializeField] private float grabbedIntensity = 2.2f;
    [SerializeField] private float pulseSpeed = 2.8f;

    private XRGrabInteractable grabInteractable;
    private Material[][] runtimeMaterials;
    private bool isGrabbed;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        runtimeMaterials = new Material[targetRenderers.Length][];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null)
                continue;

            runtimeMaterials[i] = targetRenderers[i].materials;

            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                if (runtimeMaterials[i][j] == null)
                    continue;

                runtimeMaterials[i][j].EnableKeyword("_EMISSION");
                runtimeMaterials[i][j].SetColor("_EmissionColor", baseEmissionColor * baseIntensity);
            }
        }

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Update()
    {
        if (!isGrabbed)
            return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        float intensity = Mathf.Lerp(grabbedIntensity * 0.65f, grabbedIntensity, pulse);

        ApplyEmission(grabbedEmissionColor, intensity);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        ApplyEmission(grabbedEmissionColor, grabbedIntensity);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        ApplyEmission(baseEmissionColor, baseIntensity);
    }

    private void ApplyEmission(Color color, float intensity)
    {
        if (runtimeMaterials == null)
            return;

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            if (runtimeMaterials[i] == null)
                continue;

            for (int j = 0; j < runtimeMaterials[i].Length; j++)
            {
                if (runtimeMaterials[i][j] == null)
                    continue;

                runtimeMaterials[i][j].SetColor("_EmissionColor", color * intensity);
            }
        }
    }
}