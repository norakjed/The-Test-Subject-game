using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    [Tooltip("Audio clip to play when triggered.")]
    public AudioClip audioClip;

    [Tooltip("Play only once.")]
    public bool playOnce = true;

    private bool hasPlayed = false;

    private static System.Collections.Generic.HashSet<string> disabledTriggers = new System.Collections.Generic.HashSet<string>();

    void Start()
    {
        string key = gameObject.name + "_" + transform.position.ToString();
        if (disabledTriggers.Contains(key))
        {
            hasPlayed = true;
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && (!playOnce || !hasPlayed))
        {
            if (audioClip != null)
            {
                AudioSource playerAudio = other.GetComponent<AudioSource>();
                if (playerAudio == null)
                {
                    playerAudio = other.gameObject.AddComponent<AudioSource>();
                }
                playerAudio.spatialBlend = 0f; // 2D audio
                playerAudio.clip = audioClip;
                playerAudio.Play();
                hasPlayed = true;
                string key = gameObject.name + "_" + transform.position.ToString();
                disabledTriggers.Add(key);
                // Disable the collider to prevent re-triggering in the same session
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}