using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Abilities;
using MOBA.Combat;
using MOBA.UI;

namespace MOBA
{
    /// <summary>
    /// Modernized player controller using the unified movement and ability systems.
    /// Implements clean Input System integration with proper callback handling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    [DefaultExecutionOrder(-100)] // Execute before other components that depend on PlayerInput
        public class SimplePlayerController : MonoBehaviour, IDamageable, IDamageSnapshotReceiver, IEvolutionInputHandler
    {
        [Header("Movement")]
        [SerializeField] private UnifiedMovementSystem movementSystem = new UnifiedMovementSystem();
        [SerializeField] private float rotationResponsiveness = 14f;

        [Header("Health & Respawn")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool autoRespawn = true;
        [SerializeField] private float respawnDelay = 3f;
        [SerializeField] private Transform respawnPoint;

        [Header("Team")]
        [Tooltip("Team index used for scoring and team-based logic.")]
        [SerializeField] private int teamIndex = 0;

        [Header("Jump Settings")]
        [SerializeField, Tooltip("Time in seconds the jump button must be held before the hold boost activates.")]
        private float holdJumpWarmup = 0.2f;
        [SerializeField, Tooltip("Maximum time window to trigger the hold boost after pressing jump.")]
        private float holdJumpMaxDuration = 0.35f;

        private Rigidbody body;
        private PlayerInput playerInput;
        private EnhancedAbilitySystem enhancedAbilitySystem;
        private AbilityEvolutionHandler evolutionHandler;

        private Vector2 moveInput;
        private Vector3 desiredLook = Vector3.forward;
        private Vector3 defaultLookDirection = Vector3.forward;
        private float yawVelocity;

        private float currentHealth;
        private bool isDead;
        private bool inputActive = true;
        private Vector3 initialSpawnPosition;
        private Coroutine respawnRoutine;
        private bool jumpButtonHeld;
        private bool holdBoostCandidate;
        private bool holdBoostApplied;
        private float jumpButtonPressTime;

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public int TeamIndex => Mathf.Max(0, teamIndex);

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            playerInput = GetComponent<PlayerInput>();
            enhancedAbilitySystem = GetComponent<EnhancedAbilitySystem>();
            evolutionHandler = GetComponent<AbilityEvolutionHandler>();

            // Configure PlayerInput to use BroadcastMessage behavior
            if (playerInput != null)
            {
                playerInput.notificationBehavior = PlayerNotifications.BroadcastMessages;
                
                // Critical diagnostic logging
                if (playerInput.actions == null)
                {
                    Debug.LogError("[SimplePlayerController] CRITICAL: PlayerInput component has no actions asset assigned!");
                    Debug.LogError("[SimplePlayerController] SOLUTION: In Unity Inspector, drag 'InputSystem_Actions' asset to the PlayerInput component's 'Actions' field");
                }
                else
                {
                    Debug.Log($"[SimplePlayerController] PlayerInput configured with actions: {playerInput.actions.name}");
                    Debug.Log($"[SimplePlayerController] Notification Behavior: {playerInput.notificationBehavior}");
                    
                    // List all available actions for debugging
                    foreach (var actionMap in playerInput.actions.actionMaps)
                    {
                        Debug.Log($"[SimplePlayerController] Action Map: {actionMap.name}");
                        foreach (var action in actionMap.actions)
                        {
                            Debug.Log($"[SimplePlayerController] - Action: {action.name}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("[SimplePlayerController] No PlayerInput component found!");
            }

            ConfigureRigidBody();

            initialSpawnPosition = transform.position;
            defaultLookDirection = transform.forward.sqrMagnitude > 0.01f
                ? transform.forward.normalized
                : Vector3.forward;
            desiredLook = defaultLookDirection;

            currentHealth = maxHealth;

            var networkObject = GetComponent<NetworkObject>();
            movementSystem.Initialize(transform, body, networkObject);
        }

        private void OnEnable()
        {
            // Setup direct input action connections as fallback
            if (playerInput?.actions != null)
            {
                var moveAction = playerInput.actions.FindAction("Move");
                var jumpAction = playerInput.actions.FindAction("Jump");
                var aimAction = playerInput.actions.FindAction("Aim");
                
                if (moveAction != null)
                {
                    moveAction.performed += OnMoveAction;
                    moveAction.canceled += OnMoveAction;
                    Debug.Log("[SimplePlayerController] Move action connected");
                }
                
                if (jumpAction != null)
                {
                    jumpAction.started += OnJumpAction;
                    jumpAction.canceled += OnJumpAction;
                    Debug.Log("[SimplePlayerController] Jump action connected");
                }
                
                if (aimAction != null)
                {
                    aimAction.performed += OnAimAction;
                    aimAction.canceled += OnAimAction;
                    Debug.Log("[SimplePlayerController] Aim action connected");
                }
            }
            
            SetInputEnabled(true);
        }

        private void OnDisable()
        {
            // Cleanup direct input action connections
            if (playerInput?.actions != null)
            {
                var moveAction = playerInput.actions.FindAction("Move");
                var jumpAction = playerInput.actions.FindAction("Jump");
                var aimAction = playerInput.actions.FindAction("Aim");
                
                if (moveAction != null)
                {
                    moveAction.performed -= OnMoveAction;
                    moveAction.canceled -= OnMoveAction;
                }
                
                if (jumpAction != null)
                {
                    jumpAction.started -= OnJumpAction;
                    jumpAction.canceled -= OnJumpAction;
                }
                
                if (aimAction != null)
                {
                    aimAction.performed -= OnAimAction;
                    aimAction.canceled -= OnAimAction;
                }
            }
        }

        // Direct action callback methods (fallback approach)
        private void OnMoveAction(InputAction.CallbackContext context)
        {
            OnMove(context);
        }
        
        private void OnJumpAction(InputAction.CallbackContext context)
        {
            OnJump(context);
        }
        
        private void OnAimAction(InputAction.CallbackContext context)
        {
            OnAim(context);
        }

        private void Update()
        {
            if (!inputActive || isDead)
            {
                movementSystem.SetMovementInput(Vector3.zero);
                return;
            }

            // Movement input is now handled via OnMove callback
            var moveVector = new Vector3(moveInput.x, 0f, moveInput.y);
            movementSystem.SetMovementInput(moveVector);
            UpdateDesiredOrientation(moveVector);

            HandleJumpHoldLogic();
        }

        private void FixedUpdate()
        {
            movementSystem.UpdateMovement();
        }

        private void ConfigureRigidBody()
        {
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        public void SetTeam(int newTeamIndex)
        {
            teamIndex = Mathf.Max(0, newTeamIndex);
        }







        private void HandleJumpHoldLogic()
        {
            if (!holdBoostCandidate)
            {
                return;
            }

            if (!movementSystem.IsHoldBoostPending && !holdBoostApplied)
            {
                holdBoostCandidate = false;
                return;
            }

            if (!jumpButtonHeld)
            {
                holdBoostCandidate = false;
                movementSystem.CancelHoldBoostCandidate();
                return;
            }

            float heldDuration = Time.time - jumpButtonPressTime;

            if (!holdBoostApplied && heldDuration >= holdJumpWarmup && heldDuration <= holdJumpMaxDuration)
            {
                if (movementSystem.TryApplyHoldBoost())
                {
                    holdBoostApplied = true;
                    holdBoostCandidate = false;
                }
            }
            else if (heldDuration > holdJumpMaxDuration)
            {
                holdBoostCandidate = false;
                movementSystem.CancelHoldBoostCandidate();
            }
        }

        private void UpdateDesiredOrientation(Vector3 movementInput)
        {
            if (movementInput.sqrMagnitude > 0.0001f)
            {
                desiredLook = movementInput.normalized;
            }
            else
            {
                desiredLook = Vector3.Slerp(desiredLook, defaultLookDirection, rotationResponsiveness * Time.deltaTime);
            }

            float targetYaw = Mathf.Atan2(desiredLook.x, desiredLook.z) * Mathf.Rad2Deg;
            float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref yawVelocity, 1f / Mathf.Max(0.01f, rotationResponsiveness));
            Quaternion nextRotation = Quaternion.Euler(0f, smoothedYaw, 0f);
            body.MoveRotation(nextRotation);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputActive = enabled;

            if (enabled)
            {
                // Input System actions are managed automatically
            }
            else
            {
                moveInput = Vector2.zero;
                // Input System actions are managed automatically
                jumpButtonHeld = false;
                holdBoostCandidate = false;
                holdBoostApplied = false;
                movementSystem.CancelHoldBoostCandidate();
                movementSystem.SetMovementInput(Vector3.zero);
                desiredLook = defaultLookDirection;
            }

            enhancedAbilitySystem?.SetInputEnabled(enabled);
        }

        #region IDamageable Implementation

        public void TakeDamage(float damage)
        {
            if (isDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                HandleDeath();
            }
        }

        public float GetHealth() => currentHealth;

        public bool IsDead() => isDead;

        public void ApplyServerHealthSnapshot(float health, bool dead)
        {
            float clamped = Mathf.Clamp(health, 0f, maxHealth);
            currentHealth = clamped;
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (dead)
            {
                if (!isDead)
                {
                    if (respawnRoutine != null)
                    {
                        StopCoroutine(respawnRoutine);
                        respawnRoutine = null;
                    }
                    isDead = true;
                    SetInputEnabled(false);
                    PhysicsUtility.SetVelocity(body, Vector3.zero);
                    Died?.Invoke();
                }
            }
            else if (isDead)
            {
                isDead = false;
                SetInputEnabled(true);
            }
        }

        private void HandleDeath()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            SetInputEnabled(false);
            PhysicsUtility.SetVelocity(body, Vector3.zero);
            Died?.Invoke();

            if (autoRespawn)
            {
                if (respawnRoutine != null)
                {
                    StopCoroutine(respawnRoutine);
                }
                respawnRoutine = StartCoroutine(RespawnAfterDelay());
            }
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            Vector3 targetPosition = respawnPoint != null ? respawnPoint.position : initialSpawnPosition;
            if (movementSystem != null)
            {
                movementSystem.WarpTo(targetPosition);
            }
            else
            {
                transform.position = targetPosition;
                PhysicsUtility.SetVelocity(body, Vector3.zero);
            }

            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);

            isDead = false;
            desiredLook = defaultLookDirection;
            transform.rotation = Quaternion.LookRotation(defaultLookDirection, Vector3.up);
            SetInputEnabled(true);
            respawnRoutine = null;
        }

        #endregion

        #region Input System Interface Implementation

        // Basic Input Actions (existing functionality)
        public void OnMove(InputAction.CallbackContext context)
        {
            Debug.Log($"[SimplePlayerController] OnMove called: {context.phase}");
            if (context.performed)
            {
                moveInput = context.ReadValue<Vector2>();
                Debug.Log($"[SimplePlayerController] Move input: {moveInput}");
            }
            else if (context.canceled)
            {
                moveInput = Vector2.zero;
                Debug.Log("[SimplePlayerController] Move input canceled");
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            Debug.Log($"[SimplePlayerController] OnJump called: {context.phase}");
            if (context.started)
            {
                if (!inputActive || isDead)
                {
                    return;
                }

                var jumpResult = movementSystem.TryExecuteJump();

                if (jumpResult == UnifiedMovementSystem.JumpExecutionResult.Initial)
                {
                    jumpButtonHeld = true;
                    holdBoostCandidate = true;
                    holdBoostApplied = false;
                    jumpButtonPressTime = Time.time;
                }
                else if (jumpResult == UnifiedMovementSystem.JumpExecutionResult.Double)
                {
                    jumpButtonHeld = true;
                    holdBoostCandidate = false;
                    holdBoostApplied = false;
                }
            }
            else if (context.canceled)
            {
                jumpButtonHeld = false;

                if (holdBoostCandidate)
                {
                    holdBoostCandidate = false;
                    movementSystem.CancelHoldBoostCandidate();
                }
            }
        }

        public void OnLMBAttack(InputAction.CallbackContext context) { /* Handled by PlayerAttackSystem */ }
        public void OnRMBAttack(InputAction.CallbackContext context) { /* Handled by PlayerAttackSystem */ }
        public void OnAbilityQ(InputAction.CallbackContext context) { /* Handled by EnhancedAbilitySystem */ }
        public void OnAbilityE(InputAction.CallbackContext context) { /* Handled by EnhancedAbilitySystem */ }
        public void OnAbilityG(InputAction.CallbackContext context) { /* Handled by EnhancedAbilitySystem */ }

        // Modern Input System Ability Handlers (required by InputSystem_Actions.cs)
        private float lastAbility1Time = 0f;
        private float lastAbility2Time = 0f;
        private float lastAbility3Time = 0f;
        private const float ABILITY_INPUT_DEBOUNCE = 0.1f; // Prevent rapid-fire input
        
        public void OnAbility1(InputAction.CallbackContext context)
        {
            if (context.performed && Time.time - lastAbility1Time > ABILITY_INPUT_DEBOUNCE)
            {
                lastAbility1Time = Time.time;
                // Forward to Enhanced Ability System
                var abilitySystem = GetComponent<EnhancedAbilitySystem>();
                if (abilitySystem != null)
                {
                    abilitySystem.TryCastAbility(0); // Ability index 0
                }
            }
        }

        public void OnAbility2(InputAction.CallbackContext context)
        {
            if (context.performed && Time.time - lastAbility2Time > ABILITY_INPUT_DEBOUNCE)
            {
                lastAbility2Time = Time.time;
                // Forward to Enhanced Ability System
                var abilitySystem = GetComponent<EnhancedAbilitySystem>();
                if (abilitySystem != null)
                {
                    abilitySystem.TryCastAbility(1); // Ability index 1
                }
            }
        }

        public void OnAbility3(InputAction.CallbackContext context)
        {
            if (context.performed && Time.time - lastAbility3Time > ABILITY_INPUT_DEBOUNCE)
            {
                lastAbility3Time = Time.time;
                // Forward to Enhanced Ability System
                var abilitySystem = GetComponent<EnhancedAbilitySystem>();
                if (abilitySystem != null)
                {
                    abilitySystem.TryCastAbility(2); // Ability index 2
                }
            }
        }

        #region General Input Handlers

        // General Action Input Handlers
        public void OnAim(InputAction.CallbackContext context) 
        { 
            Debug.Log($"[SimplePlayerController] OnAim called: {context.phase}");
        }
        
        public void OnAttack(InputAction.CallbackContext context) 
        { 
            // Forward to PlayerAttackSystem
            var attackSystem = GetComponent<PlayerAttackSystem>();
            if (attackSystem != null)
            {
                if (context.performed)
                {
                    attackSystem.OnPrimaryAttackPerformed(context);
                }
                else if (context.canceled)
                {
                    attackSystem.OnPrimaryAttackCanceled(context);
                }
            }
        }
        
        public void OnAttackNPC(InputAction.CallbackContext context) 
        { 
            // Forward to PlayerAttackSystem
            var attackSystem = GetComponent<PlayerAttackSystem>();
            if (attackSystem != null)
            {
                if (context.performed)
                {
                    attackSystem.OnNPCAttackPerformed(context);
                }
                else if (context.canceled)
                {
                    attackSystem.OnNPCAttackCanceled(context);
                }
            }
        }
        public void OnCancel(InputAction.CallbackContext context) { }
        public void OnScore(InputAction.CallbackContext context) { }
        public void OnItem(InputAction.CallbackContext context) { }
        public void OnHome(InputAction.CallbackContext context) { }
        public void OnChat(InputAction.CallbackContext context) 
        { 
            // Forward to ChatPingSystem
            var chatSystem = GetComponent<ChatPingSystem>();
            if (chatSystem != null)
            {
                if (context.performed)
                {
                    chatSystem.OnChatInputPerformed(context);
                }
                else if (context.canceled)
                {
                    chatSystem.OnChatInputCanceled(context);
                }
            }
        }

        #endregion

        #region IEvolutionInputHandler Implementation

        /// <summary>
        /// Handle first evolution path selection (mapped to "1" key)
        /// </summary>
        public void OnEvolutionPath1()
        {
            if (evolutionHandler != null)
            {
                evolutionHandler.SelectEvolutionPath(EvolutionPath.PathA);
            }
        }

        /// <summary>
        /// Handle second evolution path selection (mapped to "2" key)
        /// </summary>
        public void OnEvolutionPath2()
        {
            if (evolutionHandler != null)
            {
                evolutionHandler.SelectEvolutionPath(EvolutionPath.PathB);
            }
        }

        #endregion

        #region Unite-Style Evolution Input System

        // Evolution Input Handlers
        public void OnAbilitySelect1(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnEvolutionPath1();
            }
        }

        public void OnAbilitySelect2(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                OnEvolutionPath2();
            }
        }

        #endregion

        #endregion

        private void OnDrawGizmosSelected()
        {
            movementSystem?.DrawDebugInfo();
        }
    }
}
