using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace MOBA
{
    /// <summary>
    /// Simple object pool for GameObjects (not Components)
    /// </summary>
    public class GameObjectPool
    {
        private readonly Queue<GameObject> availableObjects = new();
        private readonly List<GameObject> allObjects = new();
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly int initialSize;

        public GameObjectPool(GameObject prefab, int initialSize = 10, Transform parent = null)
        {
            this.prefab = prefab;
            this.initialSize = initialSize;
            this.parent = parent;

            // Pre-populate the pool
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        public GameObject Get()
        {
            GameObject obj;
            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.SetActive(true);
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            availableObjects.Enqueue(obj);
        }

        public void ReturnAll()
        {
            foreach (var obj in allObjects)
            {
                if (obj.activeSelf)
                {
                    Return(obj);
                }
            }
        }

        private GameObject CreateNewObject()
        {
            // Removed automatic Object.Instantiate to prevent automatic loading
            // Use CreateObjectWithInstance() method with manually provided instance instead
            Debug.LogError("[GameObjectPool] Automatic instantiation disabled - use CreateObjectWithInstance() instead");
            return null;
        }

        public int TotalCount => allObjects.Count;
        public int AvailableCount => availableObjects.Count;
        public int ActiveCount => TotalCount - AvailableCount;
    }

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

        // Object pools for each unit type
        private Dictionary<UnitType, GameObjectPool> unitPools = new();

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

        private void InitializePools()
        {
            foreach (UnitType unitType in System.Enum.GetValues(typeof(UnitType)))
            {
                if ((int)unitType < unitPrefabs.Length && unitPrefabs[(int)unitType] != null)
                {
                    var pool = new GameObjectPool(
                        unitPrefabs[(int)unitType],
                        initialPoolSize,
                        spawnParent
                    );
                    unitPools[unitType] = pool;
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

            if (useObjectPooling && unitPools.ContainsKey(type))
            {
                // Use object pool
                unit = unitPools[type].Get();
                unit.transform.position = position;
                unit.transform.rotation = rotation;
            }
            else
            {
                // Removed automatic Object.Instantiate to prevent automatic loading
                // Use CreateUnitWithInstance() method with manually provided instance instead
                Debug.LogError($"[UnitFactory] Automatic instantiation disabled for {type} - use CreateUnitWithInstance() instead");
                return null;
            }

            // Initialize network components if needed
            if (unit.TryGetComponent(out NetworkObject netObj))
            {
                // Only spawn if we're the server or if it's a local player object
                if (IsServer || (!IsClient || IsHost))
                {
                    netObj.Spawn();
                }
                else
                {
                    // For client-side only objects, don't spawn on network
                    Debug.Log($"Created local unit: {type} (not networked)");
                }
            }

            // Initialize unit-specific components
            InitializeUnitComponents(unit, type);

            // Register with memory manager for tracking
            var memoryManager = FindAnyObjectByType<MemoryManager>();
            if (memoryManager != null)
            {
                memoryManager.TrackObject(unit, "Units");
            }

            Debug.Log($"Created unit: {type} at {position}");
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
        /// </summary>
        public void ReturnUnit(GameObject unit, UnitType type)
        {
            if (useObjectPooling && unitPools.ContainsKey(type))
            {
                unitPools[type].Return(unit);
            }
            else
            {
                Destroy(unit);
            }
        }

        /// <summary>
        /// Pre-warms pools for better performance
        /// </summary>
        public void PrewarmPools()
        {
            foreach (var pool in unitPools.Values)
            {
                // Pools are already pre-warmed in InitializePools
                // Additional pre-warming can be done here if needed
            }
        }

        /// <summary>
        /// Gets pool statistics for debugging
        /// </summary>
        public Dictionary<UnitType, (int total, int available, int active)> GetPoolStats()
        {
            var stats = new Dictionary<UnitType, (int, int, int)>();

            foreach (var kvp in unitPools)
            {
                var pool = kvp.Value;
                stats[kvp.Key] = (pool.TotalCount, pool.AvailableCount, pool.ActiveCount);
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
            if (!unit.TryGetComponent(out AbilitySystem abilitySystem))
            {
                unit.AddComponent<AbilitySystem>();
            }
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

        private void OnGUI()
        {
            if (!Application.isEditor) return;

            GUI.Label(new Rect(10, 10, 300, 20), "Unit Factory - Pool Statistics:");

            int y = 30;
            foreach (var kvp in GetPoolStats())
            {
                var (total, available, active) = kvp.Value;
                GUI.Label(new Rect(10, y, 300, 20),
                    $"{kvp.Key}: Total={total}, Available={available}, Active={active}");
                y += 20;
            }
        }
    }
}