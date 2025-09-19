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
    /// </summary>
    public class ProductionNetworkManager : MonoBehaviour
    {
        public static ProductionNetworkManager Instance { get; private set; }
#if UNITY_INCLUDE_TESTS
        public static bool AllowMultipleInstancesForTesting { get; set; }
#endif

        private NetworkErrorCode lastErrorCode = NetworkErrorCode.None;
        private string lastErrorMessage = string.Empty;
        // Edge case: rapid connect/disconnect cooldown
        private float lastDisconnectTime = -10f;
        [SerializeField] private float reconnectCooldown = 2f; // seconds
        [Header("Network Configuration")]
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        [SerializeField] private bool enableReconnection = true;
        [SerializeField] private float reconnectionInterval = 5f;
        [SerializeField] private int maxReconnectionAttempts = 3;
        [Header("Anti-Cheat")]
        [SerializeField] private bool enableServerValidation = true;
        [SerializeField] private float maxAllowedPing = 500f;
        [SerializeField, Tooltip("Maximum movement speed (m/s) tolerated server-side before flagging a player.")]
        private float maxServerMovementSpeed = 18f;
        [SerializeField, Tooltip("Maximum instantaneous displacement permitted between validation samples.")]
        private float maxServerTeleportDistance = 25f;
        [SerializeField, Tooltip("How many suspicious events are tolerated before kicking a client.")]
        private int maxSuspicionBeforeKick = 3;
        [SerializeField, Tooltip("Seconds after which a suspicion stack decays by one.")]
        private float suspicionDecayInterval = 45f;
        [Header("Debug")]
        [SerializeField] private bool logNetworkEvents = true;
        // Network state tracking
        private NetworkConnectionState connectionState = NetworkConnectionState.Disconnected;
        private float lastPingTime = 0f;
        private float currentPing = 0f;
        private int reconnectionAttempts = 0;
        private Coroutine reconnectionCoroutine;
        
        // Player management
        private Dictionary<ulong, PlayerNetworkData> connectedPlayers = new Dictionary<ulong, PlayerNetworkData>();
        private int currentPlayerCount = 0;
        private readonly Dictionary<ulong, NetworkObject> playerAvatars = new Dictionary<ulong, NetworkObject>();
        
        // Network statistics
        private NetworkStats networkStats = new NetworkStats();

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

        private GameDebugContext BuildContext(
            GameDebugMechanicTag mechanic = GameDebugMechanicTag.Networking,
            string subsystem = null,
            ulong? actorClientId = null)
        {
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                mechanic,
                subsystem: subsystem ?? nameof(ProductionNetworkManager),
                actor: actorClientId.HasValue ? $"Client:{actorClientId.Value}" : null);
        }
        
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

            UpdatePing();
            UpdateNetworkStats();

            if (networkManager.IsServer && enableServerValidation)
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

            if (Instance == this)
            {
                Instance = null;
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
                // Network Simulator is now handled via Multiplayer Tools package in Unity 2023+
            }
            
            // Set up NetworkManager callbacks
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
            NetworkManager.Singleton.OnClientStarted += OnClientStarted;
            
            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                    "Network initialization complete.");
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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "StartHost"),
                        "Starting network session as host.");
                }
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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "StartServer"),
                        "Starting network session as server.");
                }
            }
        }
        
        public void StartClient()
        {
            if (NetworkManager.Singleton == null) return;
            // Prevent rapid reconnects
            if (Time.time - lastDisconnectTime < reconnectCooldown)
            {
                float remaining = Mathf.Max(0f, reconnectCooldown - (Time.time - lastDisconnectTime));
                string warning = $"Reconnect attempted too quickly. Please wait {remaining:F1}s.";
                HandleConnectionWarning(warning);
                lastErrorCode = NetworkErrorCode.RapidReconnect;
                lastErrorMessage = warning;
                SetConnectionState(NetworkConnectionState.Error);
                OnNetworkError?.Invoke($"[{NetworkErrorCode.RapidReconnect}] {warning}");
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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "StartClient"),
                        "Connecting to server as client.",
                        ("ServerIP", serverIP),
                        ("Port", serverPort));
                }
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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "Disconnect"),
                        "Disconnected from network session.");
                }
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
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "ServerLifecycle"),
                    "Server started successfully.");
            }
        }

        private void OnClientStarted()
        {
            SetConnectionState(NetworkConnectionState.Connected);
            reconnectionAttempts = 0;
            
            if (logNetworkEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "ClientLifecycle"),
                    "Client connected successfully.");
            }
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
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                    "Player connected to session.",
                    ("ConnectedPlayers", currentPlayerCount));
            }

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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                        "Player disconnected from session.",
                        ("ConnectedPlayers", currentPlayerCount));
                }
                        
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
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Recovery, subsystem: "Reconnection"),
                    "Attempting reconnection.",
                    ("Attempt", reconnectionAttempts),
                    ("MaxAttempts", maxReconnectionAttempts));
            }

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
            if (Time.time - lastPingTime <= 1f)
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

            int measuredRtt = -1;

            if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                ulong serverClientId = NetworkManager.ServerClientId;
                ulong rawRtt = transport.GetCurrentRtt(serverClientId);
                if (rawRtt > int.MaxValue)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Networking, subsystem: "Ping"),
                        "Server RTT exceeds int range.",
                        ("Rtt", rawRtt));
                    return;
                }

                measuredRtt = (int)rawRtt;
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                if (NetworkManager.Singleton.IsClient)
                {
                    // Host: treat self ping as zero for UI purposes
                    measuredRtt = 0;
                }
                else
                {
                    // Dedicated server: average connected clients that report valid RTTs
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
                            GameDebug.LogWarning(
                                BuildContext(GameDebugMechanicTag.Networking, subsystem: "Ping"),
                                "Client RTT exceeds int range.",
                                ("ClientId", clientId),
                                ("Rtt", rawClientRtt));
                            continue;
                        }

                        int clientRtt = (int)rawClientRtt;
                        if (clientRtt >= 0)
                        {
                            totalRtt += clientRtt;
                            sampleCount++;
                        }
                    }

                    measuredRtt = sampleCount > 0 ? Mathf.RoundToInt(totalRtt / sampleCount) : 0;
                }
            }

            if (measuredRtt < 0)
            {
                return;
            }

            currentPing = measuredRtt;
            OnPingUpdated?.Invoke(currentPing);

            if (currentPing > maxAllowedPing)
            {
                HandleConnectionWarning($"High ping detected: {currentPing}ms");
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
            foreach (var kvp in connectedPlayers)
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

            var transform = avatar.transform;
            Vector3 currentPosition = transform.position;
            float now = Time.time;

            // First sample initialisation
            if (player.LastMovementTime <= 0f)
            {
                player.LastPosition = currentPosition;
                player.LastMovementTime = now;
                return;
            }

            float deltaTime = now - player.LastMovementTime;
            if (deltaTime <= Mathf.Epsilon)
            {
                return;
            }

            float distance = Vector3.Distance(currentPosition, player.LastPosition);
            float speed = distance / deltaTime;

            bool flagged = false;

            if (speed > maxServerMovementSpeed)
            {
                flagged = true;
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Speed" , actorClientId: player.ClientId),
                    "Player speed exceeded server threshold.",
                    ("Speed", speed),
                    ("Threshold", maxServerMovementSpeed));
            }

            if (distance > maxServerTeleportDistance)
            {
                flagged = true;
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Teleport", actorClientId: player.ClientId),
                    "Player teleported suspiciously.",
                    ("Distance", distance),
                    ("Threshold", maxServerTeleportDistance));
            }

            if (flagged)
            {
                IncrementSuspicion(player, "Movement anomaly");
            }
            else if (player.SuspiciousActivityCount > 0 && now - player.LastSuspicionTime >= suspicionDecayInterval)
            {
                player.SuspiciousActivityCount = Mathf.Max(0, player.SuspiciousActivityCount - 1);
                player.LastSuspicionTime = now;
            }

            player.LastPosition = currentPosition;
            player.LastMovementTime = now;
        }

        private void IncrementSuspicion(PlayerNetworkData player, string reason)
        {
            player.SuspiciousActivityCount++;
            player.LastSuspicionTime = Time.time;

            if (player.SuspiciousActivityCount >= maxSuspicionBeforeKick)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Enforcement", actorClientId: player.ClientId),
                    "Player exceeded suspicion limit; kicking.",
                    ("Reason", reason),
                    ("Suspicion", player.SuspiciousActivityCount));

                KickPlayer(player.ClientId, $"Anti-cheat triggered: {reason}");
            }
            else
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.AntiCheat, subsystem: "Warning", actorClientId: player.ClientId),
                    "Player flagged by anti-cheat.",
                    ("Reason", reason),
                    ("Suspicion", player.SuspiciousActivityCount),
                    ("Threshold", maxSuspicionBeforeKick));
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
                        ("State", connectionState));
                }
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

        public void SpawnPlayerAvatars(GameObject playerPrefab, Transform[] spawnPoints)
        {
            if (!IsServer() || NetworkManager.Singleton == null)
            {
                return;
            }

            if (playerPrefab == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Spawning),
                    "Cannot spawn player avatars; player prefab is null.");
                return;
            }

            var clientIds = NetworkManager.Singleton.ConnectedClientsIds;
            int spawnCount = spawnPoints != null ? spawnPoints.Length : 0;
            int spawnIndex = 0;

            foreach (var clientId in clientIds)
            {
                if (!connectedPlayers.TryGetValue(clientId, out var playerData))
                {
                    continue;
                }

                if (playerAvatars.TryGetValue(clientId, out var existingAvatar) && existingAvatar != null && existingAvatar.IsSpawned)
                {
                    spawnIndex++;
                    continue;
                }

                var spawnTransform = ResolveSpawnTransform(spawnPoints, spawnCount, spawnIndex);
                Vector3 position = spawnTransform != null ? spawnTransform.position : Vector3.zero;
                Quaternion rotation = spawnTransform != null ? spawnTransform.rotation : Quaternion.identity;

                var instance = Instantiate(playerPrefab, position, rotation);
                var networkObject = instance.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    GameDebug.LogError(BuildContext(GameDebugMechanicTag.Spawning),
                        "Player prefab missing NetworkObject component.",
                        ("Prefab", playerPrefab.name));
                    Destroy(instance);
                    continue;
                }

                networkObject.SpawnAsPlayerObject(clientId, true);
                playerAvatars[clientId] = networkObject;
                playerData.Avatar = networkObject;
                spawnIndex++;
            }
        }

        public void DespawnAllPlayerAvatars()
        {
            if (!IsServer() || NetworkManager.Singleton == null)
            {
                return;
            }

            var keys = new List<ulong>(playerAvatars.Keys);
            foreach (var clientId in keys)
            {
                DespawnPlayerAvatar(clientId);
            }
        }

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
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                        "Player kicked from session.",
                        ("Reason", reason));
                }
            }
        }

        private void DespawnPlayerAvatar(ulong clientId)
        {
            if (playerAvatars.TryGetValue(clientId, out var avatar) && avatar != null)
            {
                if (avatar.IsSpawned)
                {
                    avatar.Despawn(true);
                }

                Destroy(avatar.gameObject);
            }

            playerAvatars.Remove(clientId);

            if (connectedPlayers.TryGetValue(clientId, out var data))
            {
                data.Avatar = null;
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
