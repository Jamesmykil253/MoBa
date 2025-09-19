using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace MOBA.Networking
{
    /// <summary>
    /// Lag compensation manager for server-side hit validation with historical state rewind
    /// Reference: Multiplayer Game Programming Chapter 6, Valve's Source Engine networking
    /// Provides fair hit registration by compensating for network latency
    /// </summary>
    public class LagCompensationManager : NetworkBehaviour
    {
        #region Configuration
        
        [Header("Lag Compensation Settings")]
        [SerializeField, Tooltip("Maximum time in milliseconds to compensate for lag")]
        private float maxCompensationTimeMs = 200f;
        
        [SerializeField, Tooltip("Interval between state snapshots in milliseconds")]
        private float snapshotIntervalMs = 16.67f; // 60fps snapshots
        
        [SerializeField, Tooltip("Maximum number of snapshots to keep in history")]
        private int maxSnapshotHistory = 256;
        
        [SerializeField, Tooltip("Enable lag compensation system")]
        private bool enableLagCompensation = true;
        
        [SerializeField, Tooltip("Enable detailed debug logging")]
        private bool enableDebugLogging = false;
        
        [Header("Hit Validation")]
        [SerializeField, Tooltip("Maximum distance for valid hit detection")]
        private float maxHitDistance = 50f;
        
        [SerializeField, Tooltip("Tolerance for hit position validation")]
        private float hitPositionTolerance = 1.0f;
        
        [SerializeField, Tooltip("Enable hit validation against historical positions")]
        private bool enableHitValidation = true;
        
        #endregion
        
        #region State Data
        
        // Player state histories - key is ClientId
        private Dictionary<ulong, Queue<PlayerSnapshot>> playerHistories = new Dictionary<ulong, Queue<PlayerSnapshot>>();
        
        // RTT tracking for lag compensation calculations
        private Dictionary<ulong, float> playerRTTs = new Dictionary<ulong, float>();
        
        // Last snapshot times to control snapshot frequency
        private Dictionary<ulong, float> lastSnapshotTimes = new Dictionary<ulong, float>();
        
        // Active compensation requests for debugging
        private List<CompensationRequest> activeRequests = new List<CompensationRequest>();
        
        #endregion
        
        #region Structures
        
        /// <summary>
        /// Player state snapshot for lag compensation
        /// </summary>
        [System.Serializable]
        public struct PlayerSnapshot
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 velocity;
            public float timestamp;
            public bool isValid;
            public bool isGrounded;
            public float health;
            public uint frameNumber;
            
            public PlayerSnapshot(Vector3 pos, Quaternion rot, Vector3 vel, float time, bool valid = true, bool grounded = false, float hp = 100f, uint frame = 0)
            {
                position = pos;
                rotation = rot;
                velocity = vel;
                timestamp = time;
                isValid = valid;
                isGrounded = grounded;
                health = hp;
                frameNumber = frame;
            }
        }
        
        /// <summary>
        /// Hit validation request data
        /// </summary>
        [System.Serializable]
        public struct HitRequest
        {
            public ulong shooterClientId;
            public ulong targetClientId;
            public Vector3 hitPosition;
            public Vector3 shootDirection;
            public float clientTimestamp;
            public float serverTimestamp;
            public float damage;
            public string weaponId;
        }
        
        /// <summary>
        /// Compensation request for debugging and analytics
        /// </summary>
        [System.Serializable]
        public struct CompensationRequest
        {
            public ulong shooterClientId;
            public ulong targetClientId;
            public float lagCompensationTime;
            public Vector3 originalTargetPosition;
            public Vector3 compensatedTargetPosition;
            public bool hitValidated;
            public float processingTime;
            public float timestamp;
        }
        
        /// <summary>
        /// Lag compensation statistics
        /// </summary>
        [System.Serializable]
        public struct LagCompensationStats
        {
            public int totalPlayers;
            public int totalSnapshots;
            public float averageRTT;
            public int validatedHitsLastSecond;
            public int rejectedHitsLastSecond;
            public float averageCompensationTime;
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Only run on server
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            
            // Subscribe to network events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
            
            Debug.Log("[LagCompensationManager] Initialized on server");
        }
        
        private void Update()
        {
            if (!IsServer || !enableLagCompensation)
                return;
                
            // Record player states at regular intervals
            RecordPlayerStates();
            
            // Clean up old snapshots
            CleanupOldSnapshots();
            
            // Clean up old compensation requests
            CleanupOldRequests();
        }
        
        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }
        
        #endregion
        
        #region Network Events
        
        private void OnClientConnected(ulong clientId)
        {
            // Initialize history for new player
            if (!playerHistories.ContainsKey(clientId))
            {
                playerHistories[clientId] = new Queue<PlayerSnapshot>();
                playerRTTs[clientId] = 0f;
                lastSnapshotTimes[clientId] = 0f;
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[LagCompensationManager] Initialized tracking for client {clientId}");
                }
            }
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            // Clean up player data
            if (playerHistories.ContainsKey(clientId))
            {
                playerHistories.Remove(clientId);
                playerRTTs.Remove(clientId);
                lastSnapshotTimes.Remove(clientId);
                
                if (enableDebugLogging)
                {
                    Debug.Log($"[LagCompensationManager] Removed tracking for client {clientId}");
                }
            }
        }
        
        #endregion
        
        #region State Recording
        
        /// <summary>
        /// Record current state of all players
        /// </summary>
        private void RecordPlayerStates()
        {
            float currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                ulong clientId = client.Key;
                var clientObject = client.Value;
                
                if (clientObject.PlayerObject == null)
                    continue;
                    
                // Check if enough time has passed since last snapshot
                if (currentTime - lastSnapshotTimes.GetValueOrDefault(clientId, 0f) < snapshotIntervalMs / 1000f)
                    continue;
                
                RecordPlayerState(clientId, clientObject.PlayerObject, currentTime);
                lastSnapshotTimes[clientId] = currentTime;
            }
        }
        
        /// <summary>
        /// Record state for specific player
        /// </summary>
        /// <param name="clientId">Client ID to record</param>
        /// <param name="playerObject">Player network object</param>
        /// <param name="timestamp">Current server timestamp</param>
        public void RecordPlayerState(ulong clientId, NetworkObject playerObject, float timestamp)
        {
            if (playerObject == null || !playerObject.IsSpawned)
                return;
                
            var transform = playerObject.transform;
            var rigidbody = playerObject.GetComponent<Rigidbody>();
            
            Vector3 velocity = rigidbody != null ? rigidbody.linearVelocity : Vector3.zero;
            bool isGrounded = CheckPlayerGrounded(playerObject);
            float health = GetPlayerHealth(playerObject);
            
            var snapshot = new PlayerSnapshot(
                transform.position,
                transform.rotation,
                velocity,
                timestamp,
                true,
                isGrounded,
                health,
                GetCurrentFrameNumber()
            );
            
            if (!playerHistories.ContainsKey(clientId))
            {
                playerHistories[clientId] = new Queue<PlayerSnapshot>();
            }
            
            playerHistories[clientId].Enqueue(snapshot);
            
            // Limit history size
            while (playerHistories[clientId].Count > maxSnapshotHistory)
            {
                playerHistories[clientId].Dequeue();
            }
        }
        
        /// <summary>
        /// Check if player is grounded (helper method)
        /// </summary>
        private bool CheckPlayerGrounded(NetworkObject playerObject)
        {
            // Integrate with unified movement system
            var movementSystem = playerObject.GetComponent<UnifiedMovementSystem>();
            if (movementSystem != null)
            {
                return movementSystem.IsGrounded;
            }
            
            return false; // Default fallback
        }
        
        /// <summary>
        /// Get player health (helper method)
        /// </summary>
        private float GetPlayerHealth(NetworkObject playerObject)
        {
            // Integrate with existing IDamageable interface
            var damageable = playerObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                return damageable.GetHealth();
            }
            
            return 100f; // Default fallback
        }
        
        /// <summary>
        /// Get current frame number for tracking
        /// </summary>
        private uint GetCurrentFrameNumber()
        {
            // This would integrate with your frame counting system
            return (uint)(Time.fixedTime / Time.fixedDeltaTime);
        }
        
        #endregion
        
        #region Lag Compensation
        
        /// <summary>
        /// Validate hit with lag compensation
        /// </summary>
        /// <param name="hitRequest">Hit validation request</param>
        /// <returns>True if hit is valid after lag compensation</returns>
        public bool ValidateHitWithLagCompensation(HitRequest hitRequest)
        {
            if (!enableLagCompensation || !enableHitValidation)
                return ValidateHitDirect(hitRequest);
                
            float startTime = Time.realtimeSinceStartup;
            
            // Calculate lag compensation time
            float shooterRTT = GetPlayerRTT(hitRequest.shooterClientId);
            float compensationTime = hitRequest.serverTimestamp - hitRequest.clientTimestamp - (shooterRTT * 0.5f);
            
            // Clamp compensation time to maximum allowed
            compensationTime = Mathf.Clamp(compensationTime, 0f, maxCompensationTimeMs / 1000f);
            
            // Get target's historical position at the time of shooting
            Vector3 compensatedPosition = GetPlayerPositionAtTime(hitRequest.targetClientId, hitRequest.clientTimestamp - compensationTime);
            
            // Validate hit against compensated position
            bool hitValid = ValidateHitAtPosition(hitRequest, compensatedPosition);
            
            // Record compensation request for debugging
            var request = new CompensationRequest
            {
                shooterClientId = hitRequest.shooterClientId,
                targetClientId = hitRequest.targetClientId,
                lagCompensationTime = compensationTime,
                originalTargetPosition = GetCurrentPlayerPosition(hitRequest.targetClientId),
                compensatedTargetPosition = compensatedPosition,
                hitValidated = hitValid,
                processingTime = Time.realtimeSinceStartup - startTime,
                timestamp = hitRequest.serverTimestamp
            };
            
            activeRequests.Add(request);
            
            if (enableDebugLogging)
            {
                Debug.Log($"[LagCompensationManager] Hit validation: Shooter={hitRequest.shooterClientId}, Target={hitRequest.targetClientId}, " +
                         $"Compensation={compensationTime:F3}s, Valid={hitValid}, ProcessTime={request.processingTime:F3}ms");
            }
            
            return hitValid;
        }
        
        /// <summary>
        /// Get player position at specific time using historical data
        /// </summary>
        /// <param name="clientId">Client ID to query</param>
        /// <param name="timestamp">Target timestamp</param>
        /// <returns>Player position at the specified time</returns>
        private Vector3 GetPlayerPositionAtTime(ulong clientId, float timestamp)
        {
            if (!playerHistories.ContainsKey(clientId))
                return GetCurrentPlayerPosition(clientId);
                
            var history = playerHistories[clientId];
            if (history.Count == 0)
                return GetCurrentPlayerPosition(clientId);
                
            var snapshots = history.ToArray();
            
            // Find the two snapshots that bracket the target time
            PlayerSnapshot? beforeSnapshot = null;
            PlayerSnapshot? afterSnapshot = null;
            
            for (int i = 0; i < snapshots.Length; i++)
            {
                if (snapshots[i].timestamp <= timestamp)
                {
                    beforeSnapshot = snapshots[i];
                }
                else
                {
                    afterSnapshot = snapshots[i];
                    break;
                }
            }
            
            // If we have both snapshots, interpolate between them
            if (beforeSnapshot.HasValue && afterSnapshot.HasValue)
            {
                float timeDelta = afterSnapshot.Value.timestamp - beforeSnapshot.Value.timestamp;
                if (timeDelta > 0f)
                {
                    float t = (timestamp - beforeSnapshot.Value.timestamp) / timeDelta;
                    return Vector3.Lerp(beforeSnapshot.Value.position, afterSnapshot.Value.position, t);
                }
            }
            
            // If we only have one snapshot, use it
            if (beforeSnapshot.HasValue)
                return beforeSnapshot.Value.position;
            if (afterSnapshot.HasValue)
                return afterSnapshot.Value.position;
            
            // Fallback to current position
            return GetCurrentPlayerPosition(clientId);
        }
        
        /// <summary>
        /// Get current player position
        /// </summary>
        private Vector3 GetCurrentPlayerPosition(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null)
                {
                    return client.PlayerObject.transform.position;
                }
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Get shooter position for hit validation
        /// </summary>
        private Vector3 GetShooterPosition(ulong shooterClientId)
        {
            return GetCurrentPlayerPosition(shooterClientId);
        }
        
        /// <summary>
        /// Validate hit at specific position
        /// </summary>
        private bool ValidateHitAtPosition(HitRequest hitRequest, Vector3 targetPosition)
        {
            // Check distance from hit position to target position
            float distance = Vector3.Distance(hitRequest.hitPosition, targetPosition);
            
            if (distance > hitPositionTolerance)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"[LagCompensationManager] Hit rejected: distance {distance:F3} > tolerance {hitPositionTolerance:F3}");
                }
                return false;
            }
            
            // Check maximum hit distance (for range validation)
            float shooterDistance = Vector3.Distance(GetShooterPosition(hitRequest.shooterClientId), targetPosition);
            if (shooterDistance > maxHitDistance)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"[LagCompensationManager] Hit rejected: shooter distance {shooterDistance:F3} > max distance {maxHitDistance:F3}");
                }
                return false;
            }
            
            // Additional hit validation could include:
            // - Line of sight checks
            // - Weapon range validation
            // - Angle of attack validation
            // - Penetration calculations
            
            return true;
        }
        
        /// <summary>
        /// Direct hit validation without lag compensation
        /// </summary>
        private bool ValidateHitDirect(HitRequest hitRequest)
        {
            Vector3 currentTargetPosition = GetCurrentPlayerPosition(hitRequest.targetClientId);
            return ValidateHitAtPosition(hitRequest, currentTargetPosition);
        }
        
        #endregion
        
        #region RTT Management
        
        /// <summary>
        /// Update player RTT for lag compensation calculations
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="rtt">Round-trip time in seconds</param>
        public void UpdatePlayerRTT(ulong clientId, float rtt)
        {
            playerRTTs[clientId] = rtt;
            
            if (enableDebugLogging)
            {
                Debug.Log($"[LagCompensationManager] Updated RTT for client {clientId}: {rtt:F3}s");
            }
        }
        
        /// <summary>
        /// Get player RTT
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <returns>Round-trip time in seconds</returns>
        public float GetPlayerRTT(ulong clientId)
        {
            return playerRTTs.GetValueOrDefault(clientId, 0f);
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Clean up old snapshots beyond maximum compensation time
        /// </summary>
        private void CleanupOldSnapshots()
        {
            float currentTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            float cutoffTime = currentTime - (maxCompensationTimeMs / 1000f);
            
            foreach (var kvp in playerHistories.ToArray())
            {
                var history = kvp.Value;
                
                while (history.Count > 0 && history.Peek().timestamp < cutoffTime)
                {
                    history.Dequeue();
                }
            }
        }
        
        /// <summary>
        /// Clean up old compensation requests
        /// </summary>
        private void CleanupOldRequests()
        {
            float currentTime = Time.time;
            float cutoffTime = currentTime - 1f; // Keep requests for 1 second
            
            activeRequests.RemoveAll(request => request.timestamp < cutoffTime);
        }
        
        #endregion
        
        #region Combat Integration
        
        /// <summary>
        /// Validate attack hit with lag compensation for PlayerAttackSystem
        /// </summary>
        /// <param name="shooterClientId">Attacker client ID</param>
        /// <param name="targetNetworkId">Target network object ID</param>
        /// <param name="damage">Damage amount</param>
        /// <param name="attackPosition">Position where attack originated</param>
        /// <param name="clientTimestamp">Client timestamp of attack</param>
        /// <returns>True if hit is valid</returns>
        public bool ValidateAttackHit(ulong shooterClientId, ulong targetNetworkId, float damage, Vector3 attackPosition, float clientTimestamp)
        {
            if (!enableLagCompensation)
                return true; // Fall back to direct validation
                
            // Get target client ID from network object
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var targetNetworkObject))
                return false;
                
            ulong targetClientId = targetNetworkObject.OwnerClientId;
            
            var hitRequest = new HitRequest
            {
                shooterClientId = shooterClientId,
                targetClientId = targetClientId,
                hitPosition = targetNetworkObject.transform.position,
                shootDirection = (targetNetworkObject.transform.position - attackPosition).normalized,
                clientTimestamp = clientTimestamp,
                serverTimestamp = NetworkManager.Singleton.ServerTime.TimeAsFloat,
                damage = damage,
                weaponId = "melee_attack"
            };
            
            return ValidateHitWithLagCompensation(hitRequest);
        }
        
        /// <summary>
        /// Validate ability hit with lag compensation for AbilityExecutionManager
        /// </summary>
        /// <param name="shooterClientId">Caster client ID</param>
        /// <param name="targetNetworkId">Target network object ID</param>
        /// <param name="abilityDamage">Ability damage</param>
        /// <param name="castPosition">Position where ability was cast</param>
        /// <param name="clientTimestamp">Client timestamp of cast</param>
        /// <param name="abilityId">Ability identifier</param>
        /// <returns>True if hit is valid</returns>
        public bool ValidateAbilityHit(ulong shooterClientId, ulong targetNetworkId, float abilityDamage, Vector3 castPosition, float clientTimestamp, string abilityId)
        {
            if (!enableLagCompensation)
                return true; // Fall back to direct validation
                
            // Get target client ID from network object
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var targetNetworkObject))
                return false;
                
            ulong targetClientId = targetNetworkObject.OwnerClientId;
            
            var hitRequest = new HitRequest
            {
                shooterClientId = shooterClientId,
                targetClientId = targetClientId,
                hitPosition = targetNetworkObject.transform.position,
                shootDirection = (targetNetworkObject.transform.position - castPosition).normalized,
                clientTimestamp = clientTimestamp,
                serverTimestamp = NetworkManager.Singleton.ServerTime.TimeAsFloat,
                damage = abilityDamage,
                weaponId = abilityId
            };
            
            return ValidateHitWithLagCompensation(hitRequest);
        }
        
        /// <summary>
        /// Create a hit request for damage validation
        /// </summary>
        /// <param name="shooterClientId">Shooter client ID</param>
        /// <param name="targetClientId">Target client ID</param>
        /// <param name="damage">Damage amount</param>
        /// <param name="hitPosition">Hit position</param>
        /// <param name="weaponId">Weapon/ability identifier</param>
        /// <returns>Hit request structure</returns>
        public HitRequest CreateHitRequest(ulong shooterClientId, ulong targetClientId, float damage, Vector3 hitPosition, string weaponId = "unknown")
        {
            return new HitRequest
            {
                shooterClientId = shooterClientId,
                targetClientId = targetClientId,
                hitPosition = hitPosition,
                shootDirection = Vector3.forward, // Will be calculated if needed
                clientTimestamp = NetworkManager.Singleton.ServerTime.TimeAsFloat,
                serverTimestamp = NetworkManager.Singleton.ServerTime.TimeAsFloat,
                damage = damage,
                weaponId = weaponId
            };
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get lag compensation statistics
        /// </summary>
        /// <returns>Current lag compensation statistics</returns>
        public LagCompensationStats GetStats()
        {
            int totalSnapshots = playerHistories.Values.Sum(history => history.Count);
            float averageRTT = playerRTTs.Values.Count > 0 ? playerRTTs.Values.Average() : 0f;
            
            float recentRequestTime = Time.time - 1f;
            var recentRequests = activeRequests.Where(r => r.timestamp >= recentRequestTime).ToArray();
            int validatedHits = recentRequests.Count(r => r.hitValidated);
            int rejectedHits = recentRequests.Count(r => !r.hitValidated);
            float averageCompensation = recentRequests.Length > 0 ? recentRequests.Average(r => r.lagCompensationTime) : 0f;
            
            return new LagCompensationStats
            {
                totalPlayers = playerHistories.Count,
                totalSnapshots = totalSnapshots,
                averageRTT = averageRTT,
                validatedHitsLastSecond = validatedHits,
                rejectedHitsLastSecond = rejectedHits,
                averageCompensationTime = averageCompensation
            };
        }
        
        /// <summary>
        /// Enable or disable lag compensation system
        /// </summary>
        /// <param name="enabled">True to enable, false to disable</param>
        public void SetLagCompensationEnabled(bool enabled)
        {
            enableLagCompensation = enabled;
            Debug.Log($"[LagCompensationManager] Lag compensation {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Get recent compensation requests for debugging
        /// </summary>
        /// <returns>Array of recent compensation requests</returns>
        public CompensationRequest[] GetRecentRequests()
        {
            float recentTime = Time.time - 5f; // Last 5 seconds
            return activeRequests.Where(r => r.timestamp >= recentTime).ToArray();
        }
        
        /// <summary>
        /// Clear all historical data (useful for testing)
        /// </summary>
        public void ClearHistory()
        {
            foreach (var history in playerHistories.Values)
            {
                history.Clear();
            }
            
            activeRequests.Clear();
            
            Debug.Log("[LagCompensationManager] Cleared all historical data");
        }
        
        #endregion
    }
}