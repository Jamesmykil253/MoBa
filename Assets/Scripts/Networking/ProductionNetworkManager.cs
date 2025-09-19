using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections.Generic;
using System.Collections;
using MOBA;
using MOBA.Debugging;

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
    /// Uses component-based architecture for separation of concerns
    /// </summary>
    public class ProductionNetworkManager : MonoBehaviour
    {
        public static ProductionNetworkManager Instance { get; private set; }
#if UNITY_INCLUDE_TESTS
        public static bool AllowMultipleInstancesForTesting { get; set; }
#endif

        #region Configuration
        
        [Header("Network Configuration")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        [Header("Debug")]
        [SerializeField] private bool logNetworkEvents = true;
        
        #endregion
        
        #region Network State
        
        private NetworkErrorCode lastErrorCode = NetworkErrorCode.None;
        private string lastErrorMessage = string.Empty;
        
        // Network state tracking
        private NetworkConnectionState connectionState = NetworkConnectionState.Disconnected;
        
        // Network Manager Components
        private NetworkConnectionManager connectionManager;
        private NetworkAntiCheatManager antiCheatManager;
        private NetworkStatisticsManager statisticsManager;
        private NetworkReconnectionManager reconnectionManager;
        private NetworkPlayerManager playerManager;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when connection state changes. Listeners MUST unsubscribe to prevent memory leaks.
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
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
#if UNITY_INCLUDE_TESTS
            if (AllowMultipleInstancesForTesting && Instance != null && Instance != this)
            {
                return;
            }
#endif
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
#if UNITY_INCLUDE_TESTS
            if (AllowMultipleInstancesForTesting && Instance != this)
            {
                return;
            }
#endif
            InitializeNetworking();
            
            if (autoStartAsHost)
            {
                StartHost();
            }
        }
        
        void Update()
        {
#if UNITY_INCLUDE_TESTS
            if (AllowMultipleInstancesForTesting && Instance != this)
            {
                return;
            }
#endif
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                return;
            }

            if (!networkManager.IsClient && !networkManager.IsServer)
            {
                return;
            }

            // Update statistics through the statistics manager
            statisticsManager?.UpdateStatistics();

            // Perform anti-cheat checks if server
            if (networkManager.IsServer)
            {
                PerformAntiCheatChecks();
            }
        }
        
        void OnDestroy()
        {
#if UNITY_INCLUDE_TESTS
            if (AllowMultipleInstancesForTesting && Instance != this)
            {
                return;
            }
#endif
            // Shutdown components
            connectionManager?.Shutdown();
            antiCheatManager?.Shutdown();
            statisticsManager?.Shutdown();
            playerManager?.Shutdown();
            reconnectionManager?.Shutdown();

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

            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "Shutdown"),
                    "ProductionNetworkManager shutdown complete.");
            }
        }
        
        #endregion

        #region Network Initialization
        
        private void InitializeNetworking()
        {
            if (NetworkManager.Singleton == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "NetworkManager.Singleton is null; cannot initialize networking.");
                return;
            }

            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = null;

            // Configure Unity Transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(serverIP, serverPort);
            }
            
            // Set up NetworkManager callbacks
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            
            // Initialize network manager components
            InitializeComponents();
            
            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                    "Network initialization complete.");
            }
        }

        private void InitializeComponents()
        {
            // Initialize connection manager
            connectionManager = gameObject.AddComponent<NetworkConnectionManager>();
            connectionManager.Initialize(this);

            // Initialize anti-cheat manager
            antiCheatManager = gameObject.AddComponent<NetworkAntiCheatManager>();
            antiCheatManager.Initialize(this);

            // Initialize statistics manager
            statisticsManager = gameObject.AddComponent<NetworkStatisticsManager>();
            statisticsManager.Initialize(this);
            // Subscribe to ping updates from statistics manager
            statisticsManager.OnPingUpdated += (ping) => OnPingUpdated?.Invoke(ping);
            
            // Initialize player manager
            playerManager = gameObject.AddComponent<NetworkPlayerManager>();
            playerManager.Initialize(this);
            // Subscribe to player events
            playerManager.OnPlayerConnected += (clientId) => OnPlayerConnected?.Invoke(clientId);
            playerManager.OnPlayerDisconnected += (clientId) => OnPlayerDisconnected?.Invoke(clientId);
            
            // Initialize reconnection manager
            reconnectionManager = gameObject.AddComponent<NetworkReconnectionManager>();
            reconnectionManager.Initialize(this);

            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "Components"),
                    "All network manager components initialized.");
            }
        }
        
        #endregion

        #region Connection Management (Delegated to ConnectionManager)
        
        /// <summary>
        /// Starts the network session as host (server + client).
        /// Delegates to NetworkConnectionManager for implementation.
        /// </summary>
        public void StartHost()
        {
            connectionManager?.StartHost();
        }
        
        /// <summary>
        /// Starts the network session as dedicated server.
        /// Delegates to NetworkConnectionManager for implementation.
        /// </summary>
        public void StartServer()
        {
            connectionManager?.StartServer();
        }
        
        /// <summary>
        /// Connects to a remote server as a client player.
        /// Delegates to NetworkConnectionManager for implementation.
        /// </summary>
        public void StartClient()
        {
            connectionManager?.StartClient();
        }
        
        /// <summary>
        /// Gracefully disconnects from the network session.
        /// Delegates to NetworkConnectionManager for implementation.
        /// </summary>
        public void Disconnect()
        {
            connectionManager?.Disconnect();
        }
        
        #endregion
        
        #region Connection Callbacks
        
        private void OnServerStarted()
        {
            SetConnectionState(NetworkConnectionState.Connected);
            // Note: Host player count is managed by playerManager
            
            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "ServerLifecycle"),
                    "Server started successfully.");
            }
        }

        private void OnClientStarted()
        {
            SetConnectionState(NetworkConnectionState.Connected);
            reconnectionManager?.ResetReconnectionAttempts();
            
            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "ClientLifecycle"),
                    "Client connected successfully.");
            }
        }
        
        private void OnClientConnected(ulong clientId)
        {
            // Delegate player connection handling to the player manager
            playerManager?.HandlePlayerConnected(clientId);
            
            // Add player to statistics tracking
            if (playerManager?.GetPlayerData(clientId) is var playerData && playerData != null)
            {
                statisticsManager?.AddConnectedPlayer(clientId, playerData);
            }
            
            // Start tracking player for anti-cheat
            antiCheatManager?.StartTrackingPlayer(clientId, Vector3.zero);
        }
        
        private void OnClientDisconnected(ulong clientId)
        {
            // Delegate player disconnection handling to the player manager
            playerManager?.HandlePlayerDisconnected(clientId);
            
            // Remove player from tracking systems
            statisticsManager?.RemoveConnectedPlayer(clientId);
            antiCheatManager?.StopTrackingPlayer(clientId);
            
            // Handle self disconnection - delegate to reconnection manager
            if (clientId == NetworkManager.Singleton.LocalClientId && reconnectionManager != null)
            {
                reconnectionManager.MarkDisconnectTime();
                reconnectionManager.StartReconnection("Client disconnected from server");
            }
        }
        
        #endregion
        
        #region Anti-Cheat (Delegated to AntiCheatManager)
        
        private void PerformAntiCheatChecks()
        {
            if (playerManager?.ConnectedPlayers == null) return;
            
            foreach (var kvp in playerManager.ConnectedPlayers)
            {
                var player = kvp.Value;
                if (!player.IsActive)
                {
                    continue;
                }

                ValidatePlayerBehavior(player);
            }
        }

        private void ValidatePlayerBehavior(PlayerNetworkData player)
        {
            var avatar = player.Avatar;
            if (avatar == null || !avatar)
            {
                return;
            }

            var currentPosition = avatar.transform.position;
            float deltaTime = Time.time - player.LastMovementTime;

            // Delegate to anti-cheat manager
            antiCheatManager?.ValidatePlayerBehavior(player.ClientId, currentPosition, deltaTime);
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
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Networking, subsystem: "ErrorHandling"),
                    "Network error encountered.",
                    ("Code", code),
                    ("Message", error));
            }
        }

        private void HandleConnectionWarning(string warning)
        {
            if (logNetworkEvents)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Networking, subsystem: "Warning"),
                    "Network warning issued.",
                    ("Message", warning));
            }
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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "State"),
                        "Connection state changed.",
                        ("State", connectionState.ToString()));
                }
            }
        }
        
        #endregion
        
        #region Public Interface
        
        public NetworkConnectionState GetConnectionState() => connectionState;
        public int GetPlayerCount() => playerManager?.CurrentPlayerCount ?? 0;
        public float GetCurrentPing() => statisticsManager?.GetCurrentPing() ?? 0f;
        public NetworkStats GetNetworkStats() => statisticsManager?.GetNetworkStats() ?? new NetworkStats();
        public bool IsHost() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        public bool IsServer() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        public bool IsClient() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;

        /// <summary>
        /// Spawns player avatar prefabs for all connected clients at specified spawn points.
        /// Server-only method that handles NetworkObject spawning and assignment.
        /// Delegates to NetworkPlayerManager for implementation.
        /// </summary>
        public void SpawnPlayerAvatars(GameObject playerPrefab, Transform[] spawnPoints)
        {
            playerManager?.SpawnPlayerAvatars(playerPrefab, spawnPoints);
        }

        /// <summary>
        /// Despawns and destroys all player avatars from the network session.
        /// Delegates to NetworkPlayerManager for implementation.
        /// </summary>
        public void DespawnAllPlayerAvatars()
        {
            playerManager?.DespawnAllPlayerAvatars();
        }

        /// <summary>
        /// Configures the server address and port for client connections.
        /// </summary>
        public void SetServerAddress(string ip, ushort port)
        {
            serverIP = ip;
            serverPort = port;
            
            var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ip, port);
            }
        }
        
        public void KickPlayer(ulong clientId, string reason = "")
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.DisconnectClient(clientId, reason);
                if (logNetworkEvents)
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                        "Player kicked from session.",
                        ("Reason", reason));
                }
            }
        }

        private Transform ResolveSpawnTransform(Transform[] spawnPoints, int spawnCount, int index)
        {
            if (spawnPoints == null || spawnCount == 0)
            {
                return null;
            }

            if (index < spawnCount)
            {
                return spawnPoints[index];
            }

            return spawnPoints[spawnCount - 1];
        }

        #endregion

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
        
        #region Helper Methods
        
        private GameDebugContext BuildContext(
            GameDebugMechanicTag mechanic = GameDebugMechanicTag.Networking,
            string subsystem = null,
            ulong? actorClientId = null)
        {
            string actor = actorClientId?.ToString();
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                mechanic,
                subsystem,
                actor);
        }
        
        #endregion
    }
    
    #region Supporting Types
    
    public enum NetworkConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
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
        public NetworkObject Avatar;
        public float LastSuspicionTime;
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