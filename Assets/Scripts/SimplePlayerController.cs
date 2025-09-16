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
    public class SimplePlayerController : MonoBehaviour, IDamageable
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

        private Rigidbody body;
        private PlayerInput playerInput;
        private EnhancedAbilitySystem enhancedAbilitySystem;

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

        public event Action<float, float> HealthChanged;
        public event Action Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            playerInput = GetComponent<PlayerInput>();
            enhancedAbilitySystem = GetComponent<EnhancedAbilitySystem>();

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
            }
        }

        private void TeardownInputActions()
        {
            if (jumpAction != null)
            {
                jumpAction.started -= OnJumpStarted;
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

            movementSystem.TryJump();
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

        private void HandleDeath()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;
            SetInputEnabled(false);
            body.linearVelocity = Vector3.zero;
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
            transform.position = targetPosition;
            body.linearVelocity = Vector3.zero;

            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);

            isDead = false;
            desiredLook = defaultLookDirection;
            transform.rotation = Quaternion.LookRotation(defaultLookDirection, Vector3.up);
            SetInputEnabled(true);
            respawnRoutine = null;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            movementSystem?.DrawDebugInfo();
        }
    }
}
