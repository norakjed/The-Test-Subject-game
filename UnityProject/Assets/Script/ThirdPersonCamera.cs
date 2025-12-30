using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;              // assign player transform in Inspector
    public float followDistance = 2.0f;   // smaller = nearer to player
    public float height = 1.2f;           // vertical offset above target
    public float smoothTime = 0.12f;      // smoothing
    Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // desired position: behind the player (relative to player's forward)
        Vector3 behind = -target.forward * followDistance;
        Vector3 desiredPos = target.position + behind + Vector3.up * height;

        // smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);

        // look at the target (optional: offset look)
        transform.LookAt(target.position + Vector3.up * (height * 0.5f));
    }
}