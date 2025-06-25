using UnityEngine;
using Photon.Pun;
using ApplicationManagers;
using GameManagers;

public class TitanSpawnMenu : MonoBehaviourPun
{
    private bool menuOpen = false;
    private string inputType = "Normal";
    private string inputCount = "1";
    private string inputX = "0", inputY = "0", inputZ = "0";

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            menuOpen = !menuOpen;
            Cursor.visible = menuOpen;
            Cursor.lockState = menuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        GUI.Box(new Rect(20, 20, 300, 250), "Titan Spawn Menu");

        GUI.Label(new Rect(30, 50, 80, 20), "Titan Type:");
        inputType = GUI.TextField(new Rect(120, 50, 150, 20), inputType);

        GUI.Label(new Rect(30, 80, 80, 20), "Count:");
        inputCount = GUI.TextField(new Rect(120, 80, 150, 20), inputCount);

        GUI.Label(new Rect(30, 110, 80, 20), "Position X:");
        inputX = GUI.TextField(new Rect(120, 110, 150, 20), inputX);

        GUI.Label(new Rect(30, 140, 80, 20), "Position Y:");
        inputY = GUI.TextField(new Rect(120, 140, 150, 20), inputY);

        GUI.Label(new Rect(30, 170, 80, 20), "Position Z:");
        inputZ = GUI.TextField(new Rect(120, 170, 150, 20), inputZ);

        if (GUI.Button(new Rect(100, 210, 100, 25), "Spawn"))
        {
            TrySpawnTitans();
        }
    }

    private void TrySpawnTitans()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only Master Client can spawn titans.");
            return;
        }

        if (!int.TryParse(inputCount, out int count)) count = 1;
        if (!float.TryParse(inputX, out float x)) x = 0f;
        if (!float.TryParse(inputY, out float y)) y = 0f;
        if (!float.TryParse(inputZ, out float z)) z = 0f;

        Vector3 basePos = new Vector3(x, y, z);
        string type = string.IsNullOrWhiteSpace(inputType) ? "Default" : inputType;

        InGameManager manager = SceneLoader.CurrentGameManager as InGameManager;
        if (manager == null)
        {
            Debug.LogError("InGameManager not found.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(i * 5f, 0f, 0f);
            manager.SpawnAITitanAt(type, basePos + offset, 0f);
        }
    }
}
