using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace Characters
{
    public class HumanInventory : MonoBehaviourPunCallbacks
    {
        [Header("Deployable Types")]
        [SerializeField]
        private List<string> defaultDeployables = new List<string>
        {
            ///ADD NEW AS NEEDED MAKE SURE TO ADD AT BOTTOM
            "Cannon",
            "Wagon1",
            "Wagon2",
            "WallCannon"
        };

        [Header("Inventory Counts")]
        public Dictionary<string, int> inventoryCounts = new Dictionary<string, int>();

        private void Awake()
        {
            foreach (var type in defaultDeployables)
            {
                if (!inventoryCounts.ContainsKey(type))
                    inventoryCounts[type] = 0;
            }
        }

        public void AddItem(string type)
        {
            EnsureItemType(type);
            int newCount = inventoryCounts[type] + 1;
            photonView.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, type, newCount);
        }

        public void RemoveItem(string type)
        {
            EnsureItemType(type);
            int newCount = Mathf.Max(0, inventoryCounts[type] - 1);
            photonView.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, type, newCount);
        }

        public void SetItemCount(string type, int count)
        {
            EnsureItemType(type);
            photonView.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, type, Mathf.Max(0, count));
        }

        public int GetItemCount(string type)
        {
            return inventoryCounts.TryGetValue(type, out int count) ? count : 0;
        }

        [PunRPC]
        public void RPC_SetItemCount(string type, int count)
        {
            EnsureItemType(type);
            inventoryCounts[type] = Mathf.Max(0, count);
        }

        public List<string> GetItemTypes()
        {
            return new List<string>(inventoryCounts.Keys);
        }

        private void EnsureItemType(string type)
        {
            if (!inventoryCounts.ContainsKey(type))
                inventoryCounts[type] = 0;
        }







        // ADD NEW BELOW AS WELL
        public int cannonCount
        {
            get => GetItemCount("Cannon");
            set => SetItemCount("Cannon", value);
        }

        public int wagon1Count
        {
            get => GetItemCount("Wagon1");
            set => SetItemCount("Wagon1", value);
        }

        public int wagon2Count
        {
            get => GetItemCount("Wagon2");
            set => SetItemCount("Wagon2", value);
        }

        public int wallCannonCount
        {
            get => GetItemCount("WallCannon");
            set => SetItemCount("WallCannon", value);
        }

    }


}
