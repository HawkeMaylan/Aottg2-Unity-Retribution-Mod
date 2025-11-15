using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;

public class ObjectPickup : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Object Pickup Settings")]
    public Collider triggerZone;
    public string hipChildName = "hip"; // Name of the hip child to parent to

    [Header("Carry Position & Rotation")]
    public Vector3 carryPositionOffset = Vector3.zero;
    public Vector3 carryRotationOffset = Vector3.zero;

    [Header("Stat Penalty Settings")]
    public int statPenalty = 20; // How much to reduce speed and acceleration
    public int minimumStatValue = 50; // Minimum value for speed and acceleration

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

    // Stats storage for the owner
    private int originalOwnerSpeed = 0;
    private int originalOwnerAcceleration = 0;

    // Object states
    public enum ObjectState
    {
        onGroundItem,
        pickedUpObject
    }
    private ObjectState currentState = ObjectState.onGroundItem;

    // Static list to track all active pickups
    private static List<ObjectPickup> activePickups = new List<ObjectPickup>();
    private static bool isProcessingPickup = false;

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

        // Add this pickup to the active list
        activePickups.Add(this);

        // Start delay coroutine
        StartCoroutine(EnablePickupAfterDelay());
    }

    private void OnDestroy()
    {
        // Remove this pickup from the active list when destroyed
        activePickups.Remove(this);
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
            // Use centralized pickup system to prevent multiple pickups
            TryCentralizedPickup();
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

    private void TryCentralizedPickup()
    {
        if (isProcessingPickup) return;

        // Find the closest pickup object using centralized system
        ObjectPickup closestPickup = FindClosestPickupObject();

        if (closestPickup != null)
        {
            isProcessingPickup = true;
            closestPickup.photonView.RPC("RPC_PickupObject", RpcTarget.All, localHuman.photonView.ViewID);

            // Reset the processing flag after a short delay
            StartCoroutine(ResetProcessingFlag());
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.1f);
        isProcessingPickup = false;
    }

    private void TryPickupObject()
    {
        // This method is now deprecated - using centralized system instead
        TryCentralizedPickup();
    }

    private ObjectPickup FindClosestPickupObject()
    {
        if (localHuman == null) return null;

        // Get player's position and forward direction
        Vector3 playerPosition = localHuman.transform.position;
        Vector3 playerForward = localHuman.transform.forward;

        List<ObjectPickup> availablePickups = new List<ObjectPickup>();

        // Find all pickups that are in onGroundItem state and can be picked up
        foreach (ObjectPickup pickup in activePickups)
        {
            if (pickup != null &&
                pickup.currentState == ObjectState.onGroundItem &&
                pickup.canBePickedUp &&
                pickup.isInside) // Only consider pickups where player is inside trigger
            {
                availablePickups.Add(pickup);
            }
        }

        if (availablePickups.Count == 0) return null;
        if (availablePickups.Count == 1) return availablePickups[0];

        // If multiple pickups available, find the closest one
        ObjectPickup closestPickup = null;
        float closestDistance = float.MaxValue;

        foreach (ObjectPickup pickup in availablePickups)
        {
            if (pickup == null) continue;

            float distance = Vector3.Distance(playerPosition, pickup.transform.position);

            // Simple distance-based priority for now
            if (distance < closestDistance)
            {
                closestPickup = pickup;
                closestDistance = distance;
            }
        }

        Debug.Log($"Closest pickup found: {closestPickup?.name} at distance {closestDistance}");
        return closestPickup;
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

        // Store original stats and apply penalty (only on owner's client)
        if (human.photonView.IsMine)
        {
            StoreAndModifyStats(human, true);
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

        // Restore original stats (only on owner's client)
        PhotonView ownerPhotonView = PhotonView.Find(currentOwnerViewID);
        if (ownerPhotonView != null && ownerPhotonView.IsMine)
        {
            Human ownerHuman = ownerPhotonView.GetComponent<Human>();
            if (ownerHuman != null)
            {
                RestoreStats(ownerHuman);
            }
        }

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

    private void StoreAndModifyStats(Human human, bool applyPenalty)
    {
        var stats = human.Stats;

        // Store original stats (only speed and acceleration)
        originalOwnerSpeed = stats.Speed;
        originalOwnerAcceleration = stats.Acceleration;

        // Apply penalty using RPC
        if (applyPenalty)
        {
            int newSpeed = Mathf.Max(minimumStatValue, stats.Speed - statPenalty);
            int newAcceleration = Mathf.Max(minimumStatValue, stats.Acceleration - statPenalty);

            human.photonView.RPC("RPC_SetStats", RpcTarget.AllBufferedViaServer,
                newSpeed, stats.Gas, stats.Ammunition, newAcceleration, stats.HorseSpeed);

            Debug.Log($"Stats modified: Speed {stats.Speed} -> {newSpeed}, Acceleration {stats.Acceleration} -> {newAcceleration}");
        }
    }

    private void RestoreStats(Human human)
    {
        var stats = human.Stats;

        // Restore original stats using RPC (only speed and acceleration, keep other stats the same)
        human.photonView.RPC("RPC_SetStats", RpcTarget.AllBufferedViaServer,
            originalOwnerSpeed, stats.Gas, stats.Ammunition, originalOwnerAcceleration, stats.HorseSpeed);

        Debug.Log($"Stats restored: Speed {originalOwnerSpeed}, Acceleration {originalOwnerAcceleration}");
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