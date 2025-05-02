using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using UI;
using GameManagers; // Added for ChatManager access

public class TeleportMenu : MonoBehaviourPunCallbacks
{
    private bool menuOpen = false;
    private Vector2 scrollPosition;
    private Player selectedPlayer;
    private string inputX = "";
    private string inputY = "";
    private string inputZ = "";
    private string searchFilter = "";

    private bool confirmKick = false;
    private bool confirmBan = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightControl) && PhotonNetwork.IsMasterClient)
        {
            menuOpen = !menuOpen;
            ToggleCursor(menuOpen);
        }
    }

    private void ToggleCursor(bool enable)
    {
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enable;
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.alignment = TextAnchor.UpperCenter;
        titleStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(Screen.width / 2 - 200, 20, 400, 40), "MC Menu", titleStyle);

        if (GUI.Button(new Rect(Screen.width - 120, 20, 100, 30), "Close"))
        {
            menuOpen = false;
            ToggleCursor(false);
        }

        // Search Field
        GUI.Label(new Rect(30, 50, 60, 20), "Search:");
        searchFilter = GUI.TextField(new Rect(90, 50, 200, 20), searchFilter);

        GUILayout.BeginArea(new Rect(30, 80, 300, Screen.height - 150));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var player in PhotonNetwork.PlayerList)
        {
            string playerLabel = GetPlayerLabel(player);

            if (!string.IsNullOrEmpty(searchFilter) && !playerLabel.ToLower().Contains(searchFilter.ToLower()))
                continue;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;

            if (GUILayout.Button(playerLabel, buttonStyle, GUILayout.Height(22)))
            {
                selectedPlayer = player;
                confirmKick = false;
                confirmBan = false;
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (selectedPlayer != null)
        {
            GUI.Box(new Rect(Screen.width - 350, 100, 300, 600), "Teleport " + GetPlayerLabel(selectedPlayer));

            GUI.Label(new Rect(Screen.width - 320, 140, 50, 25), "X:");
            inputX = GUI.TextField(new Rect(Screen.width - 270, 140, 140, 25), inputX);

            GUI.Label(new Rect(Screen.width - 320, 170, 50, 25), "Y:");
            inputY = GUI.TextField(new Rect(Screen.width - 270, 170, 140, 25), inputY);

            GUI.Label(new Rect(Screen.width - 320, 200, 50, 25), "Z:");
            inputZ = GUI.TextField(new Rect(Screen.width - 270, 200, 140, 25), inputZ);

            if (GUI.Button(new Rect(Screen.width - 300, 240, 200, 30), "Teleport Player"))
            {
                TryTeleportSelectedPlayer();
            }

            if (GUI.Button(new Rect(Screen.width - 300, 280, 200, 30), "Teleport Player's Horse"))
            {
                TryTeleportHorseToPlayer();
            }

            if (GUI.Button(new Rect(Screen.width - 300, 320, 200, 30), "Bring Selected Player to Me"))
            {
                BringPlayerToMC();
            }

            if (GUI.Button(new Rect(Screen.width - 300, 360, 200, 30), "Bring Me to Selected Player"))
            {
                BringMCToPlayer();
            }

            if (GUI.Button(new Rect(Screen.width - 300, 400, 200, 30), "Revive Player"))
            {
                TryReviveSelectedPlayer();
            }

            if (GUI.Button(new Rect(Screen.width - 300, 440, 200, 30), confirmKick ? "Are you sure? (Kick)" : "Kick Player"))
            {
                if (confirmKick)
                    ChatManager.KickPlayer(selectedPlayer);
                else
                    confirmKick = true;
            }

            if (GUI.Button(new Rect(Screen.width - 300, 480, 200, 30), confirmBan ? "Are you sure? (Ban)" : "Ban Player"))
            {
                if (confirmBan)
                    ChatManager.KickPlayer(selectedPlayer, ban: true);
                else
                    confirmBan = true;
            }

            if (GUI.Button(new Rect(Screen.width - 300, 520, 200, 30), "Kill Player"))
            {
                TryKillSelectedPlayer();
            }

            // Selected Player Display
            GUI.Box(new Rect(Screen.width - 260, 70, 250, 25), "Selected: " + GetPlayerLabel(selectedPlayer));
        }
    }

    private string GetPlayerLabel(Player player)
    {
        string name = player.NickName;

        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.Owner != null && human.photonView.Owner.ActorNumber == player.ActorNumber)
            {
                name = human.Name;
                break;
            }
        }

        string label = name;
        if (player.IsMasterClient)
            label += " (MC)";
        label += $" {{{player.ActorNumber}}}";
        return label;
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
                    if (human.MountState == HumanMountState.Horse)
                        human.Unmount(true);

                    human.photonView.RPC("RPC_Teleport", human.photonView.Owner, new Vector3(x, y, z));
                    break;
                }
            }
        }
    }

    private void TryTeleportHorseToPlayer()
    {
        foreach (var horse in FindObjectsOfType<Horse>())
        {
            if (horse.photonView != null && horse.photonView.Owner != null &&
                horse.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
            {
                horse.photonView.RPC("RPC_TeleportToHuman", horse.photonView.Owner);
                break;
            }
        }
    }

    private void BringPlayerToMC()
    {
        Human mc = FindLocalHuman();
        if (mc == null) return;

        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.Owner != null &&
                human.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
            {
                if (human.MountState == HumanMountState.Horse)
                    human.Unmount(true);

                human.photonView.RPC("RPC_Teleport", human.photonView.Owner, mc.Cache.Transform.position);
                break;
            }
        }
    }

    private void BringMCToPlayer()
    {
        Human mc = FindLocalHuman();
        if (mc == null) return;

        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.Owner != null &&
                human.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
            {
                mc.photonView.RPC("RPC_Teleport", mc.photonView.Owner, human.Cache.Transform.position);
                break;
            }
        }
    }

    private void TryReviveSelectedPlayer()
    {
        if (selectedPlayer != null)
        {
            RPCManager.PhotonView.RPC("SpawnPlayerRPC", selectedPlayer, new object[] { false });
            ChatManager.SendChat("You have been revived by master client.", selectedPlayer, ChatTextColor.System);
        }
    }

    private void TryKillSelectedPlayer()
    {
        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.Owner != null &&
                human.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
            {
                if (human != null && !human.Dead)
                {
                    human.GetHit("Smited", 400, "Thunderspear", ""); // Instant kill same as Thunderspear
                }
                break;
            }
        }
    }

    private Human FindLocalHuman()
    {
        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human != null && human.IsMine())
                return human;
        }
        return null;
    }
}
