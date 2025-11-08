using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Characters;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;

public class BladePickup : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Gas Settings")]
    public Collider triggerZone;
    public int bladePickup = 4; // Changed to int since we're adding blades
    public float cooldownDuration = 10f;
    public int maxGrants = 3;

    [Header("Object Cleanup")]
    public bool destroyWhenEmpty = false;
    public float shrinkAndDestroyTime = 1.5f;

    [Header("UI Prompt")]
    public float promptDuration = 3f;

    private Human localHuman;
    private Coroutine promptCoroutine;
    private static string currentPrompt = "";
    private static string extraPrompt = "";

    [SerializeField]
    private float lastGrantTime = -999f;
    private int grantsUsed = 0;
    private bool isInside = false;
    private bool isShrinking = false;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(grantsUsed);
            stream.SendNext(lastGrantTime);
        }
        else
        {
            grantsUsed = (int)stream.ReceiveNext();
            lastGrantTime = (float)stream.ReceiveNext();
        }
    }

    private void Update()
    {
        if (ChatManager.IsChatActive() || isShrinking) return;

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
                currentPrompt = "No Blades remaining";
                extraPrompt = "";
                return;
            }

            // Check if player is already at max blades
            if (IsAtMaxBlades(localHuman))
            {
                currentPrompt = "Blades at maximum capacity";
                extraPrompt = "";
                return;
            }

            extraPrompt = $"Blade pickups left: {remaining}";

            float timeSinceLast = Time.time - lastGrantTime;
            if (timeSinceLast < cooldownDuration)
            {
                float timeLeft = Mathf.Ceil(cooldownDuration - timeSinceLast);
                currentPrompt = $"Pickup on cooldown ({timeLeft}s)";
            }
            else
            {
                currentPrompt = $"Press {SettingsManager.InputSettings.Interaction.Interact2} to Collect Blades: +{bladePickup}";

                if (SettingsManager.InputSettings.Interaction.Interact2.GetKeyDown())
                {
                    photonView.RPC("RPC_TryGrant", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }
    }

    private bool IsAtMaxBlades(Human human)
    {
        if (human.Weapon == null)
            return false;

        // Check if the weapon is a BladeWeapon specifically
        if (human.Weapon is BladeWeapon bladeWeapon)
        {
            return bladeWeapon.BladesLeft >= bladeWeapon.MaxBlades;
        }

        var weaponType = human.Weapon.GetType();

        // Try different possible field/property names for blade count
        string[] possibleBladeCountNames = { "BladesLeft", "bladesLeft", "BladeCount", "bladeCount", "blades", "Blades", "currentBlades", "CurrentBlades" };
        string[] possibleMaxBladeNames = { "MaxBlades", "maxBlades", "MaxBladeCount", "maxBladeCount", "TotalBlades", "totalBlades" };

        foreach (var fieldName in possibleBladeCountNames)
        {
            var field = weaponType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(int))
            {
                int currentBlades = (int)field.GetValue(human.Weapon);

                // Try to find max blades field
                foreach (var maxFieldName in possibleMaxBladeNames)
                {
                    var maxField = weaponType.GetField(maxFieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (maxField != null && maxField.FieldType == typeof(int))
                    {
                        int maxBlades = (int)maxField.GetValue(human.Weapon);
                        return currentBlades >= maxBlades;
                    }
                }
                // If max not found, assume not at max
                return false;
            }

            var property = weaponType.GetProperty(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property != null && property.PropertyType == typeof(int) && property.CanRead)
            {
                int currentBlades = (int)property.GetValue(human.Weapon);

                // Try to find max blades property
                foreach (var maxPropName in possibleMaxBladeNames)
                {
                    var maxProperty = weaponType.GetProperty(maxPropName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (maxProperty != null && maxProperty.PropertyType == typeof(int) && maxProperty.CanRead)
                    {
                        int maxBlades = (int)maxProperty.GetValue(human.Weapon);
                        return currentBlades >= maxBlades;
                    }
                }
                // If max not found, assume not at max
                return false;
            }
        }

        // If we can't determine, assume not at max
        return false;
    }

    [PunRPC]
    private void RPC_TryGrant(int actorId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || isShrinking) return;

        if (grantsUsed >= maxGrants || (Time.time - lastGrantTime) < cooldownDuration)
            return;

        // Check if the target player is already at max blades
        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.OwnerActorNr == actorId)
            {
                if (IsAtMaxBlades(human))
                {
                    Debug.Log("Player already at maximum blade capacity, skipping grant");
                    return;
                }
                break;
            }
        }

        grantsUsed++;
        lastGrantTime = Time.time;

        photonView.RPC("RPC_SyncGrant", RpcTarget.All, grantsUsed, lastGrantTime);

        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human.photonView != null && human.photonView.OwnerActorNr == actorId)
            {
                // Try multiple approaches to add blades
                AddBladesToHuman(human);
                break;
            }
        }

        if (grantsUsed >= maxGrants && destroyWhenEmpty)
            StartCoroutine(ShrinkAndDestroy());
    }

    private void AddBladesToHuman(Human human)
    {
        if (human.Weapon == null)
        {
            Debug.LogWarning("Human has no weapon equipped");
            return;
        }

        // Check if the weapon is a BladeWeapon specifically
        if (human.Weapon is BladeWeapon bladeWeapon)
        {
            // Calculate how many blades we can actually add without exceeding max
            int bladesToAdd = Mathf.Min(bladePickup, bladeWeapon.MaxBlades - bladeWeapon.BladesLeft);

            if (bladesToAdd > 0)
            {
                bladeWeapon.BladesLeft += bladesToAdd;
                Debug.Log($"Added {bladesToAdd} blades. New total: {bladeWeapon.BladesLeft}/{bladeWeapon.MaxBlades}");
            }
            else
            {
                Debug.Log("Blades already at maximum capacity");
            }
            return;
        }

        var weaponType = human.Weapon.GetType();

        // Try different possible field/property names for blade count
        string[] possibleBladeCountNames = { "BladesLeft", "bladesLeft", "BladeCount", "bladeCount", "blades", "Blades", "currentBlades", "CurrentBlades" };
        string[] possibleMaxBladeNames = { "MaxBlades", "maxBlades", "MaxBladeCount", "maxBladeCount", "TotalBlades", "totalBlades" };

        foreach (var fieldName in possibleBladeCountNames)
        {
            var field = weaponType.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(int))
            {
                int currentBlades = (int)field.GetValue(human.Weapon);

                // Try to find max blades field
                int maxBlades = int.MaxValue; // Default to very high number if max not found
                bool foundMax = false;

                foreach (var maxFieldName in possibleMaxBladeNames)
                {
                    var maxField = weaponType.GetField(maxFieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (maxField != null && maxField.FieldType == typeof(int))
                    {
                        maxBlades = (int)maxField.GetValue(human.Weapon);
                        foundMax = true;
                        break;
                    }
                }

                // Calculate how many blades we can actually add
                int bladesToAdd = Mathf.Min(bladePickup, maxBlades - currentBlades);

                if (bladesToAdd > 0)
                {
                    field.SetValue(human.Weapon, currentBlades + bladesToAdd);
                    if (foundMax)
                        Debug.Log($"Added {bladesToAdd} blades via field '{fieldName}'. New total: {currentBlades + bladesToAdd}/{maxBlades}");
                    else
                        Debug.Log($"Added {bladesToAdd} blades via field '{fieldName}'. New total: {currentBlades + bladesToAdd}");
                }
                else
                {
                    Debug.Log("Blades already at maximum capacity");
                }
                return;
            }

            var property = weaponType.GetProperty(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (property != null && property.PropertyType == typeof(int) && property.CanWrite)
            {
                int currentBlades = (int)property.GetValue(human.Weapon);

                // Try to find max blades property
                int maxBlades = int.MaxValue;
                bool foundMax = false;

                foreach (var maxPropName in possibleMaxBladeNames)
                {
                    var maxProperty = weaponType.GetProperty(maxPropName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (maxProperty != null && maxProperty.PropertyType == typeof(int) && maxProperty.CanRead)
                    {
                        maxBlades = (int)maxProperty.GetValue(human.Weapon);
                        foundMax = true;
                        break;
                    }
                }

                // Calculate how many blades we can actually add
                int bladesToAdd = Mathf.Min(bladePickup, maxBlades - currentBlades);

                if (bladesToAdd > 0)
                {
                    property.SetValue(human.Weapon, currentBlades + bladesToAdd);
                    if (foundMax)
                        Debug.Log($"Added {bladesToAdd} blades via property '{fieldName}'. New total: {currentBlades + bladesToAdd}/{maxBlades}");
                    else
                        Debug.Log($"Added {bladesToAdd} blades via property '{fieldName}'. New total: {currentBlades + bladesToAdd}");
                }
                else
                {
                    Debug.Log("Blades already at maximum capacity");
                }
                return;
            }
        }

        // If direct field/property access doesn't work, try calling a method
        string[] possibleMethodNames = { "AddBlades", "AddAmmo", "Reload", "RefillBlades", "Reset" };
        foreach (var methodName in possibleMethodNames)
        {
            var method = weaponType.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    // Methods like Reset() that restore blades without parameters
                    // These typically handle their own max limits internally
                    method.Invoke(human.Weapon, null);
                    Debug.Log($"Called method '{methodName}' to restore blades");
                    return;
                }
                else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                {
                    // Methods that take an int parameter for blade count
                    // We'll let the method handle its own max limits
                    method.Invoke(human.Weapon, new object[] { bladePickup });
                    Debug.Log($"Called method '{methodName}' to add {bladePickup} blades");
                    return;
                }
            }
        }

        Debug.LogWarning($"Could not find blade count field/property/method in weapon of type: {weaponType}");
    }

    [PunRPC]
    private void RPC_SyncGrant(int used, float lastTime)
    {
        grantsUsed = used;
        lastGrantTime = lastTime;
    }

    private IEnumerator ShrinkAndDestroy()
    {
        isShrinking = true;

        if (localHuman != null && localHuman.photonView.IsMine)
        {
            ClearPrompt();
            isInside = false;
            localHuman = null;
        }

        Vector3 originalScale = transform.localScale;
        float timer = 0f;

        while (timer < shrinkAndDestroyTime)
        {
            float t = timer / shrinkAndDestroyTime;
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localScale = Vector3.zero;

        if (photonView != null && photonView.IsMine)
            PhotonNetwork.Destroy(gameObject);
        else
            Destroy(gameObject);
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