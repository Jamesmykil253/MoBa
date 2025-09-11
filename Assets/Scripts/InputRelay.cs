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
        [SerializeField] private HoldToAimSystem holdToAimSystem;
        [SerializeField] private CryptoCoinSystem cryptoCoinSystem;
        [SerializeField] private RSBCombatSystem rsbCombatSystem;

        // Public properties for external access
        public MOBACharacterController CharacterController => characterController;
        public CommandManager CommandManager => commandManager;
        public AbilitySystem AbilitySystem => abilitySystem;
        public HoldToAimSystem HoldToAimSystem => holdToAimSystem;
        public CryptoCoinSystem CryptoCoinSystem => cryptoCoinSystem;
        public RSBCombatSystem RSBCombatSystem => rsbCombatSystem;

        private PlayerInput playerInput;
        private InputActionAsset inputActions;

        // Input state tracking
        private Vector3 movementInput;
        private bool jumpPressed;
        private bool ability1Held;
        private bool ability2Held;
        private bool ultimateHeld;
        private bool scorePressed;
        private Vector3 aimPosition;

        private void Awake()
        {
            InitializePlayerInput();
            CacheComponentReferences();
        }

        /// <summary>
        /// Initialize PlayerInput component with proper error handling
        /// Following Clean Code principle of single responsibility
        /// </summary>
        private void InitializePlayerInput()
        {
            // Get PlayerInput component
            playerInput = GetComponent<PlayerInput>();

            // Load input actions if not assigned
            if (inputActions == null)
            {
                // Use the InputActionAsset from the PlayerInput component if available
                inputActions = playerInput.actions;
                if (inputActions != null && playerInput != null)
                {
                    playerInput.actions = inputActions;
                    Debug.Log("[InputRelay] ✅ InputActions loaded successfully");
                }
            }
        }

        /// <summary>
        /// Cache component references to avoid expensive FindAnyObjectByType calls
        /// Implements dependency injection pattern from Clean Code principles
        /// </summary>
        private void CacheComponentReferences()
        {
            // Find dependencies if not assigned - only do this once in Awake
            if (characterController == null)
            {
                characterController = GetComponent<MOBACharacterController>();
                if (characterController == null && Application.isPlaying)
                {
                    // Only use expensive search as last resort, and cache the result
                    characterController = FindCharacterControllerInScene();
                }
            }

            // Cache other dependencies with null-safe checks
            CacheDependency(ref commandManager, "CommandManager");
            CacheDependency(ref abilitySystem, "AbilitySystem");
            CacheDependency(ref holdToAimSystem, "HoldToAimSystem");
            CacheDependency(ref cryptoCoinSystem, "CryptoCoinSystem");
            CacheDependency(ref rsbCombatSystem, "RSBCombatSystem");
        }

        /// <summary>
        /// Generic method to cache dependencies with proper error handling
        /// Reduces code duplication following DRY principle
        /// </summary>
        private void CacheDependency<T>(ref T dependency, string componentName) where T : MonoBehaviour
        {
            if (dependency == null && Application.isPlaying)
            {
                dependency = Object.FindAnyObjectByType<T>();
                if (dependency == null)
                {
                    Debug.LogWarning($"[InputRelay] {componentName} not found in scene. {componentName} features will not work until one is available.");
                }
                else
                {
                    Debug.Log($"[InputRelay] ✅ {componentName} cached successfully");
                }
            }
        }

        /// <summary>
        /// Specialized method to find character controller with fallback logic
        /// </summary>
        private MOBACharacterController FindCharacterControllerInScene()
        {
            // Try to find on this GameObject first
            var controller = GetComponent<MOBACharacterController>();
            if (controller != null) return controller;

            // Try to find on parent
            controller = GetComponentInParent<MOBACharacterController>();
            if (controller != null) return controller;

            // Last resort: search scene (expensive)
            controller = Object.FindAnyObjectByType<MOBACharacterController>();
            if (controller != null)
            {
                Debug.Log($"[InputRelay] ✅ Found character controller on {controller.gameObject.name}");
            }

            return controller;
        }

        private void OnEnable()
        {
            if (playerInput?.actions == null)
            {
                Debug.LogWarning("[InputRelay] PlayerInput or actions not available");
                return;
            }

            try
            {
                // Movement and basic actions - using correct action map structure
                SubscribeToActionSafely("Move", OnMovement, OnMovement);
                SubscribeToActionSafely("Jump", OnJump);
                SubscribeToActionSafely("Attack", OnPrimaryAttack);
                SubscribeToActionSafely("Home", OnInteract);

                // Ability inputs with hold-to-aim mechanics (per Game Programming Patterns - Command Pattern)
                SubscribeToActionSafely("Ability1", OnAbility1Start, OnAbility1End);
                SubscribeToActionSafely("Ability2", OnAbility2Start, OnAbility2End);

                // Scoring system input (Left Alt key per CONTROLS.md)
                SubscribeToActionSafely("Score", OnScoreStart, OnScoreEnd);

                // Camera and UI actions
                SubscribeToActionSafely("Aim", OnCameraPan);

                Debug.Log("[InputRelay] ✅ Input subscriptions successful");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InputRelay] ❌ Failed to subscribe to inputs: {e.Message}");
            }
        }

        /// <summary>
        /// Safely subscribe to an input action with proper error handling
        /// Based on Clean Code principles - defensive programming
        /// </summary>
        private void SubscribeToActionSafely(string actionName, System.Action<InputAction.CallbackContext> performedCallback, System.Action<InputAction.CallbackContext> canceledCallback = null)
        {
            var action = playerInput.actions.FindAction(actionName);
            if (action != null)
            {
                action.performed += performedCallback;
                if (canceledCallback != null)
                {
                    action.canceled += canceledCallback;
                }
            }
            else
            {
                Debug.LogWarning($"[InputRelay] Action '{actionName}' not found in input actions");
            }
        }

        private void OnDisable()
        {
            if (playerInput?.actions == null) return;

            try
            {
                // Unsubscribe from all actions using the same safe method
                UnsubscribeFromActionSafely("Move", OnMovement, OnMovement);
                UnsubscribeFromActionSafely("Jump", OnJump);
                UnsubscribeFromActionSafely("Attack", OnPrimaryAttack);
                UnsubscribeFromActionSafely("Home", OnInteract);
                UnsubscribeFromActionSafely("Ability1", OnAbility1Start, OnAbility1End);
                UnsubscribeFromActionSafely("Ability2", OnAbility2Start, OnAbility2End);
                UnsubscribeFromActionSafely("Score", OnScoreStart, OnScoreEnd);
                UnsubscribeFromActionSafely("Aim", OnCameraPan);

                Debug.Log("[InputRelay] ✅ Input unsubscriptions successful");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[InputRelay] ❌ Failed to unsubscribe from inputs: {e.Message}");
            }
        }

        /// <summary>
        /// Safely unsubscribe from an input action with proper error handling
        /// Follows Clean Code defensive programming principles
        /// </summary>
        private void UnsubscribeFromActionSafely(string actionName, System.Action<InputAction.CallbackContext> performedCallback, System.Action<InputAction.CallbackContext> canceledCallback = null)
        {
            var action = playerInput.actions.FindAction(actionName);
            if (action != null)
            {
                action.performed -= performedCallback;
                if (canceledCallback != null)
                {
                    action.canceled -= canceledCallback;
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

            // Handle score input
            if (scorePressed)
            {
                // Score input is handled by calling StartScoring
                var cryptoCoinSystem = FindAnyObjectByType<CryptoCoinSystem>();
                cryptoCoinSystem?.StartScoring();
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
            // FIXED: Ensure projectiles spawn from player, not camera
            Vector3 playerPosition = transform.position + transform.forward * 1f + Vector3.up * 0.5f;
            
            // Create and execute primary attack command
            if (commandManager != null && abilitySystem != null)
            {
                var command = new AbilityCommand(abilitySystem, new AbilityData { name = "PrimaryAttack" }, aimPosition);
                commandManager.ExecuteCommand(command);
            }
            
            // FALLBACK: If command system doesn't work, spawn projectile directly
            var projectilePool = FindAnyObjectByType<ProjectilePool>();
            if (projectilePool != null)
            {
                Vector3 direction = (aimPosition - playerPosition).normalized;
                if (direction.magnitude < 0.1f) // If no aim direction, use forward
                {
                    direction = transform.forward;
                }
                
                projectilePool.SpawnProjectile(playerPosition, direction, 15f, 50f, 3f);
                Debug.Log($"[InputRelay] Direct projectile spawn from player position: {playerPosition}");
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
            
            // Start hold-to-aim for Ability1 (per GAMEPLAY.md - manual aim required)
            if (holdToAimSystem != null && abilitySystem != null)
            {
                var ability1Data = new AbilityData 
                { 
                    name = "Ability1", 
                    damage = 100f, 
                    range = 8f, 
                    speed = 15f
                };
                holdToAimSystem.StartHoldToAim(ability1Data);
            }
        }

        private void OnAbility1End(InputAction.CallbackContext context)
        {
            ability1Held = false;
            
            // Execute aimed ability when button released (per CONTROLS.md hold-to-aim spec)
            if (holdToAimSystem != null)
            {
                holdToAimSystem.ExecuteAimedAbility();
            }
        }

        private void OnAbility2Start(InputAction.CallbackContext context)
        {
            ability2Held = true;
            
            // Start hold-to-aim for Ability2 (per GAMEPLAY.md - manual aim required)
            if (holdToAimSystem != null && abilitySystem != null)
            {
                var ability2Data = new AbilityData 
                { 
                    name = "Ability2", 
                    damage = 120f, 
                    range = 12f, 
                    speed = 10f
                };
                holdToAimSystem.StartHoldToAim(ability2Data);
            }
        }

        private void OnAbility2End(InputAction.CallbackContext context)
        {
            ability2Held = false;
            
            // Execute aimed ability when button released
            if (holdToAimSystem != null)
            {
                holdToAimSystem.ExecuteAimedAbility();
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
            // Handle both camera panning and aim input based on current state
            Vector2 inputValue = context.ReadValue<Vector2>();
            
            // If holding to aim, update aim position
            if (holdToAimSystem != null && holdToAimSystem.GetAimInfo().isAiming)
            {
                holdToAimSystem.UpdateAimInput(inputValue);
            }
            else
            {
                // FIXED: Suppress camera pan logging to reduce spam
                // Camera panning is handled by the camera controller directly
                // Only log if input is significant and not too frequent
                if (inputValue.magnitude > 1f && Time.frameCount % 300 == 0) // Log every 5 seconds max
                {
                    Debug.Log($"[InputRelay] Significant camera pan detected: {inputValue}");
                }
            }
        }

        private void OnOpenMainMenu(InputAction.CallbackContext context)
        {
            // Handle main menu opening
            Debug.Log("Main menu input received");
        }

        /// <summary>
        /// Handle score input start (Left Alt pressed)
        /// Initiates crypto coin scoring sequence per CONTROLS.md
        /// </summary>
        private void OnScoreStart(InputAction.CallbackContext context)
        {
            scorePressed = true;
            
            // Start scoring sequence through CryptoCoinSystem
            if (cryptoCoinSystem != null)
            {
                cryptoCoinSystem.StartScoring();
                Debug.Log("[InputRelay] Score input started - initiating coin scoring");
            }
            else
            {
                Debug.LogWarning("[InputRelay] CryptoCoinSystem not found for scoring");
            }
        }

        /// <summary>
        /// Handle score input end (Left Alt released)
        /// Stops scoring sequence if interrupted
        /// </summary>
        private void OnScoreEnd(InputAction.CallbackContext context)
        {
            scorePressed = false;
            
            // Stop scoring if released early (interruption)
            if (cryptoCoinSystem != null && cryptoCoinSystem.IsCurrentlyScoring())
            {
                cryptoCoinSystem.StopScoring();
                Debug.Log("[InputRelay] Score input ended - scoring interrupted");
            }
        }
    }

    // Input handling is now managed by the StateMachineIntegration component
    // State-specific input handling is done within each state class

}