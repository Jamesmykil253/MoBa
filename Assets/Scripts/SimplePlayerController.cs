using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Abilities;

namespace MOBA
{
    /// <summary>
    /// Player controller that composes the unified movement and ability systems for AAA-ready modularity.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class SimplePlayerController : MonoBehaviour, IDamageable, IDamageSnapshotReceiver, IEvolutionInputHandler
    {
        [Header("Movement")]
        [SerializeField] private UnifiedMovementSystem movementSystem = new UnifiedMovementSystem();
        [SerializeField] private float rotationResponsiveness = 14f;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference moveActionReference;
        [SerializeField] private InputActionReference jumpActionReference;
        [SerializeField] private string fallbackMoveAction = "Move";
        [SerializeField] private string fallbackJumpAction = "Jump";

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

        private InputAction moveAction;
        private InputAction jumpAction;
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
            ResolveInputActions();
            SetInputEnabled(true);
        }

        private void OnDisable()
        {
            TeardownInputActions();
        }

        private void Update()
        {
            if (!inputActive || isDead)
            {
                movementSystem.SetMovementInput(Vector3.zero);
                return;
            }

            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }

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

        private void ResolveInputActions()
        {
            if (playerInput == null)
            {
                return;
            }

            if (moveActionReference != null)
            {
                moveAction = moveActionReference.action;
            }
            else if (playerInput.actions != null)
            {
                moveAction = playerInput.actions.FindAction(fallbackMoveAction, throwIfNotFound: false);
            }

            if (jumpActionReference != null)
            {
                jumpAction = jumpActionReference.action;
            }
            else if (playerInput.actions != null)
            {
                jumpAction = playerInput.actions.FindAction(fallbackJumpAction, throwIfNotFound: false);
            }

            moveAction?.Enable();

            if (jumpAction != null)
            {
                jumpAction.Enable();
                jumpAction.started += OnJumpStarted;
                jumpAction.canceled += OnJumpCanceled;
            }
        }

        private void TeardownInputActions()
        {
            if (jumpAction != null)
            {
                jumpAction.started -= OnJumpStarted;
                jumpAction.canceled -= OnJumpCanceled;
                jumpAction.Disable();
            }

            moveAction?.Disable();
        }

        private void OnJumpStarted(InputAction.CallbackContext context)
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

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            jumpButtonHeld = false;

            if (holdBoostCandidate)
            {
                holdBoostCandidate = false;
                movementSystem.CancelHoldBoostCandidate();
            }
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
                moveAction?.Enable();
                jumpAction?.Enable();
            }
            else
            {
                moveInput = Vector2.zero;
                moveAction?.Disable();
                jumpAction?.Disable();
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
            if (context.performed)
            {
                moveInput = context.ReadValue<Vector2>();
            }
            else if (context.canceled)
            {
                moveInput = Vector2.zero;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.started)
            {
                OnJumpStarted(context);
            }
            else if (context.canceled)
            {
                OnJumpCanceled(context);
            }
        }

        public void OnLMBAttack(InputAction.CallbackContext context) { /* Handled by PlayerAttackSystem */ }
        public void OnRMBAttack(InputAction.CallbackContext context) { /* Handled by PlayerAttackSystem */ }
        public void OnAbilityQ(InputAction.CallbackContext context) { /* Handled by EnhancedAbilitySystem */ }
        public void OnAbilityE(InputAction.CallbackContext context) { /* Handled by EnhancedAbilitySystem */ }
        public void OnAbilityG(InputAction.CallbackContext context) { /* Handled by EnhancedAbilitySystem */ }
        public void OnChat(InputAction.CallbackContext context) { /* Handled by ChatPingSystem */ }

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
