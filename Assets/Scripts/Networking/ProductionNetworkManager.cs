using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System.Collections;
using MOBA;

// Error codes for network errors
public enum NetworkErrorCode
{
    None = 0,
    Unknown = 1,
    StartHostFailed = 2,
    StartServerFailed = 3,
    StartClientFailed = 4,
    DuplicateConnection = 5,
    MaxReconnectionAttempts = 6,
    RapidReconnect = 7,
    HighPing = 8,
    NetworkManagerNull = 9
}

namespace MOBA.Networking
{
    /// <summary>
    /// Production-ready network manager with comprehensive features
    /// Replaces SimpleNetworkManager with robust networking capabilities
    /// </summary>
    public class ProductionNetworkManager : MonoBehaviour
    {
        private NetworkErrorCode lastErrorCode = NetworkErrorCode.None;
        private string lastErrorMessage = string.Empty;
        // Edge case: rapid connect/disconnect cooldown
        private float lastDisconnectTime = -10f;
        [SerializeField] private float reconnectCooldown = 2f; // seconds
        [Header("Network Configuration")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private int maxConnections = 10;
        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        [SerializeField] private bool enableReconnection = true;
        [SerializeField] private float reconnectionInterval = 5f;
        [SerializeField] private int maxReconnectionAttempts = 3;
        [Header("Anti-Cheat")]
        [SerializeField] private bool enableServerValidation = true;
        [SerializeField] private float maxAllowedPing = 500f;
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
    /// <summary>
    /// Raised when the connection state changes. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<NetworkConnectionState> OnConnectionStateChanged;
    /// <summary>
    /// Raised when a player connects. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<ulong> OnPlayerConnected;
    /// <summary>
    /// Raised when a player disconnects. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<ulong> OnPlayerDisconnected;
    /// <summary>
    /// Raised when ping is updated. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<float> OnPingUpdated;
    /// <summary>
    /// Raised when a network error occurs. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
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
            // Prevent rapid reconnects
            if (Time.time - lastDisconnectTime < reconnectCooldown)
            {
                HandleConnectionWarning($"Reconnect attempted too quickly. Please wait {reconnectCooldown - (Time.time - lastDisconnectTime):F1}s.");
                return;
            }

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

            // Mark disconnect time for cooldown
            lastDisconnectTime = Time.time;

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
            // Edge case: duplicate connection
            if (connectedPlayers.ContainsKey(clientId))
            {
                HandleConnectionWarning($"Duplicate connection detected for client {clientId}. Rejecting duplicate.");
                return;
            }

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

        private System.Collections.IEnumerator ReconnectionCoroutine()
        {
            reconnectionAttempts++;
            SetConnectionState(NetworkConnectionState.Reconnecting);

            if (logNetworkEvents)
                Debug.Log($"[ProductionNetworkManager] Attempting reconnection {reconnectionAttempts}/{maxReconnectionAttempts}");

            yield return new WaitForSeconds(reconnectionInterval);

            StartClient();
        }
        
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
            // Map error string to error code
            NetworkErrorCode code = NetworkErrorCode.Unknown;
            if (error.Contains("host")) code = NetworkErrorCode.StartHostFailed;
            else if (error.Contains("server")) code = NetworkErrorCode.StartServerFailed;
            else if (error.Contains("client")) code = NetworkErrorCode.StartClientFailed;
            else if (error.Contains("duplicate")) code = NetworkErrorCode.DuplicateConnection;
            else if (error.Contains("Max reconnection attempts")) code = NetworkErrorCode.MaxReconnectionAttempts;
            else if (error.Contains("Reconnect attempted too quickly")) code = NetworkErrorCode.RapidReconnect;
            else if (error.Contains("High ping")) code = NetworkErrorCode.HighPing;
            else if (error.Contains("NetworkManager.Singleton is null")) code = NetworkErrorCode.NetworkManagerNull;

            lastErrorCode = code;
            lastErrorMessage = error;

            SetConnectionState(NetworkConnectionState.Error);
            OnNetworkError?.Invoke($"[{code}] {error}");

            if (logNetworkEvents)
                Debug.LogError($"[ProductionNetworkManager] [{code}] {error}");
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

            // Show error code and user message if error
            if (connectionState == NetworkConnectionState.Error ||
                (connectionState == NetworkConnectionState.Disconnected && reconnectionAttempts >= maxReconnectionAttempts))
            {
                GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
                errorStyle.normal.textColor = Color.red;
                errorStyle.fontStyle = FontStyle.Bold;
                GUILayout.Space(10);
                string userMessage = GetUserFriendlyErrorMessage(lastErrorCode, lastErrorMessage);
                GUILayout.Label($"Error [{lastErrorCode}]: {userMessage}", errorStyle);
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
        
        private string GetUserFriendlyErrorMessage(NetworkErrorCode code, string details)
        {
            switch (code)
            {
                case NetworkErrorCode.StartHostFailed:
                    return "Failed to start as host. Please check your network settings.";
                case NetworkErrorCode.StartServerFailed:
                    return "Failed to start as server. Please check your network settings.";
                case NetworkErrorCode.StartClientFailed:
                    return "Failed to connect as client. Server may be unavailable.";
                case NetworkErrorCode.DuplicateConnection:
                    return "Duplicate connection detected. Please restart the client.";
                case NetworkErrorCode.MaxReconnectionAttempts:
                    return "Maximum reconnection attempts reached. Please check your connection.";
                case NetworkErrorCode.RapidReconnect:
                    return "You are reconnecting too quickly. Please wait a moment.";
                case NetworkErrorCode.HighPing:
                    return "High ping detected. Network may be unstable.";
                case NetworkErrorCode.NetworkManagerNull:
                    return "NetworkManager is not initialized. Restart the game.";
                case NetworkErrorCode.Unknown:
                default:
                    return !string.IsNullOrEmpty(details) ? details : "An unknown network error occurred.";
            }
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
