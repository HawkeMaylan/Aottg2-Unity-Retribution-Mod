using UnityEngine;

public class AttachToHorseTrigger : MonoBehaviour
{
    [Header("Offset from horse when attaching")]
    public Vector3 attachOffset = new Vector3(0f, 0f, -2f);

    private bool isAttached = false;
    private Transform horseRootInContact;
    private Transform attachedHorse;
    private Rigidbody rb;
    private Transform wagon;

    private void Start()
    {
        wagon = transform.root;
        rb = wagon.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!isAttached && horseRootInContact != null)
            {
                AttachToHorse(horseRootInContact);
            }
            else if (isAttached)
            {
                DetachFromHorse();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "HorseTrigger")
        {
            horseRootInContact = other.transform.root;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "HorseTrigger" && other.transform.root == horseRootInContact)
        {
            horseRootInContact = null;
        }
    }

    private void AttachToHorse(Transform horseRoot)
    {
        if (horseRoot == null) return;

        wagon.SetParent(horseRoot);
        wagon.localPosition = attachOffset;
        wagon.localRotation = Quaternion.identity;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        isAttached = true;
        attachedHorse = horseRoot;
        Debug.Log("Wagon attached to horse.");
    }

    private void DetachFromHorse()
    {
        wagon.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        isAttached = false;
        attachedHorse = null;
        Debug.Log("Wagon detached from horse.");
    }
}
