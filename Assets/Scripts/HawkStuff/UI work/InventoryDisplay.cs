using UnityEngine;
using Characters;

public class InventoryDisplay : MonoBehaviour
{
    private bool _showInventory = false;
    private Human _localHuman;
    private HumanInventory _inventory;

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
        _showInventory = !_showInventory;
    }

    private void OnGUI()
    {
        if (!_showInventory || _inventory == null)
            return;

        GUI.Box(new Rect(20, 20, 220, 100), "Inventory");

        GUI.Label(new Rect(30, 50, 200, 20), $"Cannons: {_inventory.cannonCount}");
        GUI.Label(new Rect(30, 70, 200, 20), $"Wagon1: {_inventory.wagon1Count}");
        GUI.Label(new Rect(30, 90, 200, 20), $"Wagon2: {_inventory.wagon2Count}");
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
