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
    [SerializeField] private float resetAfterActivationSeconds = 1.5f;

    [Header("Routing")]
    [Tooltip("Recommended: ON. Sends this sphere color directly to GameManager and avoids broken/static UnityEvent bindings in Inspector.")]
    [SerializeField] private bool sendDirectlyToGameManager = true;

    [SerializeField] private GameManager gameManager;

    [Tooltip("Usually OFF. Enable only if you intentionally need extra listeners in Inspector.")]
    [SerializeField] private bool alsoInvokeUnityEvent = false;

    [Header("Activation Event")]
    public UnityEvent<string> onSphereActivated;

    private bool hasTriggered;
    private Coroutine activationRoutine;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);

        if (activationRoutine != null)
            StopCoroutine(activationRoutine);
    }

    private void OnDisable()
    {
        ResetSphere();
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

        string color = GetNormalizedColor();

        Debug.Log($"[SphereInteraction] Sphere activated. Object='{name}', color='{color}'");

        if (sendDirectlyToGameManager)
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            if (gameManager != null)
                gameManager.OnSphereActivated(color);
            else
                Debug.LogError($"[SphereInteraction] GameManager not found. Cannot activate sphere color '{color}'.");
        }

        if (!sendDirectlyToGameManager || alsoInvokeUnityEvent)
            onSphereActivated?.Invoke(color);

        activationRoutine = null;

        if (resetAfterActivationSeconds > 0f)
            StartCoroutine(ResetAfterDelay(resetAfterActivationSeconds));
    }

    private IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetSphere();
    }

    private string GetNormalizedColor()
    {
        return string.IsNullOrWhiteSpace(sphereColor)
            ? "blue"
            : sphereColor.Trim().ToLowerInvariant();
    }

    public void ResetSphere()
    {
        hasTriggered = false;

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }
    }

    public void SetSphereColor(string color)
    {
        sphereColor = string.IsNullOrWhiteSpace(color)
            ? "blue"
            : color.Trim().ToLowerInvariant();
    }
}
