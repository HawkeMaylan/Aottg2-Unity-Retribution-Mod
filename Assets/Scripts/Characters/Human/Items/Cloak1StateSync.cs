using UnityEngine;
using Photon.Pun;
using System.Linq;

public class Cloak1StateSync : MonoBehaviourPun, IPunObservable
{
    private bool _cloak1On = false;

    public void Toggle()
    {
        // Only allow toggling ON if cloak2model is active
        Transform cloak2 = transform.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "cloak2model");

        bool cloak2Active = cloak2 != null && cloak2.GetComponent<SkinnedMeshRenderer>()?.enabled == true;

        if (!cloak2Active && !_cloak1On)
        {
            Debug.Log("[Cloak1StateSync] Cannot enable cloak1model because cloak2model is off.");
            return;
        }

        _cloak1On = !_cloak1On;
        UpdateCloak1();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_cloak1On);
        }
        else
        {
            _cloak1On = (bool)stream.ReceiveNext();
            UpdateCloak1();
        }
    }

    private void UpdateCloak1()
    {
        Transform cloak1 = transform.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "cloak1model");

        if (cloak1 != null)
        {
            var renderer = cloak1.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = _cloak1On;
        }

        Debug.Log($"[Cloak1StateSync] cloak1model renderer set to: {_cloak1On}");
    }
}
