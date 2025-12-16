using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 10f;
    public float lifeTime = 5f;
    [Tooltip("Optional explosion prefab spawned on impact")]
    public GameObject explosionPrefab;

    [Tooltip("Knockback force applied to the player on hit (velocity change)")]
    public float knockbackForce = 5f;

    [Tooltip("If true, force ragdoll on a single hit regardless of player's health")]
    public bool oneHitRagdoll = true;

    [Tooltip("Small spawn offset along firing direction to avoid immediate collisions at origin")]
    public float spawnOffset = 0.25f;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.velocity = transform.forward * speed;
        }
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Spawn explosion VFX at contact point
        Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, contactPoint, Quaternion.identity);
        }

        // If we hit the player, apply damage and handle knockback/ragdoll properly
        if (collision.gameObject.CompareTag("Player"))
        {
            var player = collision.gameObject;
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            Vector3 knockDir = (player.transform.position - transform.position).normalized;

            var health = player.GetComponent<PlayerHealth>();
            int dmgInt = Mathf.RoundToInt(damage);

            if (health != null)
            {
                // Determine whether this hit should cause ragdoll
                bool willDie = (health.currentHealth <= dmgInt);

                if (oneHitRagdoll)
                {
                    // Force ragdoll immediately and apply impulse to ragdoll
                    try { health.IgnoreRagdollCollisionsWith(collision.collider, -1f); } catch { }
                    health.Die(true);
                    health.ApplyRagdollImpulse(knockDir * knockbackForce);
                }
                else if (willDie)
                {
                    // Lethal hit: apply damage (which spawns ragdoll) then push ragdoll
                    health.TakeDamage(dmgInt);
                    try { health.IgnoreRagdollCollisionsWith(collision.collider, -1f); } catch { }
                    health.ApplyRagdollImpulse(knockDir * knockbackForce);
                }
                else
                {
                    // Non-lethal: apply knockback to player's rigidbody then damage
                    if (playerRb != null)
                    {
                        playerRb.AddForce(knockDir * knockbackForce, ForceMode.VelocityChange);
                    }
                    health.TakeDamage(dmgInt);
                    try { health.IgnoreRagdollCollisionsWith(collision.collider, -1f); } catch { }
                }
            }
            else
            {
                // No PlayerHealth: just apply a physical knockback if possible
                if (playerRb != null)
                {
                    playerRb.AddForce(knockDir * knockbackForce, ForceMode.VelocityChange);
                }
            }
        }

        Destroy(gameObject);
    }
}