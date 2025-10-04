using UnityEngine;
using Photon.Pun;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class ProximityChatText : MonoBehaviourPun
{
    [Header("Chat Settings")]
    public float messageDuration = 3f;
    public float fadeTime = 1f;

    [Header("Billboard Settings")]
    public Camera referenceCamera;

    private TextMesh textMesh;
    private float fadeTimer = -1f;
    private Color baseColor;

    private void Awake()
    {
        // Get the TextMesh component attached to this object
        textMesh = GetComponent<TextMesh>();

        if (textMesh == null)
        {
            Debug.LogError("ProximityChatText requires a TextMesh component on the same GameObject!");
            return;
        }

        // Initialize with empty text
        textMesh.text = "";
        baseColor = textMesh.color;

        // Get camera reference
        if (referenceCamera == null)
            referenceCamera = Camera.main;
    }

    // Public function to be called from outside sources
    public void SetMessage(string newMessage)
    {
        if (textMesh == null || string.IsNullOrEmpty(newMessage)) return;

        Debug.Log($"Setting proximity chat message: {newMessage}");

        // Update locally
        ShowMessage(newMessage);

        // Sync with other players
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_ShowMessage", RpcTarget.Others, newMessage);
        }
    }

    [PunRPC]
    private void RPC_ShowMessage(string message)
    {
        Debug.Log($"RPC received for proximity chat: {message}");
        ShowMessage(message);
    }

    private void ShowMessage(string message)
    {
        textMesh.text = message;
        textMesh.color = baseColor;
        fadeTimer = fadeTime;

        Debug.Log($"TextMesh text set to: {textMesh.text}");
    }

    private void FixedUpdate()
    {
        if (textMesh == null) return;

        // Billboard effect - always face the camera
        if (referenceCamera == null)
            referenceCamera = Camera.main;

        if (referenceCamera != null)
        {
            // Make the text face the camera while maintaining up direction
            transform.rotation = Quaternion.LookRotation(transform.position - referenceCamera.transform.position);

            // Alternative method if you want to maintain world up:
            // transform.LookAt(2 * transform.position - referenceCamera.transform.position);
        }

        // Handle fade out
        if (fadeTimer > 0f)
        {
            fadeTimer -= Time.fixedDeltaTime;

            if (fadeTimer <= fadeTime)
            {
                float alpha = Mathf.Clamp01(fadeTimer / fadeTime);
                textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            if (fadeTimer <= 0f)
            {
                textMesh.text = "";
            }
        }
    }

    // Debug method to check component status
    private void Start()
    {
        Debug.Log($"ProximityChatText initialized - TextMesh: {textMesh != null}, PhotonView: {photonView != null}, IsMine: {photonView.IsMine}");
    }
}