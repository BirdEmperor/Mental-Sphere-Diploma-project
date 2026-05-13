using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickupPhysicsNormalizer : MonoBehaviour
{
    [Header("Collider")]
    [SerializeField] private bool createRootSphereCollider = true;
    [SerializeField] private float colliderRadius = 0.18f;
    [SerializeField] private Vector3 colliderCenter = new Vector3(0f, 0.12f, 0f);
    [SerializeField] private PhysicsMaterial physicsMaterial;

    [Header("Rigidbody")]
    [SerializeField] private float mass = 0.25f;
    [SerializeField] private float drag = 0.05f;
    [SerializeField] private float angularDrag = 0.05f;

    private void Awake()
    {
        NormalizeRigidbody();
        NormalizeColliders();
    }

    private void NormalizeRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.solverIterations = 12;
        rb.solverVelocityIterations = 12;
        rb.maxAngularVelocity = 8f;
    }

    private void NormalizeColliders()
    {
        Collider[] existingColliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in existingColliders)
        {
            if (col == null)
                continue;

            col.isTrigger = false;

            if (physicsMaterial != null)
                col.material = physicsMaterial;

            MeshCollider meshCollider = col as MeshCollider;
            if (meshCollider != null)
                meshCollider.convex = true;
        }

        if (!createRootSphereCollider)
            return;

        SphereCollider rootCollider = GetComponent<SphereCollider>();

        if (rootCollider == null)
            rootCollider = gameObject.AddComponent<SphereCollider>();

        rootCollider.isTrigger = false;
        rootCollider.center = colliderCenter;
        rootCollider.radius = colliderRadius;

        if (physicsMaterial != null)
            rootCollider.material = physicsMaterial;
    }
}