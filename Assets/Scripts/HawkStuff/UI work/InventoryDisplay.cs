using UnityEngine;
using Characters;

public class InventoryDisplay : MonoBehaviour
{
    private bool _showInventory = false;
    private Human _localHuman;
    private HumanInventory _inventory;
    private HumanStats _stats;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            ToggleInventoryDisplay();
        }
        else if (_showInventory && Input.anyKeyDown)
        {
            _showInventory = false;
        }
    }

    private void ToggleInventoryDisplay()
    {
        _localHuman = FindLocalHuman();
        _inventory = _localHuman != null ? _localHuman.GetComponent<HumanInventory>() : null;
        _stats = _localHuman != null ? _localHuman.Stats : null;
        _showInventory = !_showInventory;
    }

    private void OnGUI()
    {
        if (!_showInventory || _inventory == null || _stats == null)
            return;

        // Inventory Box
        GUI.Box(new Rect(20, 20, 220, 100), "Inventory");
        GUI.Label(new Rect(30, 50, 200, 20), $"Cannons: {_inventory.cannonCount}");
        GUI.Label(new Rect(30, 70, 200, 20), $"Wagon1: {_inventory.wagon1Count}");
        GUI.Label(new Rect(30, 90, 200, 20), $"Wagon2: {_inventory.wagon2Count}");

        // Stats Box (~10 lines lower)
        float offsetY = 140;
        GUI.Box(new Rect(20, offsetY, 220, 130), "Stats");
        GUI.Label(new Rect(30, offsetY + 30, 200, 20), $"Speed: {_stats.Speed}");
        GUI.Label(new Rect(30, offsetY + 50, 200, 20), $"Gas: {_stats.Gas}");
        GUI.Label(new Rect(30, offsetY + 70, 200, 20), $"Ammo: {_stats.Ammunition}");
        GUI.Label(new Rect(30, offsetY + 90, 200, 20), $"Accel: {_stats.Acceleration}");
        GUI.Label(new Rect(30, offsetY + 110, 200, 20), $"HorseSpeed: {_stats.HorseSpeed}");
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
