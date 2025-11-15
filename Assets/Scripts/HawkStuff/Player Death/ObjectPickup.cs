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
    private Rigidbody rb;
    private bool hadRigidbody = false;
    private RigidbodyProperties savedRigidbodyProperties;
    private bool canBePickedUp = false;
    private float lastUiCheckTime = 0f;
    private const float UI_CHECK_INTERVAL = 2f;
    private int currentOwnerViewID = -1;

    // Object states
    public enum ObjectState
    {
        onGroundItem,
        pickedUpObject
    }
    private ObjectState currentState = ObjectState.onGroundItem;

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

        // Start delay coroutine
        StartCoroutine(EnablePickupAfterDelay());
    }

    private IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        canBePickedUp = true;
        Debug.Log("Object can now be picked up");
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
            stream.SendNext((int)currentState);
            stream.SendNext(currentOwnerViewID);
        }
        else
        {
            currentState = (ObjectState)stream.ReceiveNext();
            currentOwnerViewID = (int)stream.ReceiveNext();
        }
    }

    private void Update()
    {
        if (ChatManager.IsChatActive() || !canBePickedUp)
            return;

        switch (currentState)
        {
            case ObjectState.onGroundItem:
                UpdateOnGroundState();
                break;
            case ObjectState.pickedUpObject:
                UpdatePickedUpState();
                break;
        }
    }

    private void UpdateOnGroundState()
    {
        if (isInside && localHuman != null)
        {
            UpdatePromptAndInput();
        }

        // Periodically check if UI should be removed
        CheckUiVisibility();
    }

    private void UpdatePickedUpState()
    {
        // Only the owner can drop the object
        if (currentOwnerViewID == localHuman?.photonView.ViewID)
        {
            if (SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
            {
                TryDropObject();
            }
        }
    }

    private void CheckUiVisibility()
    {
        if (Time.time - lastUiCheckTime >= UI_CHECK_INTERVAL)
        {
            lastUiCheckTime = Time.time;

            // If UI is active but player is no longer inside, clear the prompt
            if (!string.IsNullOrEmpty(currentPrompt) && !isInside)
            {
                ClearPrompt();
                Debug.Log("UI cleared - player no longer in pickup zone");
            }
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
        if (currentState != ObjectState.onGroundItem || !canBePickedUp) return;

        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.photonView.IsMine)
        {
            localHuman = human;
            isInside = true;
            // Reset UI check timer when player enters
            lastUiCheckTime = Time.time;
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
        if (currentState != ObjectState.onGroundItem || localHuman == null || !canBePickedUp) return;

        // Call RPC to pickup object on all clients
        photonView.RPC("RPC_PickupObject", RpcTarget.All, localHuman.photonView.ViewID);
    }

    private void TryDropObject()
    {
        if (currentState != ObjectState.pickedUpObject || currentOwnerViewID != localHuman?.photonView.ViewID) return;

        // Call RPC to drop object on all clients
        photonView.RPC("RPC_DropObject", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_PickupObject(int humanViewID)
    {
        if (!canBePickedUp || currentState != ObjectState.onGroundItem) return;

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

        // Update state and owner
        currentState = ObjectState.pickedUpObject;
        currentOwnerViewID = humanViewID;

        // Disable the trigger zone since object is picked up
        if (triggerZone != null)
            triggerZone.enabled = false;

        // Clear prompt since object is picked up
        ClearPrompt();

        Debug.Log($"Object picked up and parented to {hipChildName}");
    }

    [PunRPC]
    private void RPC_DropObject()
    {
        if (currentState != ObjectState.pickedUpObject) return;

        // Re-add the Rigidbody component if it originally had one
        if (hadRigidbody && rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            ApplyRigidbodyProperties(rb);
        }

        // Unparent the object
        transform.SetParent(null);

        // Update state and clear owner
        currentState = ObjectState.onGroundItem;
        currentOwnerViewID = -1;

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
        // Only show prompt in onGroundItem state
        if (!string.IsNullOrEmpty(currentPrompt) && currentState == ObjectState.onGroundItem && canBePickedUp)
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