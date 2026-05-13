using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class ReturnToStartAfterRelease : MonoBehaviour
{
    [Header("Return Settings")]
    [SerializeField] private float returnDelay = 3f;
    [SerializeField] private float returnMoveDuration = 0.5f;
    [SerializeField] private bool resetVelocity = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Coroutine returnCoroutine;

    private bool returnPointInitialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectExited.AddListener(OnReleased);
        grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void Start()
    {
        if (!returnPointInitialized)
            SetCurrentTransformAsReturnPoint();
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnReleased);
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
        }
    }

    public void SetCurrentTransformAsReturnPoint()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
        returnPointInitialized = true;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (returnCoroutine != null)
            StopCoroutine(returnCoroutine);

        returnCoroutine = StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        yield return new WaitForSeconds(returnDelay);

        if (resetVelocity)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = true;

        Vector3 fromPosition = transform.position;
        Quaternion fromRotation = transform.rotation;
        Vector3 fromScale = transform.localScale;

        float time = 0f;

        while (time < returnMoveDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / returnMoveDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(fromPosition, startPosition, t);
            transform.rotation = Quaternion.Slerp(fromRotation, startRotation, t);
            transform.localScale = Vector3.Lerp(fromScale, startScale, t);

            yield return null;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        rb.isKinematic = false;

        if (resetVelocity)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        returnCoroutine = null;
    }

    public void ForceReturn()
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        if (resetVelocity && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;

        if (rb != null)
            rb.isKinematic = false;
    }
}