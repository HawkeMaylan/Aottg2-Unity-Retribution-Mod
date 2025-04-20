using UnityEngine;

public class AttachToHorseTrigger : MonoBehaviour
{
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

                // Optional: Set offset relative to the horse
                wagon.localPosition = new Vector3(0f, 0f, -2f); // Adjust as needed
                wagon.localRotation = Quaternion.identity;

                // Optional: Disable wagon physics
                Rigidbody rb = wagon.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                isAttached = true;
                Debug.Log("Wagon attached to horse via trigger!");
            }
        }
    }
}
