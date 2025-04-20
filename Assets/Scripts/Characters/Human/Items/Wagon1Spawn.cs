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



            try
            {
                Vector3 pos = human.Cache.Transform.position + Vector3.up * 1.5f;
                GameObject WagonObj = PhotonNetwork.Instantiate("Buildables/Wagon1aEdit", pos, Quaternion.identity);


            }
            catch { /* Silently ignore errors if any occur */ }
        }
    }
}
