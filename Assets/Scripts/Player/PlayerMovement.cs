using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Extracted player movement responsibility from PlayerController god object
    /// Implements Single Responsibility Principle from SOLID
    /// Based on Clean Code refactoring principles
    /// </summary>
    [System.Serializable]
    public class PlayerMovement
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 8f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;
        [SerializeField] private float maxMovementMagnitude = 2f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask groundLayer = 1;

        // Private state
        private bool isGrounded;
        private bool canDoubleJump;
        private Vector3 movementInput;
        
        // Component references
        private Transform transform;
        private Rigidbody rb;
        private UnifiedPlayerController characterController;

        // Properties
        public bool IsGrounded => isGrounded;
        public bool CanDoubleJump => canDoubleJump;
        public Vector3 MovementInput => movementInput;
        public float MovementSpeed => baseMoveSpeed;

        /// <summary>
        /// Initialize movement system with required components
        /// </summary>
        public void Initialize(Transform playerTransform, Rigidbody rigidbody, UnifiedPlayerController controller)
        {
            transform = playerTransform;
            rb = rigidbody;
            characterController = controller;
            
            // Validate required components
            if (transform == null || rb == null)
            {
                Debug.LogError("[PlayerMovement] Missing required components for initialization");
                return;
            }
            
            Debug.Log("[PlayerMovement] Movement system initialized successfully");
        }

        /// <summary>
        /// Set movement input with validation
        /// Implements defensive programming from Code Complete
        /// </summary>
        public bool SetMovementInput(Vector3 input)
        {
            // Validate input using our validation system
            var validationResult = Validate.ValidMovementInput(input, maxMovementMagnitude);
            if (!validationResult.IsSuccess)
            {
                Debug.LogWarning($"[PlayerMovement] Invalid movement input: {validationResult.Error}");
                return false;
            }

            movementInput = validationResult.Value;
            
            // Relay to character controller if available
            if (characterController != null)
            {
                characterController.SetMovementInput(movementInput);
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] No UnifiedPlayerController found to relay movement input");
            }
            
            return true;
        }

        /// <summary>
        /// Update ground detection
        /// Separated from main update loop for better performance control
        /// </summary>
        public void UpdateGroundDetection()
        {
            if (transform == null) return;

            RaycastHit hit;
            bool wasGrounded = isGrounded;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);

            // Reset double jump when landing
            if (isGrounded && !wasGrounded)
            {
                canDoubleJump = true;
                Debug.Log("[PlayerMovement] Player landed - double jump reset");
            }
        }

        /// <summary>
        /// Handle jump input with proper validation
        /// </summary>
        public bool TryJump()
        {
            if (rb == null)
            {
                Debug.LogError("[PlayerMovement] Cannot jump: Rigidbody component not found");
                return false;
            }

            // Determine jump type and force
            float jumpForceToUse;
            bool jumpAllowed = false;

            if (isGrounded)
            {
                jumpForceToUse = jumpForce;
                jumpAllowed = true;
                canDoubleJump = true; // Enable double jump after ground jump
            }
            else if (canDoubleJump)
            {
                jumpForceToUse = doubleJumpForce;
                jumpAllowed = true;
                canDoubleJump = false; // Consume double jump
            }
            else
            {
                Debug.Log("[PlayerMovement] Jump blocked - not grounded and no double jump available");
                return false;
            }

            if (jumpAllowed)
            {
                // Reset vertical velocity for consistent jump height
                Vector3 velocity = rb.linearVelocity;
                velocity.y = 0f;
                rb.linearVelocity = velocity;

                // Apply jump force
                rb.AddForce(Vector3.up * jumpForceToUse, ForceMode.Impulse);
                
                Debug.Log($"[PlayerMovement] Jump executed - Force: {jumpForceToUse}, Grounded: {isGrounded}, CanDoubleJump: {canDoubleJump}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply movement physics
        /// </summary>
        public void ApplyMovement()
        {
            if (rb == null || characterController == null) return;

            // Movement is handled by UnifiedPlayerController
            // This method can apply additional movement modifiers
            if (movementInput != Vector3.zero)
            {
                float speedModifier = GetMovementModifier();
                // Additional movement logic can be applied here
            }
        }

        /// <summary>
        /// Get movement speed modifier based on current state
        /// </summary>
        private float GetMovementModifier()
        {
            float modifier = 1f;
            
            // Reduce movement speed in air
            if (!isGrounded)
            {
                modifier *= 0.8f;
            }
            
            return modifier;
        }

        /// <summary>
        /// Check if a position is valid for movement
        /// Defensive programming to prevent invalid positions
        /// </summary>
        public bool IsValidPosition(Vector3 position)
        {
            // Check for NaN or infinity
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
                return false;
                
            if (float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
                return false;

            // Add bounds checking if needed
            // This could check world boundaries, collision, etc.
            
            return true;
        }

        /// <summary>
        /// Reset movement state
        /// </summary>
        public void ResetMovementState()
        {
            movementInput = Vector3.zero;
            isGrounded = false;
            canDoubleJump = false;
            
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Debug.Log("[PlayerMovement] Movement state reset");
        }

        /// <summary>
        /// Get movement status for debugging
        /// </summary>
        public string GetMovementStatus()
        {
            return $"Grounded: {isGrounded}, CanDoubleJump: {canDoubleJump}, Input: {movementInput}, Speed: {baseMoveSpeed}";
        }
    }
}
