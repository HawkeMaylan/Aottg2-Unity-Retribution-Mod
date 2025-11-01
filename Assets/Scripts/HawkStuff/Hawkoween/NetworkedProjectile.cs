using UnityEngine;
using Photon.Pun;

public class NetworkedProjectile : MonoBehaviourPunCallbacks
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    [PunRPC]
    public void SetVelocity(Vector3 velocity)
    {
        if (rb != null)
        {
            rb.velocity = velocity;
        }
    }
}