using UnityEngine;
using Characters;
using Photon.Pun;
using UI;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;

public class CannonBase : MonoBehaviourPunCallbacks
{
    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Prompt Texts")]
    public string mountPromptText = "Press G to Mount";
    public string unmountPromptText = "Press G to Unmount";

    [Header("Unmount Prompt Settings")]
    public float unmountPromptDuration = 5f;

    [Header("Animation Settings")]
    public bool useHorseIdle = true;
    public bool enableRunAnimation = true;
    public float runSpeedThreshold = 4f;

    [Header("Rigidbody Settings")]
    public bool disableGravityOnMount = true;
    public bool disableMassOnMount = true;
    public float mountedMass = 0.1f;

    [Header("Rotation Settings")]
    public Transform CannonBarrel;
    public float rotationSpeed = 5f;
    public float maxHorizontalAngle = 90f;
    public float maxVerticalAngle = 45f;

    [Header("Movement Settings")]
    public Transform MoveTarget;
    public float moveSpeed = 5f;
    public float turnSpeed = 90f;

    [Header("Firing Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float launchForce = 500f;
    public float upwardForce = 100f;

    private Rigidbody moveRigidbody;
    private Human humanInTrigger;
    private Rigidbody humanRigidbody;
    private bool isMounted = false;
    private bool hasExitedAfterUnmount = false;

    private float originalMass;
    private bool originalUseGravity;

    private static string currentPrompt = "";
    private float unmountPromptTimer = 0f;
    private Vector3 lastMountedWorldPos = Vector3.zero;
    private bool isCurrentlyRunning = false;

    private void Start()
    {
        if (MoveTarget != null)
            moveRigidbody = MoveTarget.GetComponent<Rigidbody>();
        ClearPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.IsMine())
        {
            humanInTrigger = human;
            humanRigidbody = human.GetComponent<Rigidbody>();
            hasExitedAfterUnmount = false;
            SetPrompt(mountPromptText);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human == humanInTrigger)
        {
            if (!isMounted)
            {
                hasExitedAfterUnmount = true;
                humanInTrigger = null;
                humanRigidbody = null;
                ClearPrompt();
            }
        }
    }

    private void Update()
    {
        HandleMountInput();
        HandleUnmountPromptTimer();
        HandleRunAnimation();
        RotateTowardsCamera();
        CheckDistanceOrAliveStatus();

        if (isMounted && Input.GetKeyDown(KeyCode.F))
        {
            FireProjectile();
        }
    }

    private void FixedUpdate()
    {
        HandleMovementInput();
    }

    private void HandleMountInput()
    {
        if (humanInTrigger == null)
            return;

        if (!InGameMenu.InMenu() && !ChatManager.IsChatActive())
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (!isMounted && !hasExitedAfterUnmount)
                    AttachHuman();
                else if (isMounted)
                    DetachHuman();
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
        if (!isMounted || humanInTrigger == null || !enableRunAnimation)
            return;

        if (humanInTrigger.MountedTransform == null)
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

    private void AttachHuman()
    {
        if (humanInTrigger == null || mountPoint == null)
            return;

        humanInTrigger.MountedTransform = mountPoint;
        humanInTrigger.MountedMapObject = null;
        humanInTrigger.MountedPositionOffset = positionOffset;
        humanInTrigger.MountedRotationOffset = rotationOffset;
        humanInTrigger.MountState = HumanMountState.MapObject;
        humanInTrigger.SetInterpolation(false);

        if (humanRigidbody != null)
        {
            originalMass = humanRigidbody.mass;
            originalUseGravity = humanRigidbody.useGravity;

            if (disableGravityOnMount)
                humanRigidbody.useGravity = false;
            if (disableMassOnMount)
                humanRigidbody.mass = mountedMass;
        }

        isMounted = true;
        hasExitedAfterUnmount = false;

        SetPrompt(unmountPromptText);
        unmountPromptTimer = unmountPromptDuration;

        lastMountedWorldPos = humanInTrigger.MountedTransform.TransformPoint(humanInTrigger.MountedPositionOffset);
        humanInTrigger.CrossFadeIfNotPlaying(GetIdleAnimation(), 0.2f);
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null)
            return;

        humanInTrigger.Unmount(true);

        if (humanRigidbody != null)
        {
            humanRigidbody.useGravity = originalUseGravity;
            humanRigidbody.mass = originalMass;
        }

        isMounted = false;

        if (humanInTrigger != null && !hasExitedAfterUnmount)
        {
            SetPrompt(mountPromptText);
            unmountPromptTimer = 0f;
        }
        else
        {
            ClearPrompt();
        }
    }

    private string GetIdleAnimation()
    {
        return useHorseIdle ? HumanAnimations.HorseIdle : HumanAnimations.IdleM;
    }

    private void RotateTowardsCamera()
    {
        if (!isMounted || CannonBarrel == null || Camera.main == null)
            return;

        Vector3 localForward = transform.InverseTransformDirection(Camera.main.transform.forward);
        float yaw = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Asin(localForward.y) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, -maxHorizontalAngle, maxHorizontalAngle);
        pitch = Mathf.Clamp(pitch, -maxVerticalAngle, maxVerticalAngle);

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        CannonBarrel.localRotation = Quaternion.Slerp(CannonBarrel.localRotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleMovementInput()
    {
        if (!isMounted || moveRigidbody == null)
            return;

        float move = 0f;
        float rotate = 0f;

        if (Input.GetKey(KeyCode.W)) move += 1f;
        if (Input.GetKey(KeyCode.S)) move -= 1f;
        if (Input.GetKey(KeyCode.D)) rotate += 1f;
        if (Input.GetKey(KeyCode.A)) rotate -= 1f;

        Vector3 forwardMovement = Vector3.ProjectOnPlane(MoveTarget.forward, Vector3.up).normalized * move * moveSpeed * Time.fixedDeltaTime;
        moveRigidbody.MovePosition(moveRigidbody.position + forwardMovement);

        Quaternion deltaRotation = Quaternion.Euler(0f, rotate * turnSpeed * Time.fixedDeltaTime, 0f);
        moveRigidbody.MoveRotation(moveRigidbody.rotation * deltaRotation);
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        string prefabPath = $"Buildables/Projectiles/{projectilePrefab.name}";
        GameObject projectile = PhotonNetwork.Instantiate(prefabPath, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = firePoint.forward * launchForce + firePoint.up * upwardForce;
            rb.AddForce(force);
        }
    }

    private void CheckDistanceOrAliveStatus()
    {
        if (!isMounted || humanInTrigger == null)
            return;

        bool isTooFar = Vector3.Distance(transform.position, humanInTrigger.transform.position) > 40f;
        bool isDead = humanInTrigger.Dead;

        if (isTooFar || isDead)
        {
            DetachHuman();
        }
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.alignment = TextAnchor.UpperCenter;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(Screen.width / 2 - 150, 10, 300, 50), currentPrompt, style);
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
