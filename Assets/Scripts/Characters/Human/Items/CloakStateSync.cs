using UnityEngine;
using Photon.Pun;
using System.Linq;

public class CloakStateSync : MonoBehaviourPun, IPunObservable
{
    private bool _cloakIsOn = true;

    public void Toggle()
    {
        _cloakIsOn = !_cloakIsOn;
        UpdateCloak();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_cloakIsOn);
        }
        else
        {
            _cloakIsOn = (bool)stream.ReceiveNext();
            UpdateCloak();
        }
    }

    private void UpdateCloak()
    {
        Transform cloak2 = transform.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "cloak2model");

        if (cloak2 != null)
        {
            var renderer = cloak2.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
                renderer.enabled = _cloakIsOn;

            var cloth = cloak2.GetComponent<Cloth>();
            if (cloth != null)
                cloth.enabled = _cloakIsOn;
        }

        // Also disable cloak1model if cloak2model is being turned off
        if (!_cloakIsOn)
        {
            Transform cloak1 = transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "cloak1model");

            if (cloak1 != null)
            {
                var cloak1Renderer = cloak1.GetComponent<MeshRenderer>();
                if (cloak1Renderer != null)
                    cloak1Renderer.enabled = false;
            }

            Debug.Log("[CloakStateSync] cloak2model OFF — forcing cloak1model OFF as well.");
        }

        Debug.Log($"[CloakStateSync] Cloak visibility set to: {_cloakIsOn}");
    }
}
