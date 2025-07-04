using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class MolotovSpawn : SimpleUseable
    {
        public MolotovSpawn(BaseCharacter owner, string name, float cooldown) : base(owner)
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
            if (inventory == null || inventory.GetItemCount("Molotov") <= 0)
            {
                Debug.Log("Not enough Molotovs.");
                return;
            }

            try
            {
                // Use cursor aim point from camera
                Vector3 target = human.GetAimPoint();
                Vector3 spawnPos = human.Cache.Transform.position + Vector3.up * 1.5f;
                Vector3 direction = (target - spawnPos).normalized;
                float throwForce = 20f;

                // Create molotov
                GameObject molotov = PhotonNetwork.Instantiate("Buildables/Molotov", spawnPos, Quaternion.LookRotation(direction));

                // Apply throw force
                Rigidbody rb = molotov.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.AddForce(direction * throwForce, ForceMode.VelocityChange);
                }

                // Decrease inventory
                int newCount = Mathf.Max(0, inventory.GetItemCount("Molotov") - 1);
                inventory.photonView?.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, "Molotov", newCount);
            }
            catch
            {
                Debug.LogWarning("Molotov spawn failed.");
            }
        }
    }
}
