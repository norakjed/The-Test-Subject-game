using UnityEngine;

public class Spike : MonoBehaviour
{
    [Header("Spike Settings")]
    public string playerTag = "Player";
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched the spike is the player
        if (other.CompareTag(playerTag))
        {
            Debug.Log("Player touched spike! Player died.");
            
            // Get the PlayerHealth component and kill the player
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.Die();
            }
            else
            {
                Debug.LogWarning("Player doesn't have a PlayerHealth component!");
            }
        }
    }
}
