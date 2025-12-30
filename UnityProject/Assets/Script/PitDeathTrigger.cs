using UnityEngine;

// Attach this to a GameObject with an IsTrigger collider (e.g., the pit area or a collision layer object).
// When the object tagged as Player enters, it will force player's Die(true) so the camera uses the pit-top ragdoll view.
public class PitDeathTrigger : MonoBehaviour
{
    [Tooltip("Player tag to look for. Default 'Player'.")]
    public string playerTag = "Player";

    [Tooltip("Audio clip to play on death.")]
    public AudioClip failAudioClip;

    private static bool failAudioPlayed = false;

    void Start()
    {
        // No persistence, resets on game restart
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        PlayerHealth ph = other.GetComponentInChildren<PlayerHealth>();
        if (ph != null)
        {
            Debug.Log("PitDeathTrigger: Player entered pit trigger — forcing fall death.");
            ph.Die(true);

            // Play fail audio only once per session
            if (failAudioClip != null && !failAudioPlayed)
            {
                AudioSource playerAudio = other.GetComponent<AudioSource>();
                if (playerAudio == null)
                {
                    playerAudio = other.gameObject.AddComponent<AudioSource>();
                }
                playerAudio.spatialBlend = 0f; // 2D audio
                playerAudio.clip = failAudioClip;
                playerAudio.Play();
                failAudioPlayed = true;
            }

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
