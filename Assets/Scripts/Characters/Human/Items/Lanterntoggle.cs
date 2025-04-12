using Characters;
using UnityEngine;
using Photon.Pun;

namespace Characters
{
    class Lanterntoggle : SimpleUseable
    {
        private GameObject _lampInstance;
        private bool _lampIsOn = false;

        public Lanterntoggle(BaseCharacter owner, string name, float cooldown) : base(owner)
        {
            Name = name;
            Cooldown = cooldown;
        }

        protected override void Activate()
        {
            var human = _owner as Human;
            if (human == null || !_owner.photonView.IsMine || !PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady)
                return;

            // Destroy the old lamp if it exists
            if (_lampInstance != null)
            {
                PhotonNetwork.Destroy(_lampInstance);
                _lampInstance = null;
            }

            // Toggle state
            _lampIsOn = !_lampIsOn;

            // Choose which prefab to instantiate
            string lampPrefab = _lampIsOn ? "Buildables/HumanLamp1" : "Buildables/HumanLamp0";

            // Spawn the new lamp
            Vector3 pos = human.Cache.Transform.position + Vector3.up * 1.5f;
            _lampInstance = PhotonNetwork.Instantiate(lampPrefab, pos, Quaternion.identity);
            _lampInstance.transform.SetParent(human.transform, true);
        }
    }
}
