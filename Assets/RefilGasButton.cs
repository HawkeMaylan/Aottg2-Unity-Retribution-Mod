using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class RefillGasButton : MonoBehaviour
{
    public SupplyStation supplyStation;

    private GameObject player;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("RefillGasButton: No player found with tag 'Player'.");
        }

        if (supplyStation == null)
        {
            Debug.LogWarning("RefillGasButton: SupplyStation reference not set.");
        }
    }

    public void OnClick()
    {
        if (supplyStation == null || player == null) return;

        supplyStation.RefillGas(player);
    }
}
