using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using System.Collections;
using System.Collections.Generic;

public class AutoItemZone: MonoBehaviourPunCallbacks, IPunObservable
{
    [System.Serializable]
    public struct InventoryChange
    {
        public string itemType;
        public int amount; // Positive for adding, negative for removing
    }

    [Header("Inventory Settings")]
    public Collider triggerZone;
    public List<InventoryChange> inventoryChanges = new List<InventoryChange>();

    private void OnTriggerEnter(Collider other)
    {
        // Check if this is a human's trigger collider
        if (other.name == "HumanTrigger" || other.transform.parent != null)
        {
            Human human = other.GetComponent<Human>();
            if (human == null && other.transform.parent != null)
                human = other.transform.parent.GetComponent<Human>();

            if (human != null && human.photonView != null && human.photonView.IsMine)
            {
                // Apply inventory changes locally
                ApplyInventoryChanges(human);
            }
        }
    }

    private void ApplyInventoryChanges(Human human)
    {
        var inventory = human.GetComponent<HumanInventory>();
        if (inventory != null)
        {
            foreach (var change in inventoryChanges)
            {
                if (change.amount > 0)
                {
                    // Add items
                    for (int i = 0; i < change.amount; i++)
                    {
                        inventory.AddItem(change.itemType);
                    }
                }
                else if (change.amount < 0)
                {
                    // Remove items (convert negative to positive for removal count)
                    int removeCount = Mathf.Abs(change.amount);
                    for (int i = 0; i < removeCount; i++)
                    {
                        inventory.RemoveItem(change.itemType);
                    }
                }
            }
        }
    }

    // Empty IPunObservable implementation since we don't need sync for this simple version
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // No data needs to be synchronized
    }
}