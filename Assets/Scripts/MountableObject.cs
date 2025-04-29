using UnityEngine;
using Characters;

public class DirectMount : MonoBehaviour
{
    [Header("Mount Target")]
    public Transform mountPoint;

    private Human humanInTrigger;

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null)
        {
            humanInTrigger = human;
            Debug.Log("[DirectMount] Human entered: " + human.name);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human == humanInTrigger)
        {
            Debug.Log("[DirectMount] Human exited: " + human.name);
            humanInTrigger = null;
        }
    }

    private void Update()
    {
        if (humanInTrigger != null && Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("[DirectMount] Parenting human to mount point.");

            Transform humanRoot = humanInTrigger.transform;
            humanRoot.SetParent(mountPoint);
            humanRoot.localPosition = Vector3.zero;
            humanRoot.localRotation = Quaternion.identity;

            // Optional: disable gravity and movement if you want
            Rigidbody rb = humanRoot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }
    }
}
