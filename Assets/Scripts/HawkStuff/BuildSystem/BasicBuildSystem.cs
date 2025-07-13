using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

    void Start()
    {
        LoadBuildablePrefabs();
        InitializeRadialMenu();
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
            }
        }

        if (buildablePrefabs.Count == 0)
        {
            Debug.LogWarning("No buildable prefabs found in Resources/Buildables");
        }
    }

    void InitializeRadialMenu()
    {
        RadialMenuController radialMenu = FindObjectOfType<RadialMenuController>();
        if (radialMenu != null)
        {
            radialMenu.InitializeWithBuildables(buildablePrefabs);
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
            Debug.LogError("Missing BuildableObjectHelper or preview");
            return;
        }

        currentPreview = Instantiate(helper.preview, currentPos, Quaternion.Euler(currentRot));
        SetLayerRecursively(currentPreview, LayerMask.NameToLayer("Preview"));
    }

    void UpdatePreview()
    {
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 40, buildLayer))
        {
            BuildableObjectHelper helper = buildablePrefabs[currentBuildableIndex].GetComponent<BuildableObjectHelper>();
            if (helper == null) return;

            // Position with grid snapping
            float gridSize = helper.gridSize;
            currentPos = hit.point + hit.normal * helper.offset;
            currentPos = new Vector3(
                Mathf.Round(currentPos.x / gridSize) * gridSize,
                Mathf.Round(currentPos.y / gridSize) * gridSize,
                Mathf.Round(currentPos.z / gridSize) * gridSize
            );

            // Rotation
            currentPreview.transform.position = currentPos;
            currentPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(currentRot);

            UpdatePreviewMaterials();
        }
    }

    void UpdatePreviewMaterials()
    {
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
        // Rotation
        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentRot += new Vector3(0, 45, 0);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentRot -= new Vector3(0, 45, 0);

        // Placement
        if (Input.GetKeyDown(placeKey))
        {
            Build();
        }
    }

    void Build()
    {
        if (currentPreview == null || !IsPreviewValid()) return;

        GameObject prefab = buildablePrefabs[currentBuildableIndex];
        PhotonNetwork.Instantiate("Buildables/" + prefab.name, currentPos, currentPreview.transform.rotation);

        Destroy(currentPreview);
        CreatePreview(); // Create new preview for continuous building
    }

    public void HandleBuildableSelection(GameObject prefab)
    {
        int index = buildablePrefabs.IndexOf(prefab);
        if (index == -1)
        {
            Debug.LogError($"Prefab {prefab.name} not in buildable list");
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