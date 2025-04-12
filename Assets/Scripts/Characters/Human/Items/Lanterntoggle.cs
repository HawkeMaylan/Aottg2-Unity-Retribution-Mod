using Characters;
using UnityEngine;
using Photon.Pun;
using System.Linq;

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

            // Destroy previous lamp if it exists
            if (_lampInstance != null)
            {
                PhotonNetwork.Destroy(_lampInstance);
                _lampInstance = null;
            }

            // Toggle lamp state
            _lampIsOn = !_lampIsOn;
            string lampPrefab = _lampIsOn ? "Buildables/HumanLamp1" : "Buildables/HumanLamp0";

            // Find target object (like player_uniform_MA)
            Transform targetParent = human.Cache.Transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.Contains("player_uniform_MA"));

            if (targetParent == null)
            {
                Debug.LogWarning("Could not find 'player_uniform_MA' to attach the lamp.");
                return;
            }

            // Instantiate the lamp prefab
            _lampInstance = PhotonNetwork.Instantiate(lampPrefab, targetParent.position, Quaternion.identity);

            // Parent it and apply custom transform values
            _lampInstance.transform.SetParent(targetParent, worldPositionStays: false);
            _lampInstance.transform.localPosition = new Vector3(-0.281f, -0.088f, 0.267f);
            _lampInstance.transform.localRotation = Quaternion.Euler(70.825f, -45.559f, -72.411f);
            _lampInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
    }
}
