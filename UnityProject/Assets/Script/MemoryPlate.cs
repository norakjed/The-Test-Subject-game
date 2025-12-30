using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MemoryPlate : MonoBehaviour
{
    public MemoryGame gameManager;
    [Header("Options")]
    public bool singleUse = true;

    private bool used = false;

    void Awake()
    {
        // Do not force trigger here. Plate should provide physical support so player can stand.
        var col = GetComponent<Collider>();
        if (col == null) Debug.LogWarning("MemoryPlate requires a Collider component.", this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (singleUse && used) return;

            if (gameManager != null) gameManager.StartGame();
            else MemoryGame.Instance?.StartGame();

            if (singleUse) used = true;
        }
    }
}
