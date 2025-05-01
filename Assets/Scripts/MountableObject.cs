using UnityEngine;
using Characters;
using Photon.Pun;
using UI;
using Settings;
using GameManagers;
using ApplicationManagers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class DirectMountBundled : MonoBehaviourPunCallbacks
{
    [Header("Mount Target")]
    public Transform mountPoint;
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    [Header("Prompt Texts")]
    public string mountPromptText = "Press G to Mount";
    public string unmountPromptText = "Press G to Unmount";

    [Header("Unmount Prompt Settings")]
    public float unmountPromptDuration = 5f;

    private Human humanInTrigger;
    private bool isMounted = false;
    private bool hasExitedAfterUnmount = false;

    private static string currentPrompt = "";
    private float unmountPromptTimer = 0f;

    private void Start()
    {
        ClearPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human.IsMine())
        {
            humanInTrigger = human;
            hasExitedAfterUnmount = false;
            SetPrompt(mountPromptText);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && human == humanInTrigger)
        {
            if (!isMounted)
            {
                hasExitedAfterUnmount = true;
                humanInTrigger = null;
                ClearPrompt();
            }
        }
    }

    private void Update()
    {
        HandleMountInput();
        HandleUnmountPromptTimer();
    }

    private void HandleMountInput()
    {
        if (humanInTrigger == null)
            return;

        //  Exactly like ItemHandler: Only block key input if InMenu or Chat active
        if (!InGameMenu.InMenu() && !ChatManager.IsChatActive())
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (!isMounted && !hasExitedAfterUnmount)
                    AttachHuman();
                else if (isMounted)
                    DetachHuman();
            }
        }
    }

    private void HandleUnmountPromptTimer()
    {
        if (isMounted && unmountPromptTimer > 0f)
        {
            unmountPromptTimer -= Time.deltaTime;
            if (unmountPromptTimer <= 0f)
                ClearPrompt();
        }
    }

    private void AttachHuman()
    {
        if (humanInTrigger == null || mountPoint == null)
            return;

        humanInTrigger.MountedTransform = mountPoint;
        humanInTrigger.MountedMapObject = null;
        humanInTrigger.MountedPositionOffset = positionOffset;
        humanInTrigger.MountedRotationOffset = rotationOffset;
        humanInTrigger.MountState = HumanMountState.MapObject;
        humanInTrigger.SetInterpolation(false);

        isMounted = true;
        hasExitedAfterUnmount = false;

        SetPrompt(unmountPromptText);
        unmountPromptTimer = unmountPromptDuration;
    }

    private void DetachHuman()
    {
        if (humanInTrigger == null)
            return;

        humanInTrigger.Unmount(true);

        isMounted = false;

        if (humanInTrigger != null && !hasExitedAfterUnmount)
        {
            SetPrompt(mountPromptText);
            unmountPromptTimer = 0f;
        }
        else
        {
            ClearPrompt();
        }
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(currentPrompt))
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 100, 300, 50), currentPrompt, style);
        }
    }

    private void SetPrompt(string text)
    {
        currentPrompt = text;
    }

    private void ClearPrompt()
    {
        currentPrompt = "";
    }
}
