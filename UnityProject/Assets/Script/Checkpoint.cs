using System;
using UnityEngine;

// Attach to a checkpoint GameObject with a trigger collider.
// When the player enters, the checkpoint will become active if any required mission is completed (or none required).
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional mission ID required for this checkpoint to activate. Leave empty to always activate.")]
    public string requiredMissionId = "";

    [Tooltip("If true, the checkpoint will only activate when the required mission ID is completed. If false, touching the checkpoint always activates it.")]
    public bool requireMission = false;

    [Tooltip("Optional: assign your mission manager here so the checkpoint can query it directly (method 'HasCompleted(string)' is used).")]
    public MonoBehaviour missionManager;

    [Tooltip("Optional visual object to enable when checkpoint is active (e.g., light or flag).")]
    public GameObject activeVisual;

    [Tooltip("If true, this checkpoint will persist as the player's respawn across scenes.")]
    public bool persistAcrossScenes = true;

    bool isActive = false;

    void Start()
    {
        if (activeVisual != null)
            activeVisual.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Check mission requirement
        if (!IsMissionRequirementMet())
            return;

        ActivateCheckpoint(other.gameObject);
    }

    bool IsMissionRequirementMet()
    {
        if (!requireMission) return true;
        if (string.IsNullOrEmpty(requiredMissionId)) return true;

        // If a missionManager was explicitly assigned, try it first
        if (missionManager != null)
        {
            var mi = missionManager.GetType().GetMethod("HasCompleted", new Type[] { typeof(string) });
            if (mi != null)
            {
                try
                {
                    var res = mi.Invoke(missionManager, new object[] { requiredMissionId });
                    if (res is bool && (bool)res) return true;
                }
                catch { }
            }
        }

        // Try to find any component in scene with HasCompleted(string)
        var list = FindObjectsOfType<MonoBehaviour>();
        foreach (var b in list)
        {
            var mi = b.GetType().GetMethod("HasCompleted", new Type[] { typeof(string) });
            if (mi != null)
            {
                try
                {
                    var res = mi.Invoke(b, new object[] { requiredMissionId });
                    if (res is bool && (bool)res)
                        return true;
                }
                catch { }
            }
        }

        // Fallback: check common PlayerPrefs keys that mission systems sometimes use
        string[] keysToCheck = new string[] {
            $"Mission_{requiredMissionId}_Complete",
            $"Mission_{requiredMissionId}",
            $"Mission{requiredMissionId}_Complete",
            $"Mission{requiredMissionId}",
            $"mission_{requiredMissionId}",
            $"mission{requiredMissionId}"
        };

        foreach (var key in keysToCheck)
        {
            if (PlayerPrefs.HasKey(key))
            {
                // interpret int==1 or bool-as-int as completed
                int val = PlayerPrefs.GetInt(key, 0);
                if (val != 0) return true;
            }
        }

        Debug.Log($"Checkpoint: mission '{requiredMissionId}' not completed or no mission manager found; checkpoint will not activate.", this);
        return false;
    }

    void ActivateCheckpoint(GameObject player)
    {
        // Set player's respawn position via PlayerHealth if present
        var ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.respawnPosition = transform.position;
            ph.useRespawnPosition = true;
        }

        // Notify manager
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetActiveCheckpoint(this);
            Debug.Log("Checkpoint: notified CheckpointManager.", this);
        }
        else
        {
            Debug.LogWarning("Checkpoint: no CheckpointManager instance found; saving position to PlayerPrefs as fallback.", this);
        }

        // Always save position to PlayerPrefs as a fallback so respawn works after scene reloads
        try
        {
            Vector3 p = transform.position;
            PlayerPrefs.SetFloat("Checkpoint_PosX", p.x);
            PlayerPrefs.SetFloat("Checkpoint_PosY", p.y);
            PlayerPrefs.SetFloat("Checkpoint_PosZ", p.z);
            PlayerPrefs.Save();
            Debug.Log($"Checkpoint: saved fallback checkpoint pos {p}", this);
        }
        catch { }

        isActive = true;
        if (activeVisual != null)
            activeVisual.SetActive(true);

        Debug.Log($"Checkpoint activated at {transform.position} (mission='{requiredMissionId}')", this);
    }

    // Expose activation so other code can force it (editor buttons, cutscenes, etc.)
    public void ForceActivate()
    {
        // attempt to find player
        var p = GameObject.FindWithTag("Player");
        if (p != null) ActivateCheckpoint(p);
        else Debug.LogWarning("Checkpoint.ForceActivate: no GameObject tagged 'Player' found.", this);
    }
}
