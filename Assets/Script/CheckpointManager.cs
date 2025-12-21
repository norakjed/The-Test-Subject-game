using UnityEngine;
using UnityEngine.SceneManagement;

// Simple singleton to track the currently active checkpoint.
// Keeps a reference for UI or other systems and optionally persists across scenes.
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("If true, the CheckpointManager will persist across scenes.")]
    public bool persistAcrossScenes = true;

    [Tooltip("If true, the CheckpointManager will prefer a scene-default checkpoint (e.g. 'Checkpoint0') on scene load and ignore saved PlayerPrefs.)")]
    public bool alwaysUseSceneDefaultOnLoad = true;
    [Tooltip("Optional: explicitly assign which Checkpoint should be used as the scene default start (e.g., Checkpoint0). If assigned, this will be applied on scene load.")]
    public Checkpoint defaultStartCheckpoint;

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

        // If developer explicitly assigned a default start checkpoint in the inspector, apply it first.
        if (defaultStartCheckpoint != null)
        {
            activeCheckpoint = defaultStartCheckpoint;
            Vector3 p = activeCheckpoint.transform.position;
            Debug.Log($"CheckpointManager: applying explicit defaultStartCheckpoint pos {p}", this);
            ph.respawnPosition = p;
            ph.useRespawnPosition = true;
            player.transform.position = ph.respawnPosition;
            return;
        }

        // Otherwise, if configured, prefer a scene-default checkpoint (e.g. a GameObject named containing "checkpoint0").
        if (alwaysUseSceneDefaultOnLoad)
        {
            var cps = FindObjectsOfType<Checkpoint>();
            if (cps != null && cps.Length > 0)
            {
                Checkpoint defaultCp = null;
                foreach (var c in cps)
                {
                    var n = c.gameObject.name.ToLower();
                    if (n.Contains("checkpoint0") || n.Contains("checkpoint 0") || n.EndsWith("0"))
                    {
                        defaultCp = c;
                        break;
                    }
                }
                if (defaultCp == null) defaultCp = cps[0];
                if (defaultCp != null)
                {
                    activeCheckpoint = defaultCp;
                    Vector3 p = activeCheckpoint.transform.position;
                    Debug.Log($"CheckpointManager: applying scene-default checkpoint pos {p}", this);
                    ph.respawnPosition = p;
                    ph.useRespawnPosition = true;
                    // Also move the player immediately to the checkpoint position after scene load
                    player.transform.position = ph.respawnPosition;
                    return;
                }
            }
        }

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