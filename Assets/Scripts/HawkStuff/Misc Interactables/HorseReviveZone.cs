using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;

public class HorseRespawnZone : MonoBehaviourPunCallbacks
{
    public Collider triggerZone; // 
    public Vector3 spawnOffset = new Vector3(2f, 0f, 0f);
    public float promptDuration = 3f;

    private Human localHuman;
    private Coroutine promptCoroutine;
    private static string currentPrompt = "";
    private bool isInside = false;

    private float lastRespawnTime = -999f;
    public float cooldownDuration = 10f;

    public int maxRespawns = 3; 
    private int respawnsUsed = 0;
    private static string extraPrompt = ""; 



    private void Update()
    {
        if (ChatManager.IsChatActive()) return;

        if (!isInside)
        {
            Human checkHuman = FindLocalHumanInZone();
            if (checkHuman != null)
            {
                localHuman = checkHuman;
                isInside = true;
            }
        }
        else if (localHuman == null || !IsStillInZone(localHuman))
        {
            ClearPrompt();
            isInside = false;
            localHuman = null;
        }

        if (isInside && localHuman != null)
        {
            float timeSinceLast = Time.time - lastRespawnTime;
            int remaining = maxRespawns - respawnsUsed;

            if (remaining <= 0)
            {
                currentPrompt = "No Horses Remaining";
                extraPrompt = "";
                return;
            }

            extraPrompt = $"Horses Left: {remaining}";

            if (timeSinceLast < cooldownDuration)
            {
                float timeLeft = Mathf.Ceil(cooldownDuration - timeSinceLast);
                currentPrompt = $"New Horse Being Prepared ({timeLeft}s)";
            }
            else
            {
                currentPrompt = $"Press {SettingsManager.InputSettings.Interaction.Interact2} To Get A New Horse";

                if (SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
                {
                    lastRespawnTime = Time.time;
                    respawnsUsed++;

                    if (PhotonNetwork.IsMasterClient)
                        TryRespawnHorse(localHuman.photonView.Owner);
                    else
                        photonView.RPC(nameof(RPC_RequestHorseRespawn), RpcTarget.MasterClient, localHuman.photonView.OwnerActorNr);

                    ClearPrompt();
                    isInside = false;
                }
            }
        }

    }


    private Human FindLocalHumanInZone()
    {
        foreach (Human h in FindObjectsOfType<Human>())
        {
            if (h.photonView.IsMine)
            {
                Transform trigger = h.transform.Find("HumanTrigger");
                if (trigger != null && triggerZone.bounds.Contains(trigger.position))
                    return h;
            }
        }
        return null;
    }

    private bool IsStillInZone(Human h)
    {
        Transform trigger = h.transform.Find("HumanTrigger");
        return trigger != null && triggerZone.bounds.Contains(trigger.position);
    }

    [PunRPC]
    private void RPC_RequestHorseRespawn(int actorNumber)
    {
        Player target = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (target != null)
            TryRespawnHorse(target);
    }

    private void TryRespawnHorse(Player player)
    {
        Human humanOwner = FindHumanByPlayer(player);
        if (humanOwner == null) return;

        Vector3 spawnPosition = humanOwner.Cache.Transform.position + spawnOffset;
        KillOwnedHorse(player);

        GameObject horseObj = PhotonNetwork.Instantiate("Characters/Horse/Prefabs/Horse", spawnPosition, Quaternion.identity);
        PhotonView horseView = horseObj.GetComponent<PhotonView>();
        horseView.TransferOwnership(player);
        StartCoroutine(EnsureHorseOwnershipAndLink(horseView, player.ActorNumber));
    }

    private void KillOwnedHorse(Player player)
    {
        foreach (var horse in FindObjectsOfType<Horse>())
        {
            if (horse.photonView != null && horse.photonView.Owner == player)
            {
                PhotonNetwork.Destroy(horse.gameObject);
                break;
            }
        }
    }

    private IEnumerator EnsureHorseOwnershipAndLink(PhotonView horseView, int actorNumber)
    {
        float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (horseView != null)
            {
                if (horseView.Owner != null && horseView.Owner.ActorNumber == actorNumber)
                {
                    horseView.RPC("RPC_SetHorseOwner", horseView.Owner, actorNumber);
                    yield break;
                }
                else
                {
                    horseView.TransferOwnership(actorNumber);
                }
            }

            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        Debug.LogWarning($"[HorseRespawnZone] Failed to assign horse to player {actorNumber}");
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

    private void SetPrompt(string baseText, float duration)
    {
        if (Time.time - lastRespawnTime < cooldownDuration)
        {
            float timeLeft = Mathf.Ceil(cooldownDuration - (Time.time - lastRespawnTime));
            currentPrompt = $"Respawn on cooldown ({timeLeft}s)";
        }
        else
        {
            currentPrompt = baseText;
        }

        if (promptCoroutine != null)
            StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(ClearPromptAfterDelay(duration));
    }


    private void ClearPrompt()
    {
        currentPrompt = "";
        extraPrompt = "";
        if (promptCoroutine != null)
        {
            StopCoroutine(promptCoroutine);
            promptCoroutine = null;
        }
    }


    private IEnumerator ClearPromptAfterDelay(float time)
    {
        yield return new WaitForSeconds(time);
        ClearPrompt();
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false,
                normal = { textColor = Color.white }
            };

            float labelWidth = 800f;
            float labelHeight = 50f;
            float labelX = Screen.width / 2 - labelWidth / 2;

            GUI.Label(new Rect(labelX, 50, labelWidth, labelHeight), currentPrompt, style);

            if (!string.IsNullOrEmpty(extraPrompt))
                GUI.Label(new Rect(labelX, 85, labelWidth, labelHeight), extraPrompt, style);
        }
    }


}
