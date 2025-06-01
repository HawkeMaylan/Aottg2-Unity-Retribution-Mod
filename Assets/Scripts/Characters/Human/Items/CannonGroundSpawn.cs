using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class CannonGroundSpawn : SimpleUseable
    {
        public CannonGroundSpawn(BaseCharacter owner, string name, float cooldown) : base(owner)
        {
            Name = name;
            Cooldown = cooldown;
        }

        protected override void Activate()
        {
            var human = _owner as Human;
            if (human == null || !_owner.photonView.IsMine || !PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady)
                return;

            var inventory = human.GetComponent<HumanInventory>();
            if (inventory == null || inventory.GetItemCount("CannonGround") <= 0)
            {
                Debug.Log("Not enough Cannon count to spawn.");
                return;
            }

            try
            {
                Vector3 pos = human.Cache.Transform.position + human.Cache.Transform.forward * 3f;
                GameObject cannonObj = PhotonNetwork.Instantiate("Buildables/CannonGround", pos, Quaternion.identity);

                // Sync inventory 
                int newCannonCount = Mathf.Max(0, inventory.GetItemCount("CannonGround") - 1);
                inventory.photonView?.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, "CannonGround", newCannonCount);
            }
            catch
            {
                Debug.LogWarning("Cannon spawn failed.");
            }
        }
    }
}
