using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace MOBA.Networking
{
    /// <summary>
    /// Master integration script for all networking systems
    /// Initializes and coordinates all network components for production-ready MOBA
    /// </summary>
    public class NetworkSystemIntegration : MonoBehaviour
    {
    [Header("Core Network Components")]
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private NetworkGameManager gameManager;        [Header("System Managers")]
        [SerializeField] private NetworkEventBus eventBus;
        [SerializeField] private NetworkObjectPoolManager poolManager;
        [SerializeField] private NetworkPoolObjectManager componentPoolManager;
        [SerializeField] private LagCompensationManager lagManager;
        [SerializeField] private AntiCheatSystem antiCheat;
        [SerializeField] private NetworkProfiler profiler;

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Configuration")]
        [SerializeField] private bool enableAntiCheat = true;
        [SerializeField] private bool enableLagCompensation = true;
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private int maxPlayers = 64;

        private void Awake()
        {
            InitializeNetworkSystems();
        }

        private void InitializeNetworkSystems()
        {
            Debug.Log("[NETWORK] Starting system initialization...");
            
            // 1. Initialize Network Manager
            if (networkManager == null)
            {
                networkManager = NetworkManager.Singleton;
                if (networkManager == null)
                {
                    Debug.LogError("[NETWORK] ❌ NetworkManager not found!");
                    return;
                }
            }

                        // 2. Initialize Event Bus (Observer Pattern)
            if (eventBus == null)
            {
                eventBus = NetworkEventBus.Instance;
            }

            // 3. Initialize Object Pool Manager (Object Pool Pattern)
            if (componentPoolManager != null)
            {
                // Using component-based pool manager
            }
            else if (poolManager == null)
            {
                poolManager = NetworkObjectPoolManager.Instance;
                if (poolManager == null)
                {
                    Debug.LogError("[NETWORK] ❌ Failed to create NetworkObjectPoolManager instance!");
                }
            }

            // 4. Initialize Lag Compensation Manager
            if (lagManager == null && enableLagCompensation)
            {
                lagManager = LagCompensationManager.Instance;
            }

            // 5. Initialize Anti-Cheat System
            if (antiCheat == null && enableAntiCheat)
            {
                antiCheat = AntiCheatSystem.Instance;
            }

            // 6. Initialize Network Profiler
            if (profiler == null && enableProfiling)
            {
                profiler = FindFirstObjectByType<NetworkProfiler>();
            }
            if (antiCheat == null && enableAntiCheat)
            {
                antiCheat = AntiCheatSystem.Instance;
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ Anti-Cheat System initialized");
            }
            else if (!enableAntiCheat)
            {
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ⚠️ Anti-Cheat disabled");
            }
            else
            {
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ Anti-Cheat System already assigned");
            }

            // 6. Initialize Network Profiler
            UnityEngine.Debug.Log($"[NetworkSystemIntegration] Step 6: Initializing Network Profiler (enabled: {enableProfiling})...");
            if (profiler == null && enableProfiling)
            {
                profiler = gameObject.AddComponent<NetworkProfiler>();
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ Network Profiler component added");
            }
            else if (!enableProfiling)
            {
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ⚠️ Network Profiling disabled");
            }
            else
            {
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ Network Profiler already assigned");
            }

            // 7. Configure Network Manager
            UnityEngine.Debug.Log("[NetworkSystemIntegration] Step 7: Configuring Network Manager...");
            ConfigureNetworkManager();

            // 8. Set up object pools
            UnityEngine.Debug.Log("[NetworkSystemIntegration] Step 8: Setting up Object Pools...");
            SetupObjectPools();

            UnityEngine.Debug.Log("[NetworkSystemIntegration] === NETWORK SYSTEM INITIALIZATION COMPLETE ===");
        }

        private void ConfigureNetworkManager()
        {
            UnityEngine.Debug.Log("[NetworkSystemIntegration] Configuring Network Manager...");
            if (networkManager == null) 
            {
                UnityEngine.Debug.LogError("[NetworkSystemIntegration] ❌ Cannot configure NetworkManager - it's null!");
                return;
            }

            // Configure network settings
            UnityEngine.Debug.Log($"[NetworkSystemIntegration] Setting PlayerPrefab: {(playerPrefab != null ? playerPrefab.name : "NULL")}");
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.EnableSceneManagement = false;
            networkManager.NetworkConfig.TickRate = 60;

            // Set up connection approval
            networkManager.ConnectionApprovalCallback = OnConnectionApproval;

            // Subscribe to network events
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;

            UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ NetworkManager configuration complete");
        }

        private void SetupObjectPools()
        {
            // Determine which pool manager to use
            bool usingComponentManager = componentPoolManager != null;
            bool hasPoolManager = poolManager != null || componentPoolManager != null;
            
            if (!hasPoolManager) 
            {
                UnityEngine.Debug.LogError("[NetworkSystemIntegration] ❌ No pool manager available, skipping object pool setup");
                return;
            }

            // Create projectile pool only if prefab is assigned
            if (projectilePrefab != null)
            {
                
                if (usingComponentManager)
                {
                    componentPoolManager.CreatePool("Projectiles", projectilePrefab, 20, 100);
                }
                else
                {
                    poolManager.CreatePool("Projectiles", projectilePrefab, 20, 100);
                }
                
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ Projectile pool created successfully");
            }
            else
            {
                UnityEngine.Debug.LogError("[NetworkSystemIntegration] ❌ ProjectilePrefab is null, skipping projectile pool creation");
            }

            // Create player pool only if prefab is assigned
            if (playerPrefab != null)
            {
                
                if (usingComponentManager)
                {
                    componentPoolManager.CreatePool("Players", playerPrefab, maxPlayers, maxPlayers);
                }
                else
                {
                    poolManager.CreatePool("Players", playerPrefab, maxPlayers, maxPlayers);
                }
                
                UnityEngine.Debug.Log("[NetworkSystemIntegration] ✅ Player pool created successfully");
            }
            else
            {
                UnityEngine.Debug.LogError("[NetworkSystemIntegration] ❌ PlayerPrefab is null, skipping player pool creation");
            }
        }

        private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // Basic connection validation
            int currentPlayers = networkManager.ConnectedClients.Count;

            if (currentPlayers >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Server is full";
                UnityEngine.Debug.LogWarning($"[NetworkSystemIntegration] Connection rejected - Server full ({currentPlayers}/{maxPlayers})");
                return;
            }

            // Anti-cheat validation could be added here
            if (enableAntiCheat && antiCheat != null)
            {
                // Additional client validation
            }

            response.Approved = true;
            response.CreatePlayerObject = true;

            UnityEngine.Debug.Log($"[NetworkSystemIntegration] Connection approved for client {request.ClientNetworkId}");
        }

        private void OnServerStarted()
        {
            UnityEngine.Debug.Log("[NetworkSystemIntegration] Server started successfully");

            // Initialize game manager
            if (gameManager == null)
            {
                var gmObject = new GameObject("NetworkGameManager");
                gameManager = gmObject.AddComponent<NetworkGameManager>();
            }

            // Publish server start event
            eventBus?.PublishGameStarted();
        }

        private void OnClientConnected(ulong clientId)
        {
            UnityEngine.Debug.Log($"[NetworkSystemIntegration] Client {clientId} connected");

            // Publish connection event
            eventBus?.PublishClientConnected(clientId);

            // Update lag compensation with client latency
            if (lagManager != null)
            {
                lagManager.UpdateClientLatency(clientId, 0.1f); // Default latency
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            UnityEngine.Debug.Log($"[NetworkSystemIntegration] Client {clientId} disconnected");

            // Publish disconnection event
            eventBus?.PublishClientDisconnected(clientId);

            // Clean up client data
            if (antiCheat != null)
            {
                // Clear anti-cheat data for client
            }
        }

        /// <summary>
        /// Get comprehensive system status
        /// </summary>
        public NetworkSystemStatus GetSystemStatus()
        {
            // Get pool stats from whichever manager is available
            Dictionary<string, (int, int, int)> poolStats = new Dictionary<string, (int, int, int)>();
            if (componentPoolManager != null)
            {
                poolStats = componentPoolManager.GetPoolStats();
            }
            else if (poolManager != null)
            {
                poolStats = poolManager.GetAllPoolStats();
            }
            
            return new NetworkSystemStatus
            {
                isNetworkManagerActive = networkManager != null && networkManager.IsListening,
                isServer = networkManager != null && networkManager.IsServer,
                isClient = networkManager != null && networkManager.IsClient,
                connectedPlayers = gameManager?.ConnectedPlayers ?? 0,
                maxPlayers = maxPlayers,
                antiCheatEnabled = enableAntiCheat && antiCheat != null,
                lagCompensationEnabled = enableLagCompensation && lagManager != null,
                profilingEnabled = enableProfiling && profiler != null,
                poolStats = poolStats,
                eventBusActive = eventBus != null
            };
        }

        /// <summary>
        /// Start server with all systems
        /// </summary>
        public void StartServer()
        {
            if (networkManager != null && !networkManager.IsListening)
            {
                networkManager.StartServer();
            }
        }

        /// <summary>
        /// Start client with all systems
        /// </summary>
        public void StartClient(string address = "127.0.0.1", ushort port = 7777)
        {
            if (networkManager != null && !networkManager.IsListening)
            {
                // Note: Transport configuration should be set in NetworkManager component in Inspector
                // The transport component may not be available in this version of Netcode
                networkManager.StartClient();
            }
        }

        /// <summary>
        /// Stop all network systems
        /// </summary>
        public void StopNetwork()
        {
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            if (networkManager != null)
            {
                networkManager.OnServerStarted -= OnServerStarted;
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        // Debug GUI for system monitoring
        private void OnGUI()
        {
            if (!Application.isEditor) return;

            var status = GetSystemStatus();

            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 400));
            GUILayout.Label("Network System Integration", GUILayout.Width(300));

            GUILayout.Label($"Network Active: {status.isNetworkManagerActive}");
            GUILayout.Label($"Server: {status.isServer}, Client: {status.isClient}");
            GUILayout.Label($"Players: {status.connectedPlayers}/{status.maxPlayers}");

            GUILayout.Space(10);
            GUILayout.Label("Systems Status:");
            GUILayout.Label($"Anti-Cheat: {status.antiCheatEnabled}");
            GUILayout.Label($"Lag Comp: {status.lagCompensationEnabled}");
            GUILayout.Label($"Profiling: {status.profilingEnabled}");
            GUILayout.Label($"Event Bus: {status.eventBusActive}");

            GUILayout.Space(10);
            if (status.poolStats.Count > 0)
            {
                GUILayout.Label("Object Pools:");
                foreach (var kvp in status.poolStats)
                {
                    var (available, active, total) = kvp.Value;
                    GUILayout.Label($"  {kvp.Key}: {available}/{active}/{total}");
                }
            }

            GUILayout.EndArea();
        }
    }

    [System.Serializable]
    public struct NetworkSystemStatus
    {
        public bool isNetworkManagerActive;
        public bool isServer;
        public bool isClient;
        public int connectedPlayers;
        public int maxPlayers;
        public bool antiCheatEnabled;
        public bool lagCompensationEnabled;
        public bool profilingEnabled;
        public bool eventBusActive;
        public System.Collections.Generic.Dictionary<string, (int available, int active, int total)> poolStats;
    }
}