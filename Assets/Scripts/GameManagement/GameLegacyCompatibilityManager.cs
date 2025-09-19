using UnityEngine;
using Unity.Netcode;
using MOBA.Services;
using MOBA.Debugging;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages legacy compatibility and fallback behavior for backward compatibility.
    /// Handles network variable synchronization, legacy UI updates, and deprecated method support.
    /// Allows gradual migration from legacy systems to new component-based architecture.
    /// </summary>
    public class GameLegacyCompatibilityManager : NetworkBehaviour
    {
        #region Configuration
        
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool enableLegacyNetworkSync = true;
        [SerializeField] private bool enableLegacyUIUpdates = true;
        [SerializeField] private bool logLegacyWarnings = true;
        
        #endregion
        
        #region Legacy Network Variables
        
        private readonly NetworkVariable<float> networkTimeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkTeamScoreA = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkTeamScoreB = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> networkGameActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkWinningTeam = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        
        #endregion
        
        #region Legacy State
        
        private float clientTimeRemaining;
        private bool gameActive = false;
        private IScoringService scoringService;
        private IMatchLifecycleService matchLifecycleService;
        
        #endregion
        
        #region Legacy Events
        
        /// <summary>
        /// Legacy game end event. DEPRECATED: Use modern event system instead.
        /// </summary>
        public System.Action<int> OnGameEnd;
        
        /// <summary>
        /// Legacy score update event. DEPRECATED: Use modern event system instead.
        /// </summary>
        public System.Action<int, int> OnScoreUpdate;
        
        #endregion
        
        #region Legacy UI References
        
        [Header("Legacy UI References")]
        [SerializeField] private UnityEngine.UI.Text timeText;
        [SerializeField] private UnityEngine.UI.Text scoreText;
        
        #endregion
        
        #region Initialization
        
        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General)
        {
            return new GameDebugContext(
                GameDebugCategory.GameLifecycle,
                GameDebugSystemTag.GameLifecycle,
                mechanic,
                subsystem: nameof(GameLegacyCompatibilityManager),
                actor: gameObject != null ? gameObject.name : null);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            // Subscribe to network variable changes
            if (enableLegacyNetworkSync)
            {
                networkTimeRemaining.OnValueChanged += OnNetworkTimeChanged;
                networkTeamScoreA.OnValueChanged += OnNetworkScoreAChanged;
                networkTeamScoreB.OnValueChanged += OnNetworkScoreBChanged;
                networkGameActive.OnValueChanged += OnNetworkGameActiveChanged;
                networkWinningTeam.OnValueChanged += OnNetworkWinningTeamChanged;
            }
            
            // Initialize legacy services
            InitializeLegacyServices();
            
            if (logLegacyWarnings)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Configuration),
                    "Legacy compatibility manager initialized. Consider migrating to modern component system.");
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            // Unsubscribe from network variable changes
            if (enableLegacyNetworkSync)
            {
                networkTimeRemaining.OnValueChanged -= OnNetworkTimeChanged;
                networkTeamScoreA.OnValueChanged -= OnNetworkScoreAChanged;
                networkTeamScoreB.OnValueChanged -= OnNetworkScoreBChanged;
                networkGameActive.OnValueChanged -= OnNetworkGameActiveChanged;
                networkWinningTeam.OnValueChanged -= OnNetworkWinningTeamChanged;
            }
            
            // Cleanup legacy services
            CleanupLegacyServices();
        }
        
        private void InitializeLegacyServices()
        {
            // Initialize legacy services for backward compatibility
            ServiceRegistry.TryResolve<IScoringService>(out scoringService);
            ServiceRegistry.TryResolve<IMatchLifecycleService>(out matchLifecycleService);
            
            // Subscribe to legacy service events
            if (scoringService != null)
            {
                scoringService.ScoreChanged += HandleScoreChanged;
            }
            if (matchLifecycleService != null)
            {
                matchLifecycleService.MatchEnded += HandleMatchEnded;
            }
        }
        
        private void CleanupLegacyServices()
        {
            // Cleanup legacy services
            if (scoringService != null)
            {
                scoringService.ScoreChanged -= HandleScoreChanged;
            }
            if (matchLifecycleService != null)
            {
                matchLifecycleService.MatchEnded -= HandleMatchEnded;
            }
            
            // Clear events
            OnGameEnd = null;
            OnScoreUpdate = null;
        }
        
        #endregion
        
        #region Legacy Network Event Handlers
        
        private void OnNetworkTimeChanged(float previous, float current)
        {
            clientTimeRemaining = current;
            if (enableLegacyUIUpdates)
            {
                UpdateLegacyUI();
            }
        }

        private void OnNetworkScoreAChanged(int previous, int current)
        {
            scoringService?.SetScore(0, current, notify: false);
            if (!IsServer)
            {
                OnScoreUpdate?.Invoke(0, current);
                if (enableLegacyUIUpdates)
                {
                    UpdateLegacyUI();
                }
            }
        }

        private void OnNetworkScoreBChanged(int previous, int current)
        {
            scoringService?.SetScore(1, current, notify: false);
            if (!IsServer)
            {
                OnScoreUpdate?.Invoke(1, current);
                if (enableLegacyUIUpdates)
                {
                    UpdateLegacyUI();
                }
            }
        }

        private void OnNetworkGameActiveChanged(bool previous, bool current)
        {
            gameActive = current;
            if (!IsServer && !current)
            {
                int winningTeam = networkWinningTeam.Value;
                OnGameEnd?.Invoke(winningTeam);
            }
            if (enableLegacyUIUpdates)
            {
                UpdateLegacyUI();
            }
        }

        private void OnNetworkWinningTeamChanged(int previous, int current)
        {
            // Clients rely on OnNetworkGameActiveChanged to raise game end events.
        }
        
        #endregion
        
        #region Legacy Service Event Handlers
        
        private void HandleScoreChanged(int team, int score)
        {
            if (IsServer && enableLegacyNetworkSync)
            {
                if (team == 0)
                {
                    networkTeamScoreA.Value = score;
                }
                else if (team == 1)
                {
                    networkTeamScoreB.Value = score;
                }
            }

            OnScoreUpdate?.Invoke(team, score);
            
            if (enableLegacyUIUpdates)
            {
                UpdateLegacyUI();
            }
        }

        private void HandleMatchEnded(int winningTeam)
        {
            if (IsServer && enableLegacyNetworkSync)
            {
                networkWinningTeam.Value = winningTeam;
                networkGameActive.Value = false;
            }

            OnGameEnd?.Invoke(winningTeam);
            
            if (enableLegacyUIUpdates)
            {
                UpdateLegacyUI();
            }
        }
        
        #endregion
        
        #region Legacy UI Updates
        
        private void UpdateLegacyUI()
        {
            if (!enableLegacyUIUpdates) return;
            
            // Update time display
            if (timeText != null)
            {
                float timeRemaining = IsServer ? 
                    (matchLifecycleService?.TimeRemaining ?? 0f) : 
                    clientTimeRemaining;
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }

            // Update score display
            if (scoreText != null)
            {
                int scoreA = IsServer ? 
                    (scoringService?.GetScore(0) ?? 0) : 
                    networkTeamScoreA.Value;
                int scoreB = IsServer ? 
                    (scoringService?.GetScore(1) ?? 0) : 
                    networkTeamScoreB.Value;
                scoreText.text = $"Team A: {scoreA} | Team B: {scoreB}";
            }
        }
        
        #endregion
        
        #region Legacy API
        
        /// <summary>
        /// Legacy method to get game active state
        /// </summary>
        public bool IsGameActive()
        {
            return matchLifecycleService?.IsActive ?? gameActive;
        }
        
        /// <summary>
        /// Legacy method to get time remaining
        /// </summary>
        public float GetTimeRemaining()
        {
            return IsServer ? (matchLifecycleService?.TimeRemaining ?? 0f) : clientTimeRemaining;
        }
        
        /// <summary>
        /// Legacy method to get team score
        /// </summary>
        public int GetScore(int team)
        {
            return scoringService?.GetScore(team) ?? 0;
        }
        
        /// <summary>
        /// Legacy method to sync network time (server only)
        /// </summary>
        public void SyncNetworkTime(float timeRemaining)
        {
            if (IsServer && enableLegacyNetworkSync)
            {
                networkTimeRemaining.Value = timeRemaining;
            }
        }
        
        /// <summary>
        /// Legacy method to sync network game state (server only)
        /// </summary>
        public void SyncNetworkGameActive(bool active)
        {
            if (IsServer && enableLegacyNetworkSync)
            {
                networkGameActive.Value = active;
            }
        }
        
        #endregion
        
        #region Public Configuration
        
        /// <summary>
        /// Configure legacy UI references for backward compatibility
        /// </summary>
        public void SetLegacyUIReferences(UnityEngine.UI.Text timeTextRef, UnityEngine.UI.Text scoreTextRef)
        {
            timeText = timeTextRef;
            scoreText = scoreTextRef;
            
            if (enableLegacyUIUpdates)
            {
                UpdateLegacyUI();
            }
        }
        
        /// <summary>
        /// Enable or disable legacy network synchronization
        /// </summary>
        public void SetLegacyNetworkSync(bool enabled)
        {
            enableLegacyNetworkSync = enabled;
        }
        
        /// <summary>
        /// Enable or disable legacy UI updates
        /// </summary>
        public void SetLegacyUIUpdates(bool enabled)
        {
            enableLegacyUIUpdates = enabled;
        }
        
        #endregion
    }
}