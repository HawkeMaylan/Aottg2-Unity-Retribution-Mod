using UnityEngine;
using Photon.Pun;

public class NetworkedFlare: MonoBehaviourPun, IPunInstantiateMagicCallback
{
    private Renderer _renderer;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        ApplyFlareColorFromData();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Color application is handled in Start to ensure renderer is ready
    }

    private void ApplyFlareColorFromData()
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
                Debug.Log($"Applied color to FlareBandMaterial: {color}");
                break;
            }
        }

        if (!foundMaterial)
        {
            Debug.LogWarning("FlareBandMaterial not found. Available materials:");
            foreach (var material in materials)
            {
                Debug.Log($" - {material.name}");
            }
        }

        _renderer.materials = materials;
    }
}