using UnityEngine;
using Photon.Pun;

public class SelfDestroy : MonoBehaviourPun
{
    public float lifetime = 3f;

    private void Start()
    {
        Invoke(nameof(DestroyObjectSafely), lifetime);
    }

    private void DestroyObjectSafely()
    {
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject); // Sync destroy for all clients
            }
        }
        else
        {
            // Fallback for non-networked or offline use
            Destroy(gameObject);
        }
    }
}
