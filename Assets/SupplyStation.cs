using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupplyStation : MonoBehaviour
{
    public int bladeSupplies = 10;
    public int gasSupplies = 5;
    public GameObject radialUI;

    private bool playerInRange = false;
    public Collider interactionZone;
    void Start()
    {
        if (radialUI != null)
            radialUI.SetActive(false); // Hide UI by default
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            ToggleRadialUI();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            radialUI.SetActive(false);
        }
    }

    void ToggleRadialUI()
    {
        if (radialUI != null)
            radialUI.SetActive(!radialUI.activeSelf);
    }

    public void RefillBlades(GameObject player)
    {
        if (bladeSupplies > 0)
        {
            // Your logic to refill blades (customize based on AOTTG2 systems)
            bladeSupplies--;
            Debug.Log("Blades refilled.");
        }
    }

    public void RefillGas(GameObject player)
    {
        if (gasSupplies > 0)
        {
            gasSupplies--;
            Debug.Log("Gas refilled.");
        }
    }
}
