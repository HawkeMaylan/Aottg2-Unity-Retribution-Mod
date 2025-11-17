using UnityEngine;
using Photon.Pun;

public class NetworkedFlare : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private Renderer _renderer;
    private bool _colorApplied = false;

    void Start()
    {
        _renderer = GetComponent<Renderer>();

        // Use RPC to apply color across all clients
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_ApplyFlareColor", RpcTarget.AllBuffered);
            photonView.RPC("RPC_StartRigidbodyRemovalTimer", RpcTarget.AllBuffered);
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Color application is handled via RPC
    }

    [PunRPC]
    private void RPC_StartRigidbodyRemovalTimer()
    {
        StartCoroutine(RemoveRigidbodyAfterDelay());
    }

    private System.Collections.IEnumerator RemoveRigidbodyAfterDelay()
    {
        // Wait for 30 seconds
        yield return new WaitForSeconds(30f);

        // Use RPC to remove rigidbody on all clients
        if (photonView.IsMine)
        {
            // Sync the final position and rotation before removing rigidbody
            Vector3 finalPosition = transform.position;
            Quaternion finalRotation = transform.rotation;

            photonView.RPC("RPC_RemoveRigidbody", RpcTarget.AllBuffered, finalPosition, finalRotation);
        }
    }

    [PunRPC]
    private void RPC_RemoveRigidbody(Vector3 finalPosition, Quaternion finalRotation)
    {
        // First sync the position across all clients
        transform.position = finalPosition;
        transform.rotation = finalRotation;

        // Then remove the rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        // Optional: Also remove the collider if you don't need it anymore
        // Collider collider = GetComponent<Collider>();
        // if (collider != null)
        // {
        //     Destroy(collider);
        // }
    }

    [PunRPC]
    private void RPC_ApplyFlareColor()
    {
        object[] data = photonView.InstantiationData;
        if (data != null && data.Length >= 4 && _renderer != null)
        {
            Color flareColor = new Color((float)data[0], (float)data[1], (float)data[2], (float)data[3]);
            ApplyColorToFlareBandMaterial(flareColor);
        }
    }

    private void ApplyColorToFlareBandMaterial(Color color)
    {
        Material[] materials = _renderer.materials;
        bool foundMaterial = false;

        for (int i = 0; i < materials.Length; i++)
        {
            // Try different possible name matches
            if (materials[i].name.StartsWith("FlareBandMaterial") ||
                materials[i].name.Contains("FlareBandMaterial") ||
                materials[i].name.Replace(" (Instance)", "") == "FlareBandMaterial")
            {
                materials[i].color = color;
                // Also try setting the main color property
                materials[i].SetColor("_Color", color);
                foundMaterial = true;
                break;
            }
        }

        _renderer.materials = materials;
        _colorApplied = true;
    }
}