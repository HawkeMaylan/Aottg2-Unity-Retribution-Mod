using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Characters;
using System.Collections;
using GameManagers;
using UI;
using Photon.Realtime;
using System.IO;
using System.Linq;

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

    // Cached references for performance
    private BuildableObjectHelper _currentHelper;
    private RadialMenuController _cachedRadialMenu;
    private Coroutine _parentParticlesCoroutine;

    // Rotation state
    private Quaternion surfaceAlignmentRotation = Quaternion.identity;

    // Cache for performance (non-allocating)
    private Collider[] _colliderCache = new Collider[32];
    private List<Renderer> _rendererCache = new List<Renderer>(16);

    // Build tracking system
    [System.Serializable]
    public class BuildableObjectData
    {
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public int viewId;
        public string ownerId;
        public double timestamp;
    }

    [System.Serializable]
    public class BuildableObjectsCollection
    {
        public List<BuildableObjectData> objects = new List<BuildableObjectData>();
    }

    private static List<BuildableObjectData> _buildableObjects = new List<BuildableObjectData>();
    private static Dictionary<int, BuildableObjectData> _buildableObjectsByViewId = new Dictionary<int, BuildableObjectData>();

    private IEnumerator Start()
    {
        // Just load prefabs and initialize UI, don't search for inventory yet
        LoadBuildablePrefabs();
        InitializeRadialMenu();

        // Register for master client changes
        PhotonNetwork.AddCallbackTarget(this);

        Debug.Log("BuildSystem: Initialized - waiting for build attempt to find inventory");
        yield break;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Master client switched to: {newMasterClient.ActorNumber}");

        // If we become the new master client, we need to restore all buildables
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(RestoreBuildablesForNewMaster());
        }
    }

    private IEnumerator RestoreBuildablesForNewMaster()
    {
        yield return new WaitForSeconds(1f); // Wait for scene to stabilize

        Debug.Log("New master client restoring buildables...");

        // Clear existing buildables (in case any were orphaned)
        ClearAllBuildables();

        // Restore from our saved data
        foreach (var buildData in _buildableObjects)
        {
            yield return StartCoroutine(InstantiateBuildableForAll(buildData));
        }

        Debug.Log($"Restored {_buildableObjects.Count} buildables as new master");
    }

    private bool FindLocalPlayerInventory()
    {
        _playerInventory = null;

        // Method 1: Find by PhotonView ownership (most reliable)
        var humans = FindObjectsOfType<Human>();
        foreach (var human in humans)
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
        HumanInventory[] allInventories = FindObjectsOfType<HumanInventory>();
        foreach (HumanInventory inventory in allInventories)
        {
            if (inventory != null && inventory.photonView != null && inventory.photonView.IsMine)
            {
                _playerInventory = inventory;
                Debug.Log("BuildSystem: Found player inventory via scene search");
                _inventorySearchPerformed = true;
                return true;
            }
        }

        Debug.LogWarning("BuildSystem: Could not find local player inventory");
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
        if (_cachedRadialMenu == null)
        {
            _cachedRadialMenu = FindObjectOfType<RadialMenuController>();
        }

        if (_cachedRadialMenu != null)
        {
            _cachedRadialMenu.InitializeWithBuildables(buildablePrefabs);
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

            if (!scriptActive)
            {
                CleanupPreview();
            }
        }
    }

    void HandleBuildingToggle()
    {
        if (Input.GetKeyDown(buildKey) && !InGameMenu.InMenu() && !ChatManager.IsChatActive())
        {
            // Only search for inventory when they first try to build
            if (_playerInventory == null && !_inventorySearchPerformed)
            {
                if (!FindLocalPlayerInventory())
                {
                    Debug.LogError("BuildSystem: Cannot start building - no player inventory found");
                    return;
                }
            }

            isBuilding = !isBuilding;

            if (!isBuilding)
            {
                CleanupPreview();
            }
            else if (currentPreview == null)
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
        _currentHelper = prefab.GetComponent<BuildableObjectHelper>();

        if (_currentHelper == null || _currentHelper.preview == null)
        {
            Debug.LogError("BuildSystem: Missing BuildableObjectHelper or preview");
            return;
        }

        // Clean up existing preview
        CleanupPreview();

        // Reset all rotation states
        currentRotation = Quaternion.identity;
        surfaceAlignmentRotation = Quaternion.identity;

        // Initialize with forced alignment if enabled
        Quaternion spawnRotation = Quaternion.identity;
        if (_currentHelper.forceUpAlignment)
        {
            spawnRotation = _currentHelper.GetForcedRotation();
            Debug.Log($"Applying forced alignment - Up: {_currentHelper.forcedUpAxis}, Forward: {_currentHelper.forwardAxis}");
        }

        // Create new preview with proper rotation
        currentPreview = Instantiate(_currentHelper.preview, currentPos, spawnRotation);
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("Preview"));

        Debug.Log($"Created preview for {prefab.name} " +
                 $"(Force Up: {_currentHelper.forceUpAlignment}, " +
                 $"Rotation: {spawnRotation.eulerAngles})");
    }

    void UpdatePreview()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 40, buildLayer))
        {
            if (_currentHelper == null) return;

            // Calculate grid-aligned position
            float gridSize = _currentHelper.gridSize;
            currentPos = hit.point + hit.normal * _currentHelper.offset;
            currentPos = new Vector3(
                Mathf.Round(currentPos.x / gridSize) * gridSize,
                Mathf.Round(currentPos.y / gridSize) * gridSize,
                Mathf.Round(currentPos.z / gridSize) * gridSize
            );

            // Calculate surface alignment
            surfaceAlignmentRotation = _currentHelper.snapToSurface ?
                Quaternion.FromToRotation(Vector3.up, hit.normal) :
                Quaternion.identity;

            // Update preview position
            currentPreview.transform.position = currentPos;

            // Apply rotation based on helper settings
            if (_currentHelper.forceUpAlignment)
            {
                // Get the forced rotation from helper
                Quaternion forcedRotation = _currentHelper.GetForcedRotation();

                // Combine rotations:
                currentPreview.transform.rotation = surfaceAlignmentRotation *
                                      forcedRotation *
                                      currentRotation;
            }
            else
            {
                // Standard rotation behavior
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

        // Use the cached list to avoid GC allocations
        _rendererCache.Clear();
        currentPreview.GetComponentsInChildren<Renderer>(true, _rendererCache);

        foreach (Renderer renderer in _rendererCache)
        {
            renderer.material = isValid ? buildableMaterial : notBuildableMaterial;
        }
    }

    bool IsPreviewValid()
    {
        if (currentPreview == null) return false;
        if (_currentHelper == null || _currentHelper.collisionCheckObject == null) return false;

        Vector3 checkPos = currentPreview.transform.position + _currentHelper.collisionCheckObject.transform.localPosition;

        // Use non-allocating version if possible, fallback to regular version
        Collider checkCollider = _currentHelper.collisionCheckObject.GetComponent<Collider>();
        if (checkCollider == null) return false;

        int numColliders = Physics.OverlapBoxNonAlloc(
            checkPos,
            checkCollider.bounds.extents,
            _colliderCache,
            currentPreview.transform.rotation,
            buildLayer | (1 << LayerMask.NameToLayer("Player"))
        );

        for (int i = 0; i < numColliders; i++)
        {
            Collider col = _colliderCache[i];
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
            if (_currentHelper != null)
            {
                currentRotation = _currentHelper.forceUpAlignment ? _currentHelper.GetForcedRotation() : Quaternion.identity;

                if (currentPreview != null)
                {
                    currentPreview.transform.rotation = surfaceAlignmentRotation * currentRotation;
                }
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
        if (_currentHelper == null) return;

        // Get the axis to rotate around from the prefab settings
        Vector3 axis = Vector3.up;
        switch (_currentHelper.rotationAxis)
        {
            case BuildableObjectHelper.RotationAxis.X: axis = Vector3.right; break;
            case BuildableObjectHelper.RotationAxis.Y: axis = Vector3.up; break;
            case BuildableObjectHelper.RotationAxis.Z: axis = Vector3.forward; break;
        }

        // Apply rotation using the prefab's increment
        currentRotation *= Quaternion.AngleAxis(direction * _currentHelper.rotationIncrement, axis);

        if (currentPreview != null)
        {
            if (_currentHelper.forceUpAlignment)
            {
                Quaternion forcedRotation = _currentHelper.GetForcedRotation();
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

        // 2. Search for inventory if we don't have it yet
        if (_playerInventory == null && !_inventorySearchPerformed)
        {
            if (!FindLocalPlayerInventory())
            {
                Debug.LogError("BuildSystem: Cannot build - no player inventory found");
                return;
            }
        }

        // 3. Only allow the LOCAL player to build
        if (!IsLocalPlayer)
        {
            Debug.Log("Not local player - skipping build logic");
            return;
        }

        // 4. Check & deduct resources (local only)
        if (_currentHelper == null) return;

        foreach (InventoryCost cost in _currentHelper.buildCosts)
        {
            if (_playerInventory.GetItemCount(cost.itemName) < cost.amount)
            {
                _playerInventory.ShowNotEnoughMessage(cost.itemName);
                return;
            }
            _playerInventory.SetItemCount(cost.itemName, _playerInventory.GetItemCount(cost.itemName) - cost.amount);
        }

        // 5. Create build data
        BuildableObjectData buildData = new BuildableObjectData
        {
            prefabName = buildablePrefabs[currentBuildableIndex].name,
            position = currentPos,
            rotation = currentPreview.transform.rotation,
            ownerId = PhotonNetwork.LocalPlayer.UserId,
            timestamp = PhotonNetwork.Time
        };

        // 6. Request master client to spawn the object
        if (PhotonNetwork.IsMasterClient)
        {
            // We are master client, spawn directly
            StartCoroutine(InstantiateBuildableForAll(buildData));
        }
        else
        {
            // Request master client to spawn
            photonView.RPC("RequestBuildObject", RpcTarget.MasterClient, buildData);
        }

        // 7. Local effects
        if (_currentHelper.buildParticleEffectPrefab != null)
            SpawnBuildParticles(_currentHelper);

        CleanupPreview();
        CreatePreview();
    }

    [PunRPC]
    private void RequestBuildObject(BuildableObjectData buildData, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Verify the request came from a valid player
        if (info.Sender == null) return;

        Debug.Log($"Master client received build request from {info.Sender.ActorNumber}");
        StartCoroutine(InstantiateBuildableForAll(buildData));
    }

    private IEnumerator InstantiateBuildableForAll(BuildableObjectData buildData)
    {
        string prefabPath = "Buildables/" + buildData.prefabName;

        // Instantiate the object
        GameObject builtObject = PhotonNetwork.InstantiateRoomObject(prefabPath, buildData.position, buildData.rotation);

        if (builtObject != null)
        {
            PhotonView photonView = builtObject.GetComponent<PhotonView>();
            if (photonView != null)
            {
                // Store the PhotonView ID for tracking
                buildData.viewId = photonView.ViewID;

                // Add to our tracking list if not already there
                if (!_buildableObjects.Exists(b => b.viewId == buildData.viewId))
                {
                    _buildableObjects.Add(buildData);
                    _buildableObjectsByViewId[buildData.viewId] = buildData;
                    Debug.Log($"Added buildable to tracking: {buildData.prefabName} (ViewID: {buildData.viewId})");
                }

                // Add build tracking component
                BuildableTracker tracker = builtObject.AddComponent<BuildableTracker>();
                tracker.Initialize(buildData.prefabName, buildData.viewId);

                // Sync the build data to all clients
                photonView.RPC("SyncBuildableData", RpcTarget.OthersBuffered, buildData);
            }
        }

        yield return null;
    }

    [PunRPC]
    private void SyncBuildableData(BuildableObjectData buildData, PhotonMessageInfo info)
    {
        if (!info.Sender.IsMasterClient) return;

        // Only process if this came from master client
        if (!_buildableObjects.Exists(b => b.viewId == buildData.viewId))
        {
            _buildableObjects.Add(buildData);
            _buildableObjectsByViewId[buildData.viewId] = buildData;
            Debug.Log($"Received sync buildable: {buildData.prefabName} from master");
        }
    }

    // JSON Saving/Loading Methods
    public void SaveBuildablesToJson(string filePath = "buildables_save.json")
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only master client can save buildables");
            return;
        }

        BuildableObjectsCollection collection = new BuildableObjectsCollection();
        collection.objects = new List<BuildableObjectData>(_buildableObjects);

        string json = JsonUtility.ToJson(collection, true);
        string fullPath = Path.Combine(Application.persistentDataPath, filePath);
        File.WriteAllText(fullPath, json);

        Debug.Log($"Saved {_buildableObjects.Count} buildables to JSON at: {fullPath}");

        // Show chat message using the static method that exists
        AddChatMessage($"[System] Saved {_buildableObjects.Count} buildables to file");
    }

    public void LoadBuildablesFromJson(string filePath = "buildables_save.json")
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only master client can load buildables");
            return;
        }

        string fullPath = Path.Combine(Application.persistentDataPath, filePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"No save file found at {fullPath}");
            AddChatMessage($"[System] No save file found at {filePath}");
            return;
        }

        string json = File.ReadAllText(fullPath);
        BuildableObjectsCollection collection = JsonUtility.FromJson<BuildableObjectsCollection>(json);

        // Clear existing buildables
        ClearAllBuildables();

        // Load new ones
        foreach (var buildData in collection.objects)
        {
            StartCoroutine(InstantiateBuildableForAll(buildData));
        }

        Debug.Log($"Loaded {collection.objects.Count} buildables from JSON");
        AddChatMessage($"[System] Loaded {collection.objects.Count} buildables from file");
    }

    [PunRPC]
    private void ClearAllBuildablesRPC(PhotonMessageInfo info)
    {
        if (!info.Sender.IsMasterClient) return;
        ClearAllBuildables();
    }

    private void ClearAllBuildables()
    {
        // Find and destroy all buildable objects in scene
        var buildables = FindObjectsOfType<BuildableTracker>();
        foreach (var buildable in buildables)
        {
            if (buildable.photonView != null && buildable.photonView.IsMine)
            {
                PhotonNetwork.Destroy(buildable.gameObject);
            }
        }
        _buildableObjects.Clear();
        _buildableObjectsByViewId.Clear();

        Debug.Log("Cleared all buildables");
    }

    public void ClearAllBuildablesMaster()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        photonView.RPC("ClearAllBuildablesRPC", RpcTarget.AllBuffered);
        AddChatMessage($"[System] Cleared all buildables");
    }

    // Safe method to add chat messages
    private void AddChatMessage(string message)
    {
        // Try multiple ways to send chat messages
        try
        {
            // Method 1: Use the static method that exists in your project
            ChatManager.AddLine(message, ChatTextColor.System);
            return;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to send chat message via ChatManager.AddLine: {e.Message}");
        }

        try
        {
            // Method 2: Find ChatManager in scene and use reflection
            ChatManager chatManager = FindObjectOfType<ChatManager>();
            if (chatManager != null)
            {
                var addLineMethod = chatManager.GetType().GetMethod("AddLine",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static);
                if (addLineMethod != null)
                {
                    addLineMethod.Invoke(null, new object[] { message, ChatTextColor.System });
                    return;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to send chat message via reflection: {e.Message}");
        }

        // Method 3: Just use debug log as fallback
        Debug.Log($"CHAT: {message}");
    }

    // Helper method to get current buildables count
    public int GetBuildableCount()
    {
        return _buildableObjects.Count;
    }

    // Get all buildable objects data
    public List<BuildableObjectData> GetAllBuildableData()
    {
        return new List<BuildableObjectData>(_buildableObjects);
    }

    // Remove a specific buildable by view ID
    public void RemoveBuildable(int viewId)
    {
        BuildableObjectData data = _buildableObjects.Find(b => b.viewId == viewId);
        if (data != null)
        {
            _buildableObjects.Remove(data);
            _buildableObjectsByViewId.Remove(viewId);
            Debug.Log($"Removed buildable from tracking: {data.prefabName} (ViewID: {viewId})");
        }
    }

    private bool IsLocalPlayer => PhotonNetwork.LocalPlayer != null &&
                             _playerInventory != null &&
                             _playerInventory.photonView != null &&
                             _playerInventory.photonView.IsMine;

    private void SpawnBuildParticles(BuildableObjectHelper helper)
    {
        if (helper.buildParticleEffectPrefab == null) return;

        string particlePrefabName = "HParticles/" + helper.buildParticleEffectPrefab.name;
        Vector3 spawnPos = currentPos + currentPreview.transform.TransformDirection(helper.particleEffectOffset);
        Quaternion spawnRot = helper.particleUsePreviewRotation ? currentPreview.transform.rotation : Quaternion.identity;

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

        if (spawnedParticles == null) return;

        // Parenting logic
        if (helper.particleParentToBuilding)
        {
            // Stop any existing coroutine
            if (_parentParticlesCoroutine != null)
            {
                StopCoroutine(_parentParticlesCoroutine);
            }
            _parentParticlesCoroutine = StartCoroutine(ParentParticlesAfterBuild(spawnedParticles, currentPos));
        }
    }

    private IEnumerator ParentParticlesAfterBuild(GameObject particles, Vector3 buildPosition)
    {
        // Wait one frame to allow building to spawn
        yield return null;

        // Check if objects are still valid
        if (this == null || particles == null || !gameObject.activeInHierarchy)
            yield break;

        // Find the nearest building object at our build position
        int numColliders = Physics.OverlapSphereNonAlloc(buildPosition, 0.5f, _colliderCache);
        for (int i = 0; i < numColliders; i++)
        {
            Collider col = _colliderCache[i];
            if (col != null && col.gameObject != currentPreview && col.CompareTag("Buildable"))
            {
                particles.transform.SetParent(col.transform);
                break;
            }
        }

        _parentParticlesCoroutine = null;
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
        _currentHelper = prefab.GetComponent<BuildableObjectHelper>();

        if (isBuilding)
        {
            CleanupPreview();
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

    public void SetPlayerInventory(HumanInventory inventory)
    {
        _playerInventory = inventory;
        _inventorySearchPerformed = true; // Mark as found
        Debug.Log($"BuildSystem: Player inventory set externally to {inventory?.gameObject?.name}");
    }

    private void CleanupPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    private void OnDisable()
    {
        // Clean up all coroutines
        if (_parentParticlesCoroutine != null)
        {
            StopCoroutine(_parentParticlesCoroutine);
            _parentParticlesCoroutine = null;
        }

        StopAllCoroutines();
        CleanupPreview();

        // Clear cached references
        _currentHelper = null;
        _cachedRadialMenu = null;
    }

    private void OnDestroy()
    {
        // Additional cleanup
        CleanupPreview();
        buildablePrefabs.Clear();

        // Ensure all coroutines are stopped
        if (_parentParticlesCoroutine != null)
        {
            StopCoroutine(_parentParticlesCoroutine);
            _parentParticlesCoroutine = null;
        }

        // Unregister from Photon callbacks
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}