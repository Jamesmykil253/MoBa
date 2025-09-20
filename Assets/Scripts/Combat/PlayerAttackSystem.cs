using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using MOBA.Debugging;
using MOBA.Networking;

namespace MOBA.Combat
{
    /// <summary>
    /// Handles player attack input and smart targeting system.
    /// Implements the documented priority targeting:
    /// - Primary Attack (LMB): Enemy players → NPCs → no attack
    /// - NPC Attack (RMB): NPCs → Enemy players → no attack
    /// </summary>
    [RequireComponent(typeof(SimplePlayerController))]
    public class PlayerAttackSystem : NetworkBehaviour
    {
        #region Configuration
        
        [Header("Attack Settings")]
        [SerializeField, Tooltip("Maximum attack range in units")]
        private float attackRange = 5f;
        
        [SerializeField, Tooltip("Base attack damage")]
        private float baseDamage = 50f;
        
        [SerializeField, Tooltip("Attack cooldown in seconds")]
        private float attackCooldown = 1f;
        
        [SerializeField, Tooltip("Auto-attack follow duration when holding attack")]
        private float autoAttackDuration = 0.5f;
        
        [Header("Targeting")]
        [SerializeField, Tooltip("Layer mask for valid attack targets")]
        private LayerMask targetLayerMask = -1;
        
        [SerializeField, Tooltip("Tag for enemy players")]
        private string enemyPlayerTag = "Player";
        
        [SerializeField, Tooltip("Tag for NPCs")]
        private string npcTag = "Enemy";
        
        [Header("Input")]
        [SerializeField, Tooltip("Primary attack input action reference")]
        private InputActionReference primaryAttackAction;
        
        [SerializeField, Tooltip("NPC attack input action reference")]
        private InputActionReference npcAttackAction;
        
        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging")]
        private bool enableDebugLog = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Reference to player controller
        /// </summary>
        private SimplePlayerController playerController;
        
        /// <summary>
        /// Reference to player input component
        /// </summary>
        private PlayerInput playerInput;
        
        /// <summary>
        /// Camera for targeting calculations
        /// </summary>
        private Camera playerCamera;
        
        /// <summary>
        /// Current attack target
        /// </summary>
        private Transform currentTarget;
        
        /// <summary>
        /// Time of last attack
        /// </summary>
        private float lastAttackTime;
        
        /// <summary>
        /// Whether primary attack is being held
        /// </summary>
        private bool primaryAttackHeld;
        
        /// <summary>
        /// Whether NPC attack is being held
        /// </summary>
        private bool npcAttackHeld;
        
        /// <summary>
        /// Auto-attack coroutine reference
        /// </summary>
        private Coroutine autoAttackCoroutine;
        
        /// <summary>
        /// Input actions for fallback
        /// </summary>
        private InputAction primaryInput;
        private InputAction npcInput;
        
        /// <summary>
        /// Reference to lag compensation manager
        /// </summary>
        private LagCompensationManager lagCompensationManager;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when an attack is performed
        /// </summary>
        public System.Action<Transform, float> OnAttackPerformed; // target, damage
        
        /// <summary>
        /// Raised when target is acquired
        /// </summary>
        public System.Action<Transform> OnTargetAcquired; // target
        
        /// <summary>
        /// Raised when target is lost
        /// </summary>
        public System.Action OnTargetLost;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            playerController = GetComponent<SimplePlayerController>();
            playerInput = GetComponent<PlayerInput>();
            playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            
            // Find lag compensation manager in scene
            lagCompensationManager = FindFirstObjectByType<LagCompensationManager>();
            
            if (playerCamera == null)
            {
                LogWarning("No camera found for targeting. Attack system may not work correctly.");
            }
            
            if (lagCompensationManager == null)
            {
                LogWarning("No LagCompensationManager found. Hit validation will use fallback method.");
            }
        }
        
        private void OnEnable()
        {
            // Input is now handled by SimplePlayerController via BroadcastMessages
            // SetupInputActions();
        }
        
        private void OnDisable()
        {
            // Input cleanup is now handled by SimplePlayerController
            // TeardownInputActions();
        }
        
        private void Update()
        {
            if (!IsOwner || playerController.IsDead())
            {
                return;
            }
            
            UpdateTargeting();
            ProcessAutoAttack();
        }
        
        #endregion
        
        #region Input System
        
        /// <summary>
        /// Setup input action callbacks
        /// </summary>
        private void SetupInputActions()
        {
            // Setup primary attack input
            if (primaryAttackAction != null)
            {
                primaryInput = primaryAttackAction.action;
            }
            else if (playerInput?.actions != null)
            {
                primaryInput = playerInput.actions.FindAction("Attack");
            }
            
            if (primaryInput != null)
            {
                primaryInput.performed += OnPrimaryAttackPerformed;
                primaryInput.canceled += OnPrimaryAttackCanceled;
                primaryInput.Enable();
            }
            
            // Setup NPC attack input
            if (npcAttackAction != null)
            {
                npcInput = npcAttackAction.action;
            }
            else if (playerInput?.actions != null)
            {
                npcInput = playerInput.actions.FindAction("AttackNPC");
            }
            
            if (npcInput != null)
            {
                npcInput.performed += OnNPCAttackPerformed;
                npcInput.canceled += OnNPCAttackCanceled;
                npcInput.Enable();
            }
        }
        
        /// <summary>
        /// Cleanup input action callbacks
        /// </summary>
        private void TeardownInputActions()
        {
            if (primaryInput != null)
            {
                primaryInput.performed -= OnPrimaryAttackPerformed;
                primaryInput.canceled -= OnPrimaryAttackCanceled;
                primaryInput.Disable();
            }
            
            if (npcInput != null)
            {
                npcInput.performed -= OnNPCAttackPerformed;
                npcInput.canceled -= OnNPCAttackCanceled;
                npcInput.Disable();
            }
        }
        
        #endregion
        
        #region Input Callbacks
        
        /// <summary>
        /// Handle primary attack input (Enemy players and other players)
        /// Priority: Enemy players → NPCs
        /// </summary>
        public void OnPrimaryAttackPerformed(InputAction.CallbackContext context)
        {
            if (!CanAttack())
            {
                return;
            }
            
            primaryAttackHeld = true;
            var target = FindBestTarget(AttackType.Primary);
            
            if (target != null)
            {
                PerformAttack(target);
                StartAutoAttack(AttackType.Primary);
            }
            
            Log($"Primary attack performed. Target: {(target ? target.name : "None")}");
        }
        
        /// <summary>
        /// Handle primary attack release
        /// </summary>
        public void OnPrimaryAttackCanceled(InputAction.CallbackContext context)
        {
            primaryAttackHeld = false;
            StopAutoAttack();
            
            Log("Primary attack released");
        }
        
        /// <summary>
        /// Handle NPC-specific attack input
        /// Targets only NPCs/monsters for farming
        /// </summary>
        public void OnNPCAttackPerformed(InputAction.CallbackContext context)
        {
            if (!CanAttack())
            {
                return;
            }
            
            npcAttackHeld = true;
            var target = FindBestTarget(AttackType.NPC);
            
            if (target != null)
            {
                PerformAttack(target);
                StartAutoAttack(AttackType.NPC);
            }
            
            Log($"NPC attack performed. Target: {(target ? target.name : "None")}");
        }
        
        /// <summary>
        /// Handle NPC attack release
        /// </summary>
        public void OnNPCAttackCanceled(InputAction.CallbackContext context)
        {
            npcAttackHeld = false;
            StopAutoAttack();
            
            Log("NPC attack released");
        }
        
        #endregion
        
        #region Targeting System
        
        /// <summary>
        /// Attack type for priority targeting
        /// </summary>
        public enum AttackType
        {
            Primary, // Enemy players → NPCs
            NPC      // NPCs → Enemy players
        }
        
        /// <summary>
        /// Find the best target based on attack type and priority
        /// </summary>
        /// <param name="attackType">Type of attack being performed</param>
        /// <returns>Best target or null if no valid targets</returns>
        private Transform FindBestTarget(AttackType attackType)
        {
            var potentialTargets = Physics.OverlapSphere(transform.position, attackRange, targetLayerMask);
            
            Transform bestTarget = null;
            float closestDistance = float.MaxValue;
            
            // Priority lists based on attack type
            List<string> primaryPriority = attackType == AttackType.Primary 
                ? new List<string> { enemyPlayerTag, npcTag }
                : new List<string> { npcTag, enemyPlayerTag };
            
            // Check each priority level
            foreach (string priorityTag in primaryPriority)
            {
                foreach (var collider in potentialTargets)
                {
                    if (!IsValidTarget(collider.transform, priorityTag))
                    {
                        continue;
                    }
                    
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = collider.transform;
                    }
                }
                
                // If we found a target at this priority level, use it
                if (bestTarget != null)
                {
                    break;
                }
            }
            
            return bestTarget;
        }
        
        /// <summary>
        /// Check if a target is valid for attacking
        /// </summary>
        /// <param name="target">Target to validate</param>
        /// <param name="requiredTag">Required tag for this priority level</param>
        /// <returns>True if target is valid</returns>
        private bool IsValidTarget(Transform target, string requiredTag)
        {
            if (target == null || target == transform)
            {
                return false;
            }
            
            // Check tag match
            if (!target.CompareTag(requiredTag))
            {
                return false;
            }
            
            // Check if target is on same team (for player targets)
            if (requiredTag == enemyPlayerTag)
            {
                var targetPlayer = target.GetComponent<SimplePlayerController>();
                if (targetPlayer != null && targetPlayer.TeamIndex == playerController.TeamIndex)
                {
                    return false; // Same team, can't attack
                }
            }
            
            // Check if target is alive
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsDead())
            {
                return false;
            }
            
            // Check line of sight
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (Physics.Raycast(transform.position, directionToTarget, distanceToTarget, ~targetLayerMask))
            {
                return false; // Blocked by obstacle
            }
            
            return true;
        }
        
        /// <summary>
        /// Update current targeting display/logic
        /// </summary>
        private void UpdateTargeting()
        {
            Transform newTarget = null;
            
            if (primaryAttackHeld)
            {
                newTarget = FindBestTarget(AttackType.Primary);
            }
            else if (npcAttackHeld)
            {
                newTarget = FindBestTarget(AttackType.NPC);
            }
            
            if (newTarget != currentTarget)
            {
                if (currentTarget != null)
                {
                    OnTargetLost?.Invoke();
                }
                
                currentTarget = newTarget;
                
                if (currentTarget != null)
                {
                    OnTargetAcquired?.Invoke(currentTarget);
                }
            }
        }
        
        #endregion
        
        #region Attack System
        
        /// <summary>
        /// Check if player can perform an attack
        /// </summary>
        /// <returns>True if attack is possible</returns>
        private bool CanAttack()
        {
            return IsOwner 
                && !playerController.IsDead() 
                && Time.time >= lastAttackTime + attackCooldown;
        }
        
        /// <summary>
        /// Perform an attack on the specified target
        /// </summary>
        /// <param name="target">Target to attack</param>
        private void PerformAttack(Transform target)
        {
            if (target == null || !CanAttack())
            {
                return;
            }
            
            lastAttackTime = Time.time;
            
            // Network the attack
            if (IsServer)
            {
                ExecuteAttackServerRpc(target.GetComponent<NetworkObject>().NetworkObjectId, baseDamage);
            }
            else
            {
                RequestAttackServerRpc(target.GetComponent<NetworkObject>().NetworkObjectId, baseDamage);
            }
            
            OnAttackPerformed?.Invoke(target, baseDamage);
            
            Log($"Attack performed on {target.name} for {baseDamage} damage");
        }
        
        /// <summary>
        /// Start auto-attack coroutine for held input
        /// </summary>
        /// <param name="attackType">Type of attack to repeat</param>
        private void StartAutoAttack(AttackType attackType)
        {
            StopAutoAttack();
            autoAttackCoroutine = StartCoroutine(AutoAttackCoroutine(attackType));
        }
        
        /// <summary>
        /// Stop auto-attack coroutine
        /// </summary>
        private void StopAutoAttack()
        {
            if (autoAttackCoroutine != null)
            {
                StopCoroutine(autoAttackCoroutine);
                autoAttackCoroutine = null;
            }
        }
        
        /// <summary>
        /// Auto-attack coroutine for continuous attacking
        /// </summary>
        /// <param name="attackType">Type of attack to perform</param>
        private IEnumerator AutoAttackCoroutine(AttackType attackType)
        {
            yield return new WaitForSeconds(autoAttackDuration);
            
            while ((attackType == AttackType.Primary && primaryAttackHeld) ||
                   (attackType == AttackType.NPC && npcAttackHeld))
            {
                if (CanAttack())
                {
                    var target = FindBestTarget(attackType);
                    if (target != null)
                    {
                        PerformAttack(target);
                    }
                }
                
                yield return new WaitForSeconds(attackCooldown);
            }
        }
        
        /// <summary>
        /// Process auto-attack logic
        /// </summary>
        private void ProcessAutoAttack()
        {
            // Auto-attack is handled by coroutines started from input callbacks
        }
        
        #endregion
        
        #region Network RPCs
        
        /// <summary>
        /// Request attack execution on server
        /// </summary>
        [ServerRpc]
        private void RequestAttackServerRpc(ulong targetNetworkId, float damage)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var targetNetworkObject))
            {
                ExecuteAttackServerRpc(targetNetworkId, damage);
            }
        }
        
        /// <summary>
        /// Execute attack on server and replicate to clients
        /// </summary>
        [ServerRpc]
        private void ExecuteAttackServerRpc(ulong targetNetworkId, float damage)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var targetNetworkObject))
            {
                // Validate hit with lag compensation if available
                bool hitValid = true;
                if (lagCompensationManager != null)
                {
                    float clientTimestamp = NetworkManager.Singleton.ServerTime.TimeAsFloat; // For server-side execution
                    hitValid = lagCompensationManager.ValidateAttackHit(
                        OwnerClientId, 
                        targetNetworkId, 
                        damage, 
                        transform.position, 
                        clientTimestamp
                    );
                }
                
                if (hitValid)
                {
                    var damageable = targetNetworkObject.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(damage);
                        
                        // Replicate to all clients for effects
                        ApplyAttackEffectsClientRpc(targetNetworkId, damage);
                        
                        Log($"Attack hit validated and applied: {damage} damage to {targetNetworkObject.name}");
                    }
                }
                else
                {
                    Log($"Attack hit rejected by lag compensation system");
                }
            }
        }
        
        /// <summary>
        /// Apply attack effects on all clients
        /// </summary>
        [ClientRpc]
        private void ApplyAttackEffectsClientRpc(ulong targetNetworkId, float damage)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var targetNetworkObject))
            {
                // TODO: Add attack effects here (particles, sound, etc.)
                Log($"Attack effect applied to {targetNetworkObject.name}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get current attack target
        /// </summary>
        /// <returns>Current target or null</returns>
        public Transform GetCurrentTarget()
        {
            return currentTarget;
        }
        
        /// <summary>
        /// Check if player is currently attacking
        /// </summary>
        /// <returns>True if attacking</returns>
        public bool IsAttacking()
        {
            return primaryAttackHeld || npcAttackHeld;
        }
        
        /// <summary>
        /// Get time until next attack is possible
        /// </summary>
        /// <returns>Cooldown remaining in seconds</returns>
        public float GetAttackCooldownRemaining()
        {
            return Mathf.Max(0f, (lastAttackTime + attackCooldown) - Time.time);
        }
        
        #endregion
        
        #region Debug Helpers
        
        /// <summary>
        /// Log debug message if enabled
        /// </summary>
        /// <param name="message">Message to log</param>
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                GameDebug.Log(BuildContext(), message);
            }
        }
        
        /// <summary>
        /// Log warning message
        /// </summary>
        /// <param name="message">Warning message</param>
        private void LogWarning(string message)
        {
            GameDebug.LogWarning(BuildContext(), message);
        }
        
        /// <summary>
        /// Build debug context
        /// </summary>
        /// <returns>Debug context for logging</returns>
        private GameDebugContext BuildContext()
        {
            return new GameDebugContext(
                GameDebugCategory.Combat,
                GameDebugSystemTag.Combat,
                GameDebugMechanicTag.Combat,
                subsystem: nameof(PlayerAttackSystem),
                actor: gameObject?.name);
        }
        
        /// <summary>
        /// Draw debug information in scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw line to current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
        }
        
        #endregion
    }
}