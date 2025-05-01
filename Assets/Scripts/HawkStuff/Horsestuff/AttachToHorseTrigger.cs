using UnityEngine;
using Photon.Pun;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Characters; // Needed for Horse script access

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

    [Header("Prompt Texts")]
    public string attachPromptText = "Press G to Attach";
    public string detachPromptText = "Press G to Detach";

    [Header("Detach Prompt Settings")]
    public float detachPromptDuration = 5f;

    private bool isAttached = false;
    private Transform horseRootInContact;
    private Transform attachedHorse;
    private Rigidbody rb;
    private Transform wagon;
    private PhotonView pv;
    private int attachedHorseViewID = -1;

    private static string currentPrompt = "";
    private float detachPromptTimer = 0f;

    private void Start()
    {
        wagon = transform.root;
        rb = wagon.GetComponent<Rigidbody>();
        pv = wagon.GetComponent<PhotonView>();

        ClearPrompt();
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
                    if (horseComponent.MountedStatus == 1)
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
                    if (horseComponent != null && horseComponent.MountedStatus == 1)
                    {
                        pv.RPC("RPC_DetachFromHorse", RpcTarget.AllBuffered);
                    }
                }
            }
        }

        if (isAttached && detachPromptTimer > 0f)
        {
            detachPromptTimer -= Time.deltaTime;
            if (detachPromptTimer <= 0f)
            {
                ClearPrompt();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "HorseTrigger")
        {
            horseRootInContact = other.transform.root;
            SetPrompt(attachPromptText);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "HorseTrigger" && other.transform.root == horseRootInContact)
        {
            horseRootInContact = null;
            ClearPrompt();
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
        if (existingJoint != null)
            Destroy(existingJoint);

        ConfigurableJoint joint = wagon.gameObject.AddComponent<ConfigurableJoint>();
        Rigidbody horseRb = horseRoot.GetComponent<Rigidbody>();
        if (horseRb != null)
        {
            joint.connectedBody = horseRb;
        }

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Limited;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = linearLimit;
        joint.linearLimit = limit;

        JointDrive linearDrive = new JointDrive
        {
            positionSpring = linearSpring,
            positionDamper = linearDamper,
            maximumForce = Mathf.Infinity
        };
        joint.xDrive = joint.yDrive = joint.zDrive = linearDrive;

        JointDrive angularDrive = new JointDrive
        {
            positionSpring = angularSpring,
            positionDamper = angularDamper,
            maximumForce = Mathf.Infinity
        };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.slerpDrive = angularDrive;

        joint.breakForce = jointBreakForce;
        joint.breakTorque = jointBreakTorque;
        joint.enableCollision = enableCollision;

        rb.isKinematic = false;
        isAttached = true;
        attachedHorse = horseRoot;
        attachedHorseViewID = horseViewID;

        SetPrompt(detachPromptText);
        detachPromptTimer = detachPromptDuration;
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

        SetPrompt(attachPromptText);
        detachPromptTimer = 0f;
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.alignment = TextAnchor.UpperCenter;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(Screen.width / 2 - 150, 50, 300, 50), currentPrompt, style);
        }
    }

    private void SetPrompt(string text)
    {
        currentPrompt = text;
    }

    private void ClearPrompt()
    {
        currentPrompt = "";
    }
}
