using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using MOBA.Debugging;

namespace MOBA.Networking
{
    /// <summary>
    /// Handles anti-cheat validation and player behavior monitoring for the network session.
    /// Responsible for detecting suspicious player activities like speed hacking, teleporting,
    /// and behavioral anomalies. Integrates with the main network manager to enforce rules.
    /// </summary>
    public class NetworkAntiCheatManager : NetworkManagerComponent
    {
        #region Anti-Cheat Configuration
        
        [Header("Anti-Cheat Settings")]
        [SerializeField, Tooltip("Maximum allowed player movement speed in units per second")]
        private float maxServerMovementSpeed = 15f;
        
        [SerializeField, Tooltip("Maximum distance a player can move in a single frame without being flagged")]
        private float maxServerTeleportDistance = 30f;
        
        [SerializeField, Tooltip("Number of suspicious activities before automatic kick")]
        private int maxSuspicionBeforeKick = 5;
        
        [SerializeField, Tooltip("Time in seconds for suspicion count to decay by 1")]
        private float suspicionDecayInterval = 60f;
        
        [SerializeField, Tooltip("Enable detailed anti-cheat logging")]
        private bool logAntiCheatEvents = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Tracks player network data for anti-cheat validation
        /// </summary>
        private Dictionary<ulong, PlayerNetworkData> trackedPlayers;
        
        /// <summary>
        /// Reference to the main production network manager
        /// </summary>
        private ProductionNetworkManager productionNetworkManager;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(ProductionNetworkManager networkManager)
        {
            base.Initialize(networkManager);
            productionNetworkManager = networkManager;
            trackedPlayers = new Dictionary<ulong, PlayerNetworkData>();
            
            if (logAntiCheatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Initialization"),
                    "Anti-cheat system initialized.",
                    ("MaxSpeed", maxServerMovementSpeed),
                    ("MaxTeleport", maxServerTeleportDistance),
                    ("MaxSuspicion", maxSuspicionBeforeKick));
            }
        }
        
        public override void Shutdown()
        {
            trackedPlayers?.Clear();
            productionNetworkManager = null;
            
            if (logAntiCheatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Shutdown"),
                    "Anti-cheat system shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Player Tracking
        
        /// <summary>
        /// Starts tracking a player for anti-cheat validation
        /// </summary>
        /// <param name="clientId">Client ID of the player to track</param>
        /// <param name="initialPosition">Initial position of the player</param>
        public void StartTrackingPlayer(ulong clientId, Vector3 initialPosition)
        {
            if (trackedPlayers.ContainsKey(clientId))
            {
                if (logAntiCheatEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Tracking", actorClientId: clientId),
                        "Player already being tracked for anti-cheat.");
                }
                return;
            }
            
            var playerData = new PlayerNetworkData
            {
                ClientId = clientId,
                ConnectedTime = Time.time,
                IsActive = true,
                LastPosition = initialPosition,
                LastMovementTime = Time.time,
                SuspiciousActivityCount = 0,
                LastSuspicionTime = 0f
            };
            
            trackedPlayers[clientId] = playerData;
            
            if (logAntiCheatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Tracking", actorClientId: clientId),
                    "Started tracking player for anti-cheat.",
                    ("Position", initialPosition));
            }
        }
        
        /// <summary>
        /// Stops tracking a player for anti-cheat validation
        /// </summary>
        /// <param name="clientId">Client ID of the player to stop tracking</param>
        public void StopTrackingPlayer(ulong clientId)
        {
            if (trackedPlayers.Remove(clientId))
            {
                if (logAntiCheatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Tracking", actorClientId: clientId),
                        "Stopped tracking player for anti-cheat.");
                }
            }
        }
        
        #endregion
        
        #region Anti-Cheat Validation
        
        /// <summary>
        /// Validates a player's behavior based on their current state and actions.
        /// Checks for movement anomalies, speed hacking, and teleporting.
        /// </summary>
        /// <param name="clientId">Client ID of the player to validate</param>
        /// <param name="currentPosition">Current position of the player</param>
        /// <param name="deltaTime">Time since last validation</param>
        public void ValidatePlayerBehavior(ulong clientId, Vector3 currentPosition, float deltaTime)
        {
            if (!trackedPlayers.TryGetValue(clientId, out var player))
            {
                // Start tracking if not already tracked
                StartTrackingPlayer(clientId, currentPosition);
                return;
            }
            
            if (deltaTime <= 0f)
            {
                return; // Skip validation for invalid time delta
            }
            
            ValidateMovement(player, currentPosition, deltaTime);
        }
        
        /// <summary>
        /// Validates player movement for speed and teleportation anomalies
        /// </summary>
        /// <param name="player">Player data to validate</param>
        /// <param name="currentPosition">Current position of the player</param>
        /// <param name="deltaTime">Time since last movement validation</param>
        private void ValidateMovement(PlayerNetworkData player, Vector3 currentPosition, float deltaTime)
        {
            float distance = Vector3.Distance(player.LastPosition, currentPosition);
            float now = Time.time;
            float speed = distance / deltaTime;

            bool flagged = false;

            // Check for speed hacking
            if (speed > maxServerMovementSpeed)
            {
                flagged = true;
                if (logAntiCheatEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Speed", actorClientId: player.ClientId),
                        "Player speed exceeded server threshold.",
                        ("Speed", speed),
                        ("Threshold", maxServerMovementSpeed));
                }
            }

            // Check for teleportation
            if (distance > maxServerTeleportDistance)
            {
                flagged = true;
                if (logAntiCheatEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Teleport", actorClientId: player.ClientId),
                        "Player teleported suspiciously.",
                        ("Distance", distance),
                        ("Threshold", maxServerTeleportDistance));
                }
            }

            // Handle flagged behavior
            if (flagged)
            {
                IncrementSuspicion(player, "Movement anomaly");
            }
            else if (player.SuspiciousActivityCount > 0 && now - player.LastSuspicionTime >= suspicionDecayInterval)
            {
                // Decay suspicion over time for good behavior
                player.SuspiciousActivityCount = Mathf.Max(0, player.SuspiciousActivityCount - 1);
                player.LastSuspicionTime = now;
                
                if (logAntiCheatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Decay", actorClientId: player.ClientId),
                        "Player suspicion decayed due to good behavior.",
                        ("NewSuspicion", player.SuspiciousActivityCount));
                }
            }

            // Update player tracking data
            player.LastPosition = currentPosition;
            player.LastMovementTime = now;
        }
        
        /// <summary>
        /// Increments a player's suspicion count and handles enforcement actions
        /// </summary>
        /// <param name="player">Player to increment suspicion for</param>
        /// <param name="reason">Reason for the suspicion increment</param>
        private void IncrementSuspicion(PlayerNetworkData player, string reason)
        {
            player.SuspiciousActivityCount++;
            player.LastSuspicionTime = Time.time;

            // Check if player should be kicked
            if (player.SuspiciousActivityCount >= maxSuspicionBeforeKick)
            {
                if (logAntiCheatEvents)
                {
                    GameDebug.LogError(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Enforcement", actorClientId: player.ClientId),
                        "Player exceeded suspicion limit; kicking.",
                        ("Reason", reason),
                        ("Suspicion", player.SuspiciousActivityCount));
                }

                // Request kick through the main network manager
                productionNetworkManager?.KickPlayer(player.ClientId, $"Anti-cheat triggered: {reason}");
            }
            else
            {
                if (logAntiCheatEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Warning", actorClientId: player.ClientId),
                        "Player flagged by anti-cheat.",
                        ("Reason", reason),
                        ("Suspicion", player.SuspiciousActivityCount),
                        ("Threshold", maxSuspicionBeforeKick));
                }
            }
        }
        
        #endregion
        
        #region Custom Validation
        
        /// <summary>
        /// Manually flag a player for suspicious activity with a custom reason
        /// </summary>
        /// <param name="clientId">Client ID of the player to flag</param>
        /// <param name="reason">Reason for flagging the player</param>
        /// <param name="severity">Severity of the violation (1-3, where 3 is most severe)</param>
        public void FlagPlayer(ulong clientId, string reason, int severity = 1)
        {
            if (!trackedPlayers.TryGetValue(clientId, out var player))
            {
                if (logAntiCheatEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Manual", actorClientId: clientId),
                        "Attempted to flag untracked player.");
                }
                return;
            }
            
            // Apply multiple suspicion increments based on severity
            for (int i = 0; i < severity; i++)
            {
                IncrementSuspicion(player, reason);
            }
        }
        
        /// <summary>
        /// Clear all suspicion for a player (admin function)
        /// </summary>
        /// <param name="clientId">Client ID of the player to clear suspicion for</param>
        public void ClearPlayerSuspicion(ulong clientId)
        {
            if (trackedPlayers.TryGetValue(clientId, out var player))
            {
                player.SuspiciousActivityCount = 0;
                player.LastSuspicionTime = Time.time;
                
                if (logAntiCheatEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Admin", actorClientId: clientId),
                        "Player suspicion cleared by admin.");
                }
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Update anti-cheat configuration at runtime
        /// </summary>
        /// <param name="maxSpeed">Maximum allowed movement speed</param>
        /// <param name="maxTeleportDistance">Maximum teleport distance</param>
        /// <param name="maxSuspicion">Maximum suspicion before kick</param>
        public void UpdateConfiguration(float maxSpeed, float maxTeleportDistance, int maxSuspicion)
        {
            maxServerMovementSpeed = maxSpeed;
            maxServerTeleportDistance = maxTeleportDistance;
            maxSuspicionBeforeKick = maxSuspicion;
            
            if (logAntiCheatEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Configuration"),
                    "Anti-cheat configuration updated.",
                    ("MaxSpeed", maxSpeed),
                    ("MaxTeleport", maxTeleportDistance),
                    ("MaxSuspicion", maxSuspicion));
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get the current suspicion count for a player
        /// </summary>
        /// <param name="clientId">Client ID to check</param>
        /// <returns>Suspicion count, or -1 if player not tracked</returns>
        public int GetPlayerSuspicion(ulong clientId)
        {
            return trackedPlayers.TryGetValue(clientId, out var player) ? player.SuspiciousActivityCount : -1;
        }
        
        /// <summary>
        /// Check if a player is currently being tracked
        /// </summary>
        /// <param name="clientId">Client ID to check</param>
        /// <returns>True if player is being tracked</returns>
        public bool IsPlayerTracked(ulong clientId)
        {
            return trackedPlayers.ContainsKey(clientId);
        }
        
        /// <summary>
        /// Get total number of players being tracked
        /// </summary>
        /// <returns>Number of tracked players</returns>
        public int GetTrackedPlayerCount()
        {
            return trackedPlayers.Count;
        }
        
        /// <summary>
        /// Get current anti-cheat configuration
        /// </summary>
        /// <returns>Anti-cheat configuration data</returns>
        public AntiCheatConfig GetConfiguration()
        {
            return new AntiCheatConfig
            {
                MaxMovementSpeed = maxServerMovementSpeed,
                MaxTeleportDistance = maxServerTeleportDistance,
                MaxSuspicionBeforeKick = maxSuspicionBeforeKick,
                SuspicionDecayInterval = suspicionDecayInterval
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for anti-cheat logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag, string subsystem = null, ulong? actorClientId = null)
        {
            string actor = actorClientId?.ToString();
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                tag,
                subsystem ?? "AntiCheat",
                actor);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration data structure for anti-cheat settings
    /// </summary>
    [System.Serializable]
    public struct AntiCheatConfig
    {
        public float MaxMovementSpeed;
        public float MaxTeleportDistance;
        public int MaxSuspicionBeforeKick;
        public float SuspicionDecayInterval;
    }
    
    #endregion
}