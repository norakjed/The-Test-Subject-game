using UnityEngine;

public class ColliderStateManager : MonoBehaviour
{
    public Collider[] collidersToEnable;
    public Collider[] collidersToDisable;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (Collider col in collidersToEnable)
            {
                if (col != null) col.enabled = true;
            }
            foreach (Collider col in collidersToDisable)
            {
                if (col != null) col.enabled = false;
            }
        }
    }
}