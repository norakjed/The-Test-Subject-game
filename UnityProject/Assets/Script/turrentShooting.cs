using UnityEngine;

public class TurretShooter : MonoBehaviour
{
    [Tooltip("Point from which projectiles are spawned")]
    public Transform firePoint;

    [Tooltip("Projectile prefab (should have a Rigidbody and Projectile script)")]
    public GameObject projectilePrefab;

    [Tooltip("Detection radius to find the player")]
    public float detectionRadius = 10f;

    [Tooltip("Layer(s) that contain the player (set this to the Player layer)")]
    public LayerMask playerLayer;

    [Tooltip("Layers that block line of sight (e.g. Default, Environment)")]
    public LayerMask obstructionLayers = ~0;

    [Tooltip("Require an unobstructed line of sight before firing")]
    public bool requireLineOfSight = true;

    [Tooltip("Enable debug logs for detection/raycast events")]
    public bool debugLogs = false;
    [Tooltip("Tags to ignore when checking line of sight (e.g. Projectile)")]
    public string[] ignoreTags = new string[] { "Projectile" };

    [Tooltip("Seconds between shots")]
    public float fireCooldown = 1f;

    [Tooltip("Speed applied to instantiated projectile (if projectile has Rigidbody)")]
    public float projectileSpeed = 20f;

    [Tooltip("Small spawn offset along firing direction to avoid immediate collisions at origin")]
    public float projectileSpawnOffset = 0.5f;

    float cooldownTimer = 0f;

    void Reset()
    {
        if (firePoint == null) firePoint = transform;
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        Collider[] foundAll = Physics.OverlapSphere(transform.position, detectionRadius);

        Transform target = null;
        float bestDist = float.MaxValue;

        // Find the nearest collider that looks like the player (tag or PlayerHealth)
        foreach (var c in foundAll)
        {
            if (c == null) continue;
            if (c.transform.IsChildOf(transform)) continue; // ignore self

            // If playerLayer is set, skip colliders not in that layer
            if ((playerLayer.value & (1 << c.gameObject.layer)) == 0) continue;

            if (c.CompareTag("Player") || c.GetComponentInParent<PlayerHealth>() != null)
            {
                float d = Vector3.SqrMagnitude(c.transform.position - transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    target = c.transform;
                }
            }
        }

        if (target == null)
        {
            if (debugLogs) Debug.Log(name + ": no player targets in detection radius");
            return;
        }
        Vector3 toTarget = (target.position - (firePoint != null ? firePoint.position : transform.position));

        // Check line of sight: raycast from firePoint to target and ensure the first hit is the player
        RaycastHit hit;
        float dist = toTarget.magnitude;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        if (requireLineOfSight)
        {
            // Use RaycastAll and treat the first non-ignored hit as blocking. This allows ignoring projectiles, triggers, small decorations, etc.
            RaycastHit[] hits = Physics.RaycastAll(origin, toTarget.normalized, dist + 0.1f, obstructionLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool blocked = false;
            bool sawTarget = false;

            foreach (var h in hits)
            {
                // If this hit belongs to the target, LOS is clear
                if (h.transform.root == target.root)
                {
                    sawTarget = true;
                    break;
                }

                // Ignore projectiles (by component) so spawned bullets won't block LOS
                if (h.transform.GetComponentInParent<Projectile>() != null) continue;
                // Ignore hits by tag
                bool skip = false;
                if (ignoreTags != null)
                {
                    foreach (var t in ignoreTags)
                    {
                        if (!string.IsNullOrEmpty(t) && h.transform.CompareTag(t))
                        {
                            skip = true;
                            break;
                        }
                    }
                }
                if (skip) continue;

                // Otherwise this hit blocks line of sight
                if (debugLogs) Debug.Log(name + ": raycast hit " + h.transform.name + " (root=" + h.transform.root.name + ")");
                blocked = true;
                break;
            }

            if (!sawTarget && blocked) return;
            if (!sawTarget && hits.Length == 0 && debugLogs) Debug.Log(name + ": raycast did not hit anything between turret and target");
        }
        else if (debugLogs)
        {
            Debug.Log(name + ": LOS check skipped (requireLineOfSight=false)");
        }

        if (cooldownTimer <= 0f)
        {
            if (debugLogs) Debug.Log(name + ": shooting at " + target.name);
            ShootAt(target);
            cooldownTimer = fireCooldown;
        }
    }

    void ShootAt(Transform target)
    {
        if (projectilePrefab == null) return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 dir = (target.position - origin).normalized;

        Vector3 spawnPos = origin + dir * projectileSpawnOffset;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        // If the prefab has a Rigidbody, give it velocity so it moves toward the player
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = dir * projectileSpeed;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(firePoint.position, 0.05f);
            Gizmos.DrawLine(transform.position, firePoint.position);
        }
    }
}