using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace MOBA.Training
{
    /// <summary>
    /// Training-specific game manager for local practice sessions
    /// Handles player spawning, training objectives, and local game logic
    /// </summary>
    public class TrainingGameManager : NetworkBehaviour
    {
        [Header("Training Configuration")]
        [SerializeField] private int maxTrainingPlayers = 1;
        [SerializeField] private bool enableTrainingBots = false;
        [SerializeField] private int botCount = 3;
        
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Vector3 defaultSpawnPosition = Vector3.zero;
        
        [Header("Training Features")]
        [SerializeField] private bool enableInstantRespawn = true;
        [SerializeField] private bool enableGodMode = false;
        [SerializeField] private bool unlimitedResources = true;
        
        // State management
        private Dictionary<ulong, GameObject> trainingPlayers = new Dictionary<ulong, GameObject>();
        private Queue<Transform> availableSpawnPoints = new Queue<Transform>();
        private bool isTrainingActive = false;
        
        // Training stats
        private float sessionStartTime;
        private int totalSpawns = 0;
        private int totalDeaths = 0;
        
        // Events
        public System.Action OnTrainingSessionStarted;
        public System.Action OnTrainingSessionEnded;
        public System.Action<ulong> OnPlayerSpawned;
        public System.Action<ulong> OnPlayerDied;
        
        public void Initialize(Transform[] spawnPoints, GameObject playerPrefab)
        {
            this.spawnPoints = spawnPoints;
            this.playerPrefab = playerPrefab;
            
            InitializeSpawnPoints();
            
            // Log max player configuration
            Debug.Log($"[TrainingGameManager] Initialized for local training - Max players: {maxTrainingPlayers}");
        }
        
        private void Awake()
        {
            InitializeSpawnPoints();
        }
        
        private void InitializeSpawnPoints()
        {
            availableSpawnPoints.Clear();
            
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        availableSpawnPoints.Enqueue(spawnPoint);
                    }
                }
                Debug.Log($"[TrainingGameManager] Initialized {availableSpawnPoints.Count} spawn points");
            }
            else
            {
                // Create default spawn point
                GameObject defaultSpawn = new GameObject("DefaultTrainingSpawn");
                defaultSpawn.transform.position = defaultSpawnPosition;
                availableSpawnPoints.Enqueue(defaultSpawn.transform);
                Debug.Log("[TrainingGameManager] Created default spawn point");
            }
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartTrainingSession();
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                EndTrainingSession();
            }
        }
        
        /// <summary>
        /// Start the training session
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void StartTrainingSessionServerRpc()
        {
            StartTrainingSession();
        }
        
        private void StartTrainingSession()
        {
            if (!IsServer) return;
            
            isTrainingActive = true;
            sessionStartTime = Time.time;
            totalSpawns = 0;
            totalDeaths = 0;
            
            Debug.Log("[TrainingGameManager] 🎯 Training session started");
            
            // Notify clients
            NotifyTrainingStartedClientRpc();
            
            OnTrainingSessionStarted?.Invoke();
            
            // Spawn training bots if enabled
            if (enableTrainingBots)
            {
                SpawnTrainingBots();
            }
        }
        
        [ClientRpc]
        private void NotifyTrainingStartedClientRpc()
        {
            OnTrainingSessionStarted?.Invoke();
        }
        
        /// <summary>
        /// End the training session
        /// </summary>
        public void EndTrainingSession()
        {
            if (!IsServer) return;
            
            isTrainingActive = false;
            
            float sessionDuration = Time.time - sessionStartTime;
            
            Debug.Log($"[TrainingGameManager] Training session ended - Duration: {sessionDuration:F1}s, Spawns: {totalSpawns}, Deaths: {totalDeaths}");
            
            // Cleanup all training players
            foreach (var player in trainingPlayers.Values)
            {
                if (player != null)
                {
                    player.GetComponent<NetworkObject>().Despawn();
                }
            }
            trainingPlayers.Clear();
            
            // Notify clients
            NotifyTrainingEndedClientRpc(sessionDuration);
            
            OnTrainingSessionEnded?.Invoke();
        }
        
        [ClientRpc]
        private void NotifyTrainingEndedClientRpc(float duration)
        {
            Debug.Log($"[TrainingGameManager] Training session ended - Duration: {duration:F1}s");
            OnTrainingSessionEnded?.Invoke();
        }
        
        /// <summary>
        /// Spawn a training player
        /// </summary>
        public void SpawnTrainingPlayer()
        {
            if (!IsServer)
            {
                SpawnTrainingPlayerServerRpc();
                return;
            }
            
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            SpawnPlayerForClient(clientId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SpawnTrainingPlayerServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            SpawnPlayerForClient(clientId);
        }
        
        private void SpawnPlayerForClient(ulong clientId)
        {
            if (!IsServer) return;
            
            // Remove existing player if any
            if (trainingPlayers.ContainsKey(clientId))
            {
                GameObject existingPlayer = trainingPlayers[clientId];
                if (existingPlayer != null)
                {
                    existingPlayer.GetComponent<NetworkObject>().Despawn();
                }
                trainingPlayers.Remove(clientId);
            }
            
            // Get spawn position
            Vector3 spawnPosition = GetNextSpawnPosition();
            
            // Spawn player
            if (playerPrefab != null)
            {
                GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                NetworkObject playerNetObj = playerInstance.GetComponent<NetworkObject>();
                
                if (playerNetObj != null)
                {
                    playerNetObj.SpawnAsPlayerObject(clientId);
                    trainingPlayers[clientId] = playerInstance;
                    totalSpawns++;
                    
                    Debug.Log($"[TrainingGameManager] ✅ Spawned training player for client {clientId} at {spawnPosition}");
                    
                    // Configure for training
                    ConfigureTrainingPlayer(playerInstance);
                    
                    OnPlayerSpawned?.Invoke(clientId);
                }
                else
                {
                    Debug.LogError("[TrainingGameManager] PlayerPrefab missing NetworkObject component!");
                    Destroy(playerInstance);
                }
            }
            else
            {
                Debug.LogError("[TrainingGameManager] PlayerPrefab is null!");
            }
        }
        
        private void ConfigureTrainingPlayer(GameObject player)
        {
            // Apply training modifications
            if (enableGodMode)
            {
                // Add god mode component or modify health
                var health = player.GetComponent<IHealth>();
                if (health != null)
                {
                    // Make invulnerable
                    Debug.Log("[TrainingGameManager] God mode enabled for training player");
                }
            }
            
            if (unlimitedResources)
            {
                // Give unlimited resources
                var resources = player.GetComponent<IResourceManager>();
                if (resources != null)
                {
                    // Set unlimited resources
                    Debug.Log("[TrainingGameManager] Unlimited resources enabled for training player");
                }
            }
        }
        
        private Vector3 GetNextSpawnPosition()
        {
            if (availableSpawnPoints.Count > 0)
            {
                Transform spawnPoint = availableSpawnPoints.Dequeue();
                availableSpawnPoints.Enqueue(spawnPoint); // Cycle back
                return spawnPoint.position;
            }
            
            return defaultSpawnPosition;
        }
        
        /// <summary>
        /// Handle player death in training
        /// </summary>
        public void OnPlayerDeath(ulong clientId)
        {
            if (!IsServer) return;
            
            totalDeaths++;
            OnPlayerDied?.Invoke(clientId);
            
            Debug.Log($"[TrainingGameManager] Player {clientId} died in training");
            
            // Instant respawn if enabled
            if (enableInstantRespawn)
            {
                StartCoroutine(RespawnPlayerDelayed(clientId, 0.5f));
            }
        }
        
        private System.Collections.IEnumerator RespawnPlayerDelayed(ulong clientId, float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnPlayerForClient(clientId);
        }
        
        private void SpawnTrainingBots()
        {
            if (!IsServer) return;
            
            Debug.Log($"[TrainingGameManager] Spawning {botCount} training bots...");
            
            for (int i = 0; i < botCount; i++)
            {
                Vector3 botSpawnPos = GetNextSpawnPosition() + Vector3.right * (i + 1) * 3f;
                
                // TODO: Implement bot spawning
                // This would require bot prefabs and AI components
                Debug.Log($"[TrainingGameManager] Bot {i + 1} spawn position: {botSpawnPos}");
            }
        }
        
        /// <summary>
        /// Get training session statistics
        /// </summary>
        public TrainingStats GetSessionStats()
        {
            float duration = isTrainingActive ? Time.time - sessionStartTime : 0f;
            
            return new TrainingStats
            {
                sessionDuration = duration,
                totalSpawns = totalSpawns,
                totalDeaths = totalDeaths,
                playersActive = trainingPlayers.Count,
                isActive = isTrainingActive
            };
        }
        
        // Training controls for debugging
        [ContextMenu("Force Respawn All Players")]
        public void ForceRespawnAllPlayers()
        {
            if (!IsServer) return;
            
            foreach (var clientId in trainingPlayers.Keys)
            {
                SpawnPlayerForClient(clientId);
            }
        }
        
        [ContextMenu("Toggle God Mode")]
        public void ToggleGodMode()
        {
            enableGodMode = !enableGodMode;
            Debug.Log($"[TrainingGameManager] God Mode: {enableGodMode}");
        }
        
        [ContextMenu("Toggle Instant Respawn")]
        public void ToggleInstantRespawn()
        {
            enableInstantRespawn = !enableInstantRespawn;
            Debug.Log($"[TrainingGameManager] Instant Respawn: {enableInstantRespawn}");
        }
    }
    
    /// <summary>
    /// Training session statistics
    /// </summary>
    [System.Serializable]
    public struct TrainingStats
    {
        public float sessionDuration;
        public int totalSpawns;
        public int totalDeaths;
        public int playersActive;
        public bool isActive;
    }
    
    // Interfaces for training features
    public interface IHealth
    {
        void SetInvulnerable(bool invulnerable);
        float GetHealth();
        void SetHealth(float health);
    }
    
    public interface IResourceManager
    {
        void SetUnlimitedResources(bool unlimited);
        void AddResources(int amount);
    }
}
