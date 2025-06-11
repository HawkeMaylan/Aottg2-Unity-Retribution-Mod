using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class RefillBladesButton : MonoBehaviour
{
    public SupplyStation supplyStation;

    private GameObject player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("RefillBladesButton: No player found with tag 'Player'.");
        }

        if (supplyStation == null)
        {
            Debug.LogWarning("RefillBladesButton: SupplyStation reference not set.");
        }
    }

    public void OnClick()
    {
        if (supplyStation == null || player == null) return;

        supplyStation.RefillBlades(player);
    }
}
