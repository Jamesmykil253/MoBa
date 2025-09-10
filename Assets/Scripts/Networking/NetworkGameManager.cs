using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace MOBA.Networking
{
    /// <summary>
    /// Network game manager handling connections, spawning, and game state
    /// </summary>
    public class NetworkGameManager : NetworkBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private int maxPlayers = 8;
        [SerializeField] private string gameSceneName = "MOBA_TestScene";

        [Header("Player Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject projectilePrefab;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        // Network state
        private NetworkVariable<int> networkConnectedPlayers = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> networkGameStarted = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Player management
        private Dictionary<ulong, GameObject> connectedPlayers = new Dictionary<ulong, GameObject>();
        private Queue<Transform> availableSpawnPoints = new Queue<Transform>();

        private void Awake()
        {
            // Initialize spawn points queue
            if (spawnPoints.Length > 0)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    availableSpawnPoints.Enqueue(spawnPoint);
                }
            }
            else
            {
                // Fallback spawn points
                for (int i = 0; i < maxPlayers; i++)
                {
                    availableSpawnPoints.Enqueue(transform);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                // Set up connection approval
                NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApprovalCallback;
            }

            // Subscribe to network variables
            networkConnectedPlayers.OnValueChanged += OnConnectedPlayersChanged;
            networkGameStarted.OnValueChanged += OnGameStartedChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            networkConnectedPlayers.OnValueChanged -= OnConnectedPlayersChanged;
            networkGameStarted.OnValueChanged -= OnGameStartedChanged;
        }

        private void ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // Check player limit
            if (networkConnectedPlayers.Value >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Server is full";
                return;
            }

            // Additional validation can be added here
            // - Check player authentication
            // - Validate client version
            // - Check ban list

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = null; // Use default prefab
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            networkConnectedPlayers.Value++;

            Debug.Log($"[NetworkGameManager] Client {clientId} connected. Total players: {networkConnectedPlayers.Value}");

            // Spawn player if not already spawned
            if (!connectedPlayers.ContainsKey(clientId))
            {
                SpawnPlayer(clientId);
            }

            // Start game if enough players
            if (networkConnectedPlayers.Value >= 2 && !networkGameStarted.Value)
            {
                StartGame();
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            networkConnectedPlayers.Value--;

            Debug.Log($"[NetworkGameManager] Client {clientId} disconnected. Total players: {networkConnectedPlayers.Value}");

            // Remove player
            if (connectedPlayers.TryGetValue(clientId, out var playerObject))
            {
                connectedPlayers.Remove(clientId);

                // Return spawn point to queue
                if (playerObject != null)
                {
                    var spawnPoint = GetPlayerSpawnPoint(playerObject.transform.position);
                    if (spawnPoint != null)
                    {
                        availableSpawnPoints.Enqueue(spawnPoint);
                    }
                }
            }

            // End game if not enough players
            if (networkConnectedPlayers.Value < 2 && networkGameStarted.Value)
            {
                EndGame();
            }
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (!IsServer) return;

            // Get spawn position
            Vector3 spawnPosition = GetNextSpawnPosition();

            // Spawn player
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            var networkObject = playerObject.GetComponent<NetworkObject>();

            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId);
                connectedPlayers[clientId] = playerObject;

                Debug.Log($"[NetworkGameManager] Spawned player for client {clientId} at {spawnPosition}");
            }
            else
            {
                Debug.LogError("[NetworkGameManager] Player prefab missing NetworkObject component");
                Object.Destroy(playerObject);
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (availableSpawnPoints.Count > 0)
            {
                var spawnPoint = availableSpawnPoints.Dequeue();
                return spawnPoint.position;
            }

            // Fallback: random position
            return new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
        }

        private Transform GetPlayerSpawnPoint(Vector3 playerPosition)
        {
            // Find the closest spawn point to return to queue
            Transform closestSpawn = null;
            float closestDistance = float.MaxValue;

            foreach (var spawnPoint in spawnPoints)
            {
                float distance = Vector3.Distance(playerPosition, spawnPoint.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestSpawn = spawnPoint;
                }
            }

            return closestSpawn;
        }

        private void StartGame()
        {
            if (!IsServer) return;

            networkGameStarted.Value = true;

            Debug.Log("[NetworkGameManager] Game started");

            // Removed automatic scene loading to prevent automatic loading
            // Use LoadGameScene() method manually instead
            Debug.Log($"[NetworkGameManager] Game started - manual scene loading required: {gameSceneName}");

            // Notify all clients
            GameStartedClientRpc();

            // Additional game start logic
            // - Initialize game timer
            // - Spawn objectives
            // - Start scoring system
        }

        private void EndGame()
        {
            if (!IsServer) return;

            networkGameStarted.Value = false;

            Debug.Log("[NetworkGameManager] Game ended");

            // Notify all clients
            GameEndedClientRpc();

            // Additional game end logic
            // - Calculate final scores
            // - Clean up game objects
            // - Return to lobby
        }

        [ClientRpc]
        private void GameStartedClientRpc()
        {
            Debug.Log("[NetworkGameManager] Game started on client");

            // Client game start logic
            // - Enable player controls
            // - Show game UI
            // - Start game timer
        }

        [ClientRpc]
        private void GameEndedClientRpc()
        {
            Debug.Log("[NetworkGameManager] Game ended on client");

            // Client game end logic
            // - Disable player controls
            // - Show end game screen
            // - Display final scores
        }

        // Network variable change handlers
        private void OnConnectedPlayersChanged(int previousValue, int newValue)
        {
            Debug.Log($"[NetworkGameManager] Connected players changed: {previousValue} -> {newValue}");
        }

        private void OnGameStartedChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"[NetworkGameManager] Game started changed: {previousValue} -> {newValue}");
        }

        // Public API
        public bool IsGameStarted => networkGameStarted.Value;
        public int ConnectedPlayers => networkConnectedPlayers.Value;
        public int MaxPlayers => maxPlayers;

        public GameObject GetPlayerObject(ulong clientId)
        {
            return connectedPlayers.TryGetValue(clientId, out var player) ? player : null;
        }

        public Dictionary<ulong, GameObject> GetAllPlayers()
        {
            return new Dictionary<ulong, GameObject>(connectedPlayers);
        }

        // Server-only methods
        [ServerRpc(RequireOwnership = false)]
        public void RequestRespawnServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (connectedPlayers.TryGetValue(clientId, out var playerObject))
            {
                var playerController = playerObject.GetComponent<NetworkPlayerController>();
                if (playerController != null)
                {
                    // Respawn player
                    Vector3 spawnPosition = GetNextSpawnPosition();
                    playerObject.transform.position = spawnPosition;

                    // Reset player state
                    RespawnPlayerClientRpc(spawnPosition, rpcParams.Receive.SenderClientId);
                }
            }
        }

        [ClientRpc]
        private void RespawnPlayerClientRpc(Vector3 spawnPosition, ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                Debug.Log($"[NetworkGameManager] Player respawned at {spawnPosition}");
            }
        }

        // Static instance for easy access
        private static NetworkGameManager _instance;
        public static NetworkGameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<NetworkGameManager>();
                }
                return _instance;
            }
        }

        public override void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            base.OnDestroy();
        }
        
        private float tickTimer = 0f;
        
        private void FixedUpdate()
        {
            if (!IsServer) return;
            tickTimer += Time.fixedDeltaTime;
            if (tickTimer >= 0.02f) { // 50 Hz
                ProcessInputs();
                SimulateWorld();
                tickTimer = 0f;
            }
        }
        
        private void ProcessInputs()
        {
            // TODO: Dequeue and process buffered inputs
        }
        
        private void SimulateWorld()
        {
            // TODO: Deterministic simulation step
        }
    }
}