using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

namespace MOBA.Networking
{
    /// <summary>
    /// Object pool for network objects to reduce instantiation overhead
    /// Implements Flyweight pattern for memory-efficient network object management
    /// </summary>
    public class NetworkObjectPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] public GameObject prefab;
        [SerializeField] public int initialPoolSize = 10;
        [SerializeField] public int maxPoolSize = 50;
        [SerializeField] private bool autoExpand = true;

        private Queue<GameObject> availableObjects = new Queue<GameObject>();
        private HashSet<GameObject> activeObjects = new HashSet<GameObject>();
        private NetworkManager networkManager;

        private void Awake()
        {
            networkManager = NetworkManager.Singleton;
            // Don't prewarm here - wait for manual initialization to avoid null prefab issues
            UnityEngine.Debug.Log("[NetworkObjectPool] Awake() called - waiting for manual initialization");
        }

        /// <summary>
        /// Initialize the pool manually after prefab assignment
        /// </summary>
        public void InitializePool()
        {
            UnityEngine.Debug.Log("[NetworkObjectPool] InitializePool() called - starting prewarming");
            PrewarmPool();
        }

        private void PrewarmPool()
        {
            UnityEngine.Debug.Log($"[NetworkObjectPool] PrewarmPool() starting for prefab: {(prefab != null ? prefab.name : "NULL")} (target: {initialPoolSize} objects)");
            
            if (prefab == null)
            {
                UnityEngine.Debug.LogError("[NetworkObjectPool] ❌ Cannot prewarm pool - prefab is null!");
                return;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                UnityEngine.Debug.Log($"[NetworkObjectPool] Creating prewarm object {i + 1}/{initialPoolSize}");
                CreateNewObject();
            }

            UnityEngine.Debug.Log($"[NetworkObjectPool] ✅ Prewarmed pool with {initialPoolSize} objects for {prefab.name}");
        }

        private GameObject CreateNewObject()
        {
            UnityEngine.Debug.Log($"[NetworkObjectPool] CreateNewObject() called for prefab: {(prefab != null ? prefab.name : "NULL")}");
            
            if (prefab == null)
            {
                UnityEngine.Debug.LogError("[NetworkObjectPool] ❌ Cannot create object - prefab is null!");
                return null;
            }

            if (availableObjects.Count + activeObjects.Count >= maxPoolSize && !autoExpand)
            {
                UnityEngine.Debug.LogWarning($"[NetworkObjectPool] Pool limit reached for {prefab.name}");
                return null;
            }

            UnityEngine.Debug.Log($"[NetworkObjectPool] Instantiating object from prefab: {prefab.name}");
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);

            // Ensure it has NetworkObject component
            var networkObject = obj.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                networkObject = obj.AddComponent<NetworkObject>();
                UnityEngine.Debug.LogWarning($"[NetworkObjectPool] Added NetworkObject component to {prefab.name}");
            }

            availableObjects.Enqueue(obj);
            UnityEngine.Debug.Log($"[NetworkObjectPool] ✅ Successfully created object for {prefab.name}");
            return obj;
        }

        /// <summary>
        /// Get an object from the pool
        /// </summary>
        public GameObject Get()
        {
            GameObject obj = null;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (autoExpand)
            {
                obj = CreateNewObject();
                if (obj != null)
                {
                    availableObjects.Dequeue(); // Remove from available since we're using it
                }
            }

            if (obj != null)
            {
                obj.SetActive(true);
                activeObjects.Add(obj);

                // Spawn network object if server
                if (networkManager != null && networkManager.IsServer)
                {
                    var networkObject = obj.GetComponent<NetworkObject>();
                    if (networkObject != null && !networkObject.IsSpawned)
                    {
                        networkObject.Spawn();
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Return an object to the pool
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null || !activeObjects.Contains(obj))
            {
                UnityEngine.Debug.LogWarning($"[NetworkObjectPool] Attempted to return invalid object to pool");
                return;
            }

            // Despawn network object if server
            if (networkManager != null && networkManager.IsServer)
            {
                var networkObject = obj.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public (int available, int active, int total) GetPoolStats()
        {
            return (availableObjects.Count, activeObjects.Count, availableObjects.Count + activeObjects.Count);
        }

        /// <summary>
        /// Clear all objects from the pool
        /// </summary>
        public void ClearPool()
        {
            // Return all active objects
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    if (networkManager != null && networkManager.IsServer)
                    {
                        var networkObject = obj.GetComponent<NetworkObject>();
                        if (networkObject != null && networkObject.IsSpawned)
                        {
                            networkObject.Despawn();
                        }
                    }
                    Destroy(obj);
                }
            }

            activeObjects.Clear();

            // Store prefab name before clearing
            string prefabName = prefab != null ? prefab.name : "Unknown";

            // Destroy all available objects
            foreach (var obj in availableObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            availableObjects.Clear();

            UnityEngine.Debug.Log($"[NetworkObjectPool] Cleared pool for {prefabName}");
        }

        /// <summary>
        /// Expand pool by specified amount
        /// </summary>
        public void ExpandPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewObject();
            }

            string prefabName = prefab != null ? prefab.name : "Unknown";
            UnityEngine.Debug.Log($"[NetworkObjectPool] Expanded pool by {count} objects for {prefabName}");
        }

        /// <summary>
        /// Assign a prefab to this pool
        /// </summary>
        public void AssignPrefab(GameObject newPrefab)
        {
            prefab = newPrefab;
            UnityEngine.Debug.Log($"[NetworkObjectPool] Assigned prefab: {(prefab != null ? prefab.name : "NULL")}");
        }

        /// <summary>
        /// Get the number of active objects in the pool
        /// </summary>
        public int ActiveCount => activeObjects.Count;

        /// <summary>
        /// Get the number of available objects in the pool
        /// </summary>
        public int AvailableCount => availableObjects.Count;

        /// <summary>
        /// Get the total number of objects in the pool
        /// </summary>
        public int TotalCount => ActiveCount + AvailableCount;

        private void OnDestroy()
        {
            ClearPool();
        }
    }

    /// <summary>
    /// Manager for multiple network object pools
    /// </summary>
    public class NetworkObjectPoolManager : MonoBehaviour
    {
        private static NetworkObjectPoolManager _instance;
        public static NetworkObjectPoolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    UnityEngine.Debug.Log("[NetworkObjectPoolManager] Creating singleton instance...");
                    var go = new GameObject("NetworkObjectPoolManager");
                    _instance = go.AddComponent<NetworkObjectPoolManager>();
                    
                    // Only use DontDestroyOnLoad in play mode, not in editor
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                    
                    UnityEngine.Debug.Log($"[NetworkObjectPoolManager] ✅ Singleton created: {go.name}");
                }
                return _instance;
            }
        }

        private Dictionary<string, NetworkObjectPool> pools = new Dictionary<string, NetworkObjectPool>();

        private void Awake()
        {
            UnityEngine.Debug.Log("[NetworkObjectPoolManager] Awake() called");
            if (_instance != null && _instance != this)
            {
                UnityEngine.Debug.Log("[NetworkObjectPoolManager] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }
            _instance = this;
            UnityEngine.Debug.Log("[NetworkObjectPoolManager] ✅ Singleton instance established");
        }

        /// <summary>
        /// Create a new object pool
        /// </summary>
        public void CreatePool(string poolName, GameObject prefab, int initialSize = 10, int maxSize = 50)
        {
            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] CreatePool() called - Name: {poolName}, Prefab: {(prefab != null ? prefab.name : "NULL")}, Initial: {initialSize}, Max: {maxSize}");
            
            if (prefab == null)
            {
                UnityEngine.Debug.LogError($"[NetworkObjectPoolManager] ❌ Cannot create pool '{poolName}' - prefab is null!");
                return;
            }

            if (pools.ContainsKey(poolName))
            {
                UnityEngine.Debug.LogWarning($"[NetworkObjectPoolManager] Pool {poolName} already exists, skipping creation");
                return;
            }

            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] Creating pool GameObject: {poolName}_Pool");
            var poolObject = new GameObject($"{poolName}_Pool");
            poolObject.transform.SetParent(transform);

            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] Adding NetworkObjectPool component to {poolObject.name}");
            var pool = poolObject.AddComponent<NetworkObjectPool>();
            
            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] Assigning prefab {prefab.name} to pool component");
            pool.prefab = prefab;
            pool.initialPoolSize = initialSize;
            pool.maxPoolSize = maxSize;
            
            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] Manually initializing pool to avoid Awake timing issue");
            pool.InitializePool();

            pools[poolName] = pool;

            pools[poolName] = pool;

            UnityEngine.Debug.Log($"[NetworkObjectPoolManager] ✅ Created pool {poolName} with {initialSize} objects");
        }

        /// <summary>
        /// Get an object from the specified pool
        /// </summary>
        public GameObject GetFromPool(string poolName)
        {
            if (!pools.ContainsKey(poolName))
            {
                UnityEngine.Debug.LogError($"[NetworkObjectPoolManager] Pool {poolName} does not exist");
                return null;
            }

            return pools[poolName].Get();
        }

        /// <summary>
        /// Return an object to the specified pool
        /// </summary>
        public void ReturnToPool(string poolName, GameObject obj)
        {
            if (!pools.ContainsKey(poolName))
            {
                UnityEngine.Debug.LogError($"[NetworkObjectPoolManager] Pool {poolName} does not exist");
                return;
            }

            pools[poolName].Return(obj);
        }

        /// <summary>
        /// Get statistics for all pools
        /// </summary>
        public Dictionary<string, (int available, int active, int total)> GetAllPoolStats()
        {
            var stats = new Dictionary<string, (int, int, int)>();

            foreach (var kvp in pools)
            {
                stats[kvp.Key] = kvp.Value.GetPoolStats();
            }

            return stats;
        }

        /// <summary>
        /// Clear all pools
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var pool in pools.Values)
            {
                pool.ClearPool();
            }

            pools.Clear();
            UnityEngine.Debug.Log("[NetworkObjectPoolManager] Cleared all pools");
        }

        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
}