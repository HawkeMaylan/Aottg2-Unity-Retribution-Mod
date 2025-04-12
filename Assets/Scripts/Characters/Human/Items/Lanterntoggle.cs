using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class Lanterntoggle : SimpleUseable
    {
        public Lanterntoggle(BaseCharacter owner, string name, float cooldown) : base(owner)
        {
            Name = name;
            Cooldown = cooldown;
        }

        protected override void Activate()
        {
            var human = _owner as Human;




            try
            {
                Vector3 pos = human.Cache.Transform.position + Vector3.up * 1.5f;
                GameObject LampObj = PhotonNetwork.Instantiate("Buildables/HumanLamp1", pos, Quaternion.identity);

                if (LampObj != null)
                    LampObj.transform.SetParent(human.transform, true);
            }
            catch { /* Silently ignore errors if any occur */ }
        }
    }
}
