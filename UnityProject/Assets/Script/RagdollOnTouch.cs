using System.Collections;
using UnityEngine;

// Attach this to an (optionally) empty GameObject. The script will ensure a trigger collider
// exists (adds a BoxCollider if none) and call PlayerHealth.Die(true) when the player enters.
[DisallowMultipleComponent]
public class RagdollOnTouch : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("If true the script will add a BoxCollider and set IsTrigger = true when no collider exists.")]
    public bool addTriggerIfMissing = true;

    [Tooltip("If a collider is added automatically, use this size (local space). You can adjust in the Inspector.")]
    public Vector3 autoColliderSize = new Vector3(1f, 2f, 1f);

    [Tooltip("Player tag to detect. The script will also fall back to finding a PlayerHealth on the colliding object or its children.")]
    public string playerTag = "Player";

    [Tooltip("Optional delay (seconds) before calling Die(true) after touch. Use 0 for immediate.")]
    public float delayBeforeDie = 0f;

    [Tooltip("If true the GameObject will be destroyed after it triggers once.")]
    public bool destroyAfterTrigger = false;
    [Tooltip("How long to ignore collisions between spawned ragdoll and this trigger collider (seconds).")]
    public float ignoreRagdollCollisionDuration = 1.0f;

    Collider attachedCollider;

    void Reset()
    {
        // Helpful defaults when script is first added
        addTriggerIfMissing = true;
        autoColliderSize = new Vector3(1f, 2f, 1f);
        playerTag = "Player";
        delayBeforeDie = 0f;
        destroyAfterTrigger = false;
    }

    void Awake()
    {
        attachedCollider = GetComponent<Collider>();

        if (attachedCollider == null && addTriggerIfMissing)
        {
            // Add a BoxCollider sized according to autoColliderSize
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = Vector3.up * (autoColliderSize.y * 0.5f);
#if UNITY_EDITOR
            // make the collider editable in the inspector
            UnityEditor.Undo.RegisterCreatedObjectUndo(bc, "Add BoxCollider for RagdollOnTouch");
#endif
            attachedCollider = bc;
            Debug.Log("RagdollOnTouch: Added BoxCollider (IsTrigger=true). Configure size and center as needed.", this);
        }
        else if (attachedCollider != null)
        {
            // Ensure it's a trigger collider
            if (!attachedCollider.isTrigger)
            {
                attachedCollider.isTrigger = true;
                Debug.Log("RagdollOnTouch: Existing collider set to IsTrigger=true.", this);
            }
        }

        if (attachedCollider == null)
            Debug.LogWarning("RagdollOnTouch: No collider present and addTriggerIfMissing is false. This object will not detect touches.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        TryTriggerOnCollider(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        // If somebody added a non-trigger collider and this object collides, support that too
        TryTriggerOnCollider(collision.collider);
    }

    void TryTriggerOnCollider(Collider other)
    {
        if (other == null) return;

        // If there's a CameraSwitcher, only allow this trigger to force ragdoll when in first-person
        CameraSwitcher cs = FindObjectOfType<CameraSwitcher>();
        if (cs != null)
        {
            if (!cs.IsFirstPerson)
            {
                // Not in first-person, ignore this trigger
                Debug.Log("RagdollOnTouch: ignored because camera is not in first-person.", this);
                return;
            }
        }

        // Quick tag check first
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            StartCoroutine(DelayedDieIfPlayer(other));
            return;
        }

        // Otherwise, look for PlayerHealth on the collider or its parents/children
        PlayerHealth ph = other.GetComponentInChildren<PlayerHealth>();
        if (ph == null)
        {
            // check parent chain
            Transform t = other.transform;
            while (t != null && ph == null)
            {
                ph = t.GetComponent<PlayerHealth>();
                t = t.parent;
            }
        }

        if (ph != null)
        {
            StartCoroutine(DelayedDie(ph));
        }
    }

    IEnumerator DelayedDieIfPlayer(Collider other)
    {
        // Find PlayerHealth component on this collider (children or parents)
        PlayerHealth ph = other.GetComponentInChildren<PlayerHealth>();
        if (ph == null)
        {
            Transform t = other.transform;
            while (t != null && ph == null)
            {
                ph = t.GetComponent<PlayerHealth>();
                t = t.parent;
            }
        }

        if (ph != null)
            yield return StartCoroutine(DelayedDie(ph));
    }

    IEnumerator DelayedDie(PlayerHealth ph)
    {
        if (ph == null) yield break;
        if (delayBeforeDie > 0f)
            yield return new WaitForSeconds(delayBeforeDie);

        // Call Die(true) to force ragdoll/fall camera
        ph.Die(true);

        // Ensure ragdoll won't get stuck in this trigger: temporarily ignore collisions
        if (attachedCollider != null)
        {
            ph.IgnoreRagdollCollisionsWith(attachedCollider, ignoreRagdollCollisionDuration);
        }

        if (destroyAfterTrigger)
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        if (attachedCollider is BoxCollider bc)
        {
            Vector3 scale = transform.lossyScale;
            // draw box at local center
            Matrix4x4 tr = Matrix4x4.TRS(transform.position + bc.center, transform.rotation, scale);
            Gizmos.matrix = tr;
            Gizmos.DrawCube(Vector3.zero, bc.size);
        }
        else if (attachedCollider == null)
        {
            // draw default box based on autoColliderSize
            Matrix4x4 tr = Matrix4x4.TRS(transform.position + Vector3.up * (autoColliderSize.y * 0.5f), transform.rotation, transform.lossyScale);
            Gizmos.matrix = tr;
            Gizmos.DrawCube(Vector3.zero, autoColliderSize);
        }
    }
#endif
}
