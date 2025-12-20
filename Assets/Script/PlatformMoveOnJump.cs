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
    [Tooltip("Maximum distance (world units) from the platform at which a player jump will trigger the platform.")]
    public float triggerDistance = 3f;

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

        // Ensure we have a valid player reference. If missing, try to find one.
        if (playerMovement == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerMovement = playerObj.GetComponent<Movement>();
        }

        // If still missing, ignore the event to avoid unintended triggers.
        if (playerMovement == null)
        {
            Debug.LogWarning("PlatformMoveOnJump: OnPlayerJump invoked but no playerMovement assigned; ignoring.", this);
            return;
        }

        // If required, ensure player's position is on top of this platform
        if (requirePlayerOnPlatform)
        {
            if (!IsPlayerOnPlatform())
                return;
        }

        // Require player to be within triggerDistance of the platform (measured to collider)
        if (triggerDistance > 0f)
        {
            // Prefer the player's ground-check position (feet) if available, otherwise use the transform
            Vector3 playerPos = playerMovement.transform.position;
            var groundCheckField = typeof(Movement).GetField("groundCheck");
            if (groundCheckField != null)
            {
                try
                {
                    var gc = groundCheckField.GetValue(playerMovement) as Transform;
                    if (gc != null) playerPos = gc.position;
                }
                catch { }
            }

            Vector3 closest = platformCollider != null ? platformCollider.ClosestPoint(playerPos) : transform.position;

            // Use horizontal (XZ) distance for triggering so vertical offsets don't falsely pass
            float horizDist = Vector2.Distance(new Vector2(playerPos.x, playerPos.z), new Vector2(closest.x, closest.z));
            float fullDist = Vector3.Distance(playerPos, closest);

            // If ClosestPoint returned the player's position (distance 0), that means the player
            // position is inside the collider. In that case ensure the player's feet are near the
            // top surface; otherwise ignore the trigger (prevents distant/embedded positions passing).
            if (fullDist <= 0f && platformCollider != null)
            {
                float topY = platformCollider.bounds.max.y;
                float feetDelta = Mathf.Abs(playerPos.y - topY);
                Debug.Log($"PlatformMoveOnJump: ClosestPoint==playerPos; feetDelta={feetDelta:F2}", this);
                // if feet aren't within ~0.6 units of the top, treat as not nearby
                if (feetDelta > 0.6f)
                    return;
            }

            Debug.Log($"PlatformMoveOnJump: Player horizDist={horizDist:F2}, fullDist={fullDist:F2}, triggerDistance={triggerDistance:F2}", this);
            if (horizDist > triggerDistance)
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
