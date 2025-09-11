using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections.Generic;

namespace MOBA.UI
{
    /// <summary>
    /// Lobby UI system providing interface for lobby management and development tools
    /// Integrates with LobbySystem for seamless lobby experience
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Lobby UI Panels")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameplayUI;
        [SerializeField] private GameObject developmentPanel;
        
        [Header("Lobby Information")]
        [SerializeField] private TextMeshProUGUI lobbyNameText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI lobbyStatusText;
        [SerializeField] private Slider playerCountSlider;
        
        [Header("Player List")]
        [SerializeField] private Transform playerListParent;
        [SerializeField] private GameObject playerListItemPrefab;
        [SerializeField] private ScrollRect playerListScrollRect;
        
        [Header("Lobby Controls")]
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private TMP_InputField portInput;
        
        [Header("Development Tools")]
        [SerializeField] private Button quickStartButton;
        [SerializeField] private Button addBotsButton;
        [SerializeField] private Toggle autoCreateToggle;
        [SerializeField] private Toggle skipLobbyToggle;
        [SerializeField] private TextMeshProUGUI debugInfoText;
        
        [Header("Visual Feedback")]
        [SerializeField] private Image connectionStatusIndicator;
        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color disconnectedColor = Color.red;
        [SerializeField] private Color connectingColor = Color.yellow;
        [SerializeField] private AnimationCurve pulseAnimation;
        
        private MOBA.Networking.LobbySystem lobbySystem;
        private Dictionary<ulong, GameObject> playerListItems = new Dictionary<ulong, GameObject>();
        private bool isPlayerReady = false;
        private float pulseTimer = 0f;
        
        private void Awake()
        {
            // Find lobby system
            lobbySystem = FindFirstObjectByType<MOBA.Networking.LobbySystem>();
            if (lobbySystem == null)
            {
                Debug.LogWarning("[LobbyUI] LobbySystem not found in scene!");
            }
            
            // Initialize UI elements
            InitializeUI();
        }
        
        private void Start()
        {
            // Subscribe to lobby events
            if (lobbySystem != null)
            {
                lobbySystem.OnLobbyStateChanged += OnLobbyStateChanged;
                lobbySystem.OnPlayerCountChanged += OnPlayerCountChanged;
                lobbySystem.OnLobbyReady += OnLobbyReady;
            }
            
            // Set default values
            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";
            if (portInput != null)
                portInput.text = "7777";
            
            // Show main menu initially
            ShowMainMenu();
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
        
        private void Update()
        {
            UpdateConnectionStatusIndicator();
            UpdateDebugInfo();
        }
        
        private void InitializeUI()
        {
            // Setup button listeners
            if (createLobbyButton != null)
                createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
            if (joinLobbyButton != null)
                joinLobbyButton.onClick.AddListener(OnJoinLobbyClicked);
            if (leaveLobbyButton != null)
                leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
            if (readyButton != null)
                readyButton.onClick.AddListener(OnReadyClicked);
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
            if (quickStartButton != null)
                quickStartButton.onClick.AddListener(OnQuickStartClicked);
            if (addBotsButton != null)
                addBotsButton.onClick.AddListener(OnAddBotsClicked);
            
            // Setup toggles
            if (autoCreateToggle != null)
                autoCreateToggle.onValueChanged.AddListener(OnAutoCreateToggled);
            if (skipLobbyToggle != null)
                skipLobbyToggle.onValueChanged.AddListener(OnSkipLobbyToggled);
            
            // Initialize button states
            UpdateButtonStates();
        }
        
        private void OnCreateLobbyClicked()
        {
            Debug.Log("[LobbyUI] Creating lobby...");
            if (lobbySystem != null)
            {
                lobbySystem.CreateLobby();
            }
        }
        
        private void OnJoinLobbyClicked()
        {
            string ipAddress = ipAddressInput?.text ?? "127.0.0.1";
            ushort port = ushort.TryParse(portInput?.text, out ushort parsedPort) ? parsedPort : (ushort)7777;
            
            Debug.Log($"[LobbyUI] Joining lobby at {ipAddress}:{port}");
            if (lobbySystem != null)
            {
                lobbySystem.JoinLobby(ipAddress, port);
            }
        }
        
        private void OnLeaveLobbyClicked()
        {
            Debug.Log("[LobbyUI] Leaving lobby...");
            if (lobbySystem != null)
            {
                lobbySystem.LeaveLobby();
            }
            isPlayerReady = false;
        }
        
        private void OnReadyClicked()
        {
            isPlayerReady = !isPlayerReady;
            Debug.Log($"[LobbyUI] Setting ready state: {isPlayerReady}");
            
            if (lobbySystem != null)
            {
                lobbySystem.SetPlayerReadyServerRpc(isPlayerReady);
            }
            
            UpdateReadyButton();
        }
        
        private void OnStartGameClicked()
        {
            Debug.Log("[LobbyUI] Starting game...");
            if (lobbySystem != null)
            {
                lobbySystem.StartGameFromLobby();
            }
        }
        
        private void OnQuickStartClicked()
        {
            Debug.Log("[LobbyUI] Quick starting development session...");
            if (lobbySystem != null)
            {
                lobbySystem.QuickStartDevelopment();
            }
        }
        
        private void OnAddBotsClicked()
        {
            Debug.Log("[LobbyUI] Adding bots (feature not implemented yet)");
            // TODO: Implement bot addition
        }
        
        private void OnAutoCreateToggled(bool value)
        {
            Debug.Log($"[LobbyUI] Auto-create lobby: {value}");
            // This would modify the lobby system's auto-create setting
        }
        
        private void OnSkipLobbyToggled(bool value)
        {
            Debug.Log($"[LobbyUI] Skip lobby UI: {value}");
            // This would modify the lobby system's skip UI setting
        }
        
        private void OnLobbyStateChanged(MOBA.Networking.LobbyState newState)
        {
            Debug.Log($"[LobbyUI] Lobby state changed: {newState}");
            
            switch (newState)
            {
                case MOBA.Networking.LobbyState.Inactive:
                    ShowMainMenu();
                    break;
                case MOBA.Networking.LobbyState.Creating:
                case MOBA.Networking.LobbyState.Joining:
                    ShowLobbyLoading();
                    break;
                case MOBA.Networking.LobbyState.WaitingForPlayers:
                case MOBA.Networking.LobbyState.Ready:
                    ShowLobby();
                    break;
                case MOBA.Networking.LobbyState.StartingGame:
                    ShowGameStarting();
                    break;
                case MOBA.Networking.LobbyState.InGame:
                    ShowGameplay();
                    break;
            }
            
            UpdateUI();
        }
        
        private void OnPlayerCountChanged(int newCount)
        {
            Debug.Log($"[LobbyUI] Player count changed: {newCount}");
            UpdatePlayerCount(newCount);
        }
        
        private void OnLobbyReady()
        {
            Debug.Log("[LobbyUI] Lobby is ready to start!");
            UpdateUI();
        }
        
        private void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(lobbyPanel, false);
            SetPanelActive(gameplayUI, false);
            SetPanelActive(developmentPanel, Application.isEditor);
        }
        
        private void ShowLobby()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(lobbyPanel, true);
            SetPanelActive(gameplayUI, false);
            SetPanelActive(developmentPanel, Application.isEditor);
        }
        
        private void ShowLobbyLoading()
        {
            ShowLobby();
            if (lobbyStatusText != null)
            {
                lobbyStatusText.text = "Connecting...";
            }
        }
        
        private void ShowGameStarting()
        {
            if (lobbyStatusText != null)
            {
                lobbyStatusText.text = "Starting Game...";
            }
        }
        
        private void ShowGameplay()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(lobbyPanel, false);
            SetPanelActive(gameplayUI, true);
            SetPanelActive(developmentPanel, false);
        }
        
        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
        
        private void UpdateUI()
        {
            UpdateButtonStates();
            UpdatePlayerCount(lobbySystem?.PlayerCount ?? 0);
            UpdateLobbyInfo();
        }
        
        private void UpdateButtonStates()
        {
            if (lobbySystem == null) return;
            
            bool isInLobby = lobbySystem.IsInLobby;
            bool isReady = lobbySystem.IsReady;
            bool isHost = NetworkManager.Singleton?.IsHost ?? false;
            
            // Update button interactability
            if (createLobbyButton != null)
                createLobbyButton.interactable = !NetworkManager.Singleton?.IsListening ?? true;
            if (joinLobbyButton != null)
                joinLobbyButton.interactable = !NetworkManager.Singleton?.IsListening ?? true;
            if (leaveLobbyButton != null)
                leaveLobbyButton.interactable = isInLobby;
            if (readyButton != null)
                readyButton.interactable = isInLobby && !isHost;
            if (startGameButton != null)
                startGameButton.interactable = isHost && (isReady || lobbySystem.PlayerCount >= 1);
        }
        
        private void UpdateReadyButton()
        {
            if (readyButton != null)
            {
                var buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isPlayerReady ? "Not Ready" : "Ready";
                }
                
                var buttonImage = readyButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = isPlayerReady ? connectedColor : disconnectedColor;
                }
            }
        }
        
        private void UpdatePlayerCount(int count)
        {
            if (playerCountText != null)
            {
                int maxPlayers = 8; // Default, could get from lobby system
                playerCountText.text = $"Players: {count}/{maxPlayers}";
            }
            
            if (playerCountSlider != null)
            {
                playerCountSlider.maxValue = 8;
                playerCountSlider.value = count;
            }
        }
        
        private void UpdateLobbyInfo()
        {
            if (lobbySystem == null) return;
            
            if (lobbyNameText != null)
            {
                lobbyNameText.text = "MOBA Development Lobby";
            }
            
            if (lobbyStatusText != null)
            {
                switch (lobbySystem.CurrentState)
                {
                    case MOBA.Networking.LobbyState.Inactive:
                        lobbyStatusText.text = "Not Connected";
                        break;
                    case MOBA.Networking.LobbyState.Creating:
                        lobbyStatusText.text = "Creating Lobby...";
                        break;
                    case MOBA.Networking.LobbyState.Joining:
                        lobbyStatusText.text = "Joining Lobby...";
                        break;
                    case MOBA.Networking.LobbyState.WaitingForPlayers:
                        lobbyStatusText.text = "Waiting for Players";
                        break;
                    case MOBA.Networking.LobbyState.Ready:
                        lobbyStatusText.text = "Ready to Start!";
                        break;
                    case MOBA.Networking.LobbyState.StartingGame:
                        lobbyStatusText.text = "Starting Game...";
                        break;
                    case MOBA.Networking.LobbyState.InGame:
                        lobbyStatusText.text = "In Game";
                        break;
                }
            }
        }
        
        private void UpdateConnectionStatusIndicator()
        {
            if (connectionStatusIndicator == null) return;
            
            pulseTimer += Time.deltaTime;
            
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.IsHost)
                {
                    connectionStatusIndicator.color = connectedColor;
                }
                else if (NetworkManager.Singleton.IsListening)
                {
                    // Pulsing yellow while connecting
                    float pulse = pulseAnimation.Evaluate(pulseTimer % 1f);
                    connectionStatusIndicator.color = Color.Lerp(connectingColor, Color.white, pulse);
                }
                else
                {
                    connectionStatusIndicator.color = disconnectedColor;
                }
            }
            else
            {
                connectionStatusIndicator.color = disconnectedColor;
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (debugInfoText == null || !Application.isEditor) return;
            
            var info = "";
            if (NetworkManager.Singleton != null)
            {
                info += $"Network Status: {(NetworkManager.Singleton.IsListening ? "Active" : "Inactive")}\n";
                info += $"Is Host: {NetworkManager.Singleton.IsHost}\n";
                info += $"Is Client: {NetworkManager.Singleton.IsClient}\n";
                info += $"Connected Clients: {NetworkManager.Singleton.ConnectedClients.Count}\n";
            }
            
            if (lobbySystem != null)
            {
                info += $"Lobby State: {lobbySystem.CurrentState}\n";
                info += $"Lobby Players: {lobbySystem.PlayerCount}\n";
                info += $"Lobby Ready: {lobbySystem.IsReady}\n";
            }
            
            debugInfoText.text = info;
        }
        
        // Public methods for external control
        public void SetLobbyName(string name)
        {
            if (lobbyNameText != null)
            {
                lobbyNameText.text = name;
            }
        }
        
        public void SetMaxPlayers(int maxPlayers)
        {
            if (playerCountSlider != null)
            {
                playerCountSlider.maxValue = maxPlayers;
            }
        }
        
        public void AddPlayerToList(ulong clientId, string playerName = null)
        {
            if (playerListParent == null || playerListItemPrefab == null) return;
            
            if (!playerListItems.ContainsKey(clientId))
            {
                var playerItem = Instantiate(playerListItemPrefab, playerListParent);
                var nameText = playerItem.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = playerName ?? $"Player {clientId}";
                }
                
                playerListItems[clientId] = playerItem;
            }
        }
        
        public void RemovePlayerFromList(ulong clientId)
        {
            if (playerListItems.TryGetValue(clientId, out var playerItem))
            {
                if (playerItem != null)
                {
                    Destroy(playerItem);
                }
                playerListItems.Remove(clientId);
            }
        }
        
        public void ClearPlayerList()
        {
            foreach (var item in playerListItems.Values)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            playerListItems.Clear();
        }
    }
}
