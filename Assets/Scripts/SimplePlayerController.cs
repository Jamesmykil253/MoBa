using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Simple player controller - updated with modern Unity 6000+ Input System
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimplePlayerController : MonoBehaviour, IDamageable
    {
        [Header("Player Stats")]
        public float maxHealth = 100f;
        public float currentHealth = 100f;
        public int playerId;

        [Header("Movement")]
        public float moveSpeed = 8f;
        public float jumpForce = 8f;

        [Header("Combat")]
        public float damage = 25f;
        public float attackRange = 5f;

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayerMask = 1;
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private Transform groundCheckPoint;

        // Components
        private Rigidbody rb;
        private Collider playerCollider;
        private Vector3 moveInput;
        private bool isGrounded;

        // Input Actions
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;

        // Events
        public System.Action<float> OnHealthChanged;
        public System.Action OnDeath;

        void Awake()
        {
            // Cache component references for performance - modern Unity best practice
            rb = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();
            
            // Validate required components
            if (rb == null)
            {
                Debug.LogError($"[SimplePlayerController] Missing Rigidbody component on {gameObject.name}");
            }
            
            if (playerCollider == null)
            {
                Debug.LogError($"[SimplePlayerController] Missing Collider component on {gameObject.name}");
            }
            
            // Auto-create ground check point if not assigned
            if (groundCheckPoint == null)
            {
                GameObject groundCheck = new GameObject("GroundCheck");
                groundCheck.transform.SetParent(transform);
                groundCheck.transform.localPosition = Vector3.down * (playerCollider ? playerCollider.bounds.extents.y : 1f);
                groundCheckPoint = groundCheck.transform;
            }
            
            // Initialize Input System - Modern Unity 6000+ approach
            if (inputActions == null)
            {
                // Fallback to finding existing Input Actions asset
                inputActions = FindFirstObjectByType<PlayerInput>()?.actions;
            }
            
            if (inputActions != null)
            {
                moveAction = inputActions.FindAction("Move");
                jumpAction = inputActions.FindAction("Jump");
                attackAction = inputActions.FindAction("Attack");
            }
        }

        void Start()
        {
            currentHealth = maxHealth;
            
            // Enable input actions if available
            if (moveAction != null) moveAction.Enable();
            if (jumpAction != null) jumpAction.Enable();
            if (attackAction != null) attackAction.Enable();
        }

        void OnEnable()
        {
            // Enable actions when object becomes active
            moveAction?.Enable();
            jumpAction?.Enable();
            attackAction?.Enable();
        }

        void OnDisable()
        {
            // Clean up input actions when disabled
            moveAction?.Disable();
            jumpAction?.Disable();
            attackAction?.Disable();
        }

        void Update()
        {
            HandleInput();
        }

        void FixedUpdate()
        {
            // Use FixedUpdate for physics-based movement
            CheckGrounded();
            Move();
        }

        /// <summary>
        /// Modern ground detection using Physics.CheckSphere for reliability
        /// </summary>
        void CheckGrounded()
        {
            if (groundCheckPoint == null) return;
            
            // Use sphere check for more reliable ground detection
            isGrounded = Physics.CheckSphere(
                groundCheckPoint.position, 
                groundCheckDistance, 
                groundLayerMask, 
                QueryTriggerInteraction.Ignore
            );
        }

        void HandleInput()
        {
            // Modern Input System approach - much more robust than legacy Input.GetAxis
            if (moveAction != null)
            {
                Vector2 inputVector = moveAction.ReadValue<Vector2>();
                moveInput = new Vector3(inputVector.x, 0, inputVector.y);
            }
            else
            {
                // Fallback to legacy input only if new Input System not available
                moveInput.x = Input.GetAxis("Horizontal");
                moveInput.z = Input.GetAxis("Vertical");
            }

            // Jump with modern Input System
            if (jumpAction != null)
            {
                if (jumpAction.WasPressedThisFrame() && isGrounded)
                {
                    Jump();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }

            // Attack with modern Input System
            if (attackAction != null)
            {
                if (attackAction.WasPressedThisFrame())
                {
                    Attack();
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Attack();
            }
        }

        void Move()
        {
            // Modern physics-based movement instead of Transform.Translate
            if (rb == null) return;

            // Calculate movement direction relative to world space
            Vector3 movement = moveInput.normalized * moveSpeed;
            
            // Preserve Y velocity for gravity and jumping
            Vector3 targetVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
            
            // Apply movement using physics system for proper collision detection
            rb.linearVelocity = targetVelocity;
        }

        void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        void Attack()
        {
            // Simple attack - find enemies in range and damage them
            Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange);
            foreach (var enemy in enemies)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    // Cache GetComponent call for better performance
                    if (enemy.TryGetComponent<IDamageable>(out var damageable))
                    {
                        damageable.TakeDamage(damage);
                    }
                }
            }
        }

        public void TakeDamage(float damageAmount)
        {
            currentHealth -= damageAmount;
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public float GetHealth()
        {
            return currentHealth;
        }

        public bool IsDead()
        {
            return currentHealth <= 0;
        }

        void Die()
        {
            OnDeath?.Invoke();
            // Simple respawn or game over logic
            currentHealth = maxHealth;
            transform.position = Vector3.zero; // Respawn at origin
        }

        void OnDrawGizmos()
        {
            // Show attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Show ground check
            if (groundCheckPoint != null)
            {
                Gizmos.color = isGrounded ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckDistance);
            }
        }
    }
}
