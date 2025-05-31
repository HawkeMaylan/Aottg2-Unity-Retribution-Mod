using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using UI;
using GameManagers;
using Photon.Pun;

public class TeleportMenu : MonoBehaviourPunCallbacks
{
    private bool menuOpen = false;
    private Vector2 scrollPosition;
    private Player selectedPlayer;
    private string inputX = "", inputY = "", inputZ = "", searchFilter = "";

    private bool confirmKick = false;
    private bool confirmBan = false;

    private List<Player> _cachedPlayers = new List<Player>();
    private Dictionary<int, string> _playerLabels = new Dictionary<int, string>();
    private Dictionary<int, string> _cachedNames = new Dictionary<int, string>();
    private Dictionary<int, bool> _cachedDeathStatus = new Dictionary<int, bool>();

    private bool showInventoryPanel = false;
    private string newCannonCount = "0";
    private string newWagon1Count = "0";
    private string newWagon2Count = "0";


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightControl) && PhotonNetwork.IsMasterClient)
        {
            menuOpen = !menuOpen;
            ToggleCursor(menuOpen);

            if (menuOpen)
                RefreshPlayerList();
        }
    }

    private void ToggleCursor(bool enable)
    {
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = enable;
    }

    private void RefreshPlayerList()
    {
        _cachedPlayers = new List<Player>(PhotonNetwork.PlayerList);
        _playerLabels.Clear();
        _cachedNames.Clear();
        _cachedDeathStatus.Clear();

        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView?.Owner != null)
            {
                int actorId = human.photonView.Owner.ActorNumber;
                _cachedNames[actorId] = human.Name;
                _cachedDeathStatus[actorId] = human.Dead;
            }
        }

        foreach (var player in _cachedPlayers)
        {
            _playerLabels[player.ActorNumber] = GeneratePlayerLabel(player);
        }
    }

    private void OnGUI()
    {
        if (!menuOpen)
            return;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };

        GUI.Label(new Rect(Screen.width / 2 - 200, 20, 400, 40), "MC Menu", titleStyle);

        if (GUI.Button(new Rect(Screen.width - 120, 20, 100, 30), "Close"))
        {
            menuOpen = false;
            ToggleCursor(false);
            return;
        }

        GUI.Label(new Rect(30, 50, 60, 20), "Search:");
        string newSearch = GUI.TextField(new Rect(90, 50, 200, 20), searchFilter);
        if (newSearch != searchFilter)
        {
            searchFilter = newSearch;
            RefreshPlayerList();
        }

        GUILayout.BeginArea(new Rect(30, 80, 300, Screen.height - 150));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var player in _cachedPlayers)
        {
            if (!_playerLabels.TryGetValue(player.ActorNumber, out string label))
                label = GeneratePlayerLabel(player);

            if (!string.IsNullOrEmpty(searchFilter) && !label.ToLower().Contains(searchFilter.ToLower()))
                continue;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };

            if (GUILayout.Button(label, buttonStyle, GUILayout.Height(22)))
            {
                selectedPlayer = player;
                confirmKick = false;
                confirmBan = false;
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (GUI.Button(new Rect(30, Screen.height - 60, 100, 30), "Refresh"))
        {
            RefreshPlayerList();
        }

        DrawPlayerPanel();
    }

    private void DrawPlayerPanel()
    {
        if (selectedPlayer == null) return;

        GUI.Box(new Rect(Screen.width - 350, 100, 300, 600), "Teleport " + GeneratePlayerLabel(selectedPlayer));

        GUI.Label(new Rect(Screen.width - 320, 140, 50, 25), "X:");
        inputX = GUI.TextField(new Rect(Screen.width - 270, 140, 140, 25), inputX);

        GUI.Label(new Rect(Screen.width - 320, 170, 50, 25), "Y:");
        inputY = GUI.TextField(new Rect(Screen.width - 270, 170, 140, 25), inputY);

        GUI.Label(new Rect(Screen.width - 320, 200, 50, 25), "Z:");
        inputZ = GUI.TextField(new Rect(Screen.width - 270, 200, 140, 25), inputZ);

        if (GUI.Button(new Rect(Screen.width - 300, 240, 200, 30), "Teleport Player")) TryTeleportSelectedPlayer();
        if (GUI.Button(new Rect(Screen.width - 300, 280, 200, 30), "Teleport Player's Horse")) TryTeleportHorseToPlayer();
        if (GUI.Button(new Rect(Screen.width - 300, 320, 200, 30), "Kill Player's Horse")) TryKillHorse();
        if (GUI.Button(new Rect(Screen.width - 300, 360, 200, 30), "Respawn Player's Horse")) TryRespawnHorse();
        if (GUI.Button(new Rect(Screen.width - 300, 400, 200, 30), "Bring Selected Player to Me")) BringPlayerToMC();
        if (GUI.Button(new Rect(Screen.width - 300, 440, 200, 30), "Bring Me to Selected Player")) BringMCToPlayer();
        if (GUI.Button(new Rect(Screen.width - 300, 480, 200, 30), "Revive Player")) TryReviveSelectedPlayer();

        if (GUI.Button(new Rect(Screen.width - 300, 520, 200, 30), confirmKick ? "Are you sure? (Kick)" : "Kick Player"))
        {
            if (confirmKick)
                ChatManager.KickPlayer(selectedPlayer);
            else
                confirmKick = true;
        }

        if (GUI.Button(new Rect(Screen.width - 300, 560, 200, 30), confirmBan ? "Are you sure? (Ban)" : "Ban Player"))
        {
            if (confirmBan)
                ChatManager.KickPlayer(selectedPlayer, ban: true);
            else
                confirmBan = true;
        }

        if (GUI.Button(new Rect(Screen.width - 300, 600, 200, 30), "Kill Player")) TryKillSelectedPlayer();

        GUI.Box(new Rect(Screen.width - 260, 70, 250, 25), "Selected: " + GeneratePlayerLabel(selectedPlayer));

        // Inventory Management Button
        if (GUI.Button(new Rect(Screen.width - 300, 640, 200, 30), "Manage Inventory"))
        {
            showInventoryPanel = !showInventoryPanel;
        }

        // Inventory Panel
        if (showInventoryPanel)
        {
            Human selectedHuman = FindHumanByPlayer(selectedPlayer);
            if (selectedHuman != null)
            {
                var inv = selectedHuman.GetComponent<HumanInventory>();
                if (inv != null)
                {
                    GUI.Box(new Rect(Screen.width - 370, 680, 340, 180), "Inventory");

                    GUI.Label(new Rect(Screen.width - 360, 710, 90, 20), $"Cannons: {inv.cannonCount}");
                    newCannonCount = GUI.TextField(new Rect(Screen.width - 270, 710, 50, 20), newCannonCount);
                    if (GUI.Button(new Rect(Screen.width - 210, 710, 60, 20), "Set"))
                    {
                        PhotonView view = inv.GetComponent<PhotonView>();
                        if (view != null)
                        {
                            view.RPC("RPC_SetInventoryCounts", RpcTarget.AllBufferedViaServer,
                                ParseSafe(newCannonCount), inv.wagon1Count, inv.wagon2Count);
                        }
                    }

                    GUI.Label(new Rect(Screen.width - 360, 740, 90, 20), $"Wagon1: {inv.wagon1Count}");
                    newWagon1Count = GUI.TextField(new Rect(Screen.width - 270, 740, 50, 20), newWagon1Count);
                    if (GUI.Button(new Rect(Screen.width - 210, 740, 60, 20), "Set"))
                    {
                        PhotonView view = inv.GetComponent<PhotonView>();
                        if (view != null)
                        {
                            view.RPC("RPC_SetInventoryCounts", RpcTarget.AllBufferedViaServer,
                                inv.cannonCount, ParseSafe(newWagon1Count), inv.wagon2Count);
                        }
                    }

                    GUI.Label(new Rect(Screen.width - 360, 770, 90, 20), $"Wagon2: {inv.wagon2Count}");
                    newWagon2Count = GUI.TextField(new Rect(Screen.width - 270, 770, 50, 20), newWagon2Count);
                    if (GUI.Button(new Rect(Screen.width - 210, 770, 60, 20), "Set"))
                    {
                        PhotonView view = inv.GetComponent<PhotonView>();
                        if (view != null)
                        {
                            view.RPC("RPC_SetInventoryCounts", RpcTarget.AllBufferedViaServer,
                                inv.cannonCount, inv.wagon1Count, ParseSafe(newWagon2Count));
                        }
                    }
                }
                else
                {
                    GUI.Label(new Rect(Screen.width - 300, 710, 200, 20), "Inventory not found on player.");
                }
            }
            else
            {
                GUI.Label(new Rect(Screen.width - 300, 710, 200, 20), "Could not find human for player.");
            }
        }
    }



    private void TryKillHorse()
    {
        foreach (var horse in FindObjectsOfType<Horse>())
        {
            if (horse.photonView != null && horse.photonView.Owner != null &&
                horse.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
            {
                PhotonNetwork.Destroy(horse.gameObject);
                break;
            }
        }
    }

    private void TryRespawnHorse()
    {
        if (selectedPlayer == null || !PhotonNetwork.IsMasterClient)
            return;

        Vector3 spawnPosition = Vector3.zero;
        Human humanOwner = null;

        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.Owner != null &&
                human.photonView.Owner.ActorNumber == selectedPlayer.ActorNumber)
            {
                spawnPosition = human.Cache.Transform.position + Vector3.right * 2f;
                humanOwner = human;
                break;
            }
        }

        TryKillHorse();

        GameObject newHorse = PhotonNetwork.Instantiate("Characters/Horse/Prefabs/Horse", spawnPosition, Quaternion.identity);
        PhotonView horseView = newHorse.GetComponent<PhotonView>();

        // Transfer ownership to target player
        horseView.TransferOwnership(selectedPlayer);

        // Call RPC to link on the owner's machine
        horseView.RPC("RPC_SetHorseOwner", selectedPlayer, selectedPlayer.ActorNumber);
    }



    private string GeneratePlayerLabel(Player player)
    {
        string name = _cachedNames.TryGetValue(player.ActorNumber, out var val) ? val : player.NickName;
        bool isDead = _cachedDeathStatus.TryGetValue(player.ActorNumber, out var dead) && dead;

        string label = "";
        if (isDead)
            label += "{X} ";

        label += name;

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
                if (!human.Dead)
                    human.GetHit("Smited", 400, "Thunderspear", "");
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
    private System.Collections.IEnumerator InitHorseAfterDelay(Horse horse, Human owner)
    {
        yield return new WaitForSeconds(0.1f); // Wait for Awake
        if (horse != null && owner != null)
            horse.Init(owner); // This links the horse to the human
    }

    private int ParseSafe(string input)
    {
        return int.TryParse(input, out int val) ? Mathf.Max(0, val) : 0;
    }

    private Human FindHumanByPlayer(Player player)
    {
        foreach (var h in FindObjectsOfType<Human>())
        {
            if (h.photonView != null && h.photonView.Owner == player)
                return h;
        }
        return null;
    }


}
