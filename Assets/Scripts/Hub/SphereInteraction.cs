using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class SphereInteraction : MonoBehaviour
{
    [Header("Sphere Profile")]
    [SerializeField] private string sphereColor = "blue";
    [SerializeField] private float activationDelay = 0.45f;

    [Header("Activation Event")]
    public UnityEvent<string> onSphereActivated;

    private bool hasTriggered;
    private Coroutine activationRoutine;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (hasTriggered)
            return;

        hasTriggered = true;

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);

        activationRoutine = StartCoroutine(ActivateAfterDelay());
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(activationDelay);

        string color = string.IsNullOrWhiteSpace(sphereColor)
            ? "blue"
            : sphereColor.Trim().ToLowerInvariant();

        Debug.Log($"[SphereInteraction] Sphere activated: {color}");

        onSphereActivated?.Invoke(color);

        activationRoutine = null;
    }

    public void ResetSphere()
    {
        hasTriggered = false;

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        Debug.Log($"[SphereInteraction] Sphere reset: {sphereColor}");
    }
}