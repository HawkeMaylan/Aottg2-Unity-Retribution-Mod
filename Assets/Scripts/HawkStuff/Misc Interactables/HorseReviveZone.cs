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

    private void Update()
    {
        if (ChatManager.IsChatActive()) return;

        if (!isInside)
        {
            Human checkHuman = FindLocalHumanInZone();
            if (checkHuman != null)
            {
                localHuman = checkHuman;
                SetPrompt($"Press {SettingsManager.InputSettings.Interaction.Interact2} to Respawn Horse", promptDuration);
                isInside = true;
            }
        }
        else if (localHuman == null || !IsStillInZone(localHuman))
        {
            ClearPrompt();
            isInside = false;
            localHuman = null;
        }

        if (isInside && localHuman != null && SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
        {
            if (PhotonNetwork.IsMasterClient)
                TryRespawnHorse(localHuman.photonView.Owner);
            else
                photonView.RPC(nameof(RPC_RequestHorseRespawn), RpcTarget.MasterClient, localHuman.photonView.OwnerActorNr);

            ClearPrompt();
            isInside = false;
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

    private void SetPrompt(string text, float duration)
    {
        currentPrompt = text;
        if (promptCoroutine != null)
            StopCoroutine(promptCoroutine);
        promptCoroutine = StartCoroutine(ClearPromptAfterDelay(duration));
    }

    private void ClearPrompt()
    {
        currentPrompt = "";
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
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(Screen.width / 2 - 150, 50, 300, 50), currentPrompt, style);
        }
    }
}
