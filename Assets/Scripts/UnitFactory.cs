using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using MOBA.Debugging;

namespace MOBA
{
    /// <summary>
    /// Factory Pattern implementation for unit spawning in MOBA games
    /// Based on GoF Factory Pattern and Game Programming Patterns
    /// Provides centralized unit creation with networking support
    /// </summary>
    public class UnitFactory : NetworkBehaviour
    {
        [Header("Unit Prefabs")]
        [SerializeField] private GameObject[] unitPrefabs;
        [SerializeField] private Transform spawnParent;

        [Header("Factory Settings")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 100;

        // Shared pool storage for now
        private readonly Dictionary<UnitType, UnifiedObjectPool.GameObjectPool> localUnitPools = new();
        private readonly Dictionary<UnitType, UnifiedObjectPool.NetworkObjectPool> networkUnitPools = new();

        // Unit type enumeration
        public enum UnitType
        {
            ElomNusk,
            DOGE,
            NeutralCreep,
            Tower,
            Inhibitor
        }

        private void Awake()
        {
            InitializeFactory();
        }

        private void InitializeFactory()
        {
            if (spawnParent == null)
            {
                spawnParent = transform;
            }

            if (useObjectPooling)
            {
                InitializePools();
            }
        }

        private GameDebugContext BuildContext(GameDebugMechanicTag mechanic = GameDebugMechanicTag.General, string subsystem = null)
        {
            return new GameDebugContext(
                GameDebugCategory.GameLifecycle,
                GameDebugSystemTag.Core,
                mechanic,
                subsystem: subsystem ?? nameof(UnitFactory));
        }

        private void InitializePools()
        {
            localUnitPools.Clear();
            networkUnitPools.Clear();

            foreach (UnitType unitType in System.Enum.GetValues(typeof(UnitType)))
            {
                if ((int)unitType >= unitPrefabs.Length)
                {
                    continue;
                }

                var prefab = unitPrefabs[(int)unitType];
                if (prefab == null)
                {
                    continue;
                }

                bool hasNetworkObject = prefab.GetComponent<NetworkObject>() != null;
                string poolName = $"UnitFactory_{gameObject.GetInstanceID()}_{unitType}";

                if (hasNetworkObject)
                {
                    var pool = UnifiedObjectPool.GetNetworkObjectPool(
                        poolName,
                        prefab,
                        initialPoolSize,
                        maxPoolSize,
                        spawnParent);
                    if (pool != null)
                    {
                        networkUnitPools[unitType] = pool;
                    }
                    else
                    {
                        GameDebug.LogWarning(
                            BuildContext(GameDebugMechanicTag.Pooling, subsystem: unitType.ToString()),
                            "Failed to create network object pool; falling back to direct instantiation.");
                    }
                }
                else
                {
                    var pool = UnifiedObjectPool.GetGameObjectPool(
                        poolName,
                        prefab,
                        initialPoolSize,
                        maxPoolSize,
                        spawnParent);
                    if (pool != null)
                    {
                        localUnitPools[unitType] = pool;
                    }
                    else
                    {
                        GameDebug.LogWarning(
                            BuildContext(GameDebugMechanicTag.Pooling, subsystem: unitType.ToString()),
                            "Failed to create local object pool; falling back to direct instantiation.");
                    }
                }
            }
        }

        /// <summary>
        /// Creates a unit of the specified type at the given position
        /// </summary>
        public GameObject CreateUnit(UnitType type, Vector3 position, Quaternion rotation = default)
        {
            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            GameObject unit = null;
            GameObject prefabReference = ((int)type < unitPrefabs.Length) ? unitPrefabs[(int)type] : null;
            bool isNetworkedPrefab = prefabReference != null && prefabReference.GetComponent<NetworkObject>() != null;

            if (isNetworkedPrefab && NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            {
                GameDebug.LogWarning(
                    BuildContext(GameDebugMechanicTag.Spawning, subsystem: type.ToString()),
                    "Client attempted to create a networked unit directly. This request should be routed through the server.");
                return null;
            }

            bool hasNetworkPool = networkUnitPools.TryGetValue(type, out var networkPoolReference);

            if (useObjectPooling)
            {
                if (isNetworkedPrefab && hasNetworkPool)
                {
                    if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
                    {
                        unit = networkPoolReference?.Get();
                    }
                }

                if (unit == null && localUnitPools.TryGetValue(type, out var localPool) && localPool != null)
                {
                    unit = localPool.Get();
                }
            }

            if (unit == null)
            {
                if (prefabReference != null)
                {
                    unit = Instantiate(prefabReference, position, rotation, spawnParent);
                }
            }

            if (unit == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Spawning),
                    "Failed to create unit instance.",
                    ("UnitType", type));
                return null;
            }

            unit.transform.SetParent(spawnParent);
            unit.transform.SetPositionAndRotation(position, rotation);

            // Initialize network components if needed
            if (unit.TryGetComponent(out NetworkObject netObj))
            {
                // Only spawn if we're the server or host; pooled network objects will already be spawned
                if (!netObj.IsSpawned && (IsServer || (!IsClient || IsHost)))
                {
                    netObj.Spawn();
                }
            }

            // Initialize unit-specific components
            InitializeUnitComponents(unit, type);

            GameDebug.Log(BuildContext(GameDebugMechanicTag.Spawning),
                "Unit created.",
                ("UnitType", type),
                ("Position", position));
            return unit;
        }

        /// <summary>
        /// Networked unit creation for multiplayer
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateUnitServerRpc(UnitType type, Vector3 position, Quaternion rotation, ServerRpcParams rpcParams = default)
        {
            var unit = CreateUnit(type, position, rotation);

            // Notify clients
            if (unit != null)
            {
                CreateUnitClientRpc(type, position, rotation, unit.GetComponent<NetworkObject>().NetworkObjectId);
            }
        }

        [ClientRpc]
        private void CreateUnitClientRpc(UnitType type, Vector3 position, Quaternion rotation, ulong networkObjectId)
        {
            // Client-side unit creation is handled by NetworkObject.Spawn()
            // This RPC is mainly for synchronization
        }

        /// <summary>
        /// Returns a unit to the pool for reuse
        /// Enforces maxPoolSize to prevent memory bloat
        /// </summary>
        public void ReturnUnit(GameObject unit, UnitType type)
        {
            if (!useObjectPooling)
            {
                Destroy(unit);
                return;
            }

            if (unit.TryGetComponent<NetworkObject>(out _) && networkUnitPools.TryGetValue(type, out var networkPool) && networkPool != null)
            {
                if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer)
                {
                    networkPool.Return(unit);
                }
                else
                {
                    GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Pooling, subsystem: type.ToString()),
                        "Client attempted to return a network pooled unit. Only the server should manage pooled network units.");
                }
                return;
            }

            if (localUnitPools.TryGetValue(type, out var localPool) && localPool != null)
            {
                localPool.Return(unit);
                return;
            }

            Destroy(unit);
        }

        /// <summary>
        /// Pre-warms pools for better performance
        /// </summary>
        public void PrewarmPools()
        {
            // Pools are already pre-warmed in InitializePools
            GameDebug.Log(BuildContext(GameDebugMechanicTag.Pooling),
                "Pools pre-warmed during initialization.");
        }

        /// <summary>
        /// Gets pool statistics for debugging
        /// </summary>
        public Dictionary<UnitType, (int total, int available, int active)> GetPoolStats()
        {
            var stats = new Dictionary<UnitType, (int, int, int)>();

            foreach (var kvp in localUnitPools)
            {
                var poolStats = kvp.Value.GetStats();
                stats[kvp.Key] = (poolStats.total, poolStats.available, poolStats.active);
            }

            foreach (var kvp in networkUnitPools)
            {
                var poolStats = kvp.Value.GetStats();
                stats[kvp.Key] = (poolStats.total, poolStats.available, poolStats.active);
            }

            return stats;
        }

        private void InitializeUnitComponents(GameObject unit, UnitType type)
        {
            // Add type-specific initialization here
            switch (type)
            {
                case UnitType.ElomNusk:
                    InitializeElomNusk(unit);
                    break;
                case UnitType.DOGE:
                    InitializeDOGE(unit);
                    break;
                case UnitType.NeutralCreep:
                    InitializeNeutralCreep(unit);
                    break;
                case UnitType.Tower:
                    InitializeTower(unit);
                    break;
                case UnitType.Inhibitor:
                    InitializeInhibitor(unit);
                    break;
            }
        }

        private void InitializeElomNusk(GameObject unit)
        {
            // Add Elom Nusk specific components
            if (!unit.TryGetComponent(out SimpleAbilitySystem abilitySystem))
            {
                abilitySystem = unit.AddComponent<SimpleAbilitySystem>();
            }

            abilitySystem.SynchroniseAbilities();
        }

        private void InitializeDOGE(GameObject unit)
        {
            // Add DOGE specific components
            // Evolution mechanics would be handled here
        }

        private void InitializeNeutralCreep(GameObject unit)
        {
            // Add neutral creep AI and loot components
        }

        private void InitializeTower(GameObject unit)
        {
            // Add tower defense components
        }

        private void InitializeInhibitor(GameObject unit)
        {
            // Add inhibitor respawn mechanics
        }

        // Legacy OnGUI debug output removed; integrate with dedicated debugging tools if needed.
    }
}
