using UnityEngine;
using Unity.Netcode;
using MOBA.Configuration;
using MOBA.Debugging;
using MOBA.Networking;
using MOBA.Services;
using MOBA.GameManagement;

namespace MOBA
{
    /// <summary>
    /// Refactored SimpleGameManager - coordinates MOBA gameplay through specialized component managers.
    /// Significantly reduced from 1046 lines to ~200 lines by extracting legacy compatibility logic.
    /// Follows Clean Code principles with clear separation of concerns.
    /// </summary>
    public class SimpleGameManager : NetworkBehaviour
    {
        public static SimpleGameManager Instance { get; private set; }

        #region Component Managers
        
        [Header("Game Management Components")]
        [SerializeField, Tooltip("Configuration management component")]
        private GameConfigurationManager configurationManager;
        
        [SerializeField, Tooltip("Service coordination component")]
        private GameServiceManager serviceManager;
        
        [SerializeField, Tooltip("Spawn management component")]
        private GameSpawnManager spawnManager;
        
        [SerializeField, Tooltip("Network synchronization component")]
        private GameNetworkManager networkManager;
        
        [SerializeField, Tooltip("UI management component")]
        private GameUIManager uiManager;
        
        [SerializeField, Tooltip("Event coordination component")]
        private GameEventManager eventManager;
        
        [SerializeField, Tooltip("Match lifecycle component")]
        private GameMatchLifecycleManager matchLifecycleManager;
        
        [SerializeField, Tooltip("Legacy compatibility component")]
        private GameLegacyCompatibilityManager legacyCompatibilityManager;
        
        #endregion

        #region Configuration
        
        [Header("Core Configuration")]
        [SerializeField] private GameConfig defaultGameConfig;
        [SerializeField] private bool autoStartOnEnable = true;
        [SerializeField] private bool spawnSingleLocalPlayer = true;
        [SerializeField] private bool enableLegacyCompatibility = true;
        
        [Header("Game Settings")]
        [SerializeField] public int maxPlayers = 4;
        [SerializeField] public float gameTime = 300f; // 5 minutes default
        [SerializeField] public int scoreToWin = 10;
        
        /// <summary>
        /// Public accessor for autoStartOnEnable configuration
        /// </summary>
        public bool AutoStartOnEnable => autoStartOnEnable;
        
        #endregion

        #region Component Access Properties
        
        /// <summary>
        /// Access to configuration management component
        /// </summary>
        public GameConfigurationManager GetConfigurationManager() => configurationManager;
        
        /// <summary>
        /// Access to service coordination component
        /// </summary>
        public GameServiceManager GetServiceManager() => serviceManager;
        
        /// <summary>
        /// Access to spawn management component
        /// </summary>
        public GameSpawnManager GetSpawnManager() => spawnManager;
        
        /// <summary>
        /// Access to network synchronization component
        /// </summary>
        public GameNetworkManager GetNetworkManager() => networkManager;
        
        /// <summary>
        /// Access to UI management component
        /// </summary>
        public GameUIManager GetUIManager() => uiManager;
        
        /// <summary>
        /// Access to event coordination component
        /// </summary>
        public GameEventManager GetEventManager() => eventManager;
        
        /// <summary>
        /// Access to match lifecycle component
        /// </summary>
        public GameMatchLifecycleManager GetMatchLifecycleManager() => matchLifecycleManager;
        
        /// <summary>
        /// Access to legacy compatibility component
        /// </summary>
        public GameLegacyCompatibilityManager GetLegacyCompatibilityManager() => legacyCompatibilityManager;
        
        #endregion

        #region Initialization
        
        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.GameLifecycle,
                GameDebugSystemTag.GameLifecycle,
                mechanic,
                subsystem: nameof(SimpleGameManager),
                actor: gameObject != null ? gameObject.name : null);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Configuration),
                    "Duplicate SimpleGameManager detected. Destroying extra instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            InitializeComponents();

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                "SimpleGameManager initialized with component-based architecture.");
        }

        /// <summary>
        /// Initialize all game management components
        /// </summary>
        private void InitializeComponents()
        {
            // Find or create components if not assigned
            if (configurationManager == null)
                configurationManager = GetComponent<GameConfigurationManager>() ?? gameObject.AddComponent<GameConfigurationManager>();
            if (serviceManager == null)
                serviceManager = GetComponent<GameServiceManager>() ?? gameObject.AddComponent<GameServiceManager>();
            if (spawnManager == null)
                spawnManager = GetComponent<GameSpawnManager>() ?? gameObject.AddComponent<GameSpawnManager>();
            if (networkManager == null)
                networkManager = GetComponent<GameNetworkManager>() ?? gameObject.AddComponent<GameNetworkManager>();
            if (uiManager == null)
                uiManager = GetComponent<GameUIManager>() ?? gameObject.AddComponent<GameUIManager>();
            if (eventManager == null)
                eventManager = GetComponent<GameEventManager>() ?? gameObject.AddComponent<GameEventManager>();
            if (matchLifecycleManager == null)
                matchLifecycleManager = GetComponent<GameMatchLifecycleManager>() ?? gameObject.AddComponent<GameMatchLifecycleManager>();
            
            // Initialize legacy compatibility manager if enabled
            if (enableLegacyCompatibility)
            {
                if (legacyCompatibilityManager == null)
                    legacyCompatibilityManager = GetComponent<GameLegacyCompatibilityManager>() ?? gameObject.AddComponent<GameLegacyCompatibilityManager>();
            }

            // Initialize components with configuration
            configurationManager?.Initialize(this);
            serviceManager?.Initialize(this);
            spawnManager?.Initialize(this);
            networkManager?.Initialize(this);
            uiManager?.Initialize(this);
            eventManager?.Initialize(this);
            matchLifecycleManager?.Initialize(this);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Auto-start if configured
            if (autoStartOnEnable && IsServer)
            {
                StartGame();
            }
            
            // Handle player connections for spawning
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandlePlayerJoined;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandlePlayerDisconnected;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Cleanup network callbacks
            if (NetworkManager.Singleton != null && IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandlePlayerJoined;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandlePlayerDisconnected;
            }
        }
        
        #endregion

        #region Game Lifecycle Management
        
        /// <summary>
        /// Starts the game session - delegates to MatchLifecycleManager
        /// </summary>
        public void StartGame()
        {
            if (matchLifecycleManager != null)
            {
                matchLifecycleManager.StartMatch();
            }
            else
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "MatchLifecycleManager not available. Game start failed.");
            }
        }

        /// <summary>
        /// Ends the current game - delegates to MatchLifecycleManager
        /// </summary>
        public void EndGame()
        {
            if (matchLifecycleManager != null)
            {
                matchLifecycleManager.EndMatch();
            }
            else
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "MatchLifecycleManager not available. Game end failed.");
            }
        }

        /// <summary>
        /// Restarts the current game - delegates to MatchLifecycleManager
        /// </summary>
        public void RestartGame()
        {
            if (matchLifecycleManager != null)
            {
                matchLifecycleManager.RestartMatch();
            }
            else
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "MatchLifecycleManager not available. Game restart failed.");
            }
        }

        /// <summary>
        /// Checks if the game match is currently active - delegates to MatchLifecycleManager
        /// </summary>
        public bool IsGameActive()
        {
            if (matchLifecycleManager != null)
            {
                return matchLifecycleManager.IsMatchActive;
            }
            
            // Fallback to legacy compatibility if available
            return legacyCompatibilityManager?.IsGameActive() ?? false;
        }
        
        /// <summary>
        /// Gets the remaining time in the current match - delegates to MatchLifecycleManager
        /// </summary>
        public float GetTimeRemaining()
        {
            if (matchLifecycleManager != null)
            {
                return matchLifecycleManager.TimeRemaining;
            }
            
            // Fallback to legacy compatibility if available
            return legacyCompatibilityManager?.GetTimeRemaining() ?? 0f;
        }

        /// <summary>
        /// Gets the current score for the specified team - delegates to ServiceManager
        /// </summary>
        public int GetScore(int team)
        {
            if (serviceManager != null)
            {
                return serviceManager.GetTeamScore(team);
            }
            
            // Fallback to legacy compatibility if available
            return legacyCompatibilityManager?.GetScore(team) ?? 0;
        }
        
        /// <summary>
        /// Adds points to the specified team's score - delegates to ServiceManager
        /// </summary>
        public bool AddScore(int team, int points = 1)
        {
            if (serviceManager != null)
            {
                return serviceManager.AddTeamScore(team, points);
            }
            
            GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.General),
                "ServiceManager not available. Score addition failed.");
            return false;
        }
        
        /// <summary>
        /// Starts a match - alias for StartGame for backward compatibility
        /// </summary>
        public void StartMatch()
        {
            StartGame();
        }
        
        /// <summary>
        /// Gets the match lifecycle service from the service manager
        /// </summary>
        public IMatchLifecycleService GetMatchLifecycleService()
        {
            // For now, return null - this should be implemented in the service manager
            GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.General),
                "GetMatchLifecycleService not yet implemented in refactored architecture.");
            return null;
        }
        
        /// <summary>
        /// Handles score change events - delegates to event system
        /// </summary>
        public void HandleScoreChanged(int team, int newScore)
        {
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Score),
                "Score changed.",
                ("Team", team),
                ("NewScore", newScore));
                
            // Trigger legacy event compatibility
            legacyCompatibilityManager?.OnScoreUpdate?.Invoke(team, newScore);
        }
        
        /// <summary>
        /// Handles match end events - delegates to event system
        /// </summary>
        public void HandleMatchEnded(int winningTeam)
        {
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Lifecycle),
                "Match ended.",
                ("WinningTeam", winningTeam));
                
            // Trigger legacy event compatibility
            legacyCompatibilityManager?.OnGameEnd?.Invoke(winningTeam);
        }
        
        #endregion
        
        #region Player Management
        
        /// <summary>
        /// Spawns all connected players - delegates to SpawnManager
        /// </summary>
        public void SpawnPlayers()
        {
            if (spawnManager != null)
            {
                if (spawnSingleLocalPlayer)
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Spawning),
                        "Spawning single local player only.");
                }
                
                spawnManager.SpawnPlayers();
            }
            else
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Spawning),
                    "SpawnManager not available. Player spawning failed.");
            }
        }
        
        private void HandlePlayerJoined(ulong clientId)
        {
            if (!IsServer) return;

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking),
                "Player joined - triggering spawn.",
                ("ClientId", clientId.ToString()));

            // Delegate to spawn manager if available
            if (spawnManager != null)
            {
                spawnManager.SpawnPlayers();
            }
        }

        private void HandlePlayerDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking),
                "Player disconnected.",
                ("ClientId", clientId.ToString()));

            // Currently no additional behavior beyond avatar despawn handled by networking layer
        }
        
        #endregion
        
        #region Legacy API Support
        
        /// <summary>
        /// Legacy game end event support - delegates to legacy compatibility manager
        /// DEPRECATED: Use modern event system instead
        /// </summary>
        public System.Action<int> OnGameEnd
        {
            get => legacyCompatibilityManager?.OnGameEnd;
            set
            {
                if (legacyCompatibilityManager != null)
                    legacyCompatibilityManager.OnGameEnd = value;
            }
        }
        
        /// <summary>
        /// Legacy score update event support - delegates to legacy compatibility manager
        /// DEPRECATED: Use modern event system instead
        /// </summary>
        public System.Action<int, int> OnScoreUpdate
        {
            get => legacyCompatibilityManager?.OnScoreUpdate;
            set
            {
                if (legacyCompatibilityManager != null)
                    legacyCompatibilityManager.OnScoreUpdate = value;
            }
        }
        
        #endregion

        #region Cleanup
        
        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // Clear singleton reference
            if (Instance == this)
            {
                Instance = null;
            }

            // Cleanup components - they handle their own shutdown
            configurationManager?.Shutdown();
            serviceManager?.Shutdown();
            spawnManager?.Shutdown();
            networkManager?.Shutdown();
            uiManager?.Shutdown();
            eventManager?.Shutdown();
            matchLifecycleManager?.Shutdown();
            
            // Legacy compatibility manager handles its own cleanup

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Lifecycle),
                "SimpleGameManager destroyed and cleaned up.");
        }
        
        #endregion
    }
}
