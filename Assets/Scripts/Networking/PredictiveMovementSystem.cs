using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Concurrent;
using MOBA.Movement;
using MOBA;

namespace MOBA.Networking
{
    /// <summary>
    /// Predictive movement system implementing client-side prediction for competitive gameplay
    /// Reference: Multiplayer Game Programming Chapter 4-5, Game Programming Patterns Chapter 12
    /// Provides responsive movement with server reconciliation and rollback networking
    /// </summary>
    public class PredictiveMovementSystem : NetworkBehaviour
    {
        #region Configuration
        
        [Header("Prediction Configuration")]
        [SerializeField, Tooltip("Maximum number of frames to store for prediction history")]
        private int maxPredictionFrames = 120; // 2 seconds at 60fps
        
        [SerializeField, Tooltip("Distance threshold for triggering reconciliation")]
        private float reconciliationThreshold = 0.1f;
        
        [SerializeField, Tooltip("Maximum time between input samples")]
        private float maxInputSampleTime = 1.0f / 60.0f; // 60fps
        
        [SerializeField, Tooltip("Enable prediction for local client")]
        private bool enableClientPrediction = true;
        
        [SerializeField, Tooltip("Enable rollback and replay on misprediction")]
        private bool enableRollbackReconciliation = true;
        
        [SerializeField, Tooltip("Smoothing factor for position corrections")]
        [Range(0.1f, 1.0f)]
        private float correctionSmoothingFactor = 0.8f;
        
        [SerializeField, Tooltip("Enable debug logging for prediction system")]
        private bool enableDebugLogging = false;
        
        #endregion
        
        #region State Data
        
        private Queue<MovementInput> pendingInputs = new Queue<MovementInput>();
        private List<MovementSnapshot> stateHistory = new List<MovementSnapshot>();
        private uint currentFrame = 0;
        private float lastInputTime = 0f;
        
        // Component references
        private UnifiedMovementSystem movementSystem;
        private Rigidbody playerRigidbody;
        private Transform playerTransform;
        
        // Network prediction state
        private Vector3 serverPosition = Vector3.zero;
        private Vector3 serverVelocity = Vector3.zero;
        private uint lastServerFrame = 0;
        private bool hasPendingCorrection = false;
        
        // Input sequencing
        private uint inputSequenceNumber = 0;
        private Dictionary<uint, MovementInput> unacknowledgedInputs = new Dictionary<uint, MovementInput>();
        
        // Performance metrics
        private int totalInputsSent = 0;
        private int totalReconciliations = 0;
        private float totalReconciliationDistance = 0f;
        private float lastReconciliationTime = 0f;
        
        // Smoothing for corrections
        private Coroutine positionCorrectionCoroutine;
        private Vector3 correctionStartPosition;
        private Vector3 correctionTargetPosition;
        private float correctionProgress = 0f;
        
        #endregion
        
        #region Structures
        
        /// <summary>
        /// Input data for movement prediction with validation
        /// </summary>
        [System.Serializable]
        public struct MovementInput : INetworkSerializable
        {
            public Vector3 moveVector;
            public bool jumpPressed;
            public bool dashPressed;
            public uint frame;
            public float timestamp;
            public ushort checksum; // Input validation
            public uint sequenceNumber; // For input ordering
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref moveVector);
                serializer.SerializeValue(ref jumpPressed);
                serializer.SerializeValue(ref dashPressed);
                serializer.SerializeValue(ref frame);
                serializer.SerializeValue(ref timestamp);
                serializer.SerializeValue(ref checksum);
                serializer.SerializeValue(ref sequenceNumber);
            }
            
            /// <summary>
            /// Calculate checksum for input validation
            /// </summary>
            public ushort CalculateChecksum()
            {
                int hash = moveVector.GetHashCode();
                hash ^= jumpPressed.GetHashCode() << 1;
                hash ^= dashPressed.GetHashCode() << 2;
                hash ^= frame.GetHashCode() << 3;
                hash ^= sequenceNumber.GetHashCode() << 4;
                return (ushort)(hash & 0xFFFF);
            }
            
            /// <summary>
            /// Update checksum after setting values
            /// </summary>
            public void UpdateChecksum()
            {
                checksum = CalculateChecksum();
            }
            
            /// <summary>
            /// Validate checksum
            /// </summary>
            public bool IsValid()
            {
                return checksum == CalculateChecksum();
            }
        }
        
        /// <summary>
        /// State snapshot for prediction history
        /// </summary>
        [System.Serializable]
        public struct MovementSnapshot
        {
            public Vector3 position;
            public Vector3 velocity;
            public bool isGrounded;
            public uint frame;
            public float timestamp;
            public MovementInput input;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Get required components
            movementSystem = GetComponent<UnifiedMovementSystem>();
            if (movementSystem == null)
            {
                // Fallback to check if there's an enhanced movement system
                var enhancedMovement = GetComponent<MOBA.Movement.EnhancedMovementSystem>();
                if (enhancedMovement == null)
                {
                    Debug.LogError("[PredictiveMovementSystem] No movement system found! Requires UnifiedMovementSystem or EnhancedMovementSystem");
                    enabled = false;
                    return;
                }
            }
            
            playerRigidbody = GetComponent<Rigidbody>();
            playerTransform = transform;
            
            if (playerRigidbody == null || playerTransform == null)
            {
                Debug.LogError("[PredictiveMovementSystem] Missing required components (Rigidbody or Transform)");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            if (!IsOwner && !IsServer)
            {
                // Only owner and server need prediction system
                enabled = false;
                return;
            }
            
            // Initialize prediction state
            currentFrame = 0;
            lastInputTime = Time.time;
            
            Debug.Log($"[PredictiveMovementSystem] Initialized for {(IsOwner ? "Client" : "Server")} (ClientId: {OwnerClientId})");
        }
        
        private void FixedUpdate()
        {
            if (!IsSpawned)
                return;
                
            currentFrame++;
            
            if (IsOwner)
            {
                HandleClientPrediction();
            }
            else if (IsServer)
            {
                HandleServerAuthoritative();
            }
            
            // Cleanup old state history
            CleanupOldHistory();
        }
        
        #endregion
        
        #region Client-Side Prediction
        
        /// <summary>
        /// Handle client-side prediction for responsive movement
        /// </summary>
        private void HandleClientPrediction()
        {
            if (!enableClientPrediction)
                return;
                
            // Process pending corrections from server
            if (hasPendingCorrection)
            {
                ProcessServerCorrection();
            }
            
            // Sample current input
            MovementInput currentInput = SampleCurrentInput();
            
            // Apply input immediately for responsiveness (prediction)
            if (movementSystem != null)
            {
                ApplyInputToMovementSystem(currentInput);
            }
            
            // Store input for server reconciliation
            StoreInputForReconciliation(currentInput);
            
            // Submit input to server with frame number
            SubmitMovementInputRpc(currentInput);
            
            // Store state snapshot for potential rollback
            StoreStateSnapshot(currentInput);
        }
        
        /// <summary>
        /// Sample current input state with validation
        /// </summary>
        private MovementInput SampleCurrentInput()
        {
            MovementInput input = new MovementInput
            {
                moveVector = GetCurrentMovementInput(),
                jumpPressed = GetJumpInputThisFrame(),
                dashPressed = GetDashInputThisFrame(),
                frame = currentFrame,
                timestamp = Time.fixedTime,
                sequenceNumber = ++inputSequenceNumber
            };
            
            // Calculate and set checksum for validation
            input.UpdateChecksum();
            
            return input;
        }
        
        /// <summary>
        /// Get current movement input from input system (integrated with SimplePlayerController)
        /// </summary>
        private Vector3 GetCurrentMovementInput()
        {
            // Try to get input from SimplePlayerController if available
            var playerController = GetComponent<SimplePlayerController>();
            if (playerController != null)
            {
                // Access moveInput field through reflection or add public property
                var moveInputField = typeof(SimplePlayerController).GetField("moveInput", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (moveInputField != null)
                {
                    var moveInput2D = (Vector2)moveInputField.GetValue(playerController);
                    return new Vector3(moveInput2D.x, 0, moveInput2D.y);
                }
            }
            
            // Fallback to direct input if no controller found
            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) input.z += 1f;
            if (Input.GetKey(KeyCode.S)) input.z -= 1f;
            if (Input.GetKey(KeyCode.A)) input.x -= 1f;
            if (Input.GetKey(KeyCode.D)) input.x += 1f;
            
            return input.normalized;
        }
        
        /// <summary>
        /// Check if jump was pressed this frame
        /// </summary>
        private bool GetJumpInputThisFrame()
        {
            // Placeholder - would integrate with actual input system
            return Input.GetKeyDown(KeyCode.Space);
        }
        
        /// <summary>
        /// Check if dash was pressed this frame
        /// </summary>
        private bool GetDashInputThisFrame()
        {
            // Placeholder - would integrate with actual input system
            return Input.GetKeyDown(KeyCode.LeftShift);
        }
        
        /// <summary>
        /// Apply input to movement system immediately for prediction
        /// </summary>
        private void ApplyInputToMovementSystem(MovementInput input)
        {
            if (movementSystem == null)
                return;
                
            // Apply movement input
            movementSystem.SetMovementInput(input.moveVector);
            
            // Handle jump input
            if (input.jumpPressed)
            {
                movementSystem.TryExecuteJump();
            }
            
            // Handle dash input - would need to be added to UnifiedMovementSystem
            if (input.dashPressed)
            {
                // movementSystem.TryStartDash(); // Would need this method
            }
        }
        
        /// <summary>
        /// Store input for server reconciliation with sequence tracking
        /// </summary>
        private void StoreInputForReconciliation(MovementInput input)
        {
            pendingInputs.Enqueue(input);
            
            // Store unacknowledged input for reconciliation
            unacknowledgedInputs[input.sequenceNumber] = input;
            
            // Limit pending inputs to prevent memory growth
            while (pendingInputs.Count > maxPredictionFrames)
            {
                var oldInput = pendingInputs.Dequeue();
                unacknowledgedInputs.Remove(oldInput.sequenceNumber);
            }
            
            totalInputsSent++;
        }
        
        /// <summary>
        /// Store state snapshot for rollback capability
        /// </summary>
        private void StoreStateSnapshot(MovementInput input)
        {
            MovementSnapshot snapshot = new MovementSnapshot
            {
                position = playerTransform.position,
                velocity = playerRigidbody.linearVelocity,
                isGrounded = IsPlayerGrounded(),
                frame = currentFrame,
                timestamp = Time.time,
                input = input
            };
            
            stateHistory.Add(snapshot);
            
            // Limit history size
            while (stateHistory.Count > maxPredictionFrames)
            {
                stateHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Check if player is currently grounded using UnifiedMovementSystem
        /// </summary>
        private bool IsPlayerGrounded()
        {
            if (movementSystem != null)
            {
                return movementSystem.IsGrounded;
            }
            
            // Fallback ground check if movement system not available
            return Physics.Raycast(playerTransform.position, Vector3.down, 1.1f);
        }
        
        #endregion
        
        #region Server Authoritative
        
        /// <summary>
        /// Handle server-side authoritative movement validation
        /// </summary>
        private void HandleServerAuthoritative()
        {
            // Server processes inputs in order and validates movement
            // This is where anti-cheat validation would occur
            
            // For now, we'll just update clients with authoritative position
            if (Time.time - lastInputTime > maxInputSampleTime)
            {
                SendAuthoritativeUpdateRpc();
                lastInputTime = Time.time;
            }
        }
        
        /// <summary>
        /// Process movement input on server with validation
        /// </summary>
        /// <param name="input">Movement input from client</param>
        /// <param name="clientId">Client that sent the input</param>
        private void ProcessMovementInput(MovementInput input, ulong clientId)
        {
            // Validate input against anti-cheat rules
            if (!ValidateMovementInput(input, clientId))
            {
                Debug.LogWarning($"[PredictiveMovementSystem] Invalid movement input from client {clientId}");
                return;
            }
            
            // Apply input to authoritative movement system
            if (movementSystem != null)
            {
                ApplyInputToMovementSystem(input);
            }
            
            // Store authoritative state
            serverPosition = playerTransform.position;
            serverVelocity = playerRigidbody.linearVelocity;
            lastServerFrame = input.frame;
        }
        
        /// <summary>
        /// Validate movement input for anti-cheat with comprehensive checks
        /// </summary>
        private bool ValidateMovementInput(MovementInput input, ulong clientId)
        {
            // Validate checksum first
            if (!input.IsValid())
            {
                Debug.LogWarning($"[PredictiveMovementSystem] Invalid checksum from client {clientId}");
                return false;
            }
            
            // Validate input magnitude
            if (input.moveVector.magnitude > 1.1f)
            {
                Debug.LogWarning($"[PredictiveMovementSystem] Excessive input magnitude from client {clientId}: {input.moveVector.magnitude}");
                return false;
            }
            
            // Validate input timing
            float timeDelta = Time.fixedTime - input.timestamp;
            if (timeDelta > 1.0f || timeDelta < -0.1f)
            {
                Debug.LogWarning($"[PredictiveMovementSystem] Invalid input timing from client {clientId}: {timeDelta}s");
                return false;
            }
            
            // Validate sequence number progression
            if (input.sequenceNumber == 0)
            {
                Debug.LogWarning($"[PredictiveMovementSystem] Invalid sequence number from client {clientId}");
                return false;
            }
            
            // Validate frame number
            if (input.frame == 0 || input.frame > currentFrame + 10) // Allow some future frames for lag
            {
                Debug.LogWarning($"[PredictiveMovementSystem] Invalid frame number from client {clientId}: {input.frame}");
                return false;
            }
            
            // Additional validation could include:
            // - Input frequency validation (anti-speed hack)
            // - Input pattern analysis (anti-bot detection)
            // - Maximum inputs per second check
            
            return true;
        }
        
        #endregion
        
        #region Server Reconciliation
        
        /// <summary>
        /// Process server correction and reconcile client state
        /// </summary>
        private void ProcessServerCorrection()
        {
            Vector3 predictedPosition = playerTransform.position;
            float distance = Vector3.Distance(predictedPosition, serverPosition);
            
            if (distance > reconciliationThreshold)
            {
                if (enableRollbackReconciliation)
                {
                    // Significant mismatch - rollback and replay
                    RollbackAndReplay(serverPosition, lastServerFrame);
                }
                else
                {
                    // Simple correction - smooth interpolation
                    StartPositionCorrection(predictedPosition, serverPosition);
                }
                
                Debug.Log($"[PredictiveMovementSystem] Reconciliation triggered - Distance: {distance:F3}m");
            }
            
            hasPendingCorrection = false;
        }
        
        /// <summary>
        /// Rollback to server state and replay inputs
        /// </summary>
        private void RollbackAndReplay(Vector3 authoritativePosition, uint serverFrame)
        {
            // Find the corresponding client prediction
            MovementSnapshot? serverSnapshot = FindSnapshotAtFrame(serverFrame);
            
            if (serverSnapshot == null)
            {
                // Can't find corresponding snapshot, use simple correction
                StartPositionCorrection(playerTransform.position, authoritativePosition);
                return;
            }
            
            // Rollback to server state
            playerTransform.position = authoritativePosition;
            playerRigidbody.linearVelocity = serverVelocity;
            
            // Replay inputs from server frame to current frame
            ReplayInputsFromFrame(serverFrame);
            
            Debug.Log($"[PredictiveMovementSystem] Rollback and replay from frame {serverFrame} to {currentFrame}");
        }
        
        /// <summary>
        /// Find state snapshot at specific frame
        /// </summary>
        private MovementSnapshot? FindSnapshotAtFrame(uint frame)
        {
            for (int i = stateHistory.Count - 1; i >= 0; i--)
            {
                if (stateHistory[i].frame == frame)
                {
                    return stateHistory[i];
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Replay inputs from specified frame to current frame
        /// </summary>
        private void ReplayInputsFromFrame(uint startFrame)
        {
            // Find inputs to replay
            List<MovementInput> inputsToReplay = new List<MovementInput>();
            
            var inputArray = pendingInputs.ToArray();
            foreach (var input in inputArray)
            {
                if (input.frame > startFrame && input.frame <= currentFrame)
                {
                    inputsToReplay.Add(input);
                }
            }
            
            // Sort by frame number
            inputsToReplay.Sort((a, b) => a.frame.CompareTo(b.frame));
            
            // Replay inputs in order
            foreach (var input in inputsToReplay)
            {
                ApplyInputToMovementSystem(input);
                
                // Simulate physics step
                Physics.Simulate(Time.fixedDeltaTime);
            }
            
            Debug.Log($"[PredictiveMovementSystem] Replayed {inputsToReplay.Count} inputs");
        }
        
        /// <summary>
        /// Start smooth position correction
        /// </summary>
        private void StartPositionCorrection(Vector3 currentPos, Vector3 targetPos)
        {
            if (positionCorrectionCoroutine != null)
            {
                StopCoroutine(positionCorrectionCoroutine);
            }
            
            correctionStartPosition = currentPos;
            correctionTargetPosition = targetPos;
            correctionProgress = 0f;
            
            positionCorrectionCoroutine = StartCoroutine(SmoothPositionCorrection());
        }
        
        /// <summary>
        /// Smooth position correction coroutine
        /// </summary>
        private IEnumerator SmoothPositionCorrection()
        {
            while (correctionProgress < 1.0f)
            {
                correctionProgress += Time.deltaTime * correctionSmoothingFactor * 5f; // Adjust speed as needed
                correctionProgress = Mathf.Clamp01(correctionProgress);
                
                Vector3 correctedPosition = Vector3.Lerp(correctionStartPosition, correctionTargetPosition, correctionProgress);
                playerTransform.position = correctedPosition;
                
                yield return null;
            }
            
            positionCorrectionCoroutine = null;
        }
        
        #endregion
        
        #region Network RPCs
        
        /// <summary>
        /// Submit movement input to server
        /// </summary>
        [Rpc(SendTo.Server)]
        private void SubmitMovementInputRpc(MovementInput input)
        {
            if (!IsServer)
                return;
                
            // Process input on server with timestamp validation
            ProcessMovementInput(input, OwnerClientId);
            
            // Send acknowledgment back to client with server state
            SendInputAcknowledgmentRpc(input.sequenceNumber, serverPosition, serverVelocity, currentFrame);
        }
        
        /// <summary>
        /// Send input acknowledgment with server state to client
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void SendInputAcknowledgmentRpc(uint acknowledgedSequence, Vector3 authPosition, Vector3 authVelocity, uint serverFrame)
        {
            if (!IsOwner)
                return;
                
            // Remove acknowledged input from unacknowledged list
            unacknowledgedInputs.Remove(acknowledgedSequence);
            
            // Receive authoritative position from server
            ReconcilePosition(authPosition, authVelocity, serverFrame, acknowledgedSequence);
        }
        
        /// <summary>
        /// Send authoritative position update to clients
        /// </summary>
        [Rpc(SendTo.Owner)]
        private void SendAuthoritativeUpdateRpc()
        {
            if (!IsOwner)
                return;
                
            // Receive authoritative position from server
            ReconcilePosition(serverPosition, serverVelocity, lastServerFrame);
        }
        
        /// <summary>
        /// Receive authoritative position update from server with acknowledgment
        /// </summary>
        private void ReconcilePosition(Vector3 authPosition, Vector3 authVelocity, uint serverFrame, uint acknowledgedSequence = 0)
        {
            serverPosition = authPosition;
            serverVelocity = authVelocity;
            lastServerFrame = serverFrame;
            hasPendingCorrection = true;
            
            // Track reconciliation metrics
            float distance = Vector3.Distance(transform.position, authPosition);
            if (distance > reconciliationThreshold)
            {
                totalReconciliations++;
                totalReconciliationDistance += distance;
                lastReconciliationTime = Time.time;
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[PredictiveMovementSystem] Reconciliation needed - Distance: {distance:F3}m, Sequence: {acknowledgedSequence}");
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Clean up old history to prevent memory growth
        /// </summary>
        private void CleanupOldHistory()
        {
            float cutoffTime = Time.time - (maxPredictionFrames * Time.fixedDeltaTime);
            
            // Clean up state history
            for (int i = stateHistory.Count - 1; i >= 0; i--)
            {
                if (stateHistory[i].timestamp < cutoffTime)
                {
                    stateHistory.RemoveAt(i);
                }
            }
            
            // Clean up pending inputs
            var inputsToKeep = new Queue<MovementInput>();
            while (pendingInputs.Count > 0)
            {
                var input = pendingInputs.Dequeue();
                if (input.timestamp >= cutoffTime)
                {
                    inputsToKeep.Enqueue(input);
                }
            }
            pendingInputs = inputsToKeep;
        }
        
        /// <summary>
        /// Get current prediction statistics for debugging and monitoring
        /// </summary>
        public PredictionStats GetPredictionStats()
        {
            float avgReconciliationDistance = totalReconciliations > 0 ? totalReconciliationDistance / totalReconciliations : 0f;
            float reconciliationRate = totalInputsSent > 0 ? (float)totalReconciliations / totalInputsSent : 0f;
            
            return new PredictionStats
            {
                CurrentFrame = currentFrame,
                PendingInputs = pendingInputs.Count,
                StateHistorySize = stateHistory.Count,
                LastReconciliationDistance = Vector3.Distance(playerTransform.position, serverPosition),
                HasPendingCorrection = hasPendingCorrection,
                TotalInputsSent = totalInputsSent,
                TotalReconciliations = totalReconciliations,
                AverageReconciliationDistance = avgReconciliationDistance,
                ReconciliationRate = reconciliationRate,
                UnacknowledgedInputs = unacknowledgedInputs.Count,
                LastReconciliationTime = lastReconciliationTime
            };
        }
        
        /// <summary>
        /// Reset prediction statistics
        /// </summary>
        public void ResetPredictionStats()
        {
            totalInputsSent = 0;
            totalReconciliations = 0;
            totalReconciliationDistance = 0f;
            lastReconciliationTime = 0f;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Statistics for prediction system debugging and monitoring
    /// </summary>
    [System.Serializable]
    public struct PredictionStats
    {
        public uint CurrentFrame;
        public int PendingInputs;
        public int StateHistorySize;
        public float LastReconciliationDistance;
        public bool HasPendingCorrection;
        public int TotalInputsSent;
        public int TotalReconciliations;
        public float AverageReconciliationDistance;
        public float ReconciliationRate;
        public int UnacknowledgedInputs;
        public float LastReconciliationTime;
    }
}