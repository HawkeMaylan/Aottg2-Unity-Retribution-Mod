using UnityEngine;
using Characters;

public class DirectMount : MonoBehaviour
{
    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

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

        // Apply mounted settings
        rb.mass = 1e-07f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.constraints = RigidbodyConstraints.FreezeAll;

        // Parent and align
        root.SetParent(mountPoint);
        root.localPosition = positionOffset;
        root.localEulerAngles = rotationOffset;

        humanInTrigger.PlayAnimation(HumanAnimations.HorseMount);
        isMounted = true;

        Debug.Log("[DirectMount] Mounted with Rigidbody override.");
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null || rb == null) return;

        Transform root = humanInTrigger.transform;
        root.SetParent(null);

        // Restore original settings and force isKinematic off
        rb.mass = originalMass;
        rb.drag = originalDrag;
        rb.angularDrag = originalAngularDrag;
        rb.useGravity = originalUseGravity;
        rb.isKinematic = false; // Force off even if it was true before
        rb.interpolation = originalInterpolation;
        rb.collisionDetectionMode = originalCollisionMode;
        rb.constraints = originalConstraints;

        ///humanInTrigger.PlayAnimation(HumanAnimations.armature|horse_idle);
        isMounted = false;

        Debug.Log("[DirectMount] Unmounted and Rigidbody restored with isKinematic OFF.");
    }
}
