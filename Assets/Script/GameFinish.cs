using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFinish : MonoBehaviour
{
    [Header("UI")]
    public GameObject finishCanvas;

    [Header("Particles")]
    public ParticleSystem[] finishParticles;

    [Header("Game Settings")]
    public bool pauseGame = true;

    private bool gameFinished = false;

    void Start()
    {
        if (finishCanvas != null)
            finishCanvas.SetActive(false);
    }

    public void FinishGame()
    {
        if (gameFinished) return;
        gameFinished = true;

        // Show UI
        if (finishCanvas != null)
            finishCanvas.SetActive(true);

        // Play particles
        foreach (ParticleSystem ps in finishParticles)
        {
            if (ps != null)
                ps.Play();
        }

        // Show and unlock cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Pause game
        if (pauseGame)
            Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        ResetSessionState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Attempt to reset common persistent/session state so a scene reload acts like a full restart.
    void ResetSessionState()
    {
        // Restore normal time
        Time.timeScale = 1f;

        // Remove saved checkpoint position so scene defaults will apply
        PlayerPrefs.DeleteKey("Checkpoint_PosX");
        PlayerPrefs.DeleteKey("Checkpoint_PosY");
        PlayerPrefs.DeleteKey("Checkpoint_PosZ");

        // Reset death counter PlayerPrefs (if used)
        PlayerPrefs.SetInt("DeathCounter_Count", 0);
        PlayerPrefs.Save();

        // Destroy in-memory DeathTracker singleton if present (named __DeathTracker)
        var dtGo = GameObject.Find("__DeathTracker");
        if (dtGo != null)
        {
            // DeathTracker.ResetCount is a static API; call via type name.
            try { DeathTracker.ResetCount(); } catch { }
            Destroy(dtGo);
        }

        // Reset and remove DeathCounter UI singleton if present
        var dc = FindObjectOfType<DeathCounter>();
        if (dc != null)
        {
            dc.ResetCount();
            if (dc.persistAcrossScenes) Destroy(dc.gameObject);
        }

        // Remove persistent CheckpointManager so scene load uses scene defaults
        var cm = FindObjectOfType<CheckpointManager>();
        if (cm != null)
        {
            PlayerPrefs.DeleteKey("Checkpoint_PosX");
            PlayerPrefs.DeleteKey("Checkpoint_PosY");
            PlayerPrefs.DeleteKey("Checkpoint_PosZ");
            Destroy(cm.gameObject);
        }

        // Reset static singletons or counters where possible
        try { MemoryGame.Instance = null; } catch { }
        try { JumpscareButton.pressCount = 0; } catch { }

        // Note: some scripts use private static flags that cannot be reset here.
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}
