using UnityEngine;
using Photon.Pun;

public class SelfDestroy : MonoBehaviourPun
{
    public float lifetime = 3f;

    private void Start()
    {
        if (photonView.IsMine || PhotonNetwork.IsMasterClient)
        {
            Invoke(nameof(DestroyNetworkedObject), lifetime);
        }
    }

    private void DestroyNetworkedObject()
    {
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
