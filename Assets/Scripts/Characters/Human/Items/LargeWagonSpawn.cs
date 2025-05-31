using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class LargeWagonSpawn : SimpleUseable
    {
        public LargeWagonSpawn(BaseCharacter owner, string name, float cooldown) : base(owner)
        {
            Name = name;
            Cooldown = cooldown;
        }

        protected override void Activate()
        {
            var human = _owner as Human;
            if (human == null || !_owner.photonView.IsMine || !PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady || human.Horse == null)
                return;

            var inventory = human.GetComponent<HumanInventory>();
            if (inventory == null || inventory.wagon2Count <= 0)
            {
                Debug.Log("Not enough Wagon2 count to spawn.");
                return;
            }

            try
            {
                Vector3 pos = human.Cache.Transform.position + Vector3.up * 1.5f;
                GameObject WagonObj = PhotonNetwork.Instantiate("Buildables/LargeWagon", pos, Quaternion.identity);

                // Use RPC to update inventory on all clients
                PhotonView view = inventory.GetComponent<PhotonView>();
                if (view != null)
                {
                    int newCannonCount = inventory.cannonCount;
                    int newWagon1Count = inventory.wagon1Count;
                    int newWagon2Count = Mathf.Max(0, inventory.wagon2Count - 1);

                    view.RPC("RPC_SetInventoryCounts", RpcTarget.AllBufferedViaServer,
                        newCannonCount, newWagon1Count, newWagon2Count);
                }
            }
            catch
            {
                Debug.LogWarning("Large wagon spawn failed.");
            }
        }
    }
}
