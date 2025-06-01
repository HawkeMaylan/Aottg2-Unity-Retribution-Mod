using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class CannonTestSpawn : SimpleUseable
    {
        public CannonTestSpawn(BaseCharacter owner, string name, float cooldown) : base(owner)
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
            if (inventory == null || inventory.GetItemCount("Cannon") <= 0)
            {
                Debug.Log("Not enough Cannon count to spawn.");
                return;
            }

            try
            {
                Vector3 pos = human.Cache.Transform.position + human.Cache.Transform.forward * 3f;
                GameObject cannonObj = PhotonNetwork.Instantiate("Buildables/CannonTest", pos, Quaternion.identity);

                // Sync inventory 
                int newCannonCount = Mathf.Max(0, inventory.GetItemCount("Cannon") - 1);
                inventory.photonView?.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, "Cannon", newCannonCount);
            }
            catch
            {
                Debug.LogWarning("Cannon spawn failed.");
            }
        }
    }
}
