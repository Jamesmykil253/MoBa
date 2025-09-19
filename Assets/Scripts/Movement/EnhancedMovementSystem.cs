using UnityEngine;
using Unity.Netcode;

namespace MOBA.Movement
{
    /// <summary>
    /// Enhanced movement system using explicit State Pattern for complex movement behaviors
    /// Reference: Game Programming Patterns Chapter 6 - State Pattern implementation
    /// Provides professional state management for grounded, airborne, and dashing movement
    /// </summary>
    [System.Serializable]
    public class EnhancedMovementSystem
    {
        #region State Management
        
        private MovementState currentState;
        private MovementContext context;
        private bool isInitialized = false;
        
        // Debug information
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private bool showStateTransitions = true;
        
        #endregion
        
        #region Configuration
        
        [Header("Movement Configuration")]
        [SerializeField] private MovementSettings settings = new MovementSettings();
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when movement state changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<MovementState, MovementState> OnStateChanged;
        
        /// <summary>
        /// Raised when movement input changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<Vector3> OnMovementInputChanged;
        
        /// <summary>
        /// Raised when grounded state changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<bool> OnGroundedStateChanged;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the enhanced movement system with required components
        /// </summary>
        /// <param name="transform">Player transform component</param>
        /// <param name="rigidbody">Player rigidbody component</param>
        /// <param name="networkObject">Network object for multiplayer support (optional)</param>
        public void Initialize(Transform transform, Rigidbody rigidbody, NetworkObject networkObject = null)
        {
            // Initialize movement context
            context = new MovementContext();
            context.Initialize(transform, rigidbody, networkObject);
            
            // Copy configuration settings to context
            CopySettingsToContext();
            
            // Subscribe to context events
            context.OnMovementInputChanged += (input) => OnMovementInputChanged?.Invoke(input);
            context.OnGroundedStateChanged += (grounded) => OnGroundedStateChanged?.Invoke(grounded);
            context.OnStateChanged += HandleStateTransition;
            
            // Start in grounded state
            TransitionToState(new GroundedMovementState());
            
            isInitialized = true;
            
            if (enableDebugLogging)
            {
                Debug.Log("[EnhancedMovementSystem] Initialized with explicit state pattern");
            }
        }
        
        /// <summary>
        /// Copy serialized settings to the movement context
        /// </summary>
        private void CopySettingsToContext()
        {
            if (context == null) return;
            
            // Copy movement settings from serialized configuration
            // This would be expanded based on the actual MovementContext.MovementSettings structure
            context.BaseMoveSpeed = settings.baseMoveSpeed;
            context.JumpForce = settings.jumpForce;
            context.HoldJumpMultiplier = settings.holdJumpMultiplier;
            context.DoubleJumpMultiplier = settings.doubleJumpMultiplier;
            context.ApexBoostMultiplier = settings.apexBoostMultiplier;
            context.ApexVelocityThreshold = settings.apexVelocityThreshold;
            context.ApexTimeWindow = settings.apexTimeWindow;
            context.GroundCheckDistance = settings.groundCheckDistance;
            context.GroundLayer = settings.groundLayer;
            context.EnableNetworkValidation = settings.enableNetworkValidation;
            context.MaxMovementSpeed = settings.maxMovementSpeed;
            context.TeleportThreshold = settings.teleportThreshold;
            context.MaxInputMagnitude = settings.maxInputMagnitude;
            context.DashForce = settings.dashForce;
            context.DashDuration = settings.dashDuration;
            context.DashCooldown = settings.dashCooldown;
            context.DashIgnoresGravity = settings.dashIgnoresGravity;
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Update movement system - call this every frame
        /// </summary>
        public void Update()
        {
            if (!isInitialized || currentState == null || context == null)
                return;
                
            // Update current state and handle potential transitions
            MovementState nextState = currentState.Update(context);
            
            if (nextState != null)
            {
                TransitionToState(nextState);
            }
        }
        
        /// <summary>
        /// Set movement input with validation
        /// </summary>
        /// <param name="input">Movement input vector</param>
        public void SetMovementInput(Vector3 input)
        {
            if (!isInitialized || context == null)
                return;
                
            // Validate input through context
            Vector3 validatedInput = context.ValidateInput(input);
            
            // Handle input through current state
            currentState?.HandleInput(context, validatedInput);
            
            // Update context movement input
            Vector3 previousInput = context.MovementInput;
            context.MovementInput = validatedInput;
            
            // Notify of input change
            if (previousInput != validatedInput)
            {
                OnMovementInputChanged?.Invoke(validatedInput);
                
                // Publish movement input event
                UnifiedEventSystem.PublishLocal(new MovementInputChangedEvent(
                    context.Transform.gameObject, previousInput, validatedInput));
            }
        }
        
        /// <summary>
        /// Execute jump input
        /// </summary>
        /// <returns>True if jump was executed, false otherwise</returns>
        public bool TryExecuteJump()
        {
            if (!isInitialized || currentState == null || context == null)
                return false;
                
            return currentState.HandleJumpInput(context);
        }
        
        /// <summary>
        /// Try to apply hold boost for enhanced jumping
        /// </summary>
        /// <returns>True if hold boost was applied, false otherwise</returns>
        public bool TryApplyHoldBoost()
        {
            if (!isInitialized || context == null)
                return false;
                
            // Check if current state is airborne and can handle hold boost
            if (currentState is AirborneMovementState airborneState)
            {
                return airborneState.TryApplyHoldBoost(context);
            }
            
            return false;
        }
        
        /// <summary>
        /// Initiate dash movement
        /// </summary>
        /// <returns>True if dash was started, false if on cooldown or already dashing</returns>
        public bool TryStartDash()
        {
            if (!isInitialized || context == null)
                return false;
                
            // Check if dash is available
            if (!context.IsDashAvailable() || context.IsDashing)
                return false;
                
            // Transition to dashing state
            TransitionToState(new DashingMovementState());
            return true;
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Transition to a new movement state
        /// </summary>
        /// <param name="newState">The state to transition to</param>
        private void TransitionToState(MovementState newState)
        {
            if (newState == null)
                return;
                
            MovementState previousState = currentState;
            
            // Exit current state
            currentState?.Exit(context);
            
            // Change state
            currentState = newState;
            
            // Enter new state
            currentState.Enter(context);
            
            // Notify of state change
            OnStateChanged?.Invoke(previousState, currentState);
            context.OnStateChanged?.Invoke(previousState, currentState);
            
            if (showStateTransitions && enableDebugLogging)
            {
                string prevName = previousState?.GetType().Name ?? "None";
                string newName = currentState.GetType().Name;
                Debug.Log($"[EnhancedMovementSystem] State transition: {prevName} → {newName}");
            }
        }
        
        /// <summary>
        /// Handle state transition events from context
        /// </summary>
        private void HandleStateTransition(MovementState oldState, MovementState newState)
        {
            // Additional logic for state transition handling if needed
            // This could include analytics, sound effects, visual effects, etc.
        }
        
        #endregion
        
        #region Public Accessors
        
        /// <summary>
        /// Get current movement state for debugging or external logic
        /// </summary>
        /// <returns>Current movement state instance</returns>
        public MovementState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Get current movement context for advanced interactions
        /// </summary>
        /// <returns>Movement context instance</returns>
        public MovementContext GetContext()
        {
            return context;
        }
        
        /// <summary>
        /// Check if player is currently grounded
        /// </summary>
        /// <returns>True if grounded, false if airborne</returns>
        public bool IsGrounded()
        {
            return context?.IsGrounded ?? false;
        }
        
        /// <summary>
        /// Check if player is currently dashing
        /// </summary>
        /// <returns>True if dashing, false otherwise</returns>
        public bool IsDashing()
        {
            return context?.IsDashing ?? false;
        }
        
        /// <summary>
        /// Get current movement input
        /// </summary>
        /// <returns>Current movement input vector</returns>
        public Vector3 GetMovementInput()
        {
            return context?.MovementInput ?? Vector3.zero;
        }
        
        /// <summary>
        /// Get current velocity
        /// </summary>
        /// <returns>Current velocity vector</returns>
        public Vector3 GetVelocity()
        {
            return context?.GetVelocity() ?? Vector3.zero;
        }
        
        /// <summary>
        /// Get debug information about current state
        /// </summary>
        /// <returns>Debug string with state information</returns>
        public string GetDebugInfo()
        {
            if (!isInitialized || currentState == null)
                return "EnhancedMovementSystem (Not Initialized)";
                
            return currentState.GetDebugInfo();
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clean up movement system and unsubscribe from events
        /// </summary>
        public void Cleanup()
        {
            // Exit current state
            currentState?.Exit(context);
            
            // Unsubscribe from context events
            if (context != null)
            {
                context.OnMovementInputChanged = null;
                context.OnGroundedStateChanged = null;
                context.OnStateChanged = null;
                context.OnPositionValidated = null;
            }
            
            // Clear event subscriptions
            OnStateChanged = null;
            OnMovementInputChanged = null;
            OnGroundedStateChanged = null;
            
            // Clear references
            currentState = null;
            context = null;
            isInitialized = false;
            
            if (enableDebugLogging)
            {
                Debug.Log("[EnhancedMovementSystem] Cleanup complete");
            }
        }
        
        #endregion
    }
}

namespace MOBA.Movement
{
    /// <summary>
    /// Serializable movement settings for inspector configuration
    /// </summary>
    [System.Serializable]
    public class MovementSettings
    {
        [Header("Basic Movement")]
        public float baseMoveSpeed = 8f;
        public float jumpForce = 8f;
        public float holdJumpMultiplier = 1.5f;
        public float doubleJumpMultiplier = 2.0f;
        public float apexBoostMultiplier = 2.8f;
        public float apexVelocityThreshold = 1.0f;
        public float apexTimeWindow = 0.4f;
        
        [Header("Ground Detection")]
        public float groundCheckDistance = 1.1f;
        public LayerMask groundLayer = 1;
        
        [Header("Network Validation")]
        public bool enableNetworkValidation = true;
        public float maxMovementSpeed = 15f;
        public float teleportThreshold = 20f;
        public float maxInputMagnitude = 1.1f;
        
        [Header("Dash Settings")]
        public float dashForce = 20f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 3f;
        public bool dashIgnoresGravity = true;
    }
}