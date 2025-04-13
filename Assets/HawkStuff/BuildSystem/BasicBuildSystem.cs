using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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
    private bool scriptActive = false;

    void Start()
    {
        changeCurrentBuilding(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            scriptActive = !scriptActive;
            IsBuilding = false;
            ToggleCursor(!scriptActive);

            if (!scriptActive && currentpreview != null)
            {
                Destroy(currentpreview.gameObject);
            }
        }

        if (!scriptActive) return;

        if (Input.GetKeyDown(KeyCode.K))
        {
            IsBuilding = !IsBuilding;

            if (!IsBuilding && currentpreview != null)
            {
                Destroy(currentpreview.gameObject);
            }

            if (IsBuilding && currentpreview == null)
            {
                GameObject curprev = Instantiate(currentobject.preview, currentpos, Quaternion.Euler(currentrot));
                currentpreview = curprev.transform;
            }
        }

        if (IsBuilding)
        {
            startPreview();

            if (Input.GetKeyDown(KeyCode.UpArrow))
                Build();

            if (Input.GetKeyDown(KeyCode.B))
                switchCurrentBuilding();
        }
    }

    public void switchCurrentBuilding()
    {
        int nextIndex = objects.IndexOf(currentobject) + 1;
        if (nextIndex >= objects.Count)
            nextIndex = 0;

        changeCurrentBuilding(nextIndex);
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
        if (currentpreview == null) return;

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

            string prefabName = currentobject.prefab.name;
            string photonPath = "Buildables/" + prefabName;

            PhotonNetwork.Instantiate(photonPath, currentpos, Quaternion.Euler(currentrot));

            Destroy(currentpreview.gameObject);

            // Immediately create a new preview so player can keep building
            GameObject curprev = Instantiate(currentobject.preview, currentpos, Quaternion.Euler(currentrot));
            currentpreview = curprev.transform;
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
