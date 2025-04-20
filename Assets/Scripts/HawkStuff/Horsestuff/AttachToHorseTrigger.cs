using UnityEngine;
using Photon.Pun;

public class AttachToHorseTrigger : MonoBehaviourPunCallbacks
{
    [Header("Offset from horse when attaching")]
    public Vector3 attachOffset = new Vector3(0f, 0f, -2f);

    private bool isAttached = false;
    private Transform horseRootInContact;
    private Transform attachedHorse;
    private Rigidbody rb;
    private Transform wagon;
    private PhotonView pv;

    private void Start()
    {
        wagon = transform.root;
        rb = wagon.GetComponent<Rigidbody>();
        pv = wagon.GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (!pv.IsMine) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!isAttached && horseRootInContact != null)
            {
                PhotonView horseView = horseRootInContact.GetComponentInParent<PhotonView>();
                if (horseView != null)
                {
                    pv.RPC("RPC_AttachToHorse", RpcTarget.AllBuffered, horseView.ViewID, attachOffset);
                }
            }
            else if (isAttached)
            {
                pv.RPC("RPC_DetachFromHorse", RpcTarget.AllBuffered);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "HorseTrigger")
        {
            horseRootInContact = other.transform.root;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "HorseTrigger" && other.transform.root == horseRootInContact)
        {
            horseRootInContact = null;
        }
    }

    [PunRPC]
    private void RPC_AttachToHorse(int horseViewID, Vector3 offset)
    {
        PhotonView horseView = PhotonView.Find(horseViewID);
        if (horseView == null) return;

        Transform horseRoot = horseView.transform;

        // Move wagon to offset position behind horse
        wagon.position = horseRoot.TransformPoint(offset);
        wagon.rotation = horseRoot.rotation;

        // Create and configure joint
        FixedJoint joint = wagon.GetComponent<FixedJoint>();
        if (joint != null) Destroy(joint); // Clean old joints if needed

        joint = wagon.gameObject.AddComponent<FixedJoint>();
        Rigidbody horseRb = horseRoot.GetComponent<Rigidbody>();
        if (horseRb != null)
        {
            joint.connectedBody = horseRb;
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
        }

        rb.isKinematic = false; // Let physics simulation occur
        isAttached = true;
        attachedHorse = horseRoot;
    }

    [PunRPC]
    private void RPC_DetachFromHorse()
    {
        FixedJoint joint = wagon.GetComponent<FixedJoint>();
        if (joint != null) Destroy(joint);

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        isAttached = false;
        attachedHorse = null;
    }
}
