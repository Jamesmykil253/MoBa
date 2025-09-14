using UnityEngine;

namespace MOBA
{
    /// <summary>
    /// Simple game manager - coordinates basic MOBA gameplay
    /// </summary>
    public class SimpleGameManager : MonoBehaviour
    {
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
        private bool gameActive = true;
        
        // Events
        public System.Action<int> OnGameEnd;
        public System.Action<int, int> OnScoreUpdate;
        
        void Start()
        {
            StartGame();
        }
        
        void Update()
        {
            if (!gameActive) return;
            
            UpdateGameTime();
            UpdateUI();
            CheckWinConditions();
        }
        
        void StartGame()
        {
            currentTime = gameTime;
            teamScores[0] = 0;
            teamScores[1] = 0;
            gameActive = true;

            ValidateSpawnsAndPrefabs();

            SpawnPlayers();
            SpawnEnemies();

            Debug.Log("Game Started!");
        }
        // --- Validation for spawns and prefabs ---
        private void ValidateSpawnsAndPrefabs()
        {
            // Player prefab
            if (playerPrefab == null)
                Debug.LogError("[SimpleGameManager] Player prefab is not assigned!");
            // Enemy prefab
            if (enemyPrefab == null)
                Debug.LogError("[SimpleGameManager] Enemy prefab is not assigned!");

            // Player spawn points
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
                Debug.LogError("[SimpleGameManager] No player spawn points assigned!");
            else
            {
                for (int i = 0; i < playerSpawnPoints.Length; i++)
                {
                    if (playerSpawnPoints[i] == null)
                        Debug.LogError($"[SimpleGameManager] Player spawn point {i} is null!");
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
                                Debug.LogWarning($"[SimpleGameManager] Player spawn points {i} and {j} are overlapping (distance: {dist})");
                        }
                    }
                }
            }

            // Enemy spawn points
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
                Debug.LogError("[SimpleGameManager] No enemy spawn points assigned!");
            else
            {
                for (int i = 0; i < enemySpawnPoints.Length; i++)
                {
                    if (enemySpawnPoints[i] == null)
                        Debug.LogError($"[SimpleGameManager] Enemy spawn point {i} is null!");
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
                                Debug.LogWarning($"[SimpleGameManager] Enemy spawn points {i} and {j} are overlapping (distance: {dist})");
                        }
                    }
                }
            }
    }
        
        void SpawnPlayers()
        {
            for (int i = 0; i < playerSpawnPoints.Length && i < maxPlayers; i++)
            {
                if (playerPrefab != null && playerSpawnPoints[i] != null)
                {
                    Instantiate(playerPrefab, playerSpawnPoints[i].position, playerSpawnPoints[i].rotation);
                }
            }
        }
        
        void SpawnEnemies()
        {
            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                if (enemyPrefab != null && enemySpawnPoints[i] != null)
                {
                    Instantiate(enemyPrefab, enemySpawnPoints[i].position, enemySpawnPoints[i].rotation);
                }
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
            if (!gameActive) {
                Debug.LogWarning("[SimpleGameManager] Attempted to add score after game ended.");
                return;
            }
            if (team >= 0 && team < teamScores.Length)
            {
                teamScores[team] += points;
                OnScoreUpdate?.Invoke(team, teamScores[team]);
            }
        }
        
        void EndGame(int winningTeam = -1)
        {
            if (!gameActive) {
                Debug.LogWarning("[SimpleGameManager] EndGame called but game is already ended.");
                return;
            }
            gameActive = false;

            if (winningTeam >= 0)
            {
                Debug.Log($"Team {winningTeam + 1} Wins!");
            }
            else
            {
                Debug.Log("Time's Up! Game Over!");
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
    }
}
