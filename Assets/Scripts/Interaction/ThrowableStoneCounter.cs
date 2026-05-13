using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class ThrowableStoneCounter : MonoBehaviour
{
    public static int TotalReleasedStones { get; private set; }

    [Header("Counter")]
    [SerializeField] private bool countOnlyAfterGrab = true;

    private XRGrabInteractable grabInteractable;
    private bool wasGrabbed;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

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

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        wasGrabbed = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (countOnlyAfterGrab && !wasGrabbed)
            return;

        TotalReleasedStones++;
        wasGrabbed = false;

        Debug.Log($"[ThrowableStoneCounter] Released stones: {TotalReleasedStones}");
    }

    public static void ResetCounter()
    {
        TotalReleasedStones = 0;
    }
}