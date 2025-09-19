using UnityEngine;
using Unity.Netcode;
using MOBA.Configuration;
using MOBA.Debugging;
using MOBA.Networking;
using MOBA.Services;

namespace MOBA
{
    /// <summary>
    /// Simple game manager - coordinates basic MOBA gameplay
    /// </summary>
    public class SimpleGameManager : NetworkBehaviour
    {
        public static SimpleGameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig defaultGameConfig;
        [SerializeField] private bool lockGameSettingsToConfig = true;

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
            ApplyConfiguredDefaultsIfNeeded();

            if (Instance != null && Instance != this)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Configuration),
                    "Duplicate SimpleGameManager detected. Destroying extra instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeServices();
        }

        private void OnValidate()
        {
            ApplyConfiguredDefaultsIfNeeded();
        }

        private void ApplyConfiguredDefaultsIfNeeded()
        {
            if (!lockGameSettingsToConfig || defaultGameConfig == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                defaultGameConfig.Validate();
            }
#endif

            maxPlayers = Mathf.Clamp(defaultGameConfig.maxPlayers, 1, 20);
            gameTime = Mathf.Max(1f, defaultGameConfig.gameTimeInSeconds);
            scoreToWin = Mathf.Max(1, defaultGameConfig.scoreToWin);
            matchLifecycleService?.Configure(gameTime, scoreToWin);
        }

        private void InitializeServices()
        {
            if (scoringService != null)
            {
                scoringService.ScoreChanged -= HandleScoreChanged;
            }

            if (!ServiceRegistry.TryResolve<IScoringService>(out scoringService))
            {
                scoringService = new ScoringService(DefaultTeamCount);
                ServiceRegistry.Register<IScoringService>(scoringService, overwrite: false);
            }

            scoringService.ScoreChanged += HandleScoreChanged;

            if (matchLifecycleService != null)
            {
                matchLifecycleService.MatchEnded -= HandleMatchEnded;
            }

            if (!ServiceRegistry.TryResolve<IMatchLifecycleService>(out matchLifecycleService))
            {
                matchLifecycleService = new MatchLifecycleService(scoringService);
                matchLifecycleService.Configure(gameTime, scoreToWin);
                ServiceRegistry.Register<IMatchLifecycleService>(matchLifecycleService, overwrite: false);
            }

            matchLifecycleService.Configure(gameTime, scoreToWin);
            matchLifecycleService.MatchEnded += HandleMatchEnded;
            clientTimeRemaining = matchLifecycleService.TimeRemaining;
            gameActive = matchLifecycleService.IsActive;
            UpdateUI();
        }
        [Header("Game Settings")]
        public int maxPlayers = 10;
        public float gameTime = 1800f; // 30 minutes
        public int scoreToWin = 100;
        
        [Header("Spawn Settings")]
        public Transform[] playerSpawnPoints;
        public Transform[] enemySpawnPoints;
        public GameObject playerPrefab;
        public GameObject enemyPrefab;

        [Header("UI")]
        public UnityEngine.UI.Text timeText;
        public UnityEngine.UI.Text scoreText;

        // Game state
        private bool gameActive = false;
        private IScoringService scoringService;
        private IMatchLifecycleService matchLifecycleService;
        private float clientTimeRemaining;
        private const int DefaultTeamCount = 2;

        private readonly NetworkVariable<float> networkTimeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkTeamScoreA = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkTeamScoreB = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> networkGameActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkWinningTeam = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [Header("Runtime")]
        [SerializeField] private bool autoStartOnEnable = true;
        [SerializeField] private bool spawnSingleLocalPlayer = true;
        
    // Events
    /// <summary>
    /// Raised when the game ends. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<int> OnGameEnd;
    /// <summary>
    /// Raised when the score updates. Listeners MUST unsubscribe to prevent memory leaks.
    /// </summary>
    public System.Action<int, int> OnScoreUpdate;
        
        void Start()
        {
            if (IsServer && autoStartOnEnable && !gameActive)
            {
                StartMatch();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            InitializeServices();

            networkTimeRemaining.OnValueChanged += OnNetworkTimeChanged;
            networkTeamScoreA.OnValueChanged += OnNetworkScoreAChanged;
            networkTeamScoreB.OnValueChanged += OnNetworkScoreBChanged;
            networkGameActive.OnValueChanged += OnNetworkGameActiveChanged;
            networkWinningTeam.OnValueChanged += OnNetworkWinningTeamChanged;

            if (!IsServer)
            {
                clientTimeRemaining = networkTimeRemaining.Value;
                scoringService?.SetScore(0, networkTeamScoreA.Value, notify: false);
                scoringService?.SetScore(1, networkTeamScoreB.Value, notify: false);
                gameActive = networkGameActive.Value;
                UpdateUI();
            }

            if (IsServer && autoStartOnEnable && !gameActive)
            {
                StartMatch();
            }

            if (IsServer)
            {
                var networkManager = ProductionNetworkManager.Instance;
                if (networkManager != null)
                {
                    networkManager.OnPlayerConnected += HandleServerPlayerJoined;
                    networkManager.OnPlayerDisconnected += HandleServerPlayerDisconnected;
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            networkTimeRemaining.OnValueChanged -= OnNetworkTimeChanged;
            networkTeamScoreA.OnValueChanged -= OnNetworkScoreAChanged;
            networkTeamScoreB.OnValueChanged -= OnNetworkScoreBChanged;
            networkGameActive.OnValueChanged -= OnNetworkGameActiveChanged;
            networkWinningTeam.OnValueChanged -= OnNetworkWinningTeamChanged;

            if (IsServer)
            {
                var networkManager = ProductionNetworkManager.Instance;
                if (networkManager != null)
                {
                    networkManager.OnPlayerConnected -= HandleServerPlayerJoined;
                    networkManager.OnPlayerDisconnected -= HandleServerPlayerDisconnected;
                }
            }
        }

        void Update()
        {
            if (!IsServer || matchLifecycleService == null || !matchLifecycleService.IsActive)
            {
                return;
            }

            matchLifecycleService.Tick(Time.deltaTime);
            clientTimeRemaining = matchLifecycleService.TimeRemaining;
            networkTimeRemaining.Value = clientTimeRemaining;
            UpdateUI();
        }
        
        public bool StartMatch()
        {
            if (NetworkManager.Singleton != null && !IsServer)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Validation),
                    "StartMatch called on client; ignoring.");
                return false;
            }

            if (matchLifecycleService != null && matchLifecycleService.IsActive)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "StartMatch called while game is already active.");
                return false;
            }

            matchLifecycleService?.Configure(gameTime, scoreToWin);
            if (matchLifecycleService == null || !matchLifecycleService.StartMatch())
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "Match lifecycle service rejected StartMatch call.");
                return false;
            }

            gameActive = true;
            clientTimeRemaining = matchLifecycleService.TimeRemaining;
            networkTimeRemaining.Value = clientTimeRemaining;
            networkGameActive.Value = true;
            networkWinningTeam.Value = -1;

            if (!ValidateSpawnsAndPrefabs())
            {
                matchLifecycleService.StopMatch(-1);
                return false;
            }

            if (IsServer && NetworkManager.Singleton != null)
            {
                ProductionNetworkManager.Instance?.DespawnAllPlayerAvatars();
            }

            SpawnPlayers();
            SpawnEnemies();

            InitializeEnemiesInScene();

            UpdateUI();

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Lifecycle), "Game match started.",
                ("Duration", gameTime), ("ScoreToWin", scoreToWin));
            return true;
        }
        // --- Validation for spawns and prefabs ---
        private bool ValidateSpawnsAndPrefabs()
        {
            bool isValid = true;
            // Player prefab
            if (playerPrefab == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Player prefab is not assigned.");
                isValid = false;
            }
            // Enemy prefab
            if (enemyPrefab == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Enemy prefab is not assigned.");
                isValid = false;
            }

            // Player spawn points
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "No player spawn points assigned.");
                isValid = false;
            }
            else
            {
                for (int i = 0; i < playerSpawnPoints.Length; i++)
                {
                    if (playerSpawnPoints[i] == null)
                    {
                        GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                            $"Player spawn point {i} is null.");
                        isValid = false;
                    }
                }
                // Check for duplicate/overlapping positions
                for (int i = 0; i < playerSpawnPoints.Length; i++)
                {
                    for (int j = i + 1; j < playerSpawnPoints.Length; j++)
                    {
                        if (playerSpawnPoints[i] != null && playerSpawnPoints[j] != null)
                        {
                            float dist = Vector3.Distance(playerSpawnPoints[i].position, playerSpawnPoints[j].position);
                            if (dist < 0.1f)
                                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Configuration),
                                    "Player spawn points overlapping.",
                                    ("FirstIndex", i),
                                    ("SecondIndex", j),
                                    ("Distance", dist));
                        }
                    }
                }
            }

            // Enemy spawn points
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "No enemy spawn points assigned.");
                isValid = false;
            }
            else
            {
                for (int i = 0; i < enemySpawnPoints.Length; i++)
                {
                    if (enemySpawnPoints[i] == null)
                    {
                        GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                            $"Enemy spawn point {i} is null.");
                        isValid = false;
                    }
                }
                // Check for duplicate/overlapping positions
                for (int i = 0; i < enemySpawnPoints.Length; i++)
                {
                    for (int j = i + 1; j < enemySpawnPoints.Length; j++)
                    {
                        if (enemySpawnPoints[i] != null && enemySpawnPoints[j] != null)
                        {
                            float dist = Vector3.Distance(enemySpawnPoints[i].position, enemySpawnPoints[j].position);
                            if (dist < 0.1f)
                                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Configuration),
                                    "Enemy spawn points overlapping.",
                                    ("FirstIndex", i),
                                    ("SecondIndex", j),
                                    ("Distance", dist));
                        }
                    }
                }
            }
            return isValid;
        }
        
        void SpawnPlayers()
        {
            if (NetworkManager.Singleton != null)
            {
                if (!IsServer)
                {
                    return;
                }

                if (playerPrefab == null)
                {
                    GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                        "Player prefab is not assigned; cannot spawn network avatars.");
                    return;
                }

                var networkManager = ProductionNetworkManager.Instance;
                if (networkManager == null)
                {
                    GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Spawning),
                        "ProductionNetworkManager instance not found; cannot spawn network players.");
                    return;
                }

                networkManager.SpawnPlayerAvatars(playerPrefab, playerSpawnPoints);
                return;
            }

            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0 || playerPrefab == null)
            {
                return;
            }

            int spawnLimit = spawnSingleLocalPlayer ? 1 : Mathf.Min(playerSpawnPoints.Length, maxPlayers);
            int spawned = 0;

            for (int i = 0; i < playerSpawnPoints.Length && spawned < spawnLimit; i++)
            {
                if (playerSpawnPoints[i] != null)
                {
                    Instantiate(playerPrefab, playerSpawnPoints[i].position, playerSpawnPoints[i].rotation);
                    spawned++;
                }
            }
        }
        
        void SpawnEnemies()
        {
            if (NetworkManager.Singleton != null && !IsServer)
            {
                return;
            }
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0 || enemyPrefab == null)
            {
                return;
            }

            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                if (enemyPrefab != null && enemySpawnPoints[i] != null)
                {
                    var enemyObj = Instantiate(enemyPrefab, enemySpawnPoints[i].position, enemySpawnPoints[i].rotation);
                    var netObj = enemyObj.GetComponent<NetworkObject>();
                    if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        netObj.Spawn();
                    }
                    var enemyController = enemyObj.GetComponent<EnemyController>();
                    if (enemyController != null)
                    {
                        enemyController.ManualInitialize();
                    }
                    else
                    {
                        GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Initialization),
                            "Spawned enemy prefab missing EnemyController.",
                            ("Prefab", enemyPrefab.name));
                    }
                }
            }
        }

        private void InitializeEnemiesInScene()
        {
            if (NetworkManager.Singleton != null && !IsServer)
            {
                return;
            }
            var enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.ManualInitialize();
            }
        }
        
        void UpdateUI()
        {
            if (timeText != null)
            {
                float time = IsServer ? (matchLifecycleService?.TimeRemaining ?? clientTimeRemaining) : clientTimeRemaining;
                int minutes = Mathf.FloorToInt(time / 60f);
                int seconds = Mathf.FloorToInt(time % 60f);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (scoreText != null)
            {
                int scoreA = scoringService?.GetScore(0) ?? networkTeamScoreA.Value;
                int scoreB = scoringService?.GetScore(1) ?? networkTeamScoreB.Value;
                scoreText.text = $"Team 1: {scoreA} | Team 2: {scoreB}";
            }
        }
        
        public bool AddScore(int team, int points = 1)
        {
            if (NetworkManager.Singleton != null && !IsServer)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Score),
                    "AddScore called on client; ignoring.");
                return false;
            }
            if (!(matchLifecycleService?.IsActive ?? gameActive))
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Score),
                    "Attempted to add score after match ended.");
                return false;
            }
            if (scoringService != null && scoringService.AddScore(team, points))
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Score),
                    "Score updated.",
                    ("Team", team),
                    ("NewScore", scoringService.GetScore(team)));
                return true;
            }

            GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Score),
                "AddScore called with invalid team index.",
                ("Team", team));
            return false;
        }
        
        void EndGame(int winningTeam = -1)
        {
            if (NetworkManager.Singleton != null && !IsServer)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "EndGame called on client; ignoring.");
                return;
            }
            if (matchLifecycleService == null || !matchLifecycleService.IsActive)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "EndGame called but game already inactive.");
                return;
            }

            matchLifecycleService.StopMatch(winningTeam);
        }
        
        public void RestartGame()
        {
            if (NetworkManager.Singleton != null && !IsServer)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "RestartGame called on client; ignoring.");
                return;
            }
            // Simple restart - reload scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        
        public bool IsGameActive()
        {
            return matchLifecycleService?.IsActive ?? gameActive;
        }
        
        public float GetTimeRemaining()
        {
            return IsServer ? (matchLifecycleService?.TimeRemaining ?? 0f) : clientTimeRemaining;
        }

        public int GetScore(int team)
        {
            return scoringService?.GetScore(team) ?? 0;
        }

        internal bool IsGameActiveServer => matchLifecycleService?.IsActive ?? false;

        internal void HandleServerPlayerJoined(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            SpawnPlayers();
        }

        internal void HandleServerPlayerDisconnected(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            // Currently no additional behavior beyond avatar despawn.
        }

        private void OnNetworkTimeChanged(float previous, float current)
        {
            clientTimeRemaining = current;
            UpdateUI();
        }

        private void OnNetworkScoreAChanged(int previous, int current)
        {
            scoringService?.SetScore(0, current, notify: false);
            if (!IsServer)
            {
                OnScoreUpdate?.Invoke(0, current);
                UpdateUI();
            }
        }

        private void OnNetworkScoreBChanged(int previous, int current)
        {
            scoringService?.SetScore(1, current, notify: false);
            if (!IsServer)
            {
                OnScoreUpdate?.Invoke(1, current);
                UpdateUI();
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
            UpdateUI();
        }

        private void OnNetworkWinningTeamChanged(int previous, int current)
        {
            // Clients rely on OnNetworkGameActiveChanged to raise game end events.
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
            {
                Instance = null;
            }
            if (scoringService != null)
            {
                scoringService.ScoreChanged -= HandleScoreChanged;
            }
            if (matchLifecycleService != null)
            {
                matchLifecycleService.MatchEnded -= HandleMatchEnded;
            }
        }

        private void HandleScoreChanged(int team, int score)
        {
            if (IsServer)
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
            UpdateUI();
        }

        private void HandleMatchEnded(int winningTeam)
        {
            gameActive = false;
            clientTimeRemaining = matchLifecycleService?.TimeRemaining ?? clientTimeRemaining;
            networkWinningTeam.Value = winningTeam;
            networkTimeRemaining.Value = clientTimeRemaining;
            networkGameActive.Value = false;

            if (winningTeam >= 0)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Score),
                    "Team won the match.",
                    ("TeamIndex", winningTeam),
                    ("Score", scoringService?.GetScore(winningTeam) ?? 0));
            }
            else
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Lifecycle), "Match ended due to time limit.");
            }

            OnGameEnd?.Invoke(winningTeam);
            UpdateUI();
        }
    }
}
