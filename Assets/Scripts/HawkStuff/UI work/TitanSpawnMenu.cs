using UnityEngine;
using Photon.Pun;
using ApplicationManagers;
using GameManagers;

public class TitanSpawnMenu : MonoBehaviourPun
{
    private bool menuOpen = false;

    private string[] titanTypes = new string[] { "Normal", "Abnormal", "Jumper", "Crawler", "Thrower", "Punk", "Aberrant" };
    private int selectedTypeIndex = 0;

    private string inputX = "0", inputY = "0", inputZ = "0", inputCount = "1";

    private bool useRandomWeights = false;

    private string[] weightInputs = new string[] { "10", "10", "10", "10", "10", "10", "10" };

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            menuOpen = !menuOpen;
            Cursor.visible = menuOpen;
            Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void OnGUI()
    {
        if (!menuOpen) return;

        GUI.Box(new Rect(20, 20, 380, 420), "Titan Spawn Menu");

        // Position Inputs
        GUI.Label(new Rect(30, 60, 80, 20), "Position X:");
        inputX = GUI.TextField(new Rect(120, 60, 100, 20), inputX);
        GUI.Label(new Rect(30, 90, 80, 20), "Position Y:");
        inputY = GUI.TextField(new Rect(120, 90, 100, 20), inputY);
        GUI.Label(new Rect(30, 120, 80, 20), "Position Z:");
        inputZ = GUI.TextField(new Rect(120, 120, 100, 20), inputZ);

        // Count
        GUI.Label(new Rect(30, 150, 80, 20), "Count:");
        inputCount = GUI.TextField(new Rect(120, 150, 100, 20), inputCount);

        // Random Toggle
        useRandomWeights = GUI.Toggle(new Rect(30, 180, 200, 20), useRandomWeights, " Use Weighted Random");

        if (useRandomWeights)
        {
            GUI.Label(new Rect(30, 210, 200, 20), "Titan Type Weights (%):");

            for (int i = 0; i < titanTypes.Length; i++)
            {
                GUI.Label(new Rect(30, 240 + i * 25, 80, 20), titanTypes[i]);
                weightInputs[i] = GUI.TextField(new Rect(110, 240 + i * 25, 60, 20), weightInputs[i]);
            }
        }
        else
        {
            GUI.Label(new Rect(30, 210, 100, 20), "Titan Type:");
            selectedTypeIndex = GUI.Toolbar(new Rect(30, 240, 320, 30), selectedTypeIndex, titanTypes);
        }

        if (GUI.Button(new Rect(140, 380, 100, 30), "Spawn"))
        {
            TrySpawnTitans();
        }
    }

    private void TrySpawnTitans()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Only Master Client can spawn titans.");
            return;
        }

        if (!float.TryParse(inputX, out float x)) x = 0f;
        if (!float.TryParse(inputY, out float y)) y = 0f;
        if (!float.TryParse(inputZ, out float z)) z = 0f;
        if (!int.TryParse(inputCount, out int count)) count = 1;

        Vector3 basePos = new Vector3(x, y, z);
        InGameManager manager = SceneLoader.CurrentGameManager as InGameManager;

        if (manager == null)
        {
            Debug.LogError("InGameManager not found.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            string typeToUse = useRandomWeights ? GetWeightedRandomType() : titanTypes[selectedTypeIndex];
            Vector3 offset = new Vector3(i * 5f, 0f, 0f);
            manager.SpawnAITitanAt(typeToUse, basePos + offset, 0f);
        }
    }

    private string GetWeightedRandomType()
    {
        float[] weights = new float[titanTypes.Length];
        float totalWeight = 0f;

        for (int i = 0; i < titanTypes.Length; i++)
        {
            if (!float.TryParse(weightInputs[i], out float w)) w = 0f;
            weights[i] = Mathf.Max(0, w);
            totalWeight += weights[i];
        }

        if (totalWeight <= 0f)
            return "Normal"; // fallback

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (rand <= cumulative)
                return titanTypes[i];
        }

        return titanTypes[0]; // fallback
    }
}
