using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System;
using MOBA.Debugging;

namespace MOBA.Networking
{
    /// <summary>
    /// Handles network performance monitoring, statistics collection, and ping tracking.
    /// Responsible for measuring RTT, tracking connection quality, and providing
    /// network performance metrics for both client and server environments.
    /// </summary>
    public class NetworkStatisticsManager : NetworkManagerComponent
    {
        #region Configuration
        
        [Header("Statistics Settings")]
        [SerializeField, Tooltip("Maximum allowed ping before warning")]
        private float maxAllowedPing = 500f;
        
        [SerializeField, Tooltip("Interval between ping measurements in seconds")]
        private float pingUpdateInterval = 1f;
        
        [SerializeField, Tooltip("Enable detailed statistics logging")]
        private bool logStatistics = true;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Current network statistics data
        /// </summary>
        private NetworkStats networkStats;
        
        /// <summary>
        /// Current ping/RTT measurement
        /// </summary>
        private float currentPing = 0f;
        
        /// <summary>
        /// Last time ping was measured
        /// </summary>
        private float lastPingTime = 0f;
        
        /// <summary>
        /// Reference to the main production network manager
        /// </summary>
        private ProductionNetworkManager productionNetworkManager;
        
        /// <summary>
        /// Reference to connected players for ping calculation
        /// </summary>
        private Dictionary<ulong, PlayerNetworkData> connectedPlayers;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when ping is updated. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<float> OnPingUpdated;
        
        /// <summary>
        /// Raised when network statistics are updated
        /// </summary>
        public System.Action<NetworkStats> OnStatsUpdated;
        
        /// <summary>
        /// Raised when high ping is detected
        /// </summary>
        public System.Action<float> OnHighPingDetected;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(ProductionNetworkManager networkManager)
        {
            base.Initialize(networkManager);
            productionNetworkManager = networkManager;
            networkStats = new NetworkStats();
            connectedPlayers = new Dictionary<ulong, PlayerNetworkData>();
            
            if (logStatistics)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking, subsystem: "Statistics"),
                    "Network statistics manager initialized.",
                    ("MaxPing", maxAllowedPing),
                    ("UpdateInterval", pingUpdateInterval));
            }
        }
        
        public override void Shutdown()
        {
            OnPingUpdated = null;
            OnStatsUpdated = null;
            OnHighPingDetected = null;
            connectedPlayers?.Clear();
            
            if (logStatistics)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking, subsystem: "Statistics"),
                    "Network statistics manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Update Loop
        
        /// <summary>
        /// Update network statistics and ping measurements
        /// Called from ProductionNetworkManager's Update loop
        /// </summary>
        public void UpdateStatistics()
        {
            UpdatePing();
            UpdateNetworkStats();
        }
        
        #endregion
        
        #region Ping Monitoring
        
        /// <summary>
        /// Measures and updates current ping/RTT values
        /// </summary>
        private void UpdatePing()
        {
            if (Time.time - lastPingTime <= pingUpdateInterval)
            {
                return;
            }

            lastPingTime = Time.time;

            if (NetworkManager.Singleton == null)
            {
                return;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                return;
            }

            int measuredRtt = CalculatePing(transport);
            
            if (measuredRtt < 0)
            {
                return;
            }

            currentPing = measuredRtt;
            OnPingUpdated?.Invoke(currentPing);

            // Check for high ping warning
            if (currentPing > maxAllowedPing)
            {
                OnHighPingDetected?.Invoke(currentPing);
                
                if (logStatistics)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Networking, subsystem: "Ping"),
                        "High ping detected.",
                        ("Ping", currentPing),
                        ("Threshold", maxAllowedPing));
                }
            }
        }
        
        /// <summary>
        /// Calculate ping based on current network role and connected clients
        /// </summary>
        /// <param name="transport">Unity Transport component</param>
        /// <returns>Calculated ping in milliseconds, or -1 if invalid</returns>
        private int CalculatePing(UnityTransport transport)
        {
            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                // Client: measure RTT to server
                return CalculateClientPing(transport);
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                // Server: calculate average or host ping
                return CalculateServerPing(transport);
            }
            
            return -1;
        }
        
        /// <summary>
        /// Calculate ping for client connections
        /// </summary>
        /// <param name="transport">Unity Transport component</param>
        /// <returns>RTT to server in milliseconds</returns>
        private int CalculateClientPing(UnityTransport transport)
        {
            ulong serverClientId = NetworkManager.ServerClientId;
            ulong rawRtt = transport.GetCurrentRtt(serverClientId);
            
            if (rawRtt > int.MaxValue)
            {
                if (logStatistics)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Networking, subsystem: "Ping"),
                        "Server RTT exceeds int range.",
                        ("Rtt", rawRtt));
                }
                return -1;
            }

            return (int)rawRtt;
        }
        
        /// <summary>
        /// Calculate ping for server (host or dedicated)
        /// </summary>
        /// <param name="transport">Unity Transport component</param>
        /// <returns>Average client ping or 0 for host</returns>
        private int CalculateServerPing(UnityTransport transport)
        {
            if (NetworkManager.Singleton.IsClient)
            {
                // Host: treat self ping as zero for UI purposes
                return 0;
            }
            else
            {
                // Dedicated server: average connected clients that report valid RTTs
                return CalculateAverageClientPing(transport);
            }
        }
        
        /// <summary>
        /// Calculate average ping across all connected clients
        /// </summary>
        /// <param name="transport">Unity Transport component</param>
        /// <returns>Average client ping in milliseconds</returns>
        private int CalculateAverageClientPing(UnityTransport transport)
        {
            float totalRtt = 0f;
            int sampleCount = 0;
            
            foreach (var kvp in connectedPlayers)
            {
                if (!kvp.Value.IsActive)
                {
                    continue;
                }

                ulong clientId = kvp.Key;
                ulong rawClientRtt = transport.GetCurrentRtt(clientId);
                
                if (rawClientRtt > int.MaxValue)
                {
                    if (logStatistics)
                    {
                        GameDebug.LogWarning(
                            BuildContext(GameDebugMechanicTag.Networking, subsystem: "Ping"),
                            "Client RTT exceeds int range.",
                            ("ClientId", clientId),
                            ("Rtt", rawClientRtt));
                    }
                    continue;
                }

                int clientRtt = (int)rawClientRtt;
                if (clientRtt >= 0)
                {
                    totalRtt += clientRtt;
                    sampleCount++;
                }
            }

            return sampleCount > 0 ? Mathf.RoundToInt(totalRtt / sampleCount) : 0;
        }
        
        #endregion
        
        #region Network Statistics
        
        /// <summary>
        /// Updates comprehensive network statistics
        /// </summary>
        private void UpdateNetworkStats()
        {
            if (NetworkManager.Singleton == null) return;
            
            // Update basic network statistics
            networkStats.ConnectedPlayers = connectedPlayers.Count;
            networkStats.CurrentPing = currentPing;
            networkStats.ConnectionState = productionNetworkManager?.GetConnectionState() ?? NetworkConnectionState.Disconnected;
            
            // Trigger statistics update event
            OnStatsUpdated?.Invoke(networkStats);
        }
        
        #endregion
        
        #region Player Management
        
        /// <summary>
        /// Add a connected player for statistics tracking
        /// </summary>
        /// <param name="clientId">Client ID of the connected player</param>
        /// <param name="playerData">Player network data</param>
        public void AddConnectedPlayer(ulong clientId, PlayerNetworkData playerData)
        {
            connectedPlayers[clientId] = playerData;
            
            if (logStatistics)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking, subsystem: "Statistics", actorClientId: clientId),
                    "Player added to statistics tracking.",
                    ("TotalPlayers", connectedPlayers.Count));
            }
        }
        
        /// <summary>
        /// Remove a disconnected player from statistics tracking
        /// </summary>
        /// <param name="clientId">Client ID of the disconnected player</param>
        public void RemoveConnectedPlayer(ulong clientId)
        {
            if (connectedPlayers.Remove(clientId))
            {
                if (logStatistics)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Networking, subsystem: "Statistics", actorClientId: clientId),
                        "Player removed from statistics tracking.",
                        ("TotalPlayers", connectedPlayers.Count));
                }
            }
        }
        
        /// <summary>
        /// Clear all connected players from statistics tracking
        /// </summary>
        public void ClearConnectedPlayers()
        {
            connectedPlayers.Clear();
            
            if (logStatistics)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking, subsystem: "Statistics"),
                    "All players cleared from statistics tracking.");
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Update statistics configuration at runtime
        /// </summary>
        /// <param name="maxPing">Maximum allowed ping before warning</param>
        /// <param name="updateInterval">Ping update interval in seconds</param>
        public void UpdateConfiguration(float maxPing, float updateInterval)
        {
            maxAllowedPing = maxPing;
            pingUpdateInterval = updateInterval;
            
            if (logStatistics)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking, subsystem: "Statistics"),
                    "Statistics configuration updated.",
                    ("MaxPing", maxPing),
                    ("UpdateInterval", updateInterval));
            }
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get current ping/RTT measurement
        /// </summary>
        /// <returns>Current ping in milliseconds</returns>
        public float GetCurrentPing()
        {
            return currentPing;
        }
        
        /// <summary>
        /// Get comprehensive network statistics
        /// </summary>
        /// <returns>Current network statistics</returns>
        public NetworkStats GetNetworkStats()
        {
            return networkStats;
        }
        
        /// <summary>
        /// Get number of connected players being tracked
        /// </summary>
        /// <returns>Number of connected players</returns>
        public int GetConnectedPlayerCount()
        {
            return connectedPlayers.Count;
        }
        
        /// <summary>
        /// Check if ping is currently high
        /// </summary>
        /// <returns>True if current ping exceeds threshold</returns>
        public bool IsHighPing()
        {
            return currentPing > maxAllowedPing;
        }
        
        /// <summary>
        /// Get ping for a specific client (server only)
        /// </summary>
        /// <param name="clientId">Client ID to check ping for</param>
        /// <returns>Client ping in milliseconds, or -1 if not available</returns>
        public float GetClientPing(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return -1f;
            }
            
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                return -1f;
            }
            
            ulong rawRtt = transport.GetCurrentRtt(clientId);
            return rawRtt > int.MaxValue ? -1f : (float)rawRtt;
        }
        
        /// <summary>
        /// Get current statistics configuration
        /// </summary>
        /// <returns>Statistics configuration data</returns>
        public StatisticsConfig GetConfiguration()
        {
            return new StatisticsConfig
            {
                MaxAllowedPing = maxAllowedPing,
                PingUpdateInterval = pingUpdateInterval,
                LogStatistics = logStatistics
            };
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build debug context for statistics logging
        /// </summary>
        private GameDebugContext BuildContext(GameDebugMechanicTag tag, string subsystem = null, ulong? actorClientId = null)
        {
            string actor = actorClientId?.ToString();
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                tag,
                subsystem ?? "Statistics",
                actor);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    /// <summary>
    /// Configuration data structure for network statistics settings
    /// </summary>
    [System.Serializable]
    public struct StatisticsConfig
    {
        public float MaxAllowedPing;
        public float PingUpdateInterval;
        public bool LogStatistics;
    }
    
    #endregion
}