using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;

public class InteractiveChanceObject : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Interaction Settings")]
    public Collider triggerZone;
    public float interactionCooldown = 5f;
    public float stallDuration = 2f;
    public int successChance = 2; // 1/50 would be 2% chance

    [Header("Audio Sources")]
    public AudioSource successAudio;
    public AudioSource failureAudio;

    [Header("Spawn Settings")]
    public string objectToSpawn = "Buildables/HellToken";
    public Transform spawnLocation; // Assign an empty object for spawn location

    [Header("UI Prompt")]
    public float promptDuration = 3f;

    private Human localHuman;
    private Coroutine promptCoroutine;
    private static string currentPrompt = "";
    private static string extraPrompt = "";

    [SerializeField]
    private float lastInteractionTime = -999f;
    private bool isInside = false;
    private bool isProcessing = false;
    private bool isOnCooldown = false;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(lastInteractionTime);
            stream.SendNext(isProcessing);
            stream.SendNext(isOnCooldown);
        }
        else
        {
            lastInteractionTime = (float)stream.ReceiveNext();
            isProcessing = (bool)stream.ReceiveNext();
            isOnCooldown = (bool)stream.ReceiveNext();
        }
    }

    private void Update()
    {
        if (ChatManager.IsChatActive() || isProcessing) return;

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
            float timeSinceLast = Time.time - lastInteractionTime;
            isOnCooldown = timeSinceLast < interactionCooldown;

            if (isOnCooldown)
            {
                float timeLeft = Mathf.Ceil(interactionCooldown - timeSinceLast);
                currentPrompt = $"On cooldown ({timeLeft}s)";
                extraPrompt = $"{successChance}% chance for Hell Token";
            }
            else
            {
                currentPrompt = $"Press {SettingsManager.InputSettings.Interaction.Interact2} to try your luck";
                extraPrompt = $"{successChance}% chance for Hell Token";

                if (SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
                {
                    photonView.RPC("RPC_AttemptInteraction", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_AttemptInteraction(int actorId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || isProcessing) return;

        // Start processing on all clients
        photonView.RPC("RPC_StartProcessing", RpcTarget.All);

        // Master client handles the chance roll after stall
        StartCoroutine(ProcessInteraction(actorId));
    }

    [PunRPC]
    private void RPC_StartProcessing()
    {
        isProcessing = true;
        // No audio played during stall, just the delay
    }

    [PunRPC]
    private void RPC_EndProcessing(bool success)
    {
        isProcessing = false;
        lastInteractionTime = Time.time;

        if (success)
        {
            // Play success audio
            if (successAudio != null)
                successAudio.Play();
        }
        else
        {
            // Play failure audio
            if (failureAudio != null)
                failureAudio.Play();
        }
    }

    [PunRPC]
    private void RPC_SpawnHellToken(int actorId)
    {
        if (spawnLocation != null)
        {
            // Spawn the Hell Token at the specified location
            PhotonNetwork.Instantiate(objectToSpawn, spawnLocation.position, spawnLocation.rotation);
        }
    }

    private IEnumerator ProcessInteraction(int actorId)
    {
        // Wait for the stall duration (silent delay)
        yield return new WaitForSeconds(stallDuration);

        // Roll for chance (1/50 = 2% chance)
        bool success = Random.Range(0, 100) < successChance;

        if (success)
        {
            // Spawn Hell Token for everyone to see
            photonView.RPC("RPC_SpawnHellToken", RpcTarget.All, actorId);
        }

        // End processing on all clients
        photonView.RPC("RPC_EndProcessing", RpcTarget.All, success);
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

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.UpperCenter,
                wordWrap = false,
                normal = { textColor = currentPrompt.Contains("cooldown") ? Color.red : Color.white }
            };

            float labelWidth = 600f;
            float labelHeight = 30f;
            float labelX = Screen.width / 2 - labelWidth / 2;

            GUI.Label(new Rect(labelX, 50, labelWidth, labelHeight), currentPrompt, style);

            if (!string.IsNullOrEmpty(extraPrompt))
                GUI.Label(new Rect(labelX, 85, labelWidth, labelHeight), extraPrompt, style);
        }
    }
}