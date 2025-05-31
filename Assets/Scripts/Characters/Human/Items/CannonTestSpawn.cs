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
            if (inventory == null || inventory.cannonCount <= 0)
            {
                Debug.Log("Not enough cannon count to spawn.");
                return;
            }

            try
            {
                Vector3 pos = human.Cache.Transform.position + human.Cache.Transform.forward * 3f;
                GameObject CannonObj = PhotonNetwork.Instantiate("Buildables/CannonTest", pos, Quaternion.identity);

                // successful spawn inventory drop
                inventory.cannonCount--;
            }
            catch
            {
                Debug.LogWarning("Cannon spawn failed.");
            }
        }
    }
}
