using UnityEngine;
using Unity.Netcode;
using System.Linq;
using MOBA.Debugging;
using MOBA.Networking;

namespace MOBA.GameManagement
{
    /// <summary>
    /// Manages player and enemy spawning, spawn point validation, and spawn coordination.
    /// Handles both networked and local spawning scenarios with proper validation.
    /// </summary>
    public class GameSpawnManager : GameManagerComponent
    {
        #region Configuration
        
        [Header("Spawn Settings")]
        [SerializeField, Tooltip("Player spawn points in the scene")]
        private Transform[] playerSpawnPoints;
        
        [SerializeField, Tooltip("Enemy spawn points in the scene")]
        private Transform[] enemySpawnPoints;
        
        [SerializeField, Tooltip("Player prefab to spawn")]
        private GameObject playerPrefab;
        
        [SerializeField, Tooltip("Enemy prefab to spawn")]
        private GameObject enemyPrefab;
        
        [Header("Spawn Behavior")]
        [SerializeField, Tooltip("Enable spawning single local player in non-networked mode")]
        private bool spawnSingleLocalPlayer = true;
        
        [SerializeField, Tooltip("Enable detailed spawn logging")]
        private bool logSpawnEvents = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Available player spawn points
        /// </summary>
        public Transform[] PlayerSpawnPoints => playerSpawnPoints;
        
        /// <summary>
        /// Available enemy spawn points
        /// </summary>
        public Transform[] EnemySpawnPoints => enemySpawnPoints;
        
        /// <summary>
        /// Player prefab for spawning
        /// </summary>
        public GameObject PlayerPrefab => playerPrefab;
        
        /// <summary>
        /// Enemy prefab for spawning
        /// </summary>
        public GameObject EnemyPrefab => enemyPrefab;
        
        #endregion
        
        #region Initialization
        
        public override void Initialize(SimpleGameManager gameManager)
        {
            base.Initialize(gameManager);
            
            ValidateSpawnConfiguration();
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "Spawn manager initialized.",
                    ("PlayerSpawnPoints", playerSpawnPoints?.Length ?? 0),
                    ("EnemySpawnPoints", enemySpawnPoints?.Length ?? 0),
                    ("PlayerPrefab", playerPrefab != null),
                    ("EnemyPrefab", enemyPrefab != null));
            }
        }
        
        public override void Shutdown()
        {
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "Spawn manager shutdown.");
            }
            
            base.Shutdown();
        }
        
        #endregion
        
        #region Spawn Validation
        
        /// <summary>
        /// Validate spawn configuration for prefabs and spawn points
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool ValidateSpawnConfiguration()
        {
            bool isValid = true;
            
            // Validate player prefab
            if (playerPrefab == null)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "Player prefab is not assigned.");
                isValid = false;
            }
            
            // Validate enemy prefab
            if (enemyPrefab == null)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "Enemy prefab is not assigned.");
                isValid = false;
            }
            
            // Validate player spawn points
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "No player spawn points assigned.");
                isValid = false;
            }
            else
            {
                for (int i = 0; i < playerSpawnPoints.Length; i++)
                {
                    if (playerSpawnPoints[i] == null)
                    {
                        GameDebug.LogError(
                            BuildContext(GameDebugMechanicTag.Validation),
                            "Player spawn point is null.",
                            ("Index", i));
                        isValid = false;
                    }
                }
            }
            
            // Validate enemy spawn points
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "No enemy spawn points assigned.");
                isValid = false;
            }
            else
            {
                for (int i = 0; i < enemySpawnPoints.Length; i++)
                {
                    if (enemySpawnPoints[i] == null)
                    {
                        GameDebug.LogError(
                            BuildContext(GameDebugMechanicTag.Validation),
                            "Enemy spawn point is null.",
                            ("Index", i));
                        isValid = false;
                    }
                }
            }
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Validation),
                    "Spawn configuration validation completed.",
                    ("IsValid", isValid));
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Validate spawns and prefabs before match start
        /// </summary>
        /// <returns>True if validation passes</returns>
        public bool ValidateSpawnsAndPrefabs()
        {
            return ValidateSpawnConfiguration();
        }
        
        #endregion
        
        #region Player Spawning
        
        /// <summary>
        /// Spawn all players based on network state
        /// </summary>
        public void SpawnPlayers()
        {
            if (!ValidateSpawnConfiguration())
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "Cannot spawn players - configuration validation failed.");
                return;
            }
            
            if (NetworkManager.Singleton != null)
            {
                SpawnNetworkedPlayers();
            }
            else
            {
                SpawnLocalPlayers();
            }
        }
        
        /// <summary>
        /// Spawn networked players using ProductionNetworkManager
        /// </summary>
        private void SpawnNetworkedPlayers()
        {
            var networkManager = ProductionNetworkManager.Instance;
            if (networkManager == null)
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "ProductionNetworkManager instance not found for networked spawning.");
                return;
            }
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "Spawning networked players via ProductionNetworkManager.");
            }
            
            // Let ProductionNetworkManager handle player spawning
            // This integrates with the existing player avatar spawning system
        }
        
        /// <summary>
        /// Spawn local players for non-networked gameplay
        /// </summary>
        private void SpawnLocalPlayers()
        {
            if (!spawnSingleLocalPlayer)
            {
                if (logSpawnEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Spawning),
                        "Local player spawning disabled.");
                }
                return;
            }
            
            if (playerSpawnPoints.Length > 0 && playerPrefab != null)
            {
                var spawnPoint = playerSpawnPoints[0];
                var playerInstance = Object.Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                
                if (logSpawnEvents)
                {
                    GameDebug.Log(
                        BuildContext(GameDebugMechanicTag.Spawning),
                        "Local player spawned.",
                        ("Position", spawnPoint.position),
                        ("Instance", playerInstance.name));
                }
            }
        }
        
        /// <summary>
        /// Get spawn position for a specific player index
        /// </summary>
        /// <param name="playerIndex">Player index</param>
        /// <returns>Spawn position and rotation</returns>
        public (Vector3 position, Quaternion rotation) GetPlayerSpawnLocation(int playerIndex)
        {
            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                return (Vector3.zero, Quaternion.identity);
            }
            
            int spawnIndex = playerIndex % playerSpawnPoints.Length;
            var spawnPoint = playerSpawnPoints[spawnIndex];
            
            if (spawnPoint == null)
            {
                return (Vector3.zero, Quaternion.identity);
            }
            
            return (spawnPoint.position, spawnPoint.rotation);
        }
        
        #endregion
        
        #region Enemy Spawning
        
        /// <summary>
        /// Spawn all enemies at designated spawn points
        /// </summary>
        public void SpawnEnemies()
        {
            if (!ValidateSpawnConfiguration())
            {
                GameDebug.LogError(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "Cannot spawn enemies - configuration validation failed.");
                return;
            }
            
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                if (logSpawnEvents)
                {
                    GameDebug.LogWarning(
                        BuildContext(GameDebugMechanicTag.Spawning),
                        "No enemy spawn points available.");
                }
                return;
            }
            
            int spawnedCount = 0;
            
            for (int i = 0; i < enemySpawnPoints.Length; i++)
            {
                var spawnPoint = enemySpawnPoints[i];
                if (spawnPoint != null && enemyPrefab != null)
                {
                    var enemyInstance = Object.Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                    spawnedCount++;
                    
                    if (logSpawnEvents)
                    {
                        GameDebug.Log(
                            BuildContext(GameDebugMechanicTag.Spawning),
                            "Enemy spawned.",
                            ("Index", i),
                            ("Position", spawnPoint.position),
                            ("Instance", enemyInstance.name));
                    }
                }
            }
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Spawning),
                    "Enemy spawning completed.",
                    ("SpawnedCount", spawnedCount),
                    ("TotalSpawnPoints", enemySpawnPoints.Length));
            }
        }
        
        /// <summary>
        /// Initialize enemies already present in the scene
        /// </summary>
        public void InitializeEnemiesInScene()
        {
            // Find all enemies in the scene (assuming they have an enemy tag or component)
            var enemies = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.CompareTag("Enemy") || mb.GetComponent<IEnemyController>() != null)
                .ToArray();
            
            int initializedCount = 0;
            
            foreach (var enemy in enemies)
            {
                // Initialize enemy if it has an initializable component
                var initializable = enemy.GetComponent<IInitializable>();
                if (initializable != null)
                {
                    initializable.Initialize();
                    initializedCount++;
                }
            }
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Initialization),
                    "Scene enemies initialized.",
                    ("InitializedCount", initializedCount),
                    ("TotalEnemies", enemies.Length));
            }
        }
        
        #endregion
        
        #region Configuration
        
        /// <summary>
        /// Set player spawn points
        /// </summary>
        /// <param name="spawnPoints">New player spawn points</param>
        public void SetPlayerSpawnPoints(Transform[] spawnPoints)
        {
            playerSpawnPoints = spawnPoints;
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Player spawn points updated.",
                    ("Count", spawnPoints?.Length ?? 0));
            }
        }
        
        /// <summary>
        /// Set enemy spawn points
        /// </summary>
        /// <param name="spawnPoints">New enemy spawn points</param>
        public void SetEnemySpawnPoints(Transform[] spawnPoints)
        {
            enemySpawnPoints = spawnPoints;
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Enemy spawn points updated.",
                    ("Count", spawnPoints?.Length ?? 0));
            }
        }
        
        /// <summary>
        /// Set player prefab
        /// </summary>
        /// <param name="prefab">New player prefab</param>
        public void SetPlayerPrefab(GameObject prefab)
        {
            playerPrefab = prefab;
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Player prefab updated.",
                    ("Prefab", prefab?.name ?? "null"));
            }
        }
        
        /// <summary>
        /// Set enemy prefab
        /// </summary>
        /// <param name="prefab">New enemy prefab</param>
        public void SetEnemyPrefab(GameObject prefab)
        {
            enemyPrefab = prefab;
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Enemy prefab updated.",
                    ("Prefab", prefab?.name ?? "null"));
            }
        }
        
        /// <summary>
        /// Enable or disable single local player spawning
        /// </summary>
        /// <param name="enabled">Whether to spawn single local player</param>
        public void SetSpawnSingleLocalPlayer(bool enabled)
        {
            spawnSingleLocalPlayer = enabled;
            
            if (logSpawnEvents)
            {
                GameDebug.Log(
                    BuildContext(GameDebugMechanicTag.Configuration),
                    "Single local player spawning setting updated.",
                    ("Enabled", enabled));
            }
        }
        
        #endregion
    }
    
    #region Supporting Interfaces
    
    /// <summary>
    /// Interface for enemy controllers
    /// </summary>
    public interface IEnemyController
    {
        void Initialize();
        void SetTarget(Transform target);
    }
    
    /// <summary>
    /// Interface for initializable components
    /// </summary>
    public interface IInitializable
    {
        void Initialize();
    }
    
    #endregion
}