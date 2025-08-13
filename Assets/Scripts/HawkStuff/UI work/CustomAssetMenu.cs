using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using ApplicationManagers;
using UI;
using GameManagers;
using Settings;

public class CustomAssetMenu : MonoBehaviourPun
{
    private bool menuOpen = false;

    private string bundleName = "";
    private string prefabName = "";
    private string posX = "0", posY = "0", posZ = "0";
    private string rotX = "0", rotY = "0", rotZ = "0";
    private string layer = "23";

    private List<GameObject> spawnedAssets = new List<GameObject>();
    private GameObject selectedObject = null;
    private string moveX = "0", moveY = "0", moveZ = "0";

    private string moveRotX = "0", moveRotY = "0", moveRotZ = "0";

    private List<string> buildablePrefabNames = new List<string>();
    private int selectedBuildableIndex = 0;
    private Vector2 buildableScroll = Vector2.zero;
    private bool buildablesLoaded = false;



    private void Update()
    {
        if (SettingsManager.InputSettings.General.CustomAssetMenu.GetKeyDown() && PhotonNetwork.IsMasterClient)
        {
            menuOpen = !menuOpen;
            Cursor.visible = menuOpen;
            Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void OnGUI()
    {
        if (!menuOpen) return;

        GUI.Box(new Rect(20, 20, 320, 340), "Custom Asset Spawner");

        GUI.Label(new Rect(30, 50, 60, 20), "Bundle:");
        bundleName = GUI.TextField(new Rect(90, 50, 230, 20), bundleName);

        GUI.Label(new Rect(30, 75, 60, 20), "Prefab:");
        prefabName = GUI.TextField(new Rect(90, 75, 230, 20), prefabName);

        GUI.Label(new Rect(30, 100, 60, 20), "Position:");
        posX = GUI.TextField(new Rect(90, 100, 60, 20), posX);
        posY = GUI.TextField(new Rect(155, 100, 60, 20), posY);
        posZ = GUI.TextField(new Rect(220, 100, 60, 20), posZ);

        GUI.Label(new Rect(30, 125, 60, 20), "Rotation:");
        rotX = GUI.TextField(new Rect(90, 125, 60, 20), rotX);
        rotY = GUI.TextField(new Rect(155, 125, 60, 20), rotY);
        rotZ = GUI.TextField(new Rect(220, 125, 60, 20), rotZ);

        GUI.Label(new Rect(30, 150, 60, 20), "Layer:");
        layer = GUI.TextField(new Rect(90, 150, 230, 20), layer);

        if (GUI.Button(new Rect(90, 180, 140, 30), "Spawn"))
        {
            if (float.TryParse(posX, out float x) &&
                float.TryParse(posY, out float y) &&
                float.TryParse(posZ, out float z) &&
                float.TryParse(rotX, out float rx) &&
                float.TryParse(rotY, out float ry) &&
                float.TryParse(rotZ, out float rz) &&
                int.TryParse(layer, out int parsedLayer))

            {
                StartCoroutine(SpawnAsset(bundleName, prefabName, new Vector3(x, y, z), new Vector3(rx, ry, rz), parsedLayer));
            }
        }

        GUI.Box(new Rect(360, 20, 300, 300), "Spawned Assets");

        for (int i = 0; i < spawnedAssets.Count; i++)
        {
            var obj = spawnedAssets[i];
            if (obj == null) continue;

            GUI.Label(new Rect(370, 50 + 25 * i, 150, 20), obj.name);

            if (GUI.Button(new Rect(520, 50 + 25 * i, 60, 20), "Move"))
            {
                selectedObject = obj;
                var pos = obj.transform.position;
                moveX = pos.x.ToString();
                moveY = pos.y.ToString();
                moveZ = pos.z.ToString();
                var rot = obj.transform.rotation.eulerAngles;
                moveRotX = rot.x.ToString();
                moveRotY = rot.y.ToString();
                moveRotZ = rot.z.ToString();

            }

            if (GUI.Button(new Rect(585, 50 + 25 * i, 60, 20), "Delete"))
            {
                obj.GetComponent<CustomAssetHelper>()?.Delete();
            }
        }

        if (selectedObject != null)
        {
            GUI.Box(new Rect(20, 380, 320, 130), "Move Asset");

            GUI.Label(new Rect(30, 410, 30, 20), "X:");
            moveX = GUI.TextField(new Rect(60, 410, 60, 20), moveX);
            GUI.Label(new Rect(130, 410, 30, 20), "Y:");
            moveY = GUI.TextField(new Rect(160, 410, 60, 20), moveY);
            GUI.Label(new Rect(230, 410, 30, 20), "Z:");
            moveZ = GUI.TextField(new Rect(260, 410, 60, 20), moveZ);
            GUI.Label(new Rect(30, 470, 30, 20), "Rot X:");
            moveRotX = GUI.TextField(new Rect(60, 470, 60, 20), moveRotX);
            GUI.Label(new Rect(130, 470, 30, 20), "Y:");
            moveRotY = GUI.TextField(new Rect(160, 470, 60, 20), moveRotY);
            GUI.Label(new Rect(230, 470, 30, 20), "Z:");
            moveRotZ = GUI.TextField(new Rect(260, 470, 60, 20), moveRotZ);


            if (GUI.Button(new Rect(60, 440, 100, 25), "Apply"))
            {
                if (float.TryParse(moveX, out float mx) &&
                float.TryParse(moveY, out float my) &&
                float.TryParse(moveZ, out float mz) &&
                float.TryParse(moveRotX, out float rx) &&
                float.TryParse(moveRotY, out float ry) &&
                float.TryParse(moveRotZ, out float rz))
                {
                    var helper = selectedObject.GetComponent<CustomAssetHelper>();
                    if (helper != null)
                    {
                        helper.Move(new Vector3(mx, my, mz), new Vector3(rx, ry, rz));
                    }
                    selectedObject = null;
                }

            }

            if (GUI.Button(new Rect(170, 440, 100, 25), "Cancel"))
            {
                selectedObject = null;
            }
        }
        if (!buildablesLoaded)
            LoadBuildablePrefabs();

        GUI.Box(new Rect(680, 20, 300, 300), "Resources/Buildables");

        GUI.Label(new Rect(690, 50, 200, 20), "Select Buildable:");

        buildableScroll = GUI.BeginScrollView(
            new Rect(690, 75, 280, 160),
            buildableScroll,
            new Rect(0, 0, 260, buildablePrefabNames.Count * 25)
        );

        for (int i = 0; i < buildablePrefabNames.Count; i++)
        {
            if (GUI.Button(new Rect(0, i * 25, 260, 25), buildablePrefabNames[i]))
            {
                selectedBuildableIndex = i;
            }
        }

        GUI.EndScrollView();

        GUI.Label(new Rect(690, 240, 200, 20), "Selected: " + buildablePrefabNames[selectedBuildableIndex]);

        if (GUI.Button(new Rect(690, 270, 140, 30), "Spawn Buildable"))
        {
            if (float.TryParse(posX, out float x) &&
                float.TryParse(posY, out float y) &&
                float.TryParse(posZ, out float z) &&
                float.TryParse(rotX, out float rx) &&
                float.TryParse(rotY, out float ry) &&
                float.TryParse(rotZ, out float rz) &&
                int.TryParse(layer, out int parsedLayer))
            {
                string buildableName = buildablePrefabNames[selectedBuildableIndex];
                SpawnBuildable(buildableName, new Vector3(x, y, z), new Vector3(rx, ry, rz), parsedLayer);
            }
        }



    }

    private void LoadBuildablePrefabs()
    {
        buildablePrefabNames.Clear();
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("Buildables");
        foreach (GameObject prefab in allPrefabs)
        {
            // Filter out subfoldered prefabs
            string fullPath = "Buildables/" + prefab.name;
            GameObject check = Resources.Load<GameObject>(fullPath);
            if (check != null)
            {
                buildablePrefabNames.Add(prefab.name);
            }
        }

        buildablesLoaded = true;
    }






    private IEnumerator SpawnAsset(string bundle, string prefab, Vector3 position, Vector3 rotation, int layer)
    {
        if (!AssetBundleManager.LoadedBundle(bundle))
            yield return AssetBundleManager.LoadBundle(bundle, "", true);

        GameObject prefabObj = AssetBundleManager.LoadAsset(bundle, prefab) as GameObject;
        if (prefabObj == null)
        {
            ChatManager.AddLine($"[MC] Prefab not found: {prefab}", ChatTextColor.System);
            yield break;
        }

        GameObject go = Instantiate(prefabObj, position, Quaternion.Euler(rotation));
        PhotonView view = go.AddComponent<PhotonView>();

        view.ViewID = PhotonNetwork.AllocateViewID(true);
        SetLayerRecursively(go, layer);
        go.AddComponent<CustomAssetHelper>();

        spawnedAssets.Add(go);
        PhotonView rpcView = RPCManager.PhotonView;
        rpcView.RPC("RPC_SpawnRemote", RpcTarget.Others, bundle, prefab, position, rotation, layer, view.ViewID);
    }

    [PunRPC]
    private void RPC_SpawnRemote(string bundle, string prefab, Vector3 position, Vector3 rotation, int layer, int viewID)
    {
        StartCoroutine(RemoteSpawn(bundle, prefab, position, rotation, layer, viewID));
    }

    private IEnumerator RemoteSpawn(string bundle, string prefab, Vector3 position, Vector3 rotation, int layer, int viewID)
    {
        if (!AssetBundleManager.LoadedBundle(bundle))
            yield return AssetBundleManager.LoadBundle(bundle, "", true);

        GameObject prefabObj = AssetBundleManager.LoadAsset(bundle, prefab) as GameObject;
        if (prefabObj == null) yield break;

        GameObject go = Instantiate(prefabObj, position, Quaternion.Euler(rotation));
        PhotonView view = go.AddComponent<PhotonView>();
        view.ViewID = viewID;
        SetLayerRecursively(go, layer);
        go.AddComponent<CustomAssetHelper>();
    }

    private void SpawnBuildable(string prefabName, Vector3 position, Vector3 rotation, int layer)
    {
        GameObject go = PhotonNetwork.Instantiate("Buildables/" + prefabName, position, Quaternion.Euler(rotation), 0);
        SetLayerRecursively(go, layer);
        go.AddComponent<CustomAssetHelper>();
        spawnedAssets.Add(go);
    }





    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
