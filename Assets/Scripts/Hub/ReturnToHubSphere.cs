using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ReturnToHubSphere : MonoBehaviour
{
    [Header("Return Target")]
    [SerializeField] private GameManager gameManager;

    [Header("Activation")]
    [SerializeField] private float activationDelay = 0.35f;
    [SerializeField] private bool allowRepeatedUse = true;

    private XRGrabInteractable grabInteractable;
    private bool activationRunning;
    private Coroutine activationRoutine;

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

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (activationRunning)
            return;

        activationRoutine = StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        activationRunning = true;

        if (activationDelay > 0f)
            yield return new WaitForSeconds(activationDelay);

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager != null)
            gameManager.FinishCurrentSession();
        else
            Debug.LogError("[ReturnToHubSphere] GameManager not found. Cannot return to Hub.");

        if (allowRepeatedUse)
            activationRunning = false;

        activationRoutine = null;
    }

    public void ResetReturnSphere()
    {
        activationRunning = false;

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }
    }
}