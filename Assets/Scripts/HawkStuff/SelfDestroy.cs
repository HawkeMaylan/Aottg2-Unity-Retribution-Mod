using UnityEngine;
using Photon.Pun;

public class SelfDestroy: MonoBehaviourPun
{
    public float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
