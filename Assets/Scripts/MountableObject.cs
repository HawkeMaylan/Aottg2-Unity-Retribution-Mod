using UnityEngine;
using Photon.Pun;
using Characters;

public class MountableObject : MonoBehaviour
{
    [Header("Mount Settings")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Trigger Area")]
    public SphereCollider triggerCollider;

    private Human playerHuman;
    private bool playerInRange = false;

    private void Awake()
    {
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
        else
        {
            Debug.LogWarning("[MountableObject] No SphereCollider assigned to triggerCollider.");
        }
    }

    private void Update()
    {
        if (playerInRange && playerHuman != null && Input.GetKeyDown(KeyCode.G))
        {
            if (playerHuman.MountState == HumanMountState.None)
            {
                Debug.Log("[MountableObject] MOUNTING: " + playerHuman.name + " at " + mountPoint.name);
                playerHuman.Mount(mountPoint, positionOffset, rotationOffset);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "HumanTrigger")
        {
            Transform root = other.transform.root;
            PhotonView view = root.GetComponent<PhotonView>();
            Human human = root.GetComponent<Human>();

            if (view != null && view.IsMine && human != null)
            {
                playerHuman = human;
                playerInRange = true;
                Debug.Log("[MountableObject] Player entered mount range via HumanTrigger: " + root.name);
            }
            else
            {
                Debug.Log("[MountableObject] HumanTrigger detected but missing PhotonView or Human script.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "HumanTrigger")
        {
            Transform root = other.transform.root;
            Human human = root.GetComponent<Human>();

            if (human == playerHuman)
            {
                Debug.Log("[MountableObject] Player exited mount range: " + root.name);
                playerHuman = null;
                playerInRange = false;
            }
        }
    }
}
