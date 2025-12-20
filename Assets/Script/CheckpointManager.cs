using UnityEngine;
using UnityEngine.SceneManagement;

// Simple singleton to track the currently active checkpoint.
// Keeps a reference for UI or other systems and optionally persists across scenes.
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("If true, the CheckpointManager will persist across scenes.")]
    public bool persistAcrossScenes = true;

    Checkpoint activeCheckpoint;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // When a new scene loads, apply the active checkpoint (or saved position) to the player
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var ph = player.GetComponent<PlayerHealth>();
        if (ph == null) return;

        if (activeCheckpoint != null)
        {
            Vector3 p = activeCheckpoint.transform.position;
            Debug.Log($"CheckpointManager: applying active checkpoint pos {p}", this);
            ph.respawnPosition = p;
            ph.useRespawnPosition = true;
            // Also move the player immediately to the checkpoint position after scene load
            player.transform.position = ph.respawnPosition;
            return;
        }

        // Fallback: check PlayerPrefs for a saved checkpoint position
        if (PlayerPrefs.HasKey("Checkpoint_PosX"))
        {
            float x = PlayerPrefs.GetFloat("Checkpoint_PosX", player.transform.position.x);
            float y = PlayerPrefs.GetFloat("Checkpoint_PosY", player.transform.position.y);
            float z = PlayerPrefs.GetFloat("Checkpoint_PosZ", player.transform.position.z);
            Vector3 saved = new Vector3(x, y, z);
            Debug.Log($"CheckpointManager: applying saved checkpoint pos {saved}", this);
            ph.respawnPosition = saved;
            ph.useRespawnPosition = true;
            player.transform.position = saved;
        }
    }

    public void SetActiveCheckpoint(Checkpoint cp)
    {
        activeCheckpoint = cp;
        if (persistAcrossScenes && cp != null)
        {
            // Save position so it can be applied after reloads
            Vector3 p = cp.transform.position;
            PlayerPrefs.SetFloat("Checkpoint_PosX", p.x);
            PlayerPrefs.SetFloat("Checkpoint_PosY", p.y);
            PlayerPrefs.SetFloat("Checkpoint_PosZ", p.z);
            PlayerPrefs.Save();
            Debug.Log($"CheckpointManager: saved checkpoint pos {p}", this);
        }
    }

    public Checkpoint GetActiveCheckpoint() => activeCheckpoint;

    // Convenience: teleport player to active checkpoint (used by debug tools)
    public void TeleportPlayerToActiveCheckpoint()
    {
        if (activeCheckpoint == null) return;
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            var ph = p.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.respawnPosition = activeCheckpoint.transform.position;
                ph.useRespawnPosition = true;
                p.transform.position = ph.respawnPosition;
            }
            else
            {
                p.transform.position = activeCheckpoint.transform.position;
            }
        }
    }
}
