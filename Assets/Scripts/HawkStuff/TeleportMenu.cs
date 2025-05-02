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
        if (Input.GetKeyDown(KeyCode.RightControl))
        {
            if (PhotonNetwork.IsMasterClient)
                menuOpen = !menuOpen;
        }
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 26;
        titleStyle.alignment = TextAnchor.UpperCenter;
        titleStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(Screen.width / 2 - 200, 20, 400, 40), "Teleport Players", titleStyle);

        if (GUI.Button(new Rect(Screen.width - 120, 20, 100, 30), "Close"))
        {
            menuOpen = false;
        }

        GUILayout.BeginArea(new Rect(50, 80, Screen.width - 100, Screen.height - 150));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerLabel = GetPlayerLabel(player);

            if (GUILayout.Button(playerLabel, GUILayout.Height(40)))
            {
                selectedPlayer = player;
                inputX = inputY = inputZ = "";
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (selectedPlayer != null)
        {
            GUI.Box(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 100, 300, 220), "Teleport " + GetPlayerLabel(selectedPlayer));

            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 - 60, 50, 25), "X:");
            inputX = GUI.TextField(new Rect(Screen.width / 2 - 70, Screen.height / 2 - 60, 140, 25), inputX);

            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2 - 30, 50, 25), "Y:");
            inputY = GUI.TextField(new Rect(Screen.width / 2 - 70, Screen.height / 2 - 30, 140, 25), inputY);

            GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2, 50, 25), "Z:");
            inputZ = GUI.TextField(new Rect(Screen.width / 2 - 70, Screen.height / 2, 140, 25), inputZ);

            if (GUI.Button(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 40, 100, 30), "Teleport"))
            {
                TryTeleportSelectedPlayer();
            }
        }
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
                    human.Cache.Transform.position = new Vector3(x, y, z);
                    break;
                }
            }
        }

        selectedPlayer = null;
    }
}
