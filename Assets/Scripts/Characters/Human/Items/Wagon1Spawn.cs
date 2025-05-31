using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class Wagon1Spawn : SimpleUseable
    {
        public Wagon1Spawn(BaseCharacter owner, string name, float cooldown) : base(owner)
        {
            Name = name;
            Cooldown = cooldown;
        }

        protected override void Activate()
        {
            var human = _owner as Human;
            if (human == null || !_owner.photonView.IsMine || !PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady || human.Horse == null)
                return;

            // Get the inventory
            var inventory = human.GetComponent<HumanInventory>();
            if (inventory == null || inventory.wagon1Count <= 0)
            {
                Debug.Log("Not enough Wagon1 count to spawn.");
                return;
            }

            try
            {
                Vector3 pos = human.Cache.Transform.position + Vector3.up * 1.5f;
                GameObject WagonObj = PhotonNetwork.Instantiate("Buildables/Wagon1aEdit", pos, Quaternion.identity);

                
                inventory.wagon1Count--;
            }
            catch
            {
                Debug.LogWarning("Wagon spawn failed.");
            }
        }
    }
}
