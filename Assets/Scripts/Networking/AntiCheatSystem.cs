using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace MOBA.Networking
{
    /// <summary>
    /// Comprehensive anti-cheat system for server-authoritative validation
    /// Detects and prevents cheating through multiple validation layers
    /// </summary>
    public class AntiCheatSystem : MonoBehaviour
    {
        private static AntiCheatSystem _instance;
        public static AntiCheatSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AntiCheatSystem");
                    _instance = go.AddComponent<AntiCheatSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Anti-Cheat Settings")]
        [SerializeField] private float speedCheckInterval = 0.1f;
        [SerializeField] private float maxSpeedMultiplier = 1.5f;
        [SerializeField] private float maxAcceleration = 50f;
        [SerializeField] private int maxViolationsBeforeAction = 5;
        [SerializeField] private float violationResetTime = 30f;

        [Header("Detection Thresholds")]
        [SerializeField] private float teleportDetectionThreshold = 10f;
        [SerializeField] private float speedHackDetectionThreshold = 2f;

        [Header("Legitimate Teleport Points")]
        [SerializeField] private Transform[] spawnPoints; // Explicitly assigned spawn points - Clean Code principle
        [SerializeField] private Transform[] teleportZones; // Designated teleport areas
        [SerializeField] private float legitimateTeleportRadius = 3f; // Tighter radius for security

        // Client tracking
        private Dictionary<ulong, ClientProfile> clientProfiles = new Dictionary<ulong, ClientProfile>();
        private Dictionary<ulong, Stopwatch> pingStopwatches = new Dictionary<ulong, Stopwatch>();

        private struct ClientProfile
        {
            public ulong clientId;
            public Vector3 lastPosition;
            public Vector3 lastVelocity;
            public float lastUpdateTime;
            public int speedViolationCount;
            public int teleportViolationCount;
            public int rapidFireViolationCount;
            public int totalViolations;
            public float lastViolationTime;
            public bool isSuspected;
            public List<string> violationHistory;
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

        private void Start()
        {
            InvokeRepeating(nameof(PeriodicValidation), speedCheckInterval, speedCheckInterval);
        }

        /// <summary>
        /// Validate client movement for speed hacks and teleportation
        /// </summary>
        public bool ValidateMovement(ulong clientId, Vector3 newPosition, Vector3 newVelocity, float deltaTime)
        {
            if (!clientProfiles.ContainsKey(clientId))
            {
                InitializeClientProfile(clientId);
            }

            var profile = clientProfiles[clientId];

            // Speed validation
            if (!ValidateSpeed(clientId, newPosition, newVelocity, deltaTime, ref profile))
            {
                profile.speedViolationCount++;
                LogViolation(clientId, "Speed hack detected", profile.speedViolationCount);
                clientProfiles[clientId] = profile;
                return false;
            }

            // Teleportation detection
            if (!ValidateTeleportation(clientId, newPosition, ref profile))
            {
                profile.teleportViolationCount++;
                LogViolation(clientId, "Teleportation detected", profile.teleportViolationCount);
                clientProfiles[clientId] = profile;
                return false;
            }

            // Acceleration validation
            if (!ValidateAcceleration(clientId, newVelocity, deltaTime, ref profile))
            {
                LogViolation(clientId, "Invalid acceleration", 1);
                clientProfiles[clientId] = profile;
                return false;
            }

            // Update profile
            profile.lastPosition = newPosition;
            profile.lastVelocity = newVelocity;
            profile.lastUpdateTime = Time.time;
            clientProfiles[clientId] = profile;

            return true;
        }

        /// <summary>
        /// Validate ability casting for rapid fire and spam
        /// </summary>
        public bool ValidateAbilityCast(ulong clientId, AbilityType abilityType, float timestamp)
        {
            if (!clientProfiles.ContainsKey(clientId))
            {
                InitializeClientProfile(clientId);
            }

            var profile = clientProfiles[clientId];

            // Rapid fire detection
            if (!ValidateRapidFire(clientId, timestamp, ref profile))
            {
                profile.rapidFireViolationCount++;
                LogViolation(clientId, "Rapid fire detected", profile.rapidFireViolationCount);
                clientProfiles[clientId] = profile;
                return false;
            }

            // Update profile
            clientProfiles[clientId] = profile;
            return true;
        }

        /// <summary>
        /// Validate input for manipulation
        /// </summary>
        public bool ValidateInput(ulong clientId, ClientInput input)
        {
            // Magnitude validation
            if (input.movement.magnitude > 1.1f) // Allow slight overage for floating point
            {
                LogViolation(clientId, "Invalid input magnitude", 1);
                return false;
            }

            // Timestamp validation (prevent replay attacks)
            float currentTime = Time.time;
            if (Mathf.Abs(input.timestamp - currentTime) > 1f) // 1 second tolerance
            {
                LogViolation(clientId, "Invalid timestamp", 1);
                return false;
            }

            return true;
        }

        private bool ValidateSpeed(ulong clientId, Vector3 newPosition, Vector3 newVelocity, float deltaTime, ref ClientProfile profile)
        {
            if (deltaTime <= 0) return true;

            // Calculate actual speed
            Vector3 displacement = newPosition - profile.lastPosition;
            float actualSpeed = displacement.magnitude / deltaTime;

            // Get expected max speed (with tolerance)
            float expectedMaxSpeed = 15f * maxSpeedMultiplier; // Base max speed * multiplier

            // Use speed hack detection threshold for additional validation
            if (speedHackDetectionThreshold > 0 && actualSpeed > speedHackDetectionThreshold)
            {
                return false;
            }

            if (actualSpeed > expectedMaxSpeed)
            {
                return false;
            }

            return true;
        }

        private bool ValidateTeleportation(ulong clientId, Vector3 newPosition, ref ClientProfile profile)
        {
            float distance = Vector3.Distance(newPosition, profile.lastPosition);

            // Allow large distances for respawns or legitimate teleports
            if (distance > teleportDetectionThreshold)
            {
                // Check if this could be a legitimate teleport (e.g., respawn)
                if (!IsLegitimateTeleport(clientId, newPosition))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateAcceleration(ulong clientId, Vector3 newVelocity, float deltaTime, ref ClientProfile profile)
        {
            if (deltaTime <= 0) return true;

            Vector3 acceleration = (newVelocity - profile.lastVelocity) / deltaTime;

            if (acceleration.magnitude > maxAcceleration)
            {
                return false;
            }

            return true;
        }

        private bool ValidateRapidFire(ulong clientId, float timestamp, ref ClientProfile profile)
        {
            // Simple rate limiting - could be enhanced with more sophisticated detection
            float timeSinceLastCast = timestamp - profile.lastUpdateTime;

            if (timeSinceLastCast < 0.05f) // Less than 50ms between casts
            {
                return false;
            }

            return true;
        }

        private bool IsLegitimateTeleport(ulong clientId, Vector3 position)
        {
            // Defensive programming - validate inputs first
            if (position == Vector3.zero || float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z))
            {
                UnityEngine.Debug.LogWarning($"[AntiCheat] Invalid teleport position for client {clientId}: {position}");
                return false;
            }

            // Check explicitly assigned spawn points - Clean Code single responsibility
            if (IsNearSpawnPoints(position))
            {
                return true;
            }

            // Check designated teleport zones
            if (IsNearTeleportZones(position))
            {
                return true;
            }

            // Check if player recently died (respawn scenario)
            if (clientProfiles.TryGetValue(clientId, out var profile))
            {
                float timeSinceLastViolation = Time.time - profile.lastViolationTime;
                if (timeSinceLastViolation < 5f) // Recent death/respawn
                {
                    return IsNearSpawnPoints(position); // Only allow spawn point teleports after death
                }
            }

            return false;
        }

        // Extract method following Clean Code principles - single responsibility
        private bool IsNearSpawnPoints(Vector3 position)
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[AntiCheat] No spawn points configured - all teleports will be flagged as invalid");
                return false;
            }

            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint != null && Vector3.Distance(position, spawnPoint.position) <= legitimateTeleportRadius)
                {
                    return true;
                }
            }
            return false;
        }

        // Extract method following Clean Code principles
        private bool IsNearTeleportZones(Vector3 position)
        {
            if (teleportZones == null || teleportZones.Length == 0)
            {
                return false; // No teleport zones configured
            }

            foreach (var zone in teleportZones)
            {
                if (zone != null && Vector3.Distance(position, zone.position) <= legitimateTeleportRadius)
                {
                    return true;
                }
            }
            return false;
        }

        private void InitializeClientProfile(ulong clientId)
        {
            clientProfiles[clientId] = new ClientProfile
            {
                clientId = clientId,
                lastPosition = Vector3.zero,
                lastVelocity = Vector3.zero,
                lastUpdateTime = Time.time,
                speedViolationCount = 0,
                teleportViolationCount = 0,
                rapidFireViolationCount = 0,
                totalViolations = 0,
                lastViolationTime = 0,
                isSuspected = false,
                violationHistory = new List<string>()
            };
        }

        private void LogViolation(ulong clientId, string violationType, int violationCount)
        {
            var profile = clientProfiles[clientId];
            profile.totalViolations++;
            profile.lastViolationTime = Time.time;
            profile.violationHistory.Add($"{Time.time}: {violationType} (Count: {violationCount})");

            // Keep only last 10 violations
            if (profile.violationHistory.Count > 10)
            {
                profile.violationHistory.RemoveAt(0);
            }

            clientProfiles[clientId] = profile;

            UnityEngine.Debug.LogWarning($"[AntiCheat] Client {clientId} violation: {violationType} (Total: {profile.totalViolations})");

            // Take action if too many violations
            if (profile.totalViolations >= maxViolationsBeforeAction)
            {
                HandleCheatingClient(clientId, violationType);
            }
        }

        private void HandleCheatingClient(ulong clientId, string violationType)
        {
            UnityEngine.Debug.LogError($"[AntiCheat] Taking action against cheating client {clientId}: {violationType}");

            // Mark as suspected
            var profile = clientProfiles[clientId];
            profile.isSuspected = true;
            clientProfiles[clientId] = profile;

            // Could implement various actions:
            // 1. Log to server console
            // 2. Send warning to client
            // 3. Temporarily slow down client
            // 4. Kick client
            // 5. Ban client

            // For now, just log and mark as suspected
            NetworkEventBus.Instance.PublishAbilityRateLimitExceeded(clientId, AbilityType.PrimaryAttack);
        }

        private void PeriodicValidation()
        {
            // Reset violation counts periodically
            foreach (var clientId in clientProfiles.Keys)
            {
                var profile = clientProfiles[clientId];

                if (Time.time - profile.lastViolationTime > violationResetTime)
                {
                    profile.speedViolationCount = Mathf.Max(0, profile.speedViolationCount - 1);
                    profile.teleportViolationCount = Mathf.Max(0, profile.teleportViolationCount - 1);
                    profile.rapidFireViolationCount = Mathf.Max(0, profile.rapidFireViolationCount - 1);
                }

                clientProfiles[clientId] = profile;
            }
        }

        /// <summary>
        /// Get client statistics for monitoring
        /// </summary>
        public Dictionary<ulong, (int violations, bool suspected, List<string> history)> GetClientStats()
        {
            var stats = new Dictionary<ulong, (int, bool, List<string>)>();

            foreach (var kvp in clientProfiles)
            {
                var profile = kvp.Value;
                stats[kvp.Key] = (profile.totalViolations, profile.isSuspected, new List<string>(profile.violationHistory));
            }

            return stats;
        }

        /// <summary>
        /// Clear client data (useful for testing or server restart)
        /// </summary>
        public void ClearClientData()
        {
            clientProfiles.Clear();
            UnityEngine.Debug.Log("[AntiCheat] Cleared all client data");
        }

        /// <summary>
        /// Check if client is suspected of cheating
        /// </summary>
        public bool IsClientSuspected(ulong clientId)
        {
            return clientProfiles.TryGetValue(clientId, out var profile) && profile.isSuspected;
        }

        private void OnDestroy()
        {
            ClearClientData();
        }
    }
}