using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MOBA.Debugging;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages UI updates for the game including time display, score display, and UI event handling.
    /// Centralizes all UI-related game state presentation and user interaction.
    /// </summary>
    public class GameUIManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("UI References")]
        [SerializeField, Tooltip("Text component showing time remaining")]
        private TextMeshProUGUI timeText;
        
        [SerializeField, Tooltip("Text component showing team A score")]
        private TextMeshProUGUI teamAScoreText;
        
        [SerializeField, Tooltip("Text component showing team B score")]
        private TextMeshProUGUI teamBScoreText;
        
        [SerializeField, Tooltip("Text component showing game status")]
        private TextMeshProUGUI statusText;
        
        [SerializeField, Tooltip("Panel showing during game over")]
        private GameObject gameOverPanel;
        
        [SerializeField, Tooltip("Text showing winner in game over")]
        private TextMeshProUGUI winnerText;
        
        [SerializeField, Tooltip("Button to restart the game")]
        private Button restartButton;
        
        [Header("UI Settings")]
        [SerializeField, Tooltip("Enable detailed UI logging")]
        private bool logUIUpdates = false;
        
        [SerializeField, Tooltip("Time format string")]
        private string timeFormat = "Time: {0:F1}";
        
        [SerializeField, Tooltip("Team score format string")]
        private string scoreFormat = "Team {0}: {1}";
        
        [SerializeField, Tooltip("Default status text")]
        private string defaultStatusText = "Game Active";
        
        [SerializeField, Tooltip("Game over status text")]
        private string gameOverStatusText = "Game Over";
        
        [SerializeField, Tooltip("Winner format string")]
        private string winnerFormat = "Team {0} Wins!";
        
        [SerializeField, Tooltip("Timeout text")]
        private string timeoutText = "Time's Up! Draw!";
        
        #endregion
        
        #region State
        
        private new bool isInitialized = false;
        private float lastTimeUpdate = -1f;
        private int lastTeamAScore = -1;
        private int lastTeamBScore = -1;
        private bool lastGameActiveState = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Whether UI components are properly configured
        /// </summary>
        public bool IsUIConfigured => 
            timeText != null && 
            teamAScoreText != null && 
            teamBScoreText != null && 
            statusText != null;
        
        /// <summary>
        /// Whether game over UI is configured
        /// </summary>
        public bool IsGameOverUIConfigured => 
            gameOverPanel != null && 
            winnerText != null;
        
        /// <summary>
        /// Whether restart functionality is configured
        /// </summary>
        public bool IsRestartConfigured => restartButton != null;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Raised when restart button is clicked. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action OnRestartRequested;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            ValidateUIReferences();
            SetupUIEvents();
            InitializeUIState();
            
            isInitialized = true;
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "UI manager initialized.",
                    ("UIConfigured", IsUIConfigured),
                    ("GameOverUIConfigured", IsGameOverUIConfigured),
                    ("RestartConfigured", IsRestartConfigured));
            }
        }
        
        public override void Shutdown()
        {
            CleanupUIEvents();
            
            // Clear events
            OnRestartRequested = null;
            
            isInitialized = false;
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "UI manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        public override void UpdateComponent()
        {
            if (!isInitialized || !IsUIConfigured)
            {
                return;
            }
            
            UpdateTimeDisplay();
            UpdateScoreDisplay();
            UpdateStatusDisplay();
        }
        
        #endregion
        
        #region UI Validation
        
        /// <summary>
        /// Validate UI references and log missing components
        /// </summary>
        private void ValidateUIReferences()
        {
            if (timeText == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Time text reference is missing.");
            }
            
            if (teamAScoreText == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Team A score text reference is missing.");
            }
            
            if (teamBScoreText == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Team B score text reference is missing.");
            }
            
            if (statusText == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Status text reference is missing.");
            }
            
            if (gameOverPanel == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Game over panel reference is missing.");
            }
            
            if (winnerText == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Winner text reference is missing.");
            }
            
            if (restartButton == null)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Restart button reference is missing.");
            }
        }
        
        #endregion
        
        #region UI Event Setup
        
        /// <summary>
        /// Setup UI event listeners
        /// </summary>
        private void SetupUIEvents()
        {
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }
            
            // Subscribe to game manager events if available
            if (simpleGameManager != null)
            {
                var serviceManager = simpleGameManager.GetServiceManager();
                if (serviceManager != null)
                {
                    serviceManager.OnScoreChanged += HandleScoreChanged;
                }
                
                var networkManager = simpleGameManager.GetNetworkManager();
                if (networkManager != null)
                {
                    networkManager.OnNetworkTimeChanged += HandleNetworkTimeChanged;
                    networkManager.OnNetworkScoreChanged += HandleNetworkScoreChanged;
                    networkManager.OnNetworkGameStateChanged += HandleNetworkGameStateChanged;
                }
            }
        }
        
        /// <summary>
        /// Cleanup UI event listeners
        /// </summary>
        private void CleanupUIEvents()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
            
            // Unsubscribe from game manager events
            if (simpleGameManager != null)
            {
                var serviceManager = simpleGameManager.GetServiceManager();
                if (serviceManager != null)
                {
                    serviceManager.OnScoreChanged -= HandleScoreChanged;
                }
                
                var networkManager = simpleGameManager.GetNetworkManager();
                if (networkManager != null)
                {
                    networkManager.OnNetworkTimeChanged -= HandleNetworkTimeChanged;
                    networkManager.OnNetworkScoreChanged -= HandleNetworkScoreChanged;
                    networkManager.OnNetworkGameStateChanged -= HandleNetworkGameStateChanged;
                }
            }
        }
        
        #endregion
        
        #region UI State Initialization
        
        /// <summary>
        /// Initialize UI to default state
        /// </summary>
        private void InitializeUIState()
        {
            // Initialize time display
            UpdateTimeDisplay(force: true);
            
            // Initialize score displays
            UpdateScoreDisplay(force: true);
            
            // Initialize status
            UpdateStatusText(defaultStatusText);
            
            // Hide game over panel initially
            SetGameOverPanelActive(false);
        }
        
        #endregion
        
        #region Time Display
        
        /// <summary>
        /// Update time display from current game state
        /// </summary>
        /// <param name="force">Force update even if time hasn't changed</param>
        private void UpdateTimeDisplay(bool force = false)
        {
            if (timeText == null || simpleGameManager == null)
            {
                return;
            }
            
            float currentTime = simpleGameManager.GetTimeRemaining();
            
            if (!force && Mathf.Approximately(currentTime, lastTimeUpdate))
            {
                return;
            }
            
            lastTimeUpdate = currentTime;
            
            string timeString = string.Format(timeFormat, currentTime);
            timeText.text = timeString;
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Time display updated.",
                    ("Time", currentTime),
                    ("Text", timeString));
            }
        }
        
        /// <summary>
        /// Set time display to specific value
        /// </summary>
        /// <param name="timeRemaining">Time to display</param>
        public void SetTimeDisplay(float timeRemaining)
        {
            if (timeText == null)
            {
                return;
            }
            
            lastTimeUpdate = timeRemaining;
            
            string timeString = string.Format(timeFormat, timeRemaining);
            timeText.text = timeString;
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Time display set.",
                    ("Time", timeRemaining),
                    ("Text", timeString));
            }
        }
        
        #endregion
        
        #region Score Display
        
        /// <summary>
        /// Update score displays from current game state
        /// </summary>
        /// <param name="force">Force update even if scores haven't changed</param>
        private void UpdateScoreDisplay(bool force = false)
        {
            if ((teamAScoreText == null && teamBScoreText == null) || simpleGameManager == null)
            {
                return;
            }
            
            var serviceManager = simpleGameManager.GetServiceManager();
            if (serviceManager == null)
            {
                return;
            }
            
            int teamAScore = serviceManager.GetTeamScore(0);
            int teamBScore = serviceManager.GetTeamScore(1);
            
            if (!force && teamAScore == lastTeamAScore && teamBScore == lastTeamBScore)
            {
                return;
            }
            
            lastTeamAScore = teamAScore;
            lastTeamBScore = teamBScore;
            
            // Update team A score
            if (teamAScoreText != null)
            {
                string teamAText = string.Format(scoreFormat, "A", teamAScore);
                teamAScoreText.text = teamAText;
            }
            
            // Update team B score
            if (teamBScoreText != null)
            {
                string teamBText = string.Format(scoreFormat, "B", teamBScore);
                teamBScoreText.text = teamBText;
            }
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Score display updated.",
                    ("TeamAScore", teamAScore),
                    ("TeamBScore", teamBScore));
            }
        }
        
        /// <summary>
        /// Set team score display to specific value
        /// </summary>
        /// <param name="team">Team index (0 for A, 1 for B)</param>
        /// <param name="score">Score to display</param>
        public void SetTeamScoreDisplay(int team, int score)
        {
            if (team == 0 && teamAScoreText != null)
            {
                lastTeamAScore = score;
                string teamAText = string.Format(scoreFormat, "A", score);
                teamAScoreText.text = teamAText;
            }
            else if (team == 1 && teamBScoreText != null)
            {
                lastTeamBScore = score;
                string teamBText = string.Format(scoreFormat, "B", score);
                teamBScoreText.text = teamBText;
            }
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Team score display set.",
                    ("Team", team),
                    ("Score", score));
            }
        }
        
        #endregion
        
        #region Status Display
        
        /// <summary>
        /// Update status display from current game state
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (statusText == null || simpleGameManager == null)
            {
                return;
            }
            
            bool gameActive = simpleGameManager.IsGameActive();
            
            if (gameActive == lastGameActiveState)
            {
                return;
            }
            
            lastGameActiveState = gameActive;
            
            string status = gameActive ? defaultStatusText : gameOverStatusText;
            UpdateStatusText(status);
        }
        
        /// <summary>
        /// Update status text
        /// </summary>
        /// <param name="status">Status message to display</param>
        private void UpdateStatusText(string status)
        {
            if (statusText == null)
            {
                return;
            }
            
            statusText.text = status;
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Status text updated.",
                    ("Status", status));
            }
        }
        
        #endregion
        
        #region Game Over UI
        
        /// <summary>
        /// Show game over UI
        /// </summary>
        /// <param name="winningTeam">Winning team index (-1 for timeout/draw)</param>
        public void ShowGameOver(int winningTeam)
        {
            SetGameOverPanelActive(true);
            
            if (winnerText != null)
            {
                string winnerMessage = winningTeam >= 0 ? 
                    string.Format(winnerFormat, winningTeam == 0 ? "A" : "B") : 
                    timeoutText;
                
                winnerText.text = winnerMessage;
                
                if (logUIUpdates)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.UI),
                        "Game over UI shown.",
                        ("WinningTeam", winningTeam),
                        ("Message", winnerMessage));
                }
            }
        }
        
        /// <summary>
        /// Hide game over UI
        /// </summary>
        public void HideGameOver()
        {
            SetGameOverPanelActive(false);
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Game over UI hidden.");
            }
        }
        
        /// <summary>
        /// Set game over panel active state
        /// </summary>
        /// <param name="active">Whether panel should be active</param>
        private void SetGameOverPanelActive(bool active)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(active);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Handle restart button clicked
        /// </summary>
        private void HandleRestartClicked()
        {
            OnRestartRequested?.Invoke();
            
            if (logUIUpdates)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.UI),
                    "Restart button clicked.");
            }
        }
        
        /// <summary>
        /// Handle score changed event
        /// </summary>
        /// <param name="team">Team that scored</param>
        /// <param name="score">New score</param>
        private void HandleScoreChanged(int team, int score)
        {
            SetTeamScoreDisplay(team, score);
        }
        
        /// <summary>
        /// Handle network time changed event
        /// </summary>
        /// <param name="previous">Previous time</param>
        /// <param name="current">Current time</param>
        private void HandleNetworkTimeChanged(float previous, float current)
        {
            SetTimeDisplay(current);
        }
        
        /// <summary>
        /// Handle network score changed event
        /// </summary>
        /// <param name="team">Team index</param>
        /// <param name="previous">Previous score</param>
        /// <param name="current">Current score</param>
        private void HandleNetworkScoreChanged(int team, int previous, int current)
        {
            SetTeamScoreDisplay(team, current);
        }
        
        /// <summary>
        /// Handle network game state changed event
        /// </summary>
        /// <param name="previous">Previous state</param>
        /// <param name="current">Current state</param>
        private void HandleNetworkGameStateChanged(bool previous, bool current)
        {
            lastGameActiveState = current;
            string status = current ? defaultStatusText : gameOverStatusText;
            UpdateStatusText(status);
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Set time format string
        /// </summary>
        /// <param name="format">Format string for time display</param>
        public void SetTimeFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                timeFormat = format;
                UpdateTimeDisplay(force: true);
            }
        }
        
        /// <summary>
        /// Set score format string
        /// </summary>
        /// <param name="format">Format string for score display</param>
        public void SetScoreFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                scoreFormat = format;
                UpdateScoreDisplay(force: true);
            }
        }
        
        /// <summary>
        /// Set UI logging enabled
        /// </summary>
        /// <param name="enabled">Whether to log UI updates</param>
        public void SetUILogging(bool enabled)
        {
            logUIUpdates = enabled;
        }
        
        #endregion
    }
}