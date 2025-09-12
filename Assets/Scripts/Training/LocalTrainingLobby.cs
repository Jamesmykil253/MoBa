using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using MOBA.Networking;
using System.Collections;

namespace MOBA.Training
{
    /// <summary>
    /// Local training lobby system for single-player practice
    /// Automatically creates and enters a local-only lobby for training purposes
    /// </summary>
    public class LocalTrainingLobby : MonoBehaviour
    {
        [Header("Training Settings")]
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool enableDebugUI = true;
        [SerializeField] private float autoStartDelay = 1f;
        
        [Header("Local Server Configuration")]
        [SerializeField] private ushort localPort = 7777;
        [SerializeField] private string localAddress = "127.0.0.1";
        [SerializeField] private int maxTrainingPlayers = 1;
        
        [Header("Training Environment")]
        [SerializeField] private GameObject trainingPlayerPrefab;
        [SerializeField] private Transform[] trainingSpawnPoints;
        [SerializeField] private bool spawnPlayerImmediately = true;
        
        // Components
        private NetworkManager networkManager;
        private NetworkSystemIntegration networkIntegration;
        
        // State
        private bool isTrainingActive = false;
        private bool isInitialized = false;
        private LobbyState currentState = LobbyState.Disconnected;
        
        /// <summary>
        /// Current state of the training lobby
        /// </summary>
        public LobbyState CurrentState => currentState;
        
        public enum LobbyState
        {
            Disconnected,
            Initializing,
            StartingLocalServer,
            ConnectingAsClient,
            Connected,
            InTraining,
            Error
        }
        
        // Events
        public System.Action<LobbyState> OnStateChanged;
        public System.Action OnTrainingStarted;
        public System.Action OnTrainingEnded;
        
        private void Awake()
        {
            if (autoStartOnAwake)
            {
                StartCoroutine(InitializeAndStartTraining());
            }
        }
        
        /// <summary>
        /// Initialize and start local training lobby
        /// </summary>
        public void StartTrainingLobby()
        {
            if (isTrainingActive)
            {
                Debug.LogWarning("[LocalTrainingLobby] Training already active");
                return;
            }
            
            StartCoroutine(InitializeAndStartTraining());
        }
        
        /// <summary>
        /// Stop training and disconnect
        /// </summary>
        public void StopTrainingLobby()
        {
            if (!isTrainingActive || !isInitialized)
            {
                Debug.LogWarning("[LocalTrainingLobby] Training not active or not initialized");
                return;
            }
            
            StartCoroutine(StopTrainingSequence());
        }
        
        private IEnumerator InitializeAndStartTraining()
        {
            SetState(LobbyState.Initializing);
            
            Debug.Log("[LocalTrainingLobby] 🎯 Starting Local Training Lobby...");
            
            // Apply auto-start delay if configured
            if (autoStartDelay > 0f)
            {
                Debug.Log($"[LocalTrainingLobby] Applying auto-start delay: {autoStartDelay}s");
                yield return new WaitForSeconds(autoStartDelay);
            }
            
            // Step 1: Initialize network components
            yield return StartCoroutine(InitializeNetworkComponents());
            
            if (currentState == LobbyState.Error)
            {
                yield break;
            }
            
            // Step 2: Start local server
            yield return StartCoroutine(StartLocalServer());
            
            if (currentState == LobbyState.Error)
            {
                yield break;
            }
            
            // Step 3: Connect as client to our own server
            yield return StartCoroutine(ConnectAsClient());
            
            if (currentState == LobbyState.Error)
            {
                yield break;
            }
            
            // Step 4: Initialize training environment
            yield return StartCoroutine(SetupTrainingEnvironment());
            
            SetState(LobbyState.InTraining);
            isTrainingActive = true;
            
            Debug.Log("[LocalTrainingLobby] ✅ Training lobby ready! You can now practice locally.");
            OnTrainingStarted?.Invoke();
        }
        
        private IEnumerator InitializeNetworkComponents()
        {
            Debug.Log("[LocalTrainingLobby] Initializing network components...");
            
            // Find or create NetworkManager
            networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                networkManager = nmObj.AddComponent<NetworkManager>();
                nmObj.AddComponent<UnityTransport>();
                Debug.Log("[LocalTrainingLobby] Created new NetworkManager");
            }
            
            // Find or create NetworkSystemIntegration
            networkIntegration = FindFirstObjectByType<NetworkSystemIntegration>();
            if (networkIntegration == null)
            {
                GameObject nsiObj = new GameObject("NetworkSystemIntegration");
                networkIntegration = nsiObj.AddComponent<NetworkSystemIntegration>();
                Debug.Log("[LocalTrainingLobby] Created new NetworkSystemIntegration");
            }
            
            // Configure for local training
            ConfigureForLocalTraining();
            
            yield return new WaitForSeconds(0.1f);
            isInitialized = true;
        }
        
        private void ConfigureForLocalTraining()
        {
            // Configure NetworkManager for local training
            if (networkManager != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = trainingPlayerPrefab;
                networkManager.NetworkConfig.EnableSceneManagement = false;
                networkManager.NetworkConfig.TickRate = 60;
                
                // Set max connection limit for training
                networkManager.NetworkConfig.ConnectionApproval = maxTrainingPlayers > 1;
                
                // Configure transport for local connections
                var transport = networkManager.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = localAddress;
                    transport.ConnectionData.Port = localPort;
                    transport.ConnectionData.ServerListenAddress = localAddress;
                }
                
                Debug.Log($"[LocalTrainingLobby] Configured for local training - {localAddress}:{localPort}");
            }
        }
        
        private IEnumerator StartLocalServer()
        {
            SetState(LobbyState.StartingLocalServer);
            Debug.Log("[LocalTrainingLobby] Starting local server...");
            
            if (networkManager == null)
            {
                Debug.LogError("[LocalTrainingLobby] NetworkManager is null!");
                SetState(LobbyState.Error);
                yield break;
            }
            
            // Start as host (server + client in one)
            bool started = networkManager.StartHost();
            
            if (!started)
            {
                Debug.LogError("[LocalTrainingLobby] Failed to start host!");
                SetState(LobbyState.Error);
                yield break;
            }
            
            // Wait for server to be ready
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!networkManager.IsHost && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (!networkManager.IsHost)
            {
                Debug.LogError("[LocalTrainingLobby] Host start timeout!");
                SetState(LobbyState.Error);
                yield break;
            }
            
            Debug.Log("[LocalTrainingLobby] ✅ Local server started as host");
        }
        
        private IEnumerator ConnectAsClient()
        {
            SetState(LobbyState.ConnectingAsClient);
            
            // Since we're using StartHost(), we're already connected as both server and client
            if (networkManager.IsHost)
            {
                SetState(LobbyState.Connected);
                Debug.Log("[LocalTrainingLobby] ✅ Connected as host");
                yield break;
            }
            
            // Fallback: traditional client connection
            Debug.Log("[LocalTrainingLobby] Connecting as client...");
            
            bool connected = networkManager.StartClient();
            
            if (!connected)
            {
                Debug.LogError("[LocalTrainingLobby] Failed to connect as client!");
                SetState(LobbyState.Error);
                yield break;
            }
            
            // Wait for connection
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!networkManager.IsConnectedClient && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (!networkManager.IsConnectedClient)
            {
                Debug.LogError("[LocalTrainingLobby] Client connection timeout!");
                SetState(LobbyState.Error);
                yield break;
            }
            
            SetState(LobbyState.Connected);
            Debug.Log("[LocalTrainingLobby] ✅ Connected as client");
        }
        
        private IEnumerator SetupTrainingEnvironment()
        {
            Debug.Log("[LocalTrainingLobby] Setting up training environment...");
            
            yield return new WaitForSeconds(0.5f);
            
            // Spawn training player if enabled
            if (spawnPlayerImmediately && networkManager.IsHost)
            {
                Debug.Log("[LocalTrainingLobby] Training player spawn disabled - system simplified");
            }
            
            Debug.Log("[LocalTrainingLobby] ✅ Training environment ready");
        }
        
        private IEnumerator StopTrainingSequence()
        {
            Debug.Log("[LocalTrainingLobby] Stopping training lobby...");
            
            OnTrainingEnded?.Invoke();
            
            // Stop networking
            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
                
                // Wait for shutdown
                float timeout = 3f;
                float elapsed = 0f;
                
                while (networkManager.IsListening && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }
            
            isTrainingActive = false;
            SetState(LobbyState.Disconnected);
            
            Debug.Log("[LocalTrainingLobby] ✅ Training lobby stopped");
        }
        
        private void SetState(LobbyState newState)
        {
            if (currentState != newState)
            {
                var oldState = currentState;
                currentState = newState;
                
                Debug.Log($"[LocalTrainingLobby] State: {oldState} → {newState}");
                OnStateChanged?.Invoke(newState);
            }
        }
        
        // Debug GUI
        private void OnGUI()
        {
            if (!enableDebugUI) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("🎯 Local Training Lobby", GUILayout.Width(280));
            GUILayout.Space(5);
            
            GUILayout.Label($"State: {currentState}");
            GUILayout.Label($"Training Active: {isTrainingActive}");
            
            if (networkManager != null)
            {
                GUILayout.Label($"Network: Host={networkManager.IsHost}, Client={networkManager.IsClient}");
                GUILayout.Label($"Connected: {networkManager.IsConnectedClient}");
            }
            
            GUILayout.Space(10);
            
            if (!isTrainingActive)
            {
                if (GUILayout.Button("Start Training", GUILayout.Width(280)))
                {
                    StartTrainingLobby();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Training", GUILayout.Width(280)))
                {
                    StopTrainingLobby();
                }
            }
            
            GUILayout.EndArea();
        }
        
        private void OnDestroy()
        {
            if (isTrainingActive)
            {
                StopTrainingLobby();
            }
        }
    }
}
