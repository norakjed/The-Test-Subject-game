using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

// Attach this to a UI controller object. It listens for PlayerHealth.OnDeath
// and updates a TextMeshPro text element with the death count.
public class DeathCounter : MonoBehaviour
{
    [Tooltip("Optional: assign the PlayerHealth component. If empty the script will find the object tagged 'Player'.")]
    public PlayerHealth playerHealth;

    [Tooltip("Assign the TextMeshPro UI text to display deaths. If left empty the script will look for a GameObject named 'DeathText'.")]
    public TMP_Text deathText;

    int deaths = 0;

    [Header("Persistence")]
    [Tooltip("If true, the counter GameObject will persist across scene loads (in-memory). Does NOT save to disk unless 'saveToPlayerPrefs' is enabled.")]
    public bool persistAcrossScenes = true;
    [Tooltip("If true, the death count will also be saved to PlayerPrefs so it survives app restarts. Default is false (session-only).")]
    public bool saveToPlayerPrefs = false;
    [Tooltip("PlayerPrefs key used to store death count if 'saveToPlayerPrefs' is enabled.")]
    public string playerPrefsKey = "DeathCounter_Count";

    // singleton instance used when persisting across scenes
    static DeathCounter instance;
    

    void Awake()
    {
        // Enforce a single instance across the app to avoid resets when scenes reload.
        if (instance != null && instance != this)
        {
            Debug.Log("DeathCounter: duplicate instance destroyed.", this);
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("DeathCounter: will persist across scenes.", this);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Load saved count only if explicitly requested
        if (saveToPlayerPrefs)
        {
            deaths = PlayerPrefs.GetInt(playerPrefsKey, 0);
        }

        
        // Ensure DeathTracker exists and subscribe to it so we always reflect the global count
        try
        {
            var dt = DeathTracker.Instance; // ensures creation
            dt.OnDeathCountChanged -= OnTrackerDeathChanged;
            dt.OnDeathCountChanged += OnTrackerDeathChanged;
            deaths = dt.Count;
        }
        catch { }

        // Attempt to find player and UI in the current scene and subscribe
        ResolvePlayerAndUI();
        UpdateText();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene reloaded/changed: find new player and UI and (re)subscribe
        ResolvePlayerAndUI();
    }

    void ResolvePlayerAndUI()
    {
        // Find player
        var p = GameObject.FindWithTag("Player");
        PlayerHealth newPH = null;
        if (p != null) newPH = p.GetComponent<PlayerHealth>();

        // We no longer subscribe to PlayerHealth.OnDeath to avoid double-counting.
        playerHealth = newPH;

        // Find UI text in scene if not assigned or if the assigned object was destroyed
        if (deathText == null || deathText.gameObject.scene.buildIndex != SceneManager.GetActiveScene().buildIndex)
        {
            var go = GameObject.Find("DeathText");
            if (go != null) deathText = go.GetComponent<TMP_Text>();
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }



    void OnTrackerDeathChanged(int newCount)
    {
        deaths = newCount;
        if (saveToPlayerPrefs)
        {
            PlayerPrefs.SetInt(playerPrefsKey, deaths);
            PlayerPrefs.Save();
        }
        UpdateText();
    }

    // Static API so other systems (like PlayerHealth) can notify a death even if
    // the DeathCounter instance hasn't been created or subscribed yet.
    public static void NotifyDeath()
    {
        // Forward to DeathTracker so all deaths are recorded in-memory.
        try { DeathTracker.RecordDeath(); } catch { }
    }

    void UpdateText()
    {
        if (deathText != null)
            deathText.text = $"Deaths: {deaths}";
    }

    // Optional API for other code to increment or reset
    public void Increment() { deaths++; if (saveToPlayerPrefs) { PlayerPrefs.SetInt(playerPrefsKey, deaths); PlayerPrefs.Save(); } UpdateText(); }
    public void ResetCount() { deaths = 0; if (saveToPlayerPrefs) { PlayerPrefs.SetInt(playerPrefsKey, deaths); PlayerPrefs.Save(); } UpdateText(); }
}
