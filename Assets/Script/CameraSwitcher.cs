using UnityEngine;
using Cinemachine;

// Switches between first-person and third-person Cinemachine virtual cameras
// - Uses vertical velocity and ground check to detect falling
// - Switches on death (listens to PlayerHealth.OnDeath)
public class CameraSwitcher : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    public CinemachineVirtualCamera firstPersonVCam;
    public CinemachineVirtualCamera thirdPersonVCam;

    [Header("Targets")]
    public Transform firstPersonLookTarget; // usually the camera/eye transform
    public Transform thirdPersonFollowTarget; // usually the player body or a follow offset

    [Header("Fall Detection")]
    public float fallVelocityThreshold = -5f; // velocity.y below this considered falling
    public float groundCheckDistance = 1.5f;  // distance to consider grounded
    public LayerMask groundMask;

    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private bool isInThirdPerson = false;
    // Public view state helper
    public bool IsFirstPerson => !isInThirdPerson;

    [Header("Switching Mode")]
    [Tooltip("If true, switch cameras by changing Cinemachine Virtual Camera priorities. If false, enable/disable vcam GameObjects.")]
    public bool usePrioritySwitching = true;

    [Tooltip("Priority value to give to the first-person vcam when active")]
    public int firstPersonPriority = 20;
    [Tooltip("Priority value to give to the third-person vcam when active")]
    public int thirdPersonPriority = 10;

    private CinemachineBrain brain;
    private string lastActiveVcamName = null;
    // Saved targets for restoring after death camera
    private Transform prevFirstFollow, prevFirstLook, prevThirdFollow, prevThirdLook;
    private GameObject deathAnchorInstance;
    [Header("Death Camera Settings")]
    public string pitTopTag = "PitTop";
    public float pitSearchRadius = 50f;
    public float deathAnchorHeightOffset = 6f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.OnDeath += OnPlayerDeath;
            playerHealth.OnRespawn += OnPlayerRespawn;
        }

        // Ensure initial camera priorities: first-person active by default
        // Try to auto-find missing virtual cameras if they weren't assigned
        if (firstPersonVCam == null || thirdPersonVCam == null)
        {
            var all = FindObjectsOfType<CinemachineVirtualCamera>(true);
            foreach (var v in all)
            {
                if (firstPersonVCam == null && v.gameObject.name.ToLower().Contains("1st") || v.gameObject.name.ToLower().Contains("first"))
                    firstPersonVCam = v;
                if (thirdPersonVCam == null && v.gameObject.name.ToLower().Contains("3rd") || v.gameObject.name.ToLower().Contains("third"))
                    thirdPersonVCam = v;
            }
        }

        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;

        Debug.Log($"CameraSwitcher start: firstPersonVCam={(firstPersonVCam!=null?firstPersonVCam.name:"null")}, thirdPersonVCam={(thirdPersonVCam!=null?thirdPersonVCam.name:"null")}, brain={(brain!=null?"found":"null")}");

        SetFirstPerson(true);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= OnPlayerDeath;
            playerHealth.OnRespawn -= OnPlayerRespawn;
        }
    }

    void OnPlayerRespawn()
    {
        // Restore camera targets and destroy any death anchor
        RestoreCameraAfterDeath();
    }

    void Update()
    {
        // If player is dead we already switched in event
        if (playerHealth != null && playerHealth.isDead)
            return;

        // Fall detection: check vertical velocity and whether player is not grounded
        bool falling = false;
        if (rb != null)
        {
            if (rb.velocity.y < fallVelocityThreshold && !IsGrounded())
            {
                falling = true;
            }
        }

        if (falling && !isInThirdPerson)
        {
            SetFirstPerson(false);
        }
        else if (!falling && isInThirdPerson)
        {
            // Return to first-person when grounded again and not dead
            if (!IsGrounded()) return;
            SetFirstPerson(true);
        }

        // Debug: log which virtual camera is currently active (when it changes)
        if (brain != null && brain.ActiveVirtualCamera != null)
        {
            string activeName = brain.ActiveVirtualCamera.Name;
            if (activeName != lastActiveVcamName)
            {
                lastActiveVcamName = activeName;
                Debug.Log($"Cinemachine active vcam: {activeName}");
            }
        }
    }

    bool IsGrounded()
    {
        if (thirdPersonFollowTarget == null)
            return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);

        return Physics.Raycast(thirdPersonFollowTarget.position, Vector3.down, groundCheckDistance, groundMask);
    }

    void OnPlayerDeath()
    {
        // Switch to third person on death
        SetFirstPerson(false);
    }

    // Called by PlayerHealth after a ragdoll is spawned. If isFallDeath=true, try to place camera at pit top and look at ragdoll.
    public void FocusOnRagdoll(GameObject ragdoll, Vector3 deathPosition, bool isFallDeath)
    {
        if (thirdPersonVCam == null)
            return;

        // Save previous targets
        prevFirstFollow = firstPersonVCam != null ? firstPersonVCam.Follow : null;
        prevFirstLook = firstPersonVCam != null ? firstPersonVCam.LookAt : null;
        prevThirdFollow = thirdPersonVCam.Follow;
        prevThirdLook = thirdPersonVCam.LookAt;

        // Determine anchor position
        Vector3 anchorPos = deathPosition;

        if (isFallDeath)
        {
            // Try to find a PitTop marker
            GameObject[] pitTops = GameObject.FindGameObjectsWithTag(pitTopTag);
            GameObject best = null;
            float bestDist = float.MaxValue;
            foreach (var p in pitTops)
            {
                float d = Vector3.Distance(p.transform.position, deathPosition);
                if (d < bestDist && d <= pitSearchRadius)
                {
                    bestDist = d;
                    best = p;
                }
            }

            if (best != null)
            {
                anchorPos = best.transform.position;
            }
            else
            {
                // No PitTop marker found: approximate rim by moving up from death position
                anchorPos.y = deathPosition.y + deathAnchorHeightOffset;
            }
        }
        else
        {
            // Non-fall deaths: place camera a little above the player position
            anchorPos.y = deathPosition.y + deathAnchorHeightOffset * 0.6f;
        }

        // Create a temporary anchor GameObject
        if (deathAnchorInstance != null)
            Destroy(deathAnchorInstance);

        deathAnchorInstance = new GameObject("DeathCamAnchor");
        deathAnchorInstance.transform.position = anchorPos;
        deathAnchorInstance.transform.rotation = Quaternion.identity;
        deathAnchorInstance.transform.parent = this.transform;

        // Set third person vcam to follow the anchor and look at the ragdoll
        thirdPersonVCam.Follow = deathAnchorInstance.transform;
        thirdPersonVCam.LookAt = ragdoll != null ? ragdoll.transform : null;

        // Ensure third-person camera is active
        SetFirstPerson(false);
    }

    void RestoreCameraAfterDeath()
    {
        if (deathAnchorInstance != null)
        {
            Destroy(deathAnchorInstance);
            deathAnchorInstance = null;
        }

        if (firstPersonVCam != null)
        {
            firstPersonVCam.Follow = prevFirstFollow;
            firstPersonVCam.LookAt = prevFirstLook;
        }

        if (thirdPersonVCam != null)
        {
            thirdPersonVCam.Follow = prevThirdFollow;
            thirdPersonVCam.LookAt = prevThirdLook;
        }

        // Return to first-person view by default
        SetFirstPerson(true);
    }

    void SetFirstPerson(bool enableFirst)
    {
        isInThirdPerson = !enableFirst;

        if (usePrioritySwitching)
        {
            if (firstPersonVCam != null && thirdPersonVCam != null)
            {
                // Ensure both game objects are active when using priority switching
                firstPersonVCam.gameObject.SetActive(true);
                thirdPersonVCam.gameObject.SetActive(true);

                firstPersonVCam.Priority = enableFirst ? firstPersonPriority : thirdPersonPriority;
                thirdPersonVCam.Priority = enableFirst ? thirdPersonPriority : firstPersonPriority;

                Debug.Log($"CameraSwitcher: switched by priority. FirstPriority={firstPersonVCam.Priority}, ThirdPriority={thirdPersonVCam.Priority}");
            }
            else
            {
                Debug.LogWarning("CameraSwitcher: one or both virtual cameras are null; falling back to SetActive switching.");
                if (firstPersonVCam != null) firstPersonVCam.gameObject.SetActive(enableFirst);
                if (thirdPersonVCam != null) thirdPersonVCam.gameObject.SetActive(!enableFirst);
            }
        }
        else
        {
            if (firstPersonVCam != null)
                firstPersonVCam.gameObject.SetActive(enableFirst);

            if (thirdPersonVCam != null)
                thirdPersonVCam.gameObject.SetActive(!enableFirst);
        }

        // Optional: update follow/LookAt targets
        if (firstPersonVCam != null && firstPersonLookTarget != null)
        {
            firstPersonVCam.Follow = firstPersonLookTarget;
            firstPersonVCam.LookAt = firstPersonLookTarget;
        }

        if (thirdPersonVCam != null && thirdPersonFollowTarget != null)
        {
            thirdPersonVCam.Follow = thirdPersonFollowTarget;
            thirdPersonVCam.LookAt = thirdPersonFollowTarget;
        }
    }
}
