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
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_1", pos, Quaternion.identity);
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_2", pos, Quaternion.identity);
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_3", pos, Quaternion.identity);
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_4", pos, Quaternion.identity);
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_5", pos, Quaternion.identity);
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_6", pos, Quaternion.identity);
                GameObject Mission = PhotonNetwork.Instantiate("Buildables/NPCS/Preset/Act05_7", pos, Quaternion.identity);
                // GameObject ShigGateS = PhotonNetwork.Instantiate("Buildables/ShigGateSouth", pos, Quaternion.identity);
                /// GameObject Varreosa = PhotonNetwork.Instantiate("Buildables/PastVarreosa", pos, Quaternion.identity);
                /// GameObject WaterBase = PhotonNetwork.Instantiate("Buildables/WaterEmpty", pos, Quaternion.identity);
            }
            catch { /* Silently ignore errors if any occur */ }
        }
    }
}
