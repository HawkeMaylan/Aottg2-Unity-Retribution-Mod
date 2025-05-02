using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using UI;

public class TeleportMenu : MonoBehaviourPunCallbacks
{
    private bool menuOpen = false;
    private Vector2 scrollPosition;
    private Player selectedPlayer;
    private string inputX = "";
    private string inputY = "";
    private string inputZ = "";

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (Input.GetKeyDown(KeyCode.RightControl))
            {
                menuOpen = !menuOpen;
                ToggleCursor(menuOpen);
            }
        }
    }

    private void ToggleCursor(bool enable)
    {
        if (enable)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(Screen.width / 2 - 200, 20, 400, 40), "Teleport Players", titleStyle);

        if (GUI.Button(new Rect(Screen.width - 120, 20, 100, 30), "Close"))
        {
            menuOpen = false;
            ToggleCursor(false);
        }

        GUILayout.BeginArea(new Rect(30, 80, 300, Screen.height - 150));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerLabel = GetPlayerLabel(player);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };

            if (GUILayout.Button(playerLabel, buttonStyle, GUILayout.Height(30)))
            {
                selectedPlayer = player;
                //  No longer clearing inputX, inputY, inputZ here
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (selectedPlayer != null)
        {
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 120, 300, 250), "Teleport " + GetPlayerLabel(selectedPlayer));

            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 - 80, 50, 25), "X:");
            inputX = GUI.TextField(new Rect(Screen.width / 2 - 70, Screen.height / 2 - 80, 140, 25), inputX);

            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 - 50, 50, 25), "Y:");
            inputY = GUI.TextField(new Rect(Screen.width / 2 - 70, Screen.height / 2 - 50, 140, 25), inputY);

            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 - 20, 50, 25), "Z:");
            inputZ = GUI.TextField(new Rect(Screen.width / 2 - 70, Screen.height / 2 - 20, 140, 25), inputZ);

            if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 20, 100, 30), "Teleport"))
            {
                TryTeleportSelectedPlayer();
            }
        }
    }

    private void TryTeleportSelectedPlayer()
    {
        if (float.TryParse(inputX, out float x) &&
            float.TryParse(inputY, out float y) &&
            float.TryParse(inputZ, out float z))
        {
            foreach (var human in FindObjectsOfType<Human>())
            {
                if (human.photonView != null && human.photonView.Owner != null &&
                    human.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
                {
                    human.photonView.RPC("RPC_Teleport", human.photonView.Owner, new Vector3(x, y, z));
                    break;
                }
            }
        }

        //  No longer clearing selectedPlayer or input fields
        // selectedPlayer = null;
    }

    private string GetPlayerLabel(Player player)
    {
        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.Owner != null && human.photonView.Owner.ActorNumber == player.ActorNumber)
            {
                return human.Name;
            }
        }

        return player.NickName + " (Not Spawned)";
    }
}
