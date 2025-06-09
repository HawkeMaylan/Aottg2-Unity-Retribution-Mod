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
        // If the object is networked and this is the Master Client, try network destroy
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient && photonView != null && photonView.ViewID != 0)
        {
            PhotonNetwork.Destroy(transform.root.gameObject);
        }
        else
        {
            // Fallback: local destroy in case Photon fails or not a networked object
            Destroy(gameObject);
        }
    }
}
