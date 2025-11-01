using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using Characters;

[System.Serializable]
public class WindDirection
{
    public string directionName;
    public Vector3 pushDirection = Vector3.forward;
    public float pushStrength = 3f;
    public GameObject particleEffect; // Assign particle system in inspector
}

public class WindZone : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Wind Zone Settings")]
    public List<WindDirection> windDirections = new List<WindDirection>();
    public float directionChangeCooldown = 5f;

    [Header("Current State (Synced)")]
    [SerializeField] private int currentDirectionIndex = 0;
    [SerializeField] private float cooldownTimer = 0f;
    [SerializeField] private bool isActive = true;

    private Dictionary<Human, bool> playersInZone = new Dictionary<Human, bool>();

    // Public properties for external access
    public int CurrentDirectionIndex => currentDirectionIndex;
    public WindDirection CurrentWindDirection => windDirections.Count > 0 ? windDirections[currentDirectionIndex] : null;
    public float CooldownTimer => cooldownTimer;

    private void Start()
    {
        // Initialize on all clients
        if (windDirections.Count > 0)
        {
            ActivateParticleEffect(currentDirectionIndex);
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Handle cooldown and direction changes on master client only
        if (isActive && windDirections.Count > 1)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                ChangeToNextDirection();
                cooldownTimer = directionChangeCooldown;
            }
        }
    }

    private void FixedUpdate()
    {
        // Apply wind forces to all players in zone on their respective clients
        ApplyWindForces();
    }

    private void ChangeToNextDirection()
    {
        int newDirectionIndex = (currentDirectionIndex + 1) % windDirections.Count;
        photonView.RPC("RPC_ChangeWindDirection", RpcTarget.All, newDirectionIndex);
    }

    [PunRPC]
    private void RPC_ChangeWindDirection(int newDirectionIndex)
    {
        currentDirectionIndex = newDirectionIndex;
        cooldownTimer = directionChangeCooldown;

        // Activate new particle effect and deactivate others
        ActivateParticleEffect(newDirectionIndex);

        Debug.Log($"Wind direction changed to: {windDirections[newDirectionIndex].directionName}");
    }

    private void ActivateParticleEffect(int activeIndex)
    {
        for (int i = 0; i < windDirections.Count; i++)
        {
            if (windDirections[i].particleEffect != null)
            {
                windDirections[i].particleEffect.SetActive(i == activeIndex);
            }
        }
    }

    private void ApplyWindForces()
    {
        foreach (var player in playersInZone)
        {
            if (player.Key != null && player.Value && player.Key.IsMine())
            {
                ApplyWindForce(player.Key);
            }
        }
    }

    private void ApplyWindForce(Human human)
    {
        if (windDirections.Count == 0 || currentDirectionIndex >= windDirections.Count) return;

        WindDirection currentWind = windDirections[currentDirectionIndex];
        Rigidbody rb = human.GetComponent<Rigidbody>();

        if (rb != null)
        {
            Vector3 pushForce = currentWind.pushDirection.normalized * currentWind.pushStrength;
            rb.AddForce(pushForce, ForceMode.Acceleration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null)
        {
            if (!playersInZone.ContainsKey(human))
            {
                playersInZone.Add(human, true);
            }
            else
            {
                playersInZone[human] = true;
            }

            if (human.IsMine())
            {
                EnterWindZone(human);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Human human = other.GetComponentInParent<Human>();
        if (human != null && playersInZone.ContainsKey(human))
        {
            playersInZone[human] = false;

            if (human.IsMine())
            {
                ExitWindZone(human);
            }
        }
    }

    private void EnterWindZone(Human human)
    {
        Rigidbody rb = human.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Optional: Adjust physics when entering wind zone
            // rb.drag = 1f;
            // rb.angularDrag = 1f;
        }
    }

    private void ExitWindZone(Human human)
    {
        Rigidbody rb = human.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Optional: Reset physics when exiting wind zone
            // rb.drag = 0f;
            // rb.angularDrag = 0.05f;
        }
    }

    // Photon Sync
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // We are the master client - send our state to others
            stream.SendNext(currentDirectionIndex);
            stream.SendNext(cooldownTimer);
            stream.SendNext(isActive);
        }
        else
        {
            // We are a remote client - receive state from master
            currentDirectionIndex = (int)stream.ReceiveNext();
            cooldownTimer = (float)stream.ReceiveNext();
            isActive = (bool)stream.ReceiveNext();

            // Ensure particle effects are synced
            ActivateParticleEffect(currentDirectionIndex);
        }
    }

    // Public methods for external control
    public void SetDirection(int newDirectionIndex)
    {
        if (PhotonNetwork.IsMasterClient && newDirectionIndex >= 0 && newDirectionIndex < windDirections.Count)
        {
            photonView.RPC("RPC_ChangeWindDirection", RpcTarget.All, newDirectionIndex);
        }
    }

    public void SetActive(bool active)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RPC_SetWindActive", RpcTarget.All, active);
        }
    }

    [PunRPC]
    private void RPC_SetWindActive(bool active)
    {
        isActive = active;

        // Activate/deactivate particle effects based on current state
        if (active && windDirections.Count > 0)
        {
            ActivateParticleEffect(currentDirectionIndex);
        }
        else
        {
            // Deactivate all particle effects
            foreach (var windDir in windDirections)
            {
                if (windDir.particleEffect != null)
                {
                    windDir.particleEffect.SetActive(false);
                }
            }
        }
    }

    // Clean up dictionary when players leave
    private void OnDestroy()
    {
        playersInZone.Clear();
    }
}