using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public float openAngle = 90f;          // How far the door opens
    public float closeAngle = 0f;          // Closed position
    public float openSpeed = 2f;           // Speed of opening/closing
    public bool isOpen = false;            // Current state
    
    [Header("Door Axis")]
    public Vector3 rotationAxis = Vector3.up;  // Axis to rotate around (Y-axis by default)
    
    private Quaternion targetRotation;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        // Store the initial rotation as closed position
        closedRotation = transform.localRotation;
        
        // Calculate open rotation
        openRotation = closedRotation * Quaternion.Euler(rotationAxis * openAngle);
        
        // Set initial target
        targetRotation = isOpen ? openRotation : closedRotation;
    }

    void Update()
    {
        // Smoothly rotate towards target
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, 
            targetRotation, 
            Time.deltaTime * openSpeed
        );
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        targetRotation = isOpen ? openRotation : closedRotation;
    }

    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
            targetRotation = openRotation;
        }
    }

    public void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            targetRotation = closedRotation;
        }
    }
}
