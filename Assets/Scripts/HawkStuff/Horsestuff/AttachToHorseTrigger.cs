using UnityEngine;

public class AttachToHorseTrigger : MonoBehaviour
{
    [Header("Offset from horse when attaching")]
    public Vector3 attachOffset = new Vector3(0f, 0f, -2f);

    private bool isAttached = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isAttached) return;

        if (other.name == "HorseTrigger") // Only attach if it hits that specific trigger
        {
            // Find the root object of the trigger (should be the horse)
            Transform horseRoot = other.transform.root;

            if (horseRoot != null)
            {
                Transform wagon = transform.root;
                wagon.SetParent(horseRoot);

                // Use the public offset
                wagon.localPosition = attachOffset;
                wagon.localRotation = Quaternion.identity;

                // Disable wagon physics
                Rigidbody rb = wagon.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                isAttached = true;
                Debug.Log("Wagon attached to horse via trigger with offset: " + attachOffset);
            }
        }
    }
}
