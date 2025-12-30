using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RagdollController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The root Rigidbody used for normal movement (where Movement script is attached).")]
    public Rigidbody mainRigidbody;
    [Tooltip("Main collider (capsule/cylinder) used during normal movement).")]
    public Collider mainCollider;
    [Tooltip("Optional Movement script to enable/disable when toggling ragdoll.")]
    public Movement movementScript;

    List<Rigidbody> boneRigidbodies = new List<Rigidbody>();
    List<Collider> boneColliders = new List<Collider>();

    void Awake()
    {
        if (mainRigidbody == null)
            mainRigidbody = GetComponent<Rigidbody>();
        if (mainCollider == null)
            mainCollider = GetComponent<Collider>();
        if (movementScript == null)
            movementScript = GetComponent<Movement>();

        // Collect all child rigidbodies and colliders (exclude the root/main ones)
        var rbs = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rbs)
        {
            if (rb == mainRigidbody) continue;
            boneRigidbodies.Add(rb);
        }

        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c == mainCollider) continue;
            boneColliders.Add(c);
        }

        // Ensure ragdoll bones are kinematic and colliders disabled by default
        SetRagdoll(false);
    }

    /// <summary>
    /// Enable or disable ragdoll physics. When enabling ragdoll we transfer
    /// the main Rigidbody velocity to each bone so motion is continuous.
    /// </summary>
    public void SetRagdoll(bool enabled)
    {
        if (enabled)
        {
            // Transfer velocity to bones so the ragdoll inherits motion
            Vector3 v = mainRigidbody != null ? mainRigidbody.velocity : Vector3.zero;
            Vector3 av = mainRigidbody != null ? mainRigidbody.angularVelocity : Vector3.zero;

            // Disable main rigidbody/collider and movement
            if (movementScript != null) movementScript.enabled = false;
            if (mainRigidbody != null) mainRigidbody.isKinematic = true;
            if (mainCollider != null) mainCollider.enabled = false;

            // Enable bones
            for (int i = 0; i < boneRigidbodies.Count; i++)
            {
                var rb = boneRigidbodies[i];
                if (rb == null) continue;
                // Copy some physics properties from the main Rigidbody to reduce
                // differences that can cause the ragdoll to slide or glide.
                if (mainRigidbody != null)
                {
                    rb.drag = mainRigidbody.drag;
                    rb.angularDrag = mainRigidbody.angularDrag;
                    rb.collisionDetectionMode = mainRigidbody.collisionDetectionMode;
                    rb.interpolation = mainRigidbody.interpolation;
                }

                rb.isKinematic = false;
                rb.velocity = v;
                rb.angularVelocity = av;
            }
            for (int i = 0; i < boneColliders.Count; i++)
            {
                var c = boneColliders[i];
                if (c == null) continue;
                c.enabled = true;
            }
        }
        else
        {
            // Turn bones off
            for (int i = 0; i < boneRigidbodies.Count; i++)
            {
                var rb = boneRigidbodies[i];
                if (rb == null) continue;
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            for (int i = 0; i < boneColliders.Count; i++)
            {
                var c = boneColliders[i];
                if (c == null) continue;
                c.enabled = false;
            }

            // Re-enable main Rigidbody/collider and movement
            if (mainRigidbody != null)
            {
                mainRigidbody.isKinematic = false;
                mainRigidbody.velocity = Vector3.zero;
                mainRigidbody.angularVelocity = Vector3.zero;
            }
            if (mainCollider != null) mainCollider.enabled = true;
            if (movementScript != null) movementScript.enabled = true;
        }
    }
}
