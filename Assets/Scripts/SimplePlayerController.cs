using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Networking;
using System.Collections.Generic;
using MOBA.Configuration;
using MOBA.Abilities;

namespace MOBA
{
    /// <summary>
    /// Simple player controller - updated with modern Unity 6000+ Input System
    /// Uses standardized config access via MasterConfig.Instance.playerConfig
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimplePlayerController : MonoBehaviour, IDamageable
    {
    // Access PlayerConfig via MasterConfig.Instance.playerConfig
        private bool isInvulnerable = false;
        // Slope and platform detection
        private Vector3 groundNormal = Vector3.up;
        private Transform currentPlatform = null;
        [Header("Combat Cooldown")]
        [SerializeField] private float attackCooldown = 0.5f;
        private float lastAttackTime = -999f;
    [Header("Player Stats")]
    /// <summary>
    /// Maximum health for the player.
    /// </summary>
    public float maxHealth = 100f;
    /// <summary>
    /// Current health for the player.
    /// </summary>
    public float currentHealth = 100f;
    /// <summary>
    /// Unique player identifier.
    /// </summary>
    public int playerId;

    [Header("Movement")]
    /// <summary>
    /// Movement speed of the player.
    /// </summary>
    public float moveSpeed = 8f;
    /// <summary>
    /// Jump force applied to the player.
    /// </summary>
    public float jumpForce = 8f;

    [Header("Combat")]
    /// <summary>
    /// Damage dealt by the player.
    /// </summary>
    public float damage = 25f;
    /// <summary>
    /// Attack range for the player.
    /// </summary>
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
        private Renderer[] cachedRenderers;
        private Dictionary<Renderer, bool> rendererDefaultStates;
        private Vector3 moveInput;
        private bool isGrounded;
        private bool canControl = true;
        private bool isAlive = true;

        // Input Actions
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;

        private PlayerConfig runtimePlayerConfig;
        private EnhancedAbilitySystem abilitySystem;

    // Events
    /// <summary>
    /// Raised when health changes. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<float> OnHealthChanged;
    /// <summary>
    /// Raised when the player dies. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action OnDeath;

        void Awake()
        {
            // Cache component references for performance - modern Unity best practice
            rb = GetComponent<Rigidbody>();
            playerCollider = GetComponent<Collider>();
            abilitySystem = GetComponent<EnhancedAbilitySystem>();
            
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

            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            rendererDefaultStates = new Dictionary<Renderer, bool>(cachedRenderers.Length);
            foreach (var renderer in cachedRenderers)
            {
                if (renderer != null && !rendererDefaultStates.ContainsKey(renderer))
                {
                    rendererDefaultStates[renderer] = renderer.enabled;
                }
            }

            var masterConfig = MasterConfig.Instance;
            runtimePlayerConfig = masterConfig != null ? masterConfig.playerConfig : null;
            ApplyPlayerConfig();
        }

        void Start()
        {
            currentHealth = maxHealth;
            canControl = true;
            isAlive = true;
            abilitySystem?.SetInputEnabled(true);
            
            // Enable input actions if available
            if (isAlive && canControl)
            {
                moveAction?.Enable();
                jumpAction?.Enable();
                attackAction?.Enable();
            }
        }

        void OnEnable()
        {
            // Enable actions when object becomes active
            if (isAlive && canControl)
            {
                moveAction?.Enable();
                jumpAction?.Enable();
                attackAction?.Enable();
                abilitySystem?.SetInputEnabled(true);
            }
        }

        void OnDisable()
        {
            // Clean up input actions when disabled
            moveAction?.Disable();
            jumpAction?.Disable();
            attackAction?.Disable();
            abilitySystem?.SetInputEnabled(false);
        }

        void OnDestroy()
        {
            // Properly dispose of input actions to prevent memory leaks
            moveAction?.Disable();
            jumpAction?.Disable();
            attackAction?.Disable();
            
            // Unsubscribe from any events to prevent memory leaks
            OnHealthChanged = null;
            OnDeath = null;
            
            // Clean up event system subscriptions if any were made
            // (This would be added if the controller subscribes to events)
        }

        void Update()
        {
            if (!canControl || !isAlive) return;
            HandleInput();
        }

        void FixedUpdate()
        {
            // Use FixedUpdate for physics-based movement
            CheckGrounded();
            if (!canControl || !isAlive) return;
            Move();
        }

        /// <summary>
        /// Modern ground detection using Physics.CheckSphere for reliability
        /// </summary>
        void CheckGrounded()
        {
            if (groundCheckPoint == null) return;

            // Sphere check for basic ground contact
            isGrounded = Physics.CheckSphere(
                groundCheckPoint.position,
                groundCheckDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore
            );

            // Raycast for slope and platform detection
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance + 0.2f, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                groundNormal = hit.normal;
                isGrounded = true;
                // Detect moving platform
                if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic)
                {
                    currentPlatform = hit.collider.transform;
                }
                else
                {
                    currentPlatform = null;
                }
            }
            else
            {
                groundNormal = Vector3.up;
                currentPlatform = null;
            }
        }

        void HandleInput()
        {
            if (!canControl) return;
            // Modern Input System approach only
            if (moveAction != null)
            {
                Vector2 inputVector = moveAction.ReadValue<Vector2>();
                moveInput = new Vector3(inputVector.x, 0, inputVector.y);
            }

            // Jump with modern Input System
            if (jumpAction != null && jumpAction.WasPressedThisFrame() && isGrounded)
            {
                Jump();
            }

            // Attack with modern Input System
            if (attackAction != null && attackAction.WasPressedThisFrame())
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
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 targetVelocity = new Vector3(movement.x, currentVelocity.y, movement.z);

            // Apply movement using physics system for proper collision detection
            rb.linearVelocity = targetVelocity;
        }

        void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        void Attack()
        {
            if (!canControl || !isAlive) return;
            // Attack cooldown check
            if (Time.time < lastAttackTime + attackCooldown)
                return;

            Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange);
            foreach (var enemy in enemies)
            {
                if (enemy.CompareTag("Enemy"))
                {
                    // Line-of-sight check: raycast from player to enemy, ignore self
                    Vector3 direction = (enemy.transform.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (!Physics.Raycast(transform.position, direction, distance, ~LayerMask.GetMask("Player")))
                    {
                        if (enemy.TryGetComponent<IDamageable>(out var damageable))
                        {
                            damageable.TakeDamage(damage);
                        }
                    }
                }
            }
            lastAttackTime = Time.time;
        }

        public void TakeDamage(float damageAmount)
    /// <summary>
    /// Applies damage to the player. Triggers OnHealthChanged and OnDeath if health reaches zero.
    /// </summary>
        {
            if (isInvulnerable) return;
            currentHealth -= damageAmount;
            OnHealthChanged?.Invoke(currentHealth);
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public float GetHealth()
    /// <summary>
    /// Returns the current health value.
    /// </summary>
        {
            return currentHealth;
        }

        public bool IsDead()
    /// <summary>
    /// Returns true if the player is dead (health &lt;= 0).
    /// </summary>
        {
            return currentHealth <= 0;
        }

        void Die()
        {
            if (!canControl || !isAlive)
                return;
            canControl = false;
            isAlive = false;
            abilitySystem?.SetInputEnabled(false);
            OnDeath?.Invoke();
            // Start respawn coroutine
            StartCoroutine(RespawnCoroutine());
        }

        private System.Collections.IEnumerator RespawnCoroutine()
        {
            // Optionally disable input or visuals here
            SetPlayerActiveState(false);
            var config = runtimePlayerConfig ?? (MasterConfig.Instance != null ? MasterConfig.Instance.playerConfig : null);
            float delay = config != null ? config.respawnDelay : 3f;
            Vector3 respawnPos = config != null ? config.respawnPosition : Vector3.zero;
            yield return new WaitForSeconds(delay);
            // Respawn
            currentHealth = maxHealth;
            transform.position = respawnPos;
            isAlive = true;
            canControl = true;
            SetPlayerActiveState(true);
            StartCoroutine(InvulnerabilityCoroutine(2f)); // 2 seconds of invulnerability
        }

        private System.Collections.IEnumerator InvulnerabilityCoroutine(float duration)
        {
            isInvulnerable = true;
            // Optionally add visual feedback here
            yield return new WaitForSeconds(duration);
            isInvulnerable = false;
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

        private void SetPlayerActiveState(bool active)
        {
            if (!active)
            {
                canControl = false;
            }
            if (rb != null)
            {
                if (!active)
                {
                    rb.linearVelocity = Vector3.zero;
                }
                rb.isKinematic = !active;
            }

            if (playerCollider != null)
            {
                playerCollider.enabled = active;
            }

            if (cachedRenderers != null)
            {
                foreach (var renderer in cachedRenderers)
                {
                    if (renderer == null) continue;
                    if (rendererDefaultStates != null && rendererDefaultStates.TryGetValue(renderer, out var defaultState))
                    {
                        renderer.enabled = active ? defaultState : false;
                    }
                    else
                    {
                        renderer.enabled = active;
                    }
                }
            }

            if (!active)
            {
                moveInput = Vector3.zero;
                if (moveAction != null) moveAction.Disable();
                if (jumpAction != null) jumpAction.Disable();
                if (attackAction != null) attackAction.Disable();
            }
            else
            {
                if (isAlive && canControl)
                {
                    moveAction?.Enable();
                    jumpAction?.Enable();
                    attackAction?.Enable();
                }
            }

            abilitySystem?.SetInputEnabled(active && isAlive && canControl);
        }

        private void ApplyPlayerConfig()
        {
            if (runtimePlayerConfig == null) return;

            maxHealth = runtimePlayerConfig.maxHealth;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            moveSpeed = runtimePlayerConfig.moveSpeed;
            jumpForce = runtimePlayerConfig.jumpForce;
            damage = runtimePlayerConfig.damage;
            attackRange = runtimePlayerConfig.attackRange;
            groundCheckDistance = runtimePlayerConfig.groundCheckDistance;
            groundLayerMask = runtimePlayerConfig.groundLayerMask;
        }
    }
}
