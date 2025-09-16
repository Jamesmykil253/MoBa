using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Unified jump controller following DRY principle from Pragmatic Programmer
    /// Single source of truth for jump mechanics shared between local and network controllers
    /// Implements Clean Code single responsibility principle
    /// </summary>
    [System.Serializable]
    public class JumpController
    {
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;
        [SerializeField] private bool allowDoubleJump = true;

        // Private state - encapsulation following Clean Code
        private bool isGrounded;
        private bool canDoubleJump;
        private Rigidbody rigidBody;

        // Events for observer pattern - orthogonal design from Pragmatic Programmer
        public event System.Action OnJumpStarted;
        public event System.Action OnDoubleJumpStarted;
        public event System.Action OnLanded;

        /// <summary>
        /// Initialize jump controller with required components
        /// Defensive programming - validate dependencies
        /// </summary>
        public void Initialize(Rigidbody rb)
        {
            if (rb == null)
            {
                Debug.LogError("[JumpController] Rigidbody cannot be null");
                return;
            }

            rigidBody = rb;
            canDoubleJump = true;
            Debug.Log("[JumpController] Initialized successfully");
        }

        /// <summary>
        /// Update ground state - must be called each frame
        /// Clean Code - clear method name indicates responsibility
        /// </summary>
        public void UpdateGroundState(bool grounded)
        {
            bool wasGrounded = isGrounded;
            isGrounded = grounded;

            // Reset double jump when landing - consistent logic
            if (isGrounded && !wasGrounded)
            {
                canDoubleJump = true;
                OnLanded?.Invoke();
            }
        }

        /// <summary>
        /// Attempt to perform jump - unified logic for all controllers
        /// Returns true if jump was executed, false if not allowed
        /// Clean Code - boolean return indicates success/failure
        /// </summary>
        public bool TryJump()
        {
            if (rigidBody == null)
            {
                Debug.LogWarning("[JumpController] Cannot jump - no rigidbody assigned");
                return false;
            }

            // Primary jump - when grounded
            if (isGrounded)
            {
                PerformJump(jumpForce);
                canDoubleJump = allowDoubleJump; // Enable double jump after primary jump
                OnJumpStarted?.Invoke();
                return true;
            }
            // Double jump - when airborne and allowed
            else if (canDoubleJump && allowDoubleJump)
            {
                PerformJump(doubleJumpForce);
                canDoubleJump = false; // Consume double jump
                OnDoubleJumpStarted?.Invoke();
                return true;
            }

            return false; // Jump not allowed
        }

        /// <summary>
        /// Force a jump regardless of ground state - for special abilities
        /// Pragmatic Programmer - provide escape hatch for edge cases
        /// </summary>
        public void ForceJump(float customForce = -1f)
        {
            if (rigidBody == null) return;

            float force = customForce > 0 ? customForce : jumpForce;
            PerformJump(force);
            OnJumpStarted?.Invoke();
        }

        /// <summary>
        /// Private method encapsulating actual jump physics
        /// Clean Code - extract method to avoid duplication
        /// </summary>
        private void PerformJump(float force)
        {
            // Preserve horizontal velocity, set vertical velocity
            Vector3 velocity = rigidBody.linearVelocity;
            velocity.y = force;
            rigidBody.linearVelocity = velocity;
        }

        // Public read-only properties for state inspection
        public bool IsGrounded => isGrounded;
        public bool CanDoubleJump => canDoubleJump;
        public float JumpForce => jumpForce;
        public float DoubleJumpForce => doubleJumpForce;

        /// <summary>
        /// Reset jump state - useful for respawning or teleportation
        /// Clean Code - clear method name and single responsibility
        /// </summary>
        public void ResetJumpState()
        {
            canDoubleJump = true;
            isGrounded = false;
        }

        /// <summary>
        /// Configure jump parameters at runtime
        /// Pragmatic Programmer - allow runtime configuration for game balance
        /// </summary>
        public void ConfigureJump(float newJumpForce, float newDoubleJumpForce, bool enableDoubleJump)
        {
            jumpForce = Mathf.Max(0, newJumpForce); // Defensive programming
            doubleJumpForce = Mathf.Max(0, newDoubleJumpForce);
            allowDoubleJump = enableDoubleJump;
        }
    }
}
