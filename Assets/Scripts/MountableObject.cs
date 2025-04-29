using UnityEngine;
using Characters;

public class DirectMount : MonoBehaviour
{
    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public float maxDriftDistance = 1.5f; // Safety check if player drifts away

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
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human == humanInTrigger)
        {
            Debug.Log("[DirectMount] Human exited: " + human.name);
            humanInTrigger = null;
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

        // Auto unmount if player drifts too far from mountPoint
        if (isMounted && humanInTrigger != null)
        {
            Vector3 targetPos = mountPoint.position + mountPoint.TransformVector(positionOffset);
            float distance = Vector3.Distance(humanInTrigger.transform.position, targetPos);

            if (distance > maxDriftDistance)
            {
                Debug.LogWarning("[DirectMount] Auto-unmount: drifted too far from mount.");
                DetachHuman();
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

        // Prep Rigidbody (disable forces before parenting!)
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // prevent fling during parenting

        // Parent and snap to mount
        root.SetParent(mountPoint);
        root.localPosition = positionOffset;
        root.localEulerAngles = rotationOffset;

        // Apply visual Rigidbody overrides (you may skip most of these if they’re not needed)
        rb.mass = 1e-07f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // better for mounting
        rb.constraints = RigidbodyConstraints.FreezeAll;

        humanInTrigger.PlayAnimation(HumanAnimations.HorseMount);
        isMounted = true;

        Debug.Log("[DirectMount] Mounted with safer Rigidbody settings.");
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null || rb == null) return;

        Transform root = humanInTrigger.transform;
        root.SetParent(null);

        // Restore original Rigidbody settings
        rb.mass = originalMass;
        rb.drag = originalDrag;
        rb.angularDrag = originalAngularDrag;
        rb.useGravity = originalUseGravity;
        rb.isKinematic = false; // must be explicitly off again
        rb.interpolation = originalInterpolation;
        rb.collisionDetectionMode = originalCollisionMode;
        rb.constraints = originalConstraints;

        // Nudge downward slightly to prevent midair "float" bug
        root.position += Vector3.down * 0.05f;

        //humanInTrigger.PlayAnimation(HumanAnimations.Idle);
        isMounted = false;

        Debug.Log("[DirectMount] Unmounted and Rigidbody restored.");
    }
}
