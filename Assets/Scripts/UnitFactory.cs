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

        // Simple pool storage for now
        private Dictionary<UnitType, Queue<GameObject>> simplePools = new();

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
            foreach (UnitType unitType in System.Enum.GetValues(typeof(UnitType)))
            {
                if ((int)unitType < unitPrefabs.Length && unitPrefabs[(int)unitType] != null)
                {
                    simplePools[unitType] = new Queue<GameObject>();
                    
                    // Pre-populate with a few objects
                    for (int i = 0; i < initialPoolSize; i++)
                    {
                        GameObject obj = Instantiate(unitPrefabs[(int)unitType], spawnParent);
                        obj.SetActive(false);
                        simplePools[unitType].Enqueue(obj);
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

            if (useObjectPooling && simplePools.ContainsKey(type))
            {
                // Use simple pool
                var pool = simplePools[type];
                if (pool.Count > 0)
                {
                    unit = pool.Dequeue();
                    if (unit != null)
                    {
                        unit.transform.position = position;
                        unit.transform.rotation = rotation;
                        unit.SetActive(true);
                    }
                }
                else
                {
                    // Create new if pool is empty
                    unit = Instantiate(unitPrefabs[(int)type], position, rotation, spawnParent);
                }
            }
            else
            {
                // Fallback to instantiation
                if ((int)type < unitPrefabs.Length && unitPrefabs[(int)type] != null)
                {
                    unit = Instantiate(unitPrefabs[(int)type], position, rotation, spawnParent);
                }
            }

            if (unit == null)
            {
                GameDebug.LogError(BuildContext(GameDebugMechanicTag.Spawning),
                    "Failed to create unit instance.",
                    ("UnitType", type));
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
                    GameDebug.Log(BuildContext(GameDebugMechanicTag.Spawning, subsystem: "Local"),
                        "Created non-networked unit instance.",
                        ("UnitType", type));
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
            if (useObjectPooling && simplePools.ContainsKey(type))
            {
                var pool = simplePools[type];
                
                // Enforce max pool size to prevent unlimited growth
                if (pool.Count < maxPoolSize)
                {
                    unit.SetActive(false);
                    pool.Enqueue(unit);
                }
                else
                {
                    // Pool is at capacity, destroy the unit instead
                    Destroy(unit);
                    GameDebug.LogWarning(BuildContext(GameDebugMechanicTag.Pooling),
                        "Pool at capacity; destroying unit.",
                        ("UnitType", type),
                        ("MaxPoolSize", maxPoolSize));
                }
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

            foreach (var kvp in simplePools)
            {
                int available = kvp.Value.Count;
                int total = available + 10; // Estimate, since we don't track total created
                int active = total - available;
                stats[kvp.Key] = (total, available, active);
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
                unit.AddComponent<SimpleAbilitySystem>();
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

        // Legacy OnGUI debug output removed; integrate with dedicated debugging tools if needed.
    }
}
