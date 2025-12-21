using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverTrigger : MonoBehaviour
{
    [Header("Particle System")]
    public ParticleSystem particleEffect;

    [Header("Sound Effect")]
    public AudioClip soundEffect;
    public float soundVolume = 1f;

    [Header("UI Canvas")]
    public Canvas gameOverCanvas;

    [Header("Buttons")]
    public Button playAgainButton;
    public Button homeButton;

    void Start()
    {
        // Initially hide the canvas
        if (gameOverCanvas != null)
        {
            gameOverCanvas.enabled = false;
        }

        // Set up button listeners
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        if (homeButton != null)
        {
            homeButton.onClick.AddListener(GoHome);
        }
    }

    // Call this method to trigger the game over sequence
    public void TriggerGameOver()
    {
        // Play particle effect
        if (particleEffect != null)
        {
            particleEffect.Play();
        }

        // Play sound effect
        if (soundEffect != null)
        {
            AudioSource.PlayClipAtPoint(soundEffect, Camera.main.transform.position, soundVolume);
        }

        // Show canvas
        if (gameOverCanvas != null)
        {
            gameOverCanvas.enabled = true;
        }
    }

    void PlayAgain()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoHome()
    {
        // Load the main menu scene (assuming it's named "Main Menu")
        SceneManager.LoadScene("Main Menu");
    }
}