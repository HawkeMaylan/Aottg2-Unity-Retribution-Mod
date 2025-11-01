using UnityEngine;
using Photon.Pun;
using Characters;
using System.Collections.Generic;

public class InventoryCheckZone: MonoBehaviourPunCallbacks
{
    [Header("Inventory Check Settings")]
    public Collider triggerZone;
    public string requiredItem; // Item to check for in inventory
    public int requiredAmount = 1; // How many of the item are required
    public GameObject objectToDisable; // Object whose collision will be disabled
    public bool reEnableOnExit = true; // Whether to re-enable when player leaves

    private Collider targetCollider;
    private HashSet<int> playersWithItemInZone = new HashSet<int>();

    private void Start()
    {
        // Get the collider from the target object
        if (objectToDisable != null)
        {
            targetCollider = objectToDisable.GetComponent<Collider>();
            if (targetCollider == null)
            {
                Debug.LogWarning("Object to disable has no collider component!");
            }
        }
    }

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
                CheckInventoryAndDisable(human);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if this is a human's trigger collider
        if (other.name == "HumanTrigger" || other.transform.parent != null)
        {
            Human human = other.GetComponent<Human>();
            if (human == null && other.transform.parent != null)
                human = other.transform.parent.GetComponent<Human>();

            if (human != null && human.photonView != null && human.photonView.IsMine)
            {
                HandlePlayerExit(human);
            }
        }
    }

    private void CheckInventoryAndDisable(Human human)
    {
        var inventory = human.GetComponent<HumanInventory>();
        if (inventory != null && targetCollider != null)
        {
            // Check if player has the required item and amount
            int itemCount = inventory.GetItemCount(requiredItem);
            if (itemCount >= requiredAmount)
            {
                playersWithItemInZone.Add(human.photonView.OwnerActorNr);

                // Disable the target collider
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("RPC_SetColliderState", RpcTarget.All, false);
                }
                else
                {
                    // If not master client, request master to disable
                    photonView.RPC("RPC_RequestDisable", RpcTarget.MasterClient, human.photonView.OwnerActorNr);
                }
            }
        }
    }

    private void HandlePlayerExit(Human human)
    {
        if (playersWithItemInZone.Contains(human.photonView.OwnerActorNr))
        {
            playersWithItemInZone.Remove(human.photonView.OwnerActorNr);

            // If no players with the item are left in zone, re-enable the collider
            if (playersWithItemInZone.Count == 0 && reEnableOnExit && targetCollider != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("RPC_SetColliderState", RpcTarget.All, true);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_RequestDisable(int actorId, PhotonMessageInfo info)
    {
        // Master client handles the disable request
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetColliderState", RpcTarget.All, false);
        }
    }

    [PunRPC]
    private void RPC_SetColliderState(bool enabled, PhotonMessageInfo info)
    {
        if (targetCollider != null)
        {
            targetCollider.enabled = enabled;
        }
    }

    // Optional: Visual debug info in the editor
    private void OnDrawGizmos()
    {
        if (triggerZone != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(triggerZone.bounds.center, triggerZone.bounds.size);
        }

        if (objectToDisable != null)
        {
            Collider objCollider = objectToDisable.GetComponent<Collider>();
            if (objCollider != null)
            {
                Gizmos.color = targetCollider != null && !targetCollider.enabled ? Color.red : Color.green;
                Gizmos.DrawWireCube(objCollider.bounds.center, objCollider.bounds.size);
            }
        }
    }
}