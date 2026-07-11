using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.5f;      // Deliberate and weighted, never hurried
    public float sprintSpeed = 5.5f;    // Minor speed boost (outdoors only)
    
    [Header("Look Settings")]
    public float mouseSensitivity = 0.2f;
    [Range(1f, 50f)]
    public float lookSmoothness = 15f;

    [Header("Interaction Settings")]
    public float interactionRange = 2.5f;
    public LayerMask interactableLayer = ~0; // Default: all layers

    [Header("Control State")]
    public bool controlsLocked = false;  // When true, all input is ignored
    public bool sprintDisabled = false;  // When true, Shift does nothing
    public bool isInInterior = false;    // Set by trigger zones — disables sprint indoors
    public bool interactionEnabled = true; // Can be disabled during certain phases

    private float currentSpeed;
    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;

    private float currentVerticalRotation = 0f;
    private float currentHorizontalRotation = 0f;

    private Camera playerCamera;
    private CharacterController characterController;
    private Vector3 verticalVelocity;

    // Interaction
    private InteractableObject currentLookTarget;
    private UIPromptManager interactionPromptManager;
    private LedgerUI ledgerUI;

    // Event for other systems to hook into
    public System.Action<InteractableObject> OnObjectExamined;

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

        // Find UI references
        interactionPromptManager = FindAnyObjectByType<UIPromptManager>();
        ledgerUI = FindAnyObjectByType<LedgerUI>();
    }

    void Update()
    {
        // If ledger is open, only handle Tab to close
        if (ledgerUI != null && ledgerUI.IsOpen)
        {
            return; // Ledger handles its own Tab input
        }

        if (!controlsLocked)
        {
            HandleLook();
            HandleMovement();
        }

        if (interactionEnabled && !controlsLocked)
        {
            HandleInteractionRaycast();
            HandleInteractionInput();
        }
    }

    // === PUBLIC API FOR GAME SYSTEMS ===

    /// <summary>
    /// Lock all player controls (movement + look). Used during cinematics and scripted sequences.
    /// </summary>
    public void LockControls()
    {
        controlsLocked = true;
    }

    /// <summary>
    /// Unlock player controls.
    /// </summary>
    public void UnlockControls()
    {
        controlsLocked = false;
    }

    /// <summary>
    /// Enable or disable sprint. Sprint is disabled in interiors.
    /// </summary>
    public void SetSprintEnabled(bool enabled)
    {
        sprintDisabled = !enabled;
    }

    /// <summary>
    /// Set interior state. Called by trigger zones at door transitions.
    /// </summary>
    public void SetInterior(bool interior)
    {
        isInInterior = interior;
    }

    /// <summary>
    /// Enable or disable interaction (E key).
    /// </summary>
    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
    }

    // === MOVEMENT ===

    private void HandleMovement()
    {
        bool isSprinting = false;
        float moveHorizontal = 0f;
        float moveVertical = 0f;

        // Use the New Input System (Keyboard)
        if (Keyboard.current != null)
        {
            // Sprint only if not disabled and not in interior
            isSprinting = Keyboard.current.shiftKey.isPressed && !sprintDisabled && !isInInterior;
            
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

    // === CAMERA LOOK ===

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

    // === INTERACTION SYSTEM ===

    /// <summary>
    /// Raycast from camera center to detect interactable objects.
    /// </summary>
    private void HandleInteractionRaycast()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        InteractableObject newTarget = null;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            newTarget = hit.collider.GetComponent<InteractableObject>();
            if (newTarget == null)
                newTarget = hit.collider.GetComponentInParent<InteractableObject>();
        }

        // Update highlight state
        if (currentLookTarget != newTarget)
        {
            // Remove highlight from old target
            if (currentLookTarget != null)
            {
                currentLookTarget.SetHighlight(false);
            }

            currentLookTarget = newTarget;

            // Add highlight to new target
            if (currentLookTarget != null && currentLookTarget.isInteractable)
            {
                currentLookTarget.SetHighlight(true);
            }
        }
    }

    /// <summary>
    /// Handle E key press for examining/interacting with objects.
    /// </summary>
    private void HandleInteractionInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame && currentLookTarget != null && currentLookTarget.isInteractable)
        {
            string result = currentLookTarget.Examine();

            if (!string.IsNullOrEmpty(result))
            {
                // Show examine text as a prompt
                if (interactionPromptManager != null)
                {
                    interactionPromptManager.ShowPrompt(result, 4f);
                }
            }

            // Fire event for other systems
            OnObjectExamined?.Invoke(currentLookTarget);
        }
    }

    // Note: Interior zone detection is handled by the InteriorZone component,
    // which calls SetInterior() directly on this script.
    // Room-specific triggers are handled by TriggerZone → GamePhaseManager.
}
