using UnityEngine;
using UnityEngine.InputSystem;

namespace MOBA
{
    /// <summary>
    /// Input relay that bridges Unity Input System with the hierarchical state machine
    /// Routes input events to the appropriate state handlers based on current state
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InputRelay : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private MOBACharacterController characterController;

        [Header("Command System")]
        [SerializeField] private CommandManager commandManager;
        [SerializeField] private AbilitySystem abilitySystem;

        // Public properties for external access
        public MOBACharacterController CharacterController => characterController;
        public CommandManager CommandManager => commandManager;
        public AbilitySystem AbilitySystem => abilitySystem;

        private PlayerInput playerInput;
        private InputActionAsset inputActions;

        // Input state tracking
        private Vector3 movementInput;
        private bool jumpPressed;
        private bool ability1Held;
        private bool ability2Held;
        private bool ultimateHeld;
        private Vector3 aimPosition;

        private void Awake()
        {
            // Get PlayerInput component
            playerInput = GetComponent<PlayerInput>();

            // Load input actions if not assigned
            if (inputActions == null)
            {
                // Use the same InputSystem_Actions as other scripts
                var inputSystemActions = new InputSystem_Actions();
                inputActions = inputSystemActions.asset;
                if (inputActions != null && playerInput != null)
                {
                    playerInput.actions = inputActions;
                    Debug.Log("[InputRelay] InputSystem_Actions loaded successfully");
                }
            }

            // Find dependencies if not assigned
            if (characterController == null)
            {
                characterController = GetComponent<MOBACharacterController>();
                if (characterController == null && Application.isPlaying)
                {
                    characterController = Object.FindAnyObjectByType<MOBACharacterController>();
                }
            }

            if (commandManager == null && Application.isPlaying)
            {
                commandManager = Object.FindAnyObjectByType<CommandManager>();
                if (commandManager == null)
                {
                    Debug.LogWarning("[InputRelay] CommandManager not found in scene. Commands will not work until one is available.");
                }
            }

            if (abilitySystem == null && Application.isPlaying)
            {
                abilitySystem = Object.FindAnyObjectByType<AbilitySystem>();
                if (abilitySystem == null)
                {
                    Debug.LogWarning("[InputRelay] AbilitySystem not found in scene. Abilities will not work until one is available.");
                }
            }
        }

        private void OnEnable()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                try
                {
                    // Movement and basic actions - using Player/ action paths
                    if (playerInput.actions.FindAction("Player/Move") != null)
                    {
                        playerInput.actions["Player/Move"].performed += OnMovement;
                        playerInput.actions["Player/Move"].canceled += OnMovement;
                    }
                    if (playerInput.actions.FindAction("Player/Jump") != null)
                    {
                        playerInput.actions["Player/Jump"].performed += OnJump;
                    }
                    if (playerInput.actions.FindAction("Player/Attack") != null)
                    {
                        playerInput.actions["Player/Attack"].performed += OnPrimaryAttack;
                    }
                    if (playerInput.actions.FindAction("Player/Home") != null)
                    {
                        playerInput.actions["Player/Home"].performed += OnInteract;
                    }

                    // Ability inputs with hold-to-aim
                    if (playerInput.actions.FindAction("Player/Ability1") != null)
                    {
                        playerInput.actions["Player/Ability1"].performed += OnAbility1Start;
                        playerInput.actions["Player/Ability1"].canceled += OnAbility1End;
                    }
                    if (playerInput.actions.FindAction("Player/Ability2") != null)
                    {
                        playerInput.actions["Player/Ability2"].performed += OnAbility2Start;
                        playerInput.actions["Player/Ability2"].canceled += OnAbility2End;
                    }

                    // Camera and UI - using available actions
                    if (playerInput.actions.FindAction("Player/Aim") != null)
                    {
                        playerInput.actions["Player/Aim"].performed += OnCameraPan;
                    }
                    if (playerInput.actions.FindAction("Player/Pause") != null)
                    {
                        playerInput.actions["Player/Pause"].performed += OnOpenMainMenu;
                    }

                    Debug.Log("[InputRelay] Input subscriptions successful");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[InputRelay] Failed to subscribe to inputs: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("[InputRelay] PlayerInput or actions not available");
            }
        }

        private void OnDisable()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                try
                {
                    // Unsubscribe from all actions - using Player/ action paths
                    if (playerInput.actions.FindAction("Player/Move") != null)
                    {
                        playerInput.actions["Player/Move"].performed -= OnMovement;
                        playerInput.actions["Player/Move"].canceled -= OnMovement;
                    }
                    if (playerInput.actions.FindAction("Player/Jump") != null)
                    {
                        playerInput.actions["Player/Jump"].performed -= OnJump;
                    }
                    if (playerInput.actions.FindAction("Player/Attack") != null)
                    {
                        playerInput.actions["Player/Attack"].performed -= OnPrimaryAttack;
                    }
                    if (playerInput.actions.FindAction("Player/Home") != null)
                    {
                        playerInput.actions["Player/Home"].performed -= OnInteract;
                    }

                    if (playerInput.actions.FindAction("Player/Ability1") != null)
                    {
                        playerInput.actions["Player/Ability1"].performed -= OnAbility1Start;
                        playerInput.actions["Player/Ability1"].canceled -= OnAbility1End;
                    }
                    if (playerInput.actions.FindAction("Player/Ability2") != null)
                    {
                        playerInput.actions["Player/Ability2"].performed -= OnAbility2Start;
                        playerInput.actions["Player/Ability2"].canceled -= OnAbility2End;
                    }

                    if (playerInput.actions.FindAction("Player/Aim") != null)
                    {
                        playerInput.actions["Player/Aim"].performed -= OnCameraPan;
                    }
                    if (playerInput.actions.FindAction("Player/Pause") != null)
                    {
                        playerInput.actions["Player/Pause"].performed -= OnOpenMainMenu;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[InputRelay] Failed to unsubscribe from inputs: {e.Message}");
                }
            }
        }

        private void Update()
        {
            // Update aim position for abilities
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, Camera.main.nearClipPlane));
                aimPosition = worldMousePos;

                // Debug logging for mouse position
                if (Time.frameCount % 120 == 0) // Log every 2 seconds
                {
                    Debug.Log($"[InputRelay] Mouse position: {mousePosition}, World position: {aimPosition:F2}");
                }
            }

            // Relay inputs to state machine
            RelayInputToStateMachine();
        }

        /// <summary>
        /// Routes input to the state machine
        /// </summary>
        private void RelayInputToStateMachine()
        {
            // Get the state machine integration component
            var stateMachineIntegration = characterController?.GetComponent<StateMachineIntegration>();
            if (stateMachineIntegration == null) return;

            // Handle movement input
            if (movementInput != Vector3.zero)
            {
                // Movement is handled by the character controller's physics
                characterController.SetMovementInput(movementInput);
            }

            // Handle jump input
            if (jumpPressed)
            {
                characterController.Jump();
                jumpPressed = false; // Reset after handling
            }

            // Handle ability inputs - these will be processed by the state machine
            if (ability1Held)
            {
                // Ability input handling is done in StateMachineIntegration
                Debug.Log("Ability 1 held");
            }

            if (ability2Held)
            {
                Debug.Log("Ability 2 held");
            }

            if (ultimateHeld)
            {
                Debug.Log("Ultimate held");
            }
        }

        // Input event handlers
        private void OnMovement(InputAction.CallbackContext context)
        {
            Vector2 input2D = context.ReadValue<Vector2>();
            movementInput = new Vector3(input2D.x, 0, input2D.y); // Convert 2D input to 3D movement
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            jumpPressed = true;
        }

        private void OnPrimaryAttack(InputAction.CallbackContext context)
        {
            // Create and execute primary attack command
            if (commandManager != null && abilitySystem != null)
            {
                var command = new AbilityCommand(abilitySystem, new AbilityData { name = "PrimaryAttack" }, aimPosition);
                commandManager.ExecuteCommand(command);
            }
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            // Handle interaction - Left Alt for scoring coins
            Debug.Log("Interact pressed - Left Alt for scoring");
            // Interaction handling is now managed by the state machine
        }

        private void OnAbility1Start(InputAction.CallbackContext context)
        {
            ability1Held = true;
        }

        private void OnAbility1End(InputAction.CallbackContext context)
        {
            ability1Held = false;
            // Execute ability command when released
            if (commandManager != null && abilitySystem != null)
            {
                var command = new AbilityCommand(abilitySystem, new AbilityData { name = "Ability1" }, aimPosition);
                commandManager.ExecuteCommand(command);
            }
        }

        private void OnAbility2Start(InputAction.CallbackContext context)
        {
            ability2Held = true;
        }

        private void OnAbility2End(InputAction.CallbackContext context)
        {
            ability2Held = false;
            // Execute ability command when released
            if (commandManager != null && abilitySystem != null)
            {
                var command = new AbilityCommand(abilitySystem, new AbilityData { name = "Ability2" }, aimPosition);
                commandManager.ExecuteCommand(command);
            }
        }

        private void OnUltimateStart(InputAction.CallbackContext context)
        {
            ultimateHeld = true;
        }

        private void OnUltimateEnd(InputAction.CallbackContext context)
        {
            ultimateHeld = false;
            // Execute ultimate command when released
            if (commandManager != null && abilitySystem != null)
            {
                var command = new AbilityCommand(abilitySystem, new AbilityData { name = "Ultimate" }, aimPosition);
                commandManager.ExecuteCommand(command);
            }
        }

        private void OnCameraPan(InputAction.CallbackContext context)
        {
            // Handle camera panning
            Debug.Log("Camera pan input received");
        }

        private void OnOpenMainMenu(InputAction.CallbackContext context)
        {
            // Handle main menu opening
            Debug.Log("Main menu input received");
        }
    }

    // Input handling is now managed by the StateMachineIntegration component
    // State-specific input handling is done within each state class

}