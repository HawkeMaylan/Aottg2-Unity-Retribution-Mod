using UnityEngine;
using Characters;
using Photon.Pun;
using UI;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class CannonProjectileOption
{
    public string name;
    public GameObject prefab;
    public float launchForce = 500f;
    public float upwardForce = 100f;
    public Sprite sprite;
}

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
    public Transform firePoint;
    public List<CannonProjectileOption> projectileOptions = new List<CannonProjectileOption>();

    [Header("Projectile UI")]
    public GameObject projectileUIPrefab;

    private int selectedProjectileIndex = 0;
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
    private GameObject currentUIImage;

    private void Start()
    {
        if (MoveTarget != null)
            moveRigidbody = MoveTarget.GetComponent<Rigidbody>();
        ClearPrompt();
    }

    private void Update()
    {
        HandleMountInput();
        HandleUnmountPromptTimer();
        HandleRunAnimation();
        RotateTowardsCamera();
        CheckDistanceOrAliveStatus();

        if (isMounted && Input.GetKeyDown(KeyCode.F))
            FireProjectile();

        if (isMounted)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                SelectProjectile((selectedProjectileIndex - 1 + projectileOptions.Count) % projectileOptions.Count);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                SelectProjectile((selectedProjectileIndex + 1) % projectileOptions.Count);
        }
    }

    private void FixedUpdate()
    {
        HandleMovementInput();
    }

    public void SelectProjectile(int index)
    {
        if (index >= 0 && index < projectileOptions.Count)
        {
            selectedProjectileIndex = index;
            UpdateProjectileUI();
        }
    }

    private void UpdateProjectileUI()
    {
        if (projectileUIPrefab == null) return;

        if (currentUIImage != null)
            Destroy(currentUIImage);

        GameObject defaultMenu = GameObject.Find("DefaultMenu(Clone)");
        if (defaultMenu == null)
        {
            Debug.LogWarning("DefaultMenu(Clone) not found in scene.");
            return;
        }

        currentUIImage = Instantiate(projectileUIPrefab, defaultMenu.transform);
        currentUIImage.transform.localPosition = new Vector3(-186.7f, 224.7f, 0f);
        currentUIImage.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 200f);

        Image img = currentUIImage.GetComponent<Image>();
        if (img != null && projectileOptions[selectedProjectileIndex].sprite != null)
            img.sprite = projectileOptions[selectedProjectileIndex].sprite;
    }

    private void FireProjectile()
    {
        if (firePoint == null || projectileOptions.Count == 0)
            return;

        CannonProjectileOption selected = projectileOptions[selectedProjectileIndex];
        if (selected.prefab == null)
            return;

        string prefabPath = $"Buildables/Projectiles/{selected.prefab.name}";
        GameObject projectile = PhotonNetwork.Instantiate(prefabPath, firePoint.position, firePoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 force = firePoint.forward * selected.launchForce + firePoint.up * selected.upwardForce;
            rb.AddForce(force);
        }
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
        if (human != null && human == humanInTrigger && !isMounted)
        {
            hasExitedAfterUnmount = true;
            humanInTrigger = null;
            humanRigidbody = null;
            ClearPrompt();
        }
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

        UpdateProjectileUI();
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

        if (currentUIImage != null)
        {
            Destroy(currentUIImage);
            currentUIImage = null;
        }

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

    private void CheckDistanceOrAliveStatus()
    {
        if (!isMounted || humanInTrigger == null)
            return;

        bool isTooFar = Vector3.Distance(transform.position, humanInTrigger.transform.position) > 40f;
        bool isDead = humanInTrigger.Dead;

        if (isTooFar || isDead)
            DetachHuman();
    }

    private void OnGUI()
    {
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

    private void SetPrompt(string text)
    {
        currentPrompt = text;
    }

    private void ClearPrompt()
    {
        currentPrompt = "";
    }
}
