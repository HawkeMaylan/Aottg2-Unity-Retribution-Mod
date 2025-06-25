using UnityEngine;
using Photon.Pun;
using ApplicationManagers;
using GameManagers;
using Characters;
using System.Collections;

public class TitanSpawnMenu : MonoBehaviourPun
{
    private bool menuOpen = false;

    private string[] titanTypes = new string[] { "Normal", "Abnormal", "Jumper", "Crawler", "Thrower", "Punk", "Aberrant" };
    private int selectedTypeIndex = 0;

    private string inputX = "0", inputY = "0", inputZ = "0", inputCount = "1";
    private bool useRandomWeights = false;
    private string[] weightInputs = new string[] { "10", "10", "10", "10", "10", "10", "10" };

    private bool overrideSize = false, overrideHP = false, overrideSpeed = false, overrideAnimSpeed = false;
    private string minSize = "1", maxSize = "1";
    private string minHP = "1000", maxHP = "2000";
    private string minSpeed = "10", maxSpeed = "20";
    private string minAnimSpeed = "1", maxAnimSpeed = "1.5";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) && PhotonNetwork.IsMasterClient)
        {
            menuOpen = !menuOpen;
            Cursor.visible = menuOpen;
            Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void OnGUI()
    {
        if (!menuOpen) return;

        GUI.Box(new Rect(20, 20, 400, 700), "Titan Spawn Menu");

        GUI.Label(new Rect(30, 60, 80, 20), "Position X:"); inputX = GUI.TextField(new Rect(120, 60, 100, 20), inputX);
        GUI.Label(new Rect(30, 90, 80, 20), "Position Y:"); inputY = GUI.TextField(new Rect(120, 90, 100, 20), inputY);
        GUI.Label(new Rect(30, 120, 80, 20), "Position Z:"); inputZ = GUI.TextField(new Rect(120, 120, 100, 20), inputZ);
        GUI.Label(new Rect(30, 150, 80, 20), "Count:"); inputCount = GUI.TextField(new Rect(120, 150, 100, 20), inputCount);

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
            selectedTypeIndex = GUI.Toolbar(new Rect(30, 240, 340, 30), selectedTypeIndex, titanTypes);
        }

        int baseY = 440;
        overrideSize = GUI.Toggle(new Rect(30, baseY, 200, 20), overrideSize, " Override Size");
        GUI.Label(new Rect(50, baseY + 25, 80, 20), "Min:"); minSize = GUI.TextField(new Rect(90, baseY + 25, 50, 20), minSize);
        GUI.Label(new Rect(150, baseY + 25, 80, 20), "Max:"); maxSize = GUI.TextField(new Rect(190, baseY + 25, 50, 20), maxSize);

        overrideHP = GUI.Toggle(new Rect(30, baseY + 55, 200, 20), overrideHP, " Override HP");
        GUI.Label(new Rect(50, baseY + 80, 80, 20), "Min:"); minHP = GUI.TextField(new Rect(90, baseY + 80, 50, 20), minHP);
        GUI.Label(new Rect(150, baseY + 80, 80, 20), "Max:"); maxHP = GUI.TextField(new Rect(190, baseY + 80, 50, 20), maxHP);

        overrideSpeed = GUI.Toggle(new Rect(30, baseY + 110, 200, 20), overrideSpeed, " Override Run Speed");
        GUI.Label(new Rect(50, baseY + 135, 80, 20), "Min:"); minSpeed = GUI.TextField(new Rect(90, baseY + 135, 50, 20), minSpeed);
        GUI.Label(new Rect(150, baseY + 135, 80, 20), "Max:"); maxSpeed = GUI.TextField(new Rect(190, baseY + 135, 50, 20), maxSpeed);

        overrideAnimSpeed = GUI.Toggle(new Rect(30, baseY + 165, 200, 20), overrideAnimSpeed, " Override Animation Speed");
        GUI.Label(new Rect(50, baseY + 190, 80, 20), "Min:"); minAnimSpeed = GUI.TextField(new Rect(90, baseY + 190, 50, 20), minAnimSpeed);
        GUI.Label(new Rect(150, baseY + 190, 80, 20), "Max:"); maxAnimSpeed = GUI.TextField(new Rect(190, baseY + 190, 50, 20), maxAnimSpeed);

        if (GUI.Button(new Rect(140, baseY + 230, 100, 30), "Spawn"))
            TrySpawnTitans();
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

        string typeToUse = useRandomWeights ? GetWeightedRandomType() : titanTypes[selectedTypeIndex];
        manager.StartCoroutine(SpawnAndOverrideRoutine(manager, typeToUse, count, basePos, 0f));
    }

    private IEnumerator SpawnAndOverrideRoutine(InGameManager manager, string type, int count, Vector3 basePos, float rotationY)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(i * 5f, 0f, 0f);
            BaseTitan titan = manager.SpawnAITitanAt(type, basePos + offset, rotationY);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (titan == null)
            {
                Debug.LogError($"[TitanSpawnMenu] Failed to spawn titan of type: {type}");
                continue;
            }

            if (overrideSize && float.TryParse(minSize, out float minS) && float.TryParse(maxSize, out float maxS))
                titan.SetSize(Random.Range(minS, maxS));

            if (overrideHP && int.TryParse(minHP, out int minHp) && int.TryParse(maxHP, out int maxHp))
                titan.SetHealth(Random.Range(minHp, maxHp + 1));

            if (overrideSpeed && float.TryParse(minSpeed, out float minSpd) && float.TryParse(maxSpeed, out float maxSpd))
                titan.RunSpeedBase = Random.Range(minSpd, maxSpd);

            if (overrideAnimSpeed && float.TryParse(minAnimSpeed, out float minAnim) && float.TryParse(maxAnimSpeed, out float maxAnim))
                titan.AttackSpeedMultiplier = Random.Range(minAnim, maxAnim);
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
            return "Normal";

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (rand <= cumulative)
                return titanTypes[i];
        }

        return titanTypes[0];
    }
}
