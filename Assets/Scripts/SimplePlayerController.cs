using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MOBA.Abilities;

namespace MOBA
{
    /// <summary>
    /// Fully rebuilt player controller that leverages modern physics and Input System APIs.
    /// Implements continuous forward locomotion, advanced jump logic, and minimal combat hooks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class SimplePlayerController : MonoBehaviour, IDamageable
    {
        [Header("Movement")]
        [SerializeField] private float forwardSpeed = 7.5f;
        [SerializeField] private float rotationResponsiveness = 14f;
        [SerializeField] private float movementBlend = 12f;
        [SerializeField] private float gravityScale = 1.1f;

        [Header("Jump Heights (meters)")]
        [SerializeField] private float baseJumpHeight = 1.0f;
        [SerializeField] private float highJumpHeight = 1.5f;
        [SerializeField] private float doubleJumpHeight = 2.0f;
        [SerializeField] private float apexDoubleJumpHeight = 2.8f;

        [Header("Jump Timing")]
        [SerializeField] private float jumpHoldWindow = 0.18f;
        [SerializeField] private float apexVelocityTolerance = 0.35f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundProbe;
        [SerializeField] private float probeRadius = 0.3f;
        [SerializeField] private LayerMask groundLayers = ~0;

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
        private bool inputActive = true;

        private bool isGrounded;
        private float lastGroundedTime;
        private bool jumpHeld;
        private bool jumpQueued;
        private float lastJumpRequestTime;
        private bool canDoubleJump;
        private bool jumpHoldActive;
        private float jumpStartedTime;
        private bool lastJumpReachedHigh;

        private float holdBoostRate;

        private float currentHealth;
        private bool isDead;
        private Vector3 initialSpawnPosition;
        private Coroutine respawnRoutine;

        private float yawVelocity;

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
            EnsureGroundProbe();

            initialSpawnPosition = transform.position;
            defaultLookDirection = transform.forward.sqrMagnitude > 0.01f
                ? transform.forward.normalized
                : Vector3.forward;
            desiredLook = defaultLookDirection;

            currentHealth = maxHealth;
            holdBoostRate = Mathf.Max(0.01f, CalculateJumpVelocity(highJumpHeight) - CalculateJumpVelocity(baseJumpHeight)) /
                             Mathf.Max(0.01f, jumpHoldWindow);
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
            if (isDead || !inputActive)
            {
                moveInput = Vector2.zero;
                return;
            }

            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }

            UpdateDesiredOrientation();
        }

        private void FixedUpdate()
        {
            ApplyGroundCheck();
            HandleJumpBuffer();
            ApplyJumpHoldBoost();
            ApplyForwardMotion();
            ApplyAdditionalGravity();
        }

        private void ConfigureRigidBody()
        {
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void EnsureGroundProbe()
        {
            if (groundProbe == null)
            {
                GameObject probe = new GameObject("GroundProbe");
                probe.transform.SetParent(transform);
                probe.transform.localPosition = Vector3.down * 0.9f;
                groundProbe = probe.transform;
            }
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

            if (moveAction != null)
            {
                moveAction.Enable();
            }

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

            if (moveAction != null)
            {
                moveAction.Disable();
            }
        }

        private void OnJumpStarted(InputAction.CallbackContext context)
        {
            if (!inputActive || isDead)
            {
                return;
            }

            jumpHeld = true;
            jumpQueued = true;
            lastJumpRequestTime = Time.time;
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            jumpHeld = false;
            jumpHoldActive = false;
        }

        private void UpdateDesiredOrientation()
        {
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            if (inputDirection.sqrMagnitude > 0.0001f)
            {
                desiredLook = inputDirection.normalized;
            }
            else
            {
                desiredLook = Vector3.Slerp(desiredLook, defaultLookDirection, rotationResponsiveness * Time.deltaTime);
            }

            float targetYaw = Mathf.Atan2(desiredLook.x, desiredLook.z) * Mathf.Rad2Deg;
            float smoothedYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref yawVelocity, 1f / rotationResponsiveness);
            Quaternion nextRotation = Quaternion.Euler(0f, smoothedYaw, 0f);
            body.MoveRotation(nextRotation);
        }

        private void ApplyForwardMotion()
        {
            if (isDead)
            {
                Vector3 haltedVelocity = body.linearVelocity;
                haltedVelocity.x = 0f;
                haltedVelocity.z = 0f;
                body.linearVelocity = haltedVelocity;
                return;
            }

            Vector3 desiredVelocity = transform.forward * forwardSpeed;
            Vector3 current = body.linearVelocity;
            desiredVelocity.y = current.y;
            Vector3 blended = Vector3.Lerp(current, desiredVelocity, movementBlend * Time.fixedDeltaTime);
            body.linearVelocity = blended;
        }

        private void ApplyGroundCheck()
        {
            bool wasGrounded = isGrounded;
            Vector3 probePosition = groundProbe != null ? groundProbe.position : transform.position + Vector3.down * 0.9f;
            isGrounded = Physics.CheckSphere(probePosition, probeRadius, groundLayers, QueryTriggerInteraction.Ignore);

            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                canDoubleJump = true;
                jumpHoldActive = false;
                if (!wasGrounded)
                {
                    lastJumpReachedHigh = false;
                }
            }
        }

        private void HandleJumpBuffer()
        {
            if (!jumpQueued)
            {
                return;
            }

            bool canUseGroundJump = isGrounded || Time.time <= lastGroundedTime + coyoteTime;
            bool bufferedExpired = Time.time > lastJumpRequestTime + jumpBufferTime;

            if (canUseGroundJump)
            {
                ExecuteGroundJump();
                return;
            }

            if (!isGrounded && canDoubleJump)
            {
                ExecuteDoubleJump();
                return;
            }

            if (bufferedExpired)
            {
                jumpQueued = false;
            }
        }

        private void ExecuteGroundJump()
        {
            jumpQueued = false;
            canDoubleJump = true;
            jumpHoldActive = true;
            jumpStartedTime = Time.time;
            lastJumpReachedHigh = false;

            Vector3 velocity = body.linearVelocity;
            velocity.y = CalculateJumpVelocity(baseJumpHeight);
            body.linearVelocity = velocity;
        }

        private void ExecuteDoubleJump()
        {
            jumpQueued = false;
            canDoubleJump = false;
            jumpHoldActive = false;

            bool atHighApex = lastJumpReachedHigh && Mathf.Abs(body.linearVelocity.y) <= apexVelocityTolerance;
            float targetHeight = atHighApex ? apexDoubleJumpHeight : doubleJumpHeight;
            Vector3 velocity = body.linearVelocity;
            velocity.y = CalculateJumpVelocity(targetHeight);
            body.linearVelocity = velocity;

            lastJumpReachedHigh = atHighApex;
        }

        private void ApplyJumpHoldBoost()
        {
            if (!jumpHoldActive)
            {
                return;
            }

            if (!jumpHeld || Time.time > jumpStartedTime + jumpHoldWindow)
            {
                jumpHoldActive = false;
                return;
            }

            float targetVelocity = CalculateJumpVelocity(highJumpHeight);
            Vector3 current = body.linearVelocity;
            float boostedY = Mathf.MoveTowards(current.y, targetVelocity, holdBoostRate * Time.fixedDeltaTime);
            current.y = boostedY;
            body.linearVelocity = current;

            if (Mathf.Abs(boostedY - targetVelocity) <= 0.05f)
            {
                lastJumpReachedHigh = true;
            }
        }

        private void ApplyAdditionalGravity()
        {
            if (gravityScale <= 1f)
            {
                return;
            }

            Vector3 extraGravity = Physics.gravity * (gravityScale - 1f);
            body.AddForce(extraGravity, ForceMode.Acceleration);
        }

        private float CalculateJumpVelocity(float height)
        {
            float gravityMagnitude = Mathf.Abs(Physics.gravity.y * gravityScale);
            return Mathf.Sqrt(Mathf.Max(0.01f, 2f * gravityMagnitude * height));
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
                jumpQueued = false;
                jumpHoldActive = false;
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
            SetInputEnabled(true);
            desiredLook = defaultLookDirection;
            transform.rotation = Quaternion.LookRotation(defaultLookDirection, Vector3.up);
            respawnRoutine = null;
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (groundProbe == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundProbe.position, probeRadius);
        }
    }
}
