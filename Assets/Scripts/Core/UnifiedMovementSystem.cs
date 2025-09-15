using UnityEngine;
using Unity.Netcode;

namespace MOBA
{
    /// <summary>
    /// Unified Movement System that consolidates all movement implementations
    /// Supports both local and networked movement with validation
    /// Replaces: SimplePlayerController movement, PlayerMovement class
    /// </summary>
    [System.Serializable]
    public class UnifiedMovementSystem
    {
        [Header("Movement Configuration")]
        [SerializeField] private float baseMoveSpeed = 8f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask groundLayer = 1;
        [SerializeField] private bool enableNetworkValidation = true;

        [Header("Network Settings")]
        [SerializeField] private float maxMovementSpeed = 15f;
        [SerializeField] private float teleportThreshold = 20f;
        [SerializeField] private float maxInputMagnitude = 1.1f;

        // State
        private Vector3 movementInput = Vector3.zero;
        private Vector3 lastValidPosition = Vector3.zero;
        private bool isGrounded = false;
        private float lastGroundedTime = 0f;

        // Component references
        private Transform transform;
        private Rigidbody rigidbody;
        private NetworkObject networkObject;
        private bool isNetworked = false;

        // Events
        public System.Action<Vector3> OnMovementInputChanged;
        public System.Action<bool> OnGroundedStateChanged;
        public System.Action<Vector3> OnPositionValidated;

        #region Initialization

        /// <summary>
        /// Initialize the movement system with required components
        /// </summary>
        public void Initialize(Transform playerTransform, Rigidbody playerRigidbody, NetworkObject netObj = null)
        {
            transform = playerTransform;
            rigidbody = playerRigidbody;
            networkObject = netObj;
            isNetworked = networkObject != null;
            
            if (transform == null || rigidbody == null)
            {
                Debug.LogError("[UnifiedMovementSystem] Missing required components (Transform or Rigidbody)");
                return;
            }
            
            lastValidPosition = transform.position;
            
            Debug.Log($"[UnifiedMovementSystem] Initialized {(isNetworked ? "networked" : "local")} movement system");
        }

        #endregion

        #region Input Handling

        /// <summary>
        /// Set movement input with validation
        /// </summary>
        public void SetMovementInput(Vector3 input)
        {
            // Validate input magnitude
            if (input.magnitude > maxInputMagnitude)
            {
                input = input.normalized * maxInputMagnitude;
                if (enableNetworkValidation)
                {
                    Debug.LogWarning($"[UnifiedMovementSystem] Movement input clamped from {input.magnitude} to {maxInputMagnitude}");
                }
            }

            // Check for NaN values
            if (float.IsNaN(input.x) || float.IsNaN(input.y) || float.IsNaN(input.z))
            {
                Debug.LogError("[UnifiedMovementSystem] Invalid movement input (NaN detected)");
                return;
            }

            var previousInput = movementInput;
            movementInput = Vector3.ClampMagnitude(input, 1f);
            
            // Notify input change
            if (previousInput != movementInput)
            {
                OnMovementInputChanged?.Invoke(movementInput);
                
                // Publish local event
                UnifiedEventSystem.PublishLocal(new MovementInputChangedEvent(
                    transform.gameObject, previousInput, movementInput));
            }
        }

        /// <summary>
        /// Handle jump input with ground validation
        /// </summary>
        public bool TryJump()
        {
            if (rigidbody == null || !isGrounded) return false;

            // Check if enough time has passed since last grounded
            if (Time.time - lastGroundedTime < 0.1f) return false;

            // Network validation for jump
            if (isNetworked && enableNetworkValidation)
            {
                if (!ValidateJumpInput())
                {
                    Debug.LogWarning("[UnifiedMovementSystem] Jump input failed network validation");
                    return false;
                }
            }

            rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            
            // Publish jump event
            UnifiedEventSystem.PublishLocal(new JumpPerformedEvent(transform.gameObject, jumpForce));
            
            return true;
        }

        #endregion

        #region Movement Execution

        /// <summary>
        /// Update the movement system (call from Update)
        /// </summary>
        public void UpdateMovement()
        {
            UpdateGroundDetection();
            ApplyMovement();
            
            if (isNetworked && enableNetworkValidation)
            {
                ValidatePosition();
            }
        }

        /// <summary>
        /// Apply movement physics with validation
        /// </summary>
        private void ApplyMovement()
        {
            if (rigidbody == null || transform == null) return;

            Vector3 movement = movementInput * GetCurrentMoveSpeed() * Time.deltaTime;
            
            // Validate movement distance
            if (movement.magnitude > maxMovementSpeed * Time.deltaTime)
            {
                movement = movement.normalized * maxMovementSpeed * Time.deltaTime;
                if (enableNetworkValidation)
                {
                    Debug.LogWarning("[UnifiedMovementSystem] Movement speed clamped to maximum");
                }
            }

            // Apply movement
            Vector3 newPosition = transform.position + movement;
            rigidbody.MovePosition(newPosition);
            
            // Update last valid position
            if (!isNetworked || ValidatePositionChange(transform.position, newPosition))
            {
                lastValidPosition = newPosition;
            }
        }

        /// <summary>
        /// Get current movement speed with modifiers
        /// </summary>
        private float GetCurrentMoveSpeed()
        {
            float currentSpeed = baseMoveSpeed;
            
            // Apply ground state modifier
            if (!isGrounded)
            {
                currentSpeed *= 0.8f; // Reduce air movement speed
            }
            
            return currentSpeed;
        }

        #endregion

        #region Ground Detection

        /// <summary>
        /// Update ground detection state
        /// </summary>
        private void UpdateGroundDetection()
        {
            if (transform == null) return;

            bool wasGrounded = isGrounded;
            
            RaycastHit hit;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
            
            if (isGrounded && !wasGrounded)
            {
                lastGroundedTime = Time.time;
                OnGroundedStateChanged?.Invoke(true);
                
                // Publish landing event
                UnifiedEventSystem.PublishLocal(new LandingEvent(transform.gameObject, hit.point));
            }
            else if (!isGrounded && wasGrounded)
            {
                OnGroundedStateChanged?.Invoke(false);
                
                // Publish takeoff event
                UnifiedEventSystem.PublishLocal(new TakeoffEvent(transform.gameObject, transform.position));
            }
        }

        #endregion

        #region Network Validation

        /// <summary>
        /// Validate position changes for anti-cheat
        /// </summary>
        private bool ValidatePositionChange(Vector3 oldPos, Vector3 newPos)
        {
            float distance = Vector3.Distance(oldPos, newPos);
            
            // Check for teleportation
            if (distance > teleportThreshold)
            {
                Debug.LogWarning($"[UnifiedMovementSystem] Teleportation detected: {distance}m movement");
                return false;
            }
            
            // Check for excessive speed
            float maxDistancePerFrame = maxMovementSpeed * Time.deltaTime;
            if (distance > maxDistancePerFrame)
            {
                Debug.LogWarning($"[UnifiedMovementSystem] Excessive speed detected: {distance}m in {Time.deltaTime}s");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Validate jump input timing and conditions
        /// </summary>
        private bool ValidateJumpInput()
        {
            // Check if grounded (server authority)
            if (!isGrounded)
            {
                return false;
            }
            
            // Check jump cooldown
            if (Time.time - lastGroundedTime < 0.1f)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Validate current position against expected bounds
        /// </summary>
        private void ValidatePosition()
        {
            if (transform == null) return;
            
            Vector3 currentPos = transform.position;
            
            // Check if position is valid
            if (ValidatePositionChange(lastValidPosition, currentPos))
            {
                lastValidPosition = currentPos;
                OnPositionValidated?.Invoke(currentPos);
            }
            else
            {
                // Reset to last valid position
                Debug.LogWarning("[UnifiedMovementSystem] Invalid position detected, resetting to last valid position");
                transform.position = lastValidPosition;
                rigidbody.velocity = Vector3.zero;
            }
        }

        #endregion

        #region Public Properties

        public bool IsGrounded => isGrounded;
        public Vector3 MovementInput => movementInput;
        public float MovementSpeed => baseMoveSpeed;
        public Vector3 LastValidPosition => lastValidPosition;
        public bool IsNetworked => isNetworked;

        #endregion

        #region Debug

        /// <summary>
        /// Draw debug information in scene view
        /// </summary>
        public void DrawDebugInfo()
        {
            if (transform == null) return;
            
            // Ground check ray
            Debug.DrawRay(transform.position, Vector3.down * groundCheckDistance, 
                         isGrounded ? Color.green : Color.red);
            
            // Movement direction
            if (movementInput != Vector3.zero)
            {
                Debug.DrawRay(transform.position, movementInput * 2f, Color.blue);
            }
        }

        #endregion
    }

    #region Movement Events

    /// <summary>
    /// Movement input changed event
    /// </summary>
    public class MovementInputChangedEvent : ILocalEvent
    {
        public GameObject Character { get; }
        public Vector3 PreviousInput { get; }
        public Vector3 NewInput { get; }

        public MovementInputChangedEvent(GameObject character, Vector3 previousInput, Vector3 newInput)
        {
            Character = character;
            PreviousInput = previousInput;
            NewInput = newInput;
        }
    }

    /// <summary>
    /// Jump performed event
    /// </summary>
    public class JumpPerformedEvent : ILocalEvent
    {
        public GameObject Character { get; }
        public float JumpForce { get; }

        public JumpPerformedEvent(GameObject character, float jumpForce)
        {
            Character = character;
            JumpForce = jumpForce;
        }
    }

    /// <summary>
    /// Landing event
    /// </summary>
    public class LandingEvent : ILocalEvent
    {
        public GameObject Character { get; }
        public Vector3 LandingPosition { get; }

        public LandingEvent(GameObject character, Vector3 landingPosition)
        {
            Character = character;
            LandingPosition = landingPosition;
        }
    }

    /// <summary>
    /// Takeoff event
    /// </summary>
    public class TakeoffEvent : ILocalEvent
    {
        public GameObject Character { get; }
        public Vector3 TakeoffPosition { get; }

        public TakeoffEvent(GameObject character, Vector3 takeoffPosition)
        {
            Character = character;
            TakeoffPosition = takeoffPosition;
        }
    }

    #endregion
}
