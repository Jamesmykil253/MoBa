using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MOBA.Training;

namespace MOBA.UI
{
    /// <summary>
    /// UI controller for the local training lobby
    /// Provides visual feedback and controls for training session
    /// </summary>
    public class TrainingLobbyUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject trainingLobbyPanel;
        [SerializeField] private GameObject trainingActivePanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("Main Menu Elements")]
        [SerializeField] private Button startTrainingButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Training Lobby Elements")]
        [SerializeField] private TextMeshProUGUI lobbyStateText;
        [SerializeField] private Button enterTrainingButton;
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TextMeshProUGUI progressText;
        
        [Header("Training Active Elements")]
        [SerializeField] private TextMeshProUGUI sessionTimeText;
        [SerializeField] private TextMeshProUGUI spawnsText;
        [SerializeField] private TextMeshProUGUI deathsText;
        [SerializeField] private Button respawnButton;
        [SerializeField] private Button endTrainingButton;
        [SerializeField] private Toggle godModeToggle;
        [SerializeField] private Toggle instantRespawnToggle;
        
        [Header("Settings Elements")]
        [SerializeField] private Toggle autoStartToggle;
        [SerializeField] private Slider playerCountSlider;
        [SerializeField] private Toggle enableBotsToggle;
        [SerializeField] private Button saveSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        
        // Components
        private LocalTrainingLobby trainingLobby;
        
        // State
        private bool isUIInitialized = false;
        
        private void Start()
        {
            InitializeUI();
            FindTrainingComponents();
            SetupEventListeners();
            
            // Start with main menu
            ShowMainMenu();
        }
        
        private void InitializeUI()
        {
            // Create UI if not assigned
            if (mainMenuPanel == null) CreateMainMenuPanel();
            if (trainingLobbyPanel == null) CreateTrainingLobbyPanel();
            if (trainingActivePanel == null) CreateTrainingActivePanel();
            if (settingsPanel == null) CreateSettingsPanel();
            
            isUIInitialized = true;
            Debug.Log("[TrainingLobbyUI] UI initialized");
        }
        
        private void FindTrainingComponents()
        {
            // Find training lobby component
            trainingLobby = FindFirstObjectByType<LocalTrainingLobby>();
            if (trainingLobby == null)
            {
                Debug.LogWarning("[TrainingLobbyUI] LocalTrainingLobby component not found");
            }
            
            // Training game manager will be found later when created
        }
        
        private void SetupEventListeners()
        {
            // Main menu buttons
            if (startTrainingButton != null)
                startTrainingButton.onClick.AddListener(OnStartTrainingClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
            
            // Training lobby buttons
            if (enterTrainingButton != null)
                enterTrainingButton.onClick.AddListener(OnEnterTrainingClicked);
            if (backToMenuButton != null)
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            
            // Training active buttons
            if (respawnButton != null)
                respawnButton.onClick.AddListener(OnRespawnClicked);
            if (endTrainingButton != null)
                endTrainingButton.onClick.AddListener(OnEndTrainingClicked);
            
            // Settings buttons
            if (saveSettingsButton != null)
                saveSettingsButton.onClick.AddListener(OnSaveSettingsClicked);
            if (closeSettingsButton != null)
                closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
            
            // Training lobby events
            if (trainingLobby != null)
            {
                trainingLobby.OnStateChanged += OnTrainingStateChanged;
                trainingLobby.OnTrainingStarted += OnTrainingStarted;
                trainingLobby.OnTrainingEnded += OnTrainingEnded;
            }
        }
        
        private void Update()
        {
            if (!isUIInitialized) return;
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            // Update status text
            if (statusText != null && trainingLobby != null)
            {
                statusText.text = $"Status: {GetFriendlyStateName()}";
            }
            
            // Update training session info disabled - system simplified
        }
        
        private void UpdateTrainingSessionUI()
        {
            if (sessionTimeText != null)
                sessionTimeText.text = $"Time: {Time.time:F1}s";
            
            if (spawnsText != null)
                spawnsText.text = $"Spawns: 0";
            
            if (deathsText != null)
                deathsText.text = $"Deaths: 0";
        }
        
        private string GetFriendlyStateName()
        {
            if (trainingLobby == null) return "Not Ready";
            
            return trainingLobby.CurrentState switch
            {
                LocalTrainingLobby.LobbyState.Disconnected => "Ready to Start",
                LocalTrainingLobby.LobbyState.Initializing => "Initializing...",
                LocalTrainingLobby.LobbyState.StartingLocalServer => "Starting Server...",
                LocalTrainingLobby.LobbyState.ConnectingAsClient => "Connecting...",
                LocalTrainingLobby.LobbyState.Connected => "Connected",
                LocalTrainingLobby.LobbyState.InTraining => "Training Active",
                LocalTrainingLobby.LobbyState.Error => "Error",
                _ => "Unknown"
            };
        }
        
        // UI State Management
        public void ShowMainMenu()
        {
            SetActivePanel(mainMenuPanel);
        }
        
        public void ShowTrainingLobby()
        {
            SetActivePanel(trainingLobbyPanel);
        }
        
        public void ShowTrainingActive()
        {
            SetActivePanel(trainingActivePanel);
        }
        
        public void ShowSettings()
        {
            SetActivePanel(settingsPanel);
            LoadSettings();
        }
        
        private void SetActivePanel(GameObject activePanel)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (trainingLobbyPanel != null) trainingLobbyPanel.SetActive(false);
            if (trainingActivePanel != null) trainingActivePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            
            if (activePanel != null) activePanel.SetActive(true);
        }
        
        // Event Handlers
        private void OnStartTrainingClicked()
        {
            Debug.Log("[TrainingLobbyUI] Start Training clicked");
            
            if (trainingLobby != null)
            {
                trainingLobby.StartTrainingLobby();
                ShowTrainingLobby();
            }
            else
            {
                Debug.LogError("[TrainingLobbyUI] LocalTrainingLobby component not found!");
            }
        }
        
        private void OnSettingsClicked()
        {
            ShowSettings();
        }
        
        private void OnExitClicked()
        {
            Debug.Log("[TrainingLobbyUI] Exit clicked");
            Application.Quit();
        }
        
        private void OnEnterTrainingClicked()
        {
            Debug.Log("[TrainingLobbyUI] Enter Training clicked");
            ShowTrainingActive();
        }
        
        private void OnBackToMenuClicked()
        {
            Debug.Log("[TrainingLobbyUI] Back to Menu clicked");
            
            if (trainingLobby != null)
            {
                trainingLobby.StopTrainingLobby();
            }
            
            ShowMainMenu();
        }
        
        private void OnRespawnClicked()
        {
            Debug.Log("[TrainingLobbyUI] Respawn clicked - simplified training system");
        }
        
        private void OnEndTrainingClicked()
        {
            Debug.Log("[TrainingLobbyUI] End Training clicked");
            
            if (trainingLobby != null)
            {
                trainingLobby.StopTrainingLobby();
            }
            
            ShowMainMenu();
        }
        
        private void OnSaveSettingsClicked()
        {
            SaveSettings();
            ShowMainMenu();
        }
        
        private void OnCloseSettingsClicked()
        {
            ShowMainMenu();
        }
        
        // Training Event Handlers
        private void OnTrainingStateChanged(LocalTrainingLobby.LobbyState newState)
        {
            Debug.Log($"[TrainingLobbyUI] State changed to: {newState}");
            
            // Update lobby state text
            if (lobbyStateText != null)
            {
                lobbyStateText.text = GetFriendlyStateName();
            }
            
            // Update progress
            UpdateProgress(newState);
            
            // Auto-transition to training active when ready
            if (newState == LocalTrainingLobby.LobbyState.InTraining)
            {
                ShowTrainingActive();
            }
        }
        
        private void UpdateProgress(LocalTrainingLobby.LobbyState state)
        {
            float progress = state switch
            {
                LocalTrainingLobby.LobbyState.Disconnected => 0f,
                LocalTrainingLobby.LobbyState.Initializing => 0.2f,
                LocalTrainingLobby.LobbyState.StartingLocalServer => 0.4f,
                LocalTrainingLobby.LobbyState.ConnectingAsClient => 0.6f,
                LocalTrainingLobby.LobbyState.Connected => 0.8f,
                LocalTrainingLobby.LobbyState.InTraining => 1f,
                LocalTrainingLobby.LobbyState.Error => 0f,
                _ => 0f
            };
            
            if (progressSlider != null)
                progressSlider.value = progress;
            
            if (progressText != null)
                progressText.text = $"{progress * 100:F0}%";
        }
        
        private void OnTrainingStarted()
        {
            Debug.Log("[TrainingLobbyUI] Training started");
        }
        
        private void OnTrainingEnded()
        {
            Debug.Log("[TrainingLobbyUI] Training ended");
            ShowMainMenu();
        }
        
        // Settings Management
        private void LoadSettings()
        {
            // Load settings from PlayerPrefs or configuration
            if (autoStartToggle != null)
                autoStartToggle.isOn = PlayerPrefs.GetInt("Training_AutoStart", 1) == 1;
            
            if (playerCountSlider != null)
                playerCountSlider.value = PlayerPrefs.GetInt("Training_PlayerCount", 1);
            
            if (enableBotsToggle != null)
                enableBotsToggle.isOn = PlayerPrefs.GetInt("Training_EnableBots", 0) == 1;
        }
        
        private void SaveSettings()
        {
            // Save settings to PlayerPrefs
            if (autoStartToggle != null)
                PlayerPrefs.SetInt("Training_AutoStart", autoStartToggle.isOn ? 1 : 0);
            
            if (playerCountSlider != null)
                PlayerPrefs.SetInt("Training_PlayerCount", (int)playerCountSlider.value);
            
            if (enableBotsToggle != null)
                PlayerPrefs.SetInt("Training_EnableBots", enableBotsToggle.isOn ? 1 : 0);
            
            PlayerPrefs.Save();
            Debug.Log("[TrainingLobbyUI] Settings saved");
        }
        
        // UI Creation Methods (simplified versions)
        private void CreateMainMenuPanel()
        {
            // Create a basic main menu panel
            // This would typically be created in the scene editor
            Debug.Log("[TrainingLobbyUI] Main menu panel should be created in the scene");
        }
        
        private void CreateTrainingLobbyPanel()
        {
            Debug.Log("[TrainingLobbyUI] Training lobby panel should be created in the scene");
        }
        
        private void CreateTrainingActivePanel()
        {
            Debug.Log("[TrainingLobbyUI] Training active panel should be created in the scene");
        }
        
        private void CreateSettingsPanel()
        {
            Debug.Log("[TrainingLobbyUI] Settings panel should be created in the scene");
        }
        
        private void OnDestroy()
        {
            // Cleanup event listeners
            if (trainingLobby != null)
            {
                trainingLobby.OnStateChanged -= OnTrainingStateChanged;
                trainingLobby.OnTrainingStarted -= OnTrainingStarted;
                trainingLobby.OnTrainingEnded -= OnTrainingEnded;
            }
        }
    }
}
