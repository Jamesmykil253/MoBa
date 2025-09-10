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
            // Update ground detection with improved logic
            RaycastHit hit;
            float raycastDistance = 1.2f; // Increased from 1.1f for better detection
            Vector3 raycastOrigin = transform.position + Vector3.up * 0.1f; // Start slightly above to avoid self-collision

            // Try to detect ground with multiple methods
            bool groundDetected = false;

            // Method 1: Raycast with Ground layer
            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastDistance, LayerMask.GetMask("Ground")))
            {
                groundDetected = true;
            }
            // Method 2: Fallback to default layer if Ground layer fails
            else if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastDistance))
            {
                // Check if we hit something that could be ground (not the player itself)
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    groundDetected = true;
                }
            }

            isGrounded = groundDetected;

            // Debug ground detection
            if (Time.frameCount % 60 == 0) // Log every second
            {
                Debug.Log($"[MOBACharacterController] Ground detection: isGrounded={isGrounded}, Position={transform.position:F2}, Hit distance={hit.distance:F2}");
            }
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