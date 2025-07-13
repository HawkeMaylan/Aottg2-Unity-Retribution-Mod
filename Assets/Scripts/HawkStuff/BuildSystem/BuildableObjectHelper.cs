using UnityEngine;

[System.Serializable]
public class InventoryCost
{
    public string itemName;  // Must match inventory item names exactly
    public int amount;
}

public class BuildableObjectHelper : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject preview;
    public GameObject collisionCheckObject;
    public Sprite menuIcon;
    public string category;
    public string displayName;

    [Header("Build Settings")]
    public float gridSize = 1.0f;
    public float offset = 1.0f;

    [Header("Inventory Cost")]
    public InventoryCost[] buildCosts;

    public string GetCostString()
    {
        if (buildCosts == null || buildCosts.Length == 0)
            return "Free";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var cost in buildCosts)
        {
            sb.AppendLine($"{cost.itemName}: {cost.amount}");
        }
        return sb.ToString().Trim();
    }
}