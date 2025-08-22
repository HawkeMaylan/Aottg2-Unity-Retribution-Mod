using UnityEngine;
using Characters;
using Photon.Pun;
using UI;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using UnityEngine.UI;

public class CannonMount : MonoBehaviourPunCallbacks
{
    [Header("Interaction Settings")]
    public Collider interactionZone;

    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("UI Settings")]
    public float unmountPromptDuration = 5f;
    public GameObject projectileUIPrefab;

    [Header("Animation Settings")]
    public MountAnimationType animationType = MountAnimationType.Sitting;
    public bool enableRunAnimation = false;
    public float runSpeedThreshold = 4f;

    private Human humanInTrigger;
    private bool isMounted = false;
    private bool hasExitedAfterUnmount = false;
    private bool isCurrentlyRunning = false;
    private Vector3 lastMountedWorldPos = Vector3.zero;

    private static string currentPrompt = "";
    private float unmountPromptTimer = 0f;

    private GameObject currentUIImage;
    private Image currentUIImageRenderer;

    private string MountPromptText;
    private string UnmountPromptText;
    private string _lastCachedKey = "";
    private float mountPromptExpireTime = -1f;

    public enum MountAnimationType
    {
        Sitting,
        Standing,
        HorseIdle,
        Custom
    }

    private void Start()
    {
        UpdatePromptTexts();
        ClearPrompt();
    }

    private void Update()
    {
        string currentKey = SettingsManager.InputSettings.Interaction.Interact.ToString();
        if (_lastCachedKey != currentKey)
        {
            _lastCachedKey = currentKey;
            UpdatePromptTexts();
            if (!isMounted)
                SetPrompt(MountPromptText);
            else
                SetPrompt(UnmountPromptText);
        }

        // Check for grab state and auto-dismount if grabbed
        if (isMounted && humanInTrigger != null && IsHumanGrabbed())
        {
            Debug.Log("Detaching due to player being grabbed.");
            DetachHuman();
            return;
        }

        HandleMountInput();
        HandleUnmountPromptTimer();
        CheckDistanceOrAliveStatus();

        // Handle run animation if enabled
        if (isMounted && enableRunAnimation)
        {
            HandleRunAnimation();
        }

        // Only detect nearby humans if not already mounted
        if (!isMounted)
        {
            DetectNearbyHuman();
        }
    }

    private void DetectNearbyHuman()
    {
        // Auto-clear prompt if it times out
        if (mountPromptExpireTime > 0f && Time.time > mountPromptExpireTime)
        {
            ClearPrompt();
            mountPromptExpireTime = -1f;
            humanInTrigger = null;
            return;
        }

        // Detect nearby human using interactionZone
        bool playerFound = false;

        if (humanInTrigger == null && interactionZone != null)
        {
            Collider[] hits = Physics.OverlapBox(
                interactionZone.bounds.center,
                interactionZone.bounds.extents,
                interactionZone.transform.rotation
            );

            foreach (var hit in hits)
            {
                Human h = hit.GetComponentInParent<Human>();
                if (h != null && h.IsMine())
                {
                    humanInTrigger = h;
                    SetPrompt(MountPromptText);
                    mountPromptExpireTime = Time.time + 10f;
                    playerFound = true;
                    break;
                }
            }
        }

        // Clear if player leaves zone (only when not mounted)
        if (!playerFound && humanInTrigger != null && !isMounted)
        {
            float dist = Vector3.Distance(humanInTrigger.transform.position, transform.position);
            if (dist > 40f || !interactionZone.bounds.Contains(humanInTrigger.transform.position))
            {
                humanInTrigger = null;
                ClearPrompt();
                mountPromptExpireTime = -1f;
            }
        }
    }

    private void HandleMountInput()
    {
        // When mounted, we can still use the stored humanInTrigger reference
        if (humanInTrigger == null && !isMounted) return;

        // Prevent mount/unmount if player is no longer actually at this mount point
        if (isMounted && humanInTrigger != null && humanInTrigger.MountedTransform != mountPoint)
            return;

        if (!InGameMenu.InMenu() && !ChatManager.IsChatActive())
        {
            if (SettingsManager.InputSettings.Interaction.Interact.GetKeyDown())
            {
                if (!isMounted && !hasExitedAfterUnmount && humanInTrigger != null)
                {
                    AttachHuman();
                }
                else if (isMounted)
                {
                    DetachHuman();
                    mountPromptExpireTime = -1f;
                }
            }
        }
    }

    private void HandleUnmountPromptTimer()
    {
        if (isMounted && unmountPromptTimer > 0f)
        {
            unmountPromptTimer -= Time.deltaTime;
            if (unmountPromptTimer <= 0f)
                ClearPrompt();
        }
    }

    private void HandleRunAnimation()
    {
        if (!isMounted || humanInTrigger == null || humanInTrigger.MountedTransform == null)
            return;

        Vector3 currentWorldPos = humanInTrigger.MountedTransform.TransformPoint(humanInTrigger.MountedPositionOffset);
        float speed = (currentWorldPos - lastMountedWorldPos).magnitude / Time.deltaTime;
        lastMountedWorldPos = currentWorldPos;

        if (speed > runSpeedThreshold)
        {
            if (!isCurrentlyRunning)
            {
                humanInTrigger.CrossFadeIfNotPlaying(HumanAnimations.HorseRun, 0.23f);
                isCurrentlyRunning = true;
            }
        }
        else
        {
            if (isCurrentlyRunning)
            {
                humanInTrigger.CrossFadeIfNotPlaying(GetIdleAnimation(), 0.1f);
                isCurrentlyRunning = false;
            }
        }
    }

    private void CheckDistanceOrAliveStatus()
    {
        if (!isMounted || humanInTrigger == null) return;

        bool isTooFar = Vector3.Distance(transform.position, humanInTrigger.transform.position) > 40f;
        bool isDead = humanInTrigger.Dead;
        bool isGrabbed = IsHumanGrabbed();

        if (isTooFar || isDead || isGrabbed)
        {
            Debug.LogWarning("Detaching due to distance, death, or grab.");
            DetachHuman();
            ClearPrompt();
        }
    }

    private void AttachHuman()
    {
        if (!ValidateHumanInTrigger()) return;

        if (humanInTrigger == null || mountPoint == null || isMounted)
        {
            Debug.LogWarning("Invalid mount attempt - missing human or already mounted.");
            return;
        }

        if (!photonView.IsMine)
        {
            Debug.Log("Requesting ownership before mounting.");
            photonView.RequestOwnership();
        }

        // Confirm human isn't already mounted to something else
        if (humanInTrigger.MountState != HumanMountState.None && humanInTrigger.MountedTransform != mountPoint)
        {
            Debug.LogWarning("Human is already mounted elsewhere.");
            return;
        }

        // Use the human's built-in mounting system - HumanMovementSync will handle the rest
        humanInTrigger.MountedTransform = mountPoint;
        humanInTrigger.MountedPositionOffset = positionOffset;
        humanInTrigger.MountedRotationOffset = rotationOffset;
        humanInTrigger.MountState = HumanMountState.MapObject;

        isMounted = true;
        hasExitedAfterUnmount = false;

        // Play the appropriate animation
        humanInTrigger.CrossFadeIfNotPlaying(GetIdleAnimation(), 0.2f);
        lastMountedWorldPos = humanInTrigger.MountedTransform.TransformPoint(humanInTrigger.MountedPositionOffset);

        Debug.Log("Human mounted successfully.");

        ClearPrompt();
        SetPrompt(UnmountPromptText);
        mountPromptExpireTime = Time.time + unmountPromptDuration;
        unmountPromptTimer = unmountPromptDuration;

        // Show UI when mounted
        ShowMountUI();
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null) return;

        // Remove UI when dismounting
        HideMountUI();

        if (!ValidateHumanInTrigger())
        {
            humanInTrigger = null;
            isMounted = false;
            return;
        }

        // Simply unmount - HumanMovementSync will handle the physics restoration
        humanInTrigger.Unmount(true);

        isMounted = false;
        isCurrentlyRunning = false;

        // After dismounting, check if player is still in range
        if (humanInTrigger != null)
        {
            float dist = Vector3.Distance(humanInTrigger.transform.position, transform.position);
            if (dist <= 40f && interactionZone.bounds.Contains(humanInTrigger.transform.position))
            {
                SetPrompt(MountPromptText);
                mountPromptExpireTime = Time.time + 10f;
            }
            else
            {
                humanInTrigger = null;
                ClearPrompt();
                mountPromptExpireTime = -1f;
            }
        }
        else
        {
            ClearPrompt();
        }

        unmountPromptTimer = 0f;
    }

    private string GetIdleAnimation()
    {
        switch (animationType)
        {

            case MountAnimationType.Standing:
                return HumanAnimations.IdleM;
            case MountAnimationType.HorseIdle:
                return HumanAnimations.HorseIdle;
            case MountAnimationType.Custom:
                // You can add custom animation name field if needed
                return HumanAnimations.IdleM;
            default:
                return HumanAnimations.IdleM;
        }
    }

    private void ShowMountUI()
    {
        if (humanInTrigger == null || !humanInTrigger.IsMine()) return;

        GameObject menu = GameObject.Find("DefaultMenu(Clone)");
        if (menu == null || projectileUIPrefab == null) return;

        // Create simple UI element to show mounted state
        currentUIImage = Instantiate(projectileUIPrefab, menu.transform);
        currentUIImageRenderer = currentUIImage.GetComponent<Image>();

        // Position the UI
        RectTransform rt = currentUIImage.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(-180f, 100f);
        rt.sizeDelta = new Vector2(130f, 130f);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // You can set a specific sprite or color for mounted state
        currentUIImageRenderer.color = Color.green;
    }

    private void HideMountUI()
    {
        if (currentUIImage != null)
        {
            Destroy(currentUIImage);
            currentUIImageRenderer = null;
        }
    }

    private bool ValidateHumanInTrigger()
    {
        if (humanInTrigger == null)
            return false;

        bool isDead = humanInTrigger.Dead || !humanInTrigger.gameObject.activeInHierarchy;
        bool isNotMine = !humanInTrigger.IsMine();
        bool isGrabbed = IsHumanGrabbed();

        return !(isDead || isNotMine || isGrabbed);
    }

    private bool IsHumanGrabbed()
    {
        return humanInTrigger != null && humanInTrigger.State == HumanState.Grab;
    }

    private void SetPrompt(string text)
    {
        if (humanInTrigger == null || !humanInTrigger.IsMine()) return;
        currentPrompt = text;
    }

    private void ClearPrompt()
    {
        if (humanInTrigger == null || !humanInTrigger.IsMine()) return;
        currentPrompt = "";
    }

    private void UpdatePromptTexts()
    {
        string key = SettingsManager.InputSettings.Interaction.Interact.ToString().Replace("Alpha", "");
        MountPromptText = $"Press {key} to Mount";
        UnmountPromptText = $"Press {key} to Unmount";
    }

    private void OnGUI()
    {
        if (humanInTrigger == null || !humanInTrigger.IsMine()) return;

        if (!string.IsNullOrEmpty(currentPrompt))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(Screen.width / 2 - 150, 10, 300, 50), currentPrompt, style);
        }
    }
}