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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main Menu");
    }
}
