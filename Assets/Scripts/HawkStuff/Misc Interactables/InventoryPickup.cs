using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;

public class ItemGrantZone : MonoBehaviourPunCallbacks
{

    [System.Serializable]
    public struct GrantItem
    {
        public string itemType;
        public int amount;
    }


    [Header("Grant Settings")]
    public Collider triggerZone;
    public List<GrantItem> itemsToGrant = new List<GrantItem>();

    public float cooldownDuration = 10f;
    public int maxGrants = 3;

    [Header("UI Prompt")]
    public float promptDuration = 3f;

    private Human localHuman;
    private Coroutine promptCoroutine;
    private static string currentPrompt = "";
    private static string extraPrompt = "";

    private float lastGrantTime = -999f;
    private int grantsUsed = 0;
    private bool isInside = false;

    Dictionary<string, string> friendlyNames = new Dictionary<string, string>
{
    { "Wagon1", "Support Wagon" },
    { "Wagon2", "Resupply Wagon" },
    { "Cannon", "Cannon" },
    { "WallCannon", "Wall Cannon" },
    { "CannonGround", "Field Cannon" },
    //Add new item names as need be
};


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
            int remaining = maxGrants - grantsUsed;

            if (remaining <= 0)
            {
                currentPrompt = "No Items remaining";
                extraPrompt = "";
                return;
            }

            extraPrompt = $"Items left: {remaining}";

            float timeSinceLast = Time.time - lastGrantTime;
            if (timeSinceLast < cooldownDuration)
            {
                float timeLeft = Mathf.Ceil(cooldownDuration - timeSinceLast);
                currentPrompt = $"Pickup on cooldown ({timeLeft}s)";
            }
            else
            {
                string itemList = string.Join(", ", itemsToGrant.ConvertAll(
    entry =>
    {
        string name = friendlyNames.TryGetValue(entry.itemType, out var display) ? display : entry.itemType;
        return $"{name} x{entry.amount}";
    }));
                currentPrompt = $"Press {SettingsManager.InputSettings.Interaction.Interact2} to Pick Up: {itemList}";




                if (SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
                {
                    lastGrantTime = Time.time;
                    grantsUsed++;

                    var inventory = localHuman.GetComponent<HumanInventory>();
                    if (inventory != null)
                    {
                        foreach (var entry in itemsToGrant)
                        {
                            for (int i = 0; i < entry.amount; i++)
                            {
                                inventory.AddItem(entry.itemType);
                            }
                        }
                    }


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
