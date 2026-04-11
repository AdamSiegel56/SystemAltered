using UnityEngine;

// Simple trigger-based pickup that applies a drug state to the player
public class DrugPickup : MonoBehaviour
{
    public DrugStateData state; // State applied on pickup

    private void OnTriggerEnter(Collider other)
    {
        // Check if object has a DrugStateController (player)
        var controller = other.GetComponent<DrugStateController>();

        if (controller != null)
        {
            // Apply the drug effect
            controller.SetState(state);

            // Destroy pickup after use
            Destroy(gameObject);
        }
    }
}