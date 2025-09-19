using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using MOBA.Debugging;

namespace MOBA.Networking
{
    /// <summary>
    /// Manages player lifecycle, spawning, and network data tracking.
    /// Handles player connections, disconnections, avatar spawning/despawning,
    /// and maintains player state data for the network session.
    /// </summary>
    public class NetworkPlayerManager : NetworkManagerComponent
    {
        #region Events
        
        /// <summary>
        /// Raised when a player connects. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<ulong> OnPlayerConnected;
        
        /// <summary>
        /// Raised when a player disconnects. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<ulong> OnPlayerDisconnected;
        
        /// <summary>
        /// Raised when player count changes. Listeners MUST unsubscribe to prevent memory leaks.
        /// </summary>
        public System.Action<int> OnPlayerCountChanged;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Dictionary tracking all connected players and their network data
        /// </summary>
        private Dictionary<ulong, PlayerNetworkData> connectedPlayers = new Dictionary<ulong, PlayerNetworkData>();
        
        /// <summary>
        /// Dictionary tracking spawned player avatars
        /// </summary>
        private readonly Dictionary<ulong, NetworkObject> playerAvatars = new Dictionary<ulong, NetworkObject>();
        
        /// <summary>
        /// Current number of connected players
        /// </summary>
        private int currentPlayerCount = 0;
        
        /// <summary>
        /// Reference to the main production network manager
        /// </summary>
        private ProductionNetworkManager productionNetworkManager;
        
        /// <summary>
        /// Enable detailed player management logging
        /// </summary>
        [SerializeField] private bool logPlayerEvents = true;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Current number of connected players
        /// </summary>
        public int CurrentPlayerCount => currentPlayerCount;
        
        /// <summary>
        /// Read-only access to connected players dictionary
        /// </summary>
        public IReadOnlyDictionary<ulong, PlayerNetworkData> ConnectedPlayers => connectedPlayers;
        
        /// <summary>
        /// Read-only access to player avatars dictionary
        /// </summary>
        public IReadOnlyDictionary<ulong, NetworkObject> PlayerAvatars => playerAvatars;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the player manager with reference to the main network manager
        /// </summary>
        /// <param name="networkManager">Reference to ProductionNetworkManager</param>
        public new void Initialize(ProductionNetworkManager networkManager)
        {
            productionNetworkManager = networkManager;
            
            if (logPlayerEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Initialization),
                    "NetworkPlayerManager initialized successfully.");
            }
        }
        
        protected override void OnInitialized()
        {
            // Subscribe to network events if needed
            if (netcode != null)
            {
                // Note: Client connection/disconnection callbacks are handled by ProductionNetworkManager
                // This component focuses on player data management and spawning
            }
        }
        
        public override void Shutdown()
        {
            // Clean up all player data and avatars
            foreach (var kvp in playerAvatars)
            {
                if (kvp.Value != null && kvp.Value.IsSpawned)
                {
                    kvp.Value.Despawn();
                }
            }
            
            playerAvatars.Clear();
            connectedPlayers.Clear();
            currentPlayerCount = 0;
            
            // Clean up events
            OnPlayerConnected = null;
            OnPlayerDisconnected = null;
            OnPlayerCountChanged = null;
            
            base.Shutdown();
            
            if (logPlayerEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, subsystem: "Shutdown"),
                    "NetworkPlayerManager shutdown complete.");
            }
        }
        
        #endregion
        
        #region Player Lifecycle Management
        
        /// <summary>
        /// Handle a new player connecting to the session
        /// </summary>
        /// <param name="clientId">Client ID of the connecting player</param>
        public void HandlePlayerConnected(ulong clientId)
        {
            // Edge case: duplicate connection
            if (connectedPlayers.ContainsKey(clientId))
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                    "Duplicate connection detected. Ignoring duplicate connection attempt.");
                return;
            }

            var playerData = new PlayerNetworkData
            {
                ClientId = clientId,
                ConnectedTime = Time.time,
                IsActive = true
            };

            connectedPlayers[clientId] = playerData;
            UpdatePlayerCount();

            OnPlayerConnected?.Invoke(clientId);

            if (logPlayerEvents)
            {
                GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                    "Player connected to session.",
                    ("ConnectedPlayers", currentPlayerCount));
            }

            // Publish network event
            if (NetworkManager.Singleton.IsServer)
            {
                UnifiedEventSystem.PublishNetwork<PlayerConnectedEvent>(new PlayerConnectedEvent(clientId, Time.time));
            }
        }
        
        /// <summary>
        /// Handle a player disconnecting from the session
        /// </summary>
        /// <param name="clientId">Client ID of the disconnecting player</param>
        public void HandlePlayerDisconnected(ulong clientId)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
                UpdatePlayerCount();
                
                // Clean up player avatar
                DespawnPlayerAvatar(clientId);
                
                OnPlayerDisconnected?.Invoke(clientId);
                
                if (logPlayerEvents)
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                        "Player disconnected from session.",
                        ("ConnectedPlayers", currentPlayerCount));
                }
                        
                // Publish network event
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    UnifiedEventSystem.PublishNetwork<PlayerDisconnectedEvent>(new PlayerDisconnectedEvent(clientId, Time.time));
                }
            }
        }
        
        /// <summary>
        /// Update the current player count and notify listeners
        /// </summary>
        private void UpdatePlayerCount()
        {
            int newCount = connectedPlayers.Count;
            if (newCount != currentPlayerCount)
            {
                currentPlayerCount = newCount;
                OnPlayerCountChanged?.Invoke(currentPlayerCount);
            }
        }
        
        #endregion
        
        #region Player Avatar Management
        
        /// <summary>
        /// Spawn player avatars for all connected players using provided prefab and spawn points
        /// </summary>
        /// <param name="playerPrefab">The prefab to spawn for each player</param>
        /// <param name="spawnPoints">Array of spawn points to use</param>
        public void SpawnPlayerAvatars(GameObject playerPrefab, Transform[] spawnPoints)
        {
            if (playerPrefab == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "Player prefab is null; cannot spawn player avatars.");
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "No spawn points available; cannot spawn player avatars.");
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Networking),
                    "Only server can spawn player avatars.");
                return;
            }

            int spawnCount = 0;
            foreach (var kvp in connectedPlayers)
            {
                ulong clientId = kvp.Key;
                if (!connectedPlayers.TryGetValue(clientId, out var playerData))
                {
                    continue;
                }

                // Skip if player already has an avatar
                if (playerAvatars.ContainsKey(clientId) && playerAvatars[clientId] != null)
                {
                    continue;
                }

                Transform spawnPoint = GetSpawnPoint(spawnPoints, spawnCount);
                GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                
                var networkObject = playerInstance.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.SpawnAsPlayerObject(clientId);
                    playerAvatars[clientId] = networkObject;
                    
                    // Update player data with avatar reference
                    playerData.Avatar = networkObject;
                    playerData.LastPosition = spawnPoint.position;
                    playerData.LastMovementTime = Time.time;

                    if (logPlayerEvents)
                    {
                        GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                            "Player avatar spawned successfully.",
                            ("SpawnPosition", spawnPoint.position.ToString()));
                    }
                }
                else
                {
                    GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration, actorClientId: clientId),
                        "Player prefab does not have NetworkObject component.");
                    Destroy(playerInstance);
                }

                spawnCount++;
            }
        }
        
        /// <summary>
        /// Despawn all player avatars
        /// </summary>
        public void DespawnAllPlayerAvatars()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Networking),
                    "Only server can despawn player avatars.");
                return;
            }

            foreach (var kvp in playerAvatars)
            {
                DespawnPlayerAvatar(kvp.Key);
            }
        }
        
        /// <summary>
        /// Despawn a specific player's avatar
        /// </summary>
        /// <param name="clientId">Client ID of the player whose avatar to despawn</param>
        private void DespawnPlayerAvatar(ulong clientId)
        {
            if (playerAvatars.TryGetValue(clientId, out var avatar) && avatar != null)
            {
                if (avatar.IsSpawned)
                {
                    avatar.Despawn();
                }
                playerAvatars.Remove(clientId);
                
                // Update player data
                if (connectedPlayers.TryGetValue(clientId, out var data))
                {
                    data.Avatar = null;
                }
                
                if (logPlayerEvents)
                {
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Networking, actorClientId: clientId),
                        "Player avatar despawned.");
                }
            }
        }
        
        /// <summary>
        /// Get an appropriate spawn point for a player
        /// </summary>
        /// <param name="spawnPoints">Available spawn points</param>
        /// <param name="spawnCount">Current spawn index</param>
        /// <returns>Transform of the selected spawn point</returns>
        private Transform GetSpawnPoint(Transform[] spawnPoints, int spawnCount)
        {
            if (spawnPoints.Length == 0)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Configuration),
                    "No spawn points available.");
                return null;
            }

            return spawnPoints[spawnCount % spawnPoints.Length];
        }
        
        #endregion
        
        #region Player Data Access
        
        /// <summary>
        /// Get player network data for a specific client
        /// </summary>
        /// <param name="clientId">Client ID to retrieve data for</param>
        /// <returns>PlayerNetworkData if found, null otherwise</returns>
        public PlayerNetworkData GetPlayerData(ulong clientId)
        {
            return connectedPlayers.TryGetValue(clientId, out var data) ? data : null;
        }
        
        /// <summary>
        /// Check if a player is currently connected
        /// </summary>
        /// <param name="clientId">Client ID to check</param>
        /// <returns>True if player is connected, false otherwise</returns>
        public bool IsPlayerConnected(ulong clientId)
        {
            return connectedPlayers.ContainsKey(clientId) && connectedPlayers[clientId].IsActive;
        }
        
        /// <summary>
        /// Get the avatar NetworkObject for a specific player
        /// </summary>
        /// <param name="clientId">Client ID to get avatar for</param>
        /// <returns>NetworkObject of the player's avatar, null if not found</returns>
        public NetworkObject GetPlayerAvatar(ulong clientId)
        {
            return playerAvatars.TryGetValue(clientId, out var avatar) ? avatar : null;
        }
        
        /// <summary>
        /// Update player position data for tracking purposes
        /// </summary>
        /// <param name="clientId">Client ID of the player</param>
        /// <param name="position">New position</param>
        public void UpdatePlayerPosition(ulong clientId, Vector3 position)
        {
            if (connectedPlayers.TryGetValue(clientId, out var playerData))
            {
                playerData.LastPosition = position;
                playerData.LastMovementTime = Time.time;
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Build a debug context for logging
        /// </summary>
        private GameDebugContext BuildContext(
            GameDebugMechanicTag mechanic = GameDebugMechanicTag.Networking,
            string subsystem = "PlayerManager",
            ulong? actorClientId = null)
        {
            string actor = actorClientId?.ToString();
            return new GameDebugContext(
                GameDebugCategory.Networking,
                GameDebugSystemTag.Networking,
                mechanic,
                subsystem,
                actor);
        }
        
        #endregion
    }
}