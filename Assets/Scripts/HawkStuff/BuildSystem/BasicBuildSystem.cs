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

        Debug.LogWarning("BuildSystem: Player inventory not found in this attempt");
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
        if (currentPreview == null || !IsPreviewValid())
        {
            Debug.Log("BuildSystem: Cannot build - invalid position or no preview");
            return;
        }

        // Double-check inventory reference
        if (_playerInventory == null)
        {
            FindLocalPlayerInventory();
            if (_playerInventory == null)
            {
                Debug.LogError("BuildSystem: Cannot build - player inventory not found!");
                return;
            }
        }

        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();

        // Check if there are any costs defined
        if (helper.buildCosts != null && helper.buildCosts.Length > 0)
        {
            if (!CanAffordBuild())
            {
                // The "Not Enough X" message will be shown by the inventory system here
                return;
            }

            // Deduct costs only after all checks pass
            foreach (InventoryCost cost in helper.buildCosts)
            {
                _playerInventory.SetItemCount(cost.itemName,
                    _playerInventory.GetItemCount(cost.itemName) - cost.amount);
            }
        }

        // Only place if all checks pass
        GameObject prefab = buildablePrefabs[currentBuildableIndex];
        PhotonNetwork.Instantiate("Buildables/" + prefab.name, currentPos, currentPreview.transform.rotation);
        Debug.Log($"BuildSystem: Built {prefab.name} at {currentPos}");

        Destroy(currentPreview);
        CreatePreview();
    }

    bool CanAffordBuild()
    {
        if (_playerInventory == null)
        {
            Debug.LogError("BuildSystem: Player inventory not found");
            return false;
        }

        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        foreach (InventoryCost cost in helper.buildCosts)
        {
            if (_playerInventory.GetItemCount(cost.itemName) < cost.amount)
            {
                // This will trigger the "Not Enough X" message in the inventory system
                _playerInventory.SetItemCount(cost.itemName, -1);
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