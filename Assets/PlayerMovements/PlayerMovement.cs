using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 10f;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 0.2f;
    [Range(1f, 50f)]
    public float lookSmoothness = 15f;

    private float currentSpeed;
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;

    private float currentVerticalRotation = 0f;
    private float currentHorizontalRotation = 0f;

    private Camera playerCamera;
    private CharacterController characterController;
    private Vector3 verticalVelocity;

    void Start()
    {
        // Lock cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        characterController = GetComponent<CharacterController>();

        // Try to get the camera attached to the player or its children
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            Debug.LogWarning("PlayerMovement: No camera found on player or its children.");
        }
        
        // Initialize rotations based on current transform
        horizontalRotation = transform.eulerAngles.y;
        currentHorizontalRotation = horizontalRotation;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleMovement()
    {
        bool isSprinting = false;
        float moveHorizontal = 0f;
        float moveVertical = 0f;

        // Use the New Input System (Keyboard)
        if (Keyboard.current != null)
        {
            isSprinting = Keyboard.current.shiftKey.isPressed;
            
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveVertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveVertical -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveHorizontal -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveHorizontal += 1f;
        }

        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Calculate movement direction relative to player's facing direction
        Vector3 move = transform.right * moveHorizontal + transform.forward * moveVertical;
        
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        // Handle basic gravity
        if (characterController.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f; // Small downward force to keep grounded
        }

        verticalVelocity.y += Physics.gravity.y * Time.deltaTime;

        // Apply movement via CharacterController
        Vector3 finalMovement = (move * currentSpeed) + verticalVelocity;
        characterController.Move(finalMovement * Time.deltaTime);
    }

    private void HandleLook()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        // Use the New Input System (Mouse)
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX = delta.x * mouseSensitivity;
            mouseY = delta.y * mouseSensitivity;
        }

        // Calculate target rotations
        horizontalRotation += mouseX;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f); // Prevent looking further than straight up/down

        // Apply smoothness
        if (lookSmoothness > 0f)
        {
            currentHorizontalRotation = Mathf.Lerp(currentHorizontalRotation, horizontalRotation, Time.deltaTime * lookSmoothness);
            currentVerticalRotation = Mathf.Lerp(currentVerticalRotation, verticalRotation, Time.deltaTime * lookSmoothness);
        }
        else
        {
            currentHorizontalRotation = horizontalRotation;
            currentVerticalRotation = verticalRotation;
        }

        // Apply rotation to player body (horizontal)
        transform.localRotation = Quaternion.Euler(0f, currentHorizontalRotation, 0f);

        // Apply rotation to camera (vertical) if it exists
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(currentVerticalRotation, 0f, 0f);
        }
    }
}
