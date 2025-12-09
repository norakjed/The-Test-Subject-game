using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fall : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Movement speed")]
    public float moveSpeed = 5f;
    
    [Tooltip("Rotation speed")]
    public float rotationSpeed = 200f;
    
    [Header("Fall Settings")]
    [Tooltip("Height from which the player will fall")]
    public float fallHeight = 20f;
    
    [Tooltip("Press this key to teleport to fall height")]
    public KeyCode teleportKey = KeyCode.F;
    
    [Tooltip("Press this key to activate ragdoll")]
    public KeyCode ragdollKey = KeyCode.R;
    
    [Header("Ragdoll Settings")]
    [Tooltip("Enable ragdoll physics on fall")]
    public bool enableRagdollOnFall = true;
    
    [Tooltip("Delay before enabling ragdoll after teleport (seconds)")]
    public float ragdollDelay = 0.5f;
    
    private Vector3 originalPosition;
    private Animator animator;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private bool isRagdollActive = false;
    private CharacterController characterController;
    private Rigidbody mainRigidbody;

    void Start()
    {
        // Store original position
        originalPosition = transform.position;
        
        // Get animator component
        animator = GetComponent<Animator>();
        
        // Get character controller or rigidbody
        characterController = GetComponent<CharacterController>();
        mainRigidbody = GetComponent<Rigidbody>();
        
        // Get all rigidbodies and colliders for ragdoll
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        
        // Initially disable ragdoll
        DisableRagdoll();
    }

    void Update()
    {
        // Only allow movement if ragdoll is not active
        if (!isRagdollActive)
        {
            HandleMovement();
        }
        
        // Teleport to fall height
        if (Input.GetKeyDown(teleportKey))
        {
            TeleportToHeight();
            
            if (enableRagdollOnFall)
            {
                StartCoroutine(EnableRagdollAfterDelay(ragdollDelay));
            }
        }
        
        // Manual ragdoll toggle
        if (Input.GetKeyDown(ragdollKey))
        {
            if (isRagdollActive)
            {
                DisableRagdoll();
            }
            else
            {
                EnableRagdoll();
            }
        }
        
        // Reset position with Spacebar
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ResetPosition();
        }
    }
    
    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Calculate movement direction
        Vector3 movement = new Vector3(horizontal, 0, vertical).normalized;
        
        // Rotate character based on movement direction
        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Move character
        if (characterController != null)
        {
            // Using CharacterController
            Vector3 velocity = movement * moveSpeed;
            velocity.y = -9.81f; // Apply gravity
            characterController.Move(velocity * Time.deltaTime);
        }
        else if (mainRigidbody != null)
        {
            // Using Rigidbody
            Vector3 velocity = movement * moveSpeed;
            mainRigidbody.MovePosition(transform.position + velocity * Time.deltaTime);
        }
        else
        {
            // Using Transform (fallback)
            transform.position += movement * moveSpeed * Time.deltaTime;
        }
        
        // Update animator parameters if available
        if (animator != null)
        {
            animator.SetFloat("Speed", movement.magnitude * moveSpeed);
            animator.SetBool("IsWalking", movement.magnitude > 0.1f);
        }
    }
    
    void TeleportToHeight()
    {
        Vector3 newPosition = new Vector3(transform.position.x, fallHeight, transform.position.z);
        transform.position = newPosition;
        Debug.Log($"Teleported to height: {fallHeight}m");
    }
    
    IEnumerator EnableRagdollAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EnableRagdoll();
    }
    
    void EnableRagdoll()
    {
        if (isRagdollActive) return;
        
        // Disable animator
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        // Enable all ragdoll rigidbodies and colliders
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }
        }
        
        foreach (Collider col in ragdollColliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
        
        isRagdollActive = true;
        Debug.Log("Ragdoll enabled");
    }
    
    void DisableRagdoll()
    {
        // Enable animator
        if (animator != null)
        {
            animator.enabled = true;
        }
        
        // Disable all ragdoll rigidbodies
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }
        
        // Keep main collider enabled, disable ragdoll colliders
        Collider mainCollider = GetComponent<Collider>();
        foreach (Collider col in ragdollColliders)
        {
            if (col != null && col != mainCollider)
            {
                col.enabled = false;
            }
        }
        
        isRagdollActive = false;
        Debug.Log("Ragdoll disabled");
    }
    
    void ResetPosition()
    {
        DisableRagdoll();
        transform.position = originalPosition;
        transform.rotation = Quaternion.identity;
        Debug.Log("Position reset");
    }
}
