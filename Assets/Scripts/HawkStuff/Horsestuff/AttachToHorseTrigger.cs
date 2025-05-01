using UnityEngine;
using Photon.Pun;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Characters; //  Needed for Horse script access

public class AttachToHorseTrigger : MonoBehaviourPunCallbacks
{
    [Header("Attachment Offset")]
    public Vector3 attachOffset = new Vector3(0f, 0f, -2f);

    [Header("Joint Motion Limits")]
    public float linearLimit = 0.5f;

    [Header("Linear Drive Settings")]
    public float linearSpring = 100f;
    public float linearDamper = 5f;

    [Header("Angular Drive Settings")]
    public float angularSpring = 10f;
    public float angularDamper = 1f;

    [Header("Joint Break Settings")]
    public float jointBreakForce = Mathf.Infinity;
    public float jointBreakTorque = Mathf.Infinity;
    public bool enableCollision = false;

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
        if (ChatManager.IsChatActive())
            return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (!isAttached && horseRootInContact != null)
            {
                PhotonView horseView = horseRootInContact.GetComponentInParent<PhotonView>();
                Horse horseComponent = horseRootInContact.GetComponentInParent<Horse>();

                if (horseView != null && horseComponent != null && horseView.Owner == PhotonNetwork.LocalPlayer)
                {
                    if (horseComponent.MountedStatus == 1) //  Only allow attaching if Mounted
                    {
                        if (!pv.IsMine)
                            pv.RequestOwnership();

                        pv.RPC("RPC_AttachToHorse", RpcTarget.AllBuffered, horseView.ViewID, attachOffset);
                    }
                }
            }
            else if (isAttached && pv.IsMine)
            {
                if (attachedHorse != null)
                {
                    Horse horseComponent = attachedHorse.GetComponentInParent<Horse>();
                    if (horseComponent != null && horseComponent.MountedStatus == 1) // Only allow detaching if still Mounted
                    {
                        pv.RPC("RPC_DetachFromHorse", RpcTarget.AllBuffered);
                    }
                }
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

        var existingJoint = wagon.GetComponent<ConfigurableJoint>();
        if (existingJoint != null) Destroy(existingJoint);

        ConfigurableJoint joint = wagon.gameObject.AddComponent<ConfigurableJoint>();
        Rigidbody horseRb = horseRoot.GetComponent<Rigidbody>();
        if (horseRb != null)
        {
            joint.connectedBody = horseRb;
        }

        // Joint motion configuration
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        // Linear limit
        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = linearLimit;
        joint.linearLimit = limit;

        // Linear spring/damper
        JointDrive linearDrive = new JointDrive
        {
            positionSpring = linearSpring,
            positionDamper = linearDamper,
            maximumForce = Mathf.Infinity
        };
        joint.xDrive = joint.yDrive = joint.zDrive = linearDrive;

        // Angular drive
        JointDrive angularDrive = new JointDrive
        {
            positionSpring = angularSpring,
            positionDamper = angularDamper,
            maximumForce = Mathf.Infinity
        };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = angularDrive;

        // Other joint settings
        joint.breakForce = jointBreakForce;
        joint.breakTorque = jointBreakTorque;
        joint.enableCollision = enableCollision;

        rb.isKinematic = false;
        isAttached = true;
        attachedHorse = horseRoot;
        attachedHorseViewID = horseViewID;
    }

    [PunRPC]
    private void RPC_DetachFromHorse()
    {
        var joint = wagon.GetComponent<ConfigurableJoint>();
        if (joint != null)
        {
            Destroy(joint);
        }

        rb.isKinematic = false;
        isAttached = false;
        attachedHorse = null;
        attachedHorseViewID = -1;
    }
}
