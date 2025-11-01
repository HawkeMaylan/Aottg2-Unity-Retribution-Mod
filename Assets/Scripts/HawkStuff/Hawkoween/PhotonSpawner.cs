using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PhotonSpawner : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    public string objectPrefabPath = "Buildables/";
    public string objectToSpawnName;
    public Transform spawnPoint;
    public float spawnCooldown = 2f;

    [Header("Velocity Settings")]
    public float minSpeed = 5f;
    public float maxSpeed = 15f;
    public float minAngle = -45f;
    public float maxAngle = 45f;

    private float lastSpawnTime;
    private float currentAngle;
    private float currentSpeed;
    private PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        if (!PhotonNetwork.IsMasterClient)
        {
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Handle spawning
        if (Time.time >= lastSpawnTime + spawnCooldown)
        {
            SpawnObject();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnObject()
    {
        if (string.IsNullOrEmpty(objectToSpawnName) || spawnPoint == null) return;

        // Randomize angle and speed
        currentAngle = Random.Range(minAngle, maxAngle);
        currentSpeed = Random.Range(minSpeed, maxSpeed);

        // Spawn object
        string fullPrefabPath = objectPrefabPath + objectToSpawnName;
        GameObject spawnedObject = PhotonNetwork.Instantiate(
            fullPrefabPath,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // Apply velocity
        if (spawnedObject != null)
        {
            Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 velocity = CalculateVelocity(currentAngle, currentSpeed);
                rb.velocity = velocity;

                PhotonView objectPhotonView = spawnedObject.GetComponent<PhotonView>();
                if (objectPhotonView != null)
                {
                    objectPhotonView.RPC("SetVelocity", RpcTarget.Others, velocity);
                }
            }
        }
    }

    Vector3 CalculateVelocity(float angle, float speed)
    {
        Vector3 baseDirection = spawnPoint.forward;
        Vector3 direction = Quaternion.AngleAxis(angle, spawnPoint.up) * baseDirection;
        return direction * speed;
    }

    // Simple debug visualization - only shows in Scene view
    void OnDrawGizmos()
    {
        if (spawnPoint == null) return;

        // Draw spawn point
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(spawnPoint.position, 0.2f);

        // Draw current firing angle
        Gizmos.color = Color.white;
        Vector3 currentDir = CalculateVelocity(currentAngle, 2f);
        Gizmos.DrawRay(spawnPoint.position, currentDir);

        // Draw angle range
        Gizmos.color = Color.red;
        Vector3 minDir = CalculateVelocity(minAngle, 1.5f);
        Gizmos.DrawRay(spawnPoint.position, minDir);

        Gizmos.color = Color.green;
        Vector3 maxDir = CalculateVelocity(maxAngle, 1.5f);
        Gizmos.DrawRay(spawnPoint.position, maxDir);

        // Draw arc between min and max angles
        Gizmos.color = Color.yellow;
        DrawAngleArc();
    }

    void DrawAngleArc()
    {
        int segments = 10;
        float angleStep = (maxAngle - minAngle) / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = minAngle + (angleStep * i);
            float angle2 = minAngle + (angleStep * (i + 1));

            Vector3 point1 = spawnPoint.position + CalculateVelocity(angle1, 2f);
            Vector3 point2 = spawnPoint.position + CalculateVelocity(angle2, 2f);

            Gizmos.DrawLine(point1, point2);
        }
    }
}