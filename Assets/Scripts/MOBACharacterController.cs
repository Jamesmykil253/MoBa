using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Character controller that manages the hierarchical state machine
    /// Integrates with InputRelay for state-based input handling
    /// </summary>
    public class MOBACharacterController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 350f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;

        // Public properties for state access
        public float JumpForce => jumpForce;
        public float DoubleJumpForce => doubleJumpForce;

        [Header("Physics")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Collider groundCollider;

        // State machine will be handled by StateMachineIntegration

        // State tracking
        private bool isGrounded;
        private bool canDoubleJump;
        private Vector3 movementInput;

        // Public property for movement input
        public Vector3 MovementInput => movementInput;

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            // Improved ground detection based on Clean Code principles
            bool wasGrounded = isGrounded;
            UpdateGroundDetection();

            // Debug ground state changes
            if (wasGrounded != isGrounded)
            {
                Debug.Log($"[MOVEMENT] Ground state changed: {(isGrounded ? "Landed" : "Airborne")} at {transform.position:F1}");
            }

            // Debug movement state every 2 seconds
            if (Time.frameCount % 120 == 0)
            {
                LogMovementState();
            }
        }

        /// <summary>
        /// Logs detailed movement state for debugging
        /// </summary>
        private void LogMovementState()
        {
            Vector3 velocity = rb.linearVelocity;
            Debug.Log($"[MOVEMENT] Pos: {transform.position:F1} | Vel: {velocity:F1} (Speed: {velocity.magnitude:F1}m/s) | " +
                     $"Input: {movementInput:F1} | Grounded: {isGrounded} | CanDoubleJump: {canDoubleJump}");
        }

        /// <summary>
        /// Reliable ground detection with proper layer filtering and slope handling
        /// Based on Clean Code single responsibility principle
        /// </summary>
        private void UpdateGroundDetection()
        {
            // Define raycast parameters with clear constants (no magic numbers)
            const float RAY_DISTANCE = 1.2f;
            const float RAY_START_OFFSET = 0.1f;
            const string GROUND_LAYER = "Ground";
            const string DEFAULT_LAYER = "Default";

            Vector3 raycastOrigin = transform.position + Vector3.up * RAY_START_OFFSET;
            RaycastHit hit;

            // Primary detection: Use Ground layer if available
            int groundLayerMask = LayerMask.GetMask(GROUND_LAYER);
            if (groundLayerMask != 0 && Physics.Raycast(raycastOrigin, Vector3.down, out hit, RAY_DISTANCE, groundLayerMask))
            {
                isGrounded = IsValidGroundHit(hit);
                return;
            }

            // Secondary detection: Use Default layer but exclude self
            int defaultLayerMask = LayerMask.GetMask(DEFAULT_LAYER);
            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, RAY_DISTANCE, defaultLayerMask))
            {
                isGrounded = IsValidGroundHit(hit) && !IsHittingSelf(hit);
                return;
            }

            // Fallback: Use all layers but exclude self and player layers
            int excludeLayers = LayerMask.GetMask("Player", "Projectile", "UI");
            int allLayersExcludePlayer = ~excludeLayers;
            
            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, RAY_DISTANCE, allLayersExcludePlayer))
            {
                isGrounded = IsValidGroundHit(hit) && !IsHittingSelf(hit);
                return;
            }

            // No ground detected
            isGrounded = false;
        }

        /// <summary>
        /// Validate if the raycast hit represents valid ground
        /// Handles slopes and prevents false positives
        /// </summary>
        private bool IsValidGroundHit(RaycastHit hit)
        {
            const float MAX_SLOPE_ANGLE = 45f; // Maximum walkable slope
            
            // Check if the surface is not too steep
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            return slopeAngle <= MAX_SLOPE_ANGLE;
        }

        /// <summary>
        /// Check if raycast hit the character's own collider
        /// Prevents false ground detection from self-collision
        /// </summary>
        private bool IsHittingSelf(RaycastHit hit)
        {
            // Check if hit collider belongs to this GameObject or its children
            return hit.collider.transform == transform || 
                   hit.collider.transform.IsChildOf(transform) ||
                   hit.collider.gameObject == gameObject;
        }

        private void FixedUpdate()
        {
            // Basic movement - state machine will handle modifications
            if (movementInput != Vector3.zero)
            {
                // Calculate movement direction relative to camera
                Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                Vector3 cameraRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;

                // Remove Y component for ground movement
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                Vector3 moveDirection = cameraRight * movementInput.x + cameraForward * movementInput.z;
                rb.linearVelocity = moveDirection * moveSpeed + Vector3.up * rb.linearVelocity.y;
            }
        }

        /// <summary>
        /// Sets movement input from InputRelay
        /// </summary>
        public void SetMovementInput(Vector3 input)
        {
            movementInput = input;
        }

        /// <summary>
        /// Performs jump action
        /// </summary>
        public void Jump()
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
                canDoubleJump = true;
            }
            else if (canDoubleJump)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, doubleJumpForce, rb.linearVelocity.z);
                canDoubleJump = false;
            }
        }

        /// <summary>
        /// Applies damage - handled by state machine
        /// </summary>
        public void TakeDamage(float damage)
        {
            // Damage handling is now managed by the state machine
            Debug.Log($"Character took {damage} damage");
        }
    }


}