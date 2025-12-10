using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    // Event invoked when the player performs a jump
    public Action OnJump;
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public Transform playerBody;
    public Transform cameraTransform;

    private Rigidbody rb;
    private float xRotation = 0f;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;

        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // If not assigned, try to find camera
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }

        // If playerBody not assigned, use this transform
        if (playerBody == null)
        {
            playerBody = transform;
        }

        // Warn if any scale is negative - this can invert local axes and cause reversed movement
        Vector3 lossy = transform.lossyScale;
        if (lossy.x < 0f || lossy.y < 0f || lossy.z < 0f)
        {
            Debug.LogWarning($"Player or parent has negative scale {lossy}. Negative scale can invert movement directions. Consider using positive scales and rotate objects instead.");
        }
    }

    void Update()
    {
        // Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate camera up/down
        xRotation -= mouseY;

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        // Rotate player left/right
        playerBody.Rotate(Vector3.up * mouseX);

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // Notify listeners that a jump happened
            try { OnJump?.Invoke(); } catch { }
        }
    }

    void FixedUpdate()
    {
        // Check if grounded
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // Fallback: raycast down from player position
            isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        }

        // WASD Movement
        float moveX = 0f;
        float moveZ = 0f;

        if (Input.GetKey(KeyCode.W)) moveZ += 1f;
        if (Input.GetKey(KeyCode.S)) moveZ -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;

        // Move relative to the camera's horizontal forward/right (prevents inverted movement when body/camera differ)
        Vector3 forwardDir;
        Vector3 rightDir;

        if (cameraTransform != null)
        {
            // Project camera forward onto XZ plane to avoid vertical look affecting movement
            forwardDir = cameraTransform.forward;
            forwardDir.y = 0f;
            forwardDir.Normalize();

            rightDir = cameraTransform.right;
            rightDir.y = 0f;
            rightDir.Normalize();
        }
        else
        {
            // Fallback to player body axes
            forwardDir = playerBody.forward;
            forwardDir.y = 0f;
            forwardDir.Normalize();

            rightDir = playerBody.right;
            rightDir.y = 0f;
            rightDir.Normalize();
        }

        Vector3 move = (rightDir * moveX + forwardDir * moveZ).normalized;
        move *= moveSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + move);
    }
}
