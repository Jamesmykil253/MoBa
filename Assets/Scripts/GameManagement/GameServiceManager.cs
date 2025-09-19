using UnityEngine;
using MOBA.Services;
using MOBA.Debugging;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages game services including scoring and match lifecycle services.
    /// Handles service registration, resolution, and coordination between services.
    /// </summary>
    public class GameServiceManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("Service Settings")]
        [SerializeField, Tooltip("Enable detailed service logging")]
        private bool logServiceEvents = true;
        
        [SerializeField, Tooltip("Default number of teams for scoring")]
        private int defaultTeamCount = 2;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Scoring service instance
        /// </summary>
        private IScoringService scoringService;
        
        /// <summary>
        /// Match lifecycle service instance
        /// </summary>
        private IMatchLifecycleService matchLifecycleService;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current scoring service
        /// </summary>
        public IScoringService ScoringService => scoringService;
        
        /// <summary>
        /// Current match lifecycle service
        /// </summary>
        public IMatchLifecycleService MatchLifecycleService => matchLifecycleService;
        
        /// <summary>
        /// Default team count for services
        /// </summary>
        public int DefaultTeamCount => defaultTeamCount;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when scoring service changes score. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int, int> OnScoreChanged; // team, score
        
        /// <summary>
        /// Raised when match lifecycle service ends match. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnMatchEnded; // winning team
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            InitializeServices();
            
            if (logServiceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Initialization),
                    "Service manager initialized.",
                    ("ScoringService", scoringService != null),
                    ("MatchLifecycleService", matchLifecycleService != null));
            }
        }
        
        public override void Shutdown()
        {
            ShutdownServices();
            
            // Clear events
            OnScoreChanged = null;
            OnMatchEnded = null;
            
            if (logServiceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Lifecycle),
                    "Service manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Service Management
        
        /// <summary>
        /// Initialize all game services
        /// </summary>
        private void InitializeServices()
        {
            InitializeScoringService();
            InitializeMatchLifecycleService();
            ConnectServiceEvents();
        }
        
        /// <summary>
        /// Initialize scoring service
        /// </summary>
        private void InitializeScoringService()
        {
            // Disconnect existing service
            if (scoringService != null)
            {
                scoringService.ScoreChanged -= HandleScoreChanged;
            }
            
            // Try to resolve existing service or create new one
            if (!ServiceRegistry.TryResolve<IScoringService>(out scoringService))
            {
                scoringService = new ScoringService(defaultTeamCount);
                ServiceRegistry.Register<IScoringService>(scoringService, overwrite: false);
                
                if (logServiceEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Initialization),
                        "Created new scoring service.",
                        ("TeamCount", defaultTeamCount));
                }
            }
            else
            {
                if (logServiceEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Initialization),
                        "Resolved existing scoring service.");
                }
            }
            
            // Connect events
            scoringService.ScoreChanged += HandleScoreChanged;
        }
        
        /// <summary>
        /// Initialize match lifecycle service
        /// </summary>
        private void InitializeMatchLifecycleService()
        {
            // Disconnect existing service
            if (matchLifecycleService != null)
            {
                matchLifecycleService.MatchEnded -= HandleMatchEnded;
            }
            
            // Try to resolve existing service or create new one
            if (!ServiceRegistry.TryResolve<IMatchLifecycleService>(out matchLifecycleService))
            {
                matchLifecycleService = new MatchLifecycleService(scoringService);
                ServiceRegistry.Register<IMatchLifecycleService>(matchLifecycleService, overwrite: false);
                
                if (logServiceEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Initialization),
                        "Created new match lifecycle service.");
                }
            }
            else
            {
                if (logServiceEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Initialization),
                        "Resolved existing match lifecycle service.");
                }
            }
            
            // Configure service
            if (simpleGameManager != null)
            {
                matchLifecycleService.Configure(simpleGameManager.gameTime, simpleGameManager.scoreToWin);
            }
            
            // Connect events
            matchLifecycleService.MatchEnded += HandleMatchEnded;
        }
        
        /// <summary>
        /// Connect service events to main game manager
        /// </summary>
        private void ConnectServiceEvents()
        {
            if (simpleGameManager != null)
            {
                OnScoreChanged += simpleGameManager.HandleScoreChanged;
                OnMatchEnded += simpleGameManager.HandleMatchEnded;
            }
        }
        
        /// <summary>
        /// Shutdown all services
        /// </summary>
        private void ShutdownServices()
        {
            // Disconnect scoring service
            if (scoringService != null)
            {
                scoringService.ScoreChanged -= HandleScoreChanged;
                scoringService = null;
            }
            
            // Disconnect match lifecycle service
            if (matchLifecycleService != null)
            {
                matchLifecycleService.MatchEnded -= HandleMatchEnded;
                matchLifecycleService = null;
            }
        }
        
        /// <summary>
        /// Reconfigure services with new settings
        /// </summary>
        /// <param name="gameTime">Game duration in seconds</param>
        /// <param name="scoreToWin">Score required to win</param>
        public void ReconfigureServices(float gameTime, int scoreToWin)
        {
            matchLifecycleService?.Configure(gameTime, scoreToWin);
            
            if (logServiceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Services reconfigured.",
                    ("GameTime", gameTime),
                    ("ScoreToWin", scoreToWin));
            }
        }
        
        #endregion
        
        #region Service Access
        
        /// <summary>
        /// Get current time remaining in match
        /// </summary>
        /// <returns>Time remaining in seconds</returns>
        public float GetTimeRemaining()
        {
            return matchLifecycleService?.TimeRemaining ?? 0f;
        }
        
        /// <summary>
        /// Check if match is currently active
        /// </summary>
        /// <returns>True if match is active</returns>
        public bool IsMatchActive()
        {
            return matchLifecycleService?.IsActive ?? false;
        }
        
        /// <summary>
        /// Get score for specific team
        /// </summary>
        /// <param name="team">Team index</param>
        /// <returns>Team score</returns>
        public int GetTeamScore(int team)
        {
            return scoringService?.GetScore(team) ?? 0;
        }
        
        /// <summary>
        /// Add score for specific team
        /// </summary>
        /// <param name="team">Team index</param>
        /// <param name="points">Points to add</param>
        /// <returns>True if score was added successfully</returns>
        public bool AddTeamScore(int team, int points = 1)
        {
            if (scoringService != null && IsMatchActive())
            {
                return scoringService.AddScore(team, points);
            }
            
            return false;
        }
        
        /// <summary>
        /// Set score for specific team
        /// </summary>
        /// <param name="team">Team index</param>
        /// <param name="score">New score value</param>
        /// <param name="notify">Whether to notify listeners</param>
        public void SetTeamScore(int team, int score, bool notify = true)
        {
            scoringService?.SetScore(team, score, notify);
        }
        
        /// <summary>
        /// Start a new match
        /// </summary>
        /// <returns>True if match started successfully</returns>
        public bool StartMatch()
        {
            if (matchLifecycleService == null)
            {
                if (logServiceEvents)
                {
                    GameDebug.LogError(
                        BuildContext(GameDebugMechanicTag.Lifecycle),
                        "Cannot start match - match lifecycle service not available.");
                }
                return false;
            }
            
            bool started = matchLifecycleService.StartMatch();
            
            if (logServiceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Lifecycle),
                    "Match start requested.",
                    ("Success", started));
            }
            
            return started;
        }
        
        /// <summary>
        /// Stop current match
        /// </summary>
        /// <param name="winningTeam">Winning team (-1 for draw/timeout)</param>
        public void StopMatch(int winningTeam = -1)
        {
            if (matchLifecycleService != null)
            {
                matchLifecycleService.StopMatch(winningTeam);
                
                if (logServiceEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Lifecycle),
                        "Match stop requested.",
                        ("WinningTeam", winningTeam));
                }
            }
        }
        
        /// <summary>
        /// Tick match lifecycle (update timers, check win conditions)
        /// </summary>
        /// <param name="deltaTime">Time since last tick</param>
        public void TickMatch(float deltaTime)
        {
            if (matchLifecycleService != null && IsMatchActive())
            {
                matchLifecycleService.Tick(deltaTime);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle score change from scoring service
        /// </summary>
        /// <param name="team">Team that scored</param>
        /// <param name="score">New score</param>
        private void HandleScoreChanged(int team, int score)
        {
            OnScoreChanged?.Invoke(team, score);
            
            if (logServiceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Score),
                    "Score changed.",
                    ("Team", team),
                    ("NewScore", score));
            }
        }
        
        /// <summary>
        /// Handle match end from match lifecycle service
        /// </summary>
        /// <param name="winningTeam">Winning team</param>
        private void HandleMatchEnded(int winningTeam)
        {
            OnMatchEnded?.Invoke(winningTeam);
            
            if (logServiceEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Lifecycle),
                    "Match ended.",
                    ("WinningTeam", winningTeam));
            }
        }
        
        #endregion
    }
}