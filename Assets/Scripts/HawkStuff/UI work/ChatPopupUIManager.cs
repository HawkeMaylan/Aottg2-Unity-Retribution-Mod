using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Characters;
using GameManagers;
using ApplicationManagers;
using Settings;
using UI;
using Utility;
using System.Collections;

public class ChatPopupUIManager : MonoBehaviourPun
{
    private GameObject panel;
    private InputField chatInput;
    private Button sendButton;
    private InGameManager gameManager;

    private void Awake()
    {
        gameManager = SceneLoader.CurrentGameManager as InGameManager;
        if (gameManager == null)
        {
            Debug.LogError("ChatPopupUIManager: Not in InGameManager scene.");
            enabled = false;
            return;
        }

        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        CreateChatUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Slash))
            ToggleChatPanel();

        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Return))
            SendChatPopup();
    }

    private void ToggleChatPanel()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf)
        {
            chatInput.text = "";
            chatInput.Select();
            chatInput.ActivateInputField();
        }
    }

    private void SendChatPopup()
    {
        string msg = chatInput.text.Trim();
        if (string.IsNullOrEmpty(msg)) return;

        BaseCharacter character = gameManager.CurrentCharacter;
        if (character != null)
        {
            PhotonView pv = character.Cache.PhotonView;
            photonView.RPC("EmoteTextRPC", RpcTarget.All, pv.ViewID, msg);
        }

        chatInput.text = "";
        panel.SetActive(false);
    }

    [PunRPC]
    public void EmoteTextRPC(int viewId, string message)
    {
        if (!SettingsManager.UISettings.ShowEmotes.Value)
            return;

        BaseCharacter character = Util.FindCharacterByViewId(viewId);
        if (character == null) return;

        StartCoroutine(SpawnFloatingText(character.Cache.Transform, message));
    }

    private IEnumerator SpawnFloatingText(Transform target, string message)
    {
        GameObject canvasGO = GameObject.Find("DefaultMenu(Clone)");
        if (canvasGO == null) yield break;

        GameObject textGO = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGO.transform.SetParent(canvasGO.transform, false);

        Text textComp = textGO.GetComponent<Text>();
        textComp.text = message;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 20;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 50);

        float duration = 2.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (target == null)
                break;

            Vector3 worldPos = target.position + Vector3.up * 2.5f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            rect.position = screenPos;

            yield return null;
        }

        Destroy(textGO);
    }

    private void CreateChatUI()
    {
        GameObject canvasGO = GameObject.Find("DefaultMenu(Clone)");
        if (canvasGO == null)
        {
            Debug.LogWarning("ChatPopupUIManager: DefaultMenu canvas not found.");
            return;
        }

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("ChatPopupUIManager: DefaultMenu has no Canvas.");
            return;
        }

        panel = new GameObject("ChatPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.1f);
        panelRect.anchorMax = new Vector2(0.7f, 0.2f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject inputGO = new GameObject("ChatInput", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputGO.transform.SetParent(panel.transform, false);
        RectTransform inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0f);
        inputRect.anchorMax = new Vector2(0.8f, 1f);
        inputRect.offsetMin = new Vector2(10, 10);
        inputRect.offsetMax = new Vector2(-10, -10);

        Image inputImage = inputGO.GetComponent<Image>();
        inputImage.color = Color.white;

        chatInput = inputGO.GetComponent<InputField>();
        chatInput.textComponent = CreateUIText(chatInput.transform, "InputText", TextAnchor.MiddleLeft, 14);
        chatInput.placeholder = CreateUIText(chatInput.transform, "Placeholder", TextAnchor.MiddleLeft, 14, "<Type Message>");
        chatInput.lineType = InputField.LineType.SingleLine;

        GameObject buttonGO = new GameObject("SendButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.8f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.offsetMin = new Vector2(5, 10);
        buttonRect.offsetMax = new Vector2(-10, -10);

        Image buttonImage = buttonGO.GetComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.8f, 0.8f);

        sendButton = buttonGO.GetComponent<Button>();
        sendButton.onClick.AddListener(SendChatPopup);
        CreateUIText(sendButton.transform, "SendText", TextAnchor.MiddleCenter, 14, "Send");

        panel.SetActive(false);
    }

    private Text CreateUIText(Transform parent, string name, TextAnchor alignment, int fontSize, string text = "")
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(parent, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text uiText = textGO.GetComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.alignment = alignment;
        uiText.color = Color.white;

        return uiText;
    }
}
