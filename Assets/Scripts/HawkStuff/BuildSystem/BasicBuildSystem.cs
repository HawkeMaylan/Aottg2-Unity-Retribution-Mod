using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Characters;
using System.Collections;
using GameManagers;
using UI;

public class BuildSystem : MonoBehaviourPunCallbacks
{
    [Header("References")]
    public Transform cam;
    public LayerMask buildLayer;
    public Material buildableMaterial;
    public Material notBuildableMaterial;

    [Header("Controls")]
    public KeyCode toggleKey = KeyCode.BackQuote;
    public KeyCode buildKey = KeyCode.K;
    public KeyCode placeKey = KeyCode.UpArrow;
    public KeyCode rotateLeftKey = KeyCode.LeftArrow;
    public KeyCode rotateRightKey = KeyCode.RightArrow;
    public KeyCode resetRotationKey = KeyCode.Space;
    public KeyCode snapToSurfaceKey = KeyCode.T;

    // Building state
    private bool isBuilding = false;
    private bool scriptActive = false;
    private GameObject currentPreview;
    private Vector3 currentPos;
    private Quaternion currentRotation;
    private List<GameObject> buildablePrefabs = new List<GameObject>();
    private int currentBuildableIndex = 0;
    private HumanInventory _playerInventory;
    private bool _inventorySearchPerformed = false;
    private Dictionary<string, int> _pendingRefunds = new Dictionary<string, int>();
    private bool IsLocalPlayer => PhotonNetwork.LocalPlayer != null &&
                             _playerInventory != null &&
                             _playerInventory.photonView != null &&
                             _playerInventory.photonView.IsMine;

    // Rotation state
    private Quaternion surfaceAlignmentRotation = Quaternion.identity;
   
    private RotationAxis currentRotationAxis = RotationAxis.Y;


    private enum RotationAxis { X, Y, Z }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => FindLocalPlayerInventory() || _inventorySearchPerformed);

        if (_playerInventory == null)
        {
            Debug.LogError("BuildSystem: Failed to find player inventory after waiting");
            yield break;
        }

        LoadBuildablePrefabs();
        InitializeRadialMenu();

        Debug.Log("BuildSystem: Initialized with inventory");
    }

    private bool FindLocalPlayerInventory()
    {
        // Method 1: Find by PhotonView ownership (most reliable)
        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human != null && human.photonView != null && human.photonView.IsMine)
            {
                _playerInventory = human.GetComponent<HumanInventory>();
                if (_playerInventory != null)
                {
                    Debug.Log("BuildSystem: Found local player inventory via PhotonView ownership");
                    _inventorySearchPerformed = true;
                    return true;
                }
            }
        }

        // Method 2: Fallback to tag search
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerInventory = player.GetComponentInChildren<HumanInventory>(true);
            if (_playerInventory != null)
            {
                Debug.Log("BuildSystem: Found player inventory via tag search");
                _inventorySearchPerformed = true;
                return true;
            }
        }

        // Method 3: Final fallback
        _playerInventory = FindObjectOfType<HumanInventory>();
        if (_playerInventory != null)
        {
            Debug.Log("BuildSystem: Found player inventory via scene search");
            _inventorySearchPerformed = true;
            return true;
        }

        return false;
    }

    void Update()
    {
        HandleSystemToggle();
        if (!scriptActive) return;

        HandleBuildingToggle();
        if (isBuilding)
        {
            UpdatePreview();
            HandleBuildingInput();
        }
    }

    void LoadBuildablePrefabs()
    {
        buildablePrefabs.Clear();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Buildables");
        foreach (GameObject prefab in prefabs)
        {
            if (prefab.GetComponent<BuildableObjectHelper>() != null)
            {
                buildablePrefabs.Add(prefab);
                Debug.Log($"BuildSystem: Loaded buildable prefab {prefab.name}");
            }
        }

        if (buildablePrefabs.Count == 0)
        {
            Debug.LogWarning("BuildSystem: No buildable prefabs found in Resources/Buildables");
        }
    }

    void InitializeRadialMenu()
    {
        RadialMenuController radialMenu = FindObjectOfType<RadialMenuController>();
        if (radialMenu != null)
        {
            radialMenu.InitializeWithBuildables(buildablePrefabs);
            Debug.Log("BuildSystem: Radial menu initialized");
        }
    }

    void HandleSystemToggle()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            scriptActive = !scriptActive;
            isBuilding = false;
            ToggleCursor(!scriptActive);

            if (!scriptActive && currentPreview != null)
            {
                Destroy(currentPreview);
            }
        }
    }

    void HandleBuildingToggle()
    {
        if (Input.GetKeyDown(buildKey) && !InGameMenu.InMenu() && !ChatManager.IsChatActive())
        {
            isBuilding = !isBuilding;

            if (!isBuilding && currentPreview != null)
            {
                Destroy(currentPreview);
            }
            else if (isBuilding && currentPreview == null)
            {
                CreatePreview();
            }
        }
    }

    void CreatePreview()
    {
        if (currentBuildableIndex < 0 || currentBuildableIndex >= buildablePrefabs.Count)
            return;

        GameObject prefab = buildablePrefabs[currentBuildableIndex];
        BuildableObjectHelper helper = prefab.GetComponent<BuildableObjectHelper>();

        if (helper == null || helper.preview == null)
        {
            Debug.LogError("BuildSystem: Missing BuildableObjectHelper or preview");
            return;
        }

        // Clean up existing preview
        if (currentPreview != null)
        {
            Destroy(currentPreview);
        }

        // Reset all rotation states
        currentRotation = Quaternion.identity;
        surfaceAlignmentRotation = Quaternion.identity;

        // Initialize with forced alignment if enabled
        Quaternion spawnRotation = Quaternion.identity;
        if (helper.forceUpAlignment)
        {
            spawnRotation = helper.GetForcedRotation();
            Debug.Log($"Applying forced alignment - Up: {helper.forcedUpAxis}, Forward: {helper.forwardAxis}");
        }

        // Create new preview with proper rotation
        currentPreview = Instantiate(helper.preview, currentPos, spawnRotation);
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("Preview"));



        Debug.Log($"Created preview for {prefab.name} " +
                 $"(Force Up: {helper.forceUpAlignment}, " +
                 $"Rotation: {spawnRotation.eulerAngles})");
    }

    void UpdatePreview()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 40, buildLayer))
        {
            BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
            if (helper == null) return;

            // Calculate grid-aligned position
            float gridSize = helper.gridSize;
            currentPos = hit.point + hit.normal * helper.offset;
            currentPos = new Vector3(
                Mathf.Round(currentPos.x / gridSize) * gridSize,
                Mathf.Round(currentPos.y / gridSize) * gridSize,
                Mathf.Round(currentPos.z / gridSize) * gridSize
            );

            // Calculate surface alignment
            surfaceAlignmentRotation = helper.snapToSurface ?
                Quaternion.FromToRotation(Vector3.up, hit.normal) :
                Quaternion.identity;

            // Update preview position
            currentPreview.transform.position = currentPos;

            // Apply rotation based on helper settings
            if (helper.forceUpAlignment)
            {
                // Get the forced rotation from helper
                Quaternion forcedRotation = helper.GetForcedRotation();

                // Combine rotations:
                // 1. First align with surface normal
                // 2. Then apply the forced axis alignment
                // 3. Finally apply any user rotation
                currentPreview.transform.rotation = surfaceAlignmentRotation *
                                      helper.GetForcedRotation() *
                                      currentRotation;
            }
            else
            {
                // Standard rotation behavior:
                // 1. Align with surface normal (if enabled)
                // 2. Apply user rotation
                currentPreview.transform.rotation = surfaceAlignmentRotation * currentRotation;
            }

            // Update preview materials based on validity
            UpdatePreviewMaterials();
        }
    }

    void UpdatePreviewMaterials()
    {
        // Only check position validity for preview, not costs
        bool isValid = IsPreviewValid();
        foreach (Renderer renderer in currentPreview.GetComponentsInChildren<Renderer>())
        {
            renderer.material = isValid ? buildableMaterial : notBuildableMaterial;
        }
    }

    bool IsPreviewValid()
    {
        if (currentPreview == null) return false;

        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        if (helper == null || helper.collisionCheckObject == null) return false;

        Vector3 checkPos = currentPreview.transform.position + helper.collisionCheckObject.transform.localPosition;
        Collider[] colliders = Physics.OverlapBox(
            checkPos,
            helper.collisionCheckObject.GetComponent<Collider>().bounds.extents,
            currentPreview.transform.rotation,
            buildLayer | (1 << LayerMask.NameToLayer("Player"))
        );

        foreach (Collider col in colliders)
        {
            if (col != null && col.gameObject != currentPreview)
            {
                return false;
            }
        }
        return true;
    }

    void HandleBuildingInput()
    {
        // Rotation application
        if (Input.GetKeyDown(rotateLeftKey))
            RotatePreview(-1f); // Counter-clockwise
        if (Input.GetKeyDown(rotateRightKey))
            RotatePreview(1f); // Clockwise

        // Rotation reset
        if (Input.GetKeyDown(resetRotationKey))
        {
            BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
            currentRotation = helper.forceUpAlignment ? helper.GetForcedRotation() : Quaternion.identity;

            if (currentPreview != null)
            {
                currentPreview.transform.rotation = surfaceAlignmentRotation * currentRotation;
            }
        }

        // Snap to surface normal
        if (Input.GetKeyDown(snapToSurfaceKey) && !InGameMenu.InMenu() && !ChatManager.IsChatActive())
        {
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 40, buildLayer))
            {
                surfaceAlignmentRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (currentPreview != null)
                {
                    currentPreview.transform.rotation = surfaceAlignmentRotation * currentRotation;
                }
            }
        }

        if (Input.GetKeyDown(placeKey))
        {
            Build();
        }
    }

    void RotatePreview(float direction)
    {
        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        if (helper == null) return;

        // Get the axis to rotate around from the prefab settings
        Vector3 axis = Vector3.up;
        switch (helper.rotationAxis)
        {
            case BuildableObjectHelper.RotationAxis.X: axis = Vector3.right; break;
            case BuildableObjectHelper.RotationAxis.Y: axis = Vector3.up; break;
            case BuildableObjectHelper.RotationAxis.Z: axis = Vector3.forward; break;
        }

        // Apply rotation using the prefab's increment
        currentRotation *= Quaternion.AngleAxis(direction * helper.rotationIncrement, axis);

        if (currentPreview != null)
        {
            if (helper.forceUpAlignment)
            {
                Quaternion forcedRotation = helper.GetForcedRotation();
                currentPreview.transform.rotation = surfaceAlignmentRotation * forcedRotation * currentRotation;
            }
            else
            {
                currentPreview.transform.rotation = surfaceAlignmentRotation * currentRotation;
            }
        }
    }

    void Build()
    {
        // 1. Validate build position
        if (currentPreview == null || !IsPreviewValid()) return;

        // 2. Only allow the LOCAL player to build
        if (!IsLocalPlayer)
        {
            Debug.Log("Not local player - skipping build logic");
            return;
        }

        // 3. Check & deduct resources (local only)
        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        if (helper == null) return;

        foreach (InventoryCost cost in helper.buildCosts)
        {
            if (_playerInventory.GetItemCount(cost.itemName) < cost.amount)
            {
                _playerInventory.ShowNotEnoughMessage(cost.itemName);
                return;
            }
            _playerInventory.SetItemCount(cost.itemName, _playerInventory.GetItemCount(cost.itemName) - cost.amount);
        }

        // 4. Spawn object for ALL players (but only local player pays)
        PhotonNetwork.Instantiate(
            "Buildables/" + buildablePrefabs[currentBuildableIndex].name,
            currentPos,
            currentPreview.transform.rotation
        );

        // 5. Local effects
        if (helper.buildParticleEffectPrefab != null)
            SpawnBuildParticles(helper);

        Destroy(currentPreview);
        CreatePreview();
    }

    private void SpawnBuildParticles(BuildableObjectHelper helper)
    {
        if (helper.buildParticleEffectPrefab == null)
        {
            Debug.LogError("Particle prefab is not assigned!");
            return;
        }

        // Hardcode the path (assuming prefab is in "Resources/HParticles/")
        string particlePrefabName = "HParticles/" + helper.buildParticleEffectPrefab.name;

        Vector3 spawnPos = currentPos + currentPreview.transform.TransformDirection(helper.particleEffectOffset);
        Quaternion spawnRot = helper.particleUsePreviewRotation ? currentPreview.transform.rotation : Quaternion.identity;

        // Debug to verify the path (check Console in build)
        Debug.Log($"Spawning particle: {particlePrefabName} at {spawnPos}");

        // Try Photon instantiation (fallback to local if fails)
        GameObject spawnedParticles = null;
        if (PhotonNetwork.IsConnectedAndReady)
        {
            spawnedParticles = PhotonNetwork.Instantiate(particlePrefabName, spawnPos, spawnRot);
        }
        else
        {
            Debug.LogWarning("Photon not ready, spawning locally");
            spawnedParticles = Instantiate(helper.buildParticleEffectPrefab, spawnPos, spawnRot);
        }

        if (spawnedParticles == null)
        {
            Debug.LogError("FAILED TO SPAWN PARTICLES!");
            return;
        }

        // Parenting logic (if needed)
        if (helper.particleParentToBuilding)
        {
            StartCoroutine(ParentParticlesAfterBuild(spawnedParticles, currentPos));
        }
    }

    private IEnumerator ParentParticlesAfterBuild(GameObject particles, Vector3 buildPosition)
    {
        // Wait one frame to allow building to spawn
        yield return null;

        // Find the nearest building object at our build position
        Collider[] colliders = Physics.OverlapSphere(buildPosition, 0.5f);
        foreach (Collider col in colliders)
        {
            if (col.gameObject != currentPreview && col.CompareTag("Buildable"))
            {
                particles.transform.SetParent(col.transform);
                break;
            }
        }
    }

    // Helper method to get Resources path for a prefab
    private string GetPrefabResourcePath(GameObject prefab)
    {
#if UNITY_EDITOR
    string path = UnityEditor.AssetDatabase.GetAssetPath(prefab);
    int resourcesIndex = path.IndexOf("Resources/");
    if (resourcesIndex < 0) return null;
    
    string resourcesPath = path.Substring(resourcesIndex + "Resources/".Length);
    return resourcesPath.Replace(".prefab", "");
#else
        // For runtime, we need to know the path - this is why we recommend setting it in editor
        // and storing as a serialized field if you need it at runtime
        return null;
#endif
    }

    bool CanAffordBuild()
    {
        if (_playerInventory == null) return false;

        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        foreach (InventoryCost cost in helper.buildCosts)
        {
            if (_playerInventory.GetItemCount(cost.itemName) < cost.amount)
            {
                return false;
            }
        }
        return true;
    }

    public void HandleBuildableSelection(GameObject prefab)
    {
        int index = buildablePrefabs.IndexOf(prefab);
        if (index == -1)
        {
            Debug.LogError($"BuildSystem: Prefab {prefab.name} not in buildable list");
            return;
        }

        currentBuildableIndex = index;

        if (isBuilding)
        {
            if (currentPreview != null) Destroy(currentPreview);
            CreatePreview();
        }
        else
        {
            isBuilding = true;
            scriptActive = true;
            ToggleCursor(false);
            CreatePreview();
        }
    }

    void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    
}