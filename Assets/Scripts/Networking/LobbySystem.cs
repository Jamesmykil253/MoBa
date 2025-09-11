using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace MOBA.Networking
{
    /// <summary>
    /// Development-focused lobby system for automatic lobby creation and joining
    /// Streamlines the development process by providing instant lobby access
    /// </summary>
    public class LobbySystem : NetworkBehaviour
    {
        [Header("Lobby Configuration")]
        [SerializeField] private string lobbyName = "MOBA Development Lobby";
        [SerializeField] private int maxLobbyPlayers = 8;
        [SerializeField] private bool autoCreateLobby = true;
        [SerializeField] private bool autoJoinAsClient = false;
        [SerializeField] private float autoJoinDelay = 2f;
        
        [Header("Development Settings")]
        [SerializeField] private bool enableQuickStart = true;
        [SerializeField] private bool skipLobbyUI = false;
        [SerializeField] private string developmentIP = "127.0.0.1";
        [SerializeField] private ushort developmentPort = 7777;
        
        [Header("Lobby State")]
        [SerializeField] private LobbyState currentState = LobbyState.Inactive;
        
        // Network Variables for lobby management
        private NetworkVariable<int> lobbyPlayerCount = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> lobbyIsReady = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        // Lobby player management
        private Dictionary<ulong, LobbyPlayer> lobbyPlayers = new Dictionary<ulong, LobbyPlayer>();
        private NetworkSystemIntegration networkSystem;
        private NetworkGameManager gameManager;
        
        // Events
        public System.Action<LobbyState> OnLobbyStateChanged;
        public System.Action<int> OnPlayerCountChanged;
        public System.Action OnLobbyReady;
        
        private void Awake()
        {
            // Find network systems
            networkSystem = FindFirstObjectByType<NetworkSystemIntegration>();
            if (networkSystem == null)
            {
                Debug.LogError("[LobbySystem] NetworkSystemIntegration not found!");
            }
        }
        
        private void Start()
        {
            if (autoCreateLobby && Application.isEditor)
            {
                Debug.Log("[LobbySystem] 🚀 Auto-creating development lobby...");
                StartCoroutine(AutoCreateLobbyCoroutine());
            }
        }
        
        private IEnumerator AutoCreateLobbyCoroutine()
        {
            // Wait for network system to initialize
            yield return new WaitForSeconds(1f);
            
            // Check if should skip UI for development
            if (skipLobbyUI)
            {
                Debug.Log("[LobbySystem] 🚀 Skipping lobby UI for development");
            }
            
            // Create lobby as host using development port if configured
            if (developmentPort != 7777)
            {
                Debug.Log($"[LobbySystem] Using development port: {developmentPort}");
            }
            
            CreateLobby();
            
            // Auto-join clients if enabled
            if (autoJoinAsClient)
            {
                yield return new WaitForSeconds(autoJoinDelay);
                StartCoroutine(AutoJoinClientCoroutine());
            }
        }
        
        private IEnumerator AutoJoinClientCoroutine()
        {
            // This would simulate additional clients joining
            Debug.Log("[LobbySystem] 🤖 Simulating client join for development...");
            // In a real scenario, this would be triggered from separate game instances
            yield return null;
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedToLobby;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedFromLobby;
                
                Debug.Log("[LobbySystem] ✅ Lobby server initialized");
                SetLobbyState(LobbyState.WaitingForPlayers);
            }
            
            // Subscribe to network variable changes
            lobbyPlayerCount.OnValueChanged += OnLobbyPlayerCountChanged;
            lobbyIsReady.OnValueChanged += OnLobbyReadyChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedToLobby;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedFromLobby;
            }
            
            lobbyPlayerCount.OnValueChanged -= OnLobbyPlayerCountChanged;
            lobbyIsReady.OnValueChanged -= OnLobbyReadyChanged;
        }
        
        /// <summary>
        /// Create a new lobby (Host)
        /// </summary>
        public void CreateLobby()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[LobbySystem] ⚠️ Network already active, stopping first...");
                NetworkManager.Singleton.Shutdown();
                StartCoroutine(DelayedCreateLobby());
                return;
            }
            
            Debug.Log($"[LobbySystem] 🏗️ Creating lobby: {lobbyName}");
            SetLobbyState(LobbyState.Creating);
            
            // Configure network for lobby
            if (networkSystem != null)
            {
                networkSystem.StartServer();
            }
            else
            {
                NetworkManager.Singleton.StartHost();
            }
        }
        
        private IEnumerator DelayedCreateLobby()
        {
            yield return new WaitForSeconds(1f);
            CreateLobby();
        }
        
        /// <summary>
        /// Join an existing lobby (Client)
        /// </summary>
        public void JoinLobby(string ipAddress = null, ushort port = 7777)
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[LobbySystem] ⚠️ Network already active, stopping first...");
                NetworkManager.Singleton.Shutdown();
                StartCoroutine(DelayedJoinLobby(ipAddress, port));
                return;
            }
            
            string targetIP = ipAddress ?? developmentIP;
            Debug.Log($"[LobbySystem] 🔌 Joining lobby at {targetIP}:{port}");
            SetLobbyState(LobbyState.Joining);
            
            // Configure network for client
            if (networkSystem != null)
            {
                networkSystem.StartClient(targetIP, port);
            }
            else
            {
                NetworkManager.Singleton.StartClient();
            }
        }
        
        private IEnumerator DelayedJoinLobby(string ipAddress, ushort port)
        {
            yield return new WaitForSeconds(1f);
            JoinLobby(ipAddress, port);
        }
        
        /// <summary>
        /// Leave the current lobby
        /// </summary>
        public void LeaveLobby()
        {
            Debug.Log("[LobbySystem] 🚪 Leaving lobby...");
            SetLobbyState(LobbyState.Inactive);
            
            if (NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // Clear lobby data
            lobbyPlayers.Clear();
        }
        
        /// <summary>
        /// Quick start for development - creates lobby and starts game immediately
        /// </summary>
        public void QuickStartDevelopment()
        {
            if (!enableQuickStart)
            {
                Debug.LogWarning("[LobbySystem] Quick start is disabled");
                return;
            }
            
            Debug.Log("[LobbySystem] ⚡ Quick starting development session...");
            
            if (!NetworkManager.Singleton.IsListening)
            {
                CreateLobby();
            }
            
            // Wait a moment then start the game
            StartCoroutine(QuickStartCoroutine());
        }
        
        private IEnumerator QuickStartCoroutine()
        {
            yield return new WaitForSeconds(2f);
            
            if (IsServer && lobbyPlayerCount.Value >= 1)
            {
                Debug.Log("[LobbySystem] 🎮 Auto-starting game for development...");
                StartGameFromLobby();
            }
        }
        
        /// <summary>
        /// Start the game from lobby
        /// </summary>
        public void StartGameFromLobby()
        {
            if (!IsServer)
            {
                RequestStartGameServerRpc();
                return;
            }
            
            Debug.Log("[LobbySystem] 🎯 Starting game from lobby...");
            SetLobbyState(LobbyState.StartingGame);
            
            // Initialize game manager if not already present
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<NetworkGameManager>();
                if (gameManager == null)
                {
                    var gameManagerObject = new GameObject("NetworkGameManager");
                    gameManager = gameManagerObject.AddComponent<NetworkGameManager>();
                    gameManager.GetComponent<NetworkObject>().Spawn();
                }
            }
            
            // Notify all clients
            StartGameClientRpc();
            
            // Transition to game
            SetLobbyState(LobbyState.InGame);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestStartGameServerRpc()
        {
            StartGameFromLobby();
        }
        
        [ClientRpc]
        private void StartGameClientRpc()
        {
            Debug.Log("[LobbySystem] 🎮 Game starting on client...");
            SetLobbyState(LobbyState.InGame);
            
            // Additional client game start logic
            OnGameStarted();
        }
        
        /// <summary>
        /// Set player ready state
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            if (lobbyPlayers.TryGetValue(clientId, out var player))
            {
                player.isReady = isReady;
                lobbyPlayers[clientId] = player;
                
                Debug.Log($"[LobbySystem] Player {clientId} ready state: {isReady}");
                
                // Check if all players are ready
                CheckAllPlayersReady();
                
                // Notify clients of player state change
                UpdatePlayerStateClientRpc(clientId, isReady);
            }
        }
        
        [ClientRpc]
        private void UpdatePlayerStateClientRpc(ulong clientId, bool isReady)
        {
            Debug.Log($"[LobbySystem] Player {clientId} is {(isReady ? "ready" : "not ready")}");
        }
        
        private void OnClientConnectedToLobby(ulong clientId)
        {
            if (!IsServer) return;
            
            Debug.Log($"[LobbySystem] 👤 Player {clientId} joined lobby");
            
            // Add player to lobby
            var lobbyPlayer = new LobbyPlayer
            {
                clientId = clientId,
                isReady = false,
                joinTime = Time.time
            };
            
            lobbyPlayers[clientId] = lobbyPlayer;
            lobbyPlayerCount.Value = lobbyPlayers.Count;
            
            // Notify clients
            PlayerJoinedLobbyClientRpc(clientId);
        }
        
        private void OnClientDisconnectedFromLobby(ulong clientId)
        {
            if (!IsServer) return;
            
            Debug.Log($"[LobbySystem] 👤 Player {clientId} left lobby");
            
            // Remove player from lobby
            if (lobbyPlayers.Remove(clientId))
            {
                lobbyPlayerCount.Value = lobbyPlayers.Count;
                
                // Notify clients
                PlayerLeftLobbyClientRpc(clientId);
            }
        }
        
        [ClientRpc]
        private void PlayerJoinedLobbyClientRpc(ulong clientId)
        {
            Debug.Log($"[LobbySystem] Player {clientId} joined the lobby");
        }
        
        [ClientRpc]
        private void PlayerLeftLobbyClientRpc(ulong clientId)
        {
            Debug.Log($"[LobbySystem] Player {clientId} left the lobby");
        }
        
        private void CheckAllPlayersReady()
        {
            if (lobbyPlayers.Count == 0) return;
            
            bool allReady = true;
            foreach (var player in lobbyPlayers.Values)
            {
                if (!player.isReady)
                {
                    allReady = false;
                    break;
                }
            }
            
            lobbyIsReady.Value = allReady && lobbyPlayers.Count >= 2;
            
            if (lobbyIsReady.Value)
            {
                Debug.Log("[LobbySystem] ✅ All players ready! Game can start.");
                OnLobbyReady?.Invoke();
            }
        }
        
        private void OnLobbyPlayerCountChanged(int previousValue, int newValue)
        {
            Debug.Log($"[LobbySystem] Lobby players: {previousValue} → {newValue}");
            OnPlayerCountChanged?.Invoke(newValue);
        }
        
        private void OnLobbyReadyChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"[LobbySystem] Lobby ready: {previousValue} → {newValue}");
            
            if (newValue)
            {
                OnLobbyReady?.Invoke();
            }
        }
        
        private void SetLobbyState(LobbyState newState)
        {
            if (currentState != newState)
            {
                var previousState = currentState;
                currentState = newState;
                
                Debug.Log($"[LobbySystem] State: {previousState} → {newState}");
                OnLobbyStateChanged?.Invoke(newState);
            }
        }
        
        private void OnGameStarted()
        {
            // Additional game start logic for clients
            Debug.Log("[LobbySystem] 🎮 Game started - switching to gameplay mode");
        }
        
        // Public getters
        public LobbyState CurrentState => currentState;
        public int PlayerCount => lobbyPlayerCount.Value;
        public bool IsReady => lobbyIsReady.Value;
        public bool IsInLobby => currentState == LobbyState.WaitingForPlayers || currentState == LobbyState.Ready;
        
        // Development helpers
        [ContextMenu("Quick Start Development")]
        private void QuickStartDevelopmentMenu()
        {
            QuickStartDevelopment();
        }
        
        [ContextMenu("Create Lobby")]
        private void CreateLobbyMenu()
        {
            CreateLobby();
        }
        
        [ContextMenu("Join Local Lobby")]
        private void JoinLocalLobbyMenu()
        {
            JoinLobby();
        }
        
        // Debug GUI
        private void OnGUI()
        {
            if (!Application.isEditor) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"Lobby System - {currentState}");
            GUILayout.Label($"Players: {lobbyPlayerCount.Value}/{maxLobbyPlayers}");
            GUILayout.Label($"Ready: {lobbyIsReady.Value}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create Lobby"))
                CreateLobby();
                
            if (GUILayout.Button("Join Lobby"))
                JoinLobby();
                
            if (GUILayout.Button("Quick Start"))
                QuickStartDevelopment();
                
            if (GUILayout.Button("Leave Lobby"))
                LeaveLobby();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
    
    [System.Serializable]
    public struct LobbyPlayer
    {
        public ulong clientId;
        public bool isReady;
        public float joinTime;
    }
    
    public enum LobbyState
    {
        Inactive,
        Creating,
        Joining,
        WaitingForPlayers,
        Ready,
        StartingGame,
        InGame
    }
}
