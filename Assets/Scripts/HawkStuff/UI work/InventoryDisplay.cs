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
    private InGameMenu _inGameMenu; // Reference to the main menu system

    // Added display name dictionary (only change)
    private readonly Dictionary<string, string> _itemDisplayNames = new Dictionary<string, string>()
    {
        {"Wagon1", "Support Wagon"},
        {"Wagon2", "Resupply Wagon"},
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
            // Only close with Escape key to prevent accidental closing
            ToggleInventoryDisplay();
        }
    }

    private void ToggleInventoryDisplay()
    {
        _localHuman = FindLocalHuman();
        _inventory = _localHuman != null ? _localHuman.GetComponent<HumanInventory>() : null;
        _stats = _localHuman != null ? _localHuman.Stats : null;
        _showInventory = !_showInventory;

        // Set the in-menu state
        if (_inGameMenu != null)
        {
            _inGameMenu.SetInventoryMenuActive(_showInventory);
        }

        // Optional: Lock cursor when inventory is open
        Cursor.visible = _showInventory;
        Cursor.lockState = _showInventory ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnGUI()
    {
        if (!_showInventory || _inventory == null || _stats == null)
            return;

        // First, draw the stats panel (top fixed)
        float topX = 20f;
        float topY = 20f;
        float boxWidth = 220f;
        float statsBoxHeight = 130f;

        GUI.Box(new Rect(topX, topY, boxWidth, statsBoxHeight), "Stats");
        GUI.Label(new Rect(topX + 10, topY + 30, 200, 20), $"Speed: {_stats.Speed}");
        GUI.Label(new Rect(topX + 10, topY + 50, 200, 20), $"Gas: {_stats.Gas}");
        GUI.Label(new Rect(topX + 10, topY + 70, 200, 20), $"Ammo: {_stats.Ammunition}");
        GUI.Label(new Rect(topX + 10, topY + 90, 200, 20), $"Accel: {_stats.Acceleration}");
        GUI.Label(new Rect(topX + 10, topY + 110, 200, 20), $"HorseSpeed: {_stats.HorseSpeed}");

        // Then, draw the inventory panel below it
        List<string> items = _inventory.GetItemTypes();
        int itemCount = items.Count;
        int inventoryHeight = 30 + itemCount * 20;

        float inventoryY = topY + statsBoxHeight + 20;
        GUI.Box(new Rect(topX, inventoryY, boxWidth, inventoryHeight), "Inventory");

        for (int i = 0; i < itemCount; i++)
        {
            string item = items[i];
            string displayName = _itemDisplayNames.ContainsKey(item) ? _itemDisplayNames[item] : item;
            int count = _inventory.GetItemCount(item);
            GUI.Label(new Rect(topX + 10, inventoryY + 20 + i * 20, 200, 20), $"{displayName}: {count}");
        }
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