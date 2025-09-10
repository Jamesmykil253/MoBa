using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace MOBA.Networking
{
    /// <summary>
    /// Lag compensation system for fair gameplay under network latency
    /// Stores historical state for accurate hit detection and ability targeting
    /// </summary>
    public class LagCompensationManager : MonoBehaviour
    {
        private static LagCompensationManager _instance;
        public static LagCompensationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("LagCompensationManager");
                    _instance = go.AddComponent<LagCompensationManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Lag Compensation Settings")]
        [SerializeField] private float historyLength = 1f; // Keep 1 second of history
        [SerializeField] private int maxHistorySize = 60; // 60 frames at 60fps
        [SerializeField] private float defaultLatency = 0.1f; // 100ms default

        // Historical state storage
        private Dictionary<ulong, Queue<EntityState>> entityHistory = new Dictionary<ulong, Queue<EntityState>>();
        private Dictionary<ulong, float> clientLatencies = new Dictionary<ulong, float>();

        private struct EntityState
        {
            public Vector3 position;
            public Vector3 velocity;
            public Quaternion rotation;
            public float timestamp;
            public ulong entityId;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Store entity state for lag compensation
        /// </summary>
        public void StoreEntityState(ulong entityId, Vector3 position, Vector3 velocity, Quaternion rotation)
        {
            if (!entityHistory.ContainsKey(entityId))
            {
                entityHistory[entityId] = new Queue<EntityState>();
            }

            var state = new EntityState
            {
                position = position,
                velocity = velocity,
                rotation = rotation,
                timestamp = Time.time,
                entityId = entityId
            };

            entityHistory[entityId].Enqueue(state);

            // Remove old states
            while (entityHistory[entityId].Count > maxHistorySize)
            {
                entityHistory[entityId].Dequeue();
            }

            // Clean up old timestamps
            CleanupOldStates(entityId);
        }

        /// <summary>
        /// Get entity state at a specific timestamp for lag compensation
        /// </summary>
        public (Vector3 position, Vector3 velocity, Quaternion rotation) GetEntityStateAtTime(ulong entityId, float timestamp)
        {
            if (!entityHistory.ContainsKey(entityId) || entityHistory[entityId].Count == 0)
            {
                return (Vector3.zero, Vector3.zero, Quaternion.identity);
            }

            var history = entityHistory[entityId];
            EntityState[] states = history.ToArray();

            // Find states around the requested timestamp
            for (int i = states.Length - 1; i >= 0; i--)
            {
                if (states[i].timestamp <= timestamp)
                {
                    if (i + 1 < states.Length)
                    {
                        // Interpolate between states
                        EntityState state1 = states[i];
                        EntityState state2 = states[i + 1];

                        float timeDiff = state2.timestamp - state1.timestamp;
                        if (timeDiff > 0)
                        {
                            float t = (timestamp - state1.timestamp) / timeDiff;
                            t = Mathf.Clamp01(t);

                            Vector3 interpolatedPosition = Vector3.Lerp(state1.position, state2.position, t);
                            Vector3 interpolatedVelocity = Vector3.Lerp(state1.velocity, state2.velocity, t);
                            Quaternion interpolatedRotation = Quaternion.Lerp(state1.rotation, state2.rotation, t);

                            return (interpolatedPosition, interpolatedVelocity, interpolatedRotation);
                        }
                    }

                    // Return exact state
                    return (states[i].position, states[i].velocity, states[i].rotation);
                }
            }

            // Return oldest state if timestamp is too old
            EntityState oldestState = states[0];
            return (oldestState.position, oldestState.velocity, oldestState.rotation);
        }

        /// <summary>
        /// Update client latency for lag compensation
        /// </summary>
        public void UpdateClientLatency(ulong clientId, float latency)
        {
            clientLatencies[clientId] = latency;
        }

        /// <summary>
        /// Get estimated latency for a client
        /// </summary>
        public float GetClientLatency(ulong clientId)
        {
            return clientLatencies.TryGetValue(clientId, out float latency) ? latency : defaultLatency;
        }

        /// <summary>
        /// Perform lag-compensated raycast for ability targeting
        /// </summary>
        public bool LagCompensatedRaycast(ulong shooterId, Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
        {
            float latency = GetClientLatency(shooterId);
            float historicalTime = Time.time - latency;

            // Get all entities and their historical positions
            var entities = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            var tempColliders = new List<(Collider collider, Vector3 originalPosition)>();

            // Move entities to historical positions
            foreach (var entity in entities)
            {
                if (entity.OwnerClientId == shooterId) continue; // Don't lag compensate shooter

                var (historicalPos, _, _) = GetEntityStateAtTime(entity.OwnerClientId, historicalTime);
                if (historicalPos != Vector3.zero)
                {
                    var collider = entity.GetComponent<Collider>();
                    if (collider != null)
                    {
                        tempColliders.Add((collider, collider.transform.position));
                        collider.transform.position = historicalPos;
                    }
                }
            }

            // Perform raycast
            bool result = Physics.Raycast(origin, direction, out hit, maxDistance);

            // Restore original positions
            foreach (var (collider, originalPos) in tempColliders)
            {
                collider.transform.position = originalPos;
            }

            return result;
        }

        /// <summary>
        /// Perform lag-compensated sphere cast for area abilities
        /// </summary>
        public bool LagCompensatedSphereCast(ulong casterId, Vector3 origin, float radius, out RaycastHit hit, float maxDistance = Mathf.Infinity)
        {
            float latency = GetClientLatency(casterId);
            float historicalTime = Time.time - latency;

            // Get all entities and their historical positions
            var entities = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            var tempColliders = new List<(Collider collider, Vector3 originalPosition)>();

            // Move entities to historical positions
            foreach (var entity in entities)
            {
                if (entity.OwnerClientId == casterId) continue; // Don't lag compensate caster

                var (historicalPos, _, _) = GetEntityStateAtTime(entity.OwnerClientId, historicalTime);
                if (historicalPos != Vector3.zero)
                {
                    var collider = entity.GetComponent<Collider>();
                    if (collider != null)
                    {
                        tempColliders.Add((collider, collider.transform.position));
                        collider.transform.position = historicalPos;
                    }
                }
            }

            // Perform sphere cast
            bool result = Physics.SphereCast(origin, radius, Vector3.up, out hit, maxDistance);

            // Restore original positions
            foreach (var (collider, originalPos) in tempColliders)
            {
                collider.transform.position = originalPos;
            }

            return result;
        }

        /// <summary>
        /// Check if a position is valid for ability targeting with lag compensation
        /// </summary>
        public bool IsValidTargetPosition(ulong casterId, Vector3 targetPosition, float maxRange)
        {
            float latency = GetClientLatency(casterId);
            float historicalTime = Time.time - latency;

            // Get caster's historical position
            var caster = FindPlayerById(casterId);
            if (caster == null) return false;

            var (casterPos, _, _) = GetEntityStateAtTime(casterId, historicalTime);
            if (casterPos == Vector3.zero) casterPos = caster.transform.position;

            // Check range
            float distance = Vector3.Distance(casterPos, targetPosition);
            return distance <= maxRange;
        }

        /// <summary>
        /// Get all entities within range at historical time
        /// </summary>
        public List<NetworkPlayerController> GetEntitiesInRangeAtTime(ulong requesterId, Vector3 center, float radius, float historicalTime)
        {
            var entitiesInRange = new List<NetworkPlayerController>();
            var allEntities = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);

            foreach (var entity in allEntities)
            {
                if (entity.OwnerClientId == requesterId) continue; // Exclude requester

                var (historicalPos, _, _) = GetEntityStateAtTime(entity.OwnerClientId, historicalTime);
                if (historicalPos == Vector3.zero) historicalPos = entity.transform.position;

                if (Vector3.Distance(center, historicalPos) <= radius)
                {
                    entitiesInRange.Add(entity);
                }
            }

            return entitiesInRange;
        }

        private void CleanupOldStates(ulong entityId)
        {
            if (!entityHistory.ContainsKey(entityId)) return;

            var history = entityHistory[entityId];
            float cutoffTime = Time.time - historyLength;

            while (history.Count > 0 && history.Peek().timestamp < cutoffTime)
            {
                history.Dequeue();
            }
        }

        private NetworkPlayerController FindPlayerById(ulong clientId)
        {
            var players = Object.FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerClientId == clientId)
                {
                    return player;
                }
            }
            return null;
        }

        /// <summary>
        /// Clear all historical data (useful for scene changes)
        /// </summary>
        public void ClearHistory()
        {
            entityHistory.Clear();
            clientLatencies.Clear();
            UnityEngine.Debug.Log("[LagCompensationManager] Cleared all historical data");
        }

        /// <summary>
        /// Get statistics for debugging
        /// </summary>
        public Dictionary<string, int> GetHistoryStats()
        {
            var stats = new Dictionary<string, int>();
            foreach (var kvp in entityHistory)
            {
                stats[kvp.Key.ToString()] = kvp.Value.Count;
            }
            return stats;
        }

        private void OnDestroy()
        {
            ClearHistory();
        }
    }
}