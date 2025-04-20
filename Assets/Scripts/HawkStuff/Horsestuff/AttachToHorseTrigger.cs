using UnityEngine;
using Photon.Pun;

public class AttachToHorseTrigger : MonoBehaviourPunCallbacks
{
    [Header("Offset from horse when attaching")]
    public Vector3 attachOffset = new Vector3(0f, 0f, -2f);

    [Header("FixedJoint Settings")]
    public float breakForce = Mathf.Infinity;
    public float breakTorque = Mathf.Infinity;
    public float massScale = 1f;
    public float connectedMassScale = 1f;
    public bool enableCollision = false;
    public bool enablePreprocessing = true;

    private bool isAttached = false;
    private Transform horseRootInContact;
    private Transform attachedHorse;
    private Rigidbody rb;
    private Transform wagon;
    private PhotonView pv;
    private int attachedHorseViewID = -1;

    private void Start()
    {
        wagon = transform.root;
        rb = wagon.GetComponent<Rigidbody>();
        pv = wagon.GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            // Try to attach if not attached
            if (!isAttached && horseRootInContact != null)
            {
                PhotonView horseView = horseRootInContact.GetComponentInParent<PhotonView>();
                if (horseView != null && horseView.Owner == PhotonNetwork.LocalPlayer)
                {
                    // Anyone can request ownership while it's detached
                    if (!pv.IsMine)
                    {
                        pv.RequestOwnership();
                    }

                    pv.RPC("RPC_AttachToHorse", RpcTarget.AllBuffered, horseView.ViewID, attachOffset);
                }
                else
                {
                    Debug.LogWarning("Cannot attach: horse is not yours.");
                }
            }

            // Only the current owner can detach
            else if (isAttached && pv.IsMine)
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

        wagon.position = horseRoot.TransformPoint(offset);
        wagon.rotation = horseRoot.rotation;

        FixedJoint joint = wagon.GetComponent<FixedJoint>();
        if (joint != null) Destroy(joint);

        joint = wagon.gameObject.AddComponent<FixedJoint>();
        Rigidbody horseRb = horseRoot.GetComponent<Rigidbody>();

        if (horseRb != null)
        {
            joint.connectedBody = horseRb;
        }

        // Apply joint settings
        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;
        joint.massScale = massScale;
        joint.connectedMassScale = connectedMassScale;
        joint.enableCollision = enableCollision;
        joint.enablePreprocessing = enablePreprocessing;

        rb.isKinematic = false;
        isAttached = true;
        attachedHorse = horseRoot;
        attachedHorseViewID = horseViewID;
    }

    [PunRPC]
    private void RPC_DetachFromHorse()
    {
        FixedJoint joint = wagon.GetComponent<FixedJoint>();
        if (joint != null)
        {
            Destroy(joint);
        }

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        isAttached = false;
        attachedHorse = null;
        attachedHorseViewID = -1;
    }
}
