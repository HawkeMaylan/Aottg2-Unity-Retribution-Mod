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
    [Tooltip("Which axis should face forward when forceUpAlignment is enabled")]
    public AlignmentAxis forwardAxis = AlignmentAxis.Z;
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

    /// <summary>
    /// Gets the world-space vector for the specified alignment axis
    /// </summary>
    public Vector3 GetAlignmentVector(AlignmentAxis axis)
    {
        switch (axis)
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

    /// <summary>
    /// Gets the rotation that forces the specified axes to align with world directions
    /// </summary>
    public Quaternion GetForcedRotation()
    {
        Vector3 up = GetAlignmentVector(forcedUpAxis);
        Vector3 forward = GetAlignmentVector(forwardAxis);

        // Ensure forward is perpendicular to up
        if (Mathf.Abs(Vector3.Dot(up, forward)) > 0.1f)
        {
            // Find an alternative forward that's perpendicular
            if (Mathf.Abs(up.x) < 0.9f) forward = Vector3.right;
            else if (Mathf.Abs(up.y) < 0.9f) forward = Vector3.up;
            else forward = Vector3.forward;

            // Make sure it's actually perpendicular
            forward = Vector3.Cross(up, Vector3.Cross(forward, up)).normalized;
        }

        return Quaternion.LookRotation(forward, up);
    }

    /// <summary>
    /// Gets a formatted string showing all build costs
    /// </summary>
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

    /// <summary>
    /// Validates the rotation settings in the editor
    /// </summary>
    private void OnValidate()
    {
        if (forceUpAlignment)
        {
            // Ensure forward axis isn't parallel to up axis
            if (GetAlignmentVector(forcedUpAxis) == GetAlignmentVector(forwardAxis))
            {
                // Default to a perpendicular axis
                if (forcedUpAxis == AlignmentAxis.X || forcedUpAxis == AlignmentAxis.NegativeX)
                    forwardAxis = AlignmentAxis.Z;
                else
                    forwardAxis = AlignmentAxis.X;
            }
        }
    }
}