using UnityEngine;
using Characters;
using Photon.Pun;
using UI;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class CannonProjectileOption
{
    public string name;
    public GameObject prefab;
    public float launchForce = 500f;
    public float upwardForce = 100f;
    public Sprite sprite;
    public int ammoCount = 5;
    public float fireCooldown = 1f;
    public int projectileCount = 1;          
    public float spreadAngle = 0f;
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
    private float nextFireTime = 0f;



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

    private Image currentUIImageRenderer;

    private bool hasFlashedReady = false;
    private Coroutine flashGreenRoutine;
    private bool isFlashingGreen = false;

    private GameObject nextUIImage;
    private Image nextUIImageRenderer;

    private GameObject prevUIImage;
    private RectTransform prevRT, currRT, nextRT;
    private float uiLerpProgress = 1f;
    private int targetIndex = -1;
    private bool isSwapping = false;





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

        if (currentUIImageRenderer != null)
        {
            float cooldown = projectileOptions[selectedProjectileIndex].fireCooldown;
            float timeSinceFire = Time.time - (nextFireTime - cooldown);
            float progress = Mathf.Clamp01(timeSinceFire / cooldown);

            // Set color based on cooldown progress
            if (!isFlashingGreen)
                currentUIImageRenderer.color = Color.Lerp(Color.gray, Color.white, progress);


            // Flash green when cooldown ends
            if (progress >= 1f && !hasFlashedReady)
            {
                if (flashGreenRoutine != null)
                    StopCoroutine(flashGreenRoutine);

                flashGreenRoutine = StartCoroutine(FlashGreen());
                hasFlashedReady = true;
            }
            else if (progress < 1f)
            {
                hasFlashedReady = false;
            }
        }

        if (isSwapping && uiLerpProgress < 1f)
        {
            uiLerpProgress += Time.deltaTime * 4f; // adjust speed here
            float t = Mathf.SmoothStep(0, 1, uiLerpProgress);

            Vector2 center = new Vector2(-180f, 100f);
            Vector2 offset = new Vector2(90f, 0f);

            if (prevRT) prevRT.anchoredPosition = Vector2.Lerp(center - offset * 2, center - offset, t);
            if (currRT) currRT.anchoredPosition = Vector2.Lerp(center, center + (targetIndex > selectedProjectileIndex ? -offset : offset), t);
            if (nextRT) nextRT.anchoredPosition = Vector2.Lerp(center + offset, center + offset * 2, t);

            if (uiLerpProgress >= 1f)
            {
               
                isSwapping = false;
                UpdateProjectileUI();
            }
        }



    }

    private void FixedUpdate()
    {
        HandleMovementInput();
    }

    public void SelectProjectile(int index)
    {
        if (Time.time < nextFireTime || isSwapping || index == selectedProjectileIndex)
            return;

        int count = projectileOptions.Count;
        selectedProjectileIndex = (index + count) % count;
        UpdateProjectileUI(); // preload all icons now
        uiLerpProgress = 0f;
        isSwapping = true;

        uiLerpProgress = 0f;
        isSwapping = true;

        nextFireTime = Time.time + projectileOptions[selectedProjectileIndex].fireCooldown;
    }



    private void UpdateProjectileUI()
    {
        GameObject menu = GameObject.Find("DefaultMenu(Clone)");
        if (menu == null) return;

        // Clean up
        if (prevUIImage) Destroy(prevUIImage);
        if (currentUIImage) Destroy(currentUIImage);
        if (nextUIImage) Destroy(nextUIImage);

        int count = projectileOptions.Count;
        int prevIndex = (selectedProjectileIndex - 1 + count) % count;
        int nextIndex = (selectedProjectileIndex + 1) % count;

        Vector2 center = new Vector2(-180f, 100f);
        Vector2 offset = new Vector2(90f, 0f);

        // --- Prev Icon ---
        prevUIImage = Instantiate(projectileUIPrefab, menu.transform);
        prevRT = prevUIImage.GetComponent<RectTransform>();
        prevRT.anchoredPosition = center - offset;
        prevRT.sizeDelta = new Vector2(100f, 100f);
        prevRT.localScale = Vector3.one * 0.8f;
        prevRT.anchorMin = prevRT.anchorMax = new Vector2(1f, 0f);
        prevRT.pivot = new Vector2(0.5f, 0.5f);
        var prevImg = prevUIImage.GetComponent<Image>();
        prevImg.sprite = projectileOptions[prevIndex].sprite;
        prevImg.color = Color.gray;

        // --- Current Icon ---
        currentUIImage = Instantiate(projectileUIPrefab, menu.transform);
        currRT = currentUIImage.GetComponent<RectTransform>();
        currRT.anchoredPosition = center;
        currRT.sizeDelta = new Vector2(130f, 130f);
        currRT.localScale = Vector3.one;
        currRT.anchorMin = currRT.anchorMax = new Vector2(1f, 0f);
        currRT.pivot = new Vector2(0.5f, 0.5f);
        currentUIImageRenderer = currentUIImage.GetComponent<Image>();
        currentUIImageRenderer.sprite = projectileOptions[selectedProjectileIndex].sprite;

        // Ammo count
        var ammoTextObj = currentUIImage.transform.Find("AmmoText");
        if (ammoTextObj)
        {
            var ammoText = ammoTextObj.GetComponent<Text>();
            ammoText.text = $"x{projectileOptions[selectedProjectileIndex].ammoCount}";
        }

        // --- Next Icon ---
        nextUIImage = Instantiate(projectileUIPrefab, menu.transform);
        nextRT = nextUIImage.GetComponent<RectTransform>();
        nextRT.anchoredPosition = center + offset;
        nextRT.sizeDelta = new Vector2(100f, 100f);
        nextRT.localScale = Vector3.one * 0.8f;
        nextRT.anchorMin = nextRT.anchorMax = new Vector2(1f, 0f);
        nextRT.pivot = new Vector2(0.5f, 0.5f);
        var nextImg = nextUIImage.GetComponent<Image>();
        nextImg.sprite = projectileOptions[nextIndex].sprite;
        nextImg.color = Color.gray;
    }



    private void FireProjectile()
    {
        if (firePoint == null || projectileOptions.Count == 0)
            return;

        CannonProjectileOption selected = projectileOptions[selectedProjectileIndex];
        if (selected.prefab == null)
            return;

        if (Time.time < nextFireTime)
            return;

        if (selected.ammoCount <= 0)
        {
            if (currentUIImage != null)
            {
                StartCoroutine(FlashRed());
            }
            return;
        }

        nextFireTime = Time.time + selected.fireCooldown;
        selected.ammoCount--;
        UpdateProjectileUI();

        int count = Mathf.Max(1, selected.projectileCount);
        float spread = selected.spreadAngle;

        string prefabPath = $"Buildables/Projectiles/{selected.prefab.name}";

        for (int i = 0; i < count; i++)
        {
            // Compute a direction within a cone around forward
            Vector3 baseDirection = firePoint.forward;

            // Randomize within the spread cone
            Vector3 spreadDir = baseDirection;
            spreadDir = Quaternion.AngleAxis(Random.Range(-spread, spread), firePoint.up) * spreadDir;
            spreadDir = Quaternion.AngleAxis(Random.Range(-spread, spread), firePoint.right) * spreadDir;

            // Spawn the projectile
            GameObject projectile = PhotonNetwork.Instantiate(prefabPath, firePoint.position, Quaternion.LookRotation(spreadDir));

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 force = spreadDir * selected.launchForce + firePoint.up * selected.upwardForce;
                rb.AddForce(force);
            }
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

        if (nextUIImage != null)
        {
            Destroy(nextUIImage);
            nextUIImage = null;
        }

        if (prevUIImage != null)
        {
            Destroy(prevUIImage);
            prevUIImage = null;
        }

        nextUIImageRenderer = null;
        currentUIImageRenderer = null;
        prevRT = currRT = nextRT = null;

        
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
    private IEnumerator FlashRed()
    {
        Image img = currentUIImage?.GetComponent<Image>();
        if (img == null) yield break;

        Color original = img.color;
        img.color = Color.red;

        yield return new WaitForSeconds(0.2f);

        img.color = original;
    }

    private IEnumerator FlashGreen()
    {
        if (currentUIImageRenderer == null)
            yield break;

        isFlashingGreen = true;

        Color originalColor = currentUIImageRenderer.color;
        currentUIImageRenderer.color = Color.green;

        yield return new WaitForSeconds(0.3f);

        currentUIImageRenderer.color = Color.white;
        isFlashingGreen = false;
    }



}
