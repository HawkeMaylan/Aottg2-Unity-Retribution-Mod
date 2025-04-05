using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicBuildSystem : MonoBehaviour
{
    public List<buildObjects> objects = new List<buildObjects>();
    public buildObjects currentobject;
    private Vector3 currentpos;
    private Vector3 currentrot;
    public Transform currentpreview;
    public Transform cam;
    public RaycastHit hit;
    public LayerMask layer;

    public float offset = 1.0f;
    public float gridSize = 1.0f;

    public bool IsBuilding;
    private bool scriptActive = false; // Tracks whether the script is active

    // Start is called before the first frame update
    void Start()
    {
        changeCurrentBuilding(0);
    }

    void Update()
    {
        // Toggle the activation of the script with the backtick key (`).
        if (Input.GetKeyDown(KeyCode.BackQuote)) // ` key
        {
            scriptActive = !scriptActive;
            IsBuilding = false; // Ensure IsBuilding is reset when script is deactivated
            ToggleCursor(!scriptActive); // Toggle cursor visibility

            // Destroy the preview if the script is deactivated
            if (!scriptActive && currentpreview != null)
            {
                Destroy(currentpreview.gameObject);
            }
        }

        // If the script is not active, do nothing
        if (!scriptActive) return;

        // Toggle IsBuilding with the K key
        if (Input.GetKeyDown(KeyCode.K))
        {
            IsBuilding = !IsBuilding;

            // Destroy the preview if IsBuilding is set to false
            if (!IsBuilding && currentpreview != null)
            {
                Destroy(currentpreview.gameObject);
            }

            // Recreate the preview if IsBuilding is set to true
            if (IsBuilding && currentpreview == null)
            {
                GameObject curprev = Instantiate(currentobject.preview, currentpos, Quaternion.Euler(currentrot));
                currentpreview = curprev.transform;
            }
        }

        // Building system logic runs only if IsBuilding is true
        if (IsBuilding)
        {
            startPreview();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                Build();

            if (Input.GetKeyDown("0") || Input.GetKeyDown("1") || Input.GetKeyDown("2"))
                switchCurrentBuilding();
        }
    }

    public void switchCurrentBuilding()
    {
        for (int i = 0; i < 3; i++)
        {
            if (Input.GetKeyDown("" + i))
                changeCurrentBuilding(i);
        }
    }

    public void changeCurrentBuilding(int cur)
    {
        if (cur < 0 || cur >= objects.Count)
        {
            Debug.LogError($"Invalid index {cur}. Ensure the index is within the bounds of the objects list.");
            return;
        }

        currentobject = objects[cur];
        if (currentpreview != null)
            Destroy(currentpreview.gameObject);

        GameObject curprev = Instantiate(currentobject.preview, currentpos, Quaternion.Euler(currentrot));
        if (curprev == null)
        {
            Debug.LogError("Failed to instantiate the preview object.");
            return;
        }

        currentpreview = curprev.transform;
    }

    public void startPreview()
    {
        if (Physics.Raycast(cam.position, cam.forward, out hit, 40, layer))
        {
            if (hit.transform != this.transform)
                showPreview(hit);
        }
    }

    public void showPreview(RaycastHit hit2)
    {
        currentpos = hit2.point;
        currentpos -= Vector3.one * offset;
        currentpos /= gridSize;
        currentpos = new Vector3(Mathf.Round(currentpos.x), Mathf.Round(currentpos.y), Mathf.Round(currentpos.z));
        currentpos *= gridSize;
        currentpos += Vector3.one * offset;
        currentpreview.position = currentpos;

        if (Input.GetKeyDown(KeyCode.RightArrow))
            currentrot += new Vector3(0, 45, 0);
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            currentrot -= new Vector3(0, 45, 0);

        currentpreview.localEulerAngles = currentrot;
    }

    public void Build()
    {
        if (currentpreview == null)
        {
            Debug.LogError("currentpreview is null. Cannot build.");
            return;
        }

        PreviewObject PO = currentpreview.GetComponent<PreviewObject>();
        if (PO == null)
        {
            Debug.LogError("PreviewObject component is missing on currentpreview.");
            return;
        }

        if (PO.IsBuildable)
        {
            if (currentobject == null || currentobject.prefab == null)
            {
                Debug.LogError("currentobject or its prefab is not assigned.");
                return;
            }

            Instantiate(currentobject.prefab, currentpos, Quaternion.Euler(currentrot));
        }
        else
        {
            Debug.LogWarning("Cannot build at the current position. Object is not buildable.");
        }
    }

    void ToggleCursor(bool enable)
    {
        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
    }
}

[System.Serializable]
public class buildObjects
{
    public string name;
    public GameObject prefab;
    public GameObject preview;
    public int resources;
}
