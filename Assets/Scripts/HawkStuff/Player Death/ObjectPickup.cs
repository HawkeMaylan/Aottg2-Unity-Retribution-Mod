using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;

public class ObjectPickup : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Object Pickup Settings")]
    public Collider triggerZone;
    public string hipChildName = "hip"; // Name of the hip child to parent to

    [Header("Carry Position & Rotation")]
    public Vector3 carryPositionOffset = Vector3.zero;
    public Vector3 carryRotationOffset = Vector3.zero;

    private Human localHuman;
    private static string currentPrompt = "";
    private bool isInside = false;
    private bool isPickedUp = false;
    private Rigidbody rb;
    private bool hadRigidbody = false;
    private RigidbodyProperties savedRigidbodyProperties;

    // Struct to store Rigidbody properties
    private struct RigidbodyProperties
    {
        public float mass;
        public float drag;
        public float angularDrag;
        public bool useGravity;
        public bool isKinematic;
        public RigidbodyInterpolation interpolation;
        public CollisionDetectionMode collisionDetectionMode;
        public RigidbodyConstraints constraints;
    }

    private void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        hadRigidbody = rb != null;

        // Save initial properties if Rigidbody exists
        if (hadRigidbody)
        {
            SaveRigidbodyProperties();
        }
    }

    private void SaveRigidbodyProperties()
    {
        if (rb != null)
        {
            savedRigidbodyProperties = new RigidbodyProperties
            {
                mass = rb.mass,
                drag = rb.drag,
                angularDrag = rb.angularDrag,
                useGravity = rb.useGravity,
                isKinematic = rb.isKinematic,
                interpolation = rb.interpolation,
                collisionDetectionMode = rb.collisionDetectionMode,
                constraints = rb.constraints
            };
        }
    }

    private void ApplyRigidbodyProperties(Rigidbody rigidbody)
    {
        rigidbody.mass = savedRigidbodyProperties.mass;
        rigidbody.drag = savedRigidbodyProperties.drag;
        rigidbody.angularDrag = savedRigidbodyProperties.angularDrag;
        rigidbody.useGravity = savedRigidbodyProperties.useGravity;
        rigidbody.isKinematic = savedRigidbodyProperties.isKinematic;
        rigidbody.interpolation = savedRigidbodyProperties.interpolation;
        rigidbody.collisionDetectionMode = savedRigidbodyProperties.collisionDetectionMode;
        rigidbody.constraints = savedRigidbodyProperties.constraints;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(isPickedUp);
        }
        else
        {
            isPickedUp = (bool)stream.ReceiveNext();
        }
    }

    private void Update()
    {
        if (ChatManager.IsChatActive() || isPickedUp)
            return;

        if (isInside && localHuman != null)
        {
            UpdatePromptAndInput();
        }
    }

    private void UpdatePromptAndInput()
    {
        currentPrompt = $"Press {SettingsManager.InputSettings.Interaction.Interact2} to Pick Up";

        if (SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
        {
            TryPickupObject();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;

        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.photonView.IsMine)
        {
            localHuman = human;
            isInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human == localHuman)
        {
            ClearPrompt();
            isInside = false;
            localHuman = null;
        }
    }

    private void TryPickupObject()
    {
        if (isPickedUp || localHuman == null) return;

        // Call RPC to pickup object on all clients
        photonView.RPC("RPC_PickupObject", RpcTarget.All, localHuman.photonView.ViewID);
    }

    [PunRPC]
    private void RPC_PickupObject(int humanViewID)
    {
        PhotonView humanPhotonView = PhotonView.Find(humanViewID);
        if (humanPhotonView == null) return;

        Human human = humanPhotonView.GetComponent<Human>();
        if (human == null) return;

        // Find the hip child using the human's FindDeepChild method
        Transform hipChild = human.FindDeepChild(human.transform, hipChildName);
        if (hipChild == null)
        {
            Debug.LogWarning($"Could not find hip child named '{hipChildName}' on human");
            return;
        }

        // Remove the Rigidbody component if it exists
        if (rb != null)
        {
            Destroy(rb);
            rb = null;
        }

        // Parent the object to the hip
        transform.SetParent(hipChild);

        // Apply position and rotation offsets
        transform.localPosition = carryPositionOffset;
        transform.localRotation = Quaternion.Euler(carryRotationOffset);

        isPickedUp = true;

        // Disable the trigger zone since object is picked up
        if (triggerZone != null)
            triggerZone.enabled = false;

        Debug.Log($"Object picked up and parented to {hipChildName}");
    }

    // Optional: Add method to drop the object and re-add Rigidbody
    [PunRPC]
    private void RPC_DropObject()
    {
        if (!isPickedUp) return;

        // Re-add the Rigidbody component if it originally had one
        if (hadRigidbody && rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            ApplyRigidbodyProperties(rb);
        }

        // Unparent the object
        transform.SetParent(null);

        isPickedUp = false;

        // Re-enable the trigger zone
        if (triggerZone != null)
            triggerZone.enabled = true;

        Debug.Log("Object dropped");
    }

    private void ClearPrompt()
    {
        currentPrompt = "";
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentPrompt) && !isPickedUp)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false,
                normal = { textColor = Color.white }
            };

            float labelWidth = 600f;
            float labelHeight = 30f;
            float labelX = Screen.width / 2 - labelWidth / 2;

            GUI.Label(new Rect(labelX, 50, labelWidth, labelHeight), currentPrompt, style);
        }
    }
}