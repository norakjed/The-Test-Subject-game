using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    
    [Header("Death Settings")]
    public float respawnDelay = 2f;
    public bool reloadSceneOnDeath = true;
    public Vector3 respawnPosition;
    public bool useRespawnPosition = false;

    void Start()
    {
        currentHealth = maxHealth;
        
        // Save initial position as respawn point if not using custom position
        if (!useRespawnPosition)
        {
            respawnPosition = transform.position;
        }

        // Cache components for ragdoll handling
        mainRigidbody = GetComponent<Rigidbody>();
        movementComp = GetComponent<Movement>();
        playerInteractionComp = GetComponent<PlayerInteraction>();
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    // Public death event so other systems can react (camera, UI, etc.)
    public System.Action OnDeath;
    public System.Action OnRespawn;
    public bool isDead { get; private set; } = false;

    [Header("Ragdoll")]
    [Tooltip("Assign a ragdoll prefab (contains rigidbodies) to spawn on death. If left empty, no ragdoll will be spawned.")]
    public GameObject playerRagdollPrefab;
    [Tooltip("Hide the original player visuals when ragdoll spawns.")]
    public bool hidePlayerOnRagdoll = true;

    // Runtime
    GameObject activeRagdollInstance;
    Rigidbody mainRigidbody;
    Movement movementComp;
    PlayerInteraction playerInteractionComp;
    Renderer[] renderers;
    [Header("Fall Detection for Death Camera")]
    public float fallVelocityThreshold = -5f;
    public float fallHeightThreshold = 3f; // difference from respawn pos to consider as falling into pit

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Backwards-compatible Die() method keeps existing call sites working
    public void Die()
    {
        Die(false);
    }

    // New overload: forceFall=true tells CameraSwitcher to treat this death as a fall into a pit
    public void Die(bool forceFall)
    {
        // Prevent double-processing if Die() is called multiple times
        if (isDead)
        {
            Debug.Log("PlayerHealth.Die called but player is already dead; ignoring duplicate call.");
            return;
        }

        Debug.Log("Player died!");
        isDead = true;
        OnDeath?.Invoke();

        // Disable player controls
        if (movementComp != null)
            movementComp.enabled = false;
        if (playerInteractionComp != null)
            playerInteractionComp.enabled = false;

        // Spawn ragdoll if configured
        if (playerRagdollPrefab != null)
        {
            // Instantiate the ragdoll at the player's current pose
            activeRagdollInstance = Instantiate(playerRagdollPrefab, transform.position, transform.rotation);

            // Ensure the instantiated ragdoll is not tagged as the Player to avoid re-triggering triggers
            ClearRagdollTags(activeRagdollInstance);

            // Disable any Animator on the ragdoll so it doesn't fight physics
            var animators = activeRagdollInstance.GetComponentsInChildren<Animator>(true);
            foreach (var a in animators)
            {
                try { a.enabled = false; } catch { }
            }

            // Ensure child rigidbodies are non-kinematic so physics moves them
            var childBodies = activeRagdollInstance.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in childBodies)
            {
                try
                {
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                }
                catch { }
            }

            // Make sure colliders are enabled on ragdoll so it can interact with world
            var ragColls = activeRagdollInstance.GetComponentsInChildren<Collider>(true);
            foreach (var c in ragColls)
            {
                try { c.enabled = true; } catch { }
            }

            // Transfer velocity from main rigidbody to ragdoll rigidbodies
            if (mainRigidbody != null)
            {
                var ragdollBodies = activeRagdollInstance.GetComponentsInChildren<Rigidbody>();
                foreach (var rb in ragdollBodies)
                {
                    rb.velocity = mainRigidbody.velocity;
                }
            }

            // Optionally hide original player visuals
            if (hidePlayerOnRagdoll && renderers != null)
            {
                foreach (var r in renderers)
                {
                    r.enabled = false;
                }
            }

            // Make main rigidbody kinematic so it doesn't interfere
            if (mainRigidbody != null)
                mainRigidbody.isKinematic = true;
        }
        else
        {
            // No ragdoll: just disable Movement (already done) and keep player visible as fallback
            if (mainRigidbody != null)
                mainRigidbody.isKinematic = true;
        }

        // Handle respawn
        if (reloadSceneOnDeath)
        {
            Invoke("ReloadScene", respawnDelay);
        }
        else
        {
            Invoke("Respawn", respawnDelay);
        }

        // Notify CameraSwitcher (if present) to focus on ragdoll when applicable
        CameraSwitcher cs = FindObjectOfType<CameraSwitcher>();
        if (cs != null && activeRagdollInstance != null)
        {
            bool isFallDeath = forceFall;
            if (!isFallDeath)
            {
                if (mainRigidbody != null && mainRigidbody.velocity.y < fallVelocityThreshold)
                    isFallDeath = true;

                if (transform.position.y < respawnPosition.y - fallHeightThreshold)
                    isFallDeath = true;
            }

            cs.FocusOnRagdoll(activeRagdollInstance, transform.position, isFallDeath);
        }
    }

    void ReloadScene()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Respawn()
    {
        // Respawn at the saved position
        transform.position = respawnPosition;
        currentHealth = maxHealth;
        isDead = false;
        
        // Re-enable player controls
        if (movementComp != null)
            movementComp.enabled = true;
        if (playerInteractionComp != null)
            playerInteractionComp.enabled = true;

        // Destroy ragdoll instance and restore visuals
        if (activeRagdollInstance != null)
        {
            Destroy(activeRagdollInstance);
            activeRagdollInstance = null;
        }

        if (hidePlayerOnRagdoll && renderers != null)
        {
            foreach (var r in renderers)
            {
                r.enabled = true;
            }
        }

        if (mainRigidbody != null)
            mainRigidbody.isKinematic = false;

        Debug.Log("Player respawned!");

        // Notify listeners that player respawned (CameraSwitcher will restore cameras)
        OnRespawn?.Invoke();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        Debug.Log($"Player healed {amount}. Health: {currentHealth}/{maxHealth}");
    }

    // Remove or set tags on ragdoll instance and its children so it won't be mistaken for the Player
    void ClearRagdollTags(GameObject ragdoll)
    {
        if (ragdoll == null) return;

        try
        {
            ragdoll.tag = "Untagged";
        }
        catch { }

        foreach (Transform t in ragdoll.GetComponentsInChildren<Transform>(true))
        {
            try
            {
                t.gameObject.tag = "Untagged";
            }
            catch { }
        }
    }

    [Header("Ragdoll Collision Helpers")]
    [Tooltip("Default seconds to ignore collisions between spawned ragdoll and the trigger that caused the death.")]
    public float ragdollIgnoreDuration = 1.0f;

    // Call this to temporarily ignore collisions between the spawned ragdoll and another collider
    public void IgnoreRagdollCollisionsWith(Collider other, float duration = -1f)
    {
        if (other == null) return;
        if (duration <= 0f) duration = ragdollIgnoreDuration;

        // If ragdoll already exists, apply immediately
        if (activeRagdollInstance != null)
        {
            var ragColliders = activeRagdollInstance.GetComponentsInChildren<Collider>(true);
            foreach (var rc in ragColliders)
            {
                if (rc != null && other != null)
                    Physics.IgnoreCollision(rc, other, true);
            }
            // Nudge the ragdoll away from the trigger so it's not overlapping when collisions are ignored
            NudgeRagdollAwayFromCollider(other, 0.25f);
            StartCoroutine(RestoreIgnoredCollisions(ragColliders, other, duration));
        }
        else
        {
            // Otherwise wait a few frames for the ragdoll to be instantiated
            StartCoroutine(WaitForRagdollThenIgnore(other, duration, 0.1f, 20));
        }
    }

    System.Collections.IEnumerator WaitForRagdollThenIgnore(Collider other, float duration, float waitPer, int maxTries)
    {
        int tries = 0;
        while (tries < maxTries && activeRagdollInstance == null)
        {
            tries++;
            yield return new WaitForSeconds(waitPer);
        }

        if (activeRagdollInstance != null)
        {
            var ragColliders = activeRagdollInstance.GetComponentsInChildren<Collider>(true);
            foreach (var rc in ragColliders)
            {
                if (rc != null && other != null)
                    Physics.IgnoreCollision(rc, other, true);
            }
            // Nudge after applying ignores
            NudgeRagdollAwayFromCollider(other, 0.25f);
            StartCoroutine(RestoreIgnoredCollisions(ragColliders, other, duration));
        }
    }

    // Move the ragdoll a small distance away from the given collider so it's not overlapping when collisions are ignored
    void NudgeRagdollAwayFromCollider(Collider other, float nudgeDistance)
    {
        if (activeRagdollInstance == null || other == null) return;

        Vector3 ragPos = activeRagdollInstance.transform.position;
        Vector3 closest = other.ClosestPoint(ragPos);
        Vector3 dir = (ragPos - closest);
        if (dir.sqrMagnitude < 0.0001f)
        {
            // fallback: use player's forward or world up
            dir = transform.forward + Vector3.up * 0.1f;
        }
        dir.Normalize();

        try
        {
            activeRagdollInstance.transform.position += dir * nudgeDistance;
            // Also apply a small impulse to child rigidbodies along the nudge so they separate
            var rbs = activeRagdollInstance.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                try { rb.AddForce(dir * 1.5f, ForceMode.VelocityChange); } catch { }
            }
        }
        catch { }
    }

    System.Collections.IEnumerator RestoreIgnoredCollisions(Collider[] ragColliders, Collider other, float duration)
    {
        yield return new WaitForSeconds(duration);

        foreach (var rc in ragColliders)
        {
            if (rc != null && other != null)
            {
                // Only re-enable if both still exist
                try
                {
                    Physics.IgnoreCollision(rc, other, false);
                }
                catch { }
            }
        }
    }
}
