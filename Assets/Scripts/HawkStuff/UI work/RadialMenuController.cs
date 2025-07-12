using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class RadialMenuController : MonoBehaviour
{
    [Header("Configuration")]
    public int segmentsPerPage = 8;
    public float radius = 200f;
    public float iconSize = 50f;
    public float deadZone = 0.2f;
    public KeyCode toggleKey = KeyCode.Tab;
    public bool useMouseSelection = true;

    [Header("UI References")]
    public GameObject radialMenuBase;
    public RectTransform selectionIndicator;
    public Text pageDisplayText;
    public Text selectionNameText;

    [Header("Popup Settings")]
    public GameObject textPopupPrefab;
    public float textDuration = 2f;
    public float textFadeDuration = 0.5f;
    public Vector2 popupOffset = new Vector2(0, 50f);

    [Header("Pages")]
    public List<RadialMenuPage> pages = new List<RadialMenuPage>();

    private int currentPage = 0;
    private bool menuActive = false;
    private int currentSelection = -1;
    private Vector2 inputDirection;
    private Queue<GameObject> _activeTextPopups = new Queue<GameObject>();
    private Transform _textPopupParent;

    void Awake()
    {
        // Create text popup parent
        _textPopupParent = new GameObject("RadialMenuTextPopups").transform;
        _textPopupParent.SetParent(radialMenuBase.transform);
        RectTransform rt = _textPopupParent.gameObject.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }

        if (!menuActive) return;

        GetInputDirection();
        UpdateSelection();
        HandleSelectionInput();
    }

    void ToggleMenu()
    {
        menuActive = !menuActive;
        radialMenuBase.SetActive(menuActive);

        if (menuActive)
        {
            Time.timeScale = 0f;
            UpdateMenuDisplay();
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    void GetInputDirection()
    {
        inputDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        if (useMouseSelection && inputDirection.magnitude < deadZone)
        {
            Vector2 mousePos = Input.mousePosition;
            Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
            inputDirection = (mousePos - center).normalized;
        }
    }

    void UpdateSelection()
    {
        if (inputDirection.magnitude < deadZone)
        {
            if (currentSelection != -1)
            {
                currentSelection = -1;
                selectionIndicator.gameObject.SetActive(false);
                selectionNameText.text = "";
            }
            return;
        }

        float angle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        int newSelection = Mathf.FloorToInt(angle / (360f / segmentsPerPage));

        if (newSelection != currentSelection)
        {
            currentSelection = newSelection;
            UpdateSelectionVisual();
        }
    }

    void UpdateSelectionVisual()
    {
        if (currentSelection < 0 || currentSelection >= segmentsPerPage) return;

        float angle = (currentSelection * (360f / segmentsPerPage) + (360f / segmentsPerPage / 2)) * Mathf.Deg2Rad;
        Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        selectionIndicator.anchoredPosition = pos;
        selectionIndicator.gameObject.SetActive(true);

        if (currentPage < pages.Count && currentSelection < pages[currentPage].options.Count)
        {
            selectionNameText.text = pages[currentPage].options[currentSelection].optionName;
            ShowTextPopup(pages[currentPage].options[currentSelection].optionName);
        }
    }

    void ShowTextPopup(string message)
    {
        if (textPopupPrefab == null) return;

        GameObject popup = Instantiate(textPopupPrefab, _textPopupParent);
        popup.transform.localPosition = popupOffset;

        Text text = popup.GetComponentInChildren<Text>();
        if (text != null)
            text.text = message;

        _activeTextPopups.Enqueue(popup);
        StartCoroutine(FadeAndDestroyText(popup, textDuration, textFadeDuration));
    }

    private IEnumerator FadeAndDestroyText(GameObject popup, float totalDuration, float fadeDuration)
    {
        yield return new WaitForSeconds(totalDuration - fadeDuration);

        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = popup.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        if (_activeTextPopups.Contains(popup))
        {
            _activeTextPopups.Dequeue();
            Destroy(popup);
        }
    }

    void HandleSelectionInput()
    {
        if (currentSelection == -1) return;

        if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
        {
            if (currentPage < pages.Count && currentSelection < pages[currentPage].options.Count)
            {
                pages[currentPage].options[currentSelection].onSelect.Invoke();
                ToggleMenu();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            PreviousPage();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            NextPage();
        }
    }

    void NextPage()
    {
        currentPage = (currentPage + 1) % pages.Count;
        UpdateMenuDisplay();
    }

    void PreviousPage()
    {
        currentPage--;
        if (currentPage < 0) currentPage = pages.Count - 1;
        UpdateMenuDisplay();
    }

    void UpdateMenuDisplay()
    {
        foreach (Transform child in radialMenuBase.transform)
        {
            if (child != selectionIndicator && child.gameObject != selectionNameText.gameObject && child != _textPopupParent)
                Destroy(child.gameObject);
        }

        pageDisplayText.text = $"{currentPage + 1}/{pages.Count}";

        if (currentPage >= pages.Count) return;

        for (int i = 0; i < pages[currentPage].options.Count; i++)
        {
            if (i >= segmentsPerPage) break;

            float angle = (i * (360f / segmentsPerPage) + (360f / segmentsPerPage / 2)) * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            // Create icon
            GameObject icon = new GameObject($"Option_{i}");
            RectTransform rt = icon.AddComponent<RectTransform>();
            rt.SetParent(radialMenuBase.transform);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(iconSize, iconSize);

            // Add image
            Image img = icon.AddComponent<Image>();
            img.sprite = pages[currentPage].options[i].icon;

            // Add text label below icon
            GameObject label = new GameObject($"Label_{i}");
            RectTransform labelRt = label.AddComponent<RectTransform>();
            labelRt.SetParent(icon.transform);
            labelRt.anchoredPosition = new Vector2(0, -iconSize);
            labelRt.sizeDelta = new Vector2(100, 30);

            // Standard Text component
            Text labelText = label.AddComponent<Text>();
            labelText.text = pages[currentPage].options[i].optionName;
            labelText.alignment = TextAnchor.UpperCenter;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
        }
    }
}

[System.Serializable]
public class RadialMenuPage
{
    public string pageName;
    public List<RadialMenuOption> options = new List<RadialMenuOption>();
}

[System.Serializable]
public class RadialMenuOption
{
    public string optionName;
    public Sprite icon;
    public bool showPopup = true;
    public UnityEngine.Events.UnityEvent onSelect;
}