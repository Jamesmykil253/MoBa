using UnityEngine;
using Unity.Netcode;
using MOBA.Debugging;
using MOBA.Networking;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages network synchronization for game state including NetworkVariables and client/server coordination.
    /// Handles network events, player connection/disconnection, and state replication.
    /// </summary>
    public class GameNetworkManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("Network Settings")]
        [SerializeField, Tooltip("Enable detailed network logging")]
        private bool logNetworkEvents = true;
        
        [SerializeField, Tooltip("Auto-start match when enabled on server")]
        private bool autoStartOnEnable = true;
        
        #endregion
        
        #region Network Variables
        
        /// <summary>
        /// Network synchronized time remaining in match
        /// </summary>
        private readonly NetworkVariable<float> networkTimeRemaining = 
            new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        /// <summary>
        /// Network synchronized team A score
        /// </summary>
        private readonly NetworkVariable<int> networkTeamScoreA = 
            new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        /// <summary>
        /// Network synchronized team B score
        /// </summary>
        private readonly NetworkVariable<int> networkTeamScoreB = 
            new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        /// <summary>
        /// Network synchronized game active state
        /// </summary>
        private readonly NetworkVariable<bool> networkGameActive = 
            new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        /// <summary>
        /// Network synchronized winning team
        /// </summary>
        private readonly NetworkVariable<int> networkWinningTeam = 
            new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current network time remaining
        /// </summary>
        public float NetworkTimeRemaining => networkTimeRemaining.Value;
        
        /// <summary>
        /// Current network team A score
        /// </summary>
        public int NetworkTeamScoreA => networkTeamScoreA.Value;
        
        /// <summary>
        /// Current network team B score
        /// </summary>
        public int NetworkTeamScoreB => networkTeamScoreB.Value;
        
        /// <summary>
        /// Current network game active state
        /// </summary>
        public bool NetworkGameActive => networkGameActive.Value;
        
        /// <summary>
        /// Current network winning team
        /// </summary>
        public int NetworkWinningTeam => networkWinningTeam.Value;
        
        /// <summary>
        /// Whether this instance is the server
        /// </summary>
        public bool IsServer => simpleGameManager != null && simpleGameManager.IsServer;
        
        /// <summary>
        /// Whether auto-start is enabled
        /// </summary>
        public bool AutoStartOnEnable => autoStartOnEnable;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when network time changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<float, float> OnNetworkTimeChanged; // previous, current
        
        /// <summary>
        /// Raised when network team score changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int, int, int> OnNetworkScoreChanged; // team, previous, current
        
        /// <summary>
        /// Raised when network game state changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<bool, bool> OnNetworkGameStateChanged; // previous, current
        
        /// <summary>
        /// Raised when network winning team changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int, int> OnNetworkWinningTeamChanged; // previous, current
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            SetupNetworkEvents();
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network manager initialized.",
                    ("IsServer", IsServer),
                    ("AutoStart", autoStartOnEnable));
            }
        }
        
        public override void Shutdown()
        {
            CleanupNetworkEvents();
            
            // Clear events
            OnNetworkTimeChanged = null;
            OnNetworkScoreChanged = null;
            OnNetworkGameStateChanged = null;
            OnNetworkWinningTeamChanged = null;
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Network Event Setup
        
        /// <summary>
        /// Setup network variable change listeners
        /// </summary>
        private void SetupNetworkEvents()
        {
            networkTimeRemaining.OnValueChanged += OnNetworkTimeRemainingChanged;
            networkTeamScoreA.OnValueChanged += OnNetworkTeamScoreAChanged;
            networkTeamScoreB.OnValueChanged += OnNetworkTeamScoreBChanged;
            networkGameActive.OnValueChanged += OnNetworkGameActiveChanged;
            networkWinningTeam.OnValueChanged += HandleNetworkWinningTeamChanged;
            
            // Setup player connection events if server
            if (IsServer)
            {
                var productionNetworkManager = ProductionNetworkManager.Instance;
                if (productionNetworkManager != null)
                {
                    productionNetworkManager.OnPlayerConnected += HandleServerPlayerJoined;
                    productionNetworkManager.OnPlayerDisconnected += HandleServerPlayerDisconnected;
                }
            }
        }
        
        /// <summary>
        /// Cleanup network variable change listeners
        /// </summary>
        private void CleanupNetworkEvents()
        {
            networkTimeRemaining.OnValueChanged -= OnNetworkTimeRemainingChanged;
            networkTeamScoreA.OnValueChanged -= OnNetworkTeamScoreAChanged;
            networkTeamScoreB.OnValueChanged -= OnNetworkTeamScoreBChanged;
            networkGameActive.OnValueChanged -= OnNetworkGameActiveChanged;
            networkWinningTeam.OnValueChanged -= HandleNetworkWinningTeamChanged;
            
            // Cleanup player connection events if server
            if (IsServer)
            {
                var productionNetworkManager = ProductionNetworkManager.Instance;
                if (productionNetworkManager != null)
                {
                    productionNetworkManager.OnPlayerConnected -= HandleServerPlayerJoined;
                    productionNetworkManager.OnPlayerDisconnected -= HandleServerPlayerDisconnected;
                }
            }
        }
        
        #endregion
        
        #region Network Synchronization
        
        /// <summary>
        /// Synchronize time remaining to clients
        /// </summary>
        /// <param name="timeRemaining">Current time remaining</param>
        public void SyncTimeRemaining(float timeRemaining)
        {
            if (IsServer)
            {
                networkTimeRemaining.Value = timeRemaining;
            }
        }
        
        /// <summary>
        /// Synchronize team score to clients
        /// </summary>
        /// <param name="team">Team index (0 or 1)</param>
        /// <param name="score">New score value</param>
        public void SyncTeamScore(int team, int score)
        {
            if (!IsServer)
            {
                return;
            }
            
            if (team == 0)
            {
                networkTeamScoreA.Value = score;
            }
            else if (team == 1)
            {
                networkTeamScoreB.Value = score;
            }
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Team score synchronized.",
                    ("Team", team),
                    ("Score", score));
            }
        }
        
        /// <summary>
        /// Synchronize game active state to clients
        /// </summary>
        /// <param name="active">Whether game is active</param>
        public void SyncGameActive(bool active)
        {
            if (IsServer)
            {
                networkGameActive.Value = active;
                
                if (logNetworkEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Networking),
                        "Game active state synchronized.",
                        ("Active", active));
                }
            }
        }
        
        /// <summary>
        /// Synchronize winning team to clients
        /// </summary>
        /// <param name="winningTeam">Winning team index (-1 for draw/timeout)</param>
        public void SyncWinningTeam(int winningTeam)
        {
            if (IsServer)
            {
                networkWinningTeam.Value = winningTeam;
                
                if (logNetworkEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Networking),
                        "Winning team synchronized.",
                        ("WinningTeam", winningTeam));
                }
            }
        }
        
        /// <summary>
        /// Initialize client state from network values
        /// </summary>
        public void InitializeClientState()
        {
            if (IsServer)
            {
                return;
            }
            
            // Update local state from network values
            if (simpleGameManager != null)
            {
                var serviceManager = simpleGameManager.GetServiceManager();
                if (serviceManager != null)
                {
                    serviceManager.SetTeamScore(0, networkTeamScoreA.Value, notify: false);
                    serviceManager.SetTeamScore(1, networkTeamScoreB.Value, notify: false);
                }
            }
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Client state initialized from network.",
                    ("TimeRemaining", networkTimeRemaining.Value),
                    ("TeamAScore", networkTeamScoreA.Value),
                    ("TeamBScore", networkTeamScoreB.Value),
                    ("GameActive", networkGameActive.Value));
            }
        }
        
        #endregion
        
        #region Network Event Handlers
        
        /// <summary>
        /// Handle network time remaining change
        /// </summary>
        /// <param name="previous">Previous value</param>
        /// <param name="current">Current value</param>
        private void OnNetworkTimeRemainingChanged(float previous, float current)
        {
            OnNetworkTimeChanged?.Invoke(previous, current);
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network time remaining changed.",
                    ("Previous", previous),
                    ("Current", current));
            }
        }
        
        /// <summary>
        /// Handle network team A score change
        /// </summary>
        /// <param name="previous">Previous score</param>
        /// <param name="current">Current score</param>
        private void OnNetworkTeamScoreAChanged(int previous, int current)
        {
            OnNetworkScoreChanged?.Invoke(0, previous, current);
            
            // Update local scoring service if client
            if (!IsServer && simpleGameManager != null)
            {
                var serviceManager = simpleGameManager.GetServiceManager();
                serviceManager?.SetTeamScore(0, current, notify: false);
            }
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network team A score changed.",
                    ("Previous", previous),
                    ("Current", current));
            }
        }
        
        /// <summary>
        /// Handle network team B score change
        /// </summary>
        /// <param name="previous">Previous score</param>
        /// <param name="current">Current score</param>
        private void OnNetworkTeamScoreBChanged(int previous, int current)
        {
            OnNetworkScoreChanged?.Invoke(1, previous, current);
            
            // Update local scoring service if client
            if (!IsServer && simpleGameManager != null)
            {
                var serviceManager = simpleGameManager.GetServiceManager();
                serviceManager?.SetTeamScore(1, current, notify: false);
            }
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network team B score changed.",
                    ("Previous", previous),
                    ("Current", current));
            }
        }
        
        /// <summary>
        /// Handle network game active state change
        /// </summary>
        /// <param name="previous">Previous state</param>
        /// <param name="current">Current state</param>
        private void OnNetworkGameActiveChanged(bool previous, bool current)
        {
            OnNetworkGameStateChanged?.Invoke(previous, current);
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network game active state changed.",
                    ("Previous", previous),
                    ("Current", current));
            }
        }
        
        /// <summary>
        /// Handle network winning team change
        /// </summary>
        /// <param name="previous">Previous winning team</param>
        /// <param name="current">Current winning team</param>
        private void HandleNetworkWinningTeamChanged(int previous, int current)
        {
            OnNetworkWinningTeamChanged?.Invoke(previous, current);
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Network winning team changed.",
                    ("Previous", previous),
                    ("Current", current));
            }
        }
        
        /// <summary>
        /// Handle server player joined event
        /// </summary>
        /// <param name="clientId">Client ID that joined</param>
        private void HandleServerPlayerJoined(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }
            
            // Trigger player respawn
            var spawnManager = simpleGameManager?.GetSpawnManager();
            spawnManager?.SpawnPlayers();
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Player joined - respawning players.",
                    ("ClientId", clientId));
            }
        }
        
        /// <summary>
        /// Handle server player disconnected event
        /// </summary>
        /// <param name="clientId">Client ID that disconnected</param>
        private void HandleServerPlayerDisconnected(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }
            
            // Currently no additional behavior beyond avatar despawn
            // which is handled by ProductionNetworkManager
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Networking),
                    "Player disconnected.",
                    ("ClientId", clientId));
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Set auto-start behavior
        /// </summary>
        /// <param name="enabled">Whether to auto-start on enable</param>
        public void SetAutoStartOnEnable(bool enabled)
        {
            autoStartOnEnable = enabled;
            
            if (logNetworkEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Auto-start setting updated.",
                    ("Enabled", enabled));
            }
        }
        
        /// <summary>
        /// Check if auto-start should trigger
        /// </summary>
        /// <returns>True if auto-start should trigger</returns>
        public bool ShouldAutoStart()
        {
            return IsServer && autoStartOnEnable && 
                   simpleGameManager != null && !simpleGameManager.IsGameActive();
        }
        
        #endregion
    }
}