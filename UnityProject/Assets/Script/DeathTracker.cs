using System;
using UnityEngine;

// In-memory singleton that tracks player deaths across scenes without writing to disk.
public class DeathTracker : MonoBehaviour
{
    static DeathTracker _instance;
    public static DeathTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = GameObject.Find("__DeathTracker");
                if (go == null)
                {
                    go = new GameObject("__DeathTracker");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<DeathTracker>();
                }
                else
                {
                    _instance = go.GetComponent<DeathTracker>();
                    if (_instance == null) _instance = go.AddComponent<DeathTracker>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    int deaths = 0;
    public int Count => deaths;

    // Notifies listeners with the new count
    public event Action<int> OnDeathCountChanged;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void RecordDeath()
    {
        Instance.AddDeath();
    }

    void AddDeath()
    {
        deaths++;
        try { OnDeathCountChanged?.Invoke(deaths); } catch { }
    }

    // Optional API
    public static int GetCount() => Instance.deaths;
    public static void ResetCount()
    {
        if (_instance != null)
        {
            _instance.deaths = 0;
            try { _instance.OnDeathCountChanged?.Invoke(0); } catch { }
        }
    }
}
