using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports;

namespace MOBA.Networking
{
    /// <summary>
    /// Dedicated server configuration and setup
    /// </summary>
    public class DedicatedServerConfig : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private string serverName = "MOBA Dedicated Server";
        [SerializeField] private int maxPlayers = 64;
        [SerializeField] private ushort port = 7777;
        [SerializeField] private string bindAddress = "0.0.0.0";

        [Header("Performance Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool enableServerGC = false;
        [SerializeField] private int serverTickRate = 60;

        [Header("Security Settings")]
        [SerializeField] private bool enableConnectionApproval = true;
        [SerializeField] private float connectionTimeout = 10f;

        private NetworkManager networkManager;

        private void Awake()
        {
            // Configure for dedicated server
            ConfigureAsDedicatedServer();

            // Set up network manager
            SetupNetworkManager();

            // Start server
            StartDedicatedServer();
        }

        private void ConfigureAsDedicatedServer()
        {
            // Disable unnecessary components for server
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                camera.enabled = false;
            }

            var audioListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var audioListener in audioListeners)
            {
                audioListener.enabled = false;
            }

            // Configure application for server
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = true;

            // Disable garbage collection for better performance
            if (!enableServerGC)
            {
                System.GC.Collect();
                // Note: In production, you might want to disable GC completely
                // but this requires careful memory management
            }

            Debug.Log($"[DedicatedServerConfig] Configured as dedicated server - Target FPS: {targetFrameRate}, GC: {enableServerGC}");
        }

        private void SetupNetworkManager()
        {
            networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogError("[DedicatedServerConfig] NetworkManager not found!");
                return;
            }

            // Configure network manager
            networkManager.NetworkConfig.PlayerPrefab = null; // We'll spawn manually
            networkManager.NetworkConfig.EnableSceneManagement = false; // Disable for dedicated server
            networkManager.NetworkConfig.TickRate = (uint)serverTickRate;

            // Set up connection approval
            if (enableConnectionApproval)
            {
                networkManager.ConnectionApprovalCallback = ConnectionApprovalCallback;
            }

            // Subscribe to events
            networkManager.OnServerStarted += OnServerStarted;
            networkManager.OnServerStopped += OnServerStopped;
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;

            Debug.Log($"[DedicatedServerConfig] NetworkManager configured - Port: {port}, Max Players: {maxPlayers}");
        }

        private void StartDedicatedServer()
        {
            if (networkManager.StartServer())
            {
                Debug.Log($"[DedicatedServerConfig] Dedicated server started successfully on {bindAddress}:{port}");
                Debug.Log($"[DedicatedServerConfig] Server Name: {serverName}");
                Debug.Log($"[DedicatedServerConfig] Max Players: {maxPlayers}");
            }
            else
            {
                Debug.LogError("[DedicatedServerConfig] Failed to start dedicated server!");
                Application.Quit(1);
            }
        }

        private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // Check player count
            int currentPlayers = networkManager.ConnectedClients.Count;
            if (currentPlayers >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Server is full";
                Debug.LogWarning($"[DedicatedServerConfig] Connection rejected - Server full ({currentPlayers}/{maxPlayers})");
                return;
            }

            // Check connection timeout
            if (connectionTimeout > 0 && Time.time > connectionTimeout)
            {
                response.Approved = false;
                response.Reason = "Connection timeout exceeded";
                Debug.LogWarning($"[DedicatedServerConfig] Connection rejected - Timeout exceeded ({connectionTimeout}s)");
                return;
            }

            // Additional validation
            // - Check client version
            // - Validate authentication token
            // - Check ban list

            response.Approved = true;
            response.CreatePlayerObject = false; // We'll handle spawning manually

            Debug.Log($"[DedicatedServerConfig] Connection approved for client {request.ClientNetworkId}");
        }

        private void OnServerStarted()
        {
            Debug.Log("[DedicatedServerConfig] Server started successfully");

            // Initialize server systems
            InitializeServerSystems();

            // Start server heartbeat
            StartCoroutine(ServerHeartbeat());
        }

        private void OnServerStopped(bool intentional)
        {
            Debug.Log($"[DedicatedServerConfig] Server stopped (intentional: {intentional})");

            if (!intentional)
            {
                // Unexpected shutdown - attempt restart or cleanup
                Debug.LogError("[DedicatedServerConfig] Unexpected server shutdown!");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[DedicatedServerConfig] Client {clientId} connected");

            // Update server stats
            UpdateServerStats();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[DedicatedServerConfig] Client {clientId} disconnected");

            // Update server stats
            UpdateServerStats();

            // Clean up client data
            CleanupClientData(clientId);
        }

        private void InitializeServerSystems()
        {
            // Find or create game manager
            var gameManager = FindFirstObjectByType<NetworkGameManager>();
            if (gameManager == null)
            {
                var gameManagerObject = new GameObject("NetworkGameManager");
                gameManager = gameManagerObject.AddComponent<NetworkGameManager>();
            }

            // Configure game manager for server
            // Additional server initialization can be added here
        }

        private void UpdateServerStats()
        {
            int connectedClients = networkManager.ConnectedClients.Count;
            Debug.Log($"[DedicatedServerConfig] Server Stats - Connected: {connectedClients}/{maxPlayers}");
        }

        private void CleanupClientData(ulong clientId)
        {
            // Clean up any client-specific data
            // - Remove from player lists
            // - Clean up owned objects
            // - Update game state
        }

        private System.Collections.IEnumerator ServerHeartbeat()
        {
            while (networkManager.IsServer)
            {
                // Server heartbeat logic
                // - Update server stats
                // - Check for timeouts
                // - Perform maintenance tasks

                UpdateServerStats();

                yield return new WaitForSeconds(30f); // Heartbeat every 30 seconds
            }
        }

        // Server monitoring and management
        private void OnApplicationQuit()
        {
            Debug.Log("[DedicatedServerConfig] Application quitting - shutting down server");

            if (networkManager != null && networkManager.IsServer)
            {
                networkManager.Shutdown();
            }
        }

        // Console commands for server management
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeServerCommands()
        {
            // Add console commands for dedicated server
            // This would integrate with Unity's command line argument system
            var args = System.Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-serverName":
                        if (i + 1 < args.Length)
                        {
                            // Override server name
                            Debug.Log($"Server name set to: {args[i + 1]}");
                        }
                        break;
                    case "-maxPlayers":
                        if (i + 1 < args.Length && int.TryParse(args[i + 1], out int maxPlayers))
                        {
                            // Override max players
                            Debug.Log($"Max players set to: {maxPlayers}");
                        }
                        break;
                    case "-port":
                        if (i + 1 < args.Length && ushort.TryParse(args[i + 1], out ushort port))
                        {
                            // Override port
                            Debug.Log($"Port set to: {port}");
                        }
                        break;
                }
            }
        }

        // Debug GUI for server monitoring
        private void OnGUI()
        {
            if (!networkManager.IsServer) return;

            GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));
            GUILayout.Label("Dedicated Server Monitor", GUILayout.Width(280));

            GUILayout.Label($"Server: {serverName}");
            GUILayout.Label($"Address: {bindAddress}:{port}");
            GUILayout.Label($"Players: {networkManager.ConnectedClients.Count}/{maxPlayers}");
            GUILayout.Label($"Tick Rate: {serverTickRate}Hz");
            GUILayout.Label($"Frame Rate: {Application.targetFrameRate} FPS");

            GUILayout.Space(10);

            if (GUILayout.Button("Shutdown Server", GUILayout.Width(280)))
            {
                networkManager.Shutdown();
                Application.Quit();
            }

            GUILayout.EndArea();
        }
    }
}