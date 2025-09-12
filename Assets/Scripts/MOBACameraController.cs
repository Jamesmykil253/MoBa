using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.Networking
{
    /// <summary>
    /// Advanced third-person camera controller for networked 3D MOBA games
    /// Implements server-authoritative camera control with client prediction, lag compensation,
    /// anti-cheat validations, and seamless integration with networked player controllers
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MOBACameraController : NetworkBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private float serverTickRate = 60f;
        [SerializeField] private float reconciliationThreshold = 0.5f;
        [SerializeField] private float maxCameraSpeed = 50f; // Anti-cheat speed limit
        [SerializeField] private float inputBufferSize = 0.1f; // 100ms input buffer

        [Header("Target & References")]
        [Tooltip("The player character to follow")]
        [SerializeField] private Transform target;

        [Tooltip("Reference to the networked player controller")]
        [SerializeField] private NetworkPlayerController networkPlayerController;

        [Header("Positioning")]
        [Tooltip("Vertical offset from the target")]
        [SerializeField] private float heightOffset = 8f;

        [Tooltip("Horizontal distance from the target")]
        [SerializeField] private float distance = 12f;

        [Tooltip("Base distance when player is stationary")]
        [SerializeField] private float baseDistance = 12f;

        [Tooltip("Maximum distance when player is moving fast")]
        [SerializeField] private float maxDistance = 18f;

        [Header("Panning Controls")]
        [Tooltip("Minimum yaw angle for horizontal panning (degrees)")]
        [SerializeField] private float minYawAngle = -45f;

        [Tooltip("Maximum yaw angle for horizontal panning (degrees)")]
        [SerializeField] private float maxYawAngle = 45f;

        [Tooltip("Pan smoothing factor (0-1, higher = smoother)")]
        [SerializeField] private float panSmoothing = 0.1f;

        [Header("Movement-Based Adjustments")]
        [Tooltip("How much player speed affects camera distance")]
        [SerializeField] private float speedDistanceMultiplier = 0.5f;

        [Tooltip("Look-ahead distance based on player velocity")]
        [SerializeField] private float lookAheadDistance = 2f;

        [Tooltip("Look-ahead smoothing factor")]
        [SerializeField] private float lookAheadSmoothing = 0.05f;

        [Header("Smoothing & Interpolation")]
        [Tooltip("Position smoothing factor (0-1, higher = smoother)")]
        [SerializeField] private float positionSmoothing = 0.05f;

        [Tooltip("Rotation smoothing factor (0-1, higher = smoother)")]
        [SerializeField] private float rotationSmoothing = 0.1f;

        [Header("Collision Avoidance")]
        [Tooltip("Enable collision avoidance to prevent clipping")]
        [SerializeField] private bool enableCollisionAvoidance = true;

        [Tooltip("Collision detection radius")]
        [SerializeField] private float collisionRadius = 0.5f;

        [Tooltip("Collision detection layers")]
        [SerializeField] private LayerMask collisionLayers = -1;

        [Tooltip("Minimum distance to maintain from obstacles")]
        [SerializeField] private float minCollisionDistance = 2f;

        [Header("Lag Compensation")]
        [Tooltip("Enable lag compensation for camera positioning")]
        [SerializeField] private bool enableLagCompensation = true;

        [Tooltip("Maximum lag compensation time (seconds)")]
        [SerializeField] private float maxLagCompensationTime = 0.2f;

        [Header("Anti-Cheat")]
        [Tooltip("Enable anti-cheat position validation")]
        [SerializeField] private bool enableAntiCheat = true;

        [Tooltip("Maximum teleport detection threshold")]
        [SerializeField] private float teleportThreshold = 10f;

        [Header("Debug Visualization")]
        [Tooltip("Show debug information in scene view")]
        [SerializeField] private bool showDebugInfo = true;

        [Tooltip("Color for pan limit visualization")]
        [SerializeField] private Color panLimitColor = Color.yellow;

        [Tooltip("Color for camera path visualization")]
        [SerializeField] private Color cameraPathColor = Color.blue;

        // Network state with delta compression
        private NetworkVariable<Vector3> networkCameraPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector3> networkCameraVelocity = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkCameraYaw = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Client prediction with reconciliation
        private Vector3 predictedCameraPosition;
        private Vector3 predictedCameraVelocity;
        private Queue<Vector3> pendingCameraInputs = new Queue<Vector3>();
        private Queue<Vector3> processedCameraInputs = new Queue<Vector3>();
        private float lastServerCameraUpdate;

        // Lag compensation
        private struct HistoricalCameraState
        {
            public Vector3 position;
            public Vector3 velocity;
            public float timestamp;
        }
        private Queue<HistoricalCameraState> cameraHistory = new Queue<HistoricalCameraState>();
        private const float CAMERA_HISTORY_LENGTH = 1f;

        // Private variables
        private Camera cam;
        private Vector3 currentVelocity;
        private float currentYaw;
        private float targetYaw;
        private Vector3 lookAheadPosition;
        private Vector3 lastTargetPosition;
        private float currentDistance;

        // Anti-cheat tracking
        private Vector3 lastValidCameraPosition;
        private float lastCameraInputTime;
        private int cameraViolationCount;

        // DISABLED: Projectile system removed for development
        // private ProjectilePool projectilePool;

        private void Awake()
        {
            cam = GetComponent<Camera>();

            // REMOVED: Projectile system was removed during cleanup

            InitializeCameraController(); // PRESERVED: Auto-targeting functionality for MOBA best practices
        }

        /// <summary>
        /// DISABLED: Projectile system removed for development
        /// Manual ProjectilePool setup for MOBA best practices
        /// </summary>
        // public void SetProjectilePool(ProjectilePool pool)
        // {
        //     projectilePool = pool;
        //     UnityEngine.Debug.Log("[MOBACameraController] ProjectilePool configured manually");
        // }

        private void InitializeCameraController()
        {
            // Ensure AudioListener exists on this camera
            EnsureAudioListener();

            // Find target if not assigned
            if (target == null)
            {
                target = GameObject.FindGameObjectWithTag("Player")?.transform;

                if (target == null)
                {
                    // Try to find NetworkPlayerController
                    var networkPlayers = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
                    if (networkPlayers.Length > 0)
                    {
                        // Find the local player's controller
                        foreach (var player in networkPlayers)
                        {
                            if (player.IsOwner)
                            {
                                target = player.transform;
                                networkPlayerController = player;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                networkPlayerController = target.GetComponent<NetworkPlayerController>();
            }

            // Initialize values
            currentYaw = transform.eulerAngles.y;
            targetYaw = currentYaw;
            currentDistance = distance;
            lastTargetPosition = target?.position ?? Vector3.zero;
            lookAheadPosition = target?.position ?? Vector3.zero;
            lastValidCameraPosition = transform.position;

            // Validate initial positions
            ValidatePosition(ref lastTargetPosition);
            ValidatePosition(ref lookAheadPosition);

            // Debug camera initialization
            if (target != null)
            {
                Debug.Log($"[CAMERA] Initialized - Target: {target.name}, Distance: {currentDistance:F1}m");
            }
            else
            {
                Debug.LogWarning("[CAMERA] No target found - camera will not function");
            }
        }

        /// <summary>
        /// Validates and sanitizes position values to prevent NaN or invalid positions
        /// </summary>
        private void ValidatePosition(ref Vector3 position)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z))
            {
                Debug.LogWarning($"[MOBACameraController] Invalid position detected: {position}. Resetting to origin.");
                position = Vector3.zero;
            }
        }

        /// <summary>
        /// Updates camera position history for lag compensation
        /// </summary>
        private void UpdateCameraHistory()
        {
            if (IsServer && enableLagCompensation)
            {
                cameraHistory.Enqueue(new HistoricalCameraState
                {
                    position = transform.position,
                    velocity = currentVelocity,
                    timestamp = Time.time
                });

                // Remove old history
                while (cameraHistory.Count > 0 && Time.time - cameraHistory.Peek().timestamp > CAMERA_HISTORY_LENGTH)
                {
                    cameraHistory.Dequeue();
                }
            }
        }

        /// <summary>
        /// Applies client-side camera prediction
        /// </summary>
        private void ApplyClientCameraPrediction()
        {
            predictedCameraPosition = transform.position;
            predictedCameraVelocity = currentVelocity;
        }

        /// <summary>
        /// Reconciles camera with server state
        /// </summary>
        private void ReconcileCameraWithServer()
        {
            if (Vector3.Distance(transform.position, networkCameraPosition.Value) > reconciliationThreshold)
            {
                transform.position = networkCameraPosition.Value;
                currentYaw = networkCameraYaw.Value;
                targetYaw = currentYaw;
                Debug.Log($"[MOBACameraController] Camera reconciliation: {transform.position} -> {networkCameraPosition.Value}");
            }
            lastServerCameraUpdate = Time.time;
        }

        /// <summary>
        /// Sends camera input to server for synchronization
        /// </summary>
        private IEnumerator SendCameraInputToServer()
        {
            while (true)
            {
                // Send camera state at configured buffer rate
                if (Time.time - lastCameraInputTime >= inputBufferSize)
                {
                    Vector3 cameraInput = new Vector3(transform.position.x, transform.position.y, currentYaw);
                    SubmitCameraInputServerRpc(cameraInput);
                    lastCameraInputTime = Time.time;
                }

                yield return new WaitForSeconds(1f / serverTickRate);
            }
        }

        [ServerRpc]
        private void SubmitCameraInputServerRpc(Vector3 cameraInput, ServerRpcParams rpcParams = default)
        {
            // Server validation
            if (ValidateCameraInput(cameraInput))
            {
                // Apply validated camera input
                transform.position = new Vector3(cameraInput.x, cameraInput.y, transform.position.z);
                currentYaw = cameraInput.z;
                targetYaw = currentYaw;
            }
        }

        /// <summary>
        /// Validates camera input for anti-cheat
        /// </summary>
        private bool ValidateCameraInput(Vector3 cameraInput)
        {
            if (!enableAntiCheat) return true;

            // Speed validation
            Vector3 inputVelocity = cameraInput - lastValidCameraPosition;
            float inputSpeed = inputVelocity.magnitude / Time.deltaTime;

            if (inputSpeed > maxCameraSpeed)
            {
                cameraViolationCount++;
                Debug.LogWarning($"[MOBACameraController] Camera speed violation: {inputSpeed} > {maxCameraSpeed}");
                return false;
            }

            // Teleport detection
            if (Vector3.Distance(cameraInput, lastValidCameraPosition) > teleportThreshold)
            {
                cameraViolationCount++;
                Debug.LogWarning($"[MOBACameraController] Camera teleport detected: {Vector3.Distance(cameraInput, lastValidCameraPosition)}");
                return false;
            }

            lastValidCameraPosition = cameraInput;
            return true;
        }

        /// <summary>
        /// Network variable change handlers
        /// </summary>
        private void OnNetworkCameraPositionChanged(Vector3 previousValue, Vector3 newValue)
        {
            if (!IsOwner)
            {
                // Smooth interpolation for other clients
                transform.position = Vector3.Lerp(transform.position, newValue, Time.deltaTime * 10f);
            }
        }

        private void OnNetworkCameraYawChanged(float previousValue, float newValue)
        {
            if (!IsOwner)
            {
                currentYaw = Mathf.Lerp(currentYaw, newValue, Time.deltaTime * 10f);
                targetYaw = currentYaw;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                networkCameraPosition.Value = transform.position;
                networkCameraYaw.Value = transform.eulerAngles.y;
            }

            if (IsClient && IsOwner)
            {
                // Initialize client prediction
                predictedCameraPosition = transform.position;
                StartCoroutine(SendCameraInputToServer());
            }

            // Subscribe to network variables
            networkCameraPosition.OnValueChanged += OnNetworkCameraPositionChanged;
            networkCameraYaw.OnValueChanged += OnNetworkCameraYawChanged;
        }

        public override void OnNetworkDespawn()
        {
            networkCameraPosition.OnValueChanged -= OnNetworkCameraPositionChanged;
            networkCameraYaw.OnValueChanged -= OnNetworkCameraYawChanged;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (IsServer)
            {
                ServerUpdate();
            }
            else if (IsClient && IsOwner)
            {
                ClientUpdate();
            }

            UpdatePanInput();
            UpdateLookAhead();
            UpdateDistance();
            UpdateCameraPosition();
            UpdateCameraRotation();

            // Store last position for next frame
            lastTargetPosition = target.position;

            // Update camera history for lag compensation
            UpdateCameraHistory();

            // Debug camera movement (reduced frequency)
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                LogCameraState();
            }
        }

        /// <summary>
        /// Logs detailed camera state for debugging
        /// </summary>
        private void LogCameraState()
        {
            if (target == null) return;

            Vector3 playerVel = Vector3.zero;
            if (networkPlayerController != null)
            {
                Rigidbody playerRb = networkPlayerController.GetComponent<Rigidbody>();
                if (playerRb != null) playerVel = playerRb.linearVelocity;
            }

            Debug.Log($"[CAMERA] Target: {target.position:F1} | Camera: {transform.position:F1} | " +
                     $"Distance: {currentDistance:F1}m | Yaw: {currentYaw:F0}° | " +
                     $"Player Speed: {playerVel.magnitude:F1}m/s | LookAhead: {Vector3.Distance(target.position, lookAheadPosition):F1}m");
        }

        private void ServerUpdate()
        {
            // Server authoritative camera updates
            networkCameraPosition.Value = transform.position;
            networkCameraYaw.Value = currentYaw;
        }

        private void ClientUpdate()
        {
            // Client-side prediction
            ApplyClientCameraPrediction();

            // Reconciliation
            if (Time.time - lastServerCameraUpdate > 1f / serverTickRate)
            {
                ReconcileCameraWithServer();
            }
        }

        /// <summary>
        /// Handle horizontal panning input
        /// </summary>
        private void UpdatePanInput()
        {
            // FIXED: Disable camera pan input to prevent unwanted camera movement
            // Camera should follow player automatically without manual panning
            
            // Don't process mouse input for panning
            // This prevents the "Camera pan input received" messages in the log
            
            // Keep current yaw stable
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, panSmoothing);
        }

        /// <summary>
        /// Update look-ahead position based on player velocity
        /// </summary>
        private void UpdateLookAhead()
        {
            if (target == null) return;

            // Calculate player velocity
            Vector3 playerVelocity = (target.position - lastTargetPosition) / Time.deltaTime;

            // Create look-ahead position based on velocity
            Vector3 velocityDirection = playerVelocity.normalized;
            float velocityMagnitude = playerVelocity.magnitude;

            // Only apply look-ahead if moving significantly
            if (velocityMagnitude > 0.1f)
            {
                Vector3 targetLookAhead = target.position + velocityDirection * lookAheadDistance * Mathf.Min(velocityMagnitude, 10f);
                lookAheadPosition = Vector3.Lerp(lookAheadPosition, targetLookAhead, lookAheadSmoothing);
            }
            else
            {
                // Return to target position when stationary
                lookAheadPosition = Vector3.Lerp(lookAheadPosition, target.position, lookAheadSmoothing);
            }
        }

        /// <summary>
        /// Update camera distance based on player movement
        /// </summary>
        private void UpdateDistance()
        {
            if (target == null || networkPlayerController == null) return;

            // Calculate player speed using network player controller's rigidbody
            Rigidbody playerRb = networkPlayerController.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                Vector3 playerVelocity = playerRb.linearVelocity;
                float playerSpeed = playerVelocity.magnitude;

                // Adjust distance based on speed
                float targetDistance = Mathf.Lerp(baseDistance, maxDistance, playerSpeed * speedDistanceMultiplier);
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, positionSmoothing);
            }
        }

        /// <summary>
        /// Update camera position with collision avoidance and anti-cheat validations
        /// </summary>
        private void UpdateCameraPosition()
        {
            // Safety check for NaN values and validate inputs
            if (target == null)
            {
                Debug.LogWarning("[MOBACameraController] No target assigned, skipping position update");
                return;
            }

            // Validate and sanitize all input values
            ValidatePosition(ref lookAheadPosition);

            if (float.IsNaN(currentYaw) || float.IsNaN(currentDistance) || float.IsNaN(heightOffset))
            {
                Debug.LogWarning($"[MOBACameraController] NaN detected in camera calculations. Resetting to safe values. Yaw: {currentYaw}, Distance: {currentDistance}");
                ResetToDefault();
                return;
            }

            // Additional validation for edge cases
            if (currentDistance <= 0.1f || heightOffset < 0)
            {
                Debug.LogWarning($"[MOBACameraController] Invalid camera parameters. Distance: {currentDistance}, Height: {heightOffset}. Resetting to defaults.");
                ResetToDefault();
                return;
            }

            // Calculate desired camera position
            Vector3 direction = Quaternion.Euler(0, currentYaw, 0) * Vector3.back;
            Vector3 desiredPosition = lookAheadPosition + Vector3.up * heightOffset + direction * currentDistance;

            // Validate desired position
            ValidatePosition(ref desiredPosition);

            // Anti-cheat: Clamp camera speed to prevent excessive movement
            if (enableAntiCheat && IsOwner)
            {
                Vector3 positionDelta = desiredPosition - transform.position;
                float speed = positionDelta.magnitude / Time.deltaTime;

                if (speed > maxCameraSpeed)
                {
                    // Clamp the movement to maximum allowed speed
                    Vector3 clampedDelta = positionDelta.normalized * (maxCameraSpeed * Time.deltaTime);
                    desiredPosition = transform.position + clampedDelta;
                    cameraViolationCount++;
                    Debug.LogWarning($"[MOBACameraController] Camera speed clamped: {speed:F2} > {maxCameraSpeed:F2}");
                }
            }

            // Apply collision avoidance if enabled
            if (enableCollisionAvoidance)
            {
                desiredPosition = ApplyCollisionAvoidance(desiredPosition);
            }

            // Smooth position interpolation with speed clamping
            Vector3 newPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothing);

            // Final anti-cheat validation before applying position
            if (enableAntiCheat && IsOwner && !ValidateCameraPosition(newPosition))
            {
                Debug.LogWarning("[MOBACameraController] Camera position validation failed, maintaining current position");
                return;
            }

            transform.position = newPosition;
        }

        /// <summary>
        /// Validates camera position for anti-cheat purposes
        /// </summary>
        private bool ValidateCameraPosition(Vector3 position)
        {
            // Check for teleportation
            if (Vector3.Distance(position, lastValidCameraPosition) > teleportThreshold)
            {
                cameraViolationCount++;
                return false;
            }

            // Check bounds (prevent camera from going too far from target)
            if (target != null && Vector3.Distance(position, target.position) > maxDistance * 2f)
            {
                cameraViolationCount++;
                return false;
            }

            lastValidCameraPosition = position;
            return true;
        }

        /// <summary>
        /// Update camera rotation to look at target
        /// </summary>
        private void UpdateCameraRotation()
        {
            if (target == null) return;

            // Look at the look-ahead position with some smoothing
            Vector3 lookTarget = lookAheadPosition + Vector3.up * 2f; // Look slightly above the target
            Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);

            // Smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing);
        }

        /// <summary>
        /// Apply collision avoidance to prevent camera clipping with lag compensation
        /// </summary>
        private Vector3 ApplyCollisionAvoidance(Vector3 desiredPosition)
        {
            if (target == null) return desiredPosition;

            // Use lag compensation to get accurate target position
            Vector3 targetPosition = lookAheadPosition;
            if (enableLagCompensation && networkPlayerController != null)
            {
                // Estimate lag and get historical position
                float estimatedLag = EstimateClientLatency();
                var (historicalPos, _) = networkPlayerController.GetPositionAtTime(Time.time - estimatedLag);
                if (historicalPos != Vector3.zero)
                {
                    targetPosition = historicalPos;
                }
            }

            // Cast ray from target to desired camera position
            Vector3 direction = desiredPosition - (targetPosition + Vector3.up * 2f);
            float distance = direction.magnitude;

            if (Physics.SphereCast(targetPosition + Vector3.up * 2f, collisionRadius, direction.normalized, out RaycastHit hit, distance, collisionLayers))
            {
                // Move camera closer to avoid collision
                float safeDistance = Mathf.Max(hit.distance - minCollisionDistance, minCollisionDistance);
                Vector3 safePosition = targetPosition + Vector3.up * heightOffset + direction.normalized * safeDistance;

                // Validate the collision-adjusted position
                ValidatePosition(ref safePosition);
                return safePosition;
            }

            return desiredPosition;
        }

        /// <summary>
        /// Estimates client latency for lag compensation
        /// </summary>
        private float EstimateClientLatency()
        {
            // Simple latency estimation - in production, use actual ping measurements
            float estimatedLatency = 0.1f; // 100ms default

            // Clamp to maximum lag compensation time to prevent excessive compensation
            return Mathf.Min(estimatedLatency, maxLagCompensationTime);
        }

        /// <summary>
        /// Integrates with game start logic and connection approval
        /// </summary>
        public void OnGameStart()
        {
            Debug.Log("[MOBACameraController] Game started - initializing camera systems");

            // Reset camera to default position
            ResetToDefault();

            // Clear any violation history
            cameraViolationCount = 0;
            lastValidCameraPosition = transform.position;

            // DISABLED: Projectile system removed for development
            // Initialize projectile pool integration if available
            // if (projectilePool != null)
            // {
            //     Debug.Log("[MOBACameraController] Projectile pool integration ready");
            // }
        }

        /// <summary>
        /// Handles connection approval for camera synchronization
        /// </summary>
        public void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                Debug.Log($"[MOBACameraController] Client {clientId} connected - synchronizing camera state");

                // Send current camera state to new client
                SyncCameraStateClientRpc(networkCameraPosition.Value, networkCameraYaw.Value, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
                });
            }
        }

        [ClientRpc]
        private void SyncCameraStateClientRpc(Vector3 serverPosition, float serverYaw, ClientRpcParams rpcParams = default)
        {
            if (!IsOwner)
            {
                // Apply server camera state for non-owning clients
                transform.position = serverPosition;
                currentYaw = serverYaw;
                targetYaw = serverYaw;
                Debug.Log($"[MOBACameraController] Camera state synchronized from server");
            }
        }

        /// <summary>
        /// Reset camera to default position behind player
        /// </summary>
        public void ResetToDefault()
        {
            if (target == null) return;

            currentYaw = 0f;
            targetYaw = 0f;
            currentDistance = baseDistance;
            lookAheadPosition = target.position;
        }

        /// <summary>
        /// Set camera target dynamically
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            networkPlayerController = newTarget?.GetComponent<NetworkPlayerController>();
            ResetToDefault();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || target == null) return;

            // Draw pan limit arcs
            Gizmos.color = panLimitColor;
            Vector3 center = target.position + Vector3.up * heightOffset;

            // Draw min and max yaw angles
            Vector3 minDirection = Quaternion.Euler(0, minYawAngle, 0) * Vector3.back * distance;
            Vector3 maxDirection = Quaternion.Euler(0, maxYawAngle, 0) * Vector3.back * distance;

            Gizmos.DrawLine(center, center + minDirection);
            Gizmos.DrawLine(center, center + maxDirection);

            // Draw pan limit arc
            const int segments = 20;
            Vector3 previousPoint = center + minDirection;

            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(minYawAngle, maxYawAngle, (float)i / segments);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.back * distance;
                Vector3 currentPoint = center + direction;

                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            // Draw camera path
            Gizmos.color = cameraPathColor;
            Gizmos.DrawLine(target.position, lookAheadPosition);
            Gizmos.DrawWireSphere(lookAheadPosition, 0.5f);

            // Draw desired camera position
            Vector3 desiredDirection = Quaternion.Euler(0, currentYaw, 0) * Vector3.back;
            Vector3 desiredPosition = lookAheadPosition + Vector3.up * heightOffset + desiredDirection * currentDistance;
            Gizmos.DrawWireCube(desiredPosition, Vector3.one * 0.5f);
        }

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 350, 200));
            GUILayout.Label("MOBA Camera Controller (Networked)", GUILayout.Width(330));

            if (networkPlayerController != null)
            {
                GUILayout.Label($"Target: {target?.name ?? "None"}");
                GUILayout.Label($"Network State: {(IsServer ? "Server" : IsClient ? "Client" : "Disconnected")}");
                GUILayout.Label($"Owner: {(IsOwner ? "Yes" : "No")}");
            }

            GUILayout.Label($"Yaw: {currentYaw:F1}° (Target: {targetYaw:F1}°)");
            GUILayout.Label($"Distance: {currentDistance:F1}");
            GUILayout.Label($"Pan Limits: {minYawAngle}° to {maxYawAngle}°");
            GUILayout.Label($"Collision Avoidance: {(enableCollisionAvoidance ? "ON" : "OFF")}");
            GUILayout.Label($"Lag Compensation: {(enableLagCompensation ? "ON" : "OFF")}");
            GUILayout.Label($"Anti-Cheat: {(enableAntiCheat ? "ON" : "OFF")}");

            if (enableAntiCheat)
            {
                GUILayout.Label($"Violations: {cameraViolationCount}");
            }

            // DISABLED: Projectile system removed for development
            // if (projectilePool != null)
            // {
            //     GUILayout.Label($"Projectile Pool: Connected");
            // }
            GUILayout.Label("Projectile Pool: Disabled for Development");

            GUILayout.EndArea();
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public Vector3 GetCameraPosition() => transform.position;
        public float GetCameraYaw() => currentYaw;
        public bool IsCameraReady() => target != null && networkPlayerController != null;
        public int GetViolationCount() => cameraViolationCount;

        /// <summary>
        /// Resets violation count (admin/debug function)
        /// </summary>
        public void ResetViolations()
        {
            cameraViolationCount = 0;
            Debug.Log("[MOBACameraController] Camera violations reset");
        }

        /// <summary>
        /// Ensures this camera has an AudioListener component
        /// </summary>
        private void EnsureAudioListener()
        {
            if (GetComponent<AudioListener>() == null)
            {
                // Check if there are other AudioListeners in the scene
                AudioListener[] existingListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
                
                if (existingListeners.Length == 0)
                {
                    // No AudioListener exists, add one to this camera
                    gameObject.AddComponent<AudioListener>();
                    Debug.Log("[CAMERA] Added AudioListener to camera");
                }
                else
                {
                    // AudioListener exists elsewhere, don't add another
                    Debug.Log("[CAMERA] AudioListener already exists in scene");
                }
            }
        }
    }
}