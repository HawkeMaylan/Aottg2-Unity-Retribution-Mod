using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using Characters;
using System.Collections;

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

    // Building state
    private bool isBuilding = false;
    private bool scriptActive = false;
    private GameObject currentPreview;
    private Vector3 currentPos;
    private Vector3 currentRot;
    private List<GameObject> buildablePrefabs = new List<GameObject>();
    private int currentBuildableIndex = 0;
    private HumanInventory _playerInventory;
    private bool _inventorySearchPerformed = false;
    private Dictionary<string, int> _pendingRefunds = new Dictionary<string, int>();

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
        if (Input.GetKeyDown(buildKey))
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

        currentPreview = Instantiate(helper.preview, currentPos, Quaternion.Euler(currentRot));
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("Preview"));
        Debug.Log($"BuildSystem: Created preview for {prefab.name}");
    }

    void UpdatePreview()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 40, buildLayer))
        {
            BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
            if (helper == null) return;

            float gridSize = helper.gridSize;
            currentPos = hit.point + hit.normal * helper.offset;
            currentPos = new Vector3(
                Mathf.Round(currentPos.x / gridSize) * gridSize,
                Mathf.Round(currentPos.y / gridSize) * gridSize,
                Mathf.Round(currentPos.z / gridSize) * gridSize
            );

            currentPreview.transform.position = currentPos;
            currentPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(currentRot);

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
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentRot += new Vector3(0, 45, 0);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentRot -= new Vector3(0, 45, 0);

        if (Input.GetKeyDown(placeKey))
        {
            Build();
        }
    }

    void Build()
    {
        // Validate build position
        if (currentPreview == null || !IsPreviewValid())
        {
            Debug.Log("Cannot build - invalid position");
            return;
        }

        // Verify player inventory
        if (_playerInventory == null && !FindLocalPlayerInventory())
        {
            Debug.LogError("Player inventory not found!");
            return;
        }

        // Get buildable helper
        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        if (helper == null) return;

        // Check resource costs
        bool canAfford = true;
        foreach (InventoryCost cost in helper.buildCosts)
        {
            int currentAmount = _playerInventory.GetItemCount(cost.itemName);
            if (currentAmount < cost.amount)
            {
                _playerInventory.ShowNotEnoughMessage(cost.itemName);
                Debug.Log($"Need {cost.amount} {cost.itemName}, only have {currentAmount}");
                canAfford = false;
            }
        }

        if (!canAfford)
        {
            return;
        }

        // Deduct resources
        foreach (InventoryCost cost in helper.buildCosts)
        {
            _playerInventory.SetItemCount(cost.itemName,
                _playerInventory.GetItemCount(cost.itemName) - cost.amount);
        }

        // Place the building
        GameObject prefab = buildablePrefabs[currentBuildableIndex];
        PhotonNetwork.Instantiate("Buildables/" + prefab.name, currentPos, currentPreview.transform.rotation);

        // Spawn networked particle effect if specified
        if (helper.buildParticleEffectPrefab != null)
        {
            SpawnBuildParticles(helper);
        }

        // Reset preview
        Destroy(currentPreview);
        CreatePreview();
    }

    private void SpawnBuildParticles(BuildableObjectHelper helper)
    {
        // Calculate final particle position with offset
        Vector3 particlePosition = currentPos +
            currentPreview.transform.TransformDirection(helper.particleEffectOffset);

        // Get rotation (either from preview or use identity)
        Quaternion particleRotation = helper.particleUsePreviewRotation ?
            currentPreview.transform.rotation : Quaternion.identity;

        // Get the resource path for Photon
        string resourcePath = GetPrefabResourcePath(helper.buildParticleEffectPrefab);
        if (string.IsNullOrEmpty(resourcePath))
        {
            Debug.LogError($"Particle prefab {helper.buildParticleEffectPrefab.name} is not in a Resources folder!");
            return;
        }

        // Instantiate networked particle effect
        GameObject particles = PhotonNetwork.Instantiate(resourcePath, particlePosition, particleRotation);

        // Optional: Parent to the built object if needed
        if (helper.particleParentToBuilding)
        {
            // Need to find the newly built object since PhotonNetwork.Instantiate is async
            StartCoroutine(ParentParticlesAfterBuild(particles, currentPos));
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