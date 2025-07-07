using UnityEngine;

public class BuildableObjectHelper : MonoBehaviour
{
    public GameObject preview; // The preview object for this buildable prefab
    public GameObject collisionCheckObject; // The child object used for collision checks
    public float gridSize = 1.0f; // Grid size for this object
    public float offset = 1.0f; // Offset for this object
}