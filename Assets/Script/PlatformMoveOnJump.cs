using System.Collections;
using UnityEngine;

// Attach this to a moving platform. When the player jumps while standing on this platform,
// the platform will move left by `moveDistance` over `moveDuration` seconds.
public class PlatformMoveOnJump : MonoBehaviour
{
    [Header("Platform Movement")]
    public float moveDistance = 2f;
    public float moveDuration = 0.5f;
    public bool useLocalLeft = true; // if true use -transform.right, otherwise use world left (Vector3.left)
    [Tooltip("If true the platform will only move once and then stop responding to jumps.")]
    public bool triggerOnce = true;

    [Header("Player Detection")]
    public Movement playerMovement; // optional - will auto-find by tag 'Player' if empty
    public bool requirePlayerOnPlatform = true; // only move if player currently stands on this platform

    Collider platformCollider;
    bool isMoving = false;
    bool hasTriggered = false;

    void Start()
    {
        platformCollider = GetComponent<Collider>();
        if (platformCollider == null)
        {
            // If no collider, add a box collider and make it non-trigger so platform has physical presence
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = false;
            platformCollider = bc;
        }

        if (playerMovement == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerMovement = playerObj.GetComponent<Movement>();
            }
        }

        if (playerMovement != null)
        {
            playerMovement.OnJump += OnPlayerJump;
        }
        else
        {
            Debug.LogWarning("PlatformMoveOnJump: No player Movement found - assign Player tag or set playerMovement manually.");
        }
    }

    void OnDestroy()
    {
        if (playerMovement != null)
            playerMovement.OnJump -= OnPlayerJump;
    }

    void OnPlayerJump()
    {
        if (isMoving) return;
        if (triggerOnce && hasTriggered) return;

        // If required, ensure player's position is on top of this platform
        if (requirePlayerOnPlatform)
        {
            if (!IsPlayerOnPlatform())
                return;
        }

        // Start movement
        StartCoroutine(MoveLeftRoutine());
    }

    bool IsPlayerOnPlatform()
    {
        if (platformCollider == null) return false;
        var playerObj = playerMovement != null ? playerMovement.transform : null;
        if (playerObj == null) return false;

        Vector3 p = playerObj.position;
        // Consider player's horizontal position inside platform bounds
        Bounds b = platformCollider.bounds;
        // Slightly shrink bounds vertically so we require player to be near top surface
        if (p.x < b.min.x || p.x > b.max.x || p.z < b.min.z || p.z > b.max.z)
            return false;

        float topY = b.max.y;
        // require player's feet (approx by player's y) to be near top of platform
        if (p.y < topY - 0.1f || p.y > topY + 2.0f)
            return false;

        return true;
    }

    IEnumerator MoveLeftRoutine()
    {
        isMoving = true;
        hasTriggered = true;

        // If configured to trigger only once, unsubscribe from the event so no further calls occur
        if (triggerOnce && playerMovement != null)
        {
            playerMovement.OnJump -= OnPlayerJump;
        }

        Vector3 start = transform.position;
        Vector3 dir = useLocalLeft ? -transform.right : Vector3.left;
        Vector3 target = start + dir.normalized * moveDistance;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            // smooth step movement
            float ease = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(start, target, ease);
            yield return null;
        }

        transform.position = target;
        isMoving = false;
    }
}
