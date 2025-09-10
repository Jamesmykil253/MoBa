using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Networking;

namespace MOBA
{
    /// <summary>
    /// Complete player controller integrating Input Relay, State Machine, and Command Pattern
    /// Designed with network development in mind
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        [Header("Core Components")]
        [SerializeField] private InputRelay inputRelay;
        [SerializeField] private MOBACharacterController characterController;
        [SerializeField] private CommandManager commandManager;
        [SerializeField] private AbilitySystem abilitySystem;

        [Header("Player Stats")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;
        [SerializeField] private int playerId;

        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 350f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;

        [Header("Combat Settings")]
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float abilityCooldownReduction = 0f;

        [Header("Scoring")]
        [SerializeField] private int cryptoCoins = 0;
        [SerializeField] private Transform scoringZone;

        // Network-ready properties
        public int PlayerId => playerId;
        public Vector2 Position => transform.position;
        public Vector2 Velocity => rb.linearVelocity;
        public float Health => currentHealth;
        public int CryptoCoins => cryptoCoins;

        // Public properties for external access
        public InputRelay InputRelay => inputRelay;
        public MOBACharacterController CharacterController => characterController;
        public CommandManager CommandManager => commandManager;
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

        // Network prediction (for future implementation)
        private Vector2 predictedPosition;
        private Vector2 serverPosition;
        private float lastNetworkUpdate;

        private void Awake()
        {
            Debug.Log("[PlayerController] Awake() - Starting initialization");

            // Get required components
            rb = GetComponent<Rigidbody>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            Debug.Log($"[PlayerController] Components found - RB: {rb != null}, SpriteRenderer: {spriteRenderer != null}, Animator: {animator != null}");

            // Initialize systems
            InitializeComponents();
            InitializeStats();

            Debug.Log($"[PlayerController] Initialization complete. Position: {transform.position}, Health: {currentHealth}");
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

            // Find or create MOBACharacterController
            if (characterController == null)
            {
                characterController = GetComponent<MOBACharacterController>();
                if (characterController == null)
                {
                    characterController = gameObject.AddComponent<MOBACharacterController>();
                }
            }

            // Find CommandManager (only in play mode or if it exists)
            if (commandManager == null && Application.isPlaying)
            {
                commandManager = FindAnyObjectByType<CommandManager>();
                if (commandManager == null)
                {
                    Debug.LogWarning("[PlayerController] CommandManager not found in scene. Commands will not work until one is available.");
                }
            }

            // Find AbilitySystem (only in play mode or if it exists)
            if (abilitySystem == null && Application.isPlaying)
            {
                abilitySystem = FindAnyObjectByType<AbilitySystem>();
                if (abilitySystem == null)
                {
                    Debug.LogWarning("[PlayerController] AbilitySystem not found in scene. Abilities will not work until one is available.");
                }
            }
        }

        private void InitializeStats()
        {
            currentHealth = maxHealth;
            canDoubleJump = true;
        }

        private void Start()
        {
            // Set up input relay callbacks
            if (inputRelay != null)
            {
                // These would be set up through the InputRelay's event system
                Debug.Log("Player Controller initialized with Input Relay");
            }
        }

        private void Update()
        {
            // Update ground detection
            UpdateGroundDetection();

            // Update aim direction
            UpdateAimDirection();

            // Handle network reconciliation (placeholder for future)
            HandleNetworkReconciliation();

            // Update animations
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            // Apply movement
            ApplyMovement();

            // Handle physics-based interactions
            HandlePhysicsInteractions();
        }

        private void UpdateGroundDetection()
        {
            // Raycast down to detect ground
            RaycastHit hit;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f, LayerMask.GetMask("Ground"));

            // Reset double jump when grounded
            if (isGrounded)
            {
                canDoubleJump = true;
            }
        }

        private void UpdateAimDirection()
        {
            // Update aim direction based on mouse position or right stick
            if (Camera.main != null && Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
                aimDirection = (worldMousePos - (Vector2)transform.position).normalized;

                // Debug logging for aim direction
                if (Time.frameCount % 120 == 0) // Log every 2 seconds
                {
                    Debug.Log($"[PlayerController] Aim direction: {aimDirection:F2}, Mouse pos: {mousePosition}");
                }
            }
        }

        private void ApplyMovement()
        {
            // Movement is now handled by MOBACharacterController and StateMachineIntegration
            // This method can be used for additional movement logic if needed
            if (movementInput != Vector3.zero)
            {
                // Additional movement modifiers can be applied here
                float speed = baseMoveSpeed * GetMovementModifier();
                // The actual movement is handled by the character controller
            }
        }

        private void HandlePhysicsInteractions()
        {
            // Handle wall sliding, ledge grabbing, etc.
            // This would include platformer-specific mechanics
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

            // Debug logging for input
            if (input != Vector3.zero && Time.frameCount % 30 == 0) // Log every 30 frames
            {
                Debug.Log($"[PlayerController] Movement input received: {input:F2}");
            }

            // Relay to character controller
            if (characterController != null)
            {
                characterController.SetMovementInput(input);
            }
            else
            {
                Debug.LogWarning("[PlayerController] No MOBACharacterController found to relay movement input");
            }
        }

        /// <summary>
        /// Handles jump input
        /// </summary>
        public void Jump()
        {
            // Jump is now handled by MOBACharacterController
            if (characterController != null)
            {
                // Check if we can perform jump or double jump
                bool canJump = isGrounded || (canDoubleJump && !isGrounded);
                if (canJump)
                {
                    // Use appropriate jump force based on jump type
                    float jumpForceToUse = isGrounded ? jumpForce : doubleJumpForce;

                    // Apply jump force directly to rigidbody for immediate response
                    if (rb != null)
                    {
                        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForceToUse, rb.linearVelocity.z);
                    }

                    // Also notify character controller for state management
                    characterController.Jump();

                    if (!isGrounded)
                    {
                        canDoubleJump = false; // Consume double jump
                    }

                    Debug.Log($"[PlayerController] Jump executed with force: {jumpForceToUse} (Grounded: {isGrounded})");
                }
            }
        }

        /// <summary>
        /// Casts an ability using the command system
        /// </summary>
        public void CastAbility(AbilityType abilityType, Vector2 targetPosition)
        {
            if (abilitySystem == null || commandManager == null) return;

            var ability = new AbilityData { name = abilityType.ToString() };
            var command = new AbilityCommand(abilitySystem, ability, targetPosition);

            if (command.CanExecute())
            {
                commandManager.ExecuteCommand(command);

                // Network: Send ability cast to server
                SendAbilityCastToServer(abilityType, targetPosition);
            }
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

                // Network: Send score update to server
                SendScoreUpdateToServer(score);

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

            // Network: Sync coin count
            SendCoinUpdateToServer(cryptoCoins);
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

            // Network: Send damage taken to server
            SendDamageTakenToServer(actualDamage);
        }

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

            // Network: Send death event to server
            SendDeathToServer();
        }

        private void Respawn()
        {
            // Reset position to spawn point
            transform.position = Vector3.zero; // Would be actual spawn point
            currentHealth = maxHealth;
            rb.linearVelocity = Vector3.zero;

            // State transition is now handled by StateMachineIntegration
        }

        private void SpawnCoinPickup(int amount)
        {
            // Use object pool to spawn coin pickup
            var pool = FindAnyObjectByType<ProjectilePool>();
            if (pool != null)
            {
                // This would spawn a coin pickup prefab
                Debug.Log($"Dropped {amount} coins at death location");
            }
        }


        // Modifier methods for future stat system
        private float GetMovementModifier() => 1f + (abilityCooldownReduction * 0.1f); // Use abilityCooldownReduction for movement bonuses
        private float GetDamageModifier() => damageMultiplier; // Uses the damage multiplier field

        // Network placeholder methods
        private void HandleNetworkReconciliation()
        {
            // Placeholder for client-side prediction and server reconciliation
            // This would compare predicted position with server position
            // and correct any discrepancies
        }

        private void SendAbilityCastToServer(AbilityType ability, Vector2 target) { /* Network implementation */ }
        private void SendScoreUpdateToServer(int score) { /* Network implementation */ }
        private void SendCoinUpdateToServer(int coins) { /* Network implementation */ }
        private void SendDamageTakenToServer(float damage) { /* Network implementation */ }
        private void SendDeathToServer() { /* Network implementation */ }

        // Network synchronization methods (for future implementation)
        public void ReceiveNetworkUpdate(Vector2 serverPos, Vector2 serverVel, float serverHealth, int serverCoins)
        {
            serverPosition = serverPos;
            currentHealth = serverHealth;
            cryptoCoins = serverCoins;

            // Apply server velocity if prediction error is too large
            if (Vector2.Distance(transform.position, serverPos) > 0.5f)
            {
                rb.linearVelocity = serverVel;
                transform.position = serverPos;
            }
        }

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
        void Interact(PlayerController player);
    }
}