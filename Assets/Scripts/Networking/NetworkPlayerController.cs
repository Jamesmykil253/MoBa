using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.Networking
{
    /// <summary>
    /// Server-authoritative player controller with client prediction
    /// Implements Netcode for GameObjects with security validation and performance optimization
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public class NetworkPlayerController : NetworkBehaviour, IDamageable
    {
        [Header("Network Settings")]
        [SerializeField] private float serverTickRate = 60f;
        [SerializeField] private float reconciliationThreshold = 0.5f;
        [SerializeField] private int maxAbilityCastsPerSecond = 10;
        [SerializeField] private float inputBufferSize = 0.1f; // 100ms input buffer

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 350f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float doubleJumpForce = 6f;
        [SerializeField] private float maxSpeed = 15f; // Anti-cheat speed limit

        [Header("Player Stats")]
        [SerializeField] private float maxHealth = 1000f;

        // Network state with delta compression
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>(
            1000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> networkCryptoCoins = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Client prediction with reconciliation
        private Vector3 predictedPosition;
        private Vector3 predictedVelocity;
        private Queue<ClientInput> pendingInputs = new Queue<ClientInput>();
        private Queue<ClientInput> processedInputs = new Queue<ClientInput>();
        private float lastServerUpdate;
        private Coroutine inputCoroutine;
        private Vector3 lastSentPosition;
        private const float POSITION_SEND_THRESHOLD = 0.01f;

        // Anti-cheat and validation
        private float lastAbilityCastTime;
        private int abilityCastCount;
        private float rateLimitWindow;
        private Vector3 lastValidPosition;
        private float lastInputTime;
        private int speedViolationCount;

        // Lag compensation
        private struct HistoricalState
        {
            public Vector3 position;
            public Vector3 velocity;
            public float timestamp;
        }
        private Queue<HistoricalState> positionHistory = new Queue<HistoricalState>();
        private const float HISTORY_LENGTH = 1f;

        // Components
        private Rigidbody rb;
        private SpriteRenderer spriteRenderer;
        private Animator animator;

        // State tracking
        private bool isGrounded;
        private bool canDoubleJump;
        private Vector3 movementInput;
        private Vector2 aimDirection;

        // Observer pattern integration
        public event System.Action<Vector3> OnPositionChanged;
        public event System.Action<float> OnHealthChanged;
        public event System.Action<int> OnCoinsChanged;

        // Public properties
        public Vector3 MovementInput => movementInput;
        public float Health => networkHealth.Value;
        public int CryptoCoins => networkCryptoCoins.Value;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                networkPosition.Value = transform.position;
                networkHealth.Value = maxHealth;
                networkCryptoCoins.Value = 0;
            }

            if (IsClient && IsOwner)
            {
                // Initialize client prediction
                predictedPosition = transform.position;
                inputCoroutine = StartCoroutine(SendInputToServer());
            }

            // Subscribe to network variable changes
            networkPosition.OnValueChanged += OnNetworkPositionChanged;
            networkHealth.OnValueChanged += OnNetworkHealthChanged;
            networkCryptoCoins.OnValueChanged += OnNetworkCoinsChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (inputCoroutine != null)
            {
                StopCoroutine(inputCoroutine);
            }

            networkPosition.OnValueChanged -= OnNetworkPositionChanged;
            networkHealth.OnValueChanged -= OnNetworkHealthChanged;
            networkCryptoCoins.OnValueChanged -= OnNetworkCoinsChanged;
        }

        private void Update()
        {
            if (IsServer)
            {
                ServerUpdate();
            }
            else if (IsClient && IsOwner)
            {
                ClientUpdate();
            }

            // Update ground detection (local for all)
            UpdateGroundDetection();

            // Update animations
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            if (IsServer)
            {
                ServerFixedUpdate();
            }
            else if (IsClient && IsOwner)
            {
                ClientFixedUpdate();
            }
        }

        private void ServerUpdate()
        {
            // Server authoritative updates
            UpdateCooldowns();
        }

        private void ServerFixedUpdate()
        {
            // Apply server movement
            ApplyServerMovement();

            // Update network variables
            networkPosition.Value = transform.position;
            networkVelocity.Value = rb.linearVelocity;
        }

        private void ClientUpdate()
        {
            // Client-side prediction
            ApplyClientPrediction();

            // Reconciliation
            if (Time.time - lastServerUpdate > 1f / serverTickRate)
            {
                ReconcileWithServer();
            }

            // Update aim direction
            UpdateAimDirection();
        }

        private void ClientFixedUpdate()
        {
            // Client prediction movement
            if (movementInput != Vector3.zero)
            {
                Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                Vector3 cameraRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                Vector3 moveDirection = cameraRight * movementInput.x + cameraForward * movementInput.z;
                rb.linearVelocity = moveDirection * moveSpeed + Vector3.up * rb.linearVelocity.y;
            }
        }

        private void UpdateGroundDetection()
        {
            RaycastHit hit;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, 1.1f, LayerMask.GetMask("Ground"));

            if (isGrounded)
            {
                canDoubleJump = true;
            }
        }

        private void UpdateAimDirection()
        {
            if (Camera.main != null && UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(mousePosition);
                aimDirection = (worldMousePos - (Vector2)transform.position).normalized;
            }
        }

        private void ApplyServerMovement()
        {
            // Server applies movement based on validated inputs
            if (movementInput != Vector3.zero)
            {
                Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
                Vector3 cameraRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;

                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();

                Vector3 moveDirection = cameraRight * movementInput.x + cameraForward * movementInput.z;
                // Clamp speed to prevent excessive movement
                float clampedSpeed = Mathf.Min(moveSpeed, maxSpeed);
                rb.linearVelocity = moveDirection * clampedSpeed + Vector3.up * rb.linearVelocity.y;
            }
        }

        private void ApplyClientPrediction()
        {
            // Client-side prediction logic
            predictedPosition = transform.position;
            predictedVelocity = rb.linearVelocity;
        }

        private void ReconcileWithServer()
        {
            // Reconciliation with server state
            if (Vector3.Distance(transform.position, networkPosition.Value) > reconciliationThreshold)
            {
                transform.position = networkPosition.Value;
                rb.linearVelocity = networkVelocity.Value;
                Debug.Log($"[NetworkPlayerController] Reconciliation: {transform.position} -> {networkPosition.Value}");
            }
            lastServerUpdate = Time.time;
        }

        private IEnumerator SendInputToServer()
        {
            float bufferTime = 1f / serverTickRate;
            float timeSinceLastSend = 0f;

            while (true)
            {
                timeSinceLastSend += Time.deltaTime;

                // Send input at configured buffer rate
                if (timeSinceLastSend >= inputBufferSize)
                {
                    // Collect current input
                    ClientInput input = new ClientInput
                    {
                        movement = movementInput,
                        jump = false, // Will be set by input system
                        abilityCast = false,
                        abilityTarget = aimDirection,
                        timestamp = Time.time
                    };

                    // Send to server
                    SubmitInputServerRpc(input);

                    timeSinceLastSend = 0f;
                }

                yield return null;
            }
        }

        [ServerRpc]
        private void SubmitInputServerRpc(ClientInput input, ServerRpcParams rpcParams = default)
        {
            // Server validation
            if (!ValidateInput(input)) return;

            // Apply validated input
            ApplyValidatedInput(input);

            // Relay to other clients
            RelayInputClientRpc(input);
        }

        [ClientRpc]
        private void RelayInputClientRpc(ClientInput input)
        {
            if (!IsOwner)
            {
                // Apply input from other players
                ApplyNetworkInput(input);
            }
        }

        private bool ValidateInput(ClientInput input)
        {
            // Speed validation
            if (input.movement.magnitude > 1.5f)
            {
                Debug.LogWarning($"[NetworkPlayerController] Invalid movement magnitude: {input.movement.magnitude}");
                return false;
            }

            // Ability rate limiting
            if (input.abilityCast && !CheckAbilityRateLimit())
            {
                Debug.LogWarning("[NetworkPlayerController] Ability rate limit exceeded");
                return false;
            }

            return true;
        }

        private bool CheckAbilityRateLimit()
        {
            float currentTime = Time.time;

            if (currentTime - rateLimitWindow > 1f)
            {
                rateLimitWindow = currentTime;
                abilityCastCount = 0;
            }

            if (abilityCastCount >= maxAbilityCastsPerSecond) return false;

            abilityCastCount++;
            return true;
        }

        private void ApplyValidatedInput(ClientInput input)
        {
            movementInput = input.movement;

            if (input.jump)
            {
                Jump();
            }
        }

        private void ApplyNetworkInput(ClientInput input)
        {
            // Apply input from network for other players
            movementInput = input.movement;
        }

        public void SetMovementInput(Vector3 input)
        {
            movementInput = input;
        }

        public void Jump()
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
                canDoubleJump = true;
            }
            else if (canDoubleJump)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, doubleJumpForce, rb.linearVelocity.z);
                canDoubleJump = false;
            }
        }

        public void TakeDamage(float damage)
        {
            if (!IsServer) return;

            networkHealth.Value = Mathf.Max(0, networkHealth.Value - damage);

            if (networkHealth.Value <= 0)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            // Server authoritative death
            DeathClientRpc();
        }

        [ClientRpc]
        private void DeathClientRpc()
        {
            // Client death handling
            Respawn();
        }

        private void Respawn()
        {
            if (IsServer)
            {
                networkPosition.Value = GetSpawnPosition();
                networkHealth.Value = maxHealth;
                networkVelocity.Value = Vector3.zero;
            }
        }

        private Vector3 GetSpawnPosition()
        {
            // Server determines spawn position
            return Vector3.zero; // Implement spawn logic
        }

        public void AddCryptoCoins(int amount)
        {
            if (IsServer)
            {
                networkCryptoCoins.Value += amount;
            }
        }

        private void UpdateCooldowns()
        {
            // Update ability rate limiting window
            if (Time.time - rateLimitWindow > 1f)
            {
                rateLimitWindow = Time.time;
                abilityCastCount = 0;
            }
        }

        private void UpdatePositionHistory()
        {
            // Store position history for lag compensation
            if (IsServer)
            {
                positionHistory.Enqueue(new HistoricalState
                {
                    position = transform.position,
                    velocity = rb.linearVelocity,
                    timestamp = Time.time
                });

                // Remove old history
                while (positionHistory.Count > 0 && Time.time - positionHistory.Peek().timestamp > HISTORY_LENGTH)
                {
                    positionHistory.Dequeue();
                }
            }
        }

        // Lag compensation method
        public (Vector3 position, Vector3 velocity) GetPositionAtTime(float timestamp)
        {
            if (positionHistory.Count == 0) return (transform.position, rb.linearVelocity);

            // Find appropriate historical state
            HistoricalState[] history = positionHistory.ToArray();
            for (int i = history.Length - 1; i >= 0; i--)
            {
                if (history[i].timestamp <= timestamp)
                {
                    return (history[i].position, history[i].velocity);
                }
            }

            return (history[0].position, history[0].velocity);
        }

        private void UpdateAnimations()
        {
            if (animator != null)
            {
                animator.SetBool("IsGrounded", isGrounded);
                animator.SetFloat("MoveSpeed", Mathf.Abs(movementInput.x));
                animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);

                if (movementInput.x != 0 && spriteRenderer != null)
                {
                    spriteRenderer.flipX = movementInput.x < 0;
                }
            }
        }

        // Network variable change handlers
        private void OnNetworkPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (!IsOwner)
            {
                // Smooth interpolation for other players
                transform.position = Vector3.Lerp(transform.position, newValue, Time.deltaTime * 10f);
            }

            // Notify observers
            OnPositionChanged?.Invoke(newValue);
        }

        private void OnNetworkHealthChanged(float previousValue, float newValue)
        {
            // Handle health UI updates
            Debug.Log($"[NetworkPlayerController] Health changed: {previousValue} -> {newValue}");

            // Notify observers
            OnHealthChanged?.Invoke(newValue);
        }

        private void OnNetworkCoinsChanged(int previousValue, int newValue)
        {
            // Notify observers
            OnCoinsChanged?.Invoke(newValue);
        }
    }

    public struct ClientInput : INetworkSerializable
    {
        public Vector3 movement;
        public bool jump;
        public bool abilityCast;
        public Vector3 abilityTarget;
        public float timestamp;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref movement);
            serializer.SerializeValue(ref jump);
            serializer.SerializeValue(ref abilityCast);
            serializer.SerializeValue(ref abilityTarget);
            serializer.SerializeValue(ref timestamp);
        }
    }
}