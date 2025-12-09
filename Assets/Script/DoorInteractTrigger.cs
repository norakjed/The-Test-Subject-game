using UnityEngine;

// Attach this to a child GameObject of the door that has an IsTrigger collider
// The trigger should cover the doorway area so the player enters it when standing in front of the door.
public class DoorInteractTrigger : MonoBehaviour
{
    DoorController door;

    void Start()
    {
        door = GetComponentInParent<DoorController>();
        if (door == null)
            Debug.LogWarning("DoorInteractTrigger: No DoorController found in parents.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (door == null) return;

        // Prefer tagged Player; fall back to any object with a PlayerInteraction component
        if (other.CompareTag("Player"))
        {
            var pi = other.GetComponentInChildren<PlayerInteraction>();
            if (pi != null)
            {
                pi.SetCurrentDoor(door);
                return;
            }
        }

        // fallback - search the collider for PlayerInteraction
        var playerInt = other.GetComponentInChildren<PlayerInteraction>();
        if (playerInt != null)
        {
            playerInt.SetCurrentDoor(door);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (door == null) return;

        if (other.CompareTag("Player"))
        {
            var pi = other.GetComponentInChildren<PlayerInteraction>();
            if (pi != null)
            {
                pi.ClearCurrentDoor(door);
                return;
            }
        }

        var playerInt = other.GetComponentInChildren<PlayerInteraction>();
        if (playerInt != null)
        {
            playerInt.ClearCurrentDoor(door);
        }
    }
}
