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

    [Header("Rotation Settings")]
    [Tooltip("Should this object force a specific axis to align with world up?")]
    public bool forceUpAlignment = false;
    [Tooltip("Which axis should point upward when forceUpAlignment is enabled")]
    public AlignmentAxis forcedUpAxis = AlignmentAxis.Y;
    [Tooltip("Should the object snap to surface normals?")]
    public bool snapToSurface = true;

    [Header("Particle Effects")]
    [Tooltip("Drag particle effect prefab here (must be in Resources folder)")]
    public GameObject buildParticleEffectPrefab;
    [Tooltip("Offset from build position")]
    public Vector3 particleEffectOffset = Vector3.zero;
    [Tooltip("Should particles use the building's rotation?")]
    public bool particleUsePreviewRotation = true;
    [Tooltip("Should particles be parented to the building?")]
    public bool particleParentToBuilding = false;

    [Header("Inventory Cost")]
    public InventoryCost[] buildCosts;

    public enum AlignmentAxis { X, Y, Z, NegativeX, NegativeY, NegativeZ }

    public Vector3 GetForcedUpVector()
    {
        switch (forcedUpAxis)
        {
            case AlignmentAxis.X: return Vector3.right;
            case AlignmentAxis.Y: return Vector3.up;
            case AlignmentAxis.Z: return Vector3.forward;
            case AlignmentAxis.NegativeX: return Vector3.left;
            case AlignmentAxis.NegativeY: return Vector3.down;
            case AlignmentAxis.NegativeZ: return Vector3.back;
            default: return Vector3.up;
        }
    }

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