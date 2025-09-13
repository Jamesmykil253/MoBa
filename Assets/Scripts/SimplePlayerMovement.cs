using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple player movement - no complex dependencies
    /// </summary>
    [System.Serializable]
    public class PlayerMovement
    {
        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 8f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask groundLayer = 1;

        // Private state
        private bool isGrounded;
        private Vector3 movementInput;
        
        // Component references
        private Transform transform;
        private Rigidbody rb;

        // Properties
        public bool IsGrounded => isGrounded;
        public Vector3 MovementInput => movementInput;
        public float MovementSpeed => baseMoveSpeed;

        /// <summary>
        /// Initialize movement system with required components
        /// </summary>
        public void Initialize(Transform playerTransform, Rigidbody rigidbody)
        {
            transform = playerTransform;
            rb = rigidbody;
            
            if (transform == null || rb == null)
            {
                Debug.LogError("[PlayerMovement] Missing required components");
                return;
            }
            
            Debug.Log("[PlayerMovement] Movement system initialized");
        }

        /// <summary>
        /// Set movement input
        /// </summary>
        public void SetMovementInput(Vector3 input)
        {
            movementInput = Vector3.ClampMagnitude(input, 1f);
        }

        /// <summary>
        /// Update ground detection
        /// </summary>
        public void UpdateGroundDetection()
        {
            if (transform == null) return;

            RaycastHit hit;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayer);
        }

        /// <summary>
        /// Handle jump input
        /// </summary>
        public bool TryJump()
        {
            if (rb == null || !isGrounded) return false;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            return true;
        }

        /// <summary>
        /// Apply movement physics
        /// </summary>
        public void ApplyMovement()
        {
            if (rb == null) return;

            Vector3 movement = movementInput * baseMoveSpeed * Time.deltaTime;
            rb.MovePosition(transform.position + movement);
        }
    }
}
