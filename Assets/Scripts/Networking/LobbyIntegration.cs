using UnityEngine;
using Unity.Netcode;

namespace MOBA.Networking
{
    /// <summary>
    /// Automatic lobby integration for seamless development workflow
    /// Coordinates between LobbySystem and NetworkSystemIntegration
    /// </summary>
    public class LobbyIntegration : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private LobbySystem lobbySystem;
        [SerializeField] private NetworkSystemIntegration networkIntegration;
        [SerializeField] private MOBA.UI.LobbyUI lobbyUI;
        
        [Header("Auto Integration Settings")]
        [SerializeField] private bool enableAutoIntegration = true;
        [SerializeField] private bool createLobbyOnStart = true;
        [SerializeField] private bool startGameWithMinPlayers = true;
        [SerializeField] private int minPlayersToStart = 1;
        [SerializeField] private float autoStartDelay = 3f;
        
        [Header("Development Shortcuts")]
        [SerializeField] private KeyCode quickStartKey = KeyCode.F1;
        [SerializeField] private KeyCode createLobbyKey = KeyCode.F2;
        [SerializeField] private KeyCode joinLobbyKey = KeyCode.F3;
        [SerializeField] private KeyCode leaveLobbyKey = KeyCode.F4;
        
        private bool hasAutoStarted = false;
        
        private void Awake()
        {
            // Auto-find systems if not assigned
            if (lobbySystem == null)
                lobbySystem = FindFirstObjectByType<LobbySystem>();
            if (networkIntegration == null)
                networkIntegration = FindFirstObjectByType<NetworkSystemIntegration>();
            if (lobbyUI == null)
                lobbyUI = FindFirstObjectByType<MOBA.UI.LobbyUI>();
        }
        
        private void Start()
        {
            if (enableAutoIntegration)
            {
                InitializeIntegration();
            }
        }
        
        private void Update()
        {
            HandleDevelopmentShortcuts();
            HandleAutoStart();
        }
        
        private void InitializeIntegration()
        {
            Debug.Log("[LobbyIntegration] 🔗 Initializing lobby integration...");
            
            // Subscribe to lobby events
            if (lobbySystem != null)
            {
                lobbySystem.OnLobbyStateChanged += OnLobbyStateChanged;
                lobbySystem.OnPlayerCountChanged += OnPlayerCountChanged;
                lobbySystem.OnLobbyReady += OnLobbyReady;
            }
            
            // Create lobby automatically if enabled
            if (createLobbyOnStart && Application.isEditor)
            {
                Invoke(nameof(AutoCreateLobby), 1f);
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (lobbySystem != null)
            {
                lobbySystem.OnLobbyStateChanged -= OnLobbyStateChanged;
                lobbySystem.OnPlayerCountChanged -= OnPlayerCountChanged;
                lobbySystem.OnLobbyReady -= OnLobbyReady;
            }
        }
        
        private void AutoCreateLobby()
        {
            if (lobbySystem != null && !NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[LobbyIntegration] 🚀 Auto-creating development lobby...");
                lobbySystem.CreateLobby();
            }
        }
        
        private void HandleDevelopmentShortcuts()
        {
            if (!Application.isEditor) return;
            
            if (Input.GetKeyDown(quickStartKey))
            {
                QuickStart();
            }
            else if (Input.GetKeyDown(createLobbyKey))
            {
                CreateLobby();
            }
            else if (Input.GetKeyDown(joinLobbyKey))
            {
                JoinLobby();
            }
            else if (Input.GetKeyDown(leaveLobbyKey))
            {
                LeaveLobby();
            }
        }
        
        private void HandleAutoStart()
        {
            if (!startGameWithMinPlayers || hasAutoStarted) return;
            if (lobbySystem == null || !NetworkManager.Singleton.IsHost) return;
            
            // Auto-start game if minimum players reached
            if (lobbySystem.PlayerCount >= minPlayersToStart && 
                lobbySystem.CurrentState == LobbyState.WaitingForPlayers)
            {
                Debug.Log($"[LobbyIntegration] ⚡ Auto-starting game with {lobbySystem.PlayerCount} players...");
                Invoke(nameof(AutoStartGame), autoStartDelay);
                hasAutoStarted = true;
            }
        }
        
        private void AutoStartGame()
        {
            if (lobbySystem != null)
            {
                lobbySystem.StartGameFromLobby();
            }
        }
        
        private void OnLobbyStateChanged(LobbyState newState)
        {
            Debug.Log($"[LobbyIntegration] 📊 Lobby state changed: {newState}");
            
            switch (newState)
            {
                case LobbyState.InGame:
                    OnGameStarted();
                    break;
                case LobbyState.Inactive:
                    OnLobbyLeft();
                    break;
            }
        }
        
        private void OnPlayerCountChanged(int newCount)
        {
            Debug.Log($"[LobbyIntegration] 👥 Player count changed: {newCount}");
            
            // Reset auto-start flag if players leave
            if (newCount < minPlayersToStart)
            {
                hasAutoStarted = false;
            }
        }
        
        private void OnLobbyReady()
        {
            Debug.Log("[LobbyIntegration] ✅ Lobby is ready to start!");
        }
        
        private void OnGameStarted()
        {
            Debug.Log("[LobbyIntegration] 🎮 Game started from lobby");
            
            // Enable game systems
            if (networkIntegration != null)
            {
                var status = networkIntegration.GetSystemStatus();
                Debug.Log($"[LobbyIntegration] Network systems active: {status.isNetworkManagerActive}");
            }
        }
        
        private void OnLobbyLeft()
        {
            Debug.Log("[LobbyIntegration] 🚪 Left lobby - resetting state");
            hasAutoStarted = false;
        }
        
        // Public methods for external control
        public void QuickStart()
        {
            Debug.Log("[LobbyIntegration] ⚡ Quick starting development session...");
            if (lobbySystem != null)
            {
                lobbySystem.QuickStartDevelopment();
            }
        }
        
        public void CreateLobby()
        {
            Debug.Log("[LobbyIntegration] 🏗️ Creating lobby...");
            if (lobbySystem != null)
            {
                lobbySystem.CreateLobby();
            }
        }
        
        public void JoinLobby(string ipAddress = "127.0.0.1", ushort port = 7777)
        {
            Debug.Log($"[LobbyIntegration] 🔌 Joining lobby at {ipAddress}:{port}...");
            if (lobbySystem != null)
            {
                lobbySystem.JoinLobby(ipAddress, port);
            }
        }
        
        public void LeaveLobby()
        {
            Debug.Log("[LobbyIntegration] 🚪 Leaving lobby...");
            if (lobbySystem != null)
            {
                lobbySystem.LeaveLobby();
            }
        }
        
        public void StartGame()
        {
            Debug.Log("[LobbyIntegration] 🎯 Starting game...");
            if (lobbySystem != null)
            {
                lobbySystem.StartGameFromLobby();
            }
        }
        
        // Getters for integration info
        public bool IsLobbyActive => lobbySystem?.IsInLobby ?? false;
        public int PlayerCount => lobbySystem?.PlayerCount ?? 0;
        public LobbyState CurrentLobbyState => lobbySystem?.CurrentState ?? LobbyState.Inactive;
        public bool IsNetworkActive => NetworkManager.Singleton?.IsListening ?? false;
        
        // Context menu helpers for editor
        [ContextMenu("Quick Start")]
        private void QuickStartContext() => QuickStart();
        
        [ContextMenu("Create Lobby")]
        private void CreateLobbyContext() => CreateLobby();
        
        [ContextMenu("Join Local Lobby")]
        private void JoinLobbyContext() => JoinLobby();
        
        [ContextMenu("Leave Lobby")]
        private void LeaveLobbyContext() => LeaveLobby();
        
        [ContextMenu("Start Game")]
        private void StartGameContext() => StartGame();
        
        // Development info display
        private void OnGUI()
        {
            if (!Application.isEditor) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 250));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("🎮 Lobby Integration", EditorGUIStyle());
            GUILayout.Space(5);
            
            GUILayout.Label($"Network: {(IsNetworkActive ? "✅ Active" : "❌ Inactive")}");
            GUILayout.Label($"Lobby: {CurrentLobbyState}");
            GUILayout.Label($"Players: {PlayerCount}");
            GUILayout.Label($"Auto-Started: {hasAutoStarted}");
            
            GUILayout.Space(10);
            GUILayout.Label("🎯 Shortcuts:");
            GUILayout.Label($"F1 - Quick Start");
            GUILayout.Label($"F2 - Create Lobby");
            GUILayout.Label($"F3 - Join Lobby");
            GUILayout.Label($"F4 - Leave Lobby");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private GUIStyle EditorGUIStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            return style;
        }
    }
}
