using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 20f;
    public LayerMask interactionLayer;  // Optional: set to only interact with specific layers
    
    [Header("UI (Optional)")]
    public GameObject interactionPrompt;  // UI element showing "Press E to interact"
    
    private DoorController currentDoor;
    public Camera playerCamera; // assign in Inspector or leave empty to use Camera.main
    [Header("Raycast Settings")]
    public float rayOriginOffset = 0.2f; // move ray origin slightly forward to avoid hitting player's own collider
    
    [Header("Proximity Fallback")]
    public bool useProximityFallback = true;
    public float proximityAngle = 45f; // degrees from camera forward to consider a door "in view"

    void Start()
    {
        // Try to use an explicitly assigned camera first
        if (playerCamera == null)
        {
            // Fallback to Camera.main
            playerCamera = Camera.main;
        }

        // If Camera.main is null, try to find a camera tagged MainCamera
        if (playerCamera == null)
        {
            GameObject camObj = GameObject.FindWithTag("MainCamera");
            if (camObj != null)
                playerCamera = camObj.GetComponent<Camera>();
        }

        // As a last resort, try to find any Camera component in children
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError("Player camera is null! Make sure there is a Camera in the scene with the tag 'MainCamera' or assign the 'playerCamera' field on PlayerInteraction.");
        }

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    void Update()
    {
        CheckForInteractable();
        
        // Press E to interact
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E key pressed!");
            
            if (currentDoor != null)
            {
                Debug.Log("Door found! Toggling door.");
                currentDoor.ToggleDoor();
            }
            else
            {
                Debug.Log("No door detected. Make sure you're looking at the door.");
            }
        }
    }

    // Public API used by trigger colliders to explicitly set the current door
    public void SetCurrentDoor(DoorController door)
    {
        currentDoor = door;
        if (interactionPrompt != null)
            interactionPrompt.SetActive(door != null);
    }

    // Clear current door if the given door matches (used on trigger exit)
    public void ClearCurrentDoor(DoorController door)
    {
        if (currentDoor == door)
        {
            currentDoor = null;
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }

    void CheckForInteractable()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player camera is null! Make sure Camera.main is set.");
            return;
        }
        
        // Start the ray a little in front of the camera to avoid immediately hitting the player's own head/collider
        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * rayOriginOffset;
        Ray ray = new Ray(origin, playerCamera.transform.forward);

        // Use RaycastAll so we can inspect hits in order and ignore player's own colliders
        RaycastHit[] hits;
        if (interactionLayer.value == 0)
        {
            hits = Physics.RaycastAll(ray, interactionDistance);
        }
        else
        {
            hits = Physics.RaycastAll(ray, interactionDistance, interactionLayer);
        }

        if (hits != null && hits.Length > 0)
        {
            // Sort hits by distance (closest first)
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                string hitName = h.collider != null ? h.collider.name : "<null>";

                // Ignore hits that belong to the player (this transform or its children)
                if (h.collider != null && h.collider.transform.IsChildOf(transform))
                {
                    Debug.Log($"Ignored hit on player: {hitName} at distance {h.distance}");
                    continue;
                }

                Debug.Log($"Interaction ray hit: {hitName} at distance {h.distance} on layer {LayerMask.LayerToName(h.collider.gameObject.layer)}");

                // Try to find DoorController on the collider, then parents, then children
                DoorController door = null;
                if (h.collider != null)
                {
                    // First check the collider's GameObject
                    door = h.collider.GetComponent<DoorController>();
                    
                    // Then check all parents up the hierarchy
                    if (door == null)
                    {
                        Transform current = h.collider.transform;
                        while (current != null && door == null)
                        {
                            door = current.GetComponent<DoorController>();
                            current = current.parent;
                        }
                        if (door != null)
                            Debug.Log($"Found DoorController on ancestor of {hitName}: {door.gameObject.name}");
                    }
                    
                    // Finally check children
                    if (door == null)
                    {
                        door = h.collider.GetComponentInChildren<DoorController>();
                        if (door != null)
                            Debug.Log($"Found DoorController on child of {hitName}: {door.gameObject.name}");
                    }
                    
                    if (door == null)
                    {
                        Debug.Log($"No DoorController found on {hitName}, its parents, or children");
                    }
                }

                if (door != null)
                {
                    currentDoor = door;

                    if (interactionPrompt != null)
                    {
                        interactionPrompt.SetActive(true);
                    }

                    return;
                }
                else
                {
                    // Hit something non-door; continue to next hit (in case door is behind it)
                    Debug.Log($"Hit {hitName} but no DoorController found; continuing to next hit.");
                    continue;
                }
            }
            
            // Raycast hit something but found no doors - fall through to proximity check
            Debug.Log("[Raycast] Hit objects but no DoorController found, falling through to proximity...");
        }
        else
        {
            Debug.Log("[Raycast] No hits found, trying proximity fallback...");
        }
        
        // Proximity fallback: if raycast didn't find a door, check for nearest door within view
        if (useProximityFallback)
        {
            DoorController nearestDoor = FindNearestDoorInView();
            if (nearestDoor != null)
            {
                currentDoor = nearestDoor;
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(true);
                }
                Debug.Log($"[Proximity] Found door: {nearestDoor.gameObject.name}");
                return;
            }
        }
        
        // No door found
        currentDoor = null;
        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    DoorController FindNearestDoorInView()
    {
        if (playerCamera == null) return null;
        
        DoorController[] allDoors = FindObjectsOfType<DoorController>();
        Debug.Log($"[Proximity] Scanning {allDoors.Length} doors");
        
        DoorController nearest = null;
        float bestScore = float.MaxValue;
        
        // Use the actual ray direction (from camera through center of screen)
        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * rayOriginOffset;
        Vector3 rayDir = playerCamera.transform.forward;
        
        foreach (var door in allDoors)
        {
            if (door == null) continue;
            
            // Determine the closest point on the door's collider to the ray origin
            Collider col = door.GetComponent<Collider>() ?? door.GetComponentInChildren<Collider>();
            Vector3 closestPoint;
            if (col != null)
            {
                closestPoint = col.ClosestPoint(origin);
            }
            else
            {
                closestPoint = door.transform.position;
            }

            // Vector from ray origin to the door's closest point
            Vector3 toDirVec = closestPoint - origin;
            float dist = toDirVec.magnitude;
            if (dist <= 0.0001f)
                continue;
            Vector3 toDir = toDirVec / dist;

            // Calculate angle and dot
            float angle = Vector3.Angle(rayDir, toDir);
            float dotProduct = Vector3.Dot(rayDir, toDir);

            Debug.Log($"[Proximity] Door '{door.gameObject.name}': closestPoint={closestPoint}, dist={dist:F2}, angle={angle:F1}°, dot={dotProduct:F2}");

            // Reject by distance first
            if (dist > interactionDistance)
            {
                Debug.Log($"  -> Too far");
                continue;
            }

            // Allow doors that are within the view angle OR are very close to the player (both sides)
            bool withinAngle = angle <= proximityAngle;
            bool veryClose = dist <= 1.25f; // small threshold to allow interaction even if camera points slightly away

            if (!withinAngle && !veryClose)
            {
                Debug.Log($"  -> Outside view angle and not very close");
                continue;
            }
            
            // Score: heavily favor smallest angle (door closest to ray direction)
            float score = angle * 10.0f + dist * 0.01f;
            
            if (score < bestScore)
            {
                bestScore = score;
                nearest = door;
                Debug.Log($"  -> New best door! (score={score:F2})");
            }
        }
        
        if (nearest == null)
        {
            Debug.Log("[Proximity] No door found within range and angle");
        }
        else
        {
            Debug.Log($"[Proximity] Selected: {nearest.gameObject.name}");
        }
        
        return nearest;
    }

    // Optional: Visualize the interaction ray in the editor
    void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance);
        }
    }
}
