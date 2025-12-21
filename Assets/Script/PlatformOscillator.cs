using System.Collections;
using UnityEngine;

// Moves a platform left and right (or along any specified direction).
// Attach to the platform GameObject (should have a Collider and possibly a Rigidbody if physics are used).
public class PlatformOscillator : MonoBehaviour
{
    [Header("Motion")]
    [Tooltip("Total distance between the left and right endpoints in world units.")]
    public float distance = 4f;
    [Tooltip("Speed in units per second (used to compute travel duration).")]
    public float speed = 2f;
    [Tooltip("Direction to move in (local or world depending on Use Local Direction).")]
    public Vector3 direction = Vector3.right;
    [Tooltip("If true, use the platform's local direction (transform.TransformDirection). If false, use world direction as-is.")]
    public bool useLocalDirection = true;
    [Tooltip("If true the platform will start at the left endpoint. If false it starts at the right endpoint.")]
    public bool startAtLeft = true;

    [Header("Behavior")]
    [Tooltip("If true the platform will continuously ping-pong between endpoints. If false it will move once and stop.")]
    public bool loop = true;
    [Tooltip("If true the platform will pause at each endpoint for Pause Duration seconds.")]
    public bool pauseAtEnds = true;
    [Tooltip("Seconds to pause at endpoints when Pause At Ends is enabled.")]
    public float pauseDuration = 0.35f;

    [Header("Player Carrying (optional)")]
    [Tooltip("Parent the player to the platform while they stand on it. Works by parenting the player's root transform when they collide.")]
    public bool carryPlayer = false;
    [Tooltip("Player tag used to detect the player for parenting. Only used when Carry Player is true.")]
    public string playerTag = "Player";

    [Tooltip("If true, the platform will move its Rigidbody using MovePosition in FixedUpdate for smoother physics interactions. If the GameObject has no Rigidbody, a kinematic one will be added when enabled.")]
    public bool useRigidbodyMovement = true;

    Vector3 leftPoint;
    Vector3 rightPoint;
    Vector3 startPos;
    Vector3 moveDir;
    bool isMoving = false;
    bool hasTriggeredOnce = false;
    // target position driven by coroutine, applied in FixedUpdate (via Rigidbody or transform)
    Vector3 targetPosition;
    Rigidbody rb;

    void Start()
    {
        startPos = transform.position;

        // Determine direction vector in world space
        Vector3 dirWorld = useLocalDirection ? transform.TransformDirection(direction.normalized) : direction.normalized;
        if (dirWorld.sqrMagnitude < 0.0001f)
            dirWorld = transform.right;

        // Points centered on startPos
        Vector3 halfOffset = dirWorld * (distance * 0.5f);
        leftPoint = startPos - halfOffset;
        rightPoint = startPos + halfOffset;

        // If startAtLeft false, swap
        if (!startAtLeft)
        {
            // Move immediately to right endpoint
            transform.position = rightPoint;
            targetPosition = rightPoint;
        }

        // Setup Rigidbody for physics-friendly movement if requested
        if (useRigidbodyMovement)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                // Add a kinematic Rigidbody so MovePosition works predictably
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            else
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            targetPosition = transform.position;
        }

        // Start movement
        StartCoroutine(MoveLoop());
    }

    IEnumerator MoveLoop()
    {
        Vector3 from = transform.position;
        Vector3 to = (Vector3.Distance(transform.position, leftPoint) < Vector3.Distance(transform.position, rightPoint)) ? rightPoint : leftPoint;

        // If not looping and already at target, don't move
        if (!loop && Vector3.Distance(from, to) < 0.001f)
            yield break;

        while (true)
        {
            if (hasTriggeredOnce && !loop)
                yield break;

            float travelDist = Vector3.Distance(from, to);
            if (travelDist < 0.001f)
            {
                // immediate swap
                Vector3 tmp = from; from = to; to = tmp;
                if (pauseAtEnds) yield return new WaitForSeconds(pauseDuration);
                continue;
            }

            float duration = travelDist / Mathf.Max(0.0001f, speed);
            float elapsed = 0f;

                while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // smoothstep easing
                float ease = t * t * (3f - 2f * t);
                // update targetPosition; actual transform/Rigidbody movement happens in FixedUpdate for smooth physics
                targetPosition = Vector3.Lerp(from, to, ease);
                yield return null;
            }

            targetPosition = to;

            if (pauseAtEnds)
            {
                yield return new WaitForSeconds(pauseDuration);
            }

            // Prepare next leg
            Vector3 nextFrom = to;
            Vector3 nextTo = from; // swap
            from = nextFrom;
            to = nextTo;

            // Mark triggered once if configured
            hasTriggeredOnce = true;

            if (!loop)
                break;
        }
    }

    void FixedUpdate()
    {
        if (useRigidbodyMovement && rb != null)
        {
            // Move the rigidbody to the target position computed by the coroutine.
            rb.MovePosition(targetPosition);
        }
        else
        {
            // Fallback: directly set transform (non-physics platforms)
            transform.position = targetPosition;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!carryPlayer) return;
        if (collision.gameObject.CompareTag(playerTag))
        {
            // Parent the player's root transform to the platform so they move with it
            Transform playerRoot = collision.gameObject.transform;
            // If the player object is nested, prefer the top-most root with the Player tag
            Transform root = playerRoot;
            while (root.parent != null && root.parent.gameObject.CompareTag(playerTag))
                root = root.parent;
            // Parent the player's root to platform. Use worldPositionStays=true so player doesn't snap.
            root.SetParent(transform, true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!carryPlayer) return;
        if (collision.gameObject.CompareTag(playerTag))
        {
            // Unparent
            Transform playerRoot = collision.gameObject.transform;
            Transform root = playerRoot;
            while (root.parent != null && root.parent.gameObject.CompareTag(playerTag))
                root = root.parent;
            // Only unparent if currently parented to this platform
            if (root.parent == transform)
                root.SetParent(null, true);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPos : transform.position;
        Vector3 dirWorld = useLocalDirection ? transform.TransformDirection(direction.normalized) : direction.normalized;
        if (dirWorld.sqrMagnitude < 0.0001f) dirWorld = transform.right;
        Vector3 half = dirWorld * (distance * 0.5f);
        Vector3 a = (Application.isPlaying ? startPos : transform.position) - half;
        Vector3 b = (Application.isPlaying ? startPos : transform.position) + half;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawSphere(a, 0.05f);
        Gizmos.DrawSphere(b, 0.05f);
    }
}
