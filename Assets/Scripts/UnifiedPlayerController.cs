using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Unified Player Controller combining the best of PlayerController and MOBACharacterController
    /// Eliminates duplication and provides comprehensive player functionality
    /// Designed with network development in mind
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class UnifiedPlayerController : MonoBehaviour, IDamageable
    {
        [Header("Core Components")]
        [SerializeField] private InputRelay inputRelay;
        [SerializeField] private AbilitySystem abilitySystem;

        [Header("Player Stats")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private int playerId;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;

        [Header("Combat Settings")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float abilityCooldownReduction = 0f;

        [Header("Scoring")]
        [SerializeField] private int cryptoCoins = 0;
        [SerializeField] private Transform scoringZone;

        // Public properties for state machine and other systems
        public float JumpForce => jumpForce;
        public float DoubleJumpForce => doubleJumpForce;
        public Vector3 MovementInput => movementInput;
        public int PlayerId => playerId;
        public Vector2 Position => transform.position;
        public Vector2 Velocity => rb.linearVelocity;
        public float Health => currentHealth;
        public int CryptoCoins => cryptoCoins;
        public bool IsGrounded => isGrounded;

        // Public properties for external access
        public InputRelay InputRelay => inputRelay;
        public AbilitySystem AbilitySystem => abilitySystem;
        public Transform ScoringZone => scoringZone;

        // Component references
        private Rigidbody rb;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        // State tracking
        private bool isGrounded;
        private bool canDoubleJump;
        private Vector3 movementInput;
        private Vector2 aimDirection;

        /// <summary>
        /// Unity Awake - Ensures automatic initialization following game programming patterns
        /// </summary>
        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Manual initialization - call this instead of relying on Awake
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[UnifiedPlayerController] Manual Initialize() - Starting initialization");

            // Get required components
            rb = GetComponent<Rigidbody>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            Debug.Log($"[UnifiedPlayerController] Components found - RB: {rb != null}, SpriteRenderer: {spriteRenderer != null}, Animator: {animator != null}");

            // Initialize systems
            InitializeComponents();
            InitializeStats();

            Debug.Log($"[UnifiedPlayerController] Manual initialization complete. Position: {transform.position}, Health: {currentHealth}");
        }

        private void InitializeComponents()
        {
            // Find or create InputRelay
            if (inputRelay == null)
            {
                inputRelay = GetComponent<InputRelay>();
                if (inputRelay == null)
                {
                    inputRelay = gameObject.AddComponent<InputRelay>();
                }
            }

            // Enhanced AbilitySystem discovery
            if (abilitySystem == null)
            {
                abilitySystem = FindAnyObjectByType<AbilitySystem>();
                if (abilitySystem == null)
                {
                    Debug.LogWarning("[UnifiedPlayerController] AbilitySystem not found. Create one in the scene or assign manually.");
                }
                else
                {
                    Debug.Log("[UnifiedPlayerController] AbilitySystem auto-discovered successfully.");
                }
            }
        }

        private void InitializeStats()
        {
            currentHealth = maxHealth;
            canDoubleJump = true;
            
            // HARDCODED: Ensure player starts on ground to prevent death loop
            // Following game programming patterns - spawn at safe position
            if (transform.position.y > 2f) // If spawned in air
            {
                Vector3 safePosition = transform.position;
                safePosition.y = 1f; // Place on ground level
                transform.position = safePosition;
                
                Debug.Log($"[UnifiedPlayerController] Moved player to safe spawn position: {transform.position}");
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

            // Update aim direction
            UpdateAimDirection();

            // Update animations
            UpdateAnimations();

            // Debug movement state every 2 seconds
            if (Time.frameCount % 120 == 0)
            {
                LogMovementState();
            }
        }

        private void FixedUpdate()
        {
            // FIXED: Ensure movement is properly applied even if rigidbody is kinematic
            if (rb != null)
            {
                // Ensure rigidbody is not kinematic for movement
                if (rb.isKinematic)
                {
                    rb.isKinematic = false;
                    Debug.Log("[UnifiedPlayerController] Rigidbody was kinematic, enabling physics for movement");
                }
                
                // Apply movement input
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
                    Vector3 targetVelocity = moveDirection * moveSpeed;
                    
                    // Preserve Y velocity for gravity/jumping
                    targetVelocity.y = rb.linearVelocity.y;
                    
                    // FIXED: Direct assignment instead of interpolation to prevent accumulation
                    rb.linearVelocity = targetVelocity;
                }
                else
                {
                    // FIXED: Stop horizontal movement when no input, preserve Y velocity
                    rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                }
                
                // CRITICAL: Clamp velocity to prevent unrealistic speeds in all cases
                Vector3 clampedVelocity = rb.linearVelocity;
                float maxSpeed = moveSpeed * 1.1f; // Allow slight overage for responsiveness
                
                // Clamp horizontal velocity components
                if (Mathf.Abs(clampedVelocity.x) > maxSpeed)
                {
                    clampedVelocity.x = Mathf.Sign(clampedVelocity.x) * maxSpeed;
                }
                if (Mathf.Abs(clampedVelocity.z) > maxSpeed)
                {
                    clampedVelocity.z = Mathf.Sign(clampedVelocity.z) * maxSpeed;
                }
                
                // Limit total horizontal speed to prevent unrealistic movement
                Vector3 horizontalVelocity = new Vector3(clampedVelocity.x, 0, clampedVelocity.z);
                if (horizontalVelocity.magnitude > maxSpeed)
                {
                    horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
                    clampedVelocity.x = horizontalVelocity.x;
                    clampedVelocity.z = horizontalVelocity.z;
                }
                
                rb.linearVelocity = clampedVelocity;
                
                // Debug movement every second only when velocity is significant
                if (Time.fixedDeltaTime > 0 && Time.fixedTime % 1f < Time.fixedDeltaTime && rb.linearVelocity.magnitude > 1f)
                {
                    Debug.Log($"[UnifiedPlayerController] Movement applied - Input: {movementInput:F2}, Velocity: {rb.linearVelocity:F2}");
                }
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
                if (isGrounded) canDoubleJump = true;
                return;
            }

            // Secondary detection: Use Default layer but exclude self
            int defaultLayerMask = LayerMask.GetMask(DEFAULT_LAYER);
            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, RAY_DISTANCE, defaultLayerMask))
            {
                isGrounded = IsValidGroundHit(hit) && !IsHittingSelf(hit);
                if (isGrounded) canDoubleJump = true;
                return;
            }

            // Fallback: Use all layers but exclude self and player layers
            int excludeLayers = LayerMask.GetMask("Player", "Projectile", "UI");
            int allLayersExcludePlayer = ~excludeLayers;
            
            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, RAY_DISTANCE, allLayersExcludePlayer))
            {
                isGrounded = IsValidGroundHit(hit) && !IsHittingSelf(hit);
                if (isGrounded) canDoubleJump = true;
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

        private void UpdateAimDirection()
        {
            // Update aim direction based on mouse position or right stick
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
                aimDirection = (worldMousePos - (Vector2)transform.position).normalized;

                // Reduced logging frequency for performance
                #if UNITY_EDITOR
                if (Time.frameCount % 300 == 0) // Log every 5 seconds in editor only
                {
                    Debug.Log($"[UnifiedPlayerController] Aim direction: {aimDirection:F2}, Mouse pos: {mousePosition}");
                }
                #endif
            }
        }

        private void UpdateAnimations()
        {
            if (animator != null)
            {
                // Update animation parameters based on state
                animator.SetBool("IsGrounded", isGrounded);
                animator.SetFloat("MoveSpeed", Mathf.Abs(movementInput.x));
                animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

                // Flip sprite based on movement direction
                if (movementInput.x != 0 && spriteRenderer != null)
                {
                    spriteRenderer.flipX = movementInput.x < 0;
                }
            }
        }

        /// <summary>
        /// Sets movement input from InputRelay
        /// </summary>
        public void SetMovementInput(Vector3 input)
        {
            movementInput = input;

            // Reduced logging for performance - only in editor
            #if UNITY_EDITOR
            if (input != Vector3.zero && Time.frameCount % 60 == 0) // Log every 60 frames in editor only
            {
                Debug.Log($"[UnifiedPlayerController] Movement input received: {input:F2}");
            }
            #endif
        }

        /// <summary>
        /// Unified jump handling following DRY principle from Pragmatic Programmer
        /// Single source of truth for jump mechanics
        /// </summary>
        public void Jump()
        {
            // Defensive programming: Validate prerequisites
            if (rb == null)
            {
                Debug.LogError("[UnifiedPlayerController] Cannot jump: Rigidbody component not found");
                return;
            }

            // Determine jump capability and appropriate force
            bool canJump = CanPerformJump(out float jumpForceToUse);
            
            if (!canJump)
            {
                Debug.Log("[UnifiedPlayerController] Jump attempt blocked - conditions not met");
                return;
            }

            // Execute jump with proper physics
            ExecuteJump(jumpForceToUse);

            // Update jump state tracking
            UpdateJumpState();

            Debug.Log($"[UnifiedPlayerController] Jump executed - Force: {jumpForceToUse}, Grounded: {isGrounded}, CanDoubleJump: {canDoubleJump}");
        }

        /// <summary>
        /// Determines if jump can be performed and returns appropriate force
        /// Follows Clean Code principle of single responsibility
        /// </summary>
        private bool CanPerformJump(out float jumpForceToUse)
        {
            jumpForceToUse = 0f;

            // Ground jump check
            if (isGrounded)
            {
                jumpForceToUse = jumpForce;
                return true;
            }

            // Double jump check
            if (canDoubleJump)
            {
                jumpForceToUse = doubleJumpForce;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Executes the actual jump physics
        /// </summary>
        private void ExecuteJump(float force)
        {
            // Reset vertical velocity to ensure consistent jump height
            Vector3 velocity = rb.linearVelocity;
            velocity.y = force;
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Updates internal jump state tracking
        /// </summary>
        private void UpdateJumpState()
        {
            if (!isGrounded && canDoubleJump)
            {
                canDoubleJump = false; // Consume double jump
            }
        }

        /// <summary>
        /// Casts an ability directly without command pattern complexity
        /// </summary>
        public void CastAbility(AbilityType abilityType, Vector2 targetPosition)
        {
            if (abilitySystem == null) return;

            // Cast ability directly - no command pattern needed for MOBA
            var abilityData = new AbilityData { name = abilityType.ToString(), damage = 75f };
            abilitySystem.CastAbility(abilityData, targetPosition);
        }

        /// <summary>
        /// Handles interaction with environment/objects
        /// </summary>
        public void Interact()
        {
            // Check for nearby interactable objects
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Interactable"));

            foreach (var collider in nearbyColliders)
            {
                var interactable = collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(this);
                    break;
                }
            }

            // Check for scoring zones
            if (scoringZone != null && Vector2.Distance(transform.position, scoringZone.position) < 3f)
            {
                DepositCoins();
            }
        }

        /// <summary>
        /// Deposits crypto coins for scoring
        /// </summary>
        private void DepositCoins()
        {
            if (cryptoCoins > 0)
            {
                // Calculate score based on coins carried
                int score = cryptoCoins * 10; // Example scoring formula

                // Add to team score (would be handled by game manager)
                Debug.Log($"Deposited {cryptoCoins} coins for {score} points");

                cryptoCoins = 0;
            }
        }

        /// <summary>
        /// Adds crypto coins (from kills, pickups, etc.)
        /// </summary>
        public void AddCryptoCoins(int amount)
        {
            cryptoCoins += amount;
            Debug.Log($"Collected {amount} crypto coins. Total: {cryptoCoins}");
        }

        public void TakeDamage(float damage)
        {
            float actualDamage = damage * GetDamageModifier();
            currentHealth -= actualDamage;

            Debug.Log($"Took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        // IDamageable interface implementation
        public float GetHealth() => currentHealth;
        public bool IsDead() => currentHealth <= 0;

        private void Die()
        {
            Debug.Log("Player died!");

            // Drop coins on death
            if (cryptoCoins > 0)
            {
                int droppedCoins = Mathf.FloorToInt(cryptoCoins * 0.5f);
                cryptoCoins -= droppedCoins;

                // Spawn coin pickup at death location
                SpawnCoinPickup(droppedCoins);
            }

            // Respawn logic would go here
            Respawn();
        }

        private void Respawn()
        {
            // Reset position to spawn point
            transform.position = Vector3.zero; // Would be actual spawn point
            currentHealth = maxHealth;
            rb.linearVelocity = Vector3.zero;
        }

        private void SpawnCoinPickup(int amount)
        {
            Debug.Log($"[UnifiedPlayerController] Would drop {amount} coins at death location");
        }

        // Modifier methods for future stat system
        private float GetMovementModifier() => 1f + (abilityCooldownReduction * 0.1f);
        private float GetDamageModifier() => damageMultiplier;

        private void OnDrawGizmos()
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 2f);

            // Draw scoring zone detection
            if (scoringZone != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, scoringZone.position);
            }
        }
    }

    // Interface for interactable objects
    public interface IInteractable
    {
        void Interact(UnifiedPlayerController player);
    }
}
