using UnityEngine;

// Attach this to a GameObject with an IsTrigger collider (e.g., the pit area or a collision layer object).
// When the object tagged as Player enters, it will force player's Die(true) so the camera uses the pit-top ragdoll view.
public class PitDeathTrigger : MonoBehaviour
{
    [Tooltip("Player tag to look for. Default 'Player'.")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerHealth ph = other.GetComponentInChildren<PlayerHealth>();
        if (ph != null)
        {
            Debug.Log("PitDeathTrigger: Player entered pit trigger — forcing fall death.");
            ph.Die(true);

            // Prevent ragdoll from getting stuck in the trigger by temporarily ignoring collisions
            Collider myCol = GetComponent<Collider>();
            if (myCol != null)
            {
                ph.IgnoreRagdollCollisionsWith(myCol, ph.ragdollIgnoreDuration);
            }
        }
        else
        {
            Debug.LogWarning("PitDeathTrigger: Player entered but no PlayerHealth component found on collider or its children.");
        }
    }
}
