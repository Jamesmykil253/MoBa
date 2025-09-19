using UnityEngine;
using MOBA.Debugging;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages match lifecycle including match states, timing, and lifecycle coordination.
    /// Handles match flow from initialization through completion and cleanup.
    /// </summary>
    public class GameMatchLifecycleManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("Match Settings")]
        [SerializeField, Tooltip("Default match duration in seconds")]
        private float defaultMatchDuration = 300f; // 5 minutes
        
        [SerializeField, Tooltip("Countdown duration before match starts")]
        private float preMatchCountdown = 3f;
        
        [SerializeField, Tooltip("Duration to show results after match ends")]
        private float postMatchDuration = 5f;
        
        [Header("Timing")]
        [SerializeField, Tooltip("Enable precise timing updates")]
        private bool usePreciseTiming = true;
        
        [SerializeField, Tooltip("Time update frequency in seconds")]
        private float timeUpdateFrequency = 0.1f;
        
        [Header("Lifecycle Logging")]
        [SerializeField, Tooltip("Enable detailed lifecycle logging")]
        private bool logLifecycleEvents = true;
        
        [SerializeField, Tooltip("Enable timing debug logs")]
        private bool logTimingUpdates = false;
        
        #endregion
        
        #region Match State
        
        /// <summary>
        /// Current match states
        /// </summary>
        public enum MatchState
        {
            Uninitialized,
            Initializing,
            PreMatch,
            Active,
            Ending,
            Completed,
            Cleanup
        }
        
        private MatchState currentState = MatchState.Uninitialized;
        private float matchDuration;
        private float timeRemaining;
        private float lastTimeUpdate;
        private float stateStartTime;
        private int winningTeam = -1;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current match state
        /// </summary>
        public MatchState CurrentState => currentState;
        
        /// <summary>
        /// Current match duration setting
        /// </summary>
        public float MatchDuration => matchDuration;
        
        /// <summary>
        /// Time remaining in current match
        /// </summary>
        public float TimeRemaining => timeRemaining;
        
        /// <summary>
        /// Whether match is currently active
        /// </summary>
        public bool IsMatchActive => currentState == MatchState.Active;
        
        /// <summary>
        /// Whether match has ended
        /// </summary>
        public bool IsMatchEnded => currentState == MatchState.Ending || 
                                   currentState == MatchState.Completed || 
                                   currentState == MatchState.Cleanup;
        
        /// <summary>
        /// Current winning team (-1 if no winner or draw)
        /// </summary>
        public int WinningTeam => winningTeam;
        
        /// <summary>
        /// Time elapsed in current state
        /// </summary>
        public float StateElapsedTime => Time.time - stateStartTime;
        
        /// <summary>
        /// Whether timing should be managed by this component
        /// </summary>
        public bool ShouldManageTiming => simpleGameManager != null && simpleGameManager.IsServer;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when match state changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<MatchState, MatchState> OnMatchStateChanged; // previous, current
        
        /// <summary>
        /// Raised when match starts. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action OnMatchStarted;
        
        /// <summary>
        /// Raised when match ends. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int, bool> OnMatchEnded; // winningTeam, isTimeout
        
        /// <summary>
        /// Raised when time updates. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<float> OnTimeUpdate; // timeRemaining
        
        /// <summary>
        /// Raised during pre-match countdown. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnCountdownUpdate; // secondsRemaining
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            InitializeMatchSettings();
            SetupEventSubscriptions();
            
            ChangeState(MatchState.Initializing);
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match lifecycle manager initialized.",
                    ("DefaultDuration", defaultMatchDuration),
                    ("ManagesTiming", ShouldManageTiming));
            }
        }
        
        public override void Shutdown()
        {
            CleanupEventSubscriptions();
            
            // Clear events
            OnMatchStateChanged = null;
            OnMatchStarted = null;
            OnMatchEnded = null;
            OnTimeUpdate = null;
            OnCountdownUpdate = null;
            
            ChangeState(MatchState.Cleanup);
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match lifecycle manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        public override void UpdateComponent()
        {
            if (!ShouldManageTiming)
            {
                return;
            }
            
            UpdateStateMachine();
            UpdateTiming();
        }
        
        #endregion
        
        #region Match Settings
        
        /// <summary>
        /// Initialize match settings from configuration
        /// </summary>
        private void InitializeMatchSettings()
        {
            // Get duration from configuration if available
            if (simpleGameManager != null)
            {
                var configManager = simpleGameManager.GetConfigurationManager();
                if (configManager != null && configManager.HasValidConfig())
                {
                    var config = configManager.GetCurrentConfig();
                    if (config != null && config.matchDuration > 0)
                    {
                        matchDuration = config.matchDuration;
                    }
                    else
                    {
                        matchDuration = defaultMatchDuration;
                    }
                }
                else
                {
                    matchDuration = defaultMatchDuration;
                }
            }
            else
            {
                matchDuration = defaultMatchDuration;
            }
            
            timeRemaining = matchDuration;
            lastTimeUpdate = Time.time;
        }
        
        #endregion
        
        #region Event Setup
        
        /// <summary>
        /// Setup event subscriptions to other components
        /// </summary>
        private void SetupEventSubscriptions()
        {
            if (simpleGameManager == null)
            {
                return;
            }
            
            // Subscribe to service manager events
            var serviceManager = simpleGameManager.GetServiceManager();
            if (serviceManager != null)
            {
                serviceManager.OnScoreChanged += HandleScoreChanged;
            }
            
            // Subscribe to UI manager events
            var uiManager = simpleGameManager.GetUIManager();
            if (uiManager != null)
            {
                uiManager.OnRestartRequested += HandleRestartRequested;
            }
        }
        
        /// <summary>
        /// Cleanup event subscriptions
        /// </summary>
        private void CleanupEventSubscriptions()
        {
            if (simpleGameManager == null)
            {
                return;
            }
            
            // Unsubscribe from service manager events
            var serviceManager = simpleGameManager.GetServiceManager();
            if (serviceManager != null)
            {
                serviceManager.OnScoreChanged -= HandleScoreChanged;
            }
            
            // Unsubscribe from UI manager events
            var uiManager = simpleGameManager.GetUIManager();
            if (uiManager != null)
            {
                uiManager.OnRestartRequested -= HandleRestartRequested;
            }
        }
        
        #endregion
        
        #region State Management
        
        /// <summary>
        /// Change match state
        /// </summary>
        /// <param name="newState">New state to transition to</param>
        private void ChangeState(MatchState newState)
        {
            var previousState = currentState;
            currentState = newState;
            stateStartTime = Time.time;
            
            OnMatchStateChanged?.Invoke(previousState, currentState);
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match state changed.",
                    ("Previous", previousState),
                    ("Current", currentState),
                    ("StateTime", stateStartTime));
            }
            
            // Handle state entry logic
            HandleStateEntry(newState, previousState);
        }
        
        /// <summary>
        /// Handle state entry logic
        /// </summary>
        /// <param name="newState">New state entered</param>
        /// <param name="previousState">Previous state</param>
        private void HandleStateEntry(MatchState newState, MatchState previousState)
        {
            switch (newState)
            {
                case MatchState.PreMatch:
                    HandlePreMatchEntry();
                    break;
                    
                case MatchState.Active:
                    HandleActiveEntry();
                    break;
                    
                case MatchState.Ending:
                    HandleEndingEntry();
                    break;
                    
                case MatchState.Completed:
                    HandleCompletedEntry();
                    break;
                    
                case MatchState.Cleanup:
                    HandleCleanupEntry();
                    break;
            }
        }
        
        /// <summary>
        /// Update state machine logic
        /// </summary>
        private void UpdateStateMachine()
        {
            switch (currentState)
            {
                case MatchState.Initializing:
                    UpdateInitializingState();
                    break;
                    
                case MatchState.PreMatch:
                    UpdatePreMatchState();
                    break;
                    
                case MatchState.Active:
                    UpdateActiveState();
                    break;
                    
                case MatchState.Ending:
                    UpdateEndingState();
                    break;
                    
                case MatchState.Completed:
                    UpdateCompletedState();
                    break;
            }
        }
        
        #endregion
        
        #region State Handlers
        
        /// <summary>
        /// Handle pre-match state entry
        /// </summary>
        private void HandlePreMatchEntry()
        {
            // Reset match state
            timeRemaining = matchDuration;
            winningTeam = -1;
            
            // Ensure players are spawned
            var spawnManager = simpleGameManager?.GetSpawnManager();
            spawnManager?.SpawnPlayers();
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Pre-match state entered.",
                    ("CountdownDuration", preMatchCountdown));
            }
        }
        
        /// <summary>
        /// Handle active state entry
        /// </summary>
        private void HandleActiveEntry()
        {
            OnMatchStarted?.Invoke();
            
            // Sync game active state
            var networkManager = simpleGameManager?.GetNetworkManager();
            networkManager?.SyncGameActive(true);
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match started.",
                    ("Duration", matchDuration));
            }
        }
        
        /// <summary>
        /// Handle ending state entry
        /// </summary>
        private void HandleEndingEntry()
        {
            bool isTimeout = timeRemaining <= 0f;
            
            // Determine winner if not already set
            if (winningTeam == -1 && !isTimeout)
            {
                DetermineWinnerFromScore();
            }
            
            OnMatchEnded?.Invoke(winningTeam, isTimeout);
            
            // Sync network state
            var networkManager = simpleGameManager?.GetNetworkManager();
            if (networkManager != null)
            {
                networkManager.SyncGameActive(false);
                networkManager.SyncWinningTeam(winningTeam);
            }
            
            // Show game over UI
            var uiManager = simpleGameManager?.GetUIManager();
            uiManager?.ShowGameOver(winningTeam);
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match ended.",
                    ("WinningTeam", winningTeam),
                    ("IsTimeout", isTimeout));
            }
        }
        
        /// <summary>
        /// Handle completed state entry
        /// </summary>
        private void HandleCompletedEntry()
        {
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match completed.");
            }
        }
        
        /// <summary>
        /// Handle cleanup state entry
        /// </summary>
        private void HandleCleanupEntry()
        {
            // Hide game over UI
            var uiManager = simpleGameManager?.GetUIManager();
            uiManager?.HideGameOver();
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match cleanup.");
            }
        }
        
        #endregion
        
        #region State Updates
        
        /// <summary>
        /// Update initializing state
        /// </summary>
        private void UpdateInitializingState()
        {
            // Check if ready to start pre-match
            if (CanStartPreMatch())
            {
                ChangeState(MatchState.PreMatch);
            }
        }
        
        /// <summary>
        /// Update pre-match state
        /// </summary>
        private void UpdatePreMatchState()
        {
            float countdown = preMatchCountdown - StateElapsedTime;
            
            if (countdown > 0f)
            {
                int secondsRemaining = Mathf.CeilToInt(countdown);
                OnCountdownUpdate?.Invoke(secondsRemaining);
            }
            else
            {
                ChangeState(MatchState.Active);
            }
        }
        
        /// <summary>
        /// Update active state
        /// </summary>
        private void UpdateActiveState()
        {
            // Check for timeout
            if (timeRemaining <= 0f)
            {
                ChangeState(MatchState.Ending);
                return;
            }
            
            // Check for early end conditions
            if (ShouldEndMatchEarly())
            {
                ChangeState(MatchState.Ending);
            }
        }
        
        /// <summary>
        /// Update ending state
        /// </summary>
        private void UpdateEndingState()
        {
            // Auto-transition to completed after showing results
            if (StateElapsedTime >= postMatchDuration)
            {
                ChangeState(MatchState.Completed);
            }
        }
        
        /// <summary>
        /// Update completed state
        /// </summary>
        private void UpdateCompletedState()
        {
            // Currently no auto-transition from completed state
            // Requires explicit restart or cleanup
        }
        
        #endregion
        
        #region Timing Management
        
        /// <summary>
        /// Update match timing
        /// </summary>
        private void UpdateTiming()
        {
            if (currentState != MatchState.Active)
            {
                return;
            }
            
            float currentTime = Time.time;
            
            // Update time remaining
            if (usePreciseTiming || currentTime - lastTimeUpdate >= timeUpdateFrequency)
            {
                float deltaTime = currentTime - lastTimeUpdate;
                timeRemaining = Mathf.Max(0f, timeRemaining - deltaTime);
                lastTimeUpdate = currentTime;
                
                // Sync to network
                var networkManager = simpleGameManager?.GetNetworkManager();
                networkManager?.SyncTimeRemaining(timeRemaining);
                
                // Notify listeners
                OnTimeUpdate?.Invoke(timeRemaining);
                
                if (logTimingUpdates)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.MatchLifecycle),
                        "Time updated.",
                        ("TimeRemaining", timeRemaining),
                        ("DeltaTime", deltaTime));
                }
            }
        }
        
        #endregion
        
        #region Match Control
        
        /// <summary>
        /// Start match if ready
        /// </summary>
        public void StartMatch()
        {
            if (currentState == MatchState.Initializing || currentState == MatchState.Completed)
            {
                ChangeState(MatchState.PreMatch);
            }
            else if (currentState == MatchState.PreMatch)
            {
                ChangeState(MatchState.Active);
            }
        }
        
        /// <summary>
        /// End match with specified winner
        /// </summary>
        /// <param name="winningTeam">Winning team (-1 for draw)</param>
        public void EndMatch(int winningTeam = -1)
        {
            if (currentState == MatchState.Active)
            {
                this.winningTeam = winningTeam;
                ChangeState(MatchState.Ending);
            }
        }
        
        /// <summary>
        /// Restart match
        /// </summary>
        public void RestartMatch()
        {
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.MatchLifecycle),
                    "Match restart requested.");
            }
            
            // Reset to initializing state
            ChangeState(MatchState.Initializing);
            
            // Reset timing
            InitializeMatchSettings();
        }
        
        #endregion
        
        #region Match Logic
        
        /// <summary>
        /// Check if pre-match can start
        /// </summary>
        /// <returns>True if pre-match can start</returns>
        private bool CanStartPreMatch()
        {
            // Basic readiness check - can be extended for more complex conditions
            return simpleGameManager != null;
        }
        
        /// <summary>
        /// Check if match should end early
        /// </summary>
        /// <returns>True if match should end early</returns>
        private bool ShouldEndMatchEarly()
        {
            // Implementation for early end conditions (e.g., score limit reached)
            // Currently no early end conditions implemented
            return false;
        }
        
        /// <summary>
        /// Determine winner from current scores
        /// </summary>
        private void DetermineWinnerFromScore()
        {
            var serviceManager = simpleGameManager?.GetServiceManager();
            if (serviceManager == null)
            {
                winningTeam = -1;
                return;
            }
            
            int teamAScore = serviceManager.GetTeamScore(0);
            int teamBScore = serviceManager.GetTeamScore(1);
            
            if (teamAScore > teamBScore)
            {
                winningTeam = 0;
            }
            else if (teamBScore > teamAScore)
            {
                winningTeam = 1;
            }
            else
            {
                winningTeam = -1; // Draw
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle score changed event
        /// </summary>
        /// <param name="team">Team that scored</param>
        /// <param name="score">New score</param>
        private void HandleScoreChanged(int team, int score)
        {
            // Check for early end conditions based on score
            // Currently no score-based early end implemented
        }
        
        /// <summary>
        /// Handle restart requested event
        /// </summary>
        private void HandleRestartRequested()
        {
            if (currentState == MatchState.Completed || currentState == MatchState.Ending)
            {
                RestartMatch();
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Set match duration
        /// </summary>
        /// <param name="duration">Match duration in seconds</param>
        public void SetMatchDuration(float duration)
        {
            matchDuration = Mathf.Max(1f, duration);
            
            // Reset time remaining if not currently in active match
            if (currentState != MatchState.Active)
            {
                timeRemaining = matchDuration;
            }
            
            if (logLifecycleEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Match duration updated.",
                    ("Duration", matchDuration));
            }
        }
        
        /// <summary>
        /// Set pre-match countdown duration
        /// </summary>
        /// <param name="countdown">Countdown duration in seconds</param>
        public void SetPreMatchCountdown(float countdown)
        {
            preMatchCountdown = Mathf.Max(0f, countdown);
        }
        
        /// <summary>
        /// Set post-match duration
        /// </summary>
        /// <param name="duration">Post-match duration in seconds</param>
        public void SetPostMatchDuration(float duration)
        {
            postMatchDuration = Mathf.Max(0f, duration);
        }
        
        /// <summary>
        /// Set precise timing enabled
        /// </summary>
        /// <param name="enabled">Whether to use precise timing</param>
        public void SetPreciseTiming(bool enabled)
        {
            usePreciseTiming = enabled;
        }
        
        /// <summary>
        /// Set lifecycle logging enabled
        /// </summary>
        /// <param name="enabled">Whether to log lifecycle events</param>
        public void SetLifecycleLogging(bool enabled)
        {
            logLifecycleEvents = enabled;
        }
        
        #endregion
    }
}