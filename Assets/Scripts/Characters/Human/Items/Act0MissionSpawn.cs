using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class Act0MissionSpawn : SimpleUseable
    {
        public Act0MissionSpawn(BaseCharacter owner, string name, float cooldown) : base(owner)
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
                Vector3 pos = Vector3.zero; // Always spawn at (0, 0, 0)

                GameObject fay = GameObject.Find("Fay(Clone)");
                if (fay != null && PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.Destroy(fay);
                }
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Cape5", pos, Quaternion.identity);
                ///GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_1", pos, Quaternion.identity);
                ///GameObject Mission1 = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_2", pos, Quaternion.identity);
                ///GameObject Mission2 = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_3", pos, Quaternion.identity);
                ///GameObject Mission3 = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_4", pos, Quaternion.identity);
                ///GameObject Mission4 = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_5", pos, Quaternion.identity);
                ///GameObject Mission5 = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_6", pos, Quaternion.identity);
                ///GameObject Mission6 = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_7", pos, Quaternion.identity);
                // GameObject ShigGateS = PhotonNetwork.Instantiate("Buildables/ShigGateSouth", pos, Quaternion.identity);
                /// GameObject Varreosa = PhotonNetwork.Instantiate("Buildables/PastVarreosa", pos, Quaternion.identity);
                /// GameObject WaterBase = PhotonNetwork.Instantiate("Buildables/WaterEmpty", pos, Quaternion.identity);
            }
            catch { /* Silently ignore errors if any occur */ }
        }
    }
}
