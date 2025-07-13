using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using UI;

namespace Characters
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhotonView))]
    public class HumanInventory : MonoBehaviourPunCallbacks
    {
        public static HumanInventory Instance { get; private set; }

        [Header("Deployable Types")]
        [SerializeField]
        private List<string> defaultDeployables = new List<string>
        {
            "Cannon",
            "Wagon1",
            "Wagon2",
            "WallCannon"
        };

        [Header("Inventory Counts")]
        [Tooltip("Dictionary of item counts. Use GetItemCount() for safe access.")]
        public Dictionary<string, int> inventoryCounts = new Dictionary<string, int>();

        private void Awake()
        {
            // Singleton pattern for easy access
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Multiple HumanInventory instances detected. Destroying duplicate.");
                Destroy(this);
                return;
            }

            InitializeInventory();
        }

        private void InitializeInventory()
        {
            foreach (var type in defaultDeployables)
            {
                if (!inventoryCounts.ContainsKey(type))
                {
                    inventoryCounts[type] = 0;
                    Debug.Log($"Initialized inventory slot for: {type}");
                }
            }
        }

        public void AddItem(string type)
        {
            if (!ValidateItemType(type)) return;
            int newCount = inventoryCounts[type] + 1;
            photonView.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, type, newCount);
        }

        public void RemoveItem(string type)
        {
            if (!ValidateItemType(type)) return;
            int newCount = Mathf.Max(0, inventoryCounts[type] - 1);
            photonView.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, type, newCount);
        }

        public void SetItemCount(string type, int count)
        {
            if (!ValidateItemType(type)) return;

            if (count < 0 && photonView.IsMine)
            {
                ItemPopupManager.Instance?.ShowPopup($"Not Enough {type}");
            }
            photonView.RPC("RPC_SetItemCount", RpcTarget.AllBufferedViaServer, type, Mathf.Max(0, count));
        }

        public int GetItemCount(string type)
        {
            if (!ValidateItemType(type)) return 0;
            return inventoryCounts[type];
        }

        [PunRPC]
        public void RPC_SetItemCount(string type, int count)
        {
            if (!ValidateItemType(type, true)) return;

            int oldCount = inventoryCounts[type];
            int newCount = Mathf.Max(0, count);

            // Show "Not Enough" message if count is being forced below 0 and we're the local player
            if (photonView.IsMine && count < 0 && newCount == 0 && oldCount > 0)
            {
                ItemPopupManager.Instance?.ShowPopup($"Not Enough {type}");
            }

            inventoryCounts[type] = newCount;

            if (photonView.IsMine && newCount != oldCount)
            {
                int delta = newCount - oldCount;
                string change = delta > 0 ? $"+{delta}" : $"{delta}";
                ItemPopupManager.Instance?.ShowPopup($"{type} {change}");

                Human human = GetComponent<Human>();
                if (human != null)
                {
                    human.RefreshItemBasedOnInventory(type);
                }
            }
        }

        public void ShowNotEnoughMessage(string itemType)
        {
            if (photonView.IsMine)
            {
                ItemPopupManager.Instance?.ShowPopup($"Not Enough {itemType}");
            }
        }

        public List<string> GetItemTypes()
        {
            return new List<string>(inventoryCounts.Keys);
        }

        private bool ValidateItemType(string type, bool autoAdd = false)
        {
            if (string.IsNullOrEmpty(type))
            {
                Debug.LogError("Item type cannot be null or empty");
                return false;
            }

            if (!inventoryCounts.ContainsKey(type))
            {
                if (autoAdd)
                {
                    Debug.LogWarning($"Auto-adding new item type: {type}");
                    inventoryCounts[type] = 0;
                    return true;
                }
                else
                {
                    Debug.LogError($"Item type '{type}' not found in inventory");
                    return false;
                }
            }

            return true;
        }

        // Quick Access Properties
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

        // Cleanup on destroy
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}