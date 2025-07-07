using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class BuildSystem : MonoBehaviourPunCallbacks
{
    public Transform cam;
    public LayerMask layer;

    private bool isBuilding = false;
    private bool scriptActive = false;

    private GameObject currentPreview;
    private Vector3 currentPos;
    private Vector3 currentRot;

    private List<GameObject> buildablePrefabs = new List<GameObject>();
    private int currentBuildableIndex = 0;

    public Material buildableMaterial;
    public Material notBuildableMaterial;

    void Start()
    {
        LoadBuildablePrefabs();
        // Do not create the preview at start. Wait for the player to press the build key.
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            scriptActive = !scriptActive;
            isBuilding = false;
            ToggleCursor(!scriptActive);

            if (!scriptActive && currentPreview != null)
            {
                Destroy(currentPreview);
            }
        }

        if (!scriptActive) return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            isBuilding = !isBuilding;

            if (!isBuilding && currentPreview != null)
            {
                Destroy(currentPreview);
            }

            if (isBuilding && currentPreview == null)
            {
                CreatePreview();
            }
        }

        if (isBuilding)
        {
            UpdatePreview();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                Build();

            if (Input.GetKeyDown(KeyCode.B))
                SwitchCurrentBuilding();
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
            }
        }
    }

    void ChangeCurrentBuilding(int index)
    {
        if (index < 0 || index >= buildablePrefabs.Count)
        {
            Debug.LogError($"Invalid index {index}. Ensure the index is within the bounds of the buildablePrefabs list.");
            return;
        }

        currentBuildableIndex = index;

        if (currentPreview != null)
            Destroy(currentPreview);

        CreatePreview();
    }

    void CreatePreview()
    {
        GameObject prefab = buildablePrefabs[currentBuildableIndex];
        BuildableObjectHelper helper = prefab.GetComponent<BuildableObjectHelper>();

        if (helper == null || helper.preview == null)
        {
            Debug.LogError("BuildableObjectHelper or its preview is not assigned.");
            return;
        }

        // Instantiate the preview object
        currentPreview = Instantiate(helper.preview, currentPos, Quaternion.Euler(currentRot));

        // Ensure the preview object and all its children are on the correct layer
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("Preview"));

        // Verify that the layer was set correctly
        if (currentPreview.layer != LayerMask.NameToLayer("Preview"))
        {
            Debug.LogError("Failed to set the layer of the preview object.");
        }
    }

    // Helper method to set the layer of an object and all its children
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    void UpdatePreview()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 40, layer))
        {
            BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
            if (helper == null)
            {
                Debug.LogError("BuildableObjectHelper is not assigned.");
                return;
            }

            // Use the grid size and offset from the helper script
            float gridSize = helper.gridSize;
            float offset = helper.offset;

            // Align the preview object to the surface normal
            currentPos = hit.point + hit.normal * offset;

            // Snap to grid
            currentPos /= gridSize;
            currentPos = new Vector3(Mathf.Round(currentPos.x), Mathf.Round(currentPos.y), Mathf.Round(currentPos.z));
            currentPos *= gridSize;

            currentPreview.transform.position = currentPos;

            // Rotate the preview object to align with the surface normal
            currentPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(currentRot);

            if (Input.GetKeyDown(KeyCode.RightArrow))
                currentRot += new Vector3(0, 45, 0);
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                currentRot -= new Vector3(0, 45, 0);

            UpdatePreviewMaterials();
        }
    }

    void UpdatePreviewMaterials()
    {
        bool isBuildable = IsPreviewBuildable();

        foreach (Transform child in currentPreview.transform)
        {
            child.GetComponent<Renderer>().material = isBuildable ? buildableMaterial : notBuildableMaterial;
        }
    }

    bool IsPreviewBuildable()
    {
        if (currentPreview == null) return false;

        BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
        if (helper == null || helper.collisionCheckObject == null)
        {
            Debug.LogError("BuildableObjectHelper or its collisionCheckObject is not assigned.");
            return false;
        }

        // Use the collisionCheckObject's bounds for the overlap check
        Bounds checkBounds = helper.collisionCheckObject.GetComponent<Collider>().bounds;
        Vector3 checkPosition = currentPreview.transform.position + helper.collisionCheckObject.transform.localPosition;

        // Ignore collisions with the "Player" layer
        int layerMask = layer | (1 << LayerMask.NameToLayer("Player"));
        Collider[] colliders = Physics.OverlapBox(checkPosition, checkBounds.extents, currentPreview.transform.rotation, layerMask);
        return colliders.Length == 0;
    }

    void Build()
    {
        if (currentPreview == null)
        {
            Debug.LogError("currentPreview is null. Cannot build.");
            return;
        }

        if (IsPreviewBuildable())
        {
            GameObject prefab = buildablePrefabs[currentBuildableIndex];
            string prefabName = prefab.name;
            string photonPath = "Buildables/" + prefabName;

            PhotonNetwork.Instantiate(photonPath, currentPos, currentPreview.transform.rotation);

            Destroy(currentPreview);

            // Immediately create a new preview so player can keep building
            CreatePreview();
        }
        else
        {
            Debug.LogWarning("Cannot build at the current position. Object is not buildable.");
        }
    }

    void SwitchCurrentBuilding()
    {
        currentBuildableIndex = (currentBuildableIndex + 1) % buildablePrefabs.Count;
        ChangeCurrentBuilding(currentBuildableIndex);
    }

    void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
    }
}