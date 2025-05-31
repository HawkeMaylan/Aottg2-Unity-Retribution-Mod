using UnityEngine;
using Photon.Pun;

namespace Characters
{
    public class HumanInventory : MonoBehaviourPunCallbacks
    {
        [Header("Deployable Counts")]
        public int cannonCount = 0;
        public int wagon1Count = 0;
        public int wagon2Count = 0;

        public void AddCannon() => photonView.RPC("RPC_SetInventoryCounts", RpcTarget.AllBuffered, cannonCount + 1, wagon1Count, wagon2Count);
        public void RemoveCannon() => photonView.RPC("RPC_SetInventoryCounts", RpcTarget.AllBuffered, Mathf.Max(0, cannonCount - 1), wagon1Count, wagon2Count);

        public void AddWagon1() => photonView.RPC("RPC_SetInventoryCounts", RpcTarget.AllBuffered, cannonCount, wagon1Count + 1, wagon2Count);
        public void RemoveWagon1() => photonView.RPC("RPC_SetInventoryCounts", RpcTarget.AllBuffered, cannonCount, Mathf.Max(0, wagon1Count - 1), wagon2Count);

        public void AddWagon2() => photonView.RPC("RPC_SetInventoryCounts", RpcTarget.AllBuffered, cannonCount, wagon1Count, wagon2Count + 1);
        public void RemoveWagon2() => photonView.RPC("RPC_SetInventoryCounts", RpcTarget.AllBuffered, cannonCount, wagon1Count, Mathf.Max(0, wagon2Count - 1));

        [PunRPC]
        public void RPC_SetInventoryCounts(int newCannon, int newWagon1, int newWagon2)
        {
            cannonCount = newCannon;
            wagon1Count = newWagon1;
            wagon2Count = newWagon2;
        }
    }
}
