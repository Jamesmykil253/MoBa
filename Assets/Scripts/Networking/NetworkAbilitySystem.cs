using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace MOBA.Networking
{
    /// <summary>
    /// Network ability system with server validation, rate limiting, and lag compensation
    /// Implements comprehensive anti-cheat measures and performance optimization
    /// </summary>
    public class NetworkAbilitySystem : NetworkBehaviour
    {
        [Header("Ability Settings")]
        [SerializeField] private float globalCooldown = 0.1f;
        [SerializeField] private float ability1Cooldown = 8f;
        [SerializeField] private float ability2Cooldown = 12f;
        [SerializeField] private float ultimateCooldown = 60f;

        [Header("Rate Limiting")]
        [SerializeField] private int maxCastsPerWindow = 10;
        [SerializeField] private float rateLimitWindow = 1f;

        [Header("Anti-Cheat")]
        [SerializeField] private float maxDistanceFromPlayer = 50f;
        [SerializeField] private float minCastInterval = 0.1f;
        [SerializeField] private int maxConsecutiveCasts = 5;

        [Header("Lag Compensation")]
        [SerializeField] private float maxRollbackTime = 0.5f;
        [SerializeField] private bool enableLagCompensation = true;

        [Header("Performance")]
        [SerializeField] private int maxQueuedCasts = 5;
        [SerializeField] private float processInterval = 0.05f;

        // State tracking
        private Dictionary<ulong, ClientRateLimit> clientRateLimits = new Dictionary<ulong, ClientRateLimit>();
        private Dictionary<AbilityType, float> lastCastTimes = new Dictionary<AbilityType, float>();
        private Dictionary<AbilityType, float> cooldownEndTimes = new Dictionary<AbilityType, float>();
        private Queue<PendingCast> pendingCasts = new Queue<PendingCast>();
        
        // Performance tracking
        private float lastProcessTime;
        private float frameStartTime;

        // Events
        public static event Action<ulong, AbilityType, Vector3> OnAbilityCast;

        #region Network Events
        
        /// <summary>
        /// Client requests to cast an ability
        /// </summary>
        [Rpc(SendTo.Server)]
        public void RequestAbilityCastRpc(AbilityType abilityType, Vector3 targetPosition, float clientTimestamp, RpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            if (!ValidateAbilityCast(clientId, abilityType, targetPosition, clientTimestamp))
                return;

            var cast = new PendingCast
            {
                clientId = clientId,
                abilityType = abilityType,
                targetPosition = targetPosition,
                timestamp = clientTimestamp,
                serverReceiveTime = Time.time
            };

            if (pendingCasts.Count < maxQueuedCasts)
            {
                pendingCasts.Enqueue(cast);
            }
            else
            {
                Debug.LogWarning($"[NetworkAbilitySystem] Cast queue full for client {clientId}");
            }
        }

        /// <summary>
        /// Server confirms ability cast to all clients
        /// </summary>
        [Rpc(SendTo.Everyone)]
        public void ConfirmAbilityCastRpc(ulong casterId, AbilityType abilityType, Vector3 targetPosition, float serverTimestamp)
        {
            OnAbilityCast?.Invoke(casterId, abilityType, targetPosition);
        }

        #endregion

        #region Unity Events

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeAbilitySystem();
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                ProcessPendingCasts();
                CleanupOldRateLimitData();
            }
            
            UpdatePerformanceTracking();
        }

        #endregion

        #region Initialization

        private void InitializeAbilitySystem()
        {
            foreach (AbilityType abilityType in System.Enum.GetValues(typeof(AbilityType)))
            {
                lastCastTimes[abilityType] = 0f;
                cooldownEndTimes[abilityType] = 0f;
            }
        }

        #endregion

        #region Validation

        private bool ValidateAbilityCast(ulong clientId, AbilityType abilityType, Vector3 targetPosition, float clientTimestamp)
        {
            // Rate limiting
            if (!IsWithinRateLimit(clientId))
            {
                Debug.LogWarning($"[NetworkAbilitySystem] Rate limit exceeded for client {clientId}");
                return false;
            }

            // Cooldown check
            if (cooldownEndTimes.ContainsKey(abilityType) && Time.time < cooldownEndTimes[abilityType])
            {
                Debug.LogWarning($"[NetworkAbilitySystem] Ability {abilityType} still on cooldown");
                return false;
            }

            // Distance validation (basic anti-cheat)
            var playerObj = GetPlayerObject(clientId);
            if (playerObj != null)
            {
                float distance = Vector3.Distance(playerObj.transform.position, targetPosition);
                if (distance > maxDistanceFromPlayer)
                {
                    Debug.LogWarning($"[NetworkAbilitySystem] Anti-cheat: Target too far from player: {distance} > {maxDistanceFromPlayer}");
                    return false;
                }
            }

            // Anti-spam protection
            if (!ValidateSpamProtection(clientId))
            {
                return false;
            }

            return true;
        }

        private bool IsWithinRateLimit(ulong clientId)
        {
            if (!clientRateLimits.ContainsKey(clientId))
            {
                clientRateLimits[clientId] = new ClientRateLimit();
            }

            var rateLimit = clientRateLimits[clientId];
            float currentTime = Time.time;

            // Reset window if needed
            if (currentTime - rateLimit.windowStart >= rateLimitWindow)
            {
                rateLimit.windowStart = currentTime;
                rateLimit.castCount = 0;
            }

            // Check if within limit
            if (rateLimit.castCount >= maxCastsPerWindow)
            {
                return false;
            }

            rateLimit.castCount++;
            return true;
        }

        private bool ValidateSpamProtection(ulong clientId)
        {
            if (!clientRateLimits.ContainsKey(clientId))
                return true;

            var rateLimit = clientRateLimits[clientId];
            float currentTime = Time.time;

            // Check for rapid consecutive casts
            if (currentTime - rateLimit.lastCastTime < minCastInterval)
            {
                rateLimit.rapidCastCount++;
                if (rateLimit.rapidCastCount > maxConsecutiveCasts)
                {
                    Debug.LogWarning($"[NetworkAbilitySystem] Spam protection triggered for client {clientId}");
                    return false;
                }
            }
            else
            {
                rateLimit.rapidCastCount = 0;
            }

            rateLimit.lastCastTime = currentTime;
            return true;
        }

        #endregion

        #region Cast Processing

        private void ProcessPendingCasts()
        {
            if (Time.time - lastProcessTime < processInterval)
                return;

            int processed = 0;
            int maxProcessPerFrame = 3; // Limit processing to maintain performance

            while (pendingCasts.Count > 0 && processed < maxProcessPerFrame)
            {
                var cast = pendingCasts.Dequeue();
                ProcessAbilityCast(cast);
                processed++;
            }

            lastProcessTime = Time.time;
        }

        private void ProcessAbilityCast(PendingCast cast)
        {
            // Apply lag compensation if enabled
            Vector3 compensatedPosition = cast.targetPosition;
            if (enableLagCompensation)
            {
                float lag = cast.serverReceiveTime - cast.timestamp;
                compensatedPosition = ApplyLagCompensation(cast.targetPosition, lag);
            }

            // Execute the ability
            ExecuteAbility(cast.abilityType, compensatedPosition);

            // Update cooldowns
            lastCastTimes[cast.abilityType] = Time.time;
            float cooldown = GetAbilityCooldown(cast.abilityType);
            cooldownEndTimes[cast.abilityType] = Time.time + cooldown;

            // Confirm to all clients
            ConfirmAbilityCastRpc(cast.clientId, cast.abilityType, compensatedPosition, Time.time);
        }

        private Vector3 ApplyLagCompensation(Vector3 targetPosition, float lag)
        {
            // Simple lag compensation - clamp to prevent abuse
            lag = Mathf.Clamp(lag, 0f, maxRollbackTime);
            
            // For basic implementation, just return the original position
            // In a full implementation, this would rollback world state
            return targetPosition;
        }

        private void ExecuteAbility(AbilityType abilityType, Vector3 targetPosition)
        {
            AbilityData ability = GetAbilityData(abilityType);
            
            // Apply instant effects
            ApplyInstantAbilityEffect(ability, targetPosition);
            
            Debug.Log($"[NetworkAbilitySystem] Executed {abilityType} at {targetPosition}");
        }

        private void ApplyInstantAbilityEffect(AbilityData ability, Vector3 targetPosition)
        {
            // Basic area damage implementation
            Collider[] hits = Physics.OverlapSphere(targetPosition, ability.range);
            
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(ability.damage);
                }
            }
        }

        #endregion

        #region Ability Data

        private float GetAbilityCooldown(AbilityType abilityType)
        {
            return abilityType switch
            {
                AbilityType.PrimaryAttack => globalCooldown,
                AbilityType.Ability1 => ability1Cooldown,
                AbilityType.Ability2 => ability2Cooldown,
                AbilityType.Ultimate => ultimateCooldown,
                _ => globalCooldown
            };
        }

        private AbilityData GetAbilityData(AbilityType abilityType)
        {
            return abilityType switch
            {
                AbilityType.PrimaryAttack => new AbilityData { name = "Primary Attack", damage = 25f, range = 5f, speed = 10f, lifetime = 2f },
                AbilityType.Ability1 => new AbilityData { name = "Fireball", damage = 50f, range = 8f, speed = 15f, lifetime = 3f },
                AbilityType.Ability2 => new AbilityData { name = "Lightning", damage = 75f, range = 12f, speed = 20f, lifetime = 1f },
                AbilityType.Ultimate => new AbilityData { name = "Meteor", damage = 150f, range = 15f, speed = 5f, lifetime = 5f },
                _ => new AbilityData { name = "Unknown", damage = 0f, range = 0f, speed = 0f, lifetime = 0f }
            };
        }

        #endregion

        #region Utility

        private NetworkObject GetPlayerObject(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                return client.PlayerObject;
            }
            return null;
        }

        private void CleanupOldRateLimitData()
        {
            float currentTime = Time.time;
            var toRemove = new List<ulong>();
            
            foreach (var kvp in clientRateLimits)
            {
                if (currentTime - kvp.Value.windowStart > rateLimitWindow * 2)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (ulong clientId in toRemove)
            {
                clientRateLimits.Remove(clientId);
            }
        }

        private void UpdatePerformanceTracking()
        {
            if (Time.frameCount % 60 == 0) // Update every 60 frames
            {
                frameStartTime = Time.time;
            }
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        private struct PendingCast
        {
            public ulong clientId;
            public AbilityType abilityType;
            public Vector3 targetPosition;
            public float timestamp;
            public float serverReceiveTime;
        }

        private class ClientRateLimit
        {
            public float windowStart;
            public int castCount;
            public float lastCastTime;
            public int rapidCastCount;
        }

        #endregion
    }

    public enum AbilityType
    {
        PrimaryAttack,
        Ability1,
        Ability2,
        Ultimate
    }

    [System.Serializable]
    public struct AbilityData
    {
        public string name;
        public float damage;
        public float range;
        public float speed;
        public float lifetime;
    }

    public interface IDamageable
    {
        void TakeDamage(float damage);
    }
}