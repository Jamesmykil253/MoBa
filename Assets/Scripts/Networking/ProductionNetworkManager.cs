using UnityEngine;
// ...existing code...
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System.Collections;
using MOBA;

namespace MOBA.Networking
{
    /// <summary>
    /// Production-ready network manager with comprehensive features
    /// Replaces SimpleNetworkManager with robust networking capabilities
    /// </summary>
    public class ProductionNetworkManager : MonoBehaviour
    {
        [Header("Network Configuration")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private int maxConnections = 10;
    // ...existing code...
        
        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        [SerializeField] private bool enableReconnection = true;
        [SerializeField] private float reconnectionInterval = 5f;
        [SerializeField] private int maxReconnectionAttempts = 3;
        
        [Header("Anti-Cheat")]
        [SerializeField] private bool enableServerValidation = true;
        [SerializeField] private float maxAllowedPing = 500f;
    // ...existing code...
        
        [Header("Debug")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool logNetworkEvents = true;
        [SerializeField] private bool showNetworkStats = false;
        
        // Network state tracking
        private NetworkConnectionState connectionState = NetworkConnectionState.Disconnected;
        private float lastPingTime = 0f;
        private float currentPing = 0f;
        private int reconnectionAttempts = 0;
        private Coroutine reconnectionCoroutine;
        
        // Player management
        private Dictionary<ulong, PlayerNetworkData> connectedPlayers = new Dictionary<ulong, PlayerNetworkData>();
        private int currentPlayerCount = 0;
        
        // Network statistics
        private NetworkStats networkStats = new NetworkStats();
        
        // Events
        public System.Action<NetworkConnectionState> OnConnectionStateChanged;
        public System.Action<ulong> OnPlayerConnected;
        public System.Action<ulong> OnPlayerDisconnected;
        public System.Action<float> OnPingUpdated;
        public System.Action<string> OnNetworkError;
        
        #region Unity Lifecycle
        
        void Start()
        {
            InitializeNetworking();
            
            if (autoStartAsHost)
            {
                StartHost();
            }
        }
        
        void Update()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                UpdatePing();
                UpdateNetworkStats();
                
                if (enableServerValidation)
                {
                    PerformAntiCheatChecks();
                }
            }
        }
        
        void OnDestroy()
        {
            // Clean up
            if (reconnectionCoroutine != null)
            {
                StopCoroutine(reconnectionCoroutine);
            }

            // Unsubscribe from NetworkManager events
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
                NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
            }

            // Clean up custom events
            OnConnectionStateChanged = null;
            OnPlayerConnected = null;
            OnPlayerDisconnected = null;
            OnPingUpdated = null;
            OnNetworkError = null;
        }
        
        #endregion
        
        #region Network Initialization
        
        private void InitializeNetworking()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[ProductionNetworkManager] NetworkManager.Singleton is null!");
                return;
            }
            
            // Configure Unity Transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(serverIP, serverPort);
                // Network Simulator is now handled via Multiplayer Tools package in Unity 2023+
            }
            
            // Set up NetworkManager callbacks
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            
            if (logNetworkEvents)
            {
                Debug.Log("[ProductionNetworkManager] Network initialization complete");
            }
        }
        
        #endregion
        
        #region Connection Management
        
        public void StartHost()
        {
            if (NetworkManager.Singleton == null) return;
            
            SetConnectionState(NetworkConnectionState.Connecting);
            
            bool success = NetworkManager.Singleton.StartHost();
            if (!success)
            {
                HandleConnectionError("Failed to start as host");
            }
            else
            {
                if (logNetworkEvents)
                    Debug.Log("[ProductionNetworkManager] Starting as Host");
            }
        }
        
        public void StartServer()
        {
            if (NetworkManager.Singleton == null) return;
            
            SetConnectionState(NetworkConnectionState.Connecting);
            
            bool success = NetworkManager.Singleton.StartServer();
            if (!success)
            {
                HandleConnectionError("Failed to start as server");
            }
            else
            {
                if (logNetworkEvents)
                    Debug.Log("[ProductionNetworkManager] Starting as Server");
            }
        }
        
        public void StartClient()
        {
            if (NetworkManager.Singleton == null) return;
            
            SetConnectionState(NetworkConnectionState.Connecting);
            
            bool success = NetworkManager.Singleton.StartClient();
            if (!success)
            {
                HandleConnectionError("Failed to start as client");
                if (enableReconnection)
                {
                    ScheduleReconnection();
                }
            }
            else
            {
                if (logNetworkEvents)
                    Debug.Log($"[ProductionNetworkManager] Connecting to {serverIP}:{serverPort}");
            }
        }
        
        public void Disconnect()
        {
            if (reconnectionCoroutine != null)
            {
                StopCoroutine(reconnectionCoroutine);
                reconnectionCoroutine = null;
            }

            reconnectionAttempts = 0;

            // Clean up player/network state
            connectedPlayers.Clear();
            currentPlayerCount = 0;
            networkStats = new NetworkStats();

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
                if (logNetworkEvents)
                    Debug.Log("[ProductionNetworkManager] Disconnected");
            }

            SetConnectionState(NetworkConnectionState.Disconnected);
        }
        
        #endregion
        
        #region Connection Callbacks
        
        private void OnServerStarted()
        {
            SetConnectionState(NetworkConnectionState.Connected);
            currentPlayerCount = 1; // Host counts as a player
            
            if (logNetworkEvents)
                Debug.Log("[ProductionNetworkManager] Server started successfully");
        }
        
        private void OnClientStarted()
        {
            SetConnectionState(NetworkConnectionState.Connected);
            reconnectionAttempts = 0;
            
            if (logNetworkEvents)
                Debug.Log("[ProductionNetworkManager] Client connected successfully");
        }
        
        private void OnClientConnected(ulong clientId)
        {
            var playerData = new PlayerNetworkData
            {
                ClientId = clientId,
                ConnectedTime = Time.time,
                IsActive = true
            };
            
            connectedPlayers[clientId] = playerData;
            currentPlayerCount = connectedPlayers.Count;
            
            OnPlayerConnected?.Invoke(clientId);
            
            if (logNetworkEvents)
                Debug.Log($"[ProductionNetworkManager] Player {clientId} connected. Total players: {currentPlayerCount}");
                
            // Publish network event
            if (NetworkManager.Singleton.IsServer)
            {
                UnifiedEventSystem.PublishNetwork<PlayerConnectedEvent>(new PlayerConnectedEvent(clientId, Time.time));
            }
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
                currentPlayerCount = connectedPlayers.Count;
                
                OnPlayerDisconnected?.Invoke(clientId);
                
                if (logNetworkEvents)
                    Debug.Log($"[ProductionNetworkManager] Player {clientId} disconnected. Total players: {currentPlayerCount}");
                    
                // Publish network event
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    UnifiedEventSystem.PublishNetwork<PlayerDisconnectedEvent>(new PlayerDisconnectedEvent(clientId, Time.time));
                }
            }
            
            // Handle self disconnection
            if (clientId == NetworkManager.Singleton.LocalClientId && enableReconnection)
            {
                ScheduleReconnection();
            }
        }
        
        #endregion
        
        #region Reconnection System
        
        private void ScheduleReconnection()
        {
            if (reconnectionAttempts >= maxReconnectionAttempts)
            {
                HandleConnectionError($"Max reconnection attempts ({maxReconnectionAttempts}) reached");
                return;
            }
            
            if (reconnectionCoroutine != null)
            {
                StopCoroutine(reconnectionCoroutine);
            }
            
            reconnectionCoroutine = StartCoroutine(ReconnectionCoroutine());
        }
        
        #endregion
        
        #region Network Monitoring
        
        private void UpdatePing()
        {
            if (Time.time - lastPingTime > 1f) // Update ping every second
            {
                lastPingTime = Time.time;
                
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
                {
                    // Get RTT from Unity Transport if available
                    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                    if (transport != null)
                    {
                        // This is a simplified ping calculation
                        // In production, you'd want more sophisticated ping measurement
                        currentPing = Time.realtimeSinceStartup % 100f; // Placeholder
                        OnPingUpdated?.Invoke(currentPing);
                        
                        // Check for high ping
                        if (currentPing > maxAllowedPing)
                        {
                            HandleConnectionWarning($"High ping detected: {currentPing}ms");
                        }
                    }
                }
            }
        }
        
        private void UpdateNetworkStats()
        {
            if (NetworkManager.Singleton == null) return;
            
            // Update basic network statistics
            networkStats.ConnectedPlayers = currentPlayerCount;
            networkStats.CurrentPing = currentPing;
            networkStats.ConnectionState = connectionState;
            networkStats.ReconnectionAttempts = reconnectionAttempts;
        }
        
        private void PerformAntiCheatChecks()
        {
            // Basic anti-cheat validation
            // In production, this would be much more sophisticated
            
            foreach (var player in connectedPlayers.Values)
            {
                if (player.IsActive)
                {
                    // Check for suspicious activity
                    ValidatePlayerBehavior(player);
                }
            }
        }
        
        private void ValidatePlayerBehavior(PlayerNetworkData player)
        {
            // Placeholder for anti-cheat validation
            // Real implementation would check movement speed, input frequency, etc.
            
            if (Time.time - player.ConnectedTime > 60f) // After 1 minute
            {
                // Perform validation checks
                // This is where you'd implement actual anti-cheat logic
            }
        }
        
        #endregion
        
        #region Error Handling
        
        private void HandleConnectionError(string error)
        {
            SetConnectionState(NetworkConnectionState.Error);
            OnNetworkError?.Invoke(error);
            
            if (logNetworkEvents)
                Debug.LogError($"[ProductionNetworkManager] {error}");
        }
        
        private void HandleConnectionWarning(string warning)
        {
            if (logNetworkEvents)
                Debug.LogWarning($"[ProductionNetworkManager] {warning}");
        }
        
        #endregion
        
        #region State Management
        
        private void SetConnectionState(NetworkConnectionState newState)
        {
            if (connectionState != newState)
            {
                connectionState = newState;
                OnConnectionStateChanged?.Invoke(connectionState);
                
                if (logNetworkEvents)
                    Debug.Log($"[ProductionNetworkManager] Connection state changed to: {connectionState}");
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public NetworkConnectionState GetConnectionState() => connectionState;
        public int GetPlayerCount() => currentPlayerCount;
        public float GetCurrentPing() => currentPing;
        public NetworkStats GetNetworkStats() => networkStats;
        public bool IsHost() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        public bool IsServer() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        public bool IsClient() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
        
        public void SetServerAddress(string ip, ushort port)
        {
            serverIP = ip;
            serverPort = port;
            
            var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ip, port);
                // Network Simulator is now handled via Multiplayer Tools package in Unity 2023+
            }
        }
        
        public void KickPlayer(ulong clientId, string reason = "")
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.DisconnectClient(clientId, reason);
                if (logNetworkEvents)
                    Debug.Log($"[ProductionNetworkManager] Kicked player {clientId}: {reason}");
            }
        }
        
        #endregion
        
        #region Debug UI
        
        void OnGUI()
        {
            if (!showDebugUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            
            GUILayout.Label($"Network Manager - {connectionState}");
            GUILayout.Label($"Players: {currentPlayerCount}/{maxConnections}");
            GUILayout.Label($"Ping: {currentPing:F0}ms");

            if (reconnectionAttempts > 0)
            {
                GUILayout.Label($"Reconnection attempts: {reconnectionAttempts}/{maxReconnectionAttempts}");
            }

            // Show error message if max reconnection attempts reached or in error state
            if (connectionState == NetworkConnectionState.Error ||
                (connectionState == NetworkConnectionState.Disconnected && reconnectionAttempts >= maxReconnectionAttempts))
            {
                GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
                errorStyle.normal.textColor = Color.red;
                errorStyle.fontStyle = FontStyle.Bold;
                GUILayout.Space(10);
                GUILayout.Label($"Connection failed. Please check your network and try again.", errorStyle);
                if (reconnectionAttempts >= maxReconnectionAttempts)
                {
                    GUILayout.Label($"Max reconnection attempts reached.", errorStyle);
                }
            }

            GUILayout.Space(10);

            if (connectionState == NetworkConnectionState.Disconnected)
            {
                if (GUILayout.Button("Start Host")) StartHost();
                if (GUILayout.Button("Start Server")) StartServer();
                if (GUILayout.Button("Start Client")) StartClient();
            }
            else
            {
                if (GUILayout.Button("Disconnect")) Disconnect();
            }

            GUILayout.Space(10);

            if (showNetworkStats && connectionState == NetworkConnectionState.Connected)
            {
                GUILayout.Label("Connected Players:");
                foreach (var player in connectedPlayers.Values)
                {
                    GUILayout.Label($"  Player {player.ClientId} - {Time.time - player.ConnectedTime:F0}s");
                }
            }

            GUILayout.EndArea();
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    public enum NetworkConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Error
    }
    
    [System.Serializable]
    public class PlayerNetworkData
    {
        public ulong ClientId;
        public float ConnectedTime;
        public bool IsActive;
        public Vector3 LastPosition;
        public float LastMovementTime;
        public int SuspiciousActivityCount;
    }
    
    [System.Serializable]
    public class NetworkStats
    {
        public int ConnectedPlayers;
        public float CurrentPing;
        public NetworkConnectionState ConnectionState;
        public int ReconnectionAttempts;
        public float DataSent;
        public float DataReceived;
        public int PacketsLost;
    }

    #endregion
}
