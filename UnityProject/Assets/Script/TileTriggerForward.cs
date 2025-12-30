using UnityEngine;

// Not used anymore - simplified approach uses only BoxCollider isTrigger
// Kept for compatibility in case any objects still reference it
public class TileTriggerForward : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // Not used
    }

    void OnTriggerExit(Collider other)
    {
        // Not used
    }
}
