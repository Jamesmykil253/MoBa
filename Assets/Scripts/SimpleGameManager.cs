using UnityEngine;
using MOBA.Debugging;

namespace MOBA
{
    /// <summary>
    /// Simple game manager - coordinates basic MOBA gameplay
    /// </summary>
    public class SimpleGameManager : MonoBehaviour
    {
        public static SimpleGameManager Instance { get; private set; }

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
        private float currentTime;
        private int[] teamScores = new int[2];
        private bool gameActive = false;

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
            if (autoStartOnEnable && !gameActive)
            {
                StartMatch();
            }
        }

        void Update()
        {
            if (!gameActive) return;
            
            UpdateGameTime();
            UpdateUI();
            CheckWinConditions();
        }
        
        public bool StartMatch()
        {
            if (gameActive)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "StartMatch called while game is already active.");
                return false;
            }

            currentTime = gameTime;
            teamScores[0] = 0;
            teamScores[1] = 0;
            gameActive = true;

            if (!ValidateSpawnsAndPrefabs())
            {
                gameActive = false;
                return false;
            }

            SpawnPlayers();
            SpawnEnemies();

            InitializeEnemiesInScene();

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
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0 || playerPrefab == null)
            {
                return;
            }

            int spawnLimit = spawnSingleLocalPlayer ? 1 : Mathf.Min(playerSpawnPoints.Length, maxPlayers);
            int spawned = 0;

            for (int i = 0; i < playerSpawnPoints.Length && spawned < spawnLimit; i++)
            {
                if (playerPrefab != null && playerSpawnPoints[i] != null)
                {
                    Instantiate(playerPrefab, playerSpawnPoints[i].position, playerSpawnPoints[i].rotation);
                    spawned++;
                }
            }
        }
        
        void SpawnEnemies()
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0 || enemyPrefab == null)
            {
                return;
            }

            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                if (enemyPrefab != null && enemySpawnPoints[i] != null)
                {
                    var enemyObj = Instantiate(enemyPrefab, enemySpawnPoints[i].position, enemySpawnPoints[i].rotation);
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
            var enemies = FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                enemy.ManualInitialize();
            }
        }
        
        void UpdateGameTime()
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }
        }
        
        void UpdateUI()
        {
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(currentTime / 60);
                int seconds = Mathf.FloorToInt(currentTime % 60);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }
            
            if (scoreText != null)
            {
                scoreText.text = $"Team 1: {teamScores[0]} | Team 2: {teamScores[1]}";
            }
        }
        
        void CheckWinConditions()
        {
            for (int i = 0; i < teamScores.Length; i++)
            {
                if (teamScores[i] >= scoreToWin)
                {
                    EndGame(i);
                    return;
                }
            }
        }
        
        public void AddScore(int team, int points = 1)
        {
            if (!gameActive)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Score),
                    "Attempted to add score after match ended.");
                return;
            }
            if (team >= 0 && team < teamScores.Length)
            {
                teamScores[team] += points;
                OnScoreUpdate?.Invoke(team, teamScores[team]);
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Score),
                    "Score updated.",
                    ("Team", team),
                    ("NewScore", teamScores[team]));
            }
        }
        
        void EndGame(int winningTeam = -1)
        {
            if (!gameActive)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Lifecycle),
                    "EndGame called but game already inactive.");
                return;
            }
            gameActive = false;

            if (winningTeam >= 0)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Score),
                    "Team won the match.",
                    ("TeamIndex", winningTeam),
                    ("Score", teamScores[winningTeam]));
            }
            else
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Lifecycle), "Match ended due to time limit.");
            }

            OnGameEnd?.Invoke(winningTeam);
        }
        
        public void RestartGame()
        {
            // Simple restart - reload scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        
        public bool IsGameActive()
        {
            return gameActive;
        }
        
        public float GetTimeRemaining()
        {
            return currentTime;
        }
        
        public int GetScore(int team)
        {
            if (team >= 0 && team < teamScores.Length)
                return teamScores[team];
            return 0;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
