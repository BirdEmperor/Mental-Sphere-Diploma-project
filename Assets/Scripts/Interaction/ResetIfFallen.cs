using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ResetIfFallen : MonoBehaviour
{
    [Header("Fall Reset")]
    [SerializeField] private float minY = -5f;
    [SerializeField] private bool resetScale = true;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;
    }

    private void Update()
    {
        if (transform.position.y < minY)
            ResetObject();
    }

    public void ResetObject()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        transform.position = startPosition;
        transform.rotation = startRotation;

        if (resetScale)
            transform.localScale = startScale;

        if (rb != null)
            rb.WakeUp();
    }
}