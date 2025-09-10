using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

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
        [SerializeField] private int maxCastsPerSecond = 10;
        [SerializeField] private float rateLimitWindow = 1f;

        [Header("Anti-Cheat")]
        [SerializeField] private float maxCastRange = 20f;
        [SerializeField] private float minCastInterval = 0.1f;
        [SerializeField] private int maxConsecutiveCasts = 5;

        // Network synchronized cooldowns
        private NetworkVariable<float> networkGlobalCooldown = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkAbility1Cooldown = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkAbility2Cooldown = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> networkUltimateCooldown = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Rate limiting per client
        private Dictionary<ulong, ClientRateLimit> clientRateLimits = new Dictionary<ulong, ClientRateLimit>();

        // Ability data cache
        private Dictionary<string, AbilityData> abilityCache = new Dictionary<string, AbilityData>();

        private void Awake()
        {
            InitializeAbilityCache();
        }

        private void InitializeAbilityCache()
        {
            abilityCache["PrimaryAttack"] = new AbilityData { name = "PrimaryAttack", damage = 50, range = 5f };
            abilityCache["Ability1"] = new AbilityData { name = "Ability1", damage = 100, range = 8f };
            abilityCache["Ability2"] = new AbilityData { name = "Ability2", damage = 150, range = 10f };
            abilityCache["Ultimate"] = new AbilityData { name = "Ultimate", damage = 300, range = 15f };
        }

        private void Update()
        {
            if (IsServer)
            {
                UpdateCooldowns();
            }
        }

        private void UpdateCooldowns()
        {
            // Update global cooldown
            if (networkGlobalCooldown.Value > 0)
            {
                networkGlobalCooldown.Value = Mathf.Max(0, networkGlobalCooldown.Value - Time.deltaTime);
            }

            // Update ability cooldowns
            if (networkAbility1Cooldown.Value > 0)
            {
                networkAbility1Cooldown.Value = Mathf.Max(0, networkAbility1Cooldown.Value - Time.deltaTime);
            }

            if (networkAbility2Cooldown.Value > 0)
            {
                networkAbility2Cooldown.Value = Mathf.Max(0, networkAbility2Cooldown.Value - Time.deltaTime);
            }

            if (networkUltimateCooldown.Value > 0)
            {
                networkUltimateCooldown.Value = Mathf.Max(0, networkUltimateCooldown.Value - Time.deltaTime);
            }
        }

        /// <summary>
        /// Casts an ability with server validation
        /// </summary>
        public void CastAbility(AbilityType abilityType, Vector3 targetPosition, ulong clientId)
        {
            if (!IsServer) return;

            if (ValidateAbilityCast(clientId, abilityType, targetPosition))
            {
                ExecuteAbility(abilityType, targetPosition);
                UpdateCooldowns(abilityType);

                // Notify clients
                AbilityCastClientRpc(abilityType, targetPosition);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CastAbilityServerRpc(AbilityType abilityType, Vector3 targetPosition, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            CastAbility(abilityType, targetPosition, clientId);
        }

        [ClientRpc]
        private void AbilityCastClientRpc(AbilityType abilityType, Vector3 targetPosition)
        {
            if (!IsServer)
            {
                // Client visual feedback
                SpawnAbilityEffect(abilityType, targetPosition);
            }
        }

        private bool ValidateAbilityCast(ulong clientId, AbilityType abilityType, Vector3 targetPosition)
        {
            // Check rate limiting with enhanced tracking
            if (!CheckClientRateLimit(clientId))
            {
                UnityEngine.Debug.LogWarning($"[NetworkAbilitySystem] Rate limit exceeded for client {clientId}");
                return false;
            }

            // Check minimum cast interval (anti-spam)
            if (!CheckMinimumCastInterval(clientId))
            {
                UnityEngine.Debug.LogWarning($"[NetworkAbilitySystem] Minimum cast interval violated for client {clientId}");
                return false;
            }

            // Check cooldowns
            if (networkGlobalCooldown.Value > 0)
            {
                UnityEngine.Debug.LogWarning($"[NetworkAbilitySystem] Global cooldown active for client {clientId}");
                return false;
            }

            switch (abilityType)
            {
                case AbilityType.Ability1:
                    if (networkAbility1Cooldown.Value > 0) return false;
                    break;
                case AbilityType.Ability2:
                    if (networkAbility2Cooldown.Value > 0) return false;
                    break;
                case AbilityType.Ultimate:
                    if (networkUltimateCooldown.Value > 0) return false;
                    break;
            }

            // Validate target position with lag compensation
            if (!IsValidTargetPosition(clientId, targetPosition, abilityType))
            {
                UnityEngine.Debug.LogWarning($"[NetworkAbilitySystem] Invalid target position for client {clientId}: {targetPosition}");
                return false;
            }

            return true;
        }

        private bool CheckMinimumCastInterval(ulong clientId)
        {
            if (!clientRateLimits.ContainsKey(clientId))
            {
                clientRateLimits[clientId] = new ClientRateLimit();
                return true;
            }

            ClientRateLimit rateLimit = clientRateLimits[clientId];
            float timeSinceLastCast = Time.time - rateLimit.lastCastTime;

            if (timeSinceLastCast < minCastInterval)
            {
                rateLimit.rapidCastCount++;
                if (rateLimit.rapidCastCount > maxConsecutiveCasts)
                {
                    // Potential cheating detected
                    UnityEngine.Debug.LogError($"[NetworkAbilitySystem] Rapid casting detected for client {clientId}");
                    return false;
                }
            }
            else
            {
                rateLimit.rapidCastCount = 0;
            }

            rateLimit.lastCastTime = Time.time;
            return true;
        }

        private bool CheckClientRateLimit(ulong clientId)
        {
            if (!clientRateLimits.ContainsKey(clientId))
            {
                clientRateLimits[clientId] = new ClientRateLimit();
            }

            ClientRateLimit rateLimit = clientRateLimits[clientId];
            float currentTime = Time.time;

            // Reset window if needed
            if (currentTime - rateLimit.windowStart > rateLimitWindow)
            {
                rateLimit.windowStart = currentTime;
                rateLimit.castCount = 0;
            }

            if (rateLimit.castCount >= maxCastsPerSecond)
            {
                return false;
            }

            rateLimit.castCount++;
            return true;
        }

        private bool IsValidTargetPosition(ulong clientId, Vector3 position, AbilityType abilityType)
        {
            // Get ability range
            float range = GetAbilityRange(abilityType);

            // Check if target is within map bounds
            if (position.magnitude > 100f) return false;

            // Check if target is within global max cast range
            if (maxCastRange > 0 && position.magnitude > maxCastRange) return false;

            // Get caster position with lag compensation
            Vector3 casterPosition = GetCasterPositionWithLagCompensation(clientId);

            // Check if target is within ability range from caster
            if (Vector3.Distance(casterPosition, position) > range) return false;

            // Additional validation: check line of sight, etc.
            // For now, basic range check
            return true;
        }

        private Vector3 GetCasterPositionWithLagCompensation(ulong clientId)
        {
            // Find the player controller for lag compensation
            var playerController = FindPlayerController(clientId);
            if (playerController != null)
            {
                // Use lag compensation to get historical position
                float lagCompensationTime = EstimateClientLatency(clientId);
                var (position, _) = playerController.GetPositionAtTime(Time.time - lagCompensationTime);
                return position;
            }

            return transform.position; // Fallback
        }

        private NetworkPlayerController FindPlayerController(ulong clientId)
        {
            // Find all player controllers and match by owner
            var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.OwnerClientId == clientId)
                {
                    return player;
                }
            }
            return null;
        }

        private float EstimateClientLatency(ulong clientId)
        {
            // Simple latency estimation - in production, use actual ping measurements
            return 0.1f; // 100ms default
        }

        private float GetAbilityRange(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.PrimaryAttack: return 5f;
                case AbilityType.Ability1: return 8f;
                case AbilityType.Ability2: return 10f;
                case AbilityType.Ultimate: return 15f;
                default: return 5f;
            }
        }

        private void UpdateCooldowns(AbilityType abilityType)
        {
            networkGlobalCooldown.Value = globalCooldown;

            switch (abilityType)
            {
                case AbilityType.Ability1:
                    networkAbility1Cooldown.Value = ability1Cooldown;
                    break;
                case AbilityType.Ability2:
                    networkAbility2Cooldown.Value = ability2Cooldown;
                    break;
                case AbilityType.Ultimate:
                    networkUltimateCooldown.Value = ultimateCooldown;
                    break;
            }
        }

        private void ExecuteAbility(AbilityType abilityType, Vector3 targetPosition)
        {
            // Server executes ability logic
            string abilityName = abilityType.ToString();

            if (abilityCache.ContainsKey(abilityName))
            {
                AbilityData ability = abilityCache[abilityName];

                // Spawn projectile or apply effect
                SpawnProjectile(ability, targetPosition);
            }
        }

        private void SpawnProjectile(AbilityData ability, Vector3 targetPosition)
        {
            // Use object pool for projectiles
            var pool = FindFirstObjectByType<ProjectilePool>();
            if (pool != null)
            {
                Vector2 direction = (targetPosition - transform.position).normalized;
                string flyweightName = GetFlyweightNameForAbility(ability.name);

                if (pool.GetAvailableFlyweightNames().Contains(flyweightName))
                {
                    pool.SpawnProjectileWithFlyweight(transform.position, direction, flyweightName);
                }
                else
                {
                    pool.SpawnProjectile(transform.position, direction, 10f, ability.damage, 3f);
                }
            }
        }

        private void SpawnAbilityEffect(AbilityType abilityType, Vector3 targetPosition)
        {
            // Client-side visual effects
            string abilityName = abilityType.ToString();

            // Create temporary effect
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = targetPosition;
            effect.transform.localScale = Vector3.one * 0.5f;

            var renderer = effect.GetComponent<Renderer>();
            renderer.material.color = GetAbilityColor(abilityType);

            Object.Destroy(effect, 0.5f);
        }

        private Color GetAbilityColor(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.PrimaryAttack: return Color.white;
                case AbilityType.Ability1: return Color.blue;
                case AbilityType.Ability2: return Color.red;
                case AbilityType.Ultimate: return Color.yellow;
                default: return Color.white;
            }
        }

        private string GetFlyweightNameForAbility(string abilityName)
        {
            switch (abilityName.ToLower())
            {
                case "primaryattack":
                    return "BasicProjectile";
                case "ability1":
                    return "FastProjectile";
                case "ability2":
                    return "HeavyProjectile";
                case "ultimate":
                    return "HomingProjectile";
                default:
                    return "BasicProjectile";
            }
        }

        // Public API for cooldown checking
        public bool CanCastAbility(AbilityType abilityType)
        {
            if (networkGlobalCooldown.Value > 0) return false;

            switch (abilityType)
            {
                case AbilityType.Ability1:
                    return networkAbility1Cooldown.Value <= 0;
                case AbilityType.Ability2:
                    return networkAbility2Cooldown.Value <= 0;
                case AbilityType.Ultimate:
                    return networkUltimateCooldown.Value <= 0;
                default:
                    return true;
            }
        }

        public float GetRemainingCooldown(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.Ability1:
                    return networkAbility1Cooldown.Value;
                case AbilityType.Ability2:
                    return networkAbility2Cooldown.Value;
                case AbilityType.Ultimate:
                    return networkUltimateCooldown.Value;
                default:
                    return 0f;
            }
        }

        private class ClientRateLimit
        {
            public float windowStart;
            public int castCount;
            public float lastCastTime;
            public int rapidCastCount;
        }
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
}