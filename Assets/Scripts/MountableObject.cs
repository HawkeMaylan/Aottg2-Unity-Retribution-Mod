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

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.IsMine())
        {
            humanInTrigger = human;
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
        if (humanInTrigger == null)
            return;

        Transform humanRoot = humanInTrigger.transform;

        // Set parent to mount point
        humanRoot.SetParent(mountPoint);

        // Set position and rotation with offsets
        humanRoot.localPosition = positionOffset;
        humanRoot.localEulerAngles = rotationOffset;

        // Freeze movement
        Rigidbody rb = humanRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        // Play mount animation if you want (optional)
        humanInTrigger.PlayAnimation(HumanAnimations.HorseMount);

        isMounted = true;
        Debug.Log("[DirectMount] Human attached to mount point.");
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null)
            return;

        Transform humanRoot = humanInTrigger.transform;

        // Unparent
        humanRoot.SetParent(null);

        // Re-enable physics
        Rigidbody rb = humanRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        // Play idle animation if you want (optional)
        ///humanInTrigger.PlayAnimation(HumanAnimations.Idle);

        isMounted = false;
        Debug.Log("[DirectMount] Human detached from mount point.");
    }
}
