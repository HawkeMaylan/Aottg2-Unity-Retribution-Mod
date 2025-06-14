
using UnityEngine;
using SimpleJSONFixed;
using System;
using System.Collections.Generic;

[ExecuteInEditMode]
public class NpcHumanSetup : MonoBehaviour
{
    [Header("Editor Tools")]
    public bool randomizeOnStart = false;

    private JSONNode costumeInfo;
    private JSONNode hairInfo;
    private Transform headBone, chestBone, legBone;

    private GameObject currentHair, currentBody, currentLegs, currentHead;

    private void Start()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && randomizeOnStart)
        {
            RandomizeAndSetup();
        }
#endif
    }

    [ContextMenu("Randomize Appearance")]
    public void RandomizeAndSetup()
    {
        LoadCostumeInfo();
        FindBones();

        if (costumeInfo == null || hairInfo == null)
        {
            Debug.LogWarning("Costume JSON not loaded.");
            return;
        }

        ClearOldParts();

        System.Random rand = new System.Random();

        // Random sex
        bool male = rand.Next(0, 2) == 0;
        var costumeArray = costumeInfo[male ? "Male" : "Female"].AsArray;
        var hairArray = hairInfo[male ? "Male" : "Female"].AsArray;

        // Select costume and hair index
        int costumeIndex = rand.Next(0, costumeArray.Count);
        int hairIndex = rand.Next(0, hairArray.Count);

        // Apply body
        currentBody = transform.Find("character_chest")?.gameObject;
        if (currentBody != null)
        {
            var mat = CreateColoredMaterial();
            var renderer = currentBody.GetComponent<Renderer>();
            if (renderer != null) renderer.material = mat;
        }

        // Apply legs
        currentLegs = transform.Find("character_leg")?.gameObject;
        if (currentLegs != null)
        {
            var mat = CreateColoredMaterial();
            var renderer = currentLegs.GetComponent<Renderer>();
            if (renderer != null) renderer.material = mat;
        }

        // Apply head
        currentHead = transform.Find("char_head")?.gameObject;
        if (currentHead != null)
        {
            var mat = CreateColoredMaterial();
            var renderer = currentHead.GetComponent<Renderer>();
            if (renderer != null) renderer.material = mat;
        }

        // Spawn hair
        var hairJson = hairArray[hairIndex];
        string hairMesh = hairJson["Texture"];
        if (!string.IsNullOrEmpty(hairMesh))
        {
            GameObject hairPrefab = Resources.Load<GameObject>("Characters/" + hairMesh);
            if (hairPrefab != null && headBone != null)
            {
                currentHair = Instantiate(hairPrefab);
                currentHair.transform.SetParent(headBone, false);
                currentHair.transform.localPosition = Vector3.zero;
                currentHair.transform.localRotation = Quaternion.identity;

                var mat = CreateColoredMaterial();
                Renderer r = currentHair.GetComponentInChildren<Renderer>();
                if (r != null)
                    r.material = mat;
            }
            else
            {
                Debug.LogWarning("Hair prefab not found: " + hairMesh);
            }
        }
    }

    private void LoadCostumeInfo()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>("Data/Info/CostumeInfo");

        if (jsonAsset != null)
        {
            var root = JSON.Parse(jsonAsset.text);
            costumeInfo = root["Costume"];
            hairInfo = root["Hair"];
        }
        else
        {
            Debug.LogError("CostumeInfo.json not found in Resources/Info!");
        }
    }

    private void FindBones()
    {
        headBone = transform.Find("Armature/Core/Controller_Body/hip/spine/chest/neck/head");
        chestBone = transform.Find("Armature/Core/Controller_Body/hip/spine/chest");
        legBone = transform.Find("character_leg");
    }

    private void ClearOldParts()
    {
        if (currentHair != null) DestroyImmediate(currentHair);
    }

    private Material CreateColoredMaterial()
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = RandomColor();
        return mat;
    }

    private Color RandomColor()
    {
        return UnityEngine.Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
    }
}
