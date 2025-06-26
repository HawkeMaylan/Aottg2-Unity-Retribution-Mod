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

    private bool overrideWalkSpeed = false, overrideTurnSpeed = false, overrideActionPause = false;
    private bool overrideTurnPause = false, overrideJumpForce = false, overrideRotateSpeed = false;
    private string minWalkSpeed = "5", maxWalkSpeed = "10";
    private string minTurnSpeed = "1", maxTurnSpeed = "3";
    private string minActionPause = "0.5", maxActionPause = "1";
    private string minTurnPause = "0.5", maxTurnPause = "1.2";
    private string minJumpForce = "100", maxJumpForce = "300";
    private string minRotateSpeed = "1.5", maxRotateSpeed = "4.0";

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

        GUI.Box(new Rect(20, 20, 400, 1150), "Titan Spawn Menu");

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
        DrawOverride("Size", ref overrideSize, ref minSize, ref maxSize, baseY);
        DrawOverride("HP", ref overrideHP, ref minHP, ref maxHP, baseY += 55);
        DrawOverride("Run Speed", ref overrideSpeed, ref minSpeed, ref maxSpeed, baseY += 55);
        DrawOverride("Animation Speed", ref overrideAnimSpeed, ref minAnimSpeed, ref maxAnimSpeed, baseY += 55);
        DrawOverride("Walk Speed", ref overrideWalkSpeed, ref minWalkSpeed, ref maxWalkSpeed, baseY += 55);
        DrawOverride("Turn Speed", ref overrideTurnSpeed, ref minTurnSpeed, ref maxTurnSpeed, baseY += 55);
        DrawOverride("Action Pause", ref overrideActionPause, ref minActionPause, ref maxActionPause, baseY += 55);
        DrawOverride("Turn Pause", ref overrideTurnPause, ref minTurnPause, ref maxTurnPause, baseY += 55);
        DrawOverride("Jump Force", ref overrideJumpForce, ref minJumpForce, ref maxJumpForce, baseY += 55);
        DrawOverride("Rotate Speed", ref overrideRotateSpeed, ref minRotateSpeed, ref maxRotateSpeed, baseY += 55);

        if (GUI.Button(new Rect(240, 60, 100, 30), "Spawn"))
            TrySpawnTitans();
    }

    private void DrawOverride(string label, ref bool toggle, ref string min, ref string max, int y)
    {
        toggle = GUI.Toggle(new Rect(30, y, 200, 20), toggle, $" Override {label}");
        GUI.Label(new Rect(50, y + 25, 80, 20), "Min:"); min = GUI.TextField(new Rect(90, y + 25, 50, 20), min);
        GUI.Label(new Rect(150, y + 25, 80, 20), "Max:"); max = GUI.TextField(new Rect(190, y + 25, 50, 20), max);
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
            if (overrideWalkSpeed && float.TryParse(minWalkSpeed, out float minWalk) && float.TryParse(maxWalkSpeed, out float maxWalk))
                titan.WalkSpeedBase = Random.Range(minWalk, maxWalk);
            if (overrideTurnSpeed && float.TryParse(minTurnSpeed, out float minTurn) && float.TryParse(maxTurnSpeed, out float maxTurn))
                titan.TurnSpeed = Random.Range(minTurn, maxTurn);
            if (overrideActionPause && float.TryParse(minActionPause, out float minActPause) && float.TryParse(maxActionPause, out float maxActPause))
                titan.ActionPause = Random.Range(minActPause, maxActPause);
            if (overrideTurnPause && float.TryParse(minTurnPause, out float minTPause) && float.TryParse(maxTurnPause, out float maxTPause))
                titan.TurnPause = Random.Range(minTPause, maxTPause);
            if (overrideJumpForce && float.TryParse(minJumpForce, out float minJump) && float.TryParse(maxJumpForce, out float maxJump))
                titan.JumpForce = Random.Range(minJump, maxJump);
            if (overrideRotateSpeed && float.TryParse(minRotateSpeed, out float minRot) && float.TryParse(maxRotateSpeed, out float maxRot))
                titan.RotateSpeed = Random.Range(minRot, maxRot);
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
