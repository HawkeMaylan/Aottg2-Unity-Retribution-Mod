using UnityEngine;
using UnityEngine.UI;
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
    public Text selectionNameText;
    public Text pageNameText; // For displaying the page name
    public Text pageNumberText; // For displaying "Page X of Y"

    [Header("Pages")]
    public List<RadialMenuPage> pages = new List<RadialMenuPage>();

    private int currentPage = 0;
    private bool menuActive = false;
    private int currentSelection = -1;
    private Vector2 inputDirection;

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
        int optionsOnPage = Mathf.Min(pages[currentPage].options.Count, segmentsPerPage);

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

        float segmentAngle = 360f / optionsOnPage;
        float angle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        int newSelection = Mathf.FloorToInt(angle / segmentAngle);

        if (newSelection != currentSelection)
        {
            currentSelection = newSelection;
            UpdateSelectionVisual();
        }
    }

    void UpdateSelectionVisual()
    {
        int optionsOnPage = Mathf.Min(pages[currentPage].options.Count, segmentsPerPage);

        if (currentSelection < 0 || currentSelection >= optionsOnPage) return;

        float segmentAngle = 360f / optionsOnPage;
        float angle = (currentSelection * segmentAngle + (segmentAngle / 2)) * Mathf.Deg2Rad;
        Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        selectionIndicator.anchoredPosition = pos;
        selectionIndicator.gameObject.SetActive(true);

        if (currentPage < pages.Count && currentSelection < pages[currentPage].options.Count)
        {
            selectionNameText.text = pages[currentPage].options[currentSelection].optionName;
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
        // Clear old menu items (preserving the UI elements we want to keep)
        foreach (Transform child in radialMenuBase.transform)
        {
            if (child != selectionIndicator &&
                child.gameObject != selectionNameText.gameObject &&
                child.gameObject != pageNameText.gameObject &&
                child.gameObject != pageNumberText.gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        // Update page information display
        pageNameText.text = pages[currentPage].pageName; // Show the page name
        pageNumberText.text = $"Page {currentPage + 1} of {pages.Count}"; // Show page numbers

        if (currentPage >= pages.Count) return;

        int optionsOnPage = Mathf.Min(pages[currentPage].options.Count, segmentsPerPage);
        float segmentAngle = 360f / optionsOnPage;

        for (int i = 0; i < optionsOnPage; i++)
        {
            float angle = (i * segmentAngle + (segmentAngle / 2)) * Mathf.Deg2Rad;
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
    public string pageName; // This will be displayed at the top
    public List<RadialMenuOption> options = new List<RadialMenuOption>();
}

[System.Serializable]
public class RadialMenuOption
{
    public string optionName;
    public Sprite icon;
    public UnityEngine.Events.UnityEvent onSelect;
}