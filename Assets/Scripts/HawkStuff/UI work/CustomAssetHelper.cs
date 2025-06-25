using UnityEngine;
using Photon.Pun;

public class CustomAssetHelper : MonoBehaviourPun
{
    public void Move(Vector3 newPosition)
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            transform.position = newPosition;
        }
        else
        {
            photonView.RPC("RPC_Move", RpcTarget.MasterClient, newPosition);
        }
    }

    public void Delete()
    {
        if (photonView.IsMine || !PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            photonView.RPC("RPC_Delete", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RPC_Move(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    [PunRPC]
    private void RPC_Delete()
    {
        PhotonNetwork.Destroy(gameObject);
    }
}
