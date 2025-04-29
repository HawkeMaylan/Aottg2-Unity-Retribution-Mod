using UnityEngine;
using Characters;

public class DirectMount : MonoBehaviour
{
    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public float maxDriftDistance = 1.5f;

    private Human humanInTrigger;
    private bool isMounted = false;
    private Rigidbody rb;

    // Backup Rigidbody settings
    private float originalMass;
    private float originalDrag;
    private float originalAngularDrag;
    private bool originalUseGravity;
    private RigidbodyInterpolation originalInterpolation;
    private CollisionDetectionMode originalCollisionMode;
    private RigidbodyConstraints originalConstraints;

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.IsMine())
        {
            humanInTrigger = human;
            rb = human.GetComponent<Rigidbody>();
            Debug.Log("[DirectMount] Human entered: " + human.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Do NOT unmount — just re-align if necessary
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human == humanInTrigger)
        {
            Debug.Log("[DirectMount] Human exited trigger zone — will reattach if needed.");
        }
    }

    private void Update()
    {
        if (humanInTrigger != null && Input.GetKeyDown(KeyCode.G))
        {
            if (!isMounted)
                AttachHuman();
            else
                DetachHuman();
        }

        if (isMounted && humanInTrigger != null)
        {
            Transform root = humanInTrigger.transform;
            Vector3 expectedWorldPos = mountPoint.TransformPoint(positionOffset);
            Quaternion expectedWorldRot = mountPoint.rotation * Quaternion.Euler(rotationOffset);

            float distance = Vector3.Distance(root.position, expectedWorldPos);
            if (distance > maxDriftDistance)
            {
                Debug.LogWarning("[DirectMount] Drifted — reattaching to mount.");
                ReMountHuman();
            }
        }
    }

    private void AttachHuman()
    {
        if (humanInTrigger == null || rb == null) return;

        Transform root = humanInTrigger.transform;

        // Backup Rigidbody settings
        originalMass = rb.mass;
        originalDrag = rb.drag;
        originalAngularDrag = rb.angularDrag;
        originalUseGravity = rb.useGravity;
        originalInterpolation = rb.interpolation;
        originalCollisionMode = rb.collisionDetectionMode;
        originalConstraints = rb.constraints;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        root.SetParent(mountPoint);
        root.localPosition = positionOffset;
        root.localEulerAngles = rotationOffset;

        rb.mass = 1e-07f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        humanInTrigger.PlayAnimation(HumanAnimations.HorseMount);
        isMounted = true;

        Debug.Log("[DirectMount] Mounted.");
    }

    private void ReMountHuman()
    {
        if (humanInTrigger == null || rb == null) return;

        Transform root = humanInTrigger.transform;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        root.SetParent(mountPoint);
        root.localPosition = positionOffset;
        root.localEulerAngles = rotationOffset;

        Debug.Log("[DirectMount] Re-mounted to correct position.");
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null || rb == null) return;

        Transform root = humanInTrigger.transform;
        root.SetParent(null);

        rb.mass = originalMass;
        rb.drag = originalDrag;
        rb.angularDrag = originalAngularDrag;
        rb.useGravity = originalUseGravity;
        rb.isKinematic = false;
        rb.interpolation = originalInterpolation;
        rb.collisionDetectionMode = originalCollisionMode;
        rb.constraints = originalConstraints;

        root.position += Vector3.down * 0.05f;
        isMounted = false;

        Debug.Log("[DirectMount] Unmounted.");
    }
}
