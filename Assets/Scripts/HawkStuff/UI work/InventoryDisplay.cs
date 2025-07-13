using UnityEngine;
using Characters;
using System.Collections.Generic;
using UI;

public class InventoryDisplay : MonoBehaviour
{
    private bool _showInventory = false;
    private Human _localHuman;
    private HumanInventory _inventory;
    private HumanStats _stats;
    private InGameMenu _inGameMenu;

    // Display name dictionary (matches ItemPopupManager)
    private readonly Dictionary<string, string> _itemDisplayNames = new Dictionary<string, string>()
    {
        {"Wagon1", "Small Wagon"},
        {"Wagon2", "Large Wagon"},
        {"Cannon", "Cannon"},
        {"CannonGround", "Ground Cannon"},
        {"WallCannon", "Wall Cannon"},
        {"GasBomb", "Gas Bomb"},
        {"CannonTest", "Test Cannon"}
    };

    private void Start()
    {
        _inGameMenu = (InGameMenu)UIManager.CurrentMenu;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            ToggleInventoryDisplay();
        }
        else if (_showInventory && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleInventoryDisplay();
        }
    }

    private void ToggleInventoryDisplay()
    {
        _localHuman = FindLocalHuman();
        _inventory = _localHuman?.GetComponent<HumanInventory>();
        _stats = _localHuman?.Stats;
        _showInventory = !_showInventory;

        if (_inGameMenu != null)
        {
            _inGameMenu.SetInventoryMenuActive(_showInventory);
        }

        // Update cursor state
        Cursor.visible = _showInventory;
        Cursor.lockState = _showInventory ? CursorLockMode.None : CursorLockMode.Locked;

        // Refresh inventory when opening
        if (_showInventory && _inventory != null)
        {
            _inventory.LogInventoryState(); // Debug output
        }
    }

    private void OnGUI()
    {
        if (!_showInventory || _inventory == null || _stats == null)
            return;

        // Style setup
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft
        };

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            richText = true
        };

        // Layout parameters
        float topX = 20f;
        float topY = 20f;
        float boxWidth = 240f;
        float statsBoxHeight = 130f;
        float padding = 10f;

        // 1. Draw stats panel
        GUI.Box(new Rect(topX, topY, boxWidth, statsBoxHeight), "<b>Character Stats</b>", boxStyle);

        float labelY = topY + padding + 10f;
        DrawStatLabel(labelStyle, topX + padding, labelY, "Speed:", _stats.Speed.ToString("F1"));
        labelY += 20f;
        DrawStatLabel(labelStyle, topX + padding, labelY, "Gas:", _stats.Gas.ToString("F1"));
        labelY += 20f;
        DrawStatLabel(labelStyle, topX + padding, labelY, "Ammo:", _stats.Ammunition.ToString());
        labelY += 20f;
        DrawStatLabel(labelStyle, topX + padding, labelY, "Acceleration:", _stats.Acceleration.ToString("F1"));
        labelY += 20f;
        DrawStatLabel(labelStyle, topX + padding, labelY, "Horse Speed:", _stats.HorseSpeed.ToString("F1"));

        // 2. Draw inventory panel
        List<string> items = _inventory.GetItemTypes();
        float inventoryHeight = 30f + (items.Count * 22f);
        float inventoryY = topY + statsBoxHeight + 15f;

        GUI.Box(new Rect(topX, inventoryY, boxWidth, inventoryHeight), "<b>Inventory</b>", boxStyle);

        for (int i = 0; i < items.Count; i++)
        {
            string item = items[i];
            string displayName = GetDisplayName(item);
            int count = _inventory.GetItemCount(item);

            float itemY = inventoryY + padding + 10f + (i * 22f);
            GUI.Label(
                new Rect(topX + padding, itemY, boxWidth - (2 * padding), 20f),
                $"{displayName}: <color=#FFD700>{count}</color>",
                labelStyle
            );
        }
    }

    private void DrawStatLabel(GUIStyle style, float x, float y, string label, string value)
    {
        GUI.Label(new Rect(x, y, 100, 20), label, style);
        GUI.Label(new Rect(x + 100, y, 100, 20), value, style);
    }

    private string GetDisplayName(string internalName)
    {
        return _itemDisplayNames.TryGetValue(internalName, out string displayName)
            ? displayName
            : internalName;
    }

    private Human FindLocalHuman()
    {
        foreach (var human in FindObjectsOfType<Human>())
        {
            if (human != null && human.IsMine())
                return human;
        }
        return null;
    }
}